using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MediaForge.Models;
using MediaForge.Models.Presets;
using MediaForge.Models.Projects;
using MediaForge.Models.Queue;
using MediaForge.Services.Options;
using MediaForge.Services.Presets;
using MediaForge.Services.Queue;
using MediaForge.Services.Runtime;
using MediaForge.Services.Session;
using Microsoft.Win32;

namespace MediaForge;

public partial class MainWindow
{
    private readonly PresetService _presetService = new();
    private readonly QueueService _queueService = new();
    private readonly QueueEstimateService _queueEstimateService = new();
    private IReadOnlyList<PresetDocument> _presetCatalogue = [];
    private Guid? _globalPresetId;
    private PresetDocument? _globalPresetFallback;
    private QueueDocument? _loadedQueueBasis;
    private bool _workflowFeaturesAvailable = true;
    private string? _workflowInitializationError;

    private sealed record PresetChoice(Guid? Id, string DisplayName, PresetDocument? Preset);

    private void InitializeWorkflowFeaturesSafe()
    {
        try
        {
            InitializeWorkflowFeatures();
        }
        catch (Exception ex)
        {
            _workflowFeaturesAvailable = false;
            _workflowInitializationError = ex.Message;
            _globalPresetId = null;
            _globalPresetFallback = null;
            _presetCatalogue = [];

            var choices = new List<PresetChoice>
            {
                new(null, "Global settings only", null)
            };
            PresetCombo.ItemsSource = choices;
            PresetCombo.DisplayMemberPath = nameof(PresetChoice.DisplayName);
            PresetCombo.SelectedIndex = 0;

            var diagnosticPath = StartupDiagnostics.TryWrite("workflow-initialization", ex);
            AppendLog($"PH10 preset/queue workflow initialization was disabled: {ex.Message}");
            if (!string.IsNullOrWhiteSpace(diagnosticPath))
            {
                AppendLog($"Startup diagnostic: {diagnosticPath}");
            }
            UpdateWorkflowControlState();
        }
    }

    private void InitializeWorkflowFeatures()
    {
        var state = _presetService.LoadState();
        _globalPresetId = state.DefaultPresetId;
        RefreshPresetCatalogue(_globalPresetId);
        RefreshAllJobWorkflowDisplays();
        UpdateWorkflowControlState();
    }

    private void ResetWorkflowForNewProject()
    {
        _loadedQueueBasis = null;
        _globalPresetFallback = null;
        if (!_workflowFeaturesAvailable)
        {
            _globalPresetId = null;
            return;
        }
        _globalPresetId = _presetService.LoadState().DefaultPresetId;
        RefreshPresetCatalogue(_globalPresetId);
    }

    private void LoadProjectWorkflowState(ProjectDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _loadedQueueBasis = null;
        _globalPresetId = document.SelectedPresetId;
        _globalPresetFallback = document.SelectedPresetId.HasValue && document.SelectedPresetSnapshot is not null
            ? new PresetDocument
            {
                Id = document.SelectedPresetId.Value,
                Version = document.SelectedPresetVersion ?? string.Empty,
                Name = document.SelectedPresetName ?? "Missing preset",
                Group = "Project snapshot",
                Description = "Preset values retained by the project because the catalogue entry may be unavailable.",
                Values = document.SelectedPresetSnapshot.Clone(),
                MediaKinds = [MediaKind.Image, MediaKind.Video, MediaKind.Audio],
                Source = "ProjectSnapshot"
            }
            : null;
        RefreshPresetCatalogue(_globalPresetId);
        RefreshAllJobWorkflowDisplays();
    }

    private PresetDocument? GetGlobalPresetForPersistence() =>
        FindCataloguePreset(_globalPresetId) ?? _globalPresetFallback;

