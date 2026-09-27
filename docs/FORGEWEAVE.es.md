# ForgeWeave

Esta página describe el código fuente de Calradia Forge 25.0.0 y la versión 10 del contrato del SDK. Los ZIP de distribución existentes no se regeneraron y no acreditan que las API del código fuente v25.0.0 estén presentes.

ForgeWeave es el framework independiente de extensiones de Calradia Forge. Es un sistema cooperativo de eventos, no una biblioteca de parches: no descubre métodos, no genera IL, no reemplaza callbacks y no carga otro framework de mods.

Cada manejador declara su identidad, contexto, permiso, prioridad, orden, límite de fallos e intervalo mínimo cuando usa pulsos. Forge registra su salud y duración sin presentar una coincidencia como conflicto.

## Qué ofrece

- Orden determinista mediante `Before` y `After` visibles.
- Protección de contexto, modo de pruebas y copia de campaña para manejadores que cambian estado.
- Aislamiento de excepciones y cuarentena del único manejador que falla repetidamente.
- Datos de evento escalares y acotados; no objetos vivos de campaña, misión o agentes.
- Filtros declarativos de coincidencia exacta sobre esos datos copiados, sin predicados arbitrarios.
- Presupuestos de ejecución opcionales que señalan manejadores lentos sin abortar ni poner en cuarentena código arbitrario.
- Duración, conteos, estado de salud y diario de despachos por extensión.
- Consulta coherente desde el panel, la aplicación de escritorio, JSON y HTML.

No pretende sustituir universalmente una biblioteca de parches. Sirve para código cooperativo que puede ejecutarse desde los ciclos de vida oficiales que Forge ya recibe.

## Registro y eventos

Registra un `IForgeEventHandler` desde una devolución de `ForgeApi.RegisterWhenAvailable(...)` y anula la suscripción con `ForgeApi.UnregisterWhenAvailable(...)` al descargar el módulo. La ayuda comprueba y suscribe de forma atómica, por lo que la carga no puede perder la conexión de Forge entre una comprobación nula y una suscripción. El evento `Available` anterior continúa solo por compatibilidad. El ejemplo `examples.forgeweave.ready` está en [`Examples.cs`](../examples/CalradiaForge.Examples/Examples.cs).

### Ciclo de vida administrado de suscripciones de delegados (contrato SDK 9)

Para un manejador basado en delegado, `ForgeCampaignEvents.SubscribeWeaveWhenAvailable` devuelve un `ForgeWeaveRegistration`. El objeto administra tanto la devolución pendiente de disponibilidad como el manejador ForgeWeave activo. Consérvalo durante el ciclo de vida del módulo y llama a `Dispose()` desde `OnSubModuleUnloaded`:

```csharp
private ForgeWeaveRegistration _registration;

protected override void OnSubModuleLoad()
{
    _registration = ForgeCampaignEvents.SubscribeWeaveWhenAvailable(
        "my_module.startup_observer",
        ForgeEventKind.ForgeReady,
        OnForgeReady,
        access: ForgeEventAccess.Observe);
}

protected override void OnSubModuleUnloaded()
{
    _registration?.Dispose();
    _registration = null;
}

private void OnForgeReady(ForgeEvent @event) { }
```

La ayuda comprueba atómicamente la disponibilidad del anfitrión y se registra sincrónicamente si Forge ya está conectado. De lo contrario, espera sin perder una conexión que llegue durante el inicio del módulo. Una desconexión anula el registro del manejador activo y devuelve el objeto a `WaitingForHost`; una conexión posterior lo registra de nuevo. Las devoluciones de registro se ejecutan sincrónicamente en el hilo de conexión del anfitrión. `Dispose()` impide registros posteriores y libera el manejador activo. El registro pendiente se puede liberar desde cualquier hilo; una vez activo el manejador, llama a `Dispose()` desde el mismo hilo que lo registró. Si se invoca desde otro hilo, lanza `InvalidOperationException`, mantiene activo el registro y guarda el error para reintentar en el hilo correcto.

| `ForgeWeaveRegistrationState` | Significado |
|---|---|
| `WaitingForHost` | Espera un anfitrión de eventos compatible. |
| `Registered` | El manejador está activo con el anfitrión actual. |
| `HostUnsupported` | Hay un anfitrión conectado, pero no admite esta ruta de registro. |
| `Failed` | Falló el registro; consulta `Error`. |
| `Disposed` | El propietario liberó el objeto de registro. |

