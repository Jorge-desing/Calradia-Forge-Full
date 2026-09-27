using System;
using System.Collections.Concurrent;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Provides extension methods for attaching arbitrary data to campaign entities.
    /// Acts as a substitute for ButterLib's campaign behaviors and variables.
    /// </summary>
    public static class ForgeData
    {
        // Internal store of entity IDs to their attached data
        // For Bannerlord, entities like Hero have a unique StringId or generic Object mapping.
        public static readonly ConcurrentDictionary<object, ConcurrentDictionary<Type, object>> EntityData = 
            new ConcurrentDictionary<object, ConcurrentDictionary<Type, object>>();

        private static readonly Func<object, ConcurrentDictionary<Type, object>> EntityDictFactory = 
            _ => new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// Gets or creates attached data of type T for the specified entity.
        /// </summary>
        public static T GetForgeData<T>(this object entity) where T : class, new()
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            while (true)
            {
                var entityDict = GetOrCreateEntityDictionary(entity);
                lock (entityDict)
                {
                    if (!IsCurrentEntityDictionary(entity, entityDict)) continue;

                    return (T)entityDict.GetOrAdd(typeof(T), _ => new T());
                }
            }
        }

        /// <summary>
        /// Sets attached data of type T for the specified entity.
        /// </summary>
        public static void SetForgeData<T>(this object entity, T data) where T : class
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (data == null) throw new ArgumentNullException(nameof(data));

            while (true)
            {
                var entityDict = GetOrCreateEntityDictionary(entity);
                lock (entityDict)
                {
                    if (!IsCurrentEntityDictionary(entity, entityDict)) continue;
                    entityDict[typeof(T)] = data;
                    return;
                }
            }
        }

        /// <summary>
        /// Checks whether attached data of type T exists for the entity.
        /// </summary>
        public static bool HasForgeData<T>(this object entity) where T : class
        {
            if (entity == null) return false;
            return EntityData.TryGetValue(entity, out var entityDict) && entityDict.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Removes attached data of type T for the specified entity.
        /// </summary>
        public static bool RemoveForgeData<T>(this object entity) where T : class
        {
            if (entity == null) return false;
            while (EntityData.TryGetValue(entity, out var entityDict))
            {
                lock (entityDict)
                {
                    if (!IsCurrentEntityDictionary(entity, entityDict)) continue;
                    if (!entityDict.TryRemove(typeof(T), out _)) return false;

                    // All SDK insertions synchronize on this per-entity dictionary and re-check
                    // that it is still the current outer value before mutating it. Removing the
                    // exact key/value pair therefore cannot detach a concurrent SDK insertion.
                    if (entityDict.IsEmpty)
                    {
                        TryRemoveEntityDictionary(entity, entityDict);
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes all attached data for the specified entity (prevents memory leaks when an entity dies/despawns).
        /// </summary>
        public static bool RemoveForgeData(this object entity)
        {
            if (entity == null) return false;
            while (EntityData.TryGetValue(entity, out var entityDict))
            {
                lock (entityDict)
                {
                    if (!IsCurrentEntityDictionary(entity, entityDict)) continue;
                    return TryRemoveEntityDictionary(entity, entityDict);
                }
            }
            return false;
        }

        /// <summary>
        /// Clears all Forge data. This is typically called when starting a new campaign or loading a save.
        /// </summary>
        public static void ClearAll()
        {
            EntityData.Clear();
        }

        private static ConcurrentDictionary<Type, object> GetOrCreateEntityDictionary(object entity)
        {
            return EntityData.GetOrAdd(entity, EntityDictFactory);
        }

        private static bool IsCurrentEntityDictionary(object entity, ConcurrentDictionary<Type, object> entityDict)
        {
            return EntityData.TryGetValue(entity, out var current) && ReferenceEquals(current, entityDict);
        }

        private static bool TryRemoveEntityDictionary(object entity, ConcurrentDictionary<Type, object> entityDict)
        {
            var entries = (System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object, ConcurrentDictionary<Type, object>>>)EntityData;
            return entries.Remove(new System.Collections.Generic.KeyValuePair<object, ConcurrentDictionary<Type, object>>(entity, entityDict));
        }
    }
}
