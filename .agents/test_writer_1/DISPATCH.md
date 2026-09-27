## 2026-09-20T21:14:13Z
You are Test Writer 1 on the Bannerlord Mod development team.

Your working directory is: c:\Users\Alex\Documents\Mod Desarrolladores\.agents\test_writer_1
Your parent is: 0e802032-b7a4-434c-94c6-7b8e94b19697

MANDATORY INSTRUCTIONS:
1. Read the following reference files first:
   - Authoritative Request: `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\ORIGINAL_REQUEST.md`
   - Project Spec: `c:\Users\Alex\Documents\Mod Desarrolladores\PROJECT.md`
   - Test Infra: `c:\Users\Alex\Documents\Mod Desarrolladores\TEST_INFRA.md`
   - Spec Miner 2 Handoff: `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_2\handoff.md`
   - Explorer 1 Handoff: `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\explorer_1\handoff.md`

2. Exclusively Owned Files:
   - `tools/verify_stateless_behavior.ps1`
   - `tools/verify_stateless_behavior.py`
   - `TEST_READY.md`
   - `tests/CalradiaForge.Tests/ClanCharacterProgressionTests.cs` (or unit test additions in `tests/CalradiaForge.Tests`)

3. Tasks:
   - Create `tools/verify_stateless_behavior.ps1` and `tools/verify_stateless_behavior.py` based on the exact specifications in `TEST_INFRA.md` and `spec_miner_2/handoff.md §5.1`.
     They must verify:
     1. `CalradiaForge.sln` compiles in Release mode (`dotnet build CalradiaForge.sln -c Release -v:minimal`).
     2. Statelessness: 0 classes inherit from `SaveableTypeDefiner` and zero custom data synced in `SyncData`.
     3. Anti-Shadowing: zero folders, namespaces, or classes named `Campaign` (GEMINI rule).
     4. SubModule Registration: `SubModule.OnGameStart` registers the behavior via `AddBehavior()`.
   - Add unit test suite `tests/CalradiaForge.Tests/ClanCharacterProgressionTests.cs` testing reflection properties (e.g. subclass of `CampaignBehaviorBase`, empty `SyncData`, `AutoRegisterBehaviorAttribute`).
   - Coordinate with Worker 1's files, run the verification scripts once Worker 1 compiles, and publish `TEST_READY.md` when all checks pass.
   - Write comprehensive report to:
     `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\test_writer_1\handoff.md`
   - Send completion message to parent using send_message.

## 2026-09-20T21:15:35Z
**Context**: User priority instruction received
**Content**: The user has requested: "Diles a los agentes que terminen de implementar los cambios" (Tell the agents to finish implementing the changes). Please finalize tools/verify_stateless_behavior.ps1, tools/verify_stateless_behavior.py, and test cases, execute the checks, and publish TEST_READY.md promptly.
**Action**: Complete test files and send handoff report.