`ForgeWeaveRegistration.State`, `.Error` y `.Handler` exponen el estado actual, el último error y el `IForgeEventHandler` activo si existe. `SubscribeWeave` sigue siendo una API de registro inmediato y lanza `InvalidOperationException` cuando `ForgeApi.Events` no está disponible o si se invoca fuera del hilo de conexión del anfitrión; usa `UnsubscribeWeave` en el hilo de conexión para liberar su manejador. El objeto administrado sigue la disponibilidad y las reconexiones; la llamada inmediata requiere un anfitrión conectado.

Los eventos iniciales son `ForgeReady`, `InitialScreenReady`, `ContextEntering`, `ContextLeaving`, `CampaignStarted`, `GameLoaded`, `MissionInitialized`, `MissionEnded`, `AgentCreated`, `AgentRemoved`, `Pulse` y `Custom`. Se originan en callbacks oficiales de Bannerlord que recibe el adaptador de Forge (o mediante publicación programática con `ForgeApi.PublishCustomEvent`) y se despachan desde el ciclo principal de Forge. `ContextLeaving` conserva su contexto lógico de salida aunque el anfitrión ya haya entrado al siguiente. La cola admite 128 eventos y se procesan como máximo 16 por actualización.

`ForgeEvent.Data` es una copia de solo lectura de pares de texto, limitada a 32 valores. No expone servicios, objetos vivos ni un resolvedor general de objetos del juego.

### Malla de Eventos Personalizados entre Mods (`ForgeEventKind.Custom`)

Además de los callbacks del ciclo de vida del anfitrión, ForgeWeave proporciona una malla desacoplada de eventos entre mods mediante `ForgeEventKind.Custom`. Cualquier módulo que referencie el SDK puede publicar eventos personalizados sin acoplamiento directo entre ensamblados.

- **Publicación de Eventos:**
  Invoca `ForgeApi.PublishCustomEvent(topic, data)` o `IForgeEventRegistry.PublishCustom(topic, data)`. La llamada valida el topic y encola el evento para su despacho en el hilo principal del juego.
- **Semántica de Coincidencia de Topics:**
  Los suscriptores declaran su patrón de topic en `ForgeEventSubscription.Topic`:
  - **Coincidencia Exacta:** `Topic = "economy.trade.caravan_arrived"` solo coincide con despachos para dicho topic exacto.
  - **Comodín de Prefijo:** `Topic = "economy.*"` coincide con cualquier topic que comience con `"economy."` (p. ej., `"economy.trade"`, `"economy.market.crisis"`).
  - **Captura Total (Catch-All):** `Topic = "*"` (o nulo/omitido) coincide con todos los eventos personalizados de la malla.
- **Encolado y Seguridad:**
  Los eventos personalizados comparten la cola acotada de despacho de Forge (hasta 128 eventos encolados, procesando como máximo 16 por fotograma). Las cargas útiles se validan según las restricciones escalares (hasta 32 pares, claves <= 64 caracteres, valores <= 512 caracteres).

### Anulación Dinámica de Registro

Las extensiones pueden anular el registro de manejadores en tiempo de ejecución mediante `IForgeEventRegistry.Unregister(string id)` o `IForgeEventRegistry.Unregister(IForgeEventHandler handler)`. Esto libera limpiamente el identificador y el hueco de manejador sin reiniciar la sesión ni perturbar a otros módulos.

### Filtros declarativos

Usa `ForgeEventSubscription.Filter.RequiredData` para recibir solo eventos cuyo contenido escalar copiado tenga los pares indicados. Usa `ForgeEventSubscription.Filter.ExcludedData` para exclusiones declarativas negativas: si cualquier par excluido está presente en los datos del evento, se descarta inmediatamente. Un mapa vacío coincide con todos los datos. Forge valida y copia ambos mapas al registrar el manejador: admite como máximo ocho pares por mapa, claves de 64 caracteres y valores de 512, con comparación ordinal. Una discrepancia produce el resultado `Filtered`, no invoca ni autoriza el manejador y no cuenta como fallo. Durante Replay Lab se vuelven a comprobar los mismos filtros contra los datos conservados.

Establece `BudgetMilliseconds` entre 1 y 5.000 para hacer visibles los callbacks lentos. La política predeterminada `ForgeBudgetPolicy.Warn` registra `OverBudget` y la duración después de que termine el callback; `Ignore` conserva la medición pero no cuenta el exceso. Es una señal diagnóstica: Forge no puede interrumpir código C# arbitrario ni convierte el exceso en fallo o cuarentena. Los registros de salud del manejador y del evento registran `MinMilliseconds`, `MeanMilliseconds` y `MaxMilliseconds`.

