# Referencia del SDK de Calradia Forge — 25.2.0

Versión del contrato del SDK: **12**
Versión del código fuente del producto: **25.2.0**
Destinos del SDK: `net472` y `net8.0`; el módulo del juego continúa en `net472`.

> Esta referencia describe el árbol de código fuente v25.2.0. Los archivos de distribución son artefactos independientes; consulta sus manifiestos o registros de validación para determinar qué contrato del SDK contienen.

## 1. Registro automático

Usa `ForgeApi.AutoRegister` desde el inicio del módulo para descubrir los componentes compatibles del SDK:

```csharp
public class MySubModule : MBSubModuleBase
{
    protected override void OnSubModuleLoad()
    {
        ForgeApi.AutoRegister(Assembly.GetExecutingAssembly(), "MyModId");
    }
}
```

El registro automático descubre pruebas, comandos, proveedores de diagnósticos y manejadores de eventos Forge admitidos en el ensamblado indicado.

`AutoRegisterWithReport(assembly, moduleId)` devuelve un `ForgeAutoRegisterReport` inmutable con el estado del recorrido, conteos de tipos/candidatos/registros/fallos y hasta 64 detalles de error acotados. Continúa con tipos independientes cuando falla uno; no revierte los registros que ya tuvieron éxito. `HostUnavailable` significa que no había un registro Forge conectado. Si la reflexión aún puede entregar algunos tipos, los fallos parciales de carga también se incluyen en el informe. El `AutoRegister` heredado conserva su retorno `void` y registra los errores acotados mediante `ForgeApi.Logger`.

## 2. Suscripciones ForgeWeave y ciclo de vida del módulo

`ForgeCampaignEvents.SubscribeWeaveWhenAvailable` acepta los mismos argumentos y valores predeterminados que `SubscribeWeave`, y devuelve un `ForgeWeaveRegistration` que administra la devolución de disponibilidad y el registro activo del manejador. Conserva el objeto mientras viva el módulo y libéralo cuando se descargue:

```csharp
private ForgeWeaveRegistration _readyRegistration;

protected override void OnSubModuleLoad()
{
    _readyRegistration = ForgeCampaignEvents.SubscribeWeaveWhenAvailable(
        "my_module.startup_observer",
        ForgeEventKind.ForgeReady,
        OnForgeReady,
        access: ForgeEventAccess.Observe);
}

protected override void OnSubModuleUnloaded()
{
    _readyRegistration?.Dispose();
    _readyRegistration = null;
}

private void OnForgeReady(ForgeEvent @event)
{
    // Procesa el evento en el flujo de despacho normal de Forge.
}
```

El registro comprueba de forma atómica si el anfitrión de eventos está disponible. Se registra sincrónicamente si Forge ya está conectado; espera si aún no lo está; al desconectarse Forge, quita el manejador y vuelve a `WaitingForHost`; y se registra otra vez cuando el anfitrión se reconecta. El registro y la anulación se ejecutan en el hilo de conexión del anfitrión. Un registro pendiente se puede liberar desde cualquier hilo; cuando ya hay un manejador activo, `Dispose()` debe ejecutarse en el hilo que lo registró. Una llamada desde otro hilo lanza `InvalidOperationException`, deja el manejador registrado y guarda el error para que el propietario pueda reintentar en el hilo correcto. Si el propio anfitrión lanza una excepción al anularlo, el registro conserva la referencia al registro activo e informa `Failed` junto con el error; una transición posterior del anfitrión o `Dispose()` en el hilo correcto puede reintentar la baja. Las reconexiones reentrantes se comparan por generación para impedir que una llamada anterior sobrescriba el estado de una conexión más nueva; los errores de limpieza de una generación antigua tampoco reemplazan el estado de la nueva.

`IForgeEventRegistry.Register` no garantiza que el anfitrión haya quedado intacto si lanza una excepción. En ese caso, el handle administrado pasa a `Failed` y deja de registrarse automáticamente en conexiones posteriores, porque el manejador podría haberse añadido. El SDK no intenta revertirlo: `Unregister(IForgeEventHandler)` elimina por ID y podría quitar otro manejador tras un conflicto de IDs. `Error` indica que el estado del anfitrión puede ser incierto. Libera el handle, inspecciona en el anfitrión el ID de la suscripción, resuelve cualquier registro restante y después crea un handle nuevo. Si existe un registro parcial que el SDK no puede identificar con seguridad, `Dispose()` conserva el diagnóstico y solo retira registros conocidos. `SubscribeWeave(...)` también depende del comportamiento del anfitrión; después de una excepción, inspecciona el anfitrión antes de reintentar.

