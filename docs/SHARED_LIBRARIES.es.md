# Bibliotecas compartidas para autores de mods

`ForgeApi.Libraries` es un registro de servicios versionados y con nombre explícito que comparten módulos de Bannerlord. Forge lo crea antes de la notificación `ForgeApi.Available` y lo invalida cuando Forge se desconecta. Se suma al registro heredado de pruebas y comandos SDK v1 y sigue disponible desde SDK v3; no sustituye al cargador de módulos de Bannerlord ni a Harmony. La versión actual de `ForgeApi.Version` es 13 e incluye las capacidades opcionales del servicio de parches y de hooks en tiempo de ejecución; estas no cambian el contrato de `ForgeApi.Libraries`. El SDK tiene como destinos `net472` y `net8.0`; el módulo del juego tiene como destino `net472`. Las operaciones del registro se ejecutan en el hilo que lo creó (el hilo del juego en Forge).

## Modificadores de modelos y ciclo de vida del propietario

`ForgeModelRegistry` puede evaluar modificadores sin mantener su bloqueo interno mientras ejecuta predicados `Condition` proporcionados por el autor. Cada evaluación usa una instantánea estable: registrar o quitar un modificador desde un predicado no cambia los modificadores que ya se seleccionaron para esa evaluación, y el predicado puede volver a entrar al registro sin provocar un bloqueo mutuo.

Para administrar los modificadores de un módulo, usa `BeginOwnerScope(ownerId)` y conserva el `ForgeModelRegistrationScope` devuelto durante la vida del módulo:

```csharp
private ForgeModelRegistrationScope _modelScope;

void RegisterModels(ForgeModelRegistry registry)
{
    _modelScope = registry.BeginOwnerScope("MyMod");
    _modelScope.Register(new ForgeModelModifier(
        "my_mod.party_speed", ForgeGameModelCategory.PartySpeed, "MyMod",
        "My party speed adjustment", 0.0f, 0.05f));
}

void OnModuleUnloaded()
{
    _modelScope?.Dispose();
    _modelScope = null;
}
```

`SourceModule` del modificador debe coincidir exactamente con el propietario del scope. Al liberar el scope solo se retiran las generaciones de registro que todavía le pertenecen; si otro registro reemplazó el mismo ID, la liberación conserva el reemplazo. Los métodos heredados `Register` y `Unregister` por ID siguen disponibles y conservan su comportamiento global sobre el registro.

## Proveedor y consumidor

Publica una interfaz C# en un ensamblado pequeño de contratos al que hagan referencia ambos mods. El proveedor distribuye ese DLL de contratos una sola vez; el consumidor referencia el mismo ensamblado con `Private=false` y declara al proveedor como módulo requerido en `SubModule.xml`. Ambos módulos dependen de CalradiaForge. No distribuyas una segunda copia del DLL del SDK de Forge.

```csharp
// Módulo proveedor, dentro de su callback ForgeApi.RegisterWhenAvailable en el hilo de actualización del juego.
library = ForgeApi.Libraries.OpenModule("MyEconomy");
registration = library.Provide<IPriceCalculator>(
    "prices", new Version(1, 0), new PriceCalculator());

// Módulo consumidor, después de cargar el proveedor.
library = ForgeApi.Libraries.OpenModule("MyTradeMod");
prices = library.Require<IPriceCalculator>(
    "MyEconomy", "prices", new Version(1, 0));
int total = prices.Use(service => service.CalculateTotal(7, 3));
```

Guarda el `ModuleLibrary` de cada módulo en un campo y libéralo en `OnSubModuleUnloaded`. El objeto descartable que devuelve `Provide` permite retirar solo ese servicio. Forge libera los registros, pero el proveedor es responsable de liberar su implementación y los demás recursos.

Durante `OnSubModuleLoad`, llama a `ForgeApi.RegisterWhenAvailable(Register)` y llama a `ForgeApi.UnregisterWhenAvailable(Register)` durante la descarga. El registro comprueba atómicamente el registro actual y se suscribe a una conexión futura, por lo que la carga del módulo no puede perder la disponibilidad. Forge inicializa el SDK en su primera actualización de la aplicación; el hilo de carga de módulos no necesariamente es el hilo de actualización. Las clases `SubModule` de los ejemplos de proveedor y consumidor muestran este ciclo de vida. Accede a las bibliotecas solo después de la notificación de disponibilidad, desde actualizaciones del juego o comandos/pruebas de Forge. Los callbacks se ejecutan de forma síncrona en el hilo que registra el callback cuando el host ya está conectado o en el hilo que establece la conexión, fuera del bloqueo global de disponibilidad del SDK. Cada entrega administrada está protegida contra generaciones obsoletas y la baja de la suscripción: dar de baja antes del turno omite el callback pendiente y la operación espera a que termine uno ya iniciado. El callback puede darse de baja a sí mismo. Mantén breves los callbacks y no los bloquees esperando otros hilos que puedan registrar o dar de baja callbacks de disponibilidad administrados.

