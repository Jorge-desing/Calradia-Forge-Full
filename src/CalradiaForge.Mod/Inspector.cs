using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using CalradiaForge.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CalradiaForge.Mod
{
    internal static class Inspector
    {
        private static readonly string[] AllowedProperties = new[]
        {
            "Age","Gold","Level","IsAlive","IsPrisoner","HitPoints","Tier","IsActive",
            "Health","HealthLimit","Value","Weight","IsHero","IsMainParty","TotalManCount",
            "Prosperity","Loyalty","Security","Militia","FoodStocks","TotalStrength"
        };

        private static readonly ConcurrentDictionary<(Type, string), PropertyInfo> PropertyCache =
            new ConcurrentDictionary<(Type, string), PropertyInfo>();

        public static List<ObjectSnapshot> Deserialize(string query)
        {
            var parts = (query ?? "Hero|").Split('|');
            var type = parts[0];
            var filter = parts.Length > 1 ? parts[1] : "";
            IEnumerable<object> objects;
            switch (type)
            {
                case "Hero": if (Campaign.Current != null) objects = Hero.AllAliveHeroes; else objects = Array.Empty<object>(); break;
                case "Clan": if (Campaign.Current != null) objects = Clan.All; else objects = Array.Empty<object>(); break;
                case "Kingdom": if (Campaign.Current != null) objects = Kingdom.All; else objects = Array.Empty<object>(); break;
                case "Settlement": if (Campaign.Current != null) objects = TaleWorlds.CampaignSystem.Settlements.Settlement.All; else objects = Array.Empty<object>(); break;
                case "Party": if (Campaign.Current != null) objects = MobileParty.All; else objects = Array.Empty<object>(); break;
                case "Item": if (MBObjectManager.Instance != null) objects = MBObjectManager.Instance.GetObjectTypeList<ItemObject>(); else objects = Array.Empty<object>(); break;
                case "Troop":
                    if (MBObjectManager.Instance != null)
                        objects = Campaign.Current != null ? (IEnumerable<object>)MBObjectManager.Instance.GetObjectTypeList<CharacterObject>() : MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>();
                    else objects = Array.Empty<object>();
                    break;
                case "Agent": if (Mission.Current != null) objects = Mission.Current.Agents; else objects = Array.Empty<object>(); break;
                default: throw new ArgumentException("Hero, Clan, Kingdom, Settlement, Party, Item, Troop, Agent");
            }

            var result = new List<ObjectSnapshot>(Math.Min(200, 32));
            bool hasFilter = !string.IsNullOrEmpty(filter);

            foreach (var o in objects)
            {
                if (o == null) continue;
                if (!hasFilter || MatchesFilter(o, filter))
                {
                    result.Add(Capture(type, o));
                    if (result.Count >= 200) break;
                }
            }
            return result;
        }

        private static bool MatchesFilter(object o, string filter)
        {
            var id = Get(o, "StringId");
            if (id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            var name = Get(o, "Name");
            if (name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            var idx = Get(o, "Index");
            if (idx.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            return false;
        }

        static string Get(object o, string p)
        {
            if (o == null) return "";
            try
            {
                var type = o.GetType();
                var prop = PropertyCache.GetOrAdd((type, p), key => key.Item1.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance));
                return prop?.GetValue(o)?.ToString() ?? "";
            }
            catch { return "<unavailable>"; }
        }

        static ObjectSnapshot Capture(string type, object o)
        {
            var r = new ObjectSnapshot
            {
                Type = type,
                Id = Get(o, type == "Agent" ? "Index" : "StringId"),
                Name = Get(o, "Name"),
                Properties = new Dictionary<string, string>(AllowedProperties.Length, StringComparer.Ordinal)
            };
            for (int i = 0; i < AllowedProperties.Length; i++)
            {
                var p = AllowedProperties[i];
                var v = Get(o, p);
                if (v != "") r.Properties[p] = v;
            }
            return r;
        }
    }
}
