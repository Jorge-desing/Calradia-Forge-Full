# Changelog

## Current patch management audits — 2026-10-01 (Registry Rev103)

- Agent audits follow current receipt synchronization and tracked detour verification instead of obsolete names, with five regressions rejecting missing protections. Hosted agent stateless verification uses the portable BAT gateway. Local Python checks pass; remote optional-agent validation is pending. Static management evidence does not prove concurrent-target safety.

## Post-push review and inventory encoding — 2026-09-30 (Registry Rev102)

- Git rules and the development skill require review of the exact pushed SHA and its remote checks, separating delivery from validation. Windows PowerShell inventory diagnostics now use an ASCII range separator; a Windows-1252 parser regression passes through BAT. Remote d59db82 passed WPF 293/293 but exposed the inventory encoding failure; the next remote validation remains pending.

## Isolated WPF runner viewport — 2026-09-30 (Registry Rev101)

- The render fixture expands only its isolated HWND tracking bounds so a smaller hosted display cannot constrain the fixed 1360×820 DIP matrix. A regression exercises native 1024×768 limits; the application and display configuration are unchanged. Desktop BAT passed 65/65 and WPF render 293/293 with 182 layout passes and a clean build. The first pushed ledger workflow passed; CI compiled and passed Desktop but exposed this render-host constraint. Remote checks for this correction remain pending.

## Hosted CI recovery — 2026-09-30 (Registry Rev100)

- Hosted CI builds portable targets through `CalradiaForge.Portable.slnf` and the BAT launcher; local Bannerlord integration retains `net472` and requires licensed TaleWorlds assemblies through `GameBin`. Core remains multi-targeted. Ledger audits recognize split Desktop ViewModels and immutable patch snapshots. Decorative overlap checks honor source-verified mutually exclusive workspace visibility and the current evidence-margin condition. Full local tests passed Core 401/401, ForgeWeave 73/73, Desktop 65/65 and WPF render 292/292; Python CI checks passed 12 asset fixtures, 5 image tests, Ruff and sprite/icon checks. The optional agent dependency installation encountered a truncated package-mirror response locally. New GitHub checks are pending at this revision; historical failed runs remain in Actions history.

## 25.2.0 source follow-up — 2026-09-30 (Registry Rev076)

- Makes hook-handle and patch-handle disposal report failed reverts instead of silently dropping the result. Hook dispatch rechecks the approved context after Prefix and before Postfix, preserving original arguments if the gate closes before the target and skipping later callbacks if the target leaves the context. Forge keeps `Patches` published while unresolved patch records need recovery, allows a Revert only after recorded installed/original bytes are verified, and restores the previous host lifecycle if an incoming host fails to reconnect after teardown. BAT validation passed Core 393/393, ForgeWeave 73/73, the serial x64 detour fixture, Desktop 65/65, and WPF render/resources 292/292 with 182 layout passes. No direct fixture EXE, live Bannerlord, campaign, battle, or ZIP regeneration.

## 25.2.0 source follow-up — 2026-09-30 (Rev075)

- Hook dispatch now checks the host's exact approved game-thread main-menu gate for every invocation. Outside that context it skips custom callbacks and invokes the original target, while the detour stays installed until explicitly reverted. WPF can plan recovery Reverts for `Conflict` and `Failed` snapshots, and its confirmation view warns that multi-hook Apply is sequential and may leave earlier hooks applied after a later failure. All 13 Desktop locales carry the lifetime and partial-Apply warnings. BAT validation passed: Core 390/390, ForgeWeave 73/73, serial x64 detour fixture, Desktop 65/65, and WPF render/resource 292/292 with 182 layout passes; builds had zero warnings/errors. No live Bannerlord check was made, serial fixtures do not prove concurrent-target safety, `ForgeApi.Version` remains 12, product version remains 25.2.0, and ZIPs were not regenerated.

## 23.0.0 source follow-up — 2026-09-25 (Rev033)

- Strengthens shared-provider diagnostics while preserving the existing fail-fast `Require<T>` contract and exception types. Missing-provider, missing-service, CLR contract-identity, and version failures now carry provider/service context, the expected and published contract or versions where applicable, and a configuration correction path. Expands integration coverage for provider/consumer lifecycle, shared contract identity across assemblies, version rejection, invalidated handles, manifest dependencies, and single-provider packaging of the contracts DLL. `ForgeApi.Version` remains 6; public signatures, `net472`, load order, and game-thread rules are unchanged. `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` built with zero warnings/errors and passed Core 252/252 and ForgeWeave 37/37; the batch host ran the Core test library in-process. No test executable was started directly, no ZIPs were created, and Bannerlord was not launched.

## 23.0.0 source follow-up — 2026-09-25 (Rev034)

- Extends shared services with optional `ModuleLibrary.Resolve<T>` results that distinguish available, missing-provider, missing-service, contract-mismatch, and incompatible-version outcomes while preserving fail-fast `Require<T>` and its exception contract. Adds transactional `SharedServiceBatch` publication through `BeginPublication`, `Add`, and `Commit`; a committed batch remains the lifetime lease and withdrawing it removes the group together. Adds `SharedServiceMonitor<T>` through `Watch<T>`, with an initial `Current` snapshot and synchronous registry-thread `Changed` notifications for later changes. `Changed` event arguments are immutable transition snapshots and may be stale by the time later handlers run; read `monitor.Current` or call `Resolve<T>` for the latest state, since a handle may have been invalidated by withdrawal. A registry mutation inside a `Changed` handler takes effect synchronously; only callback delivery is queued to prevent recursive dispatch. Handler failures are isolated and exposed through `LastNotificationError`. Disconnect silently disposes monitors. `ForgeApi.Version` advances from 6 to 7; product version remains 23.0.0, SDK target frameworks remain `net472` and `net8.0`, and game-module integration remains `net472`; no IPC or game-module behavior is changed. `cmd /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` completed with build 0 warnings/0 errors, Core 263 passed/0 failed, and ForgeWeave 43 passed/0 failed. No test executable was started directly, no ZIPs were generated, and Bannerlord was not launched.

## 22.0.0 source follow-up — 2026-09-25 (Rev032)

- Rev032 reshapes the standalone WPF workbench around a compact header, adaptive navigation and a keyboard-accessible report surface while preserving the 194 tool routes, existing commands and permissions, MVVM/IPC contracts, 13 locales and three themes. It adds optimized local cartographic and heraldic artwork to passive chrome in War Table and Parchment Light; High Contrast remains solid, and input, evidence and command surfaces stay clear. Validation remains pending: five-run same-machine render-harness comparisons, coverage-preserving Desktop/render BAT suites, and a read-only WPF launcher inspection. Harness timings are not application latency; no performance gain or live UI pass is claimed here. No public API, IPC, version, ZIP or Bannerlord resource changed.

## 22.0.0 source follow-up — 2026-09-24 (Rev031)

- Rev031 adds three original, deterministic local WPF textures for the title frame, operational rail, and tool-card corners. Their optimized packaged derivatives add 92,537 compressed bytes and 273,328 decoded RGBA bytes; artwork remains passive, while evidence, editable fields, commands, and High Contrast surfaces stay clean. The search field gains a localized clear action that appears only for non-empty text, clears the existing filter, and restores keyboard focus without adding public API. The render harness avoids reapplying an unchanged theme during the localization matrix. The complete Desktop batch built with zero warnings/errors, passed Desktop 51/51 and WPF 272 cases with 148 layout passes; deterministic texture validation passed. Five final render-BAT runs had a 5.846 s median versus a freshly measured 6.337 s pre-change median (7.7% faster). A read-only launcher smoke exposed the expected WPF window and primary controls through UI Automation. These are harness and shell-startup checks, not gameplay validation. No version, public API, IPC, ZIP, Bannerlord session, campaign, or battle changed.

## 22.0.0 source follow-up — 2026-09-24 (Rev030)

- Rev030 scopes WPF responsiveness measurements to startup, filtering, route selection, and asynchronous operations. It distinguishes thread-local synchronous allocation counts from async work, for which elapsed duration remains available and allocated bytes are reported as unavailable. Clearing a filter now releases the selected page, and disposed pages cannot retain late async results. The render suite checks all 194 routes/icons/bindings while reserving full layout for representative states, languages, themes, long text, evidence states, and 100–200% scaling. Desktop passed 51/51; WPF render/resource checks passed 271 cases with 146 representative layout passes; the build had zero warnings/errors. Five render-BAT runs were 5.675, 5.677, 5.749, 5.703, and 5.738 s (median 5.703 s, 46.0% below the 10.559 s baseline). These are harness timings, not live app latency; route selection had one synthetic sample above 16.7 ms. Read-only WPF UI Automation smoke was not run because the tool policy rejected the isolated launch/inspection command. All tests ran via `.bat`; no test binary was launched directly. No API, IPC, version, game asset, or ZIP changed.

## 22.0.0 source follow-up — 2026-09-24 (Rev029)

- Rev029 hardens cancellation and file transactions across Desktop and extension lifecycle work: named-pipe requests distinguish cancellation from timeout and disconnect after an interrupted response; assembly backups are copied and hashed in bounded chunks, staged output is revalidated before commit, and cleanup preserves files won by a competing process. Gauntlet page teardown is dispatched on the game thread, while extension queue capacity uses atomic accounting and trusted cleanup work is not dropped at saturation. The full `.bat` suite passed: build 0 warnings/errors, Core 239/239, ForgeWeave 31/31, Desktop 50/50, WPF render 268 cases, and all asset/preflight checks; source/test review scripts reported zero findings. The render harness recorded 507 passes in 11.334 s; this is not live app latency. No version, public API, ZIP, TPAC, installed resource, game session, campaign, or battle changed.

## 22.0.0 source follow-up — 2026-09-24 (Rev028)

- Rev028 moves the bounded analyzer's evidence projection and report formatting off the WPF dispatcher while preserving cancellation. Local WPF texture derivatives are now generated deterministically: only 6,901,545 compressed bytes and 11,489,464 decoded image bytes are packaged, down from 17,864,675 and 64,474,344 for the original full-size set. The authoring masters remain outside the application resources. Test maintenance removes per-fixture PowerShell process launches, avoids duplicate prefab parsing, and gives PNG fixtures isolated temporary directories. A five-run API-test median improved from 2.72 s to 2.28 s; Asset Batch fell from 14.699 s to 3.446 s and Resource Browser from 22.390 s to 3.943 s. The focused `--core-only` runner preserves both Core and ForgeWeave suites and measured 7.319 s before versus 6.115 s after in one run. The complete batch built with zero warnings/errors and passed AssetPipeline, AssetBatchPlan, ResourceBrowser, FBX preflight, Core 232/232, ForgeWeave 31/31, Desktop 42/42, and WPF render 268/268 in 34.25 s; render harness time was 9.866 s for 507 layout passes. These are test and packaging measurements, not a live application latency benchmark. No version, API, ZIP, TPAC, installed asset, campaign, or battle changed.

## 22.0.0 source follow-up — 2026-09-24 (Rev027)

- Rev027 reduces Desktop workbench churn by caching each immutable tool's search text, batching observable-collection notifications, reusing visible group view models, and reusing pinned/recent snapshots. The operational rail now virtualizes and recycles its rows, so the 194-route catalog is not materialized as a full control tree. Orphaned, unregistered source-contract test helpers were removed after useful assertions were retained in active cases. The Desktop-only runner now builds both Desktop test projects and their dependencies through one solution filter, instead of invoking two separate builds or the full solution. The WPF harness no longer drains the Dispatcher at ApplicationIdle for every frame; synchronous layout still passes the full matrix. Latest `.bat` run: build 0 warnings/errors, Desktop unit tests 42/42, WPF render tests 268/268; 507 render/layout passes completed in 9.822 seconds and the complete Desktop-only batch took 13.974 seconds. This is a harness result, not a product-runtime benchmark. No release version, public API, ZIP, TPAC, campaign, or battle changed.

## 22.0.0 source follow-up — 2026-09-24 (Rev026)

- Rev026 completes the Desktop and Gauntlet visual round without changing version, route IDs, public API, or saved preferences. WPF now verifies 11 locally packaged textures, theme-specific Parchment/War Table marks, a localized empty-evidence action, accessible custom title-bar controls, theme-aware ComboBox states, and a recycling virtualized rail. The desktop-only BAT built with zero warnings/errors; Desktop unit tests passed 42/42 and the WPF suite passed 268 cases, including all 194 current tool routes, 13 languages at four DPI scales, and three themes at four scales. The render harness completed in 10.047 seconds; this is faster than the immediately preceding 12.3-second run, but remains above the earlier 8.66-second baseline. Gauntlet source checks report deterministic texture preparation, current SpriteData, a clean prefab audit, and Core 232/232. The installed TPAC still predates the source atlas; Resource Browser import and in-game rendering remain pending. No TPAC or ZIPs were written.

## 22.0.0 source follow-up — 2026-09-24 (Rev025)

- Rev025 records the completed ImageGen source pipeline: four deterministic material sprites, a TaleWorlds-generated 2048×128 source atlas, current SpriteData with all 13 declared parts in bounds, and verified backups before replacement. Decorative and Gauntlet layout audits passed, the solution built with zero warnings/errors, Core passed 232/232, ForgeWeave passed 31/31, and the batch visual suite passed after rebuilding stale tests. The runtime TPAC remains older; Resource Browser import and live rendering are still pending. No ZIPs were generated.

## 22.0.0 source follow-up — 2026-09-24

- Rev024 replaces the geometric Gauntlet decoration set with four ImageGen material masters—pine wood, ink wash, aged brass, and pine felt—processed locally and deterministically into atlas-ready sprites. Their placement is limited to interface chrome; evidence, editable fields, and commands stay on clean surfaces. Routes and behavior are unchanged. Sprite tests, atlas regeneration, Resource Browser import, and live rendering remain pending; no TPAC or ZIPs were changed.

## 22.0.0 source follow-up — 2026-09-23 (not included in the existing ZIPs)

- Rev021 follow-up: ran TaleWorlds' official sprite generator on an isolated staging copy and regenerated the source atlas and SpriteData with the two original decorative sprites. The decorative validator now confirms exact atlas-pixel crops, metadata dimensions, AlwaysLoad registration, and safe prefab placement. The existing TPAC still predates the new atlas; Resource Browser import and live rendering remain pending. No ZIPs were regenerated.
- Rev022 adds three original deterministic Gauntlet sprite sources: `forge_table_grain` (128 × 64) for passive header/rail chrome, `forge_map_contours` (128 × 64) behind the existing header filigree, and `forge_brass_rule` (128 × 16) for a real section divider. Input, command, and evidence surfaces remain clean. Structural validation is pending; the existing TPAC is stale, Resource Browser import and live rendering are pending, and no ZIPs or version metadata changed.
- Rev023 records a third Gauntlet visual source round with `forge_heraldic_corner` (128 × 32), replacing `forge_header_filigree`, plus a woven-border texture. The replacement preserves the 2048 × 256 atlas and keeps `forge_pine_grain`; navigation, command, status, and keyboard-focus hierarchy are also refined. Decorations remain passive and away from evidence and editable surfaces; the eight areas, API, behavior, and version are unchanged. Structural checks and atlas generation for this revision are not yet recorded; TPAC import and live rendering remain pending. No ZIPs were regenerated.

- Added the standalone experimental `BannerlordFbxImporter` with four explicit batch profiles, bounded recursive scanning, duplicate-basename rejection, configured texture extensions, explicit texture-assignment maps, advisory FBX ASCII material evidence, and a separately tested hash-verified backup service. Kept `--submit` locked before confirmation and UI because the Resource Browser route, settings, material inventory, final Import control, and verified examples are not calibrated. `Test-BannerlordFbxImporter.bat` built cleanly and passed 32 tests; one symbolic-link fixture was skipped because Windows denied link creation. A read-only Editor inspection was inconclusive; no real import or ZIP rebuild occurred.
- Confirmed from TaleWorlds' sprite guide that `Alt + backtick` followed by `resource.show_resource_browser` is the documented way to open Resource Browser. The `Enter` in that procedure submits the console command; it does not establish that SpriteSheetGenerator waits for redirected stdin. The FBX helper still does not run the generator. Each reviewed FBX is now revalidated against scanned size/time metadata and held under a read-only sharing lease through UI submission; the responsive-window check uses a bounded `WM_NULL` probe. Short tests pass with 59 assertions; no Editor import or ZIP rebuild was performed.
- Reviewed the additional pasted C# automation proposal: official console instructions do not verify `SetForegroundWindow`/`SendKeys` timings or safe UIA dismissal of import errors. These are not integrated; unexpected dialogs remain open and stop processing.
- Fail-closed review: a UI Automation enumeration error or any visible/nested modal can no longer be mistaken for a closed file dialog and reported as `SUBMITTED`. Added a bounded total filesystem-entry limit and capped reparse-point warnings. Replaced the stale experimental-helper README and recorded the PDF claims with source-specific confidence; no Editor import or ZIP rebuild was performed.
- Follow-up hardening: unreadable Resource Browser tree paths and nested `Assets` folders now fail closed; regression fixtures pass through `Test-BannerlordImportAutomation.bat` (both builds: zero warnings/errors). Existing destination-name collisions are explicitly a manual Resource Browser check because the helper does not inventory installed assets. The Editor UI and actual submission remain unverified after the environment denied launching the helper executable.
- Hardened the maintained FlaUI Resource Browser helper: `.fbx`/`.png` top-level inputs only, a 100-file batch ceiling, explicit visible module targeting, unique `module > Assets` path resolution, and stop-on-uncertain-UI behavior with no retries. Added no-Editor tests and marked the old duplicate project as reference-only. Existing 22.0.0 archives were not rebuilt; `SUBMITTED` does not claim TPAC compilation or runtime success.
- Corrected the TPAC structural audit to use the 36-byte v2 header and bounded table range; unrecognized versions are reported as unsupported without reading v2-specific fields.
- Added MVVM-bound file and folder pickers for Desktop tools that require local input, with localized labels and cancellation preserving the current input.
- Added a read-only TPAC metadata diff for TpacTool inventory reports, matching assets by GUID and exporting added/removed/changed evidence with no input overwrite.
- Added a bounded, read-only Asset Batch Plan `.bat` that inventories raw source trees, hashes known candidates, marks unverified extensions, surfaces repeated basenames for human review, and writes atomic JSON reports with backup-on-replace. It does not synthesize `.meta` sidecars, invoke a compiler, or claim imports succeeded.
- Upgraded the Asset Batch Plan report to schema v4 with explicit generator availability, manual/blocked import stages, bounded safe `SpriteData` parsing, exact atlas/category/sheet mapping hints, and short planner duration/process working-set observations. Their scope is explicit; generator/game performance, allocations, and crash reduction remain unmeasured. Predicted TPAC paths remain unverified, and the planner never starts the generator or Resource Browser.
- Added an evidence review for the supplied AI-generated asset-pipeline infographic. It retains the documented sprite-generation/import sequence and excludes unsupported `.meta`/`.fbs` transforms, guessed compiler flags, redirected-stdin assumptions, Roslyn injection, and unsubstantiated throughput/crash figures.
- Added short observed planner measurements for inventory duration and the PowerShell host's lifetime peak working set, with explicit scope; allocation, generator, game, and crash metrics remain unmeasured.
- Restored the supplied `BannerlordImportAutomation` C# entry point required by its project file, documented its read-only inspection and opt-in experimental submission boundaries, and recorded FlaUI's MIT notices for the Source-SDK. Its BAT test now builds and passes without opening the Editor.
- Clarified that the inspected Bannerlord workflow does not document a C# FBX batch importer or `.meta` sidecar contract. The existing supported atlas flow remains SpriteSheetGenerator plus Resource Browser import.
- Added a source-by-source review of the supplied 18-page asset automation report. It confirms the documented folder, LOD/material naming, UI atlas, and NativeTextureExporter workflows while marking guessed sprite-generator CLI switches, Reflection/Harmony FBX import, universal axis rules, TPAC internals, and editor memory thresholds as unsupported or unverified.
- Kept the existing source and Steam-installed TPAC unchanged. Their header/table bounds pass, but the available TpacTool reader cannot parse the asset GUID, so metadata validation remains unresolved; these corrections have not been repackaged into the existing archives.
- Added a bounded read-only FBX ASCII declaration analyzer to the Desktop Assets workbench and a matching command-line `.bat`. It reports LOD naming advisories, material declaration names, and binary/unsupported inputs without importing assets. The current 22.0.0 archives were not rebuilt.
- Reviewed the updated 1.2 asset-pipeline infographic/PDF against TaleWorlds' asset naming and sprite docs plus community FBX import guidance. The Desktop Core analyzer now honors cancellation during traversal/parsing; the command-line BAT can be interrupted from its console. Both report declared Camera/Light nodes for human review. No FBX rewriting, guessed axis conversion, hidden generator flags, or automatic editor import was added. The archives were not rebuilt.
- Improved TpacTool inventory comparison provenance: each report's capture timestamp is normalized to UTC, preserved in before/after JSON, displayed by the `.bat` CLI, and validated before a diff is produced. Added an opt-in strict inventory-test mode so a missing or Windows-blocked reader cannot be mistaken for a complete local audit. Existing 22.0.0 archives were not rebuilt.
- Hardened TPAC diff input validation against missing/null nested fields, invalid unsigned numbers, empty or out-of-file v2 table ranges, trailing line breaks, and RFC 3339's unknown `-00:00` offset. Regression fixtures require actionable rejection messages; existing archives remain unchanged.
- Aligned external report validation with the reader's contract: `assets` must be a JSON array and declared source size cannot exceed 128 MiB. Fixtures reject scalar assets and a source size one byte above the bound; the reader's native behavior is still blocked by Windows.
- Refined the native Gauntlet panel into a war-table layout with an eight-route vertical rail, current-section context, status cards, command deck, untextured evidence ledger, and lower action/pagination rows. Preserved the existing routes, commands, ViewModel property, and game-state protections; gave the two `@Argument` fields unique control IDs. Added deterministic low-alpha source PNG decorations (`forge_header_filigree` and `forge_pine_grain`) and registered their SpriteData/prefab references. The source-level sprite validator and short Core/ForgeWeave tests pass; Resource Browser atlas/TPAC rebuild and live rendering remain pending. No version bump or ZIP rebuild occurred.

