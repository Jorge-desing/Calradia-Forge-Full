# Calradia Forge 25.1.0 Validation

**Execution Date:** 2026-09-26  
**Test Environment:** Windows 10/11, .NET SDK 8.0, Visual Studio Build Tools, PowerShell / cmd.  
**Verification Scope:** Release compilation, full automated test suite, WPF live layout/render harness, expanded Windows UI Automation accessibility check (29 checks with SelectionItemPattern and InvokePattern), stateless behavior audit, and release distribution packaging.

## Passed Verification

- `dotnet build CalradiaForge.sln -c Release`: Clean compilation with 0 errors and 0 warnings across all 12 projects.
- `tools/Run-CalradiaForge-Tests.bat --skip-build --no-pause`: Complete test suite execution with 100% pass (726 total test cases):
  - **Asset Pipeline & TPAC Fixtures**: 8/8 passed in 0.047s (PNG signature/IHDR reader, single-sheet 4096×512 atlas bounds, structural header v2 inspection, working backup rejection, truthful Resource Browser instructions).
  - **CalradiaForge.Tests (Core / Mod / Sdk / Net472)**: 318/318 passed (non-serialized listeners, SubModule lifecycle, modulo-24 time-slicing anti-lag, dynastic succession scoring, zero GC allocation hot paths, SuiteInfo 25.1.0 synchronization).
  - **ForgeWeave Event Mesh Tests (Net8.0)**: 71/71 passed (fault isolation, quarantine policies, execution budgets, replay registry, Gauntlet UI policies, APM latency percentiles, custom events with wildcard topic matching).
  - **Desktop Protocol & MVVM Tests (Net8.0)**: 54/54 passed (PipeClient named-pipe roundtrips, atomic preferences, 13 language catalogs, command cancellation, assembly workbench version edit and commit).
  - **Desktop WPF Render & Simulation Tests (Net8.0-windows)**: 275/275 passed in 9,226 ms (150 render/layout passes, 1,265.9 - 1,468.4 ms layout call duration, 89 visual-tree snapshot builds / 1,282 hits / 105,654 visited nodes across 13 languages, 4 DPI scales, and 3 themes; deep-frozen resource dictionaries, hardware BitmapCache on cartographic board and seal).
- `tools/Test-CalradiaForge-Desktop-Uia.ps1`:
  - 29/29 checks passed in 14,294 ms (100% pass, 0 failures, 0 warnings).
  - Verified 24+ shell nodes: `Calradia Forge 25.1.0` top-level window discovery, `WindowCloseButton`, `WindowMinimizeButton`, `WindowMaximizeButton`, `HeaderSealButton`, `CommandPaletteButton`, `ConnectButton`, `SplitDeckToggleButton`, `LanguageSelector`, `ThemeSelector`, `CategorySelector`, `DecorativeAccentsToggle`, `OperationalSearchFilter`, `OperationalRailToolViewport`, `WorkbenchPageScrollViewport`, `ActiveToolInput`, `BrowseFileButton`, `RunWorkOrderButton`, `CancelWorkOrderButton`, `ExportWorkOrderButton`, `ExportEvidenceButton`, `WorkbenchReportTabs`, `WorkbenchEvidenceTab`, `WorkbenchRawResultTab`, `EvidenceLedgerItems`, `KeyboardShortcutHint`.
  - Non-mutating tab navigation check using `SelectionItemPattern.Select()` on `WorkbenchRawResultTab` (verifies dynamic materialization of `RawResultText` and restores `WorkbenchEvidenceTab`).
  - Non-mutating `InvokePattern.Invoke()` check on `SplitDeckToggleButton` (toggles open, verifies visual state, toggles closed).
- `tools/verify_stateless_behavior.ps1`:
  - 0 classes derived from `SaveableTypeDefiner`.
  - `SyncData` completely empty / free of state serialization in stateless behaviors.
  - 0 shadowing infractions with `TaleWorlds.CampaignSystem.Campaign` or `TaleWorlds.Localization` (`GEMINI.md`).
  - SubModule correctly registers behaviors in `OnGameStart` via `AddBehavior()`.
- `tools/package.ps1`:
  - 3 official release archives generated and audited without development residue, backups, engine DLLs, or app-host binaries.
  - Package integrity audited via `tools/audit_package.py` and recorded in `artifacts/package-audit-2510.json`.

## Boundaries of Evidence

It is critical to transparently state what was **NOT** executed during this validation session:
- The game *Mount & Blade II: Bannerlord* **WAS NOT executed live**.
- No extended custom battle performance tests or multi-year campaign endurance simulations were performed.
- In-process WPF render tests were conducted within the test harness (`ShowActivated=false`), exercising layout and templates without physical multi-monitor human interaction.
- The UI Automation smoke check inspected real accessibility peers and control patterns in read-only mode, without executing state-changing work orders or mutating persisted preferences.
- TPAC inspection verifies v2 header geometry and table-of-contents range; it does not decode compressed DXT5/BC3 texture payloads.

## Official Cryptographic Digests (SHA-256)

Extracted from `artifacts/package-sha256-2510.txt`:
- `CalradiaForge-Modules-25.1.0.zip`: `1791F30493BB4DFBFB7F46869DD56C0A42B5805068AC82631813DD58789EE1DF`
- `CalradiaForge-Source-SDK-25.1.0.zip`: `C052573264EDB07B3A67B876A9E43D9DDBC234515B99CB76B868FC967B71C8FF`
- `CalradiaForge-Desktop-25.1.0.zip`: `7B6FF34AFB18E087C4888AA16D66C9D60CD06E40DDA9D8D523996FE75D551E99`
