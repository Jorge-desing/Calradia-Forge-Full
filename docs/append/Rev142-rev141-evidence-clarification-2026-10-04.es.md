# Rev142 — Aclaración de accesibilidad y evidencia de Rev141

**Fecha:** 04-10-2026

**Versión:** Calradia Forge 25.2.0; Forge API 13 sin cambios

**Alcance:** Errata documental sobre accesibilidad WPF y evidencia de validación repetible. No hay cambios de fuente ni de comportamiento del producto.

## Problema observado y justificación

El anexo protegido Rev141 agrupó de forma amplia la pista localizada de ciclo con controles que recibieron nombre/ayuda de UI Automation. El XAML actual muestra que la pista de ciclo es texto localizado; el filtro de favoritos sí tiene nombre/ayuda accesibles localizados según su estado, y el botón Ping del pie tiene nombre/ayuda localizados y conserva su comando y AutomationId. Por ello, la frase original de Rev141 requiere una precisión sin editar su documento protegido.

Algunos artefactos conservados de Rev141 pertenecían a ejecuciones anteriores. Se capturó por separado una ejecución integrada final para asociar los conteos actuales y las métricas del arnés de render a un log y JSON identificables.

## Aclaración técnica y decisiones

- Rev141 localizó la pista de ciclo y la etiqueta de favoritos. Añadió nombre/ayuda accesibles dependientes del estado y tooltip al filtro de favoritos, y nombre/ayuda accesibles a Ping. No añadió propiedades UI Automation independientes a la pista de ciclo.
- La ejecución integrada final está en `artifacts/rev141-full-suite-final4.txt`; su reporte de render WPF es `artifacts/desktop-render-rev141-final4.json`. La consola BAT informa 25.994 ms para la suite de render; el JSON registra 25.997 ms totales y 11.405 ms en 320 pases de layout/render. Son mediciones del arnés, no latencia de la aplicación.
- Como Rev141 y su registro de hash ya están protegidos, esta aclaración se anexa como Rev142; los párrafos y hashes previos no se modifican.

## Archivos y evidencia

- Se precisó el texto compartido en `AGENTS.md`, `CODEX.md` y `GEMINI.md`, y la guía de accesibilidad WPF en `.agents/skills/calradia-forge-desktop/SKILL.md`.
- `tools/Run-CalradiaForge-Tests.bat --no-pause`: 0 advertencias/errores; Core 436/436, ForgeWeave 74/74, Desktop 75/75 y WPF con 296 casos y 320 pases de layout/render aprobados.
- `tools/Test-CalradiaForge-Desktop-Uia.bat`: inspección de solo lectura aprobada, 23/23 registros observados. Es evidencia estructural de UI Automation, no aprobación visual.
- No se inició Bannerlord. No se afirma comportamiento en vivo. Las versiones del producto/API, contratos, dependencias, rutas, permisos y frameworks objetivo permanecen sin cambios.

Este anexo complementa Rev141 sin sustituirlo ni reemplazar revisiones protegidas anteriores.
