---
name: bannerlord-combat-ai
description: Best practices, AgentComponent lifecycle, Formation orders, casualty pipelines, and animation blending in Mount & Blade II Bannerlord without external detours.
---

# Bannerlord Combat AI, Formation Tactics & Animation Blending

This skill provides patterns for implementing custom combat mechanics, formation orders, casualty analysis, and agent animations natively in Mount & Blade II: Bannerlord without Harmony.

> **Prerequisites:** Read `bannerlord-shared-patterns` for universal safety rules (no Campaign namespace, no Harmony, no entity serialization).

## 1. AgentComponent Lifecycle Pattern

Attach custom runtime logic to agents without modifying sealed engine types:

```csharp
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.CombatAI.Components
{
    public class CombatStanceComponent : AgentComponent
    {
        private float _stamina = 100f;
        private const float MaxStamina = 100f;

        public CombatStanceComponent(Agent agent) : base(agent) {}

        protected override void Initialize()
        {
            base.Initialize();
            Agent.OnAgentHealthChanged += HandleHealthChanged;
        }

        // Ticks for AI-controlled units (~20-30 fps)
        public override void OnTickAsAI()
        {
            base.OnTickAsAI();
            TickStamina(0.05f);
        }

        // Manual bridge for player agent called from MissionBehavior.OnMissionTick
        public void ManualTick(float dt)
        {
            TickStamina(dt);
        }

        private void TickStamina(float dt)
        {
            if (_stamina < MaxStamina)
                _stamina = MathF.Min(MaxStamina, _stamina + (15f * dt));
        }

        private void HandleHealthChanged(Agent agent, float oldHealth, float newHealth)
        {
            if (newHealth < agent.HealthLimit * 0.3f)
            {
                agent.ChangeMorale(20f); // Rally on low health
                agent.MakeVoice(SkinVoiceType.Yell, CombatVoiceNetworkPredictionType.Prediction);
            }
        }

        public void CleanUp()
        {
            Agent.OnAgentHealthChanged -= HandleHealthChanged;
        }
    }
}
```

## 2. Programmatic Formation Orders

Directly update formation tactical posture and positions:

```csharp
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.CombatAI.Tactics
{
    public static class TacticalOrderHelper
    {
        public static void OrderDefensiveLine(Formation formation, Vec2 targetDirection, Vec3 targetPos)
        {
            if (formation == null || formation.CountOfUnits == 0) return;

            formation.ArrangementOrder = ArrangementOrder.ArrangementOrderShieldWall;
            formation.FacingOrder = FacingOrder.FacingOrderLookAtDirection(targetDirection);
            formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;

            WorldPosition worldPos = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, targetPos, false);
            formation.MovementOrder = MovementOrder.MovementOrderMove(worldPos);
        }

        public static void OrderFullCharge(Formation formation)
        {
            if (formation == null) return;
            formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
            formation.MovementOrder = MovementOrder.MovementOrderCharge;
            formation.FacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
        }
    }
}
```

## 3. Safe Animation Dispatcher (Channel 1 Overlay)

Play upper-body celebrations and emotes without freezing lower-body locomotion:

```csharp
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.CombatAI.Animations
{
    public static class AgentAnimationDispatcher
    {
        private static readonly ActionIndexCache ActCheerSpear = ActionIndexCache.Create("act_cheer_1");
        private static readonly ActionIndexCache ActCheerSword = ActionIndexCache.Create("act_cheer_2");

        public static void PlayVictoryCheer(Agent agent, bool useSwordVariant = true)
        {
            if (agent == null || !agent.IsActive() || !agent.IsHuman) return;

            ActionIndexCache action = useSwordVariant ? ActCheerSword : ActCheerSpear;

            // Channel 1 overlays upper body; Channel 0 preserves leg movement
            agent.SetActionChannel(
                channelNo: 1,
                actionIndexCache: ref action,
                ignoreAction: false,
                animFlags: AnimFlags.None,
                blendInPeriod: 0.18f,
                blendOutPeriod: 0.25f,
                actionSpeed: 1.0f,
                actionProgress: 0f,
                blendProgress: 0f,
                isFreezeAtFinish: false,
                blendOutAtFinish: 0.20f,
                priority: 10,
                sync: false
            );

            agent.MakeVoice(SkinVoiceType.Victory, CombatVoiceNetworkPredictionType.Prediction);
        }
    }
}
```

## 4. Scaled MissionBehavior Architecture (1000+ Battles)

Manage component lifecycles, player tick bridges, and casualty tracking safely:

```csharp
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using CalradiaForge.CombatAI.Components;

namespace CalradiaForge.CombatAI.Behaviors
{
    public class ScaledCombatMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private readonly List<CombatStanceComponent> _components = new List<CombatStanceComponent>(1000);

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);
            if (agent.IsHuman)
            {
                var comp = new CombatStanceComponent(agent);
                agent.AddComponent(comp);
                _components.Add(comp);
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            // Manual bridge for player agent
            var main = Mission.MainAgent;
            if (main != null && main.IsActive())
            {
                main.GetComponent<CombatStanceComponent>()?.ManualTick(dt);
            }
        }

        public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnEarlyAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
            // Safe: affectedAgent.Formation is still non-null here
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
            var comp = affectedAgent.GetComponent<CombatStanceComponent>();
            if (comp != null)
            {
                comp.CleanUp();
                _components.Remove(comp);
            }
        }

        public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
        {
            base.OnAgentTeamChanged(prevTeam, newTeam, agent);
            if (agent.IsActive() && newTeam != null)
            {
                agent.Formation = newTeam.GetFormation(agent.FormationClass);
                // Invariant: synchronise mount team
                if (agent.HasMount && agent.MountAgent != null && agent.MountAgent.Team != newTeam)
                {
                    agent.MountAgent.SetTeam(newTeam, true);
                }
            }
        }

        public override void OnRemoveBehavior()
        {
            foreach (var comp in _components) comp.CleanUp();
            _components.Clear();
            base.OnRemoveBehavior();
        }
    }
}
```
