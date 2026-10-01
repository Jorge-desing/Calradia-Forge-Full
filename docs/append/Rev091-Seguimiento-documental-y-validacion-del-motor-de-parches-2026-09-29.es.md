# Rev091 — Reconciliación archivística de la fuente

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Recuperación archivística de la fuente desde el changelog publicado; sin cambios de código ni ejecución.

**Nota de reconciliación archivística:** Este anexo fuente se reconstruyó el 30/09/2026 a partir de las secciones ya publicadas en los changelogs inglés y español. No existía un anexo fuente original de esta revisión en `docs/append/`. Este respaldo no modifica ni sustituye ningún DOCX protegido o registro de integridad previo.

## Seguimiento documental y de validación del motor de parches — 29/09/2026 (Rev091)

- Aclara el texto de Gauntlet y los mapas de arquitectura: Patch Blueprint Preflight es una revisión estructural de solo lectura de las referencias declaradas para destino y callback frente a ensamblados ya cargados. No aplica parches ni ejecuta callbacks, y resolver referencias no demuestra que se pueda instalar un reemplazo nativo. Harmony Atlas es un inventario de solo lectura independiente de los parches Harmony presentes en ensamblados ya cargados.
- Corrige la referencia de `ForgeDetour` para mostrar su API real de reemplazo mediante `MethodInfo` y su alcance experimental. El ciclo de inicio y los diagramas ahora reflejan que el módulo no busca ni aplica parches al iniciar, y que el preflight está separado de las solicitudes explícitas de reemplazo.
- Regenera los 13 catálogos nativos con 710 claves coincidentes. La auditoría de localización pasó, incluidas las etiquetas Gauntlet Page Blueprint, su descripción y Page title.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` pasó: compilación completa sin advertencias ni errores; Core 369/369; fixture serial x64 de detour `15 → 32 → 15` con reversión exacta; ForgeWeave 73/73; Desktop 63/63; y 292 casos de render WPF. No se inició Bannerlord ni el Modding Kit.
- El fixture invoca el destino de forma serial y no demuestra seguridad mientras otro hilo pueda ejecutar un destino durante una escritura de memoria. El backend de detour sigue siendo experimental. La versión permanece en 25.2.0, `ForgeApi.Version` en 11, y no cambian la superficie de escritura IPC, el comportamiento de ForgeWeave ni los ZIP de distribución.
