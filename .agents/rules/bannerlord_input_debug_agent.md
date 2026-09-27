# Bannerlord C# API: Input, Debug, and Agent

When interacting with the engine for runtime data, debugging, or input, follow these standards.

## 1. Input System
- Namespace: `TaleWorlds.InputSystem`.
- Use the static `Input` class to poll for keys.
- Methods: `Input.IsKeyDown(InputKey.X)`, `Input.IsKeyPressed(InputKey.X)`, `Input.IsKeyReleased(InputKey.X)`.
- Use `TaleWorlds.InputSystem.InputKey` enum for key mappings (e.g., `InputKey.LeftMouseButton`, `InputKey.Space`).

### Calradia Forge F10 edge detection
- `Input.IsKeyPressed` remains the normal configured-hotkey signal. A live Bannerlord diagnostic for the Calradia Forge panel showed `IsKeyPressed(F10) == false` and `IsKeyDown(F10) == false` while `IsKeyDownImmediate(F10) == true`.
- For this F10 case, preserve `IsKeyPressed` and combine it with an F10-only rising-edge fallback: `held = IsKeyDown(F10) || IsKeyDownImmediate(F10)`; trigger on `held && !wasHeld`; then store `held` for the next application tick. This detects one press without repeating while the key remains held.
- Re-arm only after both down checks are false. Clear the previous-held latch when the configured hotkey changes away from F10. Do not replace the normal key-pressed path for other hotkeys or treat `IsKeyDownImmediate` alone as a one-shot event.

## 2. Engine Debugging (MBDebug)
- Namespace: `TaleWorlds.Engine`.
- The `MBDebug` class provides visual debugging tools, such as `MBDebug.RenderDebugDirectionArrow` or `MBDebug.RenderDebugText`.
- **CRITICAL**: Most `MBDebug` methods are marked with `[Conditional("_RGL_KEEP_ASSERTS")]`.
- For debug rendering to work, you **must** define the `_RGL_KEEP_ASSERTS` compilation symbol in your project properties or build configuration.

## 3. Agent (Entities)
- Namespace: `TaleWorlds.MountAndBlade`.
- `Agent` represents living entities in a scene (humans, horses).
- **Player Agent**: Accessible via `Agent.Main`. Note that this can be null outside of scenes.
- **Properties**: Includes `Age`, `Scale`, `Health`, `MovementVelocity`.
- **Controllers**: Check `Agent.Controller` against `Agent.ControllerType` (e.g., `ControllerType.Player`, `ControllerType.AI`).
- **Combat**: Observe `Agent.UsageDirection` for block/attack directions.
