using System.Collections.ObjectModel;
using MediaForge.Models;

namespace MediaForge.Services.Session;

public interface IProjectSession
{
    ReadOnlyObservableCollection<MediaJob> Jobs { get; }
    long Revision { get; }
    bool IsDirty { get; }

    event EventHandler<ProjectSessionChangedEventArgs>? Changed;

    int AddJobs(IEnumerable<MediaJob> jobs);
    int RemoveJobs(IEnumerable<MediaJob> jobs);
    int DuplicateJobs(IEnumerable<MediaJob> jobs);
    bool MoveJobs(IEnumerable<MediaJob> jobs, int direction);
    void SetEnabled(IEnumerable<MediaJob> jobs, bool enabled);
    void AdjustPriority(IEnumerable<MediaJob> jobs, int delta);
    void Clear();
    void ReplaceJobs(IEnumerable<MediaJob> jobs, bool markDirty = false);
    void MarkDirty(ProjectSessionChangeKind kind = ProjectSessionChangeKind.ProjectOptionsChanged);
    void MarkClean();
}
