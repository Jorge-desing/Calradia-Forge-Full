# BRIEFING — 2026-09-20T21:13:00Z

## Mission
Mine and document an exhaustive catalog of CampaignEvent hooks, GameModels, TaleWorlds APIs, and stateless logic patterns for Clan and Character development in Mount & Blade II Bannerlord.

## 🔒 My Identity
- Archetype: specification-miner
- Roles: Specification Miner, Teamwork specialist
- Working directory: c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_1
- Original parent: 0e802032-b7a4-434c-94c6-7b8e94b19697
- Milestone: Clan and Character Development Specification Mining

## 🔒 Key Constraints
- Strictly read-only: do not write or modify any source code files.
- Never name a folder, sub-namespace, or class `Campaign` within this project.
- Write only to our agent folder: `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_1`.
- Follow the 5-component handoff report protocol and feature discovery tables.

## Current Parent
- Conversation ID: 0e802032-b7a4-434c-94c6-7b8e94b19697
- Updated: 2026-09-20T21:13:00Z

## Task Summary
- **What to build**: Exhaustive specification catalog of CampaignEvent hooks, Hero/Clan APIs, CharacterDevelopmentModel, ClanTierModel, and stateless logic patterns.
- **Success criteria**: Comprehensive feature tables, parameter signatures, edge cases, error behaviors, and stateless evaluation patterns.
- **Interface contracts**: TaleWorlds.CampaignSystem assemblies and APIs.
- **Code layout**: Metadata strictly in `.agents/spec_miner_1/`.

## Key Decisions Made
- Decompiled and reflected live `TaleWorlds.CampaignSystem.dll` from `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client`.
- Cataloged 276 `CampaignEvents` properties, filtered down to 45+ authoritative hooks for Hero Lifecycle, Clan Lifecycle, Succession, Companions, Marriage/Pregnancy, Progression, and Periodic Ticks.
- Formulated zero-save-data stateless architecture with time-sliced deterministic anti-lag hashing.

## Artifact Index
- `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_1\DISPATCH.md` — Dispatch instructions
- `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_1\progress.md` — Liveness heartbeat
- `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_1\handoff.md` — Final mining report
- `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_1\all_campaign_events.csv` — Dumped static properties
- `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\spec_miner_1\all_events_full.csv` — Exact generic types of all 276 events

## Loaded Skills
- **Source**: `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\skills\bannerlord-campaign-behavior\SKILL.md`
  - Core methodology: Event subscription catalog, dialogue/menu injection, and anti-lag time-slicing patterns for CampaignBehaviorBase.
- **Source**: `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\skills\bannerlord-clan-succession\SKILL.md`
  - Core methodology: ClanTierModel, companion party roles, marriage, pregnancy, dynastic succession.
- **Source**: `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\skills\bannerlord-character-development\SKILL.md`
  - Core methodology: HeroDeveloper, CharacterDevelopmentModel, skill learning rates, perk role evaluation.
- **Source**: `c:\Users\Alex\Documents\Mod Desarrolladores\.agents\skills\bannerlord-shared-patterns\SKILL.md`
  - Core methodology: Decorator Pattern for GameModels, SaveableTypeDefiner, CampaignBehaviorBase skeleton.
