using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Provides tools to inspect, serialize, and deserialize campaign variables natively.
    /// This allows mods to safely expose data to save files or the Desktop App without corruption.
    /// </summary>
    public static class CampaignVariableInspector
    {
        private static readonly ConcurrentDictionary<string, (Func<object> Getter, Action<object> Setter)> TrackedVariables = new ConcurrentDictionary<string, (Func<object>, Action<object>)>();

        /// <summary>
        /// Fired when the host requests a serialization snapshot of all tracked variables.
        /// </summary>
        public static event Action<Dictionary<string, object>> OnSnapshotRequested;

        /// <summary>
        /// Registers a variable to be tracked by the inspector.
        /// </summary>
        /// <param name="key">A unique key for this variable.</param>
        /// <param name="getter">A function that returns the current value of the variable.</param>
        /// <param name="setter">An optional function that updates the value of the variable (live tweaking).</param>
        public static void TrackVariable(string key, Func<object> getter, Action<object> setter = null)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.");
            if (getter == null) throw new ArgumentNullException(nameof(getter));

            TrackedVariables[key] = (getter, setter);
        }

        /// <summary>
        /// Removes a variable from being tracked.
        /// </summary>
        public static void UntrackVariable(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            TrackedVariables.TryRemove(key, out _);
        }

        /// <summary>
        /// Clears all tracked variables (prevents memory leaks on campaign unload).
        /// </summary>
        public static void ClearTrackedVariables()
        {
            TrackedVariables.Clear();
        }

        /// <summary>
        /// Clears all listeners from the snapshot requested event.
        /// </summary>
        public static void ClearSnapshotListeners()
        {
            OnSnapshotRequested = null;
        }

        /// <summary>
        /// Updates a tracked variable if it has an associated setter.
        /// </summary>
        public static bool SetVariable(string key, object value)
        {
            if (!string.IsNullOrWhiteSpace(key) && TrackedVariables.TryGetValue(key, out var accessors) && accessors.Setter != null)
            {
                accessors.Setter(value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reads a tracked variable by key safely without throwing.
        /// </summary>
        public static object GetVariable(string key)
        {
            if (!string.IsNullOrWhiteSpace(key) && TrackedVariables.TryGetValue(key, out var accessors) && accessors.Getter != null)
            {
                try { return accessors.Getter(); }
                catch { return null; }
            }
            return null;
        }

        /// <summary>
        /// Generates a snapshot of all currently tracked campaign variables.
        /// </summary>
        public static Dictionary<string, object> GenerateSnapshot()
        {
            var snapshot = new Dictionary<string, object>(TrackedVariables.Count);
            foreach (var kvp in TrackedVariables)
            {
                try
                {
                    snapshot[kvp.Key] = kvp.Value.Getter();
                }
                catch (Exception ex)
                {
                    ForgeLogger.LogError($"Failed to read tracked variable {kvp.Key}", ex);
                    snapshot[kvp.Key] = "<Error>";
                }
            }
            
            OnSnapshotRequested?.Invoke(snapshot);
            return snapshot;
        }
    }
}