Durante `Dispose()`, las devoluciones de disponibilidad reentrantes se ignoran hasta que termine la limpieza. Si la limpieza lanza, el handle queda disponible para reintentar la disposición en el hilo de registro. No supongas que una baja fallida tuvo éxito: revisa `State` y `Error`.

| `ForgeWeaveRegistrationState` | Significado |
|---|---|
| `WaitingForHost` | No hay conectado un anfitrión de eventos compatible. |
| `Registered` | El manejador está registrado con el anfitrión Forge activo. |
| `HostUnsupported` | Hay un anfitrión, pero no admite esta ruta de registro. |
| `Failed` | Falló el registro o la limpieza; revisa `Error` antes de reintentar o crear otro handle. Una excepción de registro puede dejar incierto el estado del anfitrión. |
| `Disposed` | El propietario liberó el registro; no se registrará en conexiones posteriores. |

`ForgeWeaveRegistration.State`, `.Error` y `.Handler` exponen el estado actual, el último error y el `IForgeEventHandler` activo cuando está disponible. `SubscribeWeave` sigue siendo la opción de registro inmediato y lanza `InvalidOperationException` cuando `ForgeApi.Events` no está disponible o si se invoca fuera del hilo de conexión del anfitrión. Libera el manejador devuelto con `UnsubscribeWeave(handler)` o `UnsubscribeWeave(handlerId)` en el hilo de conexión.

Para manejadores implementados como clases `IForgeEventHandler`, siguen disponibles los métodos atómicos de ciclo de vida `ForgeApi.RegisterWhenAvailable` y `ForgeApi.UnregisterWhenAvailable`. La entrega administrada comprueba la generación del registro capturada; la baja omite entregas pendientes y espera a que termine un callback que ya estaba en curso. Los callbacks son síncronos y se ejecutan fuera del bloqueo global de disponibilidad: en el hilo que registra si ya hay un host, o en el hilo que realiza la conexión. Pueden darse de baja a sí mismos. No bloquees un callback esperando a otros hilos que puedan registrar o dar de baja callbacks administrados. Consulta [ForgeWeave](FORGEWEAVE.es.md) para ver los datos de eventos, permisos, orden, replay y límites de despacho.

Los callbacks de disponibilidad administrados se ejecutan fuera del bloqueo del SDK. La entrega comprueba el ciclo de vida del suscriptor y la generación de conexión; si un callback vuelve a entrar en el ciclo de conexión y reemplaza el registro, se omite el resto de esa instantánea obsoleta. Dar de baja un callback administrado antes de su turno también evita su entrega.

`SharedServiceMonitor<T>.Changed` conserva una instantánea tipada de sus manejadores, que se reconstruye cuando cambian los suscriptores. Las notificaciones recorren esa instantánea en orden de suscripción y mantienen el aislamiento de excepciones por manejador, sin reconstruir la lista de invocación del delegado en cada cambio de servicio.

## 3. ForgeAgentMemory — niveles acotados en memoria

`ForgeAgentMemory` es un almacén seguro para concurrencia y local al proceso para hechos, experiencias y tácticas de agentes. No serializa datos en las partidas guardadas de Bannerlord. `ClearAll()` vacía todos los niveles; `ClearAgent(agentId)` limpia ese ID en todos los niveles. Cada nivel también dispone de su propia operación `ClearAgent`.

Los tres niveles comparten un máximo de **2,048 IDs de agente distintos**. El conteo es la unión de los IDs que tienen datos en cualquier nivel. Al alcanzar ese límite se rechaza un ID nuevo en todos los niveles; los IDs existentes aún pueden actualizar entradas dentro de las cuotas de cada nivel.

