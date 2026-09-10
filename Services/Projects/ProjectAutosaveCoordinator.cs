using MediaForge.Models.Projects;

namespace MediaForge.Services.Projects;

public sealed class ProjectAutosaveCoordinator : IDisposable
{
    private readonly ProjectRecoveryStore _store;
    private readonly TimeSpan _debounce;
    private readonly object _gate = new();
    private CancellationTokenSource? _delayCancellation;
    private ProjectDocument? _pendingDocument;
    private string? _pendingCanonicalPath;
    private bool _disposed;

    public ProjectAutosaveCoordinator(ProjectRecoveryStore store, TimeSpan? debounce = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _debounce = debounce ?? TimeSpan.FromSeconds(10);
    }

    public event EventHandler<string>? SaveFailed;
    public event EventHandler<RecoverySnapshotInfo>? SnapshotSaved;

    public void Schedule(ProjectDocument document, string? canonicalProjectPath)
    {
        ArgumentNullException.ThrowIfNull(document);
        CancellationToken token;
        lock (_gate)
        {
            ThrowIfDisposed();
            _pendingDocument = document;
            _pendingCanonicalPath = canonicalProjectPath;
            _delayCancellation?.Cancel();
            _delayCancellation?.Dispose();
            _delayCancellation = new CancellationTokenSource();
            token = _delayCancellation.Token;
        }
        _ = DelayAndSaveAsync(token);
    }

    public async Task<RecoverySnapshotInfo?> FlushAsync()
    {
        ProjectDocument? document;
        string? path;
        lock (_gate)
        {
            if (_disposed) return null;
            _delayCancellation?.Cancel();
            _delayCancellation?.Dispose();
            _delayCancellation = null;
            document = _pendingDocument;
            path = _pendingCanonicalPath;
            _pendingDocument = null;
            _pendingCanonicalPath = null;
        }
        if (document is null) return null;
        return await SaveAsync(document, path).ConfigureAwait(false);
    }

    public void CancelPending()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _delayCancellation?.Cancel();
            _delayCancellation?.Dispose();
            _delayCancellation = null;
            _pendingDocument = null;
            _pendingCanonicalPath = null;
        }
    }

    private async Task DelayAndSaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_debounce, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        ProjectDocument? document;
        string? path;
        lock (_gate)
        {
            if (_disposed || cancellationToken.IsCancellationRequested) return;
            document = _pendingDocument;
            path = _pendingCanonicalPath;
            _pendingDocument = null;
            _pendingCanonicalPath = null;
            _delayCancellation?.Dispose();
            _delayCancellation = null;
        }
        if (document is not null) await SaveAsync(document, path).ConfigureAwait(false);
    }

    private Task<RecoverySnapshotInfo?> SaveAsync(ProjectDocument document, string? path)
    {
        return Task.Run<RecoverySnapshotInfo?>(() =>
        {
            try
            {
                var saved = _store.Save(document, path);
                SnapshotSaved?.Invoke(this, saved);
                return saved;
            }
            catch (Exception ex)
            {
                SaveFailed?.Invoke(this, ex.Message);
                return null;
            }
        });
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _delayCancellation?.Cancel();
            _delayCancellation?.Dispose();
            _delayCancellation = null;
            _pendingDocument = null;
            _pendingCanonicalPath = null;
        }
    }
}
