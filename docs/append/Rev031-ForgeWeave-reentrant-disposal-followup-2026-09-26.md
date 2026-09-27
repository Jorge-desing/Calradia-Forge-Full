# Rev031 — Detener transiciones después de una disposición reentrante

**Fecha:** 26 de septiembre de 2026  
**Versión:** Calradia Forge 24.0.0, sin cambios; `ForgeApi.Version = 8`  
**Alcance:** SDK, pruebas y documentación. Reentrada de `Dispose()` durante limpiezas ForgeWeave pendientes.

## Problema observado y justificación técnica

Una limpieza de un anfitrión obsoleto llama código del anfitrión y puede reentrar en `Dispose()` mientras una transición de registro externa todavía está en la pila. La disposición anidada libera las suscripciones y establece `Disposed`, pero sin volver a comprobar ese estado el callback externo podría continuar hacia un nuevo `Register`. Si ese registro lanzara después de modificar el anfitrión, el handle ya dispuesto podría dejar un efecto no controlado.

## Solución técnica y decisiones arquitectónicas

`OnRegistryChanged` comprueba `Disposed` inmediatamente después de cada operación externa que puede reentrar (`RetryPendingUnregistrations` y `UnregisterActiveHandler`). Si la disposición ocurrió durante la llamada, conserva el estado y termina la transición exterior antes de iniciar otro registro. Los errores de limpieza que aún se produzcan se agregan al diagnóstico sin resucitar el handle.

## Cambios en activos, código y dependencias

Se modificaron `src/CalradiaForge.Sdk/ForgeCampaignEvents.cs` y `tests/CalradiaForge.Tests/ForgeWeaveTests.cs`. Se añadió Rev045 a los changelogs inglés y español y se actualizó el checklist de `task.md`. No cambiaron API pública, dependencias, TFM, contrato SDK 8, versión 24.0.0, módulos ni ZIP.

## Validación y límites de la evidencia

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` terminó con código 0.
- Las compilaciones seleccionadas `net472` y `net8.0` reportaron cero advertencias y errores; Core pasó 295/295 y ForgeWeave 65/65.
- La regresión dispone el handle desde la baja pendiente de un anfitrión obsoleto mientras el anfitrión disparador fallaría después de `Register`; confirma que el registro exterior no se ejecuta y que los manejadores conocidos quedan retirados.
- El `.bat` alojó la suite; no se inició directamente ningún ejecutable de pruebas. No se inició Bannerlord ni se verificó comportamiento en vivo; los ZIP permanecen sin cambios.

Este anexo amplía Rev030 sin reemplazar sus párrafos ni reescribir revisiones anteriores.
