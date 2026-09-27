---
name: bannerlord-shared-patterns
description: Foundational boilerplate patterns shared across all Bannerlord modding skills — Decorator Pattern for GameModels, SaveableTypeDefiner, and CampaignBehaviorBase skeleton. Reference this skill before implementing any mod system.
---

# Bannerlord Shared Modding Patterns

> **All Bannerlord skills in this workspace assume these patterns are known.**
> Start C# work with [calradia-forge-dotnet](../calradia-forge-dotnet/SKILL.md), then read [bannerlord-dotnet-artisan](../bannerlord-dotnet-artisan/SKILL.md) and this skill. The local using-dotnet file is a preserved backup snapshot, not a required step. Each specialized skill only shows what is *unique* to its domain.

---

## Pattern 1 — Decorator Pattern for GameModels (Zero Harmony)

The Decorator wraps any native or third-party `GameModel` instance, delegating to it safely and layering custom logic on top. This is the only supported way to modify simulation numbers without Harmony.

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
            // Anti-lag time-slicing: process 1/24th of entities per hourly tick
            int bucket = (int)CampaignTime.Now.ToHours % 24;
            foreach (Settlement s in Settlement.All)
            {
                if (!s.IsTown) continue;
                if ((s.StringId.GetHashCode() & 0x7FFFFFFF) % 24 != bucket) continue;

                // Process this settlement's scheduled hourly cycle
            }
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
- **Zero GC Allocations in Ticks:** Avoid LINQ (`.Where()`, `.Select()`, `.ToList()`) and lambda closures inside `HourlyTick` or `DailyTickParty` to prevent GC pauses.
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

## Universal Safety Rules

1. **Never name a namespace, folder, or class `Campaign`** — it shadows `TaleWorlds.CampaignSystem.Campaign` and breaks compilation for `Campaign.Current.*` (`GEMINI.md`).
2. **Never use Harmony** for this mod — all game model adjustments use the Decorator Pattern above.
3. **Never serialize transient engine handles** (`GameEntity`, `Agent`, `PartyVisual`) in `SyncData`.
4. **Never store raw Hero/Settlement instances** in persistent state — use `StringId`.
5. **All `AddNonSerializedListener` calls go in `RegisterEvents()`** — never in constructors or `OnSessionLaunched`.
