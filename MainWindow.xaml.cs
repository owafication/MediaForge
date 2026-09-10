using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using MediaForge.Models;
using MediaForge.Services;
using MediaForge.Services.Runtime;
using MediaForge.Services.Options;
using MediaForge.Services.Queue;
using MediaForge.Services.Session;
using MediaForge.ViewModels;
using Microsoft.Win32;

namespace MediaForge;

public partial class MainWindow : Window
{
    private readonly IProjectSession _session = new ProjectSession();
    private readonly SettingsService _settingsService = new();
    private readonly IProcessRunner _processRunner = new ProcessRunner();
    private readonly IEffectiveOptionsResolver _effectiveOptionsResolver = new EffectiveOptionsResolver();
    private readonly IQueueCoordinator _queueCoordinator;
    private readonly MainViewModel _viewModel;
    private readonly CancellationTokenSource _windowCancellation = new();
    private bool _isRunning;
    private bool _closeWhenBatchStops;
    private bool _allowClose;
    private bool _isClosing;

    public MainWindow()
    {
        _queueCoordinator = new QueueCoordinator(new MediaConversionService(_processRunner));
        _viewModel = new MainViewModel(_session.Jobs);

        InitializeComponent();
        InitializeProjectFeatures();
        DataContext = _viewModel;
        AllowDrop = true;
        DragOver += Window_DragOver;
        Drop += Window_Drop;
        Closing += Window_Closing;
        Closed += MainWindow_Closed;
        _session.Changed += Session_Changed;
        _queueCoordinator.StateChanged += QueueCoordinator_StateChanged;

        ApplySettings(_settingsService.Load());
        InitializeWorkflowFeaturesSafe();
        DetectFfmpeg(silent: true);
        UpdateOutputControls();
        UpdateSummary();
        CompleteProjectInitialization();
    }

