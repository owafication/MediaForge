using System.Text.Json;
using System.Text.Json.Serialization;
using MediaForge.Models.Projects;

namespace MediaForge.Models.Queue;

public sealed class QueueDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string ApplicationVersion { get; set; } = string.Empty;
    public Guid QueueId { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<ProjectQueueItemDocument> Items { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}

public sealed record QueueLoadResult(
    QueueDocument Document,
    IReadOnlyList<MediaJob> Jobs,
    IReadOnlyList<ProjectSourceIssue> SourceIssues,
    bool IsReadOnly,
    string? ReadOnlyReason,
    string? QueuePath);
