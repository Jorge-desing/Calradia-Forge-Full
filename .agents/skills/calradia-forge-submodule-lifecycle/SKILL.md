---
name: calradia-forge-submodule-lifecycle
description: Calradia Forge MBSubModuleBase pipeline in SubModule.cs — load, campaign start, Gauntlet tick, unload, and campaign behavior registration. Use when wiring campaign features or debugging startup crashes.
---

# Calradia Forge — SubModule lifecycle (this repo)

## Entry type

- **Class**: `CalradiaForge.Mod.SubModule` : `MBSubModuleBase`
- **Module manifest**: `modules/CalradiaForge/SubModule.xml` → `SubModuleClassType` = `CalradiaForge.Mod.SubModule`, DLL `CalradiaForge.Mod.dll`
- **Assembly**: `src/CalradiaForge.Mod/CalradiaForge.Mod.csproj` (`net472`)
- **Test visibility**: `[assembly: InternalsVisibleTo("CalradiaForge.Tests")]` at top of `SubModule.cs`

## Callback order (what runs where)

| Phase | Method | Responsibilities |
|-------|--------|------------------|
| Module load | `OnSubModuleLoad` | Unhandled-exception → `.cfcrash` JSON under `Modules/CalradiaForge/`; `Runtime` + `UIExtender.Initialize()`; `ForgeBootstrapper.InitializeGlobalPatches()` |
| Main menu ready | `OnBeforeInitialModuleScreenSetAsRoot` | `runtime.NotifyInitialScreenReady()` |
| Game type start | `OnGameStart(Game, IGameStarter)` | If `CampaignGameStarter`: `ForgeBehaviorLoader.RegisterAll(campaignStarter)` then **explicitly** `AddBehavior(new ClanCharacterProgressionBehavior())` |
| Campaign start | `OnCampaignStart(Game, object)` | `runtime.NotifyCampaignStarted()`; `AddBehavior(new DataExtensions.DataBehavior())` |
| Every frame | `OnApplicationTick` | Hotkey panel (default F10 from `Runtime.Config`), `RefreshGameLanguage()` → `PanelViewModel.RefreshLanguage()`, multiplayer closes panel |
| Mission | `OnMissionBehaviorInitialize` | Adds `EventObserver` `MissionBehavior` (agent create/delete → runtime) |
| Unload | `OnSubModuleUnloaded` | Close Gauntlet layer; clear Sdk singletons (`ForgeCampaignEvents`, `ForgeData`, `ForgeAgentMemory`, `ForgeUI`, `ForgeDetour.UnpatchAll()`); dispose `Runtime` |

## Two registration paths for behaviors

1. **Auto**: `[AutoRegisterBehavior]` on `CampaignBehaviorBase` subclasses in assemblies referencing `CalradiaForge.Core` — discovered by `CalradiaForge.Core.CampaignExtensions.ForgeBehaviorLoader.RegisterAll` (parameterless ctor required).
2. **Manual**: `ClanCharacterProgressionBehavior` is registered explicitly in `OnGameStart` and must remain undecorated. The loader scans the mod assembly too; using both paths creates two instances and duplicates every event subscription.

Do not assume one path replaces the other without reading current `SubModule.cs`.

## Runtime companion

`src/CalradiaForge.Mod/Runtime.cs`:

- Implements `ITestServices`, connects SDK via `ExtensionStartup.Connect(TestEngine)`.
- Settings file: `Paths.Data/settings.json` (language mirror of `BannerlordConfig.Language` via `RefreshGameLanguage()`).
- Context tracking: `Campaign.Current` / `Mission.Current` identity changes reset test consent and queue `ForgeEventKind` events.

## Gauntlet ownership

`SubModule` holds `GauntletLayer layer`, `ScreenBase owner`, `PanelViewModel vm` — see skill `calradia-forge-gauntlet-panel`.

## Verification

- `ClanCharacterProgressionTests.TestSubModuleRegistration` reads workspace `SubModule.cs` for `AddBehavior` + type name.
- Release build: `CalradiaForge.sln`.

## Pitfalls

- **Never** query `Campaign.Current` or heroes in behavior **constructors** (documented on `ClanCharacterProgressionBehavior`); defer to events / ticks.
- `OnCampaignStart` runs later than `OnGameStart` — register data behaviors that need full campaign there, not in `OnGameStart`, unless you know they are safe at starter time.
