---
name: calradia-forge-ui-theme
description: Guidelines for styling Calradia Forge UI components (WPF and Gauntlet).
trigger: always_on
---

# Calradia Forge UI Guidelines

When modifying or creating new UI components for Calradia Forge (both WPF Desktop and Gauntlet XML), you must adhere to the **Tactical War Theme**.

## General Aesthetic
- **Vibe:** Dark leather, worn gold, tactical parchment, command deck.
- **Backgrounds:** Deep, dark muted greens and charcoal (e.g., `#0B0F0D`, `#101512`, `#15201A`). NEVER use bright parchment or white backgrounds for large content areas.
- **Borders & Accents:** Worn brass/gold (`#C7A45A`) and verdigris/tempered steel (`#486151`, `#1A2921`).
- **Typography:**
  - Headers and decorative text: `Georgia` (emulating classic/medieval serif).
  - Data and logs: `Consolas` or standard monospaced.
  - Text colors: Soft muted gray/silver for body text (`#D1D5DB`, `#9CA3AF`), with brass/gold for headers.

## WPF (App.xaml & MainWindow.xaml)
- Always use the defined `SolidColorBrush` resources (e.g., `CoalBrush`, `DeepPineBrush`, `BrassBrush`).
- **TabItems & DataGrids:** Must use dark backgrounds (`#0D120F`, `#131B17`). Do not use light beige (`#E2D3B7`) for large surfaces.
- **Buttons:** Use solid dark backgrounds (e.g., `#1A2921` or `#17251E`) with `BrassBrush` borders and text.

## Gauntlet (CalradiaForge.xml)
- Translate the WPF hex codes into Gauntlet's `Color="#RRGGBBAA"` format (usually appending `FF`).
- Use dark panels (`#0A0D0BFF`, `#0F1311FF`) instead of legacy bright greens.
- Use `CalradiaForge.Gold` brush for headings and `CalradiaForge.Text`/`Muted` for body.

## Scaling & Implicit Controls (Added from Learning)

- **Text Wrapping Prevention:** In Gauntlet, if a TextWidget has a Brush.FontSize that exceeds its parent container's width (e.g. large watermarks), Gauntlet will auto-wrap the text (often injecting hyphens, turning "CF" into "C- 
 F"). To prevent this, always set WidthSizePolicy="CoverChildren" on the TextWidget or ensure the font size is small enough to fit.
- **Watermarks:** Always add DoNotAcceptEvents="true" and DoNotPassEventsToChildren="true" to watermark widgets so they do not block mouse interactions with underlying buttons.

- **UI Scaling:** Never hardcode manual FontSize multiplications in C# code-behind for zooming/scaling. Always use a global LayoutTransform with a ScaleTransform attached to the root container to scale the entire interface proportionally without breaking hardcoded XAML FontSizes.
- **System Controls:** Always define implicit styles in App.xaml for ScrollBar, ToolTip, ContextMenu, and ComboBoxItem. Otherwise, WPF will render them using the default bright Windows system themes, breaking the dark Tactical War Theme immersion.
- **ComboBox Templates:** A simple Background setter will not override the Windows Aero theme for ComboBox. You MUST define a full ControlTemplate for both ComboBox (including its ToggleButton and Popup) and ComboBoxItem to enforce the dark theme, otherwise dropdowns will render bright white and hide light text.

- **Hints and Tooltips:** Always use ToolTip properties on WPF action buttons utilizing the deep-pine/brass styling. In Gauntlet XML, strictly use Bannerlord's native Hint.HintText for components instead of creating custom hover widgets, as the native hint manager provides a rich dark-parchment aesthetic out of the box.
- **Geometric Decorations:** Replicate the Tactical Motif (—◇—◇—) across UI boundaries (under titles, between navigation panels) using <Path> in WPF and native Sprite="Divider\horizontal_line" in Gauntlet.

## Gauntlet Widget Architecture & Predefined Widgets (Added from Learning)
- **Class-to-XML Mapping:** The XML tag name must **exactly match** the C# class name of the widget (e.g., `<ButtonWidget>`).
- **Core Predefined Widgets:**
  - `Widget` (base class, often used as an empty container or layout anchor)
  - `ButtonWidget`, `TextWidget`, `RichTextWidget`, `ImageWidget`
  - `ListPanel`, `ScrollablePanel`, `ScrollBarWidget`
  - `TooltipWidget`
