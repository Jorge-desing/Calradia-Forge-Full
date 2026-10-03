---
name: bannerlord-quest-system
description: Design, implementation, and lifecycle management for custom Mount & Blade II Bannerlord Quests and Hero Dialogues.
---

# Bannerlord Quest & Dialogue System

This skill covers the complete lifecycle, dialogue graph construction, and save-safe persistence for custom quests in *Mount & Blade II: Bannerlord*.

---

## 1. Core Architecture

Custom quests in Bannerlord consist of three interrelated parts:
1. **The Quest Class (`QuestBase`)**: Controls quest logic, objective tracking, timeout/failure conditions, and journal logs.
2. **The Dialogue Flow (`DialogFlow` / `CampaignGameStarter`)**: Offers the quest through conversation with a Hero or Notable.
3. **The Trigger Behavior (`CampaignBehaviorBase`)**: Determines when and where the quest becomes available in the campaign simulation.

---

## 2. Complete C# Quest Implementation Pattern

```csharp
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace MyMod.QuestBehaviors
{
    public class CustomEscortQuest : QuestBase
    {
        [SaveableField(1)]
        private Settlement _targetSettlement;

        [SaveableField(2)]
        private int _rewardGold;

        [SaveableField(3)]
        private bool _hasEncounteredBandits;

        public override TextObject Title => new TextObject("{=my_quest_title}Caravan Escort to {SETTLEMENT}")
            .SetTextVariable("SETTLEMENT", _targetSettlement?.Name ?? TextObject.Empty);

        public override bool IsRemainingTimeHidden => false;

        public CustomEscortQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold, Settlement targetSettlement)
            : base(questId, questGiver, duration, rewardGold)
        {
            _targetSettlement = targetSettlement;
            _rewardGold = rewardGold;
            _hasEncounteredBandits = false;

            // CRITICAL: Must initialize dialogs on instantiation
            SetDialogs();
            InitializeQuestOnCreation();
        }

        // CRITICAL ENGINE RULE: Must re-hook dialogs when loading a save file!
        public override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
        }

        protected override void SetDialogs()
        {
            OfferDialogFlow = DialogFlow.CreateDialogFlow("my_quest_offer_start", 120)
                .NpcLine("{=my_quest_offer_line}I have valuable goods heading to {TARGET}. The roads are crawling with raiders.")
                    .Condition(() => Hero.OneToOneConversationHero == QuestGiver)
                .PlayerLine("{=my_quest_accept}I will protect your caravan for {REWARD} denars.")
                    .Condition(() => true)
                    .Consequence(() => StartQuest())
                    .CloseDialog()
                .PlayerLine("{=my_quest_decline}I have other matters to attend to.")
                    .CloseDialog();

            DiscussDialogFlow = DialogFlow.CreateDialogFlow("my_quest_discuss", 120)
                .NpcLine("{=my_quest_discuss_line}Any progress on our journey to {TARGET}?")
                    .Condition(() => Hero.OneToOneConversationHero == QuestGiver)
                .PlayerLine("{=my_quest_discuss_reply}We are making steady progress.")
                    .CloseDialog();
        }

        protected override void OnStartQuest()
        {
            SetDialogs();
            AddLogEntry(new TextObject("{=my_quest_log_start}Agreed to escort the caravan from {ORIGIN} to {TARGET}.")
                .SetTextVariable("ORIGIN", QuestGiver.CurrentSettlement?.Name ?? TextObject.Empty)
                .SetTextVariable("TARGET", _targetSettlement.Name), true);
        }

        protected override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party == MobileParty.MainParty && settlement == _targetSettlement)
            {
                CompleteQuestWithSuccess();
            }
        }

        private void OnHourlyTickParty(MobileParty party)
        {
            if (party == MobileParty.MainParty && !_hasEncounteredBandits)
            {
                // Ambush logic or situational updates
            }
        }

        protected override void HourlyTick()
        {
            // Engine handles remaining quest duration automatically
        }

        protected override void OnCompleteQuest()
        {
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
            ChangeRelationAction.ApplyPlayerRelation(QuestGiver, 5);
            AddLogEntry(new TextObject("{=my_quest_success}The caravan reached {TARGET} safely.")
                .SetTextVariable("TARGET", _targetSettlement.Name), true);
        }

        protected override void OnCancelQuest()
        {
            AddLogEntry(new TextObject("{=my_quest_cancelled}The escort contract was cancelled."), true);
        }
    }
}
```

---

## 3. Registering Quests via Campaign Behavior

To spawn quests dynamically, create an accompanying `CampaignBehaviorBase`:

```csharp
namespace MyMod.QuestBehaviors
{
    public class CustomQuestManagerBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Register standalone dialogues or quest entry point hooks here
        }

        private void DailyTickSettlement(Settlement settlement)
        {
            if (!settlement.IsTown || settlement.OwnerClan?.Leader == null) return;

            Hero notable = settlement.Notables.GetRandomElement();
            if (notable != null && notable.CanHaveQuestsOrIssues() && !notable.HasIssue)
            {
                // Instantiate and start quest if criteria are met
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Sync behavior flags if any (QuestBase serializes its own state)
        }
    }
}
```

---

## 4. Save System Registration (`SaveableTypeDefiner`)

Every custom `QuestBase` subclass MUST register its internal types:

```csharp
namespace MyMod.QuestBehaviors
{
    public class QuestSaveTypeDefiner : SaveableTypeDefiner
    {
        // Unique ID >= 2,500,000 to prevent collisions
        public QuestSaveTypeDefiner() : base(2_750_000) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(CustomEscortQuest), 1);
        }
    }
}
```

---

## 5. Golden Rules for Quests
> **Universal rules** are in `bannerlord-shared-patterns`: do not create a `Campaign` namespace, add a Harmony dependency, use Harmony patches/detours to implement this feature, or serialize engine entities. The only Forge-side Harmony path is the optional read-only observer described there; it does not load or modify Harmony.

1. **Never omit `SetDialogs()` from `InitializeQuestOnGameLoad()`**: This is the #1 cause of broken quest conversations on loaded saves.
2. **Never store raw `Agent` references in quest fields**: Agents are transient 3D entities destroyed when scenes end. Always store `Hero` or `Hero.StringId`.
3. **Use unique text localization hashes**: e.g., `{=mod_quest_title_001}Title`.
