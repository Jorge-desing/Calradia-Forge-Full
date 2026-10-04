---
name: game-ui-design
description: "Design guidance for Mount & Blade II: Bannerlord Gauntlet XML prefabs and the Calradia Forge .NET 8 WPF workbench. Covers tactical visual hierarchy, resource verification, accessible navigation, measured contrast, responsive layout checks, and the repository's existing F10 input-edge integration."
metadata:
  risk: safe
  source: Calradia Forge Agent Ecosystem (Apache 2.0)
  date_added: "2026-09-28"
---

# Game UI Design: Gauntlet XML Prefabs & Tactical Desktop Workbench

User interface design in Mount & Blade II: Bannerlord bridges two distinct domains: in-game tactical overlays rendered by TaleWorlds' proprietary **Gauntlet UI** engine, and developer workbench tooling built on modern **.NET 8 WPF**. Treat their resource systems, input models, and validation paths separately. Source contracts, headless rendering, and live engine behavior are different kinds of evidence; report each one explicitly. The project-specific F10 integration is described below, but it is not a universal guarantee that every key event or game version will behave identically.

---

## 1. Core Principles

1. **Dual UI Architecture**:
   - **In-Game Overlay (Gauntlet UI)**: Lightweight XML prefabs rendered in the game engine viewport, bound to C# ViewModels via `TaleWorlds.Library.ViewModel` with properties decorated by `[DataSourceProperty]`.
   - **Desktop Workbench (WPF MVVM)**: Standalone assembly and telemetry tool, styled with tactical resource dictionaries (`DeepPineBrush`, `BrassBrush`, `VerdigrisBrush`, `CoalBrush`), vector charts, and strict thread isolation.
2. **Tactical Military Aesthetics & Measured Contrast**:
   - WPF theme resources include a tactical palette. In the current War Table dictionary, the base values are `DeepPineBrush` (`#13231E`), `CoalBrush` (`#0D1714`), `BrassBrush` (`#C7A45A`), `VerdigrisBrush` (`#6FB183`), `EmberBrush` (`#BC6542`), and `PaperBrush` (`#F1E6C8`). Parchment and High Contrast override these values; inspect the active theme dictionary before use.
   - Gauntlet uses its own registered brushes. WPF brush names and color values do not automatically exist in Gauntlet.
   - Do not claim WCAG AAA (or another contrast level) from a palette name or color swatch alone; calculate the rendered foreground/background pair and inspect relevant states.
3. **Use the Existing F10 Input-Edge Integration**:
   - `src/CalradiaForge.Mod/SubModule.cs` routes configured F10 input through `F10InputEdgeGate` in `OnApplicationTick`, combining `IsKeyPressed`, `IsKeyDown`, and `IsKeyDownImmediate`. The gate emits one edge while any signal remains present, rearms after all clear, and is reset when the configured hotkey is not F10.
   - Preserve this integration and its tests instead of introducing a second `ForgeInputManager` or a separate latch. Other configured hotkeys use the normal `IsKeyPressed` path.
   - The gate's unit tests cover synthetic signal sequences; they do not guarantee input delivery, focus behavior, or panel rendering in every live game/version. Verify live behavior separately when the task requires it.
4. **980x680 DIP Minimum Surface Contract**:
   - Desktop workbench views must fit within a `980x680` DIP minimum viewport without vertical clipping or horizontal truncation of the evidence ledger.

---

## 2. Capabilities & Scope

### Capabilities
- `gauntlet-xml-prefab-authoring`: Guides Gauntlet XML layout construction with responsive anchors and clipping; verify the target engine's accepted attributes and resources.
- `datasource-viewmodel-binding`: Connects C# properties and commands to Gauntlet widgets cleanly.
- `tactical-palette-styling`: Applies cohesive brushes, typography, and corner radiuses.
- `f10-input-edge-integration`: Maintains the existing `F10InputEdgeGate`/`SubModule` path and regression coverage without promising universal key delivery.
- `responsive-viewport-budgeting`: Plans and validates layouts at explicitly tested viewport sizes and content lengths.

