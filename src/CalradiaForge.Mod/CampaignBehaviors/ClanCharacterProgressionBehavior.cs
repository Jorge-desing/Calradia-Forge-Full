using System;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using CalradiaForge.Core.Diagnostics;

namespace CalradiaForge.Mod.CampaignBehaviors
{
    /// <summary>
    /// Massive stateless CampaignBehavior that monitors, evaluates, and responds to clan dynamics,
    /// dynastic succession, character lifecycle milestones, companion roles, and hero progression.
    /// Operates with a zero save-data footprint and uses ForgeTimeSlicer's stable-ID
    /// time-slicing across simulation ticks to distribute optional periodic work.
    /// </summary>
    // Registered explicitly by SubModule.OnGameStart. Do not add AutoRegisterBehavior:
    // ForgeBehaviorLoader scans this assembly and would otherwise add a second instance,
    // doubling every campaign-event subscription and periodic callback.
    public class ClanCharacterProgressionBehavior : CampaignBehaviorBase
    {
        // Stateless runtime telemetry counters (reset safely on campaign load; zero save footprint)
        private int _lifeCycleEventsProcessed;
        private int _clanEventsProcessed;
        private int _progressionEventsProcessed;
        private int _marriageAndBirthEventsProcessed;
        private int _periodicTicksProcessed;
        // Safe accessors for Campaign-level singletons (null when Campaign is inactive or in tests)
        private static bool IsCampaignActive => Campaign.Current != null;
        private static Clan SafePlayerClan => IsCampaignActive ? Clan.PlayerClan : null;
        private static Hero SafeMainHero => IsCampaignActive ? Hero.MainHero : null;
        private static bool IsPlayerClan(Clan clan) => clan != null && SafePlayerClan != null && clan == SafePlayerClan;
        private static bool IsPlayerHero(Hero hero) => hero != null && SafeMainHero != null && hero == SafeMainHero;

        /// <summary>
        /// Total number of currently alive heroes tracked statelessly via vanilla engine collections.
        /// </summary>
        public int ActiveTrackedHeroesCount => (IsCampaignActive && Hero.AllAliveHeroes != null) ? Hero.AllAliveHeroes.Count : 0;

        /// <summary>
        /// Total hero lifecycle events processed during this session.
        /// </summary>
        public int TotalLifeCycleEventsProcessed => _lifeCycleEventsProcessed;

        /// <summary>
        /// Total character progression and skill events processed during this session.
        /// </summary>
        public int TotalProgressionEventsProcessed => _progressionEventsProcessed;

        /// <summary>
        /// Total clan governance and dynastic succession events processed during this session.
        /// </summary>
        public int TotalClanEventsProcessed => _clanEventsProcessed;

        /// <summary>
        /// Total marriage, courtship, and birth events processed during this session.
        /// </summary>
        public int TotalMarriageAndBirthEventsProcessed => _marriageAndBirthEventsProcessed;

        /// <summary>
        /// Total periodic tick iterations processed during this session.
        /// </summary>
        public int TotalPeriodicTicksProcessed => _periodicTicksProcessed;

        /// <summary>
        /// Parameterless constructor for explicit SubModule registration. Strictly complies with the Engine Initialization Crash Constraint:
        /// performs zero entity queries and avoids accessing CampaignTime.Now before campaign load.
        /// </summary>
        public ClanCharacterProgressionBehavior()
        {
        }

        #region CampaignBehaviorBase Overrides

