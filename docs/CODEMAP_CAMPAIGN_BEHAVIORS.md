# Campaign Behavior Event System Codemap

## CampaignBehaviorBase Lifecycle

```
Instantiation (OnGameStart)
    ↓
RegisterEvents() — Event Subscription Phase
    ↓
SyncData(IDataStore) — Save/Load Hydration
    ↓
Session Activation (OnSessionLaunchedEvent)
    ↓
Event Handlers Execute (Throughout Campaign)
    ↓
Teardown (Session Exit/Campaign Destruction)
```

## Event Registration Architecture

### Registration Pattern

```csharp
public override void RegisterEvents()
{
    // EXCLUSIVELY use AddNonSerializedListener
    CampaignEvents.EventName.AddNonSerializedListener(this, OnEventHandler);
}
```

### Event Categories in ClanCharacterProgressionBehavior

#### 1. Hero Lifecycle Events (13 events)
```
HeroCreated
HeroGrowsOutOfInfancyEvent
HeroReachesTeenAgeEvent
HeroComesOfAgeEvent
BeforeHeroKilledEvent
HeroKilledEvent
HeroWounded
HeroOccupationChangedEvent
HeroRelationChanged
OnHeroChangedClanEvent
HeroPrisonerTaken
HeroPrisonerReleased
OnHeroActivatedEvent
OnHeroGetsBusyEvent
```

#### 2. Clan & Dynastic Succession Events (10 events)
```
OnClanCreatedEvent
OnClanDestroyedEvent
ClanTierIncrease
OnClanLeaderChangedEvent
OnHeirSelectionRequestedEvent
OnHeirSelectionOverEvent
OnPlayerCharacterChangedEvent
OnClanChangedKingdomEvent
OnClanDefectedEvent
RulingClanChanged
OnClanInfluenceChangedEvent
```

#### 3. Companions & Parties Events (5 events)
```
NewCompanionAdded
CompanionRemoved
OnHeroJoinedPartyEvent
OnPartyLeaderChangedEvent
OnGovernorChangedEvent
```

#### 4. Marriage & Pregnancy Events (5 events)
```
OnMarriageOfferedToPlayerEvent
OnMarriageOfferCanceledEvent
BeforeHeroesMarried
RomanticStateChanged
OnGivenBirthEvent
```

#### 5. Character Progression Events (5 events)
```
HeroGainedSkill
HeroLevelledUp
PerkOpenedEvent
PerkResetEvent
PlayerTraitChangedEvent
RenownGained
```

#### 6. Periodic Simulation Ticks (6 events)
```
DailyTickHeroEvent
DailyTickClanEvent
HourlyTickPartyEvent
HourlyTickEvent
DailyTickEvent
WeeklyTickEvent
```

## Event Handler Pattern

### Standard Handler Structure

```csharp
private void OnEventName(Entity entity, ...)
{
    // 1. Null Safety Check
    if (entity == null) return;
    
    // 2. State Validation
    if (!entity.IsActive || !entity.IsAlive) return;
    
    // 3. Telemetry Increment
    Interlocked.Increment(ref _eventsProcessed);
    
    // 4. Domain Logic (Stateless)
    // ... operate on live vanilla state ...
}
```

### Example: Hero Lifecycle Handler

```csharp
private void OnHeroCreated(Hero hero, bool isBornNaturally)
{
    if (hero == null) return;
    Interlocked.Increment(ref _lifeCycleEventsProcessed);
    
    // Guard against unassigned clan
    if (hero.Clan != null && hero.Clan.IsNoble)
    {
        if (isBornNaturally && (hero.Father != null || hero.Mother != null))
        {
            // Stateless pedigree evaluation
        }
    }
}
```

## Time-Slicing Architecture

### Modulo-24 Hash Distribution

```csharp
public static bool ShouldProcessInCurrentHour(string stringId)
{
    if (string.IsNullOrEmpty(stringId)) return false;
    int entityHash = stringId.GetHashCode() & 0x7FFFFFFF;
    int currentHour = (int)CampaignTime.Now.ToHours % 24;
    return (entityHash % 24) == currentHour;
}
```

### Application in Tick Handlers

```csharp
private void OnDailyTickHero(Hero hero)
{
    if (hero == null || !hero.IsActive) return;
    
    // Only process this hero if it falls in current hour bucket
    if (!ShouldProcessInCurrentHour(hero.StringId)) return;
    
    // Process daily logic for this hero
    Interlocked.Increment(ref _periodicTicksProcessed);
}
```

### Performance Benefits

