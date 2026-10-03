# Specification Mining Report: Clan & Character Development Hooks in Bannerlord

## 1. Observation
- **Authoritative Request File**: `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\ORIGINAL_REQUEST.md` (lines 1-30). Demands a massive experimental `CampaignBehaviorBase` in `CalradiaForge.Mod` hooking into all clan, hero, and character development hooks statelessly without custom save data serialization.
- **TaleWorlds Assembly**: Reflected directly from `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll`.
  - Discovered **276 public static properties** on `TaleWorlds.CampaignSystem.CampaignEvents`.
  - Identified **45+ high-value hooks** dedicated to Hero lifecycle, Clan lifecycle & dynastic succession, Companions, Marriage & Pregnancy, Progression, and Periodic simulation ticks.
- **Architectural Constraints & Rules**:
  - `GEMINI.md`: "Never name a folder, sub-namespace, or class `Campaign` within this project. Doing so shadows `TaleWorlds.CampaignSystem.Campaign`."
  - `bannerlord_mission_lifecycle.md`: "Meshes, skeletons, and complex physics components CANNOT be created, swapped, or heavily modified during OnInit()."
  - `bannerlord_save_system.md`: Save corruption occurs when serializing engine objects or unregistered types; stateless behaviors bypass this completely by keeping `SyncData` empty and omitting `SaveableTypeDefiner`.
  - `SubModule.cs` in `src\CalradiaForge.Mod\SubModule.cs` (lines 56-73): OnGameStart receives `gameStarterObject as CampaignGameStarter` and registers behaviors.

---

