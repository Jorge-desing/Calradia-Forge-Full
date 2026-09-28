---
name: game-ui-design
description: World-class game UI/UX design architecture for Mount & Blade II: Bannerlord Gauntlet XML prefabs and Calradia Forge .NET 8 WPF Desktop Workbench. Tactical military aesthetics, F10 hotkey rising-edge polling, responsive scaling (1080p to 4K), WCAG AAA contrast, and accessible keyboard/gamepad navigation.
risk: safe
source: Calradia Forge Agent Ecosystem (Apache 2.0)
date_added: 2026-09-28
---

# Game UI Design: Gauntlet XML Prefabs & Tactical Desktop Workbench

User interface design in Mount & Blade II: Bannerlord bridges two distinct domains: in-game tactical overlays rendered by TaleWorlds' proprietary **Gauntlet UI** engine, and developer workbench tooling built on modern **.NET 8 WPF**. A master game UI designer understands both environments, maintaining immersive tactical aesthetics, robust input polling, high-contrast readability, and seamless responsive scaling across resolutions from 1080p to 4K.

---

## 1. Core Principles

1. **Dual UI Architecture**:
   - **In-Game Overlay (Gauntlet UI)**: Lightweight XML prefabs rendered in the game engine viewport, bound to C# ViewModels via `TaleWorlds.Library.ViewModel` with properties decorated by `[DataSourceProperty]`.
   - **Desktop Workbench (WPF MVVM)**: Standalone assembly and telemetry tool, styled with tactical resource dictionaries (`DeepPineBrush`, `BrassBrush`, `VerdigrisBrush`, `CoalBrush`), vector charts, and strict thread isolation.
2. **Tactical Military Aesthetics & WCAG AAA Contrast**:
   - Calradia Forge employs a cohesive tactical palette:
     - `DeepPineBrush` (`#0D1B1E`): Base canvas and card background.
     - `CoalBrush` (`#121517`): Recessed telemetry trays and dark containers.
     - `BrassBrush` (`#C8963E`): Primary actions, gold emblems, and important headings.
     - `VerdigrisBrush` (`#2A9D8F`): Active indicators, telemetry trends, and passing rules.
     - `EmberBrush` (`#E76F51`): Warnings, error thresholds, and combat casualties.
     - `PaperBrush` (`#EAE0D5`): High-legibility text ensuring WCAG AAA contrast ratio ($\ge 7:1$).
3. **Robust Hotkey Polling with Rising-Edge Fallback**:
   - TaleWorlds' `InputKey.F10` polling can drop keystrokes during intensive frame ticks if using only `Input.IsKeyPressed`.
   - Always implement an F10-only rising-edge fallback using `IsKeyDown` / `IsKeyDownImmediate` combined with an edge-detection latch.
4. **980x680 DIP Minimum Surface Contract**:
   - Desktop workbench views must fit within a `980x680` DIP minimum viewport without vertical clipping or horizontal truncation of the evidence ledger.

---

## 2. Capabilities & Scope

### Capabilities
- `gauntlet-xml-prefab-authoring`: Constructs clean Gauntlet XML layouts with responsive anchors and clipping.
- `datasource-viewmodel-binding`: Connects C# properties and commands to Gauntlet widgets cleanly.
- `tactical-palette-styling`: Applies cohesive brushes, typography, and corner radiuses.
- `rising-edge-input-polling`: Implements reliable hotkey listeners that never drop keystrokes.
- `responsive-viewport-budgeting`: Ensures UI cards scale gracefully across minimum and maximum display sizes.

### Scope
- **In Scope**: In-game Gauntlet UI panels (`Modules/CalradiaForge/GUI/Prefabs/`), Desktop WPF views (`src/CalradiaForge.Desktop/Resources/Views/`), theme dictionaries.
- **Out of Scope**: 3D world map mesh rendering (delegate to `bannerlord-map-visuals`).

---

## 3. Concrete Game UI Patterns

### Pattern 1: Gauntlet Prefab XML Structure
Gauntlet XML layout for an in-game telemetry overlay card.

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
        <BrushWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" 
                     Brush="CalradiaForge.Panel.DeepPine" />
        
        <!-- Content Stack -->
        <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" 
                   StackLayout.LayoutMethod="VerticalBottomToTop" MarginLeft="@Card.Padding" MarginRight="@Card.Padding">
          <Children>
            <!-- Title Header -->
            <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" 
                        Text="@CategoryTitle" Brush="CalradiaForge.Text.Brass" />
            <!-- Status Metric -->
            <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" 
                        Text="@ActiveTelemetrySummary" Brush="CalradiaForge.Text.Paper" />
          </Children>
        </ListPanel>
      </Children>
    </Widget>
  </Window>
</Prefab>
```

### Pattern 2: F10 Rising-Edge Hotkey Polling
Guarantees reliable toggling of in-game developer panels.

```csharp
public class ForgeInputManager
{
    private bool _wasF10DownLastTick;

    public bool CheckF10HotkeyTrigger()
    {
        // 1. Primary engine check
        bool isPressed = Input.IsKeyPressed(InputKey.F10);

        // 2. Rising-edge fallback check using IsKeyDownImmediate
        bool isDownNow = Input.IsKeyDownImmediate(InputKey.F10) || Input.IsKeyDown(InputKey.F10);
        bool isRisingEdge = isDownNow && !_wasF10DownLastTick;
        _wasF10DownLastTick = isDownNow;

        return isPressed || isRisingEdge;
    }
}
```

### Pattern 3: Desktop ViewTemplate Three-Column Layout
Enforces tactical studio structure while respecting the 980x680 DIP minimum surface.

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

### Edge 2: Inventing Non-Existent Geometry Keys in XAML
- **Severity**: CRITICAL
- **Symptom**: `XamlParseException` on workbench startup: *Resource not found: Icon.NewStudio*.
- **Root Cause**: Guessing icon key names instead of verifying against `TacticalIcons.xaml` or `GameIcons.xaml`.
- **Fix**: Always verify icon keys against the canonical resource dictionaries before referencing in XAML.

### Edge 3: Violating 980x680 DIP Viewport Cutoff
- **Severity**: HIGH
- **Symptom**: `AssertEmptyLedgerFitsMinimumSurface` test fails; users on smaller displays cannot see the action buttons without scrolling.
- **Root Cause**: Adding fixed-height cards that push the evidence ledger below the 640 DIP vertical threshold.
- **Fix**: Keep default tool cards collapsed (`HasVisualDashboard == false` for `toolArray[0]`) and bound inner scrollable areas with `MaxHeight="220"`.

---

## 5. Validation Rules & Verification Checklist

1. [ ] **`[DataSourceProperty]` Decorators**: All properties exposed to Gauntlet XML prefabs have `[DataSourceProperty]`.
2. [ ] **Valid Vector Geometry Keys**: All `<Path Data="{StaticResource ...}">` reference existing keys in `TacticalIcons.xaml` or `GameIcons.xaml`.
3. [ ] **F10 Rising-Edge Fallback**: Input handlers combine `IsKeyPressed` with an `IsKeyDownImmediate` edge latch.
4. [ ] **980x680 DIP Contract**: Headless render tests confirm layout fits within minimum surface bounds without vertical clipping.
5. [ ] **WCAG AAA Contrast**: Text elements maintain high contrast against backgrounds using `PaperBrush` or `BrassBrush`.
