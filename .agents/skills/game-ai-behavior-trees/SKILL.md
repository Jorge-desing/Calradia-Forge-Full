---
name: game-ai-behavior-trees
description: Proposal and review guidance for behavior-tree designs in Mount & Blade II Bannerlord. The repository currently provides a combat component scaffold, not a validated behavior-tree runtime; verify engine references, lifecycle, and measured tactical cases before implementation claims.
metadata:
  risk: safe
  source: Calradia Forge Agent Ecosystem (Apache 2.0)
  date_added: "2026-09-28"
---

# Game AI Behavior Trees: Tactical Agent & Formation Decision Architectures

Behavior trees are one possible way to organize tactical decisions, not a capability currently implemented by Calradia Forge. The repository has `ForgeNoviceHub.GenerateCombatAiComponentScaffold`, which emits an `AgentComponent`/`MissionLogic` example; it does not provide behavior-tree nodes, a blackboard runtime, or a validated tree integration. Treat every tree and lifecycle example in this skill as proposal/pseudocode. Before turning one into code, inspect the local TaleWorlds references for the target game version, verify the callback and component lifecycle, compile against the repository's `net472` targets, and validate a reproducible in-game case. A native extension point or absence of a Harmony dependency does not prove compatibility with other mods.

---

## 1. Core Principles

1. **Modular Hierarchical Decisions**:
   - Split tactical decisions into composable node types:
     - **Selectors (Fallback)**: Execute children until one succeeds (e.g. *Survival Tactics*: Retreat if routed $\rightarrow$ Seek shield cover if under fire $\rightarrow$ Engage enemy).
     - **Sequences**: Execute children until one fails (e.g. *Flank Attack*: Identify exposed flank $\rightarrow$ Move into position $\rightarrow$ Charge rear).
     - **Decorators / Inverters**: Modify child node results (e.g. *Cooldown Timer*, *Repeat Until Dead*, *Invert*).
     - **Action & Condition Leaves**: Model queries or actions; verify the actual engine API and permissions before wiring any leaf to game state or orders.
2. **Blackboard Pattern for Decoupled Perception**:
   - If multiple consumers need the same derived observations, compare a shared per-mission snapshot with direct queries. Measure the full scan, refresh cost, and data staleness; sharing is not automatically faster.
   - A proposed `MissionBlackboard` can hold derived facts (for example, threat direction), but its schema, update callback, and entity ownership must be designed against verified engine contracts.
3. **Use Verified Native Extension Points**:
   - Do not add a Harmony dependency or assume a prefix/postfix patch is necessary for custom behavior.
   - `AgentComponent` and `MissionLogic` appear in the repository's generated combat scaffold, but that is not proof that a behavior tree can be attached or that any illustrative callback signature is valid in every game version. Check local references and run an authorized runtime scenario before claiming integration.
4. **Choose Cadence from Measurements**:
   - Measure node work, candidate update frequency, number of agents, responsiveness, and observation freshness in a reproducible tactical case before choosing an interval.
   - A stable-ID phase offset is deterministic scheduling, not randomness, and can spread work only if the actual ID distribution and scheduler are suitable. It does not lower total work or guarantee even buckets; validate distribution and measure the complete callback.

---

## 2. Capabilities & Scope

### Capabilities
- `behavior-tree-design`: Sketches Selector, Sequence, and Condition node proposals; these are not implemented SDK types.
- `blackboard-design-review`: Evaluates shared observations, freshness, lifecycle, and measured work before recommending a design.
- `formation-doctrine-design`: Models how orders could influence agent decisions; verify behavior with a concrete scenario.
- `agent-component-scaffold-review`: Reviews the generated scaffold against local TaleWorlds references; does not claim runtime integration.
- `native-integration-review`: Checks candidate native extension points without adding a Harmony dependency or promising cross-mod compatibility.

### Scope
- **In Scope**: Proposal and review for in-mission battle scenes (`MissionLogic`, `AgentComponent`), tournament AI, and siege defense behavior. Implementation requires local engine-reference checks and a measured, reproducible use case.
- **Out of Scope**: Overworld campaign movement (delegate to `discrete-event-simulation` and `bannerlord-campaign-behavior`).

---

## 3. Proposed Behavior Tree Patterns (Pseudocode)

The following examples are design sketches, not compile-ready code, Forge SDK APIs, or verified Bannerlord integrations. Names such as `IBehaviorNode`, `MissionBlackboard`, and `TacticalBehaviorAgentComponent` are illustrative. Confirm every TaleWorlds type/member against the local references for the exact game version.

### Pattern 1: Node State & Tree Hierarchy
Use simple contracts as a starting design. Measure allocations across the complete evaluation path, including callbacks, before setting a budget or claiming zero allocation.

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

### Pattern 2: Proposed Shared Mission Blackboard
Consider precomputing a fact only when measurements show that reuse is worthwhile. Choose refresh cadence from the scenario's freshness requirements and measured cost; no fixed interval is assumed here.

