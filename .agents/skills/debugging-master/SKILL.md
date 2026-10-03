---
name: debugging-master
description: "Scientific debugging methodology for Mount & Blade II: Bannerlord and Calradia Forge. Hypothesis-driven root-cause isolation, TaleWorlds native crash triage (0xC0000005), 10-minute rule, stack trace symbol resolution, and save state corruption diagnosis."
---

# Debugging Master: Scientific Mod & Engine Triage

Debugging Bannerlord modules is applied science, not intuitive guessing. When a battle scene crashes without a C# stack trace, or when a campaign save corrupts upon reload, random code mutations only introduce secondary defects. A master debugger forms explicit hypotheses, isolates single variables, and proves root causes empirically.

---

## 1. Core Principles

1. **Debugging is Science**:
   - `Observe` the exact failure behavior and boundary conditions.
   - `Formulate` a falsifiable hypothesis explaining the defect.
   - `Design` a minimal deterministic test or logging probe.
   - `Verify` whether the hypothesis held true; iterate systematically.
2. **The 10-Minute Rule**:
   - If ad-hoc code inspection does not reveal the root cause within 10 minutes, immediately halt ad-hoc inspection.
   - Switch to scientific instrumentation: binary search (bisecting), AST rule auditing, or structured telemetry via `ForgeLogger`.
3. **The Native C++ Engine Barrier**:
   - The TaleWorlds game engine is split between managed C# assemblies (`net472`) and unmanaged native C++ engines (`MountAndBlade.exe`, `Engine.dll`).
   - Native crashes (`0xC0000005 AccessViolationException`) typically arise from lifecycle timing violations (e.g. creating meshes or manipulating skeletons during `OnInit` instead of `OnTick`).
4. **Non-Destructive Triage**:
   - Never debug save corruption on a user's original `.sav` file.
   - Replicate the failure in a headless mock test (`CalradiaForge.Tests`) or capture the event trajectory via ForgeWeave replay.

---

## 2. Capabilities & Scope

### Capabilities
- `bannerlord-crash-triage`: Triages Forge's JSON-text `.cfcrash` reports and native unhandled exceptions; native dump decoding requires a separate debugger workflow.
- `lifecycle-debugging`: Diagnoses timing violations across `MBSubModuleBase`, `CampaignBehaviorBase`, and `MissionLogic`.
- `memory-corruption-isolation`: Identifies memory leaks, buffer overruns, and unpinned native pointers.
- `save-system-diagnostics`: Traces broken `IDataStore.SyncData()` calls, GUID truncation, and SaveableTypeDefiner ID collisions.
- `thread-concurrency-audit`: Detects background thread race conditions violating Bannerlord's single-threaded game loop.

### Scope
- **In Scope**: TaleWorlds engine managed/native boundaries, Calradia Forge behavioral systems, WPF Desktop workbench diagnostics, and Gauntlet UI render anomalies.
- **Out of Scope / Defer**: Load testing (delegate to `performance-hunter`), general multi-agent task routing (delegate to `multi-agent-orchestration`).

---

## 3. Concrete Diagnostic Patterns

### Pattern 1: Native Lifecycle Crash Guard (OnInit vs OnTick)
When creating or swapping 3D meshes, skeleton bones, or complex physics colliders, never invoke mutations inside `MissionLogic.OnInit()`. The native scene graph is not fully compiled during initialization.

```csharp
// CORRECT: Defer 3D setup to first frame tick
public class SafeMissionEquipmentLogic : MissionLogic
{
    private bool _initialized;

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        if (!_initialized)
        {
            _initialized = true;
            InitializeCustomVisuals();
        }
    }

    private void InitializeCustomVisuals()
    {
        // Safe to attach meshes, custom banners, or physics bodies here
        foreach (var agent in Mission.Agents)
        {
            if (agent.IsHuman && agent.SpawnEquipment != null)
            {
                // Engine scene is valid and stable
            }
        }
    }
}
```

### Pattern 2: Forge `.cfcrash` JSON Metadata
`SubModule.OnUnhandledException` writes a JSON text report to `Modules/CalradiaForge/crash_<timestamp>.cfcrash`. Its current fields are `Timestamp`, `IsTerminating`, and `Exception`; it is not a binary dump and does not include game version, module provenance, native context, or thread affinity.

```
{
  "Timestamp": "<ISO-8601 timestamp>",
  "IsTerminating": true,
  "Exception": "<escaped Exception.ToString() text>"
}
```

**Triage Protocol**:
1. Treat the exception text as evidence to investigate, not proof that Forge or a specific module caused the failure.
2. Inspect the raw exception and its stack trace when present. The Forge analysis route checks only whether the `.cfcrash` text contains an exception marker; it does not infer thread affinity, identify the responsible module, or resolve native symbols.
3. Use game logs, debugger symbols, and independently captured runtime context when those details are required; do not infer fields absent from the report.