## Laboratorio de replay

Replay Lab es verificación controlada de eventos, no intercepción de métodos. Forge conserva como máximo 64 registros de eventos del **anfitrión** después de que termina su despacho. Cada registro contiene su secuencia de origen, tipo de evento, contexto lógico, duración, datos escalares copiados y resultado del despacho original. No conserva objetos, servicios ni un replay como nueva fuente de replay.

El replay empieza desactivado para todos los manejadores. Cada manejador debe declarar `ForgeEventSubscription.ReplayMode` de forma explícita:

- `Disabled` no recibe replays.
- `ObserveOnly` se reserva para manejadores de verificación sin escritura.
- `Live` permite un replay intencional, pero no concede permisos adicionales: el contexto actual debe coincidir exactamente con el registro conservado y cualquier manejador `CampaignWrite` o `MissionWrite` conserva las protecciones de contexto, modo de pruebas y copia de campaña.

Durante un replay, `ForgeEvent.IsReplay` es `true` y `ForgeEvent.SourceSequence` identifica el evento original. El replay tiene su propia secuencia de despacho. Los manejadores pueden usar esos campos para evitar trabajo externo duplicado. Forge registra como resultado el origen rechazado, la secuencia ausente o caducada, la diferencia de contexto, la falta de un manejador que aceptó el replay o el rechazo de una protección de escritura; no reintenta automáticamente.

`ForgeApi.Replays.Records` devuelve copias separadas de los registros conservados. `ForgeApi.Replays.Replay(sequence, services, cancellation)` acepta solamente una secuencia retenida, por lo que una extensión no puede inyectar datos ni callbacks arbitrarios. La acción local `replay` usa la misma regla. En el Marco nativo, escribe la secuencia conservada en **Buscar / argumento** y usa **Replay**. En escritorio, selecciona una fila de evidencia y ejecuta su comando de replay. Los informes muestran evidencia conservada, estado, duración y resultados por manejador.

## Pulso, orden y permisos

Solo `Pulse` acepta `MinimumIntervalMilliseconds`; debe ser de 250 a 60,000 ms. Forge ya limita el pulso del anfitrión y cada manejador tiene una comprobación independiente para evitar trabajo por fotograma accidental.

La prioridad mayor se ejecuta primero. `Before` y `After` solo refinan el orden cuando ambos manejadores usan el mismo evento y prioridad. Si falta una referencia, cruza evento o prioridad, se referencia a sí misma o forma un ciclo, Forge bloquea ese manejador en vez de adivinar. Los manejadores independientes siguen funcionando.

`Observe` y `Diagnostics` son de solo lectura. `CampaignWrite` exige `ChangesState=true` y contexto de campaña; `MissionWrite` exige `ChangesState=true` y contexto de misión. Los manejadores que cambian estado pasan por el mismo modo de pruebas y protección de copia de campaña que las pruebas de Forge.

## Fallos e inspección

Una excepción no detiene los demás manejadores. Se registra un aviso acotado y se actualiza la salud de dicho manejador. Los planes de eventos distintos de `Custom` se reutilizan hasta que cambia el registro de manejadores; los temas abiertos de `Custom` se planifican en cada despacho y no se guardan en caché. Las altas y bajas invalidan los planes retenidos. `Snapshot()` también reutiliza el plan vigente de cada evento finito y copia sus hallazgos actuales al informe; la planificación de temas personalizados no se almacena en caché. Los datos de salud, diario, replay y eventos del snapshot siguen copiándose desde el estado actual.

### Circuit Breaker Inteligente con Auto-Recuperación

Los manejadores pueden configurar su política de resiliencia mediante `ForgeEventSubscription.CircuitBreakerPolicy`:

- **`ForgeCircuitBreakerPolicy.AutoRecover` (Predeterminado):**
  Cuando un manejador alcanza su `FailureLimit` (de 1 a 5 excepciones consecutivas, por defecto 3), el circuito pasa de `Closed` (Cerrado) a `Open` (Abierto/en cuarentena). Mientras está en `Open`, todos los eventos entrantes se omiten de forma segura.
  Una vez transcurrido `CircuitBreakerCooldownSeconds` (por defecto 10 s, configurable entre 1 s y 300 s), el siguiente despacho transiciona el circuito a un estado probatorio `HalfOpen` (Semiabierto) y ejecuta una sonda de prueba:
  - **Sonda Exitosa:** El circuito regresa a `Closed`, los contadores de fallos se restablecen a 0, se levanta la cuarentena y el periodo de enfriamiento vuelve a su valor inicial.
  - **Sonda Fallida:** El circuito vuelve a `Open`, el manejador regresa a cuarentena y un retroceso exponencial duplica el periodo de enfriamiento (p. ej., 10 s → 20 s → 40 s → máximo 60 s).
