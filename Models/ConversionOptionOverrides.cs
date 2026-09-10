using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaForge.Models;

public sealed class ConversionOptionOverrides
{
    public bool? OutputBesideSource { get; set; }
    public bool? PreserveFolderTree { get; set; }
    public bool? PreserveTimestamps { get; set; }
    public bool? StripMetadata { get; set; }
    public string? FileSuffix { get; set; }
    public string? CollisionPolicy { get; set; }

    public string? ImageFormat { get; set; }
    public int? ImageQuality { get; set; }
    public string? ImageResizeMode { get; set; }
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public int? ImageScalePercent { get; set; }
    public string? ImageAspectRatio { get; set; }
    public double? ImageCustomAspectWidth { get; set; }
    public double? ImageCustomAspectHeight { get; set; }

    public string? VideoContainer { get; set; }
    public string? VideoCodec { get; set; }
    public int? VideoCrf { get; set; }
    public string? VideoPreset { get; set; }
    public string? VideoResolution { get; set; }
    public int? VideoWidth { get; set; }
    public int? VideoHeight { get; set; }
    public string? VideoResizeMode { get; set; }
    public string? VideoAspectRatio { get; set; }
    public double? VideoCustomAspectWidth { get; set; }
    public double? VideoCustomAspectHeight { get; set; }
    public string? VideoFps { get; set; }
    public double? VideoCustomFps { get; set; }
    public string? VideoAudioCodec { get; set; }
    public int? VideoAudioBitrate { get; set; }
    public bool? ExtractAudioOnly { get; set; }

    public string? AudioFormat { get; set; }
    public int? AudioBitrate { get; set; }
    public string? AudioSampleRate { get; set; }
    public string? AudioChannels { get; set; }
    public bool? AudioNormalize { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }

    public bool IsEmpty => GetDefinedKeys().Count == 0;

