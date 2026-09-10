using System.Text.Json;
using System.IO;
using MediaForge.Models.Projects;

namespace MediaForge.Services.Projects;

public sealed class ProjectService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public ProjectLoadResult Load(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        var fullPath = Path.GetFullPath(projectPath);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("The MediaForge project file was not found.", fullPath);

        string json;
        try
        {
            json = File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not read project '{fullPath}': {ex.Message}", ex);
        }

        return LoadJson(json, fullPath);
    }

    public ProjectLoadResult LoadJson(string json, string? projectPath = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        int schemaVersion;
        try
        {
            using var probe = JsonDocument.Parse(json);
            if (!probe.RootElement.TryGetProperty("schemaVersion", out var schemaElement) ||
                !schemaElement.TryGetInt32(out schemaVersion) || schemaVersion < 1)
            {
                throw new InvalidDataException("The project does not declare a supported positive schemaVersion.");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"The project JSON is invalid: {ex.Message}", ex);
        }

        ProjectDocument document;
        try
        {
            document = JsonSerializer.Deserialize<ProjectDocument>(json, _jsonOptions)
                ?? throw new InvalidDataException("The project document is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"The project schema is invalid: {ex.Message}", ex);
        }

        ValidateMinimum(document);
        var isReadOnly = schemaVersion > ProjectDocument.CurrentSchemaVersion;
        var readOnlyReason = isReadOnly
            ? $"This project uses schema {schemaVersion}, but this MediaForge build supports schema {ProjectDocument.CurrentSchemaVersion}. It was opened read-only to prevent data loss."
            : null;
        var materialized = ProjectDocumentMapper.Materialize(document, projectPath);
        return new ProjectLoadResult(document, materialized.Jobs, materialized.Issues, isReadOnly, readOnlyReason, projectPath);
    }

    public ProjectLoadResult Materialize(ProjectDocument document, string? projectPath, bool isReadOnly = false, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ValidateMinimum(document);
        var materialized = ProjectDocumentMapper.Materialize(document, projectPath);
        return new ProjectLoadResult(document, materialized.Jobs, materialized.Issues, isReadOnly, reason, projectPath);
    }

    public void Save(ProjectDocument document, string projectPath)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        if (document.SchemaVersion != ProjectDocument.CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Schema {document.SchemaVersion} cannot be saved by this build. Supported schema: {ProjectDocument.CurrentSchemaVersion}.");
        }

        var fullPath = Path.GetFullPath(projectPath);
        if (!string.Equals(Path.GetExtension(fullPath), ".mediaforge", StringComparison.OrdinalIgnoreCase))
        {
            fullPath += ".mediaforge";
        }

        ValidateMinimum(document);
        document.ModifiedUtc = DateTimeOffset.UtcNow;
        var json = JsonSerializer.Serialize(document, _jsonOptions) + Environment.NewLine;
        try
        {
            AtomicFile.WriteUtf8(fullPath, json, keepBackup: true);
        }
        catch (Exception ex)
        {
            throw new IOException($"Project save failed while replacing '{fullPath}': {ex.Message}", ex);
        }
    }

    public string Serialize(ProjectDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ValidateMinimum(document);
        return JsonSerializer.Serialize(document, _jsonOptions);
    }

    private static void ValidateMinimum(ProjectDocument document)
    {
        if (document.SchemaVersion < 1) throw new InvalidDataException("schemaVersion must be at least 1.");
        if (document.ProjectId == Guid.Empty) throw new InvalidDataException("projectId must not be empty.");
        if (document.CreatedUtc == default) throw new InvalidDataException("createdUtc is required.");
        if (document.Queue is null) throw new InvalidDataException("queue is required.");
        if (document.ProjectDefaults is null) throw new InvalidDataException("projectDefaults is required.");
        if (document.OutputRules is null) throw new InvalidDataException("outputRules is required.");

        var duplicateItem = document.Queue
            .GroupBy(item => item.ItemId)
            .FirstOrDefault(group => group.Key == Guid.Empty || group.Count() > 1);
        if (duplicateItem is not null) throw new InvalidDataException("Queue item IDs must be unique and non-empty.");
        if (document.Queue.Any(item => item.Source is null || item.Source.ReferenceId == Guid.Empty || string.IsNullOrWhiteSpace(item.Source.FileName)))
        {
            throw new InvalidDataException("Every queue item requires a valid typed source reference.");
        }
    }
}
