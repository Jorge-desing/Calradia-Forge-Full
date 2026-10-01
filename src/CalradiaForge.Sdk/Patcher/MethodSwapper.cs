using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CalradiaForge.Sdk.Patcher
{
    /// <summary>
    /// Compatibility adapter for legacy callers. All writes and ownership tracking are routed
    /// through ForgeDetour so the SDK has one native detour registry and one integrity policy.
    /// </summary>
    public static class MethodSwapper
    {
        private sealed class DetourReceipt
        {
            internal readonly MethodInfo Target;
            internal readonly string PatchId;
            internal readonly object GenerationToken;
            internal readonly byte[] OriginalBytes;

            internal DetourReceipt(MethodInfo target, ForgeDetour.InstallationReceipt receipt, byte[] originalBytes)
            {
                Target = target;
                PatchId = receipt.PatchId;
                GenerationToken = receipt.GenerationToken;
                OriginalBytes = (byte[])originalBytes.Clone();
            }
        }

        private static readonly ConditionalWeakTable<byte[], DetourReceipt> receipts = new ConditionalWeakTable<byte[], DetourReceipt>();

        /// <summary>
        /// Installs the legacy x64 jump and returns a copy of its original instruction bytes.
        /// Writes are experimental and are not synchronized with threads executing the target.
        /// </summary>
        public static byte[] DetourMethod(MethodInfo target, MethodInfo replacement)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));
            ForgeDetour.InstallationReceipt receipt = ForgeDetour.Patch(target, replacement,
                "direct:" + ForgeDetour.ShortHash(ForgeDetour.SignatureIdentity(target) + "->" + ForgeDetour.SignatureIdentity(replacement)), "direct");
            return CreateReceipt(target, receipt);
        }

        internal static byte[] DetourMethod(MethodInfo target, MethodInfo replacement, string patchId, string owner)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            ForgeDetour.InstallationReceipt receipt = ForgeDetour.Patch(target, replacement, patchId, owner);
            return CreateReceipt(target, receipt);
        }

        /// <summary>
        /// Restores only the installation generation associated with the exact byte-array
        /// instance returned by <see cref="DetourMethod(MethodInfo, MethodInfo)"/>. A cloned
        /// or stale receipt is rejected, and code changed by another component is never overwritten.
        /// </summary>
        public static void RestoreMethod(MethodInfo target, byte[] originalBytes)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (originalBytes == null || originalBytes.Length != 13)
                throw new ArgumentException("Invalid backup bytes.", nameof(originalBytes));
            DetourReceipt receipt;
            if (!receipts.TryGetValue(originalBytes, out receipt) || receipt.Target != target)
                throw new InvalidOperationException("The backup is not a MethodSwapper receipt for this target and installation generation.");
            // The public byte array is only an opaque receipt key. Callers can mutate its
            // contents, so all integrity/reversion checks must use our private snapshot.
            if (!ForgeDetour.UnpatchReceipt(target, receipt.PatchId, receipt.GenerationToken, receipt.OriginalBytes))
                throw new InvalidOperationException("The MethodSwapper receipt is stale or target bytes changed; the active detour was not reverted.");
            receipts.Remove(originalBytes);
        }

        internal static object GetGenerationToken(MethodInfo target, byte[] originalBytes, string patchId)
        {
            DetourReceipt receipt;
            if (target == null || originalBytes == null || !receipts.TryGetValue(originalBytes, out receipt) ||
                receipt.Target != target || !string.Equals(receipt.PatchId, patchId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The MethodSwapper receipt no longer identifies the active detour generation.");
            if (!ForgeDetour.IsTrackedReceipt(target, receipt.PatchId, receipt.GenerationToken, receipt.OriginalBytes))
                throw new InvalidOperationException("The MethodSwapper receipt no longer identifies the active detour generation.");
            return receipt.GenerationToken;
        }

        private static byte[] CreateReceipt(MethodInfo target, ForgeDetour.InstallationReceipt installation)
        {
            byte[] originalBytes = installation.OriginalBytes;
            receipts.Add(originalBytes, new DetourReceipt(target, installation, originalBytes));
            return originalBytes;
        }
    }
}
