# Bannerlord MobileParty, Custom Party Components & Spawner Architecture

In Mount & Blade II: Bannerlord, `MobileParty` represents any dynamic party moving across the world map. Modern Bannerlord versions (e1.7+, v1.1.0+) decouple party logic into **`PartyComponent`** subclasses and movement decisions into **`MobilePartyAi`**.

---

## 1. Programmatic Creation & Spawning

### Low-Level Factory:
```csharp
MobileParty party = MobileParty.CreateParty(
    stringId, 
    component, 
    delegate(MobileParty p) { /* pre-setup before registration */ }
);
```

### High-Level Native Component Factories:
Prefer native factory methods to avoid assembly lock-in:
- `CustomPartyComponent.CreateQuestParty(position, spawnRadius, homeSettlement, name, clan, template, leaderHero)`: **Recommended** for quests, scripted events, and custom mercenaries.
- `BanditPartyComponent.CreateBanditParty(stringId, clan, homeHideout, isBossParty)`.
- `CaravanPartyComponent.CreateCaravanParty(ownerHero, settlement, isElite)`.
- `LordPartyComponent.CreateLordParty(stringId, hero, position, spawnRadius, homeSettlement)`.

### Initialization & Troop Allocation:
```csharp
// Initialize position & template
party.InitializeMobilePartyAroundPosition(partyTemplate, spawnPosition, spawnRadius: 3.0f);

// Or manually add troops
party.MemberRoster.AddToCounts(CharacterObject.Find("mercenary_swordsman"), 15);
party.SetCustomWageLimit(1200); // Daily wage ceiling
party.Party.Banner = clan.Banner;
party.Party.SetVisualAsDirty();
```

---

## 2. Party AI & Movement Commands (`MobilePartyAi` in v1.1.0+)

Movement orders are issued through `party.Ai`:
```csharp
// 1. Move to map coordinates
party.Ai.SetMoveGoToPoint(destinationVec2);

// 2. Travel to settlement
party.Ai.SetMoveGoToSettlement(targetSettlement);

// 3. Patrol area around settlement or point
party.Ai.SetMovePatrolAroundSettlement(homeSettlement);
party.Ai.SetMovePatrolAroundPoint(centerPoint, radius: 15f);

// 4. Engage or Escort
party.Ai.SetMoveEngageParty(hostileMobileParty);
party.Ai.SetMoveEscortParty(friendlyCaravan);

// 5. Hold position
party.Ai.SetMoveModeHold();
```

### Preventing AI Overwrite (`DoNotMakeNewDecisions`):
By default, `MobilePartyAi.HourlyTick()` re-evaluates high-level goals and overwrites manual orders.
To lock an AI party to a scripted mission:
```csharp
party.Ai.DoNotMakeNewDecisions = true;
```

---

## 3. Save System Safety & Clean Despawning

### Avoid the Mod Uninstall Trap:
- **Do NOT subclass `PartyComponent`** unless strictly required. If a save contains an instance of `MyModCustomPartyComponent` and the user uninstalls your mod, that save will fail to load with `SaveSystemException`.
- **Solution**: Use native `CustomPartyComponent.CreateQuestParty()` and store your mod's extra state in a `CampaignBehaviorBase` dictionary keyed by `party.StringId`.

### Clean Despawn Protocol:
Never nullify a party reference. Always remove it cleanly through `DestroyPartyAction`:
```csharp
public static void DespawnParty(MobileParty party)
{
    if (party == null || !party.IsActive) return;

    party.Ai.DoNotMakeNewDecisions = false;
    if (party.Army != null)
    {
        party.Army = null;
    }
    // Cleanly remove from map, clear locator grids, and notify CampaignEvents
    DestroyPartyAction.Apply(null, party);
}
```

---

## 4. Architectural Rules
1. **Never name files, namespaces, or classes `Campaign`**: Causes namespace collision with `TaleWorlds.CampaignSystem.Campaign` (`GEMINI.md`).
2. **Never access `party.Ai` before component initialization**: Calling `party.Ai.SetMove...` before `InitializeMobileParty...` throws `NullReferenceException`.
3. **Always serialize `party.StringId`**, never `MobileParty` instances directly.
4. **Valid Navmesh Coordinates**: Ensure spawn coordinates are on passable terrain using `InitializeMobilePartyAroundPosition` or `Campaign.Current.MapSceneWrapper.GetFaceIndexForPoint`.