`SemanticMemory`, `EpisodicMemory` y `ProceduralMemory` siguen siendo construibles públicamente para conservar compatibilidad de código fuente, pero sus instancias comparten el mismo almacenamiento del proceso. Crear otro objeto de nivel no permite acumular memoria sin contabilizar, eludir la cuota global ni dejar datos después de `ForgeAgentMemory.ClearAgent`/`ClearAll`.

| Nivel | Capacidad exacta | Al alcanzar el límite | TTL |
|---|---|---|---|
| Hechos semánticos | 128 hechos por agente | Se pueden actualizar claves existentes. Se rechaza un hecho nuevo. | Opcional por hecho; es el único nivel con TTL. |
| Experiencias episódicas | 512 episodios por agente y 128 por `(agentId, type)` | Se expulsan en FIFO los episodios más antiguos del agente para respetar el límite total; cuando el tipo entrante está lleno, también se expulsa su episodio más antiguo según haga falta. | Ninguno. |
| Tareas procedurales | 128 tareas por agente | Se pueden actualizar tareas existentes. Se rechaza una tarea nueva. | Ninguno. |

Los límites públicos están disponibles en `ForgeAgentMemory.MaximumAgents`, `MaximumSemanticEntriesPerAgent`, `MaximumEpisodicEntriesPerAgent`, `MaximumEpisodicEntriesPerType` y `MaximumProceduralEntriesPerAgent`. Usa los métodos `Try*` cuando el rechazo por capacidad sea un resultado esperado. `Semantic.TryUpsert` y `Procedural.TryAdd` devuelven `false` si una clave nueva o un ID de agente supera una cuota; las claves existentes se pueden actualizar. `Episodic.TryAdd` solo devuelve `false` si el límite global rechaza un ID nuevo. La expulsión FIFO episódica cuenta como escritura aceptada y no hace que `TryAdd` falle. Los métodos heredados `Upsert`/`Add` conservan sus firmas y lanzan `InvalidOperationException` si la capacidad rechaza una escritura.

```csharp
bool storedFact = ForgeAgentMemory.Semantic.TryUpsert(
    agentId, "faction_loyalty", "empire", TimeSpan.FromHours(24));

bool storedEpisode = ForgeAgentMemory.Episodic.TryAdd(
    agentId, "battle", new { Outcome = "victory" });

bool storedTactic = ForgeAgentMemory.Procedural.TryAdd(
    agentId, "cavalry_charge", new { Preferred = true });

string faction = ForgeAgentMemory.Semantic.Get<string>(agentId, "faction_loyalty");
IReadOnlyList<string> facts = ForgeAgentMemory.Semantic.GetKeys(agentId);
List<object> battles = ForgeAgentMemory.Episodic.GetAll(agentId, "battle");
int episodeCount = ForgeAgentMemory.Episodic.TotalCount(agentId);
```

El TTL semántico comienza al invocar `Upsert`/`TryUpsert` y usa UTC. Los hechos vencidos devuelven el valor predeterminado y se eliminan al leerlos con `Get<T>` o enumerarlos con `GetKeys`. Si omites el TTL, el hecho se conserva hasta sobrescribirlo o limpiarlo. Los TTL cero y negativos ya están vencidos. El cálculo del TTL trata una duración no positiva como vencida de inmediato, sin sumarla a la marca de tiempo actual; una duración positiva demasiado grande para representarse se limita a `DateTimeOffset.MaxValue` en vez de desbordarse. Los datos episódicos y procedurales nunca expiran automáticamente.

`Semantic.GetResult<T>(agentId, key)` y `Procedural.GetResult<T>(agentId, task)` devuelven valores inmutables `ForgeMemoryReadResult<T>` con `Found`, `Missing` o `TypeMismatch`; las lecturas semánticas también pueden devolver `Expired`. Una lectura semántica elimina los hechos vencidos, igual que `Get<T>`. La memoria procedimental no tiene TTL y nunca devuelve `Expired`. Las firmas actuales de `Get<T>` y su valor predeterminado cuando falta la clave no cambian.

## 4. ForgeLocalApi — servidor REST local

`ForgeLocalApi` proporciona un servidor HTTP ligero para herramientas locales:

```csharp
var api = new ForgeLocalApi();
api.Start("http://localhost:59999/");
// ...
api.Stop();
api.Dispose(); // o usa 'using'
```