## 22.0.0 — 2026-09-22 — structural Gauntlet sprite audit

- Extended the built-in asset analyzer to validate `SpriteData` sheet sizes, IDs, part references, safe source paths, and sprite-part rectangles against declared atlas bounds.
- Added bounded PNG signature/IHDR dimension checks for each referenced source part and generated atlas, with explicit findings for missing files, malformed headers, and dimension mismatches. These checks do not decode pixels or verify PNG CRCs.
- Extended the reproducible Game-icons asset validator with the same part/atlas reference and dimension checks while preserving upstream attribution and generated sprite resources.
- Strengthened TPAC readiness evidence: the asset analyzer reads the 36-byte v2 fixed header and rejects truncated headers, zero or excessive declared asset counts, and table-of-contents ranges outside the file. Other format versions are reported as unsupported without applying v2 offsets.
- Kept the finding explicit that a plausible fixed header is not payload validation; it directs authors to the optional TpacTool metadata preflight and Bannerlord Resource Browser recovery path.
- Verified the documented Bannerlord path remains SpriteSheetGenerator followed by Resource Browser import. No public C# FBX importer or `.meta` sidecar batch-import contract was found in the installed managed assemblies or the official workflow documentation; no speculative importer was added.
- Reworked preflight failures into concise, hash-bearing diagnostics with a recovery step; failed Resource Browser collection now stops before writing to the source package and preserves the parser reason without a PowerShell stack dump.
- Excluded timestamped Resource Browser working backups from both release source/module archives and made the archive auditor reject accidental backup inclusion.
- Added valid and invalid sprite fixtures and published three separate 22.0.0 distributions.

## 21.0.1 — 2026-09-22 — Gauntlet sprite diagnostics and TPAC preflight

- Tightened the Gauntlet sprite analyzer: it now checks always-loaded category metadata against `SpriteData.xml` and generated atlas counts, and distinguishes missing, empty, unreadable, invalid-marker, and TPAC-marked packages. A matching four-byte marker is reported as limited structural evidence, never as proof that the package payload is valid or loadable.
- Added a read-only `Inspect-CalradiaForge-Tpac.bat` preflight that reports package size and SHA-256 before optionally opening the locally supplied TpacTool. It warns that upstream 0.4.0 targets Bannerlord 1.8.0 beta, so use with this 1.4.8 project is unverified; it does not redistribute TpacTool.
- Added fixture checks for valid and malformed markers and corrected the resource workflow documentation to match the imported TPAC in the source module and release package.
- Updated the release metadata and manifests to 21.0.1 while preserving the 21.0.0 feature set and separate Modules, Source-SDK, and Desktop distributions.
- Regenerated and audited the three 21.0.1 ZIPs after the short batch-based suites, DocFX/help/icon generation, native/Desktop localization checks, and read-only TPAC preflight passed.

## 21.0.0 — 2026-09-22 — Gauntlet extensions, in-game assembly tools, and contextual help

- Added public `[ForgeUiPage]` and `[ForgeUiCommand]` SDK metadata with assembly-scoped `ForgeApi.AutoRegister`, validated module-owned prefab paths and `Command.Click` bindings, per-page error isolation, owner cleanup, and a standalone Gauntlet overlay opened from Extensions.
- Added open-time page/command context checks, test-mode and copied-campaign gates for state-changing declarations, and game-thread queuing for extension open/close requests. The attributes do not sandbox extension code.
- Integrated AsmResolver 6.0.1 into the Bannerlord net472 module for bounded static PE/.NET metadata inspection on a worker thread. Added an explicit preview/apply version edit that rejects signed and mixed-mode inputs, preserves the source, creates a separate backup/output, records hashes, and validates the result.
- Added selected, attributed Game-icons.net source SVGs, generated transparent sprite-part PNGs, sprite metadata, an always-loaded category, and the Resource Browser-generated runtime TPAC. A short direct Modding Kit menu check on Bannerlord 1.4.8 confirmed the native prefab renders its semantic icons without the missing-texture warning.
- Added `Build-CalradiaForge-UiAssets.bat` to run and validate Bannerlord's official single-module sprite-sheet pipeline, plus a dedicated Desktop **Gauntlet Sprite Package Auditor** for missing source atlases, missing TPAC packages, empty outputs, and bounded scan limits.
- Added `Prepare-CalradiaForge-ResourceBrowser.bat` to validate and stage the generated atlas and metadata into the installed module, then collect the compiled TPAC back into the source tree. It verifies hashes, backs up replaced files, supports dry-run, and reports version mismatches without copying DLLs or manifests. The imported TPAC is included in the 21.0.0 source module.
- Expanded the Desktop sprite-package finding with the complete stage/import/collect flow and the explicit rule to keep module DLLs and manifests separate when versions differ.
- Refined Desktop and the native Gauntlet definitions with purpose-labeled Game-icons: Desktop groups Build, Inspect and Verify in the header, assigns an area icon to each tool page, and marks evidence and work-order status cards with matching symbols. The short in-game check confirmed the imported category is available to Gauntlet.
- Moved each Desktop theme's complete brush set into its own palette dictionary and made theme changes replace the dictionary as a unit. This fixes stale text colors and removes duplicate palette/language dictionaries during repeated changes.
- Kept Recent bounded to twelve items in its own scrolling viewport and added WPF checks for effective theme colors, icon availability on all routes, and dictionary uniqueness.
- Added build-time DocFX API generation and offline, 13-language in-game help summaries derived from the SDK XML documentation.
- Updated module packaging to include AsmResolver runtime dependencies, licenses, icon attribution, and sprite sources; updated archive audits and release scripts for the 21.0.0 outputs.
- Added focused UI registry and assembly workbench tests. Ran one short direct Modding Kit launch, opened and closed Forge at the main menu, and verified all nine sprite icons render; no campaign, battle, or long-running validation was run.

## 20.0.0 — 2026-09-22 — vector UI, declarative pages, and assembly workbench

- Replaced font-backed icon controls and emoji work-area labels with embedded, recolorable WPF geometries. The header now uses a responsive layout and clipped title text rather than overlapping decorative glyphs.
- Added selected Game-icons.net SVG assets with reproducible WPF geometry generation and per-icon artist/source/license/change records under CC BY 3.0.
- Connected `[ForgeUiPage]` and `[ForgeUiCommand]` metadata to a validated one-time catalog that creates declared pages and checks regions, slots, IDs, ICommand bindings, and cancellation contracts. XAML remains responsible for layout; no visual-tree discovery was added.
- Added Desktop-only AsmResolver 6.0.1 metadata inspection for managed assemblies without executing or runtime-loading the inspected file.
- Added an opt-in two-step assembly version editor. Preview writes nothing; apply creates a separate output plus `.source.bak`, records SHA-256 hashes, validates the output PE, and rejects strong-name signed assemblies.
- Added a pinned DocFX 2.77.0 tool manifest and a local-only static site built from English Markdown and the public SDK XML documentation.
- Removed unused WPF UI symbol and MahApps icon-pack runtime dependencies. AsmResolver remains Desktop-only; game modules keep their existing runtime dependencies.
- Added focused WPF render checks for metadata inspection, no-write preview, copy/backup/version validation, source preservation, and invalid assembly rejection. Game campaign/battle and long-running tests were not run.

## 19.0.0 — 2026-09-22 — Desktop toolkit integration

- Added pinned CommunityToolkit.Mvvm 8.4.2 adapters for observable state, parameterized commands, and cancellation-aware asynchronous work while preserving the existing routed-page contracts.
- Added MaterialDesignThemes.Wpf 5.3.2 resources, WPF UI 4.3.0 symbols and controls, and MahApps.Metro.IconPacks 6.2.1 material glyphs to the Desktop workbench. The four libraries are Desktop-only; the Bannerlord modules remain standalone.
- Replaced the header and rail glyph samples with toolkit-backed controls while retaining the tactical palette, bounded Recent/Pinned viewports, keyboard focus, and evidence behavior.
- Pinned all package versions and documented licenses, URLs, and transitive icon attribution in `THIRD_PARTY_NOTICES.md`.
- Published the three 19.0.0 archives after short build, MVVM, WPF resource, localization, and package checks. No campaign, battle, endurance, or long-running game test was executed.

## 18.0.0 — 2026-09-22 — Routed Operations Rail

- Reorganized the Desktop left rail into explicit collapsible sections with glyphs, counts, stable expansion state, independent scrolling, and a clear separation between pinned tools, recent history, and the full work areas list.
- Added section-aware search and category filtering without visual-tree traversal; every route keeps its identifier, command, input requirement, guard state, and evidence result.
- Added a bounded main-section viewport and keyboard-visible group headers so long catalogs remain navigable at 100–200% scaling.
- Added 54-key resource catalogs with localized section-scroll help across all 13 Desktop languages, keeping technical identifiers, paths, JSON, and raw evidence unchanged.
- Fixed group classification for Politics, Economy, and Combat tools and retained the previous theme, IPC, ForgeWeave, evidence, and page-disposal contracts.
- Published the three audited 18.0.0 archives after short Core, ForgeWeave, Desktop, WPF, localization, and package checks. No campaigns, battles, endurance runs, or long performance samples were executed.

## 17.0.0 — 2026-09-22 — Tactical MVVM War Table

- Replaced arbitrary header trim and corner strokes with embedded Lucide-derived map, compass, swords, flag, target, pin, recent, and shield marks. The semantic geometry is bounded to its section and keeps the header readable at 100–200% scaling; attribution remains in `THIRD_PARTY_NOTICES.md`.
- Added reversible Desktop themes: War Table (default), Parchment Light, and High Contrast. Theme and language preferences are stored atomically under `%LocalAppData%\CalradiaForge`; malformed preferences fall back safely without affecting game saves.
- Added localized theme names, contrast descriptions, failure states, and selector help across all 13 Desktop dictionaries. Theme swaps preserve the active page, command state, recent tools, favorites, and retained evidence.
- Expanded dependency-free Lucide-derived vector marks for the theme, connection, evidence, warning, pin, and recent states; attribution remains in `THIRD_PARTY_NOTICES.md`.
- Extended WPF rendering to 20 representative routes across 3 themes and 4 scale factors while retaining the complete 190-route War Table pass.
- Reworked Desktop into a dependency-free MVVM shell with routed pages, grouped rail, favorites, recent tools, command palette, session strip, command deck, and bounded evidence ledger.
- Added keyboard routing for `Ctrl+K`, `Ctrl+F`, `Ctrl+1…9`, `Ctrl+Enter`, and `Esc`; inactive pages are disposed while evidence DTOs remain retained by the shell.
- Rebuilt the WPF hierarchy around five contained zones and dynamic tactical tokens. The centered seal and evidence rules never overlap technical text or controls.
- Extended all 13 language resource catalogs with workbench and palette keys; identifiers, paths, generated code, and raw evidence remain untranslated.
- Bounded Pinned and Recent with independent auto-scrolling viewports, visible counts, empty states, keyboard focus treatment, and a 12-entry Recent limit so the operational rail cannot grow indefinitely.
- Added dependency-free Lucide-derived vector marks for recent history and recorded the ISC attribution in `THIRD_PARTY_NOTICES.md`.
- Updated release metadata, Steam preflight defaults, documentation, native assets, tests, and the three audited 17.0.0 archives. Validation remains short and excludes campaigns, battles, endurance runs, and long performance samples.

## 14.4.1 — 2026-09-22 — Desktop Startup Fix and Tactical Evidence Focus

- Fixed the startup XamlParseException: the raw-result TextBox now binds one-way to its read-only ViewModel property. Evidence remains selectable and receives source updates.
- Added actual WPF window/template rendering to short tests and the packaging gate: all 190 routes plus 52 language/scale cases. This catches binding failures that source inspections missed.
- Added four native tactical briefing cards for context, testing state, retained evidence and registered tests. These values describe the current Forge session, not global game health.
- Added Focus evidence / Show tools: enlarge the evidence viewport and text while preserving the selected section, page and argument. Added an evidence heading and page indicator; replaced random header strokes with a single title rule.
- Restored visible Framework and Extensions navigation and immediate argument updates while typing. New focus controls are translated into all 13 native languages.
- Preserved the separate three-archive distribution and Desktop batch launcher. The workspace launcher now uses the published Desktop folder instead of a stale Debug build.

## 14.4.0 — 2026-09-21 — Dynamic Desktop Resources and Session Transport

- Regenerated all 13 Forge-owned WPF language dictionaries from the authoritative English key set. Active-language changes replace only the Forge dictionary; raw evidence, paths, and code identifiers are untouched.
- Moved remaining tactical colors and control states behind dynamic palette resources, and replaced template headers with parameterized resource strings.
- Added cancelable measured named-pipe connect/reconnect transport and expanded short validation to resource parity and the 13-language × four-scale structural matrix.

## 14.3.0 — 2026-09-21 — Routed Desktop Workbench

- Replaced the active desktop's live-TreeView discovery and visibility cascade with 190 reviewed tool definitions, routed transient pages, cancellation-aware commands, and WPF ContentControl data templates.
- Moved analysis, generation, live-session queries, short measurements, navigation state, and report export behind control-free desktop services.
- Marked generated output as editable templates and made unsupported, disconnected, state-changing, and absent-input routes report explicit limits instead of a fabricated success.
- Rebuilt the desktop surface as a clean tactical workbench with one centered command seal and no decorative rules crossing controls or text.

## 14.1.0 — 2026-09-21 — Internal MVVM Command Shell

- Added dependency-free `ObservableObject`, synchronous commands, cancellation-aware asynchronous commands, visible command state, and a single desktop tool catalog.
- Routed every live navigation identifier through the catalog and selected-work-order command path. The active work order is loaded by a WPF `ContentControl` data template while compatible verified tool implementations continue on their established route.
- Preserved ForgeWeave's bounded 128-record event buffer, 16-event dispatch budget, replay protection, writer gates, and failure isolation.

## 14.0.0 — 2026-09-21 — Reliability and Verified Tooling

- Consolidated the release value across assemblies, manifests, desktop chrome, reports, documentation, tests, and archive names.
- Added the shared analyzer contract, bounded SDK `IForgeAnalyzer` registration, source/line evidence, provenance, and explicit `NotRun` and `Unsupported` outcomes.
- Replaced visible Desktop audit actions with local file analyzers for modules, XML, localization, Gauntlet, C# rules, assets, watchdog logs, crash metadata, and archives.
- Replaced silent runtime-service fallbacks with capability reporting and actionable unavailable errors.
- Refreshed the tactical workbench and native header with a centered command seal and bounded section rules; removed title-crossing diamond and arrow ornaments.
- Added catalog-driven desktop resolution for all thirteen Bannerlord languages and regenerated UTF-8 BOM native resources with stable IDs.
- Reworked packaging into Modules, Source-SDK, and Desktop archives only. Desktop publishes without an app-host EXE.
- Added a read-only Bannerlord preflight and Steam launch path; no Steam, BLSE, launcher configuration, or save data is rewritten.
- Added fixture coverage for positive, malformed, missing, unsupported, unsafe, capability, analyzer-registration, and archive cases.

## 12.4.0 — 2026-09-20 — In-Game Gauntlet UI Simulation, Multi-Rule Compliance Auditor & Diagnostics Suite

### In-Game Gauntlet UI Architecture (`CalradiaForge.xml` & `PanelViewModel.cs`)
1. **Category Switcher 3-Row Architecture**:
   - Expanded the in-game category switcher to 3 rows, introducing dedicated **`SIMULATE`** (Category 5) and **`AUDIT`** (Category 6) tabs alongside `OVERVIEW`, `INSPECT`, `TOOLKIT`, and `WEAVE`.
   - Adjusted `ScrollablePanel` `MarginTop="180"` to guarantee absolute zero visual overlap between category tabs and content items on all standard resolutions.
2. **In-Game Simulation Lab**:
   - **`ForgeSimDiplomacy`**: Evaluates kingdom war declaration scores, daily peace tributes, and diplomatic stability indices (dual-mode: inspects live `Kingdom.All` when in campaign, benchmarks Calradia factions offline).
   - **`ForgeSimSettlements`**: Evaluates settlement loyalty drift, security equilibrium, garrison-to-militia balance, and rebellion risk indices.
   - **`ForgeSimEconomy`**: Calculates dynamic market supply/demand elasticity pricing, workshop daily net yields, and underworld alley racket margins.
   - **`ForgeSimTactics`**: Calculates cavalry kinetic charge energy thresholds, braced pike resistances, morale shock pipelines, and artillery siege wall breach chances.
   - **`ForgeSimProgression`**: Computes character development learning rate decay curves, clan tier renown milestones, and companion perk-role suitability.
