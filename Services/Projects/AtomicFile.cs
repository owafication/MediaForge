using System.Text;
using System.IO;

namespace MediaForge.Services.Projects;

internal static class AtomicFile
{
    public static void WriteUtf8(string destinationPath, string content, bool keepBackup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentNullException.ThrowIfNull(content);

        var fullPath = Path.GetFullPath(destinationPath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException($"The destination has no parent directory: {fullPath}");
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        var backupPath = fullPath + ".bak";
        try
        {
            using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writer.Write(content);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            if (!File.Exists(fullPath))
            {
                File.Move(tempPath, fullPath);
                tempPath = string.Empty;
                return;
            }

            try
            {
                File.Replace(tempPath, fullPath, keepBackup ? backupPath : null, ignoreMetadataErrors: true);
                tempPath = string.Empty;
                if (!keepBackup) TryDelete(backupPath);
                return;
            }
            catch (PlatformNotSupportedException)
            {
                // Fall through to a bounded rename sequence.
            }
            catch (IOException)
            {
                // Some filesystems do not implement replacement semantics.
            }

            var rollbackPath = keepBackup ? backupPath : fullPath + $".{Guid.NewGuid():N}.rollback";
            TryDelete(rollbackPath);
            File.Move(fullPath, rollbackPath);
            try
            {
                File.Move(tempPath, fullPath);
                tempPath = string.Empty;
                if (!keepBackup) TryDelete(rollbackPath);
            }
            catch
            {
                if (!File.Exists(fullPath) && File.Exists(rollbackPath))
                {
                    File.Move(rollbackPath, fullPath);
                }
                throw;
            }
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tempPath)) TryDelete(tempPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Cleanup failure must not conceal the write result.
        }
    }
}
