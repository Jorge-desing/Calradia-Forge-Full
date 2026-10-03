using System;
using System.Collections.Generic;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Deterministic time-slicing helper for Bannerlord CampaignBehaviors.
    /// Assigns entity identifiers to buckets so callers can spread eligible work across updates.
    /// Distribution depends on the identifiers and does not guarantee even workloads or eliminate stutter.
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
            int normalizedHour = NormalizeHour(currentHour, totalBuckets);
            return GetBucket(entityId, totalBuckets) == normalizedHour;
        }

        private static int NormalizeHour(int currentHour, int totalBuckets)
        {
            int normalizedHour = currentHour % totalBuckets;
            return normalizedHour < 0 ? normalizedHour + totalBuckets : normalizedHour;
        }

        /// <summary>
        /// Processes entities assigned to the current hour's bucket without a LINQ pipeline.
        /// Uses indexed access; collection, selector, or processor implementations may still allocate.
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

            int targetBucket = NormalizeHour(currentHour, totalBuckets);

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
        /// Processes entities assigned to the current hour's bucket without a LINQ pipeline.
        /// The sequence enumerator, selector, or processor implementations may allocate.
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

            int targetBucket = NormalizeHour(currentHour, totalBuckets);

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
