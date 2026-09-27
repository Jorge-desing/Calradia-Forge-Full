using System;
using System.Collections.Generic;
using System.Linq;

namespace CalradiaForge.Core.SDK.UI
{
    public static class ForgeFloatingDamage
    {
        public static void Initialize() { }

        public static string GetDamageHexColor(bool isHeadshot, bool isCrit, string damageType)
        {
            if (isHeadshot) return "#E6CA65"; // Gold / headshot
            if (isCrit) return "#FF4444"; // Red / critical
            if (string.Equals(damageType, "Blunt", StringComparison.OrdinalIgnoreCase)) return "#B0C4DE"; // Steel blue
            return "#FFFFFF"; // Standard white
        }

        public static string FormatDamageNumber(float damage)
        {
            return Math.Round(damage).ToString("0");
        }
    }

    public static class ForgeCustomCrosshair
    {
        public static void Initialize() { }

        public static float CalculateCrosshairSpread(float baseAccuracy, float movementSpeed, bool isAiming)
        {
            float speedPenalty = movementSpeed * 8f;
            float aimingBonus = isAiming ? 0.4f : 1.0f;
            float spread = (100f - baseAccuracy) + speedPenalty;
            return Math.Max(2f, spread * aimingBonus);
        }
    }

    public static class ForgeMinimapOverlay
    {
        public static void Initialize() { }

        public static (float mapX, float mapY) WorldToRadar(float agentX, float agentY, float playerX, float playerY, float radarRadiusWorld, float radarSizePx)
        {
            float dx = agentX - playerX;
            float dy = agentY - playerY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            float scale = (radarSizePx * 0.5f) / radarRadiusWorld;

            if (dist > radarRadiusWorld)
            {
                // Clamp to edge
                dx = (dx / dist) * radarRadiusWorld;
                dy = (dy / dist) * radarRadiusWorld;
            }

            float screenX = (radarSizePx * 0.5f) + (dx * scale);
            float screenY = (radarSizePx * 0.5f) - (dy * scale); // Invert Y for screen
            return (screenX, screenY);
        }
    }

    public static class ForgeHealthBars
    {
        public static void Initialize() { }

        public static string GetHealthBarColor(float currentHp, float maxHp)
        {
            if (maxHp <= 0) return "#808080";
            float ratio = currentHp / maxHp;
            if (ratio > 0.6f) return "#2ECC71"; // Emerald green
            if (ratio > 0.25f) return "#F39C12"; // Amber orange
            return "#E74C3C"; // Crimson red
        }
    }

    public static class ForgeCombatCompass
    {
        public static void Initialize() { }

        public static float CalculateBearingDegrees(float fromX, float fromY, float toX, float toY)
        {
            double angleRad = Math.Atan2(toX - fromX, toY - fromY);
            double degrees = angleRad * (180.0 / Math.PI);
            return (float)((degrees + 360.0) % 360.0);
        }
    }

    public static class ForgeAdvancedKillfeed
    {
        public static void Initialize() { }

        public static string FormatKillEntry(string killerName, string victimName, string weaponName, bool isHeadshot, float distance)
        {
            string distStr = distance > 0 ? $" ({Math.Round(distance)}m)" : "";
            string critStr = isHeadshot ? " [HEADSHOT]" : "";
            return $"{killerName} [{weaponName}]{critStr} {victimName}{distStr}";
        }
    }

    public static class ForgeInventorySort
    {
        public static void Initialize() { }

        public class ItemEntry
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public float Weight { get; set; }
            public int Tier { get; set; }
        }

        public static List<ItemEntry> SortByValue(IEnumerable<ItemEntry> items, bool descending = true)
        {
            return descending
                ? items.OrderByDescending(i => i.Value).ToList()
                : items.OrderBy(i => i.Value).ToList();
        }

