# SDK & GameModels Architecture Codemap — product 25.2.0, ForgeApi.Version 13

## SDK Builder Pattern Architecture

### Builder Hierarchy

```
ForgeNoviceHub (Education System)
├── AddLesson(string id, string content)
├── AddChallenge(string id, string description)
└── Build() → EducationModule

ForgeTroopBuilder (Troop/Character System)
├── SetId(string id)
├── SetCulture(string culture)
├── SetLevel(int level)
├── SetSkills(Dictionary<SkillObject, int> skills)
├── SetEquipment(Equipment equipment)
└── Build() → TroopTemplate

ForgeItemBuilder (Item/Crafting System)
├── SetId(string id)
├── SetName(TextObject name)
├── SetItemType(ItemType type)
├── SetDamage(float damage)
├── SetWeight(float weight)
├── SetCraftingPiece(string pieceId)
└── Build() → ItemTemplate

ForgeQuestBuilder (Quest System)
├── SetId(string id)
├── SetTitle(TextObject title)
├── AddObjective(QuestObjective objective)
├── SetRewards(QuestReward rewards)
├── SetConditions(QuestCondition conditions)
└── Build() → QuestTemplate

ForgeDialogueBuilder (Dialogue System)
├── SetSpeakerId(string speakerId)
├── AddLine(TextObject line)
├── AddCondition(DialogueCondition condition)
├── SetOnComplete(Action onComplete)
└── Build() → DialogueNode

ForgePartySpawner (Party System)
├── SetPosition(Vec2 position)
├── SetFaction(string factionId)
├── AddTroops(TroopRoster troops)
├── SetPartySize(int size)
└── Spawn() → MobileParty

ForgeAudioBuilder (Audio System)
├── SetId(string id)
├── SetPath(string audioPath)
├── SetCategory(SoundCategory category)
├── SetVolume(float volume)
└── Build() → AudioEvent

ForgeHintBuilder (UI Hint System)
├── SetText(TextObject text)
├── SetPosition(Vec2 position)
├── SetDuration(float duration)
├── SetPriority(HintPriority priority)
└── Build() → HintDefinition
```

## SDK Service Architecture

The current product source is 25.2.0 and `ForgeApi.Version` is 13. The current contract includes the optional `ForgeApi.Patches` patch service and optional `ForgeApi.Hooks` runtime-hook service; method replacement was introduced in version 11 and runtime hooks in version 12, with additive Finalizer and hook-kind snapshot metadata in version 13. These additions did not change the shared-library contract introduced in version 7.

### SDK contract v10 — explicit outcomes and connection generations (product source 25.0.0)

`ModSettings.TrySave<T>` returns a typed result and commits a same-directory temporary file before updating the settings cache; serializer or storage failures preserve the last committed file/cache state. `ForgeApi.AutoRegisterWithReport` returns bounded counts and per-type diagnostics while retaining successful registrations when another candidate fails. Semantic and Procedural memory `GetResult<T>` methods distinguish `Found`, `Missing`, and `TypeMismatch`; Semantic additionally distinguishes and cleans up `Expired` entries. Managed `RegisterWhenAvailable` deliveries are checked against the active registry generation and subscriber lifetime, so a callback that reconnects or unregisters during dispatch cannot pass the stale registry to the remainder of that availability snapshot.

### SDK contract v9 and managed ForgeWeave registration (product source 24.0.0)

`ForgeCampaignEvents.SubscribeWeaveWhenAvailable(...)` binds a delegate handler to Forge's availability lifecycle and returns an `IDisposable` `ForgeWeaveRegistration`. Its `State` is `WaitingForHost`, `Registered`, `HostUnsupported`, `Failed` or `Disposed`; `Error` carries the latest failure and `Handler` is the active `IForgeEventHandler` when registered. The helper registers immediately when the host is available, withdraws the handler on disconnect, waits for reconnection, and stops future registrations when disposed by the module owner. Registration callbacks run synchronously on the host connection thread; disposing a pending registration is safe from any thread, while disposing an active one off-thread throws and leaves it registered for a same-thread retry. If `IForgeEventRegistry.Register` throws, the managed handle fails closed and does not auto-register on later connections: the host may have partially added the ID, while `Unregister(IForgeEventHandler)` is ID-based and is not a safe rollback after an uncertain failure. Inspect the host, dispose the failed handle, resolve any leftover ID, then create a new handle. Reentrant disposal suppresses availability callbacks until cleanup finishes, and an older cleanup error does not replace state established by a newer generation. `SubscribeWeave(...)` remains the immediate path and throws when the event registry is unavailable or the call is off-thread; after a host registration exception, inspect the host before retrying.

