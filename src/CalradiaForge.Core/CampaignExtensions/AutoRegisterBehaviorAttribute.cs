using System;

namespace CalradiaForge.Core.CampaignExtensions
{
    /// <summary>
    /// Tag a CampaignBehaviorBase class with this attribute so Calradia Forge
    /// automatically registers it into the CampaignGameStarter during game initialization.
    /// This removes the need for manual boilerplate registration in OnGameStart.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AutoRegisterBehaviorAttribute : Attribute
    {
    }
}