La notificación de disponibilidad invoca una instantánea de cada suscriptor en orden de registro. Si un callback falla, los siguientes pueden ejecutarse. Después, Connect lanza una `AggregateException` que contiene los errores de los callbacks; el host del juego la registra. Los registros creados correctamente permanecen disponibles. Esto aísla las notificaciones, pero no hace rollback transaccional: una extensión que registre recursos parcialmente antes de fallar debe limpiarlos por su cuenta. Al reconectar se crea un registro de bibliotecas nuevo y se invalidan los handles de la conexión anterior.

## Compatibilidad, diagnósticos y ciclo de vida

- Los IDs no distinguen mayúsculas de minúsculas. Se rechazan los IDs de módulo duplicados o los IDs de servicio repetidos dentro de un proveedor; no se elige ni sobrescribe un proveedor silenciosamente.
- La interfaz solicitada debe ser exactamente el tipo de contrato CLR publicado, incluida su identidad de ensamblado. Publica interfaces, no clases de implementación concretas.
- Las versiones del proveedor y la mínima requerida deben tener la misma versión mayor; la versión del proveedor debe ser igual o superior a la solicitada. Los componentes de compilación/revisión omitidos se normalizan a cero. No se admite la versión mayor cero. Los autores son responsables de cumplir sus promesas de compatibilidad.
- `Require<T>` resuelve únicamente un proveedor y un servicio ya registrados. No carga módulos, busca otro proveedor, convierte contratos ni elude el orden del launcher. Las dependencias ausentes o incompatibles fallan inmediatamente con `InvalidOperationException`; usa el contexto del diagnóstico para corregir la configuración y vuelve a intentarlo durante el ciclo normal de disponibilidad.
- El diagnóstico de proveedor ausente identifica el módulo proveedor solicitado. Confirma el ID exacto, que el proveedor esté instalado y habilitado, y que el `SubModule.xml` del consumidor declare la dependencia del proveedor. Ambos módulos también deben depender de CalradiaForge.
- El diagnóstico de servicio ausente identifica el proveedor y el ID del servicio. Confirma que el proveedor llame a `Provide<T>` con el mismo ID de servicio desde su callback de disponibilidad y que el orden de carga del consumidor siga al del proveedor.
- El diagnóstico de contrato incompatible identifica proveedor/servicio y el contrato CLR esperado por el consumidor frente al contrato publicado por el proveedor. Confirma que ambos proyectos referencien el mismo proyecto/identidad de ensamblado de contratos y empaqueta un solo DLL compartido con el proveedor, no una copia privada en el consumidor.
- El diagnóstico de versión identifica proveedor/servicio y las versiones publicada y solicitada. Alinea la versión publicada por el proveedor con el requisito mínimo del consumidor: las versiones mayores deben coincidir y la publicada debe satisfacer el mínimo. No rebajes el requisito para ocultar un cambio incompatible.
- Los handles se vinculan a una generación de registro. Tras retirar el servicio, descargar el proveedor o consumidor, o desconectar el registro, un handle falla en lugar de cambiar de implementación silenciosamente. Obtén un handle nuevo cuando el proveedor vuelva a registrarse.
- El registro, la resolución, el uso y la liberación activa se ejecutan en el hilo que creó el registro; en Forge es el hilo del juego. Las excepciones del proveedor se propagan al consumidor. El runner de pruebas puede registrarlas cuando las llamadas ocurren dentro de una prueba registrada.

## Resolución opcional y seguimiento de cambios

`ModuleLibrary.Resolve<T>(providerModule, serviceId, minimumVersion)` realiza las mismas comprobaciones explícitas de proveedor, servicio, identidad del contrato y versión que `Require<T>`, pero devuelve un `SharedServiceResolution<T>` inmutable para los resultados de búsqueda en vez de lanzar una excepción cuando un servicio no está disponible o no es compatible. Los valores de `SharedServiceResolutionStatus` son `Available`, `ProviderMissing`, `ServiceMissing`, `ContractMismatch` e `IncompatibleVersion`; `Diagnostic` contiene el contexto para corregir el problema e `IsAvailable` ofrece una comprobación directa. `TryGetService(out SharedService<T> service)` devuelve un handle solo si el resultado está disponible. Los argumentos inválidos, las infracciones de afinidad de hilo y el uso después de liberar el objeto siguen siendo errores de programación/ciclo de vida y lanzan excepciones. `Require<T>` sigue siendo fail-fast y conserva su tipo de excepción y comportamiento actuales.

