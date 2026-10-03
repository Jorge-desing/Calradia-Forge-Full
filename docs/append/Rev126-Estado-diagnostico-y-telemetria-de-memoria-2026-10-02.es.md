# Rev126 — Estado diagnóstico y telemetría de memoria aceptada

**Fecha:** 02-10-2026  
**Versión del producto:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Semántica de diagnósticos del SDK y Core, telemetría de memoria acotada de campaña, evidencia de simulación Desktop y guías de agentes.

## Problema observado y justificación técnica

El inspector de parches externos usaba el indicador `Truncated` en algunos fallos de recopilación aunque no necesariamente se hubiera recortado la salida. El comportamiento de memoria de campaña aumentaba la telemetría incluso cuando el SDK rechazaba una escritura tras alcanzar la cuota global de agentes. El helper de time-slicing carecía de una prueba explícita para IDs nulos o vacíos, y la regresión Desktop de inspección de memoria todavía esperaba un contrato anterior de runtime vivo/verificado, aunque el servicio devuelve una muestra ilustrativa.

## Solución técnica y decisiones arquitectónicas

El inspector ahora marca la salida como truncada solo cuando un límite real de salida o recopilación recorta datos; los fallos de consulta, enumeración y omisión de ensamblados permanecen como evidencia incompleta. Los contadores de memoria de campaña solo avanzan si `TryAdd`/`TryUpsert` acepta la escritura. Las pruebas documentan que `ForgeTimeSlicer` asigna IDs nulos y vacíos al bucket cero, y que los llamadores deben usar IDs estables y no vacíos; no cambia el comportamiento de runtime. La prueba de render Desktop ahora verifica el texto existente de muestra inspirada en CoALA y el estado de evidencia `Sample`, en vez de afirmar que existe un snapshot de registro en vivo.

## Cambios en activos, código y dependencias

Se actualizaron el manejo del resultado del inspector acotado, la telemetría de escrituras de memoria de campaña, las regresiones de time-slicing y cuotas del SDK, las expectativas de pruebas de simulación Desktop y el conocimiento especialista de depuración, rendimiento, simulación, UI y coordinación de agentes. No se añadió dependencia de runtime, API pública, cambio de versión, publicación de paquetes ni integración con TaleWorlds.

## Validación y límites de la evidencia

- `tools\Run-CalradiaForge-Tests.bat --no-pause` aprobó: compilaciones limpias `net472`, `net8.0` y Desktop (0 advertencias, 0 errores); Core 415/415; Patch Diagnostics 28/28; ForgeWeave 73/73; Desktop 65/65; render WPF 295 casos y 308 pases de layout/render.
- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause` aprobó durante los cambios enfocados de time-slicing y memoria. El BAT aislado de diagnósticos aprobó 28/28.
- Las duraciones de render y llamadas de layout son mediciones del arnés, no latencia de la aplicación abierta. No se usó Bannerlord, campaña, batalla, runtime de terceros en vivo, integración IDE ni publicación pública de NuGet para validar comportamiento.

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos. La versión del producto 25.2.0, los contratos públicos del SDK y las rutas existentes del módulo permanecen sin cambios.