3. **In-Game 36-Rule Compliance Auditor**:
   - **`ForgeRuleAuditor`**: Live in-game audit of installed modules against all 36 rules and best practices using `ModRuleAuditor` (detects `.Campaign` namespace collisions, `SaveableTypeDefiner` base IDs $< 2{,}500{,}000$, missing double `SetDialogs()` in `QuestBase`, un-guarded `OnInit` mesh operations, invalid sound mixer categories, non-integer troop ages, and unauthorized scripts).
   - **`ForgeModelAudit`**: Audits registered GameModels and decorator inheritance chains.
   - **`ForgeDumpDiagnostics`**: Live memory health and system performance dump.
4. **Action Bar Quick-Run Controls & Action Parity**:
   - Pinned contextual quick-run buttons `ExecuteRunSim` (visible when `ShowSimulateActions`) and `ExecuteRunAudit` (visible when `ShowAuditActions`).
   - Synchronized action parity across `ArgumentSuggestions`, `SectionHelpLabel`, `CurrentName`, `NavigationLabel`, and keyboard navigation hotkeys (`ExecuteKeyboardControl`).
5. **Runtime IPC Extension (`Runtime.cs`)**:
   - Added `rule-auditor` IPC action handler returning JSON module audit results to external tools and the Desktop companion.

### Automated Testing & Packaging
6. **Task 285 Unit Test Suite Addition**: Added Task 285 to `AdvancedToolsTests.cs` validating all in-game simulation & audit systems, XML bindings, action bar buttons, and ViewModel properties.
7. **Complete Test Pass Rate**: 100% test pass rate across all suites: **203 tests passing, 0 failures** (177 in `CalradiaForge.Tests` + 26 in `CalradiaForge.Desktop.Tests`).
8. **Distribution Packaging**: Generated v12.4.0 release archives via `tools/package.ps1 -Version "12.4.0"`:
   - `artifacts/CalradiaForge-12.4.0.zip`
   - `artifacts/CalradiaForge-Modules-12.4.0.zip`
   - `artifacts/CalradiaForge-Desktop-12.4.0.zip`



### Core & Framework Architectural Enhancements
1. **Anti-Shadowing Invariant Resolution (`GEMINI.md`)**:
   - Renamed `src/CalradiaForge.Core/SDK/Campaign/` to `src/CalradiaForge.Core/SDK/CampaignMechanics/` and changed namespace to `CalradiaForge.Core.SDK.CampaignMechanics`, eliminating any potential compiler collision with `TaleWorlds.CampaignSystem.Campaign`.
2. **Complete Gameplay Simulation Modules (`CalradiaForge.Core.SDK`)**:
   - `CampaignMechanics`: Implemented real simulation logic across `ForgeQuestManager`, `ForgeWeatherController`, `ForgeTimeManipulator`, `ForgeLoyaltyModifier`, `ForgeSecurityModifier`, `ForgeWoundRateController`, `ForgePlagueSimulator`, `ForgeBountyHunting`, `ForgeTournamentGenerator`, and `ForgeHuntingSystem`.
   - `Combat`: Implemented tactical formation calculations, siege wall damage formulas, damage type armor penetration (Cut/Pierce/Blunt soak), weapon breakage, morale shock, cavalry kinetic charge energy, pikeman brace damage, and line-of-sight obstruction checks.
   - `Economy`: Implemented trade profit calculation, price factor elasticity curves, caravan route scoring, workshop net income, market equilibrium convergence, smuggling margins, and tax policy revenue.
   - `Politics`: Implemented ruler authority metrics, clan party and companion caps, rebellion trigger checks, ruler veto costs, marriage suitability scoring, heir designation ratings, and casus belli weight models.
   - `UI`: Implemented floating damage formatting & color codes, crosshair spread, radar blip conversion, health bar gradient logic, combat compass bearings, advanced killfeed strings, multi-column inventory sorting, troop tree depth calculations, and trade margin badge styling.
3. **Comprehensive Rule Compliance Auditor (`ModRuleAuditor`)**:
   - Scans any mod directory against all 36 rules and best practices: detects `.Campaign` namespace shadowing, `SaveableTypeDefiner` base IDs $< 2{,}500{,}000$, missing double `SetDialogs()` in `QuestBase`, un-guarded `OnInit` mesh operations, invalid sound mixer categories, non-integer troop ages, and unauthorized scripts in distribution packages.

### High-Level Modder SDK (`CalradiaForge.Sdk`)
4. **`ForgeDiplomacy`**: AI war declaration score calculations, daily peace tribute & reparation algorithms, alliance stability metrics, and kingdom election outcome simulations.
5. **`ForgeSettlementSystem`**: Settlement daily loyalty and security equilibrium calculators, food shortage starvation modeling, and critical rebellion danger indices.
6. **`ForgeUnderworldSystem`**: Alley racket yields, rogue smuggling net profit calculations, and crime rating suppression decay.
7. **`ForgeProgressionSystem`**: Bannerlord `CharacterDevelopmentModel` learning rate formula with attribute/focus scaling, Clan Tier 0–6 renown thresholds, dynastic heir succession ratings, and companion perk role evaluation.
8. **`ForgeCombatTactics`**: Casualty and commander death morale shock math, cavalry AI charge distance thresholds, and artillery siege wall breach probabilities.
9. **`ForgeTradeSystem`**: Strict `ItemRoster` underflow safety guards, supply/demand price elasticity curves, and workshop net profit calculations.
10. **`ForgePartySpawner`**: Procedural mobile party blueprints with safe troop roster declarations, wage budgets, and AI behavior assignments (`Patrol`, `DefendSettlement`, `Raid`, `Flee`).
11. **`ForgeHudProjector`**: 3D-to-2D screen coordinate projection for in-mission Gauntlet HUD overlays and map track decay calculations.
12. **Unified `ForgeApi` Surface**: Exposed all new systems cleanly under `ForgeApi.Diplomacy`, `ForgeApi.Settlements`, `ForgeApi.Underworld`, `ForgeApi.Progression`, `ForgeApi.Combat`, `ForgeApi.Trade`, `ForgeApi.Parties`, and `ForgeApi.Hud`.

### In-Game Framework Developer Console (`CalradiaForge.Mod`)
13. **New Console Commands**:
    - `cf.diplomacy.calc_war_score`: Evaluates AI war declaration viability.
    - `cf.diplomacy.calc_peace_tribute`: Calculates daily peace tribute from casualties and conquests.
    - `cf.settlement.audit_rebellion`: Evaluates settlement loyalty, security, and rebellion danger.
    - `cf.underworld.calc_alley`: Simulates alley racket daily gold and crime footprint.
    - `cf.character.calc_learning_rate`: Evaluates skill progression multiplier.
    - `cf.combat.calc_morale_shock`: Calculates formation morale loss from casualties and loss of commander.
    - `cf.siege.breach_chance`: Computes siege wall breach probability from artillery barrage.
    - `cf.trade.price`: Calculates dynamic market prices based on local supply and demand.
    - `cf.trade.verify_transfer`: Validates item roster transfers against underflow.
    - `cf.rules.audit`: In-game audit of any active module against the 36 modding rules.

### Testing & Packaging
14. **Expanded Unit Test Suite**: Added Tasks 275–284 in `AdvancedToolsTests.cs` bringing total test suite pass count to **200 tests passing, 0 failures** (176 in `CalradiaForge.Tests` + 24 in `CalradiaForge.ForgeWeave.Tests`).
15. **Distribution Packaging**: Generated v12.3.0 release packages (`CalradiaForge-12.3.0.zip`, `CalradiaForge-Modules-12.3.0.zip`, `CalradiaForge-Desktop-12.3.0.zip`) via `tools/package.ps1 -Version "12.3.0"`.

## 12.2.0 — 2026-09-19 — Zero-Scroll Desktop Navigation & Workflow Enhancement Release

### Zero-Scroll Desktop Companion Architecture (WPF)
1. **Stationary Workspace Frame (`WorkspaceScrollViewer`)**: Set `VerticalScrollBarVisibility="Disabled"` on the application workspace viewport, preventing unintentional full-page scroll jumps and maintaining solid tactical window alignment across all resolutions.
2. **Auto-Collapsing Accordion Navigation (`NavTree_ItemExpanded` & `AccordionToggleButton`)**: Implemented intelligent accordion mode where expanding any category automatically collapses inactive sibling categories, shrinking the sidebar tree height from >1000px down to ~350–450px so users never have to scroll through 150+ items. Added `⇕ Accordion: ON/OFF` toggle button in the workbench header.
3. **Category Picker Dropdown Filter (`CategoryPickerComboBox`)**: Added instant 1-click category dropdown filtering with 16 dedicated category scopes (`📂 All Categories`, `🛡️ Diagnostics & Safety`, `⚡ Live Inspection & Memory`, `🎨 Asset & XML Synthesizers`, `⚖ Simulation & Balance`, `📦 Mod Tools & Deployment`, and all SDK domains) to isolate exactly the category in use and hide all others.
4. **Dynamic Breadcrumb Navigation (`BreadcrumbHomeBtn`, `BreadcrumbCategory`, `BreadcrumbItem`)**: Added an interactive breadcrumb trail at the top of the workspace frame providing instantaneous 1-click jumps back to the Command Deck (`🏠 Command Deck`) or active category contexts.
5. **Recent Tools Quick-Switcher (`RecentToolsPanel`)**: Implemented an automated MRU tool history queue rendering fast-jump chips for the last 6 accessed tools and SDK generators.
6. **Zen Mode / Maximized Focus Deck (`ZenModeToggleButton` & `F10` Hotkey)**: Added 1-key collapsible workbench sidebar (`F10` / `◀` toggle) that collapses the sidebar to 0px width, expanding the editor or inspection workspace to 100% window width, accompanied by an instant `☰ Sidebar` restore button.
7. **Compacted Tactical Command Deck (`DashboardWelcomePanel`)**: Compacted card paddings, heading spacing, and outer layout margins from ~680px down to ~398px, eliminating the vertical scrollbar completely on standard 720p, 768p, 900p, and 1080p displays.
8. **C# SDK Boilerplate Safety Toggles**: Integrated `ScaffoldSaveableCheckbox` (injecting TaleWorlds `SaveableTypeDefiner` with base ID $\ge 2{,}500{,}000$) and `ScaffoldInitGuardCheckbox` (injecting `OnInit` deferred initialization guards) directly into the SDK Builder action toolbar.
9. **Multilingual Navigation Localizations**: Fully localized all new zero-scroll buttons, breadcrumbs, tooltips, and status indicators in English, Spanish, French, and Turkish.

### Automated Testing & Packaging
10. **Task 271 Test Suite Addition**: Added Task 271 to `AdvancedToolsTests.cs` validating `CategoryPickerComboBox`, accordion sibling collapse, breadcrumbs, recent tools panel, and zero-scroll XAML properties (163 tests passing, 0 failures).
11. **Distribution Packaging**: Generated v12.2.0 release archives via `tools\package.ps1 -Version "12.2.0"`.

## 12.1.0 — 2026-09-19 — Desktop UI Simplification & Gauntlet Polish Release

### Desktop Companion UI Simplification (WPF)
1. **Tactical Command Deck (`DashboardWelcomePanel`)**: Added an interactive centralized dashboard displayed on startup or via the new `🏠 DASH` navigation filter button. Eliminates empty workspace views and provides instant access to core workflows.
2. **Interactive Quick-Action Cards**: Four high-contrast tactical cards directly linked to primary tools:
   - 🛡️ **SubModule.xml Validator**: Fast manifest syntax & dependency integrity audits.
   - ⚡ **Live Console & Log Stream**: Real-time IPC console and unhandled exception triage.
   - 🛠️ **C# SDK Code Scaffolder**: Crash-safe boilerplate generator for Quests, Behaviors, and Models.
   - 🎨 **Gauntlet UI & Brush Studio**: XML widget layout synthesizer and live inspection.
3. **Command Deck Filter Chip**: Added `🏠 DASH` filter button to the navigation chip row with instant view switching.
4. **Multilingual Dashboard Support**: Fully localized all Command Deck titles, subtitles, card descriptions, buttons, and shortcuts in English, Spanish, French, and Turkish.

### In-Game Gauntlet UI Enhancements
5. **Live Memory & Heap Telemetry Badge**: Added dynamic `@MemoryHealthText` telemetry (`HEAP: ~X MB | CTX: ...`) in the header deck to monitor memory usage and GC health in real time.
6. **Tactical Motif Dividers**: Injected Bannerlord's native `Sprite="Divider\horizontal_line"` below title banners and section headers in adherence to `calradia_forge_ui.md`.
7. **Empty State Protection**: Implemented `@ContentPlaceholder` with `IsVisible="@IsContentEmpty"` to prevent orphaned empty layout boxes in the terminal output viewport.
8. **Instant Clear Quick-Action**: Added `[Clear]` button (`ExecuteClearOutput`) with native `Hint.HintText` tooltip and mapped `ForgeClear` to the in-game keyboard controller.

### Automated Testing & Packaging
9. **Task 269 Test Suite Addition**: Added Task 269 to `AdvancedToolsTests.cs` validating `PanelViewModel` telemetry properties, empty-state detection, output clearing logic, and `CalradiaForge.xml` prefab compliance (161 tests passing, 0 failures).
10. **Distribution Packaging**: Built full solution in Release configuration and packaged modular zip distribution archives via `tools\package.ps1 -Version "12.1.0"`.

## 12.0.0 — 2026-09-19 — Major Architecture, UI Streamlining & Lifecycle Safety Release

### Save System & Mission Lifecycle Safety
1. **ForgeSaveChunker (`CalradiaForge.Sdk`)**: Prevents save game corruption caused by TaleWorlds' ~31 KB binary serializer limit (`short.MaxValue - 1024`). Partitions large string/JSON payloads exceeding 30,000 characters into safe `string[]` arrays and reassembles them upon loading.
2. **ForgeMissionLogicBuilder (`CalradiaForge.Sdk`)**: Scaffolder for crash-safe `MissionLogic` implementations. Strictly defers mesh, skeleton, and physics manipulations away from `OnInit()` to `OnMissionTick()` via `_isInitialized` guard (preventing C++ engine crashes), while handling unanimity (`IsAgentInteractionAllowed`) and first-wins (`MissionEnded`) hooks.
3. **Reversible Method Detours (`ForgeDetour`)**: Added byte backup storage, `Unpatch(MethodInfo original)`, `UnpatchAll()`, and `IsPatched(MethodInfo original)`. Restores original instruction bytes upon unpatching to prevent memory corruption.
4. **Gauntlet UI Memory Leak Prevention (`ForgeUI.Clear`)**: Added `ForgeUI.Clear()` to purge registered XML patches, widget factories, and floating panel event subscribers.
5. **SubModule Unload Lifecycle (`SubModule.cs`)**: Integrated `ForgeAgentMemory.ClearAll()`, `ForgeUI.Clear()`, and `ForgeDetour.UnpatchAll()` into `OnSubModuleUnloaded()` to ensure zero lingering state across game sessions.

### In-Game Gauntlet UI Enhancements
6. **Dynamic Version Binding**: Replaced hardcoded version text with dynamic `@VersionLabel` (`v12.0.0`) in `CalradiaForge.xml` and `PanelViewModel.cs`.
7. **Tactical Status LED**: Added `@SessionStatusColor` indicator showing active engine hook and IPC connection status.
8. **Empty State Visibility**: Added `IsVisible="@IsContextVisible"` to context widget container, preventing orphaned empty borders when no context is active.
9. **Action Parity**: Registered `project-wizard` in `CurrentName`, `ArgumentSuggestions`, `SectionHelpLabel`, and `NavigationLabel`.

### Desktop Companion UI Streamlining (WPF)
10. **Structured Tool Organization**: Reorganized 46 flat Live Tools into 5 clean, expandable subcategories: *Diagnostics & Crashes*, *Saves & Heap Inspection*, *Synthesizers & Visualizers*, *Economy & Combat Balance*, and *Packaging & Deployment*.
11. **Compact Tactical Deck**: Streamlined window header to a compact 44px tactical command deck, maximizing vertical workbench real estate.
12. **Recursive Search & Language-Agnostic Filtering**: Upgraded search filtering to recursively traverse tree levels and match tags; updated filter chips to use object identity rather than localized strings.
13. **Multilingual Subcategory Support**: Added full English, Spanish, French, and Turkish translations for the new subcategories and items.

### Automated Testing & Packaging
14. **Test Suite Expansion**: Added Tasks 265–268 to `AdvancedToolsTests.cs` covering save chunking, mission logic scaffolding, detour restoration, and UI registry purging (160 passing tests, 0 failures).
15. **Distribution Packages**: Generated v12.0.0 distribution archives (`CalradiaForge-12.0.0.zip`, `CalradiaForge-Modules-12.0.0.zip`, `CalradiaForge-Desktop-12.0.0.zip`).

## 11.0.0 — 2026-09-19 — Major Engine Architecture & Memory Safety Release

### Memory Safety & Lifecycle Cleanup (memory-leak-debugging)
1. **ForgeCampaignEvents Event Detachment**: Added `Unsubscribe(ForgeEvent eventType, Action<object[]> callback)` and `ClearSubscribers()`. Prevents static event delegate leaks across campaign sessions and scene transitions.
2. **CampaignVariableInspector Unload Safety**: Added `ClearTrackedVariables()` and `ClearSnapshotListeners()` to safely detach all live variable watchers when a campaign session concludes or unloads.
3. **ForgeData Explicit Purge & Has Checks**: Added `RemoveForgeData<T>(entity)`, `RemoveForgeData(entity)`, and `HasForgeData<T>(entity)` to avoid holding references to despawned heroes, dead agents, or disbanded parties.

### Fluent Domain Builders (SDK)
4. **ForgeTroopBuilder (`CalradiaForge.Sdk`)**: Fluent builder for `NPCCharacters.xml` strictly enforcing integer age constraint (prevents TaleWorlds XML parser floating-point crash), valid equipment slots (`Item0`-`Item3`, `Head`, `Cape`, `Body`, `Gloves`, `Leg`), civilian loadout isolation, and automatic `NPCCharacter.` prefixing on upgrade targets.
5. **ForgeItemBuilder (`CalradiaForge.Sdk`)**: Fluent builder for `Items.xml` weapon, armor, and mount definitions enforcing engine damage types (`Cut`, `Pierce`, `Blunt`), component structures (`<Weapon>`, `<Armor>`, `<Horse>`), and crafting template piece linkages.
6. **ForgeAudioBuilder (`CalradiaForge.Sdk`)**: Fluent builder for `module_sounds.xml` validating engine audio categories (`ui`, `mission_combat`, `ambient`, `voice`) and compression formats (`.ogg`, `.wav`).
7. **ForgeQuestBuilder (`CalradiaForge.Sdk`)**: Scaffolder for `QuestBase` and dialogue flows enforcing the TaleWorlds Double `SetDialogs()` rule (constructor + `InitializeQuestOnGameLoad`) and allocating safe `SaveableTypeDefiner` IDs ($\ge 2{,}500{,}000$).

### Desktop Companion Enhancements (WPF)
8. **Troop & NPC Character Synthesizer (`TroopXmlSynthesizer`)**: Added workbench tool with preset generation for Veteran Sergeants and Skirmishers, alongside engine-rule XML verification.
9. **Item & Smithing Crafting Synthesizer (`ItemXmlSynthesizer`)**: Added workbench tool supporting one-click synthesis for weapons, armors, mounts, and modular crafting templates.

