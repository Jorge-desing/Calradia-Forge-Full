# Orchestrator Project Plan: Massive Stateless Clan & Character CampaignBehavior

## Objectives
Implement a comprehensive, massive, experimental `CampaignBehaviorBase` in `CalradiaForge.Mod` exploring all clan and character development hooks (dynastic succession, companion spawning, hero progression, clan tier changes, marriages, births, deaths, education, etc.) completely statelessly without custom save data serialization.

## Phase 0: Survey & Architectural Exploration (Parallel Explorers)
1. **Explorer 1 (Codebase & Mod Architecture)**: Investigate `CalradiaForge.Mod` structure, existing SubModule, references, build files (`CalradiaForge.sln`), and existing behaviors/models.
2. **Explorer 2 / Spec Miner 1 (CampaignEvents & Hooks Analysis)**: Enumerate all relevant `CampaignEvents` and TaleWorlds clan/character hooks (dynastic succession, hero creation, companion spawning, education, marriage, death, clan leader change, hero level up, perk changes).
3. **Explorer 3 / Spec Miner 2 (Safety & Architectural Constraints)**: Analyze Bannerlord architecture rules: anti-shadowing (`GEMINI.md`), Engine Initialization Crash Constraint, stateless design pattern, and registration in `OnGameStart`.

## Phase 1: Architecture Specification & Milestone Decomposition
1. Create `PROJECT.md` with full architecture, feature inventory, code layout, interface contracts, and milestone plan.
2. Create `TEST_INFRA.md` with requirements-driven verification strategy (compile test, AST/regex check for zero `SaveableTypeDefiner`/`SyncData` persistence, and `AddBehavior` SubModule registration check).

## Phase 2: Implementation & E2E Verification Scripts Track
1. **Worker 1 (Implementation)**:
   - Create the stateless CampaignBehavior class in `CalradiaForge.Mod` (e.g. `CharacterClanBehaviors/StatelessDynastyProgressionBehavior.cs` or similar adhering to anti-shadowing).
   - Register and implement handlers for all clan, hero, succession, and progression `CampaignEvents`.
   - Implement empty `SyncData(IDataStore dataStore)` with zero saved fields/types.
   - Defer complex initializations to safe event/tick hooks avoiding engine init crash.
2. **Worker 2 (SubModule Registration)**:
   - Register the new behavior in `MBSubModuleBase.OnGameStart(Game game, IGameStarter gameStarterObject)` via `AddBehavior()`.
3. **Test Writer / Worker 3 (Verification & Test Suite)**:
   - Build script / verification for `CalradiaForge.sln` in Release mode.
   - Programmatic verification script (PowerShell/Python) checking for zero `SaveableTypeDefiner` and zero `SyncData` serialization.
   - Test script verifying `AddBehavior` registration.

## Phase 3: Gate Verification (Multi-Agent Review, Challenge & Audit)
1. **2 Reviewers (`teamwork_preview_reviewer`)**: Code quality, architectural compliance, anti-shadowing adherence, engine init safety.
2. **2 Challengers (`teamwork_preview_challenger`)**: Stress testing, empirical verification, edge cases.
3. **1 Forensic Auditor (`teamwork_preview_auditor`)**: Strict integrity verification (no cheating, no facades, genuine hooks and implementation).
4. Evaluate gate in `GATE_STATUS.md`.

## Phase 4: Finalization & Sentinel Handoff
1. Ensure all criteria pass 100%.
2. Auto-packaging check per workspace rules (`tools\package.ps1` if applicable).
3. Send final report to Sentinel.
