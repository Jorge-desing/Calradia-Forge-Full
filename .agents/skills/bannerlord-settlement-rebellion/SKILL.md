---
name: bannerlord-settlement-rebellion
description: Town/Village equilibrium models, construction queues, rebellion lifecycles, and custom buildings in Mount & Blade II Bannerlord. See bannerlord-shared-patterns for Decorator/CampaignBehavior boilerplate.
---

# Bannerlord Settlement Projects, Equilibrium & Rebellion Systems

Use this skill when working with settlement demographic simulation, custom buildings, loyalty models, and rebellion detection.

> **Prerequisites:** Read `bannerlord-shared-patterns` first for the Decorator boilerplate, CampaignBehaviorBase skeleton, and universal safety rules.

---

## Domain Architecture

| Concept | Details |
|:---|:---|
| `Settlement` → `Town` / `Village` | Core hierarchy |
| `Town.BuildingsInProgress` | FIFO `Queue<Building>` — daily power from `BuildingConstructionModel` |
| **Daily Projects** (when queue empty) | Housing, Irrigation, Festivals & Games, Train Militia |
| **Construction Strike** | Loyalty ≤ 25 → 0 daily construction, gold boosts frozen |
| Loyalty drift | `ΔL = 0.1 × (50 − L)` per day |
| Food consumption | `CitizenFood = Prosperity / 50` |
| **Rebellion trigger** | Militia > Garrison AND Loyalty ≤ 25 → `StartRebellionEvent` |
| After rebellion | Rebel clan holds town 30 days → becomes permanent |

---

## Domain Implementation 1 — Custom Building System (Zero-Harmony)

Implements custom town buildings without mutating native `Town.Buildings` lists:

```csharp
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace CalradiaForge.SettlementExtensions
{
    public class CustomBuildingRecord
    {
        [SaveableField(1)] public string BuildingId;
        [SaveableField(2)] public int    Level;
        [SaveableField(3)] public float  Progress;

        public CustomBuildingRecord() { }
        public CustomBuildingRecord(string id) { BuildingId = id; Level = 0; Progress = 0f; }
    }

    public class SettlementInfrastructureBehavior : CampaignBehaviorBase
    {
        public static SettlementInfrastructureBehavior Current { get; private set; }
        private Dictionary<string, List<CustomBuildingRecord>> _records = new();

        public SettlementInfrastructureBehavior() { Current = this; }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Forge_SettlementProjects", ref _records);
            if (dataStore.IsLoading) _records ??= new();
        }

        private void OnDailyTickSettlement(Settlement settlement)
        {
            if (!settlement.IsTown || settlement.Town == null || settlement.Town.Loyalty <= 25f) return;

            if (_records.TryGetValue(settlement.StringId, out var buildings))
            {
                foreach (var bld in buildings)
                {
                    if (bld.Level >= 3) continue;
                    bld.Progress += 10f;
                    if (bld.Progress >= 1000f) { bld.Progress = 0f; bld.Level++; }
                    break; // One building per tick
                }
            }
        }

        public int GetBuildingLevel(Settlement settlement, string buildingId)
        {
            if (settlement == null) return 0;
            if (_records.TryGetValue(settlement.StringId, out var list))
            {
                return list.Find(b => b.BuildingId == buildingId)?.Level ?? 0;
            }
            return 0;
        }
    }
}
```

---

## Domain Implementation 2 — `ForgeSettlementLoyaltyModel`

Injects custom building bonuses with native UI tooltip reflection:

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace CalradiaForge.SettlementExtensions.GameModels
{
    public class ForgeSettlementLoyaltyModel : SettlementLoyaltyModel
    {
        private readonly SettlementLoyaltyModel _baseModel;

        public ForgeSettlementLoyaltyModel(SettlementLoyaltyModel baseModel)
        {
            _baseModel = baseModel;
        }

        public override ExplainedNumber CalculateLoyaltyChange(Town town, StatExplainer explanation = null)
        {
            ExplainedNumber result = _baseModel != null
                ? _baseModel.CalculateLoyaltyChange(town, explanation)
                : new ExplainedNumber(0f, explanation != null);

            if (town?.Settlement != null && SettlementInfrastructureBehavior.Current != null)
            {
                int tier = SettlementInfrastructureBehavior.Current.GetBuildingLevel(town.Settlement, "forge_civic_hall");
                if (tier > 0)
                {
                    result.Add(tier * 0.5f,
                        new TextObject("{=civic_hall}Civic Hall (Tier {TIER})").SetTextVariable("TIER", tier));
                }
            }

            return result;
        }
    }
}
```

---

## Quelling Rebellion Spirals Programmatically

```csharp
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaForge.SettlementExtensions
{
    public static class PacificationHelper
    {
        public static void EnactEmergencyFestivals(Town town)
        {
            if (town == null) return;

            Building festivals = town.Buildings.Find(b => b.BuildingType.StringId == "building_festivals_and_games");
            if (festivals != null)
            {
                town.CurrentDefaultBuilding = festivals;
                town.BuildingsInProgress.Clear(); // Pause all construction — civic energy flows to morale
            }
        }
    }
}
```
