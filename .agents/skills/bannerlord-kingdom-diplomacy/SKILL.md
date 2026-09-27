---
name: bannerlord-kingdom-diplomacy
description: Best practices, KingdomDecision, DecisionOutcome, KingdomElection, and diplomatic actions in Mount & Blade II Bannerlord without external detours.
---

# Bannerlord Kingdom Decisions & Diplomacy

This skill provides patterns for implementing custom kingdom council proposals, voting evaluations, policies, and diplomatic actions in Mount & Blade II: Bannerlord.

> **Prerequisites:** Read `bannerlord-shared-patterns` for the SaveableTypeDefiner base pattern, universal safety rules, and SubModule registration.

## 1. Custom KingdomDecision Implementation

```csharp
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace CalradiaForge.DiplomacyExtensions.Decisions
{
    public class WarSubsidiesKingdomDecision : KingdomDecision
    {
        [SaveableField(1)]
        private readonly int _subsidyAmount;

        public WarSubsidiesKingdomDecision(Clan proposerClan, int subsidyAmount) : base(proposerClan)
        {
            _subsidyAmount = subsidyAmount;
        }

        public override bool IsAllowed()
        {
            return Kingdom != null && Kingdom.IsAtWar && ProposerClan != null && !ProposerClan.IsEliminated;
        }

        public override int GetProposalInfluenceCost() => 50;

        public override IEnumerable<DecisionOutcome> DetermineInitialCandidates()
        {
            yield return new WarSubsidiesOutcome(this, shouldSubsidize: true);
            yield return new WarSubsidiesOutcome(this, shouldSubsidize: false);
        }

        public override float DetermineSupport(Clan clan, DecisionOutcome possibleOutcome)
        {
            if (!(possibleOutcome is WarSubsidiesOutcome outcome)) return 0f;

            float score = 0f;
            if (outcome.ShouldSubsidize)
            {
                if (clan.Gold < 50_000) score += 50f;
                if (clan.Leader != null)
                {
                    score += clan.Leader.GetTraitLevel(DefaultTraits.Valor) * 20f;
                    score -= clan.Leader.GetTraitLevel(DefaultTraits.Calculating) * 15f;
                }
            }
            else
            {
                score = -DetermineSupport(clan, new WarSubsidiesOutcome(this, true));
            }
            return score;
        }

        public override void ApplyChosenOutcome(DecisionOutcome chosenOutcome)
        {
            if (chosenOutcome is WarSubsidiesOutcome outcome && outcome.ShouldSubsidize)
            {
                Clan rulingClan = Kingdom.RulingClan;
                if (rulingClan != null && rulingClan.Gold >= 25_000)
                {
                    foreach (Clan vassal in Kingdom.Clans)
                    {
                        if (vassal != rulingClan && vassal.Gold < 30_000)
                        {
                            vassal.Leader?.ChangeHeroGold(5_000);
                        }
                    }
                }
            }
        }

        public override TextObject GetChooseTitle() => new TextObject("{=war_sub_t}War Subsidies Resolution");
        public override TextObject GetChooseDescription() => new TextObject("{=war_sub_d}Distribute treasury funds to struggling vassals during active wartime?");
        public override TextObject GetSupportTitle() => new TextObject("{=war_sub_st}Vote on War Subsidies");
        public override TextObject GetSupportDescription() => new TextObject("{=war_sub_sd}Vassals are voting on royal military subsidy allotments.");
    }

    public class WarSubsidiesOutcome : DecisionOutcome
    {
        [SaveableField(1)]
        public readonly bool ShouldSubsidize;

        public WarSubsidiesOutcome(WarSubsidiesKingdomDecision decision, bool shouldSubsidize)
        {
            ShouldSubsidize = shouldSubsidize;
        }

        public override TextObject GetDecisionTitle() =>
            ShouldSubsidize ? new TextObject("{=out_yes}Enact Subsidies") : new TextObject("{=out_no}Reject Subsidies");

        public override TextObject GetDecisionDescription() =>
            ShouldSubsidize ? new TextObject("{=out_yes_d}Provide financial war chests to vassals.") : new TextObject("{=out_no_d}Preserve kingdom treasury.");
    }
}
```

## 2. SaveableTypeDefiner Registration

```csharp
using System.Collections.Generic;
using TaleWorlds.SaveSystem;
using CalradiaForge.DiplomacyExtensions.Decisions;

namespace CalradiaForge.DiplomacyExtensions.Save
{
    public class PoliticalSaveableTypeDefiner : SaveableTypeDefiner
    {
        public PoliticalSaveableTypeDefiner() : base(2_750_000) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(WarSubsidiesKingdomDecision), 1);
            AddClassDefinition(typeof(WarSubsidiesOutcome), 2);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(List<WarSubsidiesKingdomDecision>));
            ConstructContainerDefinition(typeof(List<WarSubsidiesOutcome>));
        }
    }
}
```

## 3. Diplomatic Actions vs Decisions

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace CalradiaForge.DiplomacyExtensions
{
    public static class DiplomaticActionHelper
    {
        public static void ForcePeaceTreaty(Kingdom k1, Kingdom k2, int dailyTribute = 0)
        {
            if (k1 != null && k2 != null && k1.IsAtWarWith(k2))
            {
                // Enacts immediate peace, transfers prisoners, and updates StanceLink
                MakePeaceAction.Apply(k1, k2, dailyTribute);
            }
        }

        public static void ForceWarDeclaration(Kingdom declarer, Kingdom target)
        {
            if (declarer != null && target != null && !declarer.IsAtWarWith(target))
            {
                DeclareWarAction.Apply(declarer, target);
            }
        }
    }
}
```
