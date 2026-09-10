using System.IO;
using System.Text.Json;
using MediaForge.Models;
using MediaForge.Models.Presets;
using MediaForge.Services.Projects;
using MediaForge.Services.Runtime;

namespace MediaForge.Services.Presets;

public sealed class PresetService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static IReadOnlySet<string> AllowedFieldKeys { get; } =
        ConversionOptionOverrides.FromSettings(new AppSettings()).GetDefinedKeys();

    private static readonly IReadOnlyList<PresetDocument> BuiltIns = CreateBuiltIns();
    private readonly string _catalogueDirectory;
    private readonly string _statePath;

    public PresetService(string? catalogueDirectory = null)
    {
        _catalogueDirectory = catalogueDirectory ?? ApplicationDataPaths.GetRoamingProductPath("Presets");
        _statePath = Path.Combine(_catalogueDirectory, "catalogue.json");
    }

    public IReadOnlyList<PresetDocument> LoadCatalogue()
    {
        var presets = BuiltIns.Select(Clone).ToList();
        try
        {
            Directory.CreateDirectory(_catalogueDirectory);
            foreach (var path in Directory.EnumerateFiles(_catalogueDirectory, "*.mediaforge-preset", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var preset = DeserializeAndValidate(File.ReadAllText(path));
                    preset.IsBuiltIn = false;
                    presets.RemoveAll(existing => existing.Id == preset.Id);
                    presets.Add(preset);
                }
                catch
                {
                    // A malformed catalogue entry is ignored rather than making the application unusable.
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Built-ins remain available when the user catalogue cannot be created or enumerated.
        }

        return presets
            .OrderBy(preset => preset.Group, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(preset => preset.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public PresetCatalogueState LoadState()
    {
        try
        {
            if (!File.Exists(_statePath)) return new PresetCatalogueState();
            return JsonSerializer.Deserialize<PresetCatalogueState>(File.ReadAllText(_statePath), JsonOptions)
                ?? new PresetCatalogueState();
        }
        catch
        {
            return new PresetCatalogueState();
        }
    }

    public void SetDefault(Guid? presetId)
    {
        if (presetId.HasValue && LoadCatalogue().All(preset => preset.Id != presetId.Value))
        {
            throw new InvalidOperationException("The selected preset is not in the catalogue.");
        }

        Directory.CreateDirectory(_catalogueDirectory);
        var json = JsonSerializer.Serialize(new PresetCatalogueState { DefaultPresetId = presetId }, JsonOptions) + Environment.NewLine;
        AtomicFile.WriteUtf8(_statePath, json, keepBackup: false);
    }

    public PresetDocument CreateFromSettings(
        AppSettings settings,
        string name,
        string group,
        string description,
        IEnumerable<string>? lockedFields = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var preset = new PresetDocument
        {
            Id = Guid.NewGuid(),
            Version = "1.0",
            Name = name.Trim(),
            Group = NormaliseGroup(group),
            Description = description.Trim(),
            MediaKinds = [MediaKind.Image, MediaKind.Video, MediaKind.Audio],
            Values = ConversionOptionOverrides.FromSettings(settings),
            LockedFields = NormaliseLockedFields(lockedFields),
            CreatedUtc = DateTimeOffset.UtcNow,
            ModifiedUtc = DateTimeOffset.UtcNow,
            Source = "User"
        };
        SaveUserPreset(preset);
        return Clone(preset);
    }

    public PresetDocument Duplicate(PresetDocument source, string name, string? group = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var duplicate = Clone(source);
        duplicate.Id = Guid.NewGuid();
        duplicate.Name = name.Trim();
        duplicate.Group = NormaliseGroup(group ?? source.Group);
        duplicate.Source = "User";
        duplicate.IsBuiltIn = false;
        duplicate.CreatedUtc = DateTimeOffset.UtcNow;
        duplicate.ModifiedUtc = duplicate.CreatedUtc;
        SaveUserPreset(duplicate);
        return Clone(duplicate);
    }

    public PresetDocument UpdateUserPreset(
        PresetDocument preset,
        string name,
        string group,
        string description,
        IEnumerable<string>? lockedFields)
    {
        ArgumentNullException.ThrowIfNull(preset);
        if (preset.IsBuiltIn) throw new InvalidOperationException("Built-in presets are read-only. Duplicate the preset before editing it.");
        var updated = Clone(preset);
        updated.Name = name.Trim();
        updated.Group = NormaliseGroup(group);
        updated.Description = description.Trim();
        updated.LockedFields = NormaliseLockedFields(lockedFields);
        updated.ModifiedUtc = DateTimeOffset.UtcNow;
        SaveUserPreset(updated);
        return Clone(updated);
    }

    public void Delete(PresetDocument preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        if (preset.IsBuiltIn) throw new InvalidOperationException("Built-in presets cannot be deleted.");
        var path = GetUserPresetPath(preset.Id);
        if (File.Exists(path)) File.Delete(path);
        var state = LoadState();
        if (state.DefaultPresetId == preset.Id) SetDefault(null);
    }

    public PresetDocument Import(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        var json = File.ReadAllText(Path.GetFullPath(sourcePath));
        RejectUnsafePropertyNames(json);
        var preset = DeserializeAndValidate(json);
        var catalogue = LoadCatalogue();
        if (catalogue.Any(existing => existing.Id == preset.Id)) preset.Id = Guid.NewGuid();
        preset.IsBuiltIn = false;
        preset.Source = "Imported";
        preset.CreatedUtc = DateTimeOffset.UtcNow;
        preset.ModifiedUtc = preset.CreatedUtc;
        SaveUserPreset(preset);
        return Clone(preset);
    }

    public void Export(PresetDocument preset, string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        var export = Clone(preset);
        export.IsBuiltIn = false;
        var fullPath = EnsureExtension(destinationPath);
        var json = JsonSerializer.Serialize(export, JsonOptions) + Environment.NewLine;
        AtomicFile.WriteUtf8(fullPath, json, keepBackup: true);
    }

    public PresetDocument? Find(Guid? id) => id.HasValue
        ? LoadCatalogue().FirstOrDefault(preset => preset.Id == id.Value)
        : null;

    private void SaveUserPreset(PresetDocument preset)
    {
        preset.IsBuiltIn = false;
        Validate(preset);
        Directory.CreateDirectory(_catalogueDirectory);
        var json = JsonSerializer.Serialize(preset, JsonOptions) + Environment.NewLine;
        AtomicFile.WriteUtf8(GetUserPresetPath(preset.Id), json, keepBackup: true);
    }

    private PresetDocument DeserializeAndValidate(string json)
    {
        PresetDocument preset;
        try
        {
            preset = JsonSerializer.Deserialize<PresetDocument>(json, JsonOptions)
                ?? throw new InvalidDataException("The preset document is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"The preset JSON is invalid: {ex.Message}", ex);
        }
        Validate(preset);
        return preset;
    }

    private static void Validate(PresetDocument preset)
    {
        if (preset.SchemaVersion != PresetDocument.CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Preset schema {preset.SchemaVersion} is unsupported. Supported schema: {PresetDocument.CurrentSchemaVersion}.");
        }
        if (preset.Id == Guid.Empty) throw new InvalidDataException("Preset id must not be empty.");
        if (string.IsNullOrWhiteSpace(preset.Version)) throw new InvalidDataException("Preset version is required.");
        if (preset.Version.Length > 40) throw new InvalidDataException("Preset version cannot exceed 40 characters.");
        if (string.IsNullOrWhiteSpace(preset.Name)) throw new InvalidDataException("Preset name is required.");
        if (preset.Name.Length > 120) throw new InvalidDataException("Preset name cannot exceed 120 characters.");
        if (string.IsNullOrWhiteSpace(preset.Group)) throw new InvalidDataException("Preset group is required.");
        if (preset.Group.Length > 80) throw new InvalidDataException("Preset group cannot exceed 80 characters.");
        preset.Description ??= string.Empty;
        if (preset.Description.Length > 1000) throw new InvalidDataException("Preset description cannot exceed 1000 characters.");
        if (preset.Values is null) throw new InvalidDataException("Preset values are required.");
        if (preset.MediaKinds is null) throw new InvalidDataException("Preset media kinds are required.");
        if (preset.LockedFields is null) throw new InvalidDataException("Preset locked fields are required.");
        if (preset.MediaKinds.Any(kind => kind == MediaKind.Unsupported)) throw new InvalidDataException("Unsupported media applicability is not valid.");
        if (preset.LockedFields.Any(key => !AllowedFieldKeys.Contains(key)))
        {
            throw new InvalidDataException("The preset contains an unknown locked field key.");
        }
        if (preset.Values.ImageQuality is < 1 or > 100) throw new InvalidDataException("Image quality must be from 1 to 100.");
        if (preset.Values.VideoCrf is < 0 or > 51) throw new InvalidDataException("Video CRF must be from 0 to 51.");
        if (preset.Values.ImageWidth is <= 0 or > 100000 || preset.Values.ImageHeight is <= 0 or > 100000)
            throw new InvalidDataException("Image dimensions are outside supported limits.");
        if (preset.Values.VideoWidth is <= 0 or > 16384 || preset.Values.VideoHeight is <= 0 or > 16384)
            throw new InvalidDataException("Video dimensions are outside supported limits.");
        if (preset.Values.ImageScalePercent is <= 0 or > 10000) throw new InvalidDataException("Image scale is outside supported limits.");
        if (preset.Values.VideoCustomFps is <= 0 or > 240) throw new InvalidDataException("Video frame rate is outside supported limits.");
        if (preset.Values.VideoAudioBitrate is <= 0 or > 512 || preset.Values.AudioBitrate is <= 0 or > 512)
            throw new InvalidDataException("Audio bitrate is outside supported limits.");
    }

    private static void RejectUnsafePropertyNames(string json)
    {
        using var document = JsonDocument.Parse(json);
        Walk(document.RootElement);

        static void Walk(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    var name = property.Name;
                    if (name.Contains("argument", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("command", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("executable", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("ffmpegPath", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("ffprobePath", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException($"Imported preset property '{name}' is not allowed.");
                    }
                    Walk(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in element.EnumerateArray()) Walk(child);
            }
        }
    }

    private string GetUserPresetPath(Guid id) => Path.Combine(_catalogueDirectory, $"{id:N}.mediaforge-preset");

    private static string EnsureExtension(string path) =>
        string.Equals(Path.GetExtension(path), ".mediaforge-preset", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(path + ".mediaforge-preset");

    private static string NormaliseGroup(string group) => string.IsNullOrWhiteSpace(group) ? "User" : group.Trim();

    private static List<string> NormaliseLockedFields(IEnumerable<string>? fields) =>
        (fields ?? [])
            .Select(field => field.Trim())
            .Where(field => field.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(field => field, StringComparer.Ordinal)
            .ToList();

    private static PresetDocument Clone(PresetDocument source) => new()
    {
        SchemaVersion = source.SchemaVersion,
        Id = source.Id,
        Version = source.Version,
        Name = source.Name,
        Group = source.Group,
        Description = source.Description,
        MediaKinds = [.. source.MediaKinds],
        Values = source.Values.Clone(),
        LockedFields = [.. source.LockedFields],
        CreatedUtc = source.CreatedUtc,
        ModifiedUtc = source.ModifiedUtc,
        Source = source.Source,
        CompatibilityNotes = source.CompatibilityNotes,
        IsBuiltIn = source.IsBuiltIn,
        Extensions = source.Extensions is null ? null : new Dictionary<string, JsonElement>(source.Extensions, StringComparer.Ordinal)
    };

    private static IReadOnlyList<PresetDocument> CreateBuiltIns()
    {
        var created = new DateTimeOffset(2026, 7, 29, 0, 0, 0, TimeSpan.Zero);
        return
        [
            BuiltIn("222c86a4-8285-4f8a-9622-e05ad3959a11", "Web upload", "Delivery", "Balanced browser-friendly image, video and audio output.", new ConversionOptionOverrides
            {
                ImageFormat = "WebP", ImageQuality = 82, VideoContainer = "MP4", VideoCodec = "H.264", VideoCrf = 23,
                VideoPreset = "medium", VideoAudioCodec = "AAC", VideoAudioBitrate = 160, AudioFormat = "MP3", AudioBitrate = 160
            }, created),
            BuiltIn("a5da06e8-a742-4dad-8678-31d40b6a2418", "YouTube / streaming", "Video", "General upload preset; compatibility remains subject to the active FFmpeg build.", new ConversionOptionOverrides
            {
                VideoContainer = "MP4", VideoCodec = "H.264", VideoCrf = 20, VideoPreset = "slow", VideoResolution = "Keep",
                VideoFps = "Keep", VideoAudioCodec = "AAC", VideoAudioBitrate = 192
            }, created, [MediaKind.Video]),
            BuiltIn("348efbdf-f054-4dc2-bba0-056946e73bd4", "Discord / messaging", "Delivery", "Smaller, broadly compatible output for messaging.", new ConversionOptionOverrides
            {
                ImageFormat = "JPEG", ImageQuality = 78, VideoContainer = "MP4", VideoCodec = "H.264", VideoCrf = 27,
                VideoPreset = "fast", VideoAudioCodec = "AAC", VideoAudioBitrate = 128, AudioFormat = "MP3", AudioBitrate = 128
            }, created),
            BuiltIn("514250bb-3810-47aa-a5a2-85ec449a91a2", "Email attachment", "Delivery", "Compact output intended for attachment-size constrained workflows.", new ConversionOptionOverrides
            {
                ImageFormat = "JPEG", ImageQuality = 72, ImageResizeMode = "Fit", ImageWidth = 1600, ImageHeight = 1600,
                VideoContainer = "MP4", VideoCodec = "H.264", VideoCrf = 30, VideoPreset = "fast", VideoResolution = "720p",
                VideoAudioCodec = "AAC", VideoAudioBitrate = 96, AudioFormat = "MP3", AudioBitrate = 96
            }, created),
            BuiltIn("0035c54b-d5be-42fe-99f8-f12cbd503658", "Mobile compatible", "Delivery", "Conservative mobile playback settings.", new ConversionOptionOverrides
            {
                VideoContainer = "MP4", VideoCodec = "H.264", VideoCrf = 23, VideoPreset = "medium", VideoResolution = "1080p",
                VideoAudioCodec = "AAC", VideoAudioBitrate = 160, AudioFormat = "M4A", AudioBitrate = 160
            }, created),
            BuiltIn("d95c8c29-dc23-4511-b81f-c5b9e7c6ee70", "Archival master", "Archive", "High-quality lossless-oriented settings where the chosen formats support them.", new ConversionOptionOverrides
            {
                ImageFormat = "PNG", ImageQuality = 100, VideoContainer = "MKV", VideoCodec = "H.265", VideoCrf = 16, VideoPreset = "slow", VideoAudioCodec = "AAC",
                VideoAudioBitrate = 320, AudioFormat = "FLAC", StripMetadata = false
            }, created),
            BuiltIn("ae99b868-9ea5-4a6f-84c4-b070bf31d209", "High-quality image", "Image", "High-quality still image output.", new ConversionOptionOverrides
            {
                ImageFormat = "PNG", ImageQuality = 95, ImageResizeMode = "None"
            }, created, [MediaKind.Image]),
            BuiltIn("fd006789-befa-464e-975a-c788273fc24b", "Animated image", "Image", "Animated GIF-oriented output when the source and active FFmpeg build support it.", new ConversionOptionOverrides
            {
                ImageFormat = "GIF", ImageQuality = 90
            }, created, [MediaKind.Image]),
            BuiltIn("9e0321fb-036b-45c8-8316-b57734d20a08", "Podcast audio", "Audio", "Speech-oriented compressed audio.", new ConversionOptionOverrides
            {
                AudioFormat = "MP3", AudioBitrate = 128, AudioSampleRate = "44100", AudioChannels = "1", AudioNormalize = true
            }, created, [MediaKind.Audio]),
            BuiltIn("4b1d34c8-ac07-4557-a8a7-cb6b059c0645", "Music archive", "Audio", "Lossless music archive output.", new ConversionOptionOverrides
            {
                AudioFormat = "FLAC", AudioSampleRate = "Keep", AudioChannels = "Keep", AudioNormalize = false
            }, created, [MediaKind.Audio]),
            BuiltIn("ce2e80b4-64cc-42f5-a34f-23de5c4b6792", "Social portrait", "Social", "Portrait 9:16 H.264 output.", new ConversionOptionOverrides
            {
                VideoContainer = "MP4", VideoCodec = "H.264", VideoCrf = 23, VideoResolution = "Custom", VideoWidth = 1080,
                VideoHeight = 1920, VideoAspectRatio = "9:16", VideoResizeMode = "Fit", VideoAudioCodec = "AAC"
            }, created, [MediaKind.Video]),
            BuiltIn("16bc459f-ee94-4322-915c-91103dcf170c", "Social square", "Social", "Square 1:1 H.264 output.", new ConversionOptionOverrides
            {
                VideoContainer = "MP4", VideoCodec = "H.264", VideoCrf = 23, VideoResolution = "Custom", VideoWidth = 1080,
                VideoHeight = 1080, VideoAspectRatio = "1:1", VideoResizeMode = "Fit", VideoAudioCodec = "AAC"
            }, created, [MediaKind.Video]),
            BuiltIn("7e923471-26dc-43cd-831f-23ef8fe230cc", "Social landscape", "Social", "Landscape 16:9 H.264 output.", new ConversionOptionOverrides
            {
                VideoContainer = "MP4", VideoCodec = "H.264", VideoCrf = 23, VideoResolution = "1080p", VideoAspectRatio = "16:9",
                VideoResizeMode = "Fit", VideoAudioCodec = "AAC"
            }, created, [MediaKind.Video]),
            BuiltIn("da13ce74-b2f2-4d92-a3dc-8205e62d9043", "Editing intermediate", "Video", "High-quality intermediate settings; codec availability is validated in PH11.", new ConversionOptionOverrides
            {
                VideoContainer = "MOV", VideoCodec = "H.264", VideoCrf = 10, VideoPreset = "slow", VideoAudioCodec = "AAC", VideoAudioBitrate = 320
            }, created, [MediaKind.Video])
        ];
    }

    private static PresetDocument BuiltIn(
        string id,
        string name,
        string group,
        string description,
        ConversionOptionOverrides values,
        DateTimeOffset created,
        List<MediaKind>? mediaKinds = null) => new()
    {
        Id = Guid.Parse(id),
        Version = "1.0",
        Name = name,
        Group = group,
        Description = description,
        MediaKinds = mediaKinds ?? [MediaKind.Image, MediaKind.Video, MediaKind.Audio],
        Values = values,
        LockedFields = [],
        CreatedUtc = created,
        ModifiedUtc = created,
        Source = "BuiltIn",
        IsBuiltIn = true,
        CompatibilityNotes = "Availability is resolved against the active FFmpeg build in PH11."
    };
}
