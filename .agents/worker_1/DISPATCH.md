## 2026-09-20T21:14:13Z
You are Worker 1 on the Bannerlord Mod development team.

Your working directory is: c:\Users\Alex\Documents\Mod Desarrolladores\.agents\worker_1
Your parent is: 0e802032-b7a4-434c-94c6-7b8e94b19697

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

MANDATORY INSTRUCTIONS:
1. Read the following reference files first:
   - Authoritative Request: `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\ORIGINAL_REQUEST.md`
   - Project Spec: `c:\Users\Alex\Documents\Mod Desarrolladores\PROJECT.md`
   - Spec Miner 1 Handoff (Catalog of 45+ CampaignEvents and edge cases): `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_1\handoff.md`
   - Spec Miner 2 Handoff (Architectural constraints & lifecycle): `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_2\handoff.md`
   - Explorer 1 Handoff (Codebase & build layout): `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\explorer_1\handoff.md`

2. Exclusively Owned Files:
   - `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs`
   - `src/CalradiaForge.Mod/SubModule.cs`

3. Implementation Requirements:
   - Create `src/CalradiaForge.Mod/CampaignBehaviors/ClanCharacterProgressionBehavior.cs`:
     - Inherit from `TaleWorlds.CampaignSystem.CampaignBehaviorBase`.
     - Namespace: `CalradiaForge.Mod.CampaignBehaviors` (STRICT Anti-Shadowing: NEVER use `Campaign` as class or namespace suffix!).
     - Attribute: `[CalradiaForge.Core.CampaignExtensions.AutoRegisterBehavior]`.
     - Hook into 35+ verified TaleWorlds `CampaignEvents` across:
       a) Hero Lifecycle: `HeroCreated`, `HeroGrowsOutOfInfancyEvent`, `HeroReachesTeenAgeEvent`, `HeroComesOfAgeEvent`, `BeforeHeroKilledEvent`, `HeroKilledEvent`, `HeroWounded`, `HeroOccupationChangedEvent`, `HeroRelationChanged`, `OnHeroChangedClanEvent`, `HeroPrisonerTaken`, `HeroPrisonerReleased`, `OnHeroActivatedEvent`, `OnHeroGetsBusyEvent`.
       b) Clan & Dynastic Succession: `OnClanCreatedEvent`, `OnClanDestroyedEvent`, `ClanTierIncrease`, `OnClanLeaderChangedEvent`, `OnHeirSelectionRequestedEvent`, `OnHeirSelectionOverEvent`, `OnPlayerCharacterChangedEvent`, `OnClanChangedKingdomEvent`, `OnClanDefectedEvent`, `RulingClanChanged`, `OnClanInfluenceChangedEvent`.
       c) Companions & Parties: `NewCompanionAdded`, `CompanionRemoved`, `OnHeroJoinedPartyEvent`, `OnPartyLeaderChangedEvent`, `OnGovernorChangedEvent`.
       d) Marriage & Pregnancy: `OnMarriageOfferedToPlayerEvent`, `OnMarriageOfferCanceledEvent`, `BeforeHeroesMarried`, `RomanticStateChanged`, `OnGivenBirthEvent`.
       e) Character Progression: `HeroGainedSkill`, `HeroLevelledUp`, `PerkOpenedEvent`, `PerkResetEvent`, `PlayerTraitChangedEvent`, `RenownGained`.
       f) Periodic Simulation Ticks: `DailyTickHeroEvent`, `DailyTickClanEvent`, `HourlyTickPartyEvent`, `HourlyTickEvent`, `DailyTickEvent`, `WeeklyTickEvent`.
     - Defensive Null & Boundary Guards: Guard against all 15 edge cases documented in `spec_miner_1/handoff.md` (null killer, null oldLeader, empty aliveChildren, null oldClan, dead heroes, null heroDeveloper, etc.).
     - Modulo-24 Hash Time-Slicing: In periodic hero ticks or evaluations, use `(entity.StringId.GetHashCode() & 0x7FFFFFFF) % 24 == (int)CampaignTime.Now.ToHours % 24` to avoid UI freezes.
     - Engine Crash Constraint: `RegisterEvents()` must only register listeners; defer entity queries to `OnSessionLaunchedEvent` or subsequent ticks.
     - 100% Stateless: `SyncData(IDataStore dataStore)` must be empty/no-op. DO NOT create any class inheriting from `SaveableTypeDefiner`!
     - Meaningful dynamic evaluation: implement genuine, real logic that computes stats/insights or applies vanilla actions safely (e.g. tracking telemetry counters, validating succession candidates, logging progression milestones, etc.).

   - Update `src/CalradiaForge.Mod/SubModule.cs`:
     - Inside `OnGameStart(Game game, IGameStarter gameStarterObject)`:
       Ensure `if (gameStarterObject is TaleWorlds.CampaignSystem.CampaignGameStarter campaignStarter)` registers:
       `campaignStarter.AddBehavior(new CalradiaForge.Mod.CampaignBehaviors.ClanCharacterProgressionBehavior());`
       while preserving `ForgeBehaviorLoader.RegisterAll(campaignStarter);`.

4. Verification:
   - Run `dotnet build CalradiaForge.sln -c Release` and confirm 0 errors, 0 warnings.
   - Run tests: `.\tests\CalradiaForge.Tests\bin\Release\net472\CalradiaForge.Tests.exe`.

5. Write detailed report to:
   `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\worker_1\handoff.md`
   Send a completion message back to parent using send_message.

## 2026-09-20T21:15:31Z
**Context**: User priority instruction received
**Content**: The user has requested: "Diles a los agentes que terminen de implementar los cambios" (Tell the agents to finish implementing the changes). Please finalize the implementation of ClanCharacterProgressionBehavior.cs and SubModule.cs, verify the Release build, and deliver your handoff report promptly.
**Action**: Conclude implementation, run build verification, and send completion report.