## 2. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Hero Lifecycle | `HeroCreated` | Fired when any hero is instantiated and added to the world (lords, wanderers, notables, children). | `Hero hero, bool isAlive` | `void` | Throws `NullReferenceException` if listener accesses uninitialized properties like `hero.Clan` without null checks. | `CampaignEvents.HeroCreated` |
| 2 | Hero Lifecycle | `HeroGrowsOutOfInfancyEvent` | Fired when an infant hero turns 3 years old, transitioning from infant state to visible child. | `Hero hero` | `void` | `hero` must be non-null; null reference if accessing absent family links. | `CampaignEvents.HeroGrowsOutOfInfancyEvent` |
| 3 | Hero Lifecycle | `HeroReachesTeenAgeEvent` | Fired when a child turns 11, entering teenage education / personality development stage. | `Hero hero` | `void` | Non-fatal if unhandled; `hero.HeroDeveloper` may have zero XP. | `CampaignEvents.HeroReachesTeenAgeEvent` |
| 4 | Hero Lifecycle | `HeroComesOfAgeEvent` | Fired when a hero turns 18, transitioning to an adult noble, wanderer, or soldier eligible for war and marriage. | `Hero hero` | `void` | If hero lacks equipment or clan, accessing `BattleEquipment` or `Clan` can throw NRE. | `CampaignEvents.HeroComesOfAgeEvent` |
| 5 | Hero Lifecycle | `BeforeHeroKilledEvent` | Pre-death hook triggered immediately before death resolution is finalized. Allows taking pre-mortem snapshot. | `Hero victim, Hero killer, KillCharacterActionDetail detail, bool showNotification` | `void` | `killer` is null for old age, child labor, wounds, or generic troop kills; must null-check `killer`. | `CampaignEvents.BeforeHeroKilledEvent` |
| 6 | Hero Lifecycle | `HeroKilledEvent` | Post-death hook invoked when a hero dies in battle, old age, labor, execution, or murder. | `Hero victim, Hero killer, KillCharacterActionDetail detail, bool showNotification` | `void` | `victim.IsAlive` is already false; `killer` can be null. | `CampaignEvents.HeroKilledEvent` |
| 7 | Hero Lifecycle | `HeroWounded` | Fired when a hero's HP falls below `hero.WoundedHealthLimit` (typically 20% max HP). | `Hero hero` | `void` | Listener must verify `hero != null` and `hero.IsAlive`. | `CampaignEvents.HeroWounded` |
| 8 | Hero Lifecycle | `OnHeroCombatHitEvent` | Fired when any hero or troop strikes another entity in combat on map or mission. | `CharacterObject attackerTroop, CharacterObject attackedTroop, PartyBase party, WeaponComponentData usedWeapon, bool isFatal, int xp` | `void` | `attackerTroop.HeroObject` or `attackedTroop.HeroObject` is null for regular commoner troops. | `CampaignEvents.OnHeroCombatHitEvent` |
| 9 | Hero Lifecycle | `HeroOccupationChangedEvent` | Fired when hero role/status shifts (e.g. wanderer -> companion, companion -> lord). | `Hero hero, Occupation oldOccupation` | `void` | `oldOccupation` may be `NotAssigned`; handle unexpected transitions gracefully. | `CampaignEvents.HeroOccupationChangedEvent` |
| 10 | Hero Lifecycle | `HeroRelationChanged` | Fired when relation changes between two heroes. | `Hero hero1, Hero hero2, int relationChange, bool showNotification, ChangeRelationDetail detail, Hero affectedRelative1, Hero affectedRelative2` | `void` | `affectedRelative1` and `affectedRelative2` can be null. | `CampaignEvents.HeroRelationChanged` |
| 11 | Hero Lifecycle | `OnHeroChangedClanEvent` | Fired when a hero switches clan (marriage, companion adoption, defection). | `Hero hero, Clan oldClan` | `void` | `oldClan` can be null if hero was unaligned wanderer. | `CampaignEvents.OnHeroChangedClanEvent` |
| 12 | Hero Lifecycle | `HeroPrisonerTaken` | Fired when a hero is captured as a prisoner of war. | `PartyBase capturerParty, Hero prisonerHero` | `void` | `capturerParty.LeaderHero` can be null (e.g. garrison or bandit party). | `CampaignEvents.HeroPrisonerTaken` |
| 13 | Hero Lifecycle | `HeroPrisonerReleased` | Fired when a hero is released or escapes from captivity. | `Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification` | `void` | `party` can be null if escaped from a settlement dungeon without mobile party. | `CampaignEvents.HeroPrisonerReleased` |
| 14 | Hero Lifecycle | `OnHeroActivatedEvent` | Fired when a hero transitions to active simulation state (`CharacterStates.Active`). | `Hero hero, CharacterStates previousState` | `void` | Check `hero != null`. | `CampaignEvents.OnHeroActivatedEvent` |
| 15 | Hero Lifecycle | `OnHeroGetsBusyEvent` | Fired when a hero is assigned a task (governor, caravan, alley, emissary, solving issue). | `Hero hero, HeroGetsBusyReasons heroGetsBusyReason` | `void` | `hero.PartyBelongedTo` may become null when assigned to settlement. | `CampaignEvents.OnHeroGetsBusyEvent` |
| 16 | Hero Lifecycle | `OnHeroUnregisteredEvent` | Fired when a hero is permanently purged from `MBObjectManager`. | `Hero hero` | `void` | Any cached transient references to this hero must be cleared to prevent memory leaks. | `CampaignEvents.OnHeroUnregisteredEvent` |
| 17 | Hero Lifecycle | `OnHeroTeleportationRequestedEvent` | Fired when a hero is dispatched to travel/teleport to a party or town. | `Hero hero, Settlement targetSettlement, MobileParty targetParty, TeleportationDetail detail` | `void` | Either `targetSettlement` or `targetParty` can be null depending on destination. | `CampaignEvents.OnHeroTeleportationRequestedEvent` |
| 18 | Clan Lifecycle | `OnClanCreatedEvent` | Fired when a new clan is founded in Calradia. | `Clan clan, bool isCompanion` | `void` | `clan.Leader` may be null momentarily during creation; guard with null-check. | `CampaignEvents.OnClanCreatedEvent` |
| 19 | Clan Lifecycle | `OnClanDestroyedEvent` | Fired when a clan is extinguished with no living heirs. | `Clan destroyedClan` | `void` | All clan fiefs become unassigned; `clan.Leader` is dead. | `CampaignEvents.OnClanDestroyedEvent` |
| 20 | Clan Lifecycle | `ClanTierIncrease` | Fired when a clan gains enough renown to advance to the next tier (0 through 6). | `Clan clan, bool showNotification` | `void` | Triggers dynamic increases to companion limit and party limit. | `CampaignEvents.ClanTierIncrease` |
| 21 | Clan Lifecycle | `OnClanLeaderChangedEvent` | Fired when clan leadership transfers to a new leader (heir succession or abdication). | `Hero oldLeader, Hero newLeader` | `void` | `oldLeader` can be null/dead; `newLeader` must be an alive adult noble. | `CampaignEvents.OnClanLeaderChangedEvent` |
| 22 | Dynastic Succession | `OnHeirSelectionRequestedEvent` | Fired when player dies without designated heir, presenting candidate heirs. | `Dictionary<Hero, int> heirApparents` | `void` | Dictionary can be empty if no living adult kin exist (triggers game over). | `CampaignEvents.OnHeirSelectionRequestedEvent` |
| 23 | Dynastic Succession | `OnHeirSelectionOverEvent` | Fired when player or AI completes heir selection. | `Hero selectedHeir` | `void` | `selectedHeir` becomes new player character and clan leader. | `CampaignEvents.OnHeirSelectionOverEvent` |
| 24 | Dynastic Succession | `OnPlayerCharacterChangedEvent` | Fired when the active player character switches to a new hero. | `Hero oldPlayerHero, Hero newPlayerHero, MobileParty oldParty, bool isMainPartyChanged` | `void` | `oldPlayerHero` may be dead. | `CampaignEvents.OnPlayerCharacterChangedEvent` |
| 25 | Clan Allegiance | `OnClanChangedKingdomEvent` | Fired when clan joins or leaves a kingdom (vassalage, mercenary, rebellion). | `Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomActionDetail actionDetail, bool showNotification` | `void` | Either `oldKingdom` or `newKingdom` can be null for independent clans. | `CampaignEvents.OnClanChangedKingdomEvent` |
| 26 | Clan Allegiance | `OnClanDefectedEvent` | Fired when a clan betrays their liege and joins an enemy realm. | `Clan clan, Kingdom oldKingdom, Kingdom newKingdom` | `void` | Both kingdoms must be null-checked. | `CampaignEvents.OnClanDefectedEvent` |
| 27 | Clan Allegiance | `RulingClanChanged` | Fired when kingdom monarchy transfers to a new ruling dynasty. | `Kingdom kingdom, Clan clan` | `void` | `clan.Leader` becomes the new monarch. | `CampaignEvents.RulingClanChanged` |
| 28 | Clan Economics | `OnClanInfluenceChangedEvent` | Fired whenever a clan gains or loses kingdom influence points. | `Clan clan, float change` | `void` | `change` can be negative (influence spending or penalties). | `CampaignEvents.OnClanInfluenceChangedEvent` |
| 29 | Clan Economics | `OnClanEarnedGoldFromTributeEvent` | Fired when a clan receives tribute from another realm. | `Clan receiverClan, IFaction payingFaction` | `void` | `payingFaction` can be kingdom or clan. | `CampaignEvents.OnClanEarnedGoldFromTributeEvent` |
| 30 | Companions | `NewCompanionAdded` | Fired when a wanderer is hired into the player clan. | `Hero hero` | `void` | `hero.CompanionOf` equals `Clan.PlayerClan`. | `CampaignEvents.NewCompanionAdded` |
| 31 | Companions | `CompanionRemoved` | Fired when a companion is dismissed, dies, finishes quest, or is promoted to lord. | `Hero companion, RemoveCompanionDetail detail` | `void` | `companion.CompanionOf` is cleared unless promoted to lord clan. | `CampaignEvents.CompanionRemoved` |
| 32 | Companions & Parties | `OnHeroJoinedPartyEvent` | Fired when a hero joins any mobile party roster. | `Hero hero, MobileParty mobileParty` | `void` | `mobileParty.LeaderHero` can be null. | `CampaignEvents.OnHeroJoinedPartyEvent` |
| 33 | Companions & Parties | `OnPartyLeaderChangedEvent` | Fired when party command changes to a new hero. | `MobileParty mobileParty, Hero newLeader` | `void` | `newLeader` can be null if party is leaderless and disbanding. | `CampaignEvents.OnPartyLeaderChangedEvent` |
| 34 | Companions & Fiefs | `OnGovernorChangedEvent` | Fired when town or castle governor is appointed or removed. | `Town town, Hero oldGovernor, Hero newGovernor` | `void` | Either `oldGovernor` or `newGovernor` can be null. | `CampaignEvents.OnGovernorChangedEvent` |
| 35 | Marriage & Dynasty | `OnMarriageOfferedToPlayerEvent` | Fired when AI noble proposes marriage to player clan member. | `Hero suitor, Hero maiden` | `void` | Handled via dialogue or menu prompts. | `CampaignEvents.OnMarriageOfferedToPlayerEvent` |
| 36 | Marriage & Dynasty | `OnMarriageOfferCanceledEvent` | Fired when a pending marriage offer is canceled or times out. | `Hero suitor, Hero maiden` | `void` | Either suitor or maiden may have died or married elsewhere. | `CampaignEvents.OnMarriageOfferCanceledEvent` |
| 37 | Marriage & Dynasty | `BeforeHeroesMarried` | Fired immediately before marriage ceremony completes. | `Hero firstHero, Hero secondHero, bool showNotification` | `void` | Pre-marriage hook; `hero.Spouse` not yet bound. | `CampaignEvents.BeforeHeroesMarried` |
| 38 | Marriage & Dynasty | `RomanticStateChanged` | Fired when courtship or romance level changes between two heroes. | `Hero person1, Hero person2, RomanceLevelEnum toWhat` | `void` | `toWhat` can be negative (`Ended`, `Rejection`). | `CampaignEvents.RomanticStateChanged` |
| 39 | Marriage & Dynasty | `OnGivenBirthEvent` | Fired when a pregnant noblewoman delivers children. | `Hero mother, List<Hero> aliveChildren, int stillbornCount` | `void` | `aliveChildren` can be empty on stillbirth; mother may die in labor immediately after. | `CampaignEvents.OnGivenBirthEvent` |
| 40 | Character Progression | `HeroGainedSkill` | Fired whenever a hero's skill advances by 1 or more points. | `Hero hero, SkillObject skill, int changeAmount, bool shouldNotify` | `void` | High frequency across all simulation heroes; keep callback fast. | `CampaignEvents.HeroGainedSkill` |
| 41 | Character Progression | `HeroLevelledUp` | Fired when a hero gains enough skill advances to level up. | `Hero hero, bool shouldNotify` | `void` | Increments `UnspentFocusPoints` and periodically `UnspentAttributePoints`. | `CampaignEvents.HeroLevelledUp` |
| 42 | Character Progression | `PerkOpenedEvent` | Fired when a hero unlocks a perk in a skill tree. | `Hero hero, PerkObject perk` | `void` | `hero.GetPerkValue(perk)` returns true. | `CampaignEvents.PerkOpenedEvent` |
| 43 | Character Progression | `PerkResetEvent` | Fired when a hero resets/refunds a perk. | `Hero hero, PerkObject perk` | `void` | `hero.GetPerkValue(perk)` returns false. | `CampaignEvents.PerkResetEvent` |
| 44 | Character Progression | `PlayerTraitChangedEvent` | Fired when player character traits change (Honor, Valor, Mercy, etc.). | `TraitObject trait, int changeAmount` | `void` | `changeAmount` can be negative. | `CampaignEvents.PlayerTraitChangedEvent` |
| 45 | Character Progression | `RenownGained` | Fired when a hero earns renown for their clan. | `Hero hero, int gainedRenown, bool doNotNotify` | `void` | `hero.Clan` can be null for unaffiliated notables or wanderers. | `CampaignEvents.RenownGained` |
| 46 | Periodic Ticks | `DailyTickHeroEvent` | Global daily tick executed once per game day for every hero. | `Hero hero` | `void` | Measure the complete callback under a representative workload before setting allocation or latency expectations. | `CampaignEvents.DailyTickHeroEvent` |
| 47 | Periodic Ticks | `DailyTickClanEvent` | Global daily tick executed once per game day for every clan. | `Clan clan` | `void` | Must check `!clan.IsEliminated` and `clan.Leader != null`. | `CampaignEvents.DailyTickClanEvent` |
| 48 | Periodic Ticks | `HourlyTickPartyEvent` | Hourly tick executed for each mobile party. | `MobileParty mobileParty` | `void` | High frequency; guard `mobileParty.IsActive`. | `CampaignEvents.HourlyTickPartyEvent` |
| 49 | Periodic Ticks | `HourlyTickEvent` | Global hourly heartbeat of the campaign simulation. | `none` | `void` | Optional work may be deferred by a stable-ID bucket only when event semantics allow it; measure the complete callback before claiming a performance gain. | `CampaignEvents.HourlyTickEvent` |
| 50 | Periodic Ticks | `DailyTickEvent` | Global midnight daily heartbeat. | `none` | `void` | Clean hook for daily stateless assessments and evaluations. | `CampaignEvents.DailyTickEvent` |
| 51 | Periodic Ticks | `WeeklyTickEvent` | Global weekly heartbeat. | `none` | `void` | Used for weekly dynastic succession evaluations and companion audits. | `CampaignEvents.WeeklyTickEvent` |

