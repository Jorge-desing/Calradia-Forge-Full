using System;
using TaleWorlds.CampaignSystem;
using CalradiaForge.Sdk;

namespace CalradiaForge.Mod.DataExtensions
{
    /// <summary>
    /// Lifecycle behavior that ensures ForgeData is cleaned up across campaign state changes.
    /// Operates 100% statelessly without mutating save files or holding persistent state.
    /// </summary>
    public class DataBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnGameLoaded));
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            ForgeData.ClearAll();
            ForgeAgentMemory.ClearAll();
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            ForgeData.ClearAll();
            ForgeAgentMemory.ClearAll();
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Stateless: ForgeData operates entirely in volatile memory and does not serialize to saved games.
        }
    }
}
