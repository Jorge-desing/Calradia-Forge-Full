# Rev076 — Renovación heráldica ilustrada de Gauntlet

**Fecha:** 27 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Recursos visuales Gauntlet, prefab generado, auditoría estructural y evidencia acotada de F10.

## Objetivo observado y justificación técnica

El espacio de identidad de la cabecera y el rail de navegación usaban ilustraciones materiales/cartográficas anteriores. Esta ronda sustituye esas dos capas de presentación por ilustraciones heráldicas originales y mantiene legibles la navegación de ocho áreas y sus controles. También registra la evidencia actual sobre la aserción histórica de F10 sin afirmar una causa ni una reparación que no se hayan verificado.

## Solución técnica y decisiones arquitectónicas

Los IDs pasivos existentes ahora usan `forge_heraldic_header_v2` y `forge_heraldic_rail_v2`. La cabecera conserva su rectángulo reservado de 220×42 DIP. El rail usa un ancho fijo de 128 DIP, centrado, y una altura máxima de 256 DIP; su altura disponible disminuye con los viewports admitidos. El arte nuevo sigue siendo un fondo no interactivo y la auditoría exige límites alfa de 88/255 para la cabecera y 64/255 para el texto de guía del rail.

La preparación determinista y la generación del atlas fuente oficial produjeron un atlas de 4096×512 con 32 registros de sprites: 17 iconos de juego y 15 sprites decorativos. La auditoría Gauntlet comprueba los IDs de sprite, los límites alfa, los indicadores pasivos, el centrado del rail y la contención en los perfiles de viewport admitidos.

## Cambios en activos, código y dependencias

- Se añadieron recursos locales de autoría ImageGen y sus versiones preparadas para la cabecera y el rail; se actualizaron las entradas de preparación de recursos, el prefab generado, `SpriteData` y las expectativas de la auditoría estructural.
- No se reemplazó el TPAC instalado. Es anterior al nuevo atlas fuente; la importación en Resource Browser y el renderizado en el juego siguen pendientes.
- No cambiaron API, rutas, comandos, permisos, versión del producto, dependencias ni ZIPs.

## Evidencia de validación y límites

- Pasaron `tools/Prepare-CalradiaForge-ImageGenTextures.bat --check --no-pause`, `tools/Test-CalradiaForge-DecorativeTextureDeterminism.bat --no-pause` y `tools/Validate-CalradiaForge-DecorativeSprites.bat --no-pause`.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` pasó con 0 errores estructurales y 0 advertencias de auditoría; su suite Core pasó 339/339.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` pasó Core 339/339 y ForgeWeave 73/73. La compilación Release de la solución terminó sin advertencias.
- Pasó la regresión de alternancia F10 y ciclo de vida de la capa. Una comparación de solo lectura encontró que los SHA-256 del prefab fuente e instalado son idénticos: `5741D7571C254B6487477D601F0CFE016AA254A1DFC6BD6795766274543B2F45`; ambos contienen el `<ScrollbarWidget>` canónico del motor y no la escritura `<ScrollBarWidget>`. El registro persistido de una sesión anterior indica apertura/carga y cierre del panel. Esto no reproduce de nuevo la aserción histórica; su causa exacta y su resolución actual siguen sin verificarse.
- No se inició Bannerlord. La importación y la aprobación visual en vivo quedan pendientes; las comprobaciones estructurales del código fuente no demuestran el renderizado en el juego.

Este anexo agrega evidencia después de Rev055 sin reemplazar párrafos previos del registro. La versión del producto sigue en 25.2.0.
