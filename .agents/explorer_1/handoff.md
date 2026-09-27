# Handoff Report — Codebase Structure & Stateless CampaignBehavior Exploration

## 1. Observation

### 1.1 Authoritative Requirement (`.agents/ORIGINAL_REQUEST.md`)
Lines 11-29 specify:
> "Develop a massive experimental CampaignBehavior that explores all clan and character development hooks (dynastic succession, companion spawning, progression). The system must run statelessly using vanilla game states without requiring custom save data serialization."
> - **R1**: Implement a Stateless Clan & Character CampaignBehavior in `CalradiaForge.Mod`, hooking into relevant `CampaignEvents` related to heroes, clans, and progression (e.g., birth, coming of age, death, marriage, clan leader changes).
> - **R2**: Adhere to Architecture Rules: defer complex logic (avoid Engine Initialization Crash Constraint), strictly avoid namespace/class shadowing (`Campaign`), register in `MBSubModuleBase.OnGameStart` pipeline.
> - **Acceptance Criteria**:
>   1. `CalradiaForge.sln` compiles successfully in Release mode without errors.
>   2. Programmatic script verifies no custom classes inherit from `SaveableTypeDefiner` and no data is synced in `SyncData`.
>   3. Test script confirms `SubModule` properly registers the new CampaignBehavior via `AddBehavior()`.

### 1.2 Solution & Target Framework Configuration
- **Solution file**: `c:\Users\Alex\Documents\Mod Desarrolladores\CalradiaForge.sln`
  - Defines 10 projects including `CalradiaForge.Sdk`, `CalradiaForge.Core`, `CalradiaForge.Mod`, `CalradiaForge.Desktop`, `CalradiaForge.Tests`, and `CalradiaForge.Desktop.Tests`.
- **Directory.Build.props**: `c:\Users\Alex\Documents\Mod Desarrolladores\Directory.Build.props`
  - Lines 11-12:
    ```xml
    <GamePath Condition="'$(GamePath)' == ''">C:\Program Files (x86)\Steam\steamapps\common\Mount &amp; Blade II Bannerlord</GamePath>
    <GameBin>$(GamePath)\bin\Win64_Shipping_Client</GameBin>
    ```
  - Version: `13.3.0`, Target LangVersion: `latest`, Deterministic: `true`.
- **Project file**: `c:\Users\Alex\Documents\Mod Desarrolladores\src\CalradiaForge.Mod\CalradiaForge.Mod.csproj`
  - Lines 1-6:
    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup><TargetFramework>net472</TargetFramework></PropertyGroup>
     <ItemGroup><ProjectReference Include="../CalradiaForge.Core/CalradiaForge.Core.csproj"/></ItemGroup>
     <ItemGroup><Reference Include="$(GameBin)\TaleWorlds*.dll" Exclude="$(GameBin)\TaleWorlds.Native.dll"><Private>false</Private></Reference><Reference Include="$(GameBin)\Newtonsoft.Json.dll"><Private>false</Private></Reference><Reference Include="System.Numerics"/><Reference Include="System.Runtime.Serialization"/></ItemGroup>
    </Project>
    ```

### 1.3 Existing SubModule & Behavior Registration
- **SubModule Class**: `c:\Users\Alex\Documents\Mod Desarrolladores\src\CalradiaForge.Mod\SubModule.cs`
  - Inherits from `TaleWorlds.MountAndBlade.MBSubModuleBase`.
  - Namespace: `CalradiaForge.Mod`.
  - Lines 56-64 (`OnGameStart`):
    ```csharp
    protected override void OnGameStart(TaleWorlds.Core.Game game, TaleWorlds.Core.IGameStarter gameStarterObject)
    {
        base.OnGameStart(game, gameStarterObject);
        if (gameStarterObject is TaleWorlds.CampaignSystem.CampaignGameStarter campaignStarter)
        {
            // Auto-register behaviors for all mods using CalradiaForge
            CalradiaForge.Core.CampaignExtensions.ForgeBehaviorLoader.RegisterAll(campaignStarter);
        }
    }
    ```
  - Lines 66-73 (`OnCampaignStart`):
    ```csharp
    public override void OnCampaignStart(TaleWorlds.Core.Game game,object starterObject)
    {
        runtime?.NotifyCampaignStarted();
        if (starterObject is TaleWorlds.CampaignSystem.CampaignGameStarter campaignStarter)
        {
            campaignStarter.AddBehavior(new CalradiaForge.Mod.DataExtensions.DataBehavior());
        }
    }
    ```
- **Existing Behavior**: `c:\Users\Alex\Documents\Mod Desarrolladores\src\CalradiaForge.Mod\CampaignBehaviors\DataBehavior.cs`
  - Class `DataBehavior : CampaignBehaviorBase` under namespace `CalradiaForge.Mod.DataExtensions`.
  - Directory: `src/CalradiaForge.Mod/CampaignBehaviors/`.
- **Auto-Registration Infrastructure**:
  - `c:\Users\Alex\Documents\Mod Desarrolladores\src\CalradiaForge.Core\CampaignExtensions\ForgeBehaviorLoader.cs`:
    - Lines 32-48: Scans loaded assemblies for non-abstract classes inheriting from `CampaignBehaviorBase` annotated with `[AutoRegisterBehaviorAttribute]`, creating instances via `Activator.CreateInstance(type)` and calling `starter.AddBehavior(behavior)`.
  - `c:\Users\Alex\Documents\Mod Desarrolladores\src\CalradiaForge.Core\CampaignExtensions\AutoRegisterBehaviorAttribute.cs`:
    - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)] public class AutoRegisterBehaviorAttribute : Attribute`.

