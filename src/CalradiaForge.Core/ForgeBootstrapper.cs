using System;
using System.Reflection;
using System.Linq;
using CalradiaForge.Sdk.Patcher;

namespace CalradiaForge.Core
{
    /// <summary>
    /// Serves as the central bootstrapping system for Calradia Forge.
    /// It can automatically discover and initialize patches across all loaded mod assemblies.
    /// </summary>
    public static class ForgeBootstrapper
    {
        private static bool isInitialized = false;

        /// <summary>
        /// Automatically discovers and applies all Forge patches in all loaded assemblies
        /// that reference CalradiaForge.Sdk.
        /// </summary>
        public static void InitializeGlobalPatches()
        {
            if (isInitialized) return;
            isInitialized = true;

            int totalPatches = 0;
            
            // Scan all loaded assemblies in the AppDomain
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;

                // Only scan assemblies that have CalradiaForge.Sdk as a reference to save time
                if (assembly.GetReferencedAssemblies().Any(a => a.Name == "CalradiaForge.Sdk") || assembly.GetName().Name == "CalradiaForge.Sdk")
                {
                    try
                    {
                        totalPatches += ForgePatcher.ApplyAll(assembly);
                    }
                    catch (Exception ex)
                    {
                        // In a real scenario, this would write to TaleWorlds log
                        Console.WriteLine($"ForgeBootstrapper: Error applying patches for {assembly.GetName().Name} - {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"ForgeBootstrapper: Successfully applied {totalPatches} Forge patches across all loaded mods.");
        }
    }
}
