namespace MediaForge.Services.Session;

public enum ProjectSessionChangeKind
{
    JobsAdded,
    JobsRemoved,
    Cleared,
    JobEdited,
    JobOutputChanged,
    ProjectOptionsChanged,
    QueueChanged,
    Relinked,
    Loaded,
    MarkedClean
}
