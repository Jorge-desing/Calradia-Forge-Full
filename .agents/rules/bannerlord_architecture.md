---
name: bannerlord-architecture
description: Enforces correct module structure, SubModule.xml schemas, XML merging behavior, and C# initialization for Bannerlord mods.
trigger: always_on
---

# Bannerlord Modding Architecture

## 1. Directory Structure
All mods are self-contained "Modules".
- **Module Path**: `Modules/<YourModName>/`
- **Constraint**: The folder name must **strictly match** the `<Id>` field inside `SubModule.xml`.
- **Standard Layout**:
  - `SubModule.xml` (Root manifest)
  - `bin/Win64_Shipping_Client/` (Compiled DLLs)
  - `ModuleData/` (XML data files)
  - `GUI/Prefabs/` (Gauntlet UI layouts)

## 2. SubModule.xml Schema
This is the required entry point. Key formatting rules:
- `<Id>` value must match the folder name.
- `<SubModule>` entries map to C# assemblies:
  - `<DLLName>` must exactly match the output DLL filename in the `bin` folder.
  - `<SubModuleClassType>` must be the fully qualified namespace and class inheriting from `MBSubModuleBase`.
- `<Xmls>` node registers game data:
  - `id` corresponds to engine categories (e.g., `Items`, `SPCultures`, `NPCCharacters`).
  - `path` corresponds to the filename in `ModuleData/`, **omitting the `.xml` extension**.

## 3. XML Merging & Overwrites
- **Merging**: If an XML file registers under an existing `id` (e.g., `Items`), its content is merged with existing items, not replaced.
- **Overwriting**: If a specific entity (e.g. `<Item id="empire_sword_1"...>`) shares an ID with a vanilla entity, the one loaded last overwrites the previous definition. 

## 4. C# Implementation & Lifecycle
- **Framework**: .NET Framework 4.7.2 (or modern equivalents based on game version).
- **Base Class**: The entry point must inherit from `TaleWorlds.MountAndBlade.MBSubModuleBase`.
- **Key Methods**: 
  - `OnSubModuleLoad()`: Early UI and basic initializations.
  - `OnGameStart(Game game, IGameStarter gameStarterObject)`: Register Campaign Behaviors or Models here.
- **References**: Always require `TaleWorlds.MountAndBlade.dll`, `TaleWorlds.Core.dll`, `TaleWorlds.Library.dll`, `TaleWorlds.Localization.dll`, `TaleWorlds.Engine.dll`. (Campaign mods also require `TaleWorlds.CampaignSystem.dll`).
- **Engineering Guidance**: Start C# work with `calradia-forge-dotnet`, which contains the project TFM and KISS rules. Pair game code with `bannerlord-dotnet-artisan` and the relevant domain skill. Installed general .NET guidance is optional; the protected local `using-dotnet` snapshot is not a workflow dependency. Keep runtime APIs within .NET Framework 4.7.2 and TaleWorlds calls on the game thread.

## 5. Visual Studio Debugging
- Set Working Directory to: `bin/Win64_Shipping_Client/`
- Set Command Line Arguments to force module loading:
  `/singleplayer _MODULES_*Native*SandBoxCore*CustomBattle*SandBox*StoryMode*YourModId*_MODULES_`

## 6. Campaign Behaviors (CampaignBehaviorBase)
- **Concept**: The core abstract class (TaleWorlds.CampaignSystem.CampaignBehaviorBase) for reacting to simulation events and saving state.
- **Registering Events**: Override RegisterEvents() and use CampaignEvents.[EventName].AddNonSerializedListener(this, ...) to listen to ticks, destructions, owner changes, etc.
- **SyncData**: Override SyncData(IDataStore dataStore) to save/load primitive fields or data dictionaries. Ensure the string ID is unique per behavior instance.
- **Initialization**: Must be registered in MBSubModuleBase.OnGameStart by casting IGameStarter to CampaignGameStarter and calling AddBehavior(new MyBehavior()).

## 7. Manejo Seguro de Comandos de Consola y Argumentos
- **Guardia Obligatoria contra Argumentos Nulos:**
  - Los métodos registrados con `[CommandLineFunctionality.CommandLineArgumentAttribute]` pueden recibir `args == null` si el usuario no especifica argumentos en la consola del juego.
  - Toda validación de parámetros debe verificar explícitamente: `if (args == null || args.Count < N) return "Usage: ...";` antes de acceder a elementos por índice.
  - Al procesar parámetros opcionales, verificar `args != null && args.Count > index` antes de evaluar o convertir tipos con `int.TryParse` o `bool.TryParse`.

