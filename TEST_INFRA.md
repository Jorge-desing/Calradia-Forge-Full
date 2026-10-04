# E2E Test Infrastructure: Calradia Forge Desktop Tactical Studios Full Composition Overhaul (Rev101 / Rev135)

## 1. Test Philosophy & Core Architecture
- **5-Stage Verification Pyramid (`test-architect`)**:
  1. *Stage 1: Static Source Contracts*: Regex and AST analysis verifying architectural invariants (Rule C, Propuesta 48, GEMINI anti-shadowing, stateless behaviors) in milliseconds.
  2. *Stage 2: Pure Unit Tests*: Offline unit and simulation tests for SDK models, troop diffing, CoALA cognitive memory consolidation, and assembly workbench pipelines without game engine dependencies.
  3. *Stage 3: Headless WPF Render & Layout Tests*: In-process STA layout passes in private non-activating desktops measuring element layout passes, visual tree node counts, and surface bounds (980x680 DIP).
  4. *Stage 4: Windows UI Automation (UIA) Smoke Checks*: Non-activating accessibility tree queries validating control discovery, tool navigation, and copy button accessibility without stealing user focus.
  5. *Stage 5: Packaging & Distribution Hygiene Gate*: Preflight checks ensuring release archives exclude proprietary engine DLLs, user saves, and debug residues.
- **Strict Non-Interactive Execution**: Every test runner accepts `--no-pause` and is invoked with `<nul` to eliminate interactive blocking in automated CI environments.
- **Deterministic Offline Mocks**: Zero reliance on `Bannerlord.exe` runtime; all data flows and viewmodels run decoupled.

---

## 2. Feature Inventory & Test Coverage Matrix

| # | Feature / Contract | Source Requirement | Tier 1: Feature Coverage | Tier 2: Boundary & Corner | Tier 3: Cross-Feature | Tier 4: Real-World Workloads |
|---|--------------------|--------------------|:------------------------:|:-------------------------:|:---------------------:|:----------------------------:|
| 1 | Dual Medallion Headers (12 Studios) | `ORIGINAL_REQUEST §R1` | ✓ (5 tests) | ✓ (5 tests) | ✓ (5 tests) | ✓ (Render passes) |
| 2 | KPI Cards & Accent Styling | `ORIGINAL_REQUEST §R1` | ✓ (5 tests) | ✓ (5 tests) | ✓ (5 tests) | ✓ (Render passes) |
| 3 | 4px Mini-Progress Bars | `ORIGINAL_REQUEST §R1` | ✓ (5 tests) | ✓ (5 tests) | ✓ (5 tests) | ✓ (Render passes) |
| 4 | Dossier Monospace Consoles | `ORIGINAL_REQUEST §R1` | ✓ (5 tests) | ✓ (5 tests) | ✓ (5 tests) | ✓ (Render passes) |
| 5 | Rule C Invariant (9 Dashboards) | `ORIGINAL_REQUEST §R2` | ✓ (Static scan) | ✓ (5 tests) | ✓ (5 tests) | ✓ (Build & Test) |
| 6 | Propuesta 48 (0 TwoWay Run.Text) | `ORIGINAL_REQUEST §R2` | ✓ (Static scan) | ✓ (5 tests) | ✓ (5 tests) | ✓ (Build & Test) |
| 7 | 13-Dictionary Key Parity | `ORIGINAL_REQUEST §R2` | ✓ (Catalog audit) | ✓ (5 tests) | ✓ (5 tests) | ✓ (Python check) |
| 8 | Structural Assertion Test | `ORIGINAL_REQUEST §R3` | ✓ (Method assert) | ✓ (Slice check) | ✓ (Execution) | ✓ (Render suite) |
| 9 | Stateless CampaignBehavior Gate | `RULE[bannerlord_architecture]` | ✓ (SubModule reg) | ✓ (0 Saveable) | ✓ (0 SyncData) | ✓ (4/4 BAT Gate) |
| 10 | Solution Release Compilation | `RULE[git_sync_workflow]` | ✓ (13 projects) | ✓ (0 Warnings) | ✓ (0 Errors) | ✓ (dotnet build) |

