# Rev025 — Contrato SDK 8, memoria acotada y ciclo de vida ForgeWeave

**Fecha:** 25 de septiembre de 2026  
**Versión:** Calradia Forge 24.0.0, metadatos de versión sin cambios en esta actualización  
**Alcance:** SDK, Core y documentación. Ciclo de vida de suscripciones ForgeWeave, cuotas de `ForgeAgentMemory` y procedencia de los paquetes fuente.

## Problema observado y justificación técnica

La documentación describía `ForgeAgentMemory` como un almacén sin límites y con TTL en los tres niveles. Eso no reflejaba las cuotas del nuevo contrato: el crecimiento no acotado podía retener datos de agentes durante toda la sesión. Además, una llamada de suscripción inmediata podía ejecutarse antes de que Forge publicara su anfitrión de eventos, y no existía en la documentación un dueño de ciclo de vida para anular el registro y recuperarlo tras una reconexión. La referencia del SDK seguía mostrando la versión de contrato 5 y no distinguía el código fuente v24.0.0 de los ZIP de producto 23.0.0 existentes.

## Solución técnica y decisiones arquitectónicas

El contrato público del SDK avanza a la versión 8. `ForgeCampaignEvents.SubscribeWeaveWhenAvailable` devuelve un `ForgeWeaveRegistration` que administra la suscripción pendiente y el manejador activo. Expone los estados `WaitingForHost`, `Registered`, `HostUnsupported`, `Failed` y `Disposed`; se registra al conectar, se retira al desconectar, vuelve a registrarse tras una reconexión y deja de aceptar conexiones futuras al llamar `Dispose()`. El registro y su limpieza activa son sincrónicos y requieren el hilo de conexión del anfitrión. Se puede liberar un objeto pendiente desde cualquier hilo; liberar uno activo desde otro hilo lanza `InvalidOperationException`, deja intacto el manejador activo y registra el error para reintentar desde el hilo correcto. El registro inmediato `SubscribeWeave` conserva su uso y lanza `InvalidOperationException` si no existe un anfitrión de eventos o se usa fuera del hilo de conexión.

`ForgeAgentMemory` mantiene un máximo de 2,048 IDs distintos compartidos por los tres niveles. La memoria semántica acepta hasta 128 hechos por agente y es el único nivel con TTL opcional. La episódica admite 512 episodios por agente y 128 por tipo; al excederlos expulsa FIFO el episodio más antiguo global del agente y, si hace falta, el más antiguo restante del tipo entrante. La procedimental admite 128 tareas por agente. `Semantic.TryUpsert` y `Procedural.TryAdd` devuelven `false` si una clave nueva o un ID de agente supera una cuota; `Episodic.TryAdd` solo devuelve `false` si el límite global rechaza un ID nuevo, ya que sus cuotas de nivel usan expulsión FIFO. Las variantes heredadas lanzan `InvalidOperationException` si la capacidad rechaza una escritura. Las claves ya existentes se pueden actualizar en los niveles semántico y procedural aunque su cuota esté llena.

El almacén es seguro para concurrencia, vive en memoria del proceso y no se serializa en las partidas guardadas. `ClearAgent` y `ClearAll` liberan sus entradas. La guía separa el código fuente 24.0.0 de los ZIP `CalradiaForge-Modules-23.0.0.zip` y `CalradiaForge-Source-SDK-23.0.0.zip`, que no se reconstruyeron.

## Cambios en activos, código y dependencias

- Se sincronizan `docs/sdk-reference.md` y `docs/sdk-reference.es.md`, junto con `docs/FORGEWEAVE.md` y `docs/FORGEWEAVE.es.md`.
- Se actualiza `docs/CODEMAP_SDK_GAMEMODELS.md` con contrato SDK 8, administración del ciclo de vida y cuotas exactas de memoria.
- Se agregan anotaciones fuente 24.0.0 en los changelogs EN/ES sin reemplazar entradas anteriores.
- No se añaden dependencias ni se regeneran paquetes ZIP.

## Validación y límites de la evidencia

- La documentación se contrastó con las API fuente de `ForgeCampaignEvents`, `ForgeAgentMemory` y `ForgeApi`, y con la versión de producto en `Directory.Build.props`.
- `python tools/append_detailed_changelog_revision.py docs/append/Rev025-SDK-v8-bounded-memory-and-weave-lifecycle-2026-09-25.es.md` comprueba que texto, estilos y XML canónico de los párrafos previos permanezcan como prefijo y crea la nueva revisión protegida.
- `python tools/record_detailed_changelog_integrity.py` verifica la cadena SHA-256 y registra la revisión nueva.
- Por el alcance solicitado, no se compilaron proyectos ni se ejecutaron suites de pruebas; Bannerlord no se inició. Los dos ZIP de 23.0.0 permanecen sin cambios y no se afirma que contengan las API del contrato 8.

Este anexo amplía la revisión anterior sin reemplazar sus párrafos. Mantiene los comandos, las rutas, las API públicas y la versión del producto.
