# Rev071 — Seguimiento documental y de validación del motor de parches

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Copy de Gauntlet, mapas de arquitectura, 13 catálogos nativos y validación por BAT.

## Correcciones documentales y de localización

- El panel Gauntlet distingue Harmony Atlas, de solo lectura, de Patch Blueprint Preflight y del backend explícito de reemplazo. El preflight comprueba estructuralmente las referencias declaradas de destino y callback frente a ensamblados ya cargados por el host; no carga ensamblados, ejecuta callbacks, aplica hooks ni demuestra que pueda instalarse un reemplazo nativo.
- Los mapas de arquitectura ahora coinciden con la superficie pública de `ForgeDetour`: `Patch(MethodInfo original, MethodInfo replacement)`, `Unpatch(MethodInfo original)` y `UnpatchAll()`. Ya no describen ForgeDetour como gestión de Harmony ni muestran un escaneo implícito de parches al iniciar.
- El generador de recursos de idioma ahora incluye las traducciones existentes de Gauntlet Page Blueprint, su descripción y Page title, además del copy corregido del motor de parches. Los 13 catálogos nativos generados contienen 710 claves coincidentes; la auditoría informa paridad válida.

## Evidencia de validación

- `tools/Run-CalradiaForge-Tests.bat --no-pause` terminó correctamente. La solución completa compiló con 0 advertencias y 0 errores. Core informó 369/369; el fixture serial x64 devolvió `15 → 32 → 15` y verificó la restauración exacta; ForgeWeave informó 73/73; Desktop informó 63/63; y el renderizador WPF pasó 292 casos.
- El fixture y las pruebas administradas se ejecutan mediante launchers BAT. El fixture invoca el destino en serie; estos resultados no demuestran seguridad si otro hilo puede ejecutar un destino durante una escritura de memoria. El detour sigue siendo experimental y no garantiza seguridad concurrente.
- No se inició Bannerlord ni el Modding Kit, ni se abrió una campaña o batalla. No se regeneraron ZIPs. La versión del producto es 25.2.0 y `ForgeApi.Version` es 11.

Este anexo amplía el registro protegido existente. Las revisiones anteriores permanecen intactas.
