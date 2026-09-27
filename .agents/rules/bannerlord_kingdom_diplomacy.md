# Bannerlord Kingdom Decisions, Diplomacy, and Political Voting Engine

When extending kingdom policies, diplomatic declarations, and council voting in Mount & Blade II: Bannerlord:

## 1. Kingdom Decision Architecture
- **Base Class**: Inherit from `TaleWorlds.CampaignSystem.Election.KingdomDecision`.
  - `DetermineInitialCandidates()`: Yields candidate `DecisionOutcome` instances.
  - `IsAllowed()`: Returns `false` if prerequisites are missing (prevents invalid votes).
  - `DetermineSupport(Clan clan, DecisionOutcome outcome)`: Evaluates a clan's numerical preference score. Positive indicates support; negative indicates opposition.
  - `ApplyChosenOutcome(DecisionOutcome chosenOutcome)`: Applies the game state modification upon vote resolution.
- **Queuing Decisions**: Always call `kingdom.AddDecision(decision, ignoreInfluenceCost: false)`. Decisions reside in `kingdom.UnresolvedDecisions` and are resolved via `KingdomElection` during daily ticks or player interaction.

## 2. Influence Tiers & Voting Weights
Voting is partitioned into 4 discrete tiers (`Supporter.SupportWeight`):
- `None`: Cost = 0, Weight = 0
- `Slightly`: Cost = 20 Influence, Weight = 1
- `Strongly`: Cost = 60 Influence, Weight = 3
- `Fully`: Cost = 150 Influence, Weight = 8

## 3. Sovereign Overrule & Fallout
- Rulers (`Kingdom.RulingClan == clan`) can veto council majority decisions.
- Overrule cost scales with the discarded majority weight:
  $$\text{OverruleCost} = \text{BaseCost} + K \cdot \left( \frac{W(O_{\text{majority}})}{W_{\text{total}}} \right)$$
- The policy `DefaultPolicies.RoyalPrivilege` reduces overrule cost by 20%.
- Overruling incurs an immediate relation penalty ($-5$ to $-15$) with all clans that supported the discarded majority outcome.

## 4. Diplomatic Actions vs Decisions
- **Democratic Proposals**: Use `DeclareWarDecision` or `MakePeaceKingdomDecision` to let the council debate terms.
- **Direct Executive Enactment**: Use `DeclareWarAction.Apply(f1, f2)` and `MakePeaceAction.Apply(f1, f2, dailyTribute)`.
- **`StanceLink`**: Holds war/peace states, total casualties, successful raids, and `DailyTributePaid`. Managed via `FactionManager`.

## 5. Kingdom Policies (`PolicyObject`)
- Defined programmatically in C# and registered into `MBObjectManager.Instance.RegisterPresumedObject(new PolicyObject("id"))`.
- Native policies live in `TaleWorlds.CampaignSystem.DefaultPolicies`.
- Propose policy vote via `new KingdomPolicyDecision(proposerClan, policy, isInverted)`.
- Apply directly via `kingdom.AddPolicy(policy)` or `kingdom.RemovePolicy(policy)`.

## 6. Save System Safety
- All custom `KingdomDecision` and `DecisionOutcome` types must be registered in a `SaveableTypeDefiner` with Base ID $\ge 2{,}500{,}000$.
- Mark all persistent fields with `[SaveableField(id)]`.
- **Anti-Shadowing Constraint (`GEMINI.md`)**: Never name a folder, sub-namespace, or class `Campaign`. Use `DiplomacyExtensions`, `KingdomDecisions`, or `CampaignBehaviors`.
