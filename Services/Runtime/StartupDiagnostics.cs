using System.IO;
using System.Text;

namespace MediaForge.Services.Runtime;

public static class StartupDiagnostics
{
    private const string ProductDirectoryName = "MediaForge";

    public static string TryWrite(string stage, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var safeStage = string.Concat((stage ?? "startup").Select(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-'));
        if (string.IsNullOrWhiteSpace(safeStage)) safeStage = "startup";

        var candidates = new List<string>();
        var localLogDirectory = ApplicationDataPaths.TryGetLocalProductPath("Logs");
        if (!string.IsNullOrWhiteSpace(localLogDirectory)) candidates.Add(localLogDirectory);
        candidates.Add(Path.Combine(Path.GetTempPath(), ProductDirectoryName, "Logs"));

        foreach (var directory in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, $"{safeStage}-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.log");
                File.WriteAllText(path, BuildReport(safeStage, exception), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                return path;
            }
            catch
            {
                // Startup diagnostics must never replace the original failure.
            }
        }

        return string.Empty;
    }

    private static string BuildReport(string stage, Exception exception)
    {
        var builder = new StringBuilder();
        builder.AppendLine("MediaForge startup diagnostic");
        builder.AppendLine($"UTC: {DateTimeOffset.UtcNow:O}");
        builder.AppendLine($"Stage: {stage}");
        builder.AppendLine($"Process: {Environment.ProcessPath ?? "(unknown)"}");
        builder.AppendLine($"Working directory: {Environment.CurrentDirectory}");
        builder.AppendLine($"OS: {Environment.OSVersion}");
        builder.AppendLine($"Runtime: {Environment.Version}");
        builder.AppendLine();
        builder.AppendLine(exception.ToString());
        return builder.ToString();
    }
}
