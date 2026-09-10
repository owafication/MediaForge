using System.IO;
namespace MediaForge.Services.Projects;

public static class ProjectPathPolicy
{
    public static string? ResolveProjectRoot(string? projectPath, string? projectRoot)
    {
        if (string.IsNullOrWhiteSpace(projectPath)) return null;
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath));
        if (string.IsNullOrWhiteSpace(projectDirectory)) return null;
        if (string.IsNullOrWhiteSpace(projectRoot)) return projectDirectory;
        if (Path.IsPathRooted(projectRoot)) return null;

        var candidate = Path.GetFullPath(Path.Combine(projectDirectory, projectRoot));
        return IsInside(candidate, projectDirectory) && !IsReparsePoint(candidate) ? candidate : null;
    }

    public static bool TryMakeRelativeInsideRoot(string fullPath, string? root, out string? relativePath)
    {
        relativePath = null;
        if (string.IsNullOrWhiteSpace(root)) return false;

        var canonicalRoot = Path.GetFullPath(root);
        var canonicalPath = Path.GetFullPath(fullPath);
        if (!IsInside(canonicalPath, canonicalRoot) || TraversesReparsePoint(canonicalRoot, canonicalPath)) return false;

        var relative = Path.GetRelativePath(canonicalRoot, canonicalPath);
        if (relative == "." || Path.IsPathRooted(relative) || EscapesRoot(relative)) return false;
        relativePath = relative.Replace(Path.DirectorySeparatorChar, '/');
        return true;
    }

    public static bool TryResolveRelativeInsideRoot(string root, string relativePath, out string? fullPath)
    {
        fullPath = null;
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath) || EscapesRoot(relativePath)) return false;

        var canonicalRoot = Path.GetFullPath(root);
        var candidate = Path.GetFullPath(Path.Combine(canonicalRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsInside(candidate, canonicalRoot) || TraversesReparsePoint(canonicalRoot, candidate)) return false;
        fullPath = candidate;
        return true;
    }

    private static bool TraversesReparsePoint(string root, string candidate)
    {
        try
        {
            var relative = Path.GetRelativePath(root, candidate);
            var current = Path.GetFullPath(root);
            if (IsReparsePoint(current)) return true;
            foreach (var segment in relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, segment);
                if (!File.Exists(current) && !Directory.Exists(current)) continue;
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
            }
        }
        catch
        {
            return true;
        }
        return false;
    }

    private static bool IsReparsePoint(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) return false;
        return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    }

    private static bool EscapesRoot(string relativePath)
    {
        var segments = relativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment == "..");
    }

    private static bool IsInside(string candidate, string root)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var canonicalRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var canonicalCandidate = Path.GetFullPath(candidate);
        return string.Equals(canonicalCandidate, canonicalRoot, comparison) ||
               canonicalCandidate.StartsWith(canonicalRoot + Path.DirectorySeparatorChar, comparison) ||
               canonicalCandidate.StartsWith(canonicalRoot + Path.AltDirectorySeparatorChar, comparison);
    }
}
