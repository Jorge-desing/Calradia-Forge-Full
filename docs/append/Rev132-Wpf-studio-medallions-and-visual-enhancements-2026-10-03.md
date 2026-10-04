# Rev132 — WPF studio medallions and visual enhancements

**Date:** 2026-10-03

**Product version:** Calradia Forge 25.2.0, unchanged; `ForgeApi.Version` 13

**Scope:** Desktop WPF presentation, high-quality image asset generation, XAML studio templates, regression tests, and documentation.

## Observed issue and technical rationale

Prior revisions modernized the visual identity of several tactical studios, but several operational panels and inspection dashboards remained visually unretouched or relied on temporary 1.5 KB flat placeholders and generic unboxed 28×28 laurel icons:
1. The Bytecode Hook Workbench (`HookWorkbenchControl.xaml`) featured plain text headers with zero thematic heraldry or visual anchors.
2. Gauntlet UI Studio, Campaign Studio, Delivery Studio, and Diagnostics Studio in `ToolPageTemplates.xaml` displayed bare, unbordered 28×28 `calradia-laurel-crest-rev089.png` placeholders.
3. Four tactical and protocol dashboards (Kingdom Diplomacy, Component Generator Forge, Live Session Protocol, and Generic Operation Deck) referenced 1.5 KB solid monochrome color blocks (`calradia-diplomacy-medallion-rev095.png`, `calradia-mechanism-medallion-rev095.png`, `calradia-pipe-seal-rev096.png`, and `calradia-sentinel-eye-rev096.png`).

## Technical solution and architectural decisions

Following the 9 Dimensions of Visual Intent from `/high-quality-image-generation` and automated post-processing via `tools/process_high_quality_asset.py`:
1. Generated and registered 5 new high-fidelity heraldic studio medallions:
   - `calradia-anvil-weave-sigil-rev098.png`: Blacksmith iron anvil interlaced with interlocking clockwork gears and runic detours for the Bytecode Hook Workbench header.
   - `calradia-gauntlet-sigil-rev098.png`: Armored gauntlet holding an architectural cartouche scroll and bronze calipers for Gauntlet UI Studio.
   - `calradia-campaign-astrolabe-rev098.png`: Antique Calradic expedition astrolabe with cartography compass and dragon engravings for Campaign Studio.
   - `calradia-delivery-seal-rev098.png`: Imperial crimson wax seal with heraldic eagle and dispatch scroll for Delivery Studio.
   - `calradia-diagnostics-aegis-rev098.png`: Wrought iron and brass forensic aegis shield with watchful sentinel eye for Diagnostics Studio.
2. Upgraded the 4 previously-stubbed 1.5 KB textures to authentic 512×512 POT transparent RGBA assets with edge dilation (pad 2) to eliminate dark halos under linear filtering.
3. Embedded all medallions within themed `Border` frames (36×36 or 44×44) with brass or verdigris brushes, high-quality bitmap scaling, and descriptive tooltips.
4. Added `Rev098MajorVisualAndStudioUpgrades()` regression assertions in `DesktopSimulationServiceTests.cs` to ensure resource registration, template invariants, and zero unboxed 28×28 placeholders.

## Changes to assets, code, and dependencies

- Created `src/CalradiaForge.Desktop/Resources/Textures/calradia-anvil-weave-sigil-rev098.png`, `calradia-gauntlet-sigil-rev098.png`, `calradia-campaign-astrolabe-rev098.png`, `calradia-delivery-seal-rev098.png`, and `calradia-diagnostics-aegis-rev098.png`.
- Replaced 1.5 KB stubs in `src/CalradiaForge.Desktop/Resources/Textures/` with full-fidelity assets.
- Registered all new textures in `src/CalradiaForge.Desktop/CalradiaForge.Desktop.csproj` as `<Resource>` items.
- Updated `src/CalradiaForge.Desktop/Presentation/HookWorkbenchControl.xaml` and `src/CalradiaForge.Desktop/Resources/Views/ToolPageTemplates.xaml`.
- Updated `tests/CalradiaForge.Desktop.RenderTests/DesktopSimulationServiceTests.cs`.
- No public SDK contract, IPC wire protocol, Gauntlet mod behavior, or external dependency changed.

## Validation and evidence limits

- `dotnet build CalradiaForge.sln -c Release -v:minimal` succeeded with 0 warnings and 0 errors.
- `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause` passed 296/296 WPF render cases in 17,779 ms (320 layout passes, 7,045 ms in layout calls).
- `tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause` passed all suites (Core 100%, ForgeWeave 73/73, Desktop 67/67).
- `tools\Verify-CalradiaForge-StatelessBehavior.bat` passed 4/4 criteria.
- `tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause` verified 131 ledger revisions and 42 bilingual document pairs.
- No live Bannerlord engine launch or combat session was conducted.

This annex adds a new revision without rewriting Rev131 or earlier records. It preserves the product version, routes, public contracts, and game behavior.
