# Calradia Forge Desktop 25.2.0 source guide

This guide describes the Calradia Forge Desktop 25.2.0 source tree: a standalone MVVM tactical workbench using CommunityToolkit.Mvvm, MaterialDesignThemes, and AsmResolver. The existing CalradiaForge-Desktop-25.2.0.zip is an earlier packaged snapshot and remains unchanged by the Rev066 source work; rebuild and package the source before expecting these changes in a distribution archive. Keep each extracted Desktop release together and do not combine assemblies from different releases.

The optional desktop workbench is distributed separately from the game mod. The Desktop archive contains managed assemblies, runtime configuration, documentation and a batch launcher; it has no app-host `.exe`.

1. Install the .NET 8 Desktop Runtime for Windows.
2. Extract `CalradiaForge-Desktop-25.2.0.zip` outside Bannerlord's folder. The archive remains the unchanged packaged snapshot noted above.
3. Double-click `Desktop\Run-CalradiaForge-Desktop.bat`, or run `dotnet Desktop\CalradiaForge.Desktop.dll` from the extracted folder.
4. Select a local file or folder before using an audit command. The workbench reports **Not run** or **Unsupported** when an input cannot be analyzed.

The tactical workbench uses a coal, deep-pine, tempered-green, brass, verdigris, and ember palette. Its command seal uses a shared mathematical center for the vertical axis, horizontal axis, and marker. Section rules sit below their headings and do not cross controls or labels.

Supported analysis is local and bounded: modules/dependencies/DLLs/XML, archives, localization, Gauntlet prefabs, C# source rules, asset XML, sprite-atlas-to-TPAC readiness, watchdog logs, crash-report metadata, and ASCII FBX declarations. **FBX ASCII Preflight** lists model/geometry/material declaration names, surfaces LOD naming gaps, and records declared Camera/Light nodes as review-only evidence; for those scene objects it recommends checking whether the export was intentionally limited to selected objects. The analyzer does not infer intent or delete anything. The Desktop cancellation token reaches directory traversal and line parsing; the console BAT can be interrupted from its window. Directory scans use an explicit traversal, skip reparse points at the selected root and below it, and honor cancellation between directories and entries. Each scan is limited to 10,000 directories including the root, depth 64, 200,000 filesystem entries, and the requested `MaximumFiles` clamped to 1–2,000 (default 1,000). If a bound is reached, a path is unreadable, or a reparse point is skipped, results are marked truncated with an `analysis_scan_incomplete` warning. Archive analysis checks file size against `MaximumBytesPerFile` before creating `ZipArchive` or inspecting directory entries; the default is 4 MiB and the general input ceiling is 16 MiB. Oversized archives yield `archive_size_limit` without inspecting ZIP entries. Accepted archives inspect at most `MaximumFiles` entries and mark omitted entries as truncated. Binary FBX is unsupported; materials are not resolved against game assets, and this command never imports or compiles assets. Its output is structural naming evidence, not an import verdict. The **Gauntlet Sprite Package Auditor** also checks `SpriteData` sheet IDs and dimensions, part references and rectangle bounds, safe part paths, source PNG IHDR dimensions, and source-atlas IHDR dimensions. It checks only the PNG signature and 24-byte header for image dimensions; it does not decode pixels, validate the PNG checksum, or certify visual output. TPAC checks read the 36-byte v2 header, a bounded nonzero declared asset count, and that the table-of-contents range stays inside the file; other versions are marked unsupported without applying v2 offsets. It does not parse entries or texture payloads. A plausible header is limited structural evidence, not proof that the package is valid. Use `tools/Inspect-CalradiaForge-Tpac.bat --validate-only` for the optional `TpacTool.Lib` metadata preflight; a reader failure is reported separately and does not by itself prove file corruption. Results retain analyzer, provenance, source evidence, recommendations, limits, and raw ledger.

### API Deprecation Analysis — Unavailable
The `ApiDeprecationChecker` route remains in the Desktop catalog for stable navigation identity, but deprecation analysis is unavailable until a verified, versioned TaleWorlds API deprecation catalog identifies the exact API version it covers. The route does not classify API use as deprecated or current; do not treat its presence in the catalog or generic analyzer output as compatibility evidence. Re-enable meaningful analysis only after the catalog source and version coverage have been reviewed.

### Diagnostic evidence and ForgeWeave parsing
Core source heuristics mask comments and C# string literals before checking source-only rules; they remain bounded text analysis, not compiler diagnostics. Cancellation is honored while processing individual source and localization files. An unreadable or oversized file produces a localized finding and does not prevent analysis of later files. Desktop's Evidence Ledger displays each finding's rule ID, source location, evidence, and recommendation; evidence export preserves those fields in the legacy detail value. A successful ForgeWeave transport is reported as `Unparsed` when its payload is missing, empty, malformed, or lacks required snapshot metadata. The transport remains marked `Received`, the reason is capped at 240 characters, and the payload is retained for review. `Received` and `Unparsed` are not completion or verification claims.

The internal MVVM shell delegates observable state and commands to CommunityToolkit.Mvvm through a compatibility surface, owns visible failures, cancellation, and the selected tool. MaterialDesignThemes supplies presentation resources; local WPF geometries keep icons font-independent. AsmResolver also powers the Desktop's offline assembly workbench. Game-icons.net source paths are embedded as WPF geometries with attribution in `THIRD_PARTY_NOTICES.md`; the application never fetches artwork at runtime. A reviewed tool catalog defines each navigation route, input, and mutation requirement. The active work order is rendered through a `ContentControl` data template.

The game's public declarative Gauntlet extension SDK is separate from this WPF Desktop catalog. See [Gauntlet UI extensions](GAME_UI_EXTENSIONS.md) for `[ForgeUiPage]`, `[ForgeUiCommand]`, owner-prefab validation, and game-thread behavior. Desktop keeps its internal MVVM route and does not load extension Gauntlet prefabs.

Pinned and Recent are separate bounded viewports. Recent keeps at most twelve entries and exposes its own vertical scrollbar, count and empty state, so the operational rail always leaves room for the full tool list. The small recent-history mark is a dependency-free vector geometry with attribution in `THIRD_PARTY_NOTICES.md`.

The language selector resolves all thirteen Bannerlord desktop languages through swappable `ResourceDictionary` files. The theme selector swaps three Forge-owned dictionaries: War Table (default), Parchment Light, and High Contrast. Theme and language identifiers are persisted atomically in `%LocalAppData%\CalradiaForge\desktop-preferences.json`; malformed preferences reset to safe defaults. English is authoritative; raw code, identifiers, file paths, and evidence remain unchanged. The game connection uses a local named pipe and does not require a network service.

Desktop artwork is packaged only from `Resources/Textures/Optimized`. Run `tools/Prepare-CalradiaForge-Desktop-Textures.bat` to regenerate the local variants and the same BAT with `--check` to verify byte-for-byte determinism and the exact runtime inventory. The tool-summary corner art uses the uncropped 3:1 master at 540×180 pixels for its 270×90-DIP display area at 200% scale; the generator rejects resizes that change the source aspect ratio. Retired variants are removed only from the optimized runtime set; their authoring masters remain available. The title-bar band, tool-card ornament, cartographic board, and rail illustration use high-quality WPF bitmap sampling. Render-harness screenshots and pixel checks do not establish live-window appearance.

The desktop keeps at most 64 short operation measurements. Synchronous shell construction, filter refresh, route selection, theme, and language changes report duration and allocations measured on the owning thread. Asynchronous analysis and pipe operations retain elapsed duration but show allocated bytes as not measured: `GC.GetAllocatedBytesForCurrentThread` cannot attribute work across awaits or thread switches. These samples do not include a complete input-to-frame latency and are not a game-wide performance conclusion. The separate WPF render harness measures its own layout calls.

