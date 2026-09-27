---
name: bannerlord-character-development
description: HeroDeveloper, CharacterDevelopmentModel, skill learning rates, and perk role evaluation in Mount & Blade II Bannerlord. See bannerlord-shared-patterns for Decorator Pattern boilerplate.
---

# Bannerlord Character Development & Perk Architecture

Use this skill when modifying skill progression, learning rates, focus points, and perk mechanics.

> **Prerequisites:** Read `bannerlord-shared-patterns` first for the Decorator boilerplate, SubModule registration, and universal safety rules.

---

## Domain Architecture

| Class | Type | Role |
|:---|:---|:---|
| `HeroDeveloper` | Stateful per-hero | Holds XP, focus/attribute points, perk list |
| `CharacterDevelopmentModel` | Stateless GameModel | Calculates learning limits, rates, XP thresholds |

**Learning Limit formula:** `(Attribute - 1) × 10 + Focus × 30`  
**Learning Rate floor:** Always ≥ 0.05 to prevent dead-lock progression.

---

## Domain Implementation — `ForgeCharacterDevelopmentModel`

```csharp
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace CalradiaForge.CharacterProgression.GameModels
{
    public class ForgeCharacterDevelopmentModel : CharacterDevelopmentModel
    {
        private readonly CharacterDevelopmentModel _baseModel;

        public ForgeCharacterDevelopmentModel(CharacterDevelopmentModel baseModel)
        {
            _baseModel = baseModel;
        }

        // +1 focus point per level, attribute point every 3 levels instead of 4
        public override int FocusPointsPerLevel     => _baseModel != null ? _baseModel.FocusPointsPerLevel + 1 : 2;
        public override int LevelsPerAttributePoint => 3;
        public override int MaxSkillRequiredForLevel => _baseModel?.MaxSkillRequiredForLevel ?? 1024;

        public override int CalculateLearningLimit(int attributeValue, int focusValue, TextObject attributeName, StatExplainer explainer = null)
        {
            int baseLimit = _baseModel != null
                ? _baseModel.CalculateLearningLimit(attributeValue, focusValue, attributeName, explainer)
                : ((attributeValue - 1) * 10) + (focusValue * 30);

            return baseLimit + (focusValue * 10); // Boost with focus investment
        }

        public override float CalculateLearningRate(int attributeValue, int focusValue, int skillValue, int characterLevel, TextObject attributeName, StatExplainer explainer = null)
        {
            float rate = _baseModel != null
                ? _baseModel.CalculateLearningRate(attributeValue, focusValue, skillValue, characterLevel, attributeName, explainer)
                : 1.0f;

            return MathF.Max(rate, 0.08f); // Minimum floor — prevents dead-lock at high skill values
        }

        public override int GetXpAmountForSkillLevelChange(Hero hero, SkillObject skill, int skillLevelChange)
        {
            return _baseModel?.GetXpAmountForSkillLevelChange(hero, skill, skillLevelChange)
                ?? (int)(0.5f * (hero.GetSkillValue(skill) * hero.GetSkillValue(skill)) + 15);
        }

        public override int GetSkillLevelChange(Hero hero, SkillObject skill, float skillXp)
        {
            return _baseModel?.GetSkillLevelChange(hero, skill, skillXp) ?? 1;
        }

        public override int SkillsRequiredForLevel(int level)
        {
            return _baseModel?.SkillsRequiredForLevel(level) ?? (int)(1000 * Math.Pow(level, 1.2));
        }

        public override int GetXpRequiredForLevel(int level)
        {
            return _baseModel?.GetXpRequiredForLevel(level) ?? (int)(500 * Math.Pow(level, 1.5));
        }

        public override void GetTraitLevelForTraitXp(Hero hero, TraitObject trait, int xpValue, out int traitLevel, out int remainingXp)
        {
            if (_baseModel != null)
            {
                _baseModel.GetTraitLevelForTraitXp(hero, trait, xpValue, out traitLevel, out remainingXp);
                return;
            }
            traitLevel = 0;
            remainingXp = xpValue;
        }
    }
}
```

---

## Programmatic Hero Progression & Perk Querying

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace CalradiaForge.CharacterProgression
{
    public static class HeroProgressionHelper
    {
        public static void GrantBonusFocusAndXP(Hero hero, SkillObject skill, int xpAmount)
        {
            if (hero?.HeroDeveloper == null) return;

            var dev = hero.HeroDeveloper;
            dev.AddSkillXp(skill, xpAmount, isAfflicted: false, addTotalXp: true);

            if (dev.CanAddFocusToSkill(skill) && dev.UnspentFocusPoints > 0)
            {
                dev.AddFocus(skill, 1, checkUnspentPoints: true);
            }
        }

        public static bool HasTacticalPerk(Hero hero, PerkObject perk)
        {
            return hero?.GetPerkValue(perk) ?? false;
        }
    }
}
```
