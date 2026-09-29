using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalradiaForge.Sdk;
using CalradiaForge.Sdk.Patcher;

namespace CalradiaForge.Core
{
    /// <summary>
    /// Host-side lifecycle for explicit experimental method replacements. This serializes
    /// management operations, but does not make instruction writes safe against concurrent calls.
    /// </summary>
    internal sealed class ForgePatchService : IForgePatchService, IForgePatchServiceLifecycle
    {
        private sealed class PatchEntry
        {
            internal string Id;
            internal string Owner;
            internal MethodInfo Target;
            internal MethodInfo Replacement;
            internal byte[] OriginalBytes;
            internal ForgePatchState State;
            internal string Detail;
        }

        private readonly object gate = new object();
        private readonly Dictionary<string, PatchEntry> entries = new Dictionary<string, PatchEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> applyOrder = new List<string>();
        private bool accepting = true;

        public IForgePatchHandle ApplyMethodReplacement(string patchId, string owner, MethodInfo target, MethodInfo replacement)
        {
            if (string.IsNullOrWhiteSpace(patchId) || patchId.Length > 128)
                throw new ArgumentException("A patch ID between 1 and 128 characters is required.", nameof(patchId));
            if (string.IsNullOrWhiteSpace(owner) || owner.Length > 128)
                throw new ArgumentException("An owner label between 1 and 128 characters is required.", nameof(owner));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));