| Endpoint | Respuesta |
|---|---|
| `GET /info` | Versión del servidor, versión actual de `ForgeApi.Version` bajo `sdk` y estado. |
| `GET /status` | Estado de ejecución. |
| `GET /agents` | JSON con estadísticas agregadas de memoria. Conserva el arreglo `agents` vacío y devuelve únicamente `statistics.agentCount`, `semanticEntries`, `episodicEntries` y `proceduralEntries`; no expone IDs de agentes, claves ni datos almacenados. |

## 5. Puente de compatibilidad ForgeCampaignEvents

El puente heredado `Subscribe(ForgeEvent, Action<object[]>)` aísla las excepciones de callbacks y las registra mediante `ForgeApi.Logger`. Combínalo con `Unsubscribe(eventType, callback)` o `ClearSubscribers()` para liberar callbacks estáticos. Las suscripciones de delegados específicas de ForgeWeave deben usar `SubscribeWeaveWhenAvailable` cuando el módulo deba sobrevivir desconexiones y reconexiones del anfitrión; el `ForgeWeaveRegistration` devuelto es el propietario del ciclo de vida.

## 6. Reemplazo explícito y experimental de métodos

La capacidad opcional `IForgePatchService` está disponible como `ForgeApi.Patches`. Se mantiene separada de `IForgeRegistry`, así que añadir soporte de ciclo de vida de parches no obliga a los implementadores existentes del registro a agregar métodos. `ForgeApi.Version` es una constante de compilación; usa `ForgeApi.Patches != null` para comprobar si el anfitrión conectado proporciona realmente esta capacidad. `IForgePatchServiceLifecycle` es una segunda capacidad opcional para anfitriones que pueden reutilizar la misma instancia del servicio después de reconectar; se mantiene separada para conservar la compatibilidad de código fuente de los servicios de parcheo existentes.

Usa un ID de parche estable y una etiqueta de propietario explícitos para instalar un reemplazo de método uno a uno:

```csharp
var patches = ForgeApi.Patches;
if (patches == null)
    throw new InvalidOperationException("El anfitrión Forge conectado no ofrece el servicio de parches.");

IForgePatchHandle handle = patches.ApplyMethodReplacement(
    "my_module.hero_name",
    "MyModule",
    typeof(Hero).GetMethod("GetName"),
    typeof(MyPatch).GetMethod("PatchedGetName"));

ForgePatchVerification verification = handle.Verify();
ForgePatchRevertResult result = handle.Revert();
// Dispose solicita una reversión idempotente de mejor esfuerzo; conserva el handle para el ciclo de vida y diagnóstico.
```

El handle expone una `Snapshot` inmutable, `Verify()` y `Revert()`. `Dispose()` solicita una reversión de mejor esfuerzo y puede llamarse más de una vez. `IForgePatchService.GetSnapshots(owner)` devuelve instantáneas inmutables; `Verify(patchId)`, `Revert(patchId)`, `RevertOwner(owner)` y `RevertAll()` ofrecen operaciones explícitas de estado y limpieza. Los resultados informan `Applied`, `Reverted`, `Conflict` o `Failed`; el propietario es una etiqueta de seguimiento, no un límite de autorización. Al desconectarse, el servicio deja de aceptar parches nuevos e intenta revertir sus parches en orden inverso de aplicación. Los handles existentes siguen pudiendo consultarse o liberarse después de la desconexión. Al reconectar, el servicio integrado solo vuelve a abrir solicitudes cuando cada registro anterior y sus bytes originales se verifican como revertidos y ningún destino conserva un detour rastreado; los conflictos o estados inciertos lo mantienen cerrado.

La reversión solo se permite si los bytes del destino aún coinciden con exactitud con los bytes de salto instalados por Forge. Si otro componente modificó el destino, Forge registra un conflicto y deja intactos los bytes ajenos. La restauración de la protección de página y `FlushInstructionCache` se comprueban; un fallo se informa y no se trata como una aplicación o reversión exitosa. Se rechaza antes de llamar a `VirtualProtect` cualquier escritura que cruce el límite de una página del sistema, porque este backend restaura un único valor de protección original para todo el tramo.