### 1.4 Anti-Shadowing & Architectural Constraints
- **Anti-Shadowing Rule** (`GEMINI.md` & `CalradiaForge.Core.ModRuleAuditor`):
  - Rule: Never name a folder, sub-namespace, or class `Campaign`.
  - `ModRuleAuditor.cs` line 102 checks:
    ```csharp
    if (Regex.IsMatch(content, @"\bnamespace\s+[A-Za-z0-9_\.]*\.Campaign[\s;\{]"))
    ```
  - `AdvancedToolsTests.cs` lines 163-169 confirms: `CalradiaForge.Mod.CampaignBehaviors` is valid, while `CalradiaForge.Mod.Campaign` is forbidden.
- **Engine Initialization Crash Constraint** (`bannerlord_mission_lifecycle.md`):
  - 3D visuals, heavy entity manipulations, and scene access must not run directly during early initialization or behavior constructor. Complex campaign initialization must be deferred to `OnSessionLaunchedEvent`, `OnGameLoadedEvent`, or first tick using a deferred boolean guard.

### 1.5 Reflection of Native `CampaignEvents` Available in Engine
Using `tools/ApiProbe` against `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll`, the following verified static properties and delegate types exist on `TaleWorlds.CampaignSystem.CampaignEvents`:
1. **Dynastic Succession & Life Cycle Events**:
   - `OnChildConceivedEvent` (`IMbEvent<Hero mother>`)
   - `OnGivenBirthEvent` (`IMbEvent<Hero mother, List<Hero> aliveChildren, int stillbornCount>`)
   - `HeroGrowsOutOfInfancyEvent` (`IMbEvent<Hero hero>`)
   - `HeroReachesTeenAgeEvent` (`IMbEvent<Hero hero>`)
   - `HeroComesOfAgeEvent` (`IMbEvent<Hero hero>`)
   - `ChildEducationCompletedEvent` (`IMbEvent<Hero hero, int stage>`)
   - `RomanticStateChanged` (`IMbEvent<Hero person1, Hero person2, RomanceLevelEnum romanceLevel>`)
   - `BeforeHeroesMarried` (`IMbEvent<Hero suitor, Hero maiden, bool showNotification>`)
   - `OnMarriageOfferedToPlayerEvent` (`IMbEvent<Hero suitor, Hero maiden>`)
   - `OnMarriageOfferCanceledEvent` (`IMbEvent<Hero suitor, Hero maiden>`)
   - `OnBeforePlayerCharacterChangedEvent` (`IMbEvent<Hero oldPlayerLeader, Hero newPlayerLeader>`)
   - `OnPlayerCharacterChangedEvent` (`IMbEvent<Hero oldPlayer, Hero newPlayer, MobileParty newPlayerParty, bool isMainPartyChanged>`)
   - `OnClanLeaderChangedEvent` (`IMbEvent<Hero oldLeader, Hero newLeader>`)
   - `RulingClanChanged` (`IMbEvent<Kingdom kingdom, Clan newRulingClan>`)
   - `OnHeirSelectionRequestedEvent` (`IMbEvent<Dictionary<Hero, int> candidates>`)
   - `OnHeirSelectionOverEvent` (`IMbEvent<Hero selectedHeir>`)
   - `OnBeforeMainCharacterDiedEvent` (`IMbEvent<Hero victim, Hero killer, KillCharacterActionDetail detail, bool showNotification>`)
   - `BeforeHeroKilledEvent` (`IMbEvent<Hero victim, Hero killer, KillCharacterActionDetail detail, bool showNotification>`)
   - `HeroKilledEvent` (`IMbEvent<Hero victim, Hero killer, KillCharacterActionDetail detail, bool showNotification>`)