`ForgeAgentMemory` is a static, thread-safe, process-local store and does not persist to save files. All tiers share a 2,048-distinct-agent-ID ceiling. Semantic memory holds up to 128 facts per agent and alone supports optional TTL. Episodic memory holds up to 512 entries per agent and 128 per type; FIFO removes the oldest agent-wide episode for the total cap and, when necessary, the oldest remaining episode of the incoming type for the per-type cap. Procedural memory holds up to 128 tasks per agent. Semantic and procedural writes can update existing keys at capacity but reject new keys; `TryUpsert`/`TryAdd` report capacity rejection as `false`, while legacy `Upsert`/`Add` throw `InvalidOperationException`. Episodic writes are accepted with FIFO eviction. `ClearAgent` and `ClearAll` release retained values.

### Cross-module shared services (features introduced in SDK contract 7)

`ForgeApi.Libraries` owns the thread-affine `SharedLibraryRegistry`. A module opens one `ModuleLibrary` and owns its registrations and monitors for that module lifetime:

```
ForgeApi.Libraries (ForgeApi.Version = 13)
└── OpenModule(moduleId) → ModuleLibrary
    ├── Provide<T> / Require<T> → one service / fail-fast lookup
    ├── Resolve<T> → immutable SharedServiceResolution<T>
    │   ├── Status: Available, ProviderMissing, ServiceMissing,
    │   │          ContractMismatch, IncompatibleVersion
    │   ├── Diagnostic / IsAvailable / TryGetService(out SharedService<T>)
    │   └── invalid arguments, wrong thread, disposed owner → exception
    ├── Watch<T> → SharedServiceMonitor<T>
    │   ├── Current is the initial snapshot; creation emits no event
    │   ├── Changed(SharedServiceChangedEventArgs<T>) carries Previous/Current
    │   └── LastNotificationError is the latest AggregateException, or null
    └── BeginPublication() → SharedServiceBatch lifetime lease
        ├── Add<T>(serviceId, apiVersion, implementation) stages privately
        ├── Commit() validates/publishes the complete non-empty group atomically
        └── Dispose() discards staging or withdraws the committed group
```

Resolution requires exact CLR interface identity and the existing compatible-version rules. `Require<T>` remains fail-fast. Watches are initialized from a `Current` snapshot and then receive synchronous, isolated notifications on the registry thread after complete state changes. `SharedServiceChangedEventArgs<T>.Previous` and `.Current` are immutable snapshots of the transition and may be stale by the time later handlers run. A mutation made by a handler takes effect synchronously and refreshes monitor snapshots immediately; only callback delivery is queued to prevent recursive dispatch. Read `monitor.Current` or call `Resolve<T>` to observe the latest registry state; a handle may already be invalid after withdrawal. Callback failures are aggregated on that monitor without blocking other handlers. Withdrawal and republication create a new registration generation; there is no in-place replacement. Consumer unload detaches its watches, and global registry disconnect silently disposes watches without a final event. See [`SHARED_LIBRARIES.md`](SHARED_LIBRARIES.md) for provider/consumer lifecycle and packaging details.

### SDK v9 model modifier ownership

`ForgeModelRegistry.BeginOwnerScope(ownerId)` returns a disposable `ForgeModelRegistrationScope`. The scope accepts modifiers only when `modifier.SourceModule` exactly matches its owner ID. Disposing it removes only modifier registrations and generations still owned by that scope; a later legacy or scoped registration that replaced the same ID is preserved. Existing registry-wide `Register` and `Unregister` remain available. `GetModifiers(category)` and `Evaluate(category, ...)` reuse a cached immutable snapshot containing only that category; mutations invalidate the cache, while snapshots already returned remain stable. User-supplied modifier predicates still execute after releasing the registry lock, so they may reenter or mutate the registry without holding up unrelated registry operations.

