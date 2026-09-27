using System;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using CalradiaForge.Sdk;

namespace CalradiaForge.Mod.CampaignBehaviors
{
    /// <summary>
    /// Stateless CampaignBehavior that records, manages, and decays CoALA-based cognitive
    /// memories (semantic facts and episodic experiences) for campaign heroes and NPCs in real time.
    /// Operates with zero save-game footprint via volatile <see cref="ForgeAgentMemory"/> and
    /// utilizes modulo-24 hash time-slicing across hourly simulation ticks to eliminate frame drops.
    /// </summary>
    // Registered explicitly by SubModule.OnGameStart. Do not add AutoRegisterBehavior:
    // ForgeBehaviorLoader scans this assembly and would otherwise add a second instance,
    // doubling every campaign-event subscription and periodic callback.
    public class AgentCognitiveMemoryBehavior : CampaignBehaviorBase
    {
        private static AgentCognitiveMemoryBehavior _instance;

        // Telemetry counters
        private int _episodicMemoriesRecorded;
        private int _semanticFactsUpdated;
        private int _periodicTicksProcessed;
        private int _decayPassesExecuted;

        /// <summary>Gets the singleton instance of the cognitive memory behavior if active.</summary>
        public static AgentCognitiveMemoryBehavior Instance => _instance;

        /// <summary>Total episodic memories recorded during this campaign session.</summary>
        public int TotalEpisodicMemoriesRecorded => _episodicMemoriesRecorded;

        /// <summary>Total semantic facts updated or set during this campaign session.</summary>
        public int TotalSemanticFactsUpdated => _semanticFactsUpdated;

        /// <summary>Total periodic hourly ticks evaluated during this campaign session.</summary>
        public int TotalPeriodicTicksProcessed => _periodicTicksProcessed;

        /// <summary>Total anti-lag decay passes executed across hero time-slices.</summary>
        public int TotalDecayPassesExecuted => _decayPassesExecuted;

        /// <summary>Total number of distinct cognitive agents tracked in memory.</summary>
        public int ActiveCognitiveAgentsCount => ForgeAgentMemory.RegisteredAgentsCount;

        /// <summary>
        /// Parameterless constructor strictly complying with the Engine Initialization Crash Constraint:
        /// performs zero entity queries and avoids accessing CampaignTime before session launch.
        /// </summary>
        public AgentCognitiveMemoryBehavior()
        {
            _instance = this;
        }

        #region CampaignBehaviorBase Overrides

        /// <summary>
        /// Declaratively registers non-serialized listeners to TaleWorlds CampaignEvents.
        /// Performs zero entity queries or state mutations during registration.
        /// </summary>
        public override void RegisterEvents()
        {
            // Hero Lifecycle & Milestones
            CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, OnHeroComesOfAge);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
            CampaignEvents.HeroRelationChanged.AddNonSerializedListener(this, OnHeroRelationChanged);
            CampaignEvents.HeroGainedSkill.AddNonSerializedListener(this, OnHeroGainedSkill);

            // Periodic Simulation Maintenance & Decay
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        /// <summary>
        /// Strictly stateless: all CoALA cognitive memories reside in volatile memory via
        /// <see cref="ForgeAgentMemory"/> and are reset cleanly across game loads.
        /// </summary>
        public override void SyncData(IDataStore dataStore)
        {
            // Stateless: operates entirely in volatile memory with zero save-data serialization footprint.
        }

        #endregion

        #region Campaign Event Listeners

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Reset session telemetry counters
            Interlocked.Exchange(ref _episodicMemoriesRecorded, 0);
            Interlocked.Exchange(ref _semanticFactsUpdated, 0);
            Interlocked.Exchange(ref _periodicTicksProcessed, 0);
            Interlocked.Exchange(ref _decayPassesExecuted, 0);

            if (starter != null)
            {
                RegisterCognitiveDialogues(starter);
            }
        }

