using System.IO;
using MediaForge.Models;

namespace MediaForge.Services;

public static class MediaClassifier
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".jfif", ".png", ".webp", ".avif", ".bmp", ".dib", ".gif",
        ".tif", ".tiff", ".heic", ".heif", ".jxl", ".ico", ".ppm", ".pgm", ".pbm",
        ".dds", ".exr", ".hdr"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".mov", ".avi", ".webm", ".m4v", ".wmv", ".flv",
        ".mpeg", ".mpg", ".m2v", ".ts", ".m2ts", ".mts", ".3gp", ".3g2", ".ogv", ".vob",
        ".mxf", ".f4v", ".asf", ".rm", ".rmvb"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".flac", ".aac", ".m4a", ".ogg", ".oga", ".opus",
        ".wma", ".aif", ".aiff", ".amr", ".ac3", ".eac3", ".mka", ".caf", ".ape",
        ".m4b", ".au", ".snd", ".dts"
    };

    public static string SupportedDialogPattern => string.Join(";",
        ImageExtensions.Concat(VideoExtensions).Concat(AudioExtensions)
            .OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)
            .Select(extension => $"*{extension}"));

    public static string VideoDialogPattern => string.Join(";",
        VideoExtensions.OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)
            .Select(extension => $"*{extension}"));

    public static MediaKind Classify(string path)
    {
        var extension = Path.GetExtension(path);
        if (ImageExtensions.Contains(extension)) return MediaKind.Image;
        if (VideoExtensions.Contains(extension)) return MediaKind.Video;
        if (AudioExtensions.Contains(extension)) return MediaKind.Audio;
        return MediaKind.Unsupported;
    }

    public static bool IsSupported(string path) => Classify(path) != MediaKind.Unsupported;
}
