using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MediaForge.Models;
using MediaForge.Services;
using Microsoft.Win32;

namespace MediaForge;

public partial class MediaEditorWindow : Window
{
    private readonly MediaJob _job;
    private readonly string _ffprobePath;
    private readonly MediaProbeService _probeService = new();
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly ObservableCollection<MediaClipEdit> _clips = [];
    private readonly DispatcherTimer _previewTimer;
    private MediaClipEdit? _currentClip;
    private BitmapImage? _imageSource;
    private double _sourceWidth;
    private double _sourceHeight;
    private Rect _mediaBounds;
    private bool _selectionInitialised;
    private bool _isDraggingSelection;
    private bool _isSeeking;
    private bool _isPlaying;
    private bool _playWhenOpened;
    private bool _isClosing;
    private int _suggestedWidth;
    private int _suggestedHeight;
    private Point _selectionDragStart;
    private Rect _selectionDragOrigin;
    private CropSelection _imageCrop = CropSelection.Full;

    public MediaEditPlan? ResultPlan { get; private set; }

    public MediaEditorWindow(
        MediaJob job,
        string ffprobePath,
        string defaultResizeMode,
        int defaultWidth,
        int defaultHeight,
        string defaultAspectRatio,
        double customAspectWidth,
        double customAspectHeight)
    {
        InitializeComponent();
        _job = job;
        _ffprobePath = ffprobePath;
        ClipsList.ItemsSource = _clips;

        var existing = job.EditPlan;
        SelectComboByTag(ResizeModeCombo, existing?.ResizeMode ?? defaultResizeMode);
        SelectComboByTag(AspectRatioCombo, existing?.AspectRatio ?? defaultAspectRatio);
        TargetWidthText.Text = (existing?.TargetWidth ?? defaultWidth).ToString(CultureInfo.InvariantCulture);
        TargetHeightText.Text = (existing?.TargetHeight ?? defaultHeight).ToString(CultureInfo.InvariantCulture);
        CustomAspectWidthText.Text = (existing?.CustomAspectWidth ?? customAspectWidth).ToString("0.###", CultureInfo.InvariantCulture);
        CustomAspectHeightText.Text = (existing?.CustomAspectHeight ?? customAspectHeight).ToString("0.###", CultureInfo.InvariantCulture);
        LockSelectionAspectCheck.IsChecked = existing?.LockSelectionAspect ?? true;
        _imageCrop = existing?.ImageCrop ?? CropSelection.Full;

        if (existing is not null)
        {
            foreach (var clip in existing.Clips)
            {
                _clips.Add(CloneClip(clip));
            }
        }

        _previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _previewTimer.Tick += PreviewTimer_Tick;
        Loaded += MediaEditorWindow_Loaded;
        Closing += MediaEditorWindow_Closing;
        Closed += MediaEditorWindow_Closed;
        UpdateCustomAspectVisibility();
    }

