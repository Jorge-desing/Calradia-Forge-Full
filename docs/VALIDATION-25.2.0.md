# Calradia Forge 25.2.0 Validation

**Execution Date:** 2026-09-26  
**Test Environment:** Windows 10/11, .NET SDK 8.0, Visual Studio Build Tools, PowerShell / cmd.  
**Verification Scope:** Pure graphic & rendering optimization release, full automated test suite, live WPF layout/render performance benchmarks, Windows UI Automation accessibility harness (29 checks with non-mutating SelectionItemPattern and InvokePattern), stateless behavior audit, and release distribution packaging.

## Passed Verification

- `dotnet build CalradiaForge.sln -c Release`: Clean compilation with 0 errors and 0 warnings across all 12 projects.
- `tools/Run-CalradiaForge-Tests.bat --skip-build --no-pause`: Complete test suite execution with 100% pass (726 total test cases):
  - **Asset Pipeline & TPAC Fixtures**: 8/8 passed in 0.047s (PNG signature/IHDR reader, single-sheet 4096×512 atlas bounds, structural header v2 inspection, working backup rejection, truthful Resource Browser instructions).
  - **CalradiaForge.Tests (Core / Mod / Sdk / Net472)**: 318/318 passed (non-serialized listeners, SubModule lifecycle, modulo-24 time-slicing anti-lag, dynastic succession scoring, zero GC allocation hot paths, SuiteInfo 25.2.0 synchronization).
  - **ForgeWeave Event Mesh Tests (Net8.0)**: 71/71 passed (fault isolation, quarantine policies, execution budgets, replay registry, Gauntlet UI policies, APM latency percentiles, custom events with wildcard topic matching).
  - **Desktop Protocol & MVVM Tests (Net8.0)**: 54/54 passed (PipeClient named-pipe roundtrips, atomic preferences, 13 language catalogs, command cancellation, assembly workbench version edit and commit, version block synchronization asserted 25.2.0).
  - **Desktop WPF Render & Simulation Tests (Net8.0-windows)**: 275/275 passed in 9,398 - 10,010 ms (150 render/layout passes, 1,229.5 - 1,428.1 ms layout call duration, 89 visual-tree snapshot builds / 1,282 hits / 119,410 visited nodes across 13 languages, 4 DPI scales, and 3 themes; strict UI virtualization with recycling, smooth pixel scrolling `VirtualizingPanel.ScrollUnit="Pixel"`, offscreen buffer pre-caching `CacheLength="1,1"`, pre-frozen brush caches, and crisp subpixel `EdgeMode="Aliased"` frame rendering).
- `tools/Test-CalradiaForge-Desktop-Uia.ps1`:
  - 29/29 checks passed in 29,365 ms (100% pass, 0 failures, 0 warnings).
  - Discovered and verified top-level window `Calradia Forge 25.2.0` (PID 28704).
  - Verified 24+ shell nodes: `WindowCloseButton`, `WindowMinimizeButton`, `WindowMaximizeButton`, `HeaderSealButton`, `CommandPaletteButton`, `ConnectButton`, `SplitDeckToggleButton`, `LanguageSelector`, `ThemeSelector`, `CategorySelector`, `DecorativeAccentsToggle`, `OperationalSearchFilter`, `OperationalRailToolViewport`, `WorkbenchPageScrollViewport`, `ActiveToolInput`, `BrowseFileButton`, `RunWorkOrderButton`, `CancelWorkOrderButton`, `ExportWorkOrderButton`, `ExportEvidenceButton`, `WorkbenchReportTabs`, `WorkbenchEvidenceTab`, `WorkbenchRawResultTab`, `EvidenceLedgerItems`, `KeyboardShortcutHint`.
  - Non-mutating tab navigation check using `SelectionItemPattern.Select()` on `WorkbenchRawResultTab` (verifies dynamic materialization of `RawResultText` and restores `WorkbenchEvidenceTab`).
  - Non-mutating `InvokePattern.Invoke()` check on `SplitDeckToggleButton` (toggles open, verifies visual state, toggles closed).
- `tools/verify_stateless_behavior.ps1`:
  - 0 classes derived from `SaveableTypeDefiner`.
  - `SyncData` completely empty / free of state serialization in stateless behaviors.
  - 0 shadowing infractions with `TaleWorlds.CampaignSystem.Campaign` or `TaleWorlds.Localization` (`GEMINI.md`).
  - SubModule correctly registers behaviors in `OnGameStart` via `AddBehavior()`.
- `tools/package.ps1`:
  - 3 official release archives generated and audited without development residue, backups, engine DLLs, or app-host binaries.
  - Package integrity audited via `tools/audit_package.py` and recorded in `artifacts/package-audit-2520.json`.

## Boundaries of Evidence

It is critical to transparently state what was **NOT** executed during this validation session:
- The game *Mount & Blade II: Bannerlord* **WAS NOT executed live**.
- No extended custom battle performance tests or multi-year campaign endurance simulations were performed.
- In-process WPF render tests were conducted within the test harness (`ShowActivated=false`), exercising layout and templates without physical multi-monitor human interaction.
- The UI Automation smoke check inspected real accessibility peers and control patterns in read-only mode, without executing state-changing work orders or mutating persisted preferences.
- TPAC inspection verifies v2 header geometry and table-of-contents range; it does not decode compressed DXT5/BC3 texture payloads.

