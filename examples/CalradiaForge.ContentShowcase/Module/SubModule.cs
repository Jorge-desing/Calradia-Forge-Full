using System.Reflection;
using CalradiaForge.Sdk;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.ContentShowcase
{
    public sealed class SubModule : MBSubModuleBase
    {
        private const string ModuleId = "CalradiaForgeContentShowcase";

        protected override void OnSubModuleLoad()
        {
            ForgeApi.RegisterWhenAvailable(RegisterPages);
        }

        protected override void OnSubModuleUnloaded()
        {
            ForgeApi.UnregisterWhenAvailable(RegisterPages);
            ForgeApi.UnregisterUiPages(ModuleId);
        }

        private static void RegisterPages(IForgeRegistry registry)
        {
            ForgeApi.AutoRegister(Assembly.GetExecutingAssembly(), ModuleId);
        }
    }
}
