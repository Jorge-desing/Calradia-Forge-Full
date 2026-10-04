---
name: bannerlord-gauntlet-ui
description: Best practices, XML schema, and C# ViewModel integration for creating Gauntlet UIs in Bannerlord.
---

# Gauntlet UI Framework

Gauntlet is Bannerlord's proprietary MVVM framework.

> **Note:** This skill covers pure UI patterns (ScreenBase, GauntletLayer, ViewModel). For campaign-side data binding or behavior registration, see `bannerlord-shared-patterns`.

---

## 1. C# Setup (Screen & ViewModel)

To render a custom Gauntlet interface, you need a `ScreenBase`, a `GauntletLayer`, and a `ViewModel`.

```csharp
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaForge.UI.Screens
{
    public class MyCustomScreen : ScreenBase
    {
        private GauntletLayer _gauntletLayer;
        private MyViewModel _dataSource;
        private IGauntletMovie _movie;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _dataSource = new MyViewModel();
            _gauntletLayer = new GauntletLayer(100) { IsFocusLayer = true };
            
            // Critical for allowing mouse clicks
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.Mouse);
            
            // Load the XML Prefab (GUI/Prefabs/MyCustomUI.xml)
            _movie = _gauntletLayer.LoadMovie("MyCustomUI", _dataSource);
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
            _dataSource = null;
            base.OnFinalize();
        }
    }
}
```

---

## 2. ViewModel Binding & Dynamic Collections

All properties exposed to the XML must use `[DataSourceProperty]`.
Collections must use `MBBindingList<T>` to notify Gauntlet on item addition/removal:

```csharp
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaForge.UI.ViewModels
{
    public class MyViewModel : ViewModel
    {
        private string _titleText;
        private MBBindingList<EntryItemVM> _entries = new MBBindingList<EntryItemVM>();

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (value != _titleText)
                {
                    _titleText = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<EntryItemVM> Entries
        {
            get => _entries;
            set
            {
                if (value != _entries)
                {
                    _entries = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        // Called from XML via Command.Click="ExecuteClose"
        public void ExecuteClose()
        {
            ScreenManager.PopScreen();
        }
    }

    public class EntryItemVM : ViewModel
    {
        private string _label;

        public EntryItemVM(string label) => _label = label;

        [DataSourceProperty]
        public string Label
        {
            get => _label;
            set
            {
                if (value != _label)
                {
                    _label = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }
    }
}
```

---

## 3. Complete XML Prefab Template (`GUI/Prefabs/MyCustomUI.xml`)

```xml
<Prefab>
  <Window>
    <!-- Modal Backdrop -->
    <Widget WidthSizePolicy="Fixed" SuggestedWidth="650"
            HeightSizePolicy="Fixed" SuggestedHeight="450"
            HorizontalAlignment="Center" VerticalAlignment="Center"
            Sprite="StdAssets\Background\paper_panel_dark">
      <Children>
        <!-- Header Panel -->
        <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren"
                   MarginTop="15" MarginLeft="20" MarginRight="20">
          <Children>
            <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                        Text="@TitleText" Brush="CalradiaForge.Gold" Brush.FontSize="26" />
          </Children>
        </ListPanel>

        <!-- Scrollable List of Entries -->
        <ScrollablePanel WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent"
                         MarginTop="60" MarginBottom="70" MarginLeft="20" MarginRight="20"
                         ClipRect="Clip" InnerPanel="Clip\ListContainer" VerticalScrollbar="ScrollBar">
          <Children>
            <Widget Id="Clip" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" ClipContents="true">
              <Children>
                <ListPanel Id="ListContainer" WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren"
                           StackLayout.LayoutMethod="VerticalBottomToTop">
                  <ItemTemplate>
                    <ButtonWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="40"
                                  Brush="CalradiaForge.RowButton">
                      <Children>
                        <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                                    HorizontalAlignment="Left" VerticalAlignment="Center" MarginLeft="10"
                                    Text="@Label" Brush="CalradiaForge.Text" />
                      </Children>
                    </ButtonWidget>
                  </ItemTemplate>
                </ListPanel>
              </Children>
            </Widget>
            <ScrollbarWidget Id="ScrollBar" WidthSizePolicy="Fixed" SuggestedWidth="10"
                             HeightSizePolicy="StretchToParent" HorizontalAlignment="Right" />
          </Children>
        </ScrollablePanel>

        <!-- Footer Action Button -->
        <ButtonWidget WidthSizePolicy="Fixed" SuggestedWidth="140" HeightSizePolicy="Fixed" SuggestedHeight="40"
                      HorizontalAlignment="Right" VerticalAlignment="Bottom" MarginRight="20" MarginBottom="15"
                      Brush="CalradiaForge.ActionBtn" Command.Click="ExecuteClose">
          <Children>
            <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="CoverChildren"
                        HorizontalAlignment="Center" VerticalAlignment="Center"
                        Text="{=forge_close}Close" Brush="CalradiaForge.Text" />
          </Children>
        </ButtonWidget>
      </Children>
    </Widget>
  </Window>
</Prefab>
```