        private void OnHeroComesOfAge(Hero hero)
        {
            if (hero == null || !hero.IsAlive) return;

            string agentId = hero.StringId;
            if (string.IsNullOrEmpty(agentId)) return;

            // Record episodic milestone
            ForgeAgentMemory.Episodic.TryAdd(agentId, "Lifecycle", "Reached adulthood and entered active campaign.");
            Interlocked.Increment(ref _episodicMemoriesRecorded);

            // Set semantic fact
            ForgeAgentMemory.Semantic.TryUpsert(agentId, "IsAdult", true);
            Interlocked.Increment(ref _semanticFactsUpdated);
        }

        private void OnHeroPrisonerTaken(PartyBase capturerParty, Hero prisonerHero)
        {
            if (prisonerHero == null) return;

            string prisonerId = prisonerHero.StringId;
            if (string.IsNullOrEmpty(prisonerId)) return;

            string capturerName = capturerParty?.Name?.ToString() ?? "Unknown Captor";
            string capturerId = capturerParty?.LeaderHero?.StringId ?? capturerParty?.Id ?? "unknown";

            // Record episodic memory for prisoner
            ForgeAgentMemory.Episodic.TryAdd(prisonerId, "Captivity", "Captured by " + capturerName + ".");
            Interlocked.Increment(ref _episodicMemoriesRecorded);

            // Update semantic state for prisoner
            ForgeAgentMemory.Semantic.TryUpsert(prisonerId, "IsImprisoned", true);
            ForgeAgentMemory.Semantic.TryUpsert(prisonerId, "CurrentCaptorId", capturerId);
            Interlocked.Add(ref _semanticFactsUpdated, 2);

            // If capturer has a leader hero, record victory in leader's episodic memory
            Hero capturerLeader = capturerParty?.LeaderHero;
            if (capturerLeader != null && !string.IsNullOrEmpty(capturerLeader.StringId))
            {
                string capturerLeaderId = capturerLeader.StringId;
                string prisonerName = prisonerHero.Name?.ToString() ?? "an enemy hero";
                ForgeAgentMemory.Episodic.TryAdd(capturerLeaderId, "Victory", "Captured " + prisonerName + " in battle.");
                ForgeAgentMemory.Semantic.TryUpsert(capturerLeaderId, "LastCapturedHeroId", prisonerId);
                Interlocked.Increment(ref _episodicMemoriesRecorded);
                Interlocked.Increment(ref _semanticFactsUpdated);
            }
        }

        private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
        {
            if (prisoner == null) return;

            string prisonerId = prisoner.StringId;
            if (string.IsNullOrEmpty(prisonerId)) return;

            ForgeAgentMemory.Episodic.TryAdd(prisonerId, "Liberation", "Released from captivity (" + detail + ").");
            Interlocked.Increment(ref _episodicMemoriesRecorded);

            ForgeAgentMemory.Semantic.TryUpsert(prisonerId, "IsImprisoned", false);
            ForgeAgentMemory.Semantic.TryUpsert(prisonerId, "CurrentCaptorId", "none");
            Interlocked.Add(ref _semanticFactsUpdated, 2);
        }

        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            if (victim == null) return;

            string victimId = victim.StringId;

            // If killer is known hero, record episodic martial memory and update victim's clan grievance
            if (killer != null && !string.IsNullOrEmpty(killer.StringId))
            {
                string killerId = killer.StringId;
                string victimName = victim.Name?.ToString() ?? "an enemy";
                ForgeAgentMemory.Episodic.TryAdd(killerId, "MartialKill", "Slew " + victimName + " in combat (" + detail + ").");
                Interlocked.Increment(ref _episodicMemoriesRecorded);

                int prevKills = ForgeAgentMemory.Semantic.Get<int>(killerId, "TotalSlainHeroes");
                ForgeAgentMemory.Semantic.TryUpsert(killerId, "TotalSlainHeroes", prevKills + 1);
                ForgeAgentMemory.Semantic.TryUpsert(killerId, "LastVictimId", victimId ?? "unknown");
                Interlocked.Add(ref _semanticFactsUpdated, 2);

                // Record grudge in clan leader's memory if victim was clan kin
                Clan victimClan = victim.Clan;
                if (victimClan != null && victimClan.Leader != null && victimClan.Leader != victim && !string.IsNullOrEmpty(victimClan.Leader.StringId))
                {
                    string leaderId = victimClan.Leader.StringId;
                    ForgeAgentMemory.Episodic.TryAdd(leaderId, "BloodFeud", "Kin " + victimName + " was killed by " + (killer.Name?.ToString() ?? "enemy") + ".");
                    ForgeAgentMemory.Semantic.TryUpsert(leaderId, "FeudTargetHeroId", killerId);
                    Interlocked.Increment(ref _episodicMemoriesRecorded);
                    Interlocked.Increment(ref _semanticFactsUpdated);
                }
            }

