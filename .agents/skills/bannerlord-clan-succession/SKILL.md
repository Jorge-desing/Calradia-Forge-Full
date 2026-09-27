---
name: bannerlord-clan-succession
description: ClanTierModel, companion party roles, marriage, pregnancy, and dynastic succession in Mount & Blade II Bannerlord. See bannerlord-shared-patterns for Decorator Pattern boilerplate.
---

# Bannerlord Clan Management & Succession Systems

Use this skill when modifying clan tier limits, companion roles, and handling dynastic lifecycles.

> **Prerequisites:** Read `bannerlord-shared-patterns` first for the Decorator boilerplate, SubModule registration, and universal safety rules.

---

## Domain Architecture

| Key Class | Purpose |
|:---|:---|
| `Clan` | Core faction entity — `Leader`, `Tier`, `Renown`, `Companions`, `Lords` |
| `ClanTierModel` | Stateless model — renown thresholds, companion caps, party limits |
| `MarriageAction.Apply` | Moves female hero into male's clan (unless she is reigning leader) |
| `PregnancyModel` | 36-day gestation; outcomes include twins, stillbirth, maternal death |

**Companion limit formula:** `3 + Tier + PerkModifiers` (lords never consume companion slots).  
**Succession:** `ApplyHeirSelectionAction` → `ChangePlayerCharacterAction` + `ChangeClanLeaderAction`

---

## Domain Implementation — `ForgeClanTierModel`

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace CalradiaForge.DynastyExtensions.GameModels
{
    public class ForgeClanTierModel : ClanTierModel
    {
        private readonly ClanTierModel _baseModel;

        public ForgeClanTierModel(ClanTierModel baseModel)
        {
            _baseModel = baseModel;
        }

        public override int MinClanTier => _baseModel?.MinClanTier ?? 0;
        public override int MaxClanTier => _baseModel?.MaxClanTier ?? 6;

        public override int GetRequiredRenownForTier(int tier)
        {
            return _baseModel?.GetRequiredRenownForTier(tier) ?? (tier * 300);
        }

        public override int GetCompanionLimit(Clan clan)
        {
            int baseLimit = _baseModel?.GetCompanionLimit(clan) ?? (3 + clan.Tier);
            // Extra slots for player clan to encourage companion investment
            if (clan == Clan.PlayerClan) baseLimit += 2;
            return baseLimit;
        }

        public override int GetPartyLimitForTier(Clan clan, int clanTierToCheck)
        {
            return _baseModel?.GetPartyLimitForTier(clan, clanTierToCheck) ?? (1 + clanTierToCheck);
        }
    }
}
```

---

## Companion Role Querying & Fallback Safety

Party companion roles fall back to `LeaderHero` if unassigned.

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace CalradiaForge.DynastyExtensions
{
    public static class CompanionRoleHelper
    {
        public static int GetEffectiveSurgeonMedicine(MobileParty party)
        {
            if (party == null) return 0;
            Hero surgeon = party.EffectiveSurgeon; // Falls back to LeaderHero automatically
            return surgeon?.GetSkillValue(DefaultSkills.Medicine) ?? 0;
        }

        public static int GetEffectiveScoutSpeedBonus(MobileParty party)
        {
            if (party == null) return 0;
            return party.EffectiveScout?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
        }

        public static void ReassignCompanionRole(MobileParty party, Hero companion, SkillEffect.PerkRole role)
        {
            if (party == null || companion == null) return;
            switch (role)
            {
                case SkillEffect.PerkRole.Surgeon:      party.SetPartySurgeon(companion);      break;
                case SkillEffect.PerkRole.Quartermaster: party.SetPartyQuartermaster(companion); break;
                case SkillEffect.PerkRole.Scout:        party.SetPartyScout(companion);        break;
                case SkillEffect.PerkRole.Engineer:     party.SetPartyEngineer(companion);     break;
            }
        }
    }
}
```

---

## Dynastic Lifecycle Actions

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace CalradiaForge.DynastyExtensions
{
    public static class DynastyActionHelper
    {
        public static void ArrangeNobleMarriage(Hero suitor, Hero maiden)
        {
            if (suitor == null || maiden == null) return;
            if (Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(suitor, maiden))
            {
                MarriageAction.Apply(suitor, maiden, showNotification: true);
            }
        }

        public static void ExecuteHeirSuccession(Hero newHeir)
        {
            if (newHeir == null || !newHeir.IsAlive || newHeir.Age < 18) return;
            ChangePlayerCharacterAction.Apply(newHeir);
            ChangeClanLeaderAction.Apply(Clan.PlayerClan, newHeir);
        }
    }
}
```