---

## 3. Systematic 4-Tier Test Specifications

### Tier 1: Feature Coverage (>=5 Tests per Feature)

#### Feature 1: Dual Medallions Header across all 12 Tactical Studios
- **T1.1.1 (Asset Existence & Size)**: Verify all 19 primary and secondary medallion PNG assets exist on disk in `src/CalradiaForge.Desktop/Resources/Textures/` and exceed 350 KB (high-resolution master quality).
- **T1.1.2 (Csproj Resource Registration)**: Verify all 19 medallion assets are declared as `<Resource Include="Resources/Textures/..." />` in `CalradiaForge.Desktop.csproj`.
- **T1.1.3 (Pack URI Formatting)**: Verify dual medallion image sources use canonical WPF pack URIs: `pack://application:,,,/CalradiaForge.Desktop;component/Resources/Textures/<filename>`.
- **T1.1.4 (Container Geometry & Dimensions)**: Verify dual medallion borders are sized to 36x36 DIP, use `Background="{DynamicResource FrameSurfaceSolidBrush}"`, `BorderBrush="{DynamicResource BrassBrush}"`, and `BorderThickness="1.5"`.
- **T1.1.5 (HighQuality Scaling & Alignment)**: Verify `RenderOptions.BitmapScalingMode="HighQuality"` and `Stretch="Uniform"` on both primary and secondary medallion `Image` controls.

#### Feature 2: KPI Metric Cards
- **T1.2.1 (Border Thickness)**: Verify summary KPI cards enforce tactile 3px accent borders (`BorderThickness="3"`).
- **T1.2.2 (Theme Brush Conformance)**: Verify card backgrounds bind to `{DynamicResource CoalBrush}` or `{DynamicResource FrameSurfaceSolidBrush}` and borders bind to `{DynamicResource BrassBrush}` or `{DynamicResource VerdigrisBrush}`.
- **T1.2.3 (Header Typography)**: Verify KPI metric titles use `FontWeight="SemiBold"` or `FontWeight="Bold"` with `Foreground="{DynamicResource BrassBrush}"`.
- **T1.2.4 (Readout Typography)**: Verify KPI metric readouts use prominent sizing (`FontSize="14"` or higher) with `Foreground="{DynamicResource VerdigrisBrush}"`.
- **T1.2.5 (OneWay Property Binding)**: Verify metric values bind cleanly to ViewModel gauge properties (e.g. `RebellionRiskGaugeValue`, `SenateConsensusGaugeValue`, `DrawCallHealthScore`) with `Mode=OneWay`.

#### Feature 3: 4px Mini-Progress Bars
- **T1.3.1 (Height Invariant)**: Verify strict 4px height (`Height="4"`) on all gauge mini-progress indicators.
- **T1.3.2 (Border Elimination)**: Verify clean zero-border styling (`BorderThickness="0"`) on progress bar elements.
- **T1.3.3 (Dynamic Brush Pairing)**: Verify foreground tracks use `{DynamicResource VerdigrisBrush}` and track troughs use `{DynamicResource FrameSurfaceSolidBrush}`.
- **T1.3.4 (Scale Normalization)**: Verify range normalization with `Minimum="0"` and `Maximum="100"`.
- **T1.3.5 (Binding Mode Safety)**: Verify `Value="{Binding ...GaugeValue, Mode=OneWay}"` prevents reverse binding propagation.

#### Feature 4: Dossier Monospace Consoles
- **T1.4.1 (Shell Prompt Glyph)**: Verify presence of terminal shell prompt `$ ` with `Consolas, monospace` typography and `Foreground="{DynamicResource BrassBrush}"`.
- **T1.4.2 (Monospace Command Stream)**: Verify command syntax blocks use `FontFamily="Consolas, monospace"`, `Foreground="{DynamicResource VerdigrisBrush}"`, and `TextWrapping="Wrap"`.
- **T1.4.3 (Touch Copy Button)**: Verify tactile copy button styled with `Style="{StaticResource SearchClearButton}"`, 20x20 DIP dimensions, and vector icon `Path Data="{StaticResource Icon.File}"`.
- **T1.4.4 (Copy Command Dispatch)**: Verify button dispatches `CopyTextCommand` passing the command string as `CommandParameter`.
- **T1.4.5 (Curated Commands Binding)**: Verify dossier console items controls bind to `CuratedConsoleCommands` exposed by each studio ViewModel.

