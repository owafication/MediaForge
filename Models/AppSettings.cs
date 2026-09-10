using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaForge.Models;

public sealed class AppSettings
{
    public string FfmpegPath { get; set; } = string.Empty;
    public bool OutputBesideSource { get; set; }
    public string OutputFolder { get; set; } = string.Empty;
    public bool PreserveFolderTree { get; set; } = true;
    public bool PreserveTimestamps { get; set; } = true;
    public bool StripMetadata { get; set; }
    public string FileSuffix { get; set; } = "_converted";
    public string CollisionPolicy { get; set; } = "Rename";
    public int ParallelJobs { get; set; } = 2;

    public string ImageFormat { get; set; } = "JPEG";
    public int ImageQuality { get; set; } = 85;
    public string ImageResizeMode { get; set; } = "None";
    public int ImageWidth { get; set; } = 1920;
    public int ImageHeight { get; set; } = 1080;
    public int ImageScalePercent { get; set; } = 100;
    public string ImageAspectRatio { get; set; } = "Free";
    public double ImageCustomAspectWidth { get; set; } = 16;
    public double ImageCustomAspectHeight { get; set; } = 9;

    public string VideoContainer { get; set; } = "MP4";
    public string VideoCodec { get; set; } = "H.264";
    public int VideoCrf { get; set; } = 23;
    public string VideoPreset { get; set; } = "medium";
    public string VideoResolution { get; set; } = "Keep";
    public int VideoWidth { get; set; } = 1920;
    public int VideoHeight { get; set; } = 1080;
    public string VideoResizeMode { get; set; } = "Fit";
    public string VideoAspectRatio { get; set; } = "16:9";
    public double VideoCustomAspectWidth { get; set; } = 16;
    public double VideoCustomAspectHeight { get; set; } = 9;
    public string VideoFps { get; set; } = "Keep";
    public double VideoCustomFps { get; set; } = 30;
    public string VideoAudioCodec { get; set; } = "AAC";
    public int VideoAudioBitrate { get; set; } = 192;
    public bool ExtractAudioOnly { get; set; }

    public string AudioFormat { get; set; } = "MP3";
    public int AudioBitrate { get; set; } = 192;
    public string AudioSampleRate { get; set; } = "Keep";
    public string AudioChannels { get; set; } = "Keep";
    public bool AudioNormalize { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}
