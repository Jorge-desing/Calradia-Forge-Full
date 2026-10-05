---
name: calradia-forge-desktop
description: Work on the standalone Calradia Forge .NET 8 WPF desktop workbench, including its MVVM routes, local analyzers, themes, localization, IPC, and validation. Use bannerlord-gauntlet-ui for the in-game Gauntlet panel.
---

# Calradia Forge Desktop

Use this skill for `src/CalradiaForge.Desktop/`, its WPF presentation, and its desktop-specific tests. Read [Desktop documentation](../../../docs/DESKTOP.md), [system design](../../../docs/SYSTEM_DESIGN.md), and the relevant source before changing behavior; these describe the current contracts and evidence boundaries.

## Product and UI boundary

- Desktop is an optional .NET 8 WPF application distributed separately from the Bannerlord module. Keep its assemblies and WPF dependencies out of `Modules`; Desktop does not load Gauntlet prefabs. The game module and its public Gauntlet extension API are separate surfaces.
- Keep presentation state and commands in MVVM view models and render routed pages through the existing catalog and XAML `ContentControl`/`DataTemplate` flow. Do not add visual-tree traversal to change controls. Dispose inactive pages so running work is cancelled and page evidence is released.
- Preserve the bounded, independently scrolling navigation history, visible keyboard focus, theme-aware contrast, and empty/loading/error states when changing layouts.
- **First-Class Graphical Visualizers (No Headless Fallback):** Features designated as visual workbench tools, analyzers, or simulators must render real, interactive XAML graphical controls rather than raw ASCII/text strings in a generic TextBox. The workbench provides 8 specialized domain visualizers + 1 generic operation flight deck:
  1. *Troop Progression Tree (`TroopTreeVisualizer`)*: Hierarchical DAG cards across tiers T1–T6 with combat stats and upgrade arrows.
  2. *Tactical Audio Studio (`AudioFmodMixerInspector`)*: Oscilloscope waveform trajectory, 5-band EQ bars, and dual stereo VU peak meters.
  3. *Workshop Enterprise Simulator (`WorkshopEnterpriseSimulator`)*: Comparative horizontal profit bars, payback countdown gauges, and rebellion index.
  4. *CoALA Cognitive Memory Inspector (`SaveInspector`)*: Working memory card, episodic FIFO timeline, semantic facts with TTL bars, and 2,048-slot gauge.
  5. *PE Assembly & CLR Security Radar (`SaveTypeDefinerAuditor`)*: CLR metadata cards and 5 security compliance bars (Anti-Shadowing, Statelessness, Base ID, MBGUID, Assembly Metadata).
  6. *Module Topology & Dependency DAG (`ModConflictMatrix`)*: Upstream native dependencies, topological load order cards, and conflict hazard scoring.
  7. *Kingdom Geopolitical Diplomacy Barometer (`DiplomaticMatrix`)*: Bilateral realm tension meters, casus belli gauges, and senate voting consensus breakdown.
  8. *Component Blueprint Synthesis Studio (`SoundXmlSynthesizer`)*: 3-stage synthesis pipeline, entity attribute badges, and XML schema validation cards.
  9. *Operation Flight Deck (`GenericOperationDashboardViewModel`)*: Structured operational telemetry, input payload inspector, and execution safety indicators.
  Visual tools must auto-populate default demo models on route selection so they are immediately visible.
- **Operation-Level Aesthetic Customization & Identity Surfaces (Rev032+):** Every tool route in the 194-tool catalog projects a distinct tactical aesthetic identity:
  - *Tactical Category Banners & Mottos:* Displays domain banners (e.g. `[ CLR ASSEMBLY & PE METADATA AUDITOR ]`, `[ GEOPOLITICAL DIPLOMACY & SENATE ]`) and Latin military mottos (e.g. *"Integritas Codicis et Fides Salutis"*, *"Si Vis Pacem, Para Bellum"*).
  - *Dynamic Category Accent Brushes:* Tool card borders, banners, and vector glyphs bind dynamically to category palette brushes (`VerdigrisBrush`, `BrassBrush`, `DeepPineBrush`, `EmberBrush`, `TemperedSteelBrush`).
  - *Input Domain Badges & Security Pills:* Parameter decks feature explicit domain badges (`PE/CLR BINARY`, `MODULE DIRECTORY`, `XML FRAGMENT`) and security pills (`READ-ONLY / ZERO CLR EXECUTION`, `GUARDED MUTATION / ATOMIC BACKUP`).
