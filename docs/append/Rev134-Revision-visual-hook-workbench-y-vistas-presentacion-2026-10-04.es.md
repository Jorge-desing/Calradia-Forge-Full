# Rev134 — Revisión visual de Hook Workbench y vistas de presentación WPF

**Fecha:** 2026-10-04

**Versión:** Calradia Forge 25.2.0, sin cambios; `ForgeApi.Version` 13

**Alcance:** Presentación WPF de escritorio, Hook Workbench (banco de hooks IL de código de bytes), barra superior de comandos (WorkbenchHeaderControl), barra de título de MainWindow, vista envolvente WorkbenchShellView, plantilla de operaciones genéricas en ToolPageTemplates, generación de texturas maestras Rev100 (`calradia-bytecode-matrix-rev100.png` y `calradia-tactical-emblem-rev100.png`), optimización de textura `tool-card-top-corners-v1.png` (>350 KB), pruebas de renderizado Rev100 y libro mayor de mejoras de solo anexión.

## Problema observado y justificación técnica

Tras la elevación de las vistas tácticas principales en la Rev133, ciertas superficies operativas y componentes clave del banco de trabajo de `CalradiaForge.Desktop` conservaban un diseño plano o carecían de cohesión física e iconográfica:
1. `HookWorkbenchControl.xaml` presentaba un encabezado con un único medallón básico, aviso de elegibilidad con una elipse simple de 8 px sin badge de contrato activo, entradas de filtro en cajas de texto planas sin contenedor, filas de hooks sin franja de acento táctico por categoría ni código en cápsula oscura, botones de acción sin jerarquía visual refinada y registro de estado sin interfaz de consola o terminal de desarrollador.
2. `WorkbenchHeaderControl.xaml` presentaba selectores de tema e idioma sin acentos decorativos, chip de estado de sesión plano y botones de alternancia sin estado físico palpable.
3. `MainWindow.xaml` y `WorkbenchShellView.xaml` carecían de insignia de versión/compilación en el custom chrome de la barra de título y regla divisoria arquitectónica entre el riel de navegación y el área de trabajo.
4. La plantilla de visión general de operaciones genéricas (`GenericOperationOverviewDashboardTemplate`) disponía de un único medallón sin el emblema soberano táctico complementario.
5. La textura `tool-card-top-corners-v1.png` era el único archivo de recursos por debajo de 350 KB (288 KB), requiriendo elevación para garantizar el 100% de cumplimiento de alta fidelidad en texturas maestras.

## Solución técnica y decisiones arquitectónicas

1. **Generación y procesamiento de texturas maestras de alta resolución Rev100**:
   - Se generó y procesó mediante `/high-quality-image-generation` y `tools/process_high_quality_asset.py` el activo `calradia-bytecode-matrix-rev100.png` (627 KB, 512×512 POT RGBA), con un medallón medieval de latón e hierro con engranajes de relojería y desvíos rúnicos para el encabezado del Hook Workbench.
   - Se generó y procesó el activo `calradia-tactical-emblem-rev100.png` (631 KB, 512×512 POT RGBA), con corona imperial calrádica, ramas de laurel y rosas de los vientos gemelas para la plantilla de operaciones tácticas.
   - Se optimizó `tool-card-top-corners-v1.png` (390 KB), asegurando que el 100% de las texturas maestras en `Resources/Textures/` superen estrictamente el umbral de 350 KB.
2. **Revisión táctica de Hook Workbench (`HookWorkbenchControl.xaml`)**:
   - Se incorporó una presentación de doble medallón enmarcado (sigilo de yunque Rev098 y nueva matriz de código de bytes Rev100) y chip de título "IL BYTECODE WEAVER · LIVE INTERCEPTOR".
   - Se elevó el aviso de elegibilidad a una tarjeta táctica en verde pino profundo con insignia "ACTIVE CONTRACT", estado monoespaciado de sesión y contador enmarcado.
   - Se envolvieron los filtros de Propietario, Destino y Tipo en tarjetas individuales con marcadores de latón.
   - Se añadieron franjas verticales de 3 px en cardenillo a cada fila de hook, cápsulas monoespaciadas para métodos de destino e insignias de estado de latón.
   - Se transformó el registro de estado inferior en una consola de terminal de desarrollador ("CALRADIA BYTECODE AUDIT CONSOLE · REALTIME LOG") con LED verde de actividad en tiempo real.
