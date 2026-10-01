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

## In-game Gauntlet Page Blueprint

Open the navigation palette with `Ctrl+P` and select **Gauntlet Page Blueprint** in the Novice group. The route focuses its page-title field; selecting the route does not generate anything. Enter a title and use the normal Generate action. The complete text is split into four sections: the `GUI/Prefabs` XML, the ViewModel, English localization entries, and SubModule integration steps.

The title is reduced to letters, digits, and spaces and capped at 48 characters before it is used in generated identifiers or localization fallbacks. Empty or symbol-only input becomes `Tools`; Unicode letters and digits are retained. The blueprint only returns text and never creates or overwrites files in the user's module. Replace every `your_unique_module_page_prefix` marker with the same short, globally unique lowercase prefix. That prefix must remain identical in the `ForgeUiPage` attribute, the `ForgeUI.OpenPage` call, and the localization IDs; keep the full page ID within the SDK's 96-character limit. Replace `YOUR_EXACT_MODULE_FOLDER_ID` with the exact module folder ID that contains the assembly and `GUI/Prefabs` directory. `YourMod.UI` is a separate valid C# namespace example and must be renamed independently when needed.

Merge the English localization entries into the English strings resource already referenced by the target module's `SubModule.xml`. Integrate the registration and unregistration calls into the existing `OnSubModuleLoad` and `OnSubModuleUnloaded` overrides. Do not add duplicate overrides, availability callbacks, or `AutoRegister` calls for an assembly and owner that are already registered. **Refresh** and **Close** are demonstration commands: Refresh changes only the sample status text, and Close requests that the extension page close. When the page is registered, Forge validates the owner folder, prefab, ViewModel, and `Command.Click` bindings.

## In-game Gauntlet Page Composer

Open the navigation palette with `Ctrl+P` and select **Gauntlet Page Composer** in the Novice group. Its route ID is `novice-gauntlet-composer`. The dedicated workspace has a page title, a component catalog and ordered list, a properties editor, a sample preview, and **Save Draft**, **Generate**, and **Copy Package** actions. It is separate from **Gauntlet Page Blueprint**.

Add up to 12 components: heading, text, editable field, button, status/metric, list, toggle, progress bar, and selector. Component IDs stay attached to their blocks when the order changes. Lists and selectors accept 1–8 editable entries or options. The page title is limited to 48 characters; labels and text values are limited to 128 characters.

The preview uses a fixed Gauntlet template bound to `MBBindingList` and local sample data. Preview buttons, toggles, and selectors only change that sample model; they do not run campaign actions or load generated XML.

Draft persistence is explicit. **Save Draft** writes schema version 1 to `%LOCALAPPDATA%\CalradiaForge\gauntlet-composer.json`; the file is limited to 64 KiB. Opening the route loads the last valid draft. A missing file leaves an empty canvas. If a draft is damaged, oversized, or uses an unknown schema version, the composer remains available and reports its load status; the existing file is kept until the user explicitly saves a replacement.

**Generate** requires at least one component and creates a complete text package with four parts: a Gauntlet prefab XML, a C# ViewModel, English localization entries, and SubModule integration instructions. Before enabling **Copy Package**, the generator checks that XML bindings and commands resolve to generated ViewModel members, list templates have matching collection and row contexts, and localization IDs agree across the package. Validation errors prevent copying an invalid package. **Copy Package** copies the complete generated output, including all sections, rather than only the currently visible output page.

The composer generates text in memory. It does not create or modify files in the user's module and does not change the public SDK API.

## In-game Campaign Rule Builder

Open the navigation palette with `Ctrl+P` and select **Campaign Rule Builder** in the Novice group. Its route ID is `novice-campaign-rule-builder`. The left column contains an ordered list of up to eight rules. The right column edits the selected rule as **When / If / Then** and shows a preview based on fixed sample facts. Each rule keeps its ID when moved. Internal scroll areas keep the bottom **Save draft**, **Validate**, **Generate**, and **Copy package** actions available on smaller viewports.

