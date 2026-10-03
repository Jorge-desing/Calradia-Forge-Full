# Test Readiness Declaration (TEST_READY.md)

**Status**: READY  
**Date**: 2026-09-20T21:18:25Z  
**Author**: Test Writer 1  
**Target Solution**: `CalradiaForge.sln`  
**Target Behavior**: `CalradiaForge.Mod.CampaignBehaviors.ClanCharacterProgressionBehavior`  

---

## 1. Acceptance Criteria Verification Matrix

| # | Acceptance Criterion | Source Requirement | Verification Tool | Result |
|---|----------------------|--------------------|-------------------|:------:|
| 1 | `CalradiaForge.sln` compiles in Release mode (`0 Errors`, `0 Warnings`) | `ORIGINAL_REQUEST.md §Acceptance` | `dotnet build CalradiaForge.sln -c Release -v:minimal` | **PASS** |
| 2 | Statelessness: 0 classes inherit from `SaveableTypeDefiner` | `ORIGINAL_REQUEST.md §R1, §Acceptance` | AST/Regex & Reflection in `tools/verify_stateless_behavior.*` | **PASS** |
| 3 | Statelessness: `SyncData(IDataStore)` is empty / no-op with 0 synced fields | `ORIGINAL_REQUEST.md §Acceptance` | AST/Regex & Reflection in `tools/verify_stateless_behavior.*` | **PASS** |
| 4 | GEMINI Anti-Shadowing: 0 folders, namespaces, or classes named `Campaign` | `GEMINI.md`, `R2` | Directory & Regex Scanner in `tools/verify_stateless_behavior.*` | **PASS** |
| 5 | SubModule Registration: `SubModule.OnGameStart` registers behavior via `AddBehavior()` | `ORIGINAL_REQUEST.md §R2, §Acceptance` | AST/Regex & Unit Test in `ClanCharacterProgressionTests.cs` | **PASS** |
| 6 | Engine Crash Guard: `RegisterEvents()` solely declarative, no startup entity queries | `ORIGINAL_REQUEST.md §R2`, `bannerlord_mission_lifecycle.md` | AST inspection in `ClanCharacterProgressionTests.cs` | **PASS** |
| 7 | Modulo-24 hash selection gates eligible periodic work; this source check does not establish lower in-game latency | Scheduling contract | AST inspection in `ClanCharacterProgressionTests.cs` | **PASS** |
| 8 | Defensive Null & Boundary Guards: Handles 15 mined edge cases | Spec Mining (§Spec Miner 1) | AST inspection in `ClanCharacterProgressionTests.cs` | **PASS** |

---

## 2. Automated Test Suite Results

### 2.1 Python Acceptance Script (`tools/verify_stateless_behavior.py`)
- **Command**: `python tools/verify_stateless_behavior.py`
- **Output**:
  - `[1/4] Verifying CalradiaForge.sln compiles in Release mode... [OK]`
  - `[2/4] Verifying statelessness: 0 SaveableTypeDefiner & 0 SyncData fields... [OK]`
  - `[3/4] Verifying GEMINI.md Anti-Shadowing constraints... [OK]`
  - `[4/4] Verifying MBSubModuleBase.OnGameStart registration... [OK]`
- **Exit Code**: `0` (Success)

### 2.2 PowerShell Acceptance Script (`tools/verify_stateless_behavior.ps1`)
- **Command**: `powershell -ExecutionPolicy Bypass -File tools/verify_stateless_behavior.ps1`
- **Output**:
  - `[1/4] Checking Release build of CalradiaForge.sln... [OK]`
  - `[2/4] Checking for zero SaveableTypeDefiner and stateless SyncData... [OK]`
  - `[3/4] Checking GEMINI.md Anti-Shadowing constraints... [OK]`
  - `[4/4] Checking SubModule.OnGameStart registration... [OK]`
- **Exit Code**: `0` (Success)

### 2.3 Core & Mod Unit Tests (`tests/CalradiaForge.Tests/ClanCharacterProgressionTests.cs`)
- **Command**: `CalradiaForgeTests.bat` (compatibility entrypoint) or `tools\Run-CalradiaForge-Core-Tests.bat`
  - **Total Tests**: `230 passed, 0 failed` (Exit Code `0`, current `.bat` suite run on 2026-09-22)
