using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using MediaForge.Models;

namespace MediaForge.Services.Session;

public sealed class ProjectSession : IProjectSession
{
    private readonly ObservableCollection<MediaJob> _jobs = [];
    private readonly Dictionary<string, int> _sourcePathCounts = new(StringComparer.OrdinalIgnoreCase);

    public ProjectSession()
    {
        Jobs = new ReadOnlyObservableCollection<MediaJob>(_jobs);
    }

    public ReadOnlyObservableCollection<MediaJob> Jobs { get; }
    public long Revision { get; private set; }
    public bool IsDirty { get; private set; }

    public event EventHandler<ProjectSessionChangedEventArgs>? Changed;

    public int AddJobs(IEnumerable<MediaJob> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        var added = AddJobsCore(jobs, allowDuplicateSources: false);
        if (added > 0) Touch(ProjectSessionChangeKind.JobsAdded);
        return added;
    }

    public int RemoveJobs(IEnumerable<MediaJob> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        var removed = 0;
        foreach (var job in jobs.Distinct().ToList())
        {
            if (!_jobs.Remove(job)) continue;
            job.PropertyChanged -= Job_PropertyChanged;
            DecrementSourcePath(Path.GetFullPath(job.SourcePath));
            removed++;
        }

        if (removed > 0) Touch(ProjectSessionChangeKind.JobsRemoved);
        return removed;
    }


    public int DuplicateJobs(IEnumerable<MediaJob> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        var selected = jobs.Distinct().Where(_jobs.Contains).OrderBy(job => _jobs.IndexOf(job)).ToList();
        var inserted = 0;
        foreach (var job in selected)
        {
            var duplicate = job.Duplicate();
            var index = _jobs.IndexOf(job) + 1;
            _jobs.Insert(Math.Min(index, _jobs.Count), duplicate);
            var sourcePath = Path.GetFullPath(duplicate.SourcePath);
            _sourcePathCounts[sourcePath] = _sourcePathCounts.GetValueOrDefault(sourcePath) + 1;
            duplicate.PropertyChanged += Job_PropertyChanged;
            inserted++;
        }
        if (inserted > 0) Touch(ProjectSessionChangeKind.QueueChanged);
        return inserted;
    }

    public bool MoveJobs(IEnumerable<MediaJob> jobs, int direction)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        if (direction is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(direction));
        var selected = jobs.Distinct().Where(_jobs.Contains).ToHashSet();
        if (selected.Count == 0) return false;
        var moved = false;
        if (direction < 0)
        {
            for (var index = 1; index < _jobs.Count; index++)
            {
                if (!selected.Contains(_jobs[index]) || selected.Contains(_jobs[index - 1])) continue;
                _jobs.Move(index, index - 1);
                moved = true;
            }
        }
        else
        {
            for (var index = _jobs.Count - 2; index >= 0; index--)
            {
                if (!selected.Contains(_jobs[index]) || selected.Contains(_jobs[index + 1])) continue;
                _jobs.Move(index, index + 1);
                moved = true;
            }
        }
        if (moved) Touch(ProjectSessionChangeKind.QueueChanged);
        return moved;
    }

    public void SetEnabled(IEnumerable<MediaJob> jobs, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        foreach (var job in jobs.Distinct().Where(_jobs.Contains)) job.Enabled = enabled;
    }

    public void AdjustPriority(IEnumerable<MediaJob> jobs, int delta)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        if (delta == 0) return;
        foreach (var job in jobs.Distinct().Where(_jobs.Contains)) job.Priority = Math.Clamp(job.Priority + delta, -100, 100);
    }

    public void Clear()
    {
        if (_jobs.Count == 0) return;
        ClearCore();
        Touch(ProjectSessionChangeKind.Cleared);
    }

    public void ReplaceJobs(IEnumerable<MediaJob> jobs, bool markDirty = false)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ClearCore();
        AddJobsCore(jobs, allowDuplicateSources: true);
        checked { Revision++; }
        IsDirty = markDirty;
        Changed?.Invoke(this, new ProjectSessionChangedEventArgs(
            markDirty ? ProjectSessionChangeKind.Relinked : ProjectSessionChangeKind.Loaded,
            Revision));
    }

    public void MarkDirty(ProjectSessionChangeKind kind = ProjectSessionChangeKind.ProjectOptionsChanged)
    {
        if (kind is ProjectSessionChangeKind.MarkedClean or ProjectSessionChangeKind.Loaded)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Use MarkClean or ReplaceJobs for this transition.");
        }
        Touch(kind);
    }

    public void MarkClean()
    {
        if (!IsDirty) return;
        IsDirty = false;
        Changed?.Invoke(this, new ProjectSessionChangedEventArgs(ProjectSessionChangeKind.MarkedClean, Revision));
    }

    private int AddJobsCore(IEnumerable<MediaJob> jobs, bool allowDuplicateSources)
    {
        var added = 0;
        foreach (var job in jobs)
        {
            ArgumentNullException.ThrowIfNull(job);
            var sourcePath = Path.GetFullPath(job.SourcePath);
            if (!allowDuplicateSources && _sourcePathCounts.ContainsKey(sourcePath)) continue;

            _sourcePathCounts[sourcePath] = _sourcePathCounts.GetValueOrDefault(sourcePath) + 1;
            _jobs.Add(job);
            job.PropertyChanged += Job_PropertyChanged;
            added++;
        }
        return added;
    }

    private void ClearCore()
    {
        foreach (var job in _jobs) job.PropertyChanged -= Job_PropertyChanged;
        _jobs.Clear();
        _sourcePathCounts.Clear();
    }

    private void DecrementSourcePath(string sourcePath)
    {
        if (!_sourcePathCounts.TryGetValue(sourcePath, out var count)) return;
        if (count <= 1) _sourcePathCounts.Remove(sourcePath);
        else _sourcePathCounts[sourcePath] = count - 1;
    }

    private void Job_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaJob.EditPlan))
        {
            Touch(ProjectSessionChangeKind.JobEdited);
            return;
        }
        if (e.PropertyName is nameof(MediaJob.Enabled) or nameof(MediaJob.Priority) or nameof(MediaJob.SelectedPresetId) or
            nameof(MediaJob.SelectedPresetVersion) or nameof(MediaJob.SelectedPresetSnapshot) or nameof(MediaJob.PerJobOverrides))
        {
            Touch(ProjectSessionChangeKind.QueueChanged);
            return;
        }
        if (e.PropertyName == nameof(MediaJob.OutputPath))
        {
            Touch(ProjectSessionChangeKind.JobOutputChanged);
            return;
        }
        if (e.PropertyName == nameof(MediaJob.State) && sender is MediaJob { State: JobState.Completed or JobState.CompletedWithWarnings or JobState.VerificationFailed or JobState.Failed or JobState.Cancelled or JobState.Skipped })
        {
            Touch(ProjectSessionChangeKind.JobOutputChanged);
        }
    }

    private void Touch(ProjectSessionChangeKind kind)
    {
        checked { Revision++; }
        IsDirty = true;
        Changed?.Invoke(this, new ProjectSessionChangedEventArgs(kind, Revision));
    }
}
