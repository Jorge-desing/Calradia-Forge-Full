# Mapa del sistema de eventos de CampaignBehavior

## Ciclo de vida de `CampaignBehaviorBase`

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

## Arquitectura del registro de eventos

### Patrón de registro

```csharp
public override void RegisterEvents()
{
    // EXCLUSIVELY use AddNonSerializedListener
    CampaignEvents.EventName.AddNonSerializedListener(this, OnEventHandler);
}
```

### Categorías de eventos en `ClanCharacterProgressionBehavior`

#### 1. Eventos del ciclo de vida de héroes (13 eventos)
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

#### 2. Eventos de clanes y sucesión dinástica (10 eventos)
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

#### 3. Eventos de compañeros y partidas (5 eventos)
```
NewCompanionAdded
CompanionRemoved
OnHeroJoinedPartyEvent
OnPartyLeaderChangedEvent
OnGovernorChangedEvent
```

#### 4. Eventos de matrimonio y embarazo (5 eventos)
```
OnMarriageOfferedToPlayerEvent
OnMarriageOfferCanceledEvent
BeforeHeroesMarried
RomanticStateChanged
OnGivenBirthEvent
```

#### 5. Eventos de progresión de personajes (5 eventos)
```
HeroGainedSkill
HeroLevelledUp
PerkOpenedEvent
PerkResetEvent
PlayerTraitChangedEvent
RenownGained
```

#### 6. Pulsos periódicos de simulación (6 eventos)
```
DailyTickHeroEvent
DailyTickClanEvent
HourlyTickPartyEvent
HourlyTickEvent
DailyTickEvent
WeeklyTickEvent
```

## Patrón de manejador de eventos

### Estructura estándar del manejador

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

### Ejemplo: manejador del ciclo de vida de un héroe

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

## Arquitectura de división temporal

### Planificación por buckets estables para trabajo aplazable

```csharp
int currentHour = (int)CampaignTime.Now.ToHours;
if (ForgeTimeSlicer.ShouldProcess(entity.StringId, currentHour))
{
    ProcessDeferrableEntityWork(entity);
}
```

`ForgeTimeSlicer.ShouldProcess` calcula un hash determinista del identificador recibido y normaliza la hora según la cantidad de buckets. Úsalo solo cuando procesar el trabajo en otro intervalo conserve la semántica de la función. La asignación de buckets es estable, pero no garantiza cargas de trabajo uniformes.

### Uso opcional en manejadores de ticks horarios

```csharp
public override void RegisterEvents()
{
    CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
}

private void OnHourlyTick()
{
    int currentHour = (int)CampaignTime.Now.ToHours;
    var heroes = Hero.AllAliveHeroes;
    if (heroes == null) return;
    int count = heroes.Count;
    for (int i = 0; i < count; i++)
    {
        Hero hero = heroes[i];
        if (hero == null || !hero.IsAlive) continue;
        if (!ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour)) continue;

        ProcessDeferrableHeroWork(hero);
    }
}
```

Invoca este código desde un evento horario para volver a evaluar el predicado conforme la hora avanza por todos los buckets. No filtres un callback individual diario con el bucket horario: el callback no garantiza que volverá a evaluar al héroe en la hora que tiene asignada, así que el trabajo puede aplazarse más allá de la cadencia anunciada o quedar omitido repetidamente. Mantén el trabajo diario que deba ocurrir en su evento diario y sin este filtro.

### Consideraciones de rendimiento

- **Asignación estable**: El mismo identificador siempre corresponde al mismo bucket; la distribución resultante puede ser desigual.
- **Trabajo aplazado**: La operación aplazada solo se realiza para las entidades del bucket seleccionado, pero filtrar una colección sigue recorriéndola completa.
- **Medición necesaria**: Mide la duración de los callbacks, las asignaciones y la distribución del trabajo con datos representativos. La división temporal por sí sola no garantiza menos tiempo por frame ni evita tirones.

## Arquitectura de sincronización del estado

### Patrón `SyncData` (enfoque sin estado)

```csharp
public override void SyncData(IDataStore dataStore)
{
    // EMPTY - Stateless behavior derives all data from live vanilla state
}
```

### Patrón `SyncData` (enfoque con datos heredados)

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

### Patrón `SaveableTypeDefiner`

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

## Seguridad de las referencias a entidades

### Patrón prohibido (no serializar referencias directas)

```csharp
// ❌ WRONG - Causes save corruption on entity destruction
private Hero _trackedHero;
private Settlement _homeSettlement;
```

### Patrón correcto (almacenamiento de `StringId`)

