# Gauntlet UI extensions

Calradia Forge exposes an opt-in SDK catalog for standalone Gauntlet pages. It never inserts controls into Forge's shared prefab or patches another module's interface. Each registered page points to an XML prefab under its owning module's `GUI/Prefabs` folder, and Forge opens that movie in its own Gauntlet overlay.

## Declare and register a page

Reference the Forge SDK and Bannerlord's `TaleWorlds.Library` assembly. Put the ViewModel assembly in the owning module's `bin/Win64_Shipping_Client` folder and the prefab in `GUI/Prefabs`.

```csharp
using CalradiaForge.Sdk;
using TaleWorlds.Library;

[ForgeUiPage("my_mod.tools", "ForgeToolsPage", "MyMod.Tools.Title", Context = Context.Any)]
public sealed class ForgeToolsPageViewModel : ViewModel
{
    [DataSourceProperty] public string Title => "My tools";

    [ForgeUiCommand("close", nameof(ExecuteClose), Context = Context.Any, ChangesState = false)]
    public void ExecuteClose() => ForgeUI.ClosePage();
}
```

The owning module registers only its own assembly after Forge is available:

```csharp
ForgeApi.AutoRegister(typeof(ForgeToolsPageViewModel).Assembly, "MyMod");
```

At runtime the catalog checks unique page/command IDs, owner folder, prefab containment and presence, a public parameterless ViewModel constructor, command method signatures, valid contexts, and matching `Command.Click` names in the prefab. Errors are logged per page and do not stop Forge from opening. `ForgeUI.OpenPage("my_mod.tools")` requests the page while Forge's panel is open; Escape or `ForgeUI.ClosePage()` closes the overlay. Calls from another thread are queued for the game thread.

The host checks page and command contexts before opening. A declared state-changing command requires test mode, and campaign context also requires confirmation of a campaign copy. Gauntlet command methods still execute extension code directly on the game thread. Attributes are metadata and open-time guards; they do not sandbox an extension, revoke its permissions later, or make an untrusted mod safe. Only install extensions you trust.

## Example prefab

```xml
<Prefab>
  <Window>
    <Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent">
      <Children>
        <TextWidget Text="@Title" />
        <ButtonWidget Command.Click="ExecuteClose">
          <Children><TextWidget Text="Close" /></Children>
        </ButtonWidget>
      </Children>
    </Widget>
  </Window>
</Prefab>
```

See the packaged `CalradiaForgeExamples/GUI/Prefabs/ForgeExamplesPage.xml` and `ForgeExamplesPageViewModel` for a complete, minimal sample. Owners unregister their pages on module unload with `ForgeApi.UnregisterUiPages("MyMod")`.

## Contextual help and icons

The in-game Help action shows short offline summaries derived from SDK XML documentation and localized in Bannerlord's active language. DocFX is used at build time only. Game-icons.net SVG source and attributed transparent sprite-part PNGs are included with Modules; Bannerlord's SpriteSheetGenerator and Resource Browser must pack those PNGs into the game's sprite resources before a Gauntlet prefab references them.

## First-party War Table artwork and layout

The built-in Calradia Forge War Table overlay is separate from the third-party page catalog described above. Its first-party Gauntlet composition is generated from `tools/generate_assets.py`, bound by `src/CalradiaForge.Mod/PanelViewModel.cs`, and rendered by `modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml`. Keep artwork and sprite registration in the Gauntlet pipeline; WPF resources are a separate set and must not be reused as Gauntlet sprites.

ImageGen master artwork is kept in `assets/gauntlet-imagegen/` with its original dimensions and SHA-256 provenance. The Rev082 masters are:

| Master | Dimensions and mode | SHA-256 |
| --- | --- | --- |
| `forge_war_table_cloth_v3_master.png` | 2172×724 RGB | `32206FCD23A74379412DAC373CB26984553A6404CB9A732495D10700C2ADF552` |
| `forge_heraldic_rail_v4_master.png` | 887×1774 RGB | `9EA66B7F9F91828539D637EC2EE939E61BD7A8B66238CF7426C8420EE7F80FCF` |
| `forge_heraldic_header_v3_master.png` | 2048×768 RGBA, alpha 0–254 | `FA854C634D051EBD9D98909DD9F332963FD09FA78A628B69FFE6DFF78CAC0083` |

Run `tools/Prepare-CalradiaForge-ImageGenTextures.bat --prepare --no-pause` to derive the fixed-size, alpha-bounded sprites, then use `tools/generate_assets.py` and the project SpriteSheetGenerator launcher to produce prefab, `SpriteData`, and source atlas outputs. `--check` verifies the prepared source pipeline; it does not prove atlas import or game rendering. Keep ornaments passive and outside the evidence ledger, editable fields, buttons, and result text.

The eight legacy `forge_header_*_v1` source ornaments were each authored at 128×64 and displayed at 24×12, where their detail aliased. They are retired from active sprite generation and retained with the prepared and SpriteParts copies under `assets/gauntlet-imagegen/archive/2026-09-28/`; `SHA256SUMS.txt` records their archived digests. The existing semantic route icons and focus cues remain responsible for navigation state.

The normal evidence ledger is required to retain at least 160 DIPs at the 1280×720 audit viewport. In the test-results explorer, status and duration occupy separate clipped cells with at least 6 DIPs of clearance, so long values cannot paint over one another. The Gauntlet visual auditor checks these geometric contracts at 1220×880, 1280×720, 1600×900, and 1920×1080; this remains source/layout evidence, not a live-render guarantee.

The existing F10-only rising-edge fallback and `GauntletLayer` open/close telemetry in `SubModule.cs` remain the diagnostic path. The Core regression validates source ordering and lifecycle checks, not live keyboard input or the native assertion. Treat Resource Browser import and in-game F10 rendering as pending until directly observed; a passing texture `--check` or source audit is not import/runtime evidence.
