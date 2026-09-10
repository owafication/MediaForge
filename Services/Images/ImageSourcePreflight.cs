using System.Globalization;

namespace MediaForge.Services.Images;

public static class ImageSourcePreflight
{
    public const long ConservativeMaximumEstimatedFrameBytes = int.MaxValue;

    public static ImageSourceInfo? ValidateForFfmpeg(string sourcePath)
    {
        var info = ImageHeaderProbe.TryRead(sourcePath);
        if (info is null || info.EstimatedDecodedBytes <= ConservativeMaximumEstimatedFrameBytes) return info;

        var megapixels = info.PixelCount / 1_000_000d;
        var gibibytes = info.EstimatedDecodedBytes / 1024d / 1024d / 1024d;
        throw new InvalidOperationException(
            $"The source image is {info.Width.ToString("N0", CultureInfo.InvariantCulture)} × {info.Height.ToString("N0", CultureInfo.InvariantCulture)} ({megapixels.ToString("0.0", CultureInfo.InvariantCulture)} MP). " +
            $"Its estimated decoded frame is {gibibytes.ToString("0.00", CultureInfo.InvariantCulture)} GiB, above MediaForge's conservative FFmpeg backend safety threshold. " +
            "FFmpeg must decode the full source before resizing, so this file cannot be converted safely by the current backend. " +
            "Export a smaller source or split it into tiles before converting.");
    }

    public static string DescribeFfmpegFailure(string standardError, int exitCode)
    {
        if (standardError.Contains("Picture size", StringComparison.OrdinalIgnoreCase) &&
            standardError.Contains("is invalid", StringComparison.OrdinalIgnoreCase))
        {
            return "FFmpeg rejected the image dimensions before decoding. The source exceeds a decoded-frame limit enforced by this FFmpeg build or its image utilities. " +
                   "FFmpeg must decode the full source before resizing; export a smaller source or split it into tiles.\n" + standardError;
        }

        return string.IsNullOrWhiteSpace(standardError)
            ? $"FFmpeg exited with code {exitCode}."
            : standardError;
    }
}
