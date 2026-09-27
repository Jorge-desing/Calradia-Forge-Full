# Project: Massive Stateless Clan & Character CampaignBehavior

## Architecture
- **Target Assembly**: `src/CalradiaForge.Mod/CalradiaForge.Mod.csproj` (`net472`), referencing TaleWorlds Bannerlord Campaign binaries.
- **Namespace Design**: `CalradiaForge.Mod.CampaignBehaviors` strictly adhering to the Anti-Shadowing rule (`GEMINI.md`). Never use `Campaign` as a folder, sub-namespace, or class name.
- **Lifecycle & Engine Crash Guard**: 
  - Declarative `RegisterEvents()`: solely attaches `CampaignEvents.*.AddNonSerializedListener(this, delegate)`.
  - Session Deferral: world scanning deferred to `OnSessionLaunchedEvent` or campaign ticks.
  - Modulo-24 Time-Slicing: partitions tick evaluations across campaign hours using hash modulus to prevent the "Midnight Freeze" stutter.
- **Zero Save Footprint**:
  - Zero classes inheriting from `TaleWorlds.SaveSystem.SaveableTypeDefiner`.
  - `SyncData(IDataStore dataStore)` override is deliberately empty (no-op).
  - All character and clan progression dynamics derived statelessly on-the-fly from live TaleWorlds engine state (`Hero`, `Clan`, `HeroDeveloper`, `MobileParty`).
- **Registration**: Registered in `MBSubModuleBase.OnGameStart` via `campaignStarter.AddBehavior(new ClanCharacterProgressionBehavior())` and decorated with `[AutoRegisterBehavior]`.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Anti-Shadowing Compliance | Enforce namespace `CalradiaForge.Mod.CampaignBehaviors` and avoid `Campaign` identifiers to prevent CS0118/CS0234 compiler collisions | M2, M3 | `GEMINI.md`, Spec Miner 2 |
| 2 | Declarative Event Subscriptions | Hook into 35+ verified TaleWorlds `CampaignEvents` without executing logic during engine startup | M2 | Spec Miner 1, Explorer 1 |
| 3 | Hero Lifecycle Hooks | Handle `HeroCreated`, `HeroGrowsOutOfInfancyEvent`, `HeroReachesTeenAgeEvent`, `HeroComesOfAgeEvent`, `BeforeHeroKilledEvent`, `HeroKilledEvent`, `HeroWounded`, `HeroOccupationChangedEvent`, `HeroRelationChanged`, `OnHeroChangedClanEvent`, `HeroPrisonerTaken`, `HeroPrisonerReleased`, `OnHeroActivatedEvent`, `OnHeroGetsBusyEvent` | M2 | Spec Miner 1 |
| 4 | Clan & Dynastic Succession Hooks | Handle `OnClanCreatedEvent`, `OnClanDestroyedEvent`, `ClanTierIncrease`, `OnClanLeaderChangedEvent`, `OnHeirSelectionRequestedEvent`, `OnHeirSelectionOverEvent`, `OnPlayerCharacterChangedEvent`, `OnClanChangedKingdomEvent`, `OnClanDefectedEvent`, `RulingClanChanged`, `OnClanInfluenceChangedEvent` | M2 | Spec Miner 1 |
| 5 | Companion & Party Hooks | Handle `NewCompanionAdded`, `CompanionRemoved`, `OnHeroJoinedPartyEvent`, `OnPartyLeaderChangedEvent`, `OnGovernorChangedEvent` | M2 | Spec Miner 1 |
| 6 | Marriage & Pregnancy Hooks | Handle `OnMarriageOfferedToPlayerEvent`, `OnMarriageOfferCanceledEvent`, `BeforeHeroesMarried`, `RomanticStateChanged`, `OnGivenBirthEvent` | M2 | Spec Miner 1 |
| 7 | Character Progression Hooks | Handle `HeroGainedSkill`, `HeroLevelledUp`, `PerkOpenedEvent`, `PerkResetEvent`, `PlayerTraitChangedEvent`, `RenownGained` | M2 | Spec Miner 1 |
| 8 | Periodic Simulation Ticks & Anti-Lag | Handle `DailyTickHeroEvent`, `DailyTickClanEvent`, `HourlyTickPartyEvent`, `HourlyTickEvent`, `DailyTickEvent`, `WeeklyTickEvent` with modulo-24 hash time-slicing | M2 | Spec Miner 1 |
| 9 | Defensive Null & Boundary Guards | Guard against null killers, null old/new leaders, empty alive children lists, null clans, uninitialized hero developers, and dead heroes | M2 | Spec Miner 1, Spec Miner 2 |
| 10 | 100% Stateless Save Safety | Ensure zero `SaveableTypeDefiner` classes and an empty `SyncData(IDataStore dataStore)` method | M2, M1 | `ORIGINAL_REQUEST.md`, Spec Miner 2 |
| 11 | SubModule OnGameStart Registration | Register behavior in `SubModule.OnGameStart` via `campaignStarter.AddBehavior(...)` for new and loaded games | M3 | `ORIGINAL_REQUEST.md`, Explorer 1 |
| 12 | Automated Verification Scripts | Programmatic PowerShell & Python test scripts validating Release build, AST/regex statelessness, anti-shadowing, and registration | M1 | `ORIGINAL_REQUEST.md`, Spec Miner 2 |
| 13 | Multi-Agent Review & Challenge | Comprehensive review (2 Reviewers), stress testing & corner-case challenge (2 Challengers), and forensic audit (1 Auditor) | M4 | System Instructions |
| 14 | Workspace Packaging | Build and package distribution zip artifacts via `tools/package.ps1` | M5 | `auto_packaging.md` |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | E2E Testing Track & Verification Scripts | Create `TEST_INFRA.md`, `tools/verify_stateless_behavior.ps1`, `tools/verify_stateless_behavior.py`, unit test in `CalradiaForge.Tests`, and `TEST_READY.md` | Survey | IN_PROGRESS |
| M2 | Stateless CampaignBehavior Implementation | Create `ClanCharacterProgressionBehavior.cs` with 35+ campaign event hooks, defensive guards, and modulo-24 time-slicing | Survey | IN_PROGRESS |
| M3 | SubModule Registration & Integration | Register behavior in `SubModule.cs` `OnGameStart` pipeline | M2 | PLANNED |
| M4 | Gate Verification & Audit | Execute verification scripts, 2 Reviewers, 2 Challengers, 1 Forensic Auditor | M1, M2, M3 | PLANNED |
| M5 | Distribution Packaging & Sentinel Handoff | Execute `tools/package.ps1`, verify artifacts, deliver final report to Sentinel | M4 | PLANNED |

