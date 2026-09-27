using System;
using System.Collections.Generic;

namespace CalradiaForge.Core.SDK.Politics
{
    public static class ForgeKingdomManager
    {
        public static void Initialize() { }

        public static float CalculateRulerAuthority(int rulerInfluence, int totalLordInfluence)
        {
            if (totalLordInfluence <= 0) return 1.0f;
            return Math.Min(1.0f, (float)rulerInfluence / (rulerInfluence + totalLordInfluence * 0.5f));
        }
    }

    public static class ForgeClanManager
    {
        public static void Initialize() { }

        public static int CalculatePartyLimit(int clanTier, bool hasLeadershipPerk)
        {
            // Bannerlord baseline: Tier 0 = 1, Tier 1-2 = 2, Tier 3-4 = 3, Tier 5+ = 4
            int limit = 1;
            if (clanTier >= 5) limit = 4;
            else if (clanTier >= 3) limit = 3;
            else if (clanTier >= 1) limit = 2;

            if (hasLeadershipPerk) limit += 1;
            return limit;
        }

        public static int CalculateCompanionLimit(int clanTier)
        {
            return 3 + clanTier; // Tier 0 = 3, Tier 6 = 9
        }
    }

    public static class ForgeRebellionSystem
    {
        public static void Initialize() { }

        public static bool CheckRebellionTrigger(float settlementLoyalty, int militiaCount, int garrisonCount)
        {
            if (settlementLoyalty > 25f) return false;
            // Rebellion breaks out when loyalty is low and militia outnumbers garrison significantly
            return militiaCount > (garrisonCount * 2);
        }
    }

    public static class ForgePolicyEnforcer
    {
        public static void Initialize() { }

        public static int CalculateVetoCost(int highestOptionVotes, float rulerAuthority)
        {
            float discount = Math.Max(0.2f, rulerAuthority);
            return (int)(highestOptionVotes * 1.5f / discount);
        }
    }

    public static class ForgeVassalRelations
    {
        public static void Initialize() { }

        public static int CalculateFiefAwardRelationDelta(bool isRecipient, int recipientRelationWithLord)
        {
            if (isRecipient) return +25;
            // Other lords lose relation if they had high claims or dislike the recipient
            return recipientRelationWithLord < 0 ? -3 : -1;
        }
    }

    public static class ForgeDiplomacyEngine
    {
        public static void Initialize() { }

        public static float CalculateWarDeclarationScore(int myStrength, int targetStrength, int myWars, int targetWars, int relation)
        {
            float strengthRatio = (float)myStrength / Math.Max(1, targetStrength);
            float warPenalty = myWars * 25f;
            float targetVulnerability = targetWars * 20f;
            float relationFactor = (relation < 0) ? Math.Abs(relation) * 0.5f : -relation;

            return (strengthRatio * 50f) - warPenalty + targetVulnerability + relationFactor;
        }
    }

    public static class ForgeMarriageArranger
    {
        public static void Initialize() { }

        public static float EvaluateMarriageSuitability(int age1, int age2, int clanTier1, int clanTier2, int relation)
        {
            int ageDiff = Math.Abs(age1 - age2);
            float ageScore = Math.Max(0f, 40f - (ageDiff * 2f));
            int tierDiff = Math.Abs(clanTier1 - clanTier2);
            float tierScore = Math.Max(0f, 30f - (tierDiff * 10f));
            float relScore = Math.Max(0f, relation * 0.3f);

            return ageScore + tierScore + relScore;
        }
    }

    public static class ForgeHeirDesignator
    {
        public static void Initialize() { }

        public static float ScoreHeirCandidate(int age, int leadershipSkill, int combatSkill, int nobleRelation)
        {
            if (age < 18) return 0f;
            float maturity = Math.Min(30f, age);
            float leadership = leadershipSkill * 0.4f;
            float combat = combatSkill * 0.2f;
            float relations = nobleRelation * 0.3f;
            return maturity + leadership + combat + relations;
        }
    }

    public static class ForgeElectionRigger
    {
        public static void Initialize() { }

        public static int CalculateVotesForInfluence(int influenceSpent, float corruptionFactor)
        {
            float efficiency = 1.0f + (corruptionFactor * 0.5f);
            return (int)(influenceSpent * efficiency);
        }
    }

    public static class ForgeTreasonSystem
    {
        public static void Initialize() { }

