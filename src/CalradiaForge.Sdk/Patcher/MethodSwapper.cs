using System;
using System.Reflection;

namespace CalradiaForge.Sdk.Patcher
{
    /// <summary>
    /// Compatibility adapter for legacy callers. All writes and ownership tracking are routed
    /// through ForgeDetour so the SDK has one native detour registry and one integrity policy.
    /// </summary>
    public static class MethodSwapper
    {
        /// <summary>
        /// Installs the legacy x64 jump and returns a copy of its original instruction bytes.
        /// Writes are experimental and are not synchronized with threads executing the target.
        /// </summary>
        public static byte[] DetourMethod(MethodInfo target, MethodInfo replacement)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            ForgeDetour.Patch(target, replacement);
            return ForgeDetour.GetOriginalBytes(target);
        }

        internal static byte[] DetourMethod(MethodInfo target, MethodInfo replacement, string patchId, string owner)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            ForgeDetour.Patch(target, replacement, patchId, owner);
            return ForgeDetour.GetOriginalBytes(target);
        }

        /// <summary>
        /// Restores only a still-tracked Forge detour whose original-byte receipt matches.
        /// It will not overwrite code changed by another component.
        /// </summary>
        public static void RestoreMethod(MethodInfo target, byte[] originalBytes)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (originalBytes == null || originalBytes.Length != 13)
                throw new ArgumentException("Invalid backup bytes.", nameof(originalBytes));
            if (!ForgeDetour.OriginalBytesMatch(target, originalBytes))
                throw new InvalidOperationException("The backup does not match the active Forge detour record.");
            if (!ForgeDetour.Unpatch(target))
                throw new InvalidOperationException("Forge did not revert the detour because target bytes no longer match its installed jump.");
        }
    }
}