ForgeWeave Replay Lab is available only for retained in-game Forge events. Desktop sends the selected source sequence, not a caller-provided payload. A replay still needs handler opt-in, exact context matching, and existing writer gates.

### Advanced Assembly & Save System Auditor (AsmResolver)
Desktop routes `SaveTypeDefinerAuditor` and `CampaignNamespaceGuard` accept compiled `.dll` or `.exe` binaries in addition to source trees. Powered by AsmResolver, the static PE auditor inspects managed metadata without loading or executing the image:
- **GEMINI.md Rule A (Anti-Shadowing):** Verifies that no types or namespaces shadow `TaleWorlds.CampaignSystem.Campaign` or `TaleWorlds.Localization`.
- **Rule B (Stateless Behavior Contract):** Audits mod classes inheriting from `CampaignBehaviorBase`, ensuring zero `[SaveableField]` or `[SaveableProperty]` attributes are declared.
- **SaveableTypeDefiner Base ID Safety:** Disassembles constructor IL instructions to verify that base type definer identifiers are $\ge 2,500,000$, eliminating collision hazards with native engine definitions (0–100,000) or third-party mods.
- **Engine Entity Serialization Risk:** Flags fields directly serializing MBGUID entities (`Hero`, `MobileParty`, `Settlement`, `Clan`, `Kingdom`) and recommends StringId decoupling.
- **Distribution Safety Metadata:** Audits required assembly attributes (`AssemblyCompanyAttribute`, `AssemblyProductAttribute`, `AssemblyDescriptionAttribute`, `AssemblyCopyrightAttribute`) to prevent false-positive antivirus quarantines.

### Live ForgeWeave APM Telemetry & Event Mesh Dashboard
Through the named pipe `framework` action, the `GauntletLivePreview` route transforms runtime snapshots into an operational APM dashboard:
- **Circuit Breakers:** Surfaces live circuit state transitions (`[CLOSED]`, `[HALF-OPEN]`, `[OPEN]`) with failure counters and backoff intervals.
- **Latency Percentiles:** Computes and displays P50, P95, and P99 latency percentiles alongside execution counts.
- **Distribution Histograms:** Visualizes execution timing via ASCII bucket histogram bars (`[<1ms | 1-5ms | 5-20ms | >20ms]`).
- **Evidence Ledger Integration:** Projects per-handler operational evidence directly into the interactive ledger.

### Event Simulation & Dispatcher (Live Console)
The `LiveConsole` route automatically normalizes interactive commands into ForgeWeave framework actions (`cf.forgeweave.publish`, `cf.forgeweave.unquarantine`, `cf.forgeweave.replay`, `cf.forgeweave.status`, `cf.forgeweave.handlers`, `cf.forgeweave.journal`, and `cf.forgeweave.clear`). Modders can dispatch custom event payloads or reset quarantined handlers directly from the desktop shell.

### Tactical Command Palette (Ctrl+K)
The keyboard-driven command palette features tactical prefix routing:
- Supported prefixes: `>live` (Live Session), `>diag` (Diagnostics & Safety), `>asset` (Assets & Synthesizers), `>sim` (Simulation & Balance), `>econ` (Economy), `>pol` (Politics), `>camp` (Campaign), `>comb` (Combat), `>gaunt` (Gauntlet), `>deliv` (Delivery), `>gen` (Generators), `>rep` (Reports), and `>asm` (Assembly Editor & Inspector).
- Pressing `Enter` instantly activates the first matching tool and closes the palette overlay.
- Selecting any tool automatically dismisses the palette overlay.

### Interactive Graphical Simulation Studios & Split Deck (Rev030)
The tactical workbench upgrades all simulation and diagnostic engines from raw headless text into first-class, immediately interactive WPF graphical visualizers:
1. **Interactive Troop Progression Tree (`TroopTreeVisualizer` / `ItemBalanceAnalyzer`):**
   - Renders canonical Imperial & Noble troop trees as hierarchical node cards across 6 tiers (T1–T6) with directional upgrade path arrows.
   - Displays combat attribute cards (HP, Armor, Speed, Wage, Role) with colored tier badges and dynamic stat meters.
   - Provides instant demo pre-population on tool selection and automated semantic XML diff (`fileA.xml|fileB.xml`) detecting added, removed, and mutated attributes.
2. **Interactive Tactical Audio Studio & Waveform Oscilloscope (`AudioFmodMixerInspector` / `SoundXmlSynthesizer`):**
   - Live acoustic visualizer featuring an animated waveform oscilloscope canvas (`Polyline` trajectory across dynamic audio samples).
   - 5-band frequency equalizer bars (`Sub-Bass`, `Bass`, `Mid`, `Presence`, `Brilliance`) with decibel readouts and visual level meters.
   - Dual stereo VU peak meters (`L` / `R`) with dynamic headroom warning thresholds (-1.4 dBFS) conforming strictly to `bannerlord_audio_system.md`.
3. **Interactive Workshop Enterprise Simulator (`WorkshopEnterpriseSimulator` / `SettlementCalculator`):**
   - Dynamic comparative horizontal profit bar chart across 7 enterprise types (Silversmith, Smithy, Brewery, Weaver, Wood Workshop, Pottery, Olive Press) with color-coded profit gradients.
   - Capital payback period countdown gauges (e.g. 41.1 days for optimal Silversmith) and input/output equilibrium tracking.
   - Real-time settlement civic stability equilibrium meters and Rebellion Risk Index ($R_{rebellion}$).
4. **Interactive CoALA Agent Cognitive Memory Inspector (`SaveInspector` / `ObjectInspector`):**
   - 3-tier visual architecture cards displaying Working Memory (current task, intention, active formation), Episodic Memory (FIFO historical experience stream with timestamps and emotional salience badges), and Semantic Memory (belief network facts with dynamic TTL expiration progress bars).
   - Global capacity gauge visualizing utilization across the 2,048 maximum agent slot ceiling (`ForgeAgentMemory` SDK).
   - Interactive hero profile selector with instant cognitive state inspection.
5. **Elevated Split Deck Dual-Tool Workspace (`[Ctrl+D]`):**
   - Prominently integrated into the main tactical header bar with an active verdigris indicator pill and shortcut label `[Ctrl+D]`.
   - Side-by-side comparative inspection canvas retaining pinned tool execution output, status, and evidence ledger while navigating the main workbench.
   - Dedicated keyboard toggle (`Ctrl+D`), pin, clear, and close actions with accessible tooltips.

### Contextual Operational Buttons & Tactical Action Deck (Rev031)
The tactical command deck upgrades the operational button bar from a monotonous generic interface into a domain-differentiated, contextual action bar across all 194 tool routes:
- **Dynamic Semantic Verbs & Accents:** Rather than repeating a generic "Run work order" button, each operational route projects a contextual verb, vector icon, and palette accent matching its domain:
  - *Simulations & Balancers (`VerdigrisBrush`):* `"⚡ Simular Progresión"`, `"🔬 Analizar Espectro"`, `"📊 Simular Economía"`, `"🧠 Auditar Memoria"`.
  - *Audits & Security (`BrassBrush`):* `"🛡️ Auditar Ensamblado"`, `"🔍 Verificar Anti-Shadowing"`, `"📋 Validar Manifiesto"`.
  - *Generators & Scaffolding (`DeepPineBrush`):* `"⚙️ Generar Andamiaje"`, `"📜 Sintetizar XML"`, `"🔨 Forjar Componente"`.
  - *Live Sessions & IPC (`EmberBrush`):* `"📡 Consultar Telemetría"`, `"⚡ Despachar Evento"`.
- **Secondary Quick-Action Buttons:**
  - *Canonical Scenario Cycler (`LoadPresetCommand` / `PresetActionButton`):* Present in simulation routes, enabling instantaneous toggling between canonical scenarios (e.g., Imperial Base vs. Dynastic Noble Cataphract, Combat hit vs. UI Fanfare, Marunath vs. Epicrotea workshop economies, and Rhagaea vs. Garios vs. Lucon cognitive memory profiles) without manual file hunting.
  - *Input Text Clearer (`ClearInputCommand` / `ClearInputButton`):* Contextual quick clearer that illuminates when argument or path text is present.