- **`ForgeCircuitBreakerPolicy.PermanentQuarantine`:**
  Mantiene el comportamiento tradicional de cuarentena estricta: al alcanzarse `FailureLimit`, el manejador permanece en cuarentena indefinidamente hasta que un operador lo restablezca explícitamente o se recargue la sesión.

Los manejadores en cuarentena también pueden restablecerse manualmente de forma individual, por módulo (`ResetModuleQuarantines`) o globalmente (`ResetAllQuarantines`).

### Telemetría APM de Latencia Acotada

ForgeWeave monitoriza métricas de rendimiento de aplicaciones (APM) por manejador. El registro del despacho escribe en un búfer preasignado de 64 muestras; al consultar percentiles se copia y ordena esa ventana acotada, por lo que la ruta de informes sí puede generar asignaciones:

- **Ventana de Muestreo Circular:** Un búfer circular fijo de 64 elementos (`double[64]`) registra la latencia de ejecución en milisegundos.
- **Percentiles de Latencia:** Calcula percentiles de latencia P50 (mediana), P95 y P99 bajo demanda.
- **Contadores de Distribución de Histograma:**
  - `< 1.0 ms`: Ejecuciones submilimétricas (`BucketUnder1Ms`)
  - `1.0 ms - 5.0 ms`: Operaciones rápidas (`Bucket1To5Ms`)
  - `5.0 ms - 20.0 ms`: Operaciones moderadas (`Bucket5To20Ms`)
  - `> 20.0 ms`: Operaciones pesadas (`BucketOver20Ms`)
- Todas las métricas se exponen en `ForgeWeaveHandlerHealth`, en las herramientas de consola del desarrollador y en el Workbench de escritorio.

`StopPropagation` solo detiene manejadores ForgeWeave posteriores del evento actual; nunca bloquea el callback original de Bannerlord.

La acción de protocolo `framework` devuelve el estado, orden, salud, tiempos, hallazgos, evidencia de replay y diario reciente. `event-journal` devuelve solo ese diario acotado. `replay` ejecuta una única secuencia conservada dentro del juego y devuelve su resultado protegido. Las acciones `forgeweave-unquarantine` y `forgeweave-clear` gestionan la cuarentena y los diarios por tubería nombrada.

### Comandos de Consola del Desarrollador

ForgeWeave expone comandos nativos en la consola del juego (`ALT + ~`):

- `cf.forgeweave.status` — estado general de manejadores registrados, activos, bloqueados y en cuarentena.
- `cf.forgeweave.handlers [modulo]` — lista los manejadores registrados y sus métricas de ejecución.
- `cf.forgeweave.handler <id>` — inspección detallada de un manejador específico, incluyendo su patrón de Topic, estado del circuito (`Closed`, `Open`, `HalfOpen`), estado del enfriamiento con retroceso, percentiles P50/P95/P99 y distribución en histograma. El cálculo de percentiles copia y ordena la ventana de muestras acotada.
- `cf.forgeweave.publish <topic> [clave=valor ...]` — publica un evento personalizado entre módulos en la malla de eventos de ForgeWeave.
- `cf.forgeweave.unquarantine <id|all|module:nombre>` — restablece la cuarentena de manejadores.
- `cf.forgeweave.replay <secuencia>` — repite una secuencia conservada en el juego.
- `cf.forgeweave.journal [limite]` — muestra los despachos recientes con sus tiempos y resultados.
- `cf.forgeweave.clear [journal|replays|all]` — limpia los historiales acotados de diario o repeticiones.

El botón **Marco / Framework** del panel y la sección **Marco / Framework** de escritorio muestran esa información. Los informes exportados capturan una instantánea de memoria actual sin reflexión ni recorrido de objetos del juego.

ForgeWeave acepta hasta 256 manejadores y 32 referencias de orden por manejador. Los filtros son una ampliación aditiva del SDK v3; los consumidores existentes de pruebas, comandos, diagnósticos, bibliotecas compartidas y Patch Blueprint siguen siendo compatibles. No depende de Harmony, MCM, ButterLib ni otro mod externo.
