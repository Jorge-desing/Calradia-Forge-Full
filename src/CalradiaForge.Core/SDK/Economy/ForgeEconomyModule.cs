using System;
using System.Collections.Generic;

namespace CalradiaForge.Core.SDK.Economy
{
    public static class ForgeTradeManager
    {
        public static void Initialize() { }

        public static int CalculateTradeProfit(int buyPrice, int sellPrice, int quantity, float marketTaxRate)
        {
            float revenue = sellPrice * quantity * (1.0f - marketTaxRate);
            float cost = buyPrice * quantity;
            return (int)Math.Floor(revenue - cost);
        }

        public static float CalculatePriceFactor(int currentSupply, int idealDemand)
        {
            if (idealDemand <= 0) return 1.0f;
            float ratio = (float)currentSupply / idealDemand;
            // Bannerlord price curve: low supply raises price up to 2.5x, high supply lowers to 0.3x
            if (ratio < 1.0f) return Math.Min(2.5f, 1.0f + (1.0f - ratio) * 1.5f);
            return Math.Max(0.3f, 1.0f - (ratio - 1.0f) * 0.4f);
        }
    }

    public static class ForgeCaravanController
    {
        public static void Initialize() { }

        public static float ScoreTradeDestination(float distance, int estimatedProfit, float routeDanger)
        {
            if (distance <= 0) return 0f;
            float safety = Math.Max(0.1f, 1.0f - routeDanger);
            return (estimatedProfit / distance) * safety;
        }
    }

    public static class ForgeWorkshopManager
    {
        public static void Initialize() { }

        public static int CalculateDailyNetIncome(int rawCost, int outputPrice, int outputPerDay, int dailyWage)
        {
            int revenue = outputPrice * outputPerDay;
            int totalCost = (rawCost * outputPerDay) + dailyWage;
            return revenue - totalCost;
        }
    }

    public static class ForgeMarketFluctuation
    {
        public static void Initialize() { }

        public static float MoveTowardsEquilibrium(float currentPrice, float equilibriumPrice, float dailyConvergenceRate = 0.05f)
        {
            float diff = equilibriumPrice - currentPrice;
            return currentPrice + (diff * dailyConvergenceRate);
        }
    }

    public static class ForgeTaxesController
    {
        public static void Initialize() { }

        public static int CalculateSettlementTaxes(int prosperity, string taxPolicy)
        {
            float rate = 0.08f;
            if (string.Equals(taxPolicy, "High", StringComparison.OrdinalIgnoreCase)) rate = 0.12f;
            else if (string.Equals(taxPolicy, "Low", StringComparison.OrdinalIgnoreCase)) rate = 0.05f;

            return (int)(prosperity * rate);
        }
    }

    public static class ForgeSmugglingSystem
    {
        public static void Initialize() { }

        public static (int netProfit, float seizureChance) EvaluateSmuggling(int baseValue, int quantity, float localTariffRate, float settlementSecurity)
        {
            int tariffSavings = (int)(baseValue * quantity * localTariffRate);
            float risk = (settlementSecurity / 100f) * 0.6f;
            return (tariffSavings, risk);
        }
    }

    public static class ForgeBlackMarket
    {
        public static void Initialize() { }

        public static int CalculateFencingPrice(int standardPrice, int roguerySkill)
        {
            float fenceRatio = 0.5f + (roguerySkill / 300f) * 0.35f;
            return (int)(standardPrice * fenceRatio);
        }
    }

    public static class ForgeLoanSystem
    {
        public static void Initialize() { }

        public static int CalculateDailyInterest(int principal, float annualRate)
        {
            float dailyRate = annualRate / 84f; // Bannerlord campaign year has 84 days
            return (int)Math.Ceiling(principal * dailyRate);
        }
    }

    public static class ForgeBankSystem
    {
        public static void Initialize() { }

        public static int CalculateDepositInterest(int balance, float interestRate)
        {
            return (int)(balance * (interestRate / 84f));
        }
    }

