# Validación de Lanzamiento Calradia Forge 25.2.0

**Fecha de Ejecución:** 2026-09-26  
**Entorno de Pruebas:** Windows 10/11, .NET SDK 8.0, Visual Studio Build Tools, PowerShell / cmd.  
**Alcance de la Verificación:** Lanzamiento centrado en motor gráfico puro y renderizado, suite completa de pruebas automatizadas, pruebas de rendimiento de layout y renderizado WPF en vivo, arnés de accesibilidad Windows UI Automation (29 comprobaciones con SelectionItemPattern e InvokePattern no mutantes), auditoría de comportamiento sin estado y empaquetado de distribución.

## Verificaciones Superadas con Éxito

- `dotnet build CalradiaForge.sln -c Release`: Compilación limpia con 0 errores y 0 advertencias en los 12 proyectos.
- `tools/Run-CalradiaForge-Tests.bat --skip-build --no-pause`: Ejecución de suite completa con 100% de éxito (726 casos de prueba totales):
  - **Pipeline de Activos y Fixtures TPAC**: 8/8 superadas en 0.047s (lector de firma PNG/IHDR, atlas de una sola hoja 4096×512, inspección estructural de cabecera v2, rechazo de copias de seguridad de trabajo e instrucciones veraces de Resource Browser).
  - **CalradiaForge.Tests (Core / Mod / Sdk / Net472)**: 318/318 superadas (oyentes no serializados, ciclo de vida SubModule, anti-lag de división temporal módulo-24, puntuación de sucesión dinástica, rutas críticas sin asignación en GC, sincronización de SuiteInfo 25.2.0).
  - **ForgeWeave Event Mesh Tests (Net8.0)**: 71/71 superadas (aislamiento de fallos, políticas de cuarentena, presupuestos de ejecución, registro de repetición, políticas de Gauntlet UI, percentiles de latencia APM, eventos personalizados con coincidencia por comodines).
  - **Desktop Protocol & MVVM Tests (Net8.0)**: 54/54 superadas (comunicación PipeClient sobre tuberías nombradas, preferencias atómicas, 13 catálogos de idiomas, cancelación de comandos, edición y confirmación en workbench de ensamblados, sincronización de versión asegurada en 25.2.0).
  - **Desktop WPF Render & Simulation Tests (Net8.0-windows)**: 275/275 superadas en 9,398 - 10,010 ms (150 pasadas de render/layout, 1,229.5 - 1,428.1 ms de duración en llamadas de layout, 89 construcciones de árbol visual / 1,282 aciertos / 119,410 nodos visitados en 13 idiomas, 4 escalas DPI y 3 temas; virtualización estricta de UI con reciclaje de contenedores, desplazamiento suave por píxel `VirtualizingPanel.ScrollUnit="Pixel"`, precaché de búfer fuera de pantalla `CacheLength="1,1"`, caché pre-congelada de pinceles y renderizado nítido de reglas sin subpíxeles `EdgeMode="Aliased"`).
- `tools/Test-CalradiaForge-Desktop-Uia.ps1`:
  - 29/29 comprobaciones superadas en 29,365 ms (100% de éxito, 0 fallos, 0 advertencias).
  - Descubrimiento y verificación de ventana raíz `Calradia Forge 25.2.0` (PID 28704).
  - Verificación de más de 24 nodos de shell: `WindowCloseButton`, `WindowMinimizeButton`, `WindowMaximizeButton`, `HeaderSealButton`, `CommandPaletteButton`, `ConnectButton`, `SplitDeckToggleButton`, `LanguageSelector`, `ThemeSelector`, `CategorySelector`, `DecorativeAccentsToggle`, `OperationalSearchFilter`, `OperationalRailToolViewport`, `WorkbenchPageScrollViewport`, `ActiveToolInput`, `BrowseFileButton`, `RunWorkOrderButton`, `CancelWorkOrderButton`, `ExportWorkOrderButton`, `ExportEvidenceButton`, `WorkbenchReportTabs`, `WorkbenchEvidenceTab`, `WorkbenchRawResultTab`, `EvidenceLedgerItems`, `KeyboardShortcutHint`.
  - Verificación de navegación por pestañas no mutante con `SelectionItemPattern.Select()` en `WorkbenchRawResultTab` (verifica la materialización dinámica de `RawResultText` y restaura `WorkbenchEvidenceTab`).
  - Verificación no mutante con `InvokePattern.Invoke()` en `SplitDeckToggleButton` (abre el panel, verifica estado visual y lo cierra).
- `tools/verify_stateless_behavior.ps1`:
  - 0 clases derivadas de `SaveableTypeDefiner`.
  - `SyncData` completamente vacío / libre de serialización de estado en comportamientos sin estado.
  - 0 infracciones de sombreado con `TaleWorlds.CampaignSystem.Campaign` o `TaleWorlds.Localization` (`GEMINI.md`).
  - SubModule registra correctamente comportamientos en `OnGameStart` mediante `AddBehavior()`.
- `tools/package.ps1`:
  - 3 archivos oficiales de distribución generados y auditados sin residuos de desarrollo, copias temporales, DLLs del motor ni ejecutables app-host.
  - Integridad de paquetes auditada mediante `tools/audit_package.py` y registrada en `artifacts/package-audit-2520.json`.

