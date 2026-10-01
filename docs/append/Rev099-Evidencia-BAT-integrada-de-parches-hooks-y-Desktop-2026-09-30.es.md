# Rev099 — Evidencia BAT integrada de parches, hooks y Desktop

**Fecha:** 30 de septiembre de 2026  
**Alcance:** Core, ForgeWeave, fixture desechable de detours y harness de pruebas/render de Desktop; solo evidencia de validación.

## Resultados verificados

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause` terminó con código 0. Core pasó 397/397 y ForgeWeave pasó 73/73. Las compilaciones seleccionadas `net472` y `net8.0` informaron cero advertencias y cero errores.
- El BAT anidado del fixture serial x64 de detours pasó, incluidos los callbacks inertes durante la descarga y la restauración exacta `15 → 32 → 15`. No se inició directamente `DetourFixture.exe`.
- `tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause` pasó la suite Desktop 65/65 y 292 casos de render; la compilación informó cero advertencias y cero errores.

Este anexo, limitado a evidencia, registra estos resultados BAT comunicados y no agrega afirmaciones de runtime o versión.
