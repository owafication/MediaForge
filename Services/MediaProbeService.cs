using System.Globalization;
using System.IO;
using System.Text.Json;
using MediaForge.Models;
using MediaForge.Services.Runtime;

namespace MediaForge.Services;

public sealed class MediaProbeService
{
    private readonly IProcessRunner _processRunner;

    public MediaProbeService(IProcessRunner? processRunner = null)
    {
        _processRunner = processRunner ?? new ProcessRunner();
    }

    public async Task<MediaProbeInfo> ProbeAsync(string sourcePath, string ffprobePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("The media file was not found.", sourcePath);
        if (!File.Exists(ffprobePath)) throw new FileNotFoundException("ffprobe.exe was not found.", ffprobePath);

        var result = await _processRunner.RunAsync(
            new ProcessRunRequest
            {
                FileName = ffprobePath,
                Arguments =
                [
                    "-v", "error",
                    "-show_entries", "format=duration:stream=codec_type,width,height,avg_frame_rate,r_frame_rate,duration",
                    "-of", "json",
                    sourcePath
                ]
            },
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StandardError)
                ? "ffprobe could not read the media file."
                : result.StandardError.Trim());
        }

        using var document = JsonDocument.Parse(result.StandardOutput);
        var root = document.RootElement;
        var duration = 0d;
        if (root.TryGetProperty("format", out var format) && format.TryGetProperty("duration", out var durationProperty))
        {
            duration = ParseNumber(durationProperty);
        }

        var width = 0;
        var height = 0;
        var frameRate = 0d;
        var streamDuration = 0d;
        var hasVideo = false;
        var hasAudio = false;

        if (root.TryGetProperty("streams", out var streams))
        {
            foreach (var stream in streams.EnumerateArray())
            {
                var codecType = stream.TryGetProperty("codec_type", out var typeProperty) ? typeProperty.GetString() : null;
                if (stream.TryGetProperty("duration", out var streamDurationProperty))
                {
                    streamDuration = Math.Max(streamDuration, ParseNumber(streamDurationProperty));
                }
                if (string.Equals(codecType, "audio", StringComparison.OrdinalIgnoreCase))
                {
                    hasAudio = true;
                    continue;
                }

                if (!string.Equals(codecType, "video", StringComparison.OrdinalIgnoreCase) || hasVideo) continue;
                hasVideo = true;
                if (stream.TryGetProperty("width", out var widthProperty)) width = widthProperty.GetInt32();
                if (stream.TryGetProperty("height", out var heightProperty)) height = heightProperty.GetInt32();
                var rate = stream.TryGetProperty("avg_frame_rate", out var averageRate) ? averageRate.GetString() : null;
                if (string.IsNullOrWhiteSpace(rate) || rate == "0/0")
                {
                    rate = stream.TryGetProperty("r_frame_rate", out var realRate) ? realRate.GetString() : null;
                }
                frameRate = ParseRate(rate);
            }
        }

        if (duration <= 0) duration = streamDuration;
        return new MediaProbeInfo(duration, width, height, frameRate, hasVideo, hasAudio);
    }

    private static double ParseNumber(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var number))
        {
            return double.IsFinite(number) && number > 0 ? number : 0;
        }

        return element.ValueKind == JsonValueKind.String &&
               double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number) &&
               double.IsFinite(number) && number > 0
            ? number
            : 0;
    }

    private static double ParseRate(string? rate)
    {
        if (string.IsNullOrWhiteSpace(rate)) return 0;
        var parts = rate.Split('/');
        if (parts.Length == 2 &&
            double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator) &&
            Math.Abs(denominator) > double.Epsilon)
        {
            var parsedRate = numerator / denominator;
            return double.IsFinite(parsedRate) && parsedRate > 0 ? parsedRate : 0;
        }

        return double.TryParse(rate, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) &&
               double.IsFinite(parsed) && parsed > 0
            ? parsed
            : 0;
    }
}
