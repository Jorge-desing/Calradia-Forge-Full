---
name: bannerlord-quests-dialogues
description: Architectural constraints, lifecycle rules, and dialogue token flows for Mount & Blade II Bannerlord Quests and Conversations.
trigger: always_on
---

# Bannerlord Quests and Dialogues Architecture

When implementing custom hero dialogues, conversation flows, and campaign quests in Bannerlord, you MUST follow these engine lifecycle rules to ensure dialogue stability and prevent save corruption.

## 1. Dialogue Flows & Conversation Tokens (`CampaignGameStarter`)
Conversations are token-driven state machines injected into `CampaignGameStarter` during `CampaignEvents.OnSessionLaunchedEvent`.

### Token Jumping Rules:
- **`AddPlayerLine`**: Adds a dialogue choice selected by the player.
- **`AddDialogLine`**: Adds a dialogue line spoken by the NPC.
- **Token Chaining**: The `outputToken` of the active line MUST match the `inputToken` of the next line.
  - Standard root tokens: `start`, `hero_main_options`, `lord_talk_ask_something_2`, `close_window`.
  - Always end terminal conversation branches with `close_window` or return to `hero_main_options`.
- **Priorities**: Native lines default to priority `100`. Use priority `110` or higher to override vanilla dialogues when specific mod conditions are met.
- **Condition & Consequence Delegates**:
  - `ConversationSentence.OnConditionDelegate`: Must be side-effect free and return a boolean.
  - `ConversationSentence.OnConsequenceDelegate`: Executes game state modifications (giving gold, modifying relations, spawning parties, starting quests).

```csharp
starter.AddPlayerLine("my_mod_ask", "lord_talk_ask_something_2", "my_mod_reply_token", "Is there work to be done?", null, null, 110);
starter.AddDialogLine("my_mod_reply", "my_mod_reply_token", "lord_talk_ask_something_2", "Indeed, my caravan requires an escort.", null, null, 110);
```

## 2. Quest Lifecycle (`QuestBase`)
All campaign quests must inherit from `TaleWorlds.CampaignSystem.QuestBase`.

### Critical Lifecycle Constraints:
- **The Double `SetDialogs()` Rule:** You MUST invoke `SetDialogs()` in BOTH:
  1. The custom quest constructor (when instantiated).
  2. `InitializeQuestOnGameLoad()` (when restoring from a save file).
  *Failing to call `SetDialogs()` inside `InitializeQuestOnGameLoad()` will cause NPCs to become permanently silent or unresponsive after loading a saved game.*
- **Required Overrides:**
  - `public override TextObject Title => new TextObject("{=quest_id}Quest Title");`
  - `public override bool IsRemainingTimeHidden => false;`
  - `protected override void SetDialogs()`
  - `public override void InitializeQuestOnGameLoad()`
  - `protected override void OnStartQuest()`
  - `protected override void OnCompleteQuest()`
  - `protected override void RegisterEvents()`
  - `protected override void HourlyTick()`
- **Journal Entries:** Add quest journal updates via `AddLogEntry(new TextObject("{=log_key}Text"), true)`.

## 3. Save System Safety for Quests
- **`SaveableTypeDefiner`:** All custom `QuestBase` classes and their saveable internal states must be registered in a custom `SaveableTypeDefiner` with a base ID $\ge 2{,}500{,}000$.
- **Field Identifiers:** All persistent quest fields must be decorated with `[SaveableField(id)]`. Never change field IDs between mod versions.
- **Engine Entity References:** Never serialize transient game entities (like dynamic agents or temporary parties) directly into custom dictionaries. Store `Hero.StringId` or `Settlement.StringId` and resolve via `MBObjectManager.Instance.GetObject<Hero>(id)`.

## 4. Anti-Shadowing Constraint (`GEMINI.md`)
- Never name quest folders, namespaces, or sub-classes `Campaign`. Use `CampaignBehaviors`, `Quests`, or `StoryExtensions`.
