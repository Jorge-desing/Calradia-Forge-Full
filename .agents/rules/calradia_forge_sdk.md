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
