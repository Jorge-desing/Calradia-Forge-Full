using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalradiaForge.Sdk;

namespace CalradiaForge.Core
{
    /// <summary>Captures Forge-owned hook and replacement-patch records, with optional bounded observations from an already loaded external patch runtime.</summary>
    public static class ForgePatchDiagnostics
    {
        const int MaximumForgeRecords = 128;
        const int MaximumForgeOrderingIds = 16;
        const int MaximumTotalForgeOrderingIds = 1024;
        const int MaximumExternalTargets = 64;
        const int MaximumForgeTextLength = 256;
        const int MaximumNotes = 16;

        /// <summary>Captures built-in Forge registries and optionally probes already loaded assemblies without loading a runtime.</summary>
        /// <remarks>Custom registry implementations remain supported as best-effort inputs, but their synchronous GetSnapshots call and internal allocation cannot be bounded by this observer. Such captures are marked Incomplete; copied record output is still capped.</remarks>
        public static ForgePatchDiagnosticsSnapshot Capture(
            IForgeHookService hooks,
            IForgePatchService patches,
            IEnumerable<Assembly> loadedAssemblies = null,
            int maximumExternalTargets = 200)
        {
            var result = new ForgePatchDiagnosticsSnapshot { CapturedAt = DateTime.UtcNow.ToString("O") };
            var incomplete = false;
            var unavailable = hooks == null && patches == null;
            var remainingOrderingIds = MaximumTotalForgeOrderingIds;

            CaptureHooks(hooks, result, ref remainingOrderingIds, ref incomplete);
            CapturePatches(patches, result, ref incomplete);

            if (loadedAssemblies == null)
            {
                result.ExternalRuntime = ExternalPatchRuntimeInspector.NotRequested();
            }
            else
            {
                result.ExternalRuntime = ExternalPatchRuntimeInspector.Inspect(loadedAssemblies, null,
                    Math.Max(1, Math.Min(maximumExternalTargets, MaximumExternalTargets)));
                if (string.Equals(result.ExternalRuntime.Status, "Incomplete", StringComparison.Ordinal))
                    AddNote(result, "Optional external-runtime observations are incomplete; Forge-owned registry records remain separate.");
            }

            if (unavailable)
            {
                result.Status = "Unavailable";
                AddNote(result, "Forge hook and patch registries were not supplied; no conclusion about Forge-owned registrations is available.");
            }
            else if (incomplete || result.Truncated)
            {
                result.Status = "Incomplete";
            }
            else
            {
                result.Status = "Observed";
            }

            result.HookCount = result.Hooks.Count;
            result.PatchCount = result.Patches.Count;
            result.ConflictCount = result.Hooks.Count(hook => hook.State == ForgeHookState.Conflict.ToString())
                + result.Patches.Count(patch => patch.State == ForgePatchState.Conflict.ToString());
            result.FailedCount = result.Hooks.Count(hook => hook.State == ForgeHookState.Failed.ToString())
                + result.Patches.Count(patch => patch.State == ForgePatchState.Failed.ToString());

            if (result.Status == "Observed" && result.HookCount == 0 && result.PatchCount == 0)
                AddNote(result, "Observed: the supplied Forge-owned registries returned no hook or replacement-patch records.");
            else if (result.Status == "Observed")
                AddNote(result, "Observed: records describe Forge-owned registrations only; they do not enumerate unrelated patching systems.");

            return result;
        }

        static void CaptureHooks(IForgeHookService service, ForgePatchDiagnosticsSnapshot result, ref int remainingOrderingIds, ref bool incomplete)
        {
            if (service == null)
            {
                incomplete = true;
                AddNote(result, "Forge hook registry was not supplied; hook state is unavailable.");
                return;
            }

            IReadOnlyList<ForgeHookSnapshot> snapshots;
            var sourceTruncated = false;
            try
            {
                var builtIn = service as ForgeHookService;
                if (builtIn != null)
                    snapshots = builtIn.GetDiagnosticSnapshots(RemainingRecordBudget(result), MaximumForgeOrderingIds, remainingOrderingIds, out sourceTruncated);
                else
                {
                    snapshots = service.GetSnapshots();
                    incomplete = true;
                    AddNote(result, "Custom Forge hook registry GetSnapshots ran synchronously and its internal time/allocation could not be bounded; copied records are capped.");
                }
            }
            catch (Exception error)
            {
                incomplete = true;
                AddNote(result, "Forge hook registry query failed with " + ErrorType(error) + ".");
                return;
            }

            if (snapshots == null)
            {
                incomplete = true;
                AddNote(result, "Forge hook registry returned no snapshot collection.");
                return;
            }

            try
            {
                foreach (var snapshot in snapshots)
                {
                    if (result.Hooks.Count + result.Patches.Count >= MaximumForgeRecords)
                    {
                        MarkTruncated(result, ref incomplete, "Forge-owned hook and patch records were limited to " + MaximumForgeRecords + ".");
                        return;
                    }
                    if (snapshot == null)
                    {
                        incomplete = true;
                        AddNote(result, "Forge hook registry contained a null snapshot; it was skipped.");
                        continue;
                    }
                    result.Hooks.Add(new ForgeOwnedHookRecord
                    {
                        Id = Bound(snapshot.Id, result, ref incomplete),
                        Owner = Bound(snapshot.Owner, result, ref incomplete),
                        TargetMethod = Bound(snapshot.TargetMethod, result, ref incomplete),
                        State = snapshot.State.ToString(),
                        Detail = Bound(snapshot.Detail, result, ref incomplete),
                        HasPrefix = snapshot.HasPrefix,
                        HasPostfix = snapshot.HasPostfix,
                        HasFinalizer = snapshot.HasFinalizer,
                        HasTranspiler = snapshot.HasTranspiler,
                        Priority = snapshot.Priority,
                        Before = CopyStrings(snapshot.Before, result, ref remainingOrderingIds, ref incomplete),
                        After = CopyStrings(snapshot.After, result, ref remainingOrderingIds, ref incomplete)
                    });
                }
            }
            catch (Exception error)
            {
                incomplete = true;
                AddNote(result, "Forge hook snapshot enumeration stopped with " + ErrorType(error) + ".");
            }

            if (sourceTruncated)
                MarkTruncated(result, ref incomplete, "Forge hook diagnostic records or ordering metadata exceeded their output bounds.");
        }

