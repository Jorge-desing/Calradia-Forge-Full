using System;
using System.Collections.Generic;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Geopolitical war-justification scoring engine and KingdomDecision voting simulator.
    /// Adheres strictly to bannerlord-kingdom-diplomacy rules.
    /// </summary>
    public static class ForgeCasusBelliEngine
    {
        public struct CasusBelliAnalysis
        {
            public string AttackerKingdom;
            public string TargetKingdom;
            public int TotalJustificationScore; // 0-100
            public bool IsWarJustified;
            public int ClashingBorderSettlements;
            public float RelativeStrengthRatio;
            public bool HasMarriagePact;
            public int CouncilSupportPercentage;
            public int EstimatedTributeDaily;
            public string DominantReason;
            public string Summary;
        }

        /// <summary>
        /// Evaluates geopolitical justification score for declaring war between two realms.
        /// </summary>
        public static CasusBelliAnalysis EvaluateWarJustification(string attackerKingdom, string targetKingdom,
            int attackerStrength, int targetStrength, int borderingSettlements, bool hasMarriagePact,
            int pastRaidsCount, int diplomaticRelation)
        {
            if (string.IsNullOrWhiteSpace(attackerKingdom)) attackerKingdom = "Vlandia";
            if (string.IsNullOrWhiteSpace(targetKingdom)) targetKingdom = "Battania";
            attackerStrength = Math.Max(100, attackerStrength);
            targetStrength = Math.Max(100, targetStrength);
            borderingSettlements = Math.Max(0, borderingSettlements);
            pastRaidsCount = Math.Max(0, pastRaidsCount);
            diplomaticRelation = Math.Max(-100, Math.Min(100, diplomaticRelation));

            int score = 0;
            string dominantReason = "Border Friction";

            // 1. Border Friction (up to 30 points)
            int borderScore = Math.Min(30, borderingSettlements * 6);
            score += borderScore;

            // 2. Relative Military Advantage (up to 30 points)
            float strengthRatio = (float)attackerStrength / targetStrength;
            if (strengthRatio > 1.8f)
            {
                score += 30;
                dominantReason = "Overwhelming Military Dominance";
            }
            else if (strengthRatio > 1.2f)
            {
                score += 18;
                dominantReason = "Favorable Military Balance";
            }
            else if (strengthRatio < 0.7f)
            {
                score -= 20; // Realm is too weak to attack
            }

            // 3. Past Raids & Grievances (up to 25 points)
            int raidScore = Math.Min(25, pastRaidsCount * 5);
            score += raidScore;
            if (pastRaidsCount >= 3) dominantReason = "Retaliation for Village Raids";

            // 4. Diplomatic Relations
            if (diplomaticRelation < -40) score += 20;
            else if (diplomaticRelation > 30) score -= 35;

            // 5. Dynastic Marriage Pact (Major deterrent: -40 points)
            if (hasMarriagePact)
            {
                score -= 40;
                if (score < 40) dominantReason = "Deterred by Royal Marriage Alliance";
            }

            score = Math.Max(0, Math.Min(100, score));
            bool isJustified = score >= 55;

            // Council voting simulation
            int councilSupport = (int)Math.Round(score * 0.95);
            councilSupport = Math.Max(5, Math.Min(95, councilSupport));

            // Estimated daily tribute balance if war ends in peace
            int tribute = 0;
            if (strengthRatio > 1.5f)
            {
                float calculated = (strengthRatio - 1.0f) * 1200f;
                tribute = (int)Math.Min(25000f, calculated);
            }
            else if (strengthRatio < 0.7f)
            {
                float calculated = -(1.0f - strengthRatio) * 1500f;
                tribute = (int)Math.Max(-25000f, calculated);
            }

            string summary = $"[Casus Belli Audit: {attackerKingdom} vs {targetKingdom}]\n" +
                             $"• Justification Score: {score}/100 ({(isJustified ? "LEGITIMATE CASUS BELLI" : "UNJUSTIFIED AGGRESSION")})\n" +
                             $"• Dominant Factor: {dominantReason}\n" +
                             $"• Forces: {attackerStrength} vs {targetStrength} (Ratio: {strengthRatio:0.00}x)\n" +
                             $"• Border Contests: {borderingSettlements} settlements | Past Raids: {pastRaidsCount}\n" +
                             $"• Royal Marriage Pact: {(hasMarriagePact ? "Active (Deterrent)" : "None")}\n" +
                             $"• Noble Council Support: {councilSupport}% in favor of war\n" +
                             $"• Projected Peace Tribute: {(tribute > 0 ? $"+{tribute}d/day to {attackerKingdom}" : tribute < 0 ? $"{tribute}d/day to {targetKingdom}" : "Equal status")}";

            return new CasusBelliAnalysis
            {
                AttackerKingdom = attackerKingdom,
                TargetKingdom = targetKingdom,
                TotalJustificationScore = score,
                IsWarJustified = isJustified,
                ClashingBorderSettlements = borderingSettlements,
                RelativeStrengthRatio = (float)Math.Round(strengthRatio, 2),
                HasMarriagePact = hasMarriagePact,
                CouncilSupportPercentage = councilSupport,
                EstimatedTributeDaily = tribute,
                DominantReason = dominantReason,
                Summary = summary
            };
        }
    }
}
