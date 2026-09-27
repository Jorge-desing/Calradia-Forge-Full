# Bannerlord Economic Simulation, Workshops & Trade Architecture

Mount & Blade II: Bannerlord operates a closed-loop macroeconomic simulation across primary agrarian production, caravan arbitrage, urban workshop transformation, and dynamic market supply/demand clearing.

---

## 1. Urban Workshops (`Workshop` & `WorkshopCampaignBehavior`)
- **Capacity**: Towns support 3–4 workshop slots.
- **Conversion Lifecycle**:
  - Daily tick deducts `WorkshopModel.GetDailyExpense()` (base $\approx 100$ denars) from `Workshop.Capital`.
  - Raw inputs are purchased from `Town.MarketData` or pulled from the player warehouse.
  - When production progress reaches $1.0$, inputs are consumed and outputs generated.
  - Finished goods are sold into the town market, depositing sales proceeds into `Workshop.Capital`.
- **The 20% Profit Sweep Formula**:
  When end-of-day capital exceeds baseline ($Capital > 10{,}000$ denars):
  $$\text{Surplus} = \text{Workshop.Capital} - 10{,}000$$
  $$\text{DailyPayout} = \lfloor 0.20 \times \text{Surplus} \rfloor$$
  $$\text{Workshop.Capital} \leftarrow \text{Workshop.Capital} - \text{DailyPayout}$$
  $$\text{Clan.Leader.ChangeClanGold}(\text{DailyPayout})$$
- **Bankruptcy**: If capital drops $\le 0$, production halts until market input costs fall or capital is injected.

---

## 2. Dynamic Market Pricing (`Town.MarketData`)
The market does not use static prices. Prices adjust along a non-linear supply/demand curve:
- **Demand**: Scaled by town prosperity ($\text{Town.Prosperity} / 1000.0$), population, garrison size, and workshop input requirements.
- **Supply**: Total count in `Town.Settlement.ItemRoster`.
- **Price Factor Curve ($F$)**:
  $$F(\text{Supply}, \text{Demand}) = \text{Clamp}\left( \left( \frac{\text{Demand} + 1.0}{\text{Supply} + 1.0} \right)^{\alpha}, \; 0.30, \; 3.00 \right)$$
- **Bid-Ask Spread & Trade Penalty**:
  $$\text{BuyPrice} = \text{BaseValue} \times F \times (1.0 + \text{TradePenalty})$$
  $$\text{SellPrice} = \text{BaseValue} \times F \times (1.0 - \text{TradePenalty})$$
- **Trade Tariffs**: Base $10\%$ transaction tariff accumulates in `Town.TradeTaxAccumulated` and pays out daily to the fief owner clan.

---

## 3. Caravans & Trade Routes (`CaravanPartyComponent`)
- **AI Destination Utility**:
  $$\text{Score}(T) = \frac{\text{CargoProfit} + \text{PotentialOutwardProfit}}{\text{Distance}^{1.2}} \times \text{SafetyFactor}(T)$$
  Filters out towns belonging to hostile factions, towns under siege, or surrounded by high threat density.
- **Finances**: Operating purse ($\text{PartyTradeGold}$, base $10{,}000$ denars). If $> 10{,}000$, excess daily profits sweep to the player clan. If gold hits $0$, unpaid troop desertion begins.

---

## 4. Village Primary Production (`VillageProductionCalculatorModel`)
- **Hearth Scaling**:
  - Raided/Struggling ($< 200$ Hearths): $-50\%$ output.
  - Stable ($200$–$600$ Hearths): Normal output.
  - Prosperous ($> 600$ Hearths): $+20\%+$ output.
- **Shipment Logistics**:
  - Produced goods stage in `Village.Settlement.ItemRoster`.
  - Spawns `VillagerPartyComponent` to transport cargo to `village.TradeBound`.
  - Villagers sell goods in town, replenish town food stocks, and return with gold to increase village Hearths (+1 to +2). If raided on the road, town prices spike and famine looms.

---

## 5. Economic GameModel Decorators (Zero Harmony)
Extend economic models cleanly by decorating the existing instance:
- `WorkshopModel`: Tune daily expense, player workshop limits, conversion costs.
- `VillageProductionCalculatorModel`: Tune daily item outputs and famine relief subsidies.
- `TradeItemPriceFactorModel`: Adjust trade penalty and bid-ask spread.
- `SettlementEconomyModel`: Adjust trade tax extraction and tariff rates.
