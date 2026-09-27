# Rev028 — Límites de memoria, reentrancia y lecturas acotadas

**Fecha:** 25 de septiembre de 2026  
**Versión:** Calradia Forge 24.0.0, sin cambios; `ForgeApi.Version = 8`  
**Alcance:** SDK, Core, pruebas y documentación. Seguimiento a la auditoría de ciclo de vida y límites de análisis.

## Problema observado y justificación técnica

Una revisión independiente encontró tres casos que podían debilitar las garantías añadidas en Rev027: las clases públicas de cada nivel de `ForgeAgentMemory` permitían construir almacenes independientes fuera de la cuota global; una reconexión reentrante durante `IForgeEventRegistry.Register` podía dejar el manejador enlazado al host anterior y al nuevo; y comprobar el tamaño de archivo antes de `ReadAllText` no imponía un límite efectivo si otro proceso ampliaba el archivo después de la comprobación. Además, el límite comprimido de un ZIP por sí solo no limitaba la cantidad de objetos de entrada que `ZipArchive` podía materializar.

## Solución técnica y decisiones arquitectónicas

- Se conservaron los constructores públicos de `SemanticMemory`, `EpisodicMemory` y `ProceduralMemory` por compatibilidad de fuente, pero todas sus instancias comparten el almacenamiento estático de proceso. Así, los contadores, las cuotas y `ClearAgent`/`ClearAll` cubren cualquier objeto de esos tipos.
- `ForgeWeaveRegistration` vuelve a comprobar la generación después de retornar de `Register`. Si el callback fue superado por una conexión reentrante, retira el manejador del registro obsoleto sin reemplazar el registro activo nuevo. Si esa retirada falla, guarda el registro y la afinidad de hilo para reintentarlo en una transición posterior o en `Dispose()`.
- La lectura de textos abre un flujo y cuenta los bytes realmente leídos hasta el límite, detectando crecimiento concurrente. La carga XML utiliza la misma lectura acotada antes de aplicar el parser con DTD prohibido.
- Antes de crear `ZipArchive`, Core lee el registro final del directorio central con un búfer acotado. Rechaza registros ilegibles, archivos multidisco y recuentos declarados superiores a 10.000; de ese modo la construcción del directorio de entradas queda dentro de ese máximo. Los archivos aceptados conservan el límite solicitado de entradas inspeccionadas y marcan las omitidas como truncadas.

## Cambios en activos, código y dependencias

Se actualizaron `src/CalradiaForge.Sdk/ForgeAgentMemory.cs` y `ForgeCampaignEvents.cs`, `src/CalradiaForge.Core/ForgeAnalysisCatalog.cs`, las regresiones en `tests/CalradiaForge.Tests/ForgeWeaveTests.cs`, `SdkFeaturesTests.cs` y `ForgeAnalysisTests.cs`, y las referencias SDK y System Design en inglés y español. Se anexó Rev042 a los changelogs bilingües sin modificar Rev039–Rev041. No se añadieron dependencias, firmas públicas, TFM, comandos, ZIPs ni cambios de versión.

## Validación y límites de la evidencia

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` terminó con código 0 después de estos cambios.
- Compilación seleccionada de `net472` y `net8.0`: cero advertencias y cero errores.
- Core: 288 pruebas aprobadas, cero fallidas. ForgeWeave: 58 pruebas aprobadas, cero fallidas.
- La biblioteca de pruebas se alojó dentro del flujo del launcher `.bat`; no se ejecutó ningún ejecutable de pruebas directamente.
- Bannerlord no se inició ni se validó el comportamiento en vivo. Los ZIP existentes no se regeneraron ni modificaron.

Este anexo amplía la evidencia de Rev027 sin reemplazar sus párrafos ni reescribir revisiones anteriores. Mantiene las firmas públicas, las rutas y la versión del producto.