### Automated Testing
10. **AdvancedToolsTests Suite Expansion**: Added Tasks 260 through 264 covering all new builders and memory leak guards with 100% pass rate.

## 4.1.0 — 2026-09-18 — Robust Skills Integration

### Bug Fixes
1. **ForgeCampaignEvents silent errors (critical)**: Dispatch callbacks that threw exceptions were silently swallowed, making it impossible for modders to diagnose why their event handlers failed. Errors are now routed through `ForgeApi.Logger.LogError` and appear in the Calradia Forge log view.

### SDK Improvements (agent-memory-systems)
2. **ForgeAgentMemory — TTL-based memory decay**: `Semantic.Upsert` now accepts an optional `TimeSpan? ttl`. Expired entries are removed transparently on the next `Get<T>` or `GetKeys` call, preventing unbounded memory growth across long campaigns.
3. **ForgeAgentMemory — `Semantic.GetKeys(agentId)`**: Returns all live (non-expired) fact keys for a given NPC/hero, enabling modders to enumerate what an agent knows without reflection.
4. **ForgeAgentMemory — `Episodic.Count(agentId, type)`**: Returns the number of recorded episodes of a specific type (e.g. battles, treaties) for an agent.
5. **ForgeAgentMemory — `Episodic.TotalCount(agentId)`**: Returns the total episode count across all types for an agent.
6. **ForgeAgentMemory — `Procedural.GetTaskNames(agentId)`**: Returns all stored task/skill names for an agent.

### API Builder (api-builder)
7. **ForgeLocalApi — `/info` endpoint**: Returns `{"version":"4.1.0","sdk":5,"status":"running"}` for external tool integration.
8. **ForgeLocalApi — `/agents` endpoint**: Returns agent-memory statistics in JSON (safe, no payload data exposed).
9. **ForgeLocalApi — CORS headers**: `Access-Control-Allow-Origin: *` on every response to support local web dashboards.
10. **ForgeLocalApi — `KeepAlive=false`**: Prevents socket-held connections from leaking on rapid polling (memory-leak-debugging).
11. **ForgeLocalApi — Per-request ThreadPool dispatch**: Each HTTP request is handled on a ThreadPool thread, unblocking the accept loop and improving concurrency.
12. **ForgeLocalApi — `ObjectDisposedException` guard**: Double-`Start` or `Start` after `Dispose` is now caught cleanly.

### Version & Protocol
13. **ForgeApi.Version bumped to 5**: Signals the new TTL memory, extended API, and error logging capabilities to extension authors.
14. **SuiteInfo.Version → 4.1.0**: All 4 SubModule XML manifests, `Directory.Build.props`, and `SuiteInfo.cs` updated.

### Tests (Superpowers TDD)
15. **6 new automated test cases** added covering: TTL expiry, `GetKeys`, `Episodic.Count`, `/info`+`/agents` HTTP endpoints with CORS verification, `ForgeCampaignEvents` error logging, and `ForgeApi.Version` value assertion. All pass (≥137 total).

## 0.7.6 — 2026-09-16 — Exact command-disc intersection

- Centre the command disc exactly at the 34/34 intersection of the diamond's horizontal and vertical tactical lines.
- Expand the desktop regression suite from 10 to 24 short cases across transport, reconnect, error handling, localization, scaling, evidence surfaces and Replay Lab selection; keep the smoke matrix across all supported Bannerlord languages and both 100% and 200% text scales.

## 0.7.5 — 2026-09-16 — Optical command-disc correction

- Move the desktop command disc half a design unit left and down after shield-stroke rounding, correcting the observed high-DPI optical drift while retaining the shared tactical crosshair.
- Re-run the bounded ForgeWeave, Core/SDK, desktop, generated-asset and release-package checks; no endurance or prolonged in-game sampling is included.

## 0.7.4 — 2026-09-16 — High-scale tactical navigation

- Make the desktop navigation identity use the same compact chrome scale as the header, so its section label and formation rail expand and reflow with the 200% text-size option.
- Refine the maker mark's command disc to a larger ten-unit disk centred on the shared 34/34 diamond crosshair, preserving a clear optical centre at high DPI.
- Keep the formation rail inside the scaled navigation column and preserve its non-interactive, decorative role.

## 0.7.3 — 2026-09-16 — Tactical identity alignment

- Rebuild the desktop maker mark on one 68-unit vector canvas: the command diamond, crosshair and brass focal point now share the exact center, while the CF ribbon remains in its own lower field.
- Scale the complete identity block with the existing compact chrome scale at 200%, including a matching header height so the seal stays aligned with the title and never crowds the work area.
- Replace the former diagonal battle line with a bounded deployment rail: separated line segments, three formation markers and end caps read as a tactical map notation without crossing the identity, title or status plate.
- Replace the sidebar endpoint decoration with an in-bounds formation rail whose markers and caps remain fully visible at 100% and 200%.

## 0.7.2 — 2026-09-16 — Tactical war-table interface refresh

- Replace the crowded CF diamond with a centered heraldic shield, command diamond, brass focal point and separate CF ribbon.
- Rebuild the header decoration as one restrained battle-line motif that stays behind the chrome, and align the navigation rail's endpoint diamonds to its rule.
- Preserve keyboard focus, language switching, 200% scaling and the existing coal, pine, verdigris and brass design tokens.

## 0.7.1 — 2026-09-16 — Desktop Bannerlord language catalogs

- Expand the desktop language picker to the 13 languages present in the installed Bannerlord language packs: English, Latin American Spanish, Brazilian Portuguese, German, French, Italian, Polish, Russian, Turkish, Simplified Chinese, Traditional Chinese, Japanese and Korean.
- Embed generated desktop catalogs in Core, reusing the reviewed native menu translations and keeping English as the explicit fallback for technical diagnostic copy without a reviewed wording.
- Add catalog coverage checks for every supported language and preserve the English-first source workflow, scalable tactical layout and separate desktop package.

## 0.7.0 — 2026-09-16 — ForgeWeave event filters and SDK v3

1. Add `ForgeEventFilter.RequiredData`, a bounded declarative exact-match filter over copied scalar host data. Empty filters match all events; non-matches are recorded as `Filtered` and never invoke or authorize the handler.
2. Apply the same filter to live dispatch and Replay Lab, copy declarations at registration, expose filters in Framework health, native output and HTML reports, and keep the filter limits at eight pairs, 64-character keys and 512-character values.
3. Add opt-in per-handler execution budgets from 1 to 5,000 ms. `Warn` records `OverBudget` evidence without aborting or quarantining extension code; `Ignore` leaves timing visible without a budget finding.
4. Add focused SDK/Core tests for immutable registration copies, matching and non-matching events, replay filtering, budget overruns and malformed or oversized declarations.
5. Bump the public SDK contract to v3 and the developer preview to 0.7.0. Update the built-in example, manifests, documentation and separate release archives.

## 0.6.5 — 2026-09-16 — clean interface decoration

1. Replace the native header's scattered decorative strokes with a fixed four-zone composition: Forge seal, identity, current section/context plaque and build stamp.
2. Recenter the CF seal on a shared monogram axis, with a measured inset, glyph, top rule and brass underline for a clean small-size mark.
3. Add aligned brass and verdigris rules, inset plates and restrained spacing while keeping the eight-section rail and the Close control unchanged.
4. Keep every decorative widget passive and bounded so no line crosses the logo, text, version stamp or button hit area.
5. Synchronize manifests, assemblies, generated native assets, release metadata, documentation and the three separate 0.6.5 archives.

## 0.6.0 — 2026-09-15 — ForgeWeave Replay Lab and tactical workbench

1. Add Replay Lab, a controlled event-verification facility for ForgeWeave. Forge retains bounded copied host-event evidence and can replay one selected source sequence only; it never accepts an injected payload, records a replay as a new source or retries a rejected replay automatically.
2. Add explicit `ForgeReplayMode` opt-in for handlers, `ForgeEvent.IsReplay` and `SourceSequence`, and the additive `ForgeApi.Replays` registry. `Disabled` remains the default; exact current-context matching is mandatory and writer handlers keep their existing descriptor, test-mode and copied-campaign gates.
3. Add the `replay` named-pipe action, replay status and rejection reasons, copied handler outcomes, timings, retained source records and recent outcomes to the Framework snapshot, event journal, JSON report and HTML report.
4. Make the native Framework workbench accept a retained sequence in its existing argument field, and let the desktop Framework table replay its selected evidence row.
5. Refresh both interfaces as a tactical war table: eight native sections remain intact while Framework becomes a first-class workbench; work-order strips, command decks and ledger rows use the coal, pine, tempered-green, brass, verdigris and ember system. The native header now has one Forge seal, an aligned work-order/context plaque and a separate build stamp; scattered pseudo-random strokes are removed.
6. Keep ForgeWeave independent of Harmony, MCM, ButterLib and external assets. Replay Lab is not method interception and does not claim universal replacement for a patch framework.
7. Add focused replay, reporting, protocol and desktop-selection regressions, then run only the short build suite and a brief main-menu session. Campaign, battle, state-changing and endurance validation remain outside this release.

## 0.5.0 — 2026-09-15 — ForgeWeave cooperative extension framework

1. Add ForgeWeave, Calradia Forge's original standalone framework for cooperative extensions. It uses only lifecycle callbacks owned by Forge and does not discover methods, emit IL, replace callbacks or load a third-party mod framework.
2. Give authors explicit `ForgeReady`, screen, context, campaign, mission, agent and bounded `Pulse` events with copied scalar data rather than live Bannerlord objects or Forge test services.
3. Make handler execution deterministic and inspectable: priority plus same-event/same-priority `Before` and `After` constraints form an explicit plan. Missing, cross-event, cross-priority, self-referential and cyclic declarations are blocked with findings instead of guessed ordering.
4. Reuse Forge's existing context, test-mode and copied-campaign gates for write-capable handlers. Isolate exceptions per handler, quarantine repeated failures and retain bounded handler timings, event health and dispatch history.
5. Add `framework` and `event-journal` protocol actions, a Framework action in the native panel, a Framework section in the desktop companion, and JSON/HTML report coverage for the same copied ForgeWeave snapshot.
6. Add a read-only `ForgeReady` example handler plus English-first ForgeWeave documentation and Spanish companion documentation for extension authors.
7. Move the optional Harmony Patch Atlas out of the primary native and desktop workflow. It remains an isolated, bounded, read-only historical diagnostic endpoint when another selected module already provides a compatible runtime; ForgeWeave neither uses nor requires it.
8. Keep Patch Blueprint Preflight independent of Harmony observation. It continues to review inert author declarations structurally and never applies, removes or reorders a patch.
9. Add atomic `RegisterWhenAvailable` and `UnregisterWhenAvailable` SDK helpers so module loading cannot miss Forge between a registry check and a later availability subscription.
10. Preserve the logical source context on `ContextLeaving`, allowing eligible read-only cleanup handlers to run against the context they are leaving while retaining Forge's existing write safeguards.

## 0.4.0 — 2026-09-15 — standalone patch diagnostics and interaction refinement

1. Keep Calradia Forge independent: the production mod has no Harmony, MCM, ButterLib or other third-party mod-framework reference, and the package audit rejects those framework DLLs from module and developer archives.
2. Add the optional, read-only Harmony Patch Atlas. When a compatible Harmony runtime is already present, it records bounded original-method identity, owners, Prefix/Postfix/Transpiler/Finalizer records, declared priority, before/after declarations, index and patch-method identity. It reports absent or incompatible Harmony without affecting Forge, never patches or loads it, and labels shared targets as review candidates rather than conflicts.
3. Add the Atlas to the native Modules/Dependencies actions, desktop live workflow, local protocol capability negotiation, offline report workspace and HTML export.
4. Keep healthy SDK registrations after another availability subscriber fails, log the contained startup error once, and prevent that extension failure from closing the Forge UI tick.
5. Complete the pipe inspection helper's declared action surface and strengthen keyboard-selected native button contrast.
6. Add regression coverage for standalone optional diagnostics, unsupported public API shapes, overloaded targets, owners, ordering metadata, filters, bounds, malformed metadata, HTML escaping and extension-startup containment.

## 0.3.0 — 2026-09-14 — interface and release-quality update

1. Rebuild both interfaces as a forge workbench: deep-pine structure, brass hierarchy, parchment desktop data surfaces, an original maker's seal and readable technical typography.
2. Add a persistent desktop section rail, connection badge, contextual action groups, scalable body text, visible keyboard focus, localized accessibility names and a dedicated Calradia Forge Windows icon. Actions scroll separately from results; empty arrays show guidance while preserving the original JSON in Raw data.
3. Replace the native horizontal button banks with an eight-section rail and flat, module-owned button styles. Split Summary into context, testing, registered tests and session details with brief guidance. Add 44-pixel controls, contextual actions, active-section markers, disabled page controls and left-aligned diagnostics without heavy text shadows. The rail is foreground-rendered and decorative containers do not accept clicks. Tab navigation skips hidden and disabled controls, including their parent groups. Start with an empty search field. The installed final rail's Tests click is recorded in the native validation log.
4. Format native test results and known operational messages without translating identifiers, JSON, paths or arbitrary diagnostic payloads; add translated empty-log guidance across the shipped game-language catalogs.
5. Validate localization catalogs, generated translation identifiers, manifests, generated prefab bindings, command contracts and native input-layout invariants before assets are written.
6. Centralize suite and target-game version metadata so reports, the desktop interface, the native runtime and generated manifests stay aligned.
7. Add reproducible original release artwork and a multiresolution desktop icon; clean temporary packaging staging folders after package creation.

## 0.2.0 — developer preview

- Give the native search/argument field a localized label and visible outline; verify click focus, Ctrl+A replacement and log browsing inside the game.
- Add Ctrl+F search focus and Ctrl+Enter refresh, verified in native log filtering. Empty log searches now explain how to clear the filter, with English and Spanish text; the Spanish empty-state text and clearing the filter were verified inside the game.
- Add versioned shared services with explicit provider ownership, interface contracts, thread checks and lifetime-bound handles. Include separate contract, provider and consumer projects and an executable integration example verified inside Bannerlord. Initialize the SDK on the first game update to avoid binding services to the module-loading thread. See SHARED_LIBRARIES.md and VALIDATION.md.
- Serialize session log writes and clear stale persistence errors after recovery. Preserve the previous file on a failed replacement and report the operation, path and HRESULT. Verify native recovery after a controlled file-sharing violation.
- Add native Ctrl+1–8 section shortcuts, Ctrl+Left/Right paging and a localized active-section footer. These shortcuts only browse information while the panel is open.
- Native menu language now follows Bannerlord through TextObject translation resources, without a manual toggle or language whitelist. Navigation catalogs cover the 13 installed game languages; custom language packs can add matching string IDs.
- Add a dedicated desktop connection regression suite using the production PipeClient over local named pipes, including peer restart and protocol rejection.
- Add dependency ordering with explicit blocked modules and version declaration warnings. The suggested order does not change launcher settings.
- Add test batches of up to 50 entries, with preflight validation and stop-on-failure execution.
- Add snapshot browsing and removal to both interfaces.
- Explore imported reports offline and export the current report without replacing it with a connected session.
- Add an explicit live capture action and structured HTML report sections.
- Redesign the desktop interface with a permanent section sidebar, contextual actions, labelled inputs, busy feedback and text scaling.
- Freeze extension metadata at registration, preserve results when logging fails, and fix cancellation ownership and reconnect races.
- Distinguish engine asset XML review from confirmed generic XML defects.
- Keep English as the source/default language and Spanish as a secondary translation.
- Fix native custom-battle troop creation by using BasicBattleAgentOrigin instead of a campaign-only origin. Native execution and subsequent removal from the mission collection were verified.
- Fix Gauntlet button labels intercepting clicks; verify Tests, language switching, export and Close in the installed game.
- Avoid accessing finalized screen/layer resources when shutting down with the native panel open. An RBM/Harmony battle reproduced the failure without running SDK tests; the corrected build passed the same exit path.
- Verify inventory restoration in a copied sandbox campaign and load the saved working copy without Forge. This result applies to the tested save.
- Keep offline folder scans in their own report workspace with the scanned folder as the source.

Build, regression and completed native checks are recorded separately in VALIDATION.md. Remaining snapshot, interaction, compatibility and overhead checks are explicit; this release is not certified for public distribution.

## 0.1.0

Initial developer preview: Gauntlet panel, WPF client, local named pipes, diagnostics, bounded logs, object inspection, test SDK, inventory/troop examples and JSON/HTML export.



## 22.0.0 source follow-up — 2026-09-25 (Rev033)

- Rev033 records the High Contrast adjustment after Rev032. The theme keeps its surface brushes solid and permits passive decorative overlays when decorative accents are enabled; the WPF visibility binding still gates them by the accent setting and selected theme. The source artwork uses low opacity and does not receive hit testing. Rendered legibility across the full layout and scaling matrix has not yet been confirmed; final render and live inspection remain pending. No test or in-game result is claimed by this entry.

## 22.0.0 source follow-up — 2026-09-25 (Rev034)

- The High Contrast render matrix now confirms that solid surface brushes are retained while eligible passive overlays appear with decorative accents enabled. The header heraldic mark is enabled; the primary seal retains the plain `CF` fallback. A 2-DIP title-bar rule keeps the decorative strip from measuring to zero height. The screenshot is from the WPF render harness, not a live-window observation.
- Validation through `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` built with zero warnings/errors, passed Desktop 51/51 and WPF 273/273 cases with 150 representative layout passes. Texture determinism and optimized-package checks passed: 7,278,592 compressed bytes and 15,778,992 decoded bytes.
- Five render BAT runs completed in 5.260, 5.680, 5.162, 5.338 and 5.205 seconds (median 5.260 seconds). This is 7.8% below the complete five-run Rev030 median of 5.703 seconds. The latest fresh pre-change attempt had only four valid runs; their 5.304-second median is reported as a four-run reference, not a complete baseline. The route/navigation/filter phase median fell from 1,133 ms to 810 ms (28.5%); other phase timings varied and are not presented as uniform product latency improvements.
- Live WPF UI Automation/visual inspection remains pending. No game, campaign, battle, ZIP, API or version change is claimed.

## 22.0.0 source follow-up — 2026-09-25 (Rev035)

