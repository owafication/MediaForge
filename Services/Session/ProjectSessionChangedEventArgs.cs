namespace MediaForge.Services.Session;

public sealed class ProjectSessionChangedEventArgs : EventArgs
{
    public ProjectSessionChangedEventArgs(ProjectSessionChangeKind kind, long revision)
    {
        Kind = kind;
        Revision = revision;
    }

    public ProjectSessionChangeKind Kind { get; }
    public long Revision { get; }
}
