# Rev092 — Reconciliación archivística de la fuente

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Recuperación archivística de la fuente desde el changelog publicado; sin cambios de código ni ejecución.

**Nota de reconciliación archivística:** Este anexo fuente se reconstruyó el 30/09/2026 a partir de las secciones ya publicadas en los changelogs inglés y español. No existía un anexo fuente original de esta revisión en `docs/append/`. Este respaldo no modifica ni sustituye ningún DOCX protegido o registro de integridad previo.

## Corrección del host BAT del fixture de detour y localización del panel — 29/09/2026 (Rev092)

- El fixture desechable de detour ahora se compila como una biblioteca `net472` x64 y se ejecuta únicamente mediante `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat`, cargada en un proceso temporal x64 de Windows PowerShell. Ya no crea ni inicia `CalradiaForge.DetourFixture.exe`; el BAT comprueba que no se haya emitido un apphost. La prueba permanece alineada con el runtime de Bannerlord. Un ensayo bajo el JIT de .NET 8 no detectó el reemplazo en la etapa 4 y se descartó, sin reportarlo como fixture aprobado.
- Corrige el texto del juego para dejar claro que Patch Blueprint Preflight es estructural y de solo lectura, que Harmony Atlas sigue siendo un inventario separado de solo lectura y que la guía de ForgeWeave Replay no implica que el preflight aplique detours. Siete claves de copy ya están traducidas en los 13 catálogos nativos.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` pasó la compilación completa con 0 advertencias y 0 errores; ForgeWeave 73/73; Desktop 63/63; y 292 casos de render WPF. El fixture serial x64, alojado por el BAT, devolvió `15 → 32 → 15` con restauración exacta. Cada uno de los 13 catálogos nativos contiene 737 claves y la auditoría de localización es válida.
- El fixture sigue siendo serial y no demuestra seguridad ante ejecución concurrente durante escrituras de código máquina. No se inició Bannerlord ni el Modding Kit, y no se invocó la generación de paquetes en esta corrección. La versión permanece en 25.2.0 y `ForgeApi.Version` en 11.
