---
name: performance-hunter
description: Measured .NET 4.7.2 and .NET 8 optimization for Mount & Blade II Bannerlord and Calradia Forge. Simulation ticks, stable time slicing, traversal costs and allocation budgets verified through reproducible benchmarks.
metadata:
  risk: safe
  source: Calradia Forge Agent Ecosystem (Apache 2.0)
  date_added: "2026-09-28"
---

# Performance Hunter: Measured Simulation Performance

Simulation ticks can process many agents and entities. Repeated allocations can increase GC pressure, but source syntax alone does not establish a pause or a frame-time regression. Measure duration and allocations across the complete callback, then reduce demonstrated costs within a documented budget.

---

## 1. Core Principles

1. **Simulation Ticks Are Sacred**:
   - Record a budget for the actual mission, hourly or UI callback. They do not share one universal allocation budget.
   - Claim **0 bytes allocated** only when a suitable measurement covers the complete synchronous path.
2. **Measure LINQ and Iteration Costs**:
   - LINQ pipelines and materialization can add allocations or work, but cost depends on the operator, source and runtime. Capturing lambdas may allocate closures; not every `Any` or `Count` call allocates.
   - Compare a direct loop with the existing implementation under the same representative workload before changing code. Simpler iteration is not automatically faster or allocation-free.
3. **Stable Time-Slicing for Deferrable Work**:
   - Defer only work whose behavior permits the changed cadence, using `ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour)`.
   - Supply a stable, nonempty entity ID: null and empty IDs map to bucket zero, concentrating those entities in that bucket.
   - The helper assigns deterministic buckets; their sizes depend on entity IDs and are not guaranteed to be uniform. A filter inside a full collection loop still traverses every entity. Measure traversal separately from selected processing.
4. **Measured Capacity Hinting**:
   - Growing collections may allocate new storage and dictionaries or sets may rehash; growth behavior depends on the collection type and runtime.
   - Set an initial capacity when a workload bound or representative estimate is known and measurements show growth cost. Avoid arbitrary large reservations.
5. **Struct Enumeration & Non-Boxing Enumerators**:
   - Interface enumeration can box a value-type enumerator; other enumerators already have reference types. Measure the actual collection and enumeration path.
   - Use concrete `List<T>` (which uses struct `List<T>.Enumerator`) or direct `for` indexing.

---

## 2. Capabilities & Scope

### Capabilities
- `gc-churn-elimination`: Identifies and replaces heap allocations in mission and campaign tick loops.
- `stable-id-time-buckets`: Selects eligible, deferrable work by deterministic ID bucket; it does not reduce a full collection scan or guarantee balanced work.
- `cache-locality-optimization`: Arranges entity data for contiguous memory access and CPU cache line efficiency.
- `string-memory-chunking`: Replaces string concatenation with pooled `StringBuilder` and chunked buffers.
- `apm-latency-benchmarking`: Measures tick execution against a workload-specific budget established for the actual callback and host; there is no universal per-tick threshold.

### Scope
- **In Scope**: `src/CalradiaForge.Mod` in-game game loop, `src/CalradiaForge.Core` behavioral engines, `src/CalradiaForge.Desktop` WPF render passes.
- **Out of Scope**: Static architectural rule checks (delegate to `debugging-master` and `calradia-forge-gemini-conventions`).

---

## 3. Concrete High-Performance Patterns

### Pattern 1: Mission Agent Traversal
Direct iteration can avoid a LINQ pipeline when evaluating nearby combatants or formation targets. Measure the complete path, including engine calls, before making allocation or latency claims.

```csharp
// Candidate to profile: this captured LINQ pipeline and materialization can add allocations and work.
var enemies = Mission.Agents.Where(a => a.IsEnemyOf(agent) && a.IsActive()).ToList();

// Direct iteration avoids this LINQ pipeline; measure engine calls separately.
public static Agent FindNearestEnemy(Agent sourceAgent, float maxDistanceSquared)
{
    Agent nearest = null;
    float bestDistanceSq = maxDistanceSquared;
    var agents = Mission.Current.Agents;
    int count = agents.Count;

    for (int i = 0; i < count; i++)
    {
        Agent candidate = agents[i];
        if (!candidate.IsActive() || !candidate.IsHuman)
            continue;

        if (candidate.Team != sourceAgent.Team && candidate.Team.IsEnemyOf(sourceAgent.Team))
        {
            float distSq = candidate.Position.DistanceSquared(sourceAgent.Position);
            if (distSq < bestDistanceSq)
            {
                bestDistanceSq = distSq;
                nearest = candidate;
            }
        }
    }

    return nearest;
}
```

