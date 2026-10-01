# Rev088 — Reconciliación archivística de la fuente

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Recuperación archivística de la fuente desde el changelog publicado; sin cambios de código ni ejecución.

**Nota de reconciliación archivística:** Este anexo fuente se reconstruyó el 30/09/2026 a partir de las secciones ya publicadas en los changelogs inglés y español. No existía un anexo fuente original de esta revisión en `docs/append/`. Este respaldo no modifica ni sustituye ningún DOCX protegido o registro de integridad previo.

## Seguimiento de seguridad del motor de parcheo de Calradia Forge — 29/09/2026 (Rev088)

- Rechaza antes de cambiar la protección de memoria las escrituras de detour que crucen el límite de una página del sistema; agrega regresiones de frontera y ausencia de escritura. `TypeReference.From` y Patch Preflight conservan y comparan el propietario/posición de parámetros genéricos (`!0` frente a `!!0`) para impedir que parámetros de tipo y método con el mismo nombre se confundan.
- Agrega la capacidad opcional separada `IForgePatchServiceLifecycle`, que reabre un servicio integrado desconectado únicamente si cada registro anterior y sus bytes originales se verifican como revertidos y no queda un destino rastreado. Mantenerla separada evita exigir nuevos miembros a implementaciones existentes de `IForgePatchService`.
- El reintento integrado por BAT pasó compilaciones limpias `net472`/`net8.0`, 0 advertencias y errores, Core 363/363, el fixture serial de detour x64 (`15 → 32 → 15`, reversión exacta) y ForgeWeave 73/73. Un intento integrado anterior informó un fallo Core que no pudo reproducirse; pasaron tanto el BAT Core aislado como la ejecución integrada posterior.
- No se inició Bannerlord ni el Modding Kit. El fixture serial no demuestra seguridad frente a ejecución concurrente durante la escritura de código; el backend de detour sigue siendo experimental. La versión permanece en 25.2.0, `ForgeApi.Version` en 11, y no cambian ForgeWeave, las rutas de escritura IPC ni los ZIP de distribución.