    private async void MediaEditorWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_job.Kind == MediaKind.Image)
            {
                ClipPanel.Visibility = Visibility.Collapsed;
                ClipColumn.Width = new GridLength(0);
                TimelinePanel.Visibility = Visibility.Collapsed;
                await LoadImageAsync(_job.SourcePath);
                return;
            }

            if (_job.Kind != MediaKind.Video)
            {
                PreviewStatusText.Text = "Preview editing is available for image and video jobs.";
                PreviewStatusText.Visibility = Visibility.Visible;
                return;
            }

            if (_clips.Count == 0)
            {
                await AddVideoClipAsync(_job.SourcePath, select: true);
            }
            else
            {
                ClipsList.SelectedIndex = 0;
            }

            UpdateResolutionSuggestion();
            _previewTimer.Start();
        }
        catch (OperationCanceledException) when (_isClosing)
        {
            // The editor is closing.
        }
        catch (Exception ex)
        {
            ShowPreviewStatus($"Could not initialise the editor: {ex.Message}");
        }
    }

    private void MediaEditorWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _isClosing = true;
        _lifetimeCancellation.Cancel();
    }

    private void MediaEditorWindow_Closed(object? sender, EventArgs e)
    {
        _previewTimer.Stop();
        _lifetimeCancellation.Dispose();
        try
        {
            VideoPreview.Stop();
            VideoPreview.Source = null;
        }
        catch
        {
        }
    }

    private Task LoadImageAsync(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();

        _imageSource = bitmap;
        _sourceWidth = bitmap.PixelWidth;
        _sourceHeight = bitmap.PixelHeight;
        ApplySourceTargetIfNeeded();
        ImagePreview.Source = bitmap;
        ImagePreview.Visibility = Visibility.Visible;
        VideoPreview.Visibility = Visibility.Collapsed;
        PreviewStatusText.Visibility = Visibility.Collapsed;
        _selectionInitialised = false;
        UpdatePreviewLayout();
        return Task.CompletedTask;
    }

    private async Task AddVideoClipAsync(string path, bool select)
    {
        var info = await _probeService.ProbeAsync(path, _ffprobePath, _lifetimeCancellation.Token);
        if (!info.HasVideo) throw new InvalidOperationException($"{Path.GetFileName(path)} does not contain a video stream.");
        if (info.DurationSeconds <= 0)
        {
            throw new InvalidOperationException($"Could not determine the duration of {Path.GetFileName(path)}. It can still be converted without timeline edits.");
        }

        var clip = new MediaClipEdit
        {
            SourcePath = Path.GetFullPath(path),
            DurationSeconds = Math.Max(0.001, info.DurationSeconds),
            Width = info.Width,
            Height = info.Height,
            FrameRate = info.FrameRate,
            HasAudio = info.HasAudio,
            TrimEndSeconds = Math.Max(0.001, info.DurationSeconds),
            Crop = CropSelection.Full
        };
        _clips.Add(clip);
        if (select) ClipsList.SelectedItem = clip;
        UpdateResolutionSuggestion();
    }

    private void ClipsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        StoreCurrentSelection();
        StoreTrimText();
        _currentClip = ClipsList.SelectedItem as MediaClipEdit;
        if (_currentClip is null) return;

        try
        {
            _isPlaying = false;
            VideoPreview.Pause();
            PlayPauseButton.Content = "Play";
            _sourceWidth = Math.Max(1, _currentClip.Width);
            _sourceHeight = Math.Max(1, _currentClip.Height);
            ApplySourceTargetIfNeeded();
            VideoPreview.Source = null;
            VideoPreview.Source = new Uri(_currentClip.SourcePath, UriKind.Absolute);
            VideoPreview.Visibility = Visibility.Visible;
            ImagePreview.Visibility = Visibility.Collapsed;
            VideoPreview.Position = TimeSpan.FromSeconds(_currentClip.TrimStartSeconds);
            SeekSlider.Minimum = _currentClip.TrimStartSeconds;
            SeekSlider.Maximum = Math.Max(_currentClip.TrimStartSeconds + 0.001, _currentClip.TrimEndSeconds);
            SeekSlider.Value = _currentClip.TrimStartSeconds;
            TrimStartText.Text = FormatTime(_currentClip.TrimStartSeconds);
            TrimEndText.Text = FormatTime(_currentClip.TrimEndSeconds);
            DurationText.Text = FormatTime(_currentClip.TrimEndSeconds);
            _selectionInitialised = false;
            UpdatePreviewLayout();
        }
        catch (Exception ex)
        {
            ShowPreviewStatus($"Windows preview could not open this clip. FFmpeg export may still work. {ex.Message}");
        }
    }

    private void VideoPreview_MediaOpened(object sender, RoutedEventArgs e)
    {
        if (_currentClip is null) return;
        if (VideoPreview.NaturalVideoWidth > 0 && VideoPreview.NaturalVideoHeight > 0)
        {
            _sourceWidth = VideoPreview.NaturalVideoWidth;
            _sourceHeight = VideoPreview.NaturalVideoHeight;
        }
        VideoPreview.Position = TimeSpan.FromSeconds(_currentClip.TrimStartSeconds);
        PreviewStatusText.Visibility = Visibility.Collapsed;
        UpdatePreviewLayout();
        if (_playWhenOpened)
        {
            _playWhenOpened = false;
            VideoPreview.Play();
            _isPlaying = true;
            PlayPauseButton.Content = "Pause";
        }
    }

    private void VideoPreview_MediaEnded(object sender, RoutedEventArgs e)
    {
        if (!AdvanceSequencePreview())
        {
            PausePreview();
            if (_currentClip is not null) SeekTo(_currentClip.TrimStartSeconds);
        }
    }

    private void VideoPreview_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        _playWhenOpened = false;
        PausePreview();
        ShowPreviewStatus("Windows could not decode this clip for live preview. The FFmpeg export path remains available. " + e.ErrorException.Message);
    }

    private void PreviewCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => UpdatePreviewLayout();

    private void UpdatePreviewLayout()
    {
        if (PreviewCanvas.ActualWidth <= 1 || PreviewCanvas.ActualHeight <= 1 || _sourceWidth <= 0 || _sourceHeight <= 0) return;
        var retainedCrop = _selectionInitialised && _mediaBounds.Width > 0 && _mediaBounds.Height > 0
            ? GetSelectionCrop()
            : (_job.Kind == MediaKind.Image ? _imageCrop : _currentClip?.Crop ?? CropSelection.Full);
        const double margin = 18;
        var availableWidth = Math.Max(1, PreviewCanvas.ActualWidth - margin * 2);
        var availableHeight = Math.Max(1, PreviewCanvas.ActualHeight - margin * 2);
        var scale = Math.Min(availableWidth / _sourceWidth, availableHeight / _sourceHeight);
        var width = Math.Max(1, _sourceWidth * scale);
        var height = Math.Max(1, _sourceHeight * scale);
        var left = (PreviewCanvas.ActualWidth - width) / 2;
        var top = (PreviewCanvas.ActualHeight - height) / 2;

        _mediaBounds = new Rect(left, top, width, height);
        Canvas.SetLeft(MediaHost, left);
        Canvas.SetTop(MediaHost, top);
        MediaHost.Width = width;
        MediaHost.Height = height;

        SetSelectionFromCrop(retainedCrop);
        _selectionInitialised = true;
        UpdateSelectionInfo();
    }

    private void SetSelectionFromCrop(CropSelection crop)
    {
        crop = crop.Clamp();
        var rect = new Rect(
            _mediaBounds.Left + crop.X * _mediaBounds.Width,
            _mediaBounds.Top + crop.Y * _mediaBounds.Height,
            crop.Width * _mediaBounds.Width,
            crop.Height * _mediaBounds.Height);
        SetSelectionRect(rect);
    }

    private CropSelection GetSelectionCrop()
    {
        if (_mediaBounds.Width <= 0 || _mediaBounds.Height <= 0) return CropSelection.Full;
        var rect = GetSelectionRect();
        return new CropSelection(
            (rect.Left - _mediaBounds.Left) / _mediaBounds.Width,
            (rect.Top - _mediaBounds.Top) / _mediaBounds.Height,
            rect.Width / _mediaBounds.Width,
            rect.Height / _mediaBounds.Height).Clamp();
    }

    private Rect GetSelectionRect() => new(
        Canvas.GetLeft(SelectionBox),
        Canvas.GetTop(SelectionBox),
        SelectionBox.Width,
        SelectionBox.Height);

    private void SetSelectionRect(Rect rect, bool preserveAspect = false)
    {
        const double preferredMinimum = 24;
        var maximumWidth = Math.Max(1, _mediaBounds.Width);
        var maximumHeight = Math.Max(1, _mediaBounds.Height);
        var minimumWidth = Math.Min(preferredMinimum, maximumWidth);
        var minimumHeight = Math.Min(preferredMinimum, maximumHeight);
        var width = rect.Width;
        var height = rect.Height;

        if (preserveAspect && width > 0 && height > 0)
        {
            var ratio = width / height;
            if (width < minimumWidth)
            {
                width = minimumWidth;
                height = width / ratio;
            }
            if (height < minimumHeight)
            {
                height = minimumHeight;
                width = height * ratio;
            }
            var scale = Math.Min(1d, Math.Min(maximumWidth / width, maximumHeight / height));
            width *= scale;
            height *= scale;
        }
        else
        {
            width = Math.Clamp(width, minimumWidth, maximumWidth);
            height = Math.Clamp(height, minimumHeight, maximumHeight);
        }

        var left = Math.Clamp(rect.Left, _mediaBounds.Left, _mediaBounds.Right - width);
        var top = Math.Clamp(rect.Top, _mediaBounds.Top, _mediaBounds.Bottom - height);
        Canvas.SetLeft(SelectionBox, left);
        Canvas.SetTop(SelectionBox, top);
        SelectionBox.Width = width;
        SelectionBox.Height = height;
        UpdateSelectionInfo();
    }

    private void ConstrainSelectionToMedia() => SetSelectionRect(GetSelectionRect(), LockSelectionAspectCheck.IsChecked == true);

    private void StoreCurrentSelection()
    {
        if (!_selectionInitialised) return;
        var crop = GetSelectionCrop();
        if (_job.Kind == MediaKind.Image) _imageCrop = crop;
        else if (_currentClip is not null) _currentClip.Crop = crop;
    }

    private void Selection_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSelection = true;
        _selectionDragStart = e.GetPosition(PreviewCanvas);
        _selectionDragOrigin = GetSelectionRect();
        SelectionBox.CaptureMouse();
        e.Handled = true;
    }

    private void Selection_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingSelection || e.LeftButton != MouseButtonState.Pressed) return;
        var point = e.GetPosition(PreviewCanvas);
        var delta = point - _selectionDragStart;
        SetSelectionRect(new Rect(_selectionDragOrigin.Left + delta.X, _selectionDragOrigin.Top + delta.Y,
            _selectionDragOrigin.Width, _selectionDragOrigin.Height));
    }

    private void Selection_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSelection = false;
        SelectionBox.ReleaseMouseCapture();
        StoreCurrentSelection();
        e.Handled = true;
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb thumb) return;
        var direction = thumb.Tag?.ToString() ?? string.Empty;
        var rect = GetSelectionRect();
        var left = rect.Left;
        var top = rect.Top;
        var right = rect.Right;
        var bottom = rect.Bottom;

        if (direction.Contains('W')) left += e.HorizontalChange;
        if (direction.Contains('E')) right += e.HorizontalChange;
        if (direction.Contains('N')) top += e.VerticalChange;
        if (direction.Contains('S')) bottom += e.VerticalChange;

        var proposed = RectFromEdges(left, top, right, bottom);
        if (LockSelectionAspectCheck.IsChecked == true)
        {
            proposed = ApplySelectionAspect(proposed, direction, Math.Abs(e.HorizontalChange) >= Math.Abs(e.VerticalChange));
        }

        SetSelectionRect(proposed, LockSelectionAspectCheck.IsChecked == true);
        StoreCurrentSelection();
        e.Handled = true;
    }

    private Rect ApplySelectionAspect(Rect proposed, string direction, bool horizontalPrimary)
    {
        var ratio = ResolveSelectionAspectRatio();
        if (ratio <= 0) return proposed;
        var width = proposed.Width;
        var height = proposed.Height;

        if (horizontalPrimary || direction is "E" or "W") height = width / ratio;
        else width = height * ratio;

        var left = proposed.Left;
        var top = proposed.Top;
        if (direction.Contains('W')) left = proposed.Right - width;
        if (direction.Contains('N')) top = proposed.Bottom - height;
        if (direction is "E" or "W") top = proposed.Top + (proposed.Height - height) / 2;
        if (direction is "N" or "S") left = proposed.Left + (proposed.Width - width) / 2;
        return new Rect(left, top, Math.Max(1, width), Math.Max(1, height));
    }

    private static Rect RectFromEdges(double left, double top, double right, double bottom)
    {
        if (right < left) (left, right) = (right, left);
        if (bottom < top) (top, bottom) = (bottom, top);
        return new Rect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
    }

    private void ResetCrop_Click(object sender, RoutedEventArgs e)
    {
        SetSelectionFromCrop(CropSelection.Full);
        StoreCurrentSelection();
    }

    private void CenterSelection_Click(object sender, RoutedEventArgs e)
    {
        var rect = GetSelectionRect();
        SetSelectionRect(new Rect(
            _mediaBounds.Left + (_mediaBounds.Width - rect.Width) / 2,
            _mediaBounds.Top + (_mediaBounds.Height - rect.Height) / 2,
            rect.Width,
            rect.Height));
        StoreCurrentSelection();
    }

    private void UpdateSelectionInfo()
    {
        if (_mediaBounds.Width <= 0 || _sourceWidth <= 0) return;
        var crop = GetSelectionCrop();
        var width = Math.Max(1, (int)Math.Round(_sourceWidth * crop.Width));
        var height = Math.Max(1, (int)Math.Round(_sourceHeight * crop.Height));
        var x = Math.Max(0, (int)Math.Round(_sourceWidth * crop.X));
        var y = Math.Max(0, (int)Math.Round(_sourceHeight * crop.Y));
        SelectionInfoText.Text = $"Selection: {width}×{height} at {x},{y}";
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_currentClip is null) return;
        if (_isPlaying) PausePreview();
        else
        {
            if (VideoPreview.Position.TotalSeconds >= _currentClip.TrimEndSeconds - 0.01)
            {
                VideoPreview.Position = TimeSpan.FromSeconds(_currentClip.TrimStartSeconds);
            }
            VideoPreview.Play();
            _isPlaying = true;
            PlayPauseButton.Content = "Pause";
        }
    }

    private void PausePreview()
    {
        try { VideoPreview.Pause(); } catch { }
        _isPlaying = false;
        PlayPauseButton.Content = "Play";
    }

    private bool AdvanceSequencePreview()
    {
        if (!_isPlaying || PlaySequenceCheck.IsChecked != true || _currentClip is null) return false;
        var index = _clips.IndexOf(_currentClip);
        if (index < 0 || index >= _clips.Count - 1) return false;

        StoreCurrentSelection();
        StoreTrimText();
        _playWhenOpened = true;
        ClipsList.SelectedIndex = index + 1;
        return true;
    }

    private void PreviewTimer_Tick(object? sender, EventArgs e)
    {
        if (_currentClip is null || _isSeeking) return;
        var position = VideoPreview.Position.TotalSeconds;
        if (position >= _currentClip.TrimEndSeconds)
        {
            if (AdvanceSequencePreview()) return;
            PausePreview();
            position = _currentClip.TrimEndSeconds;
            VideoPreview.Position = TimeSpan.FromSeconds(position);
        }
        SeekSlider.Value = Math.Clamp(position, SeekSlider.Minimum, SeekSlider.Maximum);
        CurrentTimeText.Text = FormatTime(position);
    }

    private void SeekSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e) => _isSeeking = true;

    private void SeekSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        SeekTo(SeekSlider.Value);
        _isSeeking = false;
    }

    private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isSeeking) CurrentTimeText.Text = FormatTime(e.NewValue);
    }

    private void SeekTo(double seconds)
    {
        if (_currentClip is null) return;
        var target = Math.Clamp(seconds, _currentClip.TrimStartSeconds, _currentClip.TrimEndSeconds);
        VideoPreview.Position = TimeSpan.FromSeconds(target);
        SeekSlider.Value = target;
        CurrentTimeText.Text = FormatTime(target);
    }

    private void SkipBack5_Click(object sender, RoutedEventArgs e) => SeekRelative(-5);
    private void SkipBack1_Click(object sender, RoutedEventArgs e) => SeekRelative(-1);
    private void SkipForward1_Click(object sender, RoutedEventArgs e) => SeekRelative(1);
    private void SkipForward5_Click(object sender, RoutedEventArgs e) => SeekRelative(5);
    private void PreviousFrame_Click(object sender, RoutedEventArgs e) => SeekFrame(-1);
    private void NextFrame_Click(object sender, RoutedEventArgs e) => SeekFrame(1);

    private void SeekRelative(double seconds)
    {
        PausePreview();
        SeekTo(VideoPreview.Position.TotalSeconds + seconds);
    }

    private void SeekFrame(int direction)
    {
        PausePreview();
        var fps = _currentClip?.FrameRate > 0 ? _currentClip.FrameRate : 30;
        SeekTo(VideoPreview.Position.TotalSeconds + direction / fps);
    }

    private void SetTrimStart_Click(object sender, RoutedEventArgs e)
    {
        if (_currentClip is null) return;
        _currentClip.SetTrimRange(
            Math.Min(VideoPreview.Position.TotalSeconds, _currentClip.TrimEndSeconds - 0.001),
            _currentClip.TrimEndSeconds);
        RefreshTrimControls();
    }

    private void SetTrimEnd_Click(object sender, RoutedEventArgs e)
    {
        if (_currentClip is null) return;
        _currentClip.SetTrimRange(
            _currentClip.TrimStartSeconds,
            Math.Max(VideoPreview.Position.TotalSeconds, _currentClip.TrimStartSeconds + 0.001));
        RefreshTrimControls();
    }

    private void SplitClip_Click(object sender, RoutedEventArgs e)
    {
        if (_currentClip is null) return;
        StoreCurrentSelection();
        StoreTrimText();
        var split = VideoPreview.Position.TotalSeconds;
        if (split <= _currentClip.TrimStartSeconds + 0.001 || split >= _currentClip.TrimEndSeconds - 0.001)
        {
            System.Windows.MessageBox.Show(this, "Move the playhead inside the retained clip range before splitting.",
                "Cannot split clip", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var first = CloneClip(_currentClip);
        first.SetTrimRange(first.TrimStartSeconds, split);
        var second = CloneClip(_currentClip);
        second.SetTrimRange(split, second.TrimEndSeconds);
        var index = _clips.IndexOf(_currentClip);
        _clips.RemoveAt(index);
        _clips.Insert(index, first);
        _clips.Insert(index + 1, second);
        ClipsList.SelectedItem = second;
        UpdateResolutionSuggestion();
    }

    private void ResetTrim_Click(object sender, RoutedEventArgs e)
    {
        if (_currentClip is null) return;
        _currentClip.SetTrimRange(0, _currentClip.DurationSeconds);
        RefreshTrimControls();
    }

    private void TrimText_LostFocus(object sender, RoutedEventArgs e) => StoreTrimText();

    private void StoreTrimText()
    {
        if (_currentClip is null) return;
        var start = TryParseTime(TrimStartText.Text, out var parsedStart)
            ? parsedStart
            : _currentClip.TrimStartSeconds;
        var end = TryParseTime(TrimEndText.Text, out var parsedEnd)
            ? parsedEnd
            : _currentClip.TrimEndSeconds;
        _currentClip.SetTrimRange(start, end);
        RefreshTrimControls();
    }

    private void RefreshTrimControls()
    {
        if (_currentClip is null) return;
        TrimStartText.Text = FormatTime(_currentClip.TrimStartSeconds);
        TrimEndText.Text = FormatTime(_currentClip.TrimEndSeconds);
        SeekSlider.Minimum = _currentClip.TrimStartSeconds;
        SeekSlider.Maximum = Math.Max(_currentClip.TrimStartSeconds + 0.001, _currentClip.TrimEndSeconds);
        DurationText.Text = FormatTime(_currentClip.TrimEndSeconds);
        SeekTo(Math.Clamp(VideoPreview.Position.TotalSeconds, _currentClip.TrimStartSeconds, _currentClip.TrimEndSeconds));
    }

    private async void AddClips_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Add video clips",
            Multiselect = true,
            CheckFileExists = true,
            Filter = $"Supported video|{MediaClassifier.VideoDialogPattern}|All files|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;
        await AddClipPathsAsync(dialog.FileNames);
    }

    private async Task AddClipPathsAsync(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            try
            {
                if (MediaClassifier.Classify(path) != MediaKind.Video) continue;
                await AddVideoClipAsync(path, select: false);
            }
            catch (OperationCanceledException) when (_isClosing)
            {
                return;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(this, $"Could not add {Path.GetFileName(path)}: {ex.Message}", "Add clip", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        if (ClipsList.SelectedItem is null && _clips.Count > 0) ClipsList.SelectedIndex = 0;
        UpdateResolutionSuggestion();
    }

    private void RemoveClip_Click(object sender, RoutedEventArgs e)
    {
        if (ClipsList.SelectedItem is not MediaClipEdit clip) return;
        if (_clips.Count <= 1)
        {
            System.Windows.MessageBox.Show(this, "A video edit must contain at least one clip.", "MediaForge", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var index = _clips.IndexOf(clip);
        _clips.Remove(clip);
        ClipsList.SelectedIndex = Math.Min(index, _clips.Count - 1);
        UpdateResolutionSuggestion();
    }

    private void MoveClipUp_Click(object sender, RoutedEventArgs e) => MoveSelectedClip(-1);
    private void MoveClipDown_Click(object sender, RoutedEventArgs e) => MoveSelectedClip(1);

    private void MoveSelectedClip(int direction)
    {
        if (ClipsList.SelectedItem is not MediaClipEdit clip) return;
        var oldIndex = _clips.IndexOf(clip);
        var newIndex = oldIndex + direction;
        if (newIndex < 0 || newIndex >= _clips.Count) return;
        _clips.Move(oldIndex, newIndex);
        ClipsList.SelectedItem = clip;
    }

    private void ClipsList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void ClipsList_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
        await AddClipPathsAsync(paths.Where(File.Exists));
    }

    private void UpdateResolutionSuggestion()
    {
        _suggestedWidth = 0;
        _suggestedHeight = 0;
        ApplyResolutionSuggestionButton.IsEnabled = false;
        if (_clips.Count == 0)
        {
            ResolutionSuggestionText.Text = "Drag video files here to append them to the stitched output.";
            return;
        }

        var resolutions = _clips.Select(clip => (clip.Width, clip.Height)).Distinct().ToList();
        if (resolutions.Count <= 1)
        {
            ResolutionSuggestionText.Text = "All clips use the same resolution. Drag more clips here to stitch them in this order.";
            return;
        }

        var largest = _clips.OrderByDescending(clip => (long)clip.Width * clip.Height).First();
        _suggestedWidth = largest.Width;
        _suggestedHeight = largest.Height;
        ApplyResolutionSuggestionButton.IsEnabled = _suggestedWidth > 0 && _suggestedHeight > 0;
        ResolutionSuggestionText.Text =
            $"Resolution mismatch detected ({string.Join(", ", resolutions.Select(r => $"{r.Width}×{r.Height}"))}). " +
            $"Suggested target: {largest.Width}×{largest.Height}. Use Fit to preserve every frame with padding, Crop to fill without bars, or Stretch only when distortion is acceptable.";
    }

    private void ApplyResolutionSuggestion_Click(object sender, RoutedEventArgs e)
    {
        if (_suggestedWidth <= 0 || _suggestedHeight <= 0) return;
        TargetWidthText.Text = _suggestedWidth.ToString(CultureInfo.InvariantCulture);
        TargetHeightText.Text = _suggestedHeight.ToString(CultureInfo.InvariantCulture);
        SelectComboByTag(ResizeModeCombo, "Fit");
        SelectComboByTag(AspectRatioCombo, "Free");
    }

    private void ResizeModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        UpdateResolutionSuggestion();
    }

    private void AspectRatioCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCustomAspectVisibility();
        if (!IsLoaded) return;
        if (SelectedTag(AspectRatioCombo) != "Free") ApplyAspectRatioToTarget();
    }

    private void UpdateCustomAspectVisibility()
    {
        if (CustomAspectPanel is null || AspectRatioCombo is null) return;
        CustomAspectPanel.Visibility = SelectedTag(AspectRatioCombo) == "Custom" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyRatio_Click(object sender, RoutedEventArgs e) => ApplyAspectRatioToTarget();

    private void ApplySourceTargetIfNeeded()
    {
        if (TryParsePositiveInt(TargetWidthText.Text, out _) && TryParsePositiveInt(TargetHeightText.Text, out _)) return;
        var maximum = _job.Kind == MediaKind.Image ? 100000 : 16384;
        TargetWidthText.Text = Math.Clamp((int)Math.Round(_sourceWidth), 1, maximum).ToString(CultureInfo.InvariantCulture);
        TargetHeightText.Text = Math.Clamp((int)Math.Round(_sourceHeight), 1, maximum).ToString(CultureInfo.InvariantCulture);
    }

    private void ApplyAspectRatioToTarget()
    {
        if (!TryParsePositiveInt(TargetWidthText.Text, out var width)) return;
        var ratio = ResolveTargetAspectRatio();
        if (ratio <= 0) return;
        var targetHeight = width / ratio;
        var maximum = _job.Kind == MediaKind.Image ? 100000 : 16384;
        if (!double.IsFinite(targetHeight) || targetHeight > maximum) return;
        TargetHeightText.Text = Math.Max(1, (int)Math.Round(targetHeight)).ToString(CultureInfo.InvariantCulture);
        if (LockSelectionAspectCheck.IsChecked == true) FitSelectionToAspect(ratio);
    }

    private void FitSelectionToAspect(double ratio)
    {
        if (!_selectionInitialised || ratio <= 0 || _mediaBounds.Width <= 0 || _mediaBounds.Height <= 0) return;
        var rect = GetSelectionRect();
        var width = rect.Width;
        var height = width / ratio;
        if (height > _mediaBounds.Height)
        {
            height = _mediaBounds.Height;
            width = height * ratio;
        }
        if (width > _mediaBounds.Width)
        {
            width = _mediaBounds.Width;
            height = width / ratio;
        }
        SetSelectionRect(new Rect(
            _mediaBounds.Left + (_mediaBounds.Width - width) / 2,
            _mediaBounds.Top + (_mediaBounds.Height - height) / 2,
            width, height), preserveAspect: true);
        StoreCurrentSelection();
    }

    private double ResolveTargetAspectRatio()
    {
        var tag = SelectedTag(AspectRatioCombo);
        if (tag == "Free") return 0;
        if (tag == "Source") return _sourceHeight > 0 ? _sourceWidth / _sourceHeight : 0;
        if (tag == "Custom")
        {
            if (!TryParsePositiveDouble(CustomAspectWidthText.Text, out var customWidth) ||
                !TryParsePositiveDouble(CustomAspectHeightText.Text, out var customHeight)) return 0;
            var customRatio = customWidth / customHeight;
            return double.IsFinite(customRatio) && customRatio > 0 ? customRatio : 0;
        }
        return ParseRatio(tag);
    }

    private double ResolveSelectionAspectRatio()
    {
        var ratio = ResolveTargetAspectRatio();
        if (ratio > 0) return ratio;
        var current = GetSelectionRect();
        return current.Height > 0 ? current.Width / current.Height : 0;
    }

    private void ApplyEdits_Click(object sender, RoutedEventArgs e)
    {
        StoreCurrentSelection();
        StoreTrimText();
        if (SelectedTag(AspectRatioCombo) == "Custom" &&
            (!TryParsePositiveDouble(CustomAspectWidthText.Text, out var checkedCustomWidth) ||
             !TryParsePositiveDouble(CustomAspectHeightText.Text, out var checkedCustomHeight) ||
             checkedCustomWidth / checkedCustomHeight <= 0 ||
             !double.IsFinite(checkedCustomWidth / checkedCustomHeight)))
        {
            System.Windows.MessageBox.Show(this, "Custom aspect ratio values must be positive numbers.", "Cannot apply edits", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (SelectedTag(AspectRatioCombo) != "Free") ApplyAspectRatioToTarget();
        if (!TryParsePositiveInt(TargetWidthText.Text, out var width) || !TryParsePositiveInt(TargetHeightText.Text, out var height))
        {
            System.Windows.MessageBox.Show(this, "Target width and height must be positive whole numbers.", "Cannot apply edits", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var maximumDimension = _job.Kind == MediaKind.Image ? 100000 : 16384;
        if (width > maximumDimension || height > maximumDimension)
        {
            System.Windows.MessageBox.Show(this, $"Target dimensions cannot exceed {maximumDimension} pixels for this media type.", "Cannot apply edits", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var customWidth = TryParsePositiveDouble(CustomAspectWidthText.Text, out var parsedWidth) ? parsedWidth : 16;
        var customHeight = TryParsePositiveDouble(CustomAspectHeightText.Text, out var parsedHeight) ? parsedHeight : 9;
        var plan = new MediaEditPlan
        {
            ResizeMode = SelectedTag(ResizeModeCombo),
            TargetWidth = width,
            TargetHeight = height,
            AspectRatio = SelectedTag(AspectRatioCombo),
            CustomAspectWidth = customWidth,
            CustomAspectHeight = customHeight,
            LockSelectionAspect = LockSelectionAspectCheck.IsChecked == true,
            ImageCrop = _imageCrop
        };

        foreach (var clip in _clips) plan.Clips.Add(CloneClip(clip));
        ResultPlan = plan;
        DialogResult = true;
    }

    private void ShowPreviewStatus(string message)
    {
        PreviewStatusText.Text = message;
        PreviewStatusText.Visibility = Visibility.Visible;
    }

    private static MediaClipEdit CloneClip(MediaClipEdit clip) => new()
    {
        SourcePath = clip.SourcePath,
        DurationSeconds = clip.DurationSeconds,
        Width = clip.Width,
        Height = clip.Height,
        FrameRate = clip.FrameRate,
        HasAudio = clip.HasAudio,
        TrimStartSeconds = clip.TrimStartSeconds,
        TrimEndSeconds = clip.TrimEndSeconds,
        Crop = clip.Crop
    };

    private static string SelectedTag(ComboBox comboBox) => comboBox.SelectedItem is ComboBoxItem item
        ? item.Tag?.ToString() ?? item.Content?.ToString() ?? string.Empty
        : comboBox.Text;

    private static void SelectComboByTag(ComboBox comboBox, string? tag)
    {
        var match = comboBox.Items.Cast<object>().OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase));
        comboBox.SelectedItem = match ?? comboBox.Items.Cast<object>().OfType<ComboBoxItem>().FirstOrDefault();
    }

    private static double ParseRatio(string text)
    {
        var parts = text.Split(':');
        return parts.Length == 2 &&
               double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) &&
               double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var height) && height > 0
            ? width / height
            : 0;
    }

    private static bool TryParsePositiveInt(string? text, out int value) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value > 0;

    private static bool TryParsePositiveDouble(string? text, out double value) =>
        (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
         double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)) &&
        double.IsFinite(value) && value > 0;

    private static string FormatTime(double seconds) => TimeSpan.FromSeconds(Math.Max(0, seconds)).ToString(@"hh\:mm\:ss\.fff");

    private static bool TryParseTime(string? text, out double seconds)
    {
        seconds = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds) &&
            double.IsFinite(seconds) && seconds >= 0) return true;
        if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var time) && time.TotalSeconds >= 0)
        {
            seconds = time.TotalSeconds;
            return true;
        }
        return false;
    }
}
