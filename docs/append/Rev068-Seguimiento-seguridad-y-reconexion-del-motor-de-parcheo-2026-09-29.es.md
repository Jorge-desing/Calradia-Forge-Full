# Rev068 — Seguimiento de seguridad por página y reconexión del motor de parcheo

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** SDK, Core, preflight de parches, regresiones y documentación bilingüe del SDK.

## Problema observado y justificación técnica

El escritor de bytes ejecutables cambia la protección del tramo de detour de 13 bytes y restaura un único valor devuelto para la primera dirección. Un tramo que cruce el límite entre páginas del sistema podría aplicar sobre la página vecina el valor de la primera. La revisión del ciclo de vida también encontró que desconectar y reconectar el mismo registro dejaba cerrado permanentemente el servicio de parches integrado. Por último, los nombres por sí solos no identifican con precisión parámetros genéricos: un parámetro del tipo contenedor y uno del método pueden llamarse `T` aunque ocupen posiciones de firma distintas.

## Solución técnica y decisiones arquitectónicas

- Rechaza direcciones nulas, longitudes no positivas, desbordamiento del rango y cualquier escritura que cruce una página del sistema antes de llamar a `VirtualProtect`. Sigue permitida una escritura que termine exactamente en el límite de página.
- Añade la interfaz opcional separada `IForgePatchServiceLifecycle` en lugar de agregar un miembro obligatorio a `IForgePatchService`, para mantener compatibles los servicios existentes. El servicio integrado actualiza todos los registros y reanuda solicitudes únicamente cuando cada registro está en `Reverted`, existe y coincide su captura de bytes originales y no queda ningún destino rastreado. La conexión invoca esta capacidad antes de publicar el registro entrante o reabrir el gate compartido de detours.
- Representa las referencias genéricas con tokens de tipo de propietario y posición: `!0` para el parámetro del tipo contenedor y `!!0` para el del método. Preflight compara estos tokens en vez de aceptar nombres coincidentes.
- Las regresiones cubren una escritura de una sola página, el rechazo de un cruce de página sin llamadas a protección/escritura/flush, reconexión segura de la misma instancia, conexión repetida, bloqueo de reconexión con conflicto y discrepancia de ámbito genérico.

## Cambios en código y dependencias

Los cambios se limitan a la validación del rango de detour, la capacidad opcional de ciclo de vida del SDK y su implementación integrada, la identidad genérica de firmas en preflight, las pruebas y las guías emparejadas en inglés/español del SDK y Patch Blueprint. No cambian dependencias de ejecución, miembros de `IForgeRegistry`, acciones de escritura IPC, comportamiento ForgeWeave, versión del juego o del producto ni archivos de distribución. `ForgeApi.Version` permanece en 11.

## Validación y límites de la evidencia

- `cmd.exe /c "tools\Run-CalradiaForge-Core-Tests.bat -NonInteractive <nul"` pasó: Core 363/363 y el fixture desechable x64 confirmó la secuencia serial `15 → 32 → 15` con reversión exacta.
- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` pasó con compilaciones `net472` y `net8.0` limpias, 0 advertencias, 0 errores, Core 363/363, el fixture serial x64 y ForgeWeave 73/73.
- Un intento integrado anterior informó un fallo Core que no volvió a aparecer en la ejecución Core aislada ni en el reintento integrado posterior. El resultado integrado final aceptado es el del reintento aprobado.
- No se inició Bannerlord ni el Modding Kit. Los fixtures llaman al destino en serie alrededor de las escrituras; no demuestran seguridad si otro hilo puede ejecutar el método durante la escritura de código. El backend de detour sigue siendo experimental.

Este anexo complementa Rev067 sin modificar los párrafos de revisiones anteriores. La versión permanece en 25.2.0; la capacidad del SDK sigue en 11 y los ZIP no cambian.
