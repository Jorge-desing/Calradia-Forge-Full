---
name: bannerlord-save-system
description: Enforces critical constraints and safety rules for the Mount & Blade II Bannerlord Save System.
trigger: always_on
---

# Bannerlord Save System Guidelines

When writing C# code that persists data to Bannerlord save files (e.g., inside a `CampaignBehaviorBase`), you MUST adhere to the following safety rules to prevent unrecoverable save corruption.

## 1. Do Not Inherit Engine Types for Saving
- **Constraint:** Never create custom classes that inherit from built-in engine objects (like `LogEntry`) and register them to native managers (like `LogEntry.AddLogEntry`).
- **Reason:** If your mod is uninstalled, the user's save will crash when it tries to deserialize an assembly type that no longer exists. Use decoupled external data structures instead.

## 2. Using SaveableTypeDefiner
When you must persist custom structs or nested collections natively:
- Inherit from `TaleWorlds.SaveSystem.SaveableTypeDefiner`.
- Provide a large, unique integer to the base constructor (e.g., `base(2_500_000)`) to prevent ID collisions with the native engine (0-100k) or other mods.
- **Fields vs Properties:** Mark members with `[SaveableField(id)]` or `[SaveableProperty(id)]`. **CRITICAL:** Once a member is marked as a Field or Property, you cannot change it in future versions. Changing it breaks backwards compatibility.
- Ensure all nested generic collections are registered via `ConstructContainerDefinition()`.

## 3. JSON Serialization Constraints
If using JSON serialization within `SyncData(IDataStore dataStore)`:
- **Never serialize Engine Entities directly:** Objects with `MBGUID` (like `Hero`, `Settlement`, `MobileParty`) will lose their GUID mapping. Only serialize their `StringId` and resolve them upon load using `MBObjectManager.FindSystemBaseObject()`.
- **String Length Limits:** The engine's binary serializer has a hard limit for string sizes (~31 KB / `short.MaxValue - 1024`). Large JSON payloads will truncate and corrupt the save file.
- **Solution:** Chunk large JSON strings into `string[]` arrays before passing them to `dataStore.SyncData()`.
