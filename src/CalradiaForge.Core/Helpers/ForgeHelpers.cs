#if NET472
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.Core.Helpers
{
    internal static class ForgeHelperSupport
    {
        internal static NotSupportedException Unsupported(string memberName)
        {
            return new NotSupportedException(
                memberName + " is not implemented for the current Bannerlord integration; no action was performed.");
        }
    }

    // 1. ForgeEventBus
    public static class ForgeEventBus
    {
        private static readonly Dictionary<string, List<Action<object>>> _events = new Dictionary<string, List<Action<object>>>();
        public static void Subscribe(string eventName, Action<object> callback) { if (!_events.ContainsKey(eventName)) _events[eventName] = new List<Action<object>>(); _events[eventName].Add(callback); }
        public static void Publish(string eventName, object data = null) { if (_events.ContainsKey(eventName)) { foreach (var cb in _events[eventName]) cb?.Invoke(data); } }
    }

    // 2. ForgeDI
    public static class ForgeDI
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        public static void Register<T>(T service) { _services[typeof(T)] = service; }
        public static T Get<T>() { return _services.ContainsKey(typeof(T)) ? (T)_services[typeof(T)] : default; }
    }

    // 3. ForgeTask
    public static class ForgeTask
    {
        public static void RunNextTick(Action action) { throw ForgeHelperSupport.Unsupported(nameof(RunNextTick)); }
    }

    // 4. ForgeMessage
    public static class ForgeMessage
    {
        public static void Show(string message, uint color = 0xFFFFFF) => InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(color)));
    }

    // 5. ForgeCamera
    public static class ForgeCamera
    {
        public static Camera GetCurrent() { throw ForgeHelperSupport.Unsupported(nameof(GetCurrent)); }
    }

    // 6. ForgeHero
    public static class ForgeHero
    {
        public static void Heal(Hero hero) { hero.HitPoints = hero.CharacterObject.MaxHitPoints(); }
        public static void Kill(Hero hero) { KillCharacterAction.ApplyByMurder(hero); }
    }

    // 7. ForgeParty
    public static class ForgeParty
    {
        public static void Teleport(MobileParty party, Settlement target) { throw ForgeHelperSupport.Unsupported(nameof(Teleport)); }
    }

    // 8. ForgeItem
    public static class ForgeItem
    {
        public static void GiveItemToPlayer(ItemObject item, int amount) { MobileParty.MainParty.ItemRoster.AddToCounts(item, amount); }
    }

    // 9. ForgeFaction
    public static class ForgeFaction
    {
        public static void DeclareWar(IFaction f1, IFaction f2) { DeclareWarAction.ApplyByDefault(f1, f2); }
    }

    // 10. ForgeMap
    public static class ForgeMap
    {
        public static Settlement GetClosestSettlement(Vec2 position) => Settlement.All.OrderBy(s => s.GatePosition.DistanceSquared(position)).FirstOrDefault();
    }

    // 11. ForgeAgent
    public static class ForgeAgent
    {
        public static void Heal(Agent agent) { agent.Health = agent.HealthLimit; }
    }

    // 12. ForgeMenu & 13. ForgeDialog
    public static class ForgeUI
    {
        // Placeholders for fluent API extensions in CampaignBehaviorBase
    }

    // 14. ForgeSave
    public static class ForgeSave
    {
        public static void QuickSave<T>(T data, IDataStore dataStore, string key) { dataStore.SyncData(key, ref data); }
    }

    // 15. ForgeInput
    public static class ForgeInput
    {
        public static bool IsKeyDown(InputKey key) => Input.IsKeyDown(key);
    }

    // 16. ForgeTime
    public static class ForgeTime
    {
        public static void FastForward(float hours) { throw ForgeHelperSupport.Unsupported(nameof(FastForward)); }
    }

    // 17. ForgeGold
    public static class ForgeGold
    {
        public static void GiveGold(Hero hero, int amount) { GiveGoldAction.ApplyBetweenCharacters(null, hero, amount); }
    }

    // 18. ForgeInfluence
    public static class ForgeInfluence
    {
        public static void GiveInfluence(Clan clan, float amount) { ChangeClanInfluenceAction.Apply(clan, amount); }
    }

    // 19. ForgeRenown
    public static class ForgeRenown
    {
        public static void AddRenown(Clan clan, float amount) { GainRenownAction.Apply(clan.Leader, amount); }
    }

    // 20. ForgeClan
    public static class ForgeClan
    {
        public static Clan CreatePlayerSubClan(string name) { throw ForgeHelperSupport.Unsupported(nameof(CreatePlayerSubClan)); }
    }

    // 21. ForgeKingdom
    public static class ForgeKingdom
    {
        public static void FormKingdom(Clan clan) { Campaign.Current.KingdomManager.CreateKingdom(clan.Name, clan.Name, clan.Culture, clan); }
    }

    // 22. ForgeSettlement
    public static class ForgeSettlement
    {
        public static void ChangeOwner(Settlement settlement, Hero newOwner) { ChangeOwnerOfSettlementAction.ApplyByGift(settlement, newOwner); }
    }

    // 23. ForgeTroop
    public static class ForgeTroop
    {
        public static void AddTroop(MobileParty party, CharacterObject troop, int count) { party.MemberRoster.AddToCounts(troop, count); }
    }

    // 24. ForgePrisoner
    public static class ForgePrisoner
    {
        public static void TakePrisoner(PartyBase captor, CharacterObject prisoner) { captor.PrisonRoster.AddToCounts(prisoner, 1); }
    }

    // 25. ForgeSkill
    public static class ForgeSkill
    {
        public static void AddSkillXp(Hero hero, SkillObject skill, float xp) { hero.HeroDeveloper.AddSkillXp(skill, xp); }
    }
    // 26. ForgeQuest
    public static class ForgeQuest
    {
        public static void StartQuest(QuestBase quest) { quest.StartQuest(); }
    }

    // 27. ForgeConversation
    public static class ForgeConversation
    {
        public static void ForceDialog(Hero hero) { throw ForgeHelperSupport.Unsupported(nameof(ForceDialog)); }
    }

    // 28. ForgeWorkshop
    public static class ForgeWorkshop
    {
        public static void ChangeWorkshopType(object workshop, object newType) { throw ForgeHelperSupport.Unsupported(nameof(ChangeWorkshopType)); }
    }

    // 29. ForgeCaravan
    public static class ForgeCaravan
    {
        public static void SpawnCaravan(Hero owner, Settlement spawnSettlement) { throw ForgeHelperSupport.Unsupported(nameof(SpawnCaravan)); }
    }

    // 30. ForgeAlley
    public static class ForgeAlley
    {
        public static void ClearAlley(Alley alley) { alley.SetOwner(null); }
    }

    // 31. ForgeTournament
    public static class ForgeTournament
    {
        public static void StartTournament(Town town) { throw ForgeHelperSupport.Unsupported(nameof(StartTournament)); }
    }

    // 32. ForgeBattle
    public static class ForgeBattle
    {
        public static void StartEncounter(MobileParty attacker, MobileParty defender) { throw ForgeHelperSupport.Unsupported(nameof(StartEncounter)); }
    }

    // 33. ForgeCrafting
    public static class ForgeCrafting
    {
        public static void UnlockPart(CraftingPiece piece) { throw ForgeHelperSupport.Unsupported(nameof(UnlockPart)); }
    }

    // 34. ForgeSiege
    public static class ForgeSiege
    {
        public static void StartSiege(MobileParty besieger, Settlement settlement) { throw ForgeHelperSupport.Unsupported(nameof(StartSiege)); }
    }

    // 35. ForgeWeather
    public static class ForgeWeather
    {
        public static void ForceRain() { throw ForgeHelperSupport.Unsupported(nameof(ForceRain)); }
    }

    // 36. ForgeMount
    public static class ForgeMount
    {
        public static void EquipMount(Hero hero, ItemObject horse) { hero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.ArmorItemEndSlot, new EquipmentElement(horse)); }
    }

    // 37. ForgeSound
    public static class ForgeSound
    {
        public static void PlaySound(string soundId) { throw ForgeHelperSupport.Unsupported(nameof(PlaySound)); }
    }

    // 38. ForgeParticle
    public static class ForgeParticle
    {
        public static void SpawnParticle(string particleId, MatrixFrame frame) { throw ForgeHelperSupport.Unsupported(nameof(SpawnParticle)); }
    }

    // 39. ForgeMusic
    public static class ForgeMusic
    {
        public static void PlayMusic(string trackId) { throw ForgeHelperSupport.Unsupported(nameof(PlayMusic)); }
    }

    // 40. ForgeTooltip
    public static class ForgeTooltip
    {
        public static void ShowTooltip(string text) { throw ForgeHelperSupport.Unsupported(nameof(ShowTooltip)); }
    }

    // 41. ForgeCheat
    public static class ForgeCheat
    {
        public static void ToggleCheats(bool enable) { throw ForgeHelperSupport.Unsupported(nameof(ToggleCheats)); }
    }

    // 42. ForgeCulture
    public static class ForgeCulture
    {
        public static void SetCulture(Settlement settlement, CultureObject culture) { settlement.Culture = culture; }
    }

    // 43. ForgeReligion
    public static class ForgeReligion
    {
        public static void ConvertHero(Hero hero, string religionId) { throw ForgeHelperSupport.Unsupported(nameof(ConvertHero)); }
    }

    // 44. ForgeTrait
    public static class ForgeTrait
    {
        public static void SetTraitLevel(Hero hero, object trait, int level) { throw ForgeHelperSupport.Unsupported(nameof(SetTraitLevel)); }
    }

    // 45. ForgeMarriage
    public static class ForgeMarriage
    {
        public static void Marry(Hero hero1, Hero hero2) { MarriageAction.Apply(hero1, hero2); }
    }
    // 86. ForgeBanner
    public static class ForgeBanner { public static void RandomizeBanner(Clan clan) { throw ForgeHelperSupport.Unsupported(nameof(RandomizeBanner)); } }
    // 87. ForgeTavern
    public static class ForgeTavern { public static void AddMercenary(Settlement town) { throw ForgeHelperSupport.Unsupported(nameof(AddMercenary)); } }
    // 88. ForgeCompanion
    public static class ForgeCompanion { public static void SpawnWanderer(Settlement town) { throw ForgeHelperSupport.Unsupported(nameof(SpawnWanderer)); } }
    // 89. ForgeHideout
    public static class ForgeHideout { public static void SpawnHideout() { throw ForgeHelperSupport.Unsupported(nameof(SpawnHideout)); } }
    // 90. ForgeBandit
    public static class ForgeBandit { public static void SpawnBanditParty() { throw ForgeHelperSupport.Unsupported(nameof(SpawnBanditParty)); } }
    // 91. ForgeMercenary
    public static class ForgeMercenary { public static void HireMercenary(Clan merc, Kingdom kingdom) { throw ForgeHelperSupport.Unsupported(nameof(HireMercenary)); } }
    // 92. ForgeTrade
    public static class ForgeTrade { public static void ModifyPrice(ItemObject item, Town town, float factor) { throw ForgeHelperSupport.Unsupported(nameof(ModifyPrice)); } }
    // 93. ForgeWorkshopProduction
    public static class ForgeWorkshopProduction { public static void SetSpeed(object workshop, float speed) { throw ForgeHelperSupport.Unsupported(nameof(SetSpeed)); } }
    // 94. ForgeSiegeEngine
    public static class ForgeSiegeEngine { public static void SpawnEngine() { throw ForgeHelperSupport.Unsupported(nameof(SpawnEngine)); } }
    // 95. ForgeVillage
    public static class ForgeVillage { public static void AddHearths(Village village, float hearths) { village.Hearth += hearths; } }
    // 96. ForgeLoyalty
    public static class ForgeLoyalty { public static void AddLoyalty(Town town, float loyalty) { town.Loyalty += loyalty; } }
    // 97. ForgeSecurity
    public static class ForgeSecurity { public static void AddSecurity(Town town, float security) { town.Security += security; } }
    // 98. ForgeRebellion
    public static class ForgeRebellion { public static void StartRebellion(Settlement town) { throw ForgeHelperSupport.Unsupported(nameof(StartRebellion)); } }
    // 99. ForgeFormation
    public static class ForgeFormation { public static void OrderCharge() { throw ForgeHelperSupport.Unsupported(nameof(OrderCharge)); } }
    // 100. ForgeOrder
    public static class ForgeOrder { public static void OrderRetreat() { throw ForgeHelperSupport.Unsupported(nameof(OrderRetreat)); } }
    // 101. ForgeWoundRate
    public static class ForgeWoundRate { public static void SetSurvivalChance(float chance) { throw ForgeHelperSupport.Unsupported(nameof(SetSurvivalChance)); } }
    // 102. ForgeExecution
    public static class ForgeExecution { public static void ExecuteHero(Hero hero) { KillCharacterAction.ApplyByExecution(hero, Hero.MainHero); } }
    // 103. ForgeHeir
    public static class ForgeHeir { public static void SetHeir(Hero heir) { throw ForgeHelperSupport.Unsupported(nameof(SetHeir)); } }
    // 104. ForgeNotable
    public static class ForgeNotable { public static void SpawnNotable(Settlement town) { throw ForgeHelperSupport.Unsupported(nameof(SpawnNotable)); } }
    // 105. ForgeCaravanGuard
    public static class ForgeCaravanGuard { public static void AddGuards(MobileParty caravan, int amount) { throw ForgeHelperSupport.Unsupported(nameof(AddGuards)); } }
}
#endif

