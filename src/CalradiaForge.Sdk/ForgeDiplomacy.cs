using System;
using System.Collections.Generic;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Diplomatic calculations, kingdom decision evaluation, and war/peace scoring
    /// based on Bannerlord KingdomDecision and KingdomElection mechanics.
    /// </summary>
    public static class ForgeDiplomacy
    {
        public static float CalculateWarScore(int militaryStrength, int gold, int activeWars, int tributeReceived, int tributePaid)
        {
            float baseScore = militaryStrength * 0.1f;
            float financialFactor = Math.Min(50f, gold / 20000f);
            float warFatiguePenalty = activeWars * 35f;
            float tributeImpact = (tributePaid - tributeReceived) / 100f; // Higher paid tribute incentivizes war to break it

            float total = baseScore + financialFactor - warFatiguePenalty + tributeImpact;
            return Math.Max(0f, total);
        }

        public static int CalculatePeaceTribute(int casualtiesInflicted, int casualtiesSuffered, int settlementsCaptured, int settlementsLost)
        {
            // Advantage calculation with 64-bit integers to prevent overflow on prolonged campaigns:
            long casualtyAdvantage = (long)casualtiesInflicted - casualtiesSuffered;
            long territorialAdvantage = ((long)settlementsCaptured - settlementsLost) * 600L;

            long netScore = casualtyAdvantage + territorialAdvantage;
            // Daily tribute clamped to reasonable Bannerlord values (-15000 to +15000)
            long dailyTribute = netScore / 15L;
            return (int)Math.Max(-15000L, Math.Min(15000L, dailyTribute));
        }

        public static float EvaluateAllianceStability(int relation, int commonEnemies, int geographicDistanceFactor)
        {
            float relScore = Math.Max(-50f, Math.Min(50f, relation * 0.5f));
            float enemyBond = commonEnemies * 20f;
            float distancePenalty = geographicDistanceFactor * 1.5f;

            return Math.Max(0f, relScore + enemyBond - distancePenalty);
        }

        public static (string winningOption, int winningVotes, float winningShare) SimulateElectionOutcome(int totalInfluencePool, Dictionary<string, int> optionVotes)
        {
            if (optionVotes == null || optionVotes.Count == 0)
                return (null, 0, 0f);

            string winner = null;
            int maxVotes = -1;
            int totalCast = 0;

            foreach (var kvp in optionVotes)
            {
                int votes = Math.Max(0, kvp.Value);
                totalCast += votes;
                if (votes > maxVotes)
                {
                    maxVotes = votes;
                    winner = kvp.Key;
                }
            }

            float share = totalCast > 0 ? (float)maxVotes / totalCast : 0f;
            return (winner, maxVotes, share);
        }
    }
}
