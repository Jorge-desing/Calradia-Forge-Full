# Mapa de código de la arquitectura de Calradia Forge

## Visión general de la estructura de módulos

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

## Jerarquía de dependencias entre ensamblados

```
TaleWorlds.* (Native Engine)
    ↓
CalradiaForge.Core (Framework Layer)
    ↓                    ↓
CalradiaForge.Mod   CalradiaForge.Sdk
    ↓                    ↓
CalradiaForge.Desktop (Companion)
```

## Arquitectura de CalradiaForge.Core

### Componentes principales del framework

| Componente | Propósito | Clases principales |
|-----------|---------|-------------|
| **Extensiones de campaña** | Registro y descubrimiento automático de comportamientos | `ForgeBehaviorLoader`, `AutoRegisterBehaviorAttribute` |
| **Auditor de reglas del mod** | Análisis estático y validación | `ModRuleAuditor`, `RuleFinding`, `RuleAuditResult` |
| **Diagnóstico** | Registro e informes de errores | `ForgeLogger` |
| **Configuración** | Gestión de la configuración del mod | `ForgeConfig` |
| **Framework de interfaz** | Extensiones de la interfaz Gauntlet | Componentes del espacio de nombres `UI` |
| **Flujos de trabajo de parches** | Revisión previa de solo lectura de planos de parches y sustitución experimental de métodos de Forge solicitada por separado | `PatchPreflightEngine`, `ForgePatchService`, `ForgePatcher`, `ForgeDetour` |

### Patrones críticos de Core

1. **Patrón de registro automático**: El atributo `[AutoRegisterBehavior]` permite descubrir automáticamente comportamientos en los ensamblados
2. **Validación basada en reglas**: `ModRuleAuditor` aplica restricciones arquitectónicas mediante análisis con expresiones regulares/AST
3. **Revisión previa de parches**: `PatchPreflightEngine` resuelve las referencias exactas al destino y a las funciones de devolución de llamada declaradas por los planos con respecto a los ensamblados ya cargados en la sesión. No carga ensamblados, invoca funciones de devolución de llamada, aplica parches ni demuestra que pueda instalarse un desvío.

## Arquitectura de CalradiaForge.Mod

### Flujo del ciclo de vida de SubModule

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
    └─ Navegación por teclado con guardas de nulidad (UIContext/EventManager)

OnMissionBehaviorInitialize(Mission)
    └─ Add EventObserver (agent create/delete)

OnSubModuleUnloaded()
    ├─ Desacoplar manejador UnhandledException (prevención de fugas)
    ├─ Close Gauntlet layer
    ├─ Clear SDK singletons
    ├─ ForgeDetour.UnpatchAll()
    └─ Runtime.Dispose()
```

### Arquitectura de los comportamientos de campaña

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
│   └── ForgeTimeSlicer.ShouldProcess(stringId, currentHour) - Planificación por buckets estables
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
│   └── ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour) para mantenimiento aplazable
└── Volatile Memory Integration
    ├── ForgeAgentMemory.Episodic.TryAdd (Captivity, Liberation, MartialKill, BloodFeud, DispositionShift)
    ├── ForgeAgentMemory.Semantic.TryUpsert (IsAdult, IsImprisoned, TotalSlainHeroes, FeudTargetHeroId)
    └── Zero save footprint (empty SyncData)

DataBehavior (Stateless)
├── Event Registration (OnNewGameCreated, OnGameLoaded)
├── Zero save footprint (empty SyncData adhering to Rule B)
└── Resets ForgeData volatile cache on session transitions
```

### Arquitectura de los espacios de nombres

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

## Arquitectura de CalradiaForge.Sdk

### Patrón de constructores del SDK

El SDK proporciona API de construcción fluida para varios sistemas de Bannerlord:

| Constructor | Sistema de destino | Métodos principales |
|---------|---------------|-------------|
| `ForgeNoviceHub` | Sistema de aprendizaje/tutoriales | `AddLesson()`, `AddChallenge()` |
| `ForgeTroopBuilder` | Creación de tropas/personajes | `SetEquipment()`, `SetSkills()` |
| `ForgeItemBuilder` | Definiciones de objetos/fabricación | `SetDamage()`, `SetWeight()` |
| `ForgeQuestBuilder` | Sistema de misiones | `AddObjective()`, `SetRewards()` |
| `ForgeDialogueBuilder` | Sistema de diálogos | `AddLine()`, `AddCondition()` |
| `ForgePartySpawner` | Generación de grupos | `SetPosition()`, `AddTroops()` |
| `ForgeAudioBuilder` | Sistema de audio | `SetCategory()`, `SetPath()` |
| `ForgeHintBuilder` | Indicaciones/descripciones emergentes de la interfaz | `SetText()`, `SetPosition()` |

### Arquitectura de servicios del SDK

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

## Arquitectura contra el ocultamiento de nombres (GEMINI.md)

### Patrones prohibidos

| Patrón | Valor prohibido | Sustitución |
|---------|----------------|-------------|
| Nombre de carpeta | `Campaign/` | `CampaignBehaviors/`, `CampaignExtensions/` |
| Espacio de nombres | `*.Campaign` | `*.CampaignBehaviors`, `*.CampaignMechanics` |
| Nombre de clase | `Campaign` | `CampaignBehavior`, `CampaignSystem` |
| Espacio de nombres | `*.Localization` | `*.GameLocalization` |

### Puntos de aplicación

1. **ModRuleAuditor.GEMINI_CAMPAIGN_SHADOWING** - Comprobación con expresiones regulares en tiempo de ejecución
2. **verify_stateless_behavior.ps1/.py** - Verificación durante la compilación
3. **Revisión manual del código** - Validación previa al commit

## Arquitectura de verificación

### Flujo de compilación

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

### Infraestructura de pruebas

```
tests/CalradiaForge.Tests/
├── ClanCharacterProgressionTests.cs
│   ├── TestAntiShadowingRule
│   ├── TestSubModuleRegistration
│   └── TestStatelessBehavior
└── (Additional test suites)
```

## Patrones de flujo de datos

### Flujo de eventos de campaña

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

### Flujo del puente del SDK

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

## Arquitectura de rendimiento

### Estrategias de rendimiento medidas

1. **Planificación por buckets estables**: Aplazar solo trabajo cuya semántica permita procesarlo en otro intervalo. `ForgeTimeSlicer` usa un hash estable del identificador; la distribución puede ser desigual y filtrar una colección sigue recorriéndola completa.
   ```csharp
   int currentHour = (int)CampaignTime.Now.ToHours;
   if (ForgeTimeSlicer.ShouldProcess(entity.StringId, currentHour)) { ProcessEntity(entity); }
   ```

2. **Medición de asignaciones**: Evitar LINQ en un tick caliente solo cuando el perfil o benchmark confirme el costo; los bucles explícitos y callbacks no son automáticamente libres de asignaciones.
3. **Distancia al cuadrado**: Usar `DistanceSquared()` en lugar de `Math.Sqrt()` cuando solo se comparan distancias.
4. **Contadores Interlocked**: Usar incrementos atómicos para contadores compartidos entre hilos; medir por separado el trabajo circundante.

## Arquitectura del sistema de guardado

### Enfoque sin estado (ClanCharacterProgressionBehavior)

```
Zero Save Footprint:
├─ No SaveableTypeDefiner inheritance
├─ Empty SyncData() method
├─ No [SaveableField] attributes
└─ All data derived from live vanilla state
```

### Enfoque de datos heredado (DataBehavior)

```
Custom Save Data:
├─ SaveableTypeDefiner with base ID >= 2,500,000
├─ Container definitions for collections
└─ Defensive null guards in SyncData
```

## Seguridad y recuperación ante fallos

### Sistema de volcados de fallos

```
OnSubModuleLoad()
    └─ AppDomain.CurrentDomain.UnhandledException
        ├─ Generate .cfcrash JSON timestamped dump
        ├─ Write to Modules/CalradiaForge/
        └─ Include: timestamp, isTerminating, exception stack
```

### Seguridad de la inicialización

