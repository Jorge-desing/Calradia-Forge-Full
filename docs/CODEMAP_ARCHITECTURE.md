# Calradia Forge Architecture Codemap

## Module Structure Overview

```
Mod Desarrolladores/
├── src/
│   ├── CalradiaForge.Core/          # Shared framework & utilities
│   ├── CalradiaForge.Mod/            # Main Bannerlord mod assembly
│   ├── CalradiaForge.Sdk/            # SDK for external integrations
│   └── CalradiaForge.Desktop/       # Desktop companion application
├── modules/CalradiaForge/           # Distribution module structure
├── tools/                            # Build & verification scripts
├── tests/                           # Unit & integration tests
└── docs/                            # Documentation
```

## Assembly Dependency Hierarchy

```
TaleWorlds.* (Native Engine)
    ↓
CalradiaForge.Core (Framework Layer)
    ↓                    ↓
CalradiaForge.Mod   CalradiaForge.Sdk
    ↓                    ↓
CalradiaForge.Desktop (Companion)
```

## CalradiaForge.Core Architecture

### Core Framework Components

| Component | Purpose | Key Classes |
|-----------|---------|-------------|
| **Campaign Extensions** | Behavior registration & auto-discovery | `ForgeBehaviorLoader`, `AutoRegisterBehaviorAttribute` |
| **Mod Rule Auditor** | Static analysis & validation | `ModRuleAuditor`, `RuleFinding`, `RuleAuditResult` |
| **Diagnostics** | Logging & error reporting | `ForgeLogger` |
| **Configuration** | Mod configuration management | `ForgeConfig` |
| **UI Framework** | Gauntlet UI extensions | `UI` namespace components |
| **Patch workflows** | Read-only patch blueprint preflight and separately requested experimental Forge method replacement | `PatchPreflightEngine`, `ForgePatchService`, `ForgePatcher`, `ForgeDetour` |

### Critical Patterns in Core

1. **Auto-Registration Pattern**: `[AutoRegisterBehavior]` attribute enables automatic behavior discovery across assemblies
2. **Rule-Based Validation**: `ModRuleAuditor` enforces architectural constraints via regex/AST analysis
3. **Patch Preflight**: `PatchPreflightEngine` resolves the exact target and callback references declared by blueprints against assemblies already loaded in the session. It does not load assemblies, invoke callbacks, apply patches, or prove that a detour can be installed.

## CalradiaForge.Mod Architecture

### SubModule Lifecycle Pipeline

```
OnSubModuleLoad()
    ├─ UnhandledException handler (.cfcrash JSON dump)
    ├─ Runtime.Initialize()
    ├─ UIExtender.Initialize()
    └─ No patch discovery or application

OnBeforeInitialModuleScreenSetAsRoot()
    └─ runtime.NotifyInitialScreenReady()

OnGameStart(Game, IGameStarter)
    ├─ ForgeBehaviorLoader.RegisterAll(campaignStarter) [Auto-discovery]
    ├─ campaignStarter.AddBehavior(new ClanCharacterProgressionBehavior()) [Manual]
    ├─ campaignStarter.AddBehavior(new AgentCognitiveMemoryBehavior()) [Manual]
    └─ campaignStarter.AddBehavior(new DataBehavior()) [Manual]

OnCampaignStart(Game, object)
    └─ runtime.NotifyCampaignStarted()

OnApplicationTick(float dt)
    ├─ Hotkey handling (F10 default)
    ├─ Language refresh
    ├─ Panel ViewModel Tick
    └─ Keyboard navigation

OnMissionBehaviorInitialize(Mission)
    └─ Add EventObserver (agent create/delete)

OnSubModuleUnloaded()
    ├─ Close Gauntlet layer
    ├─ Clear SDK singletons
    ├─ ForgeDetour.UnpatchAll()
    └─ Runtime.Dispose()
```

### Campaign Behavior Architecture

