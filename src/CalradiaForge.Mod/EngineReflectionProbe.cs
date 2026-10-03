using System;
using System.Collections;
using System.Reflection;

namespace CalradiaForge.Mod
{
    /// <summary>
    /// Thread-safe, cached reflection probe for TaleWorlds engine types.
    /// Centralizes dynamic reflection calls without shadowing engine namespaces (GEMINI.md Rule A).
    /// </summary>
    internal static class EngineReflectionProbe
    {
        const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.Static;
        const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.Instance;

        static readonly Type CampaignType = Type.GetType("TaleWorlds.CampaignSystem.Campaign, TaleWorlds.CampaignSystem");
        static readonly PropertyInfo CampaignCurrentProp = CampaignType?.GetProperty("Current", StaticFlags),
            CampaignModelsProp = CampaignType?.GetProperty("Models", InstanceFlags),
            CampaignBehaviorManagerProp = CampaignType?.GetProperty("CampaignBehaviorManager", InstanceFlags);

        static readonly Type HeroType = Type.GetType("TaleWorlds.CampaignSystem.Hero, TaleWorlds.CampaignSystem");
        static readonly PropertyInfo MainHeroProp = HeroType?.GetProperty("MainHero", StaticFlags),
            HeroNameProp = HeroType?.GetProperty("Name", InstanceFlags),
            HeroGoldProp = HeroType?.GetProperty("Gold", InstanceFlags),
            HeroStringIdProp = HeroType?.GetProperty("StringId", InstanceFlags);

        static readonly Type SettlementType = Type.GetType("TaleWorlds.CampaignSystem.Settlements.Settlement, TaleWorlds.CampaignSystem")
            ?? Type.GetType("TaleWorlds.CampaignSystem.Settlement.Settlement, TaleWorlds.CampaignSystem")
            ?? Type.GetType("TaleWorlds.CampaignSystem.Settlement, TaleWorlds.CampaignSystem");
        static readonly PropertyInfo CurrentSettlementProp = SettlementType?.GetProperty("CurrentSettlement", StaticFlags),
            SettlementNameProp = SettlementType?.GetProperty("Name", InstanceFlags),
            SettlementStringIdProp = SettlementType?.GetProperty("StringId", InstanceFlags),
            SettlementAllProp = SettlementType?.GetProperty("All", StaticFlags),
            SettlementIsTownProp = SettlementType?.GetProperty("IsTown", InstanceFlags),
            SettlementTownProp = SettlementType?.GetProperty("Town", InstanceFlags);

        static readonly Type ClanType = Type.GetType("TaleWorlds.CampaignSystem.Clan, TaleWorlds.CampaignSystem");
        static readonly PropertyInfo ClanAllProp = ClanType?.GetProperty("All", StaticFlags),
            ClanIsEliminatedProp = ClanType?.GetProperty("IsEliminated", InstanceFlags),
            ClanIsNobleProp = ClanType?.GetProperty("IsNoble", InstanceFlags),
            ClanNameProp = ClanType?.GetProperty("Name", InstanceFlags),
            ClanLeaderProp = ClanType?.GetProperty("Leader", InstanceFlags),
            ClanTierProp = ClanType?.GetProperty("Tier", InstanceFlags);

        static readonly Type KingdomType = Type.GetType("TaleWorlds.CampaignSystem.Kingdom, TaleWorlds.CampaignSystem");
        static readonly PropertyInfo KingdomAllProp = KingdomType?.GetProperty("All", StaticFlags),
            KingdomNameProp = KingdomType?.GetProperty("Name", InstanceFlags),
            KingdomTotalStrengthProp = KingdomType?.GetProperty("TotalStrength", InstanceFlags),
            KingdomGoldProp = KingdomType?.GetProperty("Gold", InstanceFlags);

