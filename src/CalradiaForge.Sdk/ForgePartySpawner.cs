using System;
using System.Collections.Generic;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Builder and validator for procedural MobileParty spawning and AI behaviors
    /// adhering to Bannerlord party spawner and save safety architecture.
    /// </summary>
    public class ForgePartyBlueprint
    {
        public string PartyStringId { get; set; }
        public string Name { get; set; }
        public string FactionStringId { get; set; }
        public string HomeSettlementStringId { get; set; }
        public int WageBudgetLimit { get; set; } = 1500;
        public float StartingFood { get; set; } = 40f;
        public string AiBehavior { get; set; } = "Patrol";
        public Dictionary<string, int> TroopRoster { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public ForgePartyBlueprint SetIdentity(string stringId, string name, string factionId)
        {
            PartyStringId = stringId;
            Name = name;
            FactionStringId = factionId;
            return this;
        }

        public ForgePartyBlueprint SetHomeSettlement(string settlementId)
        {
            HomeSettlementStringId = settlementId;
            return this;
        }

        public ForgePartyBlueprint SetBudget(int wageLimit, float food)
        {
            WageBudgetLimit = wageLimit;
            StartingFood = food;
            return this;
        }

        public ForgePartyBlueprint SetBehavior(string behavior)
        {
            AiBehavior = behavior;
            return this;
        }

        public ForgePartyBlueprint AddTroop(string troopCharacterId, int count)
        {
            if (string.IsNullOrWhiteSpace(troopCharacterId) || count <= 0) return this;
            if (TroopRoster.ContainsKey(troopCharacterId)) TroopRoster[troopCharacterId] += count;
            else TroopRoster[troopCharacterId] = count;
            return this;
        }

        public (bool isValid, List<string> errors) Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(PartyStringId))
                errors.Add("PartyStringId cannot be empty.");

            if (string.IsNullOrWhiteSpace(FactionStringId))
                errors.Add("FactionStringId cannot be empty.");

            if (TroopRoster.Count == 0)
                errors.Add("Party blueprint must specify at least one troop character in the roster.");

            int totalTroops = 0;
            foreach (var count in TroopRoster.Values) totalTroops += count;

            if (totalTroops <= 0)
                errors.Add("Total troop count must be greater than zero.");

            if (WageBudgetLimit < 0)
                errors.Add("Wage budget limit cannot be negative.");

            if (StartingFood < 0f)
                errors.Add("Starting food cannot be negative.");

            var validBehaviors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Patrol", "DefendSettlement", "Raid", "Flee", "EngageTarget", "Escort"
            };

            if (!validBehaviors.Contains(AiBehavior))
                errors.Add($"Unknown AI behavior '{AiBehavior}'. Valid: Patrol, DefendSettlement, Raid, Flee, EngageTarget, Escort.");

            return (errors.Count == 0, errors);
        }
    }

    public static class ForgePartySpawner
    {
        public static ForgePartyBlueprint CreateBlueprint(string partyId, string name, string factionId)
        {
            return new ForgePartyBlueprint().SetIdentity(partyId, name, factionId);
        }

        public static int EstimateRosterWage(Dictionary<string, int> troopCounts, Func<string, int> tierLookup)
        {
            if (troopCounts == null || troopCounts.Count == 0) return 0;
            long totalWage = 0;
            foreach (var kvp in troopCounts)
            {
                if (kvp.Value <= 0) continue;
                int tier = 2;
                if (tierLookup != null)
                {
                    try { tier = Math.Max(0, tierLookup(kvp.Key)); }
                    catch { tier = 2; }
                }
                long wagePerTroop = Math.Max(2, tier * 3);
                totalWage += wagePerTroop * kvp.Value;
            }
            return (int)Math.Min(int.MaxValue, totalWage);
        }
    }
}
