using System.IO;

namespace BannerlordFbxImporter.Preflight;

public sealed record AssetScanLimits(
    int MaximumFiles = 100,
    int MaximumDepth = 32,
    int MaximumDirectories = 10_000,
    long MaximumFileBytes = 256L * 1024 * 1024,
    long MaximumBatchBytes = 2L * 1024 * 1024 * 1024,
    int MaximumEntries = 100_000);

public sealed record AssetFile(string FullPath, long Length, DateTime LastWriteTimeUtc);
public sealed record AssetBatch(string SourceRoot, IReadOnlyList<AssetFile> Files, IReadOnlyList<string> Warnings, long TotalBytes);

public static class AssetBatchScanner
{
    private const int MaximumDisplayedWarnings = 32;
    private static readonly IReadOnlySet<string> MeshExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".fbx" };

    public static bool IsSupportedFbx(string path) =>
        string.Equals(Path.GetExtension(path), ".fbx", StringComparison.OrdinalIgnoreCase);

    public static AssetBatch Scan(string sourceFolder, AssetScanLimits? limits = null, IEnumerable<string>? supportedExtensions = null)
    {
        limits ??= new AssetScanLimits();
        ValidateLimits(limits);
        HashSet<string> extensions = ValidateExtensions(supportedExtensions ?? MeshExtensions);
        if (string.IsNullOrWhiteSpace(sourceFolder))
            throw new ArgumentException("Provide a source folder.", nameof(sourceFolder));

        string root = Path.GetFullPath(sourceFolder);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Source folder does not exist: {root}");
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("The source root is a reparse point; choose its resolved directory explicitly.");

        var warnings = new List<string>();
        var files = new List<AssetFile>();
        var pending = new Stack<(string Path, int Depth)>();
        pending.Push((root, 0));
        int directoriesVisited = 1;
        int entriesVisited = 0;
        int reparsePointsSkipped = 0;
        long totalBytes = 0;

        while (pending.Count > 0)
        {
            var (directory, depth) = pending.Pop();
            try
            {
                foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
                {
                    entriesVisited++;
                    if (entriesVisited > limits.MaximumEntries)
                        throw new InvalidOperationException($"Filesystem entry count exceeds the limit of {limits.MaximumEntries}; no Editor interaction was attempted.");

                    FileAttributes attributes;
                    try { attributes = File.GetAttributes(entry); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        throw new InvalidOperationException($"Could not inspect '{entry}'; the batch was rejected without accessing the Editor.", ex);
                    }

                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        reparsePointsSkipped++;
                        if (warnings.Count < MaximumDisplayedWarnings)
                            warnings.Add($"Skipped reparse point: {entry}");
                        continue;
                    }

                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        int childDepth = depth + 1;
                        if (childDepth > limits.MaximumDepth)
                            throw new InvalidOperationException($"Directory depth exceeds the limit of {limits.MaximumDepth} at '{entry}'.");
                        directoriesVisited++;
                        if (directoriesVisited > limits.MaximumDirectories)
                            throw new InvalidOperationException($"Directory count exceeds the limit of {limits.MaximumDirectories}.");
                        pending.Push((entry, childDepth));
                        continue;
                    }

                    if (!extensions.Contains(Path.GetExtension(entry))) continue;
                    long length;
                    DateTime lastWriteTimeUtc;
                    try
                    {
                        var info = new FileInfo(entry);
                        info.Refresh();
                        length = info.Length;
                        lastWriteTimeUtc = info.LastWriteTimeUtc;
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        throw new InvalidOperationException($"Could not read file metadata for '{entry}'.", ex);
                    }

                    if (length < 0 || length > limits.MaximumFileBytes)
                        throw new InvalidOperationException($"Asset file exceeds the {limits.MaximumFileBytes}-byte per-file limit: '{entry}'.");
                    totalBytes = checked(totalBytes + length);
                    if (totalBytes > limits.MaximumBatchBytes)
                        throw new InvalidOperationException($"Batch exceeds the {limits.MaximumBatchBytes}-byte total limit.");

                    files.Add(new AssetFile(Path.GetFullPath(entry), length, lastWriteTimeUtc));
                    if (files.Count > limits.MaximumFiles)
                        throw new InvalidOperationException($"Batch exceeds the {limits.MaximumFiles}-file limit.");
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"Could not enumerate '{directory}'; the batch was rejected without accessing the Editor.", ex);
            }
        }

        if (reparsePointsSkipped > MaximumDisplayedWarnings)
            warnings.Add($"Skipped {reparsePointsSkipped - MaximumDisplayedWarnings} additional reparse points; warning details were capped.");

        files.Sort(static (left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.FullPath, right.FullPath));
        string[] duplicates = files
            .GroupBy(file => Path.GetFileNameWithoutExtension(file.FullPath), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (duplicates.Length > 0)
            throw new InvalidOperationException("Duplicate asset base names are not allowed in one batch: " + string.Join(", ", duplicates));

        return new AssetBatch(root, files, warnings, totalBytes);
    }

    private static void ValidateLimits(AssetScanLimits limits)
    {
        if (limits.MaximumFiles is < 1 or > 100 || limits.MaximumDepth is < 0 or > 32 ||
            limits.MaximumDirectories is < 1 or > 10_000 || limits.MaximumFileBytes is < 1 or > 256L * 1024 * 1024 ||
            limits.MaximumBatchBytes is < 1 or > 2L * 1024 * 1024 * 1024 || limits.MaximumEntries is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(limits), "Scan limits may be lowered for tests but cannot exceed the hard safety bounds.");
    }

    private static HashSet<string> ValidateExtensions(IEnumerable<string> extensions)
    {
        ArgumentNullException.ThrowIfNull(extensions);
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string? extension in extensions)
        {
            if (string.IsNullOrWhiteSpace(extension))
                throw new ArgumentException("Supported asset extensions must be explicit file extensions.", nameof(extensions));
            string normalized = extension.Trim().ToLowerInvariant();
            if (normalized.Length is < 2 or > 12 || normalized[0] != '.' ||
                normalized[1..].Any(character => !char.IsAsciiLetterOrDigit(character)))
                throw new ArgumentException($"Invalid asset extension '{extension}'. Use a simple extension recorded from the selected Editor profile.", nameof(extensions));
            result.Add(normalized);
        }
        if (result.Count == 0)
            throw new ArgumentException("At least one supported extension is required.", nameof(extensions));
        return result;
    }
}
