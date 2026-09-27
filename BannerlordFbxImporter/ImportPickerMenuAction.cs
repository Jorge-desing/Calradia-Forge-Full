namespace BannerlordFbxImporter.Automation;

/// <summary>A UIA menu item discovered beneath the exact configured Resource Browser window.</summary>
public sealed record ImportPickerMenuCandidate<T>(
    string Name,
    string ControlType,
    int ProcessId,
    string WindowTitle,
    bool IsInsideVisibleMenu,
    bool IsVisible,
    bool IsEnabled,
    bool SupportsInvoke,
    T Element);

/// <summary>
/// Opens only the Resource Browser's exact context-menu command that requests its
/// file picker. It cannot select a file or invoke an Import/Save control.
/// </summary>
public static class ImportPickerMenuAction
{
    public const string ExactMenuItemName = "Import new asset";
    public const string ExactControlType = "ControlType.MenuItem";

    public static void InvokeUnique<T>(
        IEnumerable<ImportPickerMenuCandidate<T>> candidates,
        int expectedProcessId,
        string exactResourceBrowserTitle,
        Action<T> invoke)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(exactResourceBrowserTitle);
        ArgumentNullException.ThrowIfNull(invoke);
        if (expectedProcessId <= 0)
            throw new ArgumentOutOfRangeException(nameof(expectedProcessId));
        if (!string.Equals(exactResourceBrowserTitle, exactResourceBrowserTitle.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("The Resource Browser title must be exact and trimmed.", nameof(exactResourceBrowserTitle));

        ImportPickerMenuCandidate<T>[] matches = candidates.Where(candidate =>
            candidate.ProcessId == expectedProcessId &&
            string.Equals(candidate.WindowTitle, exactResourceBrowserTitle, StringComparison.Ordinal) &&
            candidate.IsInsideVisibleMenu &&
            candidate.IsVisible &&
            string.Equals(candidate.ControlType, ExactControlType, StringComparison.Ordinal) &&
            string.Equals(candidate.Name, ExactMenuItemName, StringComparison.Ordinal)).ToArray();

        if (matches.Length == 0)
            throw new InvalidOperationException($"The exact '{ExactMenuItemName}' menu item was not uniquely exposed by the visible '{exactResourceBrowserTitle}' window in PID {expectedProcessId}. No fallback or other UI action was attempted.");
        if (matches.Length != 1)
            throw new InvalidOperationException($"Found {matches.Length} exact '{ExactMenuItemName}' menu items in the configured Resource Browser. The menu target is ambiguous; no UI action was attempted.");

        ImportPickerMenuCandidate<T> selected = matches[0];
        if (!selected.IsEnabled || !selected.SupportsInvoke)
            throw new InvalidOperationException($"The unique '{ExactMenuItemName}' item is disabled or does not expose InvokePattern. No UI action was attempted.");

        try
        {
            invoke(selected.Element);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"UNKNOWN: invoking '{ExactMenuItemName}' did not return a reliable outcome. Do not retry; the Resource Browser was not otherwise controlled.", ex);
        }
    }
}
