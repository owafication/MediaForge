using System.IO;

namespace MediaForge.Services.Runtime;

public static class ApplicationDataPaths
{
    public const string RoamingRootEnvironmentVariable = "MEDIAFORGE_APPDATA_ROOT";
    public const string LocalRootEnvironmentVariable = "MEDIAFORGE_LOCALAPPDATA_ROOT";
    private const string ProductDirectoryName = "MediaForge";

    public static string GetRoamingRoot() => ResolveRoot(
        RoamingRootEnvironmentVariable,
        Environment.SpecialFolder.ApplicationData);

    public static string GetLocalRoot() => ResolveRoot(
        LocalRootEnvironmentVariable,
        Environment.SpecialFolder.LocalApplicationData);

    public static string GetRoamingProductPath(params string[] segments) =>
        Combine(GetRoamingRoot(), segments);

    public static string GetLocalProductPath(params string[] segments) =>
        Combine(GetLocalRoot(), segments);

    public static string? TryGetLocalProductPath(params string[] segments)
    {
        try { return GetLocalProductPath(segments); }
        catch { return null; }
    }

    private static string ResolveRoot(string overrideVariable, Environment.SpecialFolder fallbackFolder)
    {
        var explicitRoot = Environment.GetEnvironmentVariable(overrideVariable);
        if (!string.IsNullOrWhiteSpace(explicitRoot))
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(explicitRoot.Trim().Trim('"')));
        }

        var systemRoot = Environment.GetFolderPath(fallbackFolder);
        if (string.IsNullOrWhiteSpace(systemRoot))
        {
            throw new DirectoryNotFoundException($"Windows did not provide {fallbackFolder}.");
        }
        return Path.GetFullPath(systemRoot);
    }

    private static string Combine(string root, IReadOnlyList<string> segments)
    {
        var path = Path.Combine(root, ProductDirectoryName);
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment)) continue;
            path = Path.Combine(path, segment);
        }
        return path;
    }
}
