using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.IO;
using MediaForge.Models;
using MediaForge.Models.Projects;
using MediaForge.Services.Projects;
using MediaForge.Services.Runtime;
using MediaForge.Services.Session;
using Microsoft.Win32;

namespace MediaForge;

public partial class MainWindow
{
    private readonly ProjectService _projectService = new();
    private readonly ProjectRecoveryStore _recoveryStore = new();
    private readonly RecentProjectStore _recentProjectStore = new();
    private readonly ProjectRelinkService _relinkService = new();
    private ProjectAutosaveCoordinator? _autosaveCoordinator;
    private Guid _projectId = Guid.NewGuid();
    private DateTimeOffset _projectCreatedUtc = DateTimeOffset.UtcNow;
    private string? _currentProjectPath;
    private bool _projectReadOnly;
    private string? _projectReadOnlyReason;
    private bool _portableProject;
    private bool _suppressProjectDirty = true;
    private ProjectDocument? _loadedProjectBasis;
    private IReadOnlyList<ProjectSourceIssue> _sourceIssues = [];

    private void InitializeProjectFeatures()
    {
        _autosaveCoordinator = new ProjectAutosaveCoordinator(_recoveryStore);
        _autosaveCoordinator.SaveFailed += AutosaveCoordinator_SaveFailed;
        _autosaveCoordinator.SnapshotSaved += AutosaveCoordinator_SnapshotSaved;
        ContentRendered += MainWindow_ContentRendered;
    }

    private void CompleteProjectInitialization()
    {
        // WPF can raise initial TextChanged/SelectionChanged/toggle events while the
        // first window is still being rendered. Keep dirty tracking suppressed until
        // ContentRendered so a fresh project does not become dirty without user input.
        UpdateProjectStatus();
        UpdateProjectControlState();
    }

    private void HandleProjectSessionChanged(ProjectSessionChangedEventArgs e)
    {
        UpdateProjectStatus();
        if (_suppressProjectDirty || _projectReadOnly) return;
        if (e.Kind is ProjectSessionChangeKind.MarkedClean or ProjectSessionChangeKind.Loaded) return;
        ScheduleAutosave();
    }

    private void ScheduleAutosave()
    {
        if (_suppressProjectDirty || _projectReadOnly || !_session.IsDirty || _autosaveCoordinator is null) return;
        try
        {
            _autosaveCoordinator.Schedule(BuildProjectDocument(_currentProjectPath), _currentProjectPath);
        }
        catch (Exception ex)
        {
            AppendLog($"Autosave could not be scheduled: {ex.Message}");
        }
    }

    private void AutosaveCoordinator_SaveFailed(object? sender, string message) =>
        Dispatcher.BeginInvoke(() => AppendLog($"Recovery snapshot failed: {message}"));

    private void AutosaveCoordinator_SnapshotSaved(object? sender, RecoverySnapshotInfo snapshot) =>
        Dispatcher.BeginInvoke(() => AppendLog($"Recovery snapshot saved: {snapshot.SnapshotPath}"));

