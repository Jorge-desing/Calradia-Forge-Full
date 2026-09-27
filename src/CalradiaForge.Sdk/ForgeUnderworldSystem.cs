using System;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Rogue mechanics, alley rackets, smuggling margins, and crime rating calculations
    /// based on Bannerlord CrimeModel and underworld systems.
    /// </summary>
    public static class ForgeUnderworldSystem
    {
        public static (int dailyGold, float dailyCrimeGain) CalculateAlleyDailyYield(int thugCount, int settlementProsperity, float securityLevel)
        {
            if (thugCount <= 0) return (0, 0f);

            settlementProsperity = Math.Max(0, settlementProsperity);
            securityLevel = Math.Max(0f, Math.Min(100f, securityLevel));

            // Thugs extort local merchants in proportion to prosperity, reduced by garrison security
            float prosperityFactor = settlementProsperity / 1000f;
            float securitySuppression = Math.Max(0.2f, 1.0f - (securityLevel / 100f) * 0.6f);

            int gold = (int)(thugCount * 12f * prosperityFactor * securitySuppression);
            float crimeGain = thugCount * 0.15f * (1.0f + (securityLevel / 100f));

            return (gold, crimeGain);
        }

        public static (int netProfitPerUnit, float riskFactor) CalculateSmugglingMargin(int purchasePrice, int destinationPrice, float tariffRate, float borderGuardBribery)
        {
            purchasePrice = Math.Max(0, purchasePrice);
            destinationPrice = Math.Max(0, destinationPrice);
            tariffRate = Math.Max(0f, Math.Min(1.0f, tariffRate));
            borderGuardBribery = Math.Max(0f, borderGuardBribery);

            int grossMargin = destinationPrice - purchasePrice;
            float evasionBenefit = destinationPrice * tariffRate;
            int netProfit = (int)(grossMargin + evasionBenefit - borderGuardBribery);

            float riskFactor = Math.Max(0.05f, Math.Min(0.95f, (destinationPrice * tariffRate) / 1000f));
            return (netProfit, riskFactor);
        }

        public static float CalculateCrimeDecay(float currentCrimeRating, int settlementSecurity, bool hasActiveRackets)
        {
            if (currentCrimeRating <= 0f) return 0f;

            settlementSecurity = Math.Max(0, Math.Min(100, settlementSecurity));

            // Security accelerates criminal investigation and arrests, decaying crime rating
            float baseDecay = 1.0f;
            float securityDecay = (settlementSecurity / 100f) * 1.5f;

            // Active rackets replenish crime footprint
            float racketPenalty = hasActiveRackets ? -1.2f : 0f;

            float netDecay = baseDecay + securityDecay + racketPenalty;
            float newRating = currentCrimeRating - Math.Max(0f, netDecay);
            return Math.Max(0f, newRating);
        }
    }
}
