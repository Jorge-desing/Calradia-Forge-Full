# Bannerlord GameModel & ExplainedNumber Architecture

Bannerlord uses a pure Model-based architecture (`GameModel`) for simulation calculations (e.g. party speed, party capacity, daily wages, settlement prosperity, troop morale, upgrade XP). GameModels are stateless calculation engines that can be extended or replaced cleanly without external detours.

---

## 1. Registration Lifecycle
- **Entry Point**: `MBSubModuleBase.OnGameStart(Game game, IGameStarter gameStarterObject)`
- **Registration**: Cast `gameStarterObject` to `CampaignGameStarter` and call `AddModel()`:
  ```csharp
  protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
  {
      base.OnGameStart(game, gameStarterObject);
      if (gameStarterObject is CampaignGameStarter campaignStarter)
      {
          var previousModel = campaignStarter.Models.OfType<PartySpeedCalculatingModel>().LastOrDefault();
          campaignStarter.AddModel(new CustomPartySpeedModel(previousModel));
      }
  }
  ```
- **Rule of Precedence**: In TaleWorlds' engine, **the last registered model wins**. When querying `Campaign.Current.Models.PartySpeedCalculatingModel`, the engine returns the most recently added instance.

---

## 2. The Native Decorator Pattern (Zero External Dependencies)
To ensure compatibility with other mods and preserve native mechanics without Harmony:
1. Accept the previous model instance in your custom model's constructor:
   `public CustomPartySpeedModel(PartySpeedCalculatingModel baseModel) => _baseModel = baseModel;`
2. In each method override, delegate first to `_baseModel`:
   `ExplainedNumber result = _baseModel != null ? _baseModel.CalculateFinalSpeed(mobileParty, baseSpeed) : baseSpeed;`
3. Append or adjust modifiers cleanly onto `result`.

---

## 3. The `ExplainedNumber` Struct Mechanics
`ExplainedNumber` is a **value-type struct** (`TaleWorlds.CampaignSystem.ExplainedNumber` or `TaleWorlds.Core.ExplainedNumber`).

### Formula & Math:
The final value of an `ExplainedNumber` is computed as:
$$\text{Result} = (\text{BaseNumber} + \sum \text{Adds}) \times \left(1.0 + \sum \text{Factors}\right)$$

- **`Add(float value, TextObject description = null)`**: Adds an additive term.
- **`AddFactor(float factor, TextObject description = null)`**: Adds a percentage modifier ($0.10\text{f} = +10\%$, $-0.25\text{f} = -25\%$).
- **`LimitMin(float min)` / `LimitMax(float max)`**: Clamps the lower or upper bound. Always clamp speeds (minimum $0.1\text{f}$) and capacities to prevent division by zero or navigation freezes.

### Critical Struct Value-Semantics:
Because `ExplainedNumber` is a `struct`, mutating a copy does NOT mutate the original unless reassigned or passed via `ref`:
```csharp
// CORRECT:
ExplainedNumber result = _baseModel.CalculateFinalSpeed(party, baseSpeed);
result.AddFactor(0.15f, new TextObject("{=forge_buff}Custom Buff"));
return result;

// WRONG (Copy lost):
// Do not pass ExplainedNumber into void methods expecting in-place mutation without 'ref'.
```

### The `includeDescriptions` Performance Gate:
- When the game performs background simulation ticks, `includeDescriptions = false` to avoid allocating `TextObject` strings and formatting overhead.
- When the player hovers over a UI element (like party speed in the bottom bar), `includeDescriptions = true`.
- **Golden Rule**: Always pass descriptions when calling `.Add()` or `.AddFactor()`, but let `ExplainedNumber` internally discard them when descriptions are disabled:
  ```csharp
  result.AddFactor(0.10f, new TextObject("{=forge_speed_mod}Leadership Momentum"));
  ```

---

## 4. Key Campaign GameModels Reference
- `PartySpeedCalculatingModel`: Map movement speed.
- `PartySizeLimitModel`: Max troops in party/garrison.
- `PartyWageModel`: Daily troop maintenance costs.
- `PartyMoraleModel`: Daily morale drift and combat battle morale base.
- `SettlementProsperityModel`: Town/Castle daily prosperity growth.
- `SettlementLoyaltyModel`: Town daily loyalty drift.
- `SettlementFoodModel`: Settlement food supply and starvation thresholds.
- `ItemValueModel`: Equipment bartering, shop prices, trade penalties.
- `TroopUpgradeTrackerModel`: XP requirements and prerequisites for promotions.

---

## 5. Architectural Safeguards
1. **Never name files, sub-namespaces, or classes `Campaign`**: Shadows `TaleWorlds.CampaignSystem.Campaign`.
2. **Stateless Operations**: Models must be stateless. Never store party-specific or settlement-specific data inside fields of a `GameModel`. Persist any custom state in a `CampaignBehaviorBase` instead.
3. **Always Fallback**: If `_baseModel` is null (e.g. edge-case early initialization), supply sensible defaults rather than throwing `NullReferenceException`.
