using MediaForge.Models;
using MediaForge.Services;

namespace MediaForge.Services.Queue;

public sealed class QueueCoordinator : IQueueCoordinator
{
    private readonly IMediaConversionService _conversionService;
    private readonly object _stateLock = new();
    private CancellationTokenSource? _runCancellation;
    private TaskCompletionSource<bool> _resumeSignal = CompletedSignal();

    public QueueCoordinator(IMediaConversionService conversionService)
    {
        _conversionService = conversionService ?? throw new ArgumentNullException(nameof(conversionService));
    }

    public bool IsRunning { get; private set; }
    public bool IsDispatchPaused { get; private set; }

    public event EventHandler? StateChanged;

    public Task<QueueRunResult> RunAsync(
        IReadOnlyList<MediaJob> jobs,
        ConversionOptions options,
        int parallelJobs,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(options);
        return RunAsync(jobs.Select(job => new QueueRunItem(job, options)).ToList(), parallelJobs, log, cancellationToken);
    }

    public async Task<QueueRunResult> RunAsync(
        IReadOnlyList<QueueRunItem> items,
        int parallelJobs,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0) return new QueueRunResult(false);
        if (items.Any(item => item.Job is null || item.Snapshot is null || item.Options is null))
            throw new ArgumentException("Queue run items require live state plus immutable source, processing and option snapshots.", nameof(items));

        CancellationTokenSource runCancellation;
        lock (_stateLock)
        {
            if (IsRunning) throw new InvalidOperationException("A queue run is already active.");
            IsRunning = true;
            IsDispatchPaused = false;
            _resumeSignal = CompletedSignal();
            runCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _runCancellation = runCancellation;
        }
        NotifyStateChanged();

        var ordered = items
            .Select((item, index) => (Item: item, Index: index))
            .Where(entry => entry.Item.Snapshot.Enabled)
            .OrderByDescending(entry => entry.Item.Snapshot.Priority)
            .ThenBy(entry => entry.Index)
            .Select(entry => entry.Item)
            .ToList();
        var nextIndex = 0;
        var queueLock = new object();

        try
        {
            var workerCount = Math.Min(Math.Clamp(parallelJobs, 1, 8), Math.Max(1, ordered.Count));
            var workers = Enumerable.Range(0, workerCount).Select(_ => WorkerAsync()).ToList();
            await Task.WhenAll(workers);
            return new QueueRunResult(runCancellation.IsCancellationRequested);
        }
        catch (OperationCanceledException) when (runCancellation.IsCancellationRequested)
        {
            foreach (var item in ordered.Where(item => item.Job.State is JobState.Pending or JobState.Ready or JobState.Paused or JobState.PauseRequested))
            {
                item.Job.Progress = 0;
                item.Job.State = JobState.Cancelled;
                item.Job.Message = "Cancelled before dispatch";
            }
            return new QueueRunResult(true);
        }
        finally
        {
            lock (_stateLock)
            {
                IsRunning = false;
                IsDispatchPaused = false;
                _runCancellation = null;
                _resumeSignal.TrySetResult(true);
            }
            runCancellation.Dispose();
            NotifyStateChanged();
        }

        async Task WorkerAsync()
        {
            while (true)
            {
                await WaitForDispatchAsync(runCancellation.Token);
                QueueRunItem item;
                lock (_stateLock)
                {
                    if (IsDispatchPaused) continue;
                    lock (queueLock)
                    {
                        if (nextIndex >= ordered.Count) return;
                        item = ordered[nextIndex++];
                    }
                }
                await ProcessJobAsync(item, log, runCancellation.Token);
            }
        }
    }

    public void PauseAfterCurrent()
    {
        lock (_stateLock)
        {
            if (!IsRunning || IsDispatchPaused) return;
            IsDispatchPaused = true;
            _resumeSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        NotifyStateChanged();
    }

    public void Resume()
    {
        TaskCompletionSource<bool> signal;
        lock (_stateLock)
        {
            if (!IsRunning || !IsDispatchPaused) return;
            IsDispatchPaused = false;
            signal = _resumeSignal;
            _resumeSignal = CompletedSignal();
        }
        signal.TrySetResult(true);
        NotifyStateChanged();
    }

    public void Cancel()
    {
        TaskCompletionSource<bool> signal;
        lock (_stateLock)
        {
            _runCancellation?.Cancel();
            signal = _resumeSignal;
        }
        signal.TrySetResult(true);
    }

    private async Task WaitForDispatchAsync(CancellationToken cancellationToken)
    {
        Task waitTask;
        lock (_stateLock) waitTask = _resumeSignal.Task;
        await waitTask.WaitAsync(cancellationToken);
    }

    private async Task ProcessJobAsync(
        QueueRunItem item,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        var job = item.Job;
        var snapshot = item.Snapshot;
        try
        {
            job.Reset();
            job.State = JobState.Running;
            job.Message = "Starting…";
            NotifyStateChanged();

            var progress = new Progress<double>(value =>
            {
                job.Progress = value;
                job.Message = $"Processing — {value:0}%";
                NotifyStateChanged();
            });

            var result = await _conversionService.ConvertAsync(
                snapshot,
                item.Options,
                progress,
                line => log?.Invoke($"[{job.FileName}] {line}"),
                cancellationToken);
            job.OutputPath = result.OutputPath;
            job.Message = result.Message;
            job.Progress = result.Skipped ? 0 : 100;
            job.State = result.Skipped ? JobState.Skipped : JobState.Completed;
        }
        catch (OperationCanceledException)
        {
            job.Progress = 0;
            job.State = JobState.Cancelled;
            job.Message = "Cancelled";
        }
        catch (Exception ex)
        {
            job.Progress = 0;
            job.State = JobState.Failed;
            job.Message = FirstUsefulLine(ex.Message);
            log?.Invoke($"[{job.FileName}] ERROR: {ex}");
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private static TaskCompletionSource<bool> CompletedSignal()
    {
        var signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.SetResult(true);
        return signal;
    }

    private static string FirstUsefulLine(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "Unknown error";
        return message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "Unknown error";
    }
}
