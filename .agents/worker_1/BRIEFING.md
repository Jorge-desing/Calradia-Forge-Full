# BRIEFING — 2026-09-20T21:18:40Z

## Mission
Implement ClanCharacterProgressionBehavior hooked to 35+ CampaignEvents with robust guards, hash time-slicing, and real logic, and register it in SubModule.cs.

## 🔒 My Identity
- Archetype: implementer
- Roles: implementer, qa, specialist
- Working directory: c:\Users\Alex\Documents\Mod Desarrolladores\.agents\worker_1
- Original parent: 0e802032-b7a4-434c-94c6-7b8e94b19697
- Milestone: ClanCharacterProgressionBehavior Implementation

## 🔒 Key Constraints
- Anti-Shadowing: NEVER use `Campaign` as class or namespace suffix or name.
- 100% Stateless: `SyncData(IDataStore dataStore)` must be empty/no-op. DO NOT create any class inheriting from `SaveableTypeDefiner`!
- Engine Crash Constraint: `RegisterEvents()` must only register listeners; defer entity queries to `OnSessionLaunchedEvent` or subsequent ticks.
- Modulo-24 Hash Time-Slicing: In periodic hero ticks or evaluations, use `(entity.StringId.GetHashCode() & 0x7FFFFFFF) % 24 == (int)CampaignTime.Now.ToHours % 24`.
- Guard against all 15 edge cases from `spec_miner_1/handoff.md`.
- Hook into 35+ verified TaleWorlds `CampaignEvents`.
- Attribute: `[CalradiaForge.Core.CampaignExtensions.AutoRegisterBehavior]`.
- Namespace: `CalradiaForge.Mod.CampaignBehaviors`.
- SubModule.cs: register `campaignStarter.AddBehavior(new CalradiaForge.Mod.CampaignBehaviors.ClanCharacterProgressionBehavior());` while preserving `ForgeBehaviorLoader.RegisterAll(campaignStarter);`.
- Exclusively owned files: `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs` and `src/CalradiaForge.Mod/SubModule.cs`.

## Current Parent
- Conversation ID: 0e802032-b7a4-434c-94c6-7b8e94b19697
- Updated: 2026-09-20T21:15:31Z

## Task Summary
- **What to build**: ClanCharacterProgressionBehavior.cs and update SubModule.cs
- **Success criteria**: 35+ CampaignEvents hooked, defensive null/boundary guards for 15 edge cases, hash time-slicing, stateless SyncData, registered in SubModule.cs, builds with 0 errors/0 warnings, tests pass.
- **Interface contracts**: PROJECT.md / spec miners handoffs
- **Code layout**: src/CalradiaForge.Mod/CampaignBehaviors/

## Change Tracker
- **Files modified**:
  - `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs`: Created stateless behavior with 47 CampaignEvents, 15 edge case guards, modulo-24 time-slicing, and dynastic succession scoring.
  - `src/CalradiaForge.Mod/SubModule.cs`: Registered ClanCharacterProgressionBehavior in OnGameStart while preserving ForgeBehaviorLoader.RegisterAll.
- **Build status**: PASS (Release build 0 errors, 0 warnings)
- **Pending issues**: None

## Quality Status
- **Build/test result**: PASS (194/194 CalradiaForge.Tests passed, 47/47 Desktop.Tests passed, verify_stateless_behavior scripts passed)
- **Lint status**: Clean (anti-shadowing validated)
- **Tests added/modified**: Covered by ClanCharacterProgressionTests.cs

## Loaded Skills
- Following Bannerlord modding guidelines, anti-shadowing conventions, and modulo-24 time-slicing architecture.

## Key Decisions Made
- Hooked 47 CampaignEvents across all 6 specified categories.
- Implemented Modulo-24 time-slicing on hourly party and hero checks to partition workloads across the 24 campaign hours and eliminate midnight freezes.
- Enforced zero-allocation daily hero ticks.
- Assessed dynastic succession fitness statelessly from vanilla hero attributes (age, level, leadership, tactics, charm, steward, kinship).

## Artifact Index
- DISPATCH.md — Assignment from orchestrator
- progress.md — Liveness & progress tracker
- handoff.md — Final deliverable report
