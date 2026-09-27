using System;
using System.Collections.Generic;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Anti-Lag Time-Slicing Engine for Bannerlord CampaignBehaviors.
    /// Distributes entity updates (heroes, settlements, caravans) evenly across hourly ticks
    /// using a deterministic 24-bucket hash algorithm to eliminate the "Midnight Freeze" stutter.
    /// </summary>
    public static class ForgeTimeSlicer
    {
        public const int DefaultHourlyBuckets = 24;

        /// <summary>
        /// Computes the deterministic hourly bucket (0 to totalBuckets - 1) for a given entity StringId.
        /// </summary>
        public static int GetBucket(string entityId, int totalBuckets = DefaultHourlyBuckets)
        {
            if (string.IsNullOrEmpty(entityId)) return 0;
            if (totalBuckets <= 1) return 0;

            // Use stable 32-bit positive hash modulo bucket count
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < entityId.Length; i++)
                {
                    hash = hash * 31 + entityId[i];
                }
                return (hash & 0x7FFFFFFF) % totalBuckets;
            }
        }

        /// <summary>
        /// Determines whether an entity scheduled for time-slicing should process during the specified hour.
        /// </summary>
        public static bool ShouldProcess(string entityId, int currentHour, int totalBuckets = DefaultHourlyBuckets)
        {
            if (totalBuckets <= 1) return true;
            int normalizedHour = ((currentHour % totalBuckets) + totalBuckets) % totalBuckets;
            return GetBucket(entityId, totalBuckets) == normalizedHour;
        }

        /// <summary>
        /// Processes only the subset of entities assigned to the current hour's bucket with zero LINQ allocations.
        /// Optimized for indexable collections (List, T[], MBReadOnlyList) with zero enumerator allocations.
        /// </summary>
        public static int ProcessBatch<T>(
            IReadOnlyList<T> entities,
            Func<T, string> idSelector,
            Action<T> processor,
            int currentHour,
            int totalBuckets = DefaultHourlyBuckets)
        {
            if (entities == null || idSelector == null || processor == null) return 0;

            int processedCount = 0;
            if (totalBuckets <= 1)
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    T entity = entities[i];
                    if (entity == null) continue;
                    processor(entity);
                    processedCount++;
                }
                return processedCount;
            }

            int targetBucket = ((currentHour % totalBuckets) + totalBuckets) % totalBuckets;

            for (int i = 0; i < entities.Count; i++)
            {
                T entity = entities[i];
                if (entity == null) continue;
                string id = idSelector(entity);
                if (GetBucket(id, totalBuckets) == targetBucket)
                {
                    processor(entity);
                    processedCount++;
                }
            }

            return processedCount;
        }

        /// <summary>
        /// Processes only the subset of entities assigned to the current hour's bucket with zero LINQ allocations.
        /// </summary>
        public static int ProcessBatch<T>(
            IEnumerable<T> entities,
            Func<T, string> idSelector,
            Action<T> processor,
            int currentHour,
            int totalBuckets = DefaultHourlyBuckets)
        {
            if (entities == null || idSelector == null || processor == null) return 0;

            int processedCount = 0;
            if (totalBuckets <= 1)
            {
                foreach (T entity in entities)
                {
                    if (entity == null) continue;
                    processor(entity);
                    processedCount++;
                }
                return processedCount;
            }

            int targetBucket = ((currentHour % totalBuckets) + totalBuckets) % totalBuckets;

            foreach (T entity in entities)
            {
                if (entity == null) continue;
                string id = idSelector(entity);
                if (GetBucket(id, totalBuckets) == targetBucket)
                {
                    processor(entity);
                    processedCount++;
                }
            }

            return processedCount;
        }
    }
}