1. **Seguridad de los constructores**: Los comportamientos no realizan consultas de entidades en los constructores
2. **Diferimiento hasta la sesión**: La exploración del mundo se difiere hasta OnSessionLaunchedEvent
3. **Protecciones frente a valores nulos**: Todos los manejadores de eventos comprueban si las entidades son nulas
4. **Solo el hilo principal**: No se llama a la API de Campaign desde hilos en segundo plano

## Puntos de integración

### Integración de herramientas externas

```
ExtensionStartup.Connect(TestEngine)
    ↓
Runtime (ITestServices implementation)
    ↓
ForgeData / ForgeAgentMemory / ForgeUI
    ↓
External tools read/write game state
```

### Capacidades explícitas de parches de Forge

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

La revisión previa y la aplicación de parches son capacidades independientes; la revisión previa no condiciona ni activa la aplicación. No existe una exploración automática de ensamblados al iniciar. El punto de entrada heredado `ForgeBootstrapper.InitializeGlobalPatches()` está obsoleto y no hace nada. La acción `patch-diagnostics` captura registros acotados de hooks y reemplazos propiedad de Forge; también puede incluir una señal diagnóstica acotada del runtime cuando un ensamblado ya cargado llamado `0Harmony` expone la superficie pública estática de consulta esperada `HarmonyLib.Harmony.GetAllPatchedMethods()` y `GetPatchInfo(MethodBase)`. La señal no es una prueba de compatibilidad con una versión o combinación de mods. El observador reflectivo opcional no tiene dependencia distribuida, nunca carga el runtime ni modifica parches; ForgeDetour y ForgeWeave no lo utilizan.

### Arquitectura de la malla cooperativa de eventos de ForgeWeave

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
    └── Muestras APM preasignadas (el comportamiento de asignaciones debe medirse por recorrido)
        ├── Rolling 64-sample circular buffer (double[64])
        ├── P50 / P95 / P99 latency percentiles
        └── Histogram distribution buckets (<1ms, 1-5ms, 5-20ms, >20ms)
### Subsistema de memoria cognitiva y telemetría IPC en tiempo real

```
Campaign Simulation Events (HeroPrisonerTaken, HeroKilled, HeroRelationChanged, etc.)
    ↓
AgentCognitiveMemoryBehavior (mantenimiento por buckets estables en HourlyTick; recorre los héroes vivos)
    ↓
ForgeAgentMemory (CoALA Semantic & Episodic Volatile Storage)
    ├── Universal Reactive Dialogues (start -> lord_start, lord_talk_ask_something_2, hero_main_options)
    └── ForgeProtocol IPC Endpoint ("agent-memory" & "agent-memory-query")
            ↓
    CalradiaForge.Desktop PipeClient
            ↓
    AgentMemoryDashboardViewModel (Live Telemetry & Multi-Tier Analytics)
```

### Subsistema de información táctica, selección de comandos y personalización del rol del creador de mods

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

La interfaz también sitúa Patch Blueprint Preflight y Patch Diagnostics en la categoría de navegación Weave. Son herramientas independientes: la revisión previa examina declaraciones inertes y el diagnóstico informa del estado propiedad de Forge; su adaptador reflectivo opcional puede observar un runtime externo ya cargado. Ninguna es una función de ForgeWeave.

## Estructura de distribución del módulo

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

## Invariantes arquitectónicas principales

1. **Sin dependencia de frameworks externos de parches en GameModels**: Usar el patrón Decorator en su lugar
2. **Siempre AddNonSerializedListener**: Nunca usar AddSerializedListener
3. **StringId para referencias a entidades**: Nunca serializar Hero/Settlement directamente
4. **Planificación por buckets estables**: Usar `ForgeTimeSlicer` solo para trabajo masivo que pueda aplazarse; medir la distribución y el costo de recorrer las entradas
5. **Cumplimiento contra el ocultamiento de nombres**: Nunca usar los nombres "Campaign" o "Localization"
6. **GameModels sin estado**: Sin estado mutable en las clases de modelos
7. **Solo el hilo principal**: Las API de Campaign funcionan en un único hilo
8. **Inmutabilidad de los ID**: Los ID de SaveableTypeDefiner nunca cambian