```csharp
public class MissionBlackboard
{
    public Vec3 ArrowDangerZoneCenter { get; set; }
    public float ArrowThreatIntensity { get; set; }
    public bool EnemyCavalryCharging { get; set; }
    public Vec3 EnemyCavalryApproachVector { get; set; }

    public void UpdatePerception(Mission mission)
    {
        // Pseudocode only: choose a verified lifecycle callback and a
        // measured refresh policy for the target game version.
        // Any aggregation still has a scan and maintenance cost.
    }
}
```

### Pattern 3: Candidate AgentComponent Lifecycle (Pseudocode)
This is a conceptual integration shape, not proof that the shown callback or attachment path works in the current engine. Verify references and runtime behavior before adapting it.

```csharp
public class TacticalBehaviorAgentComponent : AgentComponent
{
    private readonly IBehaviorNode _rootNode;
    private readonly MissionBlackboard _blackboard;
    private float _timeUntilNextEvaluation;
    private readonly float _evaluationInterval; // selected from measured case requirements

    public TacticalBehaviorAgentComponent(Agent agent, IBehaviorNode rootNode, MissionBlackboard blackboard) 
        : base(agent)
    {
        _rootNode = rootNode;
        _blackboard = blackboard;
        // If staggering is justified, derive an optional phase from a
        // verified stable identifier and validate its distribution.
        // This is deterministic scheduling, not a random offset.
        _timeUntilNextEvaluation = 0f;
    }

    public override void OnTickAsAI(float dt)
    {
        _timeUntilNextEvaluation -= dt;
        if (_timeUntilNextEvaluation <= 0f)
        {
            _timeUntilNextEvaluation = _evaluationInterval;

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

### Edge 1: Per-Frame Work Without a Measured Budget
- **Severity**: CRITICAL
- **Symptom**: A candidate AI update consumes an increasing share of the mission callback as agents or node work grow.
- **Root Cause**: Running work more often or for more agents increases total calls; the actual cost depends on the tree, engine state, and target hardware.
- **Fix**: Profile a reproducible mission and measure call counts, callback duration, and response freshness. Reduce or stagger work only if the behavior tolerates it; select cadence from evidence. A deterministic phase offset does not reduce total work and is not random.

### Edge 2: Retaining Entities Beyond Their Verified Lifecycle
- **Severity**: HIGH
- **Symptom**: A proposed shared state retains or uses an entity after its lifecycle ends.
- **Root Cause**: Keeping engine objects in long-lived state without a verified ownership and cleanup policy.
- **Fix**: Prefer stable identifiers where appropriate, check entity validity at use sites, and clear references through a lifecycle callback verified in the local engine references.

### Edge 3: Mutating Bannerlord Formation Orders from Multiple Children Concurrently
- **Severity**: MEDIUM
- **Symptom**: Multiple decision paths issue contradictory mutations to a shared formation or order.
- **Root Cause**: No defined owner or arbitration policy for shared state changes.
- **Fix**: Route mutations through a single verified owner or explicit arbitration policy, and validate the result in a reproducible battle scenario.

---

## 5. Validation Rules & Verification Checklist

1. [ ] **Implementation Status**: Distinguish the repository's generated combat scaffold from a behavior-tree runtime; do not describe the latter as implemented.
2. [ ] **Engine Contract**: Confirm the exact types, members, callbacks, and threading rules in local TaleWorlds references for the target version.
3. [ ] **Measured Case**: Define a repeatable tactical scenario and measure behavior, callback cost, and data freshness before choosing cadence or claiming improvement.
4. [ ] **Entity Lifecycle**: Verify how agents and other entities are created, removed, and invalidated; exercise cleanup in the actual integration.
5. [ ] **Measured Allocations**: Measure the complete evaluation path, including callbacks; indexed iteration without LINQ does not itself prove zero allocations.
6. [ ] **Interop Evidence**: No Harmony dependency is an architectural property, not evidence of coexistence or conflict-free behavior with other mods.

## Current repository evidence (2026-10-02)

In the working-tree snapshot for the module manifest version `v25.2.0`,
`src/CalradiaForge.Sdk/ForgeNoviceHub.cs` exposes
`GenerateCombatAiComponentScaffold`. It emits example `AgentComponent` and
`MissionLogic` source for a battle-shout concept. The generated morale-recovery
and deferred setup sections are comments/placeholders; this generator does not
provide behavior-tree nodes, a blackboard runtime, or a validated tactical AI
integration. A search of maintained C# sources and project files under `src/`,
`examples/`, `modules/` and `tests/` found no `IBehaviorNode`, selector/sequence node implementation,
`MissionBlackboard`, or `TacticalBehaviorAgentComponent` runtime.

`tests/CalradiaForge.Tests/AdvancedToolsTests.cs` checks that the generated text
contains the component/lifecycle pattern and exercises identifier validation
and sound-string escaping. Those assertions do not compile a consumer mod from
the emitted text or prove behavior in a mission. The separate
`ClanCharacterProgressionBehavior` is a campaign-event behavior, not a tactical
behavior-tree runtime. Keep tree examples here labeled as proposals until a
consumer build and reproducible in-game battle validation exist.