- Corrected three screenshot-reported WPF layout defects. The title bar no longer layers a second centered heraldic frame over the botanical band, eliminating the doubled compass ornament while keeping the decoration inside the title area and clear of caption buttons.
- The tool-summary illustration now occupies its own reserved 270-DIP right column and uses uniform scaling. It no longer overlays the title, purpose, or availability text. The rail favorite action now has a fixed 28-DIP column with a centered 26-DIP button, keeping the star visible beside wrapped route labels.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` built with zero warnings/errors, passed Desktop 51/51 and WPF render 273/273 with 150 layout passes. The harness-generated High Contrast, War Table, and default previews were visually reviewed; two render runs took 4.804 and 5.200 seconds. These are harness timings, not application interaction latency or live-window validation.
- Live WPF UI Automation remains pending. No game, campaign, battle, ZIP, API, or version change is claimed.

## 22.0.0 source follow-up — 2026-09-25 (Rev036)

- Rebuilt the Desktop tool-card botanical ornament from its complete 3:1 authoring master at 540×180 pixels for its 270×90-DIP slot, supplying native pixels through 200% WPF scaling. The earlier derivative was 270×90 and cropped the master to a mismatched aspect ratio. The card, title-bar botanical band, cartographic board, and rail illustration now request high-quality bitmap sampling; the artwork remains passive and outside text and control surfaces.
- The deterministic texture preparer now rejects aspect-ratio distortion and checks the exact optimized resource inventory. Three no-longer-referenced derivatives were removed from the runtime package while their authoring masters were retained. The optimized set measures 7,202,682 compressed bytes and 15,334,592 decoded RGBA bytes, respectively 75,910 and 444,400 bytes below the previous Rev034 package measurement.
- The localized header subtitle wraps instead of ending in an ellipsis and exposes its full wording in a tooltip. The render matrix checks that it stays within its brand group. `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check` passed; `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` built without warnings or errors and passed Desktop 51/51 and WPF render 273/273 with 150 layout passes. The final harness run took 4,886 ms; that duration is not live application latency.
- Harness previews were reviewed at normal and 200% scales, including War Table and High Contrast. A live WPF window inspection remains pending. No version, ZIP, game resource, API, or Bannerlord session changed.

## 24.0.0 source follow-up — 2026-09-25 (Rev037)

- Advances the public SDK contract to version 8 with managed delegate registration through `ForgeCampaignEvents.SubscribeWeaveWhenAvailable`. Its `ForgeWeaveRegistration` owns the availability callback and active handler, reports `WaitingForHost`, `Registered`, `HostUnsupported`, `Failed` or `Disposed`, withdraws on host disconnect, registers again after reconnection, and stops registering after `Dispose()`. Registration is synchronous on the host connection thread; disposing an active handle off-thread throws and leaves it active for a same-thread retry. Immediate `SubscribeWeave` remains available and throws `InvalidOperationException` when the Forge event host is unavailable or the call is off-thread.
- Bounds `ForgeAgentMemory` to 2,048 distinct agent IDs shared across tiers, 128 semantic facts per agent, 512 episodic entries per agent and 128 per type, and 128 procedural tasks per agent. Episodic overflow evicts oldest entries FIFO; semantic and procedural writes may update existing entries but reject new entries at capacity. TTL is available only for semantic facts. `TryUpsert`/`TryAdd` report capacity rejection; legacy write methods throw `InvalidOperationException` for rejected writes.
- Updates the SDK and ForgeWeave reference pages and the SDK codemap for the v24.0.0 source tree. The existing `CalradiaForge-Modules-23.0.0.zip` and `CalradiaForge-Source-SDK-23.0.0.zip` archives remain unchanged and were not regenerated; no v24.0.0 package contents or release hashes are claimed. No build, test suite or Bannerlord session was run for this documentation and ledger update.

## 24.0.0 source follow-up — 2026-09-25 (Rev038)

- Follow-up validation after Rev037: `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` built the selected `net472` and `net8.0` dependency graphs with zero warnings and errors, then passed Core 275/275 and ForgeWeave 52/52.
- Corrected the Gauntlet prefab binding auditor to resolve `Command.Click` and `@property` bindings inside an `ItemTemplate` against the item ViewModel declared by its parent `ListPanel.DataSource`. This validates `CommandHistoryItemVM.ExecuteLoad` and `ToolItemVM.ExecuteSelect` against their actual owners; no product behavior or public SDK surface changed.
- Updated the current SDK codemap to contract version 8. Bannerlord was not launched, and no ZIPs were generated or changed.

## 24.0.0 source follow-up — 2026-09-25 (Rev039)

- Hardened semantic `ForgeAgentMemory` TTL arithmetic: zero and negative durations expire immediately without timestamp underflow, while very large positive durations saturate at `DateTimeOffset.MaxValue` instead of overflowing.
- Contained file-backed `ModSettings.Register` and `Save` IDs to a single valid file-name component and canonicalized the resulting path beneath the ModSettings directory. Path separators, rooted/drive paths, alternate-stream syntax, invalid file-name characters, control characters and empty IDs are rejected before file access; safe names with spaces and Unicode remain unchanged.
- Made managed ForgeWeave withdrawal failures retryable. `ForgeWeaveRegistration` retains the active registry when host unregistration throws, exposes the failure through `State`/`Error`, and permits a later host transition or same-thread `Dispose()` to retry. Generation checks prevent an older reentrant callback from overwriting state established by a newer reconnect.
- Corrected Core analyzer dispatch so unknown IDs remain `Unsupported` instead of being heuristically routed to an analyzer based on a keyword in the identifier. The archive analyzer now counts only entries actually inspected up to `MaximumFiles` and marks omitted entries as truncated.
- Updated the SDK reference in English and Spanish. The coordinated final `.bat` validation is pending; this entry does not claim post-change automated test success. Product source remains 24.0.0 and SDK contract remains 8; no ZIPs were generated or changed.

## 24.0.0 source follow-up — 2026-09-25 (Rev040)

- Extended the Core audit evidence with bounded directory traversal: at most 10,000 discovered directories including the selected root, depth 64, and 200,000 filesystem entries per scan. `MaximumFiles` defaults to 1,000 and is clamped to 1–2,000. The walker checks cancellation during traversal, skips reparse points at the root and below it, and reports `analysis_scan_incomplete` plus `Truncated` when it reaches a bound or encounters unreadable/skipped paths.
- Added an archive byte-size guard before `ZipArchive` construction and before ZIP directory entries are inspected. An oversized archive is reported as `archive_size_limit` with no entry inspection; accepted archives inspect at most `MaximumFiles` entries and report omitted entries as truncated. This limit is distinct from a claim that the archive format itself has been fully validated.
- Updated the bilingual System Design and Desktop evidence guides. Rev039 remains the record of its original TTL, `ModSettings`, ForgeWeave, and analyzer-dispatch findings; these additional walker and archive-size boundaries were found in the continued Core audit and are recorded here without editing Rev039.
- Final coordinated `.bat` validation remains pending; this entry does not claim post-change automated test success. Product source remains 24.0.0, SDK contract 8, and existing ZIPs were not regenerated or changed.

## 24.0.0 source follow-up — 2026-09-25 (Rev041)

- Closed the SDK/Core hardening round with boundary fixes for semantic TTL arithmetic, path-safe `ModSettings` IDs, retryable/reentrant ForgeWeave unregistration, exact analyzer aliases, bounded directory traversal, and archive-size/entry limits. Public signatures, SDK contract 8, product version 24.0.0, and target frameworks are unchanged.
- Final validation through `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` completed with exit code 0: selected `net472` and `net8.0` builds had zero warnings and errors; Core passed 286/286 and ForgeWeave passed 56/56. The batch launcher hosted the test library in PowerShell; no test executable was started directly.
- Bannerlord was not launched and no live in-game behavior is claimed. Existing ZIPs were not regenerated or modified.

## 24.0.0 source follow-up — 2026-09-25 (Rev042)

- A second review closed three edge cases: all publicly constructible `ForgeAgentMemory` tier objects now share the bounded process-wide store; a reentrant `ForgeWeave.Register` that returns after a newer host connection withdraws its stale handler and retains failed cleanup for a later retry; and bounded text reads enforce the byte cap on the open stream even if the file grows after the initial size check.
- Core archive preflight now reads the bounded end-of-central-directory record before creating `ZipArchive`. Unreadable/multi-disk directories and archives declaring more than 10,000 entries are skipped before entry objects are materialized; accepted archives retain the per-request inspected-entry cap and truncation evidence.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` passed after these follow-up changes: zero build warnings/errors, Core 288/288 and ForgeWeave 58/58. The tests remain launcher-hosted; no test executable was run directly.
- Product source remains 24.0.0 and SDK contract 8. Bannerlord and live-game behavior were not verified; ZIPs were not generated or modified.

## 24.0.0 source follow-up — 2026-09-25 (Rev043)

- Made stale ForgeWeave cleanup iteration safe against nested host callbacks that mutate the pending-unregistration list: cleanup now iterates a snapshot and skips handles already removed by reentrant callbacks. Added a regression with two failed stale registrations and a nested reconnect during retry.
- Final `.bat` validation after this correction passed with zero warnings/errors, Core 289/289, and ForgeWeave 59/59. Source remains 24.0.0, SDK contract 8, and existing ZIPs remain untouched. Bannerlord was not started.

## 24.0.0 source follow-up — 2026-09-26 (Rev044)

- Hardened ForgeWeave against three reentrant lifecycle failures: `Dispose()` now suppresses availability callbacks while unregistering; cleanup errors from stale generations no longer overwrite a newer registration state; and any thrown `IForgeEventRegistry.Register` is treated as potentially partial because the contract does not guarantee rollback and `Unregister(IForgeEventHandler)` removes by ID. A managed handle fails closed after that exception, preserves known newer registrations, and retains the diagnostic through disposal instead of attempting an unsafe ID-based rollback.
- Added regressions for partial registration, failed stale cleanup during nested reconnects, and a host replacement initiated from `Unregister` during disposal. The SDK reference in English and Spanish, SDK codemap, and interface comments document the recovery boundary.
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` completed with exit code 0: selected `net472` and `net8.0` builds reported zero warnings and errors; Core passed 294/294 and ForgeWeave 64/64. The batch launcher hosted the test library; no test executable was started directly.
- Product version remains 24.0.0 and `ForgeApi.Version` remains 8. Bannerlord was not started, live in-game behavior is not claimed, and existing ZIPs were not generated or modified.

## 24.0.0 source follow-up — 2026-09-26 (Rev045)

- Closed a nested-disposal gap in ForgeWeave: when a host cleanup callback disposes the registration while an outer registry transition is still running, the outer callback now rechecks `Disposed` after returning from host code and stops before attempting another registration.
- Added a regression in which disposal occurs during a pending stale-host cleanup and the trigger host is configured to throw after registration; the trigger remains untouched, the active known handler is removed, and the registration remains disposed.
- The final `.bat` run passed with zero warnings/errors, Core 295/295 and ForgeWeave 65/65. Source remains 24.0.0, SDK contract 8, no test executable was run directly, Bannerlord was not started, and ZIPs were not generated or changed.

## 24.0.0 source follow-up — 2026-09-26 (Rev046)

- Closed an unbounded path in the standalone `SubModule.xml` validator: module roots and nested XML scans now cap traversal at 10,000 directories, depth 64, 200,000 filesystem entries, and 10,000 additional XML files. Reparse points are skipped and partial scans report `analysis_scan_incomplete`.
- Replaced recursive nested-tree and dependency-cycle walks with explicit stacks, and restored reporting for declared incompatibilities when the conflicting module is present. Added depth-bound and incompatibility regressions.
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` passed with zero warnings/errors: Core 297/297 and ForgeWeave 65/65. The batch launcher hosted tests; no test executable was run directly. Bannerlord was not started and ZIPs were not generated or changed.

## 24.0.0 source follow-up — 2026-09-26 (Rev047)

- Advances `ForgeApi.Version` from 8 to 9. `ForgeModelRegistry.Evaluate` evaluates modifier conditions from a stable snapshot outside the registry lock; the new disposable `BeginOwnerScope(ownerId)` registration scope removes only its own still-current generations, while legacy `Register` and `Unregister` remain available.
- Fixes `ForgeData.RemoveForgeData<T>` to detach the outer entity entry after its final typed value is removed without dropping concurrent SDK insertions. `GET /agents` now reports real aggregate counts for agents and the three memory tiers while preserving the empty `agents` field and exposing no identifiers, keys, or payloads.
- Public `Core.Helpers` operations that remain unsupported now throw a clear `NotSupportedException` stating that no action was performed, rather than silently succeeding. The bounded `ModRuleAuditor` treats incomplete scans, unreadable inputs, and invalid XML as errors; ignores comments when checking numeric Pulse intervals of at least 50 ms; trusts scripts only under the repository's top-level `tools` directory; and exposes analyzer IDs as an immutable collection.
- Updated the bilingual SDK and shared-service references. The test-only Gauntlet assertion is being aligned with the existing `ForgeArgument` and `ForgeAssemblyPath` IDs; the prefab is unchanged.
- Post-change `.bat` validation is pending and is not claimed here. Product source remains 24.0.0; SDK and Core remain on `net472`/`net8.0` as configured. Existing ZIPs were not regenerated or modified, and Bannerlord was not started; no live engine behavior is claimed.

## 24.0.0 source follow-up — 2026-09-26 (Rev048)

- Records completion of Rev047's test-only Gauntlet assertion update to the existing `ForgeArgument` and `ForgeAssemblyPath` IDs; the prefab remains unchanged. The protected registry codemap and current ForgeWeave/system-design guides now identify SDK contract 9 and document owner-scoped model registrations, fail-closed bounded auditing, and the distinction between the static Pulse audit threshold (50 ms) and ForgeWeave's runtime minimum (250 ms).
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` passed with exit code 0. The selected `net472` and `net8.0` builds reported zero warnings and errors; Core passed 301/301 and ForgeWeave passed 65/65. Tests were run through the `.bat` launcher; no test executable was invoked directly.
- Product source remains 24.0.0 and `ForgeApi.Version` is 9. Bannerlord was not launched, so engine-dependent helper behavior is not claimed as live-verified. Existing ZIPs were not regenerated or modified.

## 24.0.0 source follow-up — 2026-09-26 (Rev049)

- Reduced SDK model-query work: `ForgeModelRegistry.GetModifiers` now filters under the registry lock into a detached category result instead of copying the entire registration list for every query. A synthetic 1,024-modifier/128-query harness measured 15.647 ms before and 0.57–0.62 ms after; this is a test-harness microbenchmark, not game-frame latency.
- Replaced ForgeWeave's repeated ready-list re-sorting and front removal with a binary min-heap keyed by ordinal case-insensitive handler ID. Existing deterministic equal-priority ordering is covered by regression tests. In the 200-handler/12-dispatch harness, observed runs fell from 25.965–35.696 ms before to 4.848–6.319 ms after; these values do not measure in-game callback duration.
- Reduced per-request allocations in `ForgeLocalApi`: request dispatch reuses one cached callback instead of capturing a closure, and `/status` reuses its private static UTF-8 response buffer. The 32-request loopback latency showed no improvement (349.213 ms before, 355.539 ms after), so no throughput gain is claimed; the change is limited to avoidable allocations.
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` passed with exit code 0: selected `net472` and `net8.0` builds reported zero warnings and errors, Core passed 303/303, and ForgeWeave passed 66/66. Tests ran through the `.bat` launcher.
- Product source remains 24.0.0 and SDK contract 9. No API signatures, target frameworks, module behavior, or ZIPs changed. Bannerlord was not started; live engine performance was not measured.

## 24.0.0 source follow-up — 2026-09-26 (Rev050)

- Kept the Desktop route ID `ApiDeprecationChecker` stable while changing its visible name, purpose, availability status, and dossier to state in all 13 supported languages that deprecation analysis is unavailable pending a verified, versioned TaleWorlds API catalog with explicit version coverage. The route no longer presents generic analyzer readiness as deprecation evidence.
- Updated the generated desktop resource dictionaries and language-change notifications so the route's localized text and cached search entry follow the selected language. Documented the evidence boundary in the English and Spanish Desktop guide sections.
- Post-change functional tests and live UI verification are pending; this entry does not claim a test pass. Product source remains 24.0.0, public SDK documentation/contracts and route identifiers are unchanged, and existing ZIPs were not regenerated or modified.

## 24.0.0 source follow-up — 2026-09-26 (Rev051)

- Core source heuristics now mask comments and C# literals before evaluating textual rules, honor cancellation while processing individual files, and report unreadable or over-limit source/localization files as localized findings while continuing the bounded scan. `ApiDeprecationChecker` retains its route ID but returns `Unsupported` until a verified, versioned TaleWorlds deprecation catalog is available.
- Desktop's Evidence Ledger and exports preserve rule ID, source location, evidence, and recommendation. Every successful ForgeWeave framework response, including an empty payload, is passed through snapshot validation; malformed or incomplete responses report transport `Received` and snapshot `Unparsed`, retain the payload, and cap the diagnostic reason at 240 characters.
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --no-pause <nul"` passed: solution build reported zero warnings and errors; Core 306/306, ForgeWeave 66/66, Desktop 54/54, and the WPF render harness 275 cases passed. Asset and import-plan BAT suites passed; the optional deep TPAC parser was skipped because its local library/fixture was unavailable. All tests were launched through `.bat` files.
- Product source remains 24.0.0 and SDK contract 9. No public SDK/API, route, or permission changed. Bannerlord was not started, so no in-game behavior is claimed; existing ZIPs were not regenerated or modified.

## 24.0.0 source follow-up — 2026-09-26 (Rev052)

- `ForgeModelRegistry` now reuses immutable category snapshots and evaluates only the requested category; snapshots remain stable after later registrations, and arbitrary enum values do not grow the cache. ForgeWeave reuses dispatch plans for finite non-Custom event kinds and invalidates them on successful registration changes; Custom topic plans remain uncached.
- Core analysis now propagates cancellation through bounded file reads, XML parsing, analyzer loops, and module inspection while keeping the public synchronous validator signature unchanged. Added cooperative-cancellation and bounded-source-scan regressions.
- Five runs of `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` passed: each selected `net472`/`net8.0` build had zero warnings and errors; Core passed 312/312 and ForgeWeave 68/68 in every run. Tests were launched only through the `.bat` file.
- Across those five harness runs, median `GetModifiers` time for 1,024 modifiers/128 queries fell from 0.590 ms to 0.004 ms; Core-linked 200-handler/12-dispatch timing fell from 5.789 ms to 2.167 ms; standalone ForgeWeave timing fell from 6.763 ms to 2.018 ms. These are harness measurements, not Bannerlord frame or live application latency. `Evaluate`, memory statistics, and source-scan timings have no comparable earlier baseline and are recorded as measurements only. No improvement is claimed for noisy `/status` timings.
- Product source remains 24.0.0, SDK contract 9, and public API unchanged. Bannerlord was not started; live engine behavior was not measured. Existing ZIPs were not generated or modified.

## 25.0.0 source update — 2026-09-26 (Rev053)

