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

`ForgeApi.Hooks` is a separate optional capability for explicitly registered Prefix/Postfix/Finalizer callbacks. Registration alone does not patch a method, and module startup does not discover or apply hook declarations. SDK hook contracts do not reference MonoMod. In the Bannerlord `net472` build, the Core service additionally exposes the local `RegisterTranspiler(ForgeHookDefinition, ILContext.Manipulator)` adapter backed by MonoMod.RuntimeDetour 25.3.6 `ILHook`; this overload is not part of the SDK service interface and is not available to Core `net8.0`, Desktop, or IPC callers. Transpiler metadata must not also declare Prefix, Postfix, or Finalizer callbacks. Keep the MonoMod `ILContext` delegate local to trusted in-process Core code; never transport it through SDK or IPC. Patch Blueprint hook names are descriptive declarations, not proof that a hook is executable.

Registration is inert; Apply and Revert are explicit operations. The initial Apply and Revert management require the exact game-owned main-menu screen, the game thread, and no campaign, mission, or multiplayer session. Prefix/Postfix/Finalizer callbacks execute synchronously on the target caller, subject to the host's callback gate. If a gate check fails after Prefix, Forge restores original arguments and calls the target unchanged; if it fails after the target, Postfix is skipped. Finalizer receives a pending Prefix/original/Postfix exception: leaving it unchanged preserves and rethrows it, assigning another exception replaces it, and assigning null suppresses it (a non-void target still needs a type-compatible result). If Finalizer itself throws while another failure is pending, both are retained in an `AggregateException`. Without a Finalizer, the existing Prefix/Postfix failure policy remains in effect. The gate can skip callbacks during a context transition or unload, so Finalizer is not an unconditional cleanup guarantee.

An applied detour remains installed after leaving the menu; runtime callbacks are gated, but the detour must still be explicitly reverted. An applied ILHook transforms the method body and remains active outside the menu until verified Undo/Revert; disabling callbacks does not restore transformed IL. MonoMod can rebuild the chain for an already-applied, still-owned IL activation on another caller's thread or after the host gate closes. A manipulator must therefore tolerate repeated execution, use only its supplied IL and stable configuration, and avoid live game state. This reconstruction is not a new Apply or authorization to manage hooks. Read-only preflight and registration do not execute the manipulator. Uncertain Undo retains its handle and target reservation for inspection/recovery; do not infer successful removal from a false `IsApplied` flag. These lifecycle rules are experimental and do not establish safety when other threads are executing a target during mutation. Avoid using hooks to change campaign simulation; use a `GameModel` decorator instead.

During SDK disconnect, Forge attempts to revert its owned hooks in reverse order. The disconnect guard can refuse while active or uncertain hooks remain outside the approved context, and removal can leave a conflict that needs inspection. Hook and patch handle `Dispose()` throws if a revert cannot be confirmed; use the structured `Revert()` result when the caller must inspect failure details. Forge keeps an unresolved patch capability published through disconnect so it remains inspectable and recoverable. If an incoming host cannot reconnect after a clean replacement teardown, Forge attempts to reopen the previous host; inspect snapshots and resolve any retained state before retrying. Do not infer successful removal from the unload callback alone. For the current API and confirmation flow, see [calradia-forge-modding](../calradia-forge-modding/SKILL.md#explicit-prefixpostfix-hooks-experimental) and [Patch Blueprints](../../../docs/PATCH_BLUEPRINTS.md) (Spanish: [Planos de parche](../../../docs/PATCH_BLUEPRINTS.es.md)).

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
