# Rev086 — Reconciliación archivística de la fuente

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Recuperación archivística de la fuente desde el changelog publicado; sin cambios de código ni ejecución.

**Nota de reconciliación archivística:** Este anexo fuente se reconstruyó el 30/09/2026 a partir de las secciones ya publicadas en los changelogs inglés y español. No existía un anexo fuente original de esta revisión en `docs/append/`. Este respaldo no modifica ni sustituye ningún DOCX protegido o registro de integridad previo.

## Seguimiento del motor de parcheo de Calradia Forge 25.2.0 — 29/09/2026 (Rev086)

- Se elimina el escaneo implícito de parches al iniciar el módulo; la entrada heredada `InitializeGlobalPatches()` permanece como no-op obsoleto. `ForgePatcher.ApplyAll(assembly)` solo se ejecuta de forma explícita y valida el lote suministrado antes de escribir. Patch Preflight sigue siendo de solo lectura y resuelve destinos y callbacks declarados, IDs duplicados, conflictos y orden sin cargar ensamblados ni invocar callbacks.
- Se agrega `IForgePatchService` opcional mediante `ForgeApi.Patches`; `ForgeApi.Version` avanza de 10 a 11 sin cambiar los implementadores de `IForgeRegistry`. `ForgeDetour`, `MethodSwapper` y los registros de parches comparten una ruta de escritura y verificación. La reversión comprueba bytes exactos, informa como conflicto las modificaciones ajenas y solicita vaciar la caché de instrucciones mientras comprueba los cambios de protección de páginas ejecutables. Se agregan `cf.patch_status [owner]` y `cf.patch_revert <id|owner|all>`; Patch Preflight por IPC continúa siendo de solo lectura.
- El backend sigue siendo experimental: no suspende hilos, no decodifica ni reubica instrucciones sobrescritas y no garantiza seguridad mientras se ejecuta el método objetivo. Esta entrada no incluye una prueba en tiempo de ejecución de Bannerlord ni del Modding Kit. El fixture desechable x64 y su integración con BAT ya están presentes, pero la ejecución y los resultados por BAT y la regresión completa siguen pendientes; aquí no se afirma ningún resultado aprobado.
- La versión del producto permanece en 25.2.0; ForgeWeave y los ZIP de distribución no cambian.