- **Common Attributes:**
  - **Layout:** `SuggestedWidth` / `SuggestedHeight`, `WidthSizePolicy` / `HeightSizePolicy`, `MarginLeft` / `MarginRight` / `MarginTop` / `MarginBottom`, `HorizontalAlignment` / `VerticalAlignment`
  - **Data Binding:** `DataSource` binds a widget to properties marked with `[DataSourceProperty]` in C#.
  - **Interactions:** `Command.Click` (or `Command.YourKeyHere`) binds click events directly to C# methods in the View Model. `DoNotAcceptEvents="true"` disables interactions.
- **Custom Widgets:** Create custom UI elements by inheriting from `Widget` or any derived class in C#, which can then be immediately consumed in XML using the exact class name.

## Syncing & Visibility Rules (Added from Learning)
- **Empty States:** When a text widget evaluates to an empty string `""` from a C# `[DataSourceProperty]`, the widget text is empty but its parent container will still render its background and borders, leading to broken UI boxes. Always wrap these components or their containers with an `IsVisible="@YourBooleanToggle"` property to hide them completely when empty.
- **Action Parity:** Ensure that UI `actions` (like `diagnostics`, `patch-preflight`, `extensions`) added to the Gauntlet `PanelViewModel.cs` are manually mirrored and kept in sync with the `ArgumentSuggestions` dictionaries, `NavigationLabel` mappings, and the `MainWindow.xaml.cs` (Desktop UI) mappings to prevent silent UI breakdown or missing shortcut labels.

## WPF Resource Auditing & Keyed Styles (Added from Learning)
- **Implicit Style TargetType Auditing Gotcha:** Never declare more than one unkeyed `<Style TargetType="...">` for the same control type in `App.xaml`. Even styles defined inside templates or control resources without an `x:Key` are recorded by WPF into `Application.Current.Resources`. During test suite execution, automated resource audits (e.g., `AdvancedToolsTests`) scan all `<Style>` elements in `App.xaml` and fail if duplicate implicit styles share the same `TargetType`. Specialized styles (such as summary card borders: `x:Key="RailFooterSummaryCard"`, `x:Key="ToolPageSummaryCard"`) MUST always specify an explicit `x:Key` and be applied via `Style="{StaticResource ...}"`.

## Material Masters & Gauntlet Composition (Rev024–Rev027)
- **ImageGen Material Masters:** Ad-hoc procedural or vector wireframe lines (`forge_map_contours`, filigree, rosettes) are superseded by high-fidelity ImageGen material masters registered in the sprite atlas:
  - `forge_war_table_cloth`: Dark woven tactical cloth background for main panels.
  - `forge_heraldic_overlay`: Subdued heraldic watermark texture.
  - `forge_patina_brass`: Aged brass borders and decorative framing.
  - `forge_pine_felt`: Rich dark forest felt accent backing.
- **Clean Evidence Surface & Legibility:** Never position busy, patterned textures or decorative overlays directly behind technical output, terminal consoles, command argument fields, or telemetry logs. Log and data containers must maintain a clean, solid, high-contrast dark background (`#0A0D0BFF`).
- **Decorative Event Passivity:** All decorative widgets, material overlays, and watermarks MUST declare `DoNotAcceptEvents="true"` and `DoNotPassEventsToChildren="true"` to prevent ghost hit-testing and ensure underlying buttons, scroll viewers, and input fields remain fully interactive.
- **Distinct Control IDs for Shared ViewModel Properties:** In Gauntlet XML, when multiple input widgets or text editors bind to the same ViewModel property across different panel modes (e.g., `@Argument` used for both command arguments and assembly path pickers), each widget MUST be given a unique `Id` (e.g., `Id="ForgeCommandArgument"` vs `Id="ForgeAssemblyPath"`). Reusing widget IDs or omitting them causes Gauntlet's input focus and cursor state to collide.

## WPF Desktop C# 12 MVVM & Render Performance (Rev028+)
- **C# 12 MVVM Idioms:** In `src/CalradiaForge.Desktop`, adopt modern C# 12 features to keep view models compact and performant:
  - Use primary constructors for read-only view models, rail entries, and declarative descriptors.
  - Use collection expressions (`[]`) instead of verbose `new List<...>()` or `new T[] { ... }`.
  - Use target-typed `new()` for commands and observable collections.
  - Convert verbose requirement checks and filter mappers to switch expressions.