Choose one of seven campaign events: `WeeklyTickEvent`, `DailyTickHeroEvent`, `DailyTickSettlementEvent`, `HeroComesOfAgeEvent`, `HeroGainedSkill`, `OnHeroJoinedPartyEvent`, or `OnClanCreatedEvent`. A rule can contain two condition groups with up to three conditions each. Conditions within a group are combined with AND; group B is an OR alternative to group A. The condition and target choices follow the data available from the selected event. The available actions grant gold, change influence, grant renown, or change the player's relationship with the event hero. Incompatible event, action, and target combinations are rejected before generation.

`DailyTickHeroEvent` and `DailyTickSettlementEvent` are per-entity daily events. Each rule is evaluated for the hero or settlement delivered by that event; actions run only when the rule's conditions match. A player-targeted reward can therefore repeat across matching hero or settlement events. `WeeklyTickEvent` is a campaign-wide weekly occurrence. The preview and generated validation report call out the two per-entity daily events.

The preview only evaluates sample data. **Validate** checks the draft without running any campaign action. **Generate** requires at least one rule and creates a complete text package: a `CampaignBehaviorBase` class for `net472`, integration instructions for the existing `OnGameStart` override, and a validation report. Rules sharing an event use one `AddNonSerializedListener` subscription. The generated behavior has an empty `SyncData`, no `SaveableTypeDefiner`, and a reentrancy guard. **Copy package** copies the complete output even when its presentation is paginated. Review and adapt the generated rules before integrating them into a separate module: once installed there, their actions can change a saved campaign on each matching event.

Saving is explicit. On the first visit with no saved file, the builder shows one in-memory starter row in the ordered list. It is not restored from disk and is not saved automatically. Clicking the event name edits that same row; **Add rule** appends another row. **Save draft** writes the current list as schema version 1 to `%LOCALAPPDATA%\CalradiaForge\campaign-rule-builder.json` atomically, with a 64 KiB size limit. A damaged, oversized, or future-version draft is reported and kept until the user explicitly saves a replacement. The builder itself does not register generated behaviors, write module source files, or mutate the current campaign.

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

The heraldic header uses a 512×100 crop of master pixels above alpha 8 with four source pixels of padding, so both compass leaves and the lower rule remain inside the sprite. The navigation rail uses a full-artwork 256×504 source for its 128×256 DIP slot; this leaves room for generator padding inside the fixed 4096×512 atlas. If either prepared dimension changes, rebuild the atlas and generated `SpriteData` with the official SpriteSheetGenerator. Do not hand-edit generated sprite rectangles or reuse the small Gauntlet crops in WPF.

The eight legacy `forge_header_*_v1` source ornaments were each authored at 128×64 and displayed at 24×12, where their detail aliased. They are retired from active sprite generation and retained with the prepared and SpriteParts copies under `assets/gauntlet-imagegen/archive/2026-09-28/`; `SHA256SUMS.txt` records their archived digests. The existing semantic route icons and focus cues remain responsible for navigation state.

The normal evidence ledger is required to retain at least 160 DIPs at the 1280×720 audit viewport. In the test-results explorer, status and duration occupy separate clipped cells with at least 6 DIPs of clearance, so long values cannot paint over one another. The Gauntlet visual auditor checks these geometric contracts at 1220×880, 1280×720, 1600×900, and 1920×1080; this remains source/layout evidence, not a live-render guarantee.

The existing F10-only rising-edge fallback and `GauntletLayer` open/close telemetry in `SubModule.cs` remain the diagnostic path. The Core regression validates source ordering and lifecycle checks, not live keyboard input or the native assertion. Treat Resource Browser import and in-game F10 rendering as pending until directly observed; a passing texture `--check` or source audit is not import/runtime evidence.
