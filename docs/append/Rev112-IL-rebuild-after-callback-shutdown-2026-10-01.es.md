# Rev112 — Reconstrucción IL después de detener los callbacks de ejecución

**Fecha:** 1 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios; ForgeApi.Version 13  
**Alcance:** Reconstrucción de activaciones IL de Core durante la detención de callbacks, regresión aislada de detours y documentación bilingüe.

## Problema observado y justificación técnica

Detener los callbacks de ejecución durante la descarga debe impedir nuevas llamadas de callbacks de juego y nuevas operaciones de administración. Una transformación IL ya aplicada tiene un ciclo de vida distinto: un ILHook externo puede reconstruir la cadena del mismo destino mientras la transformación autorizada permanece instalada. Rechazar su reconstrucción únicamente porque se detuvieron los callbacks podía dejar esa cadena incoherente con la activación conservada.

## Solución técnica y decisiones arquitectónicas

La ruta de reconstrucción reconoce la activación conservada exacta que se autorizó y aplicó explícitamente antes. Esa activación puede reconstruir su transformación determinista ya aplicada después de `StopCallbacksForUnload`, incluso cuando el contexto del host no está autorizado y se agrega o retira un ILHook externo. Esto conserva la coherencia de la cadena; no reanuda la distribución de callbacks de juego Prefix/Postfix/Finalizer ni permite un nuevo Apply, Revert o desconexión fuera del contexto aprobado. La limpieza aprobada sigue restaurando el destino original. Un Undo incierto permanece conservado y bloqueado en lugar de informarse como retirada verificada.

## Alcance de fuentes y dependencias

La actualización cubre la interoperabilidad de activación/descarga de hooks en Core y el fixture desechable x64 de detours existente. No agrega payloads ejecutables al SDK o IPC, descubrimiento/aplicación automática de hooks ni incremento de versión del producto o SDK. Rev001–Rev111 permanecen intactas.

## Validación y límites de la evidencia

- `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat --no-pause` pasó en el host aislado x64 serial con 0 advertencias y 0 errores.
- Con `StopCallbacksForUnload`, el contexto del host no autorizado y la incorporación/retirada de un ILHook externo, la activación autorizada exacta conservada reconstruyó los valores 11 → 15 → 11. Apply, Revert y CanDisconnect continuaron rechazados en ese contexto.
- La limpieza aprobada restauró el valor original 3. La regresión de Undo incierto permaneció conservada y bloqueada.
- Los resultados cubren el fixture serial determinista y no certifican ejecución concurrente general, todas las implementaciones externas de ILHook ni todos los ciclos de descarga del host. La validación real en Bannerlord sigue sin verificarse; no se afirma evidencia de campaña o batalla.
- Este anexo no afirma hashes de ZIP de distribución ni resultados finales de auditoría de archivos. El empaquetado sigue a la finalización de la revisión y su registro de integridad.

Esta revisión agrega evidencia de interoperabilidad de descarga a Rev111 sin modificar revisiones históricas protegidas.
