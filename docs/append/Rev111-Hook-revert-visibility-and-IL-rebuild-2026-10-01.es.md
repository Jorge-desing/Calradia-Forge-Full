# Rev111 — Elegibilidad de reversión de hooks, confirmación visible y reconstrucción IL

**Fecha:** 1 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios; ForgeApi.Version 13  
**Alcance:** Planificación de hooks en consola, confirmación visible en Desktop, reconstrucción IL de Core, compatibilidad de diagnósticos de solo lectura, pruebas y documentación bilingüe.

## Problema observado y justificación técnica

Una selección Revert de consola por propietario o `all` incluía registros Registered y Reverted junto con hooks activos. El planificador estricto del Runtime rechazaba la selección mixta e impedía planificar una retirada que sí era elegible. Los filtros de Desktop también podían ocultar selecciones conservadas sin mostrar claramente su cantidad, y una vista previa larga de confirmación era difícil de revisar en su superficie acotada. Una transformación IL aplicada debía sobrevivir una reconstrucción posterior de la cadena MonoMod fuera del menú sin permitir allí nuevas operaciones de administración. También debía restaurarse la superficie pública de compatibilidad `HarmonyDiagnostics`, de solo lectura, para consumidores existentes.

## Cambios técnicos y decisiones arquitectónicas

- `cf.hook_revert` de consola resuelve únicamente IDs/propietarios registrados y filtra la selección a snapshots Applied, Conflict o Failed antes de preparar un plan. Si no queda ningún registro elegible, informa un resultado vacío explícito sin preparar ni confirmar operaciones. Se conservan las comprobaciones de estado del Runtime, el alcance de IDs seleccionados y la confirmación separada de un solo uso.
- El workbench Desktop muestra la cantidad de selecciones ocultas y ajusta las líneas de la vista previa de confirmación dentro de un área de revisión acotada. Los cambios hacen visibles la selección conservada y la operación propuesta sin aplicar hooks automáticamente.
- La reconstrucción IL utiliza el alcance de la activación aplicada. Una reconstrucción posterior puede restaurar una transformación aplicada explícitamente fuera del menú; allí siguen bloqueadas las nuevas operaciones Apply/Revert. El registro y la prevalidación siguen siendo inertes, y los manipuladores IL continúan como código local confiable en lugar de payloads ejecutables del SDK/IPC.
- La superficie pública de compatibilidad `HarmonyDiagnostics` se restaura como API de diagnóstico de solo lectura. No aplica, retira ni reordena parches y no agrega un backend ejecutable de parcheo Harmony.

## Alcance de fuentes y dependencias

Los cambios afectan la selección de consola en `ForgeCommands.cs`, el manejo de activaciones/reconstrucciones y diagnósticos de Core, la presentación de confirmaciones de Desktop y regresiones enfocadas. La versión del producto continúa en 25.2.0 y la API del SDK en 13. Este anexo no modifica Rev001–Rev110 ni afirma una certificación nueva de dependencias.

## Validación y límites de la evidencia

- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` pasó Core 405/405 y ForgeWeave 73/73 con compilaciones seleccionadas limpias. Salida conservada: `artifacts/hook-rev111-core.txt`.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` pasó Desktop 65/65 y 294 casos de render WPF, con 182 pasadas de render/layout y un resumen de consola del arnés de 14.410 ms. Salida conservada: `artifacts/hook-rev111-desktop.txt`. El tiempo del arnés no equivale a latencia interactiva de la aplicación.
- `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat --no-pause` pasó en el host aislado x64 serial, incluida la regresión de reconstrucción IL 11 → 15 → 11 mientras la administración seguía bloqueada fuera del menú aprobado. Estos fixtures seriales no certifican seguridad mientras otro hilo ejecuta un destino durante cambios de detours.
- La mutación de hooks en Bannerlord y la mutación en Resource Browser siguen sin verificarse en vivo. No se utilizó campaña ni batalla para esta validación. Las pruebas administradas y los fixtures aislados aprobados no demuestran renderizado en el juego, importación nativa ni corrección del ciclo de vida del host real.
- Los tres ZIP de distribución de la versión actual se regenerarán después de esta revisión y su registro de integridad. Este anexo no afirma hashes finales de ZIP ni un resultado de auditoría de archivos.

Esta revisión agrega evidencia a Rev110 y conserva las revisiones protegidas anteriores.