- **Contextual Operational Action Bars & Aesthetic Differentiation (Rev031+):** Replace generic action bars ("Run work order") with domain-specific action buttons (`PrimaryActionLabel`, `PrimaryActionIconKey`, `AccentBrushKey`). Provide domain-specific quick-actions (`PresetActionButton` to cycle canonical scenarios/branches and `ClearInputButton` to clear parameters). Always preserve underlying MVVM commands (`RunCommand`, `CancelCommand`, etc.) and `AutomationId` values (`RunWorkOrderButton`, `BrowseFileButton`, etc.).
- **Conditional Input Bar Visibility & Tactical Command Dossier (Rev033+):**
  - Collapse `ActiveToolInput` text box and file browse controls when `Tool.RequiresInput == false`, showing an autonomous route status badge and leaving the workspace uncluttered for visual studios and primary actions.
  - Every tool route renders a dedicated `SectionDossierCard` exposing domain context, in-game console commands (`cf.*`, `campaign.*`) with copy-to-clipboard support, workbench hotkeys (`Ctrl+Enter`, `Ctrl+D`, `Ctrl+P`, `Ctrl+E`), and CLI syntax (`CalradiaForge.Desktop.exe --tool <Id>`).
- **Virtualized Rail Navigation & Bounded Viewports:** The navigation rail uses `VirtualizingStackPanel` with container recycling (`VirtualizationMode="Recycling"`) and cached search text matching. Pinned tools and Recent tools maintain separate, decoupled viewports from the main catalog. Recent history is strictly bounded (capped at 12 items) to eliminate layout thrashing.
- The Desktop declarative page and command metadata is an internal WPF catalog. Do not confuse it with the public `[ForgeUiPage]`/`[ForgeUiCommand]` Gauntlet extension contract.

## Resources and evidence

- Themes are Forge-owned WPF dictionaries: **War Table**, **Parchment Light**, and **High Contrast**. Use dynamic resources and keep resource keys aligned across palettes; a theme change must not discard the active route, work order, or retained evidence.
- High Contrast may use illustrated textures as separate, passive, low-opacity overlays when text and controls remain legible. Keep surface brushes solid and high-contrast, and retain a solid fallback if a texture reduces legibility. Test alpha/opacity compositing against the actual theme surface: keep foreground text at or above 4.5:1; for the navigation-rail art, require at least 1.5:1 decoration-to-surface contrast so it remains visible but subordinate. Overlays must not intercept hit testing or keyboard focus.
- Give the decorative header separator an explicit `Height="2"` (2 device-independent pixels); without an explicit height it does not render reliably.
- Specialized styles in `App.xaml` MUST declare explicit keys (e.g. `x:Key="RailFooterSummaryCard"`, `x:Key="ToolPageSummaryCard"`). Never declare multiple implicit (unkeyed) `<Style TargetType="...">` entries for the same type, as this breaks automated resource auditing (`AdvancedToolsTests`).
- Desktop supports thirteen language catalogs. Add user-facing keys to the authoritative English catalog and keep parity in all supported dictionaries. Preserve source code, identifiers, paths, JSON, and raw technical evidence without translation.
- Theme and language preferences are persisted atomically under `%LocalAppData%\CalradiaForge`; invalid or unknown values fall back safely. Keep preference persistence limited to the supported identifiers.
- Local analyzers are bounded and should report analyzer identity, provenance, evidence, limits, and a recommendation. Missing or unsupported inputs remain **Not run** or **Unsupported**; structural evidence is not an import, compatibility, or runtime verdict. Forward cancellation through traversal and parsing.
- The local named-pipe client is the Desktop-to-game transport. Keep it asynchronous and cancellable; it is not a network service and must not expose arbitrary game objects or callbacks.

## Assembly tools

- The Desktop inspector uses AsmResolver to parse PE/.NET metadata as data. Never load or execute the analyzed assembly.
- Version editing is a narrow, opt-in copy workflow: preview without writes, then apply only to a new output with a verified backup, input/output SHA-256, temporary-write cleanup, and output revalidation. Do not overwrite the source; reject signed and mixed-mode assemblies. This is not arbitrary IL editing, hot reload, a compatibility verdict, or a security scanner.
- Keep the Desktop assembly workbench distinct from the in-game Assembly Workbench and from `bannerlord-fbx-importer`'s experimental UI automation.

## Validation and distribution

