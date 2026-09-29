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
        /// Notification for legacy prefix/postfix declarations. Forge has no built-in subscriber
        /// that applies these hooks; raising this event is not evidence of an installed patch.
        /// </summary>
        public static event Action<MethodInfo, MethodInfo, MethodInfo> OnPatchRequested;
        
        /// <summary>
        /// Notification for legacy prefix/postfix declarations. Forge has no built-in subscriber
        /// that removes these hooks.
        /// </summary>
        public static event Action<MethodInfo, MethodInfo, MethodInfo> OnRevertRequested;

        /// <summary>
        /// Raises a legacy hook-request notification only. The backend does not apply prefix/postfix hooks.
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
        /// Raises a legacy hook-revert notification only. The backend does not remove prefix/postfix hooks.
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
        /// Directly installs an experimental native method replacement through ForgeDetour.
        /// This is not coordinated with threads executing the target; prefer the optional
        /// ForgeApi.Patches capability when a host is connected.
        /// </summary>
        public static void ApplyDetour(MethodInfo original, MethodInfo replacement)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));

            ForgeDetour.Patch(original, replacement);
        }
    }
}
