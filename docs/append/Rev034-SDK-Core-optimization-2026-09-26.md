# Rev034 — Optimización medida de SDK y Core

**Fecha:** 26 de septiembre de 2026  
**Versión:** Calradia Forge 24.0.0, sin cambios; `ForgeApi.Version = 9`  
**Alcance:** Implementación interna del SDK, ForgeWeave y Core; regresiones y documentación técnica.

## Problema observado y justificación técnica

La línea base autorizada mostraba trabajo repetido al consultar modificadores y preparar cada despacho ForgeWeave. Core también necesitaba propagar cancelación durante operaciones de lectura y análisis, sin cambiar su contrato público síncrono. Las mediciones existentes de `/status` eran inestables y no justificaban cambios de rendimiento en esa ruta.

## Solución técnica y decisiones arquitectónicas

`ForgeModelRegistry` conserva instantáneas inmutables por categoría y las invalida cuando cambia el registro. Las consultas no copian todas las categorías en cada llamada; `Evaluate` usa la instantánea de la categoría solicitada y ejecuta condiciones fuera del bloqueo. Los consumidores de una instantánea anterior conservan una vista estable, y categorías enum inválidas devuelven una colección vacía sin crear entradas ilimitadas.

ForgeWeave conserva planes de despacho para los tipos finitos de evento distintos de `Custom`, los invalida tras altas o bajas exitosas y mantiene sin caché los planes `Custom` dependientes del tema. Se preservan el orden de despacho, la instantánea por llamada y el comportamiento ante cambios del registro durante un despacho.

Core propaga el token de cancelación por recorridos de analizadores, lecturas acotadas, análisis XML y la inspección de módulos. La firma pública síncrona de `ModuleValidator.Inspect(string)` permanece intacta; el trabajo interno consulta cancelación durante el procesamiento.

## Cambios en activos, código y dependencias

Se modificaron `ForgeModelRegistry`, `ForgeWeaveEngine`, `ForgeAnalysisCatalog` y `ModuleValidator`, junto con pruebas SDK, ForgeWeave y Core, mapas de código y documentación técnica en inglés y español. No se añadieron dependencias ni API pública; los TFM siguen en `net472`/`net8.0`, y el producto y el contrato SDK se mantienen en 24.0.0 y 9, respectivamente.

## Validación y límites de la evidencia

- Se ejecutaron cinco veces `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"`; las cinco terminaron correctamente.
- Cada ejecución informó compilaciones seleccionadas `net472` y `net8.0` con cero advertencias y errores, Core 312/312 y ForgeWeave 68/68. Las pruebas se iniciaron exclusivamente mediante el `.bat`; no se ejecutaron directamente `.exe` ni `.dll` de pruebas.
- En el arnés de consultas `GetModifiers` (1.024 modificadores y 128 consultas), la mediana pasó de 0,590 ms a 0,004 ms. En ForgeWeave Core (200 manejadores, 12 despachos) pasó de 5,789 ms a 2,167 ms; en el arnés ForgeWeave independiente pasó de 6,763 ms a 2,018 ms. Estos datos miden el arnés, no latencia de fotogramas en el juego.
- `Evaluate`, las estadísticas de memoria y el análisis de árboles fuente se midieron sin una línea base histórica comparable; no se atribuye mejora porcentual. La variación de `/status` no se presenta como un problema corregido ni como una ganancia.
- No se inició Bannerlord ni se verificó comportamiento dentro del motor. Los ZIP existentes no se regeneraron ni modificaron.

Este anexo amplía el registro anterior sin reemplazar sus párrafos ni reescribir revisiones previas.
