# Rev030 — Cierre seguro de registros ForgeWeave reentrantes

**Fecha:** 26 de septiembre de 2026  
**Versión:** Calradia Forge 24.0.0, sin cambios; `ForgeApi.Version = 8`  
**Alcance:** SDK, Core, pruebas y documentación. Recuperación de suscripciones ForgeWeave ante callbacks reentrantes y excepciones de registro con efectos inciertos.

## Problema observado y justificación técnica

La revisión de ciclo de vida encontró que un callback `Unregister` podía conectar un anfitrión durante `Dispose()`, dejando un nuevo manejador registrado después de marcar el handle como dispuesto. Una baja pendiente antigua que reentraba en una conexión nueva también podía cambiar el estado de la generación más reciente a `Failed`. Además, `IForgeEventRegistry.Register` no garantiza que una excepción ocurra antes de modificar el anfitrión; limpiar ciegamente por ID podría retirar otra suscripción que provocó el conflicto.

## Solución técnica y decisiones arquitectónicas

`ForgeWeaveRegistration.Dispose()` activa una guarda temporal para ignorar callbacks de disponibilidad hasta completar la limpieza y la desuscripción. Las rutas de limpieza comprueban la generación después de llamar a código del anfitrión: si ya se estableció una conexión más nueva, conservan su estado y solo agregan el error antiguo al diagnóstico. Si `Register` lanza, el handle pasa a `Failed`, conserva cualquier registro más nuevo que ya conozca y deshabilita nuevos intentos automáticos. No llama a `Unregister` por ID para intentar adivinar si el registro fallido alcanzó a modificarse. `Dispose()` retira los manejadores que sí conoce y conserva `Error` cuando el efecto parcial requiere revisión manual.

## Cambios en activos, código y dependencias

Se modificaron `src/CalradiaForge.Sdk/ForgeCampaignEvents.cs`, los comentarios de contrato en `src/CalradiaForge.Sdk/Contracts.cs` y `tests/CalradiaForge.Tests/ForgeWeaveTests.cs`. Se sincronizaron `docs/sdk-reference.md`, `docs/sdk-reference.es.md`, `docs/CODEMAP_SDK_GAMEMODELS.md` y los changelogs bilingües. No cambiaron firmas públicas, TFM, dependencias, SDK 8 ni la versión de producto 24.0.0; no se modificó el módulo de juego ni se generaron ZIP.

## Validación y límites de la evidencia

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` terminó con código 0.
- Los grafos seleccionados `net472` y `net8.0` compilaron con cero advertencias y errores; Core pasó 294/294 y ForgeWeave 64/64.
- Las pruebas nuevas cubren excepciones después de efectos parciales, limpieza obsoleta con reconexión anidada, preservación del host más reciente, ausencia de rollback inseguro por ID y conexión durante `Dispose()`.
- El launcher `.bat` alojó la ejecución; no se inició directamente ningún ejecutable de pruebas. Bannerlord no se inició y la integración en vivo no se verificó.
- No se generaron ni modificaron ZIP existentes.

Este anexo amplía Rev029 sin reemplazar sus párrafos ni reescribir revisiones anteriores.