- **Render Pass & Visual Tree Allocation Elimination:**
  - The 274 WPF render test suite exercises live visual tree layout passes, scale transformations, and theme dictionary merges.
  - Avoid LINQ queries (`.Select().ToList()`, `.Where()`) on hot visual routes or navigation trees (e.g., `RefreshVisibleTools()`). Instead, project directly over source collections or use pre-allocated buffers.
  - Avoiding intermediate allocations during visual state updates significantly reduces layout pass durations (improving render test runtimes by ~25-30%).
- **Preservation of Static Source Contracts:**
  - Automated desktop contract tests (`tests/CalradiaForge.Desktop.Tests/Program.cs`) verify architectural rules via static source analysis (`File.ReadAllText`).
  - Never remove or rewrite required contract tokens, phrases, or field names (`!service.Contains("System.Windows")`, `"State-changing execution is unavailable from this guarded desktop route."`, `"Editable Calradia Forge starting point"`, `MaximumMeasurements = 64`, `Take(128)`, `File.Move(temporary, path, true)`, `"desktop-preferences.json"`, etc.) during code refactoring.

## First-Class Graphical Visualizers vs Headless Text (Learned from User Feedback)
- **Zero Headless Fallback for Visual Workbench Tools:** When implementing features requested as "mejoras a la app wpf", visualizers, analyzers, or simulators, NEVER stop at mere backend/headless text or ASCII generation dumped into a generic monospace `TextBox`. Users expect first-class, dedicated, interactive graphical controls and visual canvases rendered in XAML.
- **Adaptive Canvas Composition:** Tools with visual capabilities (`HasVisualDashboard`) must render specialized graphical layouts:
  - **Troop Progression:** Hierarchical DAG node trees with tier badges (T1-T6), combat stats, role icons, and upgrade arrows.
  - **Audio Studio:** Multi-band graphic equalizer spectrum bars, time-domain waveform oscilloscope bars, and dual stereo VU peak meters.
  - **Economy Simulator:** Proportional comparative horizontal bar charts for workshop profitability, ROI metrics, and civic equilibrium gauges.
  - **Cognitive Memory:** Multi-tier architectural dashboards (Working Memory, Episodic FIFO timeline, Semantic Facts with TTL expiration bars, and capacity meters).
- **Instant Pre-population on Route Selection:** Never force the user to hunt for local files or manually click "Run work order" just to inspect how a visual tool looks. On route navigation, visual tools must automatically initialize canonical default demo models and render live graphics immediately.
- **Split Deck Visual Prominence:** Multi-deck or comparative inspection tools must feature prominent command deck triggers (e.g. `[ ⊞ SPLIT DECK [Ctrl+D] ]`) with live glowing active badges (`ACTIVE`), clear status borders, and docked comparative canvases.

## Opening and Reviewing the In-Game Panel
- The Calradia Forge overlay opens on a singleplayer screen with `Settings.Hotkey` (`F10` by default); the same key closes it. In multiplayer, the module closes the panel.
- **F10 polling:** Keep `Input.IsKeyPressed` as the normal signal. A live diagnostic showed that F10 could report `IsKeyPressed == false` and `IsKeyDown == false` while `IsKeyDownImmediate == true`. The panel therefore combines the normal signal with an F10-only rising edge from `IsKeyDown || IsKeyDownImmediate`; update the held-state latch every application tick, re-arm after release, and clear it when the configured key changes away from F10. Do not use the immediate held state as a level-triggered toggle. See `.agents/rules/bannerlord_input_debug_agent.md`.
- **Resource Browser picker:** Right-click empty space in the central **Assets** pane, not the left module/folder tree, and choose **Import New Assets**. This opens the file picker; it is distinct from scanning an atlas and importing a sprite category. Follow [bannerlord-resource-browser](../skills/bannerlord-resource-browser/SKILL.md) and do not claim import success from the picker alone.
- **Correction and review order:** Before each source correction, close Bannerlord and the Modding Kit and end/reset Ordenador/Computer Use; keep them closed through the edit and build. If live review is requested or authorized, build both `Win64_Shipping_Client` and `Win64_Shipping_wEditor` profiles first, close the Modding Kit completely, then launch Bannerlord from Steam with Calradia Forge enabled and inspect the panel at the singleplayer main menu. Do not keep the game and Modding Kit open together.
- After each live check, close Bannerlord and end/reset the Ordenador/Computer Use session before making another source correction or finishing the task. When the user requests or authorizes live review, launch the Steam installation and inspect the menu directly; do not ask the user to open it first.
- Preserve the imported Steam runtime TPAC during code deployment and compare its SHA-256 before and after. TpacTool was removed after frequent reader errors; do not reinstall it, use it as a gate, or treat its legacy parse errors as proof of corruption.
- Record rendering and interaction only after observing the actual panel. A source audit or Resource Browser import alone does not establish live UI behavior.

