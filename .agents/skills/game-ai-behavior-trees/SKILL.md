---
name: game-ai-behavior-trees
description: Custom AI behavior tree architecture for Mount & Blade II: Bannerlord agents, formations, and tactical doctrines. Composite nodes (Selectors, Sequences), blackboard pattern, AgentComponent lifecycle, and detour-free integration without Harmony conflicts.
risk: safe
source: Calradia Forge Agent Ecosystem (Apache 2.0)
date_added: 2026-09-28
---

# Game AI Behavior Trees: Tactical Agent & Formation Decision Architectures

Combat artificial intelligence in Mount & Blade II: Bannerlord operates at dual scales: macro tactical formations (shield walls, wedge cavalry charges, skirmish skirmishing) and micro individual combatant actions (blocking, parrying, lunging, seeking cover). Monolithic `if-else` cascades quickly become unmaintainable and brittle. A master AI architect builds modular **Behavior Trees** decoupled through a shared **Blackboard**, integrated cleanly via `AgentComponent` and `MissionLogic` without external Harmony detours.

---

## 1. Core Principles

1. **Modular Hierarchical Decisions**:
   - Split tactical decisions into composable node types:
     - **Selectors (Fallback)**: Execute children until one succeeds (e.g. *Survival Tactics*: Retreat if routed $\rightarrow$ Seek shield cover if under fire $\rightarrow$ Engage enemy).
     - **Sequences**: Execute children until one fails (e.g. *Flank Attack*: Identify exposed flank $\rightarrow$ Move into position $\rightarrow$ Charge rear).
     - **Decorators / Inverters**: Modify child node results (e.g. *Cooldown Timer*, *Repeat Until Dead*, *Invert*).
     - **Action & Condition Leaves**: Query engine state or trigger agent animations/orders.
2. **Blackboard Pattern for Decoupled Perception**:
   - Never let 500 individual agents perform identical, expensive world raycasts or proximity scans.
   - Use a shared `MissionBlackboard` updated periodically by `MissionLogic`. Individual trees read precomputed facts (e.g. *dominant threat direction*, *cavalry charge detected*, *arrow density*).
3. **Detour-Free Native Integration**:
   - Do not hook deep native C++ combat functions with intrusive Harmony prefix/postfix patches.
   - Attach a custom `AgentComponent` (e.g. `ForgeTacticalAgentComponent`) upon `Mission.OnAgentCreated` or register a custom `MissionLogic` observing formation orders.
4. **Staggered & Throttled Evaluation**:
   - Re-evaluating behavior trees for 800 combatants every 16 ms causes massive framerate collapse.
   - Throttle tree ticks to 100–250 ms intervals with randomized phase offsets: `_nextTick = currentTime + 0.15f + (agent.Index % 10) * 0.01f`.

---

## 2. Capabilities & Scope

### Capabilities
- `behavior-tree-construction`: Builds composable trees using Selector, Sequence, and Condition nodes.
- `blackboard-state-management`: Shares environmental queries across agents without redundant calculations.
- `formation-doctrine-evaluation`: Translates general battle orders into individual agent tactical responses.
- `agent-component-wiring`: Hooks custom AI logic into TaleWorlds agents natively.
- `zero-detour-safety`: Avoids patch conflicts and guarantees mod stability across TaleWorlds engine updates.

### Scope
- **In Scope**: In-mission battle scenes (`MissionLogic`, `AgentComponent`), custom tournament AI, siege defense behavior.
- **Out of Scope**: Overworld campaign movement (delegate to `discrete-event-simulation` and `bannerlord-campaign-behavior`).

---

## 3. Concrete Behavior Tree Patterns

### Pattern 1: Node State & Tree Hierarchy
Define lightweight, zero-allocation node contracts.

```csharp
public enum NodeState { Success, Failure, Running }

public interface IBehaviorNode
{
    NodeState Evaluate(Agent agent, MissionBlackboard blackboard);
}

// Composite: Selector (Fallback)
public class SelectorNode : IBehaviorNode
{
    private readonly IReadOnlyList<IBehaviorNode> _children;

    public SelectorNode(IReadOnlyList<IBehaviorNode> children) => _children = children;

    public NodeState Evaluate(Agent agent, MissionBlackboard blackboard)
    {
        int count = _children.Count;
        for (int i = 0; i < count; i++)
        {
            var state = _children[i].Evaluate(agent, blackboard);
            if (state == NodeState.Success || state == NodeState.Running)
                return state;
        }
        return NodeState.Failure;
    }
}

// Composite: Sequence
public class SequenceNode : IBehaviorNode
{
    private readonly IReadOnlyList<IBehaviorNode> _children;

    public SequenceNode(IReadOnlyList<IBehaviorNode> children) => _children = children;

    public NodeState Evaluate(Agent agent, MissionBlackboard blackboard)
    {
        int count = _children.Count;
        for (int i = 0; i < count; i++)
        {
            var state = _children[i].Evaluate(agent, blackboard);
            if (state == NodeState.Failure || state == NodeState.Running)
                return state;
        }
        return NodeState.Success;
    }
}
```

