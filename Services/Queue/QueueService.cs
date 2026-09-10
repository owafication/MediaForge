using System.IO;
using System.Reflection;
using System.Text.Json;
using MediaForge.Models;
using MediaForge.Models.Projects;
using MediaForge.Models.Queue;
using MediaForge.Services.Projects;

namespace MediaForge.Services.Queue;

public sealed class QueueService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public QueueDocument Capture(IReadOnlyList<MediaJob> jobs, string? queuePath = null, QueueDocument? basis = null)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ProjectDocument? projectBasis = null;
        if (basis is not null)
        {
            projectBasis = new ProjectDocument
            {
                ProjectId = basis.QueueId,
                CreatedUtc = basis.CreatedUtc,
                ModifiedUtc = basis.ModifiedUtc,
                Queue = basis.Items,
                ProjectDefaults = new AppSettings(),
                OutputRules = new ProjectOutputRulesDocument()
            };
        }
        var project = ProjectDocumentMapper.Capture(
            jobs,
            new AppSettings(),
            queuePath,
            basis?.QueueId ?? Guid.NewGuid(),
            basis?.CreatedUtc ?? DateTimeOffset.UtcNow,
            portable: false,
            basis: projectBasis);
        return new QueueDocument
        {
            SchemaVersion = QueueDocument.CurrentSchemaVersion,
            ApplicationVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.1.0",
            QueueId = basis?.QueueId ?? project.ProjectId,
            CreatedUtc = basis?.CreatedUtc ?? project.CreatedUtc,
            ModifiedUtc = DateTimeOffset.UtcNow,
            Items = project.Queue,
            Extensions = basis?.Extensions
        };
    }

    public void Save(QueueDocument document, string queuePath)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(queuePath);
        Validate(document);
        document.ModifiedUtc = DateTimeOffset.UtcNow;
        var path = EnsureExtension(queuePath);
        var json = JsonSerializer.Serialize(document, _jsonOptions) + Environment.NewLine;
        AtomicFile.WriteUtf8(path, json, keepBackup: true);
    }

    public QueueLoadResult Load(string queuePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queuePath);
        var path = Path.GetFullPath(queuePath);
        if (!File.Exists(path)) throw new FileNotFoundException("The MediaForge queue file was not found.", path);
        QueueDocument document;
        try
        {
            document = JsonSerializer.Deserialize<QueueDocument>(File.ReadAllText(path), _jsonOptions)
                ?? throw new InvalidDataException("The queue document is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"The queue JSON is invalid: {ex.Message}", ex);
        }
        Validate(document);
        var readOnly = document.SchemaVersion > QueueDocument.CurrentSchemaVersion;
        var reason = readOnly
            ? $"This queue uses schema {document.SchemaVersion}, but this build supports schema {QueueDocument.CurrentSchemaVersion}."
            : null;
        var project = new ProjectDocument
        {
            ProjectId = document.QueueId,
            CreatedUtc = document.CreatedUtc,
            ModifiedUtc = document.ModifiedUtc,
            Queue = document.Items,
            ProjectDefaults = new AppSettings(),
            OutputRules = new ProjectOutputRulesDocument()
        };
        var materialized = ProjectDocumentMapper.Materialize(project, path);
        return new QueueLoadResult(document, materialized.Jobs, materialized.Issues, readOnly, reason, path);
    }

    private static void Validate(QueueDocument document)
    {
        if (document.SchemaVersion < 1) throw new InvalidDataException("Queue schemaVersion must be at least 1.");
        if (document.QueueId == Guid.Empty) throw new InvalidDataException("Queue id must not be empty.");
        if (document.Items is null) throw new InvalidDataException("Queue items are required.");
        var duplicate = document.Items.GroupBy(item => item.ItemId).FirstOrDefault(group => group.Key == Guid.Empty || group.Count() > 1);
        if (duplicate is not null) throw new InvalidDataException("Queue item IDs must be unique and non-empty.");
        if (document.Items.Any(item => item.Source is null || item.Source.ReferenceId == Guid.Empty || string.IsNullOrWhiteSpace(item.Source.FileName)))
            throw new InvalidDataException("Every queue item requires a valid source reference.");
    }

    private static string EnsureExtension(string path) =>
        string.Equals(Path.GetExtension(path), ".mediaforge-queue", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(path + ".mediaforge-queue");
}
