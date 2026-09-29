# Rev069 — Corrección del preflight del límite de página

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Preflight de detours del SDK, registro de lotes, regresiones y BAT del fixture.

## Problema hallado en la revisión

La guarda de límite de página se ejecutaba originalmente en el escritor de bajo nivel después de que un parche directo o un miembro del lote reservara su recibo en el registro. Por eso la excepción dejaba un registro fallido aunque no se hubiera modificado memoria ejecutable. Ese registro obsoleto impedía reutilizar el destino o el ID del parche. En un lote, también podía detectarse un tramo que cruzaba página después de leer o reservar miembros anteriores.

## Corrección

- `Patch` directo ahora valida el tramo de destino de 13 bytes bajo el gate del registro, después de las comprobaciones de host, destino e ID, pero antes de leer los bytes originales o reservar estado.
- `PatchBatch` valida los límites de página de todos sus miembros antes de leer bytes originales, copiar recibos o publicar entradas. El escritor de bajo nivel conserva su propia guarda como defensa adicional.
- Las regresiones usan un tamaño de página sintético determinista en las rutas públicas directa y por lote. Comprueban que no cambien los contadores del adaptador simulado de lectura/protección/escritura/flush, que no queden snapshots rastreados ni recibos heredados y que se puedan reutilizar y revertir los mismos destinos e IDs al permitir el tamaño de página.
- El BAT del fixture desechable ahora limita la espera del proceso hijo para que un smoke nativo bloqueado no mantenga indefinidamente el launcher de pruebas.

## Validación y límites de evidencia

- `tools/Run-CalradiaForge-Core-Tests.bat -NonInteractive` pasó la suite administrada Core: 363/363. El fixture desechable x64 compiló para `net472` con cero advertencias y cero errores.
- El fixture nativo serial más reciente se bloqueó tras iniciarse y fue detenido. Por lo tanto, la ejecución global más reciente del BAT Core y el smoke nativo no se informan como aprobados; no se ha confirmado un resultado exitoso del timeout. Las nuevas regresiones administradas verifican la limpieza del estado ante rechazo sin escritura, no la ejecución de detours nativos.
- No se inició Bannerlord ni el Modding Kit. El backend sigue siendo experimental y no garantiza seguridad si otro hilo puede ejecutar un destino durante una escritura de código.
- Esta entrada corrige únicamente el estado de pruebas del árbol más reciente. Rev068 y los registros append-only anteriores permanecen intactos. La versión del producto es 25.2.0, `ForgeApi.Version` es 11 y los ZIP de distribución no cambian.