### Scope
- **In Scope**: In-game Gauntlet UI panels (`Modules/CalradiaForge/GUI/Prefabs/`), Desktop WPF views (`src/CalradiaForge.Desktop/Resources/Views/`), theme dictionaries.
- **Out of Scope**: 3D world map mesh rendering (delegate to `bannerlord-map-visuals`).

---

## 3. Concrete Game UI Patterns

### Pattern 1: Gauntlet Prefab XML Structure
Schematic layout for an in-game telemetry card. `CalradiaForge.HeaderGold` and `CalradiaForge.TerminalText` are defined in the current module's `GUI/Brushes/CalradiaForge.xml`. A prefab reference alone does not register a brush: ensure the definition file is discovered and packaged by the target module's resource-loading setup, then verify the names resolve in the target game before copying this layout.

```xml
<Prefab>
  <Constants>
    <Constant Name="Card.Width" Value="380" />
    <Constant Name="Card.Padding" Value="12" />
  </Constants>
  <VisualDefinitions>
  </VisualDefinitions>
  <Window>
    <Widget WidthSizePolicy="Fixed" HeightSizePolicy="CoverChildren" 
            SuggestedWidth="@Card.Width" MarginLeft="16" MarginTop="16"
            HorizontalAlignment="Left" VerticalAlignment="Top">
      <Children>
        <!-- Background Frame -->
        <Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent"
                Sprite="BlankWhiteSquare_9" Color="#142019FF" />
        
        <!-- Content Stack -->
        <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" 
                   StackLayout.LayoutMethod="VerticalBottomToTop" MarginLeft="@Card.Padding" MarginRight="@Card.Padding">
          <Children>
            <!-- Title Header -->
            <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" 
                        Text="@CategoryTitle" Brush="CalradiaForge.HeaderGold" />
            <!-- Status Metric -->
            <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" 
                        Text="@ActiveTelemetrySummary" Brush="CalradiaForge.TerminalText" />
          </Children>
        </ListPanel>
      </Children>
    </Widget>
  </Window>
</Prefab>
```

### Pattern 2: Existing F10 Edge Gate in `SubModule`
Use the current integration rather than duplicating the gate. This excerpt follows `src/CalradiaForge.Mod/SubModule.cs`; it describes the repository's implementation, not a universal input guarantee.

```csharp
// Excerpt from SubModule.OnApplicationTick; names are owned by that class.
if (cachedHotkey == InputKey.F10)
{
    hotkeyPressed = f10InputGate.Poll(
        keyPressed,
        Input.IsKeyDown(InputKey.F10),
        Input.IsKeyDownImmediate(InputKey.F10));
}
else f10InputGate.Reset();
```

### Pattern 3: Desktop ViewTemplate Three-Column Layout
Illustrative WPF layout. The 980x680 DIP minimum is a project workbench contract; validate the affected view and its longest supported content through maintained render checks rather than inferring that every template fits from this sample.

```xaml
<Grid Grid.Row="1">
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="1.35*"/> <!-- Triage & Item List -->
    <ColumnDefinition Width="1.1*"/>  <!-- Pentagonal Radar Profile -->
    <ColumnDefinition Width="1.2*"/>  <!-- Latency Sparkline & Gauges -->
  </Grid.ColumnDefinitions>

  <!-- Left Column: Bounded ScrollViewer to prevent vertical overflow -->
  <Border Grid.Column="0" Background="{DynamicResource RailSurfaceBrush}" 
          BorderBrush="{DynamicResource BorderBrush}" BorderThickness="1" Padding="8">
    <ScrollViewer VerticalScrollBarVisibility="Auto" MaxHeight="220">
      <!-- Item list content -->
    </ScrollViewer>
  </Border>
</Grid>
```

---

## 4. Sharp Edges & Anti-Patterns

### Edge 1: Missing `[DataSourceProperty]` on Gauntlet ViewModel Properties
- **Severity**: HIGH
- **Symptom**: Gauntlet UI displays blank text or fails to update values when ViewModel properties change.
- **Root Cause**: TaleWorlds' reflection-based Gauntlet binding engine ignores public properties that lack the `[DataSourceProperty]` attribute.
- **Fix**: Decorate all exposed ViewModel getters and setters with `[DataSourceProperty]`.