        static readonly Type MissionType = Type.GetType("TaleWorlds.MountAndBlade.Mission, TaleWorlds.MountAndBlade");
        static readonly PropertyInfo MissionCurrentProp = MissionType?.GetProperty("Current", StaticFlags),
            MissionModeProp = MissionType?.GetProperty("Mode", InstanceFlags),
            MissionSceneNameProp = MissionType?.GetProperty("SceneName", InstanceFlags);

        static readonly Type GameType = Type.GetType("TaleWorlds.Core.Game, TaleWorlds.Core");
        static readonly PropertyInfo GameCurrentProp = GameType?.GetProperty("Current", StaticFlags),
            GameBasicModelsProp = GameType?.GetProperty("BasicModels", InstanceFlags);

        static readonly Type BehaviorBaseType = Type.GetType("TaleWorlds.CampaignSystem.CampaignBehaviorBase, TaleWorlds.CampaignSystem");

        static object SafeGet(PropertyInfo prop, object target = null)
        {
            try { return prop?.GetValue(target, null); } catch { return null; }
        }

        public static object GetCurrentCampaign() => SafeGet(CampaignCurrentProp);
        public static object GetMainHero() => SafeGet(MainHeroProp);
        public static object GetCurrentSettlement() => SafeGet(CurrentSettlementProp);
        public static object GetCurrentMission() => SafeGet(MissionCurrentProp);
        public static IEnumerable GetAllClans() => SafeGet(ClanAllProp) as IEnumerable;
        public static IEnumerable GetAllSettlements() => SafeGet(SettlementAllProp) as IEnumerable;
        public static IEnumerable GetAllKingdoms() => SafeGet(KingdomAllProp) as IEnumerable;

        public static bool TryGetMainHeroDetails(out string name, out string stringId, out object gold)
        {
            name = null; stringId = null; gold = null;
            try
            {
                var hero = GetMainHero();
                if (hero == null) return false;
                name = HeroNameProp?.GetValue(hero, null)?.ToString() ?? "Unknown Hero";
                stringId = HeroStringIdProp?.GetValue(hero, null)?.ToString() ?? "Hero_1";
                gold = HeroGoldProp?.GetValue(hero, null) ?? 0;
                return true;
            }
            catch { return false; }
        }

        public static string GetPlayerHeroId(string fallback = "Hero_1")
        {
            try { var id = HeroStringIdProp?.GetValue(GetMainHero(), null) as string; return !string.IsNullOrEmpty(id) ? id : fallback; }
            catch { return fallback; }
        }

        public static bool TryGetCurrentSettlementDetails(out string name, out string stringId)
        {
            name = null; stringId = null;
            try
            {
                var s = GetCurrentSettlement();
                if (s == null) return false;
                name = SettlementNameProp?.GetValue(s, null)?.ToString() ?? "Unknown Settlement";
                stringId = SettlementStringIdProp?.GetValue(s, null)?.ToString() ?? "Town_1";
                return true;
            }
            catch { return false; }
        }

        public static string GetCurrentSettlementId(string fallback = "Town_1")
        {
            try { var id = SettlementStringIdProp?.GetValue(GetCurrentSettlement(), null) as string; return !string.IsNullOrEmpty(id) ? id : fallback; }
            catch { return fallback; }
        }

        public static bool TryGetCurrentMissionDetails(out string sceneName, out object mode)
        {
            sceneName = null; mode = null;
            try
            {
                var m = GetCurrentMission();
                if (m == null) return false;
                sceneName = MissionSceneNameProp?.GetValue(m, null)?.ToString() ?? "Unknown Scene";
                mode = MissionModeProp?.GetValue(m, null);
                return true;
            }
            catch { return false; }
        }

