---
name: debugging-master
description: Scientific debugging methodology for Mount & Blade II: Bannerlord and Calradia Forge. Hypothesis-driven root-cause isolation, TaleWorlds native crash triage (0xC0000005), 10-minute rule, stack trace symbol resolution, and save state corruption diagnosis.
risk: safe
source: Calradia Forge Agent Ecosystem (Apache 2.0)
date_added: 2026-09-28
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
- `bannerlord-crash-triage`: Decodes binary `.cfcrash` dumps and native unhandled exceptions.
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

### Pattern 2: Binary Crash Dump (.cfcrash) Decoding
Calradia Forge generates structured `.cfcrash` dumps in `artifacts/` when an unhandled exception escapes.

```
CFCRASH DUMP HEADER:
  Timestamp:     2026-09-28T03:32:05.112Z
  GameVersion:   e1.2.9 / v25.2.0
  Exception:     System.NullReferenceException: Object reference not set to an instance of an object.
  NativeContext: TaleWorlds.MountAndBlade.View.MissionViews.MissionView.OnInit() + 0x4A
  ThreadAffinity: MainGameThread [ID: 1]
  ActiveModule:  CalradiaForge.Mod
```

**Triage Protocol**:
1. Check `ActiveModule` to verify whether the failing instruction originated from mod code or vanilla engine code called with invalid state.
2. Verify `ThreadAffinity`: If `ThreadAffinity != MainGameThread`, an asynchronous task violated game-thread affinity.
3. Inspect `NativeContext` to isolate the TaleWorlds subsystem (`MissionViews`, `CampaignSystem`, `GauntletUI`).

### Pattern 3: Stateless SyncData Guard Pattern
Mod behaviors must never corrupt user game saves. Keep `SyncData(IDataStore dataStore)` completely free of stateful serialization unless strictly isolated with unique string IDs and primitive types.

```csharp
// CORRECT: 100% Stateless CampaignBehaviorBase
public class DynasticProgressionBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
    }

    // MANDATORY: Keep completely empty to guarantee 100% save-compatibility
    public override void SyncData(IDataStore dataStore)
    {
        // Zero SaveableTypeDefiner, zero dataStore.SyncData(...)
        // State is dynamically derived from game simulation entities
    }

    private void OnHourlyTick()
    {
        // Modulo-24 anti-lag time-slicing
        int currentHour = (int)CampaignTime.Now.ToHours;
        // ...
    }
}
```

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

### Edge 3: Mutating Engine Entities from Asynchronous Background Tasks
- **Severity**: HIGH
- **Symptom**: Rare, non-reproducible crashes in `TaleWorlds.CampaignSystem.Campaign.OnTick()` or `MBObjectManager.GetObject()`.
- **Root Cause**: TaleWorlds campaign and mission objects (`Hero`, `MobileParty`, `Settlement`) are not thread-safe. Reading or writing entity fields from `Task.Run()` causes concurrent dictionary mutation crashes.
- **Fix**: Marshal all entity accesses back to the main game thread via `GameThreadActionDispatch.Dispatch(() => { ... })`.

---

## 5. Validation Rules & Verification Checklist

1. [ ] **Zero C++ Timing Violations**: Verified that no `MissionLogic` or `ScriptComponentBehaviour` creates meshes in `OnInit()`.
2. [ ] **Stateless SyncData**: Verified with `tools\verify_stateless_behavior.ps1` that mod behaviors declare zero `SaveableTypeDefiner` and empty `SyncData`.
3. [ ] **GEMINI.md Rule A**: Zero namespaces, folders, or classes named `Campaign` or `Localization` in `src/CalradiaForge.Mod`.
4. [ ] **PDB Symbol Availability**: Release builds generate portable PDBs for exact file-and-line stack trace resolution.
5. [ ] **Bounded Memory Retention**: Replay and diagnostic loggers bound retained entries to a maximum of 2,048 slots to prevent OOM.
