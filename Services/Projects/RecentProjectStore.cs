using System.Text.Json;
using System.IO;
using MediaForge.Models.Projects;
using MediaForge.Services.Runtime;

namespace MediaForge.Services.Projects;

public sealed class RecentProjectStore
{
    private const int MaximumEntries = 10;
    private readonly string? _storePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public RecentProjectStore(string? storePath = null)
    {
        try
        {
            _storePath = storePath ?? ApplicationDataPaths.GetRoamingProductPath("recent-projects.json");
            var directory = Path.GetDirectoryName(_storePath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        }
        catch
        {
            _storePath = null;
        }
    }

    public IReadOnlyList<RecentProjectEntry> Load()
    {
        if (_storePath is null || !File.Exists(_storePath)) return [];
        try
        {
            var entries = JsonSerializer.Deserialize<List<RecentProjectEntry>>(File.ReadAllText(_storePath), _jsonOptions) ?? [];
            var filtered = entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Path))
                .Select(entry =>
                {
                    try { entry.Path = Path.GetFullPath(entry.Path); }
                    catch { entry.Path = string.Empty; }
                    return entry;
                })
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Path))
                .GroupBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(entry => entry.LastOpenedUtc).First())
                .OrderByDescending(entry => entry.LastOpenedUtc)
                .Take(MaximumEntries)
                .ToList();
            return filtered;
        }
        catch
        {
            return [];
        }
    }

    public void Add(string projectPath, Guid projectId)
    {
        var storePath = _storePath ?? throw new InvalidOperationException("The recent-project store is unavailable.");
        var fullPath = Path.GetFullPath(projectPath);
        var entries = Load()
            .Where(entry => !string.Equals(entry.Path, fullPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        entries.Insert(0, new RecentProjectEntry
        {
            Path = fullPath,
            ProjectId = projectId,
            LastOpenedUtc = DateTimeOffset.UtcNow
        });
        var json = JsonSerializer.Serialize(entries.Take(MaximumEntries).ToList(), _jsonOptions) + Environment.NewLine;
        AtomicFile.WriteUtf8(storePath, json, keepBackup: false);
    }

    public void Remove(string projectPath)
    {
        if (_storePath is null) return;
        var fullPath = Path.GetFullPath(projectPath);
        var entries = Load()
            .Where(entry => !string.Equals(entry.Path, fullPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var json = JsonSerializer.Serialize(entries, _jsonOptions) + Environment.NewLine;
        AtomicFile.WriteUtf8(_storePath, json, keepBackup: false);
    }
}
