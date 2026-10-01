# SDK v3

Para las API tipadas y versionadas que comparten los módulos, consulta [Bibliotecas compartidas](SHARED_LIBRARIES.es.md). `ForgeApi.Libraries` es independiente de la interfaz de registro de pruebas y comandos.

Referencia `CalradiaForge.Sdk.dll` y declara `CalradiaForge` como módulo requerido que se carga antes que tu extensión. No incluyas otra copia de la DLL del SDK en la carpeta de tu extensión. El SDK apunta a .NET Framework 4.7.2 para módulos del juego y a .NET 8 para pruebas externas. Forge no requiere Harmony, MCM, ButterLib ni otro framework de mods.

Registra extensiones desde el callback de carga de tu submódulo con el helper de disponibilidad atómica de Forge:

```csharp
using CalradiaForge.Sdk;

protected override void OnSubModuleLoad() => ForgeApi.RegisterWhenAvailable(Register);
protected override void OnSubModuleUnloaded() => ForgeApi.UnregisterWhenAvailable(Register);
void Register(IForgeRegistry registry) => registry.Register(new MyTest());
```

`RegisterWhenAvailable` comprueba el registro actual y se suscribe como una sola operación, por lo que no puede perder una conexión entre una comprobación nula y la posterior suscripción al evento. Conserva la llamada correspondiente a `UnregisterWhenAvailable` al descargar. El evento heredado `Available` se mantiene por compatibilidad de código fuente. Las extensiones deben usar IDs globalmente únicos, como `my_module.inventory_check`. Forge invoca a cada suscriptor de disponibilidad aunque otro lance una excepción, conserva los registros exitosos y anota el callback fallido como advertencia de inicio. La extensión debe capturar y tratar sus propios errores recuperables.

## Contratos

- `ITestCase`: `Prepare`, `Execute`, `Verify`, `Cleanup`. Se intenta ejecutar la limpieza incluso si fallan la preparación o la cancelación. Haz que la limpieza sea idempotente y admita una preparación parcial.
- `ICommand`: una operación acotada con un argumento de texto.
- `IDiagnosticProvider`: devuelve registros `Finding`; las excepciones del proveedor se convierten en advertencias de diagnóstico.
- `Descriptor`: `Id`, `Module`, `Name`, `Context` y `ChangesState`. El contexto puede ser Any, Campaign o Mission. Los cambios de estado requieren el modo de pruebas y, si hay una campaña activa, la confirmación de una copia.
- `TestExecution`: servicios, token de cancelación, `Random` con semilla, pasos registrados y `Verify(condition, description)`.

## Framework de extensiones ForgeWeave

`IForgeEventRegistry` es un contrato aditivo del SDK para manejadores cooperativos. Registra un `IForgeEventHandler` desde un callback `ForgeApi.RegisterWhenAvailable`; Forge asigna `Events` antes de invocar ese callback, sin alterar el contrato existente de `IForgeRegistry` para pruebas, comandos y diagnósticos.

Un manejador expone una `ForgeEventSubscription`: un `Descriptor` globalmente único, un `ForgeEventKind`, prioridad, IDs `Before` y `After` opcionales para el mismo evento y prioridad, `ForgeEventAccess`, un `ForgeEventFilter` acotado de coincidencia exacta, `FailureLimit` y, solo para `Pulse`, `MinimumIntervalMilliseconds`. El primer intervalo de pulso válido es 250 ms; el máximo es 60.000 ms. Los demás tipos de evento deben usar cero.

`ForgeEventFilter.RequiredData` permite suscribirse a metadatos escalares copiados sin escribir un predicado en cada manejador. Un mapa vacío coincide con todos los eventos. Forge acepta hasta ocho pares clave/valor, limita las claves a 64 caracteres y los valores a 512, copia la declaración al registrarla y compara con igualdad ordinal. Una discrepancia produce el resultado `Filtered` y nunca alcanza la autorización ni el código del usuario. La misma regla rige la distribución en vivo y Replay Lab.

`BudgetMilliseconds` asigna a un manejador un presupuesto de ejecución visible y breve, entre 0 y 5.000 ms. Cero lo desactiva. Con `ForgeBudgetPolicy.Warn`, un exceso se informa como `OverBudget` en el resultado y la instantánea, pero se permite que el callback termine; `Ignore` excluye la medición de los hallazgos de presupuesto. Forge nunca aborta código arbitrario de extensiones ni pone en cuarentena a un manejador solo por tardar demasiado.

ForgeWeave solo recibe callbacks emitidos explícitamente por el adaptador Bannerlord de Forge: `ForgeReady`, `InitialScreenReady`, cambios de contexto, inicio de campaña, inicialización de misión, creación/eliminación de agentes y un `Pulse` acotado. `ContextLeaving` conserva su contexto de origen aunque el anfitrión ya haya cambiado al contexto siguiente. `ForgeEvent` expone secuencia, marca de tiempo, contexto lógico, token de cancelación, duración y un diccionario `Data` copiado de solo lectura. No expone `ITestServices`, un resolvedor de objetos del juego ni objetos vivos de Bannerlord.

