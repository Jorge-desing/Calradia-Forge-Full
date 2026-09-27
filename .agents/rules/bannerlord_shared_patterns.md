# Bannerlord Shared Patterns & Architectural Invariants

This rule defines the core architectural patterns and universal safety invariants for all Mount & Blade II: Bannerlord modules in this codebase. Every system modifying game simulation, saving campaign data, or altering engine calculations MUST follow these patterns.

---

## 1. Zero-Harmony Native Decorator Pattern (`GameModel`)

TaleWorlds calculates all simulation values (party speeds, wages, capacities, settlement yields, barter prices, upgrade XP) via `GameModel` classes. Never use Harmony patches to alter simulation numbers.

### Core Implementation Contract:
1. **Inheritance & Constructor Injection:**
   - Inherit directly from the target native model (e.g., `PartySpeedCalculatingModel`, `PartyWageModel`, `WorkshopModel`).
   - Accept the previously registered model in the constructor:
     ```csharp
     public class CustomPartySpeedModel : PartySpeedCalculatingModel
     {
         private readonly PartySpeedCalculatingModel _baseModel;
         public CustomPartySpeedModel(PartySpeedCalculatingModel baseModel)
         {
             _baseModel = baseModel;
         }
     }
     ```
2. **Null-Safe Method Delegation:**
   - Always delegate to `_baseModel` first before modifying results. If `_baseModel` is null, provide a safe native fallback:
     ```csharp
     public override ExplainedNumber CalculateFinalSpeed(MobileParty party, ExplainedNumber baseSpeed)
     {
         ExplainedNumber result = _baseModel != null
             ? _baseModel.CalculateFinalSpeed(party, baseSpeed)
             : baseSpeed;

         // Custom modifiers
         result.AddFactor(0.10f, new TextObject("{=mod_bonus}Vanguard Discipline"));
         result.LimitMin(0.5f);
         return result;
     }
     ```
3. **Registration in SubModule:**
   - Always register in `MBSubModuleBase.OnGameStart` by fetching `campaignStarter.Models.OfType<T>().LastOrDefault()`:
     ```csharp
     var previous = campaignStarter.Models.OfType<PartySpeedCalculatingModel>().LastOrDefault();
     campaignStarter.AddModel(new CustomPartySpeedModel(previous));
     ```
   - This ensures full compatibility with other mods in the load order.

### `ExplainedNumber` Math Mechanics:
- **Struct Value Semantics:** `ExplainedNumber` is a `struct`. Modifications to a local copy do not mutate the original unless reassigned or passed using `ref`.
- **Formula:**
  $$\text{FinalResult} = \left(\text{Base} + \sum \text{Adds}\right) \times \left(1.0 + \sum \text{Factors}\right)$$
- **Non-Compounding Factors:** Calling `.AddFactor(0.10f)` twice results in $+20\%$, not $1.10 \times 1.10 = +21\%$.
- **Always Clamp:** Use `.LimitMin(min)` and `.LimitMax(max)` to prevent negative speeds, division by zero, or pathfinding stalls.
- **Pass Descriptions:** Provide a `TextObject` to `.Add()` and `.AddFactor()` so the Gauntlet UI tooltip reflects the exact calculation breakdown when `includeDescriptions` is active.

---

## 2. CampaignBehavior & Reactive State Architecture (`CampaignBehaviorBase`)

All dynamic campaign simulation logic, event listeners, and save-persistent states belong in `CampaignBehaviorBase`.

### Core Implementation Contract:
1. **Event Registration:**
   - Hook events exclusively inside `RegisterEvents()` using `CampaignEvents.*.AddNonSerializedListener(this, ...)`.
   - **NEVER** call `AddSerializedListener` — serializing delegates into save files causes unrecoverable save corruption when classes or methods are renamed.
2. **State Persistence (`SyncData`):**
   - Bidirectional serialization using `IDataStore`.
   - **Post-Load Null Guards:** Collections and dictionaries must always have defensive null guards:
     ```csharp
     public override void SyncData(IDataStore dataStore)
     {
         dataStore.SyncData("Forge_CustomValues", ref _customValues);
         if (dataStore.IsLoading)
         {
             _customValues ??= new Dictionary<string, int>();
         }
     }
     ```
3. **Safe Entity Identifiers:**
   - **NEVER** serialize native engine handles (`Hero`, `Settlement`, `MobileParty`, `GameEntity`, `Agent`) directly in fields or dictionaries.
   - Store string keys (`hero.StringId`, `settlement.StringId`, `party.StringId`) and resolve them on demand using:
     `MBObjectManager.Instance.GetObject<Hero>(heroId)`
4. **Anti-Lag Time-Slicing (Modulo Pattern):**
   - Prevent frame drops and "Midnight Stutter" by distributing bulk entity updates across 24 hourly ticks:
     ```csharp
     int hour = (int)CampaignTime.Now.ToHours % 24;
     foreach (Settlement s in Settlement.All)
     {
         if ((s.StringId.GetHashCode() & 0x7FFFFFFF) % 24 == hour)
         {
             ProcessHourlySettlement(s);
         }
     }
     ```

---

## 3. Native Save System Contracts (`SaveableTypeDefiner`)

When saving custom classes, structs, or generic collections (`Dictionary<string, int>`, `List<CustomData>`):

1. **Unique Base ID:**
   - Inherit from `TaleWorlds.SaveSystem.SaveableTypeDefiner`.
   - Use a base ID $\ge 2{,}500{,}000$ to prevent collisions with the native engine ($0-100{,}000$) or third-party mods. Calradia Forge reserves the $2{,}750{,}000$ block.
2. **ID Immutability:**
   - Once a class or container definition is assigned an ID (e.g. `AddClassDefinition(typeof(CustomData), 1)`), that numerical ID MUST **never change** between mod versions. Changing it breaks save backward compatibility.
3. **Container Definitions:**
   - Register all generic collections used in `SyncData` inside `DefineContainerDefinitions()` using `ConstructContainerDefinition(typeof(...))`.

---

## 4. Universal Project Invariants

1. **The Anti-Shadowing Rule (`GEMINI.md`):**
   - **CRITICAL:** Never name a folder, sub-namespace, or class `Campaign` anywhere in this project. Doing so shadows `TaleWorlds.CampaignSystem.Campaign` and breaks compilation for properties like `Campaign.Current`. Use `CampaignBehaviors`, `CampaignExtensions`, or `Behaviors`.
2. **Single-Threaded Execution:**
   - Never invoke TaleWorlds Campaign APIs or modify campaign state from background tasks or thread pools (`Task.Run`). Campaign simulation is strictly single-threaded and bound to the main thread.
3. **No Mesh/Skeleton Setup in `OnInit()`:**
   - Complex meshes, scene entities, and skeletons must never be modified during `ScriptComponentBehaviour.OnInit()`. Defer scene setup to the first `OnTick(float dt)` or `OnSessionLaunchedEvent` to prevent seamless C++ engine crashes.
4. **Stateless GameModels:**
   - GameModels are evaluated multiple times per frame. They must remain 100% stateless. Never store hero or settlement data in fields of a `GameModel`.
