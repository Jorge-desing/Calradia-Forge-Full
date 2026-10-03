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

`ForgeApi.Hooks` is the optional API 13 capability for registered Prefix/Postfix/Finalizer hooks. Registration is inert: startup does not scan assemblies or apply declarations. Initial Apply/Revert management is allowed only on the exact game-owned main-menu screen, on the game thread, without a campaign, mission, or multiplayer session. The SDK contains no MonoMod types; Core `net472` alone exposes `ForgeHookService.RegisterTranspiler(..., ILContext.Manipulator)` using MonoMod RuntimeDetour 25.3.6 `ILHook`. Do not add Harmony.

Runtime callbacks execute synchronously on the target caller and are gated at dispatch. Postfix runs only after Prefix and the original target succeed. Finalizer runs after the runtime phases, preserves an unchanged pending exception with `ExceptionDispatchInfo`, and may replace or suppress it only with a valid result; if Finalizer throws while another exception is pending, both failures are retained. Context transitions or unload can skip callbacks. An installed detour remains installed outside the menu until explicitly reverted, so do not treat the callback gate as a thread-quiescence guarantee. Avoid hooks for campaign simulation; use a `GameModel` decorator.

Transpiler registration is also inert. Manipulators run during Apply and when MonoMod rebuilds the IL chain, never during read-only preflight. They must tolerate repeated execution, use only the supplied IL and stable configuration, and avoid live game state. After a specific activation has completed a verified explicit Apply, MonoMod may rerun its manipulator for a chain rebuild on another caller thread or outside the menu while the activation is still owned and Undo is not uncertain; this is not permission for a new Apply/Revert. Reentrant hook management from a manipulator is rejected. Shared target reservations prevent collision with Forge raw detours. If IL Undo is uncertain, retain its handle and reservation; an unresolved transformed body can require a host restart. Neither fixtures nor these gates establish general safety against a target executing concurrently with code mutation.

The repository has an isolated x64 `tests/CalradiaForge.DetourFixture`, a guarded in-game provider in `src/CalradiaForge.Mod/HookMenuFixture.cs`, and a standalone SDK consumer fixture in `tests/CalradiaForge.HookGameFixture`. `cf.hook_fixture register` registers only Forge-owned callback and IL targets and does not apply them; use it only from the approved main-menu context. The standalone fixture BAT builds and stages a module but never installs or launches it. Do not describe either build fixture as proof of Bannerlord integration. The live apply/verify/revert smoke remains pending until it is explicitly run and observed in the game. See [calradia-forge-modding](../calradia-forge-modding/SKILL.md#explicit-runtime-hooks-and-il-transpilers-experimental) and [SDK hook rules](../../rules/calradia_forge_sdk.md).

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
