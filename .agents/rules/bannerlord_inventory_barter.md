# Bannerlord Inventory, Looting, and Barter Systems

When creating custom trade screens, post-battle salvage, item rosters, and diplomatic barter in Mount & Blade II: Bannerlord:

## 1. InventoryManager & InventoryLogic Architecture
- **Coordination**: `InventoryManager` initializes state machines and opens Gauntlet screens (`SPInventoryVM`). `InventoryLogic` controls transaction rules, weight caps, and price curves.
- **Screen Openers**:
  - `OpenScreenAsReceiveItems(ItemRoster, TextObject, DoneLogicExtrasDelegate)`: One-sided salvage/loot (bandit camps, quests, stashes).
  - `OpenScreenAsLoot(Dictionary<PartyBase, ItemRoster>)`: Multi-party post-battle casualty loot.
  - `OpenScreenAsTrade(ItemRoster, SettlementComponent, InventoryCategoryType, DoneLogicExtrasDelegate)`: Interactive shop with market supply-demand curves.
  - `OpenScreenAsStash(ItemRoster)`: Settlement keep / hideout storage.
- **Execution Thread & Context**: Must run on the main thread in `CampaignState`. Never invoke `InventoryManager.OpenScreenAs*` inside an active 3D `Mission` scene; doing so crashes the Gauntlet presentation layer.
- **`DoneLogicExtrasDelegate`**: Callback triggered ONLY when the player confirms via "Done". Skipped if cancelled/reset.

## 2. ItemRoster Manipulation & Underflow Safety
- **Roster Operations**:
  - Add standard: `roster.AddToCounts(item, count)`.
  - Add modified: `roster.AddToCounts(new EquipmentElement(item, modifier), count)`.
- **CRITICAL Underflow Guard (`MBUnderFlowException`)**:
  Calling `roster.AddToCounts(item, -quantity)` when the roster has fewer items than `quantity` throws `MBUnderFlowException`.
  Always validate:
  ```csharp
  int available = roster.GetItemNumber(item);
  if (available > 0)
  {
      roster.AddToCounts(item, -Math.Min(available, requestedAmount));
  }
  ```
- **Quality Modifiers**: Use `item.ItemComponent?.ItemModifierGroup?.GetRandomItemModifierLootScoreBased()` to apply vanilla drop-rate quality modifiers.

## 3. Barter Architecture (`BarterManager`)
- **Singleton**: Accessed via `BarterManager.Instance` or `Campaign.Current.BarterManager`.
- **Barterables**: Inherit from `TaleWorlds.CampaignSystem.BarterSystem.Barterables.Barterable`:
  - `GoldBarterable`, `ItemBarterable`, `PrisonerBarterable`, `FiefBarterable`, `PeaceBarterable`, `MarriageBarterable`.
- **AI Valuation**: `barterable.GetValueForFaction(IFaction)` determines utility score. The AI only accepts when net value $\ge 0$.
- **Event Unsubscription**: When hooking `BarterManager.Instance.BarterBegin` to inject custom barterables, ALWAYS unsubscribe immediately inside the callback to prevent persistent memory leaks across campaign sessions.

## 4. Post-Battle Casualty Looting Engine
- Handled by `MapEvent.CollectLoot()` $\rightarrow$ `LootCollector.LootCasualties()`.
- Equipment drops scale with `TroopLevel`, `TierMultiplier`, and `DefaultBattleRewardModel.CalculateLootChance()`.
- Roguery increases drop rates and unlocks perks (*Deep Pockets*, *Partners in Crime*, *Paid in Promise*).
- Multi-party victories divide loot based on battle contribution share:
  $$\text{LootShare} = \frac{\text{PartyContributionScore}}{\sum \text{SideContribution}}$$

## 5. Anti-Shadowing & Save Persistence (`GEMINI.md`)
- Never serialize `ItemObject` or `ItemRoster` directly into `SyncData`. Store `ItemObject.StringId` and resolve with `MBObjectManager.Instance.GetObject<ItemObject>(id)`.
- Never name any class, namespace, or folder `Campaign`. Use `InventoryExtensions` or `CampaignBehaviors`.
