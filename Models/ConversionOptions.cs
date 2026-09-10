namespace MediaForge.Models;

public sealed record ConversionOptions
{
    public required string FfmpegPath { get; init; }
    public required string FfprobePath { get; init; }
    public required string OutputFolder { get; init; }
    public bool OutputBesideSource { get; init; }
    public bool PreserveFolderTree { get; init; }
    public bool PreserveTimestamps { get; init; }
    public bool StripMetadata { get; init; }
    public required string FileSuffix { get; init; }
    public required string CollisionPolicy { get; init; }

    public required string ImageFormat { get; init; }
    public int ImageQuality { get; init; }
    public required string ImageResizeMode { get; init; }
    public int ImageWidth { get; init; }
    public int ImageHeight { get; init; }
    public int ImageScalePercent { get; init; }
    public required string ImageAspectRatio { get; init; }
    public double ImageCustomAspectWidth { get; init; }
    public double ImageCustomAspectHeight { get; init; }

    public required string VideoContainer { get; init; }
    public required string VideoCodec { get; init; }
    public int VideoCrf { get; init; }
    public required string VideoPreset { get; init; }
    public required string VideoResolution { get; init; }
    public int VideoWidth { get; init; }
    public int VideoHeight { get; init; }
    public required string VideoResizeMode { get; init; }
    public required string VideoAspectRatio { get; init; }
    public double VideoCustomAspectWidth { get; init; }
    public double VideoCustomAspectHeight { get; init; }
    public required string VideoFps { get; init; }
    public double VideoCustomFps { get; init; }
    public required string VideoAudioCodec { get; init; }
    public int VideoAudioBitrate { get; init; }
    public bool ExtractAudioOnly { get; init; }

    public required string AudioFormat { get; init; }
    public int AudioBitrate { get; init; }
    public required string AudioSampleRate { get; init; }
    public required string AudioChannels { get; init; }
    public bool AudioNormalize { get; init; }
}