### Core Services

```
ForgeData (Campaign State Cache)
├── SetValue<T>(string key, T value)
├── GetValue<T>(string key, T defaultValue)
├── ClearAll()
└── Scope: Campaign-level state persistence

ForgeAgentMemory (bounded process-local agent memory; SDK contract v8)
├── Semantic: up to 128 facts / agent; optional per-fact TTL
├── Episodic: up to 512 episodes / agent and 128 / type; FIFO eviction
├── Procedural: up to 128 tasks / agent; no TTL
├── Global union: up to 2,048 agent IDs across all tiers
├── TryUpsert / TryAdd → bool; legacy Upsert / Add throw on rejected capacity writes
└── ClearAgent / ClearAll; no save serialization

ForgeCampaignEvents (Event Bridge)
├── SubscribeWeaveWhenAvailable(...) → ForgeWeaveRegistration : IDisposable
│   ├── State: WaitingForHost / Registered / HostUnsupported / Failed / Disposed
│   ├── host disconnect withdraws; reconnection registers again unless registration failed uncertainly
│   ├── uncertain Register exception → Failed; inspect host and recreate handle after resolution
│   └── Dispose() removes the active handler and availability callback
├── SubscribeWeave(...) → immediate; throws if ForgeApi.Events is unavailable
└── Subscribe / Unsubscribe / ClearSubscribers → legacy callback bridge

ForgeUI (UI Management)
├── RegisterPanel(string panelId, PanelDefinition panel)
├── ShowPanel(string panelId)
├── HidePanel(string panelId)
├── Clear()
└─ Scope: Gauntlet UI coordination

ForgeDetour (Experimental native method replacement)
├── Patch(MethodInfo original, MethodInfo replacement)
├── Unpatch(MethodInfo original)
├── UnpatchAll()
├── GetTrackedSnapshots(string owner = null)
├── Verify(MethodInfo method, out string status)
└─ Scope: one-for-one executable method replacement; no Harmony hook integration
```