Este escritor de bajo nivel sigue siendo experimental. Ni los cambios de protección de página ejecutable ni el vaciado de la caché de instrucciones coordinan los demás hilos que pudieran estar ejecutando el destino. El backend actual no garantiza hot patching concurrente seguro; úsalo en un fixture desechable y aislado, y asegúrate de que ningún hilo pueda ejecutar el método mientras se modifica su código. No lo uses como framework de parcheo de producción. El campo owner es solo descriptivo. Declaraciones de hook de preflight como `Transpiler` y `Finalizer` no están implementadas por el backend de reemplazo de métodos.

`ForgeLivePatcher` y las utilidades de desvío anteriores permanecen como superficies de compatibilidad. `ApplyDetour(original, replacement)` usa la ruta de reemplazo de bajo nivel de Forge; `ApplyPatch`/`RevertPatch` solo emiten sus eventos de solicitud correspondientes para un consumidor registrado. Forge no incluye Harmony ni convierte automáticamente entradas declarativas de blueprints en hooks de ejecución. La capacidad separada `ForgeApi.Hooks` admite callbacks Prefix, Postfix y Finalizer registrados explícitamente. El adaptador local de transpiler IL exclusivo de Core es una capacidad separada para `net472`; no forma parte del backend de reemplazo uno a uno ni de las utilidades antiguas de desvío, e `ILContext` no se expone mediante el SDK o IPC.

## 7. Hooks explícitos y transpilers IL exclusivos de Core

La capacidad opcional `IForgeHookService` está disponible como `ForgeApi.Hooks`. Es independiente del registro declarativo de Patch Blueprints y del servicio de reemplazo uno a uno disponible en `ForgeApi.Patches`; agregarla no añade miembros a `IForgeRegistry`. Los anfitriones que deban reabrir la misma instancia del servicio tras reconectar pueden implementar la capacidad opcional e independiente `IForgeHookServiceLifecycle`. `ForgeApi.Version` es una constante de compilación, así que comprueba `ForgeApi.Hooks != null` en ejecución para determinar si el anfitrión conectado proporciona esta capacidad. Las definiciones y los delegados de hooks permanecen dentro del proceso y no se transportan por IPC.

Registrar una definición solo guarda metadatos y callbacks. No instala un detour; aplicar, verificar y revertir son operaciones explícitas:

```csharp
var hooks = ForgeApi.Hooks;
if (hooks == null)
    throw new InvalidOperationException("El anfitrión Forge conectado no ofrece soporte de hooks de ejecución.");

var targetMethod = ResolveExactTarget(); // El módulo debe devolver un MethodInfo admitido y sin ambigüedad.
IForgeHookHandle handle = hooks.Register(new ForgeHookDefinition
{
    Id = "my_module.hero_name",
    Owner = "MyModule",
    Target = targetMethod,
    Prefix = invocation => { /* inspecciona o actualiza argumentos compatibles */ },
    Postfix = invocation => { /* inspecciona o actualiza el resultado */ },
    Finalizer = invocation => { /* conserva, sustituye o suprime una excepción pendiente */ }
});

ForgeHookOperationResult applied = handle.Apply();
ForgeHookOperationResult verified = handle.Verify();
ForgeHookOperationResult reverted = handle.Revert();
// Dispose también solicita una reversión de mejor esfuerzo; conserva el handle para estado y diagnóstico.
```

`ForgeHookInvocation` expone la instancia de destino y un arreglo de argumentos. Un Prefix puede actualizar argumentos compatibles, asignar `RunOriginal = false` y proporcionar un `Result` compatible con el tipo; un Postfix puede inspeccionar o reemplazar un resultado compatible después de que regrese el método original. Los callbacks se ejecutan sincrónicamente en el hilo que invoca el destino. Sin Finalizer, un fallo del callback Prefix continúa con la llamada original, un fallo del callback Postfix conserva el resultado original y un fallo del método original se propaga sin ejecutar Postfix. Con Finalizer, los fallos de Prefix, del método original o de Postfix se exponen en `ForgeHookInvocation.Exception`; Postfix sigue ejecutándose solo tras completar correctamente Prefix y el método original. Una excepción pendiente sin cambios se vuelve a lanzar conservando su identidad y pila original; Finalizer puede sustituirla o suprimirla, y suprimir una excepción en un destino no `void` requiere un `Result` compatible. Si Finalizer falla mientras hay otro fallo pendiente, ambos se conservan en un `AggregateException`. La puerta de callbacks del anfitrión puede omitir Finalizer, por lo que no garantiza limpieza incondicional. No bloquees estos callbacks ni supongas que se ejecutan en el hilo del juego.