```csharp
// ✅ CORRECT - Safe across entity lifecycle
private string _trackedHeroId;
private string _homeSettlementId;

// Resolution on demand
Hero hero = MBObjectManager.Instance.GetObject<Hero>(_trackedHeroId);
Settlement settlement = MBObjectManager.Instance.GetObject<Settlement>(_homeSettlementId);
```

## Patrones de rendimiento medido

### Patrón 1: medir el costo de LINQ y la iteración

```csharp
// This pipeline can allocate and add work; measure it in the target workload.
var activeHeroes = Hero.AllAliveHeroes.Where(h => h.IsActive).ToList();

// If measurement shows the pipeline is a hot-path cost, compare a direct iteration.
// A loop is not automatically allocation-free: enumerators and called code may allocate.
foreach (var hero in Hero.AllAliveHeroes)
{
    if (!hero.IsActive) continue;
    // Process hero
}
```

### Patrón 2: reutilizar colecciones

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

### Patrón 3: distancia al cuadrado

```csharp
// ❌ WRONG - Expensive sqrt call
float distance = Vec2.Distance(pos1, pos2);
if (distance < 100f) { }

// ✅ CORRECT - Squared comparison
float distanceSquared = Vec2.DistanceSquared(pos1, pos2);
if (distanceSquared < 10000f) { } // 100^2
```

## Protecciones defensivas en manejadores de eventos

### Patrones de protección comunes

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

## Arquitectura de telemetría

### Patrón de contador

```csharp
private int _lifeCycleEventsProcessed;
private int _clanEventsProcessed;
private int _progressionEventsProcessed;

// Thread-safe increment
Interlocked.Increment(ref _lifeCycleEventsProcessed);

// Public read-only access
public int TotalLifeCycleEventsProcessed => _lifeCycleEventsProcessed;
```

### Métricas sin estado

```csharp
// Derived from live vanilla state
public int ActiveTrackedHeroesCount => 
    Hero.AllAliveHeroes != null ? Hero.AllAliveHeroes.Count : 0;
```

## Patrones de registro

### Patrón de auto-registro

```csharp
[AutoRegisterBehavior]
public class CustomBehavior : CampaignBehaviorBase
{
    public CustomBehavior() { } // Parameterless required
}
```

### Patrón de registro manual

```csharp
// In SubModule.OnGameStart
if (gameStarterObject is CampaignGameStarter campaignStarter)
{
    campaignStarter.AddBehavior(new CustomBehavior());
}
```

### Registro híbrido (implementación actual)

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

## Arquitectura de `AgentCognitiveMemoryBehavior` (sistema de memoria CoALA)

```
AgentCognitiveMemoryBehavior (CampaignBehavior sin estado)
├── Registro de eventos
│   ├── Ciclo de vida de héroes y cautiverio
│   │   ├── HeroComesOfAgeEvent → Registra un episodio "Lifecycle" y el hecho semántico "IsAdult"
│   │   ├── HeroPrisonerTaken → Registra un episodio "Captivity" para el prisionero y "Victory" para quien lo capturó
│   │   ├── HeroPrisonerReleased → Registra un episodio "Liberation" y actualiza "IsImprisoned" a false
│   │   └── HeroKilledEvent → Registra "MartialKill", actualiza "TotalSlainHeroes" y registra "BloodFeud" para familiares del clan
│   ├── Progresión de personajes y dinámica social
│   │   ├── HeroGainedSkill → Registra un episodio "SkillProgression" y actualiza "LastMasteredSkill"
│   │   └── HeroRelationChanged → Registra un episodio "DispositionShift" y actualiza "Relation_{targetId}"
│   └── Mantenimiento periódico y caducidad
│       ├── HourlyTickEvent → Selecciona un bucket estable para mantenimiento semántico aplazable; aun así recorre la lista de héroes vivos
│       └── OnSessionLaunchedEvent → Restablece los contadores de telemetría de la sesión
├── Contrato de estado volátil
│   ├── Desacoplado de las partidas guardadas de Bannerlord (`SyncData` está completamente vacío)
│   ├── Los hechos y episodios cognitivos residen en ForgeAgentMemory, segura entre hilos
│   └── Los héroes fallecidos se limpian automáticamente en HeroKilledEvent
└── Planificación por buckets estables (ForgeTimeSlicer)
    └── ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour) selecciona trabajo aplazable; no garantiza buckets uniformes ni ejecución sin asignaciones
```

## Flujo de descubrimiento de behaviors

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

## Seguridad durante la inicialización

### Regla de seguridad para constructores

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

### Patrón para diferir acciones hasta la sesión

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

