# Rev133 — Tactical views and unretouched functions visual overhaul

**Date:** 2026-10-03

**Product version:** Calradia Forge 25.2.0, unchanged; `ForgeApi.Version` 13

**Scope:** Desktop WPF presentation, high-resolution texture enhancement, command palette, status telemetry cards, tool dossier, split deck, footer status strip, navigation rail, render tests, and append-only ledger.

## Observed issue and technical rationale

While prior revisions enhanced the tactical studios and added five high-fidelity studio medallions, several foundational operational surfaces and residual views across `CalradiaForge.Desktop` had remained visually unretouched:
1. `CommandPaletteControl.xaml` featured an unbordered 16px dial, flat prefix selector labels, and unbordered list items without category-based visual accents.
2. `WorkbenchStatusControl.xaml` used plain 2px bottom lines and bare 14px icons across its five status cards (`Ui.Context`, `Ui.TestPermission`, `Ui.ReportState`, `Ui.ActiveTool`, `Ui.RetainedEvidence`).
3. `ToolDossierControl.xaml` displayed console commands in plain text, shortcuts in flat chips, and a 36px wax seal image without a dedicated heraldic frame.
4. `WorkbenchWorkspaceControl.xaml` used a small 14px placeholder dial in the Pinned Split Deck header and plain evidence rows without status badges.
5. `WorkbenchFooterControl.xaml` used an unbordered 14px image and plain text indicators.
6. Four residual textures in `src/CalradiaForge.Desktop/Resources/Textures/` (`calradia-mind-medallion-rev093.png`, `imperial-wax-seal-rev085.png`, `calradia-astrolabe-dial-rev086.png`, and `calradia-aquila-seal-rev087.png`) were legacy flat images (21 KB to 52 KB) with limited color depth.

## Technical solution and architectural decisions

1. **Master High-Resolution Texture Elevation**:
   - Re-rendered `calradia-mind-medallion-rev093.png` (482 KB) as an intricate 512×512 POT 3D medallion featuring gear train clockwork, beveled brass/bronze bezel, and glowing verdigris/gold neural synapses for the CoALA-inspired Agent Memory Inspector.
   - Re-rendered `imperial-wax-seal-rev085.png` (398 KB) as an authentic imperial crimson wax seal with organic melted rim, double-headed eagle stamped core, and micro-cracked wax texture.
   - Re-rendered `calradia-astrolabe-dial-rev086.png` (445 KB) as a precision navigational astrolabe with 360° degree graduations, celestial coordinates, and brass compass rose.
   - Re-rendered `calradia-aquila-seal-rev087.png` (522 KB) as a Roman-style imperial gold & bronze aquila bas-relief medallion with thunderbolts and laurel wreath.
2. **Command Palette Tactical Overhaul (`CommandPaletteControl.xaml`)**:
   - Added a 36×36 framed brass medallion with high-quality scaling in the modal header.
   - Converted prefix selectors into interactive tactile command chips (`ALL`, `>live`, `>diag`, `>asset`, `>sim`) with category border accents.
   - Upgraded list items to tactical cards featuring a 4px left category accent bar, category Group pill, and brass Kind badge.
   - Replaced footer shortcut hints with physical beveled keycap styling.
3. **Status Telemetry Cards Overhaul (`WorkbenchStatusControl.xaml`)**:
   - Wrapped top-right icons in 20×20 circular badges (`CornerRadius="10"`) with `CoalBrush` background and category border brushes (`BrassBrush`, `VerdigrisBrush`, `EmberBrush`).
   - Upgraded bottom accent lines to prominent 3px tactile status bars (`Opacity="0.8"`).
4. **Tool Dossier & Terminal Polish (`ToolDossierControl.xaml`)**:
   - Styled console command entries into terminal prompt blocks with brass prompt (`$ `), verdigris monospace syntax, and refined copy actions.
   - Elevated shortcut chips into tactile beveled keycaps.
   - Enclosed the upgraded imperial wax seal in a 42×42 framed medallion with `DeepPineBrush` background and brass border.
5. **Split Deck & Footer Upgrades**:
   - Replaced the 14px dial in `WorkbenchWorkspaceControl.xaml` with a 32×32 framed `tactical-dial-plate-rev084.png` medallion, and added status badge pills to evidence rows.
   - Upgraded `WorkbenchFooterControl.xaml` with a circular framed dial badge, glowing verdigris IPC ping status, and beveled keycap badges.
   - Added framed icon badge to `WorkbenchNavigationControl.xaml`.
6. **Automated Verification**:
   - Added `Rev099TacticalViewsAndUnretouchedFunctionsPolish()` in `DesktopSimulationServiceTests.cs` verifying XAML element contracts and texture file size thresholds (>350 KB).

## Changes to assets, code, and dependencies

- Updated textures in `src/CalradiaForge.Desktop/Resources/Textures/`: `calradia-mind-medallion-rev093.png`, `imperial-wax-seal-rev085.png`, `calradia-astrolabe-dial-rev086.png`, `calradia-aquila-seal-rev087.png`.
- Updated presentation views: `CommandPaletteControl.xaml`, `WorkbenchStatusControl.xaml`, `ToolDossierControl.xaml`, `WorkbenchWorkspaceControl.xaml`, `WorkbenchFooterControl.xaml`, `WorkbenchNavigationControl.xaml`.
- Updated test suite: `tests/CalradiaForge.Desktop.RenderTests/DesktopSimulationServiceTests.cs`.
- No public SDK contract, IPC wire protocol, Gauntlet mod behavior, or external dependency changed.

## Validation and evidence limits

- `dotnet build CalradiaForge.sln -c Release -v:minimal` succeeded with 0 warnings and 0 errors.
- `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause` passed 296/296 WPF render cases in 16,102 ms (320 layout passes, 6,345 ms in layout calls; 26/26 unit suites passed).
- `tools\Verify-CalradiaForge-StatelessBehavior.bat` passed 4/4 acceptance criteria.
- `tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause` passed all suites (Core 420 passed, ForgeWeave 73 passed, Desktop 67 passed, Desktop Render 296 passed).
- `tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` passed all static audits and documentation parity checks.
- No live Bannerlord engine launch or combat session was conducted.

This annex adds a new revision without rewriting Rev132 or earlier records. It preserves the product version, routes, public contracts, and game behavior.
