# Rev094 — Reconciliación archivística de la fuente

**Fecha:** 30 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Recuperación archivística de la fuente desde el changelog publicado; sin cambios de código ni ejecución.

**Nota de reconciliación archivística:** Este anexo fuente se reconstruyó el 30/09/2026 a partir de las secciones ya publicadas en los changelogs inglés y español. No existía un anexo fuente original de esta revisión en `docs/append/`. Este respaldo no modifica ni sustituye ningún DOCX protegido o registro de integridad previo.

## Salvaguardas de ciclo de vida de hooks y contexto del anfitrión — 30/09/2026 (Rev094)

- Agrega una protección opcional de desconexión para impedir que reemplazar o desconectar el servicio SDK deje hooks activos o inciertos sin administrar cuando el contexto aprobado de menú e hilo del juego no está disponible. Las excepciones de verificación quedan visibles como conflictos, y los handles de hooks retirados externamente se liberan antes de informar una reversión limpia.
- Restringe la puerta del menú de Bannerlord a la identidad CLR exacta de `GauntletInitialScreen`, resuelta en ejecución desde el ensamblado Gauntlet del juego. Si el tipo oficial no se puede resolver, las mutaciones se rechazan; no se aceptan imitaciones basadas solo en el nombre.
- Agrega `hook-plan-cancel`, que solo transporta la sesión del anfitrión y el token de la vista previa. La cancelación queda ligada a ese par exacto; WPF conserva el plan si no puede confirmarla y detiene la actualización de snapshots en vez de descartar el estado local. La documentación del protocolo SDK en inglés y español describe esta acción.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: Core 384/384 y ForgeWeave 73/73 pasaron; las compilaciones pertinentes `net472`, `net8.0` y Mod `net472` terminaron con 0 advertencias y 0 errores. `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat` pasó desconexión protegida, retirada externa, verificación incierta y restauración exacta `15 → 32 → 15`.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause`: Desktop 65/65 y 292 casos de render WPF pasaron. El harness de render informó 13.895 ms en total; es tiempo del harness, no latencia de interacción observada en la aplicación.
- No se inició Bannerlord ni el Modding Kit. El fixture sigue siendo serial y no demuestra seguridad mientras otro hilo ejecuta el destino durante Apply o Revert. La versión permanece en 25.2.0 y no se regeneraron ni modificaron ZIP.
