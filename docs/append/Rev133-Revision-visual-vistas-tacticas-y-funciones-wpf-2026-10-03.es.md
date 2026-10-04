# Rev133 — Revisión visual de vistas tácticas y funciones residuales WPF

**Fecha:** 2026-10-03

**Versión:** Calradia Forge 25.2.0, sin cambios; `ForgeApi.Version` 13

**Alcance:** Presentación WPF de escritorio, mejora de texturas de alta resolución, paleta de comandos, tarjetas de telemetría de estado, dossier de herramientas, vista dividida (split deck), barra de estado de pie de página, riel de navegación, pruebas de renderizado y libro mayor de mejoras de solo anexión.

## Problema observado y justificación técnica

Si bien las revisiones previas modernizaron los estudios tácticos y añadieron cinco medallones temáticos de alta fidelidad, diversas superficies operativas clave y vistas residuales de `CalradiaForge.Desktop` permanecían visualmente sin retoques:
1. `CommandPaletteControl.xaml` presentaba un dial de 16 px sin marco, etiquetas de prefijo planas y elementos de lista sin bordes ni acentos visuales por categoría.
2. `WorkbenchStatusControl.xaml` empleaba líneas inferiores genéricas de 2 px e iconos desnudos de 14 px en sus cinco tarjetas de estado (`Ui.Context`, `Ui.TestPermission`, `Ui.ReportState`, `Ui.ActiveTool`, `Ui.RetainedEvidence`).
3. `ToolDossierControl.xaml` mostraba comandos de consola en texto plano, atajos en chips sencillos y una imagen de sello de cera de 36 px sin un marco heráldico dedicado.
4. `WorkbenchWorkspaceControl.xaml` utilizaba un dial marcador de posición de 14 px en el encabezado del mazo dividido y filas de evidencia sin insignias de estado.
5. `WorkbenchFooterControl.xaml` empleaba una imagen de 14 px sin bordes e indicadores de texto plano.
6. Cuatro texturas residuales en `src/CalradiaForge.Desktop/Resources/Textures/` (`calradia-mind-medallion-rev093.png`, `imperial-wax-seal-rev085.png`, `calradia-astrolabe-dial-rev086.png` y `calradia-aquila-seal-rev087.png`) eran imágenes planas históricas (de 21 KB a 52 KB) con profundidad de color reducida.

## Solución técnica y decisiones arquitectónicas

1. **Elevación de texturas maestras de alta resolución**:
   - Se re-renderizó `calradia-mind-medallion-rev093.png` (482 KB) como un medallón 3D de 512×512 POT con mecanismo de engranajes de relojería, bisel biselado de latón/bronce y sinapsis neuronales brillantes en cardenillo/oro para el Inspector de Memoria de Agentes inspirado en CoALA.
   - Se re-renderizó `imperial-wax-seal-rev085.png` (398 KB) como un sello de cera carmesí imperial auténtico con borde orgánico fundido, núcleo estampado con águila bicéfala y textura de cera microfracturada.
   - Se re-renderizó `calradia-astrolabe-dial-rev086.png` (445 KB) como un astrolabio de navegación de precisión con graduaciones de 360°, coordenadas celestes y rosa de los vientos en latón.
   - Se re-renderizó `calradia-aquila-seal-rev087.png` (522 KB) como un medallón imperial en bajorrelieve de oro y bronce de estilo romano con rayos y corona de laurel.
2. **Revisión táctica de la paleta de comandos (`CommandPaletteControl.xaml`)**:
   - Se incorporó un medallón enmarcado de latón de 36×36 con escalado de alta calidad en el encabezado modal.
   - Se convirtieron los selectores de prefijo en chips táctiles de comando interactivos (`ALL`, `>live`, `>diag`, `>asset`, `>sim`) con acentos de borde por categoría.
   - Se actualizaron los elementos de la lista a tarjetas tácticas con franja izquierda de 4 px para el color de categoría, píldora de grupo y placa de latón de tipo (`KindLabel`).
   - Se reemplazaron las pistas de atajos del pie con estilo de teclas físicas biseladas.