- **Contract & Accessibility Invariants:** Retains all required `AutomationProperties.AutomationId` values (`RunWorkOrderButton`, `CancelWorkOrderButton`, `ExportWorkOrderButton`, `BrowseFileButton`, `BrowseFolderButton`) and MVVM bindings to maintain 100% compatibility with test harnesses and automation.

### Operation-Level Aesthetic Customization & Domain Visualizers (Rev032)
The workbench elevates visual differentiation from generic action buttons to full-surface operational identity across all 194 tool routes:
- **Operation Identity Surfaces:**
  - *Tactical Category Banners & Mottos:* Each catalog tool renders an authoritative domain banner (e.g. `[ CLR ASSEMBLY & PE METADATA AUDITOR ]`, `[ GEOPOLITICAL DIPLOMACY & SENATE ]`, `[ COMPONENT BLUEPRINT & XML SYNTHESIZER ]`) and an authentic Latin military doctrine motto (e.g. *"Integritas Codicis et Fides Salutis"*, *"Si Vis Pacem, Para Bellum"*, *"Ferrum et Forma ad Victoriam"*).
  - *Dynamic Category Palette Accent:* Active tool card borders, decorative banners, and heraldic vector glyphs bind dynamically to category palette brushes (`VerdigrisBrush`, `BrassBrush`, `DeepPineBrush`, `EmberBrush`, `TemperedSteelBrush`).
  - *Input Domain Badges & Security Pills:* The argument deck displays explicit input format badges (`PE/CLR BINARY`, `MODULE DIRECTORY`, `XML FRAGMENT`, `IPC PIPE COMMAND`) and security classification pills (`READ-ONLY / ZERO CLR EXECUTION`, `GUARDED MUTATION / ATOMIC BACKUP`, `AIR-GAPPED STATIC SCAN`).
- **5 New First-Class Graphical Visualizers (Expanding to 8 Specialized Studios + 1 Flight Deck):**
  1. *PE Assembly & CLR Security Radar (`SaveTypeDefinerAuditor` / `CampaignNamespaceGuard` / `AssemblyInspector` / `AssemblyVersionPatchLab`):*
     - Live CLR metadata breakdown cards (Declared Types, Method Table, Assembly References, StringId Resolution Count).
     - 5 compliance gauge bars: Anti-Shadowing compliance, Stateless behavior guarantee, SaveableTypeDefiner base ID ($\ge 2,500,000$), MBGUID StringId decoupling, and Assembly metadata authenticity.
     - 3 canonical assembly audit presets cycling via `PresetActionButton`.
  2. *Module Topology & Dependency Hierarchy DAG (`ModConflictMatrix` / `DependencySorter`):*
     - Interactive topological dependency DAG linking upstream native modules (`Native`, `SandBoxCore`, `SandBox`, `StoryMode`) to active mod nodes with load priority order, version tags, and conflict status badges.
     - Topological metrics: Total Modules, Upstream Dependencies, Conflict Hazard Score, and Load Order Index.
     - 3 module dependency scenario presets cycling via `PresetActionButton`.
  3. *Kingdom Geopolitical Diplomacy & Senate Barometer (`DiplomaticMatrix` / `WarCasusBelliEngine` / `DynasticSuccessionEvaluator` / `SDK_ForgeDiplomacyEngine`):*
     - Bilateral realm comparison cards (Empire vs Vlandia, Battania vs Western Empire, Aserai vs Khuzait, Sturgia vs Northern Empire) with Casus Belli tension meters, tribute balance, and alliance pact statuses.
     - Multi-faction Senate Voting Consensus Gauge (Support %, Opposition %, Abstain %) with tribute flow balance indicators.
     - 3 geopolitical conflict presets cycling via `PresetActionButton`.
  4. *Component Blueprint Synthesis Studio (`SoundXmlSynthesizer` / `TroopXmlSynthesizer` / `ItemXmlSynthesizer` / `WeaponCraftingForge` / `BrushSynthesizer` / `XmlSnippetForge`):*
     - Interactive 3-stage synthesis pipeline: Schema Blueprint Validation, XML Entity Tree Composition, and Engine Manifest Linkage.
     - Structural attribute preview cards with syntax badges (`ID`, `Type`, `Mesh`, `Tier`, `Modifier`, `Flags`).
     - 3 blueprint presets cycling via `PresetActionButton`.
  5. *Operation Flight Deck (`GenericOperationDashboardViewModel`):*
     - For analytical and diagnostic routes, renders structured operational telemetry, input payload inspector, and execution safety indicators.

### Conditional Input Visibility & Tactical Command Dossier (Rev033)
- **Conditional Input Bar Visibility:** In tools and studios where input parameters are not required (`Tool.RequiresInput == false`), the active input text box is collapsed automatically, displaying an autonomous execution badge and keeping the layout focused on action bars and visual studios.
- **Section Dossier & Command Reference:** Every tool route features a dedicated tactical dossier (`SectionDossierCard`) providing:
  1. *Operational Domain Context:* Purpose, engine safety boundaries (stateless, thread-safe, sandbox), and expected data flows.
  2. *In-Game Console Commands:* Associated Bannerlord / Gauntlet console commands (`cf.*`, `campaign.*`) with one-click copy buttons.
  3. *Workbench Hotkeys:* Direct keyboard shortcut references (`Ctrl+Enter`, `Ctrl+D`, `Ctrl+P`, `Ctrl+E`, `Esc`, `Ctrl+F`).
  4. *Headless CLI Invocation:* Exact command line syntax for automation (`CalradiaForge.Desktop.exe --tool <Id>`).

### Measured WPF Workbench Optimization & GC Reduction (Rev034)
- **Zero-Allocation Reactive Collections:** `BatchObservableCollection<T>.ReplaceAll` features a specialized `IReadOnlyList<T>` overload that performs in-place element equality comparison (`EqualityComparer<T>.Default.Equals`), eliminating intermediate array allocations when filter or navigation states remain unchanged.
- **Direct Search & Palette Filtering:** `DesktopShellViewModel.FilterPaletteTools` and `RefreshVisibleTools` replaced LINQ `.Where(...)` and `.Select(...)` chains with pre-sized index-based loops, eliminating iterator allocations and closure allocations during rapid search keystrokes.
- **Operational Rail Pre-Allocation:** `RefreshOperationalRailEntries` pre-computes exact collection capacity based on visible groups and active tool counts, preventing array resize cycles as rail entries are populated.
- **Converter Boxing Elimination:** `PinnedGlyphConverter` evaluates `IList<ToolDefinition>` and `IReadOnlyList<ToolDefinition>` via direct indexer access, eliminating `IEnumerator` interface boxing across all 194 tool routes on every render pass.
- **Simulation Buffer Pre-Sizing:** `DesktopSimulationService` reports (troop trees, CoALA agent memory, audio waveforms, and 30-day economic models) initialize `StringBuilder` buffers at 2,048 characters, avoiding dynamic heap reallocations.
- **Empirical Layout Benchmark:** Synchronous WPF render layout call time reduced from 1,711.5 ms to 1,647.4 ms (-64.1 ms) across 275 test cases, with app startup and first render accelerating from 1,488.6 ms to 1,391.4 ms (-97.2 ms).