## Contextual Operational Buttons & Aesthetic Differentiation (Rev031+)
- **Prohibition of Monotonous Generic Action Bars:** In the WPF Desktop workbench, operational buttons must not be visually identical or share the same generic label ("Run work order") across all 194 tool routes.
- **Domain & Category Differentiation:**
  - **Simulations (Verdigris):** Active simulation verbs (e.g., *"⚡ Simular Progresión"*, *"🔬 Analizar Espectro"*, *"📊 Simular Economía"*, *"🧠 Auditar Memoria"*), `VerdigrisBrush` accent border, and thematic icon.
  - **Audits, Diagnostics & Security (Worn Brass / Tempered):** Specific inspection verbs (e.g., *"🛡️ Auditar Ensamblado"*, *"🔍 Verificar Anti-Shadowing"*, *"📋 Validar Manifiesto"*), `BrassBrush` accent border, and shield/magnifier iconography.
  - **Generators & Scaffolding (Pine Felt / Worn Gold):** Generative verbs (e.g., *"⚙️ Generar Andamiaje"*, *"📜 Sintetizar XML"*, *"🔨 Forjar Componente"*), `DeepPineBrush` accent border, and gear/scroll iconography.
  - **Live Sessions & IPC Telemetry (Ember / Azure):** Query or dispatch verbs (e.g., *"📡 Consultar Telemetría"*, *"⚡ Despachar Evento"*), `EmberBrush` accent border, and radar/pulse iconography.
- **Dedicated Secondary Quick-Actions:** Operational input decks must provide domain-specific secondary quick-actions (such as `PresetActionButton` with `LoadPresetCommand` to cycle canonical scenarios/branches and `ClearInputButton` with `ClearInputCommand` when input parameters are populated).
- **Contract & AutomationId Invariants:** Contextual personalization MUST preserve all underlying standard command bindings (`RunCommand`, `CancelCommand`, `ExportCommand`, `BrowseFileCommand`, `BrowseFolderCommand`) and their static accessibility identifiers (`AutomationProperties.AutomationId`) to avoid breaking static contract analysis or WPF render verification suites.

## Operation-Level Aesthetic Customization & Domain Visualizers (Rev032+)
- **Distinct Operation Identity Surfaces:** Every tool route in the WPF Desktop workbench incorporates a distinct visual identity composed of:
  - **Category Banner:** Tactical military department banner (e.g. `[DIAGNOSTICS & SYSTEM SAFETY RADAR]`, `[HIGH SENATE & DIPLOMATIC CORPS]`, `[GAUNTLET UI & SHADER FORGE]`).
  - **Latin Military Doctrine Motto:** Classical Latin motto reflecting domain purpose (e.g. *"Ad unum omnes ad astra"*, *"Virtus in armis, ordo in acie"*, *"Consilio et concordia regna florent"*).
  - **Category Accent Brush (`Tool.CategoryAccentBrushKey`):** Dynamically bound border accents, icon glyph backgrounds, and title kicker typography (`BrassBrush`, `VerdigrisBrush`, `EmberBrush`, `DeepPineBrush`).
  - **Input Domain Badge (`Tool.InputDomainBadge`):** Clear input classification pill (e.g. `[TARGET ASSEMBLY / PE BINARY / MANIFEST]`, `[TARGET FACTION / SENATE POLICY / CLAN]`).
  - **Security Scope Pill (`Tool.SecurityPillText`):** Explicit operational safety boundaries (`[ STRICT BOUNDED ROUTE · ZERO IN-GAME SIDE EFFECTS · THREAD ISOLATED ]`).
