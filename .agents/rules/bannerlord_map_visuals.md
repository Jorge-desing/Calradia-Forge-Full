# Bannerlord World Map Visuals, Map Icons & Map Tracks Architecture

In Mount & Blade II: Bannerlord, the campaign world map visual layer is strictly separated from logical simulation:
- **Logical Simulation**: `TaleWorlds.CampaignSystem` (`MobileParty`, `Settlement`, `Army`).
- **Visual & View Layer**: `SandBox.View.Map` (`PartyVisual`, `SettlementVisual`, `PartyVisualManager`, `MapScreen`).
- **Scene Graph & Physics**: `TaleWorlds.Engine` (`GameEntity`, `Scene`, `Camera`, `MetaMesh`, `Decal`).

---

## 1. Map Icon Architecture & Entity Structure
- **`PartyVisualManager.Current`**: The singleton mapping `PartyBase` to its corresponding `PartyVisual`:
  ```csharp
  PartyVisual visual = PartyVisualManager.Current.GetVisualOfParty(party.Party);
  ```
- **`StrategicEntity`**: The root `GameEntity` owned by `PartyVisual` in the campaign map scene.
  - Contains child entities for the character model, horse mount, and weapons.
  - Scale factor defaults to $0.3\text{f}$ for character models.
- **Dynamic Swapping**:
  To swap a party's icon (e.g. ship for naval travel or custom unit):
  1. Hide default child entities: `child.SetVisibilityExcludeParents(false);`.
  2. Instantiate and attach custom prefab or mesh: `rootEntity.AddChild(customEntity);`.

---

## 2. Map Tracks & Decal Architecture
The tracking mechanic allows tracking parties across the world map:
- **`MapTracksCampaignBehavior`**:
  - Manages `Track` objects via an internal `TrackPool` object pool.
  - Listens to party movement deltas and generates tracks (`AddTrack`).
  - Records party metadata: `Track.PartyTypeEnum`, `Position`, `Direction`, `Speed`, `NumberOfMen`, and `CreationTime`.
- **`MapTracksVisual`**:
  - Projects decal materials onto terrain based on terrain type (Mud, Grass, Snow, Sand) and party composition (horseshoe decals for cavalry, footprints for infantry, blood decals for wounded).
  - Uses terrain normal vectors from `Scene.GetTerrainHeightAndNormal()`.

---

## 3. Terrain Queries & Map Scene Calculations
Access the campaign scene via `Campaign.Current.MapSceneWrapper`:
```csharp
IMapScene mapScene = Campaign.Current.MapSceneWrapper;

// 1. Terrain classification
TerrainType terrain = mapScene.GetTerrainTypeAtPosition(position2D);

// 2. Navigation Mesh face index
PathFaceRecord face = PathFaceRecord.NullFaceRecord;
mapScene.GetFaceIndexForPoint(position2D, ref face);

// 3. Precise Terrain Height and Normal
Scene rawScene = ((MapScene)mapScene).Scene;
rawScene.GetTerrainHeightAndNormal(position2D, out float heightZ, out Vec3 normal);
```

---

## 4. Camera Navigation Hooks
- Access active camera via `MapScreen.Instance.Camera`.
- **Party Follow**:
  - To follow a party: `Campaign.Current.CameraFollowParty = party.Party;`
  - To release: `Campaign.Current.CameraFollowParty = null;`

---

## 5. Lifecycle Rules & Memory Safety (Zero Harmony)
1. **Never create meshes or entities in `OnInit()` or `OnSubModuleLoad()`**: The C++ rendering engine will crash. Always defer to `OnSessionLaunchedEvent` or during runtime ticks.
2. **Explicit Cleanup of Spawned Entities**: Any `GameEntity` created with `GameEntity.CreateEmpty()` or `GameEntity.Instantiate()` must be explicitly destroyed with `entity.Remove(0)` when the session ends.
3. **Never Serialize Engine Entities**: Do NOT store `GameEntity`, `PartyVisual`, or `Mesh` in `SyncData()`.
4. **Anti-Shadowing Constraint (`GEMINI.md`)**: Never name a folder, namespace, or class `Campaign`.