### Multi-Layer Optimization & Metadata Precomputation (Rev035)
- **Tool Catalog Metadata Precomputation (`ToolCatalog` & `ToolDefinition`):**
  - All 194 tool routes in `DesktopToolDefinitions` now precompute visual presentation and operational metadata directly inside the `ToolDefinition` constructor (`group`, `iconKey`, `categoryBanner`, `categoryMotto`, `categoryAccentBrushKey`, `inputDomainBadge`, `inputFormatHint`, `securityPillText`, `cliSyntax`, and `consoleCommands`).
  - Eliminated eleven dynamic `.StartsWith()` string checks and ten `switch` expressions on every property read.
  - Replaced ad-hoc string array allocations with static cached arrays (`DefaultHotkeys`, `AudioCommands`, `TroopCommands`, `MemoryCommands`, `EconomyCommands`, `AuditCommands`, `ConflictCommands`, `DiplomacyCommands`, `LiveConsoleCommands`, and group fallback command arrays).
  - Pre-cached unique category array in `ToolCatalog` constructor, replacing redundant LINQ `.Distinct().ToArray()` heap allocations on every category access.
  - Replaced linear array scanning `Array.IndexOf(GroupOrder, group)` in `GroupRank` with an O(1) jump table switch expression.
- **Analysis and Assembly Service Formatting Optimization:**
  - `DesktopAnalysisService.Format`: Pre-sized `StringBuilder` capacity (`Math.Max(512, count * 256)`), chained append operations, direct numeric appending (`output.Append(finding.Line.Value)`), and replaced `foreach` loops with direct indexed `for` loops.
  - `DesktopAssemblyService.Inspect` & `Audit`: Replaced LINQ `.Select().ToArray()` and `string.Join` with pre-allocated 2 KB + length buffers; replaced LINQ method filtering with indexed loops checking `ctor.IsConstructor && ctor.CilMethodBody != null`; eliminated `assembly.CustomAttributes.Select().ToHashSet()` allocations by using a single-pass loop setting boolean flags for distribution metadata.
- **Evidence Presentation Binding Optimization:**
  - `WorkspaceEvidence.Location`: Precomputed string formatting (`FormatLocation()`) in constructor and stored in a read-only property getter (`public string Location { get; }`), eliminating repeated formatting during WPF layout and data-binding passes.
- **Empirical WPF Render and Navigation Benchmark:**
  - Route navigation and filter checks accelerated from 2,145 ms baseline to **1,728.66 ms** (-416.3 ms / **19.4% speedup**).
### Simulation ViewModels & Graphic Simulators Optimization (Rev036)
- **Visual Studio ViewModels Static Caching & Precomputation (`DesktopSimulationViewModels`):**
  - `TroopTreeDashboardViewModel`: Constructed the canonical Imperial hierarchy (12 troop nodes, standard & noble progression DAGs) once in a `static` constructor and assigned cached references (`CanonicalStandardTreeRoots`, `CanonicalNobleTreeRoots`, `CanonicalHighlightedTroops`, `CanonicalDefaultSelected`, `CanonicalCataphract`). Precomputed `TroopNodeViewModel.StatSummary` (`$"HP: {hp} · Wage: {wage}d · Cost: {cost}d"`) in constructor to eliminate repeated string interpolation on every UI binding pass.
  - `AudioStudioDashboardViewModel`: Cached static `DefaultBands` and precomputed 24 acoustic waveform points (`DefaultWaveform`) once in a static constructor, eliminating 29 sub-object allocations per studio initialization.
  - `WorkshopDashboardViewModel`: Precomputed `WorkshopEnterpriseItemViewModel.BarWidth` in constructor (`Math.Max(20, normalized * 220.0)`) and cached static `DefaultEnterprises` array for instant rendering.
  - `AgentMemoryDashboardViewModel`: Cached static `DefaultAgents` (Rhagaea, Derthert, Monchug) and precomputed `MemoryFactItem.HasTtl` in constructor; used `Array.Empty<...>()` fallbacks for all collections.
  - `CodeSecurityDashboardViewModel`: Replaced repeated string array instantiation with static `DefaultMetadataStreams`.
  - `ModuleHierarchyDashboardViewModel`: Pre-allocated `ScenarioNodes` across all 3 scenarios, avoiding collection instantiation on scenario cycling.
  - `KingdomDiplomacyDashboardViewModel`: Precomputed `FactionStanceViewModel.TensionText` and `BarWidth` in constructor and cached static `ScenarioStances` arrays.
  - `ComponentGeneratorDashboardViewModel`: Pre-allocated `ScenarioBlueprints` across all 3 blueprint scenarios.
- **Headless Simulation Engine Precomputation (`DesktopSimulationService`):**
  - Precomputed `CanonicalTroopReport`, `CanonicalTroopEvidence`, `CanonicalAudioReport`, `CanonicalAudioEvidence`, `CanonicalEconomyReport`, and `CanonicalEconomyEvidence` as `static readonly` fields. Default simulation runs now return pre-allocated results with zero heap allocations.
  - Pre-allocated `BarCache` lookup table for 24-character capacity meters, eliminating string allocations during progress rendering.
  - Replaced LINQ queries and dynamic arrays in `InspectAgentMemory` with cached arrays (`SampleLordAgents`, `EpisodicTypes`) and indexed loops.
- **Empirical WPF Render and Layout Benchmark:**
  - Non-visual fixtures and contracts accelerated from 812.42 ms to **691.69 ms** (-15%).
  - App startup and first render accelerated from 1,594.45 ms to **1,460.39 ms** (-8.4%).
### Deep Theme Resource Freezing, Hardware BitmapCache & Advanced Cognitive Consolidation (Rev039)
- **Deep-Freeze of Theme Resources (`DesktopThemeService`):**
  - Implemented recursive resource freezing (`FreezeDictionaryResources`) across all `SolidColorBrush`, `LinearGradientBrush`, `DrawingBrush`, and `Color` entries in active theme dictionaries upon applying themes (`ApplyDefault` and `Apply(themeId)`).
  - Frozen resources eliminate WPF thread-affinity checks, allow direct cross-thread sharing, and eliminate DependencyProperty change notification overhead across thousands of rendered visual elements.
- **Hardware BitmapCache on Static Vector Groups (`MainWindow.xaml`):**
  - Equipped complex vector decoration groups (`WorkbenchCartographicBoardDecoration` grid and `HeaderSealButton` inner decoration) with `<BitmapCache EnableClearType="False" RenderAtScale="1.0" SnapsToDevicePixels="True"/>`.
  - Converts complex geometry rasterization into GPU texture sampling during layout passes, resize, and theme switches.
- **CoALA Memory Salience & Dynamic Consolidation Metrics:**
  - `AgentProfileViewModel`: Added zero-allocation evaluation loop in constructor computing `TopSalientFact`, `ClusterSummary`, and `ConsolidationIndex`.
  - `AgentMemoryDashboardViewModel`: Exposed `ActiveClusterOverview` and `ActiveConsolidationRate` bound directly to the header telemetry card with reactive notifications on agent selection and scenario cycling.
- **Expanded Windows UI Automation Smoke Verification (`calradia-forge-ui-automation`):**
  - Expanded `tools/Test-CalradiaForge-Desktop-Uia.ps1` to 29 checks across 24+ shell nodes.
  - Added dynamic non-mutating `SelectionItemPattern.Select()` tab navigation verification (`WorkbenchRawResultTab` -> verifies dynamic materialization of `RawResultText` -> restores `WorkbenchEvidenceTab`).
  - Added non-mutating `InvokePattern.Invoke()` check on `SplitDeckToggleButton` (toggles open, verifies visual state, toggles closed).
  - 29/29 checks passed in 14,294 ms (0 failures, 0 warnings); report exported to `artifacts/desktop-uia-smoke.json`.
- **Empirical WPF Render and Layout Benchmark:**
  - Layout call duration dropped to **1,265.9 ms - 1,468.4 ms** (down from 1,647.4 ms in Rev034 and 1,581 ms in previous runs).
  - Total WPF test execution time dropped to **9,226 ms** across all 275 render cases.
  - Complete solution test suite: 726 passed, 0 failed.