- Run desktop tests through `tools/Run-CalradiaForge-Desktop-Tests.bat` and WPF resource/layout checks through `tools/Run-CalradiaForge-Desktop-Render-Tests.bat`; use the master `tools/Run-CalradiaForge-Tests.bat` when the task requires the complete short suite. Do not launch test `.exe` or `.dll` files directly.
- Current verification record: the build and BAT suites passed Desktop 51/51 and WPF render/resource checks with 273 cases and 150 layout passes. Window-level UI Automation and live shell inspection remain pending. These results do not establish asset import or rendering inside Bannerlord.
- Preserve the render suite's coverage contract: check route, icon, binding, and selection coverage for all 194 catalog routes; exercise all thirteen language catalogs at 100%, 125%, 150%, and 200%; and lay out representative tool templates under all three themes at all four scales. Do not reduce this to a route count or remove the empty, long-text, evidence, selector, or keyboard-navigation cases.
- For before/after render comparisons, run five independent invocations on the same machine and under the same host conditions, with a unique JSON output path per run. The launcher accepts the path after its switches, for example `tools/Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause artifacts/desktop-render-rev032-baseline-20260925-081912-01.json`; never overwrite a prior run. Compare the median of five completed, passing runs and keep cold-start and repeated/warm measurements in separate groups. Retain failed JSON reports, but exclude partial runs from timing summaries. The Rev032 baseline set includes a run that stopped after four layout passes on a synthetic clear-search focus assertion; its elapsed time is not a completed benchmark sample.
- Read timing fields by scope. `phaseTimings` are timings of phases in the in-process render harness; `performance.renderLayoutMilliseconds` sums only the synchronous `UpdateLayout` calls reported by its layout counter. The Desktop shell's synchronous operation observations are ViewModel timings, not full WPF interaction timings. These values help find harness or ViewModel hotspots but do not measure end-user application latency, a complete cold launch, or Bannerlord performance. Keep direct `Measure`/`Arrange` and synchronous layout checks; do not add unbounded `Dispatcher.PushFrame` or `ApplicationIdle` drains to each layout pass.
- Treat keyboard-focus checks separately from off-screen rendering. The render host is created with `ShowActivated=false` and placed off-screen, so a synthetic focus failure can reflect host activation or dispatcher timing. Before testing real keyboard focus, activate the host on its owning STA and require a bounded wait to confirm `window.IsActive`; then pump only the dispatcher work needed for the focus/binding transition, assert the control's `IsKeyboardFocused` precondition, execute the interaction, and wait for the requested target focus. Keep the final `IsKeyboardFocused` assertion—do not replace it with logical `IsFocused` or `FocusManager` state. If the host cannot become active, preserve the failure as an invalid harness interaction rather than reporting a product pass or counting its partial timing.
- The render suite exercises actual template instantiation and layout. This is not a human visual clipping approval, a live Bannerlord check, or a game-performance measurement. Consult current run artifacts instead of repeating historical pass counts as current results.
- External window-level UI Automation is not covered by the in-process render suite. For a bounded, read-only smoke check of the native WPF window, follow [calradia-forge-ui-automation](../calradia-forge-ui-automation/SKILL.md). Keep it separate from render tests and never use it to execute work orders or commands that analyze or mutate user files. Do not use Playwright or Puppeteer for this native WPF surface.
- The Desktop package is separate and intentionally uses a `.bat` launcher rather than an app-host `.exe`. Keep packaged files from one release together and extract the application outside the game directory.
- A Desktop sprite/TPAC finding does not demonstrate that Resource Browser imported an atlas or that Gauntlet rendered it. For that workflow, use [bannerlord-resource-browser](../bannerlord-resource-browser/SKILL.md); for Gauntlet layout and texture authoring, use [bannerlord-gauntlet-ui](../bannerlord-gauntlet-ui/SKILL.md).


## High-Performance Presentation & Desktop Engine Conventions (Rev034–Rev037)

- **Real-Time Palette & Search Filtering:**
  - In `FilterPaletteTools`, hoist prefix resolution (commands `>`, `:`) outside the tools iteration loop. Resolve reusable static predicates (`static item => ...`) once before iterating over the 194-tool catalog to eliminate thousands of redundant string comparisons per keystroke.
- **Canonical ViewModels for Operation Flight Decks:**
  - Tools without dedicated visual studios route to a shared canonical instance (`GenericOperationDashboardViewModel.CanonicalInstance`), adopting copy-on-write semantics only if the user interacts with custom scenario cycling.
