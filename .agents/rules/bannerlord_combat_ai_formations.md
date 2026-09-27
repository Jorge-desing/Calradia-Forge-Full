# Bannerlord Combat AI, Formations, and Combat Behaviors

When developing or modifying combat agents, formation tactics, battle casualties, and combat animations in Mount & Blade II: Bannerlord without external injection frameworks:

## 1. Agent Lifecycle & State Management
- **Agent Component Architecture**: Attach custom agent logic by extending `TaleWorlds.MountAndBlade.AgentComponent`.
  - `OnTickAsAI()`: Ticks only for AI-controlled agents (`agent.IsPlayerControlled == false`).
  - **Player Tick Bridge**: For player-controlled agents, invoke custom tick logic manually from a companion `MissionBehavior.OnMissionTick(dt)` to ensure parity.
- **Mortality & Health**: Modifying `agent.Health` directly bypasses armor calculations, combat logs, and XP. Prefer `Mission.RegisterBlow` or `Agent.Die`. Subscribe to health events via `agent.OnAgentHealthChanged`.
- **Morale & Routing**: Modifying morale is done via `agent.ChangeMorale(float delta)` (clamped `[0..100]`). Below zero triggers `agent.OnFleeing()` and routes towards `Mission.GetClosestFleePositionForAgent()`.
- **Team Swapping & Mount Synchronization**:
  - Calling `agent.SetTeam(targetTeam, true)` detaches the agent from their current formation (`agent.Formation = null`). Always reassign `agent.Formation = targetTeam.GetFormation(agent.FormationClass)`.
  - **CRITICAL INVARIANT**: Calling `agent.SetTeam()` on a mounted rider does NOT automatically transfer their horse. You MUST explicitly call `agent.MountAgent.SetTeam(targetTeam, true)`. Otherwise, friendly units will target the horse as an enemy.

## 2. Formation Orders Architecture
A `Formation` is governed by 7 orthogonal order facets:
- **`MovementOrder`**: `Charge`, `Move(WorldPosition)`, `Follow(Agent)`, `Advance`, `FallBack`, `Stop`, `Retreat`.
- **`FacingOrder`**: `LookAtEnemy`, `LookAtDirection(Vec2)`.
- **`ArrangementOrder`**: `Line`, `ShieldWall`, `Loose`, `Circle`, `Square`, `Skein`, `Column`, `Scatter`.
- **`FiringOrder`**: `FireAtWill`, `HoldYourFire`.
- **`RidingOrder`**: `Mount`, `Dismount`.
- **`FormOrder`**: `Wide`, `Deep`, `Custom(float width)`.
- **`WeaponUsageOrder`**: Controls melee vs. ranged stance.
- Orders can be set directly via properties on `formation` (AI scripts) or dispatched via `team.PlayerOrderController` (UI orders).

## 3. Casualty Interception Pipeline
- **`OnEarlyAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow)`**: Fired immediately upon fatal impact. The agent is STILL attached to their formation and equipment is intact. Inspect casualties and formation command states here.
- **`OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow)`**: Fired after detachment. `affectedAgent.Formation` is NULL. Use for memory cleanup and roster bookkeeping only.

## 4. Animation Blending & Action Channels
- **Channel 0 (Base Locomotion)**: Drives legs and translations (walk, run, crouch, horse riding).
- **Channel 1 (Action Overlay)**: Drives upper body gestures (attacks, blocks, cheers, reloads).
- **Frozen Legs Pitfall**: Firing victory cheers or taunts on Channel 0 freezes lower-body locomotion, causing agents to slide across terrain. Always play emotes and upper-body gestures on Channel 1.
- **`ActionIndexCache` Caching**: Always store animations in `static readonly ActionIndexCache` fields. String hashing inside frame loops causes GC spikes; invalid action strings return `-1` (`act_none`) and crash the blend tree if played on Channel 0.

## 5. Performance at Scale (1000+ Battles)
- **Never iterate `Mission.Agents` every frame**: In large battles (500v500), linear iteration over thousands of agents causes CPU stalls. Partition agents into round-robin buckets (e.g., 4 buckets ticked every 4 frames).
- **No Physics Raycasts in Combat Loops**: Never call `RayCastForClosestEntityOrTerrain` inside per-agent tick loops. Use `(posA - posB).LengthSquared` for proximity checks.
- **Post-Battle Cheer Crash**: `AgentVictoryLogic` forces living agents to cheer at battle's end. Any spawned agent missing a valid `Formation` and `Team` will cause a native C++ null-pointer exception.

## 6. Anti-Shadowing Namespace Constraint (`GEMINI.md`)
- Never name a folder, sub-namespace, or class `Campaign`. Use `CombatAI`, `FormationTactics`, or `CampaignBehaviors`.
