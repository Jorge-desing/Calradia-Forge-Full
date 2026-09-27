# Bannerlord Settlement Projects, Prosperity, and Rebellion Mechanics

When developing settlement systems, construction projects, demographic equilibrium, and urban rebellions in Mount & Blade II: Bannerlord:

## 1. Settlement Hierarchy & Construction Queues
- **Domain Hierarchy**: `Settlement` contains `Town` (for towns and castles) or `Village` (rural dependencies with `Hearth` scale).
- **Building Lifecycle**:
  - `BuildingType`: Static definition (Level 1–3 point requirements, e.g., 600/1200/1800).
  - `Building`: Stateful instance per town (`CurrentLevel`, `BuildingProgress`).
  - `Town.BuildingsInProgress`: FIFO `Queue<Building>`. Advances daily using construction power from `BuildingConstructionModel`.
- **Default Daily Projects**: When the queue is empty, civic power diverts into one continuous project:
  - `Housing`: $+0.5$ to $+1.0$ daily prosperity.
  - `Irrigation`: $+0.5$ to $+1.0$ daily village hearth growth (boosts rural food shipments).
  - `Festivals and Games`: $+3.0$ daily loyalty (primary tool to stop rebellion spirals).
  - `Train Militia`: $+1.0$ to $+2.0$ daily militia recruitment.
- **The Loyalty Strike Gate**: If $\text{Town.Loyalty} \le 25$, daily construction power is clamped to **strictly 0**. No buildings progress and gold reserve boosts are frozen.

## 2. Equilibrium Mathematics & The Consumption Trap
- **Loyalty (`SettlementLoyaltyModel`)**:
  - 50-Equilibrium Drift: $\Delta \text{Loyalty} = 0.1 \times (50 - \text{CurrentLoyalty})$.
  - Culture Mismatch: $-3.0$ / day if settlement culture differs from owner ruler culture.
  - Governor Culture: $+1.0$ if matching settlement culture; $-1.0$ if foreign.
  - Starvation: $-2.0$ / day when food stocks reach 0.
- **Food Stocks (`SettlementFoodModel`)**:
  - Consumption formula: $\text{CitizenFoodConsumption} = \frac{\text{Prosperity}}{50} = 0.02 \times \text{Prosperity}$. High prosperity creates immense food demand.
  - Surplus food beyond granary capacity converts into prosperity at $+0.10 \times \text{Surplus}$.
  - Famine collapse: When food is 0, prosperity drops by $-20$ to $-30$ per day and garrison troops desert.
- **Security (`SettlementSecurityModel`)**:
  - High garrisons project security ($+3.0$ to $+5.0$/day); 0 garrison incurs $-3.0$/day penalty. Raided villages impose $-2.0$ security penalty each.

## 3. Rebellion Lifecycle State Machine
1. **Civil Strike**: $\text{Loyalty} \le 25$. Construction stops, workshops freeze.
2. **Power Inversion Gate**: $\text{MilitiaStrength} > \text{GarrisonStrength}$. If garrison is weak, `RebellionsCampaignBehavior.CheckRebellionEvent` activates daily dice rolls.
3. **Uprising (`StartRebellionEvent`)**:
   - Garrison is destroyed/expelled.
   - Culture-matched procedural Rebel Clan is instantiated with 3–4 generated heroes.
   - Town ownership transfers to Rebel Clan; loyalty resets to 100.
   - `DeclareWarAction.ApplyByDefault` initiates war between rebels and previous owner.
4. **Legitimacy Transition (30-Day Rule)**:
   - If the rebel clan holds the town for 30 consecutive days, the provisional rebel flag is removed and the clan converts into a permanent sovereign minor clan eligible for diplomacy, marriage, and vassalage.

## 4. Custom Buildings Architecture (Zero Harmony)
- **Engine Save-Safety Rule**: Never subclass `Building` or `BuildingType` to inject into native `Town.Buildings`. Doing so corrupts save files if the mod is removed.
- **Decoupled Architecture**:
  1. Store custom building state in a lightweight `CustomBuildingRecord` tracked by a `CampaignBehaviorBase`.
  2. Advance construction in `CampaignEvents.DailyTickSettlementEvent`.
  3. Reflect bonuses into town tooltips by decorating `SettlementLoyaltyModel`, `SettlementProsperityModel`, or `BuildingConstructionModel` using `result.Add(bonus, textObject)`.
- **Anti-Shadowing Constraint (`GEMINI.md`)**: Never name a folder, namespace, or class `Campaign`.