- Advances the product source and module manifest metadata to 25.0.0 and `ForgeApi.Version` to 10. The SDK and Core remain multi-targeted for `net472` and `net8.0`; existing public method signatures remain in place.
- Adds `ModSettings.TrySave<T>` with explicit serializer, serialization, and storage outcomes. It writes a same-directory temporary file and commits it before changing the cache; failures preserve the previous file and cache. `Register<T>` still returns defaults without creating a placeholder file when serialization is unavailable, while `Save<T>` keeps its signature and logs failures.
- Adds `ForgeApi.AutoRegisterWithReport` with bounded counts and error details, partial registration when independent types fail, and a compatible logging `AutoRegister` wrapper. Adds explicit `Found`, `Missing`, `Expired`, and `TypeMismatch` results for Semantic memory and the applicable non-TTL states for Procedural memory.
- Managed `RegisterWhenAvailable` delivery now checks subscriber lifetime and connection generation, skipping the remainder of a superseded availability snapshot after reentrant lifecycle changes.
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` passed: selected `net472` and `net8.0` builds had zero warnings and errors, Core passed 316/316, and ForgeWeave passed 69/69. Tests were launched only through the `.bat` file.
- Bannerlord was not started and ZIPs were not generated or modified; no in-game behavior is claimed as verified.

## 25.0.0 source follow-up — 2026-09-26 (Rev054)

- Closes the managed-availability unregistration race with per-subscription delivery gates. A queued callback removed before its turn is skipped; `UnregisterWhenAvailable` waits for an in-flight callback, while callbacks remain outside the SDK-wide availability lock and may unregister themselves.
- Rejects null settings before `TrySave<T>` prepares or commits a file, preserving both the previous file and cached object. Documents the case-insensitive Windows settings identity and the protection against an older reentrant/concurrent registration load overwriting a newer commit.
- Added deterministic coverage for a removed callback already present in `Connect`'s snapshot, in-flight callback draining, self-unregistration, and null-save preservation. The `.bat` launcher passed with zero build warnings/errors, Core 318/318, and ForgeWeave 71/71.
- This follow-up corresponds to protected Registro revision Rev035; the source changelog sequence is independent. Bannerlord was not started and ZIPs were not generated or modified.

## 25.0.0 source follow-up — 2026-09-26 (Rev055)

- Reused finite-event ForgeWeave dispatch plans while producing `Snapshot()` findings; `Custom` topics remain uncached and health/report values continue to be copied from current state. Five same-machine `.bat` measurement runs reduced the median for 8 snapshots at 256 handlers/64 retained records from 60.870 ms to 26.504 ms (56.4%); the standalone harness median moved from 94.258 ms to 67.380 ms (28.5%). These are harness timings, not in-game latency. `GetHandlerHealth` was measured and left unchanged because the results did not identify a stable bottleneck.
- `SharedServiceMonitor<T>.Changed` now reuses a typed subscriber array rebuilt only when handlers are added or removed. In five runs, a 1,000-monitor notification batch improved from an 80.041 ms median to 65.324 ms (18.4%); the 100-monitor case remained effectively flat (4.558 ms versus 4.585 ms). Subscription order, callback isolation, reentrancy and thread affinity remain covered.
- Evaluated and reverted an `Evaluate` result-list ownership shortcut: dense unconditional and mixed profiles improved by only 7.3% and 3.4%, below the 10% threshold, while a broader 127-modifier profile regressed. Dense-true improved, but the result was not consistent across profiles. The existing public constructor continues to defensively copy caller lists. Memory and shared-service resolution measurements did not justify changing their implementations.
- Corrected the ForgeWeave telemetry documentation and test naming: dispatch samples use a preallocated bounded buffer, while on-demand percentile calculation allocates a bounded copy and sorts it. No zero-allocation claim is made for reporting.
- Five baseline and five post-change runs of `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause"` passed their respective suites. After reverting the `Evaluate` candidate, the final launcher run passed with zero warnings/errors in the selected `net472` and `net8.0` builds, Core 323/323 and ForgeWeave 73/73. All tests were launched through the `.bat` file.
- This source follow-up is recorded in protected Registro revision Rev036; the source changelog and DOCX revision numbers are independent.
- Product source remains 25.0.0 and `ForgeApi.Version` remains 10; public API, target frameworks and game behavior are unchanged. Bannerlord was not started and existing ZIPs were not generated or modified.

## 25.2.0 Desktop source follow-up — 2026-09-26 (Rev056)

- Corrected narrow-window workbench composition: the navigation rail has a 220-DIP minimum (capped at 276 DIP), the work area retains a 400-DIP minimum, and the Split Deck panel is 280 DIP wide. Added minimum-window Split Deck coverage.
- Matched the cartographic card frame to its source aspect ratio (160×90 DIP for 448×252 pixels), retained uniform uncropped presentation, and removed fixed-scale bitmap caches from passive map/seal decorations. Localized fixed shell labels, help, pinned/empty fallback labels, and tooltips across all 13 language dictionaries with 101 matching keys. Clearing pinned content raises derived title/category notifications after clearing its evidence.
- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause <nul"` completed with zero build warnings/errors and Desktop MVVM 55/55. Five serial runs of `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause --output <report.json> <nul>` saved reports with suffixes `-01` through `-05`; every run passed 275 cases, with 152 layout passes.
- Across the final five render-harness runs, median total time was 11,335 ms versus 13,652 ms at baseline (-17.0%); median layout-call time was 2,075 ms versus 2,397 ms (-13.5%). Median startup, route/filter, localization, and theme phases were 2,582 ms, 2,042 ms, 2,007 ms, and 3,109 ms; all were below their baseline medians. These are harness measurements, not interactive Desktop latency. The scale matrix rasterizes preview output and does not simulate Windows system-DPI layout.
- Decorative rail artwork remains intentionally collapsed because its source aspect ratio does not fit the available rail frame. Live WPF/UI Automation inspection remains pending; no live visual approval is claimed. Product version remains 25.2.0, and no ZIPs were regenerated or modified.
## 25.2.0 Desktop source follow-up — 2026-09-26 (Rev057)

- Reorganized the WPF shell into focused presentation controls while keeping `MainWindow` as the window/chrome host and moving page templates into `Resources/Views/ToolPageTemplates.xaml`. Added `AdaptiveWorkbenchPanel` to lay out the selected route, optional Split Deck, and contextual dossier side by side when their minimum widths fit and to stack them in the workspace scroll viewport otherwise. The optional dossier is controlled by a localized, accessible toggle and is not persisted.
- Kept the existing route catalog, navigation rail, keyboard commands, MVVM state, SDK/IPC contracts, and product version. Added two local optimized passive textures: a 2172×67 title band and a 256×171 card-corner ornament. High Contrast retains solid surfaces; decoration legibility still requires review.
- Rev057 validation is pending: the five-run pre-change render baseline passed 275 cases and 152 layout passes per run, with medians of 11,286 ms total and 2,072.6 ms in synchronous layout calls. These are harness timings, not application latency. Post-change BAT suites and read-only UI Automation have not yet been approved or recorded. No ZIP was regenerated or changed.
- The theme selector now trims long display labels such as High Contrast to one line. The master test launcher also accepts --render-output <path>; it only redirects the render JSON report and does not affect preview geometry. At 100–200%, the harness rasterizes at the requested device scale while keeping a 1360×820-DIP logical viewport and identity layout transform; it does not modify Windows system DPI or DIP layout measurements.

## 25.2.0 Desktop source follow-up — 2026-09-26 (Rev058)

- Corrected the compact search and action-control dimensions: rail and command-palette searches are 42 DIP high with centered text; the 32×32-DIP clear button has reserved query padding. At the 980×680-DIP minimum, header buttons for the command palette, Split Deck, and dossier use minimum widths of 116, 120, and 126 DIP and a 36-DIP minimum height. Work-order action buttons are 34 DIP high with bounded widths and wrapping. The render harness confirms the actions remain visible at minimum and normal window sizes.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` built with zero warnings/errors, passed Desktop 56/56, and completed a build-backed render run with 277 cases/161 layout passes in 9,410 ms. Five subsequent skip-build runs, each with a distinct JSON report (`artifacts/desktop-render-rev057-controls-repeat-01..05-20260926.json`), passed 277 cases/161 layout passes. The exact `milliseconds` samples were 8,754, 8,928, 8,281, 8,355, and 8,093 ms (median 8,355 ms); `renderLayoutMilliseconds` were 1,685.9, 1,826.3, 1,652.8, 1,709.0, and 1,507.1 ms (median 1,685.9 ms).
- Compared with the five-run Rev057 final2 group (276 cases/161 layout passes; medians 8,360 ms total and 1,732.6 ms layout), the total median changed by -0.06% and layout median by -2.7%. The result is essentially stable and does not establish a substantial performance improvement. These are harness timings, not application interaction latency.
- `tools/Test-CalradiaForge-Desktop-Uia.bat` passed 20/20 bounded read-only checks in `artifacts/desktop-uia-rev057-controls-final.json`. `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check` passed; the packaged PNG inventory is 7,465,788 compressed bytes and 15,921,104 decoded RGBA bytes, including the two Rev057 decorations (310,173 compressed bytes and 975,312 decoded RGBA bytes). The raster-scale records do not change Windows system DPI or simulate OS-DPI layout; no human visual approval is claimed. Bannerlord was not started, no ZIP was regenerated, and product version 25.2.0 and public contracts remain unchanged.

## 25.2.0 Desktop visual polish — 2026-09-26 (Rev059)

- Reduced Parchment surface/title texture opacity from 0.34/0.32 to 0.11/0.12 so decorative grain stays behind content. The navigation rail exposes a visible, observable active-route selection and its search field shows the localized `Ui.SearchHint` hint.
- Reserved a 168-DIP cartographic-board column for a 160×90-DIP frame with an 8-DIP margin. Enlarged the empty-evidence illustration to 42 pixels and its action button to 32 DIP.
- `tools/Run-CalradiaForge-Tests.bat` completed the Desktop build with zero warnings/errors, passed Desktop 56/56 and passed 277 WPF render cases with 161 layout passes. The harness recorded 7,808 ms overall and 1,824.9 ms in layout calls; these are harness timings, not application latency.
- `tools/Test-CalradiaForge-Desktop-Uia.bat` passed 20/20 bounded, read-only checks in `artifacts/desktop-uia-rev059.json`; this is an accessibility-tree check, not visual approval. Product version remains 25.2.0. No live visual approval is claimed and no ZIP was regenerated.

### Focus-safe WPF test execution (Rev060)

- Moved WPF render HWNDs to a private Windows desktop before creating any WPF objects or hooks. The guard records private-desktop foreground events only and no longer calls `SetForegroundWindow`.
- Replaced native minimize/maximize/restore interaction checks with binding/style/handler inspection. Removed explicit focus restoration from the opt-in UIA runner; it now fails and reports if its owned app is observed taking foreground.
- Two hidden runs of `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output <unique-json-path>` built with zero warnings/errors, passed Desktop 57/57 and render 278/278, and preserved interactive HWND `0x171168`. The final render harness reported 158 layout passes and 9,492 ms total. UIA was not rerun; no UIA pass is claimed.
- The renderer was observed taking foreground only on its separate private desktop. Product version remains 25.2.0; no public contract, route, or ZIP changed.

### Focus-safe Desktop test launcher (Rev061) — 2026-09-26

- Added `tools/Run-CalradiaForge-Desktop-Checks-Hidden.vbs` to run the existing Desktop/render `.bat` suite with no visible command window and unique per-run logs/status/report artifacts. The opt-in UIA PowerShell smoke check remains separate from this silent path.
- UIA process startup now requests `CreateNoWindow` and `Hidden`, and its passive observer records recent foreground transitions. It never activates or restores a window.
- Kept product version 25.2.0 and all application/API behavior unchanged. The focus check in this revision ran while no Calradia Forge WPF window was open; user-window foreground preservation remains unverified in a simultaneous live-app run.
- The hidden launcher exited 0 with Desktop 59/59, 281 WPF render cases, 158 layout passes, zero build warnings/errors, and 7,912 ms total harness time. Foreground HWND `0x580296` (AvastUI) was unchanged before and after. A combined opt-in UIA attempt produced no report and ended at a different AvastUI HWND; its cause is unknown, so UIA remains excluded from this runner.

### Responsive route status readability (Rev062) — 2026-09-26

- The application-status card now wraps long selected route names to a second line at the 980×680-DIP minimum instead of truncating them on one line. The full value remains available in the card tooltip; other status cards and route behavior are unchanged.
- The Desktop `.bat` suite passed 59/59 and the WPF render suite passed 281/281 with 158 layout passes. Build completed with zero warnings and errors; the render report recorded 8,415 ms. This is harness timing, not application interaction latency.
- Product version remains 25.2.0; no API, route, command, permission, or ZIP changed.

### Portrait rail illustration (Rev063) — 2026-09-26

- Replaced the unused horizontal rail decoration with a locally generated transparent portrait illustration, optimized to 320×640 RGBA and uniformly fitted behind the navigation content. The retired derivative is excluded from the Desktop resource manifest; the source is retained.
- Added per-theme rail opacity tokens for War Table, Parchment Light, and High Contrast. Decorative visibility remains controlled by the existing setting; High Contrast surface brushes remain solid, and the artwork cannot receive input or keyboard focus.
- Texture generation and deterministic `--check` passed. Desktop `.bat` validation built without warnings/errors, passed 59/59 Desktop tests and 281 WPF render cases/158 layout passes. The harness reported 7,998 ms overall and 1,779.8 ms in layout calls; these values are not application-interaction latency. Render screenshots were reviewed; live application and actual system-DPI appearance remain unverified.
- Product version remains 25.2.0; no public API, route, command, permission, or ZIP changed.

### High-contrast rail illustration refinement (Rev064) — 2026-09-27

- Replaced the too-dark Rev063 rail derivative with a brighter, transparent brass/verdigris engraving, preserving the tall composition and passive behavior. Rev063 remains historical and is excluded from embedded resources.
- Added a High Contrast render regression that checks alpha-composited decoration visibility against the solid navigation surface and verifies paper/muted text remains at least 4.5:1 over every visible art pixel.
- Deterministic texture validation passed. The Desktop BAT build had zero warnings/errors; Desktop passed 59/59 and render passed 281 cases/158 layout passes. The report recorded 7,802 ms total and 1,738.8 ms in layout calls. These are harness measurements; screenshots are not live-app/DPI approval.
- Product version remains 25.2.0; no public API, route, command, permission, or ZIP changed.

### Evidence report surface refinement (Rev065) — 2026-09-27

- Made the Evidence and Raw Result tabs visibly distinct with localized headers, icons, a live evidence count, and selected/hover/focus states. Populated evidence now uses legible cards for source/status, rule ID, location, finding text, and recommendation. The empty-ledger action remains interactive beside its passive local illustration; it invokes the existing command-palette command.
- Added render regressions for report labels, populated evidence context, empty-state command binding and hit testing, and layout bounds. `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-report-rev065-fix1.json"` built with zero warnings/errors, passed Desktop 59/59 and WPF render/resource checks 281/281 with 158 layout passes. The report recorded 7,856 ms overall and 1,705.8 ms in layout calls; these are harness measurements, not open-app latency.
- No live application review was performed. Product version remains 25.2.0; public API, routes, commands, permissions, dependencies, assets, and ZIPs are unchanged.

### Off-screen evidence hit-testing scope (source Rev066) — 2026-09-27

- Clarified that the empty-ledger action hit test runs in the off-screen WPF render host using `VisualTreeHelper.HitTest` at the button center and checks its WPF hit surface and visible ancestors while the command-palette overlay is collapsed. This verifies the rendered visual subtree only; it does not simulate operating-system pointer input or a live-window click.
- The final post-fix artifact `artifacts/desktop-visual-report-rev066-review.json` passed with Desktop 59/59, 281 render cases, and 158 layout passes, including the whitespace-only recommendation regression. Build completed with zero warnings/errors. The JSON records 7,821 ms total and 1,707.2 ms in layout calls; neither value represents application interaction latency.
- Operating-system pointer behavior and live WPF behavior remain unverified. Source changelog Rev066 is independent of protected Register of Improvements appendix Rev046. Product version remains 25.2.0; no public API, route, command, permission, dependency, or ZIP changed.

### Status and command hierarchy refinement (Rev067) — 2026-09-27

- Promoted the existing command-palette action in the header with a brass primary surface and a passive local search glyph, preserving its accessible name and command binding. Added consistent passive semantic icons and a localized active-tool label to the five status cards; tuned their opacity per theme, with the strongest treatment in High Contrast. Increased the evidence-export target height to 30 DIP.
- Added `Ui.ActiveTool` to all 13 locale catalogs and render checks for localized status labels, passive icon hit/focus behavior, theme opacity, and primary-action sizing/accessibility.
- The Desktop BAT build completed with zero warnings/errors; Desktop passed 59/59 and WPF render passed 285 cases with 158 layout passes. The final artifact reports 8,681 ms total/2,013.7 ms in layout calls; the same-session baseline recorded 10,893 ms/2,564 ms across 281 cases/158 passes. These are single-run harness timings and do not establish open-app performance. Off-screen War Table and High Contrast previews were reviewed; live app behavior was not.
- Product version remains 25.2.0; no API, route, command, permission, dependency, or ZIP changed.

### Compact Desktop header and evidence summary (Rev068) — 2026-09-27

- Tightened the existing WPF header for the 980×680-DIP minimum: shortened the visible decorative-accent label while preserving its full localized accessible name and tooltip, changed the dossier action to a localized accessible book-glyph toggle, and reduced header control widths so the row stays compact.
- Replaced the evidence-export button's visible text with a passive local icon in a 36×30-DIP button. The existing command, automation ID, accessible name, and tooltip remain; the recovered status-card width keeps the retained-evidence summary readable.
- Added render assertions for minimum-size header placement, summary visibility, compact export geometry, and dossier/decorative-toggle accessibility. All 13 locales contain `Ui.DecorativeAccentsShort`.
- The Desktop BAT build completed with zero warnings/errors, Desktop 59/59, and WPF render 285/285 with 158 layout passes. The final single run recorded 13,135 ms total/3,072.1 ms in layout calls, versus 9,120 ms/2,033.3 ms in the single-run baseline. These are harness measurements; the final run was slower, so no performance improvement is claimed. Live WPF behavior and real system-DPI layout remain unverified.
- Product version remains 25.2.0; no API, route, command, permission, dependency, or ZIP changed.

### Minimum-window ledger and responsive identity layout (Rev069) — 2026-09-27

- Added 980×680-DIP render coverage confirming the empty evidence ledger message and action remain in the visible work-area viewport, and the action retains its minimum activation height. The tool identity badge is bounded; long category banners and mottos remain single-line, trim with an ellipsis, expose the full value in tooltips, and stay within the badge frame.
- Added a minimum-width, single-row header assertion across all 13 supported locales. Existing local illustrated-resource and passive-decoration checks remain in the render suite; this follow-up adds no runtime dependency or new asset.
- The supplied final BAT artifact reports zero build warnings/errors, Desktop 59/59, and WPF render 286/286 with 172 layout passes. Its JSON records 11,719 ms total harness time and 3,106.8 ms in layout calls. These are harness measurements, not open-app latency or a performance claim. No runtime UI Automation or live-app validation was performed.
- Product version remains 25.2.0; no public API, route, command, permission, IPC, dependency, or ZIP changed.

### Windows system-DPI audit scope clarification (Rev070) — 2026-09-27

- Clarified that the WPF render harness keeps the isolated host's Windows DPI/DIP layout fixed. The 100% and 200% preview values change `RenderTargetBitmap` output raster density only; they do not emulate Windows system-DPI layout at 125%, 150%, or 200%.
- The supplied `artifacts/desktop-visual-rev069-scale-audit.json` marks `windows-system-dpi-layout-coverage` as `not-simulated`, with no Windows DPI factors applied and preview bitmap raster factors `[1, 2]`. Existing functional route, locale, theme, and interaction coverage was retained.
- The supplied run reports Desktop 59/59, 287 WPF render cases, 172 layout passes, and zero build warnings/errors. JSON harness timings are 9,321 ms total and 2,134.7 ms in layout calls; these are not open-app latency. Actual Windows DPI behavior remains unverified. No tests were run during this documentation-only append.
- Product version remains 25.2.0; no API, route, command, permission, IPC, dependency, or ZIP changed.

### Desktop IPC, report export, and studio localization hardening (Rev071) — 2026-09-27

