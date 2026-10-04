---
name: calradia-forge-ui-theme
description: Guidelines for styling Calradia Forge UI components (WPF and Gauntlet).
trigger: always_on
---

# Calradia Forge UI Guidelines

## Scope and precedence

Gauntlet and WPF are separate UI systems with different widget, resource, input, and test contracts. Apply only the subsection for the technology being changed. The historical examples below are not universal product requirements: do not add a graphical dashboard, prepopulate every route, display a dossier or Latin motto, or emphasize Split Deck unless the task and existing route call for it. Selecting a route must never run work automatically. Preserve existing commands, bindings, permissions, and accessibility IDs unless behavior changes are explicitly requested.

When modifying or creating new UI components for Calradia Forge (both WPF Desktop and Gauntlet XML), you must adhere to the **Tactical War Theme**.

## General Aesthetic
- **Vibe:** Dark leather, worn gold, tactical parchment, command deck.
- **WPF War Table default:** Deep, dark muted greens and charcoal (e.g., `#0B0F0D`, `#101512`, `#15201A`) with brass/verdigris accents. Respect the separate Parchment and High Contrast themes; do not apply the War Table background rule to them.
- **Gauntlet:** Use the module's registered brushes and assets, verified against its current resource setup. Do not mechanically copy WPF hex colors or theme assumptions into Gauntlet.
- **Borders & Accents:** Keep borders and accents coherent with the active UI palette; WPF and Gauntlet resource names are separate.
- **Typography:**
  - Headers and decorative text: `Georgia` (emulating classic/medieval serif).
  - Data and logs: `Consolas` or standard monospaced.
  - Text colors: Soft muted gray/silver for body text (`#D1D5DB`, `#9CA3AF`), with brass/gold for headers.

## WPF (App.xaml & MainWindow.xaml)
- Always use the defined `SolidColorBrush` resources (e.g., `CoalBrush`, `DeepPineBrush`, `BrassBrush`).
- **TabItems & DataGrids:** Must use dark backgrounds (`#0D120F`, `#131B17`). Do not use light beige (`#E2D3B7`) for large surfaces.
- **Buttons:** Use solid dark backgrounds (e.g., `#1A2921` or `#17251E`) with `BrassBrush` borders and text.

## Gauntlet (CalradiaForge.xml)
- Use independently verified registered Gauntlet brush tokens. Explicit `Color="#RRGGBBAA"` values are appropriate only when the affected prefab already uses and validates them; do not translate WPF theme hex values mechanically.
- Keep tactical contrast and hierarchy coherent with the selected game UI, and inspect the registered brush palette before changing panel colors.
- Use `CalradiaForge.Gold` brush for headings and `CalradiaForge.Text`/`Muted` for body.

## Scaling & Implicit Controls (Added from Learning)

- **Text Wrapping Prevention:** In Gauntlet, if a TextWidget has a Brush.FontSize that exceeds its parent container's width (e.g. large watermarks), Gauntlet will auto-wrap the text (often injecting hyphens, turning "CF" into "C- 
 F"). To prevent this, always set WidthSizePolicy="CoverChildren" on the TextWidget or ensure the font size is small enough to fit.
- **Watermarks:** Always add DoNotAcceptEvents="true" and DoNotPassEventsToChildren="true" to watermark widgets so they do not block mouse interactions with underlying buttons.

- **WPF-only UI Scaling:** Never hardcode manual FontSize multiplications in C# code-behind for zooming/scaling. Validate the existing WPF scaling strategy; do not add a global `LayoutTransform` unless the affected view and input geometry have been checked.
- **System Controls:** Always define implicit styles in App.xaml for ScrollBar, ToolTip, ContextMenu, and ComboBoxItem. Otherwise, WPF will render them using the default bright Windows system themes, breaking the dark Tactical War Theme immersion.
- **ComboBox Templates:** A simple Background setter will not override the Windows Aero theme for ComboBox. You MUST define a full ControlTemplate for both ComboBox (including its ToggleButton and Popup) and ComboBoxItem to enforce the dark theme, otherwise dropdowns will render bright white and hide light text.

- **Hints and Tooltips:** Always use ToolTip properties on WPF action buttons utilizing the deep-pine/brass styling. In Gauntlet XML, strictly use Bannerlord's native Hint.HintText for components instead of creating custom hover widgets, as the native hint manager provides a rich dark-parchment aesthetic out of the box.
- **Geometric Decorations:** Replicate the Tactical Motif (—◇—◇—) across UI boundaries (under titles, between navigation panels) using <Path> in WPF and native Sprite="Divider\horizontal_line" in Gauntlet.