---

## 4. XML Prefab Constraints & Best Practices

- **Widget Tag Names:** The XML tag MUST match the C# class name exactly (e.g., `<ButtonWidget>`, `<TextWidget>`, `<ScrollablePanel>`).
- **Data Binding Syntax:** Use `@` to bind to ViewModel properties (`Text="@TitleText"`). Use `Command.` to bind methods (`Command.Click="ExecuteClose"`).
- **Hyphenation Bug Fix:** If a `TextWidget` has a large font size, Gauntlet auto-wraps and injects hyphens. Prevent this by ensuring the parent is wide enough, or setting `WidthSizePolicy="CoverChildren"` on the TextWidget.
- **Watermark Blocking:** Background decoration widgets MUST include `DoNotAcceptEvents="true"` and `DoNotPassEventsToChildren="true"`, or they will block all mouse clicks on buttons beneath them.
- **Scrollable Panels:** Do not use WPF syntax (e.g., `<ScrollablePanel.InnerPanel>`). Gauntlet uses ID references:
  `<ScrollablePanel ClipRect="Clip" InnerPanel="Clip\Inner" VerticalScrollbar="Scroll">`
- **Text Inputs:** On `EditableTextWidget`, always set `UpdateTextOnTyping="true"`. Otherwise, the ViewModel won't receive the user's text until they press the Enter key.
- **Focus and selection are separate:** TaleWorlds' [`Widget` API](https://apidoc.bannerlord.com/v/1.3.4/class_tale_worlds_1_1_gauntlet_u_i_1_1_base_types_1_1_widget.html) exposes `IsFocusable` (settable) and `IsFocused` (read-only). A focusable control is not proof that keyboard/gamepad navigation reaches it or that focus is visibly rendered. Keep the focused control visually distinguishable from a selected route, verify the actual brush/state behavior against the targeted game build, and test navigation in game; do not invent a `Focused` brush state from the property name alone.
- **Hook Workbench routing:** In the current Calradia Forge prefab, Hook Workbench is a conditional action surface inside `ForgePrimaryCommandHost`, shown only when `IsHookWorkbenchVisible` (`IsPatchPreflightActive && ShowCommandDeck`) is true. It is not a global overlay: keep the navigation rail and evidence frame as independent shell regions, and let other route/action content remain governed by its existing route state. Notify bindings for Hook Workbench visibility and dependent disabled states when the selected route or evidence-focus state changes; keep a structural regression for this composition.
- **Editable-field cue:** The official [`EditableTextWidget` API](https://apidoc.bannerlord.com/v/1.3.4/class_tale_worlds_1_1_gauntlet_u_i_1_1_base_types_1_1_editable_text_widget.html) documents `DefaultSearchText` as text shown while the field is empty and unfocused, plus a configurable `MaxLength` (512 in that API reference). Use a localized cue when it improves an unlabeled field, keep a persistent label for important inputs, and verify property/default behavior against the project's target game version before relying on it.
- **Reload evidence:** Verify console commands and resource-cache behavior against
  the installed target engine before claiming prefab reload. Debug-mode toggles or
  simulated `cf.reload_prefabs` catalog entries do not establish reload support.
  Composer output is a static prefab/ViewModel/localization bundle; validate its
  real engine load separately from XML/binding checks.

---

## 5. Calradia Forge War Table (22.0.0)

Use these project-specific rules when extending `modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml`:

- **Fixed composition:** Treat the 1220×880 prefab as the layout reference. Preserve its eight area routes and their existing bindings, commands, permissions, technical labels, and pagination. Keep changes presentational; do not rename or remove route/ViewModel contracts for visual work.
- **Readable hierarchy:** Make the active route visually distinct from inactive routes, and show keyboard focus independently from selection. Give the primary command stronger weight than secondary actions; communicate status with its existing text/state plus contrast, not color alone. Keep hit targets and focus indicators inside their control bounds.
- **Focus evidence:** The current prefab marks navigation/action buttons `IsFocusable="true"` and binds active-route selection separately through `IsSelected`; the local brushes define `Default`, `Hovered`, `Pressed`, `Selected`, and `Disabled` styles, with no dedicated `Focused` style. Treat focus appearance and keyboard/gamepad traversal as unverified until checked in the target game; source inspection of `IsFocusable` alone does not establish either.
- **Passive decoration:** Generate original decorative textures deterministically from the project's asset generator, register every sprite in both the atlas and `SpriteData`, and regenerate generated prefab/resources from their source. Repeated runs with the same inputs must produce identical pixels and dimensions. Do not add runtime network fetches or required dependencies.
- **Protect reading and interaction:** Decorative texture widgets must be passive (`DoNotAcceptEvents="true"`, `DoNotPassEventsToChildren="true"`) and must not overlap or sit above interactive controls. Keep the evidence ledger, raw evidence, editable fields, and command controls on clean, high-contrast surfaces; use ornament only on the header, rail, outer frame, and genuine section dividers.
- **Validate both source and runtime:** Source checks should verify XML validity, unique widget IDs, valid bindings/commands, brush and sprite registrations, texture dimensions/transparency, deterministic generation, and no geometric overlap with controls. These checks establish source consistency only. Verify the imported atlas/resource in Resource Browser, then inspect the panel in the game's main menu to confirm rendering, contrast, focus, and click-through behavior. Record unavailable live checks as pending; a successful build or source audit is not runtime visual verification.
- **Safe resource replacement:** Before replacing an existing TPAC, preserve a separate backup and verify its SHA-256. Stop if the destination or replacement prompt is ambiguous; do not infer successful import from file presence or a changed hash.

## 6. Opening and Reviewing the Calradia Forge Menu

- The in-game overlay opens on a singleplayer screen with `Settings.Hotkey` (default `F10`). Press the configured key again to close it. `Escape` closes the key-help popover first when it is open, otherwise it closes the overlay. The module closes the overlay in multiplayer.
- **F10 input edge:** Keep `Input.IsKeyPressed` as the ordinary hotkey signal. A live Bannerlord diagnostic found `IsKeyPressed(F10)` and `IsKeyDown(F10)` false while `IsKeyDownImmediate(F10)` was true. For F10, combine the ordinary signal with a rising edge computed from `IsKeyDown || IsKeyDownImmediate`; store the held state once per application tick and re-arm after release. Clear the latch when the configured hotkey changes away from F10. Do not toggle continuously from the immediate held state. The detailed input rule is [bannerlord_input_debug_agent.md](../../rules/bannerlord_input_debug_agent.md).
- For live review, build both Client and Modding Kit profiles first, close the Modding Kit completely, enable Calradia Forge in the Steam Launcher, enter the singleplayer main menu, and press the configured hotkey. Do not leave Bannerlord and the Modding Kit running together.
- **Campaign Rule Builder live interaction:** The left list is the mutable in-memory draft; a selected row is simply the working rule shown by the editor. The event name is a cycle button, not an event catalog or dropdown; every click advances to the next event and clears both condition groups. A valid non-empty saved draft can populate the list. Current first visit with a missing draft is empty, and only `Add rule` creates its first row. The user's reported initial row also matches a real historical implementation: commit `22359f4` seeded an unsaved starter without Add/Save, and clicking its event control edited that same stable ID; commit `08f4eaa` removed that behavior. A visible row or changed label is not proof of who added or saved it, and a capture cannot establish row provenance. Verify Add by recording count and stable IDs, clicking the actual Add control once, then observing the incremented count and new row/ID. Verify `Save draft` and reload separately. The 2026-10-04 observation of `1/8` before an agent Add click and `2/8` afterward confirms that click only, not the first row's origin or persistence.
- **Computer Use lifecycle:** Stop UI input when the live check ends and close the Computer Use surface if an exposed operation allows it. `cua_repl.js_reset` clears JavaScript bindings; it does not close native apps or the Computer Use surface. Never say Computer Use is closed based only on a reset. Verify closure through an exposed operation or the actual surface. Do not leave an active session idle or treat user-operated Escape as the agent's shutdown; if the tools cannot close the surface, state that limitation accurately. If the user has to stop an idle session manually, record it as a missed agent shutdown, not as a request to avoid Computer Use in future. This applies after each use and still permits future authorized UI work. If the user switches from Bannerlord to chat to correct an interaction, stop game input, acknowledge and incorporate the correction, then resume only if the task still needs UI review.
- Before each source correction, close Bannerlord and the Modding Kit and end/reset Ordenador/Computer Use; keep them closed through edits and builds. If the user requests or authorizes a live review, launch the Steam installation and inspect the menu directly instead of asking the user to open it. After each live check, close Bannerlord and reset the Computer Use session before another correction or task completion. Report rendering, contrast, keyboard focus, and click behavior only when observed in game.
- Keep source audit, Resource Browser atlas import, and live rendering as separate evidence. TpacTool was removed after frequent reader errors: do not install or use it as a gate, and do not interpret its old parser errors as proof of TPAC corruption. During code deployment, preserve the imported Steam TPAC and verify rendering in Bannerlord.

### Exploring Gauntlet Texture Motifs with ImageGen

- Use ImageGen to explore motif directions and composition, not as an untracked runtime dependency. After choosing a direction, make the approved final artwork a local source asset and have the project generator produce the exact sprite deterministically; keep the source and generation inputs under version control.
- Validate the generated sprite's pixel dimensions and alpha channel, deterministic output, atlas placement, and `SpriteData` registration before referencing it from a prefab.
- Place decoration only on passive surfaces. Decorative widgets must not intercept events or obscure evidence, editable fields, controls, or keyboard focus; keep those areas on clean backgrounds.

### Reproducible texture and prefab checks

- Start with `tools/Prepare-CalradiaForge-ImageGenTextures.bat --check --no-pause`. This renders two isolated temporary preparations and compares their RGBA PNG hashes; it removes its temporary workspace and does not update the canonical `assets/gauntlet-imagegen/prepared/` sprites. `tools/Test-CalradiaForge-DecorativeTextureDeterminism.bat --no-pause` additionally checks that the prepared sprites and module `SpriteParts` match the expected dimensions, alpha caps, and deterministic hashes without changing them.
- Use `--prepare` only when intentionally refreshing approved artwork. It writes the prepared PNGs under `assets/gauntlet-imagegen/prepared/`, with backup and hash checks. `tools/generate_assets.py` validates and copies the prepared PNGs into `modules/CalradiaForge/GUI/SpriteParts/ui_calradiaforge/`; rebuild the source atlas and `SpriteData` with `tools/Build-CalradiaForge-UiAssets.bat` in a visible console. That official generator step produces source atlas/metadata, not a runtime TPAC.
- `tools/Validate-CalradiaForge-DecorativeSprites.bat --no-pause` validates source PNG format/dimensions/alpha, current `SpriteData` registrations and exact atlas pixel crops, plus the approved decorative placements. `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` runs that validator, `tools/audit_gauntlet_ui.py` (prefab IDs, bindings/commands, brushes/sprites, routes, and layout intersections), and Core tests.
- These are source-pipeline checks. They do not prove that Resource Browser imported the current atlas, that the TPAC payload decodes, or that the game renders the textures. Follow [Rev025 — Gauntlet ImageGen atlas validation](../../../docs/append/Rev025-Gauntlet-imagegen-atlas-validation-2026-09-24.md) for the latest dated evidence: source checks and atlas crops passed, but the installed TPAC predates the atlas, and import/live rendering remain pending.

For the Calradia Forge War Table specifically, the current visual direction replaces earlier geometric, contour, rosette, braid, stitch, and corner motifs with subdued ImageGen-created material textures. Prefer organic woodgrain, ink-washed vellum, aged brass, or felt-like surfaces; do not reintroduce the retired geometric patterns without a new design request. These are project preferences, not general Gauntlet requirements. Check **assets/gauntlet-imagegen/README.md** for the approved masters, prompts, hashes, prepared sprite dimensions, alpha caps, and intended placements before generating duplicates. Prepare sources with **tools/Prepare-CalradiaForge-ImageGenTextures.bat**, then use the established asset build and validation scripts. Masters are authoring inputs; only deterministic prepared sprites belong in the runtime atlas. Resource Browser import and in-game rendering must still be recorded as pending until observed.

## Verified hook and delivery lessons

For Finalizer/ILHook boundaries, confirmation selection, serial measurement and packaging from a scoped snapshot, read [hook delivery lessons](../calradia-forge-dev-workflow/references/hook-delivery-lessons.md). Recheck current source and preserve the distinction between passing fixtures and pending live main-menu validation.
