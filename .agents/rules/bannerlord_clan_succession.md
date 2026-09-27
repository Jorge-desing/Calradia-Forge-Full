# Bannerlord Clan Management, Companion Roles, and Dynastic Succession

When developing or modifying clan progression, companion party roles, marriage, pregnancy, and heir succession in Mount & Blade II: Bannerlord:

## 1. Clan Structure & Progression (`ClanTierModel`)
- **Domain Entity**: `Clan` (`clan.Leader`, `clan.Tier`, `clan.Renown`, `clan.Companions`, `clan.Lords`).
- **Tier Advancement**: Renown thresholds are queried from `ClanTierModel.GetRequiredRenownForTier(tier)`.
  - Tier 1 (50 Renown): Mercenary contracts.
  - Tier 2 (150 Renown): Vassalage.
  - Tier 3 (350 Renown): Army creation.
  - Tier 4 (900 Renown): Kingdom creation.
- **Companion Limit**: Evaluated in `ClanTierModel.GetCompanionLimit(Clan)`.
  - Formula: $\text{Base (3)} + \text{ClanTier} + \text{PerkModifiers}$.
  - **Family Distinction**: Clan lords and bloodline members (`clan.Lords`: spouse, children, siblings) **never** consume companion slots. Only wanderers (`hero.IsCompanion`) consume slots.
  - Assigned companions (governors, caravans, party leaders) still count against the global companion cap.

## 2. Companion Party Roles Architecture (`MobileParty`)
Every mobile party exposes 4 effective role properties:
- `EffectiveScout` (`Scouting`): overland speed, environmental terrain penalty reduction, spotting vision cone, tracking duration.
- `EffectiveQuartermaster` (`Steward`): troop member capacity ($+1$ slot per 4 points), daily troop wages reduction, food variety morale bonuses.
- `EffectiveSurgeon` (`Medicine`): casualty survival rate ($+0.1\%$ per point), recovery rate from wounds, starvation attrition reduction.
- `EffectiveEngineer` (`Engineering`): hourly siege engine build progress, wall crack speed, fortification assault durability.
- **Leader Fallback Invariant**: If no companion is assigned to a role, the property automatically falls back to `party.LeaderHero`. If leader is null, returns null. Always use null-coalescing checks before reading skill/perks.

## 3. Dynastic Lifecycle: Marriage, Birth, and Succession
- **Marriage (`MarriageAction.Apply`)**:
  - Validation via `MarriageModel.IsCoupleSuitableForMarriage` ($\ge 18$ years old, both unmarried, incest filters passed).
  - Clan absorption: Female leaves her clan and joins male's clan, UNLESS she is the reigning Clan Leader (e.g. Rhagaea or player) or player clan is involved.
- **Conception & Labor (`PregnancyModel`)**:
  - Daily check for married female heroes aged 18–45. Spouses must share the same mobile party or settlement keep.
  - Gestation duration: exactly 36 campaign days.
  - Delivery rates: 96% single child, 3% twins, 1.5% maternal mortality in labor, 1% stillbirth.
- **Aging & Natural Mortality**:
  - Age 0–2: Infant. Age 3–17: Child/Adolescent. Age 18: Adult (active combatant/noble).
  - Age 47+: Natural death checks roll daily via `DefaultHeroDeathProbabilityCalculationModel`.
- **Heir Succession Actions**:
  - Main hero death triggers `ApplyHeirSelectionAction.ApplyByDeath(selectedHeir)`.
  - Updates `Hero.MainHero` via `ChangePlayerCharacterAction.Apply(selectedHeir)`.
  - Updates clan leader via `ChangeClanLeaderAction.Apply(Clan.PlayerClan, selectedHeir)`.
  - If no living adult family members exist in `Clan.PlayerClan`, dynastic extinction triggers Campaign Over.

## 4. Save Persistence & Anti-Shadowing Rules
- **StringId Serialization Contract**: Never store `Hero` or `Clan` objects directly inside custom collections. Save their string identifier (`hero.StringId`) and resolve upon load via `MBObjectManager.Instance.GetObject<Hero>(id)`.
- **`SaveableTypeDefiner`**: Base ID must be $\ge 2{,}500{,}000$.
- **Anti-Shadowing Constraint (`GEMINI.md`)**: Never name a folder, namespace, or class `Campaign`. Use `ClanManagement`, `DynastyExtensions`, or `CampaignBehaviors`.