## Prevención de fugas de memoria

### Antipatrón de referencias estáticas

```csharp
// ❌ WRONG - Static reference prevents GC
public static MyBehavior Instance;

public MyBehavior()
{
    Instance = this; // Prevents behavior from being collected
}
```

### Patrón correcto

```csharp
// ✅ CORRECT - No static references
// Behavior is naturally collected when Campaign.Current is destroyed
```

### Seguridad de las suscripciones a eventos

```csharp
// ✅ CORRECT - Non-serialized listeners are cleaned up automatically
CampaignEvents.EventName.AddNonSerializedListener(this, OnHandler);

// ❌ WRONG - Serialized listeners leak across sessions
CampaignEvents.EventName.AddSerializedListener(this, OnHandler);
```

## Seguridad entre hilos

### Requisito de hilo principal

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

### Operaciones `Interlocked`

```csharp
// ✅ CORRECT - Thread-safe counter increment
Interlocked.Increment(ref _eventsProcessed);

// ❌ WRONG - Non-atomic increment
_eventsProcessed++; // Race condition potential
```

## Arquitectura de manejo de errores

### Seguridad ante excepciones en manejadores

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

### Recuperación ante cierres inesperados

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

## Arquitectura de pruebas

### Patrón de pruebas unitarias

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

### Patrón de pruebas de integración

```csharp
[Test]
public void TestSubModuleRegistration()
{
    var submoduleContent = File.ReadAllText("src/CalradiaForge.Mod/SubModule.cs");
    Assert.IsTrue(submoduleContent.Contains("campaignStarter.AddBehavior"));
    Assert.IsTrue(submoduleContent.Contains("ClanCharacterProgressionBehavior"));
}
```

## Automatización de la verificación

### Verificación con PowerShell

```powershell
# tools/verify_stateless_behavior.ps1
1. Compilation check (dotnet build -c Release)
2. Statelessness check (0 SaveableTypeDefiner, empty SyncData)
3. Anti-shadowing check (0 "Campaign" names)
4. SubModule registration check
```

### Verificación con Python

```python
# tools/verify_stateless_behavior.py
Same 4 checks as PowerShell, cross-platform compatible
```

## Requisitos de documentación

### Plantilla para documentar un behavior

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
/// - [Notas sobre planificación temporal]
/// </summary>
```

## Memoria cognitiva y diálogos universales (`AgentCognitiveMemoryBehavior`)

### Resumen de arquitectura

`AgentCognitiveMemoryBehavior` usa el framework CoALA (`ForgeAgentMemory`) sin serializar su propio estado de memoria en las partidas guardadas. Proporciona a los NPC memorias semánticas y episódicas dinámicas, además de comportamientos de diálogo reactivos en tiempo real.

```
Eventos de campaña (HeroPrisonerTaken, HeroKilled, HeroRelationChanged, etc.)
    ↓
ForgeAgentMemory (volátil, cuotas FIFO limitadas, indexado por StringId)
    ├── Memoria episódica (experiencias, batallas, cautiverio, enemistades, entrenamiento)
    └── Memoria semántica (relaciones absolutas Relation_<Id>, deltas LastRelationDelta_<Id>, bajas totales, TTL)
    ↓
Mantenimiento por buckets estables para trabajo aplazable (HourlyTick: bucket seleccionado; aún recorre los héroes vivos)
    ↓
Flujos de diálogo cognitivo universales
    ├── Saludos de lord (start -> lord_start, prioridad 115/112/110)
    │   ├── Venganza de sangre: reacción de agravio por la muerte de un familiar
    │   ├── Liberación agradecida: agradecimiento por rescate o liberación
    │   └── Rival respetado: respeto marcial entre veteranos
    ├── Consulta a lord (lord_talk_ask_something_2 -> forge_lord_reply_recollection)
    ├── Consulta a notable del asentamiento (hero_main_options -> forge_notable_reply_recollection)
    └── Consulta a compañero del clan (hero_main_options -> forge_companion_reply_recollection)
    ↓
Puente de telemetría IPC en tiempo real a Desktop (ForgeProtocol "agent-memory")
    └── Workbench de simulación Desktop: sincronización en vivo con AgentMemoryDashboardViewModel