- **Domain Visual Dashboards (`HasVisualDashboard`):** Dedicated graphical interactive studios render rich XAML visualizers:
  1. **PE Assembly Security & CLR Invariant Radar:** Visual risk score gauge, four architectural compliance cards (Rule A, Rule B, Rule C, Rule D), and CLR metadata stream telemetry.
  2. **Module Topology & Pipeline DAG:** Horizontal directed acyclic graph stages showing load order sequencing, dependency counts, and SubModule.xml schema verification.
  3. **Geopolitical Diplomacy & Senate Chamber:** Regional tension barometers, dynastic succession heir scores, and multi-faction stance cards with border conflict probability progress meters.
  4. **Component Blueprint Synthesis:** Four-stage pipeline cards (UI View -> ViewModel -> Domain Model -> Unit Tests) with member counts and mixer compliance indicators.
- **Minimum Viewport Layout Invariant:** Visual canvas cards (`AdaptiveVisualCanvasCard`) must evaluate `HasVisualDashboard` to `false` for standard file analyzer tools (such as `SubModuleValidator`) to prevent visual tree overflow beyond the 980x680 DIP minimum application viewport during non-simulation test suites.

## Ocultación Condicional de Entrada y Dossier Táctico de Comandos (Rev033+)
- **Ocultación Dinámica de Barra de Entrada:**
  - En herramientas y estudios donde `Tool.RequiresInput == false`, la caja de texto `ActiveToolInput` (y la tarjeta de entrada si no hay parámetros) debe ocultarse automáticamente (`Visibility.Collapsed`), preservando el espacio para los lienzos interactivos y las botoneras operativas pertinentes.
  - La preservación de contratos MVVM exige que la propiedad `Input` y la lógica interna de validación permanezcan intactas en el ViewModel aunque el control visual esté colapsado.
- **Dossier Informativo y Referencia de Comandos Obligatoria:**
  - Cada sección y ruta de herramienta debe exponer una tarjeta táctica dedicada (`CommandReferenceCard` o `SectionDossierCard`) que detalle:
    1. **Contexto Operativo:** Propósito técnico, ámbito en Bannerlord y garantías de seguridad (Stateless, Thread-Isolated, Sandbox).
    2. **Comandos de Consola del Motor:** Comandos in-game asociados (`cf.*`, `campaign.*`, etc.) con badges distintivos y botón de copiado rápido.
    3. **Atajos de Teclado del Banco de Trabajo:** Hotkeys aplicables (`Ctrl+Enter` para ejecutar, `Ctrl+D` para Split Deck, `Ctrl+P` para alternar presets, `Ctrl+E` para exportar).
    4. **Sintaxis CLI Headless:** Invocación por línea de comandos para scripting y CI/CD (`CalradiaForge.Desktop.exe --tool <Id>`).



## Optimización de Rendimiento y Cero-Asignación en WPF (.NET 8) (Rev034+)
- **Eliminación de Boxing en Convertidores de Colecciones:**
  - Cuando un convertidor (`IValueConverter` o `IMultiValueConverter`) inspeccione una colección observable, castear preferentemente a `IList<T>` o `IReadOnlyList<T>` y recorrer con bucle indexado `for (int i = 0; i < list.Count; i++)`.
  - Evitar `foreach (var item in (IEnumerable)value)` en convertidores evaluados recurrentemente en plantillas de datos (`DataTemplate`).
- **Colecciones Reactivas In-Place:**
  - Usar sobrecargas de reemplazo por lotes (`BatchObservableCollection.ReplaceAll`) que comprueben paridad ordinal o por igualdad antes de emitir `NotifyCollectionChangedAction.Reset`. Si la colección es idéntica en contenido y orden, no disparar eventos ni asignar nuevos arrays.
- **Predimensionado de Vistas y Rieles de Navegación:**
  - En filtros de búsqueda y generación de entradas de navegación, precalcular la capacidad exacta de las listas resultantes para evitar realocaciones automáticas de buffers en la lista.

