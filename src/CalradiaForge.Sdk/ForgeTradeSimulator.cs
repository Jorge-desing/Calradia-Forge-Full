using System;
using System.Collections.Generic;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Interactive economic simulation engine for Calradia's town markets and workshops.
    /// Computes supply/demand curves, price elasticity, workshop production batches, and ROI.
    /// Adheres to bannerlord-economy-trade and bannerlord-inventory-barter principles.
    /// </summary>
    public static class ForgeTradeSimulator
    {
        public struct WorkshopSimResult
        {
            public string SettlementName;
            public string WorkshopType;
            public int InitialCapital;
            public int FinalCapital;
            public int TotalProduced;
            public int TotalInputConsumed;
            public int TotalWagesPaid;
            public int TotalRevenue;
            public int NetProfit;
            public float DailyRoiPercentage;
            public string Summary;
        }

        public struct TradeMarginResult
        {
            public string ItemCategory;
            public float BasePrice;
            public float LocalPrice;
            public float PriceMultiplier;
            public float SupplyDemandRatio;
            public string MarketCondition; // Surplus, Balanced, Deficit, Famine
        }

        /// <summary>
        /// Calculates price multiplier based on supply and demand equilibrium with Bannerlord elasticity curves.
        /// </summary>
        public static TradeMarginResult CalculateMarketEquilibrium(string itemCategory, float supply, float demand, float basePrice)
        {
            if (string.IsNullOrWhiteSpace(itemCategory)) itemCategory = "GeneralGoods";
            supply = Math.Max(1f, supply);
            demand = Math.Max(1f, demand);
            basePrice = Math.Max(1f, basePrice);

            float ratio = supply / demand;
            // Bannerlord price curve: prices range from ~0.3x (massive surplus) to ~3.0x (severe deficit)
            float multiplier = (float)Math.Pow(1f / ratio, 0.45);
            multiplier = Math.Max(0.35f, Math.Min(3.2f, multiplier));
            float localPrice = (float)Math.Round(basePrice * multiplier, 1);

            string condition = ratio > 1.7f ? "Severe Surplus" :
                               ratio > 1.2f ? "Moderate Surplus" :
                               ratio > 0.8f ? "Balanced Market" :
                               ratio > 0.4f ? "Deficit" : "Critical Shortage";

            return new TradeMarginResult
            {
                ItemCategory = itemCategory,
                BasePrice = basePrice,
                LocalPrice = localPrice,
                PriceMultiplier = (float)Math.Round(multiplier, 2),
                SupplyDemandRatio = (float)Math.Round(ratio, 2),
                MarketCondition = condition
            };
        }

        /// <summary>
        /// Simulates workshop economic performance over a specified number of campaign days.
        /// Accounts for daily worker wages, input purchasing, output selling, and capital limits.
        /// </summary>
        public static WorkshopSimResult SimulateWorkshopRoi(string settlementName, string workshopType,
            int initialCapital = 10000, int simulationDays = 30, float marketDemand = 1.0f)
        {
            if (string.IsNullOrWhiteSpace(settlementName)) settlementName = "Pravend";
            if (string.IsNullOrWhiteSpace(workshopType)) workshopType = "Brewery";
            initialCapital = Math.Max(1000, initialCapital);
            simulationDays = Math.Max(1, Math.Min(365, simulationDays));
            marketDemand = Math.Max(0.2f, Math.Min(3.0f, marketDemand));

            int currentCapital = initialCapital;
            int totalProduced = 0;
            int totalConsumed = 0;
            int totalWages = 0;
            int totalRevenue = 0;

            const int dailyWage = 60; // Standard Bannerlord workshop daily upkeep
            float inputBaseCost = 15f; // Default Grain base cost
            float outputBasePrice = 45f; // Default Beer base price

            // Differentiate economics based on workshop type
            string wt = workshopType.ToLowerInvariant();
            if (wt.Contains("silver")) { inputBaseCost = 120f; outputBasePrice = 320f; }
            else if (wt.Contains("smith")) { inputBaseCost = 45f; outputBasePrice = 140f; }
            else if (wt.Contains("tann")) { inputBaseCost = 30f; outputBasePrice = 95f; }
            else if (wt.Contains("weav")) { inputBaseCost = 25f; outputBasePrice = 80f; }
            else if (wt.Contains("wood")) { inputBaseCost = 20f; outputBasePrice = 65f; }
            else if (wt.Contains("potter")) { inputBaseCost = 12f; outputBasePrice = 42f; }

            for (int day = 1; day <= simulationDays; day++)
            {
                // Pay wages
                currentCapital -= dailyWage;
                totalWages += dailyWage;

                // Check solvency
                if (currentCapital < dailyWage)
                {
                    break;
                }

                // Production cycle: consumes 2 inputs to create 1 output per batch, 2 batches per day
                int batches = (currentCapital >= inputBaseCost * 4) ? 2 : 1;
                int inputNeeded = batches * 2;
                float inputCost = inputNeeded * inputBaseCost;

                if (currentCapital >= inputCost)
                {
                    currentCapital -= (int)inputCost;
                    totalConsumed += inputNeeded;

                    int outputCount = batches;
                    totalProduced += outputCount;

                    // Revenue from selling output at market price adjusted by demand
                    float salePrice = outputBasePrice * marketDemand;
                    int revenue = (int)(outputCount * salePrice);
                    currentCapital += revenue;
                    totalRevenue += revenue;
                }
            }

            int netProfit = currentCapital - initialCapital;
            float roi = ((float)netProfit / initialCapital) * 100f;
            float dailyRoi = roi / simulationDays;

            string summary = $"[Workshop Simulation: {workshopType} @ {settlementName}]\n" +
                             $"• Duration: {simulationDays} days | Initial Capital: {initialCapital}d\n" +
                             $"• Final Capital: {currentCapital}d | Net Profit: {netProfit:+0;-0;0}d\n" +
                             $"• Units Produced: {totalProduced} | Input Consumed: {totalConsumed}\n" +
                             $"• Wages Paid: {totalWages}d | Total Revenue: {totalRevenue}d\n" +
                             $"• Daily ROI: {dailyRoi:+0.00;-0.00;0.00}% (Total ROI: {roi:+0.0;-0.0;0.0}%)\n" +
                             $"• Status: {(netProfit > 0 ? "PROFITABLE ENTERPRISE" : "UNPROFITABLE / LOSS")}";

            return new WorkshopSimResult
            {
                SettlementName = settlementName,
                WorkshopType = workshopType,
                InitialCapital = initialCapital,
                FinalCapital = currentCapital,
                TotalProduced = totalProduced,
                TotalInputConsumed = totalConsumed,
                TotalWagesPaid = totalWages,
                TotalRevenue = totalRevenue,
                NetProfit = netProfit,
                DailyRoiPercentage = (float)Math.Round(dailyRoi, 2),
                Summary = summary
            };
        }
    }
}