## Gauntlet Widget Architecture & Predefined Widgets (Added from Learning)
- **Class-to-XML Mapping:** The XML tag name must **exactly match** the C# class name of the widget (e.g., `<ButtonWidget>`).
- **Core Predefined Widgets:**
  - `Widget` (base class, often used as an empty container or layout anchor)
  - `ButtonWidget`, `TextWidget`, `RichTextWidget`, `ImageWidget`
  - `ListPanel`, `ScrollablePanel`, `ScrollbarWidget` (case-sensitive engine widget name)
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
  - Avoid LINQ queries (`.Select().ToList()`, `.Where()`) on hot visual routes or navigation trees (e.g., `RefreshVisibleTools()`). Instead, project directly over source collections or use pre-allocated buffers.
  - Treat allocation reduction as a hypothesis until the affected interaction is profiled. Compare repeated runs on the same machine and record the revision, launcher, and output artifact; harness time is not application latency.
- **Preservation of Static Source Contracts:**
  - Automated desktop contract tests (`tests/CalradiaForge.Desktop.Tests/Program.cs`) verify architectural rules via static source analysis (`File.ReadAllText`).
  - Never remove or rewrite required contract tokens, phrases, or field names (`!service.Contains("System.Windows")`, `"State-changing execution is unavailable from this guarded desktop route."`, `"Editable Calradia Forge starting point"`, `MaximumMeasurements = 64`, `Take(128)`, `File.Move(temporary, path, true)`, `"desktop-preferences.json"`, etc.) during code refactoring.

## Graphical Visualizers (Use When They Fit the Requested Workflow)
- When the requested change is a visual analysis or simulator workflow, prefer a dedicated graphical view over placing every result in a generic monospace `TextBox`. A text report remains appropriate for technical output and routes whose contract is textual.
- **Adaptive Canvas Composition:** Where a route already exposes a visual dashboard, use layouts appropriate to its content. Examples include:
  - **Troop Progression:** Hierarchical DAG node trees with tier badges (T1-T6), combat stats, role icons, and upgrade arrows.
  - **Audio Studio:** Multi-band graphic equalizer spectrum bars, time-domain waveform oscilloscope bars, and dual stereo VU peak meters.
  - **Economy Simulator:** Proportional comparative horizontal bar charts for workshop profitability, ROI metrics, and civic equilibrium gauges.
  - **Cognitive Memory:** Multi-tier architectural dashboards (Working Memory, Episodic FIFO timeline, Semantic Facts with TTL expiration bars, and capacity meters).
- **Demo Data:** A route may offer clearly labeled sample data when useful. Do not label sample output as verified runtime evidence, and do not execute work, browse files, or mutate state automatically on route selection.
- **Split Deck:** Preserve and present existing comparison controls where the workflow uses them; this is not a required feature or visual motif for every route.

## Opening and Reviewing the In-Game Panel
- The Calradia Forge overlay opens on a singleplayer screen with `Settings.Hotkey` (`F10` by default); the same key closes it. In multiplayer, the module closes the panel.
- **F10 polling:** A live diagnostic showed that F10 could report `IsKeyPressed == false` and `IsKeyDown == false` while `IsKeyDownImmediate == true`. Route `IsKeyPressed`, `IsKeyDown`, and `IsKeyDownImmediate` through one F10 edge gate (`signal && !signalWasPresent`) so a repeated normal press signal cannot close a panel opened by the same continuous input. Rearm when all three signals clear and reset the gate when the configured hotkey changes away from F10. Other hotkeys retain their normal `IsKeyPressed` path. See `.agents/rules/bannerlord_input_debug_agent.md`.
- **Resource Browser picker:** Right-click empty space in the central **Assets** pane, not the left module/folder tree, and choose **Import New Assets**. This opens the file picker; it is distinct from scanning an atlas and importing a sprite category. Follow [bannerlord-resource-browser](../skills/bannerlord-resource-browser/SKILL.md) and do not claim import success from the picker alone.
- **Correction and review order:** Before each source correction, close Bannerlord and the Modding Kit and end/reset Ordenador/Computer Use; keep them closed through the edit and build. If live review is requested or authorized, build both `Win64_Shipping_Client` and `Win64_Shipping_wEditor` profiles first, close the Modding Kit completely, then launch Bannerlord from Steam with Calradia Forge enabled and inspect the panel at the singleplayer main menu. Do not keep the game and Modding Kit open together.
- After each live check, close Bannerlord and end/reset the Ordenador/Computer Use session before making another source correction or finishing the task. When the user requests or authorizes live review, launch the Steam installation and inspect the menu directly; do not ask the user to open it first.
- Preserve the imported Steam runtime TPAC during code deployment and compare its SHA-256 before and after. The legacy upstream `szszss/TpacTool` reader was removed after frequent errors; do not reinstall it or use it as a gate, and do not treat its parse errors as proof of corruption.
- **Texture pixel evidence:** For the Calradia Forge Gauntlet atlas, inspect `modules/CalradiaForge/AssetSources/GauntletUI/ui_calradiaforge_1.png` to view the authored pixels (currently observed 4096×512 RGBA; see [Rev085](../../docs/append/Rev085-Gauntlet-resource-browser-reimport-2026-09-28.md)). Use Resource Browser's Texture Editor Preview Window to inspect a compiled texture; metadata, hashes, or file presence alone do not establish a successful pixel preview or in-game render. A TpacTool Max “no pixel data” message is a reader/resource observation, not proof of TPAC corruption. The community GraniteTextureReader documents `.gts` extraction for GTS v6 only; Bannerlord/version-specific support and direct TPAC or standalone `.gtex` extraction remain unverified. See [bannerlord-resource-browser](../skills/bannerlord-resource-browser/SKILL.md).
- Record rendering and interaction only after observing the actual panel. A source audit or Resource Browser import alone does not establish live UI behavior.
- **Campaign Rule Builder interaction evidence:** The selected row, editor fields, and sample preview can represent the mutable rule currently being edited; cycling its event/action changes that same rule live. A visible row or changing label does not prove that `Add rule` added the user's intended rule, that it appeared as an additional row, or that the draft was saved. In this live review, the user reported that the intended new rule never appeared while the active editor row remained visible. Treat the cause as unresolved: confirm the Add command was received, compare visible row count and stable IDs before/after, and verify `Save draft` separately. Never infer an Add click or save from row visibility.