Usa `ModuleLibrary.Watch<T>(providerModule, serviceId, minimumVersion)` cuando el consumidor necesite reaccionar a cambios posteriores en vez de consultar repetidamente. `SharedServiceMonitor<T>.Current` contiene la instantánea inicial de resolución; crear el monitor no invoca `Changed`. La disponibilidad y baja posteriores del servicio actualizan `Current` y notifican `Changed` con snapshots de transición inmutables `SharedServiceChangedEventArgs<T>.Previous` y `.Current`. Retirar y después publicar de nuevo el mismo proveedor/servicio/versión crea una nueva generación de registro y se notifica como cambio; el registro no tiene una operación de reemplazo in situ. Si se libera el módulo proveedor mientras el consumidor sigue activo, la resolución pasa a indicar que falta el proveedor. Las notificaciones son sincrónicas en el hilo del registro y se ejecutan después de completar el cambio correspondiente. Los errores de los manejadores se aíslan y se agrupan en `LastNotificationError` para la transición más reciente; una transición exitosa limpia esa propiedad. Un manejador defectuoso no impide que los demás reciban el cambio. Una mutación realizada por un manejador de `Changed` surte efecto de inmediato en el registro y actualiza al instante las instantáneas de los monitores; solo la entrega de callbacks se encola mientras hay un pase de notificaciones activo para evitar el despacho recursivo. Por ello, los snapshots `Previous` y `Current` del evento pueden estar desactualizados cuando se ejecuten manejadores posteriores. Lee `monitor.Current` o llama a `Resolve<T>` para consultar el estado más reciente del registro; un handle de una generación anterior puede haber quedado invalidado por una baja. Libera el monitor cuando deje de ser necesario; la descarga del consumidor también desconecta sus suscripciones. Al desconectarse el registro global, los monitores se desconectan y liberan silenciosamente, sin una notificación final `Changed`.

## Lotes de publicación atómicos

Para los proveedores que exponen servicios relacionados, `ModuleLibrary.BeginPublication()` crea un `SharedServiceBatch`. Añade cada servicio con `Add<T>(serviceId, apiVersion, implementation)` y después llama a `Commit()`. Los servicios preparados no son visibles hasta confirmar el lote. `Commit()` valida el conjunto completo antes de cambiar el estado del registro; una entrada no válida o una colisión dejan el registro sin una publicación parcial. Los observadores reciben notificaciones solo después de que todo el lote esté visible.

El objeto del lote también es el lease de ciclo de vida. Liberarlo antes de confirmar descarta las entradas preparadas; liberarlo después de confirmar retira todos los registros del lote como un único cambio de estado. Conserva el lote confirmado en un campo del proveedor hasta que se descargue el módulo. La liberación retira los registros, pero no adquiere propiedad ni libera los objetos de implementación: el proveedor sigue siendo responsable de sus recursos. `Provide<T>` y su `IDisposable` individual siguen disponibles para registros independientes.

La API de callbacks comprueba el ciclo de vida antes de llamar al proveedor. No es un sandbox de C#: los callbacks no deben retener la implementación, devolverla para usarla después ni acceder a objetos del juego desde tareas de fondo. Una llamada que ya está en curso no se interrumpe a la fuerza. Los servicios compartidos son código normal de mods y no obtienen permiso de pruebas automáticamente. Usa las declaraciones existentes de modificación de estado para pruebas/comandos al exponer acciones que cambien el estado desde el banco de trabajo.

## Ejemplo compilable de proveedor y consumidor

Tres proyectos muestran módulos compilados de forma independiente:

1. `CalradiaForge.PriceContracts`: interfaz compartida `IPriceCalculator`.
2. `CalradiaForge.PriceProvider`: publica la API `prices` en versión 1.0 y comprueba totales enteros inválidos o desbordados.
3. `CalradiaForge.PriceConsumer`: resuelve esa interfaz y registra `examples.shared_prices`, una prueba de solo lectura que espera 7 × 3 = 21.

Instala las carpetas opcionales de módulo `CalradiaForgePriceProvider` y `CalradiaForgePriceConsumer`. Carga Forge, Provider y luego Consumer. El manifiesto del consumidor declara la dependencia del proveedor. El DLL de contratos se empaqueta solo con Provider; la referencia del proyecto consumidor usa `Private=false` y no debe haber una segunda copia en la carpeta del consumidor. No copies otro DLL del SDK de Forge. Ejecuta `examples.shared_prices` desde Tests e inspecciona/exporta sus pasos registrados mediante el flujo existente. Es un ejemplo pequeño de dependencia, no reemplaza una economía ni afirma implementar un algoritmo novedoso de precios.

La suite Core prueba el proveedor y consumidor reales desde ensamblados separados, incluida la identidad de contrato, el alta y la baja de registros, los errores de compatibilidad de versión, las dependencias ausentes, la invalidación por ciclo de vida y los manifiestos/disposición de empaquetado del ejemplo. Ejecútala mediante `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`; no inicies directamente los ejecutables de prueba. La validación nativa se registra por separado en `VALIDATION.md`.
