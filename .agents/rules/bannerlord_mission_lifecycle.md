---
name: bannerlord-mission-lifecycle
description: Prevents catastrophic engine crashes when injecting logic into Mission components and 3D scenes.
trigger: always_on
---

# Bannerlord Mission & Scene Lifecycle

When attaching `MissionLogic`, `MissionView`, or `ScriptComponentBehaviour` to 3D instances in combat/town scenes:

## 1. Engine Initialization Crash Constraint
- **CRITICAL GOTCHA:** Meshes, skeletons, and complex physics components **CANNOT** be created, swapped, or heavily modified during `OnInit()`. 
- **Consequence:** Attempting to manipulate meshes/skeletons in `OnInit()` will crash the internal C++ engine seamlessly.
- **Pattern:** Defer all skeleton and mesh setup to the first `OnTick(float dt)` call using a `private bool _initialized` guard flag.

## 2. Unanimity vs First-Wins in MissionLogic
- **Unanimity (All must agree):** Hooks like `IsAgentInteractionAllowed()` require *all* active `MissionLogic` instances to return `true`. If your custom logic returns `false`, it globally blocks interaction.
- **First-Wins:** Hooks like `MissionEnded(ref MissionResult)` or `OnEndMissionRequest()` poll sequentially. The first logic to return `true` immediately ends the polling and concludes the mission. Do not blindly return true unless your mod is explicitly responsible for ending the current game state.
