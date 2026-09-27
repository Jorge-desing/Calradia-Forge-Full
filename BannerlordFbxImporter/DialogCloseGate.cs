namespace BannerlordFbxImporter.Automation;

/// <summary>Evidence required before the helper may report its UI sequence as SUBMITTED.</summary>
public readonly record struct DialogCloseObservation(
    bool WindowEnumerationComplete,
    bool ExpectedDialogStillExists,
    bool UnexpectedVisibleWindowExists,
    bool MainWindowResponsive,
    bool NestedWindowVisible);

public static class DialogCloseGate
{
    public static bool CanReportSubmitted(DialogCloseObservation observation, out string reason)
    {
        if (!observation.WindowEnumerationComplete)
            reason = "Process window enumeration was incomplete.";
        else if (observation.ExpectedDialogStillExists)
            reason = "The configured file dialog still exists.";
        else if (observation.UnexpectedVisibleWindowExists)
            reason = "Another visible process-owned window remains open.";
        else if (!observation.MainWindowResponsive)
            reason = "The Editor main window did not answer a bounded, non-mutating window probe.";
        else if (observation.NestedWindowVisible)
            reason = "A nested window or modal remains visible inside the Editor.";
        else
        {
            reason = "The configured file dialog closed and no blocking window was observed.";
            return true;
        }

        return false;
    }
}