Las prioridades más altas se ejecutan primero. Las restricciones Before/After solo refinan prioridades iguales. Forge bloquea declaraciones ausentes, de otro evento, de otra prioridad, autorreferenciales o cíclicas, en lugar de inferir el orden; los manejadores independientes continúan. Los manejadores que lanzan excepciones se aíslan y se ponen en cuarentena al alcanzar su límite acotado de fallos. Los manejadores `CampaignWrite` y `MissionWrite` usan las mismas comprobaciones de contexto del descriptor, modo de pruebas y copia confirmada que las pruebas Forge. `StopPropagation` solo detiene manejadores ForgeWeave posteriores para el mismo evento; nunca cancela un callback del juego.

### Replay Lab

`IForgeReplayRegistry` es otro contrato aditivo que se expone como `ForgeApi.Replays`. Enumera copias separadas `ForgeReplayRecord` conservadas por el anfitrión y acepta solo su secuencia de origen en `Replay`. No acepta de una extensión el tipo de evento, contexto ni contenido.

Establece explícitamente `ForgeEventSubscription.ReplayMode` cuando un manejador pueda recibir una reproducción. El valor predeterminado es `Disabled`; `ObserveOnly` es para verificación de solo lectura; `Live` permite una reproducción en vivo protegida. Las reproducciones siempre requieren coincidencia exacta con el contexto actual. No eluden el acceso del descriptor, el modo de pruebas ni las comprobaciones de copia de campaña. Durante una reproducción, `ForgeEvent.IsReplay` es `true` y `SourceSequence` identifica el evento conservado del anfitrión. Forge no conserva una reproducción como nueva fuente.

`ForgeReplayResult` y la instantánea contienen estado, motivo de rechazo, tiempo y resultados copiados de los manejadores. Úsalos para verificar eventos de forma controlada. No son una API genérica de parches, inyección de callbacks ni interceptación de métodos. Consulta [ForgeWeave](FORGEWEAVE.es.md) para el ejemplo de registro, límites y comportamiento de interfaz.

La acción de protocolo `framework` devuelve `ForgeWeaveSnapshot`; `event-journal` devuelve los registros acotados recientes; `replay` solicita al juego activo reproducir una secuencia de origen conservada. Consulta [ForgeWeave](FORGEWEAVE.es.md) para un ejemplo completo y sus límites.

## Preflight de Patch Blueprint

`IPatchBlueprintProvider` es un contrato aditivo y de solo lectura del SDK para declarar un destino de parche previsto. Regístralo con `ForgeApi.PatchBlueprints?.Register(provider)` después de conectar Forge. Es independiente de `IForgeRegistry`, para que los implementadores existentes del SDK v1 de pruebas, comandos y diagnósticos sigan siendo compatibles.

El proveedor tiene un `Descriptor` y devuelve registros `PatchBlueprint` desde `Describe(PatchBlueprintRequest)`. Configura `Descriptor.ChangesState=false`; Forge rechaza proveedores que cambien el estado. Un blueprint incluye un ID estable, vocabulario de hooks (`Prefix`, `Postfix`, `Transpiler`, `Finalizer`), destino exacto `MethodReference`, referencia obligatoria del callback, prioridad declarada, IDs de propietario Before/After y justificación. `MethodReference.From(MethodBase)` y `TypeReference.From(Type)` capturan una firma sin ningún tipo Harmony. `ForgeApi.Version` es 13; los servicios de parcheo y hooks de ejecución siguen siendo capacidades opcionales del anfitrión, así que comprueba directamente `ForgeApi.Patches` o `ForgeApi.Hooks` antes de usarlos.

Forge ejecuta el proveedor únicamente durante un preflight explícito en el hilo del juego, copia los DTO devueltos y limita el tamaño de captura. No apliques parches en `Describe`, conserves objetos del juego ni inicies trabajo en segundo plano. Forge valida el destino declarado solo contra ensamblados ya cargados, registra referencias de orden autorreferenciales para revisión y nunca infiere el orden final de ejecución. Consulta [Preflight de Patch Blueprint](PATCH_BLUEPRINTS.es.md) para un ejemplo completo y reglas de resolución.

Ejecuta operaciones del juego en el hilo que las llama. No conserves objetos vivos del juego entre sesiones ni generes tareas en segundo plano que accedan a ellos. El código de extensión de larga duración debe cooperar con la cancelación; Forge no es un sandbox ni puede abortar de forma segura código C# arbitrario.

Las pruebas son síncronas y deben terminar pronto. Las solicitudes de transporte vencen tras 15 segundos y solicitan cancelación. Una operación que agotó el tiempo aún podría estar terminando su limpieza; inspecciona el resultado registrado antes de reintentar una acción que cambie estado.

