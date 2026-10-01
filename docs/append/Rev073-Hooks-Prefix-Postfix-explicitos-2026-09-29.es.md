# Rev073 — Servicio de hooks Prefix/Postfix explícitos y workbench protegido

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** SDK, Core, Mod, Desktop, avisos de dependencias y documentación. Agrega una capacidad opcional de hooks en ejecución y su flujo protegido en WPF.

## Problema observado y justificación técnica

Patch Blueprint Preflight es una descripción declarativa y revisión estructural de solo lectura; no puede aplicar un hook en ejecución. La API experimental existente de reemplazo de métodos es una capacidad separada y no ofrece el ciclo de vida Prefix/Postfix solicitado ni el flujo de revisión para el operador. Los cambios de código en ejecución requieren registro explícito, confirmación y restricción a un contexto conocido del juego.

## Solución técnica y decisiones arquitectónicas

Agrega la capacidad opcional `ForgeApi.Hooks` / `IForgeHookService`, independiente del registro declarativo de blueprints y de `ForgeApi.Patches`. Los únicos tipos de hook ejecutables admitidos son Prefix y Postfix, mediante MonoMod.RuntimeDetour 25.3.6 en el host Bannerlord `net472`. RuntimeDetour no se usa en Core `net8.0` ni en Desktop. El registro no aplica hooks; aplicar, verificar y revertir son operaciones explícitas. Los callbacks se ejecutan sincrónicamente en el hilo que invoca el destino, y el propietario es una etiqueta de seguimiento, no un límite de autorización.

Las operaciones de hooks solo se permiten en la pantalla exacta del menú principal de Bannerlord, en el hilo del juego y sin campaña, misión ni sesión multijugador activa. Hook Workbench de WPF revisa snapshots, crea un plan Apply/Revert para los IDs seleccionados y exige una casilla explícita de confirmación y un token de un solo uso antes de confirmar. IPC transporta únicamente los IDs seleccionados y el token; los delegates, callbacks, destinos `MethodInfo` y definiciones ejecutables permanecen dentro del proceso. Si la confirmación se interrumpe o su resultado es incierto, se deben actualizar los snapshots antes de preparar otro plan; no se reintenta el token. Los comandos de consola son `cf.hook_status [owner]`, `cf.hook_apply <id>` y `cf.hook_revert <id|owner|all>`.

Este backend sigue siendo experimental. El fixture serial no demuestra seguridad si otro hilo ejecuta el destino mientras se instala o retira un hook.

## Cambios en activos, código y dependencias

- Se agregó el contrato de hooks opcional del SDK y su integración en el host; la versión de capacidad del SDK avanzó según queda registrada en el código fuente.
- Se agregó al Hook Workbench de WPF un plan acotado, confirmación explícita mediante casilla/token y acciones IPC que transportan únicamente IDs para snapshots, planes y confirmaciones.
- Se añadieron comandos de consola para consultar, aplicar y revertir hooks, y se declararon MonoMod.RuntimeDetour 25.3.6, sus dependencias de ejecución y sus allowlists de despliegue únicamente para el host Bannerlord `net472`.
- Se actualizaron las guías de blueprints de parche en inglés y español para distinguir hooks ejecutables de prevalidación declarativa y de los parches de reemplazo de métodos.
- La versión del producto permanece en 25.2.0. No se generó ni modificó ningún ZIP de distribución.

## Validación y límites de la evidencia

- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: Core 372/372 y ForgeWeave 73/73.
- Core `net472` y `net8.0`, y Mod `net472`, compilaron limpiamente.
- `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat`: el fixture aislado de detours pasó. La prueba no se lanzó ejecutando directamente `DetourFixture.exe`.
- El fixture es serial; estos resultados no prueban seguridad ante ejecución concurrente del destino durante la instalación o retirada del hook. No se inició una sesión real de Bannerlord ni del Modding Kit y no se verificó comportamiento en juego ni una importación de Resource Browser.

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos. Los comandos y rutas existentes permanecen, y la versión del producto sigue siendo 25.2.0.