- **Even Distribution**: Entities spread across 24 hourly buckets
- **Frame Budget**: Each tick processes ~1/24th of entities
- **Deterministic**: Same entity always processes in same hour
- **Stutter Prevention**: Eliminates "Midnight Freeze"

## State Synchronization Architecture

### SyncData Pattern (Stateless Approach)

```csharp
public override void SyncData(IDataStore dataStore)
{
    // EMPTY - Stateless behavior derives all data from live vanilla state
}
```

### SyncData Pattern (Legacy Data Approach)

```csharp
public override void SyncData(IDataStore dataStore)
{
    // 1. Primitive synchronization
    dataStore.SyncData("_internalCounter", ref _internalCounter);
    
    // 2. Collection synchronization
    dataStore.SyncData("_heroScores", ref _heroScores);
    
    // 3. Defensive post-load null guards
    if (dataStore.IsLoading)
    {
        _heroScores ??= new Dictionary<string, int>();
    }
}
```

### SaveableTypeDefiner Pattern

```csharp
public class ForgeSaveTypeDefiner : SaveableTypeDefiner
{
    // Base ID >= 2,500,000 to avoid native collisions
    public ForgeSaveTypeDefiner() : base(2_750_000) { }
    
    protected override void DefineClassTypes()
    {
        AddClassDefinition(typeof(MyCustomRecord), 1);
    }
    
    protected override void DefineContainerDefinitions()
    {
        ConstructContainerDefinition(typeof(Dictionary<string, int>));
        ConstructContainerDefinition(typeof(List<string>));
    }
}
```

## Entity Reference Safety

### Forbidden Pattern (Never Serialize Direct References)

```csharp
// ❌ WRONG - Causes save corruption on entity destruction
private Hero _trackedHero;
private Settlement _homeSettlement;
```

### Correct Pattern (StringId Storage)

```csharp
// ✅ CORRECT - Safe across entity lifecycle
private string _trackedHeroId;
private string _homeSettlementId;

// Resolution on demand
Hero hero = MBObjectManager.Instance.GetObject<Hero>(_trackedHeroId);
Settlement settlement = MBObjectManager.Instance.GetObject<Settlement>(_homeSettlementId);
```

## Anti-Lag Coding Patterns

### Pattern 1: Avoid LINQ in Ticks

```csharp
// ❌ WRONG - GC allocations in tick
var activeHeroes = Hero.AllAliveHeroes.Where(h => h.IsActive).ToList();

// ✅ CORRECT - Manual iteration
foreach (var hero in Hero.AllAliveHeroes)
{
    if (!hero.IsActive) continue;
    // Process hero
}
```

### Pattern 2: Reuse Collections

```csharp
// ✅ CORRECT - Reuse scratch collection
private static readonly List<Hero> _scratchList = new List<Hero>();

private void OnHourlyTick()
{
    _scratchList.Clear();
    foreach (var hero in Hero.AllAliveHeroes)
    {
        if (ShouldProcess(hero)) _scratchList.Add(hero);
    }
    // Process _scratchList
}
```

### Pattern 3: Squared Distance

```csharp
// ❌ WRONG - Expensive sqrt call
float distance = Vec2.Distance(pos1, pos2);
if (distance < 100f) { }

// ✅ CORRECT - Squared comparison
float distanceSquared = Vec2.DistanceSquared(pos1, pos2);
if (distanceSquared < 10000f) { } // 100^2
```

## Event Handler Defensive Guards

### Common Guard Patterns

```csharp
// 1. Null Entity Guard
if (entity == null) return;

// 2. Activity Guard
if (!entity.IsActive) return;

// 3. Life State Guard
if (!entity.IsAlive) return;

// 4. Clan Guard
if (entity.Clan == null) return;

// 5. Player Guard
if (entity != Hero.MainHero) return;

// 6. Settlement Guard
if (settlement == null || !settlement.IsTown) return;

// 7. Collection Guard
if (heroes == null || heroes.Count == 0) return;

// 8. Developer Guard
if (hero.HeroDeveloper == null) return;
```

## Telemetry Architecture

### Counter Pattern

```csharp
private int _lifeCycleEventsProcessed;
private int _clanEventsProcessed;
private int _progressionEventsProcessed;

// Thread-safe increment
Interlocked.Increment(ref _lifeCycleEventsProcessed);

// Public read-only access
public int TotalLifeCycleEventsProcessed => _lifeCycleEventsProcessed;
```

### Stateless Metrics

