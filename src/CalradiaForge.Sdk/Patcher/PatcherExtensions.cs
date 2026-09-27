using System;
using System.Reflection;

namespace CalradiaForge.Sdk.Patcher
{
    /// <summary>
    /// Provides a fluent, programatic API for patching methods without using attributes.
    /// Useful for advanced modders who want dynamic runtime hooks.
    /// </summary>
    public static class PatcherExtensions
    {
        /// <summary>
        /// Detours this original method to point to the replacement method.
        /// Returns a PatchRecord that can be used to Revert the patch later.
        /// </summary>
        public static PatchRecord DetourWith(this MethodInfo originalMethod, MethodInfo replacementMethod)
        {
            if (originalMethod == null) throw new ArgumentNullException(nameof(originalMethod));
            if (replacementMethod == null) throw new ArgumentNullException(nameof(replacementMethod));

            byte[] originalBytes = MethodSwapper.DetourMethod(originalMethod, replacementMethod);

            var record = new PatchRecord
            {
                Original = originalMethod,
                Replacement = replacementMethod,
                SourceModule = Assembly.GetCallingAssembly().GetName().Name,
                OriginalBytes = originalBytes
            };

            ForgePatcher.Register(record);
            
            return record;
        }

        /// <summary>
        /// Restores a method that was previously detoured using DetourWith.
        /// </summary>
        public static void RevertDetour(this PatchRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            MethodSwapper.RestoreMethod(record.Original, record.OriginalBytes);
        }
    }
}
