using System;
using CalradiaForge.Sdk;
using CalradiaForge.PriceContracts;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.PriceConsumer
{
    public sealed class SubModule : MBSubModuleBase
    {
        ModuleLibrary library;
        protected override void OnSubModuleLoad()
        {
            ForgeApi.RegisterWhenAvailable(Register);
        }
        void Register(IForgeRegistry registry)
        {
            library=ForgeApi.Libraries.OpenModule("CalradiaForgePriceConsumer");
            registry.Register(new SharedPriceTest(library));
        }
        protected override void OnSubModuleUnloaded() { ForgeApi.UnregisterWhenAvailable(Register); library?.Dispose(); library=null; }
    }

    public sealed class SharedPriceTest : ITestCase
    {
        readonly ModuleLibrary library;
        SharedService<IPriceCalculator> service;
        int total;
        public SharedPriceTest(ModuleLibrary library) { this.library=library; }
        public Descriptor Descriptor=>new Descriptor { Id="examples.shared_prices",Module="CalradiaForgePriceConsumer",Name="Shared price library",Context=Context.Any,ChangesState=false };
        public void Prepare(TestExecution execution)
        {
            total=0;service=null;
            service=library.Require<IPriceCalculator>("CalradiaForgePriceProvider","prices",new Version(1,0));
            execution.Steps.Add("provider=CalradiaForgePriceProvider; service=prices; api="+service.ApiVersion);
        }
        public void Execute(TestExecution execution)
        {
            execution.Cancellation.ThrowIfCancellationRequested();
            total=service.Use(prices=>prices.CalculateTotal(7,3));
            execution.Steps.Add("shared total="+total);
        }
        public void Verify(TestExecution execution)=>execution.Verify(total==21,"Shared provider returned 21");
        public void Cleanup(TestExecution execution) { service=null; }
    }
}