### Pure Graphic Engine, Container Virtualization & Composition Optimization (Rev040 - v25.2.0)
- **Strict UI Virtualization & Smooth Pixel Scrolling (`MainWindow.xaml`):**
  - Configured `VirtualizingPanel.ScrollUnit="Pixel"`, `VirtualizingPanel.VirtualizationMode="Recycling"`, and `VirtualizingPanel.CacheLength="1,1" VirtualizingPanel.CacheLengthUnit="Page"` on `OperationalRailToolViewport` (194 tool entries) and `CommandPaletteToolList`.
  - Converted `EvidenceLedgerItems` to full container virtualization with `ScrollViewer.CanContentScroll="True"` and `VirtualizingStackPanel`, eliminating visual-tree node allocation when hundreds of finding records are rendered.
  - Enabled offscreen pre-caching (1 page margin above and below) for butter-smooth 60fps scrolling without hitching or GC pressure.
- **DirectX Composition & EdgeMode Aliased Optimization:**
  - Applied `RenderOptions.EdgeMode="Aliased"` and `SnapsToDevicePixels="True"` to 1px tactical divider rules, `TitleBarBorder`, `MainCommandHeaderBand`, and report tabs, eliminating subpixel blur and GPU rasterizer overhead on high-DPI displays.
  - Pre-freezing cached dynamic brushes in `BrushKeyConverter` and recursive freezing of root `Application.Current.Resources` in `DesktopThemeService.ApplyDefault()`.
- **Empirical WPF Render and Layout Benchmark:**
  - Layout call duration dropped to **1,229.5 ms** (down from 1,265.9 - 1,468.4 ms in Rev039 and ~1,750 ms in Rev036).
  - Total test suite: 726 passed with 100% success; Windows UI Automation 29/29 checks passed in 29,365 ms.
  - Minor version 25.2.0 synchronized across solution properties, manifests, and test assertions; packaged in `artifacts/` with SHA-256 digests.

### Narrow-window shell corrections and evidence boundaries (Rev056)
- The navigation rail now has a 220-DIP minimum width, capped at 276 DIP, while preserving a 400-DIP minimum for the work area. The Split Deck is narrowed to 280 DIP so its side-by-side layout fits the minimum supported window more reliably.
- The cartographic card frame is 160×90 DIP, matching the 16:9 proportion of its 448×252 source image. `Stretch="Uniform"` keeps the art uncropped and undistorted. Fixed-scale `BitmapCache` layers were removed from the passive map and seal decorations so WPF does not enlarge a pre-rasterized low-resolution cache.
- Fixed chrome labels, help text, and Split Deck empty/output labels resolve through the active resource dictionary. The 13 language dictionaries retain 101 matching resource keys. Clearing pinned content raises the derived title/category notifications after evidence is cleared, preventing stale labels.
- Five serial render-harness runs passed 275 cases each, with 152 layout passes per run. The median total harness time was 11,335 ms versus 13,652 ms at baseline (-17.0%); median layout-call time was 2,075 ms versus 2,397 ms (-13.5%). Median startup, route/filter, localization, and theme phases were 2,582 ms, 2,042 ms, 2,007 ms, and 3,109 ms; each phase improved against the baseline. These are harness timings, not interactive application latency.
- The harness scale matrix rasterizes previews at larger output DPI; it does not emulate Windows system-DPI layout. Decorative rail artwork remains intentionally collapsed because its source aspect ratio does not fit the available rail frame. Live window/UI Automation appearance remains pending, so these checks do not establish actual desktop rendering at OS scaling settings.

### Tactical Studios Architectural Enrichment & Modder Role Presets (Rev041)
- **Studio Documentation & TaleWorlds Invariants:**
  - All 8 Tactical Studio ViewModels (`TroopTreeDashboardViewModel`, `AudioStudioDashboardViewModel`, `WorkshopDashboardViewModel`, `AgentMemoryDashboardViewModel`, `CodeSecurityDashboardViewModel`, `ModuleHierarchyDashboardViewModel`, `KingdomDiplomacyDashboardViewModel`, and `ComponentGeneratorDashboardViewModel`) now expose rich `StudioDocumentation`, strict `ArchitecturalInvariants`, operational `StudioCaveat`, and `CuratedConsoleCommands`.
  - Invariants document TaleWorlds engine constraints directly within each studio:
    - *TroopTree:* Equipment element slots, tier wage formulas, upgrade tree DAG topology.
    - *AudioStudio:* Lowercase `.ogg` in `ModuleSounds/`, 2D vs 3D mixer categories, 44.1kHz sample rate.
    - *Workshop:* Town prosperity caps, supply/demand elasticity, anti-snowball tariffs.
    - *AgentMemory:* Modulo-24 tick time-slicing, 100% stateless behavior, zero `SaveableTypeDefiner`.
    - *CodeSecurity:* GEMINI Rule A (anti-shadowing), Rule B (statelessness), `SaveableTypeDefiner` IDs $\ge 2,500,000$.
    - *ModuleHierarchy:* Manifest `<Id>` exact folder matching, load order topological sort, dependency cycle rejection.
    - *KingdomDiplomacy:* `KingdomDecision` resolution, war exhaustion indices, bilateral casus belli.
    - *ComponentGenerator:* XML root element schemas, double `SetDialogs()` rule for quests.
- **Curated Executable Console Commands (`StudioConsoleCommand`):**
  - Each studio features a curated list of executable console commands with descriptions for in-game terminal execution (`cf.sim_tactics`, `sound.play`, `cf.quick_state`, `cf.inspect`, `cf.audit`, `cf.modules`, `cf.sim_diplomacy`, `cf.scaffold`).
- **Operational Rail Modder Role Presets (`ModderRolePreset`):**
  - Integrated 5 modder role presets into `DesktopShellViewModel`:
    - `All`: Unfiltered operational rail with all 194 tool routes.
    - `Narrative & Dialogues`: Focuses on quest design, hero dialogues, cognitive memory, localization, and narrative progression.
    - `Troop & Combat Artisan`: Focuses on troop trees, combat tactics, AI formations, weapons, armor, and audio SFX.
    - `Economy & World Architect`: Focuses on settlement economics, trade pricing, workshops, crime alleys, parties, and diplomacy.
    - `Core Dev & Performance`: Focuses on rule compliance, SaveableTypeDefiners, memory GC, ForgeWeave replays, and diagnostics.
  - Cycled dynamically via `CycleModderRoleCommand` with live UI feedback and tooltip hints.

### Tactical Studios Command Expansion, Hybrid Personalization & CoALA Memory Ranking (Rev046)
- **Unified 4-Family Command Expansion:**
  - Expanded `CuratedConsoleCommands` across all 8 Tactical Studio ViewModels to incorporate 6-8 commands per studio spanning all 4 key families:
    - *CoALA Cognitive Memory:* `cf.agent_memory_stats`, `cf.agent_memory_query`, `cf.agent_memory_salience`, `cf.agent_memory_decay`, `cf.agent_memory_cluster`, `cf.agent_memory_export`.
    - *Tactical Simulation & Balance:* `cf.sim_tactics cavalry/infantry/archery`, `cf.siege_tactics`, `cf.sim_economy workshops`, `cf.sim_settlements all`, `cf.sim_dynasty all`, `cf.sim_crime all`, `cf.sim_trade grain`.
    - *Diagnostics & Workflow:* `cf.audit`, `cf.model_audit`, `cf.dump_diagnostics`, `cf.audit_save`, `cf.audit_localization`, `cf.harmony_summary`, `cf.patch_preflight`, `cf.gc_profile`.
    - *Scaffolding & Generation:* `cf.novice_scaffold quest/behavior/troop/item/armor/submodule`, `cf.novice_checklist`, `cf.novice_events all`.
- **Hybrid Personalization & Studio Palettes:**
  - Added studio-level command pinning with toggle glyphs (`★`/`☆`), primary quick-action execution (`QuickActionCommand` / `QuickActionLabel`), and developer scratchpad annotations (`ScratchpadNotes`).
  - CoALA cognitive utility score integration with live visual ranking based on execution recency ($R$), frequency ($F$), and importance/pin status ($I$).

