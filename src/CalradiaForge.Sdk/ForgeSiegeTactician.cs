using System;
using System.Collections.Generic;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Advanced siege mechanics, wall breach thresholds, and detachment AI tactical evaluator.
    /// Adheres to bannerlord-siege-mechanics and bannerlord_mission_lifecycle rules.
    /// </summary>
    public static class ForgeSiegeTactician
    {
        public struct SiegeAssaultAnalysis
        {
            public string SettlementName;
            public int AttackerStrength;
            public int DefenderStrength;
            public bool HasRam;
            public int TowerCount;
            public int WallBreachCount;
            public float AttackerExpectedCasualtyRate;
            public float DefenderExpectedCasualtyRate;
            public float BreakthroughProbability;
            public string TacticalAdvice;
            public List<string> NavmeshRequirements;
        }

        /// <summary>
        /// Analyzes breach probability, machinery effectiveness, and casualty projections for a siege assault.
        /// </summary>
        public static SiegeAssaultAnalysis AnalyzeAssaultTactics(string settlementName, int attackers, int defenders,
            bool hasRam, int towerCount, int wallBreaches)
        {
            if (string.IsNullOrWhiteSpace(settlementName)) settlementName = "Chaikand";
            attackers = Math.Max(10, attackers);
            defenders = Math.Max(10, defenders);
            towerCount = Math.Max(0, Math.Min(2, towerCount));
            wallBreaches = Math.Max(0, Math.Min(2, wallBreaches));

            float forceRatio = (float)attackers / defenders;
            float attackerCasualtyRate = 0.45f; // Baseline ladder assault casualty rate (45%)
            float defenderCasualtyRate = 0.20f;
            float breakthroughChance = 0.50f;

            var navmeshes = new List<string>(8)
            {
                "navmesh_gatehouse_interior",
                "navmesh_courtyard_perimeter"
            };

            if (wallBreaches > 0)
            {
                // Wall breaches bypass bottlenecks, drastically reducing attacker casualties
                attackerCasualtyRate -= wallBreaches * 0.14f;
                defenderCasualtyRate += wallBreaches * 0.18f;
                breakthroughChance += wallBreaches * 0.20f;
                for (int i = 1; i <= wallBreaches; i++)
                    navmeshes.Add($"dynamic_navmesh_wall_breach_{i}");
            }

            if (hasRam)
            {
                // Gate ram forces main gate opening, establishing direct infantry route
                attackerCasualtyRate -= 0.08f;
                defenderCasualtyRate += 0.10f;
                breakthroughChance += 0.12f;
                navmeshes.Add("dynamic_navmesh_outer_gate_breached");
                navmeshes.Add("dynamic_navmesh_inner_gate_breached");
            }

            if (towerCount > 0)
            {
                // Towers provide covered ramp access, significantly safer than open ladders
                attackerCasualtyRate -= towerCount * 0.09f;
                breakthroughChance += towerCount * 0.10f;
                for (int i = 1; i <= towerCount; i++)
                    navmeshes.Add($"dynamic_navmesh_siege_tower_ramp_{i}");
            }
            else if (wallBreaches == 0)
            {
                // Pure ladder assault: highest casualty rate
                attackerCasualtyRate += 0.15f;
                navmeshes.Add("navmesh_ladder_ascent_left");
                navmeshes.Add("navmesh_ladder_ascent_right");
            }

            // Ratio modifier
            if (forceRatio >= 3.0f) breakthroughChance += 0.20f;
            else if (forceRatio < 1.5f) breakthroughChance -= 0.25f;

            attackerCasualtyRate = Math.Max(0.08f, Math.Min(0.85f, attackerCasualtyRate));
            defenderCasualtyRate = Math.Max(0.05f, Math.Min(0.95f, defenderCasualtyRate));
            breakthroughChance = Math.Max(0.05f, Math.Min(0.98f, breakthroughChance));

            string advice = (wallBreaches >= 1)
                ? "Breach assault viable. Direct shock infantry through breached walls; avoid bottlenecks."
                : (towerCount >= 1 || hasRam)
                    ? "Machinery assault ready. Coordinate simultaneous ram breach and tower ramp drops."
                    : "CAUTION: Pure ladder assault incurs heavy bottleneck casualties. Build siege engines before assault.";

            return new SiegeAssaultAnalysis
            {
                SettlementName = settlementName,
                AttackerStrength = attackers,
                DefenderStrength = defenders,
                HasRam = hasRam,
                TowerCount = towerCount,
                WallBreachCount = wallBreaches,
                AttackerExpectedCasualtyRate = (float)Math.Round(attackerCasualtyRate, 2),
                DefenderExpectedCasualtyRate = (float)Math.Round(defenderCasualtyRate, 2),
                BreakthroughProbability = (float)Math.Round(breakthroughChance, 2),
                TacticalAdvice = advice,
                NavmeshRequirements = navmeshes
            };
        }
    }
}
