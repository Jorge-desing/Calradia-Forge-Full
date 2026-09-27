# Rev044 — refinamiento del arte del rail WPF en Alto contraste

**Fecha:** 27 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Ilustración del rail WPF Desktop y comprobaciones de render en Alto contraste.

## Problema observado y justificación técnica

El marco vertical transparente Rev063 cabía en el rail de navegación, pero sus bordes oscuros eran difíciles de distinguir sobre la superficie de Alto contraste. El estado visible por sí solo no demostraba que el arte se percibiera ni que preservara el contraste del texto.

## Solución técnica y decisiones arquitectónicas

- Se creó un grabado transparente más claro en latón y verdigrís como `workbench-heraldic-rail-portrait-rev064.png`, conservando la proporción de la fuente 887×1774 y el tamaño optimizado de presentación 320×640.
- Se reemplazó Rev063 en el manifiesto de recursos Desktop y en la plantilla del rail. El derivado Rev063 se conserva como variante histórica/retirada y ya no se incrusta.
- Se preservaron las opacidades existentes por tema (War Table 0,18, Parchment Light 0,12, Alto contraste 0,24), las superficies sólidas de Alto contraste, la carga local, el hit testing pasivo y el ajuste de adornos decorativos.
- Se agregó una aserción de render que compone cada píxel visible del arte del rail con alfa sobre la superficie de navegación Alto contraste. Exige un contraste adorno-superficie mínimo de 1,5:1 y conserva al menos 4,5:1 para texto claro y atenuado.

## Cambios en activos, código y dependencias

El PNG optimizado pesa 147.309 bytes y su SHA-256 es `06D98F717E5E38F597D965E266F1D6B59208C9AD662C2ED91E4F20A6E7AF578C`. El inventario de PNG empaquetados suma 7.516.909 bytes comprimidos y 16.473.296 bytes RGBA decodificados, dentro de los límites configurados. No cambiaron dependencias, contratos públicos, rutas, comandos, permisos, versión del producto ni ZIPs.

## Validación y límites de la evidencia

- `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check` pasó las comprobaciones deterministas de dimensiones, bytes de salida y hashes.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts/desktop-visual-rev064-contrast.json` compiló sin advertencias ni errores, pasó Desktop 59/59 y aprobó 281 casos de render WPF con 158 pases de layout.
- El informe registró 7.802 ms totales y 1.738,8 ms en llamadas de layout. Son tiempos del arnés de render, no latencia de interacción de Desktop. Se inspeccionaron capturas de Alto contraste, Parchment y ventana mínima; la app en vivo y su apariencia con DPI real de Windows siguen sin verificarse.
- No se regeneraron ZIPs ni se inició Bannerlord.

Este anexo amplía la revisión protegida anterior sin sustituir párrafos ni evidencia previos.
