# Original User Request

## 2026-09-20T21:07:11Z

# Teamwork Project Prompt

> Status: Launched

Use a very large team of agents.

Develop a massive experimental CampaignBehavior that explores all clan and character development hooks (dynastic succession, companion spawning, progression). The system must run statelessly using vanilla game states without requiring custom save data serialization.

Working directory: c:\Users\Alex\Documents\Mod Desarrolladores
Integrity mode: demo

## Requirements

### R1. Implement a Stateless Clan & Character CampaignBehavior
Create a new C# class inheriting from `CampaignBehaviorBase` in the `CalradiaForge.Mod` project. Hook into as many relevant `CampaignEvents` as possible related to heroes, clans, and character progression (e.g., birth, coming of age, death, marriage, clan leader changes). 

### R2. Adhere to Architecture Rules
Ensure the behavior avoids the "Engine Initialization Crash Constraint" by deferring complex logic, and strictly follows the anti-shadowing rule (do not use `Campaign` in namespaces or class names). The behavior must be registered in the `MBSubModuleBase.OnGameStart` pipeline.

## Acceptance Criteria

### Verification & Compliance
- [ ] The `CalradiaForge.sln` compiles successfully in Release mode without errors.
- [ ] A programmatic script (e.g., PowerShell or Python) verifies that no custom classes inherit from `SaveableTypeDefiner` and no data is synced in `SyncData`.
- [ ] A test script confirms the SubModule properly registers the new CampaignBehavior via `AddBehavior()`.

## 2026-09-20T21:15:12Z

User Request (via parent): "Diles a los agentes que terminen de implementar los cambios" (Tell the agents to finish implementing the changes). Please proceed with the implementation and finalize the deliverables as soon as the quality checks are passed.

## 2026-09-21T01:02:05Z

User Request (via parent): Execute verification and audit task for the upcoming interface integration of the Underworld & Crime Rackets system (sim-crime / UnderworldCrimeSimulator). Monitor workspace files, verify zero-shadowing and statelessness rules, and prepare for test suite execution.

## 2026-09-21T01:07:00Z

# Teamwork Project Prompt

> Status: Launched

Use a very large team of agents.

Develop a massive experimental CampaignBehavior that explores all clan and character development hooks (dynastic succession, companion spawning, progression) and integrate the Underworld & Crime Rackets system (sim-crime / UnderworldCrimeSimulator) across the mod and desktop interfaces. The system must run statelessly using vanilla game states without requiring custom save data serialization.

Working directory: c:\Users\Alex\Documents\Mod Desarrolladores
Integrity mode: demo

## Requirements

### R1. Implement a Stateless Clan & Character CampaignBehavior
Maintain and verify the C# class inheriting from `CampaignBehaviorBase` in the `CalradiaForge.Mod` project. Hook into as many relevant `CampaignEvents` as possible related to heroes, clans, and character progression.

### R2. Adhere to Architecture Rules
Ensure the behavior avoids the 'Engine Initialization Crash Constraint' by deferring complex logic, and strictly follows the anti-shadowing rule (do not use `Campaign` in namespaces or class names). The behavior must be registered in the `MBSubModuleBase.OnGameStart` pipeline.

### R3. Interface Integration Parity
Integrate and audit the Underworld & Crime Rackets system (`sim-crime` in Gauntlet UI `CalradiaForge.xml` / `PanelViewModel.cs`, and `UnderworldCrimeSimulator` in WPF Desktop `MainWindow.xaml` / `MainWindow.xaml.cs`) with complete multi-language support.

## Acceptance Criteria

### Verification & Compliance
- [ ] The `CalradiaForge.sln` compiles successfully in Release mode without errors or warnings.
- [ ] A programmatic script (PowerShell and Python) verifies that no custom classes inherit from `SaveableTypeDefiner` and no data is synced in `SyncData`.
- [ ] Automated tests verify SubModule properly registers the new CampaignBehavior via `AddBehavior()`.
- [ ] Automated test suites pass 100% through the repository's `.bat` launchers, without starting test executables.
- [ ] Mod packages are generated in `artifacts/` (`CalradiaForge-13.3.0.zip`, `CalradiaForge-Modules-13.3.0.zip`, `CalradiaForge-Desktop-13.3.0.zip`).


## 2026-10-04T03:22:28Z

# Teamwork Project Prompt

Despliegue exhaustivo de la composición táctica completa en las plantillas de estudio de la aplicación de escritorio WPF (Calradia Forge Desktop), integrando encabezados con medallón dual temático, tarjetas de métricas KPI con acentos de 3px y mini-barras de progreso, y terminales monoespaciados tipo dossier en todos los estudios tácticos de `src/CalradiaForge.Desktop/Resources/Views/ToolPageTemplates.xaml`.

