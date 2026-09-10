using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace MediaForge.Models;

public enum MediaKind
{
    Image,
    Video,
    Audio,
    Unsupported
}

public enum JobState
{
    Pending,
    Ready,
    Running,
    PauseRequested,
    Paused,
    Completed,
    CompletedWithWarnings,
    VerificationFailed,
    Failed,
    Cancelled,
    Skipped,
    Disabled
}

public sealed class MediaJob : INotifyPropertyChanged
{
    private JobState _state = JobState.Pending;
    private double _progress;
    private string _outputPath = string.Empty;
    private string _message = string.Empty;
    private MediaEditPlan? _editPlan;
    private bool _enabled = true;
    private int _priority;
    private Guid? _selectedPresetId;
    private string? _selectedPresetVersion;
    private string _selectedPresetName = "Global settings";
    private ConversionOptionOverrides? _selectedPresetSnapshot;
    private ConversionOptionOverrides? _perJobOverrides;
    private string _effectiveOptionsSummary = "Global settings";
    private string _estimateDisplay = "Estimate pending (unproven)";

    public Guid Id { get; init; } = Guid.NewGuid();
    public required string SourcePath { get; init; }
    public string? RootFolder { get; init; }
    public required MediaKind Kind { get; init; }
    public long SourceBytes { get; init; }
    public DateTimeOffset SourceLastWriteUtc { get; init; }

    public string FileName => Path.GetFileName(SourcePath);
    public string Folder => Path.GetDirectoryName(SourcePath) ?? string.Empty;
    public string KindDisplay => Kind.ToString();
    public string EditSummary => EditPlan is null ? string.Empty : EditPlan.Summary(Kind);
    public string SizeDisplay => FormatBytes(SourceBytes);
    public string EnabledDisplay => Enabled ? "Enabled" : "Disabled";
    public string PriorityDisplay => Priority.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public string OverrideSummary => PerJobOverrides is null || PerJobOverrides.IsEmpty
        ? "None"
        : $"{PerJobOverrides.GetDefinedKeys().Count} field(s)";

    public JobState State
    {
        get => _state;
        set => SetField(ref _state, value);
    }

    public string StateDisplay => !Enabled && State != JobState.Running ? "Disabled" : State.ToString();

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (SetField(ref _enabled, value))
            {
                OnPropertyChanged(nameof(EnabledDisplay));
                OnPropertyChanged(nameof(StateDisplay));
            }
        }
    }

    public int Priority
    {
        get => _priority;
        set
        {
            if (SetField(ref _priority, Math.Clamp(value, -100, 100)))
            {
                OnPropertyChanged(nameof(PriorityDisplay));
            }
        }
    }

    public Guid? SelectedPresetId
    {
        get => _selectedPresetId;
        set => SetField(ref _selectedPresetId, value);
    }

    public string? SelectedPresetVersion
    {
        get => _selectedPresetVersion;
        set => SetField(ref _selectedPresetVersion, value);
    }

    public string SelectedPresetName
    {
        get => _selectedPresetName;
        set => SetField(ref _selectedPresetName, string.IsNullOrWhiteSpace(value) ? "Global settings" : value);
    }

    public ConversionOptionOverrides? SelectedPresetSnapshot
    {
        get => _selectedPresetSnapshot;
        set => SetField(ref _selectedPresetSnapshot, value);
    }

    public ConversionOptionOverrides? PerJobOverrides
    {
        get => _perJobOverrides;
        set
        {
            if (SetField(ref _perJobOverrides, value))
            {
                OnPropertyChanged(nameof(OverrideSummary));
            }
        }
    }

    public string EffectiveOptionsSummary
    {
        get => _effectiveOptionsSummary;
        set => SetField(ref _effectiveOptionsSummary, value ?? string.Empty);
    }

    public string EstimateDisplay
    {
        get => _estimateDisplay;
        set => SetField(ref _estimateDisplay, value ?? "Estimate unavailable (unproven)");
    }

    public double Progress
    {
        get => _progress;
        set => SetField(ref _progress, Math.Clamp(value, 0, 100));
    }

    public string ProgressDisplay => $"{Progress:0}%";

    public string OutputPath
    {
        get => _outputPath;
        set => SetField(ref _outputPath, value);
    }

    public string Message
    {
        get => _message;
        set => SetField(ref _message, value);
    }

    public MediaEditPlan? EditPlan
    {
        get => _editPlan;
        set
        {
            if (SetField(ref _editPlan, value))
            {
                OnPropertyChanged(nameof(EditSummary));
            }
        }
    }

    public void Reset()
    {
        Progress = 0;
        OutputPath = string.Empty;
        Message = string.Empty;
        State = JobState.Pending;
    }

    public MediaJob Duplicate() => CloneForQueue(Guid.NewGuid());

    public MediaJob CreateRunSnapshot() => CloneForQueue(Id);

    private MediaJob CloneForQueue(Guid id) => new()
    {
        Id = id,
        SourcePath = SourcePath,
        RootFolder = RootFolder,
        Kind = Kind,
        SourceBytes = SourceBytes,
        SourceLastWriteUtc = SourceLastWriteUtc,
        Enabled = Enabled,
        Priority = Priority,
        SelectedPresetId = SelectedPresetId,
        SelectedPresetVersion = SelectedPresetVersion,
        SelectedPresetName = SelectedPresetName,
        SelectedPresetSnapshot = SelectedPresetSnapshot?.Clone(),
        PerJobOverrides = PerJobOverrides?.Clone(),
        EditPlan = CloneEditPlan(EditPlan),
        EffectiveOptionsSummary = EffectiveOptionsSummary,
        EstimateDisplay = EstimateDisplay
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);

        if (propertyName == nameof(State)) OnPropertyChanged(nameof(StateDisplay));
        else if (propertyName == nameof(Progress)) OnPropertyChanged(nameof(ProgressDisplay));
        return true;
    }

    private void OnPropertyChanged(string? propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static MediaEditPlan? CloneEditPlan(MediaEditPlan? source)
    {
        if (source is null) return null;
        var clone = new MediaEditPlan
        {
            ResizeMode = source.ResizeMode,
            TargetWidth = source.TargetWidth,
            TargetHeight = source.TargetHeight,
            AspectRatio = source.AspectRatio,
            CustomAspectWidth = source.CustomAspectWidth,
            CustomAspectHeight = source.CustomAspectHeight,
            LockSelectionAspect = source.LockSelectionAspect,
            ImageCrop = source.ImageCrop
        };
        foreach (var clip in source.Clips)
        {
            var clonedClip = new MediaClipEdit
            {
                SourcePath = clip.SourcePath,
                DurationSeconds = clip.DurationSeconds,
                Width = clip.Width,
                Height = clip.Height,
                FrameRate = clip.FrameRate,
                HasAudio = clip.HasAudio,
                Crop = clip.Crop
            };
            clonedClip.SetTrimRange(clip.TrimStartSeconds, clip.TrimEndSeconds);
            clone.Clips.Add(clonedClip);
        }
        return clone;
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.##} {units[unit]}";
    }
}
