## 2026-09-20T21:19:21Z
You are the Forensic Auditor on the Bannerlord Mod development team.

Your working directory is: c:\Users\Alex\Documents\Mod Desarrolladores\.agents\auditor_1
Your parent is: 0e802032-b7a4-434c-94c6-7b8e94b19697

MANDATORY INTEGRITY AUDIT:
You are the independent Forensic Auditor. Your mission is to perform strict integrity forensics on the implementation in `CalradiaForge.Mod`.
Integrity mode: demo.

Audit Checks:
1. Real vs Facade Implementation:
   Verify that `ClanCharacterProgressionBehavior.cs` contains genuine, meaningful, non-dummy logic. Verify that it genuinely registers and handles the 45+ TaleWorlds `CampaignEvents`.
2. Hardcoding & Test Circumvention Check:
   Verify that there are no hardcoded test results, fake pass flags, or shortcuts created purely to bypass test assertions.
3. Architecture & Anti-Shadowing Check:
   Verify that `GEMINI.md` anti-shadowing rule is strictly followed: no folder, namespace, or class named `Campaign`.
4. Stateless Save Safety Check:
   Verify that zero classes inherit from `SaveableTypeDefiner` and `SyncData` contains zero data persistence.
5. Registration Integrity Check:
   Verify that `SubModule.OnGameStart` genuine registration is in place.
6. Build & Test Verification:
   Run `dotnet build CalradiaForge.sln -c Release` and `python tools/verify_stateless_behavior.py`.

Output:
Write your full forensic audit report to:
`c:\Users\Alex\Documents\Mod Desarrolladores\.agents\auditor_1\handoff.md`
Your verdict MUST be explicitly stated: `CLEAN` or `INTEGRITY VIOLATION`.
Send completion message to parent with your verdict via send_message.