- Bound newline-delimited IPC reads incrementally at the existing 32 Mi UTF-16-character limit; cancellation remains observable, and oversized responses disconnect before deserialization.
- Moved report writes to asynchronous exclusive staging and unique no-overwrite publication, with cancellation/error status and temporary-file cleanup.
- Added command-specific accessible names and localized copy feedback to the dossier; localized remaining static visualizer labels in all 13 catalogs while preserving sample values and technical identifiers.
- The Desktop BAT build completed with zero warnings/errors; Desktop passed 63/63 and WPF render passed 289 cases with 182 layout passes. The artifact records 11,758 ms total harness time and 3,483.7 ms in layout calls; neither is an open-app latency measurement.
- After wrapping translated stat lines and compact badges in narrow visualizer cards, a final BAT rerun passed the same coverage; its separate artifact records 10,939 ms total and 3,120.8 ms in layout calls, both harness-only measurements. The read-only UIA rerun passed 23/23 controls, including dossier copy buttons, and preserved the foreground window.
- The read-only UIA BAT passed 23/23 controls and recorded ForegroundUnchanged=true; it did not exercise pickers, work execution, preference changes, or visual approval. No Bannerlord session was started.
- Product version remains 25.2.0; no public API, route, command, permission, protocol, dependency, or ZIP changed.

### Responsive Desktop footer and Gauntlet panel fit (Rev072) — 2026-09-27

- Kept the WPF shortcut footer fully inside the 980×680-DIP minimum viewport with a bounded bottom inset and text-fit coverage in all 13 locales. Replaced the packaged Rev057 titlebar crop with the deterministic, higher-resolution Rev072 cartographic panorama; the full-resolution master stays out of the assembly.
- Refined the Gauntlet header spacing, evidence focus layout, and Playbook reservation. The Playbook now yields its right-side workspace margin and hides while evidence is focused. Eight navigation areas use their semantic sprites; all 194 tool routes and 56 command bindings remain intact.
- Updated the visual auditor to model 1220/1280/1600/1920 viewport widths, normal/detailed/focused evidence states, and verified opaque panel occlusion. Updated the icon allowlist to current prefab use while keeping all 17 generated icon assets and the 30-sprite atlas inventory required.
- Desktop render coverage remained 289 cases and 182 layout passes in each of five baseline and five after runs. Harness medians were 10,953 ms before and 11,474 ms after (+4.8%), while layout-call medians were 3,220.1 ms and 3,095.3 ms (-3.9%). These are render-harness measurements, not open-application latency; no overall performance improvement is claimed. Texture `--check` was deterministic and the package stayed within its resource budgets.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` completed with zero build warnings/errors: Assets 8/8, Core 338/338, ForgeWeave 73/73, Desktop 63/63, and WPF render 289 cases/182 layout passes. `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` passed. Read-only Desktop UIA passed 23/23 checks.
- The Client and Modding Kit profiles built and were deployed with verified file backups. The installed TPAC was preserved with SHA-256 `69513B5617F026E1F106F6CA6D47CF5C5CE0B5869FFA472E9166B037CA8EDEA6`. Modding Kit startup stopped at an `RGL WARNING` dialog, which was left open; therefore live panel rendering remains unverified. No campaign or battle was loaded.
- Product version remains 25.2.0; no public API, route, command, permission, dependency, or ZIP changed.

### Gauntlet F10 scrollbar crash correction (Rev073) — 2026-09-27

- Corrected the generated `ForgeEvidenceScroll` prefab structure after the main-menu F10 path produced a TaleWorlds built-in scrollbar assertion and a `ScrollablePanel.UpdateScrollablePanel` access violation. The panel now resolves its relative reference to a sibling vertical `ScrollbarWidget` with a matching identified handle.
- Extended the structural audit to enforce the scrollbar contract across all five scrollable panels and adjusted the evidence-surface regression to allow only the scrollbar handle image.
- Core/ForgeWeave BAT checks passed 333/333 and 73/73 with zero build warnings/errors; the Gauntlet visual-check BAT passed with zero audit errors/warnings and Core 338/338. Client and Modding Kit profiles built and deployed with verified backups; the existing TPAC was preserved.
- Live validation in the Steam-launched singleplayer main menu confirmed F10 opens the eight-area panel and a second F10 closes it without the assert or crash. No campaign or battle was loaded. This does not validate Modding Kit runtime behavior.
- Product version remains 25.2.0; no public API, route, command, permission, dependency, TPAC replacement, or ZIP changed.

### Responsive detailed Playbook and scroll layout (Rev074) — 2026-09-27

- Moved the detailed Playbook below the context cards and reserved a right-side column so it no longer covers active controls or rows. Detailed-mode action buttons contract to 100 DIPs, and the Playbook content now uses adaptive-height scrolling.
- Made Key Help and Playbook mutually exclusive and extended the Gauntlet scrollbar audit to six scrollable surfaces.
- The full test BAT passed, including Desktop 63/63 and WPF render 289 cases; the Gauntlet visual-check BAT passed with zero visual-audit errors and zero warnings.
- Live in-game validation of this additional placement/scroll adjustment remains pending confirmation; the BAT checks do not establish its rendered behavior in Bannerlord.
- Product version remains 25.2.0; no public API, route, command, permission, dependency, or ZIP changed.

### Playbook text wrapping and scrollbar type guard (Rev075) — 2026-09-27

- Added word-boundary wrapping for localized detailed Playbook and troubleshooting text while preserving source strings, routes, and commands. The generated Playbook uses a clipped vertical `CoverChildren` flow with the macro action as its reachable final item.
- Made the Gauntlet audit case-sensitive for the native `ScrollbarWidget` type and explicitly reject the invalid `ScrollBarWidget` spelling; added checks for Playbook flow ancestry and content order.
- After correcting an outdated `CoverChildren` decorative-audit assumption without relaxing layout limits, `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` exited 0 and reported 338 passed, 0 failed.
- The F10 session log recorded toggles, but the associated capture did not show the overlay. Live visual approval for this Playbook follow-up remains pending.
- Product version remains 25.2.0; no public API, route, command, permission, dependency, or ZIP changed.

### Illustrated heraldic Gauntlet renewal and bounded F10 evidence (Rev076) — 2026-09-27

- Replaced the header identity-gap and navigation-rail underlay artwork with two original local heraldic sprites: `forge_heraldic_header_v2` (256×48, alpha capped at 88/255) and `forge_heraldic_rail_v2` (128×256, alpha capped at 64/255). Existing widget IDs remain passive; the rail artwork is centered at its native 128-DIP width and height-capped to its available viewport.
- Regenerated the source atlas and `SpriteData` through the project asset pipeline. The source atlas is 4096×512 with 32 registrations (17 game icons and 15 decorative sprites). The structural audit now checks the new sprite references, alpha caps and rail geometry at the supported viewport profiles.
- `tools/Prepare-CalradiaForge-ImageGenTextures.bat --check --no-pause`, `tools/Test-CalradiaForge-DecorativeTextureDeterminism.bat --no-pause`, and `tools/Validate-CalradiaForge-DecorativeSprites.bat --no-pause` passed. `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` passed with 0 audit errors/warnings and Core 339/339; `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` passed Core 339/339 and ForgeWeave 73/73. The Release solution build completed with zero warnings.
- The installed TPAC was left intact and predates this atlas; Resource Browser import and in-game visual rendering remain pending. No game was launched for this round.
- F10 evidence is limited to source and persisted diagnostics: source and installed prefab SHA-256 values match (`5741D7571C254B6487477D601F0CFE016AA254A1DFC6BD6795766274543B2F45`), and both use the canonical `ScrollbarWidget` spelling. The prior session log records opening/loading and closing, and the F10 edge/layer telemetry regression passed. This is not a fresh crash reproduction; the historical assertion's exact cause and whether it is resolved remain unverified.
- Product version remains 25.2.0; no public API, route, command, permission, dependency, TPAC or ZIP changed.

### F10 prefab hash chronology and instrumentation boundary (Rev077) — 2026-09-27

- Clarifies that Rev076's matching source/installed prefab hash `5741D7571C254B6487477D601F0CFE016AA254A1DFC6BD6795766274543B2F45` was measured before source regeneration. The regenerated source prefab is now `C7BDE061D57F142797DF7B046BC93437E53049351A9D1C1821F6326CDC3BAC31`; the installed prefab remains at the prior hash because the new source assets were not imported or deployed. Both use canonical `<ScrollbarWidget>` and omit `<ScrollBarWidget>`; this does not establish the cause or resolution of the historical F10 assertion.
- `SubModule.Open()` now records ordered brush-file and panel-movie loading/loaded milestones. The regression checks source order statically; it is not a runtime telemetry capture or a reproduction of the assertion. Cause/resolution, Resource Browser import, and live rendering remain unverified or pending.
- Product version remains 25.2.0; this clarification changes no API, route, command, permission, TPAC, or ZIP.

### Gauntlet test-results explorer (Rev081) — 2026-09-28

- Added a panel-lifetime inspector for the latest structurally valid `run` or `run-batch` response. A scrollable list displays result ID, status and duration; selecting a result reveals its seed, context, `StartedAt`, steps, error and cleanup error. The presentation parser defensively retains at most 51 records; the current `TestEngine` accepts batches of 1–50 tests and rejects larger batches.
- Opening the inspector or selecting a row is read-only and does not execute or repeat tests. Failed test statuses remain explicit. Raw ledger output, arguments, history, filtering and output comparison are unchanged; no new route, command, public API or protocol surface is introduced.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` passed the sprite and prefab/layout audits with 0 errors and 0 warnings; its Core suite passed 343/343. `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` passed clean `net472` and `net8.0` builds, Core 343/343 and ForgeWeave 73/73. Structural bindings and all 13 generated language resources passed.
- The existing F10 rising-edge and GauntletLayer lifecycle telemetry regression passed. It is a source-structure check, not a live input test; the earlier native assertion and live overlay behavior remain unverified, and no code change to `SubModule.cs` was needed.
- In-game inspection remains pending. The installed TPAC predates the current source atlas; source checks do not establish TPAC import or live Gauntlet rendering.
- Product version remains 25.2.0; no ZIP is regenerated.

### Illustrated Gauntlet layout corrections and F10 source review (Rev082) — 2026-09-28

- Rebalanced the normal evidence ledger so the 1280×720 audit viewport reserves at least 160 DIPs for evidence. Separated the test-results status and duration cells, clipped their text to each column, and enforced at least 6 DIPs of clearance.
- Retired the eight legacy `forge_header_*_v1` ornaments, authored at 128×64 but drawn at 24×12, from active sprite generation. Their master, prepared, and SpriteParts copies remain preserved in `assets/gauntlet-imagegen/archive/2026-09-28/` with a SHA-256 manifest. Added three local ImageGen masters for cartographic cloth, the vertical heraldic rail, and the transparent header treatment; the project texture-preparation check is deterministic.
- Reviewed the existing F10 rising-edge fallback and `GauntletLayer` telemetry path. The source-level regression required no `SubModule.cs` change; this does not establish live input or resolve the historical native assertion.
- `tools/Prepare-CalradiaForge-ImageGenTextures.bat --check --no-pause` passed. The reported Core/ForgeWeave run passed Core 343/343 and ForgeWeave 73/73 without build warnings. The full Gauntlet visual audit and source-atlas regeneration remain pending for this revision.
- Resource Browser import and live Gauntlet rendering remain pending; no current source check proves either. No live F10 observation, campaign, or battle was performed for this revision.
- Product version remains 25.2.0; public API, routes, commands, permissions, and ZIPs are unchanged.

### Resource Browser Gauntlet atlas TPAC import verified (Rev083) — 2026-09-28

- Imported the generated `ui_calradiaforge_1` atlas through Resource Browser with the existing texture settings. After saving and refreshing, the inspector loaded the resource as a 4096×512 texture; runtime details showed DXT5 and 13 mip levels.
- The installed `ui_calradiaforge_1_tex.tpac` changed from SHA-256 `69513B5617F026E1F106F6CA6D47CF5C5CE0B5869FFA472E9166B037CA8EDEA6` to `131E623077708032C76790324F31EDC7862BDC6D5ACA79350B4A03C241A0529E`. The pre-import installed and source TPAC backups were rechecked and retained.
- The workspace source TPAC remains at its prior hash because the collection helper depends on the obsolete TpacTool reader; TpacTool was not used. Resource Browser import is verified, while live Gauntlet panel rendering remains unverified.
- Product version remains 25.2.0; no API, route, command, permission, or ZIP changed.

### Corrected Resource Browser atlas import and live Gauntlet verification (Rev084) — 2026-09-28

- Corrected Rev083's evidence: the Texture Inspector **Save** action stores import settings; selecting the atlas and confirming **Update** in Resource Browser performed the import. The browser reloaded `ui_calradiaforge_1` as a 4096×512 texture (DXT5 runtime, 13 mip levels).
- The imported TPAC and collected workspace source TPAC are 539 bytes with SHA-256 `1506C5EAECD4BFC752678C6E6CDB3FA87C4A7A51D8B3B7846B15715276D571EF`. The pre-import installed and source backups were retained and hash-verified. The intermediate `131E6230...` value in Rev083 was not proof of the completed import; TpacTool was not used.
- After Client and Modding Kit builds completed with zero warnings and errors, one F10 observation at Bannerlord's main menu showed the panel and its header/rail texture ornaments. No assertion or missing-texture placeholder appeared. No campaign, battle, or test was started.
- Rev083's statement about Escape is corrected: the user closed Computer Use to reduce resource use. Version 25.2.0, API, routes, commands, permissions, and ZIPs remain unchanged.

### Resource Browser atlas re-import confirmation (Rev085) — 2026-09-28

- The user repeated the Resource Browser import. The loaded inspector showed `ui_calradiaforge_1` as a 4096×512 texture, with `B8G8R8A8` source data and DXT5 runtime data at 13 mip levels. The import uses file selection and **Update**; inspector **Save** only stores import settings.
- The installed Steam TPAC and workspace source TPAC are both 539 bytes and match at SHA-256 `8899A48A407591ADA53573EFC0DD699EA47D2A30F32A0C12C1875003F7993047`. Pre-import and post-import backups remain outside the repository and were hash-verified. TpacTool was not used.
- No new in-game render or F10 observation followed this repeat import; live rendering remains pending. Product version stays 25.2.0; no API, route, command, permission, dependency, or ZIP changed.

## Calradia Forge 25.2.0 patch-engine source follow-up — 2026-09-29 (Rev086)

- Removes implicit patch scanning from module startup; the legacy `InitializeGlobalPatches()` entry remains an obsolete no-op. `ForgePatcher.ApplyAll(assembly)` is explicit and validates the supplied patch batch before writing. Patch Preflight remains read-only and resolves declared targets/callbacks, duplicate IDs, conflicts, and ordering without loading assemblies or invoking callbacks.
- Adds optional `IForgePatchService` through `ForgeApi.Patches`, advancing `ForgeApi.Version` from 10 to 11 without changing `IForgeRegistry` implementers. `ForgeDetour`, `MethodSwapper`, and patch records share one write/verification path. Reversion checks exact bytes, reports foreign modifications as conflicts, and requests instruction-cache flushing while checking executable-page protection changes. Adds `cf.patch_status [owner]` and `cf.patch_revert <id|owner|all>`; Patch Preflight over IPC remains read-only.
- The backend remains experimental: it does not suspend threads, decode or relocate overwritten instructions, or guarantee safety while a target method is executing. No Bannerlord or Modding Kit runtime test was performed for this entry. The disposable x64 fixture and its BAT integration are present, but BAT execution/results and full regression results remain pending; no test pass is claimed here.
- Product version remains 25.2.0; ForgeWeave and distribution ZIPs are unchanged.

## Calradia Forge patch-engine verification follow-up — 2026-09-29 (Rev087)

- Completed the isolated x64 detour fixture through `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat --no-pause`. The launcher builds into a unique temporary directory and removes only that output; the serial fixture verified the target result sequence `15 → 32 → 15` and exact revert.
- `cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` passed with clean `net472` and `net8.0` builds, 0 warnings and 0 errors, Core 361/361, the isolated native detour fixture, and ForgeWeave 73/73. Regressions include explicit batch rollback, conflicted host reconnection/manual recovery, service ownership, byte-state uncertainty, and reserved `all` command collisions.
- This follow-up updates Rev086's pending test status with the completed BAT evidence. No Bannerlord or Modding Kit runtime session was started. Fixtures invoke targets serially and do not prove safety when another thread may execute a target during a write; the patch backend remains experimental.
- Product version remains 25.2.0; SDK capability version is 11. No ForgeWeave, IPC write route, or distribution ZIP changed.

## Calradia Forge patch-engine safety follow-up — 2026-09-29 (Rev088)

- Reject detour writes that cross a system page boundary before changing memory protection; add boundary and no-write regressions. `TypeReference.From` and Patch Preflight now preserve and compare generic parameter owner/position (`!0` versus `!!0`) so same-name type and method parameters cannot alias.
- Add the separate optional `IForgePatchServiceLifecycle` capability to reopen a disconnected built-in patch service only after each prior record and original bytes are verified reverted with no tracked target. Keeping it separate avoids adding a required member to existing `IForgePatchService` implementations.
- The integrated BAT retry passed clean `net472`/`net8.0` builds with 0 warnings/errors, Core 363/363, the serial x64 detour fixture (`15 → 32 → 15`, exact revert), and ForgeWeave 73/73. An earlier integrated attempt reported one non-reproducible Core failure; the isolated Core BAT and subsequent integrated run both passed.
- No Bannerlord or Modding Kit runtime session was started. The serial fixture does not establish safety against concurrent execution during a code write; the detour backend remains experimental. Product version remains 25.2.0, `ForgeApi.Version` remains 11, and ForgeWeave, IPC write routes, and distribution ZIPs are unchanged.

## Calradia Forge patch-boundary preflight correction — 2026-09-29 (Rev089)

- Moves page-span rejection in direct and batch patch routes ahead of any original-byte read or registry reservation. A rejected batch validates every span before reading or reserving any target, so the same IDs and targets remain retryable.
- Adds public-route regressions for direct `Patch` and `ForgePatcher.ApplyAll`, verifying unchanged read/protect/write/flush counts, empty receipts after rejection, and successful reuse under a permitted synthetic page size.
- The latest Core BAT run passed 363/363 managed regressions and compiled the disposable `net472` x64 fixture with zero warnings or errors. The serial native fixture then blocked and was stopped; the latest overall BAT run and native smoke are therefore not reported as passing. The fixture BAT now bounds its child-process wait.
- This follow-up supersedes Rev088's fixture-pass statement for the latest tree only; it preserves Rev088 as the record of the earlier successful run. No Bannerlord or Modding Kit session was started. The detour backend remains experimental and has no concurrent-execution safety guarantee. Product version remains 25.2.0, `ForgeApi.Version` remains 11, and ZIPs remain unchanged.

## Calradia Forge fixture-launcher status correction — 2026-09-29 (Rev090)

- A PowerShell timeout wrapper around the disposable native fixture did not return control reliably and was removed. The fixture BAT is restored to direct child-process launch; no timeout guarantee is claimed.
- The current native smoke remains blocked/unverified. The latest managed Core regressions pass 363/363 and the x64 fixture compiles cleanly, but the Core BAT does not complete because its native child stalls. Earlier successful fixture evidence remains scoped to the earlier tree recorded in Rev088.
- This entry corrects only the timeout statement in Rev089. No module/game session or ZIP change occurred; product version remains 25.2.0 and the patch backend remains experimental.

## Calradia Forge patch-engine documentation and validation follow-up — 2026-09-29 (Rev091)

