---
name: bannerlord-siege-mechanics
description: Best practices, SiegeWeapon, CastleGate, WallSegment, dynamic navmeshes, and siege detachment AI in Mount & Blade II Bannerlord without external detours.
---

# Bannerlord Siege Mechanics & Dynamic Siege Systems

This skill provides implementation patterns for custom siege engines, destruction tracking, dynamic navmesh integration, and mission logic in Mount & Blade II: Bannerlord.

> **Prerequisites:** Read `bannerlord-shared-patterns` for universal safety rules (no Campaign namespace, no Harmony, no entity serialization in OnInit).

## 1. Custom Siege Engine (MissionObject)

```csharp
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.SiegeMechanics
{
    public class CustomBombardEngine : SiegeWeapon
    {
        private static readonly ActionIndexCache ActShoot = ActionIndexCache.Create("act_usage_mangonel_big_shoot");
        private static readonly ActionIndexCache ActReload = ActionIndexCache.Create("act_usage_mangonel_big_reload");

        [EditorVisibleScriptComponentVariable(true)]
        public string ProjectileItemId = "iron_bombard_ball";

        [EditorVisibleScriptComponentVariable(true)]
        public float MuzzleVelocity = 80f;

        private SynchedMissionObject _muzzleEntity;
        private bool _isLoaded = true;
        private bool _isInitialized;

        public override SiegeEngineType GetSiegeEngineType() => SiegeEngineTypes.Trebuchet;

        protected override void OnInit()
        {
            base.OnInit();
            // Invariant: Never manipulate complex meshes/skeletons here to prevent C++ crash
        }

        public override void AfterMissionStart()
        {
            base.AfterMissionStart();
            var muzzleGameEntity = GameEntity.FindChildWithTag("muzzle");
            if (muzzleGameEntity != null)
            {
                _muzzleEntity = muzzleGameEntity.GetFirstScriptOfType<SynchedMissionObject>();
            }
        }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            if (!_isInitialized)
            {
                SetScriptComponentToTick(GetTickRequirement());
                _isInitialized = true;
            }

            if (PilotAgent != null && _isLoaded && PilotAgent.IsAIControlled)
            {
                FireProjectile();
            }
        }

        public void FireProjectile()
        {
            if (!_isLoaded || _muzzleEntity == null) return;

            MatrixFrame frame = _muzzleEntity.GameEntity.GetGlobalFrame();
            Vec3 launchVelocity = frame.rotation.f * MuzzleVelocity;

            ItemObject itemObj = MBObjectManager.Instance.GetObject<ItemObject>(ProjectileItemId);
            if (itemObj != null)
            {
                MissionWeapon missile = new MissionWeapon(itemObj, null, null);
                Mission.Current.AddCustomMissile(
                    PilotAgent,
                    missile,
                    frame.origin,
                    launchVelocity,
                    frame.rotation.f,
                    MuzzleVelocity,
                    MuzzleVelocity,
                    false,
                    this
                );
            }

            SoundEvent.PlaySound2D("event:/mission/siege/mangonel/shot");
            _isLoaded = false;

            if (PilotAgent != null)
            {
                PilotAgent.SetActionChannel(0, ActShoot, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
            }
        }

        public void Reload()
        {
            _isLoaded = true;
            if (PilotAgent != null)
            {
                PilotAgent.SetActionChannel(0, ActReload, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
            }
        }
    }
}
```

## 2. Gate & Wall Structural Monitor (MissionLogic)

Monitor outer gate integrity and scale durability safely post-deployment:

```csharp
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.SiegeMechanics
{
    public class SiegeIntegrityMissionLogic : MissionLogic
    {
        private CastleGate _outerGate;
        private bool _deploymentFinished;

        public override void AfterMissionStart()
        {
            base.AfterMissionStart();
            _outerGate = Mission.Current.ActiveMissionObjects
                .FindAllWithType<CastleGate>()
                .Find(g => g.GameEntity.HasTag("outer_gate"));
        }

        public override void OnDeploymentFinished()
        {
            base.OnDeploymentFinished();
            _deploymentFinished = true;

            // Safe modification of structural health after deployment state resolves
            if (_outerGate != null && _outerGate.DestructionComponent != null)
            {
                _outerGate.DestructionComponent.HitPoint = 15000f;
            }
        }

        public override void OnAgentHit(
            Agent affectedAgent,
            Agent affectorAgent,
            in AttackCollisionData collisionData,
            in MissionWeapon attackerWeapon)
        {
            base.OnAgentHit(affectedAgent, affectorAgent, in collisionData, in attackerWeapon);

            if (affectedAgent == null || collisionData.IsCollisionWithWorld) return;

            // Boulders knock down troops
            if (attackerWeapon.CurrentUsageItem != null && 
                attackerWeapon.CurrentUsageItem.WeaponClass == WeaponClass.Boulder)
            {
                if (affectedAgent.IsHuman && affectedAgent.Health > 0)
                {
                    affectedAgent.SetKnockedDown();
                }
            }
        }
    }
}
```

## 3. Dynamic NavMesh Swapping Contract

When building custom scenes with destructible walls or deployable ramps:
- Ensure static scene faces do NOT use reserved dynamic IDs ($0-100$ and $400-500$).
- Configure `CastleGate`:
  - Set `NavigationMeshId` to keep interior open face.
  - Set `NavigationMeshIdToDisableOnOpen` to barrier face.
- Configure `WallSegment`:
  - Solid IDs: `OnSolidWallNavmeshID1/2`.
  - Breached IDs: `BrokenWallNavmeshID1/2`.
