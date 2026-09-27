---
name: bannerlord-economy-trade
description: Workshop mechanics, dynamic market supply/demand pricing, and trade decorator models in Mount & Blade II Bannerlord. See bannerlord-shared-patterns for Decorator Pattern boilerplate and SubModule registration.
---

# Bannerlord Economy & Trade Modding Skill

Use this skill when modifying workshops, dynamic market pricing, trade penalties, caravan trading routes, village production, or settlement tariffs.

> **Prerequisites:** Read `bannerlord-shared-patterns` first for the Decorator boilerplate, SubModule registration, and universal safety rules.

---

## When to Use This Skill
- Customizing workshop production rates, daily operating expenses, or player workshop caps.
- Modifying village primary production formulas based on Hearth counts.
- Adjusting market price volatility, bid-ask trade penalties, or trade tariffs.
- Interacting with `Town.MarketData` or calculating item prices cleanly.

---

## Domain Implementation — `ForgeWorkshopModel`

Wraps `WorkshopModel` to boost production in prosperous towns, reduce costs, and add extra workshop slots:

```csharp
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Localization;

namespace CalradiaForge.Core.GameModels.Economy
{
    public class ForgeWorkshopModel : WorkshopModel
    {
        private readonly WorkshopModel _baseModel;

        public ForgeWorkshopModel(WorkshopModel baseModel)
        {
            _baseModel = baseModel;
        }

        public override int WarehouseCapacity => _baseModel?.WarehouseCapacity ?? 6000;
        public override int InitialCapital    => _baseModel?.InitialCapital    ?? 10000;
        public override int DailyExpense      => _baseModel != null ? Math.Max(50, _baseModel.DailyExpense - 20) : 80;
        public override int DaysForDoWorkshopsThink => _baseModel?.DaysForDoWorkshopsThink ?? 1;

        public override int GetMaxWorkshopCountForClanTier(int clanTier)
        {
            int baseCount = _baseModel?.GetMaxWorkshopCountForClanTier(clanTier) ?? (1 + clanTier);
            return baseCount + 1; // Bonus slot for merchant clans
        }

        public override ExplainedNumber GetDailyProductionProgress(Workshop workshop, bool includeDescriptions = false)
        {
            ExplainedNumber progress = _baseModel != null
                ? _baseModel.GetDailyProductionProgress(workshop, includeDescriptions)
                : new ExplainedNumber(1.0f, includeDescriptions, new TextObject("{=forge_prod}Base Production"));

            // Prosperity network bonus
            if (workshop?.Settlement?.Town != null && workshop.Settlement.Town.Prosperity > 5000f)
            {
                progress.AddFactor(0.15f, new TextObject("{=forge_prosper}Guild Prosperity Network"));
            }

            progress.LimitMin(0.2f);
            return progress;
        }

        public override int GetBuyingCostForPlayer(Workshop workshop) =>
            _baseModel?.GetBuyingCostForPlayer(workshop) ?? (InitialCapital + 5000);

        public override int GetCostForChangingWorkshopType(Workshop workshop) =>
            _baseModel?.GetCostForChangingWorkshopType(workshop) ?? 2000;

        public override int GetDailyExpense(int level) =>
            Math.Max(40, _baseModel?.GetDailyExpense(level) ?? 100);

        public override int GetConvertProductionCost(WorkshopType workshopType) =>
            _baseModel?.GetConvertProductionCost(workshopType) ?? 1000;
    }
}
```

---

## Critical Domain Rules
1. **Stateless:** `WorkshopModel` is queried every AI tick — never store mutable settlement state in it. Use `CampaignBehaviorBase.SyncData` instead.
2. **Always pass `includeDescriptions`** through to child `GetDailyProductionProgress` calls to preserve Gauntlet tooltips.