- **ClanCharacterProgression Unit Tests (12/12 Passed)**:
  1. `ClanProgression: Source file exists and adheres to namespace naming rules` -> **PASS**
  2. `ClanProgression: Inherits from CampaignBehaviorBase via reflection` -> **PASS**
  3. `ClanProgression: Decorated with AutoRegisterBehaviorAttribute` -> **PASS** *(historical baseline; superseded by the explicit-only registration contract below)*
  4. `ClanProgression: Parameterless constructor for ForgeBehaviorLoader compatibility` -> **PASS**
  5. `ClanProgression: 100% Stateless - SyncData is no-op with zero saved fields` -> **PASS**
  6. `ClanProgression: Zero SaveableTypeDefiner classes in codebase and assembly` -> **PASS**
  7. `ClanProgression: GEMINI.md Anti-Shadowing compliance (no 'Campaign' collisions)` -> **PASS**
  8. `ClanProgression: SubModule.OnGameStart registers behavior via AddBehavior()` -> **PASS**
  9. `ClanProgression: Hooks into 35+ verified TaleWorlds CampaignEvents` -> **PASS**
  10. `ClanProgression: Engine crash guard - declarative RegisterEvents without early queries` -> **PASS**
  11. `ClanProgression: modulo-24 selection gates eligible IDs; no UI-stutter performance claim` -> **PASS**
  12. `ClanProgression: Defensive null and boundary guards for game edge cases` -> **PASS**

## Evidence scope clarification — 2026-10-02
The time-slicing test checks source structure and selection behavior; it does not profile the full `Hero.AllAliveHeroes` traversal, callback allocations, or Bannerlord frame latency. Empty behavior-owned `SyncData` and absence of `SaveableTypeDefiner` establish that this behavior adds no custom save fields; they do not certify compatibility of the complete save format or mod set.

### Current registration contract correction — 2026-10-02
The numbered test output above is a historical 2026-09-22 record. Its AutoRegisterBehavior result is obsolete for the current source: `ClanCharacterProgressionBehavior` is registered explicitly by `SubModule.OnGameStart` and must not carry that attribute, because the Forge behavior loader would add a duplicate instance. The current regression asserts that the attribute is absent. The historical certification paragraph below records that earlier run only; it does not certify the current source or in-game application behavior.

### 2.4 Desktop UI Test Suite
- **Command**: `tools\Run-CalradiaForge-Desktop-Tests.bat`
  - **Total Tests**: `39 passed, 0 failed` (Exit Code `0`, current `.bat` suite run on 2026-09-22)

---

## 3. CampaignEvents Subscription Verification Summary
The implementation was verified to hook into all 45+ TaleWorlds campaign events across 6 distinct categories:
- **Hero Lifecycle (14 hooks)**: `HeroCreated`, `HeroGrowsOutOfInfancyEvent`, `HeroReachesTeenAgeEvent`, `HeroComesOfAgeEvent`, `BeforeHeroKilledEvent`, `HeroKilledEvent`, `HeroWounded`, `HeroOccupationChangedEvent`, `HeroRelationChanged`, `OnHeroChangedClanEvent`, `HeroPrisonerTaken`, `HeroPrisonerReleased`, `OnHeroActivatedEvent`, `OnHeroGetsBusyEvent`.
- **Clan & Dynastic Succession (11 hooks)**: `OnClanCreatedEvent`, `OnClanDestroyedEvent`, `ClanTierIncrease`, `OnClanLeaderChangedEvent`, `OnHeirSelectionRequestedEvent`, `OnHeirSelectionOverEvent`, `OnPlayerCharacterChangedEvent`, `OnClanChangedKingdomEvent`, `OnClanDefectedEvent`, `RulingClanChanged`, `OnClanInfluenceChangedEvent`.
- **Companions & Parties (5 hooks)**: `NewCompanionAdded`, `CompanionRemoved`, `OnHeroJoinedPartyEvent`, `OnPartyLeaderChangedEvent`, `OnGovernorChangedEvent`.
- **Marriage & Pregnancy (5 hooks)**: `OnMarriageOfferedToPlayerEvent`, `OnMarriageOfferCanceledEvent`, `BeforeHeroesMarried`, `RomanticStateChanged`, `OnGivenBirthEvent`.
- **Character Progression (6 hooks)**: `HeroGainedSkill`, `HeroLevelledUp`, `PerkOpenedEvent`, `PerkResetEvent`, `PlayerTraitChangedEvent`, `RenownGained`.
- **Simulation Ticks (6 hooks)**: `DailyTickHeroEvent`, `DailyTickClanEvent`, `HourlyTickPartyEvent`, `HourlyTickEvent`, `DailyTickEvent`, `WeeklyTickEvent`.

---

## 4. Conclusion & Certification
Historical 2026-09-22 report: the test artifacts and verification scripts listed in that snapshot had been executed and verified. The result is not a current certification; use current BAT runs and source contracts for present acceptance.
