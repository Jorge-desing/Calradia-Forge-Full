---
name: bannerlord-inventory-barter
description: Best practices, InventoryManager, ItemRoster underflow safety, EquipmentElement modifiers, and BarterManager in Mount & Blade II Bannerlord without external detours.
---

# Bannerlord Inventory, Looting & Barter Systems

This skill provides patterns for programmatic trade screens, post-battle salvage stashes, item roster mutations, and conversation barters in Mount & Blade II: Bannerlord.

> **Prerequisites:** Read `bannerlord-shared-patterns` for the CampaignBehaviorBase skeleton, SyncData patterns, and universal safety rules.

## 1. Post-Battle Salvage Screen (InventoryManager)

Open an interactive salvage stash without triggering crashes:

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace CalradiaForge.InventoryExtensions
{
    public static class CustomSalvageHelper
    {
        public static void OpenSalvageLootScreen(DoneLogicExtrasDelegate onDone)
        {
            if (Campaign.Current == null || PartyBase.MainParty == null) return;

            ItemRoster salvageRoster = new ItemRoster();

            // Populate guaranteed commodity
            ItemObject grain = MBObjectManager.Instance.GetObject<ItemObject>("grain");
            if (grain != null) salvageRoster.AddToCounts(grain, 20);

            // Populate quality-modified weapon
            ItemObject sword = MBObjectManager.Instance.GetObject<ItemObject>("spatha_m1_iron");
            if (sword != null)
            {
                ItemModifier modifier = sword.ItemComponent?.ItemModifierGroup?.GetRandomItemModifierLootScoreBased();
                salvageRoster.AddToCounts(new EquipmentElement(sword, modifier), 1);
            }

            TextObject header = new TextObject("{=salvage_cache}Salvaged War Booty");

            InventoryManager.OpenScreenAsReceiveItems(salvageRoster, header, () =>
            {
                onDone?.Invoke();
            });
        }
    }
}
```

## 2. Safe ItemRoster Removal (Underflow Guard)

Prevent `TaleWorlds.Core.MBUnderFlowException` when consuming or selling supplies:

```csharp
using System;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace CalradiaForge.InventoryExtensions
{
    public static class RosterSafeOperations
    {
        public static bool TryConsumeItems(ItemRoster roster, ItemObject item, int amountToConsume)
        {
            if (roster == null || item == null || amountToConsume <= 0) return false;

            int currentCount = roster.GetItemNumber(item);
            if (currentCount >= amountToConsume)
            {
                roster.AddToCounts(item, -amountToConsume);
                return true;
            }
            return false;
        }
    }
}
```

## 3. Conversation-Driven Barter Hook (BarterManager)

Initiate noble negotiations via dialogue while avoiding event listener memory leaks:

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.Party;

namespace CalradiaForge.InventoryExtensions
{
    public class BarterDialogueBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddPlayerLine(
                "forge_start_barter",
                "hero_main_options",
                "forge_barter_response",
                "{=barter_propose}Let us negotiate terms.",
                () => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.IsLord,
                ExecuteBarter,
                110
            );

            starter.AddDialogLine(
                "forge_barter_response",
                "forge_barter_response",
                "hero_main_options",
                "{=barter_agree}Very well, show me your offer.",
                null,
                null,
                110
            );
        }

        private void ExecuteBarter()
        {
            Hero partner = Hero.OneToOneConversationHero;
            if (partner == null) return;

            // Invariant: Unsubscribe immediately to prevent memory leak across campaign sessions
            BarterManager.Instance.BarterBegin += HandleBarterBegin;

            BarterManager.Instance.StartBarter(
                Hero.MainHero,
                partner,
                PartyBase.MainParty,
                partner.PartyBelongedTo?.Party ?? partner.CurrentSettlement?.Party
            );
        }

        private void HandleBarterBegin(BarterData data)
        {
            BarterManager.Instance.BarterBegin -= HandleBarterBegin;
            // Inject custom Barterables into data if needed
        }
    }
}
```
