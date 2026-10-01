# Rev105 — Recuperación acotada de fallos de reemplazo atómico sin cambio de nombres

**Fecha:** 1 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Persistencia de ajustes SDK, persistencia JSON Core y regresiones tras la auditoría del motor de hooks.

## Problema observado y justificación técnica

Rev104 conservaba una observación de almacenamiento sin resolver. La décima ejecución diagnóstica de Core capturó `HRESULT=80070497` en `ModSettings.TrySave`, identificando Win32 `ERROR_UNABLE_TO_REMOVE_REPLACED` (1175). El fallo de reemplazo JSON observado por separado tenía el mismo texto de error Windows. Esto identifica la operación de almacenamiento fallida, no el proceso responsable.

El contrato ReplaceFileW de Microsoft indica que el error 1175 mantiene ambos archivos con sus nombres originales. Los errores 1176 y 1177 pueden tener otros resultados y no deben tratarse como el mismo estado recuperable. Fuente: https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-replacefilew .

## Solución técnica y decisiones arquitectónicas

El helper interno compartido `AtomicFileReplacement` repite únicamente HRESULT 80070497, solo mientras existen el archivo preparado y el destino, y como máximo tres veces después del intento inicial. Las esperas son de 20, 40 y 60 ms, con un máximo solicitado de 120 ms. Conserva `File.Replace`; no hay alternativa de borrar/mover ni reintentos de resultados inciertos. Los errores persistentes conservan y propagan la excepción original. La limpieza del llamador y la actualización de caché después de confirmar siguen intactas.

Core `Json.Save` y SDK `ModSettings.TrySave` usan este helper. Los serializadores siguen ejecutándose fuera del bloqueo de confirmación por mod. Las esperas acotadas ocurren durante una confirmación fallida; los guardados correctos normales no agregan demora. Esto no promete recuperación frente a bloqueos persistentes, permisos denegados, otros procesos ni pérdida de energía.

## Cambios en código y registros

- Se agregó el helper interno sin cambiar API pública ni versión de capacidad SDK.
- Se agregó una regresión determinista con operaciones de reemplazo y espera inyectadas, archivos preparados reales y comprobaciones de éxito después de rechazos transitorios, presupuesto de rechazo persistente, conservación exacta de excepción, preservación del contenido original/preparado, fuente ausente y ausencia de reintentos para errores 32, 5, 1176 y 1177.
- Se conserva la aceptación concurrente de alias con distinta capitalización y se refuerza el diagnóstico con HRESULT. La aserción JSON conserva la excepción original aunque falle la inspección del contenido retenido.
- Rev104 sigue inmutable y registra la incertidumbre anterior. Este anexo bilingüe registra la causa capturada posteriormente y la corrección acotada. Sin cambios en sesión del juego, TPAC, paquetes, ZIPs ni versión del producto.

## Validación y límites de la evidencia

`tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` compiló sin advertencias ni errores y aprobó Core 402/402, ForgeWeave 73/73 y el fixture aislado serial de detours/hooks. Las pruebas siguen ejecutándose mediante BAT, sin iniciar directamente un ejecutable de fixture. La inyección determinista demuestra la decisión y los límites de reintento; las ejecuciones correctas del sistema de archivos no establecen qué actor externo causó el error anterior ni garantizan todos los futuros fallos de almacenamiento.

La revisión del backend aceptada por el usuario está registrada en Rev104. Los límites de concurrencia experimental de hooks y detours directos no cambian. Debe comprobarse la validación remota para el SHA exacto subido antes de informar la entrega.