- Clarifies the Gauntlet copy and architecture maps: Patch Blueprint Preflight is a read-only structural review of declared target and callback references against already-loaded assemblies. It neither applies patches nor invokes callback code, and its resolution does not prove a native replacement can be installed. The Harmony Atlas is a separate read-only inventory of Harmony patches already present in loaded assemblies.
- Corrects the `ForgeDetour` reference to its actual `MethodInfo` replacement API and experimental scope. The module lifecycle and diagrams now show that startup does not scan or apply patches and that preflight is separate from explicit replacement requests.
- Regenerates all 13 native language catalogs with 710 matching keys. Localization audit passed, including the Gauntlet Page Blueprint label, description, and Page title.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` passed: full solution build with 0 warnings and 0 errors; Core 369/369; isolated serial x64 detour fixture `15 → 32 → 15` with exact revert; ForgeWeave 73/73; Desktop 63/63; and 292 WPF render cases. No Bannerlord or Modding Kit session was started.
- The fixture invokes the target serially and does not establish safety while another thread may execute a target during a memory write. The detour backend remains experimental. Product version remains 25.2.0, `ForgeApi.Version` remains 11, and no IPC write surface, ForgeWeave behavior, or distribution ZIP changed.

## Detour fixture BAT-host correction and patch-copy localization — 2026-09-29 (Rev092)

- The disposable detour fixture now builds as a `net472` x64 library and runs only through `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat`, loaded into a temporary x64 Windows PowerShell process. It no longer creates or starts `CalradiaForge.DetourFixture.exe`; the BAT checks that no fixture apphost was emitted. The test remains aligned with the Bannerlord runtime. A trial under the .NET 8 JIT failed to observe the replacement at stage 4 and was discarded rather than treated as a passing fixture.
- Corrects in-game copy so Patch Blueprint Preflight is clearly structural and read-only, Harmony Atlas remains a separate read-only inventory, and ForgeWeave replay guidance does not imply that preflight applies detours. Seven copy keys now have translations across all 13 native catalogs.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` passed the full build with 0 warnings and 0 errors; ForgeWeave 73/73; Desktop 63/63; and 292 WPF render cases. The BAT-hosted serial x64 fixture returned `15 → 32 → 15` with exact restoration. The 13 native catalogs each contain 737 keys and the localization audit is valid.
- The fixture remains serial and does not establish safety against concurrent execution during machine-code writes. No Bannerlord or Modding Kit session was started, and no package generation was invoked in this correction pass. Product version remains 25.2.0 and `ForgeApi.Version` remains 11.

## Calradia Forge explicit Prefix/Postfix hook workbench — 2026-09-29 (Rev093)

- Adds the optional `ForgeApi.Hooks` / `IForgeHookService` capability for explicitly registered Prefix and Postfix hooks through MonoMod.RuntimeDetour 25.3.6 in the Bannerlord `net472` host. Registration remains inert; callbacks execute synchronously on the thread that calls the target. The hook capability is distinct from declarative Patch Blueprint Preflight and from the separate method-replacement patch API.
- Adds guarded Hook Workbench actions in WPF. The operator reviews snapshots, prepares an Apply/Revert plan, checks the confirmation box, and confirms with a single-use token. IPC accepts selected hook IDs and the token only; callbacks, delegates, and executable targets do not cross the pipe. Apply/Revert is restricted to the exact main-menu context on the game thread with no campaign, mission, or multiplayer session. Console commands are `cf.hook_status [owner]`, `cf.hook_apply <id>`, and `cf.hook_revert <id|owner|all>`.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` passed Core 372/372 and ForgeWeave 73/73. Core `net472`/`net8.0` and Mod `net472` builds completed cleanly. The isolated detour fixture passed through `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat`; `DetourFixture.exe` was not executed.
- The fixture is serial and does not prove safety against a concurrent thread executing the target during hook installation or removal; the backend remains experimental. No live Bannerlord or Modding Kit session or asset import was performed. Product version remains 25.2.0; no distribution ZIP was generated or changed.

## Calradia Forge hook lifecycle and host-session safeguards — 2026-09-30 (Rev094)

- Adds an optional disconnect guard so SDK replacement or disconnection cannot silently orphan active or uncertain hooks when the approved game-thread/menu context is unavailable. Verification exceptions remain visible as conflicts, and handles for hooks removed externally are disposed before the service reports a clean reverted state.
- Tightens the Bannerlord menu gate to exact CLR identity for the engine's `GauntletInitialScreen`, resolved from the game Gauntlet assembly at runtime. If the official type cannot be resolved, hook mutations fail closed; name-only lookalikes are rejected.
- Adds `hook-plan-cancel`, which carries only the host session and preview token. Cancellation is bound to that exact pair; WPF retains the preview when cancellation is unconfirmed and stops snapshot refresh instead of discarding local state. English and Spanish SDK protocol documentation now describes the action.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: Core 384/384 and ForgeWeave 73/73 passed; the relevant `net472`, `net8.0`, and Mod `net472` builds completed with 0 warnings and 0 errors. The isolated `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat` passed guarded disconnect, external removal, uncertain verification, and exact `15 → 32 → 15` restoration.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause`: Desktop 65/65 and 292 WPF render cases passed. The render harness reported 13,895 ms total; this is harness timing, not observed application interaction latency.
- No live Bannerlord or Modding Kit session was started. The fixture remains serial and does not establish safety while another thread executes a target during Apply or Revert. Product version remains 25.2.0 and no ZIP was regenerated or modified.

## Typed hook dispatch invoker and measured benchmark — 2026-09-30 (Rev095)

- Replaces normal Prefix/Postfix `Delegate.DynamicInvoke` calls with a strongly typed invoker compiled once per registration, caches parameter metadata, reuses callback arguments, and clones original arguments only when Prefix requires them. BAT validation passed Core 394/394, ForgeWeave 73/73, Desktop 65/65, and the disposable `net472` x64 detour fixture; the fixture BAT build reported zero warnings/errors and the fixture passed its typed-invoker, ordering, duplicate-ID, restoration, and exception checks. No fixture EXE was launched directly.
- One pre-change benchmark BAT run with five internal samples recorded p50s of 773.1 ns for Prefix, 656.0 ns for Postfix, and 657.9 ns for both. The median across five post-change BAT runs was 218.5 ns, 159.5 ns, and 218.6 ns, respectively—approximately 71.7%, 75.7%, and 66.8% lower. This compares one baseline run against the median of five post-change runs; it is not a matched five-before/five-after study. These are isolated fixture measurements, not Bannerlord latency or frame-time measurements.
- The hook backend remains experimental and the serial fixture gives no guarantee when another thread executes the target during modification. No Bannerlord/Modding Kit session or ZIP generation occurred. The source changelog and annex advance to Rev095, while protected DOCX/integrity remain at Rev076 pending reconciliation of missing Rev086–Rev092 annexes; the DOCX generator and registrar were not run.

## Patch preflight hardening and protected-ledger reconciliation — 2026-09-30 (Rev096)

- Hardens ForgeDetour preflight before executable-memory access: rejects P/Invoke, InternalCall, and varargs; validates implicit instance receivers and required/optional signature modifiers; and blocks targets reserved by the hook service. Core regressions verify rejected cases make no memory-adapter reads, protection changes, writes, or cache flushes, and leave no receipt.
- Fixes the disposable fixture BAT build-failure exit code and validates the actual immutable snapshot path in the patch-status code-smell audit. The net472 x64 fixture remains BAT-hosted; no fixture EXE is launched directly.
- tools/Run-CalradiaForge-Tests.bat --core-only --no-pause <nul passed with clean net472/net8.0 builds, Core 395/395, ForgeWeave 73/73, and the serial x64 fixture. These checks do not establish safety during concurrent target execution; the backend remains experimental.
- The protected DOCX and integrity chain were reconciled through Rev095. Rev086–Rev094 source annexes were archival backfills from published bilingual changelog sections; protected revisions 001–076 remain preserved. This entry corrects Rev095's stale pending-reconciliation status without editing it.
- Public-source review did not establish a published independent security audit of MonoMod RuntimeDetour; this does not rule out private or unindexed work. Hooks execute trusted extension code in-process. No Bannerlord/Modding Kit session or ZIP generation occurred; version 25.2.0 and ForgeApi.Version 12 remain unchanged.

## Recoverable hook teardown and self-retiring callbacks — 2026-09-30 (Rev097)

- The built-in `TestEngine` now forwards the optional hook disconnect guard and retains its public parameterless constructor. A direct `ForgeApi.Disconnect()` rejected while `Runtime` is still ticking preserves the published recovery route. Actual `Runtime.Dispose()` / `OnSubModuleUnloaded` closes the pipe in `finally`; pipe-based recovery after unload is not available and that path remains unverified in a live host.
- The hook dispatch lifetime now covers callbacks that revert or dispose their own handle: a request during an active dispatch returns pending without attempting or claiming `Undo`; after dispatches drain, the service rechecks the host gate, then verifies `Undo`, disposes the hook, and releases the target reservation. The current call keeps its own MonoMod trampoline alive through the dispatch `finally`, while later calls use the original target. Any deferred cleanup failure remains visible as a conflict. This does not broaden the host's exact main-menu/game-thread gate or certify arbitrary hook concurrency.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause <nul` built net472/net8.0 with zero warnings/errors, passed Core 395/395 and ForgeWeave 73/73, and ran the serial x64 detour fixture through its BAT/PowerShell host. No test EXE was started directly.
- Documentation now states that the named-pipe SID ACL does not authenticate WPF as the client process; its checkbox/token is the normal confirmation flow, not a same-user process-identity boundary. No live module unload, Bannerlord session, campaign, battle, independent MonoMod audit, or ZIP generation is claimed. Product version 25.2.0 and `ForgeApi.Version` 12 remain unchanged.

## Patch and hook cleanup BAT evidence — 2026-09-30 (Rev098)

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause` completed successfully. The Core and ForgeWeave builds and suites passed with zero build warnings and errors.
- `tests\CalradiaForge.DetourFixture\Run-DetourFixture.bat --no-pause` completed successfully. The fixture build had zero warnings and errors, and the fixture passed via its BAT-hosted path; `DetourFixture.exe` was not launched directly.
- This entry records BAT evidence only for the current patch and hook cleanup changes. It adds no claims beyond the reported commands. Product version remains 25.2.0.

## Integrated patch, hook, and Desktop BAT evidence — 2026-09-30 (Rev099)

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause` exited 0: Core 397/397 and ForgeWeave 73/73 passed; selected `net472` and `net8.0` builds reported zero warnings and errors.
- The nested serial x64 detour fixture BAT passed with unload-inert callbacks and exact `15 → 32 → 15` restoration. `DetourFixture.exe` was not launched directly.
- `tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause` passed Desktop 65/65 and 292 render cases; the build reported zero warnings and errors.
- This entry records only the integrated BAT evidence reported for this validation pass.

## Finalizer, IL Transpiler, and explicit hook confirmation — 2026-10-01 (Rev109)

- Extends the optional hook contract to Finalizer and local `net472` IL transpilers through MonoMod RuntimeDetour 25.3.6. Finalizer preserves the pending exception by default, supports explicitly validated replacement or suppression, and aggregates a Finalizer failure with an earlier failure. IL manipulators remain local callbacks and can run again when MonoMod rebuilds the IL chain; they do not cross SDK, Desktop, or IPC boundaries.
- Closes the console Apply/Revert bypass: `cf.hook_apply` and `cf.hook_revert` now prepare a host-validated preview, while `cf.hook_confirm <apply|revert> <token>` consumes the exact single-use token and revalidates session, main-menu context, game thread, and selected hook snapshots. The WPF/Gauntlet workbench remains ID-only and explicitly confirmed.
- BAT verification passed: clean Release build (0 warnings, 0 errors), Core 404/404, ForgeWeave 73/73, Desktop 65/65, WPF render 294 cases (13,960 ms harness time; 182 render/layout passes), hook utility 29/29 (22 isolated transport cases), Gauntlet structural checks, the serial x64 detour fixture, and all four stateless acceptance checks. The utility transport fixture simulates token rejection and does not verify Runtime/Bannerlord session, screen, or expiry state. Harness timings are not application latency, and serial fixtures do not establish safety while another thread is executing a target during patching.
- Live Bannerlord mutation remains unverified: the attempted main-menu F10 did not display the Forge panel, and the WPF workbench reported the game context as unavailable. No hook Apply/Verify/Revert action was issued; no campaign or battle was opened. The backend remains experimental. Product version stays 25.2.0 and `ForgeApi.Version` is 13.

## Release archive guard and evidence reconciliation — 2026-10-01 (Rev110)

- Hardened distribution staging and auditing: development/test `.bat` and `.ps1` files are excluded from Modules and Source-SDK; only `Desktop/Run-CalradiaForge-Desktop.bat` is allowed in the Desktop archive. Any `node_modules` tree is omitted and rejected by the archive auditor.
- New BAT evidence: Asset Pipeline 20/20; Hook Utility 29 argument checks and 22 isolated transport cases; integrated suite Core 404/404, ForgeWeave 73/73, Desktop 65/65, and WPF 294 render cases. The render console summary was 13,183 ms while the structured JSON recorded 13,187 ms; both record 182 passes and 5,827.1 ms in layout. Rev109's corresponding console/JSON values were 13,960/13,963 ms. These are separate harness readouts, not Desktop interaction latency.
- The transport fixture simulates token rejection and does not test Bannerlord Runtime session, screen, expiry, or menu lifecycle. Live hook mutation remains unverified; no Apply/Verify/Revert was issued and no campaign or battle was opened. Product version remains 25.2.0; `ForgeApi.Version` remains 13.

## Hook revert eligibility, confirmation visibility and IL rebuild recovery — 2026-10-01 (Rev111)

- Console Revert filters inactive registered records before preparing an explicit plan and reports an empty eligible selection without mutation. Desktop exposes hidden selection counts and wraps its bounded confirmation preview. Applied IL reconstruction is activation-scoped; off-menu management stays blocked. The public read-only HarmonyDiagnostics compatibility API is restored.
- BAT validation passed Core 405/405 and ForgeWeave 73/73 with clean selected builds; Desktop 65/65 and WPF 294 render cases, 182 layout passes, console harness 14,410 ms; isolated x64 serial fixture includes IL rebuild 11 → 15 → 11. Harness timing is not application latency and serial fixtures do not certify general concurrency. Live Bannerlord/Resource Browser mutation remains unverified; no campaign or battle validation. Final ZIPs and hashes will be regenerated after Rev111. Product 25.2.0 and SDK API 13 remain unchanged.

## IL reconstruction after callback shutdown — 2026-10-01 (Rev112)

- The exact retained, explicitly authorized IL activation can reconstruct its deterministic transformation after StopCallbacksForUnload when an external ILHook rebuilds the chain, even with a false host gate. Runtime gameplay callbacks remain stopped; new Apply/Revert and CanDisconnect remain denied outside the approved context.
- The x64 serial fixture BAT passed with 0 warnings/errors: retained reconstruction 11 → 15 → 11 during external ILHook add/undo, approved cleanup restored 3, and uncertain Undo stayed retained/blocked. This does not certify general concurrency or live Bannerlord behavior. Product 25.2.0 and SDK API 13 remain unchanged; final ZIP evidence is not asserted here.

## Deterministic Harmony patch inventory — 2026-10-01 (Rev113)

- Normalize Harmony owner snapshots by case-insensitive deduplication and ordinal sorting, and resolve the explicit `Transpilers` and `Finalizers` metadata properties. The regression now covers deterministic owners and all four patch kinds.
- Core/ForgeWeave BAT passed 405/405 and 73/73; isolated x64 fixture passed. Canonical packaging succeeded, auditing all three ZIPs and matching their SHA-256 manifest. Desktop passed 65/65; WPF render passed 294 cases / 182 layout passes (13,978 ms harness time, not app latency). Product 25.2.0 and API 13 are unchanged. Deep TPAC parsing and live Resource Browser/Bannerlord rendering remain unverified.


# Rev114 — Scoped hook delivery validation — 2026-10-01

The Finalizer/ILHook objective was validated from the staged Git tree in an isolated local snapshot, excluding concurrent SDK/onboarding changes. The snapshot build passed with zero warnings and errors. BAT validation passed Core 405/405, ForgeWeave 73/73, Desktop 65/65, 294 WPF cases with 182 layout passes, Asset Pipeline 20/20, stateless acceptance 4/4 and Gauntlet structural checks. Hook Utility passed 29 argument checks and 22 isolated transport cases. The WPF harness took 14,026 ms; this is not application interaction latency.

A fresh serial BAT benchmark included five samples per scenario: Finalizer median 183.0 ns/call and 136.31 B/call; IL-only median 17.8 ns/call and zero measured allocated bytes. These fixture measurements do not certify concurrent modification safety or live game behavior.

The canonical packaging pipeline generated and audited all three 25.2.0 archives from that scoped snapshot. Independent SHA-256 verification matched its manifest, and the Source-SDK archive excluded concurrent ContentShowcase, HarmonyDiagnostics test-project and SDK evolution additions. Final procedural guides distinguish initial gated application from reconstruction of an already verified owned IL activation; uncertain Undo never authorizes reconstruction.

Live main-menu hook application remains unverified because native-window control is unavailable in the current Computer Use API. No campaign, battle or TaleWorlds target was used. Product version remains 25.2.0, SDK API 13 and MonoMod 25.3.6. Delivery requires a scoped local commit without push; unrelated work remains unstaged.

# Rev116 — Dedicated Hook Workbench routing and responsive WPF states — 2026-10-01

- Moves Hook Workbench from a global header overlay to a dedicated selectable navigation route with mutually exclusive workspace surfaces, an active brass indicator, and route-aware status. Preserves the 194 tool routes, existing commands, 13 locales, and three themes.
- Closing Hook Workbench reconciles the remembered route with active rail filters; when no route is visible it clears the unreachable selection, and clearing the filter restores an accessible route.
- Fixes the clipped Connect action in the compact session header and replaces the generic Spanish-only diagnostic action label with the existing localized work-order resource. The render checks verify the complete localized Connect label and keyboard target, plus Hook Workbench layout, localized route status, and hit testing across all locales and themes at minimum and normal viewports.
- BAT validation: clean build with zero warnings/errors, Desktop 65/65, 295 WPF render cases and 308 layout/render passes. The harness uses fixed WPF DPI and does not emulate Windows display scaling; native keyboard input and live WPF interaction remain unverified. Product version remains 25.2.0; no game session, campaign, or battle was started.

## Validation follow-up — 2026-10-02 (protected ledger Rev115)

- Final review found that `TestEngine.RegisterTranspiler` bypassed the case-insensitive ID registry used by commands. Transpiler registration now reserves IDs under the shared registry lock and rolls the reservation back when registration fails. A regression covers collisions in both registration orders and rollback after invalid metadata.
- Gauntlet now notifies derived Hook Workbench and Patch Preflight bindings when navigation or evidence focus changes. A structural regression confirms the workbench remains inside the Patch Preflight action host and leaves the section rail, evidence ledger, and regular action route intact.
- Package outputs can be directed only to ignored `artifacts/` subdirectories; the pipeline refuses to overwrite existing ZIPs, manifests, or audits and no longer deletes prior package files. Cleanup is limited to the current run's staging directories; access-denied cleanup is reported and retained under ignored artifacts.
- Full BAT packaging validation passed: Core 414/414, ForgeWeave 73/73, Desktop 65/65, and WPF 295 render cases with 308 layout passes (14,552 ms harness time). Release and DocFX builds had zero warnings and errors; stateless acceptance passed 4/4. The archive audit verified all three 25.2.0 ZIPs and independent SHA-256 comparisons matched the generated manifest.
- Bannerlord was not launched. Live Hook Workbench behavior and applying the registered fixture hook at the main menu remain unverified; no campaign or battle was opened. Product version 25.2.0 and SDK API 13 are unchanged.
