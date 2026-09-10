using MediaForge.Models;

namespace MediaForge.Services.Queue;

public interface IQueueCoordinator
{
    bool IsRunning { get; }
    bool IsDispatchPaused { get; }

    event EventHandler? StateChanged;

    Task<QueueRunResult> RunAsync(
        IReadOnlyList<MediaJob> jobs,
        ConversionOptions options,
        int parallelJobs,
        Action<string>? log,
        CancellationToken cancellationToken);

    Task<QueueRunResult> RunAsync(
        IReadOnlyList<QueueRunItem> items,
        int parallelJobs,
        Action<string>? log,
        CancellationToken cancellationToken);

    void PauseAfterCurrent();
    void Resume();
    void Cancel();
}
