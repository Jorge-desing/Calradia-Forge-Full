#if NET472
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using CalradiaForge.Core.Diagnostics;

namespace CalradiaForge.Core.CampaignExtensions
{
    /// <summary>
    /// Utility to automatically load behaviors.
    /// </summary>
    public static class ForgeBehaviorLoader
    {
        public static void RegisterAll(CampaignGameStarter starter)
        {
            if (starter == null) return;
            
            int count = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;
                // Only scan assemblies that reference Core
                if (!assembly.GetReferencedAssemblies().Any(a => a.Name == "CalradiaForge.Core") && assembly.GetName().Name != "CalradiaForge.Core") continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (var type in types)
                {
                    if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(CampaignBehaviorBase)))
                    {
                        if (type.GetCustomAttribute<AutoRegisterBehaviorAttribute>() != null)
                        {
                            try
                            {
                                var behavior = (CampaignBehaviorBase)Activator.CreateInstance(type);
                                starter.AddBehavior(behavior);
                                count++;
                            }
                            catch (Exception ex)
                            {
                                ForgeLogger.PrintError($"Failed to auto-register behavior {type.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            if (count > 0)
                ForgeLogger.PrintSuccess($"Forge auto-registered {count} campaign behaviors.");
        }
    }
}
#endif