```
ClanCharacterProgressionBehavior (Stateless)
├── Event Registration (45+ CampaignEvents)
│   ├── Hero Lifecycle (13 events)
│   ├── Clan & Dynastic Succession (10 events)
│   ├── Companions & Parties (5 events)
│   ├── Marriage & Pregnancy (5 events)
│   ├── Character Progression (5 events)
│   └── Periodic Ticks (6 events)
├── Time-Slicing Utility
│   └── ForgeTimeSlicer.ShouldProcess(stringId, currentHour) - Stable bucket scheduling
└── Event Handlers
    ├── Null-safe entity checks
    ├── Interlocked counter increments
    └── Stateless vanilla state queries

AgentCognitiveMemoryBehavior (Stateless)
├── Event Registration (TaleWorlds CampaignEvents)
│   ├── Hero Lifecycle & Captivity (HeroComesOfAge, HeroKilled, HeroPrisonerTaken, HeroPrisonerReleased)
│   ├── Social & Progression (HeroRelationChanged, HeroGainedSkill)
│   └── Periodic Maintenance (HourlyTickEvent, OnSessionLaunchedEvent)
├── Time-Slicing Utility
│   └── ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour) for eligible deferred maintenance
└── Volatile Memory Integration
    ├── ForgeAgentMemory.Episodic.TryAdd (Captivity, Liberation, MartialKill, BloodFeud, DispositionShift)
    ├── ForgeAgentMemory.Semantic.TryUpsert (IsAdult, IsImprisoned, TotalSlainHeroes, FeudTargetHeroId)
    └── Zero save footprint (empty SyncData)

DataBehavior (Stateless)
├── Event Registration (OnNewGameCreated, OnGameLoaded)
├── Zero save footprint (empty SyncData adhering to Rule B)
└── Resets ForgeData volatile cache on session transitions
```

### Namespace Architecture

```
CalradiaForge.Mod
├── CampaignBehaviors/          # All CampaignBehaviorBase implementations
│   ├── ClanCharacterProgressionBehavior.cs
│   ├── AgentCognitiveMemoryBehavior.cs
│   └── DataBehavior.cs
├── Commands/                    # Console/commands infrastructure
├── (Root level files)
│   ├── SubModule.cs            # MBSubModuleBase entry point
│   ├── Runtime.cs              # SDK bridge & lifecycle
│   ├── PanelViewModel.cs       # Gauntlet UI ViewModel
│   ├── UIExtender.cs           # UI extension hooks
│   ├── GameLocalization.cs     # Localization wrapper (NOT "Localization")
│   └── Inspector.cs            # Runtime inspection tools
```

## CalradiaForge.Sdk Architecture

### SDK Builder Pattern

The SDK provides fluent builder APIs for various Bannerlord systems:

| Builder | Target System | Key Methods |
|---------|---------------|-------------|
| `ForgeNoviceHub` | Education/tutorial system | `AddLesson()`, `AddChallenge()` |
| `ForgeTroopBuilder` | Troop/character creation | `SetEquipment()`, `SetSkills()` |
| `ForgeItemBuilder` | Item/crafting definitions | `SetDamage()`, `SetWeight()` |
| `ForgeQuestBuilder` | Quest system | `AddObjective()`, `SetRewards()` |
| `ForgeDialogueBuilder` | Dialogue system | `AddLine()`, `AddCondition()` |
| `ForgePartySpawner` | Party spawning | `SetPosition()`, `AddTroops()` |
| `ForgeAudioBuilder` | Audio system | `SetCategory()`, `SetPath()` |
| `ForgeHintBuilder` | UI hints/tooltips | `SetText()`, `SetPosition()` |

### SDK Service Architecture

```
ForgeData (Campaign State Cache)
    ├─ ClearAll()
    └─ Get/Set typed values

ForgeAgentMemory (Mission State)
    ├─ ClearAll()
    └─ Track agent lifecycle

ForgeCampaignEvents (Event Bridge)
    ├─ ClearSubscribers()
    └─ Publish events to SDK

ForgeUI (UI Management)
    ├─ Clear()
    └─ UI state coordination

ForgeDetour (Experimental native method replacement)
    ├─ Patch(MethodInfo original, MethodInfo replacement)
    ├─ Unpatch(MethodInfo original)
    ├─ UnpatchAll()
    └─ Verify(MethodInfo method, out string status)
```

## Anti-Shadowing Architecture (GEMINI.md)

### Forbidden Patterns

| Pattern | Forbidden Value | Replacement |
|---------|----------------|-------------|
| Folder name | `Campaign/` | `CampaignBehaviors/`, `CampaignExtensions/` |
| Namespace | `*.Campaign` | `*.CampaignBehaviors`, `*.CampaignMechanics` |
| Class name | `Campaign` | `CampaignBehavior`, `CampaignSystem` |
| Namespace | `*.Localization` | `*.GameLocalization` |

