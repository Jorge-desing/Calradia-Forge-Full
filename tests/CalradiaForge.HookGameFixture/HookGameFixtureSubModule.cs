using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using CalradiaForge.Sdk;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.HookGameFixture
{
    /// <summary>
    /// Disposable menu-only integration provider. It registers one owned target and never applies it.
    /// </summary>
    public sealed class HookGameFixtureSubModule : MBSubModuleBase
    {
        private const string Owner = "CalradiaForgeHookFixture";
        private const string HookId = "calradiaforge.hookgamefixture.menu-probe";

        private IForgeHookService hookService;
        private IForgeHookHandle hookHandle;
        private int lastObservedValue = int.MinValue;

        protected override void OnSubModuleLoad()
        {
            ForgeApi.RegisterWhenAvailable(RegisterProbe);
        }

        private void RegisterProbe(IForgeRegistry registry)
        {
            var service = registry as IForgeHookService;
            if (service == null)
            {
                ForgeApi.Logger?.LogWarning(Owner, "The connected Forge host does not expose the optional hook capability.");
                return;
            }

            if (ReferenceEquals(service, hookService) && hookHandle != null)
                return;

            if (hookHandle != null && hookHandle.Snapshot.State == ForgeHookState.Applied)
            {
                ForgeApi.Logger?.LogWarning(Owner, "A prior fixture hook is still applied; refusing to register another host generation.");
                return;
            }

            var target = typeof(HookGameFixtureSubModule).GetMethod(
                nameof(ProbeTarget), BindingFlags.Static | BindingFlags.NonPublic);
            if (target == null)
                throw new MissingMethodException(typeof(HookGameFixtureSubModule).FullName, nameof(ProbeTarget));

            hookService = service;
            hookHandle = service.Register(new ForgeHookDefinition
            {
                Id = HookId,
                Owner = Owner,
                Target = target,
                Prefix = ApplyProbeResult
            });

            ForgeApi.Logger?.LogInfo(Owner, "Registered the inert menu probe. It returns 17 until explicitly applied, then 29; revert it before disabling this fixture.");
        }

        protected override void OnApplicationTick(float dt)
        {
            if (hookHandle == null)
                return;

            var observed = ProbeTarget();
            if (observed == lastObservedValue)
                return;

            lastObservedValue = observed;
            ForgeApi.Logger?.LogInfo(Owner, "Owned menu probe returned " + observed + " (registered/reverted: 17; applied: 29).");
        }

        protected override void OnSubModuleUnloaded()
        {
            ForgeApi.UnregisterWhenAvailable(RegisterProbe);
            if (hookHandle != null && hookHandle.Snapshot.State == ForgeHookState.Applied)
            {
                var result = hookHandle.Revert();
                if (!result.Succeeded)
                {
                    ForgeApi.Logger?.LogWarning(Owner, "Fixture unload could not verify hook reversion: " + result.Detail);
                    return;
                }
            }

            hookHandle = null;
            hookService = null;
            lastObservedValue = int.MinValue;
        }

        private static void ApplyProbeResult(ForgeHookInvocation invocation)
        {
            invocation.Result = 29;
            invocation.RunOriginal = false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ProbeTarget() => 17;
    }
}