    private void RegisterProjectOptionTracking()
    {
        OptionsTabs.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(ProjectTextChanged));
        OptionsTabs.AddHandler(Selector.SelectionChangedEvent, new SelectionChangedEventHandler(ProjectSelectionChanged));
        OptionsTabs.AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(ProjectOptionChanged));
        OptionsTabs.AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler(ProjectOptionChanged));
        OptionsTabs.AddHandler(RangeBase.ValueChangedEvent, new RoutedPropertyChangedEventHandler<double>(ProjectSliderChanged));
    }

    private void ProjectTextChanged(object sender, TextChangedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, LogText)) return;
        MarkProjectOptionsChanged();
    }

    private void ProjectSelectionChanged(object sender, SelectionChangedEventArgs e) => MarkProjectOptionsChanged();
    private void ProjectSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => MarkProjectOptionsChanged();

    private void ProjectOptionChanged(object sender, RoutedEventArgs e) => MarkProjectOptionsChanged();

    private void MarkProjectOptionsChanged()
    {
        if (_suppressProjectDirty || _projectReadOnly || _isRunning) return;
        _portableProject = PortableProjectCheck.IsChecked == true;
        _session.MarkDirty(ProjectSessionChangeKind.ProjectOptionsChanged);
    }

    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || !ConfirmProjectTransition()) return;
        _autosaveCoordinator?.CancelPending();
        _suppressProjectDirty = true;
        try
        {
            _projectId = Guid.NewGuid();
            _projectCreatedUtc = DateTimeOffset.UtcNow;
            _currentProjectPath = null;
            _projectReadOnly = false;
            _projectReadOnlyReason = null;
            _portableProject = false;
            _loadedProjectBasis = null;
            _sourceIssues = [];
            PortableProjectCheck.IsChecked = false;
            ResetWorkflowForNewProject();
            _session.ReplaceJobs([], markDirty: false);
            OptionsTabs.SelectedIndex = 0;
        }
        finally
        {
            _suppressProjectDirty = false;
        }
        AppendLog("Created a new unsaved project.");
        UpdateProjectStatus();
        UpdateProjectControlState();
    }

    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || !ConfirmProjectTransition()) return;
        var dialog = new OpenFileDialog
        {
            Title = "Open MediaForge project",
            Filter = "MediaForge projects (*.mediaforge)|*.mediaforge|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) == true) OpenProjectPath(dialog.FileName);
    }

    private void RecentProjects_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;
        var entries = _recentProjectStore.Load();
        var menu = new ContextMenu { PlacementTarget = RecentProjectsButton };
        if (entries.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = "No recent projects", IsEnabled = false });
        }
        else
        {
            foreach (var entry in entries)
            {
                var item = new MenuItem
                {
                    Header = $"{Path.GetFileNameWithoutExtension(entry.Path)} — {entry.Path}",
                    ToolTip = entry.Path
                };
                item.Click += (_, _) =>
                {
                    if (!ConfirmProjectTransition()) return;
                    if (!File.Exists(entry.Path))
                    {
                        TryRemoveRecentProject(entry.Path);
                        MessageBox.Show(this, "The recent project no longer exists and was removed from the list.",
                            "Recent project missing", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    OpenProjectPath(entry.Path);
                };
                menu.Items.Add(item);
            }
        }
        menu.IsOpen = true;
    }

    private void SaveProject_Click(object sender, RoutedEventArgs e) => TrySaveProject(saveAs: false);
    private void SaveProjectAs_Click(object sender, RoutedEventArgs e) => TrySaveProject(saveAs: true);

    private bool TrySaveProject(bool saveAs)
    {
        if (_isRunning) return false;
        if (_projectReadOnly)
        {
            MessageBox.Show(this, _projectReadOnlyReason ?? "This project is read-only and cannot be saved by this build.",
                "Read-only project", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var destination = _currentProjectPath;
        if (saveAs || string.IsNullOrWhiteSpace(destination))
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save MediaForge project",
                Filter = "MediaForge projects (*.mediaforge)|*.mediaforge",
                AddExtension = true,
                DefaultExt = ".mediaforge",
                FileName = string.IsNullOrWhiteSpace(destination) ? "MediaForge project.mediaforge" : Path.GetFileName(destination),
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != true) return false;
            destination = EnsureProjectExtension(dialog.FileName);
        }

        try
        {
            var document = BuildProjectDocument(destination);
            _projectService.Save(document, destination);
            _currentProjectPath = EnsureProjectExtension(destination);
            _loadedProjectBasis = document;
            _projectCreatedUtc = document.CreatedUtc;
            _portableProject = document.ProjectRoot is not null;
            _session.MarkClean();
            _autosaveCoordinator?.CancelPending();
            _recoveryStore.PruneAfterManualSave(document.ProjectId, document.ModifiedUtc);
            TryAddRecentProject(_currentProjectPath, document.ProjectId);
            AppendLog($"Project saved: {_currentProjectPath}");
            UpdateProjectStatus();
            UpdateProjectControlState();
            return true;
        }
        catch (Exception ex)
        {
            AppendLog($"Project save failed: {ex}");
            MessageBox.Show(this, ex.Message, "Could not save project", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void OpenProjectPath(string path)
    {
        try
        {
            var result = _projectService.Load(path);
            OpenProjectDocument(result, markDirty: false);
            TryAddRecentProject(path, result.Document.ProjectId);
            AppendLog($"Project opened: {path}");
            ShowSourceIssueSummary(result.SourceIssues);
            if (result.IsReadOnly)
            {
                MessageBox.Show(this, result.ReadOnlyReason, "Project opened read-only", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Project open failed: {ex}");
            MessageBox.Show(this, ex.Message, "Could not open project", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenProjectDocument(ProjectLoadResult result, bool markDirty)
    {
        _autosaveCoordinator?.CancelPending();
        _suppressProjectDirty = true;
        try
        {
            _projectId = result.Document.ProjectId;
            _projectCreatedUtc = result.Document.CreatedUtc;
            _currentProjectPath = string.IsNullOrWhiteSpace(result.ProjectPath) ? null : Path.GetFullPath(result.ProjectPath);
            _projectReadOnly = result.IsReadOnly;
            _projectReadOnlyReason = result.ReadOnlyReason;
            _portableProject = result.Document.ProjectRoot is not null;
            _loadedProjectBasis = result.Document;
            _sourceIssues = result.SourceIssues;
            ApplySettings(ProjectDocumentMapper.CreateEffectiveSettings(result.Document));
            LoadProjectWorkflowState(result.Document);
            PortableProjectCheck.IsChecked = _portableProject;
            OptionsTabs.SelectedIndex = Math.Clamp(result.Document.UiState.SelectedTabIndex, 0, OptionsTabs.Items.Count - 1);
            _session.ReplaceJobs(result.Jobs, markDirty);
            RefreshAllJobWorkflowDisplays();
            var selectedId = result.Document.UiState.SelectedItemId;
            if (selectedId.HasValue)
            {
                JobsGrid.SelectedItem = _session.Jobs.FirstOrDefault(job => job.Id == selectedId.Value);
            }
        }
        finally
        {
            _suppressProjectDirty = false;
        }
        UpdateOutputControls();
        UpdateSummary();
        UpdatePreviewEditButton();
        UpdateProjectStatus();
        UpdateProjectControlState();
        if (markDirty) ScheduleAutosave();
    }

    private void RelinkMissing_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || _projectReadOnly) return;
        var document = _loadedProjectBasis ?? BuildProjectDocument(_currentProjectPath);
        if (!_sourceIssues.Any(issue => issue.Kind == ProjectSourceIssueKind.Missing))
        {
            MessageBox.Show(this, "This project has no missing source references.", "Relink sources", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = "Choose a folder to search recursively for missing source files",
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var plan = _relinkService.Plan(document, dialog.FolderName, _currentProjectPath);
            if (plan.ExactCount == 0 && plan.ChangedCount == 0)
            {
                MessageBox.Show(this,
                    $"No unique matches were found. Ambiguous: {plan.AmbiguousCount}; unresolved: {plan.UnresolvedCount}.",
                    "Relink sources", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var applyExact = plan.ExactCount > 0 && MessageBox.Show(this,
                $"Found {plan.ExactCount} unique exact fingerprint match(es). Apply these relinks?",
                "Confirm exact relinks", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
            var applyChanged = plan.ChangedCount > 0 && MessageBox.Show(this,
                $"Found {plan.ChangedCount} unique filename match(es) whose size or modified time changed. Apply these changed-file relinks?",
                "Confirm changed sources", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
            if (!applyExact && !applyChanged) return;

            var applied = _relinkService.Apply(document, plan, applyExact, applyChanged);
            var refreshed = _projectService.Materialize(document, _currentProjectPath);
            _loadedProjectBasis = document;
            _sourceIssues = refreshed.SourceIssues;
            _suppressProjectDirty = true;
            try { _session.ReplaceJobs(refreshed.Jobs, markDirty: true); }
            finally { _suppressProjectDirty = false; }
            AppendLog($"Relinked {applied.ExactApplied} exact and {applied.ChangedApplied} changed source(s); {applied.Remaining} unresolved or ambiguous.");
            UpdateProjectStatus();
            UpdateProjectControlState();
            ScheduleAutosave();
            ShowSourceIssueSummary(_sourceIssues);
        }
        catch (Exception ex)
        {
            AppendLog($"Relink failed: {ex}");
            MessageBox.Show(this, ex.Message, "Could not relink sources", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MainWindow_ContentRendered(object? sender, EventArgs e)
    {
        ContentRendered -= MainWindow_ContentRendered;

        // Initial control/binding churn is complete. User-driven project changes from
        // this point forward must mark the project dirty.
        RegisterProjectOptionTracking();
        _suppressProjectDirty = false;
        UpdateProjectStatus();
        UpdateProjectControlState();

        Dispatcher.BeginInvoke(new Action(ShowRecoveryPromptSafe));
    }

    private void ShowRecoveryPromptSafe()
    {
        try
        {
            ShowRecoveryPrompt();
        }
        catch (Exception ex)
        {
            var diagnosticPath = StartupDiagnostics.TryWrite("recovery-prompt", ex);
            AppendLog($"Recovery prompt could not be opened: {ex}");
            var diagnosticDetail = string.IsNullOrWhiteSpace(diagnosticPath)
                ? string.Empty
                : $"\n\nDiagnostic file:\n{diagnosticPath}";
            MessageBox.Show(this,
                $"MediaForge opened without recovery because the recovery prompt failed.\n\n{ex.Message}{diagnosticDetail}",
                "Recovery unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ShowRecoveryPrompt()
    {
        var snapshots = _recoveryStore.GetRecoverableSnapshots();
        while (snapshots.Count > 0)
        {
            var dialog = new RecoveryDialog(snapshots) { Owner = this };
            dialog.ShowDialog();
            var selected = dialog.SelectedSnapshot;
            if (dialog.Action == RecoveryDialogAction.Later || selected is null) return;
            if (dialog.Action == RecoveryDialogAction.Discard)
            {
                _recoveryStore.Discard(selected.SnapshotPath);
                snapshots = _recoveryStore.GetRecoverableSnapshots();
                continue;
            }

            var canonicalPath = selected.Snapshot.CanonicalProjectPath;
            var readOnly = dialog.Action == RecoveryDialogAction.OpenReadOnly;
            var reason = readOnly ? "This recovery snapshot was opened read-only by request." : null;
            var result = _projectService.Materialize(selected.Snapshot.Document, canonicalPath, readOnly, reason);
            OpenProjectDocument(result, markDirty: !readOnly);
            AppendLog(readOnly
                ? $"Opened recovery snapshot read-only: {selected.SnapshotPath}"
                : $"Recovered project snapshot: {selected.SnapshotPath}");
            ShowSourceIssueSummary(result.SourceIssues);
            return;
        }
    }

    private bool ConfirmProjectTransition()
    {
        if (!_session.IsDirty) return true;
        WriteRecoveryBeforeRiskyTransition();
        if (_projectReadOnly)
        {
            return MessageBox.Show(this,
                "The read-only project has unsaved in-memory changes. Discard them and continue?",
                "Discard read-only changes", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        var answer = MessageBox.Show(this,
            "Save changes to the current project before continuing? A separate recovery snapshot has also been written when possible.",
            "Unsaved project changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
        return answer switch
        {
            MessageBoxResult.Yes => TrySaveProject(saveAs: false),
            MessageBoxResult.No => true,
            _ => false
        };
    }

    private void WriteRecoveryBeforeRiskyTransition()
    {
        if (!_session.IsDirty || _projectReadOnly) return;
        try
        {
            _autosaveCoordinator?.CancelPending();
            var snapshot = _recoveryStore.Save(BuildProjectDocument(_currentProjectPath), _currentProjectPath);
            AppendLog($"Recovery snapshot saved before transition: {snapshot.SnapshotPath}");
        }
        catch (Exception ex)
        {
            AppendLog($"Recovery snapshot could not be written before transition: {ex.Message}");
        }
    }

    private ProjectDocument BuildProjectDocument(string? destinationPath)
    {
        Guid? selectedId = JobsGrid.SelectedItem is MediaJob selected ? selected.Id : null;
        return ProjectDocumentMapper.Capture(
            _session.Jobs,
            CaptureCurrentSettings(),
            destinationPath,
            _projectId,
            _projectCreatedUtc,
            PortableProjectCheck.IsChecked == true,
            _loadedProjectBasis,
            OptionsTabs.SelectedIndex,
            selectedId,
            GetGlobalPresetForPersistence());
    }

    private AppSettings CaptureCurrentSettings() => new()
    {
        FfmpegPath = FfmpegPathText.Text.Trim(),
        OutputBesideSource = OutputBesideSourceCheck.IsChecked == true,
        OutputFolder = OutputFolderText.Text.Trim(),
        PreserveFolderTree = PreserveTreeCheck.IsChecked == true,
        PreserveTimestamps = PreserveTimestampsCheck.IsChecked == true,
        StripMetadata = StripMetadataCheck.IsChecked == true,
        FileSuffix = SuffixText.Text,
        CollisionPolicy = SelectedTag(CollisionCombo),
        ParallelJobs = ParseInt(SelectedTag(ParallelJobsCombo), 2),
        ImageFormat = SelectedTag(ImageFormatCombo),
        ImageQuality = (int)Math.Round(ImageQualitySlider.Value),
        ImageResizeMode = SelectedTag(ImageResizeModeCombo),
        ImageWidth = ParseInt(ImageWidthText.Text, 1920),
        ImageHeight = ParseInt(ImageHeightText.Text, 1080),
        ImageScalePercent = ParseInt(ImagePercentText.Text, 100),
        ImageAspectRatio = SelectedTag(ImageAspectRatioCombo),
        ImageCustomAspectWidth = ParseDouble(ImageCustomAspectWidthText.Text, 16),
        ImageCustomAspectHeight = ParseDouble(ImageCustomAspectHeightText.Text, 9),
        VideoContainer = SelectedTag(VideoContainerCombo),
        VideoCodec = SelectedTag(VideoCodecCombo),
        VideoCrf = (int)Math.Round(VideoCrfSlider.Value),
        VideoPreset = SelectedTag(VideoPresetCombo),
        VideoResolution = SelectedTag(VideoResolutionCombo),
        VideoWidth = ParseInt(VideoWidthText.Text, 1920),
        VideoHeight = ParseInt(VideoHeightText.Text, 1080),
        VideoResizeMode = SelectedTag(VideoResizeModeCombo),
        VideoAspectRatio = SelectedTag(VideoAspectRatioCombo),
        VideoCustomAspectWidth = ParseDouble(VideoCustomAspectWidthText.Text, 16),
        VideoCustomAspectHeight = ParseDouble(VideoCustomAspectHeightText.Text, 9),
        VideoFps = SelectedTag(VideoFpsCombo),
        VideoCustomFps = ParseDouble(VideoCustomFpsText.Text, 30),
        VideoAudioCodec = SelectedTag(VideoAudioCodecCombo),
        VideoAudioBitrate = Math.Clamp(ParseInt(VideoAudioBitrateText.Text, 192), 1, 512),
        ExtractAudioOnly = ExtractAudioCheck.IsChecked == true,
        AudioFormat = SelectedTag(AudioFormatCombo),
        AudioBitrate = Math.Clamp(ParseInt(AudioBitrateText.Text, 192), 1, 512),
        AudioSampleRate = SelectedTag(AudioSampleRateCombo),
        AudioChannels = SelectedTag(AudioChannelsCombo),
        AudioNormalize = AudioNormalizeCheck.IsChecked == true
    };

    private void ShowSourceIssueSummary(IReadOnlyList<ProjectSourceIssue> issues)
    {
        _sourceIssues = issues;
        UpdateProjectControlState();
        if (issues.Count == 0) return;
        var missing = issues.Count(issue => issue.Kind == ProjectSourceIssueKind.Missing);
        var changed = issues.Count(issue => issue.Kind == ProjectSourceIssueKind.FingerprintChanged);
        var unsafePaths = issues.Count(issue => issue.Kind == ProjectSourceIssueKind.UnsafeRelativePath);
        var message = $"Project source review: {missing} missing, {changed} fingerprint-changed, {unsafePaths} unsafe relative path(s).";
        AppendLog(message);
        if (missing > 0 && !_projectReadOnly)
        {
            var answer = MessageBox.Show(this,
                message + Environment.NewLine + Environment.NewLine + "Choose a folder now to relink missing sources?",
                "Project source review", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer == MessageBoxResult.Yes) RelinkMissing_Click(this, new RoutedEventArgs());
        }
        else
        {
            MessageBox.Show(this, message, "Project source review", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void TryAddRecentProject(string path, Guid projectId)
    {
        try { _recentProjectStore.Add(path, projectId); }
        catch (Exception ex) { AppendLog($"Recent-project list could not be updated: {ex.Message}"); }
    }

    private void TryRemoveRecentProject(string path)
    {
        try { _recentProjectStore.Remove(path); }
        catch (Exception ex) { AppendLog($"Recent-project list could not be updated: {ex.Message}"); }
    }

    private void UpdateProjectStatus()
    {
        if (ProjectStatusText is null) return;
        var name = string.IsNullOrWhiteSpace(_currentProjectPath)
            ? "Unsaved project"
            : Path.GetFileNameWithoutExtension(_currentProjectPath);
        var dirty = _session.IsDirty ? " *" : string.Empty;
        var mode = _projectReadOnly ? " — read-only" : _portableProject ? " — portable paths" : string.Empty;
        ProjectStatusText.Text = name + dirty + mode;
        ProjectStatusText.ToolTip = _currentProjectPath ?? "This project has not been saved yet.";
        Title = $"MediaForge — {name}{dirty}{(_projectReadOnly ? " (read-only)" : string.Empty)}";
    }

    private void UpdateProjectControlState()
    {
        if (NewProjectButton is null) return;
        var canEdit = !_isRunning && !_projectReadOnly;
        NewProjectButton.IsEnabled = !_isRunning;
        OpenProjectButton.IsEnabled = !_isRunning;
        RecentProjectsButton.IsEnabled = !_isRunning;
        SaveProjectButton.IsEnabled = canEdit;
        SaveProjectAsButton.IsEnabled = canEdit;
        PortableProjectCheck.IsEnabled = canEdit;
        RelinkMissingButton.IsEnabled = canEdit &&
            _sourceIssues.Any(issue => issue.Kind == ProjectSourceIssueKind.Missing);
        AddFilesButton.IsEnabled = canEdit;
        AddFolderButton.IsEnabled = canEdit;
        RemoveButton.IsEnabled = canEdit;
        ClearButton.IsEnabled = canEdit;
        RetryButton.IsEnabled = canEdit;
        StartButton.IsEnabled = canEdit;
        OptionsTabs.IsEnabled = canEdit;
        UpdatePreviewEditButton();
        UpdateWorkflowControlState();
    }

    private void DisposeProjectFeatures()
    {
        ContentRendered -= MainWindow_ContentRendered;
        if (_autosaveCoordinator is not null)
        {
            _autosaveCoordinator.SaveFailed -= AutosaveCoordinator_SaveFailed;
            _autosaveCoordinator.SnapshotSaved -= AutosaveCoordinator_SnapshotSaved;
            _autosaveCoordinator.Dispose();
            _autosaveCoordinator = null;
        }
    }

    private static string EnsureProjectExtension(string path) =>
        string.Equals(Path.GetExtension(path), ".mediaforge", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(path + ".mediaforge");
}
