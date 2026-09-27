---
name: bannerlord-crime-underworld
description: CrimeModel, Alley rackets, gang leader dynamics, and rogue underworld mechanics in Mount & Blade II Bannerlord. See bannerlord-shared-patterns for Decorator/CampaignBehavior boilerplate.
---

# Bannerlord Crime, Town Alleys & Underworld Architecture

Use this skill when modifying crime rating, alley rackets, gang interactions, and tavern mercenary recruitment.

> **Prerequisites:** Read `bannerlord-shared-patterns` first for the Decorator boilerplate, CampaignBehaviorBase skeleton, and universal safety rules.

---

## Domain Architecture

| Concept | Details |
|:---|:---|
| Crime rating storage | Per-faction on `IFaction.MainHeroCrimeRating` |
| Modify crime rating | `ChangeCrimeRatingAction.Apply(faction, delta, notify)` |
| `CrimeModel` | `DefaultCrimeModel` — daily decay, fine/bribe costs; wrappable via Decorator |
| Alley objects | `Town.Alleys` — ~3 per town; controlled by `Occupation.GangLeader` notable |
| Claim alley | Requires companion with `Roguery ≥ 30` and `Mercy ≤ 0`; companion becomes `Alley.Owner` |
| Daily alley income | ~150 gold + 0.3 crime rating to owning kingdom |

**Crime consequence tiers:**
- 0–29: Normal gameplay
- 30–59: Gate suspicion + bribe option
- ≥ 60: Gate lockout, disguise required, possible arrest or combat

---

## Domain Implementation — `ForgeCrimeModel`

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace CalradiaForge.Underworld.GameModels
{
    public class ForgeCrimeModel : CrimeModel
    {
        private readonly CrimeModel _baseModel;

        public ForgeCrimeModel(CrimeModel baseModel)
        {
            _baseModel = baseModel;
        }

        public override float GetDailyCrimeRatingChange(IFaction faction, bool isUnderAlleyInfluence)
        {
            float change = _baseModel?.GetDailyCrimeRatingChange(faction, isUnderAlleyInfluence) ?? -1.0f;

            // Roguery perk slows crime decay (player retains underworld influence longer)
            if (Hero.MainHero?.GetPerkValue(DefaultPerks.Roguery.DeepPockets) == true)
            {
                change -= 0.5f;
            }

            return change;
        }

        public override int GetCostOfClearingCrimeRating(IFaction faction, CrimeModel.PaymentMethod paymentMethod)
        {
            int cost = _baseModel?.GetCostOfClearingCrimeRating(faction, paymentMethod) ?? 1000;
            return (int)(cost * 0.85f); // Underworld connection discount
        }
    }
}
```

---

## Underworld CampaignBehavior — Alley Racket Management

```csharp
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaForge.Underworld
{
    public class UnderworldRacketBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, int> _syndicateHeat = new Dictionary<string, int>();

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, OnDailyTickTown);
            CampaignEvents.AlleyCleared.AddNonSerializedListener(this, OnAlleyCleared);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Forge_SyndicateHeat", ref _syndicateHeat);
            if (dataStore.IsLoading) _syndicateHeat ??= new Dictionary<string, int>();
        }

        private void OnDailyTickTown(Town town)
        {
            if (town?.Alleys == null) return;

            foreach (Alley alley in town.Alleys)
            {
                if (alley.Owner?.Clan == Clan.PlayerClan)
                {
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 150, true);

                    if (town.Settlement?.MapFaction != null)
                    {
                        ChangeCrimeRatingAction.Apply(town.Settlement.MapFaction, 0.3f, false);
                    }
                }
            }
        }

        private void OnAlleyCleared(Alley alley)
        {
            // Custom response to street battle victory
        }
    }
}
```

---

## Black Market Menu Injection

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;

namespace CalradiaForge.Underworld
{
    public static class UnderworldMenuInjector
    {
        public static void InjectMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption(
                "town_backstreet",
                "forge_underworld_broker",
                "{=broker_option}Visit the Underworld Fence",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    return true;
                },
                args =>
                {
                    // Open fence barter or custom mission
                },
                index: 2
            );
        }
    }
}
```