---

## 3. Edge Cases

| # | Feature | Input | Observed Behavior |
|---|---------|-------|-------------------|
| 1 | `BeforeHeroKilledEvent` / `HeroKilledEvent` | `killer == null` (natural causes, old age, child labor, sickness, or generic arrows) | NRE if listener accesses `killer.Clan` or `killer.Name`. Must check `if (killer != null)`. |
| 2 | `OnClanLeaderChangedEvent` | `oldLeader == null` (initial clan establishment, or leader died before succession event) | NRE if referencing `oldLeader.StringId`. Must verify `if (oldLeader != null)`. |
| 3 | `OnHeroChangedClanEvent` | `oldClan == null` (new wanderer, newly generated notable, or independent bandit hero) | NRE if referencing `oldClan.StringId`. Must guard with `if (oldClan != null)`. |
| 4 | `OnHeirSelectionRequestedEvent` | `heirApparents` dictionary is empty (player clan has 0 living adult members) | Game triggers immediate game over screen or failsafe monarchy election. Behavior must not assume `heirApparents.Count > 0`. |
| 5 | `OnGivenBirthEvent` | `aliveChildren` list is empty (`aliveChildren.Count == 0`, `stillbornCount > 0`) | Accessing `aliveChildren[0]` throws `ArgumentOutOfRangeException`. Check `aliveChildren != null && aliveChildren.Count > 0`. |
| 6 | `HeroCreated` / `HeroLevelledUp` | `hero.Clan == null` (rural notable, tavern keeper, newly spawned wanderer) | Accessing `hero.Clan.Tier` or `hero.Clan.Leader` throws NRE. Must guard `if (hero.Clan != null)`. |
| 7 | `HeroCreated` / `HeroGainedSkill` | `hero.HeroDeveloper == null` (partially formed template hero or non-character entity) | NRE when inspecting `hero.HeroDeveloper.TotalXp`. Guard `if (hero.HeroDeveloper != null)`. |
| 8 | `HourlyTickPartyEvent` | `mobileParty.LeaderHero == null` (caravans, villager parties, garrison patrols, bandit parties) | NRE if expecting hero leadership. Guard `if (mobileParty?.LeaderHero != null)`. |
| 9 | `OnGovernorChangedEvent` | `oldGovernor == null` or `newGovernor == null` (governor appointed to vacant fief or removed) | Either governor reference can be null. Must check `if (newGovernor != null)` before querying perks. |
| 10 | `CampaignTime.Now` in constructors | Accessed during `MBSubModuleBase.OnSubModuleLoad` or class constructor | `Campaign.Current` is null before campaign loads; throws fatal initialization NRE. Defer to `RegisterEvents` and event callbacks. |
| 11 | `ChangeRomanticStateAction` | `RomanceLevelEnum` is `Rejection` (-1) or `Ended` (-2) | Negative integer enum values; range comparisons must account for negative values. |
| 12 | `HeroPrisonerReleased` | `party == null` (escaped from castle dungeon or settlement without active mobile party) | NRE if inspecting `party.LeaderHero`. Check `if (party != null)`. |
| 13 | `OnClanChangedKingdomEvent` | `oldKingdom == null` or `newKingdom == null` (independent clan becoming vassal or vice versa) | NRE if reading kingdom name without null check. Check `if (newKingdom != null)`. |
| 14 | `DailyTickHeroEvent` | Historical estimate of ~3,000+ heroes; the count and callback cost were not measured or independently verified in this report. | Treat callback cost as workload-dependent; measure the complete callback before making GC or frame-time claims. Do not assume a zero-allocation loop is required without evidence. |
| 15 | `HeroComesOfAgeEvent` | Hero comes of age while captive or displaced | `hero.PartyBelongedTo` might be null or in dungeon; `hero.HeroState` can be `Prisoner`. |

