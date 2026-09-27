using System.Security.Cryptography;
using System.Text.Json;
using System.IO;

namespace BannerlordFbxImporter;

public sealed record AssetBackupEntry(string RelativePath, long Length, string Sha256);
public sealed record AssetBackupResult(string BackupDirectory, string ManifestPath, int FileCount, long TotalBytes);

/// <summary>Creates a verified, non-destructive snapshot of a module's editable Assets tree before replacements.</summary>
public static class AssetBackupService
{
    public const int MaximumFiles = 100_000;
    public const long MaximumBytes = 4L * 1024 * 1024 * 1024;
    private const int MaximumDepth = 32;

    public static string ValidateAssetsRoot(string path, string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        string fullPath = Path.GetFullPath(path);
        var root = new DirectoryInfo(fullPath);
        if (!root.Exists || !string.Equals(root.Name, "Assets", StringComparison.Ordinal))
            throw new InvalidOperationException("moduleAssetsDirectory must be an existing folder named Assets.");
        if (root.Parent is null || !string.Equals(root.Parent.Name, moduleName, StringComparison.Ordinal))
            throw new InvalidOperationException("The on-disk Assets folder must be directly inside the module named by --module-name.");
        if ((root.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("A reparse-point Assets folder cannot be used as a backup root.");
        return root.FullName;
    }

    public static AssetBackupResult CreateVerifiedBackup(string assetsPath, string moduleName, string? backupRoot = null)
    {
        string sourceRoot = ValidateAssetsRoot(assetsPath, moduleName);
        string backupBase = Path.GetFullPath(backupRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CalradiaForge", "Importer", "Backups"));
        if (IsSameOrChildPath(backupBase, sourceRoot))
            throw new InvalidOperationException("Backup directory must be outside the module Assets tree.");

        string runDirectory = Path.Combine(backupBase, DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffZ"), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(runDirectory);
        string payloadRoot = Path.Combine(runDirectory, "Assets");
        Directory.CreateDirectory(payloadRoot);

        try
        {
            string[] sourceFiles = EnumerateSafeFiles(sourceRoot)
                .OrderBy(path => Path.GetRelativePath(sourceRoot, path), StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (sourceFiles.Length > MaximumFiles)
                throw new InvalidOperationException($"Assets snapshot exceeds its safety limit of {MaximumFiles} files; no replacement was performed.");

            var before = new Dictionary<string, (long Length, DateTime LastWriteUtc)>(StringComparer.OrdinalIgnoreCase);
            long totalBytes = 0;
            foreach (string source in sourceFiles)
            {
                FileAttributes attributes = File.GetAttributes(source);
                if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
                    throw new InvalidOperationException($"Unsupported file entry encountered during backup: '{source}'.");
                var info = new FileInfo(source);
                info.Refresh();
                string relative = Path.GetRelativePath(sourceRoot, source);
                before.Add(relative, (info.Length, info.LastWriteTimeUtc));
                totalBytes = checked(totalBytes + info.Length);
                if (totalBytes > MaximumBytes)
                    throw new InvalidOperationException($"Assets snapshot exceeds its safety limit of {MaximumBytes} bytes; no replacement was performed.");
            }

            var manifest = new List<AssetBackupEntry>(sourceFiles.Length);
            foreach (string source in sourceFiles)
            {
                string relative = Path.GetRelativePath(sourceRoot, source);
                string destination = Path.Combine(payloadRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                (long length, string hash) = CopyAndHash(source, destination);
                if (length != before[relative].Length || !string.Equals(hash, HashFile(destination), StringComparison.OrdinalIgnoreCase))
                    throw new IOException($"Backup hash or size verification failed for '{relative}'.");
                manifest.Add(new(relative, length, hash));
            }

            string[] finalFiles = EnumerateSafeFiles(sourceRoot)
                .Select(path => Path.GetRelativePath(sourceRoot, path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string[] initialFiles = before.Keys.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
            if (!finalFiles.SequenceEqual(initialFiles, StringComparer.OrdinalIgnoreCase))
                throw new IOException("The Assets tree changed while it was being backed up; the snapshot is not considered verified.");
            Dictionary<string, AssetBackupEntry> manifestByPath = manifest.ToDictionary(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase);
            foreach (string source in sourceFiles)
            {
                string relative = Path.GetRelativePath(sourceRoot, source);
                var info = new FileInfo(source);
                info.Refresh();
                if (info.Length != before[relative].Length || info.LastWriteTimeUtc != before[relative].LastWriteUtc ||
                    !string.Equals(HashFile(source), manifestByPath[relative].Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new IOException($"The source asset changed during final backup verification: '{relative}'.");
            }

            string manifestPath = Path.Combine(runDirectory, "backup-manifest.json");
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
            return new(runDirectory, manifestPath, manifest.Count, totalBytes);
        }
        catch
        {
            try { Directory.Delete(runDirectory, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            throw;
        }
    }

    private static IEnumerable<string> EnumerateSafeFiles(string root)
    {
        var pending = new Stack<(string Path, int Depth)>();
        pending.Push((root, 0));
        int visited = 0;
        int directories = 0;
        while (pending.Count > 0)
        {
            var (directory, depth) = pending.Pop();
            foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
            {
                if (++visited > MaximumFiles * 2)
                    throw new InvalidOperationException("Assets snapshot enumeration exceeded its entry bound.");
                FileAttributes attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException($"Reparse points are not allowed in a verified Assets backup: '{entry}'.");
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    if (++directories > MaximumFiles)
                        throw new InvalidOperationException("Assets snapshot enumeration exceeded its directory bound.");
                    if (depth >= MaximumDepth)
                        throw new InvalidOperationException($"Assets backup exceeds maximum nesting depth at '{entry}'.");
                    pending.Push((entry, depth + 1));
                }
                else yield return entry;
            }
        }
    }

    private static (long Length, string Sha256) CopyAndHash(string source, string destination)
    {
        using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
        using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.SequentialScan);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[64 * 1024];
        long total = 0;
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            output.Write(buffer, 0, read);
            hash.AppendData(buffer, 0, read);
            total += read;
        }
        output.Flush(flushToDisk: true);
        return (total, Convert.ToHexString(hash.GetHashAndReset()));
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static bool IsSameOrChildPath(string candidate, string parent)
    {
        string normalizedCandidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
        string normalizedParent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent));
        return string.Equals(normalizedCandidate, normalizedParent, StringComparison.OrdinalIgnoreCase) ||
            normalizedCandidate.StartsWith(normalizedParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
