# Rev043 — ilustración vertical del rail WPF

**Fecha:** 26 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Ilustración del rail de navegación WPF Desktop y presentación por tema.

## Problema observado y justificación técnica

La antigua decoración del rail era una imagen horizontal que no se adaptaba a la superficie de navegación vertical y no se utilizaba en la plantilla actual. El rail necesitaba una composición vertical detrás de sus controles que no recibiera eventos de entrada ni compitiera con el texto.

## Solución técnica y decisiones arquitectónicas

- Se agregó una ilustración vertical transparente de diario de campo, creada localmente y optimizada desde su fuente de 887×1774 a un recurso WPF RGBA de 320×640.
- Se reemplazó el recurso horizontal obsoleto en el manifiesto del proyecto y en la plantilla del rail. La fuente histórica se conserva en el directorio de texturas; el manifiesto de preparación identifica como retirado su derivado optimizado anterior.
- Se mantuvieron `Stretch=Uniform`, el muestreo bitmap de alta calidad, el binding existente de visibilidad decorativa y la configuración pasiva frente a entrada y foco.
- Se agregaron tokens de opacidad por tema: War Table 0,18, Parchment Light 0,12 y Alto contraste 0,24. Las superficies de Alto contraste siguen siendo sólidas.

## Cambios en activos, código y dependencias

El PNG optimizado pesa 126.258 bytes y su SHA-256 es `DF8C90E403A3B4D5B8476824B72FEFF006C83023165A4924A4CE084EFF362638`. El inventario de recursos del proyecto suma 7.495.858 bytes comprimidos y 16.473.296 bytes RGBA decodificados, dentro de los límites configurados. No cambiaron dependencias, rutas, API pública, comandos, permisos, versión del producto ni ZIPs.

## Validación y límites de la evidencia

- Pasaron `tools/Prepare-CalradiaForge-Desktop-Textures.bat` y `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check`, incluida la comprobación determinista del resultado.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts/desktop-visual-rev063-opacity/render.json` compiló sin advertencias ni errores, pasó Desktop 59/59 y aprobó 281 casos de render con 158 pases de layout.
- El arnés reportó 7.998 ms totales y 1.779,8 ms en llamadas de layout. Son mediciones del arnés, no latencia de interacción de la aplicación. Se inspeccionaron capturas renderizadas, pero esto no equivale a una revisión de la aplicación en vivo ni a verificarla con el DPI real de Windows.
- No se regeneró ningún ZIP ni se inició el juego.

Este anexo amplía la revisión protegida anterior sin sustituir sus párrafos ni evidencia previos.