- **Precomputed Group Glyphs & Rail ViewModels:**
  - Navigation group icon keys (`GroupIconKey`) must be precomputed in the `ToolGroupViewModel` constructor rather than resolved via dynamic getters.
- **Node Hierarchy & DAG Precomputation:**
  - Hierarchical node trees (e.g. troop progression trees, module dependency graphs) must be instantiated in static constructors and shared across ViewModel instances.
- **Derived Format Precomputation in List Items:**
  - Properties in secondary item models formatting text (`StatSummary`, `TensionText`) or layout dimensions (`BarWidth`) must be precalculated in constructors and exposed via `{ get; }` to eliminate string interpolation and floating-point math during continuous WPF binding passes.
- **Measured Converter Caching:**
  - Reuse cached geometry or brush resources only where repeated binding work is measured and the resource remains valid for the active theme. Freeze a `Freezable` only when it is safe to share as immutable. Invalidate theme-dependent entries when the palette changes and verify that cached values follow the selected theme. Converter caching and `Freezable.Freeze()` can reduce repeated lookup or mutation work, but they do not guarantee zero allocations for a binding, layout, render, or complete user interaction; measure that full path before making performance claims.

## Command Lifecycle & Module DAG Topology Conventions (Rev071+)

- **Command Initialization Order in ViewModels:**
  - Any RelayCommand whose execution state is signaled by property setters (e.g. SelectedModule) must be instantiated in the constructor BEFORE any method call (e.g. ResetLoadOrder()) that updates those properties.
  - Setters must invoke Command?.NotifyCanExecuteChanged() using null-conditional propagation.
- **Interactive Module DAG Topology:**
  - Module topology visualizers (ModuleHierarchyDashboardViewModel) must support in-memory reordering (MoveModuleUp, MoveModuleDown, ResetLoadOrder) and heuristic conflict warnings (LoadOrderWarning) without mutating game files.
- **Real-Time IPC Telemetry in Footer:**
  - The telemetry ping control in WorkbenchFooterControl.xaml must remain within a compact horizontal stack in Column 0, preserving the central shortcut hint's margins and satisfying AssertFooterFitsShellViewport.

## Tactical Studio Tripartite Composition & Invariant Guidelines (Rev101/Rev135)
- **Tripartite Studio Architecture:**
  - All 12 tactical studio routes in `ToolPageTemplates.xaml` implement the tripartite layout pattern:
    1. Dual-medallion header (domain emblem + sovereign tactical crest in 512x512 POT).
    2. KPI metric cards with 3px bottom accent borders (`BorderThickness="1,1,1,3"`) and 4px proportional progress bars (`Height="4"` with `Mode=OneWay`).
    3. Monospace dossier console cards in Consolas with `$ ` prompt, `{Binding ...CuratedConsoleCommands}` and `CopyTextCommand`.
- **Desktop Template Invariants:**
  - Strictly enforce Rule C: exactly 9 `DashboardTemplate` instances in `ToolPageTemplates.xaml`.
  - Strictly enforce Propuesta 48: zero `TwoWay` bindings in `Run.Text` elements.
  - Test coverage: enforce structural validation via `Rev101TacticalStudiosFullCompositionOverhaul()` in `DesktopSimulationServiceTests.cs`.

## Verified hook and delivery lessons

For Finalizer/ILHook boundaries, confirmation selection, serial measurement and packaging from a scoped snapshot, read [hook delivery lessons](../calradia-forge-dev-workflow/references/hook-delivery-lessons.md). Recheck current source and preserve the distinction between passing fixtures and pending live main-menu validation.

## Verified navigation and connection accessibility (Rev141)

- Keep the role-cycle hint and favorites-only label localized in all thirteen resource catalogs. The favorites filter should expose explicit localized `AutomationProperties.Name` and `HelpText`; do not assume a nested icon/text layout yields a stable UIA name.
- When a toggle changes state, update its localized UI Automation name and help/tooltip alongside its visible label. Test both inactive and active states through the rendered control; static resource presence alone does not prove state announcements.
- The footer ping control keeps its existing `AutomationId`, command, and compact layout while exposing localized automation `Name` and `HelpText`. Extend both structural and rendered-resource checks when these bindings change.
- Resource parity, a render harness, and UI Automation are separate evidence: do not claim live WPF observation from resource-key or template tests alone.
