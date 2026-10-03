---
name: bannerlord-missionview-hud
description: Best practices, 3D-to-2D WorldToScreen projection, and in-mission Gauntlet HUD overlays using MissionView and MBSubModuleBase in Mount & Blade II Bannerlord without external detours.
---

# Bannerlord MissionView & In-Mission HUD Skill

Use this skill when developing client-side visual overlays, overhead nameplates, target reticles, 3D healthbars, in-mission tactical menus, or custom keyboard/mouse interaction layers during battles, sieges, and town visits.

> **Prerequisites:** Read `bannerlord-shared-patterns` for universal safety rules: no `Campaign` namespace, no Harmony dependency or Harmony patch/detour implementation, and no entity serialization. The only Forge-side Harmony path is the optional read-only observer described there; it does not load or modify Harmony.

---

## Architecture Overview

```
MBSubModuleBase.OnMissionBehaviorInitialize(Mission mission)
                     │
                     ▼ mission.AddMissionBehavior(new CustomMissionView())
MissionScreen discovers MissionView during OnInitialize()
                     │
                     ▼
MissionView.OnMissionScreenInitialize()  ──► Attaches GauntletLayer to MissionScreen
                     │
                     ▼
MissionView.OnMissionScreenTick(dt)      ──► MBWindowManager.WorldToScreen projection
                     │
                     ▼
MissionView.OnMissionScreenFinalize()    ──► Cleans up layers, movies, and memory
```

---

## Implementation Template

### 1. ViewModel (`TargetMarkerVM.cs`)

```csharp
using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.Core.MissionViews
{
    public class TargetMarkerVM : ViewModel
    {
        private float _screenX;
        private float _screenY;
        private bool _isVisible;
        private float _healthRatio;

        public Agent BoundAgent { get; }

        public TargetMarkerVM(Agent agent)
        {
            BoundAgent = agent;
            _healthRatio = agent.Health / agent.HealthLimit;
        }

        [DataSourceProperty]
        public float ScreenX
        {
            get => _screenX;
            set
            {
                if (MathF.Abs(value - _screenX) > 0.5f)
                {
                    _screenX = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public float ScreenY
        {
            get => _screenY;
            set
            {
                if (MathF.Abs(value - _screenY) > 0.5f)
                {
                    _screenY = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public float HealthRatio
        {
            get => _healthRatio;
            set
            {
                if (MathF.Abs(value - _healthRatio) > 0.01f)
                {
                    _healthRatio = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        public void UpdateHealth() => HealthRatio = BoundAgent.Health / BoundAgent.HealthLimit;
    }
}
```

### 2. Custom MissionView (`CustomHudMissionView.cs`)

```csharp
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace CalradiaForge.Core.MissionViews
{
    public class CustomHudMissionView : MissionView
    {
        private GauntletLayer _gauntletLayer;
        private IGauntletMovie _movie;
        private Camera _combatCamera;
        private readonly List<TargetMarkerVM> _markers = new List<TargetMarkerVM>();

        private const float MAX_RENDER_DIST_SQ = 45f * 45f;

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            _gauntletLayer = new GauntletLayer(35, "CustomHudLayer");
            // For passive HUD, leave input uninhibited
            _movie = _gauntletLayer.LoadMovie("CustomMissionHud", this);
            MissionScreen.AddLayer(_gauntletLayer);
        }

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);
            if (agent.IsHuman && !agent.IsMainAgent)
            {
                _markers.Add(new TargetMarkerVM(agent));
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
            _markers.RemoveAll(m => m.BoundAgent == affectedAgent);
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);

            if (_combatCamera == null)
            {
                _combatCamera = MissionScreen?.CombatCamera;
                if (_combatCamera == null) return;
            }

            Vec3 camPos = _combatCamera.Position;

            for (int i = 0; i < _markers.Count; i++)
            {
                var marker = _markers[i];
                var agent = marker.BoundAgent;

                if (!agent.IsActive() || !agent.IsAlive())
                {
                    marker.IsVisible = false;
                    continue;
                }

                Vec3 diff = agent.Position - camPos;
                if (diff.LengthSquared > MAX_RENDER_DIST_SQ)
                {
                    marker.IsVisible = false;
                    continue;
                }

                // Eye level + height offset
                Vec3 worldPoint = agent.Position + new Vec3(0f, 0f, agent.GetEyeGlobalHeight() + 0.30f);

                float screenX = 0f;
                float screenY = 0f;
                float depth = 0f;

                MBWindowManager.WorldToScreen(_combatCamera, worldPoint, ref screenX, ref screenY, ref depth);

                // Crucial depth guard
                if (depth > 0.05f)
                {
                    marker.IsVisible = true;
                    marker.ScreenX = screenX - 40f;
                    marker.ScreenY = screenY - 15f;
                    marker.UpdateHealth();
                }
                else
                {
                    marker.IsVisible = false;
                }
            }
        }

        public override void OnMissionScreenFinalize()
        {
            if (_gauntletLayer != null)
            {
                MissionScreen.RemoveLayer(_gauntletLayer);
                _gauntletLayer.ReleaseMovie(_movie);
                _gauntletLayer = null;
            }
            _markers.Clear();
            _combatCamera = null;
            base.OnMissionScreenFinalize();
        }
    }
}
```

---

## Essential Checklist
- [ ] Registered via `MBSubModuleBase.OnMissionBehaviorInitialize(Mission mission)` without external detours.
- [ ] Projection math executed inside `OnMissionScreenTick(float dt)`, NOT `OnMissionTick(dt)`.
- [ ] `depth > 0.05f` checked on every projection before making the marker visible.
- [ ] Root Gauntlet XML container tagged with `DoNotAcceptEvents="true"` and `DoNotPassEventsToChildren="true"` for passive overlays.
- [ ] Distance-squared pre-filter applied before executing `MBWindowManager.WorldToScreen`.
- [ ] Memory freed in `OnMissionScreenFinalize()` by calling `ReleaseMovie()` and `RemoveLayer()`.
