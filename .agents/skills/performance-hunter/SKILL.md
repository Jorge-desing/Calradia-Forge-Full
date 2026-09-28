---
name: performance-hunter
description: High-performance .NET 4.7.2 and .NET 8 optimization for Mount & Blade II: Bannerlord and Calradia Forge. Zero-allocation simulation ticks, anti-lag modulo-24 time slicing, SIMD-friendly loops, struct enumerators, and GC churn elimination in 1000-agent battles.
risk: safe
source: Calradia Forge Agent Ecosystem (Apache 2.0)
date_added: 2026-09-28
---

# Performance Hunter: Zero-Allocation & Anti-Lag Engine Optimization

In Mount & Blade II: Bannerlord, simulation tick loops execute continuously across thousands of dynamic agents and world entities. Generating even small allocations during high-frequency ticks triggers Gen0 Garbage Collection spikes, producing noticeable stutter and frame drops during intense 1,000-agent battles. A master performance hunter enforces **Zero GC Allocations** on hot paths and distributes simulation workloads gracefully.

---

## 1. Core Principles

1. **Simulation Ticks Are Sacred**:
   - Every allocation inside `OnMissionTick(dt)`, `HourlyTick()`, or Gauntlet UI render passes is technical debt.
   - Target: **0 bytes allocated per tick** on the managed heap.
2. **Ban LINQ in Hot Paths**:
   - Calls to `.Where()`, `.Select()`, `.Any()`, `.ToList()`, or `.Count()` allocate delegate instances (`Func<T, bool>`), iterator state machines, and closure display classes.
   - Replace with index-based `for (int i = 0; i < count; i++)` loops using direct array or `List<T>` indexed access.
3. **Anti-Lag Time-Slicing (Modulo-24)**:
   - In a campaign world with 2,000+ heroes and 400+ settlements, never iterate all entities in a single hourly tick.
   - Sift entities across 24 hours: `hero.Id.GetHashCode() % 24 == currentHour`, smoothing CPU latency across the entire day.
4. **Pre-Allocation & Capacity Hinting**:
   - Dynamic collection re-hashing (`Dictionary`, `HashSet`, `List`) doubles capacity and discards old arrays. Always initialize collections with known or estimated capacities: `new List<Agent>(128)`.
5. **Struct Enumeration & Non-Boxing Enumerators**:
   - `foreach` over `IEnumerable<T>` boxes the enumerator as an `IDisposable` object.
   - Use concrete `List<T>` (which uses struct `List<T>.Enumerator`) or direct `for` indexing.

---

## 2. Capabilities & Scope

### Capabilities
- `gc-churn-elimination`: Identifies and replaces heap allocations in mission and campaign tick loops.
- `anti-lag-slicing`: Implements hash-based temporal distribution across simulation cycles.
- `cache-locality-optimization`: Arranges entity data for contiguous memory access and CPU cache line efficiency.
- `string-memory-chunking`: Replaces string concatenation with pooled `StringBuilder` and chunked buffers.
- `apm-latency-benchmarking`: Measures tick execution budgets against target frame thresholds (e.g. < 2.0 ms per tick).

### Scope
- **In Scope**: `src/CalradiaForge.Mod` in-game game loop, `src/CalradiaForge.Core` behavioral engines, `src/CalradiaForge.Desktop` WPF render passes.
- **Out of Scope**: Static architectural rule checks (delegate to `debugging-master` and `calradia-forge-gemini-conventions`).

---

## 3. Concrete High-Performance Patterns

### Pattern 1: Zero-Allocation Mission Agent Traversal
Avoid LINQ when evaluating nearby combatants or formation targets.

```csharp
// BAD: Allocates delegate, closure, and iterator
var enemies = Mission.Agents.Where(a => a.IsEnemyOf(agent) && a.IsActive()).ToList();

// GOOD: Zero heap allocation, cache-friendly array iteration
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

### Pattern 2: Modulo-24 Anti-Lag Time-Slicing
Distribute periodic behavioral updates across the 24 campaign hours evenly.

```csharp
// CORRECT: Modulo-24 anti-lag distribution
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
            // Distribute heroes across 24 hourly buckets
            if (Math.Abs(hero.Id.GetHashCode()) % 24 == currentHour)
            {
                UpdateHeroDynasticProgression(hero);
            }
        }
    }

    private void UpdateHeroDynasticProgression(Hero hero)
    {
        // Lightweight processing for 1/24th of the world population
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

## 4. Sharp Edges & Anti-Patterns

### Edge 1: Using LINQ in `OnMissionTick` or `HourlyTick`
- **Severity**: HIGH
- **Symptom**: Micro-stutters every 3-5 seconds in 500+ agent battles as the .NET Framework 4.7.2 Gen0 GC sweeps the nursery.
- **Root Cause**: LINQ lambda capture allocates closures (`<>c__DisplayClass`) and enumerators on the managed heap.
- **Fix**: Replace all LINQ queries in tick paths with standard index-based `for` loops.

### Edge 2: String Interpolation in High-Frequency HUD Calls
- **Severity**: HIGH
- **Symptom**: Hundreds of megabytes of short-lived strings allocated per minute, driving excessive GC collections.
- **Root Cause**: `$"Speed: {speed:0.0} m/s"` allocates a new string every frame, even if `speed` has not changed.
- **Fix**: Cache previous values, update UI text only upon value change (`PropertyChanged`), or use integer lookup tables.

### Edge 3: Dynamic Dictionary Re-Hashing
- **Severity**: MEDIUM
- **Symptom**: Unpredictable latency spikes (10-25 ms) when inserting entries into tracking maps.
- **Root Cause**: `new Dictionary<string, object>()` starts with capacity 0 or 3. Adding 500 items triggers 9 re-allocation cycles and rehashing of all existing keys.
- **Fix**: Always specify capacity upfront: `new Dictionary<string, object>(expectedCapacity)`.

---

## 5. Validation Rules & Benchmark Verification

1. [ ] **Zero LINQ in Mod Ticks**: Verified that no LINQ namespaces (`System.Linq`) are imported or used in `src/CalradiaForge.Mod` tick loops.
2. [ ] **Modulo-24 Time-Slicing**: Campaign loops updating all heroes verify `Math.Abs(entity.Id.GetHashCode()) % 24 == currentHour`.
3. [ ] **Pre-Sized Collections**: All intermediate lists and dictionaries specify capacity in constructor calls.
4. [ ] **Bounded Ring Buffers**: In-memory telemetry collections limit retention to 2,048 items max.
5. [ ] **Single-Thread Dispatch**: Simulation calls from background threads are marshaled through `GameThreadActionDispatch`.