---

### Tier 2: Boundary & Corner Cases (>=5 Tests per Feature)

#### Boundary 1: Rule C Invariant (Preservation of Exactly 9 DashboardTemplates)
- **T2.1.1 (Exact Count Assertion)**: Regex scan `@<DataTemplate x:Key="\w+DashboardTemplate"` in `ToolPageTemplates.xaml` matches exactly 9 instances.
- **T2.1.2 (Zero DashboardTemplate Leaks)**: Assert zero new tactical studios adopt the `DashboardTemplate` suffix; all extensions must use `ViewTemplate`.
- **T2.1.3 (Whitelist Integrity)**: Verify all 9 keys match the canonical set: `TroopTreeVisualizer`, `AudioMixerInspector`, `WorkshopSimulator`, `AgentMemoryInspector`, `CodeSecurityAuditor`, `ModuleHierarchyValidator`, `KingdomDiplomacyStudio`, `ComponentGeneratorStudio`, `GenericOperationOverview`.
- **T2.1.4 (ViewTemplate Key Invariance)**: Verify the remaining 7 studio templates preserve the exact `ViewTemplate` suffix (`CaravanTradeViewTemplate`, `GauntletStudioViewTemplate`, `CampaignStudioViewTemplate`, `LiveSessionViewTemplate`, `DeliveryStudioViewTemplate`, `DiagnosticsStudioViewTemplate`, `CombatStudioViewTemplate`).
- **T2.1.5 (Template Selector Resolution)**: Verify `ToolPageViewModel` data template selector cleanly resolves both `DashboardTemplate` and `ViewTemplate` types without collision.

#### Boundary 2: Propuesta 48 Invariant (Zero TwoWay Bindings in Run.Text)
- **T2.2.1 (TwoWay Prohibition Scan)**: Regex scan `@<Run[^>]+Text="\{Binding[^"]*Mode=TwoWay` across all XAML files returns exactly 0 matches.
- **T2.2.2 (Bare Binding Prohibition)**: Regex scan for `<Run Text="{Binding ...}">` missing explicit `Mode=OneWay` returns exactly 0 matches.
- **T2.2.3 (Inlined StringFormat Verification)**: Verify complex inline bindings (e.g. `{Binding Value, Mode=OneWay, StringFormat={}{0:N0}}`) explicitly include `Mode=OneWay`.
- **T2.2.4 (TextBlock Inlines Purity)**: Verify all `<Run>` elements within `<TextBlock.Inlines>` collections preserve read-only data flow.
- **T2.2.5 (WPF Binding Engine Diagnostics)**: Confirm WPF binding trace logs during render testing report 0 `System.Windows.Data Error` warnings for `Run.Text`.

#### Boundary 3: 13-Language Dictionary Key Parity
- **T2.3.1 (Key Count Parity)**: Verify all 13 dictionary files (`Strings.<lang>.xaml` for `de`, `en`, `es`, `fr`, `it`, `ja`, `ko`, `pl`, `pt`, `ru`, `tr`, `zh-HANS`, `zh-HANT`) define exactly 264 string resource keys.
- **T2.3.2 (Symmetric Key Set Equality)**: Verify the symmetric difference between `Strings.en.xaml` and every other language dictionary is empty (`SetEquals == true`).
- **T2.3.3 (Hardcoded String Prohibition)**: Verify zero unlocalized string literals in studio headers, tooltips, or console labels.
- **T2.3.4 (Reusable UI Token Coverage)**: Verify essential tokens (`Ui.CopyCommandToolTip`, `Ui.ConsoleCommandsHeading`, `Ui.ActiveTool`) are fully defined in all 13 locales.
- **T2.3.5 (XML Schema & C14N Encoding)**: Verify UTF-8 encoding, valid XML headers, and valid mscorlib namespace declarations across all dictionaries.

---

