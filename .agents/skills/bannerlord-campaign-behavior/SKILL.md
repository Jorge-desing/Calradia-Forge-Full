---
name: bannerlord-campaign-behavior
description: Event subscription catalog, dialogue/menu injection, and anti-lag time-slicing patterns for CampaignBehaviorBase in Mount & Blade II Bannerlord. See bannerlord-shared-patterns for the full CampaignBehaviorBase skeleton.
---

# Bannerlord CampaignBehavior Skill

Use this skill when developing custom simulation logic, periodic economic or diplomatic cycles, encounter triggers, or persistent world tracking.

> **Prerequisites:** Read `bannerlord-shared-patterns` first for the full CampaignBehaviorBase skeleton, SyncData null-safe patterns, SaveableTypeDefiner, and universal safety rules.

---

## When to Use This Skill
- Listening to campaign events (`HourlyTick`, `DailyTickParty`, `SettlementEntered`, `WarDeclared`, `HeroCreated`).
- Persisting mod state safely in Bannerlord save files via `IDataStore.SyncData`.
- Implementing anti-lag distributed updates for thousands of heroes/settlements.
- Registering conversation dialogues or game menus upon campaign session launch.

---

## Domain Implementation — Full Working Behavior Template

```csharp
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace CalradiaForge.Core.CampaignBehaviors
{
    public class ForgeCampaignBehavior : CampaignBehaviorBase
    {
        private int _totalCycles;
        private Dictionary<string, int> _heroLoyalty = new Dictionary<string, int>();

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Forge_TotalCycles", ref _totalCycles);
            dataStore.SyncData("Forge_HeroLoyalty", ref _heroLoyalty);
            if (dataStore.IsLoading) _heroLoyalty ??= new Dictionary<string, int>();
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Register custom game menus or dialogues here — see bannerlord-quest-system for dialogue patterns
        }

        private void OnHourlyTick()
        {
            // Anti-Lag Pattern: 24-hour modulo time-slicing
            // Spreads processing across 24 hourly buckets — each tick processes ~1/24th of settlements
            int currentHour = (int)CampaignTime.Now.ToHours % 24;

            foreach (Settlement settlement in Settlement.All)
            {
                if (!settlement.IsTown) continue;

                // Each settlement maps to a deterministic hourly bucket via its StringId hash
                int bucket = (settlement.StringId.GetHashCode() & 0x7FFFFFFF) % 24;
                if (bucket != currentHour) continue;

                // Execute logic for this settlement's slot
            }
        }

        private void OnDailyTickParty(MobileParty party)
        {
            if (party == null || !party.IsActive) return;
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (settlement == null) return;
        }
    }
}
```

---

## SaveableTypeDefiner for this Behavior

```csharp
using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace CalradiaForge.Core.CampaignBehaviors
{
    public class ForgeSaveTypeDefiner : SaveableTypeDefiner
    {
        public ForgeSaveTypeDefiner() : base(2_750_000) { }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
            ConstructContainerDefinition(typeof(List<string>));
        }
    }
}
```

---

## Campaign Event Catalog

Key events to listen to per use case:

| Use Case | Event |
|:---|:---|
| Global hourly simulation | `CampaignEvents.HourlyTickEvent` |
| Per-party daily logic | `CampaignEvents.DailyTickPartyEvent` |
| Per-town daily logic | `CampaignEvents.DailyTickTownEvent` |
| Per-settlement daily | `CampaignEvents.DailyTickSettlementEvent` |
| Player enters settlement | `CampaignEvents.SettlementEntered` |
| Dialogue + menu registration | `CampaignEvents.OnSessionLaunchedEvent` |
| Hero created/died | `CampaignEvents.HeroCreated` / `CampaignEvents.HeroKilled` |
| War declared | `CampaignEvents.WarDeclaredEvent` |
| Clan destroyed | `CampaignEvents.OnClanDestroyedEvent` |
| Alley cleared | `CampaignEvents.AlleyCleared` |
