---
name: bannerlord-gamemodels
description: Concrete decorator implementations for custom GameModels in Mount & Blade II Bannerlord — party speed, troop wages, and equipment purchase costs. See bannerlord-shared-patterns for Decorator Pattern boilerplate and ExplainedNumber reference.
---

# Bannerlord GameModels — Concrete Implementations

Use this skill when modifying, overriding, or decorating campaign and combat calculation models.

> **Prerequisites:** Read `bannerlord-shared-patterns` first for the Decorator Pattern template, SubModule registration, ExplainedNumber quick reference, and universal safety rules.

---

## When to Use This Skill
- Implementing custom party movement speed formulas.
- Adjusting party troop capacity or prisoner limits.
- Modifying troop wages, garrison maintenance costs, or upgrade XP requirements.
- Customizing settlement prosperity, loyalty, food production, or construction speed.
- Creating native game balance modifiers with complete UI tooltip integration.

---

## Implementation 1 — `ForgePartySpeedModel` (Map Movement)

Wraps the native `PartySpeedCalculatingModel` to apply a Vanguard Discipline bonus for exceptional leaders:

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace CalradiaForge.Core.GameModels
{
    public class ForgePartySpeedModel : PartySpeedCalculatingModel
    {
        private readonly PartySpeedCalculatingModel _baseModel;

        public ForgePartySpeedModel(PartySpeedCalculatingModel baseModel)
        {
            _baseModel = baseModel;
        }

        public override float BaseSpeed => _baseModel != null ? _baseModel.BaseSpeed : 5.0f;

        public override ExplainedNumber CalculateBaseSpeed(MobileParty mobileParty, bool includeDescriptions = false, int additionalTroopNumber = 0, int additionalFootTroopNumber = 0)
        {
            if (_baseModel != null)
                return _baseModel.CalculateBaseSpeed(mobileParty, includeDescriptions, additionalTroopNumber, additionalFootTroopNumber);

            return new ExplainedNumber(BaseSpeed, includeDescriptions, new TextObject("{=base_speed}Base"));
        }

        public override ExplainedNumber CalculateFinalSpeed(MobileParty mobileParty, ExplainedNumber baseSpeed)
        {
            ExplainedNumber result = _baseModel != null
                ? _baseModel.CalculateFinalSpeed(mobileParty, baseSpeed)
                : baseSpeed;

            // Domain-specific: Forge Vanguard Discipline bonus for player-led exceptional parties
            if (mobileParty.IsLeaderException)
            {
                result.AddFactor(0.12f, new TextObject("{=forge_speed_commander}Forge Vanguard Discipline"));
            }

            result.LimitMin(0.5f); // Never slower than 0.5 — prevents permanent map stall
            return result;
        }
    }
}
```

---

## Implementation 2 — `ForgePartyWageModel` (Troop Upkeep & Garrison Discounts)

Wraps `PartyWageModel` to reduce upkeep for disciplined garrison troops and veteran mercenary units:

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace CalradiaForge.Core.GameModels
{
    public class ForgePartyWageModel : PartyWageModel
    {
        private readonly PartyWageModel _baseModel;

        public ForgePartyWageModel(PartyWageModel baseModel)
        {
            _baseModel = baseModel;
        }

        public override int MaxWage => _baseModel?.MaxWage ?? 10000;

        public override ExplainedNumber GetTotalWage(MobileParty mobileParty, bool includeDescriptions = false)
        {
            ExplainedNumber result = _baseModel != null
                ? _baseModel.GetTotalWage(mobileParty, includeDescriptions)
                : new ExplainedNumber(0f, includeDescriptions);

            // 15% maintenance subsidy for settlement garrisons
            if (mobileParty.IsGarrison)
            {
                result.AddFactor(-0.15f, new TextObject("{=forge_garrison_subsidy}Royal Garrison Subsidy"));
            }

            // Upkeep floor prevents negative wages
            result.LimitMin(0f);
            return result;
        }

        public override int GetCharacterWage(CharacterObject character)
        {
            return _baseModel?.GetCharacterWage(character) ?? (character.Tier * 3);
        }
    }
}
```

---

## Domain Gotchas Specific to GameModels

1. **`ExplainedNumber` is a struct** — always work with the returned value. Assigning to a local and modifying won't affect the caller's copy.
2. **Stateless requirement** — GameModels run every AI tick. Never store mutable state; persist it in `CampaignBehaviorBase.SyncData`.
3. **Always pass `includeDescriptions`** downstream so Gauntlet UI tooltips reflect the full calculation breakdown.
4. **Never return negative wages or zero speeds** — always use `LimitMin` to ensure logical simulation boundaries.