### Enforcement Points

1. **ModRuleAuditor.GEMINI_CAMPAIGN_SHADOWING** - Runtime regex check
2. **verify_stateless_behavior.ps1/.py** - Build-time verification
3. **Manual code review** - Pre-commit validation

## Verification Architecture

### Build Pipeline

```
tools/build.ps1
    ├─ dotnet build CalradiaForge.sln -c Release
    ├─ validate DLLs
    └─ exit on compilation errors

tools/verify_stateless_behavior.ps1/.py
    ├─ [1/4] Compilation check
    ├─ [2/4] Statelessness check (0 SaveableTypeDefiner, empty SyncData)
    ├─ [3/4] Anti-shadowing check (0 "Campaign" names)
    └─ [4/4] SubModule registration check
```

### Test Infrastructure

```
tests/CalradiaForge.Tests/
├── ClanCharacterProgressionTests.cs
│   ├── TestAntiShadowingRule
│   ├── TestSubModuleRegistration
│   └── TestStatelessBehavior
└── (Additional test suites)
```

## Data Flow Patterns

### Campaign Event Flow

```
TaleWorlds Engine Event
    ↓
CampaignEvents.*.AddNonSerializedListener(this, handler)
    ↓
ClanCharacterProgressionBehavior Event Handler
    ├─ Null safety checks
    ├─ Interlocked.Increment (telemetry)
    └─ Stateless vanilla state queries
```

### SDK Bridge Flow

```
Mod Action (Player/Script)
    ↓
ForgeXxxBuilder fluent API
    ↓
ForgeData / ForgeAgentMemory / etc.
    ↓
Runtime bridge (ITestServices)
    ↓
External Tool Integration
```

## Performance Architecture

### Measured Performance Strategies

1. **Stable-Bucket Time Slicing**: Defer only work whose semantics permit processing in a later slice. `ForgeTimeSlicer` uses a stable identifier hash; distribution may be uneven, and filtering a collection still scans that collection.
   ```csharp
   int currentHour = (int)CampaignTime.Now.ToHours;
   if (ForgeTimeSlicer.ShouldProcess(entity.StringId, currentHour)) { ProcessEntity(entity); }
   ```

2. **Allocation Measurement**: Avoid LINQ in a hot tick only when profiling or a benchmark confirms the cost; explicit loops and callbacks are not automatically allocation-free.
3. **Squared Distance**: Use `DistanceSquared()` instead of `Math.Sqrt()` when only comparing distances.
4. **Interlocked Counters**: Use atomic increments for counters accessed across threads; measure surrounding work separately.

## Save System Architecture

### Stateless Approach (ClanCharacterProgressionBehavior)

```
Zero Save Footprint:
├─ No SaveableTypeDefiner inheritance
├─ Empty SyncData() method
├─ No [SaveableField] attributes
└─ All data derived from live vanilla state
```

### Legacy Data Approach (DataBehavior)

```
Custom Save Data:
├─ SaveableTypeDefiner with base ID >= 2,500,000
├─ Container definitions for collections
└─ Defensive null guards in SyncData
```

## Security & Crash Recovery

### Crash Dump System

```
OnSubModuleLoad()
    └─ AppDomain.CurrentDomain.UnhandledException
        ├─ Generate .cfcrash JSON timestamped dump
        ├─ Write to Modules/CalradiaForge/
        └─ Include: timestamp, isTerminating, exception stack
```

### Initialization Safety

1. **Constructor Safety**: Behaviors perform zero entity queries in constructors
2. **Session Deferral**: World scanning deferred to OnSessionLaunchedEvent
3. **Null Guards**: All event handlers check for null entities
4. **Main Thread Only**: No Campaign API calls from background threads

## Integration Points

### External Tool Integration

```
ExtensionStartup.Connect(TestEngine)
    ↓
Runtime (ITestServices implementation)
    ↓
ForgeData / ForgeAgentMemory / ForgeUI
    ↓
External tools read/write game state
```

### Explicit Forge Patch Capabilities

