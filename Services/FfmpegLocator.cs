using System.IO;
using MediaForge.Services.Runtime;

namespace MediaForge.Services;

public static class FfmpegLocator
{
    private static readonly IProcessRunner ProcessRunner = new ProcessRunner();

    public static string? FindFfmpeg(string? configuredPath = null)
    {
        var candidates = new List<string?>
        {
            configuredPath,
            Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"),
            Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg.exe"),
            ApplicationDataPaths.TryGetLocalProductPath("tools", "ffmpeg.exe")
        };

        var pathEnvironment = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        candidates.AddRange(pathEnvironment.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory.Trim().Trim('"'), "ffmpeg.exe")));

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            try
            {
                var expanded = Environment.ExpandEnvironmentVariables(candidate.Trim().Trim('"'));
                var fullPath = Path.GetFullPath(expanded);
                if (File.Exists(fullPath)) return fullPath;
            }
            catch
            {
                // Ignore malformed PATH entries and continue searching.
            }
        }

        return null;
    }

    public static string? FindFfprobe(string ffmpegPath)
    {
        var sibling = Path.Combine(Path.GetDirectoryName(ffmpegPath) ?? string.Empty, "ffprobe.exe");
        return File.Exists(sibling) ? sibling : null;
    }

    public static async Task<(bool Success, string Message)> TestAsync(string ffmpegPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(ffmpegPath)) return (false, "ffmpeg.exe was not found.");
        var ffprobePath = FindFfprobe(ffmpegPath);
        if (ffprobePath is null) return (false, "ffprobe.exe must be in the same folder as ffmpeg.exe.");

        try
        {
            var result = await ProcessRunner.RunAsync(
                new ProcessRunRequest
                {
                    FileName = ffmpegPath,
                    Arguments = ["-version"]
                },
                cancellationToken);
            var firstLine = result.StandardOutput
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            return result.ExitCode == 0
                ? (true, string.IsNullOrWhiteSpace(firstLine) ? "FFmpeg is available." : firstLine)
                : (false, string.IsNullOrWhiteSpace(result.StandardError)
                    ? $"FFmpeg exited with code {result.ExitCode}."
                    : result.StandardError.Trim());
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