### Tactical Studios Playbooks, Troubleshooting Trees & Procedural Macros (Rev047)
- **Step-by-Step Engineering Playbooks:**
  - All 8 Tactical Studio ViewModels expose structured `PlaybookTitle` and 3 actionable `PlaybookSteps` defining standard engineering recipes (DAG validation, acoustic mix calibration, market elasticity tuning, CoALA memory decay pruning, IL instruction scanning, cycle-free dependency resolution, diplomatic casus belli stabilization, and safe XML scaffolding).
- **Subsystem Troubleshooting Trees & Remedies:**
  - Concrete failure diagnosis headers (`TroubleshootingHeader`) and prescriptive remedies (`TroubleshootingRemedy`) address TaleWorlds runtime failure modes directly within the studio (e.g. DAG cycle crashes, audio mixer category mismatches, hyperinflation cascades, unbounded heap retention, and savegame corruption).
- **Procedural Macro Actions:**
  - Each studio features a specialized composite action (`ProceduralMacroAction`) executing end-to-end diagnostic pipelines with a single click or keyboard trigger.
- **View Density Toggle:**
  - Support for compact versus detailed view density (`IsCompactMode`) allowing modders to toggle between minimal operational palettes and comprehensive technical manuals.

Do not copy Desktop files into `Modules`. The game module works without the desktop workbench.
### Responsive presentation shell and contextual dossier (Rev057)

The WPF window remains the host for native window chrome, top-level keyboard bindings, and the `WorkbenchShellView`. The shell now composes dedicated `WorkbenchHeaderControl`, `WorkbenchStatusControl`, `WorkbenchNavigationControl`, `WorkbenchWorkspaceControl`, `WorkbenchFooterControl`, and `CommandPaletteControl` views. The specialized visualizer templates and route template live in `Resources/Views/ToolPageTemplates.xaml`, and `ToolDossierControl` owns the contextual commands, hotkeys, and CLI reference. ViewModels continue to own state and commands; this presentation split does not add routes or change public SDK, IPC, or game contracts.

The header uses wrapping layout for its settings and actions, while the theme selector keeps long display labels such as `High Contrast` on a single trimmed line. The action `WrapPanel` is constrained to its star-sized column so controls wrap within their allocation instead of widening the shell. `AdaptiveWorkbenchPanel` keeps the current route, Split Deck, and optional dossier side by side only while the visible surfaces' minimum widths fit; otherwise, it stacks them inside the workspace's vertical scroll viewport. The route width is selected from `ToolPageViewModel.MinimumPresentationWidth` (360 DIP for standard pages and 520 DIP for visual dashboards); the secondary minimums are 250 DIP for the deck and 260 DIP for the dossier. The outer shell keeps the 980×680-DIP minimum window, the 220–276-DIP navigation rail, and a 400-DIP minimum work area. These are layout constraints, not a claim that every control has passed visual review at those sizes.

The header's accessible `ContextDossierToggleButton` uses the localized `Ui.DossierHeading` label and binds to `IsContextDossierOpen`. An explicit button style and `ContentPresenter` keep the localized label visible instead of letting an implicit Material Design template hide it; the regression requires at least 80 DIP for the label. The dossier is closed by default, follows the currently selected route, and is presentation state rather than a persisted preference. Closing it leaves the toggle available as the keyboard return point.

Two local optimized WPF resources add a cartographic title band (`titlebar-cartographic-panorama-rev057.png`, 2172×67 pixels, 274,555 bytes) and a restrained card-corner ornament (`dossier-corner-ornament-rev057.png`, 384×256 pixels, 35,618 bytes). The additions total 310,173 compressed bytes and 975,312 decoded RGBA bytes. The full packaged Desktop PNG inventory is 7,465,788 compressed bytes and 15,921,104 decoded RGBA bytes. The textures are passive; hit testing and keyboard focus remain with the controls. High Contrast retains solid surface brushes. Its low-opacity decorations are configured separately from those surfaces, but legibility review for this revision is pending; no live appearance approval is claimed.

The Rev057 pre-change baseline consists of five passing render-BAT reports, each with 275 cases and 152 layout passes; the median was 11,294 ms total and 2,072.6 ms in synchronous layout calls. After the shell changes, five comparable skip-build runs of `tools/Run-CalradiaForge-Tests.bat --desktop-only --skip-build --no-pause --render-output <unique-json-path>` each passed 276 render cases with 161 layout passes. Their total times were 8,360, 8,327, 8,403, 7,659, and 8,542 ms (8,360-ms median, 26.0% below baseline); layout-call times were 1,759, 1,714.2, 1,747.8, 1,601.6, and 1,732.6 ms (1,732.6-ms median, 16.4% below baseline). A Desktop-only BAT build completed with zero warnings/errors and Desktop 56/56; a later report-label BAT run also passed Desktop 56/56 and 276 render cases/161 layout passes (8,014 ms total and 1,632.6 ms in layout calls). The comparisons retain all baseline cases and add coverage; all timings are harness measurements, not Desktop interaction latency.

The bounded read-only launcher `tools/Test-CalradiaForge-Desktop-Uia.bat` passed 20/20 accessibility-tree checks; its report is `artifacts/desktop-uia-rev057-final.json`. It identified the owned main window and shell controls without opening pickers, running work orders, or changing preferences. UI Automation does not provide human visual approval. The preview matrix records `render-language-raster-scale` for 1, 1.25, 1.5, and 2 scale factors; it keeps a 1360×820-DIP logical window with identity `LayoutTransform`, then rasterizes `RenderTargetBitmap` output at the requested scale. The report confirms `windowsSystemDpiChanged=false`; this does not simulate Windows system-DPI layout or change DIP measurements. Texture `--check` passed. High Contrast ornament legibility and visual review at real OS scaling remain unverified. The master `--render-output <path>` flag only selects the JSON report destination; use a distinct path per run. No ZIP was regenerated or changed.

### Responsive search and action sizing follow-up (Rev058)

The rail and command-palette search fields use a 42-DIP height with vertically centered text. The 32×32-DIP clear button has reserved text padding so it does not overlap the query. At the 980×680-DIP minimum window, the command-palette, Split Deck, and dossier header controls have minimum widths of 116, 120, and 126 DIP respectively and a 36-DIP minimum height. Work-order action buttons use a 34-DIP height with bounded widths and wrapping, keeping the actions within the available column. The report-tab and dossier labels retain their explicit visible-content templates. The render harness confirmed the action controls remain in view at the minimum and normal viewport sizes.

Validation used `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` for a clean build (zero warnings/errors), Desktop 56/56, and one build-backed WPF render run (277 cases, 161 layout passes, 9,410 ms). Five subsequent skip-build render runs passed 277 cases and 161 layout passes each. Their JSON `milliseconds` were 8,754, 8,928, 8,281, 8,355, and 8,093 ms (8,355-ms median); `renderLayoutMilliseconds` were 1,685.9, 1,826.3, 1,652.8, 1,709.0, and 1,507.1 ms (1,685.9-ms median). Against the five Rev057 runs (8,360-ms and 1,732.6-ms medians at 276 cases/161 passes), the total median was 0.06% lower and layout median 2.7% lower. This is effectively stable harness performance, not a substantial optimization or an application-latency measurement.

`tools/Test-CalradiaForge-Desktop-Uia.bat` passed 20/20 read-only checks in `artifacts/desktop-uia-rev057-controls-final.json`; it did not manipulate OS scaling. `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check` passed. The packaged PNG inventory is 7,465,788 compressed bytes and 15,921,104 decoded RGBA bytes, including the two Rev057 decorations (310,173 compressed bytes and 975,312 decoded RGBA bytes); the asset check reported deterministic outputs. These results do not establish visual legibility under actual Windows DPI settings or human visual approval. Bannerlord was not launched, and no ZIP or product version changed.

