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
        static readonly PropertyInfo CampaignCurrentProp = CampaignType?.GetProperty("Current", StaticFlags);
        static readonly PropertyInfo CampaignModelsProp = CampaignType?.GetProperty("Models", InstanceFlags);
        static readonly PropertyInfo CampaignBehaviorManagerProp = CampaignType?.GetProperty("CampaignBehaviorManager", InstanceFlags);

        static readonly Type HeroType = Type.GetType("TaleWorlds.CampaignSystem.Hero, TaleWorlds.CampaignSystem");
        static readonly PropertyInfo MainHeroProp = HeroType?.GetProperty("MainHero", StaticFlags);
        static readonly PropertyInfo HeroNameProp = HeroType?.GetProperty("Name", InstanceFlags);
        static readonly PropertyInfo HeroGoldProp = HeroType?.GetProperty("Gold", InstanceFlags);
        static readonly PropertyInfo HeroStringIdProp = HeroType?.GetProperty("StringId", InstanceFlags);
        static readonly PropertyInfo HeroPartyBelongedToProp = HeroType?.GetProperty("PartyBelongedTo", InstanceFlags);

        static readonly Type SettlementType = Type.GetType("TaleWorlds.CampaignSystem.Settlements.Settlement, TaleWorlds.CampaignSystem")
            ?? Type.GetType("TaleWorlds.CampaignSystem.Settlement.Settlement, TaleWorlds.CampaignSystem")
            ?? Type.GetType("TaleWorlds.CampaignSystem.Settlement, TaleWorlds.CampaignSystem");
        static readonly PropertyInfo CurrentSettlementProp = SettlementType?.GetProperty("CurrentSettlement", StaticFlags);
        static readonly PropertyInfo SettlementNameProp = SettlementType?.GetProperty("Name", InstanceFlags);
        static readonly PropertyInfo SettlementStringIdProp = SettlementType?.GetProperty("StringId", InstanceFlags);
        static readonly PropertyInfo SettlementAllProp = SettlementType?.GetProperty("All", StaticFlags);
        static readonly PropertyInfo SettlementIsTownProp = SettlementType?.GetProperty("IsTown", InstanceFlags);
        static readonly PropertyInfo SettlementTownProp = SettlementType?.GetProperty("Town", InstanceFlags);

        static readonly Type ClanType = Type.GetType("TaleWorlds.CampaignSystem.Clan, TaleWorlds.CampaignSystem");
        static readonly PropertyInfo ClanAllProp = ClanType?.GetProperty("All", StaticFlags);
        static readonly PropertyInfo ClanIsEliminatedProp = ClanType?.GetProperty("IsEliminated", InstanceFlags);
        static readonly PropertyInfo ClanIsNobleProp = ClanType?.GetProperty("IsNoble", InstanceFlags);
        static readonly PropertyInfo ClanNameProp = ClanType?.GetProperty("Name", InstanceFlags);
        static readonly PropertyInfo ClanLeaderProp = ClanType?.GetProperty("Leader", InstanceFlags);
        static readonly PropertyInfo ClanTierProp = ClanType?.GetProperty("Tier", InstanceFlags);

        static readonly Type KingdomType = Type.GetType("TaleWorlds.CampaignSystem.Kingdom, TaleWorlds.CampaignSystem");
        static readonly PropertyInfo KingdomAllProp = KingdomType?.GetProperty("All", StaticFlags);
        static readonly PropertyInfo KingdomNameProp = KingdomType?.GetProperty("Name", InstanceFlags);
        static readonly PropertyInfo KingdomTotalStrengthProp = KingdomType?.GetProperty("TotalStrength", InstanceFlags);
        static readonly PropertyInfo KingdomGoldProp = KingdomType?.GetProperty("Gold", InstanceFlags);

        static readonly Type MissionType = Type.GetType("TaleWorlds.MountAndBlade.Mission, TaleWorlds.MountAndBlade");
        static readonly PropertyInfo MissionCurrentProp = MissionType?.GetProperty("Current", StaticFlags);
        static readonly PropertyInfo MissionModeProp = MissionType?.GetProperty("Mode", InstanceFlags);
        static readonly PropertyInfo MissionSceneNameProp = MissionType?.GetProperty("SceneName", InstanceFlags);

        static readonly Type GameType = Type.GetType("TaleWorlds.Core.Game, TaleWorlds.Core");
        static readonly PropertyInfo GameCurrentProp = GameType?.GetProperty("Current", StaticFlags);
        static readonly PropertyInfo GameBasicModelsProp = GameType?.GetProperty("BasicModels", InstanceFlags);

        static readonly Type BehaviorBaseType = Type.GetType("TaleWorlds.CampaignSystem.CampaignBehaviorBase, TaleWorlds.CampaignSystem");

        public static object GetCurrentCampaign()
        {
            try { return CampaignCurrentProp?.GetValue(null, null); }
            catch { return null; }
        }

        public static object GetMainHero()
        {
            try { return MainHeroProp?.GetValue(null, null); }
            catch { return null; }
        }

        public static bool TryGetMainHeroDetails(out string name, out string stringId, out object gold)
        {
            name = null;
            stringId = null;
            gold = null;
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
            try
            {
                var hero = GetMainHero();
                if (hero == null) return fallback;
                var id = HeroStringIdProp?.GetValue(hero, null) as string;
                return !string.IsNullOrEmpty(id) ? id : fallback;
            }
            catch { return fallback; }
        }

        public static object GetCurrentSettlement()
        {
            try { return CurrentSettlementProp?.GetValue(null, null); }
            catch { return null; }
        }

        public static bool TryGetCurrentSettlementDetails(out string name, out string stringId)
        {
            name = null;
            stringId = null;
            try
            {
                var settlement = GetCurrentSettlement();
                if (settlement == null) return false;
                name = SettlementNameProp?.GetValue(settlement, null)?.ToString() ?? "Unknown Settlement";
                stringId = SettlementStringIdProp?.GetValue(settlement, null)?.ToString() ?? "Town_1";
                return true;
            }
            catch { return false; }
        }

        public static string GetCurrentSettlementId(string fallback = "Town_1")
        {
            try
            {
                var settlement = GetCurrentSettlement();
                if (settlement == null) return fallback;
                var id = SettlementStringIdProp?.GetValue(settlement, null) as string;
                return !string.IsNullOrEmpty(id) ? id : fallback;
            }
            catch { return fallback; }
        }

        public static object GetCurrentMission()
        {
            try { return MissionCurrentProp?.GetValue(null, null); }
            catch { return null; }
        }

        public static bool TryGetCurrentMissionDetails(out string sceneName, out object mode)
        {
            sceneName = null;
            mode = null;
            try
            {
                var mission = GetCurrentMission();
                if (mission == null) return false;
                sceneName = MissionSceneNameProp?.GetValue(mission, null)?.ToString() ?? "Unknown Scene";
                mode = MissionModeProp?.GetValue(mission, null);
                return true;
            }
            catch { return false; }
        }

        public static IEnumerable GetAllClans()
        {
            try { return ClanAllProp?.GetValue(null, null) as IEnumerable; }
            catch { return null; }
        }

        public static bool TryGetClanDetails(object clan, out string name, out string tier, out string leaderName, out bool isEliminated, out bool isNoble)
        {
            name = "Noble Clan";
            tier = "0";
            leaderName = "None";
            isEliminated = false;
            isNoble = false;
            if (clan == null) return false;
            try
            {
                isEliminated = (bool)(ClanIsEliminatedProp?.GetValue(clan, null) ?? false);
                isNoble = (bool)(ClanIsNobleProp?.GetValue(clan, null) ?? false);
                name = ClanNameProp?.GetValue(clan, null)?.ToString() ?? "Noble Clan";
                tier = ClanTierProp?.GetValue(clan, null)?.ToString() ?? "0";
                var leader = ClanLeaderProp?.GetValue(clan, null);
                if (leader != null)
                {
                    var leaderNameProp = leader.GetType().GetProperty("Name", InstanceFlags);
                    leaderName = leaderNameProp?.GetValue(leader, null)?.ToString() ?? "None";
                }
                return true;
            }
            catch { return false; }
        }

        public static IEnumerable GetAllSettlements()
        {
            try { return SettlementAllProp?.GetValue(null, null) as IEnumerable; }
            catch { return null; }
        }

        public static bool TryGetSettlementTownDetails(object settlement, out string name, out int prosperity, out float security, out bool isTown)
        {
            name = "Settlement";
            prosperity = 4000;
            security = 50f;
            isTown = false;
            if (settlement == null) return false;
            try
            {
                isTown = (bool)(SettlementIsTownProp?.GetValue(settlement, null) ?? false);
                name = SettlementNameProp?.GetValue(settlement, null)?.ToString() ?? "Settlement";
                var town = SettlementTownProp?.GetValue(settlement, null);
                if (town != null)
                {
                    var townType = town.GetType();
                    var pProp = townType.GetProperty("Prosperity", InstanceFlags);
                    var sProp = townType.GetProperty("Security", InstanceFlags);
                    var pVal = pProp?.GetValue(town, null);
                    if (pVal is int p) prosperity = p;
                    else if (pVal is float pf) prosperity = (int)pf;
                    else if (pVal != null && int.TryParse(pVal.ToString(), out var parsedP)) prosperity = parsedP;

                    var sVal = sProp?.GetValue(town, null);
                    if (sVal is float s) security = s;
                    else if (sVal is int si) security = si;
                    else if (sVal != null && float.TryParse(sVal.ToString(), out var parsedS)) security = parsedS;
                }
                return true;
            }
            catch { return false; }
        }

        public static IEnumerable GetAllKingdoms()
        {
            try { return KingdomAllProp?.GetValue(null, null) as IEnumerable; }
            catch { return null; }
        }

        public static bool TryGetKingdomDetails(object kingdom, out string name, out double strength, out int gold)
        {
            name = "Unknown Kingdom";
            strength = 0;
            gold = 0;
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
                if (modelsObj == null) return null;
                var getModelsMethod = modelsObj.GetType().GetMethod("GetGameModels", InstanceFlags);
                return getModelsMethod?.Invoke(modelsObj, null) as IEnumerable;
            }
            catch { return null; }
        }

        public static IEnumerable GetBasicGameModels()
        {
            try
            {
                var game = GameCurrentProp?.GetValue(null, null);
                if (game == null) return null;
                var basicModelsObj = GameBasicModelsProp?.GetValue(game, null);
                if (basicModelsObj == null) return null;
                var getModelsMethod = basicModelsObj.GetType().GetMethod("GetGameModels", InstanceFlags);
                return getModelsMethod?.Invoke(basicModelsObj, null) as IEnumerable;
            }
            catch { return null; }
        }

        public static IEnumerable GetCampaignBehaviors(object campaign)
        {
            if (campaign == null) return null;
            try
            {
                var cbm = CampaignBehaviorManagerProp?.GetValue(campaign, null);
                if (cbm == null) return null;
                var getBehaviorsMethod = cbm.GetType().GetMethod("GetBehaviors", InstanceFlags);
                if (getBehaviorsMethod != null && getBehaviorsMethod.IsGenericMethod && BehaviorBaseType != null)
                {
                    var genericMethod = getBehaviorsMethod.MakeGenericMethod(BehaviorBaseType);
                    return genericMethod.Invoke(cbm, null) as IEnumerable;
                }
                return null;
            }
            catch { return null; }
        }
    }
}