    public IReadOnlySet<string> GetDefinedKeys()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        if (OutputBesideSource.HasValue) keys.Add(nameof(AppSettings.OutputBesideSource));
        if (PreserveFolderTree.HasValue) keys.Add(nameof(AppSettings.PreserveFolderTree));
        if (PreserveTimestamps.HasValue) keys.Add(nameof(AppSettings.PreserveTimestamps));
        if (StripMetadata.HasValue) keys.Add(nameof(AppSettings.StripMetadata));
        Add(keys, nameof(AppSettings.FileSuffix), FileSuffix);
        Add(keys, nameof(AppSettings.CollisionPolicy), CollisionPolicy);
        Add(keys, nameof(AppSettings.ImageFormat), ImageFormat);
        if (ImageQuality.HasValue) keys.Add(nameof(AppSettings.ImageQuality));
        Add(keys, nameof(AppSettings.ImageResizeMode), ImageResizeMode);
        if (ImageWidth.HasValue) keys.Add(nameof(AppSettings.ImageWidth));
        if (ImageHeight.HasValue) keys.Add(nameof(AppSettings.ImageHeight));
        if (ImageScalePercent.HasValue) keys.Add(nameof(AppSettings.ImageScalePercent));
        Add(keys, nameof(AppSettings.ImageAspectRatio), ImageAspectRatio);
        if (ImageCustomAspectWidth.HasValue) keys.Add(nameof(AppSettings.ImageCustomAspectWidth));
        if (ImageCustomAspectHeight.HasValue) keys.Add(nameof(AppSettings.ImageCustomAspectHeight));
        Add(keys, nameof(AppSettings.VideoContainer), VideoContainer);
        Add(keys, nameof(AppSettings.VideoCodec), VideoCodec);
        if (VideoCrf.HasValue) keys.Add(nameof(AppSettings.VideoCrf));
        Add(keys, nameof(AppSettings.VideoPreset), VideoPreset);
        Add(keys, nameof(AppSettings.VideoResolution), VideoResolution);
        if (VideoWidth.HasValue) keys.Add(nameof(AppSettings.VideoWidth));
        if (VideoHeight.HasValue) keys.Add(nameof(AppSettings.VideoHeight));
        Add(keys, nameof(AppSettings.VideoResizeMode), VideoResizeMode);
        Add(keys, nameof(AppSettings.VideoAspectRatio), VideoAspectRatio);
        if (VideoCustomAspectWidth.HasValue) keys.Add(nameof(AppSettings.VideoCustomAspectWidth));
        if (VideoCustomAspectHeight.HasValue) keys.Add(nameof(AppSettings.VideoCustomAspectHeight));
        Add(keys, nameof(AppSettings.VideoFps), VideoFps);
        if (VideoCustomFps.HasValue) keys.Add(nameof(AppSettings.VideoCustomFps));
        Add(keys, nameof(AppSettings.VideoAudioCodec), VideoAudioCodec);
        if (VideoAudioBitrate.HasValue) keys.Add(nameof(AppSettings.VideoAudioBitrate));
        if (ExtractAudioOnly.HasValue) keys.Add(nameof(AppSettings.ExtractAudioOnly));
        Add(keys, nameof(AppSettings.AudioFormat), AudioFormat);
        if (AudioBitrate.HasValue) keys.Add(nameof(AppSettings.AudioBitrate));
        Add(keys, nameof(AppSettings.AudioSampleRate), AudioSampleRate);
        Add(keys, nameof(AppSettings.AudioChannels), AudioChannels);
        if (AudioNormalize.HasValue) keys.Add(nameof(AppSettings.AudioNormalize));
        return keys;
    }

    public ConversionOptionInput ApplyTo(ConversionOptionInput source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source with
        {
            OutputBesideSource = OutputBesideSource ?? source.OutputBesideSource,
            PreserveFolderTree = PreserveFolderTree ?? source.PreserveFolderTree,
            PreserveTimestamps = PreserveTimestamps ?? source.PreserveTimestamps,
            StripMetadata = StripMetadata ?? source.StripMetadata,
            FileSuffix = FileSuffix ?? source.FileSuffix,
            CollisionPolicy = CollisionPolicy ?? source.CollisionPolicy,
            ImageFormat = ImageFormat ?? source.ImageFormat,
            ImageQuality = ImageQuality ?? source.ImageQuality,
            ImageResizeMode = ImageResizeMode ?? source.ImageResizeMode,
            ImageWidth = Format(ImageWidth) ?? source.ImageWidth,
            ImageHeight = Format(ImageHeight) ?? source.ImageHeight,
            ImageScalePercent = Format(ImageScalePercent) ?? source.ImageScalePercent,
            ImageAspectRatio = ImageAspectRatio ?? source.ImageAspectRatio,
            ImageCustomAspectWidth = Format(ImageCustomAspectWidth) ?? source.ImageCustomAspectWidth,
            ImageCustomAspectHeight = Format(ImageCustomAspectHeight) ?? source.ImageCustomAspectHeight,
            VideoContainer = VideoContainer ?? source.VideoContainer,
            VideoCodec = VideoCodec ?? source.VideoCodec,
            VideoCrf = VideoCrf ?? source.VideoCrf,
            VideoPreset = VideoPreset ?? source.VideoPreset,
            VideoResolution = VideoResolution ?? source.VideoResolution,
            VideoWidth = Format(VideoWidth) ?? source.VideoWidth,
            VideoHeight = Format(VideoHeight) ?? source.VideoHeight,
            VideoResizeMode = VideoResizeMode ?? source.VideoResizeMode,
            VideoAspectRatio = VideoAspectRatio ?? source.VideoAspectRatio,
            VideoCustomAspectWidth = Format(VideoCustomAspectWidth) ?? source.VideoCustomAspectWidth,
            VideoCustomAspectHeight = Format(VideoCustomAspectHeight) ?? source.VideoCustomAspectHeight,
            VideoFps = VideoFps ?? source.VideoFps,
            VideoCustomFps = Format(VideoCustomFps) ?? source.VideoCustomFps,
            VideoAudioCodec = VideoAudioCodec ?? source.VideoAudioCodec,
            VideoAudioBitrate = Format(VideoAudioBitrate) ?? source.VideoAudioBitrate,
            ExtractAudioOnly = ExtractAudioOnly ?? source.ExtractAudioOnly,
            AudioFormat = AudioFormat ?? source.AudioFormat,
            AudioBitrate = Format(AudioBitrate) ?? source.AudioBitrate,
            AudioSampleRate = AudioSampleRate ?? source.AudioSampleRate,
            AudioChannels = AudioChannels ?? source.AudioChannels,
            AudioNormalize = AudioNormalize ?? source.AudioNormalize
        };
    }

    public static ConversionOptionOverrides FromSettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new ConversionOptionOverrides
        {
            OutputBesideSource = settings.OutputBesideSource,
            PreserveFolderTree = settings.PreserveFolderTree,
            PreserveTimestamps = settings.PreserveTimestamps,
            StripMetadata = settings.StripMetadata,
            FileSuffix = settings.FileSuffix,
            CollisionPolicy = settings.CollisionPolicy,
            ImageFormat = settings.ImageFormat,
            ImageQuality = settings.ImageQuality,
            ImageResizeMode = settings.ImageResizeMode,
            ImageWidth = settings.ImageWidth,
            ImageHeight = settings.ImageHeight,
            ImageScalePercent = settings.ImageScalePercent,
            ImageAspectRatio = settings.ImageAspectRatio,
            ImageCustomAspectWidth = settings.ImageCustomAspectWidth,
            ImageCustomAspectHeight = settings.ImageCustomAspectHeight,
            VideoContainer = settings.VideoContainer,
            VideoCodec = settings.VideoCodec,
            VideoCrf = settings.VideoCrf,
            VideoPreset = settings.VideoPreset,
            VideoResolution = settings.VideoResolution,
            VideoWidth = settings.VideoWidth,
            VideoHeight = settings.VideoHeight,
            VideoResizeMode = settings.VideoResizeMode,
            VideoAspectRatio = settings.VideoAspectRatio,
            VideoCustomAspectWidth = settings.VideoCustomAspectWidth,
            VideoCustomAspectHeight = settings.VideoCustomAspectHeight,
            VideoFps = settings.VideoFps,
            VideoCustomFps = settings.VideoCustomFps,
            VideoAudioCodec = settings.VideoAudioCodec,
            VideoAudioBitrate = settings.VideoAudioBitrate,
            ExtractAudioOnly = settings.ExtractAudioOnly,
            AudioFormat = settings.AudioFormat,
            AudioBitrate = settings.AudioBitrate,
            AudioSampleRate = settings.AudioSampleRate,
            AudioChannels = settings.AudioChannels,
            AudioNormalize = settings.AudioNormalize
        };
    }

    public ConversionOptionOverrides Clone() => new()
    {
        OutputBesideSource = OutputBesideSource,
        PreserveFolderTree = PreserveFolderTree,
        PreserveTimestamps = PreserveTimestamps,
        StripMetadata = StripMetadata,
        FileSuffix = FileSuffix,
        CollisionPolicy = CollisionPolicy,
        ImageFormat = ImageFormat,
        ImageQuality = ImageQuality,
        ImageResizeMode = ImageResizeMode,
        ImageWidth = ImageWidth,
        ImageHeight = ImageHeight,
        ImageScalePercent = ImageScalePercent,
        ImageAspectRatio = ImageAspectRatio,
        ImageCustomAspectWidth = ImageCustomAspectWidth,
        ImageCustomAspectHeight = ImageCustomAspectHeight,
        VideoContainer = VideoContainer,
        VideoCodec = VideoCodec,
        VideoCrf = VideoCrf,
        VideoPreset = VideoPreset,
        VideoResolution = VideoResolution,
        VideoWidth = VideoWidth,
        VideoHeight = VideoHeight,
        VideoResizeMode = VideoResizeMode,
        VideoAspectRatio = VideoAspectRatio,
        VideoCustomAspectWidth = VideoCustomAspectWidth,
        VideoCustomAspectHeight = VideoCustomAspectHeight,
        VideoFps = VideoFps,
        VideoCustomFps = VideoCustomFps,
        VideoAudioCodec = VideoAudioCodec,
        VideoAudioBitrate = VideoAudioBitrate,
        ExtractAudioOnly = ExtractAudioOnly,
        AudioFormat = AudioFormat,
        AudioBitrate = AudioBitrate,
        AudioSampleRate = AudioSampleRate,
        AudioChannels = AudioChannels,
        AudioNormalize = AudioNormalize,
        Extensions = Extensions is null ? null : new Dictionary<string, JsonElement>(Extensions, StringComparer.Ordinal)
    };

    private static void Add(HashSet<string> keys, string key, string? value)
    {
        if (value is not null) keys.Add(key);
    }

    private static string? Format(int? value) => value?.ToString(CultureInfo.InvariantCulture);
    private static string? Format(double? value) => value?.ToString(CultureInfo.InvariantCulture);
}