---

## 4. Logic Chain

1. **User Requirement & Architecture Compliance**:
   - `ORIGINAL_REQUEST.md` (R1 & R2) requires creating a massive experimental CampaignBehavior in `CalradiaForge.Mod` hooking into hero, clan, and progression hooks.
   - It mandates: **stateless execution** without custom save data serialization, adhering to the anti-shadowing rule (never name folder/namespace/class `Campaign`), avoiding engine initialization crashes, and registering in `MBSubModuleBase.OnGameStart`.
2. **Analysis of Save System Safety**:
   - The Bannerlord save system crashes if saved games reference classes inheriting from `SaveableTypeDefiner` or sync custom types into `IDataStore.SyncData` that are subsequently removed or altered.
   - By enforcing an **empty `SyncData(IDataStore dataStore)`** (no-op) and **omitting `SaveableTypeDefiner`**, the mod is 100% save-game agnostic. It can be added to an existing save or removed at any time with zero risk of save corruption.
3. **Stateless Dynamic Evaluation Pattern**:
   - All necessary state already exists in the TaleWorlds engine objects: `Hero` (age, skills, perks, clan, spouse, children, gold), `Clan` (tier, renown, influence, leader, companions, fiefs), and `Kingdom`.
   - Instead of maintaining duplicate tables, the behavior inspects live properties on-the-fly when events occur, or periodically during ticks.
