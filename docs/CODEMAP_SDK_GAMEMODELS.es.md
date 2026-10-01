# Mapa de arquitectura del SDK y GameModels — producto 25.2.0, ForgeApi.Version 13

## Arquitectura del patrón Builder del SDK

### Jerarquía de builders

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

## Arquitectura de los servicios del SDK

El código fuente actual del producto es 25.2.0 y `ForgeApi.Version` es 13. El contrato actual incluye el servicio opcional de parches `ForgeApi.Patches` y el servicio opcional de hooks de ejecución `ForgeApi.Hooks`; el reemplazo de métodos se introdujo en la versión 11 y los hooks de ejecución en la versión 12, con Finalizer y metadatos aditivos de tipos de hook en snapshots en la versión 13. Estas incorporaciones no cambiaron el contrato de bibliotecas compartidas introducido en la versión 7.

### Contrato SDK v10 — resultados explícitos y generaciones de conexión (fuente del producto 25.0.0)

`ModSettings.TrySave<T>` devuelve un resultado tipado y confirma un archivo temporal en el mismo directorio antes de actualizar la caché de ajustes; los fallos del serializador o del almacenamiento conservan el último estado confirmado del archivo y la caché. `ForgeApi.AutoRegisterWithReport` devuelve conteos acotados y diagnósticos por tipo, y conserva los registros correctos aunque falle otro candidato. Los métodos `GetResult<T>` de las memorias Semantic y Procedural distinguen `Found`, `Missing` y `TypeMismatch`; Semantic también distingue y limpia las entradas `Expired`. Las entregas administradas de `RegisterWhenAvailable` se comprueban frente a la generación activa del registro y la vigencia del suscriptor, de modo que un callback que se reconecte o se dé de baja durante el despacho no pueda pasar el registro obsoleto al resto de esa instantánea de disponibilidad.

### Contrato SDK v9 y registro administrado de ForgeWeave (fuente del producto 24.0.0)

`ForgeCampaignEvents.SubscribeWeaveWhenAvailable(...)` vincula un manejador delegado al ciclo de disponibilidad de Forge y devuelve un `IDisposable` `ForgeWeaveRegistration`. Su `State` puede ser `WaitingForHost`, `Registered`, `HostUnsupported`, `Failed` o `Disposed`; `Error` contiene el fallo más reciente y `Handler` es el `IForgeEventHandler` activo cuando está registrado. El helper registra de inmediato si el host está disponible, retira el manejador al desconectarse, espera la reconexión y deja de registrar cuando el propietario del módulo lo elimina. Los callbacks de registro se ejecutan sincrónicamente en el hilo de conexión del host; es seguro eliminar un registro pendiente desde cualquier hilo, mientras que eliminar uno activo desde otro hilo lanza una excepción y lo deja registrado para volver a intentarlo en el hilo correcto. Si `IForgeEventRegistry.Register` lanza una excepción, el handle administrado falla de forma cerrada y no se registra automáticamente en conexiones posteriores: el host pudo haber agregado el ID parcialmente y `Unregister(IForgeEventHandler)` se basa en el ID, por lo que no es una reversión segura tras un fallo incierto. Inspecciona el host, elimina el handle fallido, resuelve cualquier ID residual y crea un handle nuevo. La eliminación reentrante suprime los callbacks de disponibilidad hasta completar la limpieza, y un error de limpieza antiguo no sustituye el estado establecido por una generación más reciente. `SubscribeWeave(...)` sigue siendo la vía inmediata y lanza una excepción si el registro de eventos no está disponible o la llamada se realiza desde otro hilo; si falla el registro en el host, inspecciona el host antes de reintentar.

