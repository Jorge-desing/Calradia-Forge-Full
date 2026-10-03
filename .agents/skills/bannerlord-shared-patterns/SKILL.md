---
name: bannerlord-shared-patterns
description: Foundational boilerplate patterns shared across all Bannerlord modding skills — Decorator Pattern for GameModels, SaveableTypeDefiner, and CampaignBehaviorBase skeleton. Reference this skill before implementing any mod system.
---

# Bannerlord Shared Modding Patterns

> **All Bannerlord skills in this workspace assume these patterns are known.**
> Start C# work with [calradia-forge-dotnet](../calradia-forge-dotnet/SKILL.md), then read [bannerlord-dotnet-artisan](../bannerlord-dotnet-artisan/SKILL.md) and this skill. The local using-dotnet file is a preserved backup snapshot, not a required step. Each specialized skill only shows what is *unique* to its domain.

---

## Pattern 1 — Decorator Pattern for GameModels (Preferred for simulation)

The Decorator wraps any native or third-party `GameModel` instance, delegating to it safely and layering custom logic on top. Use this pattern to modify campaign simulation numbers. Calradia Forge also has a separate experimental Prefix/Postfix/Finalizer RuntimeDetour capability; it is not Harmony and is not the supported simulation extension path. Its callbacks run synchronously only while the exact host game-thread main-menu gate is true; dispatch checks again between Prefix, the original target, and Postfix. If the gate closes before the target call, Forge discards Prefix argument edits and calls the original target unchanged; if it closes during the target, Postfix is skipped. The detour stays installed until explicitly reverted. Finalizer also requires the callback gate. The separate Core `net472` ILHook adapter runs its manipulator at Apply and later chain rebuilds. After verified explicit Apply, that exact owned activation may be invoked again during an external chain rebuild on another caller's thread, after the host gate closes or callbacks stop, while ownership remains exact and Undo is not uncertain. This is chain reconstruction, not new hook management; the transformed method body remains active outside the menu until Undo/Revert and has no per-call callback gate. SDK API 13 keeps MonoMod types outside SDK contracts. See [calradia-forge-modding](../calradia-forge-modding/SKILL.md) for explicit-apply, disposal, and lifecycle recovery limits.

### Template

```csharp
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.Core.GameModels
{
    // Replace <NativeModel> with the specific model class you are extending
    public class ForgeXxxModel : NativeModel
    {
        private readonly NativeModel _baseModel;

        public ForgeXxxModel(NativeModel baseModel)
        {
            _baseModel = baseModel;
        }

        // 1. Null-safe delegation pattern for ExplainedNumber formulas
        public override ExplainedNumber SomeFormula(SomeArgs args, bool includeDescriptions = false)
        {
            ExplainedNumber result = _baseModel != null
                ? _baseModel.SomeFormula(args, includeDescriptions)
                : new ExplainedNumber(defaultValue, includeDescriptions);

            // ---- YOUR MOD LOGIC HERE ----
            result.Add(5.0f, new TextObject("{=mod_bonus}Forge Baseline Bonus"));
            result.AddFactor(0.15f, new TextObject("{=mod_factor}Vanguard Modifier"));
            result.LimitMin(0.5f);

            return result;
        }

        // 2. Safe delegation pattern for methods returning primitives (int, float, bool)
        public override int GetItemPrice(EquipmentElement item, MobileParty party)
        {
            int basePrice = _baseModel?.GetItemPrice(item, party) ?? 100;
            return (int)(basePrice * 0.90f); // 10% discount
        }
    }
}
```

### Registration in SubModule

```csharp
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.Core
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            if (gameStarterObject is CampaignGameStarter campaignStarter)
            {
                // Always use .LastOrDefault() to pick up any previously registered decorators in the load chain
                var existing = campaignStarter.Models.OfType<NativeModel>().LastOrDefault();
                campaignStarter.AddModel(new ForgeXxxModel(existing));

                // Also register behaviors:
                campaignStarter.AddBehavior(new ForgeXxxBehavior());
            }
        }
    }
}
```

### Deep Dive: `ExplainedNumber` Math & Struct Semantics

$$\text{FinalResult} = \left(\text{BaseNumber} + \sum \text{Adds}\right) \times \left(1.0 + \sum \text{Factors}\right)$$