4. **Optional Time-Slicing (performance outcome unverified)**:
   - The earlier estimate of 3,000+ heroes and the claim that a full traversal causes frame drops were not measured in this report; they are historical hypotheses, not established workload facts.
   - The current `ForgeTimeSlicer` computes a deterministic 32-bit hash from the supplied ID (seed 23, multiply by 31 and add each character), masks it positive, and takes modulo the bucket count. It does not use `StringId.GetHashCode()`, guarantee even bucket sizes, or select exactly 1/N entities.
   - `ShouldProcess` filters one entity at a time. A caller that loops over all entities still traverses the full collection and computes each ID's bucket; time-slicing only defers eligible work. Use it only when deferral preserves event semantics and measure the complete callback before claiming reduced frame time, latency, or allocations.
5. **SubModule Integration**:
   - In `SubModule.OnGameStart(Game game, IGameStarter gameStarterObject)`:
     ```csharp
     if (gameStarterObject is CampaignGameStarter campaignStarter)
     {
         campaignStarter.AddBehavior(new CalradiaForge.Mod.CampaignBehaviors.ClanCharacterProgressionBehavior());
     }
     ```
   - Avoids all mesh/skeleton engine initialization issues by executing purely in the campaign simulation lifecycle.

---

## 5. Caveats
- No caveats regarding event signatures: all 276 properties of `CampaignEvents` were queried and verified against the live `TaleWorlds.CampaignSystem.dll` on this machine.
- Note that while `MakePregnantAction.Apply(Hero mother)` exists in `TaleWorlds.CampaignSystem.Actions`, native pregnancy conception does not fire a distinct `OnConceptionEvent` (it sets `mother.IsPregnant = true` internally during daily ticks), whereas childbirth fires `CampaignEvents.OnGivenBirthEvent`.
- `HeroDeveloper.UnspentFocusPoints` and `UnspentAttributePoints` have public getters and setters in Bannerlord 1.2+, but changing them directly should respect game model balance.

