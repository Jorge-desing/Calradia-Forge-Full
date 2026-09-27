# Forja de Calradia 22.0.0

22.0.0 amplía el diagnóstico de recursos Gauntlet con una auditoría estática del atlas: comprueba referencias y límites de `SpriteData`, rutas seguras de las piezas PNG, dimensiones IHDR de las imágenes fuente y dimensiones del atlas. Para TPAC, detecta cabeceras fijas incompletas y cantidades declaradas fuera del rango acotado; el análisis de metadatos sigue siendo una comprobación separada. Conserva el catálogo de extensiones Gauntlet, el banco de ensamblados, la ayuda offline y el escritorio separado. Consulta la [validación breve](VALIDACION-22.0.0.es.md) y los [cambios de la versión](CHANGELOG.es.md).

Forja de Calradia es una suite independiente de herramientas para desarrollar mods de **Mount & Blade II: Bannerlord 1.4.8**. El código y la documentación principal se publican primero en inglés; este documento conserva la guía en español.

Descargas separadas:

- `CalradiaForge-Modules-22.0.0.zip`: módulos, dependencias de AsmResolver, recursos de iconos atribuidos y avisos de terceros.
- `CalradiaForge-Source-SDK-22.0.0.zip`: código, SDK, ejemplos, configuración y sitio estático generado por DocFX, documentación.
- `CalradiaForge-Desktop-22.0.0.zip`: aplicación opcional de .NET 8 sin ejecutable anfitrión `.exe`.

**Publicación en espera:** un análisis de solo lectura posterior encontró que el TPAC Gauntlet de 538 bytes dentro del ZIP Modules 22.0.0 no puede leerse con el parser de metadatos local `TpacTool.Lib`. El ZIP existe, pero la textura de iconos del juego no está verificada; no lo publiques como una entrega de iconos funcional hasta volver a importar el atlas en Resource Browser y pasar el preflight. Consulta el [flujo de recursos](GAME_ICON_ASSETS.es.md) y la [validación posterior](VALIDACION-22.0.0.es.md).

Instala `CalradiaForge` dentro de `Modules` y actívalo después de Native y SandBoxCore en el lanzador oficial. El panel del juego se abre con **F10** y sigue automáticamente el idioma activo de Bannerlord, sin botón manual. Para escritorio, instala .NET 8 Desktop Runtime y ejecuta `Desktop\Run-CalradiaForge-Desktop.bat` desde la carpeta extraída, o `dotnet Desktop\CalradiaForge.Desktop.dll`.

Forge no requiere Harmony, MCM, ButterLib ni servicios de red. ForgeWeave es el marco cooperativo propio: despacha eventos oficiales de Forge, conserva evidencia limitada, aísla fallos, pone en cuarentena manejadores repetidamente fallidos y ofrece Replay Lab protegido. Replay verifica una secuencia de evento conservada; no intercepta métodos ni inyecta cargas arbitrarias.

Las acciones de diagnóstico se basan en archivos locales explícitamente seleccionados. Indican evidencia verificada, análisis estructural, plantilla o estado no disponible. Si una entrada no existe o no es compatible, muestran **No ejecutado** o **No compatible**.

Antes de la comprobación local del juego, ejecuta `tools/Test-BannerlordPreflight.ps1`. Solo lee la superficie instalada, el manifiesto desplegado, el orden seleccionado del lanzador y la presencia de BLSE; no modifica Steam, BLSE, configuración del lanzador ni partidas. Después, `tools/launch_bannerlord.ps1` inicia la comprobación corta mediante Steam (`steam://rungameid/261550`), sin abrir `Bannerlord.exe` directamente.






Consulta [herramientas de ensamblados](ASSEMBLY_WORKBENCH.es.md) para la inspección y edición con vista previa, copia de salida y respaldo.

Para el asistente experimental y opcional de UI del Resource Browser, consulta [automatización de importación](BANNERLORD_IMPORT_AUTOMATION.es.md). No es una dependencia del módulo y sus controles de importación no están verificados para todas las versiones del Editor.