## Precomputación de Metadatos y Enlaces Inmutables (Rev035+)
- **Propiedades de Catálogo Inmutables:**
  - Los metadatos de presentación de herramientas (`Group`, `IconKey`, `CategoryBanner`, `CategoryMotto`, `CategoryAccentBrushKey`, `InputDomainBadge`, `InputFormatHint`, `SecurityPillText`, `CliSyntax`) deben calcularse en el constructor del modelo y exponerse mediante campos de respaldo directos, evitando llamadas a `StartsWith` o expresiones `switch` en los getters.
- **Colecciones de Comandos Compartidas:**
  - Las listas de atajos o comandos de consola asociados a herramientas deben apuntar a matrices estáticas compartidas (`static readonly string[]`), nunca instanciarse inline en el getter de la propiedad.
- **Precomputación en Elementos de Evidencia:**
  - En modelos de datos evaluados en listas o tablas XAML (`WorkspaceEvidence`), propiedades formateadas complejas (como `Location`) deben computarse en el constructor y exponerse con getter directo.

## Optimización de ViewModels de Simulación Gráfica y Visualizadores (Rev036+)
- **Jerarquías de Nodos Canónicas Compartidas:**
  - En visualizadores jerárquicos o de grafos DAG (p. ej. árboles de progresión de tropas o taxonomías), la estructura canónica debe construirse en un constructor estático (`static`) y compartirse entre instancias del ViewModel.
- **Precomputación de Métricas y Resúmenes en Nodos de Elementos:**
  - En modelos de elementos secundarios vinculados a controles de lista o cuadrículas (p. ej. `TroopNodeViewModel`, `FactionStanceViewModel`, `WorkshopEnterpriseItemViewModel`), las propiedades de formato de texto (`StatSummary`, `TensionText`) y medidas visuales (`BarWidth`) deben precomputarse en el constructor y exponerse como `{ get; }`, evitando interpolaciones de cadenas o aritmética en los getters durante el renderizado WPF.
- **Matrices Estáticas de Escenarios:**
  - Los estudios que admiten conmutación interactiva de escenarios deben prealmacenar los nodos de cada escenario en matrices estáticas compartidas, evitando instanciar nuevos modelos al alternar preajustes.

## Optimización del Pipeline XAML, Enlaces y Congelación de Recursos (Rev037+)
- **Congelación y Caché Estática en Convertidores de Geometrías y Pinceles:**
  - Los convertidores que devuelven objetos derivados de `Freezable` (`Geometry`, `Brush`) a partir de claves semánticas deben cachear el resultado y llamar a `.Freeze()`.
  - La invalidación de caché debe gestionarse atómicamente cuando el usuario conmute el tema visual (`ApplyDefault()`, `Apply(themeId)`).
- **Prohibición de Enlaces Rotos o Fallidos en Vistas Compartidas:**
  - Todo ViewModel presentado en una vista compartida debe implementar todas las propiedades enlazadas por el XAML, incluso con valores por defecto o indicadores informativos. Ningún enlace debe fallar en tiempo de ejecución para evitar penalizaciones de reflexión interna en el motor de enlace WPF.
- **Clasificación de Estudios y Sub-vistas en O(1):**
  - La diferenciación de sub-vistas especializadas debe resolverse mediante un enum compacto (`byte`) precalculado en la definición inmutable de la herramienta, y exponerse en el ViewModel de página como propiedades booleanas `{ get; }` inmutables, nunca como expresiones dinámicas en el getter.

## Ciclo de Vida de Comandos y Telemetría en el Pie de Página (Rev071+)
- **Orden de Inicialización de Comandos en ViewModels:**
  - Todo RelayCommand o AsyncRelayCommand cuyo estado de ejecución sea notificado por setters de propiedades observables debe ser instanciado en el constructor del ViewModel ANTES de cualquier llamada a métodos de configuración que asignen dichas propiedades.
  - En los setters de propiedades, invocar siempre Command?.NotifyCanExecuteChanged() con operador de propagación nula para garantizar seguridad ante invocaciones tempranas.
- **Controles de Telemetría en Pie de Página (AssertFooterFitsShellViewport):**
  - El botón de telemetría IPC (FooterPingButton) debe residir en la columna izquierda del pie de página dentro de un StackPanel horizontal junto al badge de paleta, manteniendo una altura máxima de 18 DIP y respetando estrictamente los márgenes centrales asignados a KeyboardShortcutHint.
