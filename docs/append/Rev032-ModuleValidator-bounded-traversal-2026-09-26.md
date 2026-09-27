# Rev032 — Recorrido acotado del validador de módulos

**Fecha:** 26 de septiembre de 2026  
**Versión:** Calradia Forge 24.0.0, sin cambios; `ForgeApi.Version = 8`  
**Alcance:** Core, pruebas y documentación. Límites de `ModuleValidator.Inspect` y análisis de dependencias.

## Problema observado y justificación técnica

La revisión final encontró que el análisis general de archivos ya acotaba carpetas, profundidad y entradas, pero el validador independiente de `SubModule.xml` enumeraba todos los directorios del nivel superior y recorría archivos XML recursivamente sin esos límites. Una jerarquía profunda también podía consumir la pila durante el análisis de XML y la detección de ciclos. Además, la versión fuente activa debía conservar la detección de módulos declarados como incompatibles.

## Solución técnica y decisiones arquitectónicas

`ModuleValidator` comparte un presupuesto por inspección: hasta 10.000 directorios incluida la raíz seleccionada, profundidad 64 y 200.000 entradas del sistema de archivos; la validación de XML anidado se limita a 10.000 archivos adicionales. Los puntos de reanálisis se omiten. Un límite alcanzado o un error de acceso genera `analysis_scan_incomplete`, por lo que la interfaz no presenta una inspección parcial como completa. Tanto el recorrido de árboles como la detección de ciclos usan pilas explícitas. Las incompatibilidades declaradas se informan si el módulo mencionado está en la raíz inspeccionada.

## Cambios en activos, código y dependencias

Se actualizó `src/CalradiaForge.Core/ModuleValidator.cs`, el estado truncado del analizador `module` en `src/CalradiaForge.Core/ForgeAnalysisCatalog.cs` y las regresiones en `tests/CalradiaForge.Tests/Program.cs`. Las guías de diseño del sistema y los changelogs EN/ES documentan estos límites. No se añadieron dependencias, API públicas ni cambios de versión; los ZIP no se modificaron.

## Validación y límites de la evidencia

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` terminó con código 0.
- Las compilaciones `net472` y `net8.0` reportaron cero advertencias y errores; Core pasó 297/297 y ForgeWeave 65/65.
- Las regresiones cubren el límite de profundidad y los módulos incompatibles. La suite se ejecutó mediante el launcher `.bat`; no se inició directamente un ejecutable de pruebas.
- No se inició Bannerlord ni se verificó comportamiento en vivo. No se generaron ni modificaron ZIPs.

Este anexo amplía el registro anterior sin reemplazar sus párrafos ni reescribir revisiones previas.