## Límites de la Evidencia

Es indispensable declarar de forma transparente lo que **NO** se ejecutó durante esta sesión de validación:
- El juego *Mount & Blade II: Bannerlord* **NO fue ejecutado en vivo**.
- No se realizaron pruebas de rendimiento en batallas personalizadas prolongadas ni simulaciones de resistencia de campaña durante múltiples años en el motor del juego.
- Las pruebas de renderizado WPF en proceso se llevaron a cabo dentro del arnés automatizado (`ShowActivated=false`), ejercitando composición y plantillas sin interacción humana multi-monitor física.
- La comprobación de UI Automation inspeccionó pares de accesibilidad reales y patrones de control en modo solo lectura, sin ejecutar órdenes de trabajo con mutación de estado ni alterar preferencias persistidas.
- La inspección de TPAC verifica la geometría de cabecera v2 y el rango de la tabla de contenidos; no decodifica datos comprimidos de texturas DXT5/BC3.

## Resúmenes Criptográficos Oficiales (SHA-256)

Extraídos de `artifacts/package-sha256-2520.txt`:
- `CalradiaForge-Modules-25.2.0.zip`: `EBC5E959175503A1AED75B4A879C01CF2E0BF2BEC1F7F848E4CE77104C45888A`
- `CalradiaForge-Source-SDK-25.2.0.zip`: `979617F0F5F05562E42749D884F00130C46F33D137115647EAD207A3B91318BB`
- `CalradiaForge-Desktop-25.2.0.zip`: `56D3B5D994BF7B6225BACE7A192E69F16C0ADEA077199CADCBF6CA36FF4BD471`

## Validación de seguimiento — 2026-10-02

Esta es una ejecución de seguimiento separada en la versión 25.2.0; no cambiaron la versión del producto ni la API pública del SDK. Cubre la identificación de la fila inicial del Constructor de reglas de campaña, el control de evento y las correcciones del generador/pipeline que conservan los bindings Gauntlet y los recursos localizados después del empaquetado. En ese momento, el commit `22359f4` creaba una fila inicial sin guardar cuando no había borrador. El commit posterior del mismo día, `08f4eaa`, eliminó esa fila y restableció la primera visita vacía; los resultados de identidad de la fila inicial que siguen son históricos y no describen el contrato actual para un borrador ausente.

### Verificaciones superadas

- La compilación Release terminó con 0 advertencias y 0 errores. Los perfiles Client y Modding Kit editor también compilaron durante el despliegue.
- `tools/verify_stateless_behavior.ps1`: pasaron las cuatro comprobaciones de aceptación.
- `tools/audit_gauntlet_ui.py`: PASS, 0 errores y 0 advertencias.
- `tools/audit_localization.py`: PASS; 13 idiomas nativos con 747 claves cada uno, incluidas 71 claves del Constructor de reglas de campaña.
- `tools/Run-CalradiaForge-Tests.bat --skip-build --no-pause <nul`: pasaron todas las suites seleccionadas. En esa revisión, las regresiones del Constructor de reglas de campaña cubrían la identidad de la fila inicial, reordenamiento/eliminación, guardado/carga, compilación del comportamiento generado y vista previa/copiado local. La regresión actual exige que un borrador ausente permanezca vacío hasta pulsar Añadir; aquí se conserva el resultado de identidad solo como evidencia histórica fechada. La suite de renderizado WPF pasó 295 casos; ForgeWeave pasó 73 pruebas; Desktop MVVM pasó 65 pruebas.
- `tools/package.ps1`: los tres archivos 25.2.0 pasaron la auditoría de archivos. La comprobación independiente de SHA-256 coincidió con `artifacts/package-sha256-2520.txt`.
- El despliegue por Steam terminó en los perfiles Client y Modding Kit con respaldos. El prefab instalado, los recursos de idioma inglés y español y el ensamblado Client coincidieron por hash con los del workspace. El TPAC de Steam permaneció sin cambios en `8899A48A407591ADA53573EFC0DD699EA47D2A30F32A0C12C1875003F7993047`.
- Bannerlord se inició mediante la ruta documentada de Steam y llegó al menú principal de 1920×1080. La sesión disponible de Computer Use capturó la ventana del juego, pero F10 no mostró el panel Forge y las flechas no cambiaron la selección del menú. Por lo tanto, el panel dentro del juego, la interacción por teclado y los tamaños menores solicitados **no quedan verificados** en esta ejecución. El juego se cerró al terminar.
- Pasó el preflight estructural de cabecera/tabla TPAC. Se omitió el análisis profundo porque TpacTool y el fixture nativo local no estaban disponibles; no se intentó importar TPAC.

### SHA-256 de los paquetes de seguimiento

- `CalradiaForge-Modules-25.2.0.zip`: `CE75B6B186D4D26DD1113BB2A02294317EF5B9353929EAF805DAEF8AEA868E48`
- `CalradiaForge-Source-SDK-25.2.0.zip`: `417C8215289EE64319E4C9F838CEE4B17C640D8903ECDE32B4BD3A59119660FF`
- `CalradiaForge-Desktop-25.2.0.zip`: `B49CA3A85944DCCAA2580DF3819C842B8DB95174FC4AD92CAB603C74FF3F71B5`
