# Rev132 — Mejoras gráficas y medallones de estudio en WPF

**Fecha:** 2026-10-03

**Versión del producto:** Calradia Forge 25.2.0, sin cambios; `ForgeApi.Version` 13

**Alcance:** Presentación WPF de Desktop, generación de activos de imagen de alta fidelidad, plantillas XAML de estudio, pruebas de regresión y documentación.

## Problema observado y justificación técnica

Revisiones anteriores modernizaron la identidad visual de varios estudios tácticos, pero varios paneles operativos y paneles de inspección permanecían visualmente sin retocar o dependían de marcadores de posición planos temporales de 1,5 KB e iconos genéricos de laurel de 28×28 sin enmarcar:
1. El banco de hooks de código de bytes (`HookWorkbenchControl.xaml`) presentaba encabezados de texto plano sin heráldica temática ni anclajes visuales.
2. Gauntlet UI Studio, Campaign Studio, Delivery Studio y Diagnostics Studio en `ToolPageTemplates.xaml` mostraban marcadores de posición `calradia-laurel-crest-rev089.png` de 28×28 desnudos y sin borde.
3. Cuatro paneles tácticos y de protocolo (Kingdom Diplomacy, Component Generator Forge, Live Session Protocol y Generic Operation Deck) hacían referencia a bloques monocromáticos sólidos de 1,5 KB (`calradia-diplomacy-medallion-rev095.png`, `calradia-mechanism-medallion-rev095.png`, `calradia-pipe-seal-rev096.png` y `calradia-sentinel-eye-rev096.png`).

## Solución técnica y decisiones arquitectónicas

Siguiendo las 9 Dimensiones de Intención Visual de `/high-quality-image-generation` y el post-procesamiento automatizado mediante `tools/process_high_quality_asset.py`:
1. Se generaron y registraron 5 nuevos medallones de estudio heráldicos de alta fidelidad:
   - `calradia-anvil-weave-sigil-rev098.png`: Yunque de hierro de herrero entrelazado con engranajes de relojería y desvíos rúnicos para el encabezado del banco de hooks de código de bytes.
   - `calradia-gauntlet-sigil-rev098.png`: Guantelete blindado sosteniendo un pergamino de cartela arquitectónica y compases de bronce para Gauntlet UI Studio.
   - `calradia-campaign-astrolabe-rev098.png`: Astrolabio de expedición calrádica antigua con brújula cartográfica y grabados de dragón para Campaign Studio.
   - `calradia-delivery-seal-rev098.png`: Sello imperial de cera carmesí con águila heráldica y pergamino de despacho para Delivery Studio.
   - `calradia-diagnostics-aegis-rev098.png`: Égida forense de hierro forjado y latón con ojo centinela vigilante para Diagnostics Studio.
2. Se actualizaron las 4 texturas de 1,5 KB previamente vacías a auténticos recursos RGBA transparentes POT de 512×512 con dilatación de bordes (pad 2) para eliminar halos oscuros bajo filtrado lineal.
3. Se integraron todos los medallones dentro de marcos `Border` temáticos (36×36 o 44×44) con pinceles de latón o cardenillo, escalado de mapa de bits de alta calidad e información sobre herramientas descriptiva.
4. Se añadieron aserciones de regresión `Rev098MajorVisualAndStudioUpgrades()` en `DesktopSimulationServiceTests.cs` para garantizar el registro de recursos, las invariantes de plantilla y cero marcadores de posición de 28×28 sin enmarcar.

## Cambios en activos, código y dependencias

- Se crearon `src/CalradiaForge.Desktop/Resources/Textures/calradia-anvil-weave-sigil-rev098.png`, `calradia-gauntlet-sigil-rev098.png`, `calradia-campaign-astrolabe-rev098.png`, `calradia-delivery-seal-rev098.png` y `calradia-diagnostics-aegis-rev098.png`.
- Se reemplazaron los stubs de 1,5 KB en `src/CalradiaForge.Desktop/Resources/Textures/` con activos de fidelidad completa.
- Se registraron todas las nuevas texturas en `src/CalradiaForge.Desktop/CalradiaForge.Desktop.csproj` como elementos `<Resource>`.
- Se actualizaron `src/CalradiaForge.Desktop/Presentation/HookWorkbenchControl.xaml` y `src/CalradiaForge.Desktop/Resources/Views/ToolPageTemplates.xaml`.
- Se actualizó `tests/CalradiaForge.Desktop.RenderTests/DesktopSimulationServiceTests.cs`.
- No cambiaron los contratos públicos del SDK, el protocolo de cable IPC, el comportamiento del mod Gauntlet ni las dependencias externas.

## Validación y límites de la evidencia

- `dotnet build CalradiaForge.sln -c Release -v:minimal` tuvo éxito con 0 advertencias y 0 errores.
- `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause` superó 296/296 casos de renderizado WPF en 17.779 ms (320 pasadas de diseño, 7.045 ms en llamadas de diseño).
- `tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause` superó todas las suites (Core 100%, ForgeWeave 73/73, Desktop 67/67).
- `tools\Verify-CalradiaForge-StatelessBehavior.bat` superó 4/4 criterios.
- `tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` verificó 131 revisiones del libro mayor y 42 pares de documentos bilingües.
- No se realizó ningún inicio en vivo del motor de Bannerlord ni sesiones de combate.

Este anexo añade una nueva revisión sin reescribir Rev131 ni registros anteriores. Conserva la versión del producto, las rutas, los contratos públicos y el comportamiento del juego.
