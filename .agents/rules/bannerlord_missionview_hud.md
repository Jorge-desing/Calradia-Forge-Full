# Bannerlord MissionView, 3D HUD & In-Mission Input Architecture

In Mount & Blade II: Bannerlord, in-mission client-side visuals, 3D overhead tags/healthbars, HUD layers, and hardware inputs are handled by `MissionView` attached to `MissionScreen`.

---

## 1. Class Hierarchy & Architectural Separation
- **`MissionLogic`** (inherits `MissionBehavior`): Runs simulation, battle rules, agent damage, spawn tickets, and game mechanics.
- **`MissionView`** (inherits `MissionBehavior`): Runs rendering hooks, Gauntlet UI layers, camera manipulation, 3D-to-2D projections, and input polling.
- **`MissionScreen`** (inherits `ScreenBase`): Manages the viewport `SceneLayer`, 3D `CombatCamera`, and layered `GauntletLayer` UI stacks.

---

## 2. Native Lifecycle & Registration (Zero Harmony)

### Clean Native Injection:
Do NOT use Harmony patches (e.g. hooking `MissionState.OpenNew`). The engine provides the official native hook on `MBSubModuleBase`:
```csharp
public class SubModule : MBSubModuleBase
{
    public override void OnMissionBehaviorInitialize(Mission mission)
    {
        base.OnMissionBehaviorInitialize(mission);
        // Automatically discovered and bound by MissionScreen
        mission.AddMissionBehavior(new CustomMarkerMissionView());
    }
}
```
When `mission.AddMissionBehavior(...)` receives a `MissionView`, `MissionScreen` automatically discovers it, assigns `view.MissionScreen`, and invokes its view lifecycle methods.

### Lifecycle Methods:
1. **`OnMissionScreenInitialize()`**: Invoked once when `MissionScreen` setup completes. Instantiate `GauntletLayer`, load movies, and call `this.MissionScreen.AddLayer(layer)`.
2. **`OnMissionScreenTick(float dt)`**: Invoked every render frame (60–144+ FPS). Perform 3D-to-2D projection math, update ViewModel screen offsets, and poll inputs.
   *(Note: Never do projection math in `OnMissionTick(dt)`, which only runs on simulation/physics ticks and pauses during menus).*
3. **`OnMissionScreenFinalize()`**: Invoked on mission exit. Deterministically remove layers, release movies (`_gauntletLayer.ReleaseMovie(_movie)`), and clear lists to prevent cross-battle memory leaks.

---

## 3. 3D-to-2D Screen Projection Mathematics

### API: `MBWindowManager.WorldToScreen`
```csharp
float screenX = -100f;
float screenY = -100f;
float depth = 0f;

MBWindowManager.WorldToScreen(
    this.MissionScreen.CombatCamera, 
    worldAnchorPosition, 
    ref screenX, 
    ref screenY, 
    ref depth
);
```

### Critical Frustum Depth Guard:
Perspective projection inverts coordinate vectors when targets are behind the camera view plane.
- **Always check `if (depth > 0.05f)`**.
- If `depth <= 0.05f`, immediately set `IsVisible = false` or position offscreen (`-1000f, -1000f`).
- Neglecting this check causes units behind the player to project onto the center of the screen inverted.

### Anchor Selection:
- Never anchor to `agent.Position` directly (anchors to ground feet level).
- Anchor to eye level: `agent.Position + new Vec3(0f, 0f, agent.GetEyeGlobalHeight() + 0.35f);`.
- `GetEyeGlobalHeight()` automatically adjusts whether the agent is mounted on horseback, standing, or crouching.

---

## 4. Input Architecture: Passive HUD vs. Interactive Modals

### Passive Overlays (HUD / Healthbars / Floating Markers):
- Do **not** set input restrictions on the `GauntletLayer`.
- In Gauntlet XML root widgets, specify `DoNotAcceptEvents="true"` and `DoNotPassEventsToChildren="true"`.
- Mouse look and combat clicks will pass cleanly to `MissionScreen.SceneLayer`.

### Interactive Modals (Tactical Menus / Radial Wheels):
- Capture mouse and freeze combat camera:
  ```csharp
  _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.MouseButtons | InputUsageMask.MouseLook);
  _gauntletLayer.IsFocusLayer = true;
  ScreenManager.TrySetFocus(_gauntletLayer);
  ```
- **Crucial**: Always call `ResetInputRestrictions()` and `TryLoseFocus()` when the modal closes, or the player's camera remains permanently frozen:
  ```csharp
  _gauntletLayer.InputRestrictions.ResetInputRestrictions();
  _gauntletLayer.IsFocusLayer = false;
  ScreenManager.TryLoseFocus(_gauntletLayer);
  ```

---

## 5. Performance Optimization for 1,000+ Agent Battles
1. **Distance-Squared Culling**: Calculate `(agent.Position - camera.Position).LengthSquared`. Skip all math if $> \text{MaxDist}^2$.
2. **Camera Direction Cone Culling**: Check `Vec3.DotProduct(camera.Direction, (agentPos - camPos).NormalizedCopy()) > 0.2f`. Skip matrix multiplication for targets outside the forward view frustum.
3. **Dirty-Checking Notification Suppression**: In ViewModels, only call `OnPropertyChangedWithValue` if the screen coordinate moved by $> 0.5\text{px}$.
4. **Memory Management**: In `OnMissionScreenFinalize()`, release `GauntletLayer` and movies, clear all `MBBindingList` collections, and set camera references to `null`.
