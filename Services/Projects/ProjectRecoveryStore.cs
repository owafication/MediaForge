using System.Text.Json;
using System.IO;
using MediaForge.Models.Projects;
using MediaForge.Services.Runtime;

namespace MediaForge.Services.Projects;

public sealed class ProjectRecoveryStore
{
    private const int MaximumSnapshotsPerProject = 5;
    private static readonly TimeSpan MaximumSnapshotAge = TimeSpan.FromDays(14);
    private readonly string? _recoveryDirectory;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public ProjectRecoveryStore(string? recoveryDirectory = null)
    {
        try
        {
            _recoveryDirectory = recoveryDirectory ?? ApplicationDataPaths.GetLocalProductPath("Recovery");
            Directory.CreateDirectory(_recoveryDirectory);
        }
        catch
        {
            _recoveryDirectory = null;
        }
    }

    public string? RecoveryDirectory => _recoveryDirectory;

    public RecoverySnapshotInfo Save(ProjectDocument document, string? canonicalProjectPath)
    {
        ArgumentNullException.ThrowIfNull(document);
        var directory = _recoveryDirectory ??
            throw new InvalidOperationException("The MediaForge recovery folder is unavailable.");
        var now = DateTimeOffset.UtcNow;
        var snapshot = new RecoverySnapshot
        {
            ProjectId = document.ProjectId,
            CreatedUtc = now,
            CanonicalProjectPath = string.IsNullOrWhiteSpace(canonicalProjectPath)
                ? null
                : Path.GetFullPath(canonicalProjectPath),
            Document = document
        };
        var path = Path.Combine(directory, $"{document.ProjectId:N}-{now:yyyyMMddTHHmmssfffffffZ}.mediaforge-recovery");
        var json = JsonSerializer.Serialize(snapshot, _jsonOptions) + Environment.NewLine;
        AtomicFile.WriteUtf8(path, json, keepBackup: false);
        try { Prune(document.ProjectId); } catch { }
        return new RecoverySnapshotInfo(path, snapshot);
    }

    public IReadOnlyList<RecoverySnapshotInfo> GetRecoverableSnapshots()
    {
        if (_recoveryDirectory is null || !Directory.Exists(_recoveryDirectory)) return [];
        try
        {
            var results = new List<RecoverySnapshotInfo>();
            foreach (var path in Directory.EnumerateFiles(_recoveryDirectory, "*.mediaforge-recovery", SearchOption.TopDirectoryOnly))
            {
                var candidate = TryRead(path);
                if (candidate is null) continue;
                if (candidate.Snapshot.CreatedUtc < DateTimeOffset.UtcNow - MaximumSnapshotAge)
                {
                    TryDelete(path);
                    continue;
                }
                if (!IsNewerThanCanonical(candidate.Snapshot)) continue;
                results.Add(candidate);
            }
            return results.OrderByDescending(item => item.Snapshot.CreatedUtc).ToList();
        }
        catch
        {
            return [];
        }
    }

    public RecoverySnapshotInfo? TryRead(string snapshotPath)
    {
        try
        {
            var json = File.ReadAllText(snapshotPath);
            var snapshot = JsonSerializer.Deserialize<RecoverySnapshot>(json, _jsonOptions);
            if (snapshot is null || snapshot.SchemaVersion != 1 || snapshot.ProjectId == Guid.Empty || snapshot.Document is null ||
                snapshot.Document.ProjectId != snapshot.ProjectId || snapshot.Document.SchemaVersion < 1 ||
                snapshot.Document.Queue is null || snapshot.Document.ProjectDefaults is null || snapshot.Document.OutputRules is null)
            {
                return null;
            }
            return new RecoverySnapshotInfo(snapshotPath, snapshot);
        }
        catch
        {
            return null;
        }
    }

    public void Discard(string snapshotPath) => TryDelete(snapshotPath);

    public void PruneAfterManualSave(Guid projectId, DateTimeOffset savedUtc)
    {
        try
        {
            if (_recoveryDirectory is null || !Directory.Exists(_recoveryDirectory)) return;
            foreach (var path in Directory.EnumerateFiles(_recoveryDirectory, $"{projectId:N}-*.mediaforge-recovery"))
            {
                var candidate = TryRead(path);
                if (candidate is not null && candidate.Snapshot.CreatedUtc <= savedUtc) TryDelete(path);
            }
            Prune(projectId);
        }
        catch
        {
            // Recovery retention is auxiliary and must not turn a successful canonical save into a failure.
        }
    }

    private void Prune(Guid projectId)
    {
        try
        {
            if (_recoveryDirectory is null || !Directory.Exists(_recoveryDirectory)) return;
            var candidates = Directory.EnumerateFiles(_recoveryDirectory, $"{projectId:N}-*.mediaforge-recovery")
                .Select(TryRead)
                .Where(item => item is not null)
                .Cast<RecoverySnapshotInfo>()
                .OrderByDescending(item => item.Snapshot.CreatedUtc)
                .ToList();
            foreach (var old in candidates.Skip(MaximumSnapshotsPerProject)) TryDelete(old.SnapshotPath);
            foreach (var expired in candidates.Where(item => item.Snapshot.CreatedUtc < DateTimeOffset.UtcNow - MaximumSnapshotAge))
            {
                TryDelete(expired.SnapshotPath);
            }
        }
        catch
        {
            // Recovery retention is best-effort and must not block autosave or manual save.
        }
    }

    private static bool IsNewerThanCanonical(RecoverySnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.CanonicalProjectPath) || !File.Exists(snapshot.CanonicalProjectPath)) return true;
        try
        {
            return snapshot.CreatedUtc.UtcDateTime > File.GetLastWriteTimeUtc(snapshot.CanonicalProjectPath);
        }
        catch
        {
            return true;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Retention cleanup is best-effort and must not block recovery reads.
        }
    }
}