`GetSnapshots(owner)` devuelve snapshots puntuales; el servicio también expone `Apply`, `Verify`, `Revert`, `RevertOwner` y `RevertAll`. Los resultados identifican `Registered`, `Applied`, `Reverted`, `Conflict`, `Failed` o `Unsupported`. El propietario es una etiqueta de seguimiento, no un límite de autorización. `Before`, `After` y `Priority` se envían como metadatos de orden de MonoMod RuntimeDetour en el destino compatible del juego. El registro rechaza firmas no admitidas, incluidos genéricos abiertos, constructores, métodos abstractos, P/Invoke, llamadas internas y métodos varargs, parámetros o retornos by-ref o de puntero, tipos byref-like, destinos de instancia cuyo tipo sea un value type y destinos con más de 12 parámetros.

El anfitrión integrado de Bannerlord solo permite Apply/Revert en la pantalla exacta del menú principal, desde el hilo del juego y sin campaña, misión ni sesión multijugador activos. El backend RuntimeDetour ejecutable está incluido para el destino `net472` del módulo; aplicar hooks en `net8.0` no está admitido. El registro sigue siendo inerte en todos los destinos. Al desconectarse, el servicio intenta revertir sus hooks aplicados en orden inverso y conserva los registros no resueltos; rechaza la reconexión mientras algún hook siga activo, en conflicto o incierto.

Esta ruta de hooks de ejecución es experimental. Las pruebas de fixture son seriales y no demuestran seguridad si otro hilo ejecuta el destino durante Apply o Revert. Mantén los destinos inactivos durante los cambios, valida con fixtures aislados y no consideres esta capacidad una garantía de hot-patching de producción. Forge no incluye Harmony.

La manipulación avanzada de IL es independiente de los callbacks del SDK. Solo `CalradiaForge.Core` compilado para el destino Bannerlord `net472` expone `ForgeHookService.RegisterTranspiler(ForgeHookDefinition, MonoMod.Cil.ILContext.Manipulator)`, respaldado por `ILHook` de MonoMod. Una definición transpiler no puede contener también callbacks Prefix, Postfix o Finalizer. El registro es inerte y Patch Blueprint Preflight no ejecuta el manipulador. Apply y las reconstrucciones posteriores de la cadena IL pueden ejecutarlo repetidas veces, incluso desde el hilo de otro llamador después de un Apply autorizado. El manipulador debe tolerar ejecuciones repetidas y usar solo el IL proporcionado y configuración estable; no debe acceder al estado vivo del juego. `ILContext` y los delegados ejecutables nunca cruzan contratos del SDK ni IPC. El cuerpo transformado permanece activo fuera del menú hasta un Revert/Undo explícito y verificado; la puerta de callbacks de ejecución no protege las invocaciones del IL transformado. Este backend sigue siendo experimental: los fixtures no demuestran mutación concurrente segura ni seguridad con llamadas en curso.

## 8. Versión del contrato de ForgeApi

El contrato 13 añade el contrato de callbacks Finalizer y metadatos de snapshots de hooks compatibles con versiones anteriores. El adaptador ILHook local de Core `net472` permanece fuera del contrato de callbacks del SDK. El contrato 12 añadió la superficie opcional de hooks de ejecución Prefix/Postfix y el contrato 11 añadió el servicio opcional e independiente de reemplazo de métodos. Como la versión es una constante de compilación incrustada en el ensamblado consumidor, comprueba la capacidad opcional en ejecución en vez de usar una comparación de versión:

```csharp
if (ForgeApi.Hooks == null)
    throw new InvalidOperationException("El anfitrión conectado no ofrece soporte de hooks de ejecución.");
```

