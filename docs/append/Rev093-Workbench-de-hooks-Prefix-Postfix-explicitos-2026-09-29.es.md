# Rev093 — Reconciliación archivística de la fuente

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Recuperación archivística de la fuente desde el changelog publicado; sin cambios de código ni ejecución.

**Nota de reconciliación archivística:** Este anexo fuente se reconstruyó el 30/09/2026 a partir de las secciones ya publicadas en los changelogs inglés y español. No existía un anexo fuente original de esta revisión en `docs/append/`. Este respaldo no modifica ni sustituye ningún DOCX protegido o registro de integridad previo.

## Workbench de hooks Prefix/Postfix explícitos de Calradia Forge — 29/09/2026 (Rev093)

- Agrega la capacidad opcional `ForgeApi.Hooks` / `IForgeHookService` para registrar explícitamente hooks Prefix y Postfix mediante MonoMod.RuntimeDetour 25.3.6 en el host Bannerlord `net472`. El registro no instala hooks; los callbacks se ejecutan sincrónicamente en el hilo que llama al destino. Esta capacidad es independiente de la prevalidación declarativa de Patch Blueprints y de la API separada de reemplazo de métodos.
- Agrega acciones protegidas del Hook Workbench en WPF. El operador revisa los snapshots, prepara un plan Apply/Revert, marca la casilla de confirmación y confirma con un token de un solo uso. IPC acepta solo los IDs de hooks seleccionados y el token; los callbacks, delegates y destinos ejecutables no cruzan el pipe. Apply/Revert se limita al contexto exacto del menú principal, en el hilo del juego y sin campaña, misión ni sesión multijugador activa. Los comandos de consola son `cf.hook_status [owner]`, `cf.hook_apply <id>` y `cf.hook_revert <id|owner|all>`.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` pasó Core 372/372 y ForgeWeave 73/73. Las compilaciones Core `net472`/`net8.0` y Mod `net472` terminaron limpiamente. El fixture aislado de detours pasó mediante `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat`; no se ejecutó `DetourFixture.exe`.
- El fixture es serial y no demuestra seguridad frente a un hilo concurrente que ejecute el destino durante la instalación o retirada del hook; el backend sigue siendo experimental. No se realizó una sesión real de Bannerlord o Modding Kit ni una importación de recursos. La versión permanece en 25.2.0; no se generaron ni modificaron ZIP de distribución.
