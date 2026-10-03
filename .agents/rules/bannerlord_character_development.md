# Bannerlord Character Development, Skills, and Perks Architecture

When modifying or extending character leveling, skill progression, focus points, attribute points, and perk mechanics in Mount & Blade II: Bannerlord:

## 1. Dual-Layer Progression Architecture
- **State & Entity Layer (`HeroDeveloper` / `IHeroDeveloper`)**:
  - Resides on `hero.HeroDeveloper`.
  - Holds `TotalXp`, `UnspentFocusPoints`, `UnspentAttributePoints`, and unlocked perks.
  - Modifying focus/attributes: `heroDeveloper.AddFocus(skill, amount)`, `heroDeveloper.AddAttribute(attribute, amount)`.
  - Maximum caps: 5 focus points per skill, 10 points per attribute.
  - Adding XP: `heroDeveloper.AddSkillXp(skill, rawXp)`. Multiplies by `learningRate` before adding to skill level and checking for character level up (`CheckLevelUp()`).
- **Mathematical Calculation Layer (`CharacterDevelopmentModel`)**:
  - Stateless game model registered in `CampaignGameStarter`.
  - Calculates `CalculateLearningLimit`, `CalculateLearningRate`, `GetXpRequiredForLevel`, and points awarded per level.

## 2. Learning Limit & Learning Rate Formulas
- **Learning Limit (Soft Cap)**:
  $$\text{LearningLimit} = (\text{AttributeValue} - 1) \times 10 + (\text{FocusPoints} \times 30)$$
- **Learning Rate Multiplier**:
  - If $\text{SkillValue} \le \text{LearningLimit}$, maximum learning speed multiplier applies.
  - If $\text{SkillValue} > \text{LearningLimit}$, steep decay occurs. Once skill exceeds limit by ~30–50 points, learning rate drops to near $0.00$.
- **Native Leveling Constants**:
  - Focus points gained per level: 1 point (`FocusPointsPerLevel`).
  - Attribute points gained: 1 point every 4 levels (`LevelsPerAttributePoint`).

## 3. Perks & Roles (`PerkObject` & `SkillEffect.PerkRole`)
- Perks are singletons defined in `DefaultPerks` with primary and secondary effects.
- **`SkillEffect.PerkRole` Scopes**:
  - `Personal`: Buffs individual hero stats (health, swing speed, armor).
  - `PartyLeader`: Buffs entire `MobileParty` (party size, speed, prisoner limit).
  - `Captain`: Buffs troops in the specific combat `Formation` led by this hero in 3D battles.
  - `Governor`: Buffs the `Town` or `Castle` where the hero is assigned governor (loyalty, security, construction).
  - `Quartermaster`, `Scout`, `Surgeon`, `Engineer`: Buffs specific logistical aspects of the mobile party.
- In 3D battles, formation troop bonuses are applied automatically if `agent.Formation.Captain != null`.

## 4. Decorator Pattern for CharacterDevelopmentModel (Zero Harmony)
- Override native formulas by wrapping the existing `CharacterDevelopmentModel`:
```csharp
public class CustomCharacterDevelopmentModel : CharacterDevelopmentModel
{
    private readonly CharacterDevelopmentModel _baseModel;
    public CustomCharacterDevelopmentModel(CharacterDevelopmentModel baseModel)
    {
        _baseModel = baseModel;
    }
    public override int LevelsPerAttributePoint => 3; // Custom frequency
    public override float CalculateLearningRate(int attr, int focus, int skill, int level, TextObject name, StatExplainer explainer = null)
    {
        float rate = _baseModel?.CalculateLearningRate(attr, focus, skill, level, name, explainer) ?? 1f;
        return MathF.Max(rate, 0.05f); // Enforce learning floor
    }
}
```
- Register via `campaignStarter.AddModel(new CustomCharacterDevelopmentModel(existingModel))`.

## 5. Performance & Save System Rules
- **Allocation-Conscious Hot Path**: `CalculateLearningRate` is queried frequently; avoid LINQ and unnecessary heap collections in this path, and guard `TextObject` explanations behind `if (explainer != null)`. Claim zero allocations only when a measurement covers the complete synchronous call path.
- **Anti-Shadowing Constraint (`GEMINI.md`)**: Never name a folder, namespace, or class `Campaign`. Use `CharacterProgression` or `CampaignBehaviors`.
