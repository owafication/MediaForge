using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaForge.Models.Presets;

public sealed class PresetDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Version { get; set; } = "1.0";
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = "User";
    public string Description { get; set; } = string.Empty;
    public List<MediaKind> MediaKinds { get; set; } = [];
    public ConversionOptionOverrides Values { get; set; } = new();
    public List<string> LockedFields { get; set; } = [];
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Source { get; set; } = "User";
    public string? CompatibilityNotes { get; set; }

    [JsonIgnore]
    public bool IsBuiltIn { get; set; }

    [JsonIgnore]
    public string DisplayName => $"{Group} — {Name}";

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}

public sealed class PresetCatalogueState
{
    public Guid? DefaultPresetId { get; set; }
}
