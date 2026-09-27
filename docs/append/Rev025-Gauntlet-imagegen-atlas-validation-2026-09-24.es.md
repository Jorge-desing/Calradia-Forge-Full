# Rev025 — validación del atlas ImageGen de Gauntlet

**Fecha:** 2026-09-24  
**Línea de versión:** Calradia Forge 22.0.0  
**Alcance:** Texturas Gauntlet fuente, validación del prefab y evidencia de pruebas.  
**Distribución:** Seguimiento solo del código fuente; no se modificaron TPAC ni ZIP.

## Canal de recursos fuente completado

Los cuatro maestros materiales generados se procesaron de forma reproducible como sprites RGBA8: `forge_dark_wood` (128×64), `forge_inkwash` (128×64), `forge_patina_brass` (128×16) y `forge_pine_felt` (128×32). El generador oficial SpriteSheetGenerator de TaleWorlds reconstruyó el atlas fuente y `CalradiaForgeSpriteData.xml` en un módulo aislado de preparación. El atlas resultante mide 2048×128; SpriteData registra las 13 partes en la categoría de carga permanente y se validaron los límites declarados de cada sprite. El prefab y los sprites fuente anteriores, junto con el atlas y los metadatos, se respaldaron y verificaron por hash antes del reemplazo.

El validador de sprites decorativos pasó y reportó vigente el registro de SpriteData. El auditor estructural de Gauntlet pasó con cero errores y cero avisos, incluidas la pasividad de los adornos, la protección de controles y evidencia, y la comprobación de intersecciones del diseño. La solución compiló con cero advertencias y cero errores. Las pruebas Core ejecutadas desde un `.bat` pasaron 232/232 y las pruebas ForgeWeave 31/31. La comprobación visual Gauntlet por lotes terminó correctamente después de reconstruir la biblioteca de pruebas.

## Límite de ejecución

El TPAC instalado es anterior al atlas nuevo. No se reemplazó ni se importó desde Resource Browser, y no se observó el render dentro del juego. La apariencia de la textura en tiempo de ejecución queda pendiente de la importación manual mediante Resource Browser y de una breve inspección en el menú. Esta revisión no afirma que el TPAC ni el render en vivo estén verificados. El producto sigue en 22.0.0; no se generaron ZIP.

Este registro es append-only y no modifica Rev021–Rev024 ni evidencias anteriores.