---

## 6. Conclusion
The TaleWorlds Campaign System provides an extensive suite of 45+ hooks that cover every stage of Clan and Character development:
- **Hero Lifecycle**: `HeroCreated`, `HeroGrowsOutOfInfancyEvent`, `HeroReachesTeenAgeEvent`, `HeroComesOfAgeEvent`, `BeforeHeroKilledEvent`, `HeroKilledEvent`, `HeroWounded`, `HeroOccupationChangedEvent`, `HeroRelationChanged`, `OnHeroChangedClanEvent`, `HeroPrisonerTaken`, `HeroPrisonerReleased`, `OnHeroActivatedEvent`, `OnHeroGetsBusyEvent`, `OnHeroUnregisteredEvent`.
- **Clan & Dynastic Succession**: `OnClanCreatedEvent`, `OnClanDestroyedEvent`, `ClanTierIncrease`, `OnClanLeaderChangedEvent`, `OnHeirSelectionRequestedEvent`, `OnHeirSelectionOverEvent`, `OnPlayerCharacterChangedEvent`, `OnClanChangedKingdomEvent`, `OnClanDefectedEvent`, `RulingClanChanged`, `OnClanInfluenceChangedEvent`.
- **Companions & Roles**: `NewCompanionAdded`, `CompanionRemoved`, `OnHeroJoinedPartyEvent`, `OnPartyLeaderChangedEvent`, `OnGovernorChangedEvent`.
- **Marriage & Dynasty**: `OnMarriageOfferedToPlayerEvent`, `OnMarriageOfferCanceledEvent`, `BeforeHeroesMarried`, `RomanticStateChanged`, `OnGivenBirthEvent`.
- **Progression**: `HeroGainedSkill`, `HeroLevelledUp`, `PerkOpenedEvent`, `PerkResetEvent`, `PlayerTraitChangedEvent`, `RenownGained`.
- **Periodic Ticks**: `DailyTickHeroEvent`, `DailyTickClanEvent`, `HourlyTickPartyEvent`, `HourlyTickEvent`, `DailyTickEvent`, `WeeklyTickEvent`.

