# Rev033 — Correcciones de SDK, API y auditoría de Core

**Fecha:** 26 de septiembre de 2026  
**Versión:** Calradia Forge 24.0.0, sin cambios; `ForgeApi.Version = 9`  
**Alcance:** SDK, Core, pruebas y documentación. Registro de modelos, datos de entidades, estadísticas locales, helpers del motor y `ModRuleAuditor`.

## Problema observado y justificación técnica

La auditoría encontró predicados de modificadores ejecutados mientras el registro mantenía su bloqueo, retención de entradas externas vacías en `ForgeData`, estadísticas `/agents` que no reflejaban el almacenamiento real y helpers públicos que podían aparentar éxito sin realizar una acción. También encontró rutas en `ModRuleAuditor` donde un recorrido incompleto o una entrada no procesable podían evitar un error, un control Pulse que aceptaba valores por debajo de su mínimo, una lista de IDs de analizadores que no era realmente inmutable y una aserción de pruebas Gauntlet desfasada respecto a los IDs existentes del prefab.

## Solución técnica y decisiones arquitectónicas

`ForgeModelRegistry.Evaluate` ahora copia las inscripciones bajo bloqueo y evalúa predicados fuera de él. El nuevo `ForgeModelRegistrationScope` retira únicamente las generaciones que todavía pertenecen a su propietario; las firmas heredadas `Register` y `Unregister` se conservan. `ForgeData.RemoveForgeData<T>` elimina la entrada externa cuando ya no quedan datos tipados sin borrar inserciones concurrentes del SDK. `/agents` conserva el campo `agents` y agrega conteos de memoria sin exponer identidades, claves ni cargas.

Los helpers públicos sin una implementación del motor verificada fallan explícitamente con `NotSupportedException`, manteniendo sus firmas y sin fingir que ejecutaron una acción. `ModRuleAuditor` aplica límites de recorrido y lectura compartidos con Core; los enlaces de reanálisis no se siguen y el análisis incompleto, los datos ilegibles o demasiado grandes y el XML inválido producen hallazgos de error. La excepción de `tools` se restringe a la carpeta superior confiable del repositorio. La regla estática exige un intervalo Pulse numérico verificable de al menos 50 ms e ignora comentarios y cadenas; ForgeWeave en ejecución conserva su mínimo independiente de 250 ms. `BuiltInAnalyzerIds` ahora es una colección de solo lectura.

## Cambios en activos, código y dependencias

Se actualizaron `ForgeModelRegistry`, `ForgeData`, `ForgeAgentMemory`, `ForgeLocalApi`, `ForgeHelpers`, `ModRuleAuditor` y `ForgeAnalysisCatalog`, además de las pruebas SDK/Core/ForgeWeave y la aserción Gauntlet obsoleta. Se sincronizaron las referencias SDK, servicios compartidos, ForgeWeave, Diseño del Sistema y el codemap SDK en inglés y español donde corresponde. No se añadieron dependencias, no se cambió el TFM y no se modificó el prefab.

## Validación y límites de la evidencia

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` terminó con código 0.
- Las compilaciones seleccionadas `net472` y `net8.0` reportaron cero advertencias y errores.
- Core pasó 301/301 y ForgeWeave 65/65. Las regresiones cubren predicados reentrantes/fallidos, reemplazo y disposición de registros propietarios, limpieza concurrente de `ForgeData`, privacidad de estadísticas, errores y límites del auditor, throttle Pulse, colección inmutable y puntos de reanálisis.
- Las pruebas se lanzaron mediante `.bat`; no se ejecutó directamente ningún `.exe` ni `.dll` de pruebas.
- No se inició Bannerlord. Las acciones que requieren estado del motor siguen sin verificación en vivo. Los ZIP existentes no se regeneraron ni modificaron.

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos ni reescribir revisiones previas.