| Method | Mathematical Effect | Notes |
|:---|:---|:---|
| `Add(float val, TextObject desc)` | Directly adds `val` to the additive sum | Applied before factors |
| `AddFactor(float factor, TextObject desc)` | Adds `factor` to $\sum \text{Factors}$ | **Non-compounding**: Two $+0.10$ factors yield $+20\%$ ($1.20$), NOT $1.21$ |
| `LimitMin(float min)` | Clamps evaluated result $\ge \text{min}$ | Critical for speed & wage floors |
| `LimitMax(float max)` | Clamps evaluated result $\le \text{max}$ | Caps upper bounds |
| `ResultNumber` | Evaluates and returns the `float` result | Final value getter |

> **Struct Value Semantics:** `ExplainedNumber` is a **struct**. Assigning it to a local variable creates a value copy. Mutating a local variable does NOT mutate the original unless reassigned or passed using `ref`.
>
> **The `includeDescriptions` Performance Gate:** Background simulation ticks run with `includeDescriptions = false` to skip string allocations. Only pass `TextObject` descriptions when adding modifiers; the engine internally ignores descriptions when formatting is turned off.
>
> **Stateless Requirement:** GameModels are queried hundreds of times per second. Never store mutable game state in fields of a `GameModel`; persist state in `CampaignBehaviorBase.SyncData` instead.

---

## Pattern 2 — CampaignBehaviorBase Skeleton

All reactive simulation logic, event subscriptions, and save-persistent state belong here.

```csharp
using System.Collections.Generic;
using CalradiaForge.Sdk;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace CalradiaForge.Core.CampaignBehaviors
{
    public class ForgeXxxBehavior : CampaignBehaviorBase
    {
        // ---- STATE — store primitive types or StringId-keyed dictionaries ONLY ----
        private Dictionary<string, int> _heroData = new Dictionary<string, int>();
        private int _tickCount;

        public override void RegisterEvents()
        {
            // Use AddNonSerializedListener exclusively — NEVER AddSerializedListener
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Forge_Xxx_HeroData", ref _heroData);
            dataStore.SyncData("Forge_Xxx_TickCount", ref _tickCount);

            // Null-safe defensive guard on load
            if (dataStore.IsLoading)
            {
                _heroData ??= new Dictionary<string, int>();
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Register dialogues and game menus here
        }

        private void OnHourlyTick()
        {
            // Use only when this work may safely be deferred to another hour.
            int currentHour = (int)CampaignTime.Now.ToHours;
            foreach (Settlement s in Settlement.All)
            {
                if (!s.IsTown) continue;
                if (!ForgeTimeSlicer.ShouldProcess(s.StringId, currentHour)) continue;

                // Process this settlement's scheduled hourly cycle
            }
            // Filtering still scans Settlement.All; measure traversal and processing separately.
        }

        private void OnDailyTickSettlement(Settlement settlement)
        {
            if (settlement == null || !settlement.IsTown) return;
        }
    }
}
```

### Safe Entity Reference Rules
- **Never serialize** `Hero`, `Settlement`, `MobileParty`, or `GameEntity` directly in behavior fields.
- Store `hero.StringId` and resolve via `MBObjectManager.Instance.GetObject<Hero>(id)`.
- **Measured Tick Costs:** Measure duration and allocations across the complete callback before setting a budget or claiming a GC pause. Avoid repeated materialization or capturing callbacks in a hot path when measurements show a cost; syntax alone does not prove an allocation or frame-time regression.
- **Single-Threaded Main Execution:** All campaign engine calls must execute on the main game thread.

---

## Pattern 3 — SaveableTypeDefiner

Required for any custom class or generic collection (`Dictionary<string, int>`, `List<string>`) persisting across save/load cycles.

```csharp
using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace CalradiaForge.Core.CampaignBehaviors
{
    public class ForgeSaveTypeDefiner : SaveableTypeDefiner
    {
        // Base ID >= 2,500,000 avoids collisions with:
        //   - Native engine (0–100,000)
        //   - Other mods (arbitrary ranges)
        // Calradia Forge reserves the 2,750,000 block.
        public ForgeSaveTypeDefiner() : base(2_750_000) { }

        protected override void DefineClassTypes()
        {
            // Register each custom saveable class with an immutable local ID (1, 2, 3...)
            // AddClassDefinition(typeof(MyCustomRecord), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            // Register generic container types used in SyncData
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
            ConstructContainerDefinition(typeof(List<string>));
        }
    }
}
```

> **CRITICAL IMMUTABILITY RULE:** Once a class or container definition is registered with an ID, that ID must **never change** between mod versions. Modifying IDs breaks save backward compatibility.

