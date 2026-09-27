using System;
using System.ComponentModel;
using System.Reflection;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Experimental patch request surface. It is not part of the supported ForgeWeave SDK workflow.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ForgeLivePatcher
    {
        /// <summary>
        /// Fired when a mod requests a live patch. 
        /// The runtime intercepts this and applies the Harmony patch.
        /// </summary>
        public static event Action<MethodInfo, MethodInfo, MethodInfo> OnPatchRequested;
        
        /// <summary>
        /// Fired when a mod requests to revert a live patch.
        /// </summary>
        public static event Action<MethodInfo, MethodInfo, MethodInfo> OnRevertRequested;

        /// <summary>
        /// Submits a request to apply a live patch to the target method.
        /// </summary>
        /// <param name="original">The original game method to patch.</param>
        /// <param name="prefix">The prefix method (optional).</param>
        /// <param name="postfix">The postfix method (optional).</param>
        public static void ApplyPatch(MethodInfo original, MethodInfo prefix = null, MethodInfo postfix = null)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (prefix == null && postfix == null) throw new ArgumentException("Must provide at least a prefix or postfix.");
            
            OnPatchRequested?.Invoke(original, prefix, postfix);
        }

        /// <summary>
        /// Submits a request to revert a live patch from the target method.
        /// </summary>
        /// <param name="original">The original game method that was patched.</param>
        /// <param name="prefix">The prefix method to remove (optional).</param>
        /// <param name="postfix">The postfix method to remove (optional).</param>
        public static void RevertPatch(MethodInfo original, MethodInfo prefix = null, MethodInfo postfix = null)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (prefix == null && postfix == null) throw new ArgumentException("Must provide at least a prefix or postfix.");

            OnRevertRequested?.Invoke(original, prefix, postfix);
        }

        /// <summary>
        /// Instantly detours the original method to the replacement method using native memory hooks.
        /// This bypasses Harmony completely and executes in native memory.
        /// </summary>
        public static void ApplyDetour(MethodInfo original, MethodInfo replacement)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));

            ForgeDetour.Patch(original, replacement);
        }
    }
}
