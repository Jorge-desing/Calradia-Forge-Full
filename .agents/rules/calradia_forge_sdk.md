# Calradia Forge SDK Usage

When creating extensions or campaign behaviors for Calradia Forge, you MUST leverage the native SDK instead of relying on external modding frameworks like ButterLib or MCM (Mod Configuration Menu).

## 1. Auto Registration (Recommended)
You can automatically register all your Mod's SDK implementations (Commands, Handlers, Diagnostic Providers, Tests) by calling:
`ForgeApi.AutoRegister(System.Reflection.Assembly.GetExecutingAssembly(), "MyMod")`

## 2. Mod Settings (MCM Alternative)
Do not use MCM. Instead, register your configuration class with `ForgeApi.Settings`.
- **Interface**: `IForgeSettingsRegistry.Register<T>(string moduleId, string displayName, T defaultSettings)`
- **Retrieval**: `IForgeSettingsRegistry.GetSettings<T>(string moduleId)`
- Calradia Forge automatically serializes the settings to JSON and generates UI configuration panels for your mod.

## 2. Campaign Data (ButterLib CampaignVariables Alternative)
Do not create custom `CampaignBehaviorBase` classes just to store variables. Use `ForgeData` to effortlessly attach persistent data to any game entity (Hero, Clan, Party, Settlement, etc.).
- **Usage**: `Hero.GetForgeData<MyDataClass>()`
- **Constraint**: You MUST attach data only to objects that inherit from `MBObjectBase` (which have a valid `StringId`).
- `CalradiaForge.Mod` will automatically save and load this data into the campaign save file using optimized JSON serialization.

## 3. Developer Logging
Avoid creating custom log files. Send your diagnostic messages directly to the Calradia Forge unified session log.
- **Usage**: `ForgeApi.Logger?.LogInfo("MyMod", "My message")`
- **Available Levels**: `LogInfo`, `LogWarning`, `LogError`.
- The session log is viewable in real-time inside the Game UI under the **Logs** section and persists to disk.

## 4. Explicit runtime hooks (SDK API 13)

- `ForgeApi.Hooks` remains an optional, explicitly managed in-process capability separate from read-only Patch Blueprint Preflight and `ForgeApi.Patches` method replacement. Registration must not apply hooks or run IL manipulators.
- `ForgeHookDefinition` supports Prefix/Postfix/Finalizer. Preserve original failures with `ExceptionDispatchInfo` when Finalizer leaves `Exception` unchanged; Postfix runs only on success. Replacement/suppression requires return-type validation. Aggregate a throwing Finalizer with any pending failure. Without Finalizer retain the existing Prefix/Postfix fallback policy.
- Core `net472` alone exposes `ForgeHookService.RegisterTranspiler(ForgeHookDefinition, MonoMod.Cil.ILContext.Manipulator)` backed by MonoMod.RuntimeDetour 25.3.6 `ILHook`. Keep MonoMod types and package references out of SDK, Core `net8.0`, Desktop and IPC. Transpiler metadata cannot also contain runtime callbacks.
- Initial Apply and Revert management, plus reconstruction of an activation that has not completed an explicitly authorized Apply, require the approved exact main-menu/game-thread context. After Apply is verified, MonoMod may invoke that exact owned IL activation again to rebuild its chain on another caller's thread, after the host context changes, or after gameplay callbacks stop. Permit this reconstruction only while the exact activation remains owned and Undo is not uncertain; this is not a new Apply or permission to manage hooks. Manipulators must tolerate repeats, use only the supplied IL and stable configuration, avoid live game state, and never run during read-only preflight. Reject reentrant hook management from the manipulator.
- Runtime callbacks, including Finalizer, are gated at dispatch and may be skipped during context transitions/unload. Applied IL bodies remain active outside the menu until Undo/Revert; disabling callbacks does not restore IL. Retain uncertain backend handles and target reservations until removal is verified.
- `HasFinalizer` and `HasTranspiler` describe immutable snapshots; preserve prior constructor signatures and transport only metadata plus selected IDs/tokens through the workbench.

See `docs/PATCH_BLUEPRINTS.md` and its Spanish counterpart for the detailed exception and lifetime contract.

### IL cleanup evidence limits

A thrown IL Undo can leave a transformed body installed although MonoMod reports IsApplied=false. Preserve the uncertain handle and shared target reservation; do not certify subsequent removal from that flag alone. Forge requires host restart for this unresolved case. Runtime callbacks and new applications remain closed during unload. An exact activation that completed explicit Apply may still reconstruct during an external chain rebuild while it remains owned and Undo is not uncertain; do not block that reconstruction solely because callbacks stopped or the host gate is false. An activation without prior Apply authorization may rebuild only during synchronous owned Undo under the service lock and approved caller-thread/context scope. The disposable BAT fixture covers failed Undo and serial chain-rebuild cases, not general live concurrency. See `docs/PATCH_BLUEPRINTS.md` for utility bounds and confirmation contracts.
