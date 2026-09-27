using BannerlordFbxImporter.Automation;

namespace BannerlordFbxImporter;

public enum UiSubmissionState { Submitted, Stopped, Unknown }
public sealed record UiSubmissionResult(UiSubmissionState State, string Detail, bool ModalLeftOpen = false);
public sealed record AssetSubmissionResult(string Path, string Outcome, string Detail, bool ModalLeftOpen);
public sealed record SubmissionSummary(IReadOnlyList<AssetSubmissionResult> Results, bool Stopped);

public interface IResourceBrowserAutomation
{
    EditorInspection Inspect();
    string ResolveUniqueAssetsTarget(string moduleName);
    UiSubmissionResult SubmitFile(string moduleName, string expectedAssetsPath, BannerlordFbxImporter.Preflight.AssetFile file);
}

public sealed record InspectedControl(string Name, string AutomationId, string ControlType, string Path, int ProcessId, string Patterns, string Bounds);
public sealed record EditorInspection(int ProcessId, string ProcessName, string WindowName, IReadOnlyList<string> AssetsTreePaths,
    IReadOnlyList<InspectedControl> Controls, IReadOnlyList<string> Warnings);

public static class SubmissionConfirmation
{
    public const string RequiredText = "IMPORT";
    public static bool IsAccepted(string? response) => string.Equals(response, RequiredText, StringComparison.Ordinal);
}

public static class CalibrationEvidenceConfirmation
{
    public const string RequiredText = "VERIFIED";
    public static bool IsAccepted(string? response) => string.Equals(response, RequiredText, StringComparison.Ordinal);
}

public static class SubmissionCoordinator
{
    public static SubmissionSummary Submit(ImportPlan plan, string moduleName, string expectedAssetsPath, IResourceBrowserAutomation automation)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(automation);
        var results = new List<AssetSubmissionResult>();
        foreach (PlannedAsset asset in plan.Assets)
        {
            UiSubmissionResult result;
            try { result = automation.SubmitFile(moduleName, expectedAssetsPath, asset.File); }
            catch (Exception ex)
            {
                results.Add(new(asset.File.FullPath, "UNKNOWN", $"UI outcome is uncertain; no retry was attempted: {ex.Message}", false));
                return new(results, true);
            }

            string outcome = result.State switch
            {
                UiSubmissionState.Submitted => "SUBMITTED",
                UiSubmissionState.Unknown => "UNKNOWN",
                _ => "STOPPED"
            };
            results.Add(new(asset.File.FullPath, outcome, result.Detail, result.ModalLeftOpen));
            if (result.State != UiSubmissionState.Submitted)
                return new(results, true);
        }
        return new(results, false);
    }
}