## Interface Contracts
### `CalradiaForge.Mod.CampaignBehaviors.ClanCharacterProgressionBehavior`
- Inherits: `TaleWorlds.CampaignSystem.CampaignBehaviorBase`
- Attributes: `[CalradiaForge.Core.CampaignExtensions.AutoRegisterBehavior]`
- Constructor: `public ClanCharacterProgressionBehavior()` (parameterless, zero entity access)
- `RegisterEvents()`: Attaches non-serialized listeners to `CampaignEvents`
- `SyncData(IDataStore dataStore)`: Empty method (no serialization)
- Public metrics / queries (stateless):
  - `int ActiveTrackedHeroesCount { get; }`
  - `int TotalLifeCycleEventsProcessed { get; }`
  - `int TotalProgressionEventsProcessed { get; }`

### `CalradiaForge.Mod.SubModule`
- `protected override void OnGameStart(Game game, IGameStarter gameStarterObject)`
- Invokes: `campaignStarter.AddBehavior(new CalradiaForge.Mod.CampaignBehaviors.ClanCharacterProgressionBehavior());`

## Code Layout
- `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs` — The stateless clan & character campaign behavior.
- `src/CalradiaForge.Mod/SubModule.cs` — SubModule registration.
- `tools/verify_stateless_behavior.ps1` — PowerShell acceptance verification script.
- `tools/verify_stateless_behavior.py` — Python acceptance verification script.
- `tests/CalradiaForge.Tests/ClanCharacterProgressionTests.cs` — Unit tests for the behavior, anti-shadowing, and statelessness.
- `TEST_INFRA.md` — E2E test infrastructure specification.
- `TEST_READY.md` — Test readiness declaration.
