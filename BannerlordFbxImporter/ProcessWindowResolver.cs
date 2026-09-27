namespace BannerlordFbxImporter.Automation;

public sealed record EditorWindowCandidate(int ProcessId, string Title, bool IsVisible, IntPtr Handle);

/// <summary>Resolves a visible top-level Editor window without accepting a different same-process window.</summary>
public static class ProcessWindowResolver
{
    public static EditorWindowCandidate ResolveUnique(
        IEnumerable<EditorWindowCandidate> candidates,
        int expectedProcessId,
        string exactWindowTitle)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(exactWindowTitle);
        if (!string.Equals(exactWindowTitle, exactWindowTitle.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("The configured Editor window title must be an exact, trimmed title.", nameof(exactWindowTitle));

        EditorWindowCandidate[] matches = candidates
            .Where(candidate => candidate.ProcessId == expectedProcessId && candidate.IsVisible &&
                string.Equals(candidate.Title, exactWindowTitle, StringComparison.Ordinal))
            .ToArray();

        if (matches.Length == 0)
            throw new InvalidOperationException($"No visible '{exactWindowTitle}' window was found in configured Editor process {expectedProcessId}.");
        if (matches.Length > 1)
            throw new InvalidOperationException($"Found {matches.Length} visible '{exactWindowTitle}' windows in configured Editor process {expectedProcessId}; the UI target is ambiguous.");
        if (matches[0].Handle == IntPtr.Zero)
            throw new InvalidOperationException($"The selected '{exactWindowTitle}' window has no usable native handle.");

        return matches[0];
    }
}