### Edge 2 (WPF-only): Inventing Non-Existent Resource Keys in XAML
- **Severity**: CRITICAL
- **Scope**: WPF XAML dictionaries only; Gauntlet XML uses the game's own widget, brush, and sprite registration system.
- **Symptom**: `XamlParseException` on workbench startup: *Resource not found: Icon.NewStudio*.
- **Root Cause**: Guessing a WPF resource key instead of verifying it in the actual merged resource dictionaries.
- **Fix**: Verify WPF resource keys against the dictionaries loaded by the affected view. Do not treat `TacticalIcons.xaml` or `GameIcons.xaml` as Gauntlet resources.

### Edge 3 (WPF-only): Violating the 980x680 DIP Viewport Contract
- **Scope**: Desktop WPF workbench render/layout checks. This is not a Gauntlet viewport contract.
- **Severity**: HIGH
- **Current regression contract**: `AssertEmptyLedgerFitsMinimumSurface` checks the empty-ledger title and action bounds against `ResponsiveWorkbenchScrollViewport` at the actual `980x680` DIP minimum, including a minimum 32-DIP action height. It does not define a universal 640-DIP ledger threshold.
- **Root Cause**: Shell/header growth, clipping, or fixed content can move the empty-state action outside the measured viewport. The default catalog route is a standard route; dashboard and Split Deck routes have separate responsive checks.
- **Fix**: Preserve measured bounds for the affected route and state, and exercise the relevant standard/dashboard cases in the render harness. Do not use catalog index (`toolArray[0]`) or an inner `MaxHeight="220"` from another template as a universal layout workaround.

---

## 5. Validation Rules & Verification Checklist

1. [ ] **`[DataSourceProperty]` Decorators**: All properties exposed to Gauntlet XML prefabs have `[DataSourceProperty]`.
2. [ ] **Resources**: For Gauntlet, verify widget/brush/sprite names in the module's registered resources. For WPF only, verify vector geometry `StaticResource` keys in the loaded XAML dictionaries.
3. [ ] **F10 Integration**: Preserve `F10InputEdgeGate` in `SubModule.OnApplicationTick`; confirm synthetic edge tests pass and separately record whether live input/panel behavior was observed.
4. [ ] **WPF 980x680 DIP Contract**: Run relevant Desktop render cases at the minimum surface and inspect measured bounds for the affected view, long text, and state variants.
5. [ ] **Contrast**: Measure actual text/background combinations and focus/selection states; color names are not evidence of WCAG conformance.

### Evidence Boundaries in Maintained UI Launchers

- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat` validates decorative-sprite inputs, runs `tools/audit_gauntlet_ui.py` for source-level prefab/binding/layout contracts, and then runs Core tests. The auditor explicitly does not replace an in-game render check; a green BAT does not prove that Bannerlord imported the atlas or rendered the prefab.
- `tools/Run-CalradiaForge-Desktop-Render-Tests.bat` runs the WPF render/resource harness on its isolated test desktop. Its render cases are evidence about WPF and the harness, not Gauntlet, native Windows DPI behavior, or live game interaction.
- Keep these claims separate in reports: static/source audit, packaged/imported resource inspection (for example, Resource Browser), off-screen WPF harness render, and actual Bannerlord UI render/input are distinct verification states. Promote a claim to imported or live-verified only after observing that exact stage.
- **Campaign Rule Builder state semantics:** The selected row, editor controls, and sample preview can be the active mutable working rule; cycling the event/action edits that rule live. Do not treat its presence or changing caption as proof that `Add rule` created the intended rule or that `Save draft` persisted it. A live user report says the intended new rule never appeared while the current editor row remained visible. Keep that discrepancy unresolved until an actual Add interaction produces a separately observable row-count/stable-ID change; test saving and reloading separately. ViewModel tests cannot establish Gauntlet click delivery or row rendering.