### Pattern 2: Stable Time-Slicing for Deferrable Campaign Work
Use stable time-slicing only when the feature can tolerate waiting until an entity's assigned hour. The deterministic bucket function does not guarantee that exactly one twenty-fourth of entities will be processed per hour.

```csharp
// Example: deterministic selection for work that is safe to defer
public class OptimizedClanProgressionBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
    }

    public override void SyncData(IDataStore dataStore)
    {
        // 100% Stateless: Rule B compliant
    }

    private void OnHourlyTick()
    {
        if (Campaign.Current == null) return;

        int currentHour = (int)(CampaignTime.Now.ToHours % 24);
        var aliveHeroes = Hero.AllAliveHeroes;
        int count = aliveHeroes.Count;

        for (int i = 0; i < count; i++)
        {
            Hero hero = aliveHeroes[i];
            // Stable IDs select a deterministic bucket; bucket populations can vary.
            if (CalradiaForge.Sdk.ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour))
            {
                UpdateHeroDynasticProgression(hero);
            }
        }
    }

    private void UpdateHeroDynasticProgression(Hero hero)
    {
        // The selected share depends on the ID distribution; it is not guaranteed to be 1/24.
    }
}
```

### Pattern 3: Pre-Allocated String Reassembly without Resizing
When formatting or reassembling strings (e.g. in `ForgeSaveChunker`), precompute the required length to avoid dynamic array resizing.

```csharp
// CORRECT: Pre-allocated string builder capacity
public static string ReassembleChunks(IReadOnlyList<string> chunks)
{
    if (chunks == null || chunks.Count == 0) return string.Empty;
    if (chunks.Count == 1) return chunks[0];

    // 1. Calculate total length in advance
    int totalLength = 0;
    int count = chunks.Count;
    for (int i = 0; i < count; i++)
    {
        if (chunks[i] != null) totalLength += chunks[i].Length;
    }

    // 2. Allocate exact buffer once
    var sb = new StringBuilder(totalLength);
    for (int i = 0; i < count; i++)
    {
        if (chunks[i] != null) sb.Append(chunks[i]);
    }

    return sb.ToString();
}
```

---

## 4. Cost Hypotheses to Verify

The following are profiling candidates, not established symptoms or universal root causes. Record the game/runtime version, collection size, callback frequency, elapsed time and allocations for the complete path before attributing a hitch or making a code change.

- **LINQ/materialization**: A query may create iterators, delegates, closures or a result collection depending on its operators and call site. Measure the allocation and time on the actual source collection; do not ban LINQ by syntax alone.
- **Repeated UI formatting**: Formatting a new value can create strings on each invocation. The per-second cost depends on call rate, text size and formatting path; measure it before introducing caches or lookup tables.
- **Collection growth**: Dictionaries, sets and lists may resize and rehash as they grow. Growth counts and latency depend on runtime, capacity and workload; size from a measured or known bounded workload instead of assuming fixed resize counts.

---

## 5. Validation Rules & Benchmark Verification

1. [ ] **Profiled Hot-Path Candidates**: Measure the complete callback with representative data before optimizing LINQ, iteration, formatting or collection growth; compare alternatives using the same workload and runtime.
2. [ ] **Measured Time-Slicing**: Use `ForgeTimeSlicer.ShouldProcess` only for
   deferrable work whose callback cadence revisits all relevant buckets; measure the full traversal because filtering does not remove its O(N) scan.
3. [ ] **Appropriate Capacity**: Size collections from a measured bounded workload;
   do not reserve arbitrary large buffers solely to satisfy a static checklist.
4. [ ] **Bounded Retention**: Verify each collection's actual declared bound;
   2,048 is an example, not a universal limit for every subsystem.
5. [ ] **Thread-Affinity Dispatch**: Verify each engine-facing call's thread-affinity contract and the host's actual dispatch path. `GameThreadActionDispatch.RunOrPost` is an internal helper used at specific `CalradiaForge.Mod` call sites; it does not automatically marshal every simulation call and is not a public SDK guarantee.

## Verified hook and delivery lessons

For Finalizer/ILHook boundaries, confirmation selection, serial measurement and packaging from a scoped snapshot, read [hook delivery lessons](../calradia-forge-dev-workflow/references/hook-delivery-lessons.md). Recheck current source and preserve the distinction between passing fixtures and pending live main-menu validation.

In the x64 `net472` DetourFixture, a no-argument Prefix target allocated a fresh empty argument array in the emitted adapter and cloned that zero-length array before dispatch. The same BAT benchmark measured 128.45 B/call before and 80.28 B/call after reusing `Array.Empty<object>()` and skipping an empty clone across five 25,000-call samples. The parameterized Prefix remained at 168.43 B/call in both runs. This is fixture-specific allocation evidence; it does not establish Bannerlord tick cost or justify broad hook-path claims.