## Optional WPF Operation-Level Styling Examples (Rev031+)
- The following WPF examples may guide task-specific styling. They do not require distinct action verbs, icons, or accent colors on every route; retain the established command and accessible name contracts.
- **Domain & Category Differentiation:**
  - **Simulations (Verdigris):** Active simulation verbs (e.g., *"⚡ Simular Progresión"*, *"🔬 Analizar Espectro"*, *"📊 Simular Economía"*, *"🧠 Auditar Memoria"*), `VerdigrisBrush` accent border, and thematic icon.
  - **Audits, Diagnostics & Security (Worn Brass / Tempered):** Specific inspection verbs (e.g., *"🛡️ Auditar Ensamblado"*, *"🔍 Verificar Anti-Shadowing"*, *"📋 Validar Manifiesto"*), `BrassBrush` accent border, and shield/magnifier iconography.
  - **Generators & Scaffolding (Pine Felt / Worn Gold):** Generative verbs (e.g., *"⚙️ Generar Andamiaje"*, *"📜 Sintetizar XML"*, *"🔨 Forjar Componente"*), `DeepPineBrush` accent border, and gear/scroll iconography.
  - **Live Sessions & IPC Telemetry (Ember / Azure):** Query or dispatch verbs (e.g., *"📡 Consultar Telemetría"*, *"⚡ Despachar Evento"*), `EmberBrush` accent border, and radar/pulse iconography.
- **Secondary Quick-Actions:** Add a quick action such as preset loading or input clearing only when the route has a defined, safe behavior for it.
- **Contract & AutomationId Invariants:** Contextual personalization MUST preserve all underlying standard command bindings (`RunCommand`, `CancelCommand`, `ExportCommand`, `BrowseFileCommand`, `BrowseFolderCommand`) and their static accessibility identifiers (`AutomationProperties.AutomationId`) to avoid breaking static contract analysis or WPF render verification suites.

## Optional WPF Identity and Visualizer Examples (Rev032+)
- Where useful, a WPF route can use a distinct visual identity. The examples below are optional and must not create unsupported claims or crowd technical content:
  - **Category Banner:** Tactical military department banner (e.g. `[DIAGNOSTICS & SYSTEM SAFETY RADAR]`, `[HIGH SENATE & DIPLOMATIC CORPS]`, `[GAUNTLET UI & SHADER FORGE]`).
  - **Optional Motto:** Use decorative copy only when it improves orientation; never require Latin text on each route.
  - **Category Accent Brush (`Tool.CategoryAccentBrushKey`):** Dynamically bound border accents, icon glyph backgrounds, and title kicker typography (`BrassBrush`, `VerdigrisBrush`, `EmberBrush`, `DeepPineBrush`).
  - **Input Domain Badge (`Tool.InputDomainBadge`):** Clear input classification pill (e.g. `[TARGET ASSEMBLY / PE BINARY / MANIFEST]`, `[TARGET FACTION / SENATE POLICY / CLAN]`).
  - **Security Scope Pill (`Tool.SecurityPillText`):** Explicit operational safety boundaries (`[ STRICT BOUNDED ROUTE · ZERO IN-GAME SIDE EFFECTS · THREAD ISOLATED ]`).
