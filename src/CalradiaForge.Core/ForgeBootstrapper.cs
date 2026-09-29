using System;
using System.ComponentModel;

namespace CalradiaForge.Core
{
    /// <summary>
    /// Compatibility surface retained for extensions compiled against earlier SDK builds.
    /// Forge no longer scans the AppDomain or applies patches during module startup.
    /// </summary>
    public static class ForgeBootstrapper
    {
        /// <summary>Does nothing. Patch application now requires an explicit experimental API call.</summary>
        [Obsolete("Global patch discovery is disabled. Use an explicit patch service only after manual review.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void InitializeGlobalPatches()
        {
            // Intentionally empty for binary/source compatibility with old callers.
        }
    }
}