    private void Session_Changed(object? sender, ProjectSessionChangedEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => Session_Changed(sender, e));
            return;
        }
        UpdateSummary();
        UpdatePreviewEditButton();
        UpdateWorkflowControlState();
        HandleProjectSessionChanged(e);
    }

    private void QueueCoordinator_StateChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateSummary();
                UpdateWorkflowControlState();
            }));
            return;
        }
        UpdateSummary();
        UpdateWorkflowControlState();
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || _projectReadOnly) return;
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Add media files",
            Multiselect = true,
            CheckFileExists = true,
            Filter = $"Supported media|{MediaClassifier.SupportedDialogPattern}|All files|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            AddPaths(dialog.FileNames, rootFolder: null);
        }
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || _projectReadOnly) return;
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choose a folder. Supported media files in subfolders will also be added.",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            AddFolder(dialog.FolderName);
        }
    }

    private void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || _projectReadOnly) return;
        _session.RemoveJobs(JobsGrid.SelectedItems.Cast<MediaJob>().ToList());
        UpdateSummary();
        UpdatePreviewEditButton();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || _projectReadOnly) return;
        _session.Clear();
        UpdateSummary();
        UpdatePreviewEditButton();
    }

    private void JobsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePreviewEditButton();
        UpdateWorkflowControlState();
    }

    private void JobsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PreviewEditButton.IsEnabled) OpenSelectedEditor();
    }

    private void PreviewEdit_Click(object sender, RoutedEventArgs e) => OpenSelectedEditor();

    private void ClearEdits_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || _projectReadOnly || JobsGrid.SelectedItems.Count != 1 || JobsGrid.SelectedItem is not MediaJob job || job.EditPlan is null) return;
        job.EditPlan = null;
        job.Reset();
        JobsGrid.Items.Refresh();
        AppendLog($"[{job.FileName}] Cleared edit plan.");
        UpdatePreviewEditButton();
    }

    private void OpenSelectedEditor()
    {
        if (_isRunning || _projectReadOnly || JobsGrid.SelectedItems.Count != 1 || JobsGrid.SelectedItem is not MediaJob job ||
            job.Kind is not (MediaKind.Image or MediaKind.Video)) return;

        var ffprobePath = FfmpegLocator.FindFfprobe(FfmpegPathText.Text.Trim());
        if (job.Kind == MediaKind.Video && ffprobePath is null)
        {
            System.Windows.MessageBox.Show(this, "Choose a valid FFmpeg folder containing ffprobe.exe before editing video.",
                "Video editor unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var isImage = job.Kind == MediaKind.Image;
        var imageDimensions = isImage ? ResolveEditorImageDimensions(job) : (Width: 0, Height: 0);
        var videoDimensions = !isImage && SelectedTag(VideoResolutionCombo) != "Keep"
            ? ResolveVideoDimensions()
            : (Width: 0, Height: 0);
        var defaultAspect = isImage
            ? SelectedTag(ImageResizeModeCombo) is "None" or "Percent" ? "Source" : SelectedTag(ImageAspectRatioCombo)
            : SelectedTag(VideoResolutionCombo) == "Keep" ? "Source" : SelectedTag(VideoAspectRatioCombo);

        var editor = new MediaEditorWindow(
            job,
            ffprobePath ?? string.Empty,
            isImage ? NormaliseEditorResizeMode(SelectedTag(ImageResizeModeCombo)) : SelectedTag(VideoResizeModeCombo),
            isImage ? imageDimensions.Width : videoDimensions.Width,
            isImage ? imageDimensions.Height : videoDimensions.Height,
            defaultAspect,
            isImage ? ParseDouble(ImageCustomAspectWidthText.Text, 16) : ParseDouble(VideoCustomAspectWidthText.Text, 16),
            isImage ? ParseDouble(ImageCustomAspectHeightText.Text, 9) : ParseDouble(VideoCustomAspectHeightText.Text, 9))
        {
            Owner = this
        };

        if (editor.ShowDialog() == true && editor.ResultPlan is not null)
        {
            job.EditPlan = editor.ResultPlan;
            job.Reset();
            AppendLog($"[{job.FileName}] Applied edit plan: {job.EditSummary}.");
            JobsGrid.Items.Refresh();
            UpdateSummary();
        }
    }

    private void UpdatePreviewEditButton()
    {
        if (PreviewEditButton is null || JobsGrid is null) return;
        PreviewEditButton.IsEnabled = !_isRunning && !_projectReadOnly && JobsGrid.SelectedItems.Count == 1 &&
            JobsGrid.SelectedItem is MediaJob { Kind: MediaKind.Image or MediaKind.Video };
        ClearEditsButton.IsEnabled = !_isRunning && !_projectReadOnly && JobsGrid.SelectedItems.Count == 1 &&
            JobsGrid.SelectedItem is MediaJob { EditPlan: not null };
    }

    private void ResetFailed_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || _projectReadOnly) return;
        foreach (var job in _session.Jobs.Where(job => job.State is JobState.Failed or JobState.Cancelled or JobState.Skipped))
        {
            job.Reset();
        }
        UpdateSummary();
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || _projectReadOnly) return;
        var candidates = _session.Jobs
            .Where(job => job.Enabled && job.State is (JobState.Pending or JobState.Ready or JobState.Failed or JobState.Cancelled or JobState.Skipped or JobState.VerificationFailed))
            .ToList();
        if (candidates.Count == 0)
        {
            MessageBox.Show(this, "There are no enabled pending files to process.", "MediaForge", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TryCreateRunItems(candidates, out var runItems, out var parallelJobs, out var resolutionError))
        {
            MessageBox.Show(this, resolutionError, "Cannot start batch", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var ffmpegPath = runItems[0].Options.FfmpegPath;
        var ffmpegTest = await FfmpegLocator.TestAsync(ffmpegPath, _windowCancellation.Token);
        if (_isClosing) return;
        if (!ffmpegTest.Success)
        {
            MessageBox.Show(this, ffmpegTest.Message, "FFmpeg unavailable", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            SaveSettings();
        }
        catch (Exception ex)
        {
            AppendLog($"Settings could not be saved: {ex.Message}");
        }

        WriteRecoveryBeforeRiskyTransition();
        SetRunningState(true);
        AppendLog($"Batch started: {runItems.Count} enabled file(s), {parallelJobs} parallel job(s), immutable per-job option snapshots.");
        var wasCancelled = false;

        try
        {
            var runResult = await _queueCoordinator.RunAsync(
                runItems,
                parallelJobs,
                AppendLog,
                _windowCancellation.Token);
            wasCancelled = runResult.WasCancelled;
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
        }
        catch (Exception ex)
        {
            AppendLog($"Batch coordinator failed: {ex}");
            MessageBox.Show(this, FirstUsefulLine(ex.Message), "Batch failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetRunningState(false);
            UpdateSummary();
            AppendLog(wasCancelled ? "Batch cancelled." : "Batch finished.");
            if (_closeWhenBatchStops)
            {
                _allowClose = true;
                Close();
            }
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _queueCoordinator.Cancel();
        CancelButton.IsEnabled = false;
        _viewModel.SetStatusOverride("Cancelling active FFmpeg processes…");
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choose an output folder",
            Multiselect = false,
            InitialDirectory = Directory.Exists(OutputFolderText.Text) ? OutputFolderText.Text : string.Empty
        };
        if (dialog.ShowDialog(this) == true)
        {
            OutputFolderText.Text = dialog.FolderName;
        }
    }

    private void BrowseFfmpeg_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Choose ffmpeg.exe",
            Filter = "FFmpeg executable|ffmpeg.exe|Executable files|*.exe",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == true)
        {
            FfmpegPathText.Text = dialog.FileName;
            _ = TestFfmpegAndDisplayAsync();
        }
    }

    private void DetectFfmpeg_Click(object sender, RoutedEventArgs e) => DetectFfmpeg(silent: false);

    private void DetectFfmpeg(bool silent)
    {
        if (FfmpegPathText is null || FfmpegStatusText is null) return;

        var found = FfmpegLocator.FindFfmpeg(FfmpegPathText.Text);
        if (found is not null)
        {
            FfmpegPathText.Text = found;
            FfmpegStatusText.Text = $"Detected: {found}";
            if (!silent) _ = TestFfmpegAndDisplayAsync();
        }
        else if (!silent)
        {
            FfmpegStatusText.Text = "FFmpeg was not found in the configured path, app folder, local tools folder, or PATH.";
        }
    }

    private async void TestFfmpeg_Click(object sender, RoutedEventArgs e) => await TestFfmpegAndDisplayAsync();

    private async Task TestFfmpegAndDisplayAsync()
    {
        var path = FfmpegPathText.Text.Trim();
        var result = await FfmpegLocator.TestAsync(path, _windowCancellation.Token);
        if (_isClosing) return;
        FfmpegStatusText.Text = result.Success ? $"Ready — {result.Message}" : $"Not ready — {result.Message}";
    }

    private async void DownloadFfmpeg_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(this,
                "This runs the included PowerShell helper to download a third-party Windows FFmpeg build into your local app-data folder. Continue?",
                "Download FFmpeg tools",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        var scriptPath = Path.Combine(AppContext.BaseDirectory, "scripts", "download-ffmpeg.ps1");
        if (!File.Exists(scriptPath))
        {
            var sourceScript = Path.Combine(AppContext.BaseDirectory, "download-ffmpeg.ps1");
            if (File.Exists(sourceScript)) scriptPath = sourceScript;
        }

        if (!File.Exists(scriptPath))
        {
            System.Windows.MessageBox.Show(this, "The download helper script was not found beside the application.", "MediaForge", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            FfmpegStatusText.Text = "Downloading and validating FFmpeg tools…";
            var result = await _processRunner.RunAsync(
                new ProcessRunRequest
                {
                    FileName = "powershell.exe",
                    Arguments = ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", scriptPath]
                },
                _windowCancellation.Token);
            AppendLog(result.StandardOutput);
            if (!string.IsNullOrWhiteSpace(result.StandardError)) AppendLog(result.StandardError);

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(FirstUsefulLine(result.StandardError));
            }

            DetectFfmpeg(silent: true);
            await TestFfmpegAndDisplayAsync();
        }
        catch (OperationCanceledException) when (_isClosing)
        {
            // Window shutdown requested; ProcessRunner handles process-tree termination.
        }
        catch (Exception ex)
        {
            FfmpegStatusText.Text = $"Download failed — {ex.Message}";
        }
    }

    private void ImageAspectRatio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ImageCustomAspectPanel is null) return;
        ImageCustomAspectPanel.Visibility = SelectedTag(ImageAspectRatioCombo) == "Custom" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void VideoAspectRatio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VideoCustomAspectPanel is null) return;
        VideoCustomAspectPanel.Visibility = SelectedTag(VideoAspectRatioCombo) == "Custom" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyImageAspect_Click(object sender, RoutedEventArgs e)
    {
        if (!TryApplyAspectRatio(ImageAspectRatioCombo, ImageCustomAspectWidthText, ImageCustomAspectHeightText,
                ImageWidthText, ImageHeightText, MediaKind.Image))
        {
            System.Windows.MessageBox.Show(this, "Choose a fixed or valid custom aspect ratio. Source ratio requires one selected image.",
                "Aspect ratio", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ApplyVideoAspect_Click(object sender, RoutedEventArgs e)
    {
        if (!TryApplyAspectRatio(VideoAspectRatioCombo, VideoCustomAspectWidthText, VideoCustomAspectHeightText,
                VideoWidthText, VideoHeightText, MediaKind.Video))
        {
            System.Windows.MessageBox.Show(this, "Choose a fixed or valid custom aspect ratio. Source ratio requires a selected video with an edit plan.",
                "Aspect ratio", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        SelectComboByTag(VideoResolutionCombo, "Custom");
    }

    private bool TryApplyAspectRatio(ComboBox ratioCombo, TextBox customWidthText, TextBox customHeightText,
        TextBox widthText, TextBox heightText, MediaKind sourceKind)
    {
        if (!int.TryParse(widthText.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var width) || width < 1) return false;
        var tag = SelectedTag(ratioCombo);
        double ratio;
        if (tag == "Free") return true;
        if (tag == "Custom")
        {
            var customWidth = ParseDouble(customWidthText.Text, 0);
            var customHeight = ParseDouble(customHeightText.Text, 0);
            if (customWidth <= 0 || customHeight <= 0) return false;
            ratio = customWidth / customHeight;
            if (!double.IsFinite(ratio)) return false;
        }
        else if (tag == "Source")
        {
            ratio = ResolveSelectedSourceAspect(sourceKind);
            if (ratio <= 0) return false;
        }
        else
        {
            ratio = ParseAspectRatio(tag);
            if (ratio <= 0) return false;
        }

        var targetHeight = width / ratio;
        var maximum = sourceKind == MediaKind.Image ? 100000 : 16384;
        if (!double.IsFinite(targetHeight) || targetHeight > maximum) return false;
        heightText.Text = Math.Max(1, (int)Math.Round(targetHeight)).ToString(CultureInfo.InvariantCulture);
        return true;
    }

    private double ResolveSelectedSourceAspect(MediaKind kind)
    {
        if (JobsGrid.SelectedItems.Count != 1 || JobsGrid.SelectedItem is not MediaJob job || job.Kind != kind) return 0;
        if (kind == MediaKind.Image)
        {
            try
            {
                var frame = BitmapFrame.Create(new Uri(job.SourcePath, UriKind.Absolute), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                return frame.PixelHeight > 0 ? (double)frame.PixelWidth / frame.PixelHeight : 0;
            }
            catch
            {
                return 0;
            }
        }

        var clip = job.EditPlan?.Clips.FirstOrDefault();
        return clip is { Height: > 0 } ? (double)clip.Width / clip.Height : 0;
    }

    private void OutputModeChanged(object sender, RoutedEventArgs e) => UpdateOutputControls();

    private void UpdateOutputControls()
    {
        if (OutputFolderText is null || OutputBesideSourceCheck is null) return;
        OutputFolderText.IsEnabled = OutputBesideSourceCheck.IsChecked != true;
        PreserveTreeCheck.IsEnabled = OutputBesideSourceCheck.IsChecked != true;
    }

    private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) && !_isRunning
            ? System.Windows.DragDropEffects.Copy
            : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (_isRunning || !e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;
        var paths = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
        foreach (var path in paths)
        {
            if (Directory.Exists(path)) AddFolder(path);
            else if (File.Exists(path)) AddPaths([path], rootFolder: null);
        }
    }

    private void AddFolder(string folder)
    {
        try
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                ReturnSpecialDirectories = false,
                AttributesToSkip = FileAttributes.System | FileAttributes.ReparsePoint
            };
            AddPaths(Directory.EnumerateFiles(folder, "*", options), folder);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "Could not read folder", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddPaths(IEnumerable<string> paths, string? rootFolder)
    {
        if (_isRunning || _projectReadOnly) return;
        var existing = new HashSet<string>(
            _session.Jobs.Select(job => Path.GetFullPath(job.SourcePath)),
            StringComparer.OrdinalIgnoreCase);
        var pending = new List<MediaJob>();
        var unsupported = 0;

        foreach (var path in paths)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath) || !existing.Add(fullPath)) continue;
                var kind = MediaClassifier.Classify(fullPath);
                if (kind == MediaKind.Unsupported)
                {
                    unsupported++;
                    continue;
                }

                pending.Add(new MediaJob
                {
                    SourcePath = fullPath,
                    RootFolder = rootFolder,
                    Kind = kind,
                    SourceBytes = new FileInfo(fullPath).Length,
                    SourceLastWriteUtc = new DateTimeOffset(File.GetLastWriteTimeUtc(fullPath), TimeSpan.Zero)
                });
            }
            catch (Exception ex)
            {
                AppendLog($"Could not add {path}: {ex.Message}");
            }
        }

        var added = _session.AddJobs(pending);
        AppendLog($"Added {added} file(s)." + (unsupported > 0 ? $" Ignored {unsupported} unsupported file(s)." : string.Empty));
        UpdateSummary();
        UpdatePreviewEditButton();
    }

    private ConversionOptionInput CaptureOptionInput() => new()
    {
        FfmpegPath = FfmpegPathText.Text.Trim(),
        OutputBesideSource = OutputBesideSourceCheck.IsChecked == true,
        OutputFolder = OutputFolderText.Text.Trim(),
        PreserveFolderTree = PreserveTreeCheck.IsChecked == true,
        PreserveTimestamps = PreserveTimestampsCheck.IsChecked == true,
        StripMetadata = StripMetadataCheck.IsChecked == true,
        FileSuffix = SuffixText.Text,
        CollisionPolicy = SelectedTag(CollisionCombo),
        ParallelJobs = SelectedTag(ParallelJobsCombo),

        ImageFormat = SelectedTag(ImageFormatCombo),
        ImageQuality = (int)Math.Round(ImageQualitySlider.Value),
        ImageResizeMode = SelectedTag(ImageResizeModeCombo),
        ImageWidth = ImageWidthText.Text,
        ImageHeight = ImageHeightText.Text,
        ImageScalePercent = ImagePercentText.Text,
        ImageAspectRatio = SelectedTag(ImageAspectRatioCombo),
        ImageCustomAspectWidth = ImageCustomAspectWidthText.Text,
        ImageCustomAspectHeight = ImageCustomAspectHeightText.Text,

        VideoContainer = SelectedTag(VideoContainerCombo),
        VideoCodec = SelectedTag(VideoCodecCombo),
        VideoCrf = (int)Math.Round(VideoCrfSlider.Value),
        VideoPreset = SelectedTag(VideoPresetCombo),
        VideoResolution = SelectedTag(VideoResolutionCombo),
        VideoWidth = VideoWidthText.Text,
        VideoHeight = VideoHeightText.Text,
        VideoResizeMode = SelectedTag(VideoResizeModeCombo),
        VideoAspectRatio = SelectedTag(VideoAspectRatioCombo),
        VideoCustomAspectWidth = VideoCustomAspectWidthText.Text,
        VideoCustomAspectHeight = VideoCustomAspectHeightText.Text,
        VideoFps = SelectedTag(VideoFpsCombo),
        VideoCustomFps = VideoCustomFpsText.Text,
        VideoAudioCodec = SelectedTag(VideoAudioCodecCombo),
        VideoAudioBitrate = VideoAudioBitrateText.Text,
        ExtractAudioOnly = ExtractAudioCheck.IsChecked == true,

        AudioFormat = SelectedTag(AudioFormatCombo),
        AudioBitrate = AudioBitrateText.Text,
        AudioSampleRate = SelectedTag(AudioSampleRateCombo),
        AudioChannels = SelectedTag(AudioChannelsCombo),
        AudioNormalize = AudioNormalizeCheck.IsChecked == true
    };

    private void ApplySettings(AppSettings settings)
    {
        FfmpegPathText.Text = settings.FfmpegPath ?? string.Empty;
        OutputBesideSourceCheck.IsChecked = settings.OutputBesideSource;
        OutputFolderText.Text = string.IsNullOrWhiteSpace(settings.OutputFolder)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "MediaForge")
            : settings.OutputFolder;
        PreserveTreeCheck.IsChecked = settings.PreserveFolderTree;
        PreserveTimestampsCheck.IsChecked = settings.PreserveTimestamps;
        StripMetadataCheck.IsChecked = settings.StripMetadata;
        SuffixText.Text = settings.FileSuffix ?? "_converted";
        SelectComboByTag(CollisionCombo, settings.CollisionPolicy);
        SelectComboByTag(ParallelJobsCombo, settings.ParallelJobs.ToString(CultureInfo.InvariantCulture));

        SelectComboByTag(ImageFormatCombo, settings.ImageFormat);
        ImageQualitySlider.Value = Math.Clamp(settings.ImageQuality, 1, 100);
        SelectComboByTag(ImageResizeModeCombo, settings.ImageResizeMode switch
        {
            "Fill" => "Crop",
            "Exact" => "Stretch",
            _ => settings.ImageResizeMode
        });
        ImageWidthText.Text = settings.ImageWidth.ToString(CultureInfo.InvariantCulture);
        ImageHeightText.Text = settings.ImageHeight.ToString(CultureInfo.InvariantCulture);
        ImagePercentText.Text = settings.ImageScalePercent.ToString(CultureInfo.InvariantCulture);
        SelectComboByTag(ImageAspectRatioCombo, settings.ImageAspectRatio);
        ImageCustomAspectWidthText.Text = settings.ImageCustomAspectWidth.ToString("0.###", CultureInfo.InvariantCulture);
        ImageCustomAspectHeightText.Text = settings.ImageCustomAspectHeight.ToString("0.###", CultureInfo.InvariantCulture);

        SelectComboByTag(VideoContainerCombo, settings.VideoContainer);
        SelectComboByTag(VideoCodecCombo, settings.VideoCodec);
        VideoCrfSlider.Value = Math.Clamp(settings.VideoCrf, 0, 51);
        SelectComboByTag(VideoPresetCombo, settings.VideoPreset);
        SelectComboByTag(VideoResolutionCombo, settings.VideoResolution);
        VideoWidthText.Text = settings.VideoWidth.ToString(CultureInfo.InvariantCulture);
        VideoHeightText.Text = settings.VideoHeight.ToString(CultureInfo.InvariantCulture);
        SelectComboByTag(VideoResizeModeCombo, settings.VideoResizeMode);
        SelectComboByTag(VideoAspectRatioCombo, settings.VideoAspectRatio);
        VideoCustomAspectWidthText.Text = settings.VideoCustomAspectWidth.ToString("0.###", CultureInfo.InvariantCulture);
        VideoCustomAspectHeightText.Text = settings.VideoCustomAspectHeight.ToString("0.###", CultureInfo.InvariantCulture);
        SelectComboByTag(VideoFpsCombo, settings.VideoFps);
        VideoCustomFpsText.Text = settings.VideoCustomFps.ToString(CultureInfo.InvariantCulture);
        SelectComboByTag(VideoAudioCodecCombo, settings.VideoAudioCodec);
        VideoAudioBitrateText.Text = settings.VideoAudioBitrate.ToString(CultureInfo.InvariantCulture);
        ExtractAudioCheck.IsChecked = settings.ExtractAudioOnly;

        SelectComboByTag(AudioFormatCombo, settings.AudioFormat);
        AudioBitrateText.Text = settings.AudioBitrate.ToString(CultureInfo.InvariantCulture);
        SelectComboByTag(AudioSampleRateCombo, settings.AudioSampleRate);
        SelectComboByTag(AudioChannelsCombo, settings.AudioChannels);
        AudioNormalizeCheck.IsChecked = settings.AudioNormalize;
    }

    private void SaveSettings()
    {
        _settingsService.Save(CaptureCurrentSettings());
    }

    private void SetRunningState(bool running)
    {
        _isRunning = running;
        _viewModel.SetRunning(running);
        var canEdit = !running && !_projectReadOnly;
        AddFilesButton.IsEnabled = canEdit;
        AddFolderButton.IsEnabled = canEdit;
        RemoveButton.IsEnabled = canEdit;
        ClearButton.IsEnabled = canEdit;
        RetryButton.IsEnabled = canEdit;
        StartButton.IsEnabled = canEdit;
        CancelButton.IsEnabled = running;
        OptionsTabs.IsEnabled = canEdit;
        UpdateProjectControlState();
        UpdatePreviewEditButton();
        UpdateWorkflowControlState();
    }

    private void UpdateSummary()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(UpdateSummary);
            return;
        }

        _viewModel.RefreshSummary();
    }

    private void AppendLog(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => AppendLog(text));
            return;
        }

        LogText.AppendText($"[{DateTime.Now:HH:mm:ss}] {text.TrimEnd()}{Environment.NewLine}");
        LogText.ScrollToEnd();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            WriteRecoveryBeforeRiskyTransition();
            _isClosing = true;
            _windowCancellation.Cancel();
            try { SaveSettings(); } catch { }
            return;
        }

        if (_isRunning)
        {
            if (_closeWhenBatchStops)
            {
                e.Cancel = true;
                return;
            }

            var answer = System.Windows.MessageBox.Show(this,
                "A batch is still running. Cancel it and close MediaForge?",
                "Close MediaForge",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            WriteRecoveryBeforeRiskyTransition();
            e.Cancel = true;
            _closeWhenBatchStops = true;
            _isClosing = true;
            _windowCancellation.Cancel();
            _queueCoordinator.Cancel();
            CancelButton.IsEnabled = false;
            _viewModel.SetStatusOverride("Cancelling active FFmpeg processes before closing…");
            return;
        }

        if (!ConfirmProjectTransition())
        {
            e.Cancel = true;
            return;
        }

        _isClosing = true;
        _windowCancellation.Cancel();
        try { SaveSettings(); } catch { }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _session.Changed -= Session_Changed;
        _queueCoordinator.StateChanged -= QueueCoordinator_StateChanged;
        DisposeProjectFeatures();
        _windowCancellation.Dispose();
    }

    private static string SelectedTag(System.Windows.Controls.ComboBox comboBox)
    {
        return comboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item
            ? item.Tag?.ToString() ?? item.Content?.ToString() ?? string.Empty
            : comboBox.Text;
    }

    private static void SelectComboByTag(System.Windows.Controls.ComboBox comboBox, string? tag)
    {
        var match = comboBox.Items.Cast<object>()
            .OfType<System.Windows.Controls.ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase));
        comboBox.SelectedItem = match ?? comboBox.Items.Cast<object>().OfType<System.Windows.Controls.ComboBoxItem>().FirstOrDefault();
    }

    private (int Width, int Height) ResolveEditorImageDimensions(MediaJob job)
    {
        var mode = SelectedTag(ImageResizeModeCombo);
        if (mode is not ("None" or "Percent"))
        {
            return (ParseInt(ImageWidthText.Text, 1920), ParseInt(ImageHeightText.Text, 1080));
        }

        try
        {
            var frame = BitmapFrame.Create(new Uri(job.SourcePath, UriKind.Absolute), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            var scale = mode == "Percent" ? Math.Clamp(ParseInt(ImagePercentText.Text, 100), 1, 10000) / 100d : 1d;
            return (
                Math.Max(1, (int)Math.Round(frame.PixelWidth * scale)),
                Math.Max(1, (int)Math.Round(frame.PixelHeight * scale)));
        }
        catch
        {
            return (0, 0);
        }
    }

    private (int Width, int Height) ResolveVideoDimensions() => SelectedTag(VideoResolutionCombo) switch
    {
        "720p" => (1280, 720),
        "1080p" => (1920, 1080),
        "1440p" => (2560, 1440),
        "2160p" => (3840, 2160),
        _ => (ParseInt(VideoWidthText.Text, 1920), ParseInt(VideoHeightText.Text, 1080))
    };

    private static string NormaliseEditorResizeMode(string mode) => mode switch
    {
        "None" or "Percent" => "Fit",
        _ => mode
    };

    private static double ParseAspectRatio(string text)
    {
        var parts = text.Split(':');
        return parts.Length == 2 &&
               double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) &&
               double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var height) && height > 0
            ? width / height
            : 0;
    }


    private static int ParseInt(string? text, int fallback) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    private static double ParseDouble(string? text, double fallback)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariant) && double.IsFinite(invariant))
        {
            return invariant;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var current) && double.IsFinite(current)
            ? current
            : fallback;
    }


    private static string FirstUsefulLine(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "Unknown error";
        var lines = message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();

        var preferred = lines.AsEnumerable().Reverse().FirstOrDefault(line =>
            line.Contains("Unknown encoder", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Invalid argument", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Permission denied", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("not found", StringComparison.OrdinalIgnoreCase));

        return preferred ?? lines.AsEnumerable().Reverse().FirstOrDefault(line =>
            !line.Equals("Conversion failed!", StringComparison.OrdinalIgnoreCase))
            ?? lines.LastOrDefault()
            ?? "Unknown error";
    }
}
