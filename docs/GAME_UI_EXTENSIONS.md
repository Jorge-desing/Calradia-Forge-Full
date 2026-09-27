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
