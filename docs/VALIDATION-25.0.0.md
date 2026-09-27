# Calradia Forge 25.0.0 Validation

**Execution Date:** 2026-09-26  
**Test Environment:** Windows 10/11, .NET SDK 8.0, Visual Studio Build Tools, PowerShell / cmd.  
**Verification Scope:** Release compilation, full automated test suite, WPF live layout/render harness, Windows UI Automation accessibility check, stateless behavior audit, and release distribution packaging.

## Passed Verification

- `dotnet build CalradiaForge.sln -c Release`: Clean compilation with 0 errors and 0 warnings across all 12 projects.
- `tools/Run-CalradiaForge-Tests.bat --skip-build --no-pause`: Complete test suite execution with 100% pass (645 total test cases):
  - **Asset Pipeline & TPAC Fixtures**: 8/8 passed in 0.043s (PNG signature/IHDR reader, 2048×256 atlas bounds, structural header v2 inspection, working backup rejection, truthful Resource Browser instructions).
  - **CalradiaForge.Tests (Core / Mod / Sdk / Net472)**: 239/239 passed (non-serialized listeners, SubModule lifecycle, modulo-24 time-slicing anti-lag, dynastic succession scoring, zero GC allocation hot paths).
  - **ForgeWeave Event Mesh Tests (Net8.0)**: 69/69 passed (fault isolation, quarantine policies, execution budgets, replay registry, Gauntlet UI policies, APM latency percentiles).
  - **Desktop Protocol & MVVM Tests (Net8.0)**: 54/54 passed (PipeClient named-pipe roundtrips, atomic preferences, 13 language catalogs, command cancellation, assembly workbench version edit and commit).
  - **Desktop WPF Render Tests (Net8.0-windows)**: 275/275 passed (150 render/layout passes, 1,382.5 ms layout call duration, 89 visual-tree snapshot builds / 1,282 hits / 105,654 visited nodes across 13 languages, 4 DPI scales, and 3 themes).
- `tools/Test-CalradiaForge-Desktop-Uia.ps1`:
  - 18/18 accessibility nodes verified via Windows UI Automation client (`UIAutomationClient` / `UIAutomationTypes`) in 7,159 ms.
  - Verified controls: `Calradia Forge 25.0.0` top-level window discovery, `WindowCloseButton`, `HeaderSealButton`, `CommandPaletteButton`, `LanguageSelector`, `ThemeSelector`, `SplitDeckToggleButton`, `DecorativeAccentsToggle`, `OperationalSearchFilter`, `CategorySelector`, `OperationalRailToolViewport`, `WorkbenchPageScrollViewport`, `RunWorkOrderButton`, `CancelWorkOrderButton`, `ExportWorkOrderButton`, `WorkbenchReportTabs`, `WorkbenchEvidenceTab`, `WorkbenchRawResultTab`.
- `tools/verify_stateless_behavior.ps1`:
  - 0 classes derived from `SaveableTypeDefiner`.
  - `SyncData` completely empty / free of state serialization in stateless behaviors.
  - 0 shadowing infractions with `TaleWorlds.CampaignSystem.Campaign` or `TaleWorlds.Localization` (`GEMINI.md`).
  - SubModule correctly registers behaviors in `OnGameStart` via `AddBehavior()`.
- `tools/package.ps1`:
  - 3 official release archives generated and audited without development residue, backups, engine DLLs, or app-host binaries.

## Boundaries of Evidence

It is critical to transparently state what was **NOT** executed during this validation session:
- The game *Mount & Blade II: Bannerlord* **WAS NOT executed live**.
- No extended custom battle performance tests or multi-year campaign endurance simulations were performed.
- In-process WPF render tests were conducted within the test harness (`ShowActivated=false`), exercising layout and templates without physical multi-monitor human interaction.
- The UI Automation smoke check inspected real accessibility peers and control patterns in read-only mode, without executing state-changing work orders or mutating persisted preferences.
- TPAC inspection verifies v2 header geometry and table-of-contents range; it does not decode compressed DXT5/BC3 texture payloads.

## Official Cryptographic Digests (SHA-256)

Extracted from `artifacts/package-sha256-2500.txt`:
- `CalradiaForge-Modules-25.0.0.zip`: `FA16BF883305B963054C957B9C3F756419AA388206CBB5BE42FA034F2B89F49A`
- `CalradiaForge-Source-SDK-25.0.0.zip`: `1D9E6A07C87B3C3B17D7DDC88FB8B60285D3F7824EB8F812308A695844B1B951`
- `CalradiaForge-Desktop-25.0.0.zip`: `F7AD0AFE754E19463EB038E0340181447001DDB0AE6305D7AE55EEC51B3A9E31`