2. **Progression & Character Development Events**:
   - `HeroLevelledUp` (`IMbEvent<Hero hero, bool shouldNotify>`)
   - `HeroGainedSkill` (`IMbEvent<Hero hero, SkillObject skill, int change, bool shouldNotify>`)
   - `PerkOpenedEvent` (`IMbEvent<Hero hero, PerkObject perk>`)
   - `PerkResetEvent` (`IMbEvent<Hero hero, PerkObject perk>`)
   - `PlayerTraitChangedEvent` (`IMbEvent<TraitObject trait, int change>`)
   - `RenownGained` (`IMbEvent<Hero hero, int gainedRenown, bool doNotify>`)
3. **Clan & Companion Mechanics Events**:
   - `OnClanCreatedEvent` (`IMbEvent<Clan clan, bool isPlayerClan>`)
   - `ClanTierIncrease` (`IMbEvent<Clan clan, bool shouldNotify>`)
   - `OnClanChangedKingdomEvent` (`IMbEvent<Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomActionDetail detail, bool showNotification>`)
   - `OnClanDefectedEvent` (`IMbEvent<Clan clan, Kingdom oldKingdom, Kingdom newKingdom>`)
   - `OnClanDestroyedEvent` (`IMbEvent<Clan clan>`)
   - `OnClanInfluenceChangedEvent` (`IMbEvent<Clan clan, float change>`)
   - `OnHeroChangedClanEvent` (`IMbEvent<Hero hero, Clan oldClan>`)
   - `NewCompanionAdded` (`IMbEvent<Hero newCompanion>`)
   - `CompanionRemoved` (`IMbEvent<Hero companion, RemoveCompanionDetail detail>`)
   - `OnHeroJoinedPartyEvent` (`IMbEvent<Hero hero, MobileParty mobileParty>`)
   - `OnPartyLeaderChangedEvent` (`IMbEvent<MobileParty party, Hero newLeader>`)
   - `HeroCreated` (`IMbEvent<Hero hero, bool isBornNaturally>`)
   - `OnHeroActivatedEvent` (`IMbEvent<Hero hero, CharacterStates state>`)
   - `HeroOccupationChangedEvent` (`IMbEvent<Hero hero, Occupation oldOccupation>`)
   - `HeroPrisonerTaken` (`IMbEvent<PartyBase captor, Hero prisoner>`)
   - `HeroPrisonerReleased` (`IMbEvent<Hero prisoner, PartyBase party, IFaction captorFaction, EndCaptivityDetail detail, bool showNotification>`)
   - `DailyTickHeroEvent` (`IMbEvent<Hero hero>`)
   - `DailyTickClanEvent` (`IMbEvent<Clan clan>`)
   - `HourlyTickClanEvent` (`IMbEvent<Clan clan>`)

### 1.6 Build and Packaging Verification
- Command: `dotnet build CalradiaForge.sln -c Release -v:minimal`
  - Result: Exit Code 0, 0 Warnings, 0 Errors.
- Test runner: `.\tests\CalradiaForge.Tests\bin\Release\net472\CalradiaForge.Tests.exe`
  - Result: 182 passed, 0 failed.
- Desktop test runner: `dotnet tests/CalradiaForge.Desktop.Tests/bin/Release/net8.0/CalradiaForge.Desktop.Tests.dll`
  - Result: 47 passed, 0 failed.
- Packaging tool: `tools/package.ps1` (and `tools/build.ps1`):
  - Stages modules under `modules/CalradiaForge` and zips outputs to `artifacts/`. Version in manifests is `v13.3.0`.

---

## 2. Logic Chain

1. **Requirement Mapping**:
   - `ORIGINAL_REQUEST.md` demands a massive stateless `CampaignBehaviorBase` in `CalradiaForge.Mod` covering all clan and character development hooks (dynastic succession, companion spawning, progression).
   - "Stateless" specifically requires that vanilla game state is observed/reacted to without custom persistent data serialization in `SyncData` and without any `SaveableTypeDefiner` registration.
2. **Namespace & Class Naming**:
   - The anti-shadowing rule strictly forbids `Campaign` as a class, folder, or namespace suffix.
   - Using `CalradiaForge.Mod.CampaignBehaviors` for the namespace and `DynasticProgressionBehavior` for the class name complies with `ModRuleAuditor` and `GEMINI.md`.
3. **File Placement & Auto-Inclusion**:
   - `src/CalradiaForge.Mod/CalradiaForge.Mod.csproj` uses standard SDK globbing (`<Project Sdk="Microsoft.NET.Sdk">`). Placing `DynasticProgressionBehavior.cs` in `src/CalradiaForge.Mod/CampaignBehaviors/` automatically includes it in compilation.
