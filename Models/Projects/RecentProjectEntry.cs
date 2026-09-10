namespace MediaForge.Models.Projects;

public sealed class RecentProjectEntry
{
    public required string Path { get; set; }
    public Guid ProjectId { get; set; }
    public DateTimeOffset LastOpenedUtc { get; set; } = DateTimeOffset.UtcNow;
}
