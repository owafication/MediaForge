namespace MediaForge.Models.Projects;

public enum ProjectSourceIssueKind
{
    Missing,
    FingerprintChanged,
    UnsafeRelativePath
}

public sealed record ProjectSourceIssue(
    Guid ReferenceId,
    string FileName,
    string RecordedPath,
    ProjectSourceIssueKind Kind,
    string Detail);

public sealed record ProjectLoadResult(
    ProjectDocument Document,
    IReadOnlyList<MediaJob> Jobs,
    IReadOnlyList<ProjectSourceIssue> SourceIssues,
    bool IsReadOnly,
    string? ReadOnlyReason,
    string? ProjectPath);