        static void CapturePatches(IForgePatchService service, ForgePatchDiagnosticsSnapshot result, ref bool incomplete)
        {
            if (service == null)
            {
                incomplete = true;
                AddNote(result, "Forge replacement-patch registry was not supplied; patch state is unavailable.");
                return;
            }

            IReadOnlyList<ForgePatchSnapshot> snapshots;
            var sourceTruncated = false;
            try
            {
                var builtIn = service as ForgePatchService;
                if (builtIn != null)
                    snapshots = builtIn.GetDiagnosticSnapshots(RemainingRecordBudget(result), MaximumForgeTextLength, out sourceTruncated);
                else
                {
                    snapshots = service.GetSnapshots();
                    incomplete = true;
                    AddNote(result, "Custom Forge patch registry GetSnapshots ran synchronously and its internal time/allocation could not be bounded; copied records are capped.");
                }
            }
            catch (Exception error)
            {
                incomplete = true;
                AddNote(result, "Forge replacement-patch registry query failed with " + ErrorType(error) + ".");
                return;
            }

            if (snapshots == null)
            {
                incomplete = true;
                AddNote(result, "Forge replacement-patch registry returned no snapshot collection.");
                return;
            }

            try
            {
                foreach (var snapshot in snapshots)
                {
                    if (result.Hooks.Count + result.Patches.Count >= MaximumForgeRecords)
                    {
                        MarkTruncated(result, ref incomplete, "Forge-owned hook and patch records were limited to " + MaximumForgeRecords + ".");
                        return;
                    }
                    if (snapshot == null)
                    {
                        incomplete = true;
                        AddNote(result, "Forge replacement-patch registry contained a null snapshot; it was skipped.");
                        continue;
                    }
                    result.Patches.Add(new ForgeOwnedPatchRecord
                    {
                        PatchId = Bound(snapshot.PatchId, result, ref incomplete),
                        Owner = Bound(snapshot.Owner, result, ref incomplete),
                        TargetMethod = Bound(snapshot.TargetMethod, result, ref incomplete),
                        ReplacementMethod = Bound(snapshot.ReplacementMethod, result, ref incomplete),
                        State = snapshot.State.ToString(),
                        IsIntact = snapshot.IsIntact
                    });
                }
            }
            catch (Exception error)
            {
                incomplete = true;
                AddNote(result, "Forge replacement-patch snapshot enumeration stopped with " + ErrorType(error) + ".");
            }

            if (sourceTruncated)
                MarkTruncated(result, ref incomplete, "Forge replacement-patch diagnostic records exceeded their output bounds.");
        }

        static int RemainingRecordBudget(ForgePatchDiagnosticsSnapshot result) =>
            Math.Max(0, MaximumForgeRecords - result.Hooks.Count - result.Patches.Count);

        static List<string> CopyStrings(IReadOnlyList<string> values, ForgePatchDiagnosticsSnapshot result, ref int remainingOrderingIds, ref bool incomplete)
        {
            var copied = new List<string>();
            if (values == null) return copied;
            try
            {
                var count = Math.Min(values.Count, Math.Min(MaximumForgeOrderingIds, remainingOrderingIds));
                for (var index = 0; index < count; index++) copied.Add(Bound(values[index], result, ref incomplete));
                remainingOrderingIds -= count;
                if (values.Count > count)
                    MarkTruncated(result, ref incomplete, "Forge hook ordering metadata exceeded its per-side or aggregate output bound.");
            }
            catch (Exception error)
            {
                incomplete = true;
                AddNote(result, "Forge hook ordering metadata could not be read with " + ErrorType(error) + ".");
            }
            return copied;
        }

        static string ErrorType(Exception error) => error?.GetType().Name ?? "UnknownError";

        static string Bound(string value, ForgePatchDiagnosticsSnapshot result, ref bool incomplete)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Length <= MaximumForgeTextLength) return value;
            MarkTruncated(result, ref incomplete, "Forge-owned diagnostic text was limited to " + MaximumForgeTextLength + " characters per field.");
            return value.Substring(0, MaximumForgeTextLength - 1) + "…";
        }

        static string Bound(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= MaximumForgeTextLength ? value : value.Substring(0, MaximumForgeTextLength - 1) + "…";
        }

        static void MarkTruncated(ForgePatchDiagnosticsSnapshot result, ref bool incomplete, string note)
        {
            result.Truncated = true;
            incomplete = true;
            AddNote(result, note);
        }

        static void AddNote(ForgePatchDiagnosticsSnapshot result, string note)
        {
            if (result.Notes.Count < MaximumNotes && !result.Notes.Contains(note)) result.Notes.Add(Bound(note));
        }
    }
}