3. **Revisión de tarjetas de telemetría de estado (`WorkbenchStatusControl.xaml`)**:
   - Se envolvieron los iconos superiores derechos en insignias circulares de 20×20 (`CornerRadius="10"`) con fondo `CoalBrush` y pinceles de borde por categoría (`BrassBrush`, `VerdigrisBrush`, `EmberBrush`).
   - Se elevaron las líneas inferiores de acento a barras táctiles de estado de 3 px (`Opacity="0.8"`).
4. **Pulido del dossier de herramientas y terminal (`ToolDossierControl.xaml`)**:
   - Se estilizaron las entradas de comandos de consola en bloques de terminal con prompt de latón (`$ `), sintaxis monoespaciada en cardenillo y botones de copia refinados.
   - Se elevaron los chips de atajos a teclas físicas biseladas.
   - Se encerró el sello de cera imperial mejorado en un medallón enmarcado de 42×42 con fondo `DeepPineBrush` y borde de latón.
5. **Mejoras en el mazo dividido y pie de página**:
   - Se sustituyó el dial de 14 px en `WorkbenchWorkspaceControl.xaml` por un medallón enmarcado de 32×32 con `tactical-dial-plate-rev084.png` y se agregaron píldoras de estado a las filas de evidencia.
   - Se actualizó `WorkbenchFooterControl.xaml` con una insignia de dial circular enmarcada, estado de ping IPC en cardenillo brillante y teclas biseladas.
   - Se agregó una insignia de icono enmarcada a `WorkbenchNavigationControl.xaml`.
6. **Verificación automatizada**:
   - Se añadió `Rev099TacticalViewsAndUnretouchedFunctionsPolish()` en `DesktopSimulationServiceTests.cs` verificando los contratos de elementos XAML y el umbral de tamaño de archivos de textura (>350 KB).

## Cambios en activos, código y dependencias

- Se actualizaron las texturas en `src/CalradiaForge.Desktop/Resources/Textures/`: `calradia-mind-medallion-rev093.png`, `imperial-wax-seal-rev085.png`, `calradia-astrolabe-dial-rev086.png`, `calradia-aquila-seal-rev087.png`.
- Se actualizaron las vistas de presentación: `CommandPaletteControl.xaml`, `WorkbenchStatusControl.xaml`, `ToolDossierControl.xaml`, `WorkbenchWorkspaceControl.xaml`, `WorkbenchFooterControl.xaml`, `WorkbenchNavigationControl.xaml`.
- Se actualizó el conjunto de pruebas: `tests/CalradiaForge.Desktop.RenderTests/DesktopSimulationServiceTests.cs`.
- Ningún contrato público del SDK, protocolo de transporte IPC, comportamiento del mod Gauntlet o dependencia externa fue alterado.

## Validación y límites de la evidencia

- `dotnet build CalradiaForge.sln -c Release -v:minimal` finalizó con 0 advertencias y 0 errores.
- `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause` superó 296/296 casos de renderizado WPF en 16.102 ms (320 pasadas de diseño, 6.345 ms en llamadas de diseño; 26/26 suites unitarias superadas).
- `tools\Verify-CalradiaForge-StatelessBehavior.bat` superó 4/4 criterios de aceptación.
- `tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause` superó todas las suites (Core 420 superadas, ForgeWeave 73 superadas, Desktop 67 superadas, Desktop Render 296 superadas).
- `tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` superó todas las auditorías estáticas y verificaciones de paridad de documentación.
- No se llevó a cabo ningún lanzamiento en vivo del motor de Bannerlord ni sesiones de combate en tiempo real.

Este anexo añade una nueva revisión sin reescribir Rev132 ni registros anteriores. Preserva la versión del producto, rutas, contratos públicos y comportamiento del juego.
