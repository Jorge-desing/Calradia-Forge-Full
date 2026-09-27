using System;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Settlement simulation and rebellion risk analysis based on Bannerlord town/castle equilibrium models.
    /// </summary>
    public static class ForgeSettlementSystem
    {
        public static float CalculateDailyLoyaltyDelta(bool cultureMismatch, bool governorCultureMatch, int foodSurplus, int taxes, int corruption)
        {
            taxes = Math.Max(0, taxes);
            corruption = Math.Max(0, corruption);

            float delta = 0f;
            if (cultureMismatch) delta -= 3.0f;
            if (governorCultureMatch) delta += 1.0f;

            if (foodSurplus < 0)
            {
                // Starvation heavily degrades loyalty
                delta += Math.Max(-4.0f, foodSurplus * 0.5f);
            }
            else if (foodSurplus > 5)
            {
                delta += 0.5f;
            }

            delta -= taxes * 0.2f;
            delta -= corruption * 0.4f;

            return delta;
        }

        public static float CalculateDailySecurityDelta(int garrisonSize, int militiaSize, int activeUnderworldRackets, bool banditLairNearby)
        {
            garrisonSize = Math.Max(0, garrisonSize);
            militiaSize = Math.Max(0, militiaSize);
            activeUnderworldRackets = Math.Max(0, activeUnderworldRackets);

            float garrisonPower = garrisonSize * 0.015f;
            float militiaPower = militiaSize * 0.005f;
            float racketPenalty = activeUnderworldRackets * 0.75f;
            float lairPenalty = banditLairNearby ? 1.5f : 0f;

            return (garrisonPower + militiaPower) - (racketPenalty + lairPenalty);
        }

        public static (bool isCritical, float dangerIndex, string status) EvaluateRebellionRisk(float currentLoyalty, int militiaSize, int garrisonSize)
        {
            // Bannerlord Rebellion rules:
            // Loyalty <= 25 starts the rebellion countdown if militia significantly outnumbers garrison.
            // When loyalty hits <= 15 and militia > 2 * garrison, rebellion trigger is critical.
            militiaSize = Math.Max(0, militiaSize);
            garrisonSize = Math.Max(0, garrisonSize);
            float militiaRatio = garrisonSize > 0 ? (float)militiaSize / garrisonSize : militiaSize;
            float dangerIndex = 0f;

            if (currentLoyalty <= 15f) dangerIndex += 60f;
            else if (currentLoyalty <= 25f) dangerIndex += 35f;
            else if (currentLoyalty <= 40f) dangerIndex += 10f;

            if (militiaRatio > 2.0f) dangerIndex += 40f;
            else if (militiaRatio > 1.2f) dangerIndex += 20f;

            bool isCritical = currentLoyalty <= 25f && militiaRatio > 2.0f;
            string status = isCritical
                ? "Imminent Rebellion"
                : (dangerIndex >= 50f ? "Unrest High" : (dangerIndex >= 25f ? "Discontent" : "Stable"));

            return (isCritical, Math.Min(100f, dangerIndex), status);
        }
    }
}
