# Bannerlord Getting Started & Development Environment

This guide specifies project setup, assembly reference configurations, `MBSubModuleBase` lifecycle hooks, and local debugging arguments for Mount & Blade II: Bannerlord modding.

---

## 1. Project Configuration & Dependencies (`.csproj`)

Bannerlord mods are .NET Framework 4.7.2 or modern .NET Core/.NET 8 class libraries (depending on Bannerlord version and launcher target).

### A. Reference Contracts:
- Always reference official TaleWorlds assemblies from the game's `bin/Win64_Shipping_Client/` directory.
- **MANDATORY:** Set `<Private>False</Private>` (`Copy Local = False`) on all TaleWorlds references. Never distribute official TaleWorlds DLLs with your mod archive.

```xml
<ItemGroup>
  <Reference Include="TaleWorlds.MountAndBlade">
    <HintPath>$(BannerlordDir)\bin\Win64_Shipping_Client\TaleWorlds.MountAndBlade.dll</HintPath>
    <Private>False</Private>
  </Reference>
  <Reference Include="TaleWorlds.Core">
    <HintPath>$(BannerlordDir)\bin\Win64_Shipping_Client\TaleWorlds.Core.dll</HintPath>
    <Private>False</Private>
  </Reference>
  <Reference Include="TaleWorlds.CampaignSystem">
    <HintPath>$(BannerlordDir)\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll</HintPath>
    <Private>False</Private>
  </Reference>
  <Reference Include="TaleWorlds.Library">
    <HintPath>$(BannerlordDir)\bin\Win64_Shipping_Client\TaleWorlds.Library.dll</HintPath>
    <Private>False</Private>
  </Reference>
  <Reference Include="TaleWorlds.Localization">
    <HintPath>$(BannerlordDir)\bin\Win64_Shipping_Client\TaleWorlds.Localization.dll</HintPath>
    <Private>False</Private>
  </Reference>
  <Reference Include="TaleWorlds.Engine">
    <HintPath>$(BannerlordDir)\bin\Win64_Shipping_Client\TaleWorlds.Engine.dll</HintPath>
    <Private>False</Private>
  </Reference>
  <Reference Include="TaleWorlds.Engine.GauntletUI">
    <HintPath>$(BannerlordDir)\bin\Win64_Shipping_Client\TaleWorlds.Engine.GauntletUI.dll</HintPath>
    <Private>False</Private>
  </Reference>
</ItemGroup>
```

---

## 2. The `MBSubModuleBase` Entry Point

Every Bannerlord C# module entry class must inherit from `TaleWorlds.MountAndBlade.MBSubModuleBase`.

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.Core
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            // Early startup: initialize logging, SDK services, UI handlers
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            if (gameStarterObject is CampaignGameStarter campaignStarter)
            {
                // Register GameModels via Decorator Pattern
                // Register CampaignBehaviorBase instances
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            // Attach in-mission controllers, MissionLogic, or MissionView HUD overlays
        }

        protected override void OnSubModuleUnloaded()
        {
            // Session exit cleanup
            base.OnSubModuleUnloaded();
        }
    }
}
```

---

## 3. SubModule.xml Root Schema

The folder name inside `Modules/` **must strictly match** `<Id value="..." />`:

```xml
<Module>
  <Id value="CalradiaForge"/>
  <Name value="Calradia Forge"/>
  <Version value="v12.2.0"/>
  <SingleplayerModule value="true"/>
  <MultiplayerModule value="false"/>
  <SubModules>
    <SubModule>
      <Name value="CalradiaForge"/>
      <DLLName value="CalradiaForge.Core.dll"/>
      <SubModuleClassType value="CalradiaForge.Core.SubModule"/>
      <Tags>
        <Tag key="DedicatedServerType" value="none"/>
        <Tag key="IsNoRenderModeElement" value="false"/>
      </Tags>
    </SubModule>
  </SubModules>
  <Xmls>
    <XmlNode>
      <XmlName id="NPCCharacters" path="custom_troops"/>
    </XmlNode>
  </Xmls>
</Module>
```

---

## 4. Local Debugging & Fast Launch Arguments

To bypass the TaleWorlds launcher and boot directly into the game with all required modules loaded:

1. **Executable:** `$(BannerlordDir)\bin\Win64_Shipping_Client\Bannerlord.exe`
2. **Working Directory:** `$(BannerlordDir)\bin\Win64_Shipping_Client`
3. **Command-line Arguments:**
   ```
   /singleplayer _MODULES_*Native*SandBoxCore*CustomBattle*SandBox*StoryMode*CalradiaForge*_MODULES_
   ```

---

## 5. Architectural Checklist Before Distribution
- [ ] No assembly files (`TaleWorlds.*.dll`) included in final release ZIP.
- [ ] Folder name in `Modules/<ModId>/` exactly matches `SubModule.xml` `<Id value="..." />`.
- [ ] Assembly metadata (`Company`, `Product`, `Description`, `Copyright`) defined in project files.
- [ ] Zero Harmony dependencies — all calculations use the native `GameModel` decorator pattern.
- [ ] No folder, namespace, or class named `Campaign` (`GEMINI.md` shadowing invariant).