        public static List<ItemEntry> SortByTierThenValue(IEnumerable<ItemEntry> items)
        {
            return items.OrderByDescending(i => i.Tier).ThenByDescending(i => i.Value).ToList();
        }
    }

    public static class ForgePartyFilter
    {
        public static void Initialize() { }
    }

    public static class ForgeTroopTreeViewer
    {
        public static void Initialize() { }

        public static int CalculateTreeDepth(Dictionary<string, List<string>> upgradeTree, string rootTroopId)
        {
            if (!upgradeTree.ContainsKey(rootTroopId) || upgradeTree[rootTroopId].Count == 0) return 1;
            int maxChildDepth = 0;
            foreach (var child in upgradeTree[rootTroopId])
            {
                int depth = CalculateTreeDepth(upgradeTree, child);
                if (depth > maxChildDepth) maxChildDepth = depth;
            }
            return 1 + maxChildDepth;
        }
    }

    /// <summary>
    /// Extends Mount &amp; Blade II: Bannerlord's in-game Encyclopedia / Codex system with
    /// custom entry registration, metadata tagging, bookmark management, and category filtering.
    /// </summary>
    public static class ForgeEncyclopediaExtender
    {
        private static readonly object _syncLock = new object();
        private static readonly Dictionary<string, EncyclopediaEntry> _entries = new Dictionary<string, EncyclopediaEntry>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _bookmarks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Dictionary<string, Func<EncyclopediaEntry, bool>>> _filters =
            new Dictionary<string, Dictionary<string, Func<EncyclopediaEntry, bool>>>(StringComparer.OrdinalIgnoreCase);

        public class EncyclopediaEntry
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            public string Description { get; set; }
            public string SourceModule { get; set; }
            public Dictionary<string, string> Attributes { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Tags { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public EncyclopediaEntry() { }

            public EncyclopediaEntry(string id, string name, string category, string description = null, string sourceModule = null)
            {
                Id = id ?? string.Empty;
                Name = name ?? string.Empty;
                Category = category ?? "Concept";
                Description = description ?? string.Empty;
                SourceModule = sourceModule ?? "CalradiaForge";
            }
        }

        public static void Initialize()
        {
            // Backward-compatible hook for game launch initialization
        }

        public static void Clear()
        {
            lock (_syncLock)
            {
                _entries.Clear();
                _bookmarks.Clear();
                _filters.Clear();
            }
        }

        public static bool RegisterEntry(EncyclopediaEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Id)) return false;
            lock (_syncLock)
            {
                _entries[entry.Id] = entry;
                return true;
            }
        }

        public static bool UnregisterEntry(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            lock (_syncLock)
            {
                return _entries.Remove(id);
            }
        }

        public static EncyclopediaEntry GetEntry(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            lock (_syncLock)
            {
                return _entries.TryGetValue(id, out var entry) ? entry : null;
            }
        }

        public static IReadOnlyList<EncyclopediaEntry> GetAllEntries(string category = null)
        {
            lock (_syncLock)
            {
                if (_entries.Count == 0) return Array.Empty<EncyclopediaEntry>();
                if (string.IsNullOrEmpty(category))
                {
                    return new List<EncyclopediaEntry>(_entries.Values);
                }
                var list = new List<EncyclopediaEntry>();
                foreach (var entry in _entries.Values)
                {
                    if (string.Equals(entry.Category, category, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(entry);
                    }
                }
                return list;
            }
        }

        public static IReadOnlyList<EncyclopediaEntry> Search(string query, string category = null)
        {
            if (string.IsNullOrWhiteSpace(query)) return GetAllEntries(category);
            lock (_syncLock)
            {
                var list = new List<EncyclopediaEntry>();
                foreach (var entry in _entries.Values)
                {
                    if (!string.IsNullOrEmpty(category) && !string.Equals(entry.Category, category, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (entry.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        entry.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (!string.IsNullOrEmpty(entry.Description) && entry.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        entry.Tags.Contains(query))
                    {
                        list.Add(entry);
                    }
                }
                return list;
            }
        }

        public static bool AddBookmark(string entryId)
        {
            if (string.IsNullOrWhiteSpace(entryId)) return false;
            lock (_syncLock)
            {
                return _bookmarks.Add(entryId);
            }
        }

        public static bool RemoveBookmark(string entryId)
        {
            if (string.IsNullOrWhiteSpace(entryId)) return false;
            lock (_syncLock)
            {
                return _bookmarks.Remove(entryId);
            }
        }

        public static bool IsBookmarked(string entryId)
        {
            if (string.IsNullOrWhiteSpace(entryId)) return false;
            lock (_syncLock)
            {
                return _bookmarks.Contains(entryId);
            }
        }

        public static IReadOnlyList<string> GetBookmarks()
        {
            lock (_syncLock)
            {
                if (_bookmarks.Count == 0) return Array.Empty<string>();
                return new List<string>(_bookmarks);
            }
        }

        public static void RegisterFilter(string category, string filterTag, Func<EncyclopediaEntry, bool> predicate)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(filterTag) || predicate == null) return;
            lock (_syncLock)
            {
                if (!_filters.TryGetValue(category, out var catFilters))
                {
                    catFilters = new Dictionary<string, Func<EncyclopediaEntry, bool>>(StringComparer.OrdinalIgnoreCase);
                    _filters[category] = catFilters;
                }
                catFilters[filterTag] = predicate;
            }
        }

        public static IReadOnlyList<EncyclopediaEntry> Filter(string category, string filterTag)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(filterTag)) return GetAllEntries(category);
            lock (_syncLock)
            {
                if (!_filters.TryGetValue(category, out var catFilters) || !catFilters.TryGetValue(filterTag, out var predicate))
                {
                    return GetAllEntries(category);
                }
                var list = new List<EncyclopediaEntry>();
                foreach (var entry in _entries.Values)
                {
                    if (string.Equals(entry.Category, category, StringComparison.OrdinalIgnoreCase) && predicate(entry))
                    {
                        list.Add(entry);
                    }
                }
                return list;
            }
        }
    }

    public static class ForgeDialogueOptionsUI
    {
        public static void Initialize() { }
    }

    public static class ForgeTradeProfitUI
    {
        public static void Initialize() { }

        public static (string badgeText, string badgeColor) GetProfitBadge(int purchasePrice, int currentMarketPrice)
        {
            if (purchasePrice <= 0) return ("-", "#808080");
            float margin = (float)(currentMarketPrice - purchasePrice) / purchasePrice;
            if (margin >= 0.30f) return ($"+{Math.Round(margin * 100)}%", "#2ECC71"); // High profit green
            if (margin >= 0.05f) return ($"+{Math.Round(margin * 100)}%", "#C7A45A"); // Modest profit gold
            if (margin >= -0.05f) return ("0%", "#D1D5DB"); // Break even silver
            return ($"{Math.Round(margin * 100)}%", "#E74C3C"); // Loss red
        }
    }

    public static class ForgeKingdomOverviewUI
    {
        public static void Initialize() { }

        public static float CalculatePowerRatio(int kingdomStrength, int enemyStrength)
        {
            int total = kingdomStrength + enemyStrength;
            if (total <= 0) return 0.5f;
            return (float)kingdomStrength / total;
        }
    }

    public static class ForgeClanRolesUI
    {
        public static void Initialize() { }

        public class HeroCandidate
        {
            public string Name { get; set; }
            public int Scouting { get; set; }
            public int Steward { get; set; }
            public int Medicine { get; set; }
            public int Engineering { get; set; }
        }

        public static string PickBestForRole(IEnumerable<HeroCandidate> candidates, string role)
        {
            if (candidates == null || !candidates.Any()) return null;
            if (string.Equals(role, "Scout", StringComparison.OrdinalIgnoreCase))
                return candidates.OrderByDescending(c => c.Scouting).First().Name;
            if (string.Equals(role, "Quartermaster", StringComparison.OrdinalIgnoreCase))
                return candidates.OrderByDescending(c => c.Steward).First().Name;
            if (string.Equals(role, "Surgeon", StringComparison.OrdinalIgnoreCase))
                return candidates.OrderByDescending(c => c.Medicine).First().Name;
            if (string.Equals(role, "Engineer", StringComparison.OrdinalIgnoreCase))
                return candidates.OrderByDescending(c => c.Engineering).First().Name;
            return candidates.First().Name;
        }
    }

    public static class ForgeSiegeHUD
    {
        public static void Initialize() { }

        public static float CalculateBreachPercentage(float currentWallHp, float maxWallHp)
        {
            if (maxWallHp <= 0) return 100f;
            float damage = Math.Max(0f, maxWallHp - currentWallHp);
            return Math.Min(100f, (damage / maxWallHp) * 100f);
        }
    }

    public static class ForgeTournamentBracketUI
    {
        public static void Initialize() { }

        public static List<(string fighter1, string fighter2)> GenerateRoundPairings(List<string> participants)
        {
            var pairings = new List<(string, string)>();
            for (int i = 0; i < participants.Count - 1; i += 2)
            {
                pairings.Add((participants[i], participants[i + 1]));
            }
            return pairings;
        }
    }

    public static class ForgeWeaponStatsUI
    {
        public static void Initialize() { }

        public static float NormalizeStat(float statValue, float maxStatValue)
        {
            if (maxStatValue <= 0) return 0f;
            return Math.Min(1.0f, statValue / maxStatValue);
        }
    }

    public static class ForgeCharacterEditorExtra
    {
        public static void Initialize() { }
    }

    public static class ForgeMapBordersUI
    {
        public static void Initialize() { }
    }

    public static class ForgeArmyMoraleUI
    {
        public static void Initialize() { }

        public static string GetMoraleStatusLabel(float morale)
        {
            if (morale >= 80f) return "Inspired";
            if (morale >= 60f) return "Confident";
            if (morale >= 40f) return "Steady";
            if (morale >= 20f) return "Shaken";
            return "Desperate";
        }
    }

    public static class ForgeGarrisonManagerUI
    {
        public static void Initialize() { }

        public static bool IsWageOverBudget(int currentTotalWage, int wageBudgetLimit)
        {
            return wageBudgetLimit > 0 && currentTotalWage > wageBudgetLimit;
        }
    }

    public static class ForgeWorkshopStatsUI
    {
        public static void Initialize() { }

        public static string FormatDailyProfit(int dailyProfit)
        {
            return dailyProfit >= 0 ? $"+{dailyProfit} d/day" : $"{dailyProfit} d/day";
        }
    }

    public static class ForgeSettlementIcons
    {
        public static void Initialize() { }

        public static List<string> GetStatusIcons(bool isUnderSiege, bool hasFoodShortage, bool hasLowLoyalty, bool hasHighProsperity)
        {
            var icons = new List<string>();
            if (isUnderSiege) icons.Add("siege");
            if (hasFoodShortage) icons.Add("famine");
            if (hasLowLoyalty) icons.Add("rebellion_risk");
            if (hasHighProsperity) icons.Add("prosperous");
            return icons;
        }
    }

    public static class ForgePrisonerRansomUI
    {
        public static void Initialize() { }

        public static int CalculateTotalRansom(IEnumerable<int> individualRansoms)
        {
            return individualRansoms.Sum();
        }
    }

    public static class ForgeRelationshipBarsUI
    {
        public static void Initialize() { }

        public static float MapRelationToBarRatio(int relation)
        {
            // Maps -100..+100 to 0.0..1.0
            int clamped = Math.Max(-100, Math.Min(100, relation));
            return (clamped + 100f) / 200f;
        }
    }

    public static class ForgeSkillTrackerUI
    {
        public static void Initialize() { }

        public static float CalculateSkillProgressPct(int currentXp, int xpRequiredForNext)
        {
            if (xpRequiredForNext <= 0) return 1.0f;
            return Math.Min(1.0f, (float)currentXp / xpRequiredForNext);
        }
    }

    public static class ForgeGoldTrackerUI
    {
        public static void Initialize() { }

        public static int CalculateNetLedger(int dailyIncome, int dailyExpenses)
        {
            return dailyIncome - dailyExpenses;
        }
    }

    public static class ForgeInfluenceGainUI
    {
        public static void Initialize() { }
    }

    public static class ForgeRenownGainUI
    {
        public static void Initialize() { }
    }

    public static class ForgeFoodConsumptionUI
    {
        public static void Initialize() { }

        public static float CalculateDaysRemaining(float totalFoodUnits, float dailyConsumption)
        {
            if (dailyConsumption <= 0) return 999f;
            return totalFoodUnits / dailyConsumption;
        }
    }
}