---

## Pattern 4 — Safe Developer Console Command Handlers (`CommandLineFunctionality`)

Custom developer console commands provide live debugging and tooling interfaces inside Bannerlord. They must defensively guard against empty and null arguments passed by the engine.

### Template

```csharp
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace CalradiaForge.Mod
{
    public static class ForgeConsoleCommands
    {
        [CommandLineFunctionality.CommandLineArgumentAttribute("inspect_hero", "calradiaforge")]
        public static string InspectHero(List<string> args)
        {
            // 1. Mandatory null and argument count check
            if (args == null || args.Count < 1)
            {
                return "Usage: calradiaforge.inspect_hero <hero_string_id> [optional_verbosity_level]";
            }

            string heroId = args[0];
            int verbosity = 1;

            // 2. Defensive parsing of optional parameters
            if (args.Count > 1 && !int.TryParse(args[1], out verbosity))
            {
                return $"Error: Invalid integer verbosity '{args[1]}'. Expected 1, 2, or 3.";
            }

            // 3. Game state verification
            if (Campaign.Current == null)
            {
                return "Error: Campaign is not currently active.";
            }

            Hero target = MBObjectManager.Instance.GetObject<Hero>(heroId);
            if (target == null)
            {
                return $"Error: Hero with ID '{heroId}' not found.";
            }

            return $"Hero '{target.Name}' (Level {target.Level}, Clan: {target.Clan?.Name?.ToString() ?? "None"}). Verbosity={verbosity}.";
        }
    }
}
```

---

## Pattern 5 — CoALA Semantic & Episodic Memory Facts (`ForgeAgentMemory`)

When recording observations, relational scores, and state changes for AI decision-making or narrative agents via `ForgeAgentMemory`:

### Template

```csharp
using TaleWorlds.CampaignSystem;
using CalradiaForge.Sdk;

namespace CalradiaForge.Core.CampaignBehaviors
{
    public class ForgeAgentMemorySync
    {
        public static void RecordHeroInteraction(ForgeAgentMemory memory, Hero observer, Hero target, int relationDelta)
        {
            if (memory == null || observer == null || target == null) return;

            // 1. Semantic Memory: ALWAYS store the real absolute relation from the engine
            int absoluteRelation = observer.GetRelation(target);
            memory.Semantic.SetFact($"Relation_{target.StringId}", absoluteRelation, importance: 0.8f);

            // 2. Episodic / Delta Memory: Store the transient delta separately for explainability
            memory.Semantic.SetFact($"LastRelationDelta_{target.StringId}", relationDelta, importance: 0.4f);

            // 3. Episodic Log: Register the event in short-term narrative memory
            memory.Episodic.RecordEvent(
                category: "Diplomacy",
                summary: $"Relation with {target.Name} changed by {relationDelta:+#;-#;0} (New absolute: {absoluteRelation})",
                importance: Math.Abs(relationDelta) >= 10 ? 0.9f : 0.5f
            );
        }
    }
}
```

### Cognitive Invariants:
- **No Delta Shadowing:** Never store `relationDelta` as the value for `Relation_<targetId>`. Decisions require absolute affinity $[-100, 100]$.
- **Stateless Persistence Rule:** In `src/CalradiaForge.Mod`, keep agent memory transient and rebuild it on-demand from world queries. Never register memory containers in `SaveableTypeDefiner` within the game module.

---

## Universal Safety Rules

1. **Never name a namespace, folder, or class `Campaign`** — it shadows `TaleWorlds.CampaignSystem.Campaign` and breaks compilation for `Campaign.Current.*` (`GEMINI.md`).
2. **Do not depend on Harmony.** Never add `0Harmony` as a compile-time/runtime dependency or distribute it. Use the Decorator Pattern above for simulation changes. Forge's optional patch observer may use reflection only against the exact public query surface of an already-loaded `0Harmony` assembly; it must not load Harmony or modify external patches. This is bounded diagnostic evidence, not a coexistence guarantee or a sandbox. The separate experimental runtime hook capability is not Harmony, is explicitly applied, and must not be treated as an alternative `GameModel` extension path.
3. **Never serialize transient engine handles** (`GameEntity`, `Agent`, `PartyVisual`) in `SyncData`.
4. **Never store raw Hero/Settlement instances** in persistent state — use `StringId`.
5. **All `AddNonSerializedListener` calls go in `RegisterEvents()`** — never in constructors or `OnSessionLaunched`.
