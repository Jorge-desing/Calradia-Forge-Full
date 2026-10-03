using System.Reflection;
using CalradiaForge.Sdk;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.ModTemplate
{
    public sealed class SubModule : MBSubModuleBase
    {
        private const string ModuleId = "__MODULE_ID__";

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            ForgeApi.RegisterWhenAvailable(RegisterForge);
        }

        protected override void OnSubModuleUnloaded()
        {
            ForgeApi.UnregisterWhenAvailable(RegisterForge);
            ForgeApi.UnregisterUiPages(ModuleId);
        }

        private static void RegisterForge(IForgeRegistry registry)
        {
            ForgeApi.AutoRegister(Assembly.GetExecutingAssembly(), ModuleId);
        }
    }
}