- **Domain Visual Dashboards (`HasVisualDashboard`):** When the task calls for a graphical studio and the data supports it, possible XAML visualizations include:
  1. **PE Assembly Security & CLR Invariant Radar:** Visual risk score gauge, four architectural compliance cards (Rule A, Rule B, Rule C, Rule D), and CLR metadata stream telemetry.
  2. **Module Topology & Pipeline DAG:** Horizontal directed acyclic graph stages showing load order sequencing, dependency counts, and SubModule.xml schema verification.
  3. **Geopolitical Diplomacy & Senate Chamber:** Regional tension barometers, dynastic succession heir scores, and multi-faction stance cards with border conflict probability progress meters.
  4. **Component Blueprint Synthesis:** Four-stage pipeline cards (UI View -> ViewModel -> Domain Model -> Unit Tests) with member counts and mixer compliance indicators.
- **Minimum Viewport Layout Invariant:** Visual canvas cards (`AdaptiveVisualCanvasCard`) must evaluate `HasVisualDashboard` to `false` for standard file analyzer tools (such as `SubModuleValidator`) to prevent visual tree overflow beyond the 980x680 DIP minimum application viewport during non-simulation test suites.

## Ocultación Condicional de Entrada y Dossier Táctico de Comandos (Rev033+)
- **Ocultación Dinámica de Barra de Entrada:**
  - En herramientas y estudios donde `Tool.RequiresInput == false`, la caja de texto `ActiveToolInput` (y la tarjeta de entrada si no hay parámetros) debe ocultarse automáticamente (`Visibility.Collapsed`), preservando el espacio para los lienzos interactivos y las botoneras operativas pertinentes.
  - La preservación de contratos MVVM exige que la propiedad `Input` y la lógica interna de validación permanezcan intactas en el ViewModel aunque el control visual esté colapsado.
- **Dossier informativo opcional:**
  - Una tarjeta de referencia (`CommandReferenceCard` o `SectionDossierCard`) puede ayudar cuando los comandos documentados estén confirmados. No inventes comandos ni añadas este panel a cada ruta por defecto. Si se incluye, puede detallar:
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

## Pipeline de Activos Visuales y Generación de Imágenes de Alta Calidad (Rev082+)
- **Generación y Normalización de Activos Gráficos:**
  - Para la creación de nuevos iconos, heráldica, texturas de pergamino o banners de presentación, aplicar la skill [`high-quality-image-generation`](../skills/high-quality-image-generation/SKILL.md) siguiendo la arquitectura de 9 dimensiones de intención visual.
  - Todo activo destinado al motor TaleWorlds o Gauntlet UI debe someterse a `tools/process_high_quality_asset.py` para garantizar dimensiones Power-of-Two (POT), recorte alfa sin halos oscuros (dilatación de color) y registro de integridad SHA-256.

## Composición Táctica Tripartita y Dimensionamiento Elástico (Rev101/Rev135)
- **Estándar Tripartito de Plantillas de Estudio (ToolPageTemplates.xaml):**
  - **Medallón Dual Temático:** Todo estudio táctico debe presentar en su encabezado un marco biselado de latón con escalado de alta calidad emparejando el sello del dominio con un medallón táctico soberano (ej. `calradia-tactical-emblem-rev100.png` o `calradia-aquila-seal-rev087.png`).
  - **Tarjetas KPI con Acento de 3px:** Los paneles de métricas deben utilizar bordes con acento inferior `BorderThickness="1,1,1,3"`, fondos `CoalBrush` o `FrameSurfaceSolidBrush`, y mini-barras proporcionales `<ProgressBar Height="4" ... Mode=OneWay .../>` para indicadores clave (rentabilidad, cohesión, riesgo de rebelión, latencia).
  - **Terminales Monoespaciados Tipo Dossier:** Los bloques de comandos deben utilizar tipografía `Consolas`, prompt `$ `, enlace a comandos curados del ViewModel y botón de copiado rápido.
- **Dimensionamiento Elástico de Insignias Compuestas:**
  - Las insignias que combinan texto e indicadores LED o iconos deben contar con holgura horizontal suficiente (`MaxWidth >= 64px`, contenedor `>= 126px`) para evitar truncamiento por elipsis (`TACTI…`) en diferentes escalas de visualización DPI.
- **Invariantes Estructurales en XAML:**
  - Preservar exactamente 9 `DashboardTemplate` en `ToolPageTemplates.xaml` (Regla C).
  - Cero enlaces `TwoWay` en elementos `Run.Text` (Propuesta 48; usar siempre `Mode=OneWay` o bindings estáticos).
