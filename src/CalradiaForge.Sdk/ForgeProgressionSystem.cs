using System;
using System.Collections.Generic;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Character progression, perk role evaluation, and clan succession models
    /// based on Bannerlord CharacterDevelopmentModel and ClanTierModel.
    /// </summary>
    public static class ForgeProgressionSystem
    {
        private static readonly int[] ClanTierRenownThresholds = new[]
        {
            0,      // Tier 0
            50,     // Tier 1
            150,    // Tier 2
            350,    // Tier 3
            900,    // Tier 4
            2350,   // Tier 5
            6000    // Tier 6
        };

        private static readonly Dictionary<string, HashSet<string>> PerkRoleMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "PartyLeader", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "leadership_disciplinarian", "tactics_swift_strike", "steward_swords_as_tribute" } },
            { "Surgeon", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "medicine_triage_tent", "medicine_preventive_medicine", "medicine_cheat_death" } },
            { "Engineer", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "engineering_dungeon_architect", "engineering_improved_masonry", "engineering_scaffolding" } },
            { "Scout", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "scouting_day_walker", "scouting_pathfinder", "scouting_vanguard" } },
            { "Quartermaster", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "steward_bannerlords", "steward_logistics", "steward_master_of_warcraft" } }
        };

        public static float CalculateLearningRate(int attributeValue, int focusPoints, int currentSkillLevel)
        {
            attributeValue = Math.Max(0, attributeValue);
            focusPoints = Math.Max(0, Math.Min(5, focusPoints));
            currentSkillLevel = Math.Max(0, currentSkillLevel);

            // Bannerlord learning rate formula:
            // Learning limit = (attributeValue * 14) + (focusPoints * 40) - 10
            // Base learning rate = (attributeValue * 0.4) + (focusPoints * 1.0)
            int learningLimit = (attributeValue * 14) + (focusPoints * 40) - 10;
            float baseRate = (attributeValue * 0.4f) + (focusPoints * 1.0f);

            if (currentSkillLevel >= learningLimit)
            {
                // Rapid falloff beyond learning limit down to minimum 0.0x
                float excess = currentSkillLevel - learningLimit;
                float multiplier = Math.Max(0.0f, 1.0f - (excess / 60.0f));
                return baseRate * multiplier;
            }

            return Math.Max(0.1f, baseRate);
        }

        public static int GetClanTierThreshold(int tier)
        {
            int clamped = Math.Max(0, Math.Min(ClanTierRenownThresholds.Length - 1, tier));
            return ClanTierRenownThresholds[clamped];
        }

        public static int EvaluateClanTier(float currentRenown)
        {
            for (int i = ClanTierRenownThresholds.Length - 1; i >= 0; i--)
            {
                if (currentRenown >= ClanTierRenownThresholds[i])
                    return i;
            }
            return 0;
        }

        public static float EvaluateDynasticSuccession(int age, int leadership, int martialSkill, int relationWithLords)
        {
            if (age < 18) return 0f; // Minor cannot rule

            leadership = Math.Max(0, leadership);
            martialSkill = Math.Max(0, martialSkill);
            relationWithLords = Math.Max(-100, Math.Min(100, relationWithLords));

            float maturityScore = Math.Min(40f, age * 0.8f);
            float competence = (leadership * 0.35f) + (martialSkill * 0.25f);
            float nobilitySupport = relationWithLords * 0.4f;

            return Math.Max(0f, maturityScore + competence + nobilitySupport);
        }

        public static bool DoesPerkApplyToRole(string perkId, string role)
        {
            if (string.IsNullOrEmpty(perkId) || string.IsNullOrEmpty(role)) return false;
            if (PerkRoleMap.TryGetValue(role, out var perks))
            {
                return perks.Contains(perkId);
            }
            return false;
        }
    }
}