        public static bool TryGetClanDetails(object clan, out string name, out string tier, out string leaderName, out bool isEliminated, out bool isNoble)
        {
            name = "Noble Clan"; tier = "0"; leaderName = "None"; isEliminated = false; isNoble = false;
            if (clan == null) return false;
            try
            {
                isEliminated = (bool)(ClanIsEliminatedProp?.GetValue(clan, null) ?? false);
                isNoble = (bool)(ClanIsNobleProp?.GetValue(clan, null) ?? false);
                name = ClanNameProp?.GetValue(clan, null)?.ToString() ?? "Noble Clan";
                tier = ClanTierProp?.GetValue(clan, null)?.ToString() ?? "0";
                var leader = ClanLeaderProp?.GetValue(clan, null);
                if (leader != null)
                    leaderName = leader.GetType().GetProperty("Name", InstanceFlags)?.GetValue(leader, null)?.ToString() ?? "None";
                return true;
            }
            catch { return false; }
        }

        public static bool TryGetSettlementTownDetails(object settlement, out string name, out int prosperity, out float security, out bool isTown)
        {
            name = "Settlement"; prosperity = 4000; security = 50f; isTown = false;
            if (settlement == null) return false;
            try
            {
                isTown = (bool)(SettlementIsTownProp?.GetValue(settlement, null) ?? false);
                name = SettlementNameProp?.GetValue(settlement, null)?.ToString() ?? "Settlement";
                var town = SettlementTownProp?.GetValue(settlement, null);
                if (town != null)
                {
                    var tt = town.GetType();
                    var pVal = tt.GetProperty("Prosperity", InstanceFlags)?.GetValue(town, null);
                    if (pVal is int p) prosperity = p;
                    else if (pVal is float pf) prosperity = (int)pf;
                    else if (pVal != null && int.TryParse(pVal.ToString(), out var parsedP)) prosperity = parsedP;

                    var sVal = tt.GetProperty("Security", InstanceFlags)?.GetValue(town, null);
                    if (sVal is float s) security = s;
                    else if (sVal is int si) security = si;
                    else if (sVal != null && float.TryParse(sVal.ToString(), out var parsedS)) security = parsedS;
                }
                return true;
            }
            catch { return false; }
        }

        public static bool TryGetKingdomDetails(object kingdom, out string name, out double strength, out int gold)
        {
            name = "Unknown Kingdom"; strength = 0; gold = 0;
            if (kingdom == null) return false;
            try
            {
                name = KingdomNameProp?.GetValue(kingdom, null)?.ToString() ?? "Unknown";
                var sObj = KingdomTotalStrengthProp?.GetValue(kingdom, null);
                if (sObj != null) double.TryParse(sObj.ToString(), out strength);
                var gObj = KingdomGoldProp?.GetValue(kingdom, null);
                if (gObj != null) int.TryParse(gObj.ToString(), out gold);
                return true;
            }
            catch { return false; }
        }

        public static IEnumerable GetCampaignGameModels(object campaign)
        {
            if (campaign == null) return null;
            try
            {
                var modelsObj = CampaignModelsProp?.GetValue(campaign, null);
                return modelsObj?.GetType().GetMethod("GetGameModels", InstanceFlags)?.Invoke(modelsObj, null) as IEnumerable;
            }
            catch { return null; }
        }

        public static IEnumerable GetBasicGameModels()
        {
            try
            {
                var game = GameCurrentProp?.GetValue(null, null);
                var basicModelsObj = GameBasicModelsProp?.GetValue(game, null);
                return basicModelsObj?.GetType().GetMethod("GetGameModels", InstanceFlags)?.Invoke(basicModelsObj, null) as IEnumerable;
            }
            catch { return null; }
        }

        public static IEnumerable GetCampaignBehaviors(object campaign)
        {
            if (campaign == null) return null;
            try
            {
                var cbm = CampaignBehaviorManagerProp?.GetValue(campaign, null);
                var m = cbm?.GetType().GetMethod("GetBehaviors", InstanceFlags);
                return (m != null && m.IsGenericMethod && BehaviorBaseType != null)
                    ? m.MakeGenericMethod(BehaviorBaseType).Invoke(cbm, null) as IEnumerable
                    : null;
            }
            catch { return null; }
        }
    }
}
