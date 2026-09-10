namespace MediaForge.Models;

public sealed record ConversionOptionInput
{
    public required string FfmpegPath { get; init; }
    public bool OutputBesideSource { get; init; }
    public required string OutputFolder { get; init; }
    public bool PreserveFolderTree { get; init; }
    public bool PreserveTimestamps { get; init; }
    public bool StripMetadata { get; init; }
    public required string FileSuffix { get; init; }
    public required string CollisionPolicy { get; init; }
    public required string ParallelJobs { get; init; }

    public required string ImageFormat { get; init; }
    public int ImageQuality { get; init; }
    public required string ImageResizeMode { get; init; }
    public required string ImageWidth { get; init; }
    public required string ImageHeight { get; init; }
    public required string ImageScalePercent { get; init; }
    public required string ImageAspectRatio { get; init; }
    public required string ImageCustomAspectWidth { get; init; }
    public required string ImageCustomAspectHeight { get; init; }

    public required string VideoContainer { get; init; }
    public required string VideoCodec { get; init; }
    public int VideoCrf { get; init; }
    public required string VideoPreset { get; init; }
    public required string VideoResolution { get; init; }
    public required string VideoWidth { get; init; }
    public required string VideoHeight { get; init; }
    public required string VideoResizeMode { get; init; }
    public required string VideoAspectRatio { get; init; }
    public required string VideoCustomAspectWidth { get; init; }
    public required string VideoCustomAspectHeight { get; init; }
    public required string VideoFps { get; init; }
    public required string VideoCustomFps { get; init; }
    public required string VideoAudioCodec { get; init; }
    public required string VideoAudioBitrate { get; init; }
    public bool ExtractAudioOnly { get; init; }

    public required string AudioFormat { get; init; }
    public required string AudioBitrate { get; init; }
    public required string AudioSampleRate { get; init; }
    public required string AudioChannels { get; init; }
    public bool AudioNormalize { get; init; }
}
