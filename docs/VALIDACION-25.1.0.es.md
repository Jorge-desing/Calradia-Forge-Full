# Validación de Calradia Forge 25.1.0

**Fecha de Ejecución:** 2026-09-26  
**Entorno de Pruebas:** Windows 10/11, .NET SDK 8.0, Visual Studio Build Tools, PowerShell / cmd.  
**Alcance de la Verificación:** Compilación Release, suite completa de pruebas automatizadas, arnés de renderizado y diseño WPF en vivo, comprobación ampliada de accesibilidad mediante Windows UI Automation (29 verificaciones con SelectionItemPattern e InvokePattern), auditoría de comportamiento sin estado (stateless) y empaquetado de distribución de la versión.

## Verificación Superada

- `dotnet build CalradiaForge.sln -c Release`: Compilación limpia con 0 errores y 0 advertencias en los 12 proyectos.
- `tools/Run-CalradiaForge-Tests.bat --skip-build --no-pause`: Ejecución de la suite completa de pruebas con 100% de éxito (726 casos de prueba totales):
  - **Canal de Activos y Pruebas TPAC**: 8/8 superadas en 0.047s (lector de firma PNG/IHDR, límites de atlas 4096×512 de hoja única, inspección de cabecera estructural v2, rechazo de copias de seguridad de trabajo, instrucciones verídicas de Resource Browser).
  - **CalradiaForge.Tests (Core / Mod / Sdk / Net472)**: 318/318 superadas (oyentes no serializados, ciclo de vida de SubModule, anti-lag con división temporal módulo-24, puntuación de sucesión dinástica, rutas críticas con cero asignación en el recolector de basura, sincronización de SuiteInfo 25.1.0).
  - **Pruebas de la Malla de Eventos ForgeWeave (Net8.0)**: 71/71 superadas (aislamiento de fallos, políticas de cuarentena, presupuestos de ejecución, registro de reproducción, políticas de interfaz Gauntlet, percentiles de latencia APM, eventos personalizados con coincidencia por comodines).
  - **Pruebas de Protocolo y MVVM de Desktop (Net8.0)**: 54/54 superadas (comunicaciones de ida y vuelta PipeClient sobre tuberías con nombre, preferencias atómicas, catálogos en 13 idiomas, cancelación de órdenes de trabajo, edición y confirmación de versiones en el banco de trabajo de ensamblados).
  - **Pruebas de Renderizado WPF y Simulación de Desktop (Net8.0-windows)**: 275/275 superadas en 9,226 ms (150 pasadas de diseño/renderizado, 1,265.9 - 1,468.4 ms de duración en llamadas de diseño, 89 capturas del árbol visual / 1,282 aciertos de prueba de impacto / 105,654 nodos visitados a lo largo de 13 idiomas, 4 escalas DPI y 3 temas; congelación profunda de recursos y hardware BitmapCache en el tablero cartográfico y el sello).
- `tools/Test-CalradiaForge-Desktop-Uia.ps1`:
  - 29/29 verificaciones superadas en 14,294 ms (100% de éxito, 0 fallos, 0 advertencias).
  - Verificados más de 24 nodos de interfaz: Detección de ventana principal `Calradia Forge 25.1.0`, `WindowCloseButton`, `WindowMinimizeButton`, `WindowMaximizeButton`, `HeaderSealButton`, `CommandPaletteButton`, `ConnectButton`, `SplitDeckToggleButton`, `LanguageSelector`, `ThemeSelector`, `CategorySelector`, `DecorativeAccentsToggle`, `OperationalSearchFilter`, `OperationalRailToolViewport`, `WorkbenchPageScrollViewport`, `ActiveToolInput`, `BrowseFileButton`, `RunWorkOrderButton`, `CancelWorkOrderButton`, `ExportWorkOrderButton`, `ExportEvidenceButton`, `WorkbenchReportTabs`, `WorkbenchEvidenceTab`, `WorkbenchRawResultTab`, `EvidenceLedgerItems`, `KeyboardShortcutHint`.
  - Verificación no mutante de cambio de pestañas usando `SelectionItemPattern.Select()` sobre `WorkbenchRawResultTab` (comprueba la materialización dinámica de `RawResultText` y restaura `WorkbenchEvidenceTab`).
  - Verificación no mutante de `InvokePattern.Invoke()` sobre `SplitDeckToggleButton` (abre la cubierta dividida, valida el estado visual y la cierra).
- `tools/verify_stateless_behavior.ps1`:
  - 0 clases derivadas de `SaveableTypeDefiner`.
  - `SyncData` completamente vacío / libre de serialización de estado en comportamientos sin estado.
  - 0 infracciones de ensombrecimiento con `TaleWorlds.CampaignSystem.Campaign` o `TaleWorlds.Localization` (`GEMINI.md`).
  - SubModule registra correctamente los comportamientos en `OnGameStart` mediante `AddBehavior()`.
- `tools/package.ps1`:
  - 3 archivos oficiales de versión generados y auditados sin residuos de desarrollo, copias de seguridad, DLLs propietarias del motor ni ejecutables app-host.
  - Integridad de paquetes auditada mediante `tools/audit_package.py` y registrada en `artifacts/package-audit-2510.json`.

## Límites de la Evidencia

Es fundamental declarar con total transparencia aquello que **NO** se ejecutó durante esta sesión de validación:
- El juego *Mount & Blade II: Bannerlord* **NO fue ejecutado en vivo**.
- No se realizaron pruebas de rendimiento en batallas personalizadas prolongadas ni simulaciones de campañas de varios años de duración.
- Las pruebas de renderizado WPF en proceso se llevaron a cabo dentro del arnés de pruebas (`ShowActivated=false`), ejercitando el diseño y las plantillas sin interacción humana física en configuraciones multimonitor.
- La comprobación de humo de UI Automation inspeccionó pares de accesibilidad reales y patrones de control en modo de solo lectura, sin ejecutar órdenes de trabajo que muten el estado ni modificar preferencias persistidas.
- La inspección TPAC verifica la geometría de la cabecera v2 y el rango del índice de contenidos; no decodifica bloques de textura comprimidos DXT5/BC3.

## Resúmenes Criptográficos Oficiales (SHA-256)

Extraídos de `artifacts/package-sha256-2510.txt`:
- `CalradiaForge-Modules-25.1.0.zip`: `1791F30493BB4DFBFB7F46869DD56C0A42B5805068AC82631813DD58789EE1DF`
- `CalradiaForge-Source-SDK-25.1.0.zip`: `C052573264EDB07B3A67B876A9E43D9DDBC234515B99CB76B868FC967B71C8FF`
- `CalradiaForge-Desktop-25.1.0.zip`: `7B6FF34AFB18E087C4888AA16D66C9D60CD06E40DDA9D8D523996FE75D551E99`
