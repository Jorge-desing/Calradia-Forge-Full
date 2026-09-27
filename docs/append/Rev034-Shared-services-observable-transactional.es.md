# Rev034 — Servicios compartidos observables y transaccionales

**Fecha:** 25 de septiembre de 2026  
**Versión:** Calradia Forge 23.0.0; API del SDK versión 7  
**Alcance:** resolución, publicación agrupada y observación del ciclo de vida de servicios compartidos del SDK.  
**Distribución:** seguimiento solo del código fuente; no se regeneraron ZIP ni paquetes del producto.

## Problema observado y justificación técnica

El registro de servicios compartidos ofrecía una búsqueda `Require<T>` fail-fast y registros `Provide<T>` individuales. Los consumidores que necesitaban dependencias opcionales tenían que capturar excepciones o implementar su propio sondeo, mientras que los proveedores que exponían servicios relacionados podían hacerlos visibles uno por uno y administrar varios handles de registro. Estos patrones dificultaban expresar de forma uniforme las integraciones opcionales y los grupos de servicios que debían publicarse como una unidad.

## Solución técnica y decisiones arquitectónicas

`ForgeApi.Version` aumenta de 6 a 7 mientras el producto permanece en 23.0.0. `ModuleLibrary.Resolve<T>` devuelve un `SharedServiceResolution<T>` inmutable con estado, contexto de diagnóstico y `TryGetService`; sus estados distinguen disponibilidad, proveedor ausente, servicio ausente, discrepancia del contrato CLR y versión incompatible. Los argumentos inválidos, las infracciones de afinidad de hilo y el uso de objetos liberados siguen generando excepciones. `Require<T>` continúa siendo fail-fast y conserva su tipo de excepción y semántica.

`ModuleLibrary.BeginPublication()` crea un `SharedServiceBatch`. Los proveedores preparan entradas con `Add<T>` y publican el grupo completo mediante `Commit()`. La validación termina antes de modificar el registro, lo que impide la visibilidad parcial si una entrada preparada no es válida o colisiona. El objeto del lote es el lease de ciclo de vida: liberarlo antes de confirmar descarta lo preparado; liberarlo después de confirmar retira el grupo completo. Retira los registros, pero no adquiere propiedad ni libera las implementaciones del proveedor. Se conservan los handles individuales de `Provide<T>`.

`ModuleLibrary.Watch<T>` devuelve un `SharedServiceMonitor<T>` cuyo valor `Current` es la instantánea inicial; crear el monitor no invoca `Changed`. La disponibilidad y baja posteriores actualizan la instantánea y notifican a los observadores. Retirar y después publicar genera una nueva generación de registro; no existe una operación de reemplazo in situ. Las notificaciones son sincrónicas en el hilo del registro y ocurren después de completar cada cambio de estado. Los argumentos de `Changed` son snapshots de transición inmutables y pueden estar desactualizados cuando se ejecuten manejadores posteriores; `monitor.Current` y `Resolve<T>` reflejan el estado actual, y un handle anterior puede haber quedado invalidado tras retirar el registro. Una mutación del registro dentro de un manejador `Changed` surte efecto de inmediato y actualiza las instantáneas de los monitores; solo la entrega de los callbacks resultantes se encola mientras hay un pase activo para evitar el despacho recursivo. Los errores de los manejadores se aíslan, se conservan mediante `LastNotificationError` y no detienen a los demás observadores. La descarga del consumidor desconecta sus observadores; la desconexión del registro global los desconecta y libera silenciosamente, sin un evento final.

## Cambios en código, activos y dependencias

El cambio añade únicamente tipos y miembros de la API de servicios compartidos del SDK. El SDK mantiene compatibilidad con `net472`; no se añaden ensamblados de TaleWorlds, contratos IPC, dependencias de terceros, comandos de módulos ni comportamiento del juego. El ejemplo proveedor/consumidor y la guía de servicios describen las adiciones sin cambiar la identidad del contrato compartido ni la regla de empaquetarlo una sola vez con el proveedor.

## Validación y límites de la evidencia

- `cmd /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"`: compilación con 0 advertencias y 0 errores; Core pasó 263/263 y ForgeWeave pasó 43/43.
- Se prevé conservar pruebas de estados de resolución, `Require<T>` fail-fast, visibilidad atómica y fallos sin publicación parcial, colisiones y ciclo de vida, instantáneas y transiciones de observadores, aislamiento de callbacks, reentrancia, afinidad de hilo, integración proveedor/consumidor, dependencia de manifiesto y empaquetado único del contrato.
- Las suites de prueba se ejecutaron mediante el launcher `.bat`; no se inició directamente ningún `.exe` o `.dll` de pruebas. No se inició Bannerlord, campaña ni batalla. No se generó ZIP.

Este anexo registra la adición aprobada de API v7 sin reemplazar párrafos anteriores del libro mayor ni cambiar la versión del producto 23.0.0.
