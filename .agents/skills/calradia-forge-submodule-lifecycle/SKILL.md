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
| Module load | `OnSubModuleLoad` | Subscribe crash reporting and create `Runtime`; no global patch scan or hook auto-application. `ForgeBootstrapper.InitializeGlobalPatches()` remains an obsolete no-op compatibility method and is not called here. |
| Main menu ready | `OnBeforeInitialModuleScreenSetAsRoot` | `runtime.NotifyInitialScreenReady()` |
| Game type start | `OnGameStart(Game, IGameStarter)` | If `CampaignGameStarter`: `ForgeBehaviorLoader.RegisterAll(campaignStarter)` then **explicitly** `AddBehavior(new ClanCharacterProgressionBehavior())` |
| Campaign start | `OnCampaignStart(Game, object)` | `runtime.NotifyCampaignStarted()`; `AddBehavior(new DataExtensions.DataBehavior())` |
| Every frame | `OnApplicationTick` | Hotkey panel (default F10 from `Runtime.Config`), `RefreshGameLanguage()` → `PanelViewModel.RefreshLanguage()`, multiplayer closes panel |
| Mission | `OnMissionBehaviorInitialize` | Adds `EventObserver` `MissionBehavior` (agent create/delete → runtime) |
| Unload | `OnSubModuleUnloaded` | Close Gauntlet layer and clear session-owned SDK state; dispose `Runtime`, which disposes its IPC server and calls `ForgeApi.Disconnect()` |

## Two registration paths for behaviors

1. **Auto**: `[AutoRegisterBehavior]` on `CampaignBehaviorBase` subclasses in assemblies referencing `CalradiaForge.Core` — discovered by `CalradiaForge.Core.CampaignExtensions.ForgeBehaviorLoader.RegisterAll` (parameterless ctor required).
2. **Manual**: `ClanCharacterProgressionBehavior` is registered explicitly in `OnGameStart` and must remain undecorated. The loader scans the mod assembly too; using both paths creates two instances and duplicates every event subscription.

Do not assume one path replaces the other without reading current `SubModule.cs`.

## Explicit hook lifecycle (experimental)

`ForgeApi.Hooks` is a separate optional capability for explicitly registered Prefix/Postfix hooks. Registration alone does not patch a method, and module startup does not discover or apply hook declarations. The current hook manager permits Apply/Revert only on the exact game-owned main-menu screen, on the game thread, with no campaign, mission, or multiplayer session active.

An applied detour runs callbacks synchronously on the target caller only when the host's exact game-thread main-menu gate is true. Dispatch checks the gate before callbacks, after Prefix, and before Postfix: a denied check after Prefix restores the original arguments and calls the target unchanged; a denied check after the target skips Postfix. Out-of-context invocations skip custom callbacks and call the original target. The detour remains installed after leaving the menu, so revert it before leaving when that is the intended lifecycle. Avoid using hooks to change campaign simulation; use a `GameModel` decorator instead. The current Bannerlord `net472` backend uses MonoMod RuntimeDetour and supports Prefix/Postfix only. Harmony, Finalizer, and Transpiler support are not part of this workflow.

During SDK disconnect, Forge attempts to revert its owned hooks in reverse order. The disconnect guard can refuse while active or uncertain hooks remain outside the approved context, and removal can leave a conflict that needs inspection. Hook and patch handle `Dispose()` now throws if a revert cannot be confirmed; use the structured `Revert()` result when the caller must inspect failure details. Forge keeps an unresolved patch capability published through disconnect so it remains inspectable and recoverable. If an incoming host cannot reconnect after a clean replacement teardown, Forge attempts to reopen the previous host; inspect snapshots and resolve any retained state before retrying. Do not infer successful removal from the unload callback alone. For API details, see [calradia-forge-modding](../calradia-forge-modding/SKILL.md#explicit-prefixpostfix-hooks-experimental).

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
