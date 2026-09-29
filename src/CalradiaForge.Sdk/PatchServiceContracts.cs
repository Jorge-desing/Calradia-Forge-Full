using System;
using System.Collections.Generic;
using System.Reflection;

namespace CalradiaForge.Sdk
{
    /// <summary>Describes the lifecycle of an explicitly applied method replacement.</summary>
    public enum ForgePatchState
    {
        /// <summary>The replacement is installed and its bytes match the recorded patch.</summary>
        Applied,
        /// <summary>The original method bytes have been restored.</summary>
        Reverted,
        /// <summary>Another component changed the method bytes; Forge left them untouched.</summary>
        Conflict,
        /// <summary>Application or restoration failed and the final memory state is not confirmed.</summary>
        Failed
    }

    /// <summary>An immutable point-in-time description of a Forge-owned patch record.</summary>
    public sealed class ForgePatchSnapshot
    {
        public ForgePatchSnapshot(
            string patchId,
            string owner,
            string targetMethod,
            string replacementMethod,
            ForgePatchState state,
            bool isIntact)
        {
            if (string.IsNullOrWhiteSpace(patchId)) throw new ArgumentException("A patch ID is required.", nameof(patchId));
            if (!Enum.IsDefined(typeof(ForgePatchState), state)) throw new ArgumentOutOfRangeException(nameof(state));

            PatchId = patchId;
            Owner = owner ?? string.Empty;
            TargetMethod = targetMethod ?? string.Empty;
            ReplacementMethod = replacementMethod ?? string.Empty;
            State = state;
            IsIntact = isIntact;
        }

        /// <summary>Gets the stable patch ID.</summary>
        public string PatchId { get; }

        /// <summary>Gets the descriptive owner label. It is not an authorization boundary.</summary>
        public string Owner { get; }

        /// <summary>Gets the target method identity captured by the host.</summary>
        public string TargetMethod { get; }

        /// <summary>Gets the replacement method identity captured by the host.</summary>
        public string ReplacementMethod { get; }

        /// <summary>Gets the patch lifecycle state at the time this snapshot was created.</summary>
        public ForgePatchState State { get; }

        /// <summary>
        /// Gets whether current bytes match the state recorded by this entry. For an Applied entry,
        /// this means the installed replacement bytes match; for Reverted, it means original bytes match.
        /// </summary>
        public bool IsIntact { get; }
    }

    /// <summary>An immutable result from checking one patch's current memory state.</summary>
    public sealed class ForgePatchVerification
    {
        public ForgePatchVerification(string patchId, ForgePatchState state, bool isIntact, string detail)
        {
            if (string.IsNullOrWhiteSpace(patchId)) throw new ArgumentException("A patch ID is required.", nameof(patchId));
            if (!Enum.IsDefined(typeof(ForgePatchState), state)) throw new ArgumentOutOfRangeException(nameof(state));

            PatchId = patchId;
            State = state;
            IsIntact = isIntact;
            Detail = detail ?? string.Empty;
        }

        /// <summary>Gets the patch ID that was verified.</summary>
        public string PatchId { get; }
        /// <summary>Gets the lifecycle state observed during verification.</summary>
        public ForgePatchState State { get; }
        /// <summary>True only when current bytes match the memory state expected for <see cref="State"/>.</summary>
        public bool IsIntact { get; }
        /// <summary>Gets a bounded host-provided explanation of the verification outcome.</summary>
        public string Detail { get; }
    }

    /// <summary>An immutable result from a patch revert request.</summary>
    public sealed class ForgePatchRevertResult
    {
        public ForgePatchRevertResult(string patchId, ForgePatchState state, bool isReverted, string detail)
        {
            if (string.IsNullOrWhiteSpace(patchId)) throw new ArgumentException("A patch ID is required.", nameof(patchId));
            if (!Enum.IsDefined(typeof(ForgePatchState), state)) throw new ArgumentOutOfRangeException(nameof(state));
            if (isReverted && state != ForgePatchState.Reverted)
                throw new ArgumentException("A reverted result must have Reverted state.", nameof(isReverted));

            PatchId = patchId;
            State = state;
            IsReverted = isReverted;
            Detail = detail ?? string.Empty;
        }

