# Rev029 — Reintentos ForgeWeave seguros ante callbacks anidados

**Fecha:** 25 de septiembre de 2026  
**Versión:** Calradia Forge 24.0.0, sin cambios; `ForgeApi.Version = 8`  
**Alcance:** SDK, pruebas y documentación. Iteración de bajas ForgeWeave obsoletas durante reconexiones reentrantes.

## Problema observado y justificación técnica

Al reintentar varias bajas que habían fallado, `Unregister` puede invocar código del host y reentrar en otra conexión. Ese callback anidado también puede retirar elementos de la lista de limpiezas pendientes. Recorrer la lista mutable por índices permitía que el marco exterior accediera a una posición que el callback anidado ya había eliminado.

## Solución técnica y decisiones arquitectónicas

`RetryPendingUnregistrations` toma una instantánea local de las referencias pendientes antes de llamar al host. Cada limpieza ignora un registro que ya no forme parte de la lista y mantiene la bandera `InProgress` para que la reentrada no intente retirar de nuevo el mismo handle. Las fallas todavía conservan el registro para un intento posterior. Se agregó una regresión que crea dos bajas fallidas, reentra a una nueva conexión durante el reintento y comprueba el estado y la limpieza del host final.

## Cambios en activos, código y dependencias

Se modificaron `src/CalradiaForge.Sdk/ForgeCampaignEvents.cs` y `tests/CalradiaForge.Tests/ForgeWeaveTests.cs`. Se añadió Rev043 a los changelogs inglés y español. No se modificaron las firmas públicas, los TFM, las dependencias, la versión del producto, los ZIP ni el módulo del juego.

## Validación y límites de la evidencia

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` terminó con código 0.
- Compilación seleccionada de `net472` y `net8.0`: cero advertencias y cero errores.
- Core: 289 pruebas aprobadas, cero fallidas. ForgeWeave: 59 pruebas aprobadas, cero fallidas.
- El launcher `.bat` alojó las pruebas; no se invocó directamente ningún ejecutable de pruebas.
- No se inició Bannerlord ni se verificó la integración en vivo. Los ZIP existentes no se regeneraron ni modificaron.

Este anexo amplía Rev028 sin reemplazar su contenido ni reescribir las revisiones anteriores. Mantiene las API públicas y la versión del producto.