Working directory: `c:\Users\Alex\Documents\Mod Desarrolladores`
Integrity mode: development

## Requirements

### R1. Composición Táctica Completa en Todos los Estudios Tácticos
- Enriquecer los encabezados de todos los estudios tácticos en `ToolPageTemplates.xaml`:
  * `WorkshopSimulatorDashboardTemplate`: Medallón dual con `calradia-guild-medallion-rev092.png` y `calradia-tactical-emblem-rev100.png`.
  * `KingdomDiplomacyStudioDashboardTemplate`: Medallón dual con `calradia-diplomacy-medallion-rev095.png` y `calradia-aquila-seal-rev087.png`.
  * `CaravanTradeViewTemplate`: Medallón dual con `calradia-trade-sigil-rev096.png` y `calradia-astrolabe-dial-rev086.png`.
  * `GauntletStudioViewTemplate`: Medallón dual con `calradia-gauntlet-sigil-rev098.png` y `calradia-anvil-weave-sigil-rev098.png`.
  * `CampaignStudioViewTemplate`: Medallón dual con `calradia-campaign-astrolabe-rev098.png` y `calradia-astrolabe-dial-rev086.png`.
  * `DeliveryStudioViewTemplate`: Medallón dual con `calradia-delivery-seal-rev098.png` e `imperial-wax-seal-rev085.png`.
  * `DiagnosticsStudioViewTemplate`: Medallón dual con `calradia-diagnostics-aegis-rev098.png` y `calradia-sentinel-eye-rev096.png`.
  * `LiveSessionViewTemplate`: Medallón dual con `calradia-pipe-seal-rev096.png` y `calradia-bytecode-matrix-rev100.png`.
  * `AgentMemoryInspectorDashboardTemplate`: Medallón dual con `calradia-mind-medallion-rev093.png` y `calradia-tactical-emblem-rev100.png`.
  * `CodeSecurityAuditorDashboardTemplate`: Medallón dual con `calradia-cipher-seal-rev095.png` y `calradia-bytecode-matrix-rev100.png`.
  * `ModuleHierarchyValidatorDashboardTemplate`: Medallón dual con `calradia-hierarchy-seal-rev095.png` y `calradia-aquila-seal-rev087.png`.
  * `ComponentGeneratorStudioDashboardTemplate`: Medallón dual con `calradia-mechanism-medallion-rev095.png` y `calradia-anvil-weave-sigil-rev098.png`.
- Diseñar tarjetas KPI con bordes de 3px, fondo `CoalBrush` / `FrameSurfaceSolidBrush`, tipografía destacada en `BrassBrush` / `VerdigrisBrush` y mini-barras de progreso proporcionales de 4px para métricas clave.
- Incorporar bloques de consola tipo dossier con prompt `$ `, tipografía monoespaciada en `Consolas` y botón táctil de copia para comandos de consola relevantes en cada estudio.

### R2. Preservación Estricta de Invariantes Arquitectónicos
- **Regla C:** Mantener exactamente 9 instancias de `DashboardTemplate` en `ToolPageTemplates.xaml`.
- **Propuesta 48:** Cero enlaces `TwoWay` en elementos `Run.Text` (utilizar `OneWay` o bindings estáticos).
- **Paridad Lingüística:** Utilizar las claves de localización existentes en los 13 diccionarios sin introducir cadenas fijas en inglés ni romper la paridad bilingüe.

### R3. Cobertura de Pruebas y Validación Empírica
- Incorporar prueba estructural de aserción `Rev101TacticalStudiosFullCompositionOverhaul()` en `tests/CalradiaForge.Desktop.RenderTests/DesktopSimulationServiceTests.cs`.
- Ejecutar y verificar con éxito:
  * `dotnet build CalradiaForge.sln -c Release -v:minimal` (0 errores).
  * `tools\Verify-CalradiaForge-StatelessBehavior.bat` (4/4 criterios).
  * `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause` (296/296 render tests).
  * `tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause` (67/67 tests).
  * `tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` (135/135 revisiones enlazadas, 42/42 pares bilingües).

### R4. Registro Inmutable y Empaquetado Canónico
- Redactar anexo bilingüe en `docs/append/Rev135-*.md` y `.es.md`.
- Compilar el documento Word protegido `docs/CalradiaForge-Registro-Mejoras-Rev135.docx` con validación de prefijo C14N y registro en `integrity.jsonl`.
- Compilar y empaquetar mediante `tools/package.ps1` verificando que los 3 archivos ZIP de distribución correspondan exactamente a los hashes del manifiesto `package-sha256-2520.txt` y superen la auditoría `package-audit-2520.json`.
- Comprometer y sincronizar cambios en `origin/main` bajo la autorización permanente y revisar los workflows de GitHub Actions.