        /// <summary>Gets the patch ID that was reverted.</summary>
        public string PatchId { get; }
        /// <summary>Gets the lifecycle state observed after the request.</summary>
        public ForgePatchState State { get; }
        /// <summary>True when the current recorded state is Reverted, including repeated idempotent requests.</summary>
        public bool IsReverted { get; }
        /// <summary>Gets a bounded host-provided explanation of the revert outcome.</summary>
        public string Detail { get; }
    }

    /// <summary>
    /// Optional capability for explicit method replacement. Implementations must validate exact signatures
    /// before writing and must report uncertain writes as Failed rather than claiming success.
    /// </summary>
    /// <remarks>
    /// This capability does not make an in-process code write safe while another thread may execute the target.
    /// Callers must avoid concurrent execution; the backend remains experimental without thread quiescence.
    /// Owner values are descriptive labels, not access-control credentials.
    /// </remarks>
    public interface IForgePatchService
    {
        /// <summary>Explicitly applies one method replacement and returns a handle for verification and rollback.</summary>
        /// <param name="patchId">Stable unique ID for this patch record.</param>
        /// <param name="owner">Descriptive owner label; not a security boundary.</param>
        /// <param name="target">Exact target method, already resolved by the caller.</param>
        /// <param name="replacement">Replacement method with a compatible exact signature.</param>
        /// <exception cref="ArgumentException">The ID, owner, or method pair is invalid.</exception>
        /// <exception cref="InvalidOperationException">The host is disconnected, the ID conflicts, or the backend cannot safely apply the request.</exception>
        IForgePatchHandle ApplyMethodReplacement(string patchId, string owner, MethodInfo target, MethodInfo replacement);

        /// <summary>Gets immutable point-in-time snapshots, optionally filtered by the descriptive owner label.</summary>
        IReadOnlyList<ForgePatchSnapshot> GetSnapshots(string owner = null);

        /// <summary>Checks whether the recorded bytes for one patch still match its expected lifecycle state.</summary>
        ForgePatchVerification Verify(string patchId);

        /// <summary>Requests an idempotent revert of one patch. Foreign bytes are reported as Conflict and left untouched.</summary>
        ForgePatchRevertResult Revert(string patchId);

        /// <summary>Requests reversion of all patches bearing this descriptive owner label.</summary>
        IReadOnlyList<ForgePatchRevertResult> RevertOwner(string owner);

        /// <summary>Requests reversion of all patches in reverse application order.</summary>
        IReadOnlyList<ForgePatchRevertResult> RevertAll();

        /// <summary>Stops new applications and performs best-effort reverse-order reversion during host disconnect.</summary>
        void Disconnect();
    }

    /// <summary>Optional lifecycle capability for patch services that can be safely reused after a host reconnect.</summary>
    /// <remarks>Keep this separate so existing implementations of <see cref="IForgePatchService"/> remain compatible.</remarks>
    public interface IForgePatchServiceLifecycle
    {
        /// <summary>Reopens patch applications only when all prior records are safely reverted and verified.</summary>
        /// <exception cref="InvalidOperationException">A prior patch is still applied, conflicted, failed, or cannot be verified as reverted.</exception>
        void Reconnect();
    }

    /// <summary>A stable handle for inspecting and idempotently reverting one explicitly applied patch.</summary>
    /// <remarks>Disposing the handle performs a best-effort idempotent revert.</remarks>
    public interface IForgePatchHandle : IDisposable
    {
        /// <summary>Gets a fresh immutable point-in-time snapshot of this patch.</summary>
        ForgePatchSnapshot Snapshot { get; }

        /// <summary>Verifies the current state even after the service has disconnected.</summary>
        ForgePatchVerification Verify();

        /// <summary>Requests an idempotent revert; a conflict is returned rather than overwritten.</summary>
        ForgePatchRevertResult Revert();

    }
}
