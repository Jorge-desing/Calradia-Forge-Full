# BRIEFING — 2026-09-20T21:10:37Z

## Mission
Investigate and specify architectural rules, engine lifecycle crash constraints, stateless guarantees, registration pipeline, and programmatic acceptance verification for the new Stateless Clan & Character CampaignBehavior in CalradiaForge.Mod.

## 🔒 My Identity
- Archetype: Specification Miner
- Roles: Specification Mining, Architectural Analysis, Verification Strategy
- Working directory: c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_2
- Original parent: 0e802032-b7a4-434c-94c6-7b8e94b19697
- Milestone: Specification Complete (Handoff to Parent)

## 🔒 Key Constraints
- Anti-shadowing: NEVER use `Campaign` as a folder, namespace, or class name (prevents shadowing `TaleWorlds.CampaignSystem.Campaign`).
- Strictly read-only specification mining; DO NOT write or modify source code files.
- Zero `SaveableTypeDefiner` and zero data serialization in `SyncData(IDataStore dataStore)`.
- Avoid "Engine Initialization Crash Constraint" by deferring complex logic past initialization.
- Must register in `MBSubModuleBase.OnGameStart` pipeline.
- Define exact programmatic validation methods (PowerShell / Python) to verify compilation, statelessness, and registration.

## Current Parent
- Conversation ID: 0e802032-b7a4-434c-94c6-7b8e94b19697
- Updated: 2026-09-20T21:10:37Z

## Task Summary
- **What to build**: Specification for a Stateless Clan & Character CampaignBehavior adhering to all architectural constraints.
- **Success criteria**: Exhaustive analysis of constraints (Anti-shadowing, Engine Initialization Crash, Statelessness, SubModule Registration) and concrete programmatic verification methods.
- **Interface contracts**: `TaleWorlds.CampaignSystem.CampaignBehaviorBase`, `TaleWorlds.MountAndBlade.MBSubModuleBase`.
- **Code layout**: `src/CalradiaForge.Mod/CampaignBehaviors/`

## Key Decisions Made
- Analyzed `CalradiaForge.sln` release compilation via `dotnet build CalradiaForge.sln -c Release` (verified 0 errors, 0 warnings).
- Confirmed anti-shadowing compiler resolution failure modes (`CS0118`, `CS0234`) and established approved naming convention (`CalradiaForge.Mod.CampaignBehaviors`).
- Specified engine initialization crash deferral mechanisms: declarative-only `RegisterEvents()`, deferral of world state queries to `OnSessionLaunchedEvent`, and time-slicing via hash modulus for daily/hourly ticks.
- Specified the 100% stateless pattern: zero `SaveableTypeDefiner` classes, no-op `SyncData`, and deriving progression from native vanilla entities.
- Specified `MBSubModuleBase.OnGameStart` pipeline registration using type guard `if (gameStarterObject is CampaignGameStarter campaignStarter)`.
- Designed and verified complete Python and PowerShell acceptance test scripts.

## Artifact Index
- `.agents/spec_miner_2/DISPATCH.md` — Original dispatch instructions.
- `.agents/spec_miner_2/BRIEFING.md` — Persistent agent memory and context.
- `.agents/spec_miner_2/progress.md` — Heartbeat and execution step tracker.
- `.agents/spec_miner_2/handoff.md` — Comprehensive architectural findings and verification specifications.

## Loaded Skills
- **bannerlord-campaign-behavior**: Lifecycle, event subscription, anti-lag time slicing, safe rehydration.
- **bannerlord-shared-patterns**: Decorator pattern, SaveableTypeDefiner safety, CampaignBehaviorBase skeleton.
- **bannerlord-clan-succession**: Clan progression, dynastic succession, companion party roles.
- **bannerlord-character-development**: HeroDeveloper, CharacterDevelopmentModel, leveling and perks.
