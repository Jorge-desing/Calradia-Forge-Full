# Validación de Calradia Forge 25.0.0

**Fecha de Ejecución:** 2026-09-26  
**Entorno de Pruebas:** Windows 10/11, .NET SDK 8.0, Visual Studio Build Tools, PowerShell / cmd.  
**Alcance de la Verificación:** Compilación Release, suite completa de pruebas automatizadas, arnés de renderizado y diseño WPF en vivo, comprobación de accesibilidad mediante Windows UI Automation, auditoría de comportamiento sin estado (stateless) y empaquetado de distribución de la versión.

## Verificación Superada

- `dotnet build CalradiaForge.sln -c Release`: Compilación limpia con 0 errores y 0 advertencias en los 12 proyectos.
- `tools/Run-CalradiaForge-Tests.bat --skip-build --no-pause`: Ejecución de la suite completa de pruebas con 100% de éxito (645 casos de prueba totales):
  - **Canal de Activos y Pruebas TPAC**: 8/8 superadas en 0.043s (lector de firma PNG/IHDR, límites de atlas 2048×256, inspección de cabecera estructural v2, rechazo de copias de seguridad de trabajo, instrucciones verídicas de Resource Browser).
  - **CalradiaForge.Tests (Core / Mod / Sdk / Net472)**: 239/239 superadas (oyentes no serializados, ciclo de vida de SubModule, anti-lag con división temporal módulo-24, puntuación de sucesión dinástica, rutas críticas con cero asignación en el recolector de basura).
  - **Pruebas de la Malla de Eventos ForgeWeave (Net8.0)**: 69/69 superadas (aislamiento de fallos, políticas de cuarentena, presupuestos de ejecución, registro de reproducción, políticas de interfaz Gauntlet, percentiles de latencia APM).
  - **Pruebas de Protocolo y MVVM de Desktop (Net8.0)**: 54/54 superadas (comunicaciones de ida y vuelta PipeClient sobre tuberías con nombre, preferencias atómicas, catálogos en 13 idiomas, cancelación de órdenes de trabajo, edición y confirmación de versiones en el banco de trabajo de ensamblados).
  - **Pruebas de Renderizado WPF de Desktop (Net8.0-windows)**: 275/275 superadas (150 pasadas de diseño/renderizado, 1,382.5 ms de duración en llamadas de diseño, 89 capturas del árbol visual / 1,282 aciertos de prueba de impacto / 105,654 nodos visitados a lo largo de 13 idiomas, 4 escalas DPI y 3 temas).
- `tools/Test-CalradiaForge-Desktop-Uia.ps1`:
  - 18/18 nodos de accesibilidad verificados mediante el cliente de Windows UI Automation (`UIAutomationClient` / `UIAutomationTypes`) en 7,159 ms.
  - Controles verificados: Detección de ventana principal `Calradia Forge 25.0.0`, `WindowCloseButton`, `HeaderSealButton`, `CommandPaletteButton`, `LanguageSelector`, `ThemeSelector`, `SplitDeckToggleButton`, `DecorativeAccentsToggle`, `OperationalSearchFilter`, `CategorySelector`, `OperationalRailToolViewport`, `WorkbenchPageScrollViewport`, `RunWorkOrderButton`, `CancelWorkOrderButton`, `ExportWorkOrderButton`, `WorkbenchReportTabs`, `WorkbenchEvidenceTab`, `WorkbenchRawResultTab`.
- `tools/verify_stateless_behavior.ps1`:
  - 0 clases derivadas de `SaveableTypeDefiner`.
  - `SyncData` completamente vacío / libre de serialización de estado en comportamientos sin estado.
  - 0 infracciones de ensombrecimiento con `TaleWorlds.CampaignSystem.Campaign` o `TaleWorlds.Localization` (`GEMINI.md`).
  - SubModule registra correctamente los comportamientos en `OnGameStart` mediante `AddBehavior()`.
- `tools/package.ps1`:
  - 3 archivos oficiales de versión generados y auditados sin residuos de desarrollo, copias de seguridad, DLLs propietarias del motor ni ejecutables app-host.

## Límites de la Evidencia

Es fundamental declarar con total transparencia aquello que **NO** se ejecutó durante esta sesión de validación:
- El juego *Mount & Blade II: Bannerlord* **NO fue ejecutado en vivo**.
- No se realizaron pruebas de rendimiento en batallas personalizadas prolongadas ni simulaciones de campañas de varios años de duración.
- Las pruebas de renderizado WPF en proceso se llevaron a cabo dentro del arnés de pruebas (`ShowActivated=false`), ejercitando el diseño y las plantillas sin interacción humana física en configuraciones multimonitor.
- La comprobación de humo de UI Automation inspeccionó pares de accesibilidad reales y patrones de control en modo de solo lectura, sin ejecutar órdenes de trabajo que muten el estado ni modificar preferencias persistidas.
- La inspección TPAC verifica la geometría de la cabecera v2 y el rango del índice de contenidos; no decodifica bloques de textura comprimidos DXT5/BC3.

## Resúmenes Criptográficos Oficiales (SHA-256)

Extraídos de `artifacts/package-sha256-2500.txt`:
- `CalradiaForge-Modules-25.0.0.zip`: `FA16BF883305B963054C957B9C3F756419AA388206CBB5BE42FA034F2B89F49A`
- `CalradiaForge-Source-SDK-25.0.0.zip`: `1D9E6A07C87B3C3B17D7DDC88FB8B60285D3F7824EB8F812308A695844B1B951`
- `CalradiaForge-Desktop-25.0.0.zip`: `F7AD0AFE754E19463EB038E0340181447001DDB0AE6305D7AE55EEC51B3A9E31`
