using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Experimental low-level x64 detouring. This implementation does not suspend threads or
    /// decode/relocate instructions, so it is not safe against a target executing during a write.
    /// Use only in an isolated development build after validating the exact target runtime.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ForgeDetour
    {
        private const uint PageExecuteReadWrite = 0x40;
        private const int JumpInstructionLength = 13;
        private static readonly object Gate = new object();
        private static Dictionary<IntPtr, DetourState> Active = new Dictionary<IntPtr, DetourState>();
        private static readonly List<IntPtr> ApplyOrder = new List<IntPtr>();
        private static IExecutableMemoryAdapter memory = new NativeExecutableMemoryAdapter();
        private static bool applicationsEnabled = true;
        private static ulong? systemPageSizeOverrideForTests;

        // The explicit SDK host lifecycle closes every application route (including legacy
        // direct adapters) before cleanup. Reverts remain available while the gate is closed.
        internal static void StopAcceptingApplications()
        {
            lock (Gate) applicationsEnabled = false;
        }

        internal static void AllowApplications()
        {
            lock (Gate) applicationsEnabled = true;
        }

        private static void EnsureApplicationsEnabled()
        {
            if (!applicationsEnabled)
                throw new InvalidOperationException("Forge patch applications are disabled while the SDK host is disconnected or resolving a patch conflict.");
        }

        internal interface IExecutableMemoryAdapter
        {
            bool Is64BitProcess { get; }
            byte[] Read(IntPtr address, int count);
            void Write(IntPtr address, byte[] bytes);
            bool TryProtect(IntPtr address, int count, uint newProtect, out uint oldProtect, out int error);
            bool Flush(IntPtr address, int count, out int error);
        }

        private sealed class NativeExecutableMemoryAdapter : IExecutableMemoryAdapter
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool VirtualProtect(IntPtr address, UIntPtr size, uint newProtect, out uint oldProtect);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool FlushInstructionCache(IntPtr process, IntPtr baseAddress, UIntPtr size);

            [DllImport("kernel32.dll")]
            private static extern IntPtr GetCurrentProcess();

            public bool Is64BitProcess { get { return Environment.Is64BitProcess; } }
            public byte[] Read(IntPtr address, int count)
            {
                var bytes = new byte[count];
                Marshal.Copy(address, bytes, 0, count);
                return bytes;
            }
            public void Write(IntPtr address, byte[] bytes) { Marshal.Copy(bytes, 0, address, bytes.Length); }
            public bool TryProtect(IntPtr address, int count, uint newProtect, out uint oldProtect, out int error)
            {
                bool succeeded = VirtualProtect(address, (UIntPtr)count, newProtect, out oldProtect);
                error = succeeded ? 0 : Marshal.GetLastWin32Error();
                return succeeded;
            }
            public bool Flush(IntPtr address, int count, out int error)
            {
                bool succeeded = FlushInstructionCache(GetCurrentProcess(), address, (UIntPtr)count);
                error = succeeded ? 0 : Marshal.GetLastWin32Error();
                return succeeded;
            }
        }

        private sealed class DetourState
        {
            internal string Id;
            internal string Owner;
            internal MethodInfo OriginalMethod;
            internal MethodInfo ReplacementMethod;
            internal byte[] OriginalBytes;
            internal byte[] InstalledBytes;
            internal bool Conflict;
            internal bool RevertAttemptFailed;
            internal string Failure;
        }

        private sealed class WriteRecoveryException : InvalidOperationException
        {
            internal bool RollbackVerified { get; private set; }
            internal WriteRecoveryException(string message, bool rollbackVerified, Exception inner = null)
                : base(message, inner) { RollbackVerified = rollbackVerified; }
        }

        /// <summary>Redirects a method to an exact-signature replacement.</summary>
        public static void Patch(MethodInfo original, MethodInfo replacement)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));
            Patch(original, replacement, DefaultPatchId(original, replacement), "direct");
        }

        /// <summary>
        /// Redirects a method through the experimental backend using a stable shared patch ID.
        /// IDs are unique across the Forge detour registry; owner is a descriptive label only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static void Patch(MethodInfo original, MethodInfo replacement, string patchId, string owner)
        {
            ValidateMethodPair(original, replacement);
            if (string.IsNullOrWhiteSpace(patchId) || patchId.Length > 128 || !string.Equals(patchId, patchId.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("A patch ID between 1 and 128 non-padded characters is required.", nameof(patchId));
            if (string.IsNullOrWhiteSpace(owner) || owner.Length > 128 || !string.Equals(owner, owner.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("An owner label between 1 and 128 non-padded characters is required.", nameof(owner));
            if (!memory.Is64BitProcess)
                throw new PlatformNotSupportedException("Forge detours require a 64-bit process.");

            RuntimeHelpers.PrepareMethod(original.MethodHandle);
            RuntimeHelpers.PrepareMethod(replacement.MethodHandle);
            IntPtr address = original.MethodHandle.GetFunctionPointer();
            IntPtr replacementAddress = replacement.MethodHandle.GetFunctionPointer();
            if (address == IntPtr.Zero || replacementAddress == IntPtr.Zero)
                throw new InvalidOperationException("The JIT returned a null method address.");

            byte[] installed = CreateJump(replacementAddress);
            lock (Gate)
            {
                EnsureApplicationsEnabled();
                if (Active.ContainsKey(address))
                    throw new InvalidOperationException("A Forge detour is already tracked for this method.");
                foreach (var existing in Active.Values)
                    if (string.Equals(existing.Id, patchId, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Patch ID already exists in the Forge detour registry: " + patchId);
                EnsureSinglePageWrite(address, JumpInstructionLength);

                var state = new DetourState
                {
                    Id = patchId,
                    Owner = owner,
                    OriginalMethod = original,
                    ReplacementMethod = replacement,
                    OriginalBytes = ReadBytes(address),
                    InstalledBytes = installed
                };

                // Reserve ownership and rollback order before touching executable memory. If
                // a collection allocation fails after the write, the target could otherwise
                // be modified without a record that can later verify or restore it.
                Active.Add(address, state);
                try { ApplyOrder.Add(address); }
                catch
                {
                    Active.Remove(address);
                    throw;
                }

                try
                {
                    WriteExecutableBytes(address, installed, state.OriginalBytes);
                }
                catch (Exception error)
                {
                    // Remove the receipt only when the writer explicitly proved bytes,
                    // instruction-cache flush, and page-protection restoration all succeeded.
                    var recovery = error as WriteRecoveryException;
                    if (recovery != null && recovery.RollbackVerified)
                    {
                        RemoveState(address);
                    }
                    else state.Failure = error.Message;
                    throw;
                }
            }
        }

        /// <summary>
        /// Reverts a detour only when the target still contains the exact jump installed by Forge.
        /// A foreign modification is recorded as a conflict and is never overwritten.
        /// </summary>
        public static bool Unpatch(MethodInfo original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            return UnpatchMemory(original.MethodHandle.GetFunctionPointer());
        }

        /// <summary>Attempts reverse-order rollback for all tracked detours.</summary>
        public static void UnpatchAll()
        {
            List<IntPtr> snapshot;
            lock (Gate) snapshot = new List<IntPtr>(ApplyOrder);
            var failures = new List<Exception>();
            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                try
                {
                    bool shouldAttempt;
                    string priorFailure = null;
                    lock (Gate)
                    {
                        DetourState current;
                        shouldAttempt = Active.TryGetValue(snapshot[i], out current) && !current.Conflict &&
                            !current.RevertAttemptFailed && string.IsNullOrEmpty(current.Failure);
                        if (Active.TryGetValue(snapshot[i], out current)) priorFailure = current.Failure;
                        if (current != null && current.RevertAttemptFailed) priorFailure = "a previous revert attempt failed";
                    }
                    if (!shouldAttempt)
                    {
                        failures.Add(new InvalidOperationException(string.IsNullOrEmpty(priorFailure)
                            ? "Forge detour rollback skipped because target bytes changed outside Forge."
                            : "Forge detour rollback skipped because its prior write state is uncertain: " + priorFailure));
                        continue;
                    }
                    if (!UnpatchMemory(snapshot[i]))
                    {
                        lock (Gate)
                        {
                            DetourState state;
                            if (Active.TryGetValue(snapshot[i], out state) && state.Conflict)
                                failures.Add(new InvalidOperationException("Forge detour rollback skipped because target bytes changed outside Forge."));
                        }
                    }
                }
                catch (Exception error) { failures.Add(error); }
            }
            if (failures.Count > 0)
                throw new AggregateException("One or more Forge detours could not be safely reverted.", failures);
        }

        // Lifecycle callers preserve per-entry conflict/failure snapshots and must not throw
        // during module shutdown. Failed or conflicting entries remain available for review.
        internal static void UnpatchAllForLifecycle()
        {
            try { UnpatchAll(); }
            catch (AggregateException) { }
        }

        /// <summary>Gets immutable snapshots for every detour retained in the shared registry.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IReadOnlyList<ForgePatchSnapshot> GetTrackedSnapshots(string owner = null)
        {
            lock (Gate)
            {
                var snapshots = new List<ForgePatchSnapshot>(ApplyOrder.Count);
                for (int index = 0; index < ApplyOrder.Count; index++)
                {
                    DetourState state;
                    if (!Active.TryGetValue(ApplyOrder[index], out state) ||
                        (!string.IsNullOrWhiteSpace(owner) && !string.Equals(owner, state.Owner, StringComparison.OrdinalIgnoreCase))) continue;
                    bool installed = false;
                    try { installed = BytesEqual(ReadBytes(ApplyOrder[index]), state.InstalledBytes); }
                    catch { }
                    if (!installed && !state.Conflict && !state.RevertAttemptFailed && string.IsNullOrEmpty(state.Failure)) state.Conflict = true;
                    var lifecycle = (!string.IsNullOrEmpty(state.Failure) || state.RevertAttemptFailed) ? ForgePatchState.Failed :
                        state.Conflict ? ForgePatchState.Conflict : ForgePatchState.Applied;
                    snapshots.Add(new ForgePatchSnapshot(state.Id, state.Owner, Identity(state.OriginalMethod),
                        Identity(state.ReplacementMethod), lifecycle, lifecycle == ForgePatchState.Applied && installed));
                }
                return snapshots.AsReadOnly();
            }
        }

        /// <summary>
        /// Validates and reserves a complete compatibility batch while holding the shared
        /// registry lock. Every declaration and receipt is prepared before the first write;
        /// competing direct/service applications cannot enter between batch members.
        /// </summary>
        internal static void PatchBatch(MethodInfo[] originals, MethodInfo[] replacements, string[] patchIds,
            string owner, byte[][] originalByteCopies)
        {
            if (originals == null) throw new ArgumentNullException(nameof(originals));
            if (replacements == null) throw new ArgumentNullException(nameof(replacements));
            if (patchIds == null) throw new ArgumentNullException(nameof(patchIds));
            if (originalByteCopies == null) throw new ArgumentNullException(nameof(originalByteCopies));
            if (originals.Length == 0 || originals.Length != replacements.Length || originals.Length != patchIds.Length || originals.Length != originalByteCopies.Length)
                throw new ArgumentException("A non-empty patch batch requires equally sized target, replacement, ID, and receipt arrays.");
            if (string.IsNullOrWhiteSpace(owner) || owner.Length > 128 || !string.Equals(owner, owner.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("An owner label between 1 and 128 non-padded characters is required.", nameof(owner));
            if (!memory.Is64BitProcess) throw new PlatformNotSupportedException("Forge detours require a 64-bit process.");

            var addresses = new IntPtr[originals.Length];
            var replacementAddresses = new IntPtr[originals.Length];
            for (int index = 0; index < originals.Length; index++)
            {
                ValidateMethodPair(originals[index], replacements[index]);
                string id = patchIds[index];
                if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || !string.Equals(id, id.Trim(), StringComparison.Ordinal))
                    throw new ArgumentException("Every patch ID must contain 1 to 128 non-padded characters.", nameof(patchIds));
                if (originalByteCopies[index] != null)
                    throw new ArgumentException("Batch receipt slots must be empty before application.", nameof(originalByteCopies));
                RuntimeHelpers.PrepareMethod(originals[index].MethodHandle);
                RuntimeHelpers.PrepareMethod(replacements[index].MethodHandle);
                addresses[index] = originals[index].MethodHandle.GetFunctionPointer();
                replacementAddresses[index] = replacements[index].MethodHandle.GetFunctionPointer();
                if (addresses[index] == IntPtr.Zero || replacementAddresses[index] == IntPtr.Zero)
                    throw new InvalidOperationException("The JIT returned a null method address.");
            }

            lock (Gate)
            {
                EnsureApplicationsEnabled();
                if (!memory.Is64BitProcess) throw new PlatformNotSupportedException("Forge detours require a 64-bit process.");
                var batchIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var batchTargets = new HashSet<IntPtr>();
                var states = new DetourState[originals.Length];
                var installedImages = new byte[originals.Length][];
                for (int index = 0; index < originals.Length; index++)
                {
                    string id = patchIds[index];
                    IntPtr address = addresses[index];
                    if (!batchIds.Add(id)) throw new InvalidOperationException("Patch ID is duplicated within the batch: " + id);
                    if (!batchTargets.Add(address)) throw new InvalidOperationException("Multiple batch declarations target the same method: " + Identity(originals[index]));
                    if (Active.ContainsKey(address)) throw new InvalidOperationException("A Forge detour is already tracked for this method.");
                    foreach (var active in Active.Values)
                        if (string.Equals(active.Id, id, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Patch ID already exists in the Forge detour registry: " + id);
                }

                // Reject every unsupported span before reading any original bytes or
                // publishing any reservation, so callers can retry the same batch cleanly.
                for (int index = 0; index < addresses.Length; index++)
                    EnsureSinglePageWrite(addresses[index], JumpInstructionLength);

                for (int index = 0; index < originals.Length; index++)
                {
                    IntPtr address = addresses[index];
                    byte[] originalBytes = ReadBytes(address);
                    byte[] installedBytes = CreateJump(replacementAddresses[index]);
                    states[index] = new DetourState
                    {
                        Id = patchIds[index],
                        Owner = owner,
                        OriginalMethod = originals[index],
                        ReplacementMethod = replacements[index],
                        OriginalBytes = originalBytes,
                        InstalledBytes = installedBytes
                    };
                    installedImages[index] = installedBytes;
                    originalByteCopies[index] = (byte[])originalBytes.Clone();
                }

                // Replace the private dictionary with a pre-sized copy and reserve the
                // reverse-order list before publishing any executable bytes.
                var preparedActive = new Dictionary<IntPtr, DetourState>(Active.Count + states.Length);
                foreach (var entry in Active) preparedActive.Add(entry.Key, entry.Value);
                Active = preparedActive;
                ApplyOrder.Capacity = Math.Max(ApplyOrder.Capacity, ApplyOrder.Count + states.Length);

                int registered = 0;
                try
                {
                    for (; registered < states.Length; registered++)
                    {
                        Active.Add(addresses[registered], states[registered]);
                        ApplyOrder.Add(addresses[registered]);
                    }
                }
                catch
                {
                    // Include the current slot: Active.Add can succeed before adding the
                    // matching rollback-order entry throws.
                    for (int index = Math.Min(registered, states.Length - 1); index >= 0; index--)
                        RemoveState(addresses[index]);
                    throw;
                }

                int currentIndex = 0;
                try
                {
                    for (; currentIndex < states.Length; currentIndex++)
                    {
                        // A third-party patcher can alter code without taking Forge's
                        // registry lock. Never overwrite bytes that changed after preflight.
                        if (!BytesEqual(ReadBytes(addresses[currentIndex]), states[currentIndex].OriginalBytes))
                        {
                            states[currentIndex].Conflict = true;
                            throw new InvalidOperationException("Target bytes changed after batch preflight; the foreign bytes were left untouched.");
                        }
                        WriteExecutableBytes(addresses[currentIndex], installedImages[currentIndex], states[currentIndex].OriginalBytes);
                    }
                }
                catch (Exception applyError)
                {
                    var rollbackFailures = new List<Exception>();
                    var failedRecovery = applyError as WriteRecoveryException;
                    if (failedRecovery != null && failedRecovery.RollbackVerified)
                        RemoveState(addresses[currentIndex]);
                    else if (!states[currentIndex].Conflict)
                        states[currentIndex].Failure = applyError.Message;
                    else
                        rollbackFailures.Add(new InvalidOperationException("Batch rollback retained " + states[currentIndex].Id + " as a foreign-byte conflict."));

                    for (int index = currentIndex - 1; index >= 0; index--)
                    {
                        try
                        {
                            if (!UnpatchMemory(addresses[index]))
                            {
                                DetourState retained;
                                if (Active.TryGetValue(addresses[index], out retained))
                                    rollbackFailures.Add(new InvalidOperationException("Batch rollback retained " + retained.Id + " as " +
                                        (retained.Conflict ? "a foreign-byte conflict." : "an uncertain detour.")));
                            }
                        }
                        catch (Exception rollbackError) { rollbackFailures.Add(rollbackError); }
                    }
                    for (int index = currentIndex + 1; index < states.Length; index++) RemoveState(addresses[index]);

                    if (rollbackFailures.Count > 0)
                    {
                        rollbackFailures.Insert(0, applyError);
                        throw new AggregateException("Patch batch application failed and rollback was incomplete; inspect retained records.", rollbackFailures);
                    }
                    throw new InvalidOperationException("Patch batch was rejected; prior writes were safely reverted.", applyError);
                }
            }
        }

        /// <summary>Reverts one shared-registry patch by its unique ID.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ForgePatchRevertResult RevertById(string patchId)
        {
            if (string.IsNullOrWhiteSpace(patchId)) throw new ArgumentException("A patch ID is required.", nameof(patchId));
            IntPtr address = IntPtr.Zero;
            lock (Gate)
            {
                foreach (var pair in Active)
                    if (string.Equals(pair.Value.Id, patchId, StringComparison.OrdinalIgnoreCase))
                    {
                        if (address != IntPtr.Zero)
                            return new ForgePatchRevertResult(patchId, ForgePatchState.Failed, false, "Patch ID is ambiguous in the shared detour registry.");
                        address = pair.Key;
                    }
            }
            if (address == IntPtr.Zero)
                return new ForgePatchRevertResult(patchId, ForgePatchState.Failed, false, "No shared Forge detour exists for this ID.");
            try
            {
                if (UnpatchMemory(address)) return new ForgePatchRevertResult(patchId, ForgePatchState.Reverted, true, "Original bytes restored and verified.");
                var snapshot = GetTrackedSnapshots().FirstOrDefault(item => string.Equals(item.PatchId, patchId, StringComparison.OrdinalIgnoreCase));
                return new ForgePatchRevertResult(patchId, snapshot == null ? ForgePatchState.Failed : snapshot.State, false,
                    snapshot != null && snapshot.State == ForgePatchState.Conflict
                        ? "Target bytes changed outside Forge; the foreign bytes were left untouched."
                        : "Forge could not confirm a safe reversion.");
            }
            catch (Exception error)
            {
                var snapshot = GetTrackedSnapshots().FirstOrDefault(item => string.Equals(item.PatchId, patchId, StringComparison.OrdinalIgnoreCase));
                return new ForgePatchRevertResult(patchId, snapshot == null ? ForgePatchState.Failed : snapshot.State, false, error.Message);
            }
        }

        /// <summary>Reverts shared-registry entries for one descriptive owner label.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IReadOnlyList<ForgePatchRevertResult> RevertOwner(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("An owner label is required.", nameof(owner));
            var ids = GetTrackedSnapshots(owner).Select(item => item.PatchId).Reverse().ToArray();
            var results = new List<ForgePatchRevertResult>(ids.Length);
            foreach (string id in ids) results.Add(RevertById(id));
            return results.AsReadOnly();
        }

        /// <summary>Attempts one reverse-order pass over every shared-registry detour.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IReadOnlyList<ForgePatchRevertResult> RevertAllTracked()
        {
            var before = GetTrackedSnapshots();
            try { UnpatchAll(); }
            catch (AggregateException) { /* Per-entry snapshots below report retained failures and conflicts. */ }
            var remaining = GetTrackedSnapshots().ToDictionary(item => item.PatchId, StringComparer.OrdinalIgnoreCase);
            var results = new List<ForgePatchRevertResult>(before.Count);
            foreach (var prior in before)
            {
                ForgePatchSnapshot current;
                if (!remaining.TryGetValue(prior.PatchId, out current))
                    results.Add(new ForgePatchRevertResult(prior.PatchId, ForgePatchState.Reverted, true, "Original bytes restored and verified."));
                else
                    results.Add(new ForgePatchRevertResult(prior.PatchId, current.State, false,
                        current.State == ForgePatchState.Conflict
                            ? "Target bytes changed outside Forge; the foreign bytes were left untouched."
                            : "Forge could not confirm a safe reversion; the record remains available for review."));
            }
            return results.AsReadOnly();
        }

        /// <summary>Returns true only when the tracked target still has Forge's exact jump bytes.</summary>
        public static bool IsPatched(MethodInfo method)
        {
            if (method == null) return false;
            IntPtr address;
            try { address = method.MethodHandle.GetFunctionPointer(); }
            catch { return false; }
            lock (Gate)
            {
                DetourState state;
                return Active.TryGetValue(address, out state) &&
                    BytesEqual(ReadBytes(address), state.InstalledBytes);
            }
        }

        public static bool Verify(MethodInfo method, out string status)
        {
            if (method == null) { status = "missing-target"; return false; }
            IntPtr address;
            try { address = method.MethodHandle.GetFunctionPointer(); }
            catch { status = "unavailable-target"; return false; }
            lock (Gate)
            {
                DetourState state;
                if (!Active.TryGetValue(address, out state)) { status = "not-tracked"; return false; }
                if (!string.IsNullOrEmpty(state.Failure))
                {
                    status = "write-state-uncertain";
                    return false;
                }
                if (state.RevertAttemptFailed)
                {
                    status = "revert-state-uncertain";
                    return false;
                }
                byte[] current;
                try { current = ReadBytes(address); }
                catch { status = "unreadable-target"; return false; }
                if (BytesEqual(current, state.InstalledBytes)) { status = "intact"; return true; }
                if (BytesEqual(current, state.OriginalBytes))
                {
                    status = "unexpected-original-bytes";
                    return false;
                }
                state.Conflict = true;
                status = "foreign-bytes";
                return false;
            }
        }

        internal static bool IsTracked(MethodInfo method)
        {
            if (method == null) return false;
            try
            {
                lock (Gate) return Active.ContainsKey(method.MethodHandle.GetFunctionPointer());
            }
            catch { return false; }
        }

        internal static string GetTrackedPatchId(MethodInfo method)
        {
            if (method == null) return null;
            try
            {
                lock (Gate)
                {
                    DetourState state;
                    return Active.TryGetValue(method.MethodHandle.GetFunctionPointer(), out state) ? state.Id : null;
                }
            }
            catch { return null; }
        }

        internal static bool IsTrackedPatch(MethodInfo method, string patchId)
        {
            if (method == null || string.IsNullOrWhiteSpace(patchId)) return false;
            try
            {
                lock (Gate)
                {
                    DetourState state;
                    return Active.TryGetValue(method.MethodHandle.GetFunctionPointer(), out state) &&
                        string.Equals(state.Id, patchId, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { return false; }
        }

        internal static void ValidatePair(MethodInfo original, MethodInfo replacement)
        {
            ValidateMethodPair(original, replacement);
        }

        public static byte[] GetOriginalBytes(MethodInfo method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            IntPtr address = method.MethodHandle.GetFunctionPointer();
            lock (Gate)
            {
                DetourState state;
                if (!Active.TryGetValue(address, out state)) throw new InvalidOperationException("No Forge detour is tracked for this method.");
                return (byte[])state.OriginalBytes.Clone();
            }
        }

        internal static byte[] GetInstalledBytes(MethodInfo method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            IntPtr address = method.MethodHandle.GetFunctionPointer();
            lock (Gate)
            {
                DetourState state;
                if (!Active.TryGetValue(address, out state)) throw new InvalidOperationException("No Forge detour is tracked for this method.");
                return (byte[])state.InstalledBytes.Clone();
            }
        }

        public static bool VerifyRestored(MethodInfo method, byte[] originalBytes)
        {
            if (method == null || originalBytes == null || originalBytes.Length != JumpInstructionLength) return false;
            IntPtr address;
            try { address = method.MethodHandle.GetFunctionPointer(); }
            catch { return false; }
            lock (Gate)
            {
                DetourState active;
                if (Active.TryGetValue(address, out active)) return false;
                return BytesEqual(ReadBytes(address), originalBytes);
            }
        }

        internal static bool OriginalBytesMatch(MethodInfo method, byte[] expected)
        {
            if (method == null || expected == null || expected.Length != JumpInstructionLength) return false;
            IntPtr address = method.MethodHandle.GetFunctionPointer();
            lock (Gate)
            {
                DetourState state;
                return Active.TryGetValue(address, out state) && BytesEqual(state.OriginalBytes, expected);
            }
        }

        // Friend test assemblies use a deterministic fake adapter. This never changes the
        // production public API and is restored to native mode by each test fixture.
        internal static void SetMemoryAdapterForTests(IExecutableMemoryAdapter adapter)
        {
            lock (Gate)
            {
                if (Active.Count != 0) throw new InvalidOperationException("Cannot replace the memory adapter while patches are tracked.");
                memory = adapter ?? new NativeExecutableMemoryAdapter();
            }
        }

        internal static void SetSystemPageSizeForTests(ulong? pageSize)
        {
            if (pageSize == 0) throw new ArgumentOutOfRangeException(nameof(pageSize), "A synthetic page size must be positive.");
            lock (Gate) systemPageSizeOverrideForTests = pageSize;
        }

        internal static void WriteExecutableBytesForTests(IntPtr address, byte[] bytes, byte[] rollbackBytes)
        {
            WriteExecutableBytes(address, bytes, rollbackBytes);
        }

        private static bool UnpatchMemory(IntPtr address)
        {
            lock (Gate)
            {
                DetourState state;
                if (!Active.TryGetValue(address, out state)) return false;
                byte[] current = ReadBytes(address);
                if (BytesEqual(current, state.OriginalBytes))
                {
                    if (!string.IsNullOrEmpty(state.Failure) || state.RevertAttemptFailed) return false;
                    RemoveState(address);
                    return true;
                }
                if (!BytesEqual(current, state.InstalledBytes))
                {
                    state.Conflict = true;
                    return false;
                }

                try { WriteExecutableBytes(address, state.OriginalBytes, state.InstalledBytes); }
                catch
                {
                    state.RevertAttemptFailed = true;
                    throw;
                }
                RemoveState(address);
                return true;
            }
        }

        private static void RemoveState(IntPtr address)
        {
            Active.Remove(address);
            ApplyOrder.Remove(address);
        }

        private static void WriteExecutableBytes(IntPtr address, byte[] bytes, byte[] rollbackBytes)
        {
            if (rollbackBytes == null || rollbackBytes.Length != bytes.Length)
                throw new ArgumentException("A same-length rollback image is required.", nameof(rollbackBytes));
            EnsureSinglePageWrite(address, bytes.Length);
            if (!memory.TryProtect(address, bytes.Length, PageExecuteReadWrite, out uint oldProtect, out int protectError))
                throw new WriteRecoveryException("VirtualProtect could not make the method writable (Win32 " + protectError + "). No write was made.", true);

            Exception writeFailure = null;
            try
            {
                // The expected rollback image is the only byte sequence Forge may
                // replace. This catches a third-party change that races the registry lock.
                if (!BytesEqual(memory.Read(address, rollbackBytes.Length), rollbackBytes))
                    throw new InvalidOperationException("Target bytes changed before the executable write; the current image was left untouched.");
                memory.Write(address, bytes);
                if (!memory.Flush(address, bytes.Length, out int flushError))
                    throw new InvalidOperationException("FlushInstructionCache failed (Win32 " + flushError + ").");
                if (!BytesEqual(memory.Read(address, bytes.Length), bytes))
                    throw new InvalidOperationException("Executable memory did not retain the exact requested bytes.");
            }
            catch (Exception error) { writeFailure = error; }

            bool protectionRestored = false;
            int protectionError = 0;
            Exception protectionFailure = null;
            try { protectionRestored = memory.TryProtect(address, bytes.Length, oldProtect, out _, out protectionError); }
            catch (Exception error) { protectionFailure = error; }
            if (writeFailure == null && protectionRestored) return;

            int restoreProtectionError = protectionRestored ? 0 : protectionError;
            var rollbackFailures = new List<Exception>();
            byte[] currentBytes = null;
            try { currentBytes = memory.Read(address, rollbackBytes.Length); }
            catch (Exception error) { rollbackFailures.Add(new InvalidOperationException("Could not inspect executable bytes before rollback.", error)); }

            bool currentIsDesired = currentBytes != null && BytesEqual(currentBytes, bytes);
            bool currentIsRollback = currentBytes != null && BytesEqual(currentBytes, rollbackBytes);
            if (!currentIsDesired && !currentIsRollback)
                rollbackFailures.Add(new InvalidOperationException("Executable bytes no longer match either expected Forge image; rollback was not written."));

            bool rollbackWritable = false;
            try
            {
                if (currentIsDesired)
                {
                    rollbackWritable = memory.TryProtect(address, bytes.Length, PageExecuteReadWrite, out _, out int reopenError);
                    if (!rollbackWritable)
                        rollbackFailures.Add(new InvalidOperationException("Could not reopen target memory for rollback (Win32 " + reopenError + ")."));
                }
            }
            catch (Exception error) { rollbackFailures.Add(error); }

            if (currentIsRollback)
            {
                try
                {
                    if (!memory.Flush(address, bytes.Length, out int rollbackFlushError))
                        rollbackFailures.Add(new InvalidOperationException("FlushInstructionCache failed while verifying the unchanged rollback image (Win32 " + rollbackFlushError + ")."));
                }
                catch (Exception error) { rollbackFailures.Add(error); }
            }
            else if (rollbackWritable)
            {
                try
                {
                    memory.Write(address, rollbackBytes);
                    if (!memory.Flush(address, bytes.Length, out int rollbackFlushError))
                        rollbackFailures.Add(new InvalidOperationException("FlushInstructionCache failed while rolling back (Win32 " + rollbackFlushError + ")."));
                }
                catch (Exception error) { rollbackFailures.Add(error); }
            }

            // Attempt to restore protection even if reopening or writing the rollback failed.
            bool rollbackProtectionRestored = false;
            try
            {
                rollbackProtectionRestored = memory.TryProtect(address, bytes.Length, oldProtect, out _, out int rollbackProtectError);
                if (!rollbackProtectionRestored)
                    rollbackFailures.Add(new InvalidOperationException("Could not restore page protection after rollback (Win32 " + rollbackProtectError + ")."));
            }
            catch (Exception error) { rollbackFailures.Add(error); }

            bool rollbackBytesRestored = false;
            try { rollbackBytesRestored = BytesEqual(memory.Read(address, rollbackBytes.Length), rollbackBytes); }
            catch (Exception error) { rollbackFailures.Add(error); }
            if (!rollbackBytesRestored)
                rollbackFailures.Add(new InvalidOperationException("Rollback bytes did not match the original image after recovery."));

            if (rollbackFailures.Count > 0 || !rollbackProtectionRestored)
            {
                if (writeFailure != null) rollbackFailures.Insert(0, writeFailure);
                else rollbackFailures.Insert(0, new InvalidOperationException("Page protection restore failed (Win32 " + restoreProtectionError + ")."));
                if (protectionFailure != null) rollbackFailures.Insert(1, protectionFailure);
                throw new AggregateException("Detour write failed and rollback could not be verified; target state is unknown.",
                    rollbackFailures);
            }
            throw new WriteRecoveryException(writeFailure == null
                ? "Page protection restore failed; original bytes, instruction cache, and protection were restored (Win32 " + restoreProtectionError + ")."
                : "Detour write failed; rollback bytes, instruction cache, and protection were restored. " + writeFailure.Message,
                true, writeFailure);
        }

        private static void EnsureSinglePageWrite(IntPtr address, int count)
        {
            if (address == IntPtr.Zero) throw new ArgumentException("Executable write address cannot be zero.", nameof(address));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Executable write length must be positive.");

            ulong first = unchecked((ulong)address.ToInt64());
            ulong length = (ulong)count;
            if (first > ulong.MaxValue - (length - 1))
                throw new InvalidOperationException("Detour write address range overflowed; no write was made.");

            ulong last = first + length - 1;
            ulong pageSize = systemPageSizeOverrideForTests ?? (ulong)Environment.SystemPageSize;
            if (pageSize == 0 || first / pageSize != last / pageSize)
                throw new WriteRecoveryException("Detour writes that cross a system page boundary are unsupported; no write was made.", true);
        }

        private static string DefaultPatchId(MethodInfo original, MethodInfo replacement)
        {
            return "direct:" + ShortHash(SignatureIdentity(original) + "->" + SignatureIdentity(replacement));
        }

        internal static string SignatureIdentity(MethodInfo method)
        {
            var builder = new StringBuilder();
            builder.Append(Identity(method)).Append('`').Append(method.IsGenericMethod ? method.GetGenericArguments().Length : 0).Append('(');
            foreach (var parameter in method.GetParameters()) builder.Append(parameter.ParameterType.AssemblyQualifiedName ?? parameter.ParameterType.FullName ?? parameter.ParameterType.Name).Append(';');
            builder.Append(")->").Append(method.ReturnType.AssemblyQualifiedName ?? method.ReturnType.FullName ?? method.ReturnType.Name)
                .Append(method.IsStatic ? ":static" : ":instance");
            return builder.ToString();
        }

        internal static string ShortHash(string value)
        {
            using (var hash = SHA256.Create())
            {
                byte[] digest = hash.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var builder = new StringBuilder(32);
                for (int index = 0; index < 16; index++) builder.Append(digest[index].ToString("x2"));
                return builder.ToString();
            }
        }

        private static string Identity(MethodInfo method)
        {
            return method == null ? "?" : (method.DeclaringType == null ? "?" : method.DeclaringType.FullName) + "." + method.Name;
        }

        private static byte[] ReadBytes(IntPtr address)
        {
            return memory.Read(address, JumpInstructionLength);
        }

        private static byte[] CreateJump(IntPtr destination)
        {
            var bytes = new byte[JumpInstructionLength];
            bytes[0] = 0x49;
            bytes[1] = 0xBB;
            Array.Copy(BitConverter.GetBytes(destination.ToInt64()), 0, bytes, 2, 8);
            bytes[10] = 0x41;
            bytes[11] = 0xFF;
            bytes[12] = 0xE3;
            return bytes;
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }

        private static void ValidateMethodPair(MethodInfo original, MethodInfo replacement)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));
            if (original.IsAbstract || replacement.IsAbstract)
                throw new NotSupportedException("Abstract methods cannot be detour targets or replacements.");
            if (original.ContainsGenericParameters || replacement.ContainsGenericParameters ||
                original.IsGenericMethod || replacement.IsGenericMethod ||
                original.DeclaringType == null || replacement.DeclaringType == null ||
                original.DeclaringType.ContainsGenericParameters || replacement.DeclaringType.ContainsGenericParameters)
                throw new NotSupportedException("Generic or open methods are not supported by the experimental detour backend.");
            if (original.IsStatic != replacement.IsStatic || original.CallingConvention != replacement.CallingConvention ||
                original.ReturnType != replacement.ReturnType)
                throw new ArgumentException("Target and replacement must have identical staticness, calling convention, and return type.");
            ParameterInfo[] originalParameters = original.GetParameters();
            ParameterInfo[] replacementParameters = replacement.GetParameters();
            if (originalParameters.Length != replacementParameters.Length)
                throw new ArgumentException("Target and replacement parameter counts must match exactly.");
            for (int i = 0; i < originalParameters.Length; i++)
            {
                if (originalParameters[i].ParameterType != replacementParameters[i].ParameterType ||
                    originalParameters[i].IsOut != replacementParameters[i].IsOut ||
                    originalParameters[i].IsIn != replacementParameters[i].IsIn)
                    throw new ArgumentException("Target and replacement parameter signatures must match exactly.");
            }
        }
    }
}