| Contrato | Código fuente del producto | Incorporación relevante |
|---|---|---|
| 13 | 25.2.0 | Contrato de callback Finalizer y metadatos de snapshots compatibles; el adaptador transpiler local de Core `net472` usa MonoMod `ILHook` sin exponer `ILContext` al SDK ni IPC. |
| 12 | 25.2.0 | `IForgeHookService` opcional para callbacks Prefix/Postfix registrados explícitamente; Apply, Verify y Revert son explícitos, con MonoMod RuntimeDetour en el destino Bannerlord `net472`. |
| 11 | 25.0.0 | `IForgePatchService` opcional con handles explícitos para reemplazo de métodos, verificación y reversión con detección de conflictos. |
| 10 | 25.0.0 | Resultados explícitos de guardado seguro, informes acotados de auto-registro, lecturas tipadas de memoria semántica/procedimental y notificaciones de disponibilidad seguras por generación. |
| 9 | 24.0.0 | Registros de `ForgeModelRegistry` con propietario; las condiciones de modificadores se ejecutan sobre una instantánea fuera del bloqueo del registro. |
| 8 | 24.0.0 | Registro administrado de disponibilidad ForgeWeave y `ForgeAgentMemory` acotado; el TTL solo se aplica a la memoria semántica. |
| 7 | 23.0.0 | Resolución de servicios compartidos, publicación transaccional y monitoreo. |

`ForgeData.RemoveForgeData<T>(entity)` también elimina la entrada externa de datos de la entidad cuando se quita el último valor tipado. Comprueba el diccionario actual bajo su bloqueo por entidad antes de retirarlo, para no perder inserciones simultáneas del SDK.

El endpoint `/agents` ofrece diagnósticos agregados. Calcula conteos de los niveles de memoria y deliberadamente no expone IDs de agentes, claves semánticas, tipos de episodios ni objetos arbitrarios almacenados.

`ForgeModelRegistry.GetModifiers(category)` devuelve una instantánea inmutable en caché con solo la categoría solicitada. Los registros, reemplazos y bajas invalidan la caché; las instantáneas ya entregadas permanecen estables. `Evaluate` reutiliza la instantánea de categoría e invoca las condiciones de los modificadores fuera del bloqueo del registro. Son mejoras internas de asignaciones y recorrido; no cambia el orden de registro ni el comportamiento del cálculo.

## 9. ModSettings

```csharp
var settings = ModSettings.Register("MyMod", new MySettings { Volume = 1.0f });
settings.Volume = 0.8f;
ModSettings.Save("MyMod", settings);
var current = ModSettings.Get<MySettings>("MyMod");
```

`Register` y `Save` tratan `modId` como un solo componente de nombre de archivo, no como una ruta. Rechazan IDs vacíos, separadores de directorio, rutas absolutas o con unidad, sintaxis de flujos alternativos, caracteres no válidos en nombres de archivo y caracteres de control antes de leer o escribir. La ruta JSON resultante se canonicaliza y se comprueba que permanezca bajo `Documentos/Mount and Blade II Bannerlord/Configs/ModSettings`. Los nombres seguros, incluidos espacios y Unicode, se conservan. `Get<T>` solo consulta la caché en memoria y no resuelve una ruta de archivo.

`TrySave<T>` devuelve un `ModSettingsSaveResult` con `Saved`, `SerializerUnavailable`, `SerializationFailed` o `StorageFailed`. Rechaza ajustes nulos antes de serializar, escribe un archivo temporal en el mismo directorio y luego mueve o reemplaza el destino de forma atómica. La caché solo se actualiza después de confirmar el archivo; un fallo de serialización o almacenamiento conserva el archivo y el valor en caché anteriores. En Windows, los IDs de módulo comparten identidad de caché y confirmación sin distinguir mayúsculas. Los serializadores y deserializadores del usuario se ejecutan fuera del bloqueo de confirmación por módulo; si una operación concurrente o reentrante más reciente confirma cambios mientras `Register<T>` carga, la carga anterior no los sobrescribe. `Save<T>` conserva su firma y registra los fallos. Si no hay serializador, `Register<T>` sigue devolviendo los valores predeterminados proporcionados, pero no crea un archivo marcador que aparente haberlos guardado.
