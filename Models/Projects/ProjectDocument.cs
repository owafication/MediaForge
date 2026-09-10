using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaForge.Models.Projects;

public sealed class ProjectDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string ApplicationVersion { get; set; } = string.Empty;
    public Guid ProjectId { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? ProjectRoot { get; set; }
    public Guid? SelectedPresetId { get; set; }
    public string? SelectedPresetVersion { get; set; }
    public string? SelectedPresetName { get; set; }
    public ConversionOptionOverrides? SelectedPresetSnapshot { get; set; }
    public List<ProjectQueueItemDocument> Queue { get; set; } = [];
    public AppSettings ProjectDefaults { get; set; } = new();
    public ProjectOutputRulesDocument OutputRules { get; set; } = new();
    public List<ProjectPresetReferenceDocument> PresetReferences { get; set; } = [];
    public ProjectUiStateDocument UiState { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}

public sealed class ProjectQueueItemDocument
{
    public Guid ItemId { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = true;
    public int Order { get; set; }
    public int Priority { get; set; }
    public required ProjectSourceReferenceDocument Source { get; set; }
    public ProjectEditPlanDocument? EditPlan { get; set; }
    public Guid? SelectedPresetId { get; set; }
    public string? SelectedPresetVersion { get; set; }
    public string? SelectedPresetName { get; set; }
    public ConversionOptionOverrides? SelectedPresetSnapshot { get; set; }
    public ConversionOptionOverrides? PerJobOverrides { get; set; }
    public string? LastKnownOutputPath { get; set; }
    public JobState? LastKnownOutputState { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}

public sealed class ProjectSourceReferenceDocument
{
    public Guid ReferenceId { get; set; } = Guid.NewGuid();
    public required string FileName { get; set; }
    public string? RelativePath { get; set; }
    public string? AbsolutePath { get; set; }
    public string? RootFolderRelativePath { get; set; }
    public string? RootFolderAbsolutePath { get; set; }
    public MediaKind Kind { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset LastWriteUtc { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}

public sealed class ProjectEditPlanDocument
{
    public string ResizeMode { get; set; } = "Fit";
    public int TargetWidth { get; set; } = 1920;
    public int TargetHeight { get; set; } = 1080;
    public string AspectRatio { get; set; } = "16:9";
    public double CustomAspectWidth { get; set; } = 16;
    public double CustomAspectHeight { get; set; } = 9;
    public bool LockSelectionAspect { get; set; } = true;
    public ProjectCropSelectionDocument ImageCrop { get; set; } = ProjectCropSelectionDocument.Full;
    public List<ProjectClipEditDocument> Clips { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}

public sealed class ProjectClipEditDocument
{
    public required ProjectSourceReferenceDocument Source { get; set; }
    public double DurationSeconds { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double FrameRate { get; set; }
    public bool HasAudio { get; set; }
    public double TrimStartSeconds { get; set; }
    public double TrimEndSeconds { get; set; }
    public ProjectCropSelectionDocument Crop { get; set; } = ProjectCropSelectionDocument.Full;

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}

public sealed record ProjectCropSelectionDocument(double X, double Y, double Width, double Height)
{
    public static ProjectCropSelectionDocument Full { get; } = new(0, 0, 1, 1);
}

public sealed class ProjectOutputRulesDocument
{
    public bool OutputBesideSource { get; set; }
    public string OutputFolder { get; set; } = string.Empty;
    public bool PreserveFolderTree { get; set; } = true;
    public bool PreserveTimestamps { get; set; } = true;
    public bool StripMetadata { get; set; }
    public string FileSuffix { get; set; } = "_converted";
    public string CollisionPolicy { get; set; } = "Rename";

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}

public sealed class ProjectPresetReferenceDocument
{
    public Guid PresetId { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? Name { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}

public sealed class ProjectUiStateDocument
{
    public int SelectedTabIndex { get; set; }
    public Guid? SelectedItemId { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}