    private PresetDocument? ResolvePresetForJob(MediaJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (job.SelectedPresetId.HasValue)
        {
            var cataloguePreset = FindCataloguePreset(job.SelectedPresetId);
            if (cataloguePreset is not null) return cataloguePreset;
            if (job.SelectedPresetSnapshot is null) return null;
            return new PresetDocument
            {
                Id = job.SelectedPresetId.Value,
                Version = job.SelectedPresetVersion ?? string.Empty,
                Name = job.SelectedPresetName,
                Group = "Queue snapshot",
                Description = "Preset values retained with the queue item.",
                Values = job.SelectedPresetSnapshot.Clone(),
                MediaKinds = [job.Kind],
                Source = "QueueSnapshot"
            };
        }
        return GetGlobalPresetForPersistence();
    }

    private PresetDocument? FindCataloguePreset(Guid? id) => id.HasValue
        ? _presetCatalogue.FirstOrDefault(preset => preset.Id == id.Value)
        : null;

    private void RefreshPresetCatalogue(Guid? preferredId = null)
    {
        if (PresetCombo is null) return;
        if (!_workflowFeaturesAvailable)
        {
            var fallbackChoices = new List<PresetChoice>
            {
                new(null, "Global settings only", null)
            };
            PresetCombo.ItemsSource = fallbackChoices;
            PresetCombo.DisplayMemberPath = nameof(PresetChoice.DisplayName);
            PresetCombo.SelectedIndex = 0;
            return;
        }
        var previousSuppress = _suppressProjectDirty;
        _suppressProjectDirty = true;
        try
        {
            _presetCatalogue = _presetService.LoadCatalogue();
            var choices = new List<PresetChoice>
            {
                new(null, "Global settings only", null)
            };
            choices.AddRange(_presetCatalogue.Select(preset => new PresetChoice(preset.Id, preset.DisplayName, preset)));

            var desiredId = preferredId ?? _globalPresetId;
            if (desiredId.HasValue && choices.All(choice => choice.Id != desiredId.Value) && _globalPresetFallback is not null)
            {
                choices.Add(new PresetChoice(
                    desiredId,
                    $"Missing catalogue preset — {_globalPresetFallback.Name} (project snapshot)",
                    _globalPresetFallback));
            }

            PresetCombo.ItemsSource = choices;
            PresetCombo.DisplayMemberPath = nameof(PresetChoice.DisplayName);
            PresetCombo.SelectedItem = choices.FirstOrDefault(choice => choice.Id == desiredId) ?? choices[0];
            if (PresetCombo.SelectedItem is PresetChoice choice)
            {
                _globalPresetId = choice.Id;
                if (choice.Preset is not null) _globalPresetFallback = choice.Preset;
            }
        }
        finally
        {
            _suppressProjectDirty = previousSuppress;
        }
        RefreshAllJobWorkflowDisplays();
    }

