param(
    [Parameter(Mandatory=$true)]
    [string]$ModName,
    
    [string]$Version = "1.0.0",
    [string]$Author = "Modder"
)

$workspace = Split-Path $PSScriptRoot -Parent
$targetDir = Join-Path $workspace "modules\$ModName"
$srcDir = Join-Path $workspace "src\$ModName"

if (Test-Path $targetDir) { throw "Mod directory already exists: $targetDir" }
if (Test-Path $srcDir) { throw "Source directory already exists: $srcDir" }

Write-Host "Scaffolding new Bannerlord Mod: $ModName..." -ForegroundColor Cyan

# 1. Create Directories
New-Item -ItemType Directory -Force (Join-Path $targetDir "bin\Win64_Shipping_Client") | Out-Null
New-Item -ItemType Directory -Force (Join-Path $targetDir "GUI\Prefabs") | Out-Null
New-Item -ItemType Directory -Force (Join-Path $targetDir "ModuleData") | Out-Null

New-Item -ItemType Directory -Force $srcDir | Out-Null

# 2. Create SubModule.xml
$submoduleXml = @"
<?xml version="1.0" encoding="utf-8"?>
<Module>
  <Name value="$ModName"/>
  <Id value="$ModName"/>
  <Version value="v$Version"/>
  <SingleplayerModule value="true"/>
  <MultiplayerModule value="false"/>
  <DependedModules>
    <DependedModule Id="Native"/>
    <DependedModule Id="SandBoxCore"/>
    <DependedModule Id="Sandbox"/>
    <DependedModule Id="CustomBattle"/>
    <DependedModule Id="StoryMode" />
    <DependedModule Id="CalradiaForge" />
  </DependedModules>
  <SubModules>
    <SubModule>
      <Name value="$ModName"/>
      <DLLName value="$ModName.dll"/>
      <SubModuleClassType value="$ModName.SubModule"/>
      <Tags>
        <Tag key="DedicatedServerType" value="none" />
        <Tag key="IsNoRenderModeElement" value="false" />
      </Tags>
    </SubModule>
  </SubModules>
  <Xmls>
  </Xmls>
</Module>
"@
Set-Content -Path (Join-Path $targetDir "SubModule.xml") -Value $submoduleXml

# 3. Create .csproj
$csprojXml = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <AssemblyName>$ModName</AssemblyName>
    <RootNamespace>$ModName</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <!-- Referencing Calradia Forge Core -->
    <ProjectReference Include="..\CalradiaForge.Core\CalradiaForge.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <!-- Game References -->
    <Reference Include="`$(GameBin)\TaleWorlds*.dll" Exclude="`$(GameBin)\TaleWorlds.Native.dll">
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
"@
Set-Content -Path (Join-Path $srcDir "$ModName.csproj") -Value $csprojXml

# 4. Create Boilerplate C# SubModule
$csCode = @"
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using CalradiaForge.Core.Diagnostics;

namespace $ModName
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            ForgeLogger.PrintSuccess("$ModName loaded successfully!");
        }

        protected internal override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            
            if (gameStarterObject is CampaignGameStarter campaignStarter)
            {
                // Register your custom behaviors here or use [AutoRegisterBehavior]
            }
        }
    }
}
"@
Set-Content -Path (Join-Path $srcDir "SubModule.cs") -Value $csCode

Write-Host "Mod scaffolded successfully! Don't forget to add '$ModName.csproj' to CalradiaForge.sln" -ForegroundColor Green
