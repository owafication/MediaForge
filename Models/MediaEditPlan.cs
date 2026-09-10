using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;

namespace MediaForge.Models;

public sealed record CropSelection(double X, double Y, double Width, double Height)
{
    public static CropSelection Full { get; } = new(0, 0, 1, 1);

    public CropSelection Clamp()
    {
        var x = Math.Clamp(double.IsFinite(X) ? X : 0, 0, 0.999);
        var y = Math.Clamp(double.IsFinite(Y) ? Y : 0, 0, 0.999);
        return new CropSelection(
            x,
            y,
            Math.Clamp(double.IsFinite(Width) ? Width : 1, 0.001, 1 - x),
            Math.Clamp(double.IsFinite(Height) ? Height : 1, 0.001, 1 - y));
    }

    public bool IsFullFrame => X <= 0.0005 && Y <= 0.0005 && Width >= 0.999 && Height >= 0.999;
}

public sealed class MediaClipEdit : INotifyPropertyChanged
{
    private double _trimStartSeconds;
    private double _trimEndSeconds;
    private CropSelection _crop = CropSelection.Full;

    public required string SourcePath { get; init; }
    public double DurationSeconds { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public double FrameRate { get; init; }
    public bool HasAudio { get; init; }

    public string FileName => Path.GetFileName(SourcePath);
    public string ResolutionDisplay => Width > 0 && Height > 0 ? $"{Width} × {Height}" : "Unknown";
    public string DurationDisplay => TimeSpan.FromSeconds(Math.Max(0, EffectiveDurationSeconds)).ToString(@"hh\:mm\:ss\.fff");

    public double TrimStartSeconds
    {
        get => _trimStartSeconds;
        set
        {
            var maximum = Math.Max(0, TrimEndSeconds - 0.001);
            if (SetField(ref _trimStartSeconds, Math.Clamp(value, 0, maximum)))
            {
                OnPropertyChanged(nameof(EffectiveDurationSeconds));
                OnPropertyChanged(nameof(DurationDisplay));
            }
        }
    }

    public double TrimEndSeconds
    {
        get => _trimEndSeconds <= 0 ? DurationSeconds : _trimEndSeconds;
        set
        {
            var clamped = Math.Clamp(value, Math.Min(DurationSeconds, TrimStartSeconds + 0.001), DurationSeconds);
            if (SetField(ref _trimEndSeconds, clamped))
            {
                OnPropertyChanged(nameof(EffectiveDurationSeconds));
                OnPropertyChanged(nameof(DurationDisplay));
            }
        }
    }

    public double EffectiveDurationSeconds => Math.Max(0.001, TrimEndSeconds - TrimStartSeconds);

    public void SetTrimRange(double startSeconds, double endSeconds)
    {
        var duration = double.IsFinite(DurationSeconds) ? Math.Max(0.001, DurationSeconds) : 0.001;
        if (!double.IsFinite(startSeconds)) startSeconds = 0;
        if (!double.IsFinite(endSeconds)) endSeconds = duration;
        var start = Math.Clamp(startSeconds, 0, Math.Max(0, duration - 0.001));
        var end = Math.Clamp(endSeconds, start + 0.001, duration);
        var startChanged = Math.Abs(_trimStartSeconds - start) > double.Epsilon;
        var endChanged = Math.Abs(TrimEndSeconds - end) > double.Epsilon;

        if (!startChanged && !endChanged) return;
        _trimStartSeconds = start;
        _trimEndSeconds = end;
        if (startChanged) OnPropertyChanged(nameof(TrimStartSeconds));
        if (endChanged) OnPropertyChanged(nameof(TrimEndSeconds));
        OnPropertyChanged(nameof(EffectiveDurationSeconds));
        OnPropertyChanged(nameof(DurationDisplay));
    }

    public CropSelection Crop
    {
        get => _crop;
        set => SetField(ref _crop, value.Clamp());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(string? propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class MediaEditPlan
{
    public string ResizeMode { get; set; } = "Fit";
    public int TargetWidth { get; set; } = 1920;
    public int TargetHeight { get; set; } = 1080;
    public string AspectRatio { get; set; } = "16:9";
    public double CustomAspectWidth { get; set; } = 16;
    public double CustomAspectHeight { get; set; } = 9;
    public bool LockSelectionAspect { get; set; } = true;
    public CropSelection ImageCrop { get; set; } = CropSelection.Full;
    public ObservableCollection<MediaClipEdit> Clips { get; } = [];

    public bool HasCrop => !ImageCrop.IsFullFrame || Clips.Any(clip => !clip.Crop.IsFullFrame);
    public bool HasTimelineEdits => Clips.Any(clip => clip.TrimStartSeconds > 0.0005 || clip.TrimEndSeconds < clip.DurationSeconds - 0.0005);

    public string Summary(MediaKind kind)
    {
        var actions = new List<string>();
        if (HasCrop) actions.Add("crop");
        if (kind == MediaKind.Video && Clips.Count > 1) actions.Add($"stitch {Clips.Count} clips");
        if (kind == MediaKind.Video && HasTimelineEdits) actions.Add("trim");
        actions.Add($"{ResizeMode.ToLowerInvariant()} {TargetWidth}×{TargetHeight}");
        return string.Join(", ", actions);
    }
}
