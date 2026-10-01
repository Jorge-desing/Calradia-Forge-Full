# Rev087 — Reconciliación archivística de la fuente

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Recuperación archivística de la fuente desde el changelog publicado; sin cambios de código ni ejecución.

**Nota de reconciliación archivística:** Este anexo fuente se reconstruyó el 30/09/2026 a partir de las secciones ya publicadas en los changelogs inglés y español. No existía un anexo fuente original de esta revisión en `docs/append/`. Este respaldo no modifica ni sustituye ningún DOCX protegido o registro de integridad previo.

## Seguimiento de validación del motor de parcheo de Calradia Forge — 29/09/2026 (Rev087)

- Se completó el fixture aislado de detour x64 mediante `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat --no-pause`. El launcher compila en una carpeta temporal única y elimina solo esa salida; el fixture serial verificó la secuencia `15 → 32 → 15` y la reversión exacta.
- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` pasó con compilaciones `net472` y `net8.0` limpias, cero advertencias y cero errores, Core 361/361, el fixture nativo aislado y ForgeWeave 73/73. Las regresiones incluyen reversión de lote explícito, reconexión con conflicto y recuperación manual, propiedad del servicio, incertidumbre de bytes y colisiones del comando reservado `all`.
- Este seguimiento actualiza el estado pendiente de pruebas de Rev086 con evidencia BAT ya completada. No se inició una sesión de Bannerlord ni del Modding Kit. Los fixtures llaman los destinos en serie y no demuestran seguridad si otro hilo pudiera ejecutarlos durante una escritura; el backend sigue siendo experimental.
- La versión permanece en 25.2.0; la capacidad del SDK usa la versión 11. No cambian ForgeWeave, las rutas de escritura IPC ni los ZIP de distribución.
