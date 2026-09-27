using System;
using CalradiaForge.Sdk;
using CalradiaForge.PriceContracts;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.PriceProvider
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
            library=ForgeApi.Libraries.OpenModule("CalradiaForgePriceProvider");
            library.Provide<IPriceCalculator>("prices",new Version(1,0),new PriceCalculator());
        }
        protected override void OnSubModuleUnloaded() { ForgeApi.UnregisterWhenAvailable(Register); library?.Dispose(); library=null; }
    }

    // A deliberately small shared API example, not an alternative game economy.
    public sealed class PriceCalculator : IPriceCalculator
    {
        public int CalculateTotal(int unitPrice,int quantity)
        {
            if(unitPrice<0)throw new ArgumentOutOfRangeException(nameof(unitPrice));
            if(quantity<0)throw new ArgumentOutOfRangeException(nameof(quantity));
            return checked(unitPrice*quantity);
        }
    }
}