            lock (gate)
            {
                if (!accepting) throw new InvalidOperationException("The Forge patch service is disconnected and rejects new patches.");
                if (entries.ContainsKey(patchId)) throw new InvalidOperationException("Patch ID already exists: " + patchId);
                if (entries.Values.Any(entry => entry.State == ForgePatchState.Applied && entry.Target == target))
                    throw new InvalidOperationException("A Forge patch is already active for the target method.");
                if (ForgeDetour.IsTracked(target))
                    throw new InvalidOperationException("A Forge detour is already active for the target method.");

                var entry = new PatchEntry
                {
                    Id = patchId,
                    Owner = owner,
                    Target = target,
                    Replacement = replacement,
                    State = ForgePatchState.Failed,
                    Detail = "Patch application has not completed."
                };
                // Reserve both service receipts before executable bytes can be written.
                entries.Add(patchId, entry);
                try
                {
                    applyOrder.Add(patchId);
                    ForgeDetour.Patch(target, replacement, patchId, owner);
                    entry.OriginalBytes = ForgeDetour.GetOriginalBytes(target);
                    entry.State = ForgePatchState.Applied;
                    entry.Detail = "Exact replacement signature validated; detour bytes and instruction cache were installed.";
                    return new Handle(this, patchId);
                }
                catch (Exception error)
                {
                    // ForgeDetour retains a record when it cannot prove rollback. Preserve
                    // that uncertain result so operators can inspect and retry deliberately.
                    if (ForgeDetour.IsTrackedPatch(target, patchId))
                    {
                        try
                        {
                            entry.OriginalBytes = ForgeDetour.GetOriginalBytes(target);
                            entry.State = ForgePatchState.Failed;
                            entry.Detail = Bound("Apply failed; target memory may need manual review: " + error.Message, 240);
                        }
                        catch { entry.Detail = Bound("Apply failed and the retained detour record could not be read: " + error.Message, 240); }
                    }
                    else
                    {
                        entries.Remove(patchId);
                        applyOrder.Remove(patchId);
                    }
                    throw;
                }
            }
        }

        public IReadOnlyList<ForgePatchSnapshot> GetSnapshots(string owner = null)
        {
            lock (gate)
            {
                var snapshots = new List<ForgePatchSnapshot>();
                foreach (string id in applyOrder)
                {
                    PatchEntry entry;
                    if (!entries.TryGetValue(id, out entry) || (!string.IsNullOrWhiteSpace(owner) &&
                        !string.Equals(owner, entry.Owner, StringComparison.OrdinalIgnoreCase))) continue;
                    Refresh(entry);
                    snapshots.Add(Snapshot(entry));
                }
                return snapshots.AsReadOnly();
            }
        }

        public ForgePatchVerification Verify(string patchId)
        {
            if (string.IsNullOrWhiteSpace(patchId)) throw new ArgumentException("A patch ID is required.", nameof(patchId));
            lock (gate)
            {
                PatchEntry entry;
                if (!entries.TryGetValue(patchId, out entry))
                    return new ForgePatchVerification(patchId, ForgePatchState.Failed, false, "No patch record exists for this ID.");
                Refresh(entry);
                return Verification(entry);
            }
        }

        public ForgePatchRevertResult Revert(string patchId)
        {
            if (string.IsNullOrWhiteSpace(patchId)) throw new ArgumentException("A patch ID is required.", nameof(patchId));
            lock (gate)
            {
                PatchEntry entry;
                if (!entries.TryGetValue(patchId, out entry))
                    return new ForgePatchRevertResult(patchId, ForgePatchState.Failed, false, "No patch record exists for this ID.");
                Refresh(entry);
                if (entry.State == ForgePatchState.Reverted)
                    return new ForgePatchRevertResult(entry.Id, entry.State, true, "Already reverted; no write was made.");
                if (entry.State == ForgePatchState.Conflict)
                    return new ForgePatchRevertResult(entry.Id, entry.State, false, entry.Detail);

                try
                {
                    if (ForgeDetour.Unpatch(entry.Target))
                    {
                        entry.State = ForgePatchState.Reverted;
                        entry.Detail = "Original bytes restored and verified.";
                    }
                    else
                    {
                        string status;
                        ForgeDetour.Verify(entry.Target, out status);
                        entry.State = status == "foreign-bytes" ? ForgePatchState.Conflict : ForgePatchState.Failed;
                        entry.Detail = status == "foreign-bytes"
                            ? "Target bytes changed outside Forge; the foreign bytes were left untouched."
                            : "Forge could not confirm the target state; no success is reported.";
                    }
                }
                catch (Exception error)
                {
                    entry.State = ForgePatchState.Failed;
                    entry.Detail = Bound("Revert failed: " + error.Message, 240);
                }
                return RevertResult(entry);
            }
        }

        public IReadOnlyList<ForgePatchRevertResult> RevertOwner(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("An owner label is required.", nameof(owner));
            lock (gate)
            {
                var ids = applyOrder.Where(id => entries.ContainsKey(id) &&
                    string.Equals(entries[id].Owner, owner, StringComparison.OrdinalIgnoreCase)).Reverse().ToArray();
                var result = new List<ForgePatchRevertResult>(ids.Length);
                foreach (string id in ids) result.Add(Revert(id));
                return result.AsReadOnly();
            }
        }

        public IReadOnlyList<ForgePatchRevertResult> RevertAll()
        {
            lock (gate)
            {
                var ids = applyOrder.AsEnumerable().Reverse().ToArray();
                var result = new List<ForgePatchRevertResult>(ids.Length);
                foreach (string id in ids) result.Add(Revert(id));
                return result.AsReadOnly();
            }
        }

        public void Reconnect()
        {
            lock (gate)
            {
                if (accepting) return;

                foreach (string id in applyOrder)
                {
                    PatchEntry entry;
                    if (entries.TryGetValue(id, out entry)) Refresh(entry);
                }

                bool unresolved = entries.Values.Any(entry => entry.State != ForgePatchState.Reverted ||
                    entry.OriginalBytes == null || ForgeDetour.IsTracked(entry.Target) ||
                    !ForgeDetour.VerifyRestored(entry.Target, entry.OriginalBytes));
                if (unresolved)
                    throw new InvalidOperationException("The Forge patch service retains an applied, conflicted, uncertain, or unverified patch; resolve it before reconnecting.");

                accepting = true;
            }
        }

        public void Disconnect()
        {
            lock (gate)
            {
                accepting = false;
            }

            // Disconnect only reverses entries owned by this service instance. The shared
            // low-level registry also serves direct/compatibility callers; those entries
            // must remain untouched by an unrelated service lifecycle.
            Exception failure = null;
            try { RevertAll(); }
            catch (Exception error) { failure = error; }
            ForgePatcher.RemoveVerifiedRevertedRecords();

            lock (gate)
                foreach (string id in applyOrder)
                {
                    PatchEntry entry;
                    if (entries.TryGetValue(id, out entry))
                    {
                        Refresh(entry);
                        if (failure != null && entry.State != ForgePatchState.Reverted)
                            entry.Detail = Bound("Disconnect retained this patch for manual review: " + failure.Message, 240);
                    }
                }
        }

        private ForgePatchSnapshot Snapshot(PatchEntry entry)
        {
            return new ForgePatchSnapshot(entry.Id, entry.Owner, Identity(entry.Target), Identity(entry.Replacement), entry.State,
                entry.State == ForgePatchState.Applied ? ForgeDetour.Verify(entry.Target, out _) :
                    entry.State == ForgePatchState.Reverted && ForgeDetour.VerifyRestored(entry.Target, entry.OriginalBytes));
        }

        private ForgePatchVerification Verification(PatchEntry entry)
        {
            bool intact = entry.State == ForgePatchState.Applied
                ? ForgeDetour.Verify(entry.Target, out _)
                : entry.State == ForgePatchState.Reverted && ForgeDetour.VerifyRestored(entry.Target, entry.OriginalBytes);
            return new ForgePatchVerification(entry.Id, entry.State, intact, entry.Detail);
        }

        private void Refresh(PatchEntry entry)
        {
            if (entry.State == ForgePatchState.Conflict)
            {
                if (!ForgeDetour.IsTracked(entry.Target) && ForgeDetour.VerifyRestored(entry.Target, entry.OriginalBytes))
                {
                    entry.State = ForgePatchState.Reverted;
                    entry.Detail = "Original bytes were verified after external cleanup.";
                }
                else if (ForgeDetour.Verify(entry.Target, out string conflictStatus))
                {
                    entry.State = ForgePatchState.Applied;
                    entry.Detail = "Installed bytes match the Forge detour record again; an explicit revert is still required.";
                }
                else if (conflictStatus == "write-state-uncertain")
                {
                    entry.State = ForgePatchState.Failed;
                    entry.Detail = "The write state remains uncertain; manual review is required.";
                }
            }
            else if (entry.State == ForgePatchState.Applied || entry.State == ForgePatchState.Failed)
            {
                if (!ForgeDetour.IsTracked(entry.Target) && ForgeDetour.VerifyRestored(entry.Target, entry.OriginalBytes))
                {
                    entry.State = ForgePatchState.Reverted;
                    entry.Detail = "Original bytes were verified after shared-registry rollback.";
                    return;
                }
                string status;
                bool intact = ForgeDetour.Verify(entry.Target, out status);
                if (intact)
                {
                    entry.State = ForgePatchState.Applied;
                    entry.Detail = "Installed bytes match the Forge detour record.";
                }
                else if (status == "foreign-bytes")
                {
                    entry.State = ForgePatchState.Conflict;
                    entry.Detail = "Target bytes changed outside Forge; Forge will not overwrite them.";
                }
            }
            else if (entry.State == ForgePatchState.Reverted && !ForgeDetour.VerifyRestored(entry.Target, entry.OriginalBytes))
            {
                entry.State = ForgePatchState.Conflict;
                entry.Detail = "Target bytes changed after Forge reverted the method.";
            }
        }

        private static ForgePatchRevertResult RevertResult(PatchEntry entry)
        {
            bool reverted = entry.State == ForgePatchState.Reverted;
            return new ForgePatchRevertResult(entry.Id, entry.State, reverted, entry.Detail);
        }

        private static string Identity(MethodInfo method)
        {
            return method == null ? string.Empty :
                (method.DeclaringType == null ? "?" : method.DeclaringType.FullName) + "." + method.Name;
        }

        private static string Bound(string value, int maximum)
        {
            value = value ?? string.Empty;
            return value.Length <= maximum ? value : value.Substring(0, maximum);
        }

        private sealed class Handle : IForgePatchHandle
        {
            private readonly ForgePatchService service;
            private readonly string patchId;
            internal Handle(ForgePatchService service, string patchId) { this.service = service; this.patchId = patchId; }
            public ForgePatchSnapshot Snapshot
            {
                get
                {
                    var snapshots = service.GetSnapshots();
                    return snapshots.FirstOrDefault(item => string.Equals(item.PatchId, patchId, StringComparison.OrdinalIgnoreCase))
                        ?? new ForgePatchSnapshot(patchId, string.Empty, string.Empty, string.Empty, ForgePatchState.Failed, false);
                }
            }
            public ForgePatchVerification Verify() { return service.Verify(patchId); }
            public ForgePatchRevertResult Revert() { return service.Revert(patchId); }
            public void Dispose() { service.Revert(patchId); }
        }
    }
}