## Official Cryptographic Digests (SHA-256)

Extracted from `artifacts/package-sha256-2520.txt`:
- `CalradiaForge-Modules-25.2.0.zip`: `EBC5E959175503A1AED75B4A879C01CF2E0BF2BEC1F7F848E4CE77104C45888A`
- `CalradiaForge-Source-SDK-25.2.0.zip`: `979617F0F5F05562E42749D884F00130C46F33D137115647EAD207A3B91318BB`
- `CalradiaForge-Desktop-25.2.0.zip`: `56D3B5D994BF7B6225BACE7A192E69F16C0ADEA077199CADCBF6CA36FF4BD471`

## Follow-up validation — 2026-10-02

This is a separate follow-up run on version 25.2.0; the product version and public SDK API were not changed. It covers the Campaign Rule Builder starter-row/event affordance and the generator/pipeline fixes that keep Gauntlet bindings and localized resources intact after packaging.

### Passed verification

- Release build completed with 0 warnings and 0 errors. The Client and Modding Kit editor profiles both compiled during deployment.
- `tools/verify_stateless_behavior.ps1`: all four acceptance checks passed.
- `tools/audit_gauntlet_ui.py`: PASS, 0 errors and 0 warnings.
- `tools/audit_localization.py`: PASS; 13 native languages with 747 keys each, including 71 Campaign Rule Builder keys.
- `tools/Run-CalradiaForge-Tests.bat --skip-build --no-pause <nul`: all selected suites passed. Campaign Rule Builder regressions cover first-visit starter identity, reorder/removal, save/reload, generated behavior compilation, and local preview/copy. The WPF render suite passed 295 cases; ForgeWeave passed 73 tests; Desktop MVVM passed 65 tests.
- `tools/package.ps1`: the three 25.2.0 archives passed the archive audit. Independent SHA-256 checks matched `artifacts/package-sha256-2520.txt`.
- Steam deployment completed to Client and Modding Kit profiles with backups. The installed prefab, English and Spanish language resources, and Client assembly matched their workspace hashes. The Steam runtime TPAC remained unchanged at `8899A48A407591ADA53573EFC0DD699EA47D2A30F32A0C12C1875003F7993047`.
- Bannerlord launched through the documented Steam path and reached the 1920×1080 main menu. The available Computer Use session captured the game window, but F10 did not reveal the Forge panel and arrow input did not change the menu selection. Therefore the in-game panel, keyboard interaction, and the requested smaller layouts are **not verified** in this run. The game was closed afterward.
- TPAC header/table structural preflight passed. Deep TPAC parsing was skipped because TpacTool and the local native fixture were unavailable; no TPAC import was attempted.

### Follow-up package SHA-256

- `CalradiaForge-Modules-25.2.0.zip`: `CE75B6B186D4D26DD1113BB2A02294317EF5B9353929EAF805DAEF8AEA868E48`
- `CalradiaForge-Source-SDK-25.2.0.zip`: `417C8215289EE64319E4C9F838CEE4B17C640D8903ECDE32B4BD3A59119660FF`
- `CalradiaForge-Desktop-25.2.0.zip`: `B49CA3A85944DCCAA2580DF3819C842B8DB95174FC4AD92CAB603C74FF3F71B5`

## Follow-up hook ID normalization correction — 2026-10-02

A code review caught a potential compatibility regression in rejecting public hook IDs that begin with `CalradiaForge.Hook.`. Registration continues to accept printable IDs in that shape. The runtime prefix is prepended exactly once; unqualified order references are local IDs and prefixed values are fully qualified runtime tags. Regression tests cover registration, self-reference detection, cycle detection and console order diagnostics for prefix-shaped IDs.

- `dotnet build CalradiaForge.sln -c Release -v:minimal`: succeeded with 0 warnings and 0 errors.
- `cmd.exe /c "tools\Run-CalradiaForge-Core-Tests.bat --no-pause <nul"`: Core 416 passed, 0 failed; the BAT also completed the serial x64 DetourFixture and hook/IL lifecycle scenarios successfully.
- `cmd.exe /c "tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause <nul"`: ledger chain, playbooks, code-smell audit, and docs parity command passed; the audit reports 12 older English-only docs without Spanish counterparts.
- The full master test BAT was unavailable because it was already deleted in the working tree before this correction; no deleted file was restored or staged.
- Live application of the owned menu hook fixture remains pending at the user's request. Computer Use selected Bannerlord's window ID but returned a Rogue Command screenshot, so no game input or hook application was sent under that ambiguous capture.

## Live hook fixture clarification — 2026-10-02

- Repository inspection found no in-game Forge-owned hook provider registered by the product or examples. `tests/CalradiaForge.DetourFixture` is an isolated x64 test host, not a Bannerlord module, and cannot prove menu integration.
- No owned hook fixture is currently deployed in Bannerlord. A dedicated test module/provider must be created and loaded before a main-menu smoke test can verify apply, verify, and revert in the game. This remains deferred at the user's request; no game input or in-game hook operation was performed.