### Pattern 3: Stateless SyncData Guard Pattern
Mod behaviors must not persist custom state through `SyncData(IDataStore dataStore)`. In `src/CalradiaForge.Mod`, keep the override free of `dataStore.SyncData(...)` calls; derive transient state from engine entities or use the approved SDK services instead.

```csharp
// CORRECT: 100% Stateless CampaignBehaviorBase
public class DynasticProgressionBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
    }

    // Keep empty when this behavior owns no persistent state. This prevents
    // this behavior from adding custom save fields; it does not certify whole-save
    // compatibility across the game, other mods, or future versions.
    public override void SyncData(IDataStore dataStore)
    {
        // Zero SaveableTypeDefiner, zero dataStore.SyncData(...)
        // State is dynamically derived from game simulation entities
    }

    private void OnHourlyTick()
    {
        // Defer only optional work whose semantics allow it to run in its stable-ID bucket.
        int currentHour = (int)CampaignTime.Now.ToHours;
        foreach (Hero hero in Hero.AllAliveHeroes)
        {
            if (hero == null || string.IsNullOrEmpty(hero.StringId)) continue;
            if (!ForgeTimeSlicer.ShouldProcess(hero.StringId, currentHour)) continue;

            ProcessOptionalMaintenance(hero);
        }
    }
}
```

`ForgeTimeSlicer.ShouldProcess` selects entities by stable identifier; it does not schedule or enumerate them for the caller. A loop still traverses the full source collection and hashes each eligible ID, bucket sizes can be uneven, and default hourly slicing can defer selected work by up to 24 in-game hours. Use it only where that delay preserves event semantics. Measure the full callback, including collection traversal and work performed for selected entities, before claiming reduced cost, latency, or allocations. Time-slicing is not a general fix for frame-time spikes.

---

## 4. Sharp Edges & Anti-Patterns

### Edge 1: Manipulating 3D Meshes or Skeletons in `OnInit()`
- **Severity**: CRITICAL
- **Symptom**: Instant crash to desktop (`0xC0000005 AccessViolationException`) during mission loading screen with zero C# stack trace.
- **Root Cause**: TaleWorlds C++ renderer compiles scene skeletons after `OnInit()` returns. Calling `agent.AgentVisuals.AddMesh()` in `OnInit()` accesses null unmanaged memory pointers.
- **Fix**: Guard with `private bool _initialized` and defer all mesh setup to the first call of `OnMissionTick(dt)`.

### Edge 2: Swallowing Exceptions with Empty Catch Blocks in Simulation Ticks
- **Severity**: HIGH
- **Symptom**: Game performance degrades mysteriously (FPS drops from 60 to 12) or campaign NPCs freeze on the map without reporting an error.
- **Root Cause**: `catch (Exception) { }` inside `HourlyTick` or `OnMissionTick` catches thousands of exceptions per second, causing massive CLR exception handling overhead and silent state corruption.
- **Fix**: Never use empty catch blocks. Log structured failure evidence via `ForgeLogger.Error(ex)` and isolate or quarantine the failing subsystem.

### Edge 3: Accessing Engine Entities from Asynchronous Background Tasks
- **Severity**: HIGH
- **Symptom**: A worker-thread access may race with game-loop updates, throw, or fail inside engine code; the exact failure depends on the API and lifecycle.
- **Root Cause**: Campaign and mission APIs generally have game-thread affinity; do not assume entities are safe for concurrent reads or writes unless that member's contract says so.
- **Fix**: Marshal work through a dispatcher supplied by the host. In this repository, `GameThreadActionDispatch` is internal to `CalradiaForge.Mod` and exposes `RunOrPost`; external SDK consumers must not assume it is public or that a `Dispatch` method exists.

---

## 5. Validation Rules & Verification Checklist

1. [ ] **Zero C++ Timing Violations**: Verified that no `MissionLogic` or `ScriptComponentBehaviour` creates meshes in `OnInit()`.
2. [ ] **Stateless SyncData**: Verified with `tools\verify_stateless_behavior.ps1` that mod behaviors declare zero `SaveableTypeDefiner` and empty `SyncData`.
3. [ ] **GEMINI.md Rule A**: Zero namespaces, folders, or classes named `Campaign` or `Localization` in `src/CalradiaForge.Mod`.
4. [ ] **PDB Symbol Availability**: Release builds generate portable PDBs for exact file-and-line stack trace resolution.
5. [ ] **Bounded Memory Retention**: Verify each feature's actual limit. ForgeWeave retains at most 64 dispatch-journal entries and 64 replay records/results; `ForgeAgentMemory.MaximumAgents` is a separate 2,048-agent identity limit, not a replay or log capacity.
