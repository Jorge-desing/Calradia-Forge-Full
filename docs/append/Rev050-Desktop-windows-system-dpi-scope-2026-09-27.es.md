# Rev050 — Aclaración de cobertura de render con DPI del sistema Windows

**Fecha:** 27 de septiembre de 2026<br>
**Versión:** Calradia Forge 25.2.0, sin cambios<br>
**Alcance:** Evidencia del arnés de render WPF de Desktop independiente y documentación de cobertura de escala.

## Problema observado y justificación técnica

El arnés de render genera vistas previas de bitmap etiquetadas al 100 % y 200 %, mientras el host WPF aislado conserva el mismo DPI de Windows y layout DIP fijo. Esas etiquetas de vista previa podían interpretarse como validación con los ajustes de escala de pantalla de Windows, incluidos 125 %, 150 % o 200 % de DPI del sistema.

## Solución técnica y decisiones

La documentación ahora distingue la densidad raster del bitmap frente al DPI del sistema operativo. Los factores de vista previa `[1, 2]` de `RenderTargetBitmap` solo cambian la densidad de píxeles de salida; no cambian el DPI de la ventana host, sus dimensiones DIP ni la escala de layout WPF. El JSON de auditoría proporcionado marca explícitamente `windows-system-dpi-layout-coverage` como `not-simulated` y deja vacío `appliedWindowsDpiScaleFactors`. Por ello, el layout con DPI del sistema Windows al 125 %, 150 % y 200 % sigue sin verificarse.

La auditoría conserva los casos funcionales existentes de rutas, idiomas, temas e interacciones; no se retiró cobertura funcional. Esta corrección delimita el alcance de la evidencia y no cambia el arnés ni la aplicación.

## Cambios en activos, código y dependencias

- Se añadió una sección aclaratoria Rev070 a las guías Desktop y los changelogs fuente en inglés y español.
- Este seguimiento documental no modificó código de la aplicación, implementación de pruebas, recursos, rutas, API ni dependencias.
- La versión del producto sigue en 25.2.0; no se regeneraron ZIPs.

## Validación y límites de la evidencia

- El artefacto proporcionado `artifacts/desktop-visual-rev069-scale-audit.json` registra `passed: true`, Desktop 59/59, 287 casos de render WPF, 172 pases de layout y cero advertencias/errores de compilación.
- El artefacto registra 9.321 ms de tiempo total del arnés y 2.134,7 ms en llamadas de layout. Son mediciones del arnés, no latencia de la aplicación abierta.
- El JSON marca `windows-system-dpi-layout-coverage` como `not-simulated`; los factores raster `[1, 2]` no equivalen a pruebas de DPI de Windows. No se ejecutaron pruebas durante este cambio solo documental ni se validó la app/DPI en vivo.

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos. Rev070 del changelog fuente y Rev050 del Registro protegido son contadores independientes.
