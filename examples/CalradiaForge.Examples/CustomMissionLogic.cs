using System;
using TaleWorlds.MountAndBlade;

namespace CalradiaForge.Examples
{
    /// <summary>
    /// Demonstrates safe mesh and skeleton initialization outside of OnInit.
    /// Task 143: Bannerlord Mission Lifecycle Compliance.
    /// </summary>
    public class CustomMissionLogic : MissionLogic
    {
        private bool _isInitialized;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            // CRITICAL: Meshes, skeletons, and complex physics CANNOT be modified here.
            // Doing so crashes the internal C++ engine seamlessly.
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            // Safe Initialization Block inside the first Tick
            if (!_isInitialized)
            {
                InitializeVisualAssets();
                _isInitialized = true;
            }
        }

        private void InitializeVisualAssets()
        {
            // Safe to swap meshes, add physics components, and manipulate skeletons here.
        }

        
            }
}

