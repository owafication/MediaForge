using MediaForge.Models;

namespace MediaForge.Services.Queue;

public sealed record QueueEstimate(long? EstimatedBytes, TimeSpan? EstimatedProcessingTime, string Basis, string Confidence)
{
    public string Display
    {
        get
        {
            if (!EstimatedBytes.HasValue && !EstimatedProcessingTime.HasValue) return "Estimate unavailable (unproven)";
            var parts = new List<string>();
            if (EstimatedBytes.HasValue) parts.Add($"~{FormatBytes(EstimatedBytes.Value)}");
            if (EstimatedProcessingTime.HasValue) parts.Add($"~{FormatDuration(EstimatedProcessingTime.Value)}");
            return $"Est. {string.Join(" / ", parts)} ({Confidence.ToLowerInvariant()} confidence)";
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.##} {units[unit]}";
    }

    private static string FormatDuration(TimeSpan duration) => duration.TotalHours >= 1
        ? $"{duration.TotalHours:0.#} h"
        : duration.TotalMinutes >= 1
            ? $"{duration.TotalMinutes:0.#} min"
            : $"{Math.Max(1, duration.TotalSeconds):0} sec";
}

public sealed class QueueEstimateService
{
    public QueueEstimate Estimate(MediaJob job, ConversionOptions options)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(options);
        if (job.SourceBytes <= 0) return new QueueEstimate(null, null, "No source-size evidence", "Unproven");

        var factor = job.Kind switch
        {
            MediaKind.Image => options.ImageFormat switch
            {
                "PNG" => 1.05,
                "GIF" => 0.8,
                "JPEG" => 0.35 + (options.ImageQuality / 100d * 0.35),
                "WebP" => 0.25 + (options.ImageQuality / 100d * 0.3),
                _ => 0.75
            },
            MediaKind.Video when options.VideoCodec == "Copy" => 1.0,
            MediaKind.Video => Math.Clamp(0.18 + ((51 - options.VideoCrf) / 51d * 0.62), 0.15, 0.9),
            MediaKind.Audio when options.AudioFormat == "WAV" => 1.2,
            MediaKind.Audio when options.AudioFormat == "FLAC" => 0.65,
            MediaKind.Audio => Math.Clamp(options.AudioBitrate / 512d, 0.08, 0.75),
            _ => 1.0
        };
        var estimated = checked((long)Math.Min(long.MaxValue, Math.Max(1, job.SourceBytes * factor)));
        var throughputBytesPerSecond = job.Kind switch
        {
            MediaKind.Image => 20d * 1024 * 1024,
            MediaKind.Video when options.VideoCodec == "Copy" => 80d * 1024 * 1024,
            MediaKind.Video => 8d * 1024 * 1024,
            MediaKind.Audio => 16d * 1024 * 1024,
            _ => 10d * 1024 * 1024
        };
        var seconds = Math.Clamp(job.SourceBytes / throughputBytesPerSecond, 1, 24 * 60 * 60);
        return new QueueEstimate(
            estimated,
            TimeSpan.FromSeconds(seconds),
            "Source bytes plus selected format/quality and conservative throughput heuristics",
            "Low");
    }
}