`ForgeAgentMemory` es un almacén estático, seguro para hilos y local al proceso; no persiste en archivos de guardado. Todos los niveles comparten un límite de 2.048 IDs de agente distintos. La memoria Semantic admite hasta 128 datos por agente y es la única que admite TTL opcional. La memoria Episodic admite hasta 512 entradas por agente y 128 por tipo; FIFO elimina el episodio más antiguo del agente para respetar el límite total y, cuando hace falta, el episodio más antiguo restante del tipo entrante para respetar el límite por tipo. La memoria Procedural admite hasta 128 tareas por agente. Semantic y Procedural pueden actualizar claves existentes al llegar al límite, pero rechazan claves nuevas; `TryUpsert`/`TryAdd` indican el rechazo por capacidad con `false`, mientras que los métodos heredados `Upsert`/`Add` lanzan `InvalidOperationException`. Las escrituras Episodic se aceptan con expulsión FIFO. `ClearAgent` y `ClearAll` liberan los valores retenidos.

### Servicios compartidos entre módulos (funciones introducidas en el contrato SDK 7)

`ForgeApi.Libraries` administra `SharedLibraryRegistry`, que está vinculado al hilo que lo creó. Un módulo abre un `ModuleLibrary` y es propietario de sus registros y monitores durante la vida útil del módulo:

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

La resolución exige identidad exacta de la interfaz CLR y conserva las reglas existentes de compatibilidad de versiones. `Require<T>` sigue fallando de inmediato. Los monitores se inicializan con una instantánea `Current` y después reciben notificaciones sincrónicas y aisladas en el hilo del registro tras cada cambio completo de estado. `SharedServiceChangedEventArgs<T>.Previous` y `.Current` son instantáneas inmutables de la transición y pueden quedar obsoletas antes de que se ejecuten los manejadores posteriores. Una mutación realizada por un manejador surte efecto sincrónicamente y actualiza las instantáneas de los monitores de inmediato; solo se pone en cola la entrega de callbacks para evitar el despacho recursivo. Lee `monitor.Current` o llama a `Resolve<T>` para observar el estado más reciente del registro; un handle puede haber quedado invalidado tras retirarse. Los fallos de callbacks se agregan en ese monitor sin bloquear a los otros manejadores. Retirar y volver a publicar crea una nueva generación de registro; no existe un reemplazo en sitio. Al descargarse el consumidor se desconectan sus monitores, y la desconexión global del registro los elimina silenciosamente, sin un evento final. Consulta [`SHARED_LIBRARIES.md`](SHARED_LIBRARIES.md) para conocer el ciclo de vida de proveedores y consumidores y los detalles de empaquetado.

### Propiedad de modificadores de modelos del SDK v9

`ForgeModelRegistry.BeginOwnerScope(ownerId)` devuelve un `ForgeModelRegistrationScope` desechable. El scope acepta modificadores solo cuando `modifier.SourceModule` coincide exactamente con su ID propietario. Al eliminarlo, quita únicamente los registros y generaciones de modificadores que aún pertenecen a ese scope; se conserva un registro heredado o scoped posterior que haya reemplazado el mismo ID. Siguen disponibles los métodos de registro global `Register` y `Unregister`. `GetModifiers(category)` y `Evaluate(category, ...)` reutilizan una instantánea inmutable en caché que contiene solo esa categoría; las mutaciones invalidan la caché, mientras que las instantáneas ya devueltas permanecen estables. Los predicados de modificadores proporcionados por el usuario siguen ejecutándose después de liberar el bloqueo del registro, por lo que pueden volver a entrar o modificarlo sin detener operaciones no relacionadas.

### Servicios principales

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

