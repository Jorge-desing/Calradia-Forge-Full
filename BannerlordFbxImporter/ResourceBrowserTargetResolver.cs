namespace BannerlordFbxImporter.Automation;

public static class ResourceBrowserTargetResolver
{
    public static string ResolveUniqueAssetsPath(IEnumerable<string?> candidatePaths, string moduleName)
    {
        ArgumentNullException.ThrowIfNull(candidatePaths);
        if (string.IsNullOrWhiteSpace(moduleName) || moduleName.Trim() != moduleName ||
            moduleName.Contains(" > ", StringComparison.Ordinal) || moduleName.IndexOfAny(new[] { '\\', '/' }) >= 0)
            throw new ArgumentException("Provide the exact visible module folder name.", nameof(moduleName));

        string?[] paths = candidatePaths.ToArray();
        if (paths.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("At least one visible Assets tree path was unreadable; destination selection is unsafe.");

        string[] matches = paths
            .Cast<string>()
            .Where(path => IsDirectModuleAssetsPath(path, moduleName))
            .ToArray();
        if (matches.Length == 0)
            throw new InvalidOperationException($"No direct '{moduleName} > Assets' path was visible. Run --inspect and verify the exact module name.");
        if (matches.Length > 1)
            throw new InvalidOperationException($"The module path is ambiguous: {string.Join("; ", matches)}");
        return matches[0];
    }

    private static bool IsDirectModuleAssetsPath(string path, string moduleName)
    {
        string[] segments = path.Split(" > ", StringSplitOptions.None);
        return segments.Length == 3 &&
            string.Equals(segments[0], "Modules", StringComparison.Ordinal) &&
            string.Equals(segments[^2], moduleName, StringComparison.Ordinal) &&
            string.Equals(segments[^1], "Assets", StringComparison.Ordinal);
    }
}