```
Read-only review:
PatchPreflightEngine.Inspect(...)
    └─ Resolves declared target/callback references; no patch backend call

Explicit, experimental application (separate capability):
ForgeApi.Patches.ApplyMethodReplacement(...)
    or ForgePatcher.ApplyAll(assembly)
    ↓
ForgeDetour.Patch(MethodInfo original, MethodInfo replacement)
    └─ One-for-one native method replacement; verify/revert explicitly
```

Patch preflight and patch application are independent capabilities; preflight does not gate or trigger application. There is no automatic startup assembly scan. The legacy `ForgeBootstrapper.InitializeGlobalPatches()` entry point is obsolete and does nothing. The `patch-diagnostics` action captures bounded Forge-owned hook/replacement records and may include a bounded runtime diagnostic signal when an already-loaded assembly named `0Harmony` exposes the expected public static `HarmonyLib.Harmony.GetAllPatchedMethods()` and `GetPatchInfo(MethodBase)` query surface. This signal is not a release or mod-compatibility test. The optional reflection observer has no distributed dependency, never loads the runtime, and does not mutate patches; ForgeDetour and ForgeWeave do not use it.

### ForgeWeave Cooperative Event Mesh Architecture

```
ForgeApi.PublishCustomEvent(topic, data) / Host Lifecycle Callbacks
    ↓
QueuedForgeEvent (Bounded 128 queue, drains <=16 per frame)
    ↓
ForgeWeaveEngine.Dispatch()
    ├── Topic Matching (Exact, Prefix Wildcard `*`, Catch-All `*`)
    ├── Smart Circuit Breaker
    │   ├── Closed: Normal execution
    │   ├── Open: Quarantined on FailureLimit; skips dispatch
    │   └── HalfOpen: Probationary probe after CooldownSeconds
    │       ├── Success → Closed (quarantine lifted, cooldown reset)
    │       └── Failure → Open (exponential backoff up to 60s)
    ├── Exception Isolation (Per-handler try/catch)
    └── Preallocated APM Telemetry Samples (allocation behavior must be measured per call path)
        ├── Rolling 64-sample circular buffer (double[64])
        ├── P50 / P95 / P99 latency percentiles
        └── Histogram distribution buckets (<1ms, 1-5ms, 5-20ms, >20ms)
### Cognitive Memory & Real-Time IPC Telemetry Subsystem

```
Campaign Simulation Events (HeroPrisonerTaken, HeroKilled, HeroRelationChanged, etc.)
    ↓
AgentCognitiveMemoryBehavior (stable-bucket maintenance on HourlyTick; scans alive heroes)
    ↓
ForgeAgentMemory (CoALA Semantic & Episodic Volatile Storage)
    ├── Universal Reactive Dialogues (start -> lord_start, lord_talk_ask_something_2, hero_main_options)
    └── ForgeProtocol IPC Endpoint ("agent-memory" & "agent-memory-query")
            ↓
    CalradiaForge.Desktop PipeClient
            ↓
    AgentMemoryDashboardViewModel (Live Telemetry & Multi-Tier Analytics)