```

### Mapa de encadenamiento de tokens de diálogo registrados

| Rama / actor | Token de entrada | Token de salida | ID de línea | Prioridad | Resumen de la condición |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Saludo de lord: venganza de sangre** | `start` | `lord_start` | `forge_lord_blood_feud_greet` | 115 | `FeudTargetHeroId == player.Id` o el episodio BloodFeud contiene el nombre del jugador |
| **Saludo de lord: liberación** | `start` | `lord_start` | `forge_lord_grateful_greet` | 112 | Tiene un episodio `Liberation` y una relación no negativa |
| **Saludo de lord: respeto** | `start` | `lord_start` | `forge_lord_respected_greet` | 110 | `TotalSlainHeroes > 0` o relación $\ge 20$ |
| **Consulta a lord (jugador)** | `lord_talk_ask_something_2` | `forge_lord_reply_recollection` | `forge_lord_ask_recollection` | 110 | `Hero.OneToOneConversationHero.IsLord` |
| **Consulta a lord (respuesta de NPC)** | `forge_lord_reply_recollection` | `lord_talk_ask_something_2` | `forge_lord_reply_recollection` | 110 | Recuerdo dinámico de memoria en `{FORGE_COGNITIVE_REPLY}` |
| **Consulta a notable (jugador)** | `hero_main_options` | `forge_notable_reply_recollection` | `forge_notable_ask_recollection` | 110 | `Hero.OneToOneConversationHero.IsNotable` |
| **Consulta a notable (respuesta de NPC)**| `forge_notable_reply_recollection` | `hero_main_options` | `forge_notable_reply_recollection` | 110 | Recuerdo comercial dinámico en `{FORGE_COGNITIVE_REPLY}` |
| **Consulta a compañero (jugador)**| `hero_main_options` | `forge_companion_reply_recollection`| `forge_companion_ask_recollection` | 110 | `Hero.OneToOneConversationHero.IsPlayerCompanion` |
| **Consulta a compañero (respuesta de NPC)**| `forge_companion_reply_recollection`| `hero_main_options` | `forge_companion_reply_recollection`| 110 | Recuerdo dinámico sobre lealtad y progresión en `{FORGE_COGNITIVE_REPLY}` |

## Ciclo de vida de datos sin estado (`DataBehavior`)

### Resumen de arquitectura

`DataBehavior` administra el ciclo de vida del almacenamiento volátil del SDK (`ForgeData`) en los límites de sesión y mantiene el cumplimiento de la Regla B (contrato de behaviors de campaña sin estado).

```
Inicio de sesión (OnGameStart)
    ↓
DataBehavior (registrado en SubModule.OnGameStart)
    ├── RegisterEvents()
    │   ├── CampaignEvents.OnNewGameCreatedEvent -> ForgeData.ClearAll()
    │   └── CampaignEvents.OnGameLoadedEvent -> ForgeData.ClearAll()
    └── SyncData(IDataStore dataStore)
        └── Vacío: sin llamadas dataStore.SyncData ni SaveableTypeDefiner propios
```

### Garantías invariantes
1. **Sin datos propios en el guardado**: `SyncData` vacío evita que el behavior serialice su estado; esto no constituye una garantía general sobre el tamaño del guardado o la ausencia de corrupción por otras causas.
2. **Ciclo de vida determinista**: Limpia variables transitorias al crear o cargar una partida para evitar que se filtren datos antiguos entre sesiones de campaña.

## Resumen de reglas arquitectónicas clave

1. **Usa siempre `AddNonSerializedListener`**: no serialices las suscripciones a eventos.
2. **No accedas a entidades desde constructores**: difiere ese trabajo hasta el inicio de sesión.
3. **Planificación por buckets estables**: opcional para trabajo masivo aplazable; mide el balance de buckets y el costo del recorrido.
4. **Usa `StringId` para referencias a entidades**: no serialices directamente `Hero` o `Settlement`.
5. **Usa `Interlocked` para telemetría**: incrementos de contadores seguros entre hilos.
6. **Mide las asignaciones de los ticks**: elige una estrategia de iteración más sencilla solo si el perfil confirma que ayuda; ninguna sintaxis garantiza cero asignaciones.
7. **Añade protecciones defensivas contra valores nulos**: valida las referencias a entidades.
8. **Usa solo el hilo principal**: las API de campaña son de un solo hilo.
9. **Deja `SyncData` vacío en behaviors sin estado**: sin datos propios que serializar.
10. **Usa `[AutoRegisterBehavior]` para el descubrimiento**: registro automático de behaviors.

### Lecturas del reloj de campaña con fallo cerrado (Rev141)

`ClanCharacterProgressionBehavior.ShouldProcessInCurrentHour` devuelve `false` cuando no puede leer `CampaignTime.Now.ToHours`. Un fallo transitorio del reloj del motor omite ese elemento periódico en vez de considerarlo listo para procesar. La regresión comprueba estructuralmente la protección; no inyecta un reloj defectuoso del motor en vivo.

