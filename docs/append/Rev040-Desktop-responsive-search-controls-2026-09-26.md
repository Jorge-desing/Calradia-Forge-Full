# Rev040 — Corrección de tamaño adaptable para búsquedas y acciones WPF

**Fecha:** 26 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Desktop WPF. Tamaño y ajuste de los campos de búsqueda, acciones de cabecera y botones de órdenes de trabajo.

## Problema observado y justificación técnica

En la ventana mínima, los campos de búsqueda y sus botones ocupaban demasiado espacio o recortaban texto. Los controles de paleta, Split Deck y dossier también podían competir por ancho en la cabecera, y la barra de acciones de una orden podía desbordar su columna. La corrección debía mantener las 194 rutas, los comandos existentes, la localización y las dimensiones mínimas del shell.

## Solución técnica y decisiones arquitectónicas

- Se fijó una altura de 42 DIP para las búsquedas del rail y la paleta de comandos, con texto centrado verticalmente.
- El botón de limpiar mide 32×32 DIP y el campo reserva relleno para que el botón no cubra el texto buscado.
- A 980×680 DIP, los controles de cabecera de la paleta de comandos, Split Deck y dossier tienen anchos mínimos de 116, 120 y 126 DIP, con una altura mínima de 36 DIP.
- Los botones de órdenes de trabajo miden 34 DIP de alto, limitan su ancho y se ajustan dentro de la columna disponible. El harness confirmó que las acciones quedan visibles tanto en el tamaño mínimo como en el normal.
- No se modificaron API, SDK, IPC, catálogo de rutas, comandos, permisos ni MVVM.

## Cambios en activos, código y dependencias

- Se mantuvieron las dos decoraciones locales de Rev057 y sus dimensiones: `titlebar-cartographic-panorama-rev057.png` (2172×67) y `dossier-corner-ornament-rev057.png` (384×256). El delta de ambos recursos es 310.173 bytes comprimidos y 975.312 bytes RGBA decodificados. El inventario empaquetado total es 7.465.788 bytes comprimidos y 15.921.104 bytes RGBA decodificados; el BAT de texturas confirmó salidas deterministas.
- No se agregaron dependencias ni recursos en esta corrección. Se mantuvo la versión del producto 25.2.0 y no se regeneraron ZIPs.

## Validación y límites de la evidencia

- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause`: compilación sin advertencias ni errores, Desktop 56/56 y una corrida de render con compilación, 277 casos/161 pases de layout, 9.410 ms.
- Cinco repeticiones comparables sin compilación por el launcher `.bat`: 277 casos/161 pases cada una. Tiempos `milliseconds`: 8.754, 8.928, 8.281, 8.355 y 8.093 ms; mediana 8.355 ms. Tiempos de llamadas síncronas de layout `renderLayoutMilliseconds`: 1.685,9, 1.826,3, 1.652,8, 1.709,0 y 1.507,1 ms; mediana 1.685,9 ms. Los informes usan destinos únicos con el prefijo `artifacts/desktop-render-rev057-controls-repeat-` y sufijos `01`–`05`.
- Comparación homogénea con Rev057 final2: 276 casos/161 pases y medianas de 8.360 ms totales y 1.732,6 ms de layout. Rev058 cambia -0,06 % en la mediana total y -2,7 % en layout; el resultado es esencialmente estable, no una mejora sustancial. Estas métricas pertenecen al harness, no a la latencia interactiva.
- `tools/Test-CalradiaForge-Desktop-Uia.bat`: 20/20 comprobaciones de accesibilidad de solo lectura en `artifacts/desktop-uia-rev057-controls-final.json`; no se ejecutaron órdenes de trabajo ni se alteraron preferencias. `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check`: aprobado.
- La matriz produce rasterizaciones a distintas escalas, pero no cambia el DPI del sistema ni simula el layout con DPI real de Windows. UI Automation comprueba el árbol accesible, no la apariencia; no se afirma aprobación visual humana. Bannerlord no se inició.

Este anexo amplía Rev039 sin sustituir sus párrafos ni sus evidencias. La versión, la API pública, los permisos y los archivos ZIP permanecen sin cambios.