```csharp
// Derived from live vanilla state
public int ActiveTrackedHeroesCount => 
    Hero.AllAliveHeroes != null ? Hero.AllAliveHeroes.Count : 0;
```

## Registration Patterns

### Auto-Registration Pattern

```csharp
[AutoRegisterBehavior]
public class CustomBehavior : CampaignBehaviorBase
{
    public CustomBehavior() { } // Parameterless required
}
```

### Manual Registration Pattern

```csharp
// In SubModule.OnGameStart
if (gameStarterObject is CampaignGameStarter campaignStarter)
{
    campaignStarter.AddBehavior(new CustomBehavior());
}
```

### Hybrid Registration (Current Implementation)

```csharp
// SubModule.cs
protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
{
    if (gameStarterObject is CampaignGameStarter campaignStarter)
    {
        // Auto-register all [AutoRegisterBehavior] classes
        CalradiaForge.Core.CampaignExtensions.ForgeBehaviorLoader.RegisterAll(campaignStarter);
        
        // Manual registration for specific behaviors
        campaignStarter.AddBehavior(new ClanCharacterProgressionBehavior());
        campaignStarter.AddBehavior(new AgentCognitiveMemoryBehavior());
    }
}
```

## AgentCognitiveMemoryBehavior Architecture (CoALA Memory System)

```
AgentCognitiveMemoryBehavior (Stateless CampaignBehavior)
├── Event Registration
│   ├── Hero Lifecycle & Captivity
│   │   ├── HeroComesOfAgeEvent → Records "Lifecycle" episode & "IsAdult" semantic fact
│   │   ├── HeroPrisonerTaken → Records "Captivity" episode for prisoner & "Victory" for captor
│   │   ├── HeroPrisonerReleased → Records "Liberation" episode & updates "IsImprisoned" to false
│   │   └── HeroKilledEvent → Records "MartialKill", updates "TotalSlainHeroes", records "BloodFeud" for clan kin
│   ├── Character Progression & Social Dynamics
│   │   ├── HeroGainedSkill → Records "SkillProgression" episode & updates "LastMasteredSkill"
│   │   └── HeroRelationChanged → Records "DispositionShift" episode & updates "Relation_{targetId}"
│   └── Periodic Maintenance & Decay
│       ├── HourlyTickEvent → Anti-lag modulo-24 time-sliced semantic maintenance
│       └── OnSessionLaunchedEvent → Resets session telemetry counters
├── Volatile State Contract
│   ├── Fully decoupled from Bannerlord savegames (SyncData is completely empty)
│   ├── All cognitive facts & episodes reside in thread-safe ForgeAgentMemory
│   └── Deceased heroes are cleared automatically upon HeroKilledEvent
└── Modulo-24 Hash Partitioning
    └── ((hero.Id.GetHashCode() & 0x7FFFFFFF) % 24) == currentHour ensures 0 GC spikes
```

## Behavior Discovery Flow

```
ForgeBehaviorLoader.RegisterAll(campaignStarter)
    ↓
Scan all AppDomain assemblies
    ↓
Filter: Assembly references CalradiaForge.Core
    ↓
GetTypes() from each assembly
    ↓
Filter: IsClass && !IsAbstract && IsSubclassOf(CampaignBehaviorBase)
    ↓
Filter: Has [AutoRegisterBehavior] attribute
    ↓
Activator.CreateInstance(type) — Parameterless ctor
    ↓
campaignStarter.AddBehavior(behavior)
```

## Initialization Safety

### Constructor Safety Rule

```csharp
// ✅ CORRECT - Zero entity access
public ClanCharacterProgressionBehavior()
{
    // Only initialize primitive counters
    _lifeCycleEventsProcessed = 0;
}

// ❌ WRONG - Entity access crashes on module load
public unsafeConstructor()
{
    var hero = Hero.MainHero; // Campaign.Current not ready!
}
```

### Session Deferral Pattern

```csharp
// Defer world scanning to session launch
CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);

private void OnSessionLaunched(CampaignGameStarter starter)
{
    // Safe to query Campaign.Current, settlements, heroes here
    foreach (var settlement in Settlement.All)
    {
        // Initialize settlement-specific data
    }
}
```

## Memory Leak Prevention

### Static Reference Anti-Pattern

```csharp
// ❌ WRONG - Static reference prevents GC
public static MyBehavior Instance;

public MyBehavior()
{
    Instance = this; // Prevents behavior from being collected
}
```

### Correct Pattern

```csharp
// ✅ CORRECT - No static references
// Behavior is naturally collected when Campaign.Current is destroyed
```