`ForgeApi.Patches.ApplyMethodReplacement(string patchId, string owner, MethodInfo target, MethodInfo replacement)` y `ForgePatcher.ApplyAll(Assembly assembly)` son vías de aplicación explícitas e independientes. El escritor de bajo nivel es experimental y no coordina a otros hilos que estén ejecutando el destino. Los tipos de hook de blueprints siguen siendo declaraciones inertes. El servicio opcional de ejecución `ForgeHookService` implementa Prefix/Postfix/Finalizer por separado; su adaptador `RegisterTranspiler`, exclusivo de Core `net472`, usa MonoMod `ILHook` sin añadir tipos MonoMod al SDK. ForgeDetour no expone sobrecargas de HarmonyMethod. Consulta las [políticas de hooks de ejecución](PATCH_BLUEPRINTS.es.md#hooks-explícitos-en-ejecución-y-transpilers-il) para manejo de excepciones, duración del cuerpo IL, autorización del host y límites de reconstrucción.

## Patrón decorador de GameModel

### Arquitectura del decorador

```
Native GameModel (TaleWorlds)
    ↓
Custom Decorator (Inherits Native Model)
    ├─ Constructor injection of base model
    ├─ Null-safe delegation
    ├─ Custom logic layer
    └─ Return modified result
```

### Plantilla del decorador

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

### Patrón de registro

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

## Mecánica de ExplainedNumber

### Fórmula matemática

```
FinalResult = (BaseNumber + Σ Adds) × (1.0 + Σ Factors)
```

### Efectos de los métodos

| Método | Efecto | Notas |
|--------|--------|-------|
| `Add(float val, TextObject desc)` | Suma directa a la base | Se aplica antes de los factores |
| `AddFactor(float factor, TextObject desc)` | Suma al total de factores | No es compuesto: 0.10 + 0.10 = 0.20 (no 0.21) |
| `LimitMin(float min)` | Limita el resultado a ≥ min | Esencial para establecer mínimos |
| `LimitMax(float max)` | Limita el resultado a ≤ max | Establece un máximo |
| `ResultNumber` | Devuelve el valor `float` final | Getter de evaluación |

### Semántica de valor de los structs

```csharp
// ❌ WRONG - Local copy doesn't affect original
ExplainedNumber result = baseSpeed;
result.Add(5.0f, desc); // Modifies copy, not baseSpeed

// ✅ CORRECT - Work with returned value
ExplainedNumber result = _baseModel.CalculateFinalSpeed(party, baseSpeed);
result.Add(5.0f, desc); // Modifies the result we'll return
return result;
```

### Optimización del rendimiento

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

## Implementaciones habituales de GameModel

### Modelo de velocidad de grupos

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

### Modelo de salarios de grupos

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

### Modelo de talleres

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

## Flujo de integración del SDK

### Integración con herramientas externas

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

### Puente de ejecución

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

## Ejemplos de uso de builders del SDK

### Builder de tropas

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

### Builder de misiones

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

### Builder de diálogos

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

## Ausencia de estado en GameModel

### Regla de ausencia de estado

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

### Patrón de persistencia del estado

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

## Puntos de extensión del SDK

### Registro de builders personalizados

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

### Registro de servicios personalizados

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

## Gestión de errores del SDK

### Validación de builders

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

### Gestión de errores de servicios

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

## Patrones de prueba del SDK

### Prueba unitaria de builder

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

### Prueba de integración de servicios

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

## Estándares de documentación del SDK

### Plantilla de documentación de builders

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

### Plantilla de documentación de servicios

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

## Reglas clave del SDK y GameModel

1. **Ausencia de estado en GameModel**: Nunca guardes estado mutable en clases GameModel
2. **Patrón decorador**: Envuelve siempre los modelos base; nunca los reemplaces
3. **Delegación segura ante null**: Comprueba siempre `_baseModel != null` antes de delegar
4. **Struct ExplainedNumber**: Ten presente la semántica de valor de los structs
5. **IncludeDescriptions**: Propaga el parámetro `includeDescriptions`
6. **Validación de builders**: Valida las propiedades obligatorias en el método `Build()`
7. **Ciclo de vida de servicios**: Limpia los servicios al terminar la campaña o misión
8. **Seguridad de hilos**: Documenta los requisitos de seguridad entre hilos
9. **Gestión de errores**: Ofrece alternativas seguras y registra los fallos
10. **Seguridad de tipos**: Usa `GetValue<T>` genérico con comprobación de tipo
