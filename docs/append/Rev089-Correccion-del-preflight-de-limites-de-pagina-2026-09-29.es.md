# Rev089 — Reconciliación archivística de la fuente

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Recuperación archivística de la fuente desde el changelog publicado; sin cambios de código ni ejecución.

**Nota de reconciliación archivística:** Este anexo fuente se reconstruyó el 30/09/2026 a partir de las secciones ya publicadas en los changelogs inglés y español. No existía un anexo fuente original de esta revisión en `docs/append/`. Este respaldo no modifica ni sustituye ningún DOCX protegido o registro de integridad previo.

## Corrección del preflight de límites de página del motor de parcheo — 29/09/2026 (Rev089)

- Mueve el rechazo de tramos por límite de página en las rutas de parche directo y por lote antes de leer bytes originales o reservar registros. Un lote rechazado valida todos sus tramos antes de leer o reservar cualquier destino, por lo que sus mismos IDs y destinos quedan disponibles para reintentar.
- Agrega regresiones por las rutas públicas `Patch` y `ForgePatcher.ApplyAll`, que verifican que no cambien los contadores de lectura/protección/escritura/flush, que no queden recibos tras el rechazo y que se puedan reutilizar los destinos con un tamaño de página sintético permitido.
- La ejecución más reciente del BAT Core pasó 363/363 regresiones administradas y compiló el fixture desechable `net472` x64 con cero advertencias o errores. Después, el fixture nativo serial se bloqueó y se detuvo; por ello, no se informa como aprobada la ejecución global más reciente del BAT ni el smoke nativo. El BAT del fixture ahora limita la espera del proceso hijo.
- Este seguimiento reemplaza únicamente para el árbol más reciente la afirmación de fixture aprobado en Rev088; conserva Rev088 como registro de la ejecución aprobada anterior. No se inició Bannerlord ni el Modding Kit. El backend de detour sigue siendo experimental y no garantiza seguridad ante ejecución concurrente. La versión permanece en 25.2.0, `ForgeApi.Version` en 11 y los ZIP no cambian.
