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

            var sourceModule = Assembly.GetCallingAssembly().GetName().Name;
            string patchId = "legacy:" + ForgeDetour.ShortHash(
                ForgeDetour.SignatureIdentity(originalMethod) + "->" + ForgeDetour.SignatureIdentity(replacementMethod));
            byte[] originalBytes = MethodSwapper.DetourMethod(originalMethod, replacementMethod, patchId, sourceModule);
            var record = new PatchRecord
            {
                Id = patchId,
                Original = originalMethod,
                Replacement = replacementMethod,
                Owner = sourceModule,
                SourceModule = sourceModule,
                OriginalBytes = originalBytes
            };

            try { ForgePatcher.Register(record); }
            catch
            {
                MethodSwapper.RestoreMethod(originalMethod, originalBytes);
                throw;
            }
            
            return record;
        }

        /// <summary>
        /// Restores a method that was previously detoured using DetourWith.
        /// </summary>
        public static void RevertDetour(this PatchRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            ForgePatcher.Revert(record);
        }
    }
}