Los dos ejemplos completos están en `examples/CalradiaForge.Examples/Examples.cs`. Obtienen `IGameLaboratory` mediante `ITestServices.GetService`; este adaptador del juego vive en el ensamblado del mod. Sus pruebas automatizadas usan un laboratorio falso y no demuestran la corrección del motor nativo.

La API 13 del SDK expone `ForgeHookDefinition.Finalizer` opcional, `Exception` mutable en la invocación y los indicadores de snapshot `HasFinalizer`/`HasTranspiler`, conservando las firmas anteriores del constructor. Prefix/Postfix/Finalizer se registran mediante `IForgeHookService`; los transpilers IL locales usan el adaptador `ForgeHookService.RegisterTranspiler`, exclusivo de Core `net472`. El SDK no contiene tipos de contrato MonoMod. Consulta las [políticas de hooks de ejecución](PATCH_BLUEPRINTS.es.md#hooks-explícitos-en-ejecución-y-transpilers-il) antes de registrar código ejecutable: los Finalizers requieren autorización de callbacks del host, mientras que una transformación IL aplicada permanece activa fuera del menú hasta revertirla explícitamente.

## Protocolo local

Conéctate a `CalradiaForge-{BannerlordProcessId}` mediante una canalización con nombre de Windows dúplex. La ACL permite solo al usuario actual de Windows. No se usa un puerto TCP ni un servicio externo.

Cada línea UTF-8 es un `Request` JSON: `Version=1`, `Id`, `Action`, `Argument`, `Seed`. Las respuestas repiten la versión y el ID de solicitud, e incluyen `Success`, `Error` y `Data`. `Data` estructurado también es una cadena JSON. Comienza con `hello` para negociar capacidades. Su lista contiene `protocol:1`, etiquetas de suite y destino y, después, cada acción compatible. Las solicitudes se limitan a 64 KiB; las respuestas de Desktop, a 32 MiB.

Acciones: `hello`, `summary`, `scan`, `modules`, `dependencies`, `diagnostics`, `logs`, `inspect`, `pin`, `compare`, `snapshots`, `unpin`, `tests`, `commands`, `command`, `test-mode`, `confirm-copy`, `run`, `run-batch`, `metrics`, `framework`, `event-journal`, `replay`, `harmony`, `patch-blueprints`, `patch-preflight`, `hook-snapshots`, `hook-apply-plan`, `hook-apply-confirm`, `hook-revert-plan`, `hook-revert-confirm`, `hook-plan-cancel`, `report`, `export`, `panel-open`, `panel-close`, `language`, `agent-memory`.

`language` es una consulta de compatibilidad de solo lectura que devuelve el identificador de idioma actual del juego. No cambia el idioma de ninguna de las interfaces. `run-batch` acepta hasta 50 IDs de prueba separados por comas, valida el lote antes de ejecutarlo y se detiene ante el primer fallo. `snapshots` enumera los objetos fijados; `unpin` usa la misma clave exacta `Type|ID` que `pin`. `dependencies` devuelve un orden sugerido a partir del último análisis de módulos completado y no edita el launcher. `panel-open` y `panel-close` controlan el panel nativo.

`framework` devuelve la salud de los manejadores ForgeWeave, el orden declarado, los datos de contexto/acceso, tiempos, fallos, cuarentenas, contadores de eventos, fuentes de reproducción conservadas y resultados recientes. `event-journal` solo devuelve el historial reciente acotado. `replay` recibe una única secuencia de origen conservada y la ejecuta en el hilo del juego; no reintenta automáticamente y mantiene todas las puertas de escritura existentes. `framework` y `event-journal` son de solo lectura y no recorren objetos del juego; `replay` es una acción de verificación protegida cuyo resultado puede rechazarse sin alterar la sesión.

`harmony` sigue siendo un endpoint histórico acotado y de solo lectura, únicamente cuando una API Harmony compatible ya está cargada por el juego u otro módulo seleccionado. ForgeWeave no lo usa y no forma parte de la vía recomendada para extensiones.

`patch-blueprints` enumera los descriptores de proveedores de blueprint registrados. `patch-preflight` captura sus declaraciones y evalúa destinos exactos. No requiere ni invoca un runtime de parches y no aplica ninguno.

`hook-snapshots` es de solo lectura. Apply y Revert usan acciones separadas de plan y confirmación con un token breve de un solo uso; `hook-plan-cancel` solo transporta la sesión del anfitrión y el token de la vista previa, e invalida el plan únicamente cuando ambos coinciden. Apply/Revert operan solo sobre IDs de hook registrados previamente y no aceptan métodos de destino ni callbacks arbitrarios. El anfitrión Bannerlord sigue imponiendo la puerta del hilo del juego y de la pantalla exacta del menú principal.

Los argumentos `command` usan `command.id|argument`. `inspect` usa `Type|filter`; pin y compare requieren IDs exactos. Reconecta explícitamente después de que el juego salga o falle el transporte; las solicitudes fallidas nunca se repiten automáticamente.
