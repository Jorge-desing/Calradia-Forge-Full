# Bannerlord Crime, Town Alleys, and Underworld Architecture

When developing crime systems, urban alley rackets, gang leader dynamics, and rogue operations in Mount & Blade II: Bannerlord:

## 1. Faction-Level Crime Rating & `CrimeModel`
- **Faction State**: Crime rating is tracked per faction on `IFaction.MainHeroCrimeRating`.
- **Modifying Crime**: Always use the action pattern:
  `ChangeCrimeRatingAction.Apply(IFaction faction, float deltaCrime, bool showNotification = true)`.
- **Decay & Bribes**: Governed by `CrimeModel` (native: `DefaultCrimeModel`). Evaluates daily decay and fine/bribe costs based on player Roguery/Charm perks.
- **Consequence Tiers**:
  - `0–29 (Negligible)`: Normal settlement access.
  - `30–59 (Moderate)`: Gate guards challenge entry; minor offenses can be settled with gold bribes.
  - `60+ (Severe / Outlaw)`: Direct gate lockout. Entry requires sneaking in disguise (`IsMainHeroDisguised`). If intercepted, options are Bribe, Surrender ("Pound of Flesh" imprisonment/injury), or Street Fight.
  - Extreme criminal acts provide the faction with *casus belli*, triggering `DeclareWarAction`.

## 2. Urban Alleys (`Alley`) & Gang Leader Rackets
- **Gang Leaders**: Town notables with `CharacterObject.Occupation == Occupation.GangLeader`. Control alleys and offer rogue quests.
- **Urban Alleys (`Alley`)**: Each major town contains ~3 physical alleys (`town.Alleys`).
- **Clearing Alleys**: Managed by `AlleyCampaignBehavior` and `AlleyFightMissionController`. Player enters street scene in civilian equipment (`CivilianEquipment`) and defeats the occupying gang thugs in combat.
- **Claiming Rackets & Companion Assignment**:
  - Requirements: Assigned companion must have `Roguery >= 30` and cannot have high Mercy (`DefaultTraits.Mercy <= 0`).
  - Player stations a troop garrison with the companion.
  - Benefits: Daily gold yield, periodic rogue troop generation, continuous Roguery XP ticks.
  - Cost: Generates **+0.5 daily Crime Rating** with the town's owner kingdom.
  - Turf Wars: Rival gangs trigger "Alley Under Attack" countdowns requiring the player to defend the alley in street combat or forfeit it.

## 3. Tavern Recruitment & Rogue Infiltration
- **Mercenary Spawning**: `RecruitmentCampaignBehavior` refreshes tavern mercenary pools daily (`CampaignEvents.DailyTickTownEvent`) based on culture definitions in `spcultures.xml`.
- **Dungeon Jailbreaks**: Infiltrating town dungeons in disguise to liberate captive lords; requires neutralizing dungeon guards and fighting to the gate in civilian gear.

## 4. Native Extension Patterns (Zero Harmony)
- **Decorator for `CrimeModel`**: Wrap `CrimeModel` to modify daily crime decay, bribe costs, or rogue perk effects without bytecode patches.
- **Custom Underworld `CampaignBehaviorBase`**:
  - Listen to `CampaignEvents.DailyTickTownEvent` and `CampaignEvents.AlleyCleared`.
  - Inject dialogue lines into `CampaignGameStarter` during `OnSessionLaunchedEvent` with priority $>100$.
  - Add game menu options to `town_backstreet` via `starter.AddGameMenuOption`.
- **Save Persistence**: Never store `Alley` or `Town` entities directly into custom dictionaries. Save `Settlement.StringId` or `Hero.StringId`.
- **Anti-Shadowing Constraint (`GEMINI.md`)**: Never name a folder, namespace, or class `Campaign`. Use `Underworld`, `CrimeExtensions`, or `CampaignBehaviors`.
