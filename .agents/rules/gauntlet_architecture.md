# Gauntlet UI Architecture & MVVM Constraints

Gauntlet is TaleWorlds' proprietary declarative MVVM user interface framework. It powers all Bannerlord screens, HUD overlays, and campaign panels using XML layouts in `GUI/Prefabs/` paired with C# ViewModels.

---

## 1. Core Architecture (Screen → Layer → Movie → ViewModel)

To render a custom Gauntlet interface, you must instantiate three components:
1. **`ScreenBase`**: Manages the screen stack, focus state, and lifecycle.
2. **`GauntletLayer`**: A rendering and input layer attached to the screen.
3. **`IGauntletMovie`**: The compiled XML layout binding the layer to a `ViewModel`.

```csharp
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaForge.UI.Screens
{
    public class CustomScreen : ScreenBase
    {
        private GauntletLayer _gauntletLayer;
        private CustomViewModel _viewModel;
        private IGauntletMovie _movie;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _viewModel = new CustomViewModel();

            // Layer priority 100 for foreground modal screens
            _gauntletLayer = new GauntletLayer(100) { IsFocusLayer = true };

            // CRITICAL: Grant mouse interaction to the layer
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.Mouse);

            // Loads GUI/Prefabs/CustomScreenPrefab.xml
            _movie = _gauntletLayer.LoadMovie("CustomScreenPrefab", _viewModel);
            AddLayer(_gauntletLayer);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            ScreenManager.TrySetFocus(_gauntletLayer);
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            _gauntletLayer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(_gauntletLayer);
        }

        protected override void OnFinalize()
        {
            if (_gauntletLayer != null)
            {
                RemoveLayer(_gauntletLayer);
                _gauntletLayer.ReleaseMovie(_movie);
                _gauntletLayer = null;
            }
            _viewModel = null;
            base.OnFinalize();
        }
    }
}
```

---

## 2. ViewModel Contracts & Data Binding

All ViewModels MUST inherit from `TaleWorlds.Library.ViewModel`.

### Data Binding Attributes:
- **`[DataSourceProperty]`**: Exposes properties to the XML prefab.
- **`OnPropertyChangedWithValue(value)`**: Triggers the Gauntlet binding engine to re-render. Always use this instead of standard WPF `PropertyChanged`:

```csharp
public class CustomViewModel : ViewModel
{
    private string _statusText;
    private bool _isActionAvailable;

    [DataSourceProperty]
    public string StatusText
    {
        get => _statusText;
        set
        {
            if (value != _statusText)
            {
                _statusText = value;
                OnPropertyChangedWithValue(value);
            }
        }
    }

    [DataSourceProperty]
    public bool IsActionAvailable
    {
        get => _isActionAvailable;
        set
        {
            if (value != _isActionAvailable)
            {
                _isActionAvailable = value;
                OnPropertyChangedWithValue(value);
            }
        }
    }

    // Method bound to XML via Command.Click="ExecuteConfirm"
    public void ExecuteConfirm()
    {
        // Business logic
        ScreenManager.PopScreen();
    }
}
```

---

## 3. Core Predefined XML Widgets

The XML element name MUST **exactly match** the C# widget class name:

| XML Tag | C# Class | Usage |
|:---|:---|:---|
| `<Widget>` | `Widget` | Generic container, anchor point, or background panel |
| `<ButtonWidget>` | `ButtonWidget` | Clickable button with hover/pressed states |
| `<TextWidget>` | `TextWidget` | Single-line or wrapped text block |
| `<RichTextWidget>` | `RichTextWidget` | Formatted text supporting embedded sprite icons (`<img src="..."/>`) |
| `<ImageWidget>` | `ImageWidget` | Texture or sprite rendering (`Brush="..."` or `Sprite="..."`) |
| `<ListPanel>` | `ListPanel` | Flex-like stack panel (`StackLayout.LayoutMethod="VerticalBottomToTop"` or `"HorizontalLeftToRight"`) |
| `<ScrollablePanel>`| `ScrollablePanel` | Viewport container with scrollbar integration |
| `<EditableTextWidget>`| `EditableTextWidget` | Text input box |

---

## 4. Critical Engine Constraints & Bug Workarounds

### A. The Hyphenation Auto-Wrap Bug:
If a `TextWidget` has a `Brush.FontSize` that exceeds its parent container's width, Gauntlet automatically injects hyphens into the words (e.g. turning `"CALRADIA"` into `"CAL- RADIA"`).
- **Prevention:** Always set `WidthSizePolicy="CoverChildren"` on the `TextWidget` or ensure the parent container width is sufficiently sized.

### B. Event Consumption by Decorative Watermarks:
Background textures, frames, and watermark images without click events will still intercept mouse input by default, silently blocking clicks to buttons underneath.
- **Prevention:** Always tag decorative widgets with:
  `DoNotAcceptEvents="true" DoNotPassEventsToChildren="true"`

### C. ScrollablePanel Syntax (Not WPF):
Never use WPF property element syntax (`<ScrollablePanel.InnerPanel>`). Gauntlet connects panels through sibling ID references:
```xml
<ScrollablePanel Id="MyScroll"
                 ClipRect="Clip"
                 InnerPanel="Clip\Inner"
                 VerticalScrollbar="ScrollBar">
  <Children>
    <Widget Id="Clip">
      <Children>
        <ListPanel Id="Inner" />
      </Children>
    </Widget>
    <ScrollbarWidget Id="ScrollBar" />
  </Children>
</ScrollablePanel>
```

### D. EditableTextWidget Two-Way Binding Delay:
By default, `EditableTextWidget` only updates its bound property when the user hits Enter.
- **Prevention:** Always specify `UpdateTextOnTyping="true"` so that the bound ViewModel property updates keystroke-by-keystroke before submit buttons are pressed.

### E. Empty String Visual Artifacts:
When a bound string property is empty (`""`), Gauntlet still renders the container's brush background, borders, and margins, creating orphaned visual boxes.
- **Prevention:** Bind container visibility to a boolean flag: `IsVisible="@HasContent"`.

### F. Reported Hot-Reload Workflow (Unverified)
Some development notes report enabling a UI debug mode with `Ctrl + ~` and `ui.toggle_debug_mode` before editing Gauntlet XML. Availability and behavior depend on the installed Bannerlord version and have not been verified for this project baseline. Treat this as an optional research lead, not a supported Forge workflow or validation gate; confirm it in the target game's documentation or a controlled live session before relying on it.