### Event Subscription Safety

```csharp
// ✅ CORRECT - Non-serialized listeners are cleaned up automatically
CampaignEvents.EventName.AddNonSerializedListener(this, OnHandler);

// ❌ WRONG - Serialized listeners leak across sessions
CampaignEvents.EventName.AddSerializedListener(this, OnHandler);
```

## Thread Safety

### Main Thread Requirement

```csharp
// ❌ WRONG - Campaign API from background thread
Task.Run(() => {
    var hero = Hero.MainHero; // Crashes - wrong thread
});

// ✅ CORRECT - Only on main thread
private void OnHourlyTick()
{
    var hero = Hero.MainHero; // Safe - on main thread
}
```

### Interlocked Operations

```csharp
// ✅ CORRECT - Thread-safe counter increment
Interlocked.Increment(ref _eventsProcessed);

// ❌ WRONG - Non-atomic increment
_eventsProcessed++; // Race condition potential
```

## Error Handling Architecture

### Exception Safety in Handlers

```csharp
private void OnEventHandler(Entity entity)
{
    try
    {
        if (entity == null) return;
        // Process entity
    }
    catch (Exception ex)
    {
        ForgeLogger.PrintError($"Error in OnEventHandler: {ex.Message}");
        // Continue processing - don't crash campaign
    }
}
```

### Crash Recovery

```csharp
// SubModule level crash handler
private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    if (e.ExceptionObject is Exception ex)
    {
        // Write .cfcrash JSON dump
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var path = Path.Combine(basePath, "Modules", "CalradiaForge", $"crash_{timestamp}.cfcrash");
        File.WriteAllText(path, JsonSerializer.Serialize(new CrashDump(ex)));
    }
}
```

## Testing Architecture

### Unit Test Pattern

```csharp
[Test]
public void TestAntiShadowingRule()
{
    var modDir = "src/CalradiaForge.Mod";
    var hasCampaignNamespace = Directory.GetFiles(modDir, "*.cs", SearchOption.AllDirectories)
        .Any(f => File.ReadAllText(f).Contains("namespace.*Campaign"));
    Assert.IsFalse(hasCampaignNamespace, "Found forbidden Campaign namespace");
}
```

### Integration Test Pattern

```csharp
[Test]
public void TestSubModuleRegistration()
{
    var submoduleContent = File.ReadAllText("src/CalradiaForge.Mod/SubModule.cs");
    Assert.IsTrue(submoduleContent.Contains("campaignStarter.AddBehavior"));
    Assert.IsTrue(submoduleContent.Contains("ClanCharacterProgressionBehavior"));
}
```

## Verification Automation

### PowerShell Verification

```powershell
# tools/verify_stateless_behavior.ps1
1. Compilation check (dotnet build -c Release)
2. Statelessness check (0 SaveableTypeDefiner, empty SyncData)
3. Anti-shadowing check (0 "Campaign" names)
4. SubModule registration check
```

### Python Verification

```python
# tools/verify_stateless_behavior.py
Same 4 checks as PowerShell, cross-platform compatible
```

## Documentation Requirements

### Behavior Documentation Template

```csharp
/// <summary>
/// [Purpose of behavior]
/// 
/// Lifecycle:
/// - [Event A] triggers [Action A]
/// - [Event B] triggers [Action B]
/// 
/// State Management:
/// - [Stateless/Legacy data approach]
/// - [Save footprint description]
/// 
/// Performance:
/// - [Time-slicing strategy]
/// - [Anti-lag measures]
/// </summary>
```

## Cognitive Memory & Universal Dialogues (`AgentCognitiveMemoryBehavior`)

### Architectural Overview

`AgentCognitiveMemoryBehavior` implements a zero savegame footprint cognitive architecture based on the CoALA framework (`ForgeAgentMemory`), providing NPCs with dynamic semantic and episodic memories and real-time reactive dialogue behaviors.

