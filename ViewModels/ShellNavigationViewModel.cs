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
    Loading,
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
    private bool _isLoading;
    private bool _reviewReady;
    private string? _decisionMessage;

    public ShellNavigationViewModel(ReadOnlyObservableCollection<MediaJob> jobs)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));

        if (jobs is INotifyCollectionChanged notifyCollection)
        {
            notifyCollection.CollectionChanged += (_, _) =>
            {
                _reviewReady = false;
                _decisionMessage = null;
                RefreshMediaState();
            };
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
                _reviewReady = false;
                _decisionMessage = null;
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
                OnPropertyChanged(nameof(StateTitle));
                OnPropertyChanged(nameof(StateDetail));
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
        WorkflowState.Loading => "Loading media",
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

    public string StateTitle => State switch
    {
        WorkflowState.Empty => "Add media",
        WorkflowState.Loading => "Loading media",
        WorkflowState.MediaReady => "Choose a task",
        WorkflowState.Configuring => "Configure",
        WorkflowState.NeedsDecision => "Decision needed",
        WorkflowState.ReviewReady => "Review",
        WorkflowState.Running => "Processing",
        WorkflowState.Paused => "Processing paused",
        WorkflowState.Cancelling => "Cancelling",
        WorkflowState.Completed => "Completed",
        WorkflowState.CompletedWithWarning => "Completed with warnings",
        WorkflowState.Failed => "Processing failed",
        WorkflowState.VerificationFailed => "Verification failed",
        _ => State.ToString()
    };

    public string StateDetail => State switch
    {
        WorkflowState.Empty =>
            "Add one or more supported media files. You can work without creating a project.",
        WorkflowState.Loading =>
            "MediaForge is adding supported media and updating the current task state.",
        WorkflowState.MediaReady =>
            "Your media is ready. Choose Convert, Resize, Crop & Resize, Trim / Split, or Combine.",
        WorkflowState.Configuring =>
            "Use the normal task controls first. Advanced technical controls stay separate from the normal workflow.",
        WorkflowState.NeedsDecision =>
            ResolveDecisionMessage(),
        WorkflowState.ReviewReady =>
            "The shared workflow is ready to present task review details before processing.",
        WorkflowState.Running =>
            "Processing is active. The existing queue/runtime services remain the execution authority.",
        WorkflowState.Paused =>
            "No new queue work will start until processing is resumed.",
        WorkflowState.Cancelling =>
            "MediaForge is cancelling owned processing work and waiting for cleanup.",
        WorkflowState.Completed =>
            "Enabled media reached a completed state.",
        WorkflowState.CompletedWithWarning =>
            "Processing finished, but one or more items completed with warnings or were skipped.",
        WorkflowState.Failed =>
            FirstProblemMessage("Processing failed. Review the affected item before retrying."),
        WorkflowState.VerificationFailed =>
            FirstProblemMessage("Output verification failed. Treat the affected result as unverified."),
        _ =>
            string.Empty
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

        if (isRunning)
        {
            _isLoading = false;
            _reviewReady = false;
        }

        RefreshMediaState();
    }

    public void SetRunning(bool running) =>
        SetRuntimeState(running, isPaused: false, isCancelling: false);

    public void SetLoading(bool loading)
    {
        _isLoading = loading;
        RefreshMediaState();
    }

    public void RequireDecision(string message)
    {
        _decisionMessage = string.IsNullOrWhiteSpace(message)
            ? "Review the current task choices before continuing."
            : message.Trim();
        RefreshMediaState();
    }

    public void ClearDecision()
    {
        _decisionMessage = null;
        RefreshMediaState();
    }

    public void SetReviewReady(bool reviewReady)
    {
        _reviewReady = reviewReady &&
                       SelectedTask.HasValue &&
                       _jobs.Count > 0 &&
                       string.IsNullOrWhiteSpace(_decisionMessage);
        RefreshMediaState();
    }

    public void RefreshMediaState()
    {
        OnPropertyChanged(nameof(MediaSummary));
        OnPropertyChanged(nameof(StateDetail));

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

        if (_isLoading)
        {
            State = WorkflowState.Loading;
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

        if (!string.IsNullOrWhiteSpace(_decisionMessage))
        {
            State = WorkflowState.NeedsDecision;
            return;
        }

        if (SelectedTask == WorkflowTaskKind.Combine && _jobs.Count < 2)
        {
            State = WorkflowState.NeedsDecision;
            return;
        }

        if (_reviewReady)
        {
            State = WorkflowState.ReviewReady;
            return;
        }

        State = SelectedTask.HasValue
            ? WorkflowState.Configuring
            : WorkflowState.MediaReady;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private string ResolveDecisionMessage()
    {
        if (!string.IsNullOrWhiteSpace(_decisionMessage))
        {
            return _decisionMessage;
        }

        if (SelectedTask == WorkflowTaskKind.Combine && _jobs.Count < 2)
        {
            return "Combine needs at least two media items. Add another item or choose a different task.";
        }

        return "Review the current task choices before continuing.";
    }

    private string FirstProblemMessage(string fallback)
    {
        var problem = _jobs.FirstOrDefault(job =>
            job.Enabled &&
            job.State is JobState.Failed or JobState.VerificationFailed);

        return problem is null || string.IsNullOrWhiteSpace(problem.Message)
            ? fallback
            : $"{problem.FileName}: {problem.Message}";
    }

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
