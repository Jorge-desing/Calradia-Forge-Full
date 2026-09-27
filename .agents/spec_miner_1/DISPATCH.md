## 2026-09-20T21:08:15Z

You are Spec Miner 1 on the Bannerlord Mod development team.

Your working directory is: c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_1
Your parent is: 0e802032-b7a4-434c-94c6-7b8e94b19697

MANDATORY INSTRUCTIONS:
1. You MUST read the authoritative user request at:
   c:\Users\Alex\Documents\Mod Desarrolladores\.agents\ORIGINAL_REQUEST.md
2. Mine specifications and hooks for Clan and Character development in Bannerlord:
   - Read skills at:
     - `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\skills\bannerlord-campaign-behavior\SKILL.md`
     - `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\skills\bannerlord-clan-succession\SKILL.md`
     - `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\skills\bannerlord-character-development\SKILL.md`
     - `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\skills\bannerlord-shared-patterns\SKILL.md`
   - Investigate TaleWorlds CampaignEvents available in the referenced Bannerlord DLLs or codebase:
     - Hero creation, birth, coming of age, death, execution, hero wounded/killed.
     - Clan leader change, dynastic succession, clan tier change, companion recruitment/dismissal/spawning.
     - Marriage, pregnancy, child birth.
     - Character progression: skill level up, hero level up, perk changes, renown/influence gains.
     - Periodic ticks (DailyTickHero, DailyTickClan, HourlyTickParty, etc.) for stateless evaluation.
3. Formulate an exhaustive catalog of campaign event hooks and stateless logic patterns (using vanilla game state, Hero/Clan properties, without saving any custom data).
4. Write your comprehensive findings to:
   `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_1\handoff.md`
5. Send a concise completion message back to parent when done using send_message.

DO NOT write or modify any source code files. You are strictly read-only specification mining.