```
Campaign Events (HeroPrisonerTaken, HeroKilled, HeroRelationChanged, etc.)
    ↓
ForgeAgentMemory (Volatile, Bounded FIFO Quotas, StringId Indexed)
    ├── Episodic Memory (Experiences, Battles, Captivity, Feuds, Training)
    └── Semantic Memory (Facts, Relations, Total Kills, Imprisonment state, TTL)
    ↓
Modulo-24 Time-Slicing (HourlyTick: 1/24th of active heroes per hour)
    ↓
Universal Cognitive Dialogue Flows
    ├── Lord Greetings (start -> lord_start, Priority 115/112/110)
    │   ├── Blood Feud: Kin slaying grievance reaction
    │   ├── Grateful Liberation: Ransom/release appreciation
    │   └── Respected Rival: Veteran martial respect
    ├── Lord Inquiry (lord_talk_ask_something_2 -> forge_lord_reply_recollection)
    ├── Settlement Notable Inquiry (hero_main_options -> forge_notable_reply_recollection)
    └── Clan Companion Inquiry (hero_main_options -> forge_companion_reply_recollection)
    ↓
Real-Time Desktop IPC Telemetry Bridge (ForgeProtocol "agent-memory")
    └── Desktop Simulation Workbench: Live sync to AgentMemoryDashboardViewModel
```

### Registered Dialogue Token Chaining Map

| Branch / Actor | Input Token | Output Token | Line ID | Priority | Condition Summary |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Lord Greet: Blood Feud** | `start` | `lord_start` | `forge_lord_blood_feud_greet` | 115 | `FeudTargetHeroId == player.Id` or BloodFeud episode contains player name |
| **Lord Greet: Liberation** | `start` | `lord_start` | `forge_lord_grateful_greet` | 112 | Has `Liberation` episode and non-negative relation |
| **Lord Greet: Respected** | `start` | `lord_start` | `forge_lord_respected_greet` | 110 | `TotalSlainHeroes > 0` or relation $\ge 20$ |
| **Lord Inquiry (Player)** | `lord_talk_ask_something_2` | `forge_lord_reply_recollection` | `forge_lord_ask_recollection` | 110 | `Hero.OneToOneConversationHero.IsLord` |
| **Lord Inquiry (NPC Reply)** | `forge_lord_reply_recollection` | `lord_talk_ask_something_2` | `forge_lord_reply_recollection` | 110 | Dynamic memory recall into `{FORGE_COGNITIVE_REPLY}` |
| **Notable Inquiry (Player)** | `hero_main_options` | `forge_notable_reply_recollection` | `forge_notable_ask_recollection` | 110 | `Hero.OneToOneConversationHero.IsNotable` |
| **Notable Inquiry (NPC Reply)**| `forge_notable_reply_recollection` | `hero_main_options` | `forge_notable_reply_recollection` | 110 | Dynamic commercial recall into `{FORGE_COGNITIVE_REPLY}` |
| **Companion Inquiry (Player)**| `hero_main_options` | `forge_companion_reply_recollection`| `forge_companion_ask_recollection` | 110 | `Hero.OneToOneConversationHero.IsPlayerCompanion` |
| **Companion Inquiry (NPC Reply)**| `forge_companion_reply_recollection`| `hero_main_options` | `forge_companion_reply_recollection`| 110 | Dynamic loyalty/progression recall into `{FORGE_COGNITIVE_REPLY}` |

## Stateless Data Lifecycle (`DataBehavior`)

### Architectural Overview

`DataBehavior` provides session-boundary lifecycle management for volatile SDK storage (`ForgeData`), ensuring complete adherence to Rule B (Stateless Campaign Behavior Contract).

```
Session Launch (OnGameStart)
    ↓
DataBehavior (Registered in SubModule.OnGameStart)
    ├── RegisterEvents()
    │   ├── CampaignEvents.OnNewGameCreatedEvent -> ForgeData.ClearAll()
    │   └── CampaignEvents.OnGameLoadedEvent -> ForgeData.ClearAll()
    └── SyncData(IDataStore dataStore)
        └── Completely empty: zero dataStore.SyncData calls, zero SaveableTypeDefiner
```

### Invariant Guarantees
1. **Zero Savegame Bloat**: Empty `SyncData` ensures no serialization footprint or save corruption risks across versions.
2. **Deterministic Lifecycle**: Clears transient variables upon new game creation and game loading, preventing stale state leakage across different campaign sessions.

## Key Architectural Rules Summary

1. **Always AddNonSerializedListener** - Never serialize event subscriptions
2. **Zero Entity Access in Constructors** - Defer to session launch
3. **Modulo-24 Time Slicing** - Required for bulk entity processing
4. **StringId for Entity References** - Never serialize Hero/Settlement directly
5. **Interlocked for Telemetry** - Thread-safe counter increments
6. **No LINQ in Ticks** - Manual iteration to prevent GC
7. **Defensive Null Guards** - Check all entity references
8. **Main Thread Only** - Campaign APIs are single-threaded
9. **Empty SyncData for Stateless** - Zero save footprint
10. **[AutoRegisterBehavior] for Discovery** - Automatic behavior registration

