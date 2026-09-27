using System;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Item transaction underflow safety, price elasticity, and workshop economics
    /// based on Bannerlord inventory and trade architecture.
    /// </summary>
    public static class ForgeTradeSystem
    {
        public static (bool isValid, int allowedAmount, string reason) ValidateItemTransfer(int currentCount, int requestedAmount)
        {
            // Bannerlord ItemRoster Underflow rule:
            // Attempting to remove more items than exist in the roster causes integer underflow or engine assertion crash.
            if (currentCount < 0)
                return (false, 0, "Current roster count is corrupted (< 0).");

            if (requestedAmount <= 0)
                return (false, 0, "Transfer amount must be greater than zero.");

            if (requestedAmount > currentCount)
            {
                return (false, currentCount, $"Underflow prevented: requested {requestedAmount} exceeds available {currentCount}.");
            }

            return (true, requestedAmount, "Transfer permitted.");
        }

        public static int CalculatePriceBySupplyDemand(int basePrice, int localSupply, int idealDemand)
        {
            if (basePrice <= 0) return 1;
            if (idealDemand <= 0) idealDemand = 1;
            localSupply = Math.Max(0, localSupply);

            float ratio = (float)localSupply / idealDemand;

            // Elasticity curve:
            // Scarcity (< 1.0): price scales up to 3.0x base
            // Abundance (> 1.0): price drops to minimum 0.25x base
            float priceMultiplier;
            if (ratio < 1.0f)
            {
                priceMultiplier = 1.0f + (1.0f - ratio) * 2.0f; // up to 3.0x
            }
            else
            {
                priceMultiplier = Math.Max(0.25f, 1.0f - (ratio - 1.0f) * 0.35f);
            }

            int finalPrice = (int)Math.Round(basePrice * priceMultiplier);
            return Math.Max(1, finalPrice);
        }

        public static int CalculateWorkshopDailyNet(int rawMaterialUnitCost, int outputUnitPrice, int productionVolume, int dailyWageOverhead)
        {
            productionVolume = Math.Max(0, productionVolume);
            rawMaterialUnitCost = Math.Max(0, rawMaterialUnitCost);
            outputUnitPrice = Math.Max(0, outputUnitPrice);
            dailyWageOverhead = Math.Max(0, dailyWageOverhead);

            int revenue = outputUnitPrice * productionVolume;
            int materialExpense = rawMaterialUnitCost * productionVolume;
            int totalExpense = materialExpense + dailyWageOverhead;

            return revenue - totalExpense;
        }
    }
}