        public static float CalculateDefectionProbability(int relationWithRuler, int kingdomFiefCount, int bribeDenarsOffered)
        {
            if (relationWithRuler > 50) return 0f;
            float disloyalty = (50 - relationWithRuler) * 0.8f;
            float weakness = Math.Max(0f, (10 - kingdomFiefCount) * 2f);
            float bribeBonus = Math.Min(40f, bribeDenarsOffered / 10000f);
            return Math.Min(95f, disloyalty + weakness + bribeBonus);
        }
    }

    public static class ForgeCivilWarTrigger
    {
        public static void Initialize() { }

        public static bool IsCivilWarImminent(float factionCohesion, int disgruntledLordCount, int totalLordCount)
        {
            if (totalLordCount <= 0) return false;
            float disgruntledPct = (float)disgruntledLordCount / totalLordCount;
            return factionCohesion < 20f && disgruntledPct > 0.4f;
        }
    }

    public static class ForgeAllianceBuilder
    {
        public static void Initialize() { }

        public static bool CanFormAlliance(int relation, int commonEnemies, bool sharesBorder)
        {
            return relation >= 40 && commonEnemies >= 1 && sharesBorder;
        }
    }

    public static class ForgeTruceNegotiator
    {
        public static void Initialize() { }

        public static (int dailyTribute, int truceDays) ProposeTruce(int winnerCasualties, int loserCasualties, int winnerFiefsTaken)
        {
            int advantage = (loserCasualties - winnerCasualties) + (winnerFiefsTaken * 500);
            int tribute = Math.Max(0, advantage / 20);
            int duration = 40; // ~half a campaign year
            return (tribute, duration);
        }
    }

    public static class ForgeCasusBelli
    {
        public static void Initialize() { }

        public static float CalculateCasusBelliWeight(bool hasBorderRaid, bool hasHistoricClaim, bool brokenTruce)
        {
            float weight = 0f;
            if (hasBorderRaid) weight += 20f;
            if (hasHistoricClaim) weight += 40f;
            if (brokenTruce) weight += 50f;
            return weight;
        }
    }

    public static class ForgeSpyNetwork
    {
        public static void Initialize() { }

        public static float CalculateInfiltrationSuccess(int roguerySkill, float targetSecurity)
        {
            float skillFactor = roguerySkill * 0.4f;
            float securityDefense = targetSecurity * 0.5f;
            return Math.Max(5f, Math.Min(90f, 50f + skillFactor - securityDefense));
        }
    }

    public static class ForgeAssassinationPlot
    {
        public static void Initialize() { }

        public static (bool successful, bool detected) ResolvePlot(float infiltrationSuccessRate, float targetParanoia)
        {
            bool success = infiltrationSuccessRate > 65f;
            bool detected = targetParanoia > 40f || !success;
            return (success, detected);
        }
    }

    public static class ForgeInfluenceMarket
    {
        public static void Initialize() { }

        public static int DenarsToInfluence(int denars, float merchantRelation)
        {
            float rate = 500f / (1.0f + merchantRelation * 0.005f);
            return (int)(denars / rate);
        }
    }

    public static class ForgeRenownTracker
    {
        public static void Initialize() { }

        public static float CalculateRenownGain(int battleTroopsDefeated, float enemyStrengthAdvantage)
        {
            float baseRenown = battleTroopsDefeated * 0.1f;
            float oddsMultiplier = Math.Max(0.5f, enemyStrengthAdvantage);
            return baseRenown * oddsMultiplier;
        }
    }

    public static class ForgeTitleGranter
    {
        public static void Initialize() { }

        public static string DetermineFeudalTitle(int clanTier, int ownedFiefCount)
        {
            if (clanTier >= 6 && ownedFiefCount >= 4) return "Duke";
            if (clanTier >= 4 && ownedFiefCount >= 2) return "Count";
            if (clanTier >= 2 && ownedFiefCount >= 1) return "Baron";
            return "Knight";
        }
    }

    public static class ForgeFactionSplitter
    {
        public static void Initialize() { }
    }

    public static class ForgeNobleCourt
    {
        public static void Initialize() { }

        public static int CalculateDailyCourtInfluence(int courtierCount, float kingdomProsperityIndex)
        {
            return (int)(courtierCount * 0.5f * kingdomProsperityIndex);
        }
    }
}
