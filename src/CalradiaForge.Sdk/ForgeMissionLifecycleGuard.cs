using System;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Lifecycle Guard for Bannerlord MissionLogic and MissionView components.
    /// Strictly protects against the critical TaleWorlds C++ engine initialization crash:
    /// Meshes, scene entities, and skeletons MUST NOT be instantiated or altered during OnInit().
    /// All scene manipulations are deferred to the first OnTick(float dt) cycle using an initialization guard.
    /// </summary>
    public sealed class ForgeMissionLifecycleGuard
    {
        private bool _isInitialized;
        private readonly Action _deferredInitializer;

        public bool IsInitialized => _isInitialized;

        public ForgeMissionLifecycleGuard(Action deferredInitializer)
        {
            _deferredInitializer = deferredInitializer ?? throw new ArgumentNullException(nameof(deferredInitializer));
        }

        /// <summary>
        /// Call this on every mission tick (e.g. OnMissionTick or OnTick).
        /// Executes the deferred initialization strictly once on the first tick cycle.
        /// </summary>
        /// <param name="dt">Delta time passed by the engine.</param>
        /// <returns>True if this was the initial initialization tick; false otherwise.</returns>
        public bool OnTick(float dt)
        {
            if (_isInitialized) return false;

            _deferredInitializer();
            _isInitialized = true;
            return true;
        }

        /// <summary>
        /// Resets the guard when a mission concludes or restarts to prevent stale state.
        /// </summary>
        public void Reset()
        {
            _isInitialized = false;
        }

        /// <summary>
        /// Validates that a component is NOT running initialization code during the forbidden OnInit phase.
        /// Throws an InvalidOperationException if an attempt is made to mutate meshes/skeletons before the first tick.
        /// </summary>
        public static void AssertSafePhase(bool isOnInitPhase, string componentName)
        {
            if (isOnInitPhase)
            {
                throw new InvalidOperationException(
                    $"[ForgeMissionLifecycleGuard] CRITICAL ENGINE VIOLATION in '{componentName}': " +
                    "Meshes, skeletons, and physics entities cannot be created or modified during OnInit(). " +
                    "Defer all setup to the first OnTick() call using ForgeMissionLifecycleGuard.");
            }
        }
    }
}