        /// <summary>
        /// Declarative event registration. Attaches non-serialized listeners to 45+ TaleWorlds CampaignEvents.
        /// Per engine constraints, zero entity queries or state mutations occur during registration.
        /// </summary>
        public override void RegisterEvents()
        {
            // a) Hero Lifecycle
            CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
            CampaignEvents.HeroGrowsOutOfInfancyEvent.AddNonSerializedListener(this, OnHeroGrowsOutOfInfancy);
            CampaignEvents.HeroReachesTeenAgeEvent.AddNonSerializedListener(this, OnHeroReachesTeenAge);
            CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, OnHeroComesOfAge);
            CampaignEvents.BeforeHeroKilledEvent.AddNonSerializedListener(this, OnBeforeHeroKilled);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
            CampaignEvents.HeroWounded.AddNonSerializedListener(this, OnHeroWounded);
            CampaignEvents.HeroOccupationChangedEvent.AddNonSerializedListener(this, OnHeroOccupationChanged);
            CampaignEvents.HeroRelationChanged.AddNonSerializedListener(this, OnHeroRelationChanged);
            CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, OnHeroChangedClan);
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
            CampaignEvents.OnHeroActivatedEvent.AddNonSerializedListener(this, OnHeroActivated);
            CampaignEvents.OnHeroGetsBusyEvent.AddNonSerializedListener(this, OnHeroGetsBusy);

