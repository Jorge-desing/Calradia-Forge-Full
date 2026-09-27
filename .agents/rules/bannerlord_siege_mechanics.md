# Bannerlord Siege Mechanics, Siege Weapons, and Dynamic Scene Systems

When developing or modifying siege engines, destructible fortifications, and tactical siege deployment in Mount & Blade II: Bannerlord:

## 1. Dual-Layer Siege Architecture
- **Campaign Meta Simulation**: Governed by `TaleWorlds.Core.SiegeEngineType`, `SiegeEvent`, `BesiegerCamp`, and `SiegeStrategy`.
  - Defined in `ModuleData/siegeengines.xml`. Registered under XML category `SiegeEngines`.
  - Properties (`man_day_cost`, `difficulty`, `campaign_rate_of_fire_per_day`, `damage`, `max_hit_points`) affect **only** campaign map bombardment and construction times, NOT 3D mission health.
- **Mission Tactical Combat**: Governed by `TaleWorlds.MountAndBlade.SiegeWeapon`, `RangedSiegeWeapon`, `SynchedMissionObject`, and `UsableMachine`.
  - Represents the 3D physics entity, user slots (`StandingPoint`), projectile delivery, and structural durability.

## 2. Dynamic Scene Integration & Dynamic NavMeshes (`.dnm`)
- **Castle Gates (`CastleGate`)**:
  - Durability is driven by `DestructableComponent` (outer gate ~12,000 HP, inner gate ~6,000 HP).
  - While closed, navigation face `NavigationMeshIdToDisableOnOpen` blocks movement.
  - When breached/destroyed, that face is disabled and `NavigationMeshId` is activated to open entry into the keep.
- **Wall Breaches (`WallSegment`)**:
  - Solid state uses `OnSolidWallNavmeshID1/2` and `SolidWallConnectionNavmeshID1/2`.
  - When breached, the engine toggles solid IDs off and enables `BrokenWallNavmeshID1/2`, letting troops walk over rubble.
- **Siege Towers & Battering Rams**:
  - Follow splines defined by `PathEntity` via the `PathTracker` component.
  - Traverse speed is calculated dynamically from the number of pushing agents stationed at `StandingPoint` slots (`AutoSheathWeapons="true"`, `TranslateUser="true"`).
  - Wheels animate via linear distance: $\Delta\theta = \frac{\Delta\text{Distance}}{\text{WheelDiameter} \times 0.5}$.
  - Dynamic NavMesh (`siege_tower_5m_dnm`) activates upon reaching battlements to link internal tower ladders to ramparts.

## 3. Tactical Lanes & Detachments
- **Tactical Lanes**: Managed via `TeamAISiegeAttacker` / `Defender` across `CastleSide.Left`, `Middle`, and `Right`. Formations re-route when lanes change from Unopened to Breached or Secured.
- **The Detachment Pattern**: Individual agents detach from parent formations via `Formation.DetachmentManager` to pilot siege weapons, carry ammunition, or push vehicles without breaking army cohesion.
- **Ladder Dogpile Mitigation**: `LadderQueueManager` dynamically inflates pathfinding cost penalties on navmesh faces around the ladder base if $>2-3$ agents are waiting.

## 4. Critical Engine Pitfalls
- **`OnInit()` Scene Modification Crash**: Never modify meshes, skeletons, or physics colliders inside `MissionObject.OnInit()`. Defer scene manipulations to the first `OnTick(dt)` guarded by an `_isInitialized` boolean flag.
- **Dynamic NavMesh ID Collision**: Do not use static navmesh IDs that overlap with dynamic siege IDs ($0-100$ and $400-500$). Overlaps cause agents to freeze or jump from walls.
- **Detachment Leaks on Engine Destruction**: If a siege engine is destroyed mid-battle, always call `UsableMachine.ClearAgentAssignments()` before unregistering; otherwise, dangling agent references will crash when the formation routs.
- **Anti-Shadowing Constraint (`GEMINI.md`)**: Never name a folder, sub-namespace, or class `Campaign`. Use `SiegeMechanics`, `SiegeMissionLogics`, or `CampaignBehaviors`.