```

### Tactical Briefing, Command Curation & Modder Role Customization Subsystem

```
In-Game Gauntlet UI (PanelViewModel)
├── 8 Category Suites:
│   ├── Overview: Project Setup, Manifests, Load Order, Logs
│   ├── Inspector: Live Entities (Hero/Settlement), Memory & Metrics
│   ├── Toolkit: Non-destructive Tests, ModSettings, Commands
│   ├── Weave: ForgeWeave Event Pipeline and Replay Lab
│   ├── Simulate: Balance Models (Diplomacy, Loyalty, Economy, Tactics)
│   ├── Audit: Rule Compliance (36 Bannerlord Rules, Statelessness, Save Safety)
│   ├── Novice: Interactive Scaffolding Wizards (Behaviors, XMLs, Quests)
│   └── SDK: Surface Contracts for 110+ Advanced Developer Tools
├── Tactical Briefing Cards & Engineering Playbooks:
│   ├── CategoryMissionDescription (Technical architectural purpose)
│   ├── CategoryEngineRules (TaleWorlds engine invariants & constraints)
│   ├── CategorySuggestedCommands (Curated executable console commands across 4 families)
│   ├── CategoryPlaybooks: 3-step structured actionable guides (CategoryPlaybookStep1/2/3)
│   └── Troubleshooting Trees: CategoryTroubleshootingTitle & CategoryTroubleshootingAdvice
├── Hybrid Personalization & CoALA Procedural Memory:
│   ├── ModderRole Presets & Dynamic QuickSlots: Auto-preset slot mappings (ApplyRoleQuickSlotPresets)
│   ├── CoALA Cognitive Utility Model: U = 0.4*R + 0.3*F + 0.3*I (Recency, Frequency, Importance/Pinned)
│   ├── CoALA Procedural Memory Macros: Chained action macros in ForgeAgentMemory.Procedural (ExecuteRunMacro)
│   ├── View Density Toggle: IsDetailedMode / DetailModeLabel toggles ForgePlaybookPanel overlay
│   ├── CategoryCommandItemVM: CommandText, Description, IsPinned, UtilityScore, UtilityBadge, SlotLabel (⚡), Run
│   ├── Quick Action Slots: QuickSlot1, QuickSlot2, QuickSlot3 with direct header execution and assignment
│   └── Pinned Favorite Commands: Deck of user-pinned commands prioritized at top of list
│
Desktop Workbench (.NET 8 WPF)
├── 8 Tactical Studio ViewModels:
│   ├── TroopTreeDashboardViewModel (DAG visualizer & combat balance)
│   ├── AudioStudioDashboardViewModel (Acoustic waveforms & mixer categories)
│   ├── WorkshopDashboardViewModel (Macroeconomics & trade pricing)
│   ├── AgentMemoryDashboardViewModel (CoALA cognitive memory inspector)
│   ├── CodeSecurityDashboardViewModel (Security rules & IL instruction audits)
│   ├── ModuleHierarchyDashboardViewModel (SubModule graph & cycle prevention)
│   ├── KingdomDiplomacyDashboardViewModel (Casus belli & power equilibrium)
│   └── ComponentGeneratorDashboardViewModel (Safe C# & XML code synthesizers)
├── Studio Playbooks, Remedies & Command Palettes:
│   ├── StudioDocumentation (Engineering purpose and operational guidelines)
│   ├── ArchitecturalInvariants (Strict TaleWorlds engine rules and limits)
│   ├── Studio Playbooks: PlaybookTitle & PlaybookSteps (3-step tactical workflow)
│   ├── Troubleshooting Trees: TroubleshootingHeader & TroubleshootingRemedy
│   ├── ProceduralMacroAction: Studio-specific multi-step procedural automation
│   ├── CuratedConsoleCommands (StudioConsoleCommand collections with family metadata, pin glyphs ★/☆)
│   ├── QuickActionCommand & QuickActionLabel (One-click execution of studio primary action)
│   └── ScratchpadNotes (Persistent studio-level developer annotations)
└── Operational Rail Modder Role Filtering:
    ├── ModderRolePreset: Dynamic filtering of operational rail routes by specialization
    └── CycleModderRoleCommand: Quick keyboard and UI switching between roles
```

The UI also places Patch Blueprint Preflight and Patch Diagnostics in the Weave navigation category. They are separate tools: preflight reviews inert declarations, while diagnostics reports Forge-owned state and may observe an already-loaded external runtime through its optional reflection adapter. Neither is a ForgeWeave feature.

## Module Distribution Structure

```
modules/CalradiaForge/
├── SubModule.xml                  # Module manifest
├── bin/Win64_Shipping_Client/    # Compiled DLLs
│   ├── CalradiaForge.Mod.dll
│   ├── CalradiaForge.Core.dll
│   └── CalradiaForge.Sdk.dll
├── ModuleData/                    # XML data files
├── GUI/                           # Gauntlet UI prefabs
│   └── Prefabs/
└── (Optional assets)
```

## Key Architectural Invariants

1. **No external patch-framework dependency in GameModels**: Use Decorator Pattern instead
2. **Always AddNonSerializedListener**: Never use AddSerializedListener
3. **StringId for Entity References**: Never serialize Hero/Settlement directly
4. **Stable-Bucket Scheduling**: Use `ForgeTimeSlicer` only for bulk work that is safe to defer; measure distribution and the cost of scanning inputs
5. **Anti-Shadowing Compliance**: Never use "Campaign" or "Localization" names
6. **Stateless GameModels**: No mutable state in model classes
7. **Main Thread Only**: Campaign APIs are single-threaded
8. **ID Immutability**: SaveableTypeDefiner IDs never change