            // Cleanup deceased hero's volatile memory to free slot
            if (!string.IsNullOrEmpty(victimId))
            {
                ForgeAgentMemory.ClearAgent(victimId);
            }
        }

        private void OnHeroRelationChanged(Hero hero1, Hero hero2, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero affectedRelative1, Hero affectedRelative2)
        {
            if (hero1 == null || hero2 == null) return;

            string id1 = hero1.StringId;
            string id2 = hero2.StringId;
            if (string.IsNullOrEmpty(id1) || string.IsNullOrEmpty(id2)) return;

            string name1 = hero1.Name?.ToString() ?? "Hero";
            string name2 = hero2.Name?.ToString() ?? "Hero";

            // Record episodic relation shifts
            ForgeAgentMemory.Episodic.TryAdd(id1, "DispositionShift", "Relation with " + name2 + " changed by " + relationChange + " (" + detail + ").");
            ForgeAgentMemory.Episodic.TryAdd(id2, "DispositionShift", "Relation with " + name1 + " changed by " + relationChange + " (" + detail + ").");
            Interlocked.Add(ref _episodicMemoriesRecorded, 2);

            // Record affected relatives if present with null safety
            if (affectedRelative1 != null && !string.IsNullOrEmpty(affectedRelative1.StringId))
            {
                ForgeAgentMemory.Episodic.TryAdd(affectedRelative1.StringId, "FamilyDispositionShift", "Family relation between " + name1 + " and " + name2 + " shifted by " + relationChange + " (" + detail + ").");
                Interlocked.Increment(ref _episodicMemoriesRecorded);
            }
            if (affectedRelative2 != null && !string.IsNullOrEmpty(affectedRelative2.StringId))
            {
                ForgeAgentMemory.Episodic.TryAdd(affectedRelative2.StringId, "FamilyDispositionShift", "Family relation between " + name1 + " and " + name2 + " shifted by " + relationChange + " (" + detail + ").");
                Interlocked.Increment(ref _episodicMemoriesRecorded);
            }

            // Update semantic relation facts
            ForgeAgentMemory.Semantic.TryUpsert(id1, "Relation_" + id2, relationChange);
            ForgeAgentMemory.Semantic.TryUpsert(id2, "Relation_" + id1, relationChange);
            Interlocked.Add(ref _semanticFactsUpdated, 2);
        }

        private void OnHeroGainedSkill(Hero hero, SkillObject skill, int changeAmount, bool shouldNotify)
        {
            if (hero == null || skill == null) return;

            string heroId = hero.StringId;
            if (string.IsNullOrEmpty(heroId)) return;

            string skillName = skill.Name?.ToString() ?? skill.StringId ?? "Skill";
            ForgeAgentMemory.Episodic.TryAdd(heroId, "SkillProgression", "Advanced " + skillName + " by +" + changeAmount + ".");
            Interlocked.Increment(ref _episodicMemoriesRecorded);

            ForgeAgentMemory.Semantic.TryUpsert(heroId, "LastMasteredSkill", skill.StringId);
            Interlocked.Increment(ref _semanticFactsUpdated);
        }

        private void OnHourlyTick()
        {
            Interlocked.Increment(ref _periodicTicksProcessed);

            if (Campaign.Current == null) return;

            // Anti-lag time-slicing modulo 24: processes 1/24th of active alive heroes per hour
            int currentHour = (int)CampaignTime.Now.ToHours % 24;
            if (currentHour < 0) currentHour += 24;

            var aliveHeroes = Hero.AllAliveHeroes;
            if (aliveHeroes == null || aliveHeroes.Count == 0) return;

            for (int i = 0; i < aliveHeroes.Count; i++)
            {
                var hero = aliveHeroes[i];
                if (hero == null || !hero.IsAlive) continue;

                // Modulo-24 hash partition without negative values
                if (((hero.Id.GetHashCode() & 0x7FFFFFFF) % 24) == currentHour)
                {
                    string agentId = hero.StringId;
                    if (!string.IsNullOrEmpty(agentId))
                    {
                        // Clean expired TTL facts for this time-sliced hero
                        ForgeAgentMemory.Semantic.GetResult<object>(agentId, "__liveness_check__");
                    }
                }
            }

            Interlocked.Increment(ref _decayPassesExecuted);
        }

        #endregion

        #region Cognitive Dialogue Flows

        private void RegisterCognitiveDialogues(CampaignGameStarter starter)
        {
            if (starter == null) return;

            // 1. Lord Reactive Greetings (start -> lord_start, priorities 115, 112, 110)
            starter.AddDialogLine(
                "forge_lord_blood_feud_greet",
                "start",
                "lord_start",
                "{=forge_dlg_feud_greet}You dare show your face before me? The blood of my kin cries out against you!",
                LordBloodFeudGreetingCondition,
                null,
                115
            );

            starter.AddDialogLine(
                "forge_lord_grateful_greet",
                "start",
                "lord_start",
                "{=forge_dlg_grateful_greet}Greetings, my friend. I have not forgotten the freedom I was granted, and the honor shown to me.",
                LordGratefulLiberationCondition,
                null,
                112
            );

            starter.AddDialogLine(
                "forge_lord_respected_greet",
                "start",
                "lord_start",
                "{=forge_dlg_respected_greet}Well met, warrior. Calradia sings of battles fought and deeds proven. What brings you to me today?",
                LordRespectedRivalCondition,
                null,
                110
            );

            // 2. Lord Universal Cognitive Inquiry (lord_talk_ask_something_2 -> forge_lord_reply_recollection -> lord_talk_ask_something_2)
            starter.AddPlayerLine(
                "forge_lord_ask_recollection",
                "lord_talk_ask_something_2",
                "forge_lord_reply_recollection",
                "{=forge_dlg_ask_history}Do you recall our recent history together?",
                LordMemoryInquiryCondition,
                null,
                110
            );

            starter.AddDialogLine(
                "forge_lord_reply_recollection",
                "forge_lord_reply_recollection",
                "lord_talk_ask_something_2",
                "{=forge_dlg_lord_memory}{FORGE_COGNITIVE_REPLY}",
                LordMemoryReplyCondition,
                null,
                110
            );

            // 3. Settlement Notable Cognitive Inquiry (hero_main_options -> forge_notable_reply_recollection -> hero_main_options)
            starter.AddPlayerLine(
                "forge_notable_ask_recollection",
                "hero_main_options",
                "forge_notable_reply_recollection",
                "{=forge_dlg_notable_ask}How stand our commercial and local affairs in this town?",
                NotableMemoryInquiryCondition,
                null,
                110
            );

            starter.AddDialogLine(
                "forge_notable_reply_recollection",
                "forge_notable_reply_recollection",
                "hero_main_options",
                "{=forge_dlg_notable_memory}{FORGE_COGNITIVE_REPLY}",
                NotableMemoryReplyCondition,
                null,
                110
            );

            // 4. Clan Companion Cognitive Inquiry (hero_main_options -> forge_companion_reply_recollection -> hero_main_options)
            starter.AddPlayerLine(
                "forge_companion_ask_recollection",
                "hero_main_options",
                "forge_companion_reply_recollection",
                "{=forge_dlg_comp_ask}How fares your loyalty and our clan's journey?",
                CompanionMemoryInquiryCondition,
                null,
                110
            );

            starter.AddDialogLine(
                "forge_companion_reply_recollection",
                "forge_companion_reply_recollection",
                "hero_main_options",
                "{=forge_dlg_comp_memory}{FORGE_COGNITIVE_REPLY}",
                CompanionMemoryReplyCondition,
                null,
                110
            );
        }

        private bool LordBloodFeudGreetingCondition()
        {
            Hero conversant = Hero.OneToOneConversationHero;
            Hero player = Hero.MainHero;
            if (conversant == null || player == null || !conversant.IsLord) return false;

            string conversantId = conversant.StringId;
            string playerId = player.StringId;
            if (string.IsNullOrEmpty(conversantId) || string.IsNullOrEmpty(playerId)) return false;

            string feudTarget = ForgeAgentMemory.Semantic.Get<string>(conversantId, "FeudTargetHeroId");
            if (string.Equals(feudTarget, playerId, StringComparison.Ordinal))
            {
                return true;
            }

            var feudEpisodes = ForgeAgentMemory.Episodic.GetAll(conversantId, "BloodFeud");
            if (feudEpisodes != null && feudEpisodes.Count > 0)
            {
                string playerName = player.Name?.ToString();
                for (int i = 0; i < feudEpisodes.Count; i++)
                {
                    string text = feudEpisodes[i] as string;
                    if (text != null && !string.IsNullOrEmpty(playerName) && text.IndexOf(playerName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool LordGratefulLiberationCondition()
        {
            Hero conversant = Hero.OneToOneConversationHero;
            Hero player = Hero.MainHero;
            if (conversant == null || player == null || !conversant.IsLord) return false;

            string conversantId = conversant.StringId;
            if (string.IsNullOrEmpty(conversantId)) return false;

            var libEpisodes = ForgeAgentMemory.Episodic.GetAll(conversantId, "Liberation");
            if (libEpisodes != null && libEpisodes.Count > 0)
            {
                return conversant.GetRelation(player) >= 0;
            }

            return false;
        }

        private bool LordRespectedRivalCondition()
        {
            Hero conversant = Hero.OneToOneConversationHero;
            Hero player = Hero.MainHero;
            if (conversant == null || player == null || !conversant.IsLord) return false;

            string conversantId = conversant.StringId;
            if (string.IsNullOrEmpty(conversantId)) return false;

            int kills = ForgeAgentMemory.Semantic.Get<int>(conversantId, "TotalSlainHeroes");
            int relation = conversant.GetRelation(player);
            return kills > 0 || relation >= 20;
        }

        private bool LordMemoryInquiryCondition()
        {
            Hero conversant = Hero.OneToOneConversationHero;
            return conversant != null && conversant.IsLord;
        }

        private bool LordMemoryReplyCondition()
        {
            Hero conversant = Hero.OneToOneConversationHero;
            Hero player = Hero.MainHero;
            if (conversant == null || player == null) return false;

            string conversantId = conversant.StringId;
            string playerId = player.StringId;
            string reply;

            string feudTarget = ForgeAgentMemory.Semantic.Get<string>(conversantId, "FeudTargetHeroId");
            if (string.Equals(feudTarget, playerId, StringComparison.Ordinal))
            {
                reply = "Every wound and grievance remains burned in my mind. You slew those who shared my blood, and no peace shall erase that debt.";
            }
            else
            {
                string lastCaptured = ForgeAgentMemory.Semantic.Get<string>(conversantId, "LastCapturedHeroId");
                var victories = ForgeAgentMemory.Episodic.GetAll(conversantId, "Victory");
                var martial = ForgeAgentMemory.Episodic.GetAll(conversantId, "MartialKill");

                if (string.Equals(lastCaptured, playerId, StringComparison.Ordinal))
                {
                    reply = "Indeed. I remember holding you at sword's point on the field of honor. A warrior does not forget such captive banners.";
                }
                else if (victories != null && victories.Count > 0)
                {
                    string latestVic = victories[victories.Count - 1] as string ?? "A great triumph";
                    reply = "Aye. I recall the tumult of recent campaigns: " + latestVic + ". Such milestones shape our realm's destiny.";
                }
                else if (martial != null && martial.Count > 0)
                {
                    int totalKills = ForgeAgentMemory.Semantic.Get<int>(conversantId, "TotalSlainHeroes");
                    reply = "My blade has tasted combat in our shared campaigns, with " + totalKills + " champion engagements recorded in our chronicles.";
                }
                else
                {
                    int rel = conversant.GetRelation(player);
                    if (rel >= 15)
                    {
                        reply = "Our paths have crossed with honor and goodwill. I consider you a steadfast ally upon these perilous roads.";
                    }
                    else if (rel <= -15)
                    {
                        reply = "Our encounters have brought suspicion and discord. Tread carefully, for memory does not fade quickly.";
                    }
                    else
                    {
                        reply = "Our shared deeds are few, yet I watch your renown grow across Calradia with keen interest.";
                    }
                }
            }

            MBTextManager.SetTextVariable("FORGE_COGNITIVE_REPLY", new TextObject(reply));
            return true;
        }

        private bool NotableMemoryInquiryCondition()
        {
            Hero conversant = Hero.OneToOneConversationHero;
            return conversant != null && conversant.IsNotable;
        }

        private bool NotableMemoryReplyCondition()
        {
            Hero conversant = Hero.OneToOneConversationHero;
            Hero player = Hero.MainHero;
            if (conversant == null || player == null) return false;

            string conversantId = conversant.StringId;
            string reply;

            var shifts = ForgeAgentMemory.Episodic.GetAll(conversantId, "DispositionShift");
            int rel = conversant.GetRelation(player);

            if (shifts != null && shifts.Count > 0)
            {
                string latestShift = shifts[shifts.Count - 1] as string ?? "Our dealings";
                reply = "Our ledgers record your influence here: " + latestShift + ". Our workshops and trade caravans feel the ripple of your presence.";
            }
            else if (rel > 10)
            {
                reply = "Our coin and trade flow smoothly under your favored eye. The merchants and artisans here speak highly of your justice and patronage.";
            }
            else if (rel < -10)
            {
                reply = "Tension lingers in the alleys and markets. Many here remember past grievances, and our guild masters urge caution when dealing with your party.";
            }
            else
            {
                reply = "The markets turn, caravans arrive, and our ledgers remain balanced. We welcome honest coin and civil conduct in our streets.";
            }

            MBTextManager.SetTextVariable("FORGE_COGNITIVE_REPLY", new TextObject(reply));
            return true;
        }

        private bool CompanionMemoryInquiryCondition()
        {
            Hero conversant = Hero.OneToOneConversationHero;
            return conversant != null && conversant.IsPlayerCompanion;
        }

        private bool CompanionMemoryReplyCondition()
        {
            Hero conversant = Hero.OneToOneConversationHero;
            Hero player = Hero.MainHero;
            if (conversant == null || player == null) return false;

            string conversantId = conversant.StringId;
            string reply;

            var skills = ForgeAgentMemory.Episodic.GetAll(conversantId, "SkillProgression");
            string lastSkill = ForgeAgentMemory.Semantic.Get<string>(conversantId, "LastMasteredSkill");
            var captivity = ForgeAgentMemory.Episodic.GetAll(conversantId, "Captivity");

            if (skills != null && skills.Count > 0)
            {
                string latestSkill = skills[skills.Count - 1] as string ?? "Our martial training";
                reply = "My strength grows each day under your leadership. " + latestSkill + ". My blade and oath belong to our clan!";
            }
            else if (!string.IsNullOrEmpty(lastSkill))
            {
                reply = "I have dedicated myself to mastering our tactics and martial craft. Under your banner, our clan marches ever onward to greatness.";
            }
            else if (captivity != null && captivity.Count > 0)
            {
                reply = "We endured hard times in enemy hands, but we emerged stronger. I stand ready for the next campaign whenever you give the word.";
            }
            else
            {
                reply = "My spirit is high and my gear is ready, captain. Lead on, and the clan shall prosper through every battle ahead.";
            }

            MBTextManager.SetTextVariable("FORGE_COGNITIVE_REPLY", new TextObject(reply));
            return true;
        }

        #endregion
    }
}