### Tier 3: Cross-Feature Combinations (Pairwise Coverage)

- **T3.1 (Theme Switching x Contrast Ratios)**: Verify cycling across all 3 themes (`MidnightSteel`, `ImperialBurgundy`, `AmberParchment`) preserves >= 4.5:1 text contrast on KPI cards, medallion frames, and dossier consoles.
- **T3.2 (Locale Switching x Responsive Layout)**: Verify cycling across all 13 locales under minimum surface bounds (980x680 DIP) causes zero text truncation or header overflow.
- **T3.3 (Split Deck x Studio Composition)**: Verify dual medallions, KPI cards, and dossier consoles render cleanly in both full workbench mode and pinned Split Deck split-view mode.
- **T3.4 (Dynamic Resource Binding x Terminal)**: Verify that terminal backgrounds (`FrameSurfaceSolidBrush`) and prompts (`BrassBrush`) update immediately upon runtime theme swap without visual tree recreation.
- **T3.5 (High-DPI Scaling x Bitmap Rendering)**: Verify dual medallion bitmaps scale uniformly at 100%, 125%, 150%, and 200% system DPI scaling without blur or aspect distortion.

---

### Tier 4: Real-World Workloads

- **T4.1 (296 Headless WPF Render Passes)**: Execute complete headless STA test harness validating 296 cases across all views, controls, and dialogs (`PASS 296 WPF render cases`).
- **T4.2 (Visual Tree Performance Budgets)**: Validate 320 layout passes complete within performance budget (< 10,000 ms), with visual tree node visits staying under budget (< 300,000 nodes).
- **T4.3 (Stateless CampaignBehavior 4/4 Gate)**: Verify 100% compliance with Bannerlord stateless behavior invariants:
  1. Solution compiles clean in Release mode.
  2. Zero `SaveableTypeDefiner` and zero `SyncData` serialization across behaviors.
  3. Anti-shadowing verified: zero folders, namespaces, or classes named `Campaign`.
  4. Explicit `SubModule.OnGameStart` behavior registration.
- **T4.4 (Clean Release Solution Build)**: Compile all 13 projects in `CalradiaForge.sln` under Release configuration with 0 errors and 0 warnings.
- **T4.5 (Cryptographic Ledger & Docs Parity)**: Verify 100% validity of the SHA-256 hash chain in `integrity.jsonl` (134+ linked revisions) and 42/42 synchronized English/Spanish technical documentation pairs.

---

## 4. Test Runner Commands & Automated Execution

| Test Suite / Gate | Canonical Command Line | Timeout | Pass Criteria |
|---|---|---|---|
| **Solution Release Build** | `dotnet build CalradiaForge.sln -c Release -v:minimal` | 60s | Exit code 0, 0 Errors, 0 Warnings |
| **Stateless Behavior Gate** | `cmd.exe /c "tools\Verify-CalradiaForge-StatelessBehavior.bat <nul"` | 60s | Exit code 0, 4/4 criteria passed |
| **Desktop Tests Suite** | `cmd.exe /c "tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause <nul"` | 45s | Exit code 0, 67/67 tests passed |
| **Desktop Render Tests** | `cmd.exe /c "tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause <nul"` | 120s | Exit code 0, 296/296 render cases passed |
| **Python Ledger & Parity** | `cmd.exe /c "tools\Run-CalradiaForge-Python-Checks.bat --ledger --no-pause <nul"` | 60s | Exit code 0, hash chain valid, 42/42 pairs |

---

## 5. Evidence Boundaries & Operational Limits
1. **Headless WPF Isolation**: WPF render tests execute on a private Windows desktop via `IsolatedRenderDesktop.AttachCurrentThread()` and `CALRADIA_FORGE_RENDER_NO_ACTIVATE=1`. Windows are never activated or displayed on the user's interactive desktop.
2. **Offline Simulation Scope**: Simulation tests verify data models, troop diffing algorithms, and UI telemetry. They do not boot `Bannerlord.exe` or require native engine singletons.
3. **Artifact Quarantine**: All execution logs, JSON test captures, and diagnostic records reside strictly within ignored `artifacts/`, preventing development spillover into Git tracking.