### Visual hierarchy and texture restraint follow-up (Rev059)

The Parchment theme now renders its surface and title textures at opacities of 0.11 and 0.12, down from 0.34 and 0.32, to keep the illustration subordinate to controls and text. The active route in the navigation rail has an observable selected state. The rail search exposes the localized `Ui.SearchHint` hint. The cartographic board reserves a 168-DIP column for a 160×90-DIP frame with an 8-DIP margin, avoiding artwork collisions with the work surface. The empty evidence state uses 42-pixel artwork and a 32-DIP action button to give its call to action a clearer visual scale.

The latest `tools/Run-CalradiaForge-Tests.bat` Desktop run built with zero warnings and errors, passed Desktop 56/56, and passed 277 WPF render cases with 161 layout passes. The render harness reported 7,808 ms total and 1,824.9 ms in layout calls. These are harness measurements, not application interaction latency; they do not demonstrate that the app is faster while open. `tools/Test-CalradiaForge-Desktop-Uia.bat` passed 20/20 bounded, read-only checks in `artifacts/desktop-uia-rev059.json`; UI Automation inspected the accessibility tree and is not visual approval. No live visual approval is claimed.

Product version remains 25.2.0. No public API, route catalog, or workbench behavior was intentionally changed, and no ZIP was regenerated.

### Focus-safe automated render tests (Rev060)

The WPF render harness attaches its dedicated STA render thread to a private Windows desktop before creating WPF objects, HWNDs, or hooks. A render window can become foreground inside that private desktop, but that desktop is separate from the user's interactive desktop. `ForegroundWindowGuard` now only observes those private-desktop events; it never calls `SetForegroundWindow` or attempts to restore focus. The title-bar regression inspects bindings, styles, and handlers without maximizing, minimizing, or restoring a native window.

The opt-in UI Automation smoke runner also has no focus-restoration call. It installs its process-filtered foreground observer before starting the read-only app and fails if that app is observed taking foreground. Its UIA run was not repeated for this revision; only PowerShell parsing and the absence of focus-mutation calls were checked. Do not treat that static check as a UIA pass.

Validation used the hidden `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output <unique-json-path>` launcher twice. Both runs built with zero warnings/errors, passed Desktop 57/57 and render 278/278, and left the interactive foreground HWND at `0x171168` before and after. The final render run reported 158 layout passes, 2,043.3 ms in layout calls, and 9,492 ms total harness time. Its private desktop observer saw the test process become foreground only within the isolated desktop, which is expected; the user's interactive foreground did not change. These values describe the test harness, not application interaction latency.

### Focus-safe Desktop test launcher (Rev061)

When starting the WPF test suite from Explorer, use `wscript.exe tools\Run-CalradiaForge-Desktop-Checks-Hidden.vbs` (or open the VBS file). It runs `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` with a hidden command window and writes a unique log, status file, and render report under `artifacts/desktop-focus-safe/`. The render harness keeps its WPF HWNDs on its private Windows desktop. The separate UIA smoke check is intentionally not part of this silent runner because its BAT starts a PowerShell process; the UIA child itself now requests `CreateNoWindow`/`Hidden` and records foreground transitions. This avoids confusing a command or security-notification window with a WPF test window. The hidden runner was validated while no Calradia Forge WPF window was open, so that run cannot prove foreground preservation with the user's application already focused.

The latest hidden run exited 0: Desktop 59/59 and WPF render 281 cases/158 layout passes, with zero build warnings/errors and 7,912 ms total harness time. The foreground remained the same AvastUI window (`0x580296`) before and after; no Calradia Forge WPF window was open for that run. A combined attempt that also invoked the opt-in UIA BAT produced no UIA report and ended on a different AvastUI HWND; its cause was not established, so UIA stays outside the default hidden test path.

### Responsive route status readability (Rev062)

At the 980×680-DIP minimum window size, the application-status card wraps a long selected route name to a second line instead of hiding its ending. The card still exposes the full value in its tooltip. The Desktop and WPF render BAT suites passed with zero build warnings/errors; render coverage remains 281 cases and 158 layout passes.

### Portrait rail illustration (Rev063)

The navigation rail now uses the local transparent `workbench-heraldic-rail-portrait-rev063.png` illustration, fitted uniformly behind the rail content. The former landscape rail strip is retained as an authoring/history asset but marked as retired by the texture preparation manifest and is no longer embedded by the Desktop project. The image remains passive and follows the decorative-accent setting. Theme opacity tokens are 0.18 for War Table, 0.12 for Parchment Light, and 0.24 for High Contrast; High Contrast surfaces remain solid.

`tools/Prepare-CalradiaForge-Desktop-Textures.bat` regenerated the optimized derivative at 320×640 RGBA (126,258 bytes; SHA-256 `DF8C90E403A3B4D5B8476824B72FEFF006C83023165A4924A4CE084EFF362638`), and its `--check` deterministic verification passed. The packaged inventory is 7,495,858 compressed bytes and 16,473,296 decoded RGBA bytes, within the configured limits. `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts/desktop-visual-rev063-opacity/render.json` built with zero warnings/errors, passed Desktop 59/59 and 281 WPF render cases with 158 layout passes. The render harness took 7,998 ms overall and 1,779.8 ms in layout calls; these are harness measurements, not open-app latency. Rendered screenshots were reviewed; live application appearance and actual Windows system-DPI behavior were not verified. Product version remains 25.2.0; no public contract, route, command, permission, or ZIP changed.

### High-contrast rail artwork refinement (Rev064)

The first portrait rail derivative was too dark to read clearly against the High Contrast surface. It has been superseded by `workbench-heraldic-rail-portrait-rev064.png`, a locally generated transparent line engraving with brighter brass and verdigris edges. The prior Rev063 derivative remains historical and is excluded from the embedded resource manifest. War Table, Parchment Light, and High Contrast keep their existing per-theme opacity tokens; High Contrast remains at 0.24 with solid surfaces.

The renderer now checks the packaged rail art after alpha/opacity compositing over the High Contrast navigation surface. It requires at least 1.5:1 decoration-to-surface contrast while keeping both paper and muted text at 4.5:1 or higher. The artwork remains passive and governed by the existing decorative-accent setting. The optimized derivative is 320×640 RGBA, 147,309 bytes, SHA-256 `06D98F717E5E38F597D965E266F1D6B59208C9AD662C2ED91E4F20A6E7AF578C`; the packaged inventory is 7,516,909 compressed bytes and 16,473,296 decoded RGBA bytes.

`tools/Prepare-CalradiaForge-Desktop-Textures.bat --check` passed deterministic validation. `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts/desktop-visual-rev064-contrast.json` built with zero warnings/errors, passed Desktop 59/59 and 281 render cases with 158 layout passes. Its JSON report recorded 7,802 ms total and 1,738.8 ms in layout calls; these are harness measurements only. High Contrast, Parchment, and minimum-window render screenshots were reviewed. Live app/DPI review remains unverified. Product version remains 25.2.0; no public contract, route, command, permission, or ZIP changed.

### Evidence report presentation (Rev065)

The workbench report uses visible, keyboard-focusable **Evidence** and **Raw Result** tabs with selected, hover, and focus states. Evidence records are rendered as separate cards with source/status, rule ID, source location, evidence text, and recommendation. The empty ledger keeps its localized **Open command palette** action interactive beside a passive local illustration. The render suite verifies the tab labels, card contents, empty-state action binding, and layout bounds. Its hit-test check runs against the off-screen WPF render host: it calls `VisualTreeHelper.HitTest` at the button's center and checks the button's WPF hit surface and visible ancestors while the command-palette overlay is collapsed. This proves the rendered WPF subtree exposes an unobstructed hit surface; it does not simulate the operating-system pointer, dispatch a real click, or verify a live application window.

### Evidence report hit-testing clarification (Rev066)