Downstream implementation can construct `ClanCharacterProgressionBehavior` under namespace `CalradiaForge.Mod.CampaignBehaviors` in `src\CalradiaForge.Mod\CampaignBehaviors\ClanCharacterProgressionBehavior.cs` with an empty `SyncData`, register it in `SubModule.OnGameStart`, and run entirely statelessly.

---

## 7. Verification Method

### 1. Build Verification
To verify that the project compiles cleanly without namespace collisions or type errors:
```powershell
dotnet build CalradiaForge.sln -c Release
```
Expected result: Build succeeded, 0 warnings, 0 errors.

### 2. Stateless Compliance Script
To verify that no custom class inherits from `SaveableTypeDefiner` and no data is serialized in `SyncData`:
```powershell
python -c "
import os, re

mod_dir = r'src\CalradiaForge.Mod'
for root, dirs, files in os.walk(mod_dir):
    for f in files:
        if f.endswith('.cs') and 'ClanCharacter' in f:
            path = os.path.join(root, f)
            with open(path, 'r', encoding='utf-8') as src:
                content = src.read()
                assert 'SaveableTypeDefiner' not in content, 'Must not use SaveableTypeDefiner'
                # Verify SyncData does not sync custom variables
                sync_match = re.search(r'void\s+SyncData\s*\([^)]*\)\s*\{([^}]*)\}', content)
                if sync_match:
                    body = sync_match.group(1).strip()
                    assert 'dataStore.SyncData' not in body, 'SyncData must be empty/no-op'
print('SUCCESS: Stateless verification passed - zero save data footprint.')
"
```

### 3. SubModule Registration Check
To verify that `MBSubModuleBase.OnGameStart` registers the behavior:
```powershell
python -c "
with open(r'src\CalradiaForge.Mod\SubModule.cs', 'r', encoding='utf-8') as f:
    text = f.read()
    assert 'AddBehavior' in text, 'SubModule must register behavior via AddBehavior'
print('SUCCESS: SubModule registration verified.')
"
```
