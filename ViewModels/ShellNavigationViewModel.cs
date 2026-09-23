using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MediaForge.Models;

namespace MediaForge.ViewModels;

public enum ShellSurface
{
    Home,
    Task,
    Advanced
}

public enum WorkflowTaskKind
{
    Convert,
    Resize,
    CropResize,
    TrimSplit,
    Combine
}

public enum WorkflowState
{
    Empty,
    MediaReady,
    Configuring,
    NeedsDecision,
    ReviewReady,
    Running,
    Paused,
    Cancelling,
    Completed,
    CompletedWithWarning,
    Failed,
    VerificationFailed
}

public sealed class ShellNavigationViewModel : INotifyPropertyChanged
{
    private readonly ReadOnlyObservableCollection<MediaJob> _jobs;
    private ShellSurface _surface = ShellSurface.Home;
    private WorkflowTaskKind? _selectedTask;
    private WorkflowState _state = WorkflowState.Empty;
    private bool _isRunning;
    private bool _isPaused;
    private bool _isCancelling;

    public ShellNavigationViewModel(ReadOnlyObservableCollection<MediaJob> jobs)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        if (jobs is INotifyCollectionChanged notifyCollection)
        {
            notifyCollection.CollectionChanged += (_, _) => RefreshMediaState();
        }

        RefreshMediaState();
    }

    public ShellSurface Surface
    {
        get => _surface;
        private set
        {
            if (SetField(ref _surface, value))
            {
                OnPropertyChanged(nameof(IsHome));
                OnPropertyChanged(nameof(IsTaskWorkspace));
                OnPropertyChanged(nameof(IsAdvancedWorkspace));
            }
        }
    }

    public WorkflowTaskKind? SelectedTask
    {
        get => _selectedTask;
        private set
        {
            if (SetField(ref _selectedTask, value))
            {
                OnPropertyChanged(nameof(TaskTitle));
                OnPropertyChanged(nameof(TaskGuidance));
                RefreshMediaState();
            }
        }
    }

    public WorkflowState State
    {
        get => _state;
        private set
        {
            if (SetField(ref _state, value))
            {
                OnPropertyChanged(nameof(StateDisplay));
            }
        }
    }

    public bool IsHome => Surface == ShellSurface.Home;
    public bool IsTaskWorkspace => Surface == ShellSurface.Task;
    public bool IsAdvancedWorkspace => Surface == ShellSurface.Advanced;

    public string TaskTitle => SelectedTask switch
    {
        WorkflowTaskKind.Convert => "Convert",
        WorkflowTaskKind.Resize => "Resize",
        WorkflowTaskKind.CropResize => "Crop & Resize",
        WorkflowTaskKind.TrimSplit => "Trim / Split",
        WorkflowTaskKind.Combine => "Combine",
        _ => "Choose a task"
    };

    public string TaskGuidance => SelectedTask switch
    {
        WorkflowTaskKind.Convert =>
            "Change media format using a task-focused workflow. Normal choices stay simple; technical options remain in Advanced.",
        WorkflowTaskKind.Resize =>
            "Choose the target size and how the source should fit. Codec knowledge is not required for the normal Resize workflow.",
        WorkflowTaskKind.CropResize =>
            "Choose the visible crop first, then decide how that crop fits the target frame. Crop and resize decisions stay explicit.",
        WorkflowTaskKind.TrimSplit =>
            "Choose the time range to keep or the split points to create. Review the planned segments before processing.",
        WorkflowTaskKind.Combine =>
            "Choose media, arrange the order, then review compatibility and output before processing.",
        _ =>
            "Add media, then choose the result you want. MediaForge keeps technical controls out of the way until they are useful."
    };

    public string MediaSummary => _jobs.Count switch
    {
        0 => "No media selected",
        1 => $"1 media item selected — {_jobs[0].FileName}",
        _ => $"{_jobs.Count} media items selected"
    };

    public string StateDisplay => State switch
    {
        WorkflowState.Empty => "Add media to continue",
        WorkflowState.MediaReady => "Media ready — choose a task",
        WorkflowState.Configuring => "Configure this task",
        WorkflowState.NeedsDecision => "A decision is required",
        WorkflowState.ReviewReady => "Ready for review",
        WorkflowState.Running => "Processing",
        WorkflowState.Paused => "Paused",
        WorkflowState.Cancelling => "Cancelling",
        WorkflowState.Completed => "Completed",
        WorkflowState.CompletedWithWarning => "Completed with warning",
        WorkflowState.Failed => "Failed",
        WorkflowState.VerificationFailed => "Verification failed",
        _ => State.ToString()
    };

    public void ShowHome() => Surface = ShellSurface.Home;

    public void SelectTask(WorkflowTaskKind task)
    {
        SelectedTask = task;
        Surface = ShellSurface.Task;
        RefreshMediaState();
    }

    public void ShowTaskWorkspace()
    {
        Surface = SelectedTask.HasValue
            ? ShellSurface.Task
            : ShellSurface.Home;
    }

    public void ShowAdvanced() => Surface = ShellSurface.Advanced;

    public void SetRuntimeState(bool isRunning, bool isPaused, bool isCancelling)
    {
        _isRunning = isRunning;
        _isPaused = isRunning && isPaused;
        _isCancelling = isRunning && isCancelling;
        RefreshMediaState();
    }

    public void SetRunning(bool running) =>
        SetRuntimeState(running, isPaused: false, isCancelling: false);

    public void RefreshMediaState()
    {
        OnPropertyChanged(nameof(MediaSummary));

        if (_isCancelling)
        {
            State = WorkflowState.Cancelling;
            return;
        }

        if (_isRunning)
        {
            State = _isPaused
                ? WorkflowState.Paused
                : WorkflowState.Running;
            return;
        }

        if (_jobs.Count == 0)
        {
            State = WorkflowState.Empty;
            return;
        }

        var enabledJobs = _jobs.Where(job => job.Enabled).ToList();

        if (enabledJobs.Any(job => job.State == JobState.VerificationFailed))
        {
            State = WorkflowState.VerificationFailed;
            return;
        }

        if (enabledJobs.Any(job => job.State == JobState.Failed))
        {
            State = WorkflowState.Failed;
            return;
        }

        if (enabledJobs.Any(job => job.State is JobState.Paused or JobState.PauseRequested))
        {
            State = WorkflowState.Paused;
            return;
        }

        if (enabledJobs.Count > 0 &&
            enabledJobs.All(job => job.State is JobState.Completed or JobState.CompletedWithWarnings or JobState.Skipped))
        {
            State = enabledJobs.Any(job => job.State is JobState.CompletedWithWarnings or JobState.Skipped)
                ? WorkflowState.CompletedWithWarning
                : WorkflowState.Completed;
            return;
        }

        State = SelectedTask.HasValue
            ? WorkflowState.Configuring
            : WorkflowState.MediaReady;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