    public static class ForgeInvestmentTracker
    {
        public static void Initialize() { }

        public static float CalculateROI(int totalGains, int initialCapital)
        {
            if (initialCapital <= 0) return 0f;
            return (float)totalGains / initialCapital;
        }
    }

    public static class ForgeResourceDepletion
    {
        public static void Initialize() { }

        public static float CalculateHarvestYield(float currentResourceLevel, float extractionIntensity)
        {
            return Math.Min(currentResourceLevel, currentResourceLevel * extractionIntensity);
        }
    }

    public static class ForgeInflationController
    {
        public static void Initialize() { }

        public static float CalculateInflationMultiplier(long totalCirculatingDenars, long baselineDenars)
        {
            if (baselineDenars <= 0) return 1.0f;
            float ratio = (float)totalCirculatingDenars / baselineDenars;
            return Math.Max(0.5f, Math.Min(3.0f, ratio));
        }
    }

    public static class ForgeTradeRouteOptimizer
    {
        public static void Initialize() { }

        public static string SelectBestGoodToExport(Dictionary<string, (int localPrice, int remotePrice)> priceMap)
        {
            string best = null;
            int maxMargin = 0;
            foreach (var kvp in priceMap)
            {
                int margin = kvp.Value.remotePrice - kvp.Value.localPrice;
                if (margin > maxMargin)
                {
                    maxMargin = margin;
                    best = kvp.Key;
                }
            }
            return best;
        }
    }

    public static class ForgeMerchantGuilds
    {
        public static void Initialize() { }

        public static float GetTariffDiscount(int guildRelation)
        {
            if (guildRelation <= 0) return 0f;
            return Math.Min(0.5f, guildRelation * 0.005f); // up to 50% discount at 100 relation
        }
    }

    public static class ForgeCurrencyExchange
    {
        public static void Initialize() { }

        public static int ExchangeCurrency(int sourceAmount, float exchangeRate, float transactionFee = 0.02f)
        {
            float target = sourceAmount * exchangeRate;
            return (int)Math.Floor(target * (1.0f - transactionFee));
        }
    }

    public static class ForgePricePegging
    {
        public static void Initialize() { }

        public static int ClampPrice(int calculatedPrice, int minAllowed, int maxAllowed)
        {
            return Math.Max(minAllowed, Math.Min(maxAllowed, calculatedPrice));
        }
    }

    public static class ForgeBribeManager
    {
        public static void Initialize() { }

        public static int CalculateGateBribe(int crimeRating, float settlementSecurity)
        {
            return (int)((crimeRating * 20f) * (1.0f + settlementSecurity / 100f));
        }
    }

    public static class ForgeEconomicCrisis
    {
        public static void Initialize() { }

        public static float GetCrisisPriceModifier(bool isUnderSiege, bool isPlagued, bool hasFamine)
        {
            float mod = 1.0f;
            if (isUnderSiege) mod *= 1.8f;
            if (isPlagued) mod *= 1.4f;
            if (hasFamine) mod *= 2.2f;
            return mod;
        }
    }

    public static class ForgeProsperityBooster
    {
        public static void Initialize() { }

        public static float CalculateProsperityGain(int investmentDenars, int currentProsperity)
        {
            float diminishing = 1000f / Math.Max(1000f, (float)currentProsperity);
            return (investmentDenars / 500f) * diminishing;
        }
    }

    public static class ForgeFamineSimulator
    {
        public static void Initialize() { }

        public static int CalculateDailyStarvation(int foodShortage, int settlementPopulation)
        {
            if (foodShortage <= 0) return 0;
            return Math.Min(settlementPopulation, foodShortage * 8);
        }
    }

    public static class ForgeSupplyChainManager
    {
        public static void Initialize() { }

        public static bool CanProduce(int availableRaw, int rawRequiredPerUnit, int desiredOutput)
        {
            return availableRaw >= (rawRequiredPerUnit * desiredOutput);
        }
    }
}