3. **Refinamiento de la barra superior (`WorkbenchHeaderControl.xaml`)**:
   - Se actualizó el marco principal con esquinas redondeadas de 5 px.
   - Se agregó un indicador LED verde al chip "TACTICAL" de la marca.
   - Se añadieron viñetas de latón a los selectores de idioma, tema y sesión.
   - Se refinó el indicador de estado de conexión con anillo de estado y botón de conexión táctil.
4. **Acabados de marco de ventana y división arquitectónica (`MainWindow.xaml` y `WorkbenchShellView.xaml`)**:
   - Se agregó la insignia "v25.2.0 · BANNERLORD PRO" junto al título de la barra de título de MainWindow.
   - Se integró una regla divisoria vertical arquitectónica en la columna separadora entre el riel de navegación y el mazo de trabajo en `WorkbenchShellView.xaml`.
5. **Elevación de plantilla de estudio (`ToolPageTemplates.xaml`)**:
   - Se agregó el emblema táctico soberano `calradia-tactical-emblem-rev100.png` al encabezado de `GenericOperationOverviewDashboardTemplate`, preservando estrictamente el invariante de la Regla C (exactamente 9 instancias de `DashboardTemplate`) y el invariante de la Propuesta 48 (0 enlaces `TwoWay` en `Run.Text`).
6. **Verificación automatizada**:
   - Se implementó la prueba unitaria `Rev100HookWorkbenchAndPresentationViewsVisualOverhaul()` en `DesktopSimulationServiceTests.cs` validando todos los contratos XAML, registros en `.csproj`, invariantes y tamaños de archivo (>350 KB).

## Cambios en activos, código y dependencias

- Se agregaron las texturas maestras en `src/CalradiaForge.Desktop/Resources/Textures/`: `calradia-bytecode-matrix-rev100.png`, `calradia-tactical-emblem-rev100.png` y se optimizó `tool-card-top-corners-v1.png`.
- Se registraron los nuevos recursos en `src/CalradiaForge.Desktop/CalradiaForge.Desktop.csproj`.
- Se actualizaron las vistas de presentación: `HookWorkbenchControl.xaml`, `WorkbenchHeaderControl.xaml`, `MainWindow.xaml`, `WorkbenchShellView.xaml` y `ToolPageTemplates.xaml`.
- Se actualizó la suite de pruebas: `tests/CalradiaForge.Desktop.RenderTests/DesktopSimulationServiceTests.cs`.
- Ningún contrato público del SDK, protocolo IPC o dependencia externa fue alterado.

## Validación y límites de la evidencia

- `dotnet build CalradiaForge.sln -c Release -v:minimal` finalizó con 0 advertencias y 0 errores.
- `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause` superó 296/296 casos de renderizado WPF en 17.321 ms (320 pasadas de diseño, 6.895 ms en llamadas de diseño; todas las suites superadas).
- `tools\Verify-CalradiaForge-StatelessBehavior.bat` superó 4/4 criterios de aceptación.
- `tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause` superó 67/67 pruebas unitarias de protocolo y MVVM.
- `tools\Run-CalradiaForge-ForgeWeave-Tests.bat` superó 73/73 pruebas de despacho y aislamiento.
- `tools\Run-CalradiaForge-Core-Tests.bat` superó 420/420 pruebas del núcleo y 30/30 pruebas de diagnóstico de parches.
- `tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` superó todas las comprobaciones estáticas y la paridad bilingüe.
- No se realizó ningún lanzamiento en vivo del ejecutable de Bannerlord ni simulaciones de batalla en tiempo real.
