# Rev029 — cancelación de Desktop, ciclo de vida del módulo y transacciones de ensamblados

**Fecha:** 24-09-2026  
**Línea de versión:** Calradia Forge 22.0.0  
**Alcance:** cancelación de named pipes en Desktop, copias seguras del banco de ensamblados y ciclo de vida de extensiones del módulo.  
**Distribución:** seguimiento solo del código fuente; sin cambios en paquetes.

## Cancelación y named pipes en Desktop

La cancelación ahora se propaga desde las operaciones de sesión de Desktop hasta la conexión, reconexión y el intercambio solicitud/respuesta por named pipe. Las esperas de conexión, reconexión y respuesta siguen teniendo límites. La cancelación solicitada por el llamador se informa como tal y se distingue de un tiempo de espera agotado o de un endpoint desconectado. Si una petición se cancela o vence después de comenzar el envío, se desconecta el pipe para impedir que una respuesta tardía se confunda con la siguiente. Las pruebas locales de named pipes cubren esas rutas.

## Seguridad de copias en el banco de ensamblados

La inspección y la operación de cambio de versión aceptan cancelación durante el cálculo de hashes, la lectura de metadatos, la creación de salida y la preparación del respaldo. La inspección calcula el hash de origen antes y después de leer metadatos y se detiene si el archivo cambió. Los respaldos se copian en bloques acotados mientras se calcula SHA-256; el respaldo temporal solo se confirma cuando su hash coincide con el origen inspeccionado. El ensamblado modificado se escribe en una salida temporal separada, se vuelve a abrir y validar, y se confirma solo después de la última comprobación de cancelación.

La limpieza se limita a los archivos creados por la operación actual. Si otro proceso crea la ruta de salida antes de la confirmación, el banco conserva ese archivo concurrente. Las operaciones fallidas solo eliminan una salida o respaldo confirmado cuyo hash aún coincide con los datos creados por esta operación. Las regresiones Core y Desktop prueban la cancelación antes de confirmar, después de preparar el respaldo y ante una carrera en la ruta de salida.

## Despacho al hilo del juego y contabilidad de la cola

El cierre de páginas de extensiones se despacha al hilo del juego de Bannerlord antes de modificar el estado de Gauntlet. Las finalizaciones y limpiezas de confianza no se descartan silenciosamente cuando se llena la cola acotada de solicitudes de extensiones. La capacidad de esa cola usa contabilidad atómica en vez de contar repetidamente una cola concurrente; pruebas con productores simultáneos comprueban que el límite configurado se mantenga exacto.

## Validación y límites

La validación completa se lanzó con `tools/Run-CalradiaForge-Tests.bat --no-pause`; no se ejecutó directamente ningún ejecutable de pruebas. La compilación de la solución terminó sin advertencias ni errores. Pasaron AssetPipeline, AssetBatchPlan, ResourceBrowser, comparación de inventarios TPAC y preflight FBX. Core pasó 239/239, ForgeWeave 31/31, Desktop 50/50 y el harness de render WPF pasó 268 casos. El harness registró 507 pases de layout en 11,334 segundos. Los scripts de revisión de código y calidad reportaron cero hallazgos en fuente y pruebas.

Estas comprobaciones no miden la latencia de entrada de la aplicación WPF ni de Bannerlord en ejecución. No se inició una sesión del juego, campaña ni batalla. No cambiaron los recursos instalados, archivos TPAC, paquetes ZIP, API pública ni versión de lanzamiento.

Esta entrada es append-only y no modifica Rev028 ni las evidencias anteriores.
