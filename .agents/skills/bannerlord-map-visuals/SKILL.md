---
name: bannerlord-map-visuals
description: Best practices, entity manipulation, and terrain querying for custom World Map Visuals, Map Icons, and Map Tracks in Mount & Blade II Bannerlord without external detours.
---

# Bannerlord Map Visuals & Tracks Skill

Use this skill when developing custom world map markers, swapping party or settlement map icons, projecting terrain decals/tracks, or querying campaign map elevation and terrain types in Mount & Blade II: Bannerlord.

---

## When to Use This Skill
- Customizing or dynamically replacing party map icons (e.g. ships for naval travel, custom vanguard hero models).
- Querying campaign map terrain elevation (`Z`), surface normals, or navigation mesh faces (`PathFaceRecord`).
- Interacting with party map tracks or adding custom campaign map markers.
- Controlling the campaign map camera programmatically via `MapScreen.Instance.Camera` or `Campaign.Current.CameraFollowParty`.

---

## Implementation Patterns

### 1. Dynamic Party Map Icon Swapping
Access `StrategicEntity` through `PartyVisualManager`:

```csharp
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;

namespace CalradiaForge.Core.MapVisuals
{
    public static class MapVisualHelper
    {
        public static void SetPartyCustomPrefab(MobileParty party, string prefabName)
        {
            PartyVisual visual = PartyVisualManager.Current?.GetVisualOfParty(party.Party);
            if (visual == null || visual.StrategicEntity == null) return;

            GameEntity root = visual.StrategicEntity;

            // Hide default troop & mount entities
            for (int i = 0; i < root.ChildCount; i++)
            {
                root.GetChild(i).SetVisibilityExcludeParents(false);
            }

            // Instantiate and attach custom icon
            Scene mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
            GameEntity customEntity = GameEntity.Instantiate(mapScene, prefabName, root.GetGlobalFrame());
            root.AddChild(customEntity, false);
        }
    }
}
```

### 2. Precise Terrain Queries & Height Snapping
```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace CalradiaForge.Core.MapVisuals
{
    public static class TerrainQueryHelper
    {
        public static Vec3 GetWorldPointWithTerrainHeight(Vec2 mapPosition)
        {
            IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
            Scene rawScene = ((MapScene)mapSceneWrapper).Scene;

            rawScene.GetTerrainHeightAndNormal(mapPosition, out float z, out Vec3 normal);
            return new Vec3(mapPosition.X, mapPosition.Y, z);
        }
    }
}
```

### 3. Cleanup of Dynamic Scene Entities
Always explicitly clean up manual entities when sessions end:
```csharp
public static void SafeRemoveEntity(GameEntity entity)
{
    if (entity != null && entity.Pointer != System.IntPtr.Zero)
    {
        entity.Remove(0);
    }
}
```

---

## Critical Domain Rules
> **Universal rules** (no Campaign namespace, no Harmony, no entity serialization) are in `bannerlord-shared-patterns`.

1. **Never create meshes or entities in `OnInit()`**: Wait for `OnSessionLaunchedEvent` — mesh manipulation in `OnInit()` causes a seamless C++ engine crash.
2. **Never serialize `GameEntity` or `PartyVisual` in `SyncData()`**: Entities are transient C++ handles that are invalid between sessions.