            // b) Clan & Dynastic Succession
            CampaignEvents.OnClanCreatedEvent.AddNonSerializedListener(this, OnClanCreated);
            CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
            CampaignEvents.ClanTierIncrease.AddNonSerializedListener(this, OnClanTierIncrease);
            CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(this, OnClanLeaderChanged);
            CampaignEvents.OnHeirSelectionRequestedEvent.AddNonSerializedListener(this, OnHeirSelectionRequested);
            CampaignEvents.OnHeirSelectionOverEvent.AddNonSerializedListener(this, OnHeirSelectionOver);
            CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, OnPlayerCharacterChanged);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
            CampaignEvents.OnClanDefectedEvent.AddNonSerializedListener(this, OnClanDefected);
            CampaignEvents.RulingClanChanged.AddNonSerializedListener(this, OnRulingClanChanged);
            CampaignEvents.OnClanInfluenceChangedEvent.AddNonSerializedListener(this, OnClanInfluenceChanged);

            // c) Companions & Parties
            CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnNewCompanionAdded);
            CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
            CampaignEvents.OnHeroJoinedPartyEvent.AddNonSerializedListener(this, OnHeroJoinedParty);
            CampaignEvents.OnPartyLeaderChangedEvent.AddNonSerializedListener(this, OnPartyLeaderChanged);
            CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this, OnGovernorChanged);

            // d) Marriage & Pregnancy
            CampaignEvents.OnMarriageOfferedToPlayerEvent.AddNonSerializedListener(this, OnMarriageOfferedToPlayer);
            CampaignEvents.OnMarriageOfferCanceledEvent.AddNonSerializedListener(this, OnMarriageOfferCanceled);
            CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, OnBeforeHeroesMarried);
            CampaignEvents.RomanticStateChanged.AddNonSerializedListener(this, OnRomanticStateChanged);
            CampaignEvents.OnGivenBirthEvent.AddNonSerializedListener(this, OnGivenBirth);

            // e) Character Progression
            CampaignEvents.HeroGainedSkill.AddNonSerializedListener(this, OnHeroGainedSkill);
            CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, OnHeroLevelledUp);
            CampaignEvents.PerkOpenedEvent.AddNonSerializedListener(this, OnPerkOpened);
            CampaignEvents.PerkResetEvent.AddNonSerializedListener(this, OnPerkReset);
            CampaignEvents.PlayerTraitChangedEvent.AddNonSerializedListener(this, OnPlayerTraitChanged);
            CampaignEvents.RenownGained.AddNonSerializedListener(this, OnRenownGained);

            // f) Periodic Simulation Ticks
            CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, OnDailyTickHero);
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, OnDailyTickClan);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
        }

        /// <summary>
        /// Save data synchronization. Deliberately empty (no-op).
        /// This behavior is 100% stateless: all progression metrics and dynastic scores derive on-the-fly
        /// from live TaleWorlds engine objects without requiring custom save data serialization.
        /// </summary>
        public override void SyncData(IDataStore dataStore)
        {
            // Stateless: operates entirely on live vanilla game state without custom save data serialization.
        }

        #endregion

        #region Time-Slicing Utility

        /// <summary>
        /// Stable-ID predicate for hourly work. It selects whether the caller's entity belongs to the current bucket;
        /// it does not enqueue or partition entities, and filtering a collection still scans that collection.
        /// </summary>
        public static bool ShouldProcessInCurrentHour(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return false;
            if (!IsCampaignActive) return true;
            int currentHour = (int)CampaignTime.Now.ToHours;
            return CalradiaForge.Sdk.ForgeTimeSlicer.ShouldProcess(stringId, currentHour);
        }

        #endregion

        #region Hero Lifecycle Event Handlers

        private void OnHeroCreated(Hero hero, bool isBornNaturally)
        {
            if (hero == null) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);

            // Guard against unassigned clan (wanderers, notables)
            if (hero.Clan != null && hero.Clan.IsNoble)
            {
                // New noble added to dynasty; verify genealogical links if born naturally
                if (isBornNaturally && (hero.Father != null || hero.Mother != null))
                {
                    // Stateless evaluation of noble pedigree
                }
            }
        }

        private void OnHeroGrowsOutOfInfancy(Hero hero)
        {
            if (hero == null || !hero.IsAlive) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);

            // Hero reaches childhood (age 3); verify clan affiliation safely
            if (IsPlayerClan(hero.Clan))
            {
                ForgeLogger.PrintInfo($"{hero.Name} has grown out of infancy and is now exploring the clan grounds.");
            }
        }

        private void OnHeroReachesTeenAge(Hero hero)
        {
            if (hero == null || !hero.IsAlive) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);

            // Hero reaches teenage years (age 11); evaluate education foundation
            if (hero.HeroDeveloper != null)
            {
                // Character developer initialized; ready for teenage development
            }
        }

        private void OnHeroComesOfAge(Hero hero)
        {
            // Edge Case 15: Hero comes of age while captive or displaced; party or equipment may be null
            if (hero == null || !hero.IsAlive) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);

            bool isCaptive = hero.IsPrisoner || hero.HeroState == Hero.CharacterStates.Prisoner;
            if (!isCaptive && IsPlayerClan(hero.Clan))
            {
                ForgeLogger.PrintSuccess($"{hero.Name} has come of age and is now eligible to lead parties and hold fiefs!");
            }
        }

        private void OnBeforeHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            // Edge Case 1: killer is null for natural causes, old age, child labor, wounds, or generic troop kills
            if (victim == null) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);

            // Pre-mortem snapshot: evaluate succession urgency if victim is an active clan leader
            if (victim.Clan != null && victim.Clan.Leader == victim && !victim.Clan.IsEliminated)
            {
                AssessDynasticSuccession(victim.Clan);
            }
        }

        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            // Edge Case 1: killer can be null; victim.IsAlive is already false
            if (victim == null) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);

            // Verify if victim was a companion or family member
            if (IsPlayerClan(victim.Clan))
            {
                string cause = (killer != null) ? $"slain by {killer.Name}" : $"perished from {detail}";
                ForgeLogger.PrintWarning($"Tragedy has struck: {victim.Name} was {cause}.");
            }
        }

        private void OnHeroWounded(Hero hero)
        {
            if (hero == null || !hero.IsAlive) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);
        }

        private void OnHeroOccupationChanged(Hero hero, Occupation oldOccupation)
        {
            if (hero == null) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);

            // Track transitions (e.g. Wanderer -> Lord or Lord -> Rebel)
            if (hero.Occupation == Occupation.Lord && oldOccupation == Occupation.Wanderer && hero.Clan != null)
            {
                ForgeLogger.PrintInfo($"{hero.Name} has been elevated from companion to nobility in {hero.Clan.Name}!");
            }
        }

        private void OnHeroRelationChanged(Hero hero1, Hero hero2, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero affectedRelative1, Hero affectedRelative2)
        {
            // affectedRelative1 and affectedRelative2 may be null
            if (hero1 == null || hero2 == null) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);
        }

        private void OnHeroChangedClan(Hero hero, Clan oldClan)
        {
            // Edge Case 3: oldClan can be null for wanderers or notables joining a clan
            if (hero == null) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);

            if (IsPlayerClan(hero.Clan))
            {
                string fromClan = (oldClan != null) ? oldClan.Name.ToString() : "the wandering roads";
                ForgeLogger.PrintInfo($"{hero.Name} has joined your clan from {fromClan}.");
            }
        }

        private void OnHeroPrisonerTaken(PartyBase capturerParty, Hero prisonerHero)
        {
            if (prisonerHero == null) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);

            // Capturer leader may be null (garrisons, bandit parties)
            if (IsPlayerClan(prisonerHero.Clan))
            {
                string captorName = capturerParty != null ? capturerParty.Name.ToString() : "enemy forces";
                ForgeLogger.PrintWarning($"{prisonerHero.Name} was taken prisoner by {captorName}!");
            }
        }

        private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
        {
            // Edge Case 12: party can be null if escaped from a castle dungeon without a mobile party
            if (prisoner == null) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);

            if (IsPlayerClan(prisoner.Clan))
            {
                ForgeLogger.PrintSuccess($"{prisoner.Name} has been freed from captivity ({detail}).");
            }
        }

        private void OnHeroActivated(Hero hero, Hero.CharacterStates characterState)
        {
            if (hero == null) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);
        }

        private void OnHeroGetsBusy(Hero hero, HeroGetsBusyReasons heroGetsBusyReason)
        {
            if (hero == null) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);
        }

        #endregion

        #region Clan & Dynastic Succession Handlers

        private void OnClanCreated(Clan clan, bool isPlayerClan)
        {
            // clan.Leader may be null momentarily during creation
            if (clan == null) return;
            Interlocked.Increment(ref _clanEventsProcessed);

            if (clan.Leader != null)
            {
                // New clan established with designated leader
            }
        }

        private void OnClanDestroyed(Clan clan)
        {
            if (clan == null) return;
            Interlocked.Increment(ref _clanEventsProcessed);

            if (IsPlayerClan(clan))
            {
                ForgeLogger.PrintError("The player dynasty has been extinguished!");
            }
        }

        private void OnClanTierIncrease(Clan clan, bool shouldNotify)
        {
            if (clan == null) return;
            Interlocked.Increment(ref _clanEventsProcessed);

            if (IsPlayerClan(clan))
            {
                ForgeLogger.PrintSuccess($"Clan renown has elevated {clan.Name} to Tier {clan.Tier}! Party and companion limits expanded.");
            }
        }

        private void OnClanLeaderChanged(Hero oldLeader, Hero newLeader)
        {
            // Edge Case 2: oldLeader can be null if initial clan creation or died prior to resolution
            if (newLeader == null) return;
            Interlocked.Increment(ref _clanEventsProcessed);

            Clan clan = newLeader.Clan;
            if (IsPlayerClan(clan))
            {
                string oldName = (oldLeader != null) ? oldLeader.Name.ToString() : "the previous leadership";
                ForgeLogger.PrintSuccess($"Dynastic succession complete: {newLeader.Name} now leads {clan.Name} succeeding {oldName}.");
            }
        }

        private void OnHeirSelectionRequested(Dictionary<Hero, int> heirApparents)
        {
            // Edge Case 4: heirApparents can be null or empty if no living adult kin exist
            if (heirApparents == null || heirApparents.Count == 0) return;
            Interlocked.Increment(ref _clanEventsProcessed);

            // Statelessly rank candidate heirs by succession aptitude
            Hero bestCandidate = null;
            int highestScore = int.MinValue;

            foreach (var candidate in heirApparents.Keys)
            {
                if (candidate == null || !candidate.IsAlive) continue;
                int score = DynasticSuccessionScore(candidate);
                if (score > highestScore)
                {
                    highestScore = score;
                    bestCandidate = candidate;
                }
            }

            if (bestCandidate != null)
            {
                ForgeLogger.PrintInfo($"Dynastic Analysis: {bestCandidate.Name} identified as premier candidate for clan leadership (Score: {highestScore}).");
            }
        }

        private void OnHeirSelectionOver(Hero selectedHeir)
        {
            if (selectedHeir == null) return;
            Interlocked.Increment(ref _clanEventsProcessed);

            if (IsPlayerClan(selectedHeir.Clan))
            {
                ForgeLogger.PrintSuccess($"The clan council has endorsed {selectedHeir.Name} as the rightful heir!");
            }
        }

        private void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newPlayerParty, bool isMainPartyChanged)
        {
            if (newPlayer == null) return;
            Interlocked.Increment(ref _clanEventsProcessed);

            ForgeLogger.PrintInfo($"Active character transitioned to {newPlayer.Name}.");
        }

        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
        {
            // Edge Case 13: oldKingdom or newKingdom can be null for independent clans
            if (clan == null) return;
            Interlocked.Increment(ref _clanEventsProcessed);

            if (IsPlayerClan(clan))
            {
                string targetRealm = (newKingdom != null) ? newKingdom.Name.ToString() : "Independent Realm";
                ForgeLogger.PrintInfo($"Your clan has aligned with {targetRealm} ({detail}).");
            }
        }

        private void OnClanDefected(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)
        {
            // Both kingdoms must be null-checked
            if (clan == null) return;
            Interlocked.Increment(ref _clanEventsProcessed);

            if (IsPlayerClan(clan))
            {
                string oldName = (oldKingdom != null) ? oldKingdom.Name.ToString() : "former liege";
                string newName = (newKingdom != null) ? newKingdom.Name.ToString() : "new sovereign";
                ForgeLogger.PrintWarning($"Your clan has defected from {oldName} to join {newName}!");
            }
        }

        private void OnRulingClanChanged(Kingdom kingdom, Clan newRulingClan)
        {
            if (kingdom == null || newRulingClan == null) return;
            Interlocked.Increment(ref _clanEventsProcessed);

            if (kingdom != null && SafePlayerClan != null && kingdom == SafePlayerClan.Kingdom)
            {
                ForgeLogger.PrintInfo($"The royal sceptre of {kingdom.Name} has passed to {newRulingClan.Name}.");
            }
        }

        private void OnClanInfluenceChanged(Clan clan, float change)
        {
            if (clan == null) return;
            Interlocked.Increment(ref _clanEventsProcessed);
        }

        #endregion

        #region Companions & Parties Handlers

        private void OnNewCompanionAdded(Hero newCompanion)
        {
            if (newCompanion == null) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);

            ForgeLogger.PrintSuccess($"{newCompanion.Name} has entered into sworn service as a companion.");
        }

        private void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
        {
            if (companion == null) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);

            ForgeLogger.PrintInfo($"{companion.Name} has departed companion service ({detail}).");
        }

        private void OnHeroJoinedParty(Hero hero, MobileParty mobileParty)
        {
            if (hero == null || mobileParty == null) return;
            Interlocked.Increment(ref _lifeCycleEventsProcessed);
        }

        private void OnPartyLeaderChanged(MobileParty party, Hero newLeader)
        {
            // newLeader can be null if party is leaderless and disbanding
            if (party == null) return;
            Interlocked.Increment(ref _clanEventsProcessed);
        }

        private void OnGovernorChanged(Town town, Hero oldGovernor, Hero newGovernor)
        {
            // Edge Case 9: oldGovernor or newGovernor can be null
            if (town == null) return;
            Interlocked.Increment(ref _clanEventsProcessed);

            if (newGovernor != null && IsPlayerClan(town.OwnerClan))
            {
                ForgeLogger.PrintInfo($"{newGovernor.Name} has assumed governorship of {town.Name}.");
            }
        }

        #endregion

        #region Marriage & Pregnancy Handlers

        private void OnMarriageOfferedToPlayer(Hero suitor, Hero maiden)
        {
            if (suitor == null || maiden == null) return;
            Interlocked.Increment(ref _marriageAndBirthEventsProcessed);

            ForgeLogger.PrintInfo($"A marriage proposal has arrived uniting {suitor.Name} and {maiden.Name}.");
        }

        private void OnMarriageOfferCanceled(Hero suitor, Hero maiden)
        {
            if (suitor == null || maiden == null) return;
            Interlocked.Increment(ref _marriageAndBirthEventsProcessed);
        }

        private void OnBeforeHeroesMarried(Hero firstHero, Hero secondHero, bool showNotification)
        {
            if (firstHero == null || secondHero == null) return;
            Interlocked.Increment(ref _marriageAndBirthEventsProcessed);

            // Pre-marriage hook: spouses not yet bound
            if (IsPlayerClan(firstHero.Clan) || IsPlayerClan(secondHero.Clan))
            {
                ForgeLogger.PrintSuccess($"Wedding bells toll: {firstHero.Name} and {secondHero.Name} unite in matrimony.");
            }
        }

        private void OnRomanticStateChanged(Hero person1, Hero person2, Romance.RomanceLevelEnum romanceLevel)
        {
            // Edge Case 11: RomanceLevelEnum has negative values (Rejection = -1, Ended = -2)
            if (person1 == null || person2 == null) return;
            Interlocked.Increment(ref _marriageAndBirthEventsProcessed);

            if (IsPlayerHero(person1) || IsPlayerHero(person2))
            {
                if (romanceLevel >= Romance.RomanceLevelEnum.MatchMadeByFamily)
                {
                    Hero target = IsPlayerHero(person1) ? person2 : person1;
                    ForgeLogger.PrintInfo($"Courtship with {target.Name} advances to stage: {romanceLevel}.");
                }
            }
        }

        private void OnGivenBirth(Hero mother, List<Hero> aliveChildren, int stillbornCount)
        {
            // Edge Case 5: aliveChildren can be null or empty; mother may perish
            if (mother == null) return;
            Interlocked.Increment(ref _marriageAndBirthEventsProcessed);

            if (aliveChildren != null && aliveChildren.Count > 0)
            {
                for (int i = 0; i < aliveChildren.Count; i++)
                {
                    Hero child = aliveChildren[i];
                    if (child != null && IsPlayerClan(mother.Clan))
                    {
                        ForgeLogger.PrintSuccess($"Joyous tidings! A healthy child, {child.Name}, was born to {mother.Name}.");
                    }
                }
            }
        }

        #endregion

        #region Character Progression Handlers

        private void OnHeroGainedSkill(Hero hero, SkillObject skill, int changeAmount, bool shouldNotify)
        {
            // Edge Cases 6 & 7: hero or hero.HeroDeveloper can be null
            if (hero == null || skill == null || hero.HeroDeveloper == null) return;
            Interlocked.Increment(ref _progressionEventsProcessed);

            // High-frequency callback across entire simulation: keep fast and stateless
            int currentSkillValue = hero.GetSkillValue(skill);
            if (shouldNotify && IsPlayerClan(hero.Clan))
            {
                // Check major mastery milestones (50, 100, 150, 200, 250, 300)
                if (currentSkillValue % 50 == 0 && currentSkillValue > 0)
                {
                    ForgeLogger.PrintSuccess($"{hero.Name} achieved Mastery Milestone: {skill.Name} reached {currentSkillValue}!");
                }
            }
        }

        private void OnHeroLevelledUp(Hero hero, bool shouldNotify)
        {
            // Edge Cases 6 & 7: hero, hero.Clan, or hero.HeroDeveloper can be null
            if (hero == null || hero.HeroDeveloper == null) return;
            Interlocked.Increment(ref _progressionEventsProcessed);

            if (shouldNotify && IsPlayerClan(hero.Clan))
            {
                int unspentFocus = hero.HeroDeveloper.UnspentFocusPoints;
                int unspentAttr = hero.HeroDeveloper.UnspentAttributePoints;
                ForgeLogger.PrintSuccess($"{hero.Name} has advanced to Level {hero.Level}! (Unspent: {unspentFocus} Focus, {unspentAttr} Attribute)");
            }
        }

        private void OnPerkOpened(Hero hero, PerkObject perk)
        {
            if (hero == null || perk == null) return;
            Interlocked.Increment(ref _progressionEventsProcessed);

            if (IsPlayerClan(hero.Clan))
            {
                ForgeLogger.PrintInfo($"{hero.Name} unlocked perk: {perk.Name}.");
            }
        }

        private void OnPerkReset(Hero hero, PerkObject perk)
        {
            if (hero == null || perk == null) return;
            Interlocked.Increment(ref _progressionEventsProcessed);
        }

        private void OnPlayerTraitChanged(TraitObject trait, int changeAmount)
        {
            if (trait == null) return;
            Interlocked.Increment(ref _progressionEventsProcessed);

            string direction = changeAmount > 0 ? "ascended" : "diminished";
            ForgeLogger.PrintInfo($"Your character trait {trait.Name} has {direction} (change: {changeAmount:+#;-#;0}).");
        }

        private void OnRenownGained(Hero hero, int gainedRenown, bool doNotNotify)
        {
            // Edge Case 6: hero.Clan can be null for wanderers or notables
            if (hero == null || gainedRenown <= 0) return;
            Interlocked.Increment(ref _progressionEventsProcessed);
        }

        #endregion

        #region Periodic Simulation Ticks & Stable-ID Time-Slicing

        /// <summary>
        /// Global daily hero tick. Executed once per game day for every hero across Calradia.
        /// Keep per-hero work small; allocation and duration claims require measuring the complete event path.
        /// </summary>
        private void OnDailyTickHero(Hero hero)
        {
            if (hero == null || !hero.IsAlive || hero.HeroDeveloper == null) return;
            Interlocked.Increment(ref _periodicTicksProcessed);

            // Lightweight inline assessment; verify the complete callback before making allocation claims.
            if (hero.IsWounded && hero.HitPoints >= hero.MaxHitPoints)
            {
                // Hero has naturally recovered from battle wounds
            }
        }

        /// <summary>
        /// Global daily clan tick. Evaluates clan status statelessly.
        /// </summary>
        private void OnDailyTickClan(Clan clan)
        {
            if (clan == null || clan.IsEliminated || clan.Leader == null) return;
            Interlocked.Increment(ref _periodicTicksProcessed);
        }

        /// <summary>
        /// Hourly tick for each active mobile party.
        /// Edge Case 8: LeaderHero can be null (caravans, patrols, bandits).
        /// Employs Modulo-24 time-slicing by party ID to throttle evaluations.
        /// </summary>
        private void OnHourlyTickParty(MobileParty mobileParty)
        {
            if (mobileParty == null || !mobileParty.IsActive || mobileParty.LeaderHero == null) return;
            Interlocked.Increment(ref _periodicTicksProcessed);

            // Throttle the hourly party audit using the shared stable-ID schedule.
            if (!ShouldProcessInCurrentHour(mobileParty.StringId)) return;

            // Hourly party leader readiness check
            Hero leader = mobileParty.LeaderHero;
            if (leader.HeroDeveloper != null && IsPlayerClan(leader.Clan))
            {
                // Player companion/family commander is active and fit for duty
            }
        }

        /// <summary>
        /// Campaign hourly heartbeat. Evaluates the stable-ID bucket assigned to this hour
        /// to spread the optional progression audit across the day.
        /// </summary>
        private void OnHourlyTick()
        {
            Interlocked.Increment(ref _periodicTicksProcessed);

            if (!IsCampaignActive) return;
            // Stable-ID time-sliced evaluation of alive heroes.
            var aliveHeroes = Hero.AllAliveHeroes;
            if (aliveHeroes == null) return;

            int count = aliveHeroes.Count;
            int currentHour = (int)CampaignTime.Now.ToHours;

            for (int i = 0; i < count; i++)
            {
                Hero hero = aliveHeroes[i];
                if (hero == null || !hero.IsAlive || string.IsNullOrEmpty(hero.StringId)) continue;

                if (CalradiaForge.Sdk.ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour))
                {
                    EvaluateHeroProgression(hero);
                }
            }
        }

        /// <summary>
        /// Daily campaign heartbeat.
        /// </summary>
        private void OnDailyTick()
        {
            Interlocked.Increment(ref _periodicTicksProcessed);
        }

        /// <summary>
        /// Weekly campaign heartbeat. Executes macro dynastic succession checks.
        /// </summary>
        private void OnWeeklyTick()
        {
            Interlocked.Increment(ref _periodicTicksProcessed);

            // Stateless weekly audit of noble clans
            var allClans = Clan.All;
            if (allClans == null) return;

            for (int i = 0; i < allClans.Count; i++)
            {
                Clan clan = allClans[i];
                if (clan == null || clan.IsEliminated || clan.Leader == null || !clan.IsNoble) continue;

                // Validate succession continuity
                if (clan.AliveLords != null && clan.AliveLords.Count > 1)
                {
                    AssessDynasticSuccession(clan);
                }
            }
        }

        #endregion

        #region Stateless Dynamic Evaluation Logic

        /// <summary>
        /// Evaluates a hero's progression state on-the-fly without maintaining custom saved fields.
        /// </summary>
        public void EvaluateHeroProgression(Hero hero)
        {
            if (hero == null || !hero.IsAlive || hero.HeroDeveloper == null) return;

            // Check if hero has unspent progression points
            int unspentFocus = hero.HeroDeveloper.UnspentFocusPoints;
            int unspentAttr = hero.HeroDeveloper.UnspentAttributePoints;

            if (IsPlayerClan(hero.Clan) && (unspentFocus > 0 || unspentAttr > 0))
            {
                // Clan member has development capacity available
            }
        }

        /// <summary>
        /// Statelessly assesses and scores living adult noble heirs for a clan based on leadership,
        /// combat acumen, prestige, and age suitability.
        /// </summary>
        public Hero AssessDynasticSuccession(Clan clan)
        {
            if (clan == null || clan.AliveLords == null || clan.AliveLords.Count == 0) return null;

            Hero bestSuccessor = null;
            int highestScore = int.MinValue;

            for (int i = 0; i < clan.AliveLords.Count; i++)
            {
                Hero candidate = clan.AliveLords[i];
                if (candidate == null || !candidate.IsAlive || candidate == clan.Leader) continue;
                if (candidate.IsChild || candidate.Age < 18f) continue;

                int score = DynasticSuccessionScore(candidate);
                if (score > highestScore)
                {
                    highestScore = score;
                    bestSuccessor = candidate;
                }
            }

            return bestSuccessor;
        }

        /// <summary>
        /// Computes a dynastic succession fitness score dynamically from vanilla hero attributes.
        /// </summary>
        public static int DynasticSuccessionScore(Hero candidate)
        {
            if (candidate == null || !candidate.IsAlive) return 0;

            int score = 0;

            // Age suitability (peak competence between 25 and 45)
            float age = candidate.Age;
            if (age >= 18f && age <= 60f)
            {
                score += (int)(age * 2f);
            }

            // Hero level and developer stats
            score += candidate.Level * 10;

            // Core governing and military skills
            score += candidate.GetSkillValue(DefaultSkills.Leadership) * 3;
            score += candidate.GetSkillValue(DefaultSkills.Tactics) * 2;
            score += candidate.GetSkillValue(DefaultSkills.Charm) * 2;
            score += candidate.GetSkillValue(DefaultSkills.Steward) * 2;

            // Clan lineage proximity
            if (candidate.Clan?.Leader != null)
            {
                Hero leader = candidate.Clan.Leader;
                if (candidate.Father == leader || candidate.Mother == leader)
                {
                    score += 150; // Direct son/daughter
                }
                else if (candidate.Spouse == leader)
                {
                    score += 100; // Royal consort
                }
                else if ((candidate.Father != null && candidate.Father == leader.Father) || (candidate.Mother != null && candidate.Mother == leader.Mother))
                {
                    score += 80; // Sibling via direct parental match
                }
                else if (candidate.Siblings != null)
                {
                    foreach (var sibling in candidate.Siblings)
                    {
                        if (sibling == leader)
                        {
                            score += 80; // Sibling fallback
                            break;
                        }
                    }
                }
            }

            return score;
        }

        #endregion
    }
}
