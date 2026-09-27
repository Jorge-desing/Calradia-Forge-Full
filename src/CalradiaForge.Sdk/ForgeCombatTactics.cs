using System;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Tactical AI distance evaluation, formation morale shock, and siege breach calculations
    /// based on Bannerlord combat AI and siege mechanics.
    /// </summary>
    public static class ForgeCombatTactics
    {
        public static float CalculateMoraleShock(int casualtiesInflicted, int initialTroopCount, bool isFlanked, bool isCommanderKilled)
        {
            if (initialTroopCount <= 0) return 100f;

            float casualtyRatio = (float)casualtiesInflicted / initialTroopCount;
            float shock = casualtyRatio * 60f; // up to 60 points from sudden casualties

            if (isFlanked) shock += 15f;
            if (isCommanderKilled) shock += 25f;

            return Math.Min(100f, shock);
        }

        public static float CalculateChargeDistanceThreshold(int cavalryCount, int enemyInfantryBracing, bool hasSpears)
        {
            // Cavalry AI calculates when to transition from skirmish/orbit to charge:
            // High spearmen count causes cavalry to stay back or circle flanks
            float baseDistance = 45f;
            float spearmenHesitation = hasSpears ? (enemyInfantryBracing * 0.4f) : 0f;
            float cavalryConfidence = cavalryCount * 0.2f;

            return Math.Max(15f, baseDistance + spearmenHesitation - cavalryConfidence);
        }

        public static (float breachProbability, float remainingWallHp) CalculateSiegeWallBreachProbability(int trebuchetShots, int catapultShots, int wallTier)
        {
            wallTier = Math.Max(1, Math.Min(3, wallTier));
            trebuchetShots = Math.Max(0, trebuchetShots);
            catapultShots = Math.Max(0, catapultShots);

            // Wall HP by tier: Tier 1 = 10,000, Tier 2 = 18,000, Tier 3 = 28,000
            float maxHp = wallTier == 1 ? 10000f : (wallTier == 2 ? 18000f : 28000f);

            float damage = (trebuchetShots * 450f) + (catapultShots * 220f);
            float remainingHp = Math.Max(0f, maxHp - damage);

            float damagePct = (damage / maxHp);
            float breachProb = (float)Math.Pow(Math.Min(1.0f, damagePct), 2.5); // Steep curve towards breach

            return (breachProb, remainingHp);
        }

        public static float CalculateGateDamage(int ramHits, int gateTier, float defenderOilBoiling)
        {
            ramHits = Math.Max(0, ramHits);
            gateTier = Math.Max(1, Math.Min(3, gateTier));
            defenderOilBoiling = Math.Max(0f, defenderOilBoiling);

            // Gate tier resistance
            float basePerHit = 250f;
            float gateArmor = gateTier * 40f;
            float oilDamageSuppression = 1.0f - Math.Min(0.5f, defenderOilBoiling * 0.1f);

            float effectivePerHit = Math.Max(50f, (basePerHit - gateArmor) * oilDamageSuppression);
            return effectivePerHit * ramHits;
        }
    }
}
