# E2E Test Infra: Stateless Clan & Character CampaignBehavior

## Test Philosophy
- Opaque-box, requirement-driven.
- Automated validation of all acceptance criteria defined in `ORIGINAL_REQUEST.md`:
  1. `CalradiaForge.sln` compiles successfully in Release mode without errors.
  2. Programmatic script verifies that no custom classes inherit from `SaveableTypeDefiner` and no data is synced in `SyncData`.
  3. Test script confirms the SubModule properly registers the new CampaignBehavior via `AddBehavior()`.
  4. Anti-shadowing verification (`GEMINI.md`): no folder, namespace, or class named `Campaign`.

## Feature Inventory & Test Coverage
| # | Feature | Source (Requirement) | Tier 1 (Feature) | Tier 2 (Boundary & Corner) | Tier 3 (Cross-Feature) | Tier 4 (Application Scenarios) |
|---|---------|---------------------|:----------------:|:--------------------------:|:----------------------:|:------------------------------:|
| 1 | Release Compilation | `ORIGINAL_REQUEST §Acceptance` | ✓ | ✓ | ✓ | ✓ |
| 2 | Statelessness: 0 SaveableTypeDefiner | `ORIGINAL_REQUEST §Acceptance` | ✓ | ✓ | ✓ | ✓ |
| 3 | Statelessness: Empty SyncData | `ORIGINAL_REQUEST §Acceptance` | ✓ | ✓ | ✓ | ✓ |
| 4 | Anti-Shadowing Compliance | `GEMINI.md`, `R2` | ✓ | ✓ | ✓ | ✓ |
| 5 | SubModule.OnGameStart Registration | `ORIGINAL_REQUEST §R2, §Acceptance` | ✓ | ✓ | ✓ | ✓ |
| 6 | Engine Crash Guard: Init Safety | `ORIGINAL_REQUEST §R2` | ✓ | ✓ | ✓ | ✓ |
| 7 | Modulo-24 Selection for Eligible Periodic Work | Scheduling contract | ✓ | ✓ | — | Not verified |
| 8 | Defensive Null Guards | 15 Edge Cases from Spec Mining | ✓ | ✓ | ✓ | ✓ |

## Test Architecture
- **Windows entrypoints**: start tests through maintained `.bat` launchers, such as `tools\Run-CalradiaForge-Tests.bat`; do not invoke test `.exe` or `.dll` files directly. The launcher owns the appropriate test host for each target framework.
- **Automated Verification Script (PowerShell)**: `tools/verify_stateless_behavior.ps1`
  - Runs Release build of `CalradiaForge.sln`
  - Regex & AST scan of `src/CalradiaForge.Mod` for `SaveableTypeDefiner`
  - Regex scan of `SyncData` in `CampaignBehaviors` to ensure zero `SyncData()` serialization calls
  - Directory and namespace scan for forbidden `Campaign` collisions
  - Inspects `SubModule.cs` for `campaignStarter.AddBehavior(new CalradiaForge.Mod.CampaignBehaviors.ClanCharacterProgressionBehavior())`
- **Automated Verification Script (Python)**: `tools/verify_stateless_behavior.py`
  - Mirrors PowerShell verifications cross-platform with exit codes 0/1.
- **Unit Test Suite**: `tests/CalradiaForge.Tests/ClanCharacterProgressionTests.cs`
  - Compiles and runs within the standard `CalradiaForge.Tests` test runner.
  - Tests reflection on `ClanCharacterProgressionBehavior`: inheritance from `CampaignBehaviorBase`, empty `SyncData`, absence of the auto-registration attribute (to avoid a duplicate), and explicit `SubModule` registration.

## Coverage Thresholds
- Tier 1: Core compilation, basic registration, and zero-save-state verification.
- Tier 2: Boundary tests (empty strings, null entity safety, dead hero events, 0-child births).
- Tier 3: Cross-feature tests (SubModule pipeline + BehaviorLoader + Event registration).
- Tier 4: Full solution verification + packaging verification.

## Evidence scope clarification — 2026-10-02
The time-slicing checks verify the deterministic selection gate and source structure only. `OnHourlyTick` still enumerates `Hero.AllAliveHeroes`; these checks do not measure the full callback, allocation count, or in-game frame latency and do not prove anti-lag behavior. An empty behavior-owned `SyncData` and no `SaveableTypeDefiner` show that this behavior adds no custom save fields; they do not certify full save compatibility across the game and other mods.
