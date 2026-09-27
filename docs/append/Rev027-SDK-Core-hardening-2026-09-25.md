# Rev027 — Endurecimiento de SDK y analizadores Core

**Fecha:** 25 de septiembre de 2026  
**Versión:** Calradia Forge 24.0.0, sin cambios; `ForgeApi.Version = 8`  
**Alcance:** SDK, Core, pruebas y documentación. Correcciones de límites temporales, rutas de configuración, ciclo de vida ForgeWeave y análisis local acotado.

## Problema observado y justificación técnica

La auditoría detectó varios casos límite reproducibles: una duración negativa extrema podía desbordar el cálculo de expiración semántica; `ModSettings` aceptaba identificadores que se podían interpretar como rutas; una baja ForgeWeave fallida podía perder la referencia al registro activo y las transiciones reentrantes podían dejar una generación anterior sobrescribiendo el estado nuevo; el despacho de analizadores podía inferir una ruta a partir de palabras de un identificador desconocido; y el recorrido de carpetas o archivos comprimidos necesitaba límites verificables para mantener acotados el tiempo y el volumen de lectura.

## Solución técnica y decisiones arquitectónicas

- El TTL semántico no positivo vence inmediatamente sin sumarse a la hora actual; un TTL positivo que excede el intervalo representable se satura en `DateTimeOffset.MaxValue`. El comportamiento TTL de memoria episódica y procedimental no cambia.
- `ModSettings.Register` y `Save` aceptan únicamente un componente válido de nombre de archivo, canonicalizan el destino y comprueban que permanezca bajo el directorio `ModSettings`. Los identificadores seguros, incluidos espacios y Unicode, se conservan.
- ForgeWeave conserva la referencia y afinidad de hilo del registro activo cuando `Unregister` falla, para que una transición posterior o `Dispose()` pueda reintentar. Las generaciones evitan que una reconexión reentrante antigua sobrescriba el estado más reciente. Se mantiene la API y el comportamiento fail-fast aprobados.
- Core resuelve únicamente aliases de analizador explícitos; los IDs desconocidos permanecen `Unsupported`. El recorrido de carpetas es iterativo y comprueba cancelación, omite puntos de reanálisis desde la raíz y sus descendientes y está acotado a 10.000 directorios incluida la raíz, profundidad 64, 200.000 entradas y 1–2.000 archivos solicitados (1.000 por defecto). Los recorridos parciales se exponen como advertencia y `Truncated`.
- Los archivos comprimidos que exceden `MaximumBytesPerFile` se rechazan antes de construir `ZipArchive` o inspeccionar entradas. Los archivos aceptados se limitan a `MaximumFiles` entradas y reportan las omitidas como truncadas.

## Cambios en activos, código y dependencias

Se modificaron `src/CalradiaForge.Sdk/ForgeAgentMemory.cs`, `ForgeCampaignEvents.cs` y `ModSettings.cs`; `src/CalradiaForge.Core/ForgeAnalysisCatalog.cs`; y las pruebas de `tests/CalradiaForge.Tests/`. Se actualizaron las referencias SDK en inglés y español, las guías System Design y Desktop y los changelogs bilingües mediante nuevas entradas append-only. No se añadieron dependencias, firmas públicas, referencias TaleWorlds ni cambios a los TFM. El producto fuente permanece en 24.0.0 y el contrato en 8.

## Validación y límites de la evidencia

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` terminó con código 0.
- Compilación seleccionada de `net472` y `net8.0`: 0 advertencias y 0 errores.
- Core: 286 pruebas aprobadas, 0 fallidas. ForgeWeave: 56 pruebas aprobadas, 0 fallidas.
- El launcher `.bat` alojó la biblioteca de pruebas en el proceso de PowerShell; no se inició un ejecutable de pruebas directamente.
- No se inició Bannerlord ni se verificó el comportamiento en vivo dentro del juego. No se generaron ni modificaron ZIPs.

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos. Mantiene los comandos, las rutas, las API públicas y la versión del producto.
