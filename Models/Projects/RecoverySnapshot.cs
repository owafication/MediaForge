using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace MediaForge.Models.Projects;

public sealed class RecoverySnapshot
{
    public int SchemaVersion { get; set; } = 1;
    public Guid ProjectId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CanonicalProjectPath { get; set; }
    public required ProjectDocument Document { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}

public sealed record RecoverySnapshotInfo(string SnapshotPath, RecoverySnapshot Snapshot)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Snapshot.CanonicalProjectPath)
        ? $"Unsaved project — {Snapshot.CreatedUtc.LocalDateTime:g}"
        : $"{Path.GetFileNameWithoutExtension(Snapshot.CanonicalProjectPath)} — {Snapshot.CreatedUtc.LocalDateTime:g}";
}