4. **Registration Pipeline Compliance**:
   - Acceptance criteria require: "A test script confirms the SubModule properly registers the new CampaignBehavior via `AddBehavior()`."
   - In `src/CalradiaForge.Mod/SubModule.cs`, `OnGameStart` is the standard Bannerlord hook for registering campaign behaviors.
   - Registering `campaignStarter.AddBehavior(new CalradiaForge.Mod.CampaignBehaviors.DynasticProgressionBehavior());` inside `OnGameStart` satisfies both runtime campaign loading (new campaign and saved games) and static/reflection test scripts checking `SubModule` for `AddBehavior`.
   - Adding `[CalradiaForge.Core.CampaignExtensions.AutoRegisterBehavior]` to the behavior class provides complementary discovery via `ForgeBehaviorLoader.RegisterAll(campaignStarter)`.
5. **Stateless Implementation**:
   - In `DynasticProgressionBehavior.cs`, `SyncData(IDataStore dataStore)` must remain completely empty (`// Stateless - relies on vanilla game states`), ensuring 100% save-game safety and zero uninstallation corruption.
   - No `SaveableTypeDefiner` class should be created.
6. **Engine Crash Avoidance**:
   - Avoid performing any 3D asset initialization or immediate scene operations during `RegisterEvents()` or the constructor. All event handlers should defensively check for null parameters and execute pure campaign/game logic.

---

## 3. Caveats

1. **Save-game Data Independence**: Because this behavior is completely stateless, any transient in-memory calculations (if cached) would reset between game sessions. The behavior must calculate all logic dynamically from native objects (`Hero.AllAliveHeroes`, `Clan.All`, `Hero.HeroDeveloper`, etc.) or react purely to event notifications.
2. **Single-threaded Execution**: TaleWorlds `CampaignEvents` are fired sequentially on the main game thread; heavy blocking operations or LINQ queries across all heroes inside tight tick loops (e.g. `DailyTickHero`) should employ anti-lag time-slicing or null-checks to preserve framerate.
3. **No other caveats**: The game binaries and assemblies exist at `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client`, and all event signatures have been verified directly via reflection.

---

## 4. Conclusion

- **Target File**: `c:\Users\Alex\Documents\Mod Desarrolladores\src\CalradiaForge.Mod\CampaignBehaviors\DynasticProgressionBehavior.cs`
- **Class**: `public class DynasticProgressionBehavior : CampaignBehaviorBase`
- **Namespace**: `CalradiaForge.Mod.CampaignBehaviors`
- **Attributes**: `[CalradiaForge.Core.CampaignExtensions.AutoRegisterBehavior]`
- **SubModule Registration**: In `c:\Users\Alex\Documents\Mod Desarrolladores\src\CalradiaForge.Mod\SubModule.cs` within `OnGameStart`:
  ```csharp
  if (gameStarterObject is TaleWorlds.CampaignSystem.CampaignGameStarter campaignStarter)
  {
      CalradiaForge.Core.CampaignExtensions.ForgeBehaviorLoader.RegisterAll(campaignStarter);
      campaignStarter.AddBehavior(new CalradiaForge.Mod.CampaignBehaviors.DynasticProgressionBehavior());
  }
  ```
- **Stateless Contract**:
  ```csharp
  public override void SyncData(IDataStore dataStore)
  {
      // Stateless: operates entirely on live vanilla game state without custom save data serialization.
  }
  ```
- **Coverage**: Hooks into 30+ verified `CampaignEvents` spanning Dynastic Succession (birth, infancy, teenage, coming of age, romance, marriage, heir selection, death), Character Development (level-up, skill gain, perks, traits, renown), and Clan/Companions (creation, tiers, kingdom changes, companions added/removed, captivity).

---

## 5. Verification Method

To independently verify the implementation once coded:
1. **Compilation Check**:
   ```powershell
   dotnet build CalradiaForge.sln -c Release -v:minimal
   ```
   *Expected outcome*: Exit code 0, 0 errors, 0 warnings.
2. **Stateless & Architecture Verification Script**:
   Create and run `tools/verify_stateless_behavior.ps1` to assert:
   - No class in `CalradiaForge.Mod` inherits from `SaveableTypeDefiner`.
   - `DynasticProgressionBehavior.cs` contains `public override void SyncData` with zero `dataStore.SyncData` calls.
   - `SubModule.cs` contains `campaignStarter.AddBehavior(new CalradiaForge.Mod.CampaignBehaviors.DynasticProgressionBehavior());`.
   - No namespace contains `.Campaign` (GEMINI rule).
3. **Full Test Suite Execution**:
   ```powershell
   .\tests\CalradiaForge.Tests\bin\Release\net472\CalradiaForge.Tests.exe
   dotnet tests/CalradiaForge.Desktop.Tests/bin/Release/net8.0/CalradiaForge.Desktop.Tests.dll
   ```
   *Expected outcome*: All test suites pass (182 tests in Core/Mod and 47 tests in Desktop).
4. **Packaging Check**:
   ```powershell
   powershell.exe -File tools\package.ps1 -Version 13.3.0
   ```
   *Expected outcome*: Mod and desktop packages generated successfully in `artifacts/`.