`ForgeApi.Patches.ApplyMethodReplacement(string patchId, string owner, MethodInfo target, MethodInfo replacement)` and `ForgePatcher.ApplyAll(Assembly assembly)` are separate explicit application routes. The low-level writer is experimental and does not coordinate other threads executing the target. Blueprint hook kinds remain inert declarations. The optional runtime `ForgeHookService` implements Prefix/Postfix/Finalizer separately; its `net472` Core-only `RegisterTranspiler` adapter uses MonoMod `ILHook`, without adding MonoMod types to the SDK. ForgeDetour does not expose HarmonyMethod overloads. See [runtime hook policies](PATCH_BLUEPRINTS.md#explicit-runtime-hooks-and-il-transpilers) for exception handling, IL-body lifetime, host gates and rebuild limits.

## GameModel Decorator Pattern

### Decorator Architecture

```
Native GameModel (TaleWorlds)
    ↓
Custom Decorator (Inherits Native Model)
    ├─ Constructor injection of base model
    ├─ Null-safe delegation
    ├─ Custom logic layer
    └─ Return modified result
```

### Decorator Template

```csharp
public class CustomXxxModel : NativeXxxModel
{
    private readonly NativeXxxModel _baseModel;
    
    public CustomXxxModel(NativeXxxModel baseModel)
    {
        _baseModel = baseModel;
    }
    
    public override ExplainedNumber CalculateXxx(Args args, bool includeDescriptions = false)
    {
        // 1. Null-safe delegation
        ExplainedNumber result = _baseModel != null
            ? _baseModel.CalculateXxx(args, includeDescriptions)
            : new ExplainedNumber(defaultValue, includeDescriptions);
        
        // 2. Custom logic
        result.Add(5.0f, new TextObject("{=mod_bonus}Custom Bonus"));
        result.AddFactor(0.15f, new TextObject("{=mod_factor}Custom Modifier"));
        result.LimitMin(0.5f);
        
        return result;
    }
}
```

### Registration Pattern

```csharp
// In SubModule.OnGameStart
if (gameStarterObject is CampaignGameStarter campaignStarter)
{
    var existing = campaignStarter.Models
        .OfType<NativeXxxModel>()
        .LastOrDefault();
    campaignStarter.AddModel(new CustomXxxModel(existing));
}
```

## ExplainedNumber Mechanics

### Mathematical Formula

```
FinalResult = (BaseNumber + Σ Adds) × (1.0 + Σ Factors)
```

### Method Effects

| Method | Effect | Notes |
|--------|--------|-------|
| `Add(float val, TextObject desc)` | Direct addition to base | Applied before factors |
| `AddFactor(float factor, TextObject desc)` | Adds to factor sum | Non-compounding: 0.10 + 0.10 = 0.20 (not 0.21) |
| `LimitMin(float min)` | Clamps result ≥ min | Critical for floors |
| `LimitMax(float max)` | Clamps result ≤ max | Caps upper bounds |
| `ResultNumber` | Returns final float value | Evaluation getter |

### Struct Value Semantics

```csharp
// ❌ WRONG - Local copy doesn't affect original
ExplainedNumber result = baseSpeed;
result.Add(5.0f, desc); // Modifies copy, not baseSpeed

// ✅ CORRECT - Work with returned value
ExplainedNumber result = _baseModel.CalculateFinalSpeed(party, baseSpeed);
result.Add(5.0f, desc); // Modifies the result we'll return
return result;
```

### Performance Optimization

```csharp
// Background ticks skip descriptions
bool includeDescriptions = false; // Engine sets this
ExplainedNumber result = CalculateXxx(args, includeDescriptions);

// Only add descriptions when includeDescriptions is true
if (includeDescriptions)
{
    result.Add(5.0f, new TextObject("{=bonus}Bonus"));
}
else
{
    result.Add(5.0f, TextObject.Empty); // Skip string allocation
}
```

## Common GameModel Implementations

### Party Speed Model

```csharp
public class CustomPartySpeedModel : PartySpeedCalculatingModel
{
    private readonly PartySpeedCalculatingModel _baseModel;
    
    public CustomPartySpeedModel(PartySpeedCalculatingModel baseModel)
    {
        _baseModel = baseModel;
    }
    
    public override ExplainedNumber CalculateFinalSpeed(
        MobileParty mobileParty, 
        ExplainedNumber baseSpeed)
    {
        ExplainedNumber result = _baseModel != null
            ? _baseModel.CalculateFinalSpeed(mobileParty, baseSpeed)
            : baseSpeed;
        
        // Custom speed modifier
        if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.GetSkillValue(SkillObject.Cunning) > 200)
        {
            result.AddFactor(0.12f, new TextObject("{=cunning_speed}Cunning Bonus"));
        }
        
        result.LimitMin(0.5f); // Never slower than 0.5
        return result;
    }
}
```

### Party Wage Model

```csharp
public class CustomPartyWageModel : PartyWageModel
{
    private readonly PartyWageModel _baseModel;
    
    public CustomPartyWageModel(PartyWageModel baseModel)
    {
        _baseModel = baseModel;
    }
    
    public override ExplainedNumber GetTotalWage(
        MobileParty mobileParty, 
        bool includeDescriptions = false)
    {
        ExplainedNumber result = _baseModel != null
            ? _baseModel.GetTotalWage(mobileParty, includeDescriptions)
            : new ExplainedNumber(0f, includeDescriptions);
        
        // Garrison discount
        if (mobileParty.IsGarrison)
        {
            result.AddFactor(-0.15f, new TextObject("{=garrison_discount}Garrison Subsidy"));
        }
        
        result.LimitMin(0f); // Never negative wages
        return result;
    }
}
```

### Workshop Model

```csharp
public class CustomWorkshopModel : WorkshopModel
{
    private readonly WorkshopModel _baseModel;
    
    public CustomWorkshopModel(WorkshopModel baseModel)
    {
        _baseModel = baseModel;
    }
    
    public override ExplainedNumber GetWorkshopIncome(
        Workshop workshop, 
        bool includeDescriptions = false)
    {
        ExplainedNumber result = _baseModel != null
            ? _baseModel.GetWorkshopIncome(workshop, includeDescriptions)
            : new ExplainedNumber(0f, includeDescriptions);
        
        // Prosperity bonus
        if (workshop.Town != null && workshop.Town.Prosperity > 5000)
        {
            result.AddFactor(0.10f, new TextObject("{=prosperity_bonus}Prosperity Bonus"));
        }
        
        return result;
    }
}
```

## SDK Integration Flow

### External Tool Integration

```
External Tool
    ↓
ExtensionStartup.Connect(TestEngine)
    ↓
Runtime (ITestServices implementation)
    ↓
ForgeData / ForgeAgentMemory / ForgeUI
    ↓
Bannerlord Game State
```

### Runtime Bridge

```csharp
public class Runtime : ITestServices
{
    public void SetValue(string key, object value)
    {
        ForgeData.SetValue(key, value);
    }
    
    public object GetValue(string key, object defaultValue)
    {
        return ForgeData.GetValue(key, defaultValue);
    }
    
    public bool TryRememberAgentFact(string agentId, string key, object value)
    {
        return ForgeAgentMemory.Semantic.TryUpsert(agentId, key, value);
    }
    
    public bool TryRememberAgentExperience(string agentId, string type, object payload)
    {
        return ForgeAgentMemory.Episodic.TryAdd(agentId, type, payload);
    }

    public void ForgetAgent(string agentId) => ForgeAgentMemory.ClearAgent(agentId);
}
```

## SDK Builder Usage Examples

### Troop Builder

```csharp
var troop = new ForgeTroopBuilder()
    .SetId("custom_infantry_tier_5")
    .SetCulture("empire")
    .SetLevel(25)
    .SetSkills(new Dictionary<SkillObject, int>
    {
        { SkillObject.OneHanded, 150 },
        { SkillObject.TwoHanded, 100 },
        { SkillObject.Athletics, 120 }
    })
    .SetEquipment(customEquipment)
    .Build();
```

### Quest Builder

```csharp
var quest = new ForgeQuestBuilder()
    .SetId("rescue_merchant")
    .SetTitle(new TextObject("{=rescue_merchant_title}Rescue the Merchant"))
    .AddObjective(new QuestObjective(
        "locate_merchant",
        new TextObject("{=locate_merchant}Find the merchant"),
        () => MBObjectManager.Instance.GetObject<Hero>("merchant_abc").CurrentSettlement == Hero.MainHero.CurrentSettlement
    ))
    .SetRewards(new QuestReward(1000, 50))
    .Build();
```

### Dialogue Builder

```csharp
var dialogue = new ForgeDialogueBuilder()
    .SetSpeakerId("merchant_abc")
    .AddLine(new TextObject("{=merchant_greeting}Thank you for rescuing me!"))
    .AddCondition(() => Hero.MainHero.Gold > 100)
    .SetOnComplete(() => 
    {
        Hero.MainHero.Gold -= 100;
        GiveReward();
    })
    .Build();
```

## GameModel Statelessness

### Statelessness Rule

```csharp
// ❌ WRONG - Mutable state in GameModel
public class BadModel : PartySpeedCalculatingModel
{
    private Dictionary<string, float> _partySpeedCache; // State!
    
    public override ExplainedNumber CalculateFinalSpeed(...)
    {
        // Caching state causes incorrect results
    }
}

// ✅ CORRECT - Stateless calculation
public class GoodModel : PartySpeedCalculatingModel
{
    public override ExplainedNumber CalculateFinalSpeed(...)
    {
        // Always calculate from current game state
        // No mutable fields
    }
}
```

### State Persistence Pattern

```csharp
// For state that must persist, use CampaignBehavior
public class SpeedTrackingBehavior : CampaignBehaviorBase
{
    private Dictionary<string, float> _partySpeedHistory;
    
    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_partySpeedHistory", ref _partySpeedHistory);
        if (dataStore.IsLoading)
        {
            _partySpeedHistory ??= new Dictionary<string, float>();
        }
    }
    
    private void OnHourlyTick()
    {
        // Record speeds from stateless GameModel
        foreach (var party in MobileParty.All)
        {
            var speed = Campaign.Current.Models.PartySpeedCalculatingModel
                .CalculateFinalSpeed(party, new ExplainedNumber());
            _partySpeedHistory[party.StringId] = speed.ResultNumber;
        }
    }
}
```

## SDK Extension Points

### Custom Builder Registration

```csharp
// Add custom builder to SDK namespace
namespace CalradiaForge.Sdk
{
    public class ForgeCustomBuilder
    {
        private CustomData _data = new CustomData();
        
        public ForgeCustomBuilder SetCustomProperty(string value)
        {
            _data.CustomProperty = value;
            return this;
        }
        
        public CustomData Build()
        {
            return _data;
        }
    }
}
```

### Custom Service Registration

```csharp
// Add custom service to SDK
namespace CalradiaForge.Sdk
{
    public static class ForgeCustomService
    {
        private static Dictionary<string, CustomData> _customCache = 
            new Dictionary<string, CustomData>();
        
        public static void RegisterCustom(string id, CustomData data)
        {
            _customCache[id] = data;
        }
        
        public static CustomData GetCustom(string id)
        {
            return _customCache.TryGetValue(id, out var data) ? data : null;
        }
        
        public static void ClearAll()
        {
            _customCache.Clear();
        }
    }
}
```

## SDK Error Handling

### Builder Validation

```csharp
public CustomData Build()
{
    if (string.IsNullOrEmpty(_data.Id))
    {
        throw new InvalidOperationException("Builder requires Id to be set");
    }
    
    if (_data.Value < 0)
    {
        throw new InvalidOperationException("Value cannot be negative");
    }
    
    return _data;
}
```

### Service Error Handling

```csharp
public static T GetValue<T>(string key, T defaultValue)
{
    try
    {
        if (_cache.TryGetValue(key, out var value) && value is T typed)
        {
            return typed;
        }
        return defaultValue;
    }
    catch (Exception ex)
    {
        ForgeLogger.PrintError($"Error getting value for key '{key}': {ex.Message}");
        return defaultValue;
    }
}
```

## SDK Testing Patterns

### Builder Unit Test

```csharp
[Test]
public void TestTroopBuilder()
{
    var troop = new ForgeTroopBuilder()
        .SetId("test_troop")
        .SetLevel(10)
        .Build();
    
    Assert.AreEqual("test_troop", troop.Id);
    Assert.AreEqual(10, troop.Level);
}
```

### Service Integration Test

```csharp
[Test]
public void TestForgeData()
{
    ForgeData.SetValue("test_key", 42);
    var result = ForgeData.GetValue<int>("test_key", 0);
    Assert.AreEqual(42, result);
    
    ForgeData.ClearAll();
    var afterClear = ForgeData.GetValue<int>("test_key", 0);
    Assert.AreEqual(0, afterClear);
}
```

## SDK Documentation Standards

### Builder Documentation Template

```csharp
/// <summary>
/// Fluent builder for creating [Target System] objects.
/// 
/// Usage:
/// <code>
/// var result = new ForgeXxxBuilder()
///     .SetProperty(value)
///     .Build();
/// </code>
/// 
/// Validation:
/// - [Required properties]
/// - [Value constraints]
/// 
/// Thread Safety: This builder is not thread-safe. Create a new instance per thread.
/// </summary>
```

### Service Documentation Template

```csharp
/// <summary>
/// Service for managing [Domain] state.
/// 
/// Scope: [Campaign/Mission/Global]
/// Lifecycle: [When cleared/destroyed]
/// Thread Safety: [Thread-safe with locks / Not thread-safe]
/// 
/// Methods:
/// - SetValue: Stores typed value by key
/// - GetValue: Retrieves typed value by key with default
/// - ClearAll: Clears all cached data
/// </summary>
```

## Key SDK & GameModel Rules

1. **GameModel Statelessness**: Never store mutable state in GameModel classes
2. **Decorator Pattern**: Always wrap base models, never replace them
3. **Null-Safe Delegation**: Always check `_baseModel != null` before delegation
4. **ExplainedNumber Struct**: Remember struct value semantics
5. **IncludeDescriptions**: Pass through `includeDescriptions` parameter
6. **Builder Validation**: Validate required properties in Build() method
7. **Service Lifecycle**: Clear services on campaign/mission end
8. **Thread Safety**: Document thread safety requirements
9. **Error Handling**: Graceful fallbacks with logging
10. **Type Safety**: Use generic GetValue<T> with type checking