    private void PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PresetCombo.SelectedItem is not PresetChoice choice) return;
        _globalPresetId = choice.Id;
        _globalPresetFallback = choice.Preset;
        RefreshAllJobWorkflowDisplays();
        if (!_suppressProjectDirty && !_projectReadOnly && !_isRunning)
        {
            _session.MarkDirty(ProjectSessionChangeKind.ProjectOptionsChanged);
        }
    }

    private void ApplyPresetToSelected_Click(object sender, RoutedEventArgs e)
    {
        if (!CanMutateQueue(out var selected)) return;
        var choice = PresetCombo.SelectedItem as PresetChoice;
        var preset = choice?.Preset;
        if (preset is not null)
        {
            var incompatible = selected.Where(job => preset.MediaKinds.Count > 0 && !preset.MediaKinds.Contains(job.Kind)).ToList();
            if (incompatible.Count > 0)
            {
                MessageBox.Show(this,
                    $"Preset '{preset.Name}' does not apply to: {string.Join(", ", incompatible.Select(job => job.FileName))}",
                    "Preset applicability", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        foreach (var job in selected)
        {
            job.SelectedPresetId = preset?.Id;
            job.SelectedPresetVersion = preset?.Version;
            job.SelectedPresetName = preset?.Name ?? "Global settings";
            job.SelectedPresetSnapshot = preset?.Values.Clone();
            job.Reset();
        }
        RefreshAllJobWorkflowDisplays();
        AppendLog(preset is null
            ? $"Cleared explicit presets for {selected.Count} selected job(s); they now inherit the global selection."
            : $"Applied preset '{preset.Name}' to {selected.Count} selected job(s).");
    }

    private void ApplyCurrentOverrides_Click(object sender, RoutedEventArgs e)
    {
        if (!CanMutateQueue(out var selected)) return;
        var overrides = ConversionOptionOverrides.FromSettings(CaptureCurrentSettings());
        foreach (var job in selected)
        {
            var preset = ResolvePresetForJob(job);
            var conflicts = preset?.LockedFields.Intersect(overrides.GetDefinedKeys(), StringComparer.Ordinal).ToList() ?? [];
            if (conflicts.Count > 0)
            {
                MessageBox.Show(this,
                    $"Job '{job.FileName}' uses preset '{preset!.Name}', which locks: {string.Join(", ", conflicts)}. Duplicate or edit the preset before applying these job settings.",
                    "Preset fields are locked", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        foreach (var job in selected)
        {
            job.PerJobOverrides = overrides.Clone();
            job.Reset();
        }
        RefreshAllJobWorkflowDisplays();
        AppendLog($"Captured current typed settings as independent overrides for {selected.Count} job(s).");
    }

    private void ClearJobOptions_Click(object sender, RoutedEventArgs e)
    {
        if (!CanMutateQueue(out var selected)) return;
        foreach (var job in selected)
        {
            job.SelectedPresetId = null;
            job.SelectedPresetVersion = null;
            job.SelectedPresetName = "Global settings";
            job.SelectedPresetSnapshot = null;
            job.PerJobOverrides = null;
            job.Reset();
        }
        RefreshAllJobWorkflowDisplays();
        AppendLog($"Cleared explicit preset and per-job overrides for {selected.Count} job(s).");
    }

    private void PresetManager_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;
        var selectedPreset = (PresetCombo.SelectedItem as PresetChoice)?.Preset;
        var cataloguePreset = selectedPreset is null ? null : FindCataloguePreset(selectedPreset.Id);
        var menu = new ContextMenu { PlacementTarget = PresetManagerButton };
        menu.Items.Add(CreateMenuItem("Save current settings as new preset", (_, _) => SaveCurrentAsPreset()));
        menu.Items.Add(CreateMenuItem("Edit selected user preset", (_, _) => EditSelectedPreset(), cataloguePreset is { IsBuiltIn: false }));
        menu.Items.Add(CreateMenuItem("Duplicate selected preset", (_, _) => DuplicateSelectedPreset(), selectedPreset is not null));
        menu.Items.Add(CreateMenuItem("Delete selected user preset", (_, _) => DeleteSelectedPreset(), cataloguePreset is { IsBuiltIn: false }));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("Import preset…", (_, _) => ImportPreset()));
        menu.Items.Add(CreateMenuItem("Export selected preset…", (_, _) => ExportSelectedPreset(), selectedPreset is not null));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("Set selected as application default", (_, _) => SetSelectedPresetDefault()));
        menu.Items.Add(CreateMenuItem("Clear application default", (_, _) => ClearPresetDefault()));
        menu.IsOpen = true;
    }

    private static MenuItem CreateMenuItem(string header, RoutedEventHandler handler, bool enabled = true)
    {
        var item = new MenuItem { Header = header, IsEnabled = enabled };
        item.Click += handler;
        return item;
    }

    private void SaveCurrentAsPreset()
    {
        var dialog = new PresetEditorDialog { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var created = _presetService.CreateFromSettings(
                CaptureCurrentSettings(), dialog.PresetName, dialog.PresetGroup, dialog.PresetDescription, dialog.LockedFields);
            _globalPresetId = created.Id;
            _globalPresetFallback = created;
            RefreshPresetCatalogue(created.Id);
            AppendLog($"Created user preset '{created.Name}'.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not create preset", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditSelectedPreset()
    {
        var preset = (PresetCombo.SelectedItem as PresetChoice)?.Preset;
        var cataloguePreset = preset is null ? null : FindCataloguePreset(preset.Id);
        if (cataloguePreset is null || cataloguePreset.IsBuiltIn) return;
        var dialog = new PresetEditorDialog(cataloguePreset) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var updated = _presetService.UpdateUserPreset(
                cataloguePreset, dialog.PresetName, dialog.PresetGroup, dialog.PresetDescription, dialog.LockedFields);
            RefreshPresetCatalogue(updated.Id);
            AppendLog($"Updated user preset '{updated.Name}'.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not update preset", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DuplicateSelectedPreset()
    {
        var preset = (PresetCombo.SelectedItem as PresetChoice)?.Preset;
        if (preset is null) return;
        var dialog = new PresetEditorDialog(preset, preset.Name + " copy") { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var duplicate = _presetService.Duplicate(preset, dialog.PresetName, dialog.PresetGroup);
            duplicate = _presetService.UpdateUserPreset(
                duplicate, dialog.PresetName, dialog.PresetGroup, dialog.PresetDescription, dialog.LockedFields);
            RefreshPresetCatalogue(duplicate.Id);
            AppendLog($"Duplicated preset as '{duplicate.Name}'.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not duplicate preset", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteSelectedPreset()
    {
        var preset = (PresetCombo.SelectedItem as PresetChoice)?.Preset;
        var cataloguePreset = preset is null ? null : FindCataloguePreset(preset.Id);
        if (cataloguePreset is null || cataloguePreset.IsBuiltIn) return;
        if (MessageBox.Show(this,
            $"Delete user preset '{cataloguePreset.Name}'? Existing project and queue items retain typed snapshots.",
            "Delete preset", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            _presetService.Delete(cataloguePreset);
            RefreshPresetCatalogue(_globalPresetId);
            AppendLog($"Deleted user preset '{cataloguePreset.Name}'.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not delete preset", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportPreset()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import MediaForge preset",
            Filter = "MediaForge presets (*.mediaforge-preset)|*.mediaforge-preset|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var imported = _presetService.Import(dialog.FileName);
            RefreshPresetCatalogue(imported.Id);
            AppendLog($"Imported preset '{imported.Name}' into the user catalogue.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Preset import rejected", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportSelectedPreset()
    {
        var preset = (PresetCombo.SelectedItem as PresetChoice)?.Preset;
        if (preset is null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Export MediaForge preset",
            Filter = "MediaForge presets (*.mediaforge-preset)|*.mediaforge-preset",
            AddExtension = true,
            DefaultExt = ".mediaforge-preset",
            FileName = preset.Name + ".mediaforge-preset",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            _presetService.Export(preset, dialog.FileName);
            AppendLog($"Exported preset '{preset.Name}' to {dialog.FileName}.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not export preset", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetSelectedPresetDefault()
    {
        var id = (PresetCombo.SelectedItem as PresetChoice)?.Id;
        try
        {
            _presetService.SetDefault(id);
            AppendLog(id.HasValue ? "Updated the application default preset." : "Cleared the application default preset.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not update default preset", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearPresetDefault()
    {
        try
        {
            _presetService.SetDefault(null);
            AppendLog("Cleared the application default preset.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not clear default preset", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MoveSelectedUp_Click(object sender, RoutedEventArgs e) => MoveSelected(-1);
    private void MoveSelectedDown_Click(object sender, RoutedEventArgs e) => MoveSelected(1);

    private void MoveSelected(int direction)
    {
        if (!CanMutateQueue(out var selected)) return;
        var ids = selected.Select(job => job.Id).ToHashSet();
        if (_session.MoveJobs(selected, direction)) RestoreSelection(ids);
    }

    private void PriorityUp_Click(object sender, RoutedEventArgs e)
    {
        if (!CanMutateQueue(out var selected)) return;
        _session.AdjustPriority(selected, 1);
        RefreshAllJobWorkflowDisplays();
    }

    private void PriorityDown_Click(object sender, RoutedEventArgs e)
    {
        if (!CanMutateQueue(out var selected)) return;
        _session.AdjustPriority(selected, -1);
        RefreshAllJobWorkflowDisplays();
    }

    private void ToggleEnabled_Click(object sender, RoutedEventArgs e)
    {
        if (!CanMutateQueue(out var selected)) return;
        var enabled = selected.Any(job => !job.Enabled);
        _session.SetEnabled(selected, enabled);
        RefreshAllJobWorkflowDisplays();
    }

    private void DuplicateSelected_Click(object sender, RoutedEventArgs e)
    {
        if (!CanMutateQueue(out var selected)) return;
        var count = _session.DuplicateJobs(selected);
        RefreshAllJobWorkflowDisplays();
        AppendLog($"Duplicated {count} queue item(s) with independent IDs and overrides.");
    }

    private void RetrySelected_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || _projectReadOnly) return;
        var selected = SelectedJobs();
        if (selected.Count == 0)
        {
            selected = _session.Jobs.Where(job => job.State is JobState.Failed or JobState.Cancelled or JobState.Skipped or JobState.VerificationFailed).ToList();
        }
        foreach (var job in selected.Where(job => job.State is JobState.Failed or JobState.Cancelled or JobState.Skipped or JobState.VerificationFailed))
            job.Reset();
        UpdateSummary();
    }

    private void SaveQueue_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;
        var dialog = new SaveFileDialog
        {
            Title = "Save MediaForge queue",
            Filter = "MediaForge queues (*.mediaforge-queue)|*.mediaforge-queue",
            AddExtension = true,
            DefaultExt = ".mediaforge-queue",
            FileName = "MediaForge queue.mediaforge-queue",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var document = _queueService.Capture(_session.Jobs, dialog.FileName, _loadedQueueBasis);
            _queueService.Save(document, dialog.FileName);
            _loadedQueueBasis = document;
            AppendLog($"Queue saved: {dialog.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not save queue", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadQueue_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || _projectReadOnly) return;
        var dialog = new OpenFileDialog
        {
            Title = "Load MediaForge queue",
            Filter = "MediaForge queues (*.mediaforge-queue)|*.mediaforge-queue|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true) return;
        if (_session.Jobs.Count > 0 && MessageBox.Show(this,
            "Replace the current project queue with the selected queue file? Project defaults remain unchanged.",
            "Replace queue", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            WriteRecoveryBeforeRiskyTransition();
            var result = _queueService.Load(dialog.FileName);
            if (result.IsReadOnly)
            {
                MessageBox.Show(this, result.ReadOnlyReason, "Queue schema is newer", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _loadedQueueBasis = result.Document;
            _session.ReplaceJobs(result.Jobs, markDirty: true);
            RefreshAllJobWorkflowDisplays();
            ShowSourceIssueSummary(result.SourceIssues);
            AppendLog($"Queue loaded: {dialog.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not load queue", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PauseResume_Click(object sender, RoutedEventArgs e)
    {
        if (!_queueCoordinator.IsRunning) return;
        if (_queueCoordinator.IsDispatchPaused)
        {
            _queueCoordinator.Resume();
            PauseButton.Content = "Pause after current";
            _viewModel.SetStatusOverride("Queue dispatch resumed.");
            AppendLog("Queue dispatch resumed.");
        }
        else
        {
            _queueCoordinator.PauseAfterCurrent();
            PauseButton.Content = "Resume";
            _viewModel.SetStatusOverride("Pause requested — running jobs will finish; no new jobs will start.");
            AppendLog("Pause after current requested.");
        }
    }

    private async void OpenSource_Click(object sender, RoutedEventArgs e)
    {
        var job = SelectedJobs().FirstOrDefault();
        if (job is null) return;
        await OpenInExplorerAsync(job.SourcePath, selectFile: true);
    }

    private async void OpenOutput_Click(object sender, RoutedEventArgs e)
    {
        var job = SelectedJobs().FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.OutputPath));
        if (job is null)
        {
            MessageBox.Show(this, "The selected jobs do not have a recorded output path.", "No output", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        await OpenInExplorerAsync(job.OutputPath, selectFile: true);
    }

    private async Task OpenInExplorerAsync(string path, bool selectFile)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var arguments = selectFile && File.Exists(fullPath)
                ? new[] { "/select,", fullPath }
                : new[] { Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath) ?? fullPath };
            await _processRunner.RunAsync(new Services.Runtime.ProcessRunRequest
            {
                FileName = "explorer.exe",
                Arguments = arguments,
                StandardOutputTailLineLimit = 5,
                StandardErrorTailLineLimit = 5
            }, _windowCancellation.Token);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open Explorer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyDiagnostic_Click(object sender, RoutedEventArgs e)
    {
        var selected = SelectedJobs();
        if (selected.Count == 0) return;
        var builder = new StringBuilder();
        foreach (var job in selected)
        {
            builder.AppendLine($"ID: {job.Id}");
            builder.AppendLine($"Source: {RedactPathForDiagnostics(job.SourcePath)}");
            builder.AppendLine($"Kind: {job.Kind}; Enabled: {job.Enabled}; Priority: {job.Priority}; State: {job.State}");
            builder.AppendLine($"Preset: {job.SelectedPresetName}; Overrides: {job.OverrideSummary}");
            builder.AppendLine($"Effective: {job.EffectiveOptionsSummary}");
            builder.AppendLine($"Estimate: {job.EstimateDisplay}");
            builder.AppendLine($"Output: {RedactPathForDiagnostics(job.OutputPath)}");
            builder.AppendLine($"Message: {job.Message}");
            builder.AppendLine();
        }
        try
        {
            Clipboard.SetText(builder.ToString());
            AppendLog($"Copied diagnostic summary for {selected.Count} queue item(s).");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not copy diagnostic summary", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string RedactPathForDiagnostics(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "(none)";
        var fileName = Path.GetFileName(path);
        return string.IsNullOrWhiteSpace(fileName) ? "[local path redacted]" : "[local path redacted]\\" + fileName;
    }

    private IReadOnlyList<MediaJob> SelectedJobs() => JobsGrid.SelectedItems.Cast<MediaJob>().ToList();

    private bool CanMutateQueue(out IReadOnlyList<MediaJob> selected)
    {
        selected = SelectedJobs();
        return !_isRunning && !_projectReadOnly && selected.Count > 0;
    }

    private void RestoreSelection(IReadOnlySet<Guid> ids)
    {
        JobsGrid.SelectedItems.Clear();
        foreach (var job in _session.Jobs.Where(job => ids.Contains(job.Id))) JobsGrid.SelectedItems.Add(job);
    }

    private bool TryCreateRunItems(
        IReadOnlyList<MediaJob> candidates,
        out List<QueueRunItem> runItems,
        out int parallelJobs,
        out string error)
    {
        runItems = [];
        parallelJobs = 1;
        error = string.Empty;
        var globalInput = CaptureOptionInput();
        foreach (var job in candidates)
        {
            var preset = ResolvePresetForJob(job);
            if (job.SelectedPresetId.HasValue && preset is null)
            {
                error = $"Job '{job.FileName}' references preset {job.SelectedPresetId}, but neither the catalogue entry nor its typed snapshot is available.";
                return false;
            }
            if (!job.SelectedPresetId.HasValue && _globalPresetId.HasValue && preset is null)
            {
                error = $"The global preset {_globalPresetId} is unavailable and has no retained project snapshot.";
                return false;
            }

            var resolution = _effectiveOptionsResolver.Resolve(new EffectiveOptionsRequest(
                globalInput,
                [job],
                preset,
                job.PerJobOverrides));
            if (!resolution.Success)
            {
                error = $"{job.FileName}: {resolution.Error}";
                return false;
            }

            parallelJobs = resolution.ParallelJobs;
            var options = resolution.Options!;
            runItems.Add(new QueueRunItem(job, options));
            var presetName = preset?.Name ?? "Global settings";
            var sourceCounts = resolution.Sources.Values
                .GroupBy(source => source)
                .ToDictionary(group => group.Key, group => group.Count());
            job.EffectiveOptionsSummary = $"{presetName}; " + string.Join(", ", sourceCounts.OrderBy(group => group.Key).Select(group => $"{group.Key}: {group.Value}"));
            job.EstimateDisplay = _queueEstimateService.Estimate(job, options).Display;
        }
        JobsGrid.Items.Refresh();
        return true;
    }

    private void RefreshAllJobWorkflowDisplays()
    {
        foreach (var job in _session.Jobs)
        {
            var preset = ResolvePresetForJob(job);
            if (job.SelectedPresetId.HasValue && preset is not null)
            {
                job.SelectedPresetName = preset.Name;
                job.SelectedPresetVersion = preset.Version;
                job.SelectedPresetSnapshot = preset.Values.Clone();
            }
            var source = job.SelectedPresetId.HasValue
                ? job.SelectedPresetName
                : preset is null ? "Global settings" : $"Global preset: {preset.Name}";
            job.EffectiveOptionsSummary = job.PerJobOverrides is null || job.PerJobOverrides.IsEmpty
                ? source
                : $"{source} + {job.PerJobOverrides.GetDefinedKeys().Count} job override(s)";
        }
        JobsGrid?.Items.Refresh();
        UpdateWorkflowControlState();
    }

    private void UpdateWorkflowControlState()
    {
        if (PresetCombo is null) return;
        var canEdit = _workflowFeaturesAvailable && !_isRunning && !_projectReadOnly;
        var selectionCount = JobsGrid?.SelectedItems.Count ?? 0;
        PresetCombo.IsEnabled = canEdit;
        PresetManagerButton.IsEnabled = _workflowFeaturesAvailable && !_isRunning;
        ApplyPresetButton.IsEnabled = canEdit && selectionCount > 0;
        ApplyOverrideButton.IsEnabled = canEdit && selectionCount > 0;
        ClearJobOptionsButton.IsEnabled = canEdit && selectionCount > 0;
        MoveUpButton.IsEnabled = canEdit && selectionCount > 0;
        MoveDownButton.IsEnabled = canEdit && selectionCount > 0;
        PriorityUpButton.IsEnabled = canEdit && selectionCount > 0;
        PriorityDownButton.IsEnabled = canEdit && selectionCount > 0;
        ToggleEnabledButton.IsEnabled = canEdit && selectionCount > 0;
        DuplicateSelectedButton.IsEnabled = canEdit && selectionCount > 0;
        OpenSourceButton.IsEnabled = selectionCount > 0;
        OpenOutputButton.IsEnabled = selectionCount > 0;
        CopyDiagnosticButton.IsEnabled = selectionCount > 0;
        SaveQueueButton.IsEnabled = _workflowFeaturesAvailable && !_isRunning && _session.Jobs.Count > 0;
        LoadQueueButton.IsEnabled = canEdit;
        PauseButton.IsEnabled = _isRunning;
        PauseButton.Content = _queueCoordinator.IsDispatchPaused ? "Resume" : "Pause after current";
        PresetCombo.ToolTip = _workflowFeaturesAvailable ? null : $"Preset workflow unavailable: {_workflowInitializationError}";
    }
}
