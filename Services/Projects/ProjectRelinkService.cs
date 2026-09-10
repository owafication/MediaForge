using System.IO;
using MediaForge.Models.Projects;

namespace MediaForge.Services.Projects;

public sealed record RelinkCandidate(string Path, long SizeBytes, DateTimeOffset LastWriteUtc, bool FingerprintMatches);

public sealed record RelinkPlanItem(
    ProjectSourceReferenceDocument Source,
    IReadOnlyList<RelinkCandidate> Candidates)
{
    public RelinkCandidate? UniqueExact => Candidates.Count(candidate => candidate.FingerprintMatches) == 1
        ? Candidates.Single(candidate => candidate.FingerprintMatches)
        : null;
    public RelinkCandidate? UniqueChanged => Candidates.Count == 1 && !Candidates[0].FingerprintMatches
        ? Candidates[0]
        : null;
    public bool IsAmbiguous => Candidates.Count > 1 && UniqueExact is null;
}

public sealed record RelinkPlan(IReadOnlyList<RelinkPlanItem> Items)
{
    public int ExactCount => Items.Count(item => item.UniqueExact is not null);
    public int ChangedCount => Items.Count(item => item.UniqueChanged is not null);
    public int AmbiguousCount => Items.Count(item => item.IsAmbiguous);
    public int UnresolvedCount => Items.Count(item => item.Candidates.Count == 0);
}

public sealed record RelinkApplyResult(int ExactApplied, int ChangedApplied, int Remaining);

public sealed class ProjectRelinkService
{
    public RelinkPlan Plan(ProjectDocument document, string searchRoot, string? projectPath = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchRoot);
        var canonicalRoot = Path.GetFullPath(searchRoot);
        if (!Directory.Exists(canonicalRoot)) throw new DirectoryNotFoundException(canonicalRoot);

        var references = EnumerateSourceReferences(document)
            .Where(reference => !ReferenceExists(reference, document, projectPath))
            .GroupBy(reference => reference.ReferenceId)
            .Select(group => group.First())
            .ToList();
        var names = references.Select(reference => reference.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidatesByName = new Dictionary<string, List<RelinkCandidate>>(StringComparer.OrdinalIgnoreCase);
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.System | FileAttributes.ReparsePoint
        };

        foreach (var path in Directory.EnumerateFiles(canonicalRoot, "*", options))
        {
            var name = Path.GetFileName(path);
            if (!names.Contains(name)) continue;
            try
            {
                var info = new FileInfo(path);
                if (!candidatesByName.TryGetValue(name, out var list))
                {
                    list = [];
                    candidatesByName.Add(name, list);
                }
                list.Add(new RelinkCandidate(
                    info.FullName,
                    info.Length,
                    new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
                    false));
            }
            catch
            {
                // Inaccessible candidates are ignored.
            }
        }

        var items = references.Select(reference =>
        {
            var candidates = candidatesByName.TryGetValue(reference.FileName, out var list)
                ? list.Select(candidate => candidate with
                {
                    FingerprintMatches = FingerprintMatches(reference, candidate.SizeBytes, candidate.LastWriteUtc)
                }).OrderBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase).ToList()
                : [];
            return new RelinkPlanItem(reference, candidates);
        }).ToList();
        return new RelinkPlan(items);
    }

    public RelinkApplyResult Apply(ProjectDocument document, RelinkPlan plan, bool applyExactMatches, bool includeChangedFingerprint)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(plan);
        var exactApplied = 0;
        var changedApplied = 0;
        foreach (var item in plan.Items)
        {
            var candidate = item.UniqueExact;
            if (applyExactMatches && candidate is not null)
            {
                ApplyCandidate(item.Source, candidate);
                exactApplied++;
                continue;
            }
            if (includeChangedFingerprint && item.UniqueChanged is not null)
            {
                ApplyCandidate(item.Source, item.UniqueChanged);
                changedApplied++;
            }
        }
        var remaining = plan.Items.Count - exactApplied - changedApplied;
        return new RelinkApplyResult(exactApplied, changedApplied, remaining);
    }

    private static IEnumerable<ProjectSourceReferenceDocument> EnumerateSourceReferences(ProjectDocument document)
    {
        foreach (var item in document.Queue)
        {
            yield return item.Source;
            if (item.EditPlan is null) continue;
            foreach (var clip in item.EditPlan.Clips) yield return clip.Source;
        }
    }

    private static bool ReferenceExists(ProjectSourceReferenceDocument reference, ProjectDocument document, string? projectPath)
    {
        var root = string.IsNullOrWhiteSpace(document.ProjectRoot)
            ? null
            : ProjectPathPolicy.ResolveProjectRoot(projectPath, document.ProjectRoot);
        if (root is not null && !string.IsNullOrWhiteSpace(reference.RelativePath) &&
            ProjectPathPolicy.TryResolveRelativeInsideRoot(root, reference.RelativePath, out var relativeCandidate) &&
            File.Exists(relativeCandidate))
        {
            return true;
        }
        if (!string.IsNullOrWhiteSpace(reference.AbsolutePath))
        {
            try { if (File.Exists(Path.GetFullPath(reference.AbsolutePath))) return true; }
            catch { }
        }
        return false;
    }

    private static bool FingerprintMatches(ProjectSourceReferenceDocument? reference, long size, DateTimeOffset lastWriteUtc)
    {
        if (reference is null) return false;
        var sizeMatches = reference.SizeBytes < 0 || reference.SizeBytes == size;
        var timeMatches = reference.LastWriteUtc == default || Math.Abs((reference.LastWriteUtc - lastWriteUtc).TotalSeconds) <= 1;
        return sizeMatches && timeMatches;
    }

    private static void ApplyCandidate(ProjectSourceReferenceDocument reference, RelinkCandidate candidate)
    {
        reference.AbsolutePath = Path.GetFullPath(candidate.Path);
        reference.RelativePath = null;
        reference.FileName = Path.GetFileName(candidate.Path);
        reference.SizeBytes = candidate.SizeBytes;
        reference.LastWriteUtc = candidate.LastWriteUtc;
    }
}
