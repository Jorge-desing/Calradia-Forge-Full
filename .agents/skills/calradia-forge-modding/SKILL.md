---
name: calradia-forge-modding
description: "Best practices, guidelines, and SDK integrations for Mount & Blade II: Bannerlord mods using the Calradia Forge Framework."
---

# Calradia Forge Modding Guidelines

## Core Principles
Calradia Forge replaces legacy dependencies (MCM, ButterLib) with a modern, high-performance API. All mods must use `CalradiaForge.Sdk`.

## 1. Initialization & Auto-Registration
Eliminate boilerplate by using the auto-registration engine.
- **DO NOT** manually register events, commands, or behaviors.
- **DO** call `ForgeApi.AutoRegister(Assembly.GetExecutingAssembly(), "MyMod")` in `OnSubModuleLoad`.
- *Supports:* `IForgeEventHandler`, `ICommand`, `ITestCase`, `IDiagnosticProvider`.

## 2. Configuration & Settings
Calradia Forge natively handles UI settings.
- **DO NOT** use Mod Configuration Menu (MCM).
- **Register:** `ForgeApi.Settings.Register<T>("ModId", "Mod Name", instance)`
- **Consume:** `ForgeApi.Settings.GetSettings<T>("ModId")`

## 3. Campaign Data Storage
Forget writing complex `SyncData` behaviors for simple variables.
- **DO NOT** write custom `CampaignBehaviorBase` just for serialization.
- **DO** use `entity.GetForgeData<T>()` and `entity.SetForgeData<T>()`.
- *Constraint:* The target entity MUST inherit from `MBObjectBase` (e.g., `Hero`, `Settlement`, `Clan`, `MobileParty`). The framework uses the `StringId` to serialize data transparently.

## 4. Unified Logging
- **Use** `ForgeApi.Logger.LogInfo("MyMod", "Message")`, `LogWarning()`, or `LogError()`.
- Logs automatically route to the game's UI and the master `session.log` safely.

## ⚠️ Critical Rules
> Universal safety rules (no `Campaign` namespace and no Harmony dependency) are in `bannerlord-shared-patterns` and `GEMINI.md`.

1. **Data Attachment:** Do not attach `ForgeData` to non-`MBObjectBase` entities (like `ItemRosterElement`); data will silently fail to save.
2. **`Localization` namespace collision:** Never name a namespace or class `Localization` — it shadows `TaleWorlds.Localization`.

## Explicit Prefix/Postfix hooks (experimental)

Use the regular `GameModel` decorator pattern for campaign simulation changes. Calradia Forge also exposes a separate experimental, optional Prefix/Postfix hook capability through `ForgeApi.Hooks`; it is not Harmony and is not a substitute for a `GameModel` decorator.

- Hook declarations are inert. Module startup does not scan assemblies or apply them automatically; application must be an explicit, reviewed operation.
- The current management gate permits Apply and Revert only on the exact game-owned Bannerlord main-menu screen, on the game thread, with no campaign, mission, or multiplayer session active. Dispatch also checks that gate for each invocation: while a detour remains installed outside the approved context, Forge calls the original target and skips its callbacks. Callbacks may resume if the approved context returns. This is a context policy, not a thread-quiescence guarantee.
- Prefix/Postfix callbacks execute synchronously on the target caller's thread when the host gate allows them. Treat them as trusted in-process code, keep their work bounded, and do not use them to alter campaign simulation; use `GameModel` decorators instead.
- The current game-host backend uses MonoMod RuntimeDetour for the `net472` target. It supports Prefix/Postfix only; do not add Harmony or assume Finalizer/Transpiler support.
- Unload/disconnect attempts to remove Forge-owned hooks in reverse order and can be refused or report unresolved state when safe removal cannot be established. Check hook snapshots after failures; do not claim a hook was removed merely because unload began.

See [bannerlord-shared-patterns](../bannerlord-shared-patterns/SKILL.md) for the simulation extension pattern and its distinction from experimental hooks.

## Asset and UI workflows

- For Gauntlet sprite sheets, decorative texture generation, atlas registration, Resource Browser import, and TPAC evidence, follow [bannerlord-resource-browser](../bannerlord-resource-browser/SKILL.md) and [bannerlord-gauntlet-ui](../bannerlord-gauntlet-ui/SKILL.md).
- For bounded FBX or texture batch planning in the separate experimental .NET 8 helper, follow [bannerlord-fbx-importer](../bannerlord-fbx-importer/SKILL.md). It is not part of the game module and its current submission path remains locked.
- For the separate WPF desktop workbench architecture and desktop-only workflows, follow [calradia-forge-desktop](../calradia-forge-desktop/SKILL.md); keep its implementation guidance there rather than duplicating it in this general modding skill.
- Keep asset folder roles distinct: TaleWorlds describes **Assets** as editable TPAC metadata, **AssetSources** as imported source files, and **AssetPackages** as read-only client packages. Do not delete or relocate these folders based on the unverified claim that their coexistence causes a fatal startup precedence conflict.

## .NET Architecture with using-dotnet & bannerlord-dotnet-artisan

- All C# and .NET tasks MUST start with [calradia-forge-dotnet](../calradia-forge-dotnet/SKILL.md), which contains the project's target framework boundaries (`net472` for module/Core/SDK vs `net8.0-windows` for Desktop), KISS principles, and routing rules. The local [using-dotnet](../using-dotnet/SKILL.md) file is a preserved backup snapshot, not a workflow dependency.
- Bridge into [bannerlord-dotnet-artisan](../bannerlord-dotnet-artisan/SKILL.md) to ground general .NET advice in engine contracts: single-threaded simulation, save system safety, anti-shadowing naming, and zero-allocation tick loops.
- For the separate .NET 8 WPF workbench, use [calradia-forge-desktop](../calradia-forge-desktop/SKILL.md) and `calradia-forge-ui-automation`. Keep Gauntlet, mission HUD, WPF, and backend APIs on their respective surfaces.
