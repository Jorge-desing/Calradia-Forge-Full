## 2026-09-20T21:08:15Z

<USER_REQUEST>
You are Spec Miner 2 on the Bannerlord Mod development team.

Your working directory is: c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_2
Your parent is: 0e802032-b7a4-434c-94c6-7b8e94b19697

MANDATORY INSTRUCTIONS:
1. You MUST read the authoritative user request at:
   c:\Users\Alex\Documents\Mod Desarrolladores\.agents\ORIGINAL_REQUEST.md
2. Investigate all architectural rules, acceptance criteria, and constraints:
   - Check `.agents/rules/` including `GEMINI.md` (anti-shadowing: NEVER use `Campaign` as a folder, namespace, or class name), `bannerlord_architecture.md`, `bannerlord_save_system.md`, `bannerlord_mission_lifecycle.md`.
   - Engine Initialization Crash Constraint: how to safely defer complex logic in behaviors so it doesn't crash during game/campaign startup.
   - Stateless requirement: analyze how to guarantee zero `SaveableTypeDefiner` and zero serialization in `SyncData(IDataStore dataStore)`.
   - Registration requirements: how `MBSubModuleBase.OnGameStart` accepts `AddBehavior(new ...)`.
   - Acceptance verification: define exact programmatic validation methods (PowerShell / Python script requirements) to check:
     a) `CalradiaForge.sln` compiles in Release mode.
     b) No classes inherit from `SaveableTypeDefiner` and `SyncData` contains no synced fields.
     c) SubModule registers the behavior via `AddBehavior()`.
3. Write your comprehensive findings to:
   `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_2\handoff.md`
4. Send a concise completion message back to parent when done using send_message.

DO NOT write or modify any source code files. You are strictly read-only specification mining.
</USER_REQUEST>