The Rev065 empty-ledger hit-test claim is limited to the off-screen WPF visual tree as described above. The final post-fix render artifact, `artifacts/desktop-visual-report-rev066-review.json`, records a passing report with Desktop 59/59, 281 render cases, and 158 layout passes. This run includes the whitespace-only recommendation regression. The harness reported 7,821 ms total and 1,707.2 ms in layout calls; these are test-harness measurements, not application interaction latency. The build completed with zero warnings and errors. Operating-system pointer behavior and the live WPF application remain unverified.

Rev066 is the source-documentation counter. The corresponding protected Register of Improvements appendix is Rev046; these counters are independent. Product version remains 25.2.0, and no API, route, command, permission, dependency, or ZIP changed.

### Status and command hierarchy refinement (Rev067)

The command palette is now the single primary header action, with a brass surface, a local search glyph, a 40-DIP minimum target, and the existing localized accessible name and `OpenPaletteCommand`. The five work-order cards now use consistent top-right semantic icons; the active-tool card has a localized label and a stronger brass edge. Status glyph opacity is theme-aware (0.48 War Table, 0.56 Parchment Light, 0.82 High Contrast), remains passive, and is tested against each palette. The evidence export target is 30 DIP high. All 13 locale catalogs contain the new `Ui.ActiveTool` key.

`cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-rev067-final.json"` built without warnings/errors, passed Desktop 59/59, and passed 285 WPF render cases with 158 layout passes. The final harness report recorded 8,681 ms overall and 2,013.7 ms in layout calls; the earlier same-session baseline artifact had 281 render cases/158 layout passes and recorded 10,893 ms/2,564 ms respectively. These are single-run harness timings, not application latency or a performance claim. War Table and High Contrast screenshots were reviewed from the off-screen renderer; no live WPF window was reviewed.

Rev067 is the source-documentation counter. Its protected Register of Improvements appendix is Rev047; the counters are independent. Product version remains 25.2.0; no public API, route, command, permission, dependency, or ZIP changed.

### Compact header and evidence controls (Rev068)

At the 980×680-DIP minimum, the header uses a compact localized label for decorative accents, an icon-only dossier toggle, and narrower settings/actions to keep its controls in one row. The decorative toggle retains its full localized accessible name and tooltip. The dossier toggle retains its `IsContextDossierOpen` binding and full accessible label while using the local book glyph. Evidence export uses a compact 36×30-DIP icon button with the existing command, automation ID, localized accessible name, and tooltip; the reclaimed status-card width keeps the evidence summary readable at the minimum size.

All 13 locale dictionaries provide `Ui.DecorativeAccentsShort`. Render regressions check compact header placement and visibility, the evidence summary/export control, and dossier/decorative-toggle accessibility. Routes, commands, view-model behavior, public API, IPC, product version, and packaged ZIPs are unchanged.

`cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-rev068-final.json"` passed the Desktop suite 59/59 and 285 WPF render cases with 158 layout passes and zero build warnings/errors. This single render run recorded 13,135 ms overall and 3,072.1 ms in layout calls; the same-session single-run baseline recorded 9,120 ms and 2,033.3 ms. These are harness timings, and the final run was slower; no performance improvement is claimed. The render host is off-screen, so live-window behavior, system-DPI layout, and operating-system input remain unverified.

Rev068 is the source-documentation counter; its protected Register of Improvements appendix is Rev048. No Bannerlord session, campaign, or battle was started, and no ZIP was regenerated or modified.

### Minimum-window ledger and responsive identity treatment (Rev069)

The 980×680-DIP regression now checks that the empty evidence ledger's localized message and action both remain inside the visible work-area viewport; the action keeps its minimum activation height. The tool identity badge is bounded instead of stretching across the summary card: long category banners and mottos remain on one line, trim with an ellipsis, expose their full value in tooltips, and stay within the badge frame. The compact header is also checked as a single row at the minimum width in all 13 supported locales. These constraints keep the empty-state content and category ornament readable in a narrow work area.

The supplied final render artifact `artifacts/desktop-visual-rev069-final.json` passed with Desktop 59/59, 286 WPF render cases, 172 layout passes, and zero build warnings/errors. Its JSON records 11,719 ms total harness time and 3,106.8 ms in layout calls. These are harness measurements, not open-app latency or a performance claim. The test checks local presentation and rendered bounds; no runtime UI Automation or live-app validation was performed.

Rev069 is the source-documentation counter; its protected Register of Improvements appendix is Rev049. Product version remains 25.2.0, and no public API, routes, commands, permissions, IPC, dependencies, or ZIPs changed.

### Windows system-DPI coverage clarification (Rev070)

The WPF render harness keeps the isolated host window's Windows DPI and DIP layout fixed. A preview scale factor changes only the output density of `RenderTargetBitmap`; the 100% and 200% labels describe raster density, not Windows display-scale settings. The harness does not simulate 125%, 150%, or 200% Windows system-DPI layout.

In `artifacts/desktop-visual-rev069-scale-audit.json`, `windows-system-dpi-layout-coverage` is explicitly `not-simulated`, with no applied Windows DPI factors and preview bitmap raster factors `[1, 2]`. Existing route, locale, theme, and interaction checks remain in the supplied run; no functional cases were removed. The artifact reports Desktop 59/59 and 287 WPF render cases with 172 layout passes and zero build warnings/errors. Its 9,321 ms total and 2,134.7 ms in layout calls are harness timings, not open-app latency. Actual Windows DPI behavior remains unverified.

Rev070 is the source-documentation counter; its protected Register of Improvements appendix is Rev050. Product version remains 25.2.0; no API, routes, commands, permissions, IPC, dependencies, or ZIPs changed.

### Bounded IPC, failure-safe report exports, and localized studio labels (Rev071)

Desktop reads each newline-delimited IPC response incrementally and stops at the existing 32 Mi UTF-16-character limit before buffering an unbounded line or attempting deserialization. Cancellation reaches the read loop; an oversized response follows the existing disconnect path without further reads. The wire format and limit are unchanged.

Report exports run asynchronously, stage in an exclusive temporary file in the report directory, and publish under a unique name without overwriting an earlier report. Cancellation and write failures return localized status, remove the temporary file, and leave existing reports intact. The dossier's console-command copy buttons have unique automation IDs and localized accessible names that include the command; the CLI copy control has its own localized name, and a polite live region announces localized success or failure.

Static interface labels in the troop-tree, workshop, code-security, module-topology, diplomacy, and component visualizers now resolve from the 13 locale dictionaries. Longer labels wrap in their existing cards. Example troop/faction names, commands, identifiers, numerical values, and technical sample data remain unchanged.

cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-robustness-after-20260927-final.json <nul>" built with zero warnings/errors, passed Desktop 63/63 and 289 WPF render cases with 182 layout passes. The artifact records 11,758 ms total harness time and 3,483.7 ms in layout calls; these are harness timings, not open-app latency. tools\Test-CalradiaForge-Desktop-Uia.bat passed 23/23 read-only checks; the report records ForegroundUnchanged=true and no foreground event from the owned process. UIA verifies the launched window's accessible tree, not visual approval; pickers, work execution, preference changes, and visual appearance were not examined. No Bannerlord session was started.

After adding text wrapping to translated stat lines and compact badges in narrow visualizer cards, the final BAT rerun passed the same 63 Desktop checks and 289 render cases with 182 layout passes. `artifacts/desktop-robustness-after-20260927-final-wrap.json` records 10,939 ms total harness time and 3,120.8 ms in layout calls; these remain harness timings, not application latency. The read-only UIA BAT was also rerun and passed 23/23 controls, including both dossier copy buttons, with ForegroundUnchanged=true.

Rev071 is the source-documentation counter; its protected Register of Improvements appendix is Rev051. Product version remains 25.2.0; no public API, route, command, permission, protocol, dependency, or ZIP changed.
