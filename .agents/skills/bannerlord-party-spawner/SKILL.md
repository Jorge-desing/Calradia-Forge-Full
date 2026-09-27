---
name: bannerlord-party-spawner
description: Programmatic spawning, AI orders, and save safety for MobileParty and PartyComponents in Mount & Blade II Bannerlord. See bannerlord-shared-patterns for CampaignBehaviorBase/SyncData boilerplate.
---

# Bannerlord Party Spawner Skill

Use this skill when programmatically spawning, controlling, configuring, or destroying mobile parties, caravans, bandits, or custom mercenary details.

> **Prerequisites:** Read `bannerlord-shared-patterns` first for the CampaignBehaviorBase skeleton, SyncData patterns, and universal safety rules.

---

## When to Use This Skill
- Creating dynamic quest or patrol parties via `CustomPartyComponent.CreateQuestParty`.
- Issuing persistent movement orders (`SetMoveGoToSettlement`, `SetMovePatrolAroundPoint`, `SetMoveEngageParty`).
- Locking party AI from native decision overwrites with `DoNotMakeNewDecisions = true`.
- Safely despawning parties using `DestroyPartyAction.Apply(null, party)`.

---

## Domain Implementation — Spawn, Order, Track, Despawn

```csharp
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CalradiaForge.Core.CampaignBehaviors
{
    public class CustomSpawnerBehavior : CampaignBehaviorBase
    {
        private List<string> _activeSpawnedPartyIds = new();

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("Forge_ActiveParties", ref _activeSpawnedPartyIds);
            if (dataStore.IsLoading) _activeSpawnedPartyIds ??= new();
        }

        public MobileParty SpawnPatrolParty(Settlement settlement, Vec2 mapPosition)
        {
            string id = $"forge_patrol_{CampaignTime.Now.ToMilliseconds}";

            // Native component — prevents save corruption if mod is removed
            CustomPartyComponent component = CustomPartyComponent.CreateQuestParty(
                mapPosition,
                spawnRadius: 2.5f,
                homeSettlement: settlement,
                name: new TextObject("{=forge_patrol_title}Border Vanguard"),
                clan: settlement.OwnerClan,
                template: null,
                leader: null
            );

            MobileParty party = component.MobileParty;
            party.StringId = id;

            // Configure troops
            CharacterObject troop = CharacterObject.Find("mercenary_swordsman");
            if (troop != null) party.MemberRoster.AddToCounts(troop, 15);

            // Wage and visual settings
            party.SetCustomWageLimit(800);
            if (settlement.Banner != null)
            {
                party.Party.Banner = settlement.Banner;
                party.Party.SetVisualAsDirty();
            }

            // Lock AI and issue patrol order
            party.Ai.DoNotMakeNewDecisions = true;
            party.Ai.SetMovePatrolAroundSettlement(settlement);

            _activeSpawnedPartyIds.Add(id);
            return party;
        }

        public void DespawnParty(string partyId)
        {
            MobileParty party = Campaign.Current.CampaignObjectManager.Find<MobileParty>(partyId);
            if (party != null && party.IsActive)
            {
                party.Ai.DoNotMakeNewDecisions = false;
                DestroyPartyAction.Apply(null, party);
            }
            _activeSpawnedPartyIds.Remove(partyId);
        }

        private void OnDailyTick()
        {
            // Prune defeated or expired parties from tracking list
            _activeSpawnedPartyIds.RemoveAll(id =>
            {
                MobileParty p = Campaign.Current.CampaignObjectManager.Find<MobileParty>(id);
                return p == null || !p.IsActive;
            });
        }
    }
}
```

---

## Critical Domain Rules
1. **Never subclass `PartyComponent`** — use native `CustomPartyComponent.CreateQuestParty` to avoid unresolvable assembly types if the mod is uninstalled.
2. **Always call `DestroyPartyAction.Apply(null, party)`** to despawn — never leave orphaned references.
3. **Always store `party.StringId`** — never serialize `MobileParty` instances directly.
4. **Reset `DoNotMakeNewDecisions = false` before despawning** — otherwise the engine may try to execute orders on a destroyed party.