### Pattern 2: Shared Mission Blackboard
Precalculate macro battle conditions to feed individual agent trees.

```csharp
public class MissionBlackboard
{
    public Vec3 ArrowDangerZoneCenter { get; set; }
    public float ArrowThreatIntensity { get; set; }
    public bool EnemyCavalryCharging { get; set; }
    public Vec3 EnemyCavalryApproachVector { get; set; }

    public void UpdatePerception(Mission mission)
    {
        // Executed once per 500 ms by MissionLogic
        // Aggregates missile tracks and enemy formation speeds
    }
}
```

### Pattern 3: Throttled AgentComponent Lifecycle
Attach behavior trees to agents without frame drops.

```csharp
public class TacticalBehaviorAgentComponent : AgentComponent
{
    private readonly IBehaviorNode _rootNode;
    private readonly MissionBlackboard _blackboard;
    private float _timeUntilNextEvaluation;

    public TacticalBehaviorAgentComponent(Agent agent, IBehaviorNode rootNode, MissionBlackboard blackboard) 
        : base(agent)
    {
        _rootNode = rootNode;
        _blackboard = blackboard;
        // Phase-offset initial tick based on agent index
        _timeUntilNextEvaluation = 0.1f + (agent.Index % 10) * 0.015f;
    }

    public override void OnTickAsAI(float dt)
    {
        _timeUntilNextEvaluation -= dt;
        if (_timeUntilNextEvaluation <= 0f)
        {
            // Reset evaluation interval (150 ms)
            _timeUntilNextEvaluation = 0.15f;

            if (Agent.IsActive() && Agent.IsHuman)
            {
                _rootNode.Evaluate(Agent, _blackboard);
            }
        }
    }
}
```

---

## 4. Sharp Edges & Anti-Patterns

### Edge 1: Full Behavior Tree Tick Every Frame for All Agents
- **Severity**: CRITICAL
- **Symptom**: Battle FPS collapses below 15 FPS when army size exceeds 300 soldiers.
- **Root Cause**: Evaluating 500 deep behavior trees at 60 FPS = 30,000 node evaluations per second, bottlenecking the main CPU core.
- **Fix**: Stagger tree evaluations with a 100–200 ms interval and randomized phase offsets per agent.

### Edge 2: Retaining Disposed Agent References in Blackboards
- **Severity**: HIGH
- **Symptom**: `NullReferenceException` in native engine code or target tracking after an agent dies or leaves the battlefield.
- **Root Cause**: Blackboards holding raw `Agent` references after `OnAgentRemoved` has executed.
- **Fix**: Check `agent.IsActive()` before every interaction, and subscribe to `Mission.OnAgentRemoved` to clear stale target references immediately.

### Edge 3: Mutating Bannerlord Formation Orders from Multiple Children Concurrently
- **Severity**: MEDIUM
- **Symptom**: Formations freeze, spin uncontrollably, or rapidly oscillate between charge and hold orders.
- **Root Cause**: Conflicting leaf actions issuing `formation.SetMovementOrder()` on the same frame.
- **Fix**: Only macro-level leader agents or dedicated formation behavior trees should issue `MovementOrder` mutations; line combatants should only alter personal stances.

---

## 5. Validation Rules & Verification Checklist

1. [ ] **Throttled Execution**: Confirmed that `OnTickAsAI` enforces an evaluation cooldown ($\ge 100\text{ ms}$).
2. [ ] **Zero Detour / No Harmony**: Implemented strictly via `AgentComponent` and `MissionLogic` native inheritance.
3. [ ] **Active Agent Verification**: All leaf actions check `agent.IsActive() && agent.IsHuman` before invoking actions.
4. [ ] **Blackboard Hygiene**: Disposed agents and destroyed entities are promptly cleared from blackboard registries.
5. [ ] **Zero Allocations in Node Evaluation**: Node evaluation loops use indexed iteration without LINQ.
