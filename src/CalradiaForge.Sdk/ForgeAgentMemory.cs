using System;
using System.Collections.Generic;

namespace CalradiaForge.Sdk
{
    /// <summary>Describes why a typed memory read did or did not return a value.</summary>
    public enum ForgeMemoryReadState
    {
        Found,
        Missing,
        Expired,
        TypeMismatch
    }

    /// <summary>Immutable outcome for a typed semantic or procedural memory read.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    public sealed class ForgeMemoryReadResult<T>
    {
        internal ForgeMemoryReadResult(ForgeMemoryReadState state, T value)
        {
            State = state;
            Value = value;
        }

        /// <summary>Gets the state of the read.</summary>
        public ForgeMemoryReadState State { get; }

        /// <summary>Gets whether the requested value was found and had the requested type.</summary>
        public bool HasValue => State == ForgeMemoryReadState.Found;

        /// <summary>Gets the value when <see cref="HasValue"/> is true; otherwise the type default.</summary>
        public T Value { get; }
    }

    /// <summary>
    /// Provides bounded semantic, episodic, and procedural memory for Bannerlord NPCs.
    /// Its tiers are conceptually analogous to parts of CoALA; this service is not a complete language-agent runtime.
    /// </summary>
    public static class ForgeAgentMemory
    {
        public const int MaximumAgents = 2048;
        public const int MaximumSemanticEntriesPerAgent = 128;
        public const int MaximumEpisodicEntriesPerAgent = 512;
        public const int MaximumEpisodicEntriesPerType = 128;
        public const int MaximumProceduralEntriesPerAgent = 128;

        // Every tier and the global agent registry use this one lock. No method acquires
        // a tier lock before or after this lock, so cross-tier capacity checks cannot deadlock.
        private static readonly object SyncRoot = new object();
        private static readonly HashSet<string> RegisteredAgents =
            new HashSet<string>(StringComparer.Ordinal);

        public static SemanticMemory Semantic { get; } = new SemanticMemory();
        public static EpisodicMemory Episodic { get; } = new EpisodicMemory();
        public static ProceduralMemory Procedural { get; } = new ProceduralMemory();

        private static bool TryRegisterAgentLocked(string agentId)
        {
            if (RegisteredAgents.Contains(agentId))
            {
                return true;
            }

            if (RegisteredAgents.Count >= MaximumAgents)
            {
                // TTL is lazy, so purge expired facts before rejecting a new global agent.
                Semantic.PurgeExpiredAllLocked(DateTimeOffset.UtcNow);
                if (RegisteredAgents.Count >= MaximumAgents) return false;
            }

            RegisteredAgents.Add(agentId);
            return true;
        }

        private static void RemoveAgentIfEmptyLocked(string agentId)
        {
            if (!Semantic.HasAgentLocked(agentId) &&
                !Episodic.HasAgentLocked(agentId) &&
                !Procedural.HasAgentLocked(agentId))
            {
                RegisteredAgents.Remove(agentId);
            }
        }

        private static bool TryCastValue<T>(object value, out T typedValue)
        {
            if (value is T matchedValue)
            {
                typedValue = matchedValue;
                return true;
            }

            if (value == null && ReferenceEquals(default(T), null))
            {
                typedValue = default(T);
                return true;
            }

            typedValue = default(T);
            return false;
        }

        private static bool TryGetStoreBucket<T>(
            Dictionary<string, Dictionary<string, T>> store,
            string agentId,
            string key,
            int maximumEntries,
            out Dictionary<string, T> entries,
            out bool keyExists)
        {
            bool hasEntries = store.TryGetValue(agentId, out entries);
            keyExists = hasEntries && entries.ContainsKey(key);
            if (keyExists) return true;
            if (hasEntries && entries.Count >= maximumEntries) return false;
            if (!TryRegisterAgentLocked(agentId)) return false;
            if (hasEntries) return true;

            entries = new Dictionary<string, T>(StringComparer.Ordinal);
            store.Add(agentId, entries);
            return true;
        }

        private static ForgeMemoryReadResult<T> CreateReadResult<T>(bool found, object value, bool expired)
        {
            if (!found) return new ForgeMemoryReadResult<T>(ForgeMemoryReadState.Missing, default(T));
            if (expired) return new ForgeMemoryReadResult<T>(ForgeMemoryReadState.Expired, default(T));
            return TryCastValue(value, out T typedValue)
                ? new ForgeMemoryReadResult<T>(ForgeMemoryReadState.Found, typedValue)
                : new ForgeMemoryReadResult<T>(ForgeMemoryReadState.TypeMismatch, default(T));
        }

        private static IReadOnlyList<string> GetKeysLocked<T>(
            Dictionary<string, Dictionary<string, T>> store, string agentId)
        {
            if (!store.TryGetValue(agentId, out var entries)) return Array.Empty<string>();
            var keys = new string[entries.Count];
            entries.Keys.CopyTo(keys, 0);
            return keys;
        }

        private static int CountEntriesLocked<T>(Dictionary<string, Dictionary<string, T>> store)
        {
            int count = 0;
            foreach (Dictionary<string, T> entries in store.Values) count += entries.Count;
            return count;
        }

        /// <summary>Remove one agent's data from all three tiers and release its global slot.</summary>
        public static void ClearAgent(string agentId)
        {
            if (agentId == null) throw new ArgumentNullException(nameof(agentId));
            lock (SyncRoot)
            {
                Semantic.ClearAgentLocked(agentId);
                Episodic.ClearAgentLocked(agentId);
                Procedural.ClearAgentLocked(agentId);
                RegisteredAgents.Remove(agentId);
            }
        }

        /// <summary>Clear every tier and the global agent registry.</summary>
        public static void ClearAll()
        {
            lock (SyncRoot)
            {
                Semantic.ClearAllLocked();
                Episodic.ClearAllLocked();
                Procedural.ClearAllLocked();
                RegisteredAgents.Clear();
            }
        }

        /// <summary>Gets aggregate memory counts without exposing agent identities or payloads.</summary>
        public static void GetStatistics(
            out int agentCount,
            out int semanticEntryCount,
            out int episodicEntryCount,
            out int proceduralEntryCount)
        {
            lock (SyncRoot)
            {
                Semantic.PurgeExpiredAllLocked(DateTimeOffset.UtcNow);
                agentCount = RegisteredAgents.Count;
                semanticEntryCount = Semantic.GetEntryCountLocked();
                episodicEntryCount = Episodic.GetEntryCountLocked();
                proceduralEntryCount = Procedural.GetEntryCountLocked();
            }
        }

        /// <summary>Gets the total number of registered cognitive agents.</summary>
        public static int RegisteredAgentsCount
        {
            get
            {
                lock (SyncRoot)
                {
                    return RegisteredAgents.Count;
                }
            }
        }

        // ─── Semantic Memory: facts and preferences ────────────────────────────

        public class SemanticMemory
        {
            private sealed class Entry
            {
                public object Value;
                public DateTimeOffset ExpiresAt;
                public bool HasTtl;
            }

            // Public tier instances remain source-compatible, but share the process-wide
            // bounded store so the global ID quota and ClearAgent lifecycle cannot be bypassed.
            private static readonly Dictionary<string, Dictionary<string, Entry>> _data =
                new Dictionary<string, Dictionary<string, Entry>>(StringComparer.Ordinal);

            /// <summary>Store or update a fact for an agent. No TTL — lives until cleared.</summary>
            public void Upsert(string agentId, string key, object content)
            {
                Upsert(agentId, key, content, null);
            }

            /// <summary>Store or update a fact with an optional TTL. Negative TTL = already expired.</summary>
            public void Upsert(string agentId, string key, object content, TimeSpan? ttl)
            {
                if (!TryUpsert(agentId, key, content, ttl))
                {
                    throw new InvalidOperationException(
                        "Semantic memory capacity reached for this agent or the global agent limit.");
                }
            }

            /// <summary>
            /// Try to store or update a fact. Returns false when a new key or agent exceeds a quota.
            /// Updating an existing key remains allowed at the per-agent limit.
            /// </summary>
            public bool TryUpsert(string agentId, string key, object content)
            {
                return TryUpsert(agentId, key, content, null);
            }

            /// <summary>Try to store or update a fact with an optional TTL.</summary>
            public bool TryUpsert(string agentId, string key, object content, TimeSpan? ttl)
            {
                if (agentId == null) throw new ArgumentNullException(nameof(agentId));
                if (key == null) throw new ArgumentNullException(nameof(key));

                lock (SyncRoot)
                {
                    PurgeExpiredLocked(agentId, DateTimeOffset.UtcNow);
                    if (!TryGetStoreBucket(_data, agentId, key, MaximumSemanticEntriesPerAgent,
                        out Dictionary<string, Entry> facts, out bool keyExists)) return false;
                    if (keyExists) facts[key] = CreateEntry(content, ttl);
                    else facts.Add(key, CreateEntry(content, ttl));
                    return true;
                }
            }

            /// <summary>Retrieve a fact. Returns default and removes entries whose TTL expired.</summary>
            public T Get<T>(string agentId, string key)
            {
                return GetResult<T>(agentId, key).Value;
            }

            /// <summary>Retrieve a fact and distinguish missing, expired, and incompatible values.</summary>
            public ForgeMemoryReadResult<T> GetResult<T>(string agentId, string key)
            {
                if (agentId == null || key == null)
                {
                    return new ForgeMemoryReadResult<T>(ForgeMemoryReadState.Missing, default(T));
                }

                lock (SyncRoot)
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    Dictionary<string, Entry> facts;
                    Entry entry = null;
                    bool hasEntry = _data.TryGetValue(agentId, out facts) && facts.TryGetValue(key, out entry);
                    bool expired = hasEntry && entry.HasTtl && now >= entry.ExpiresAt;
                    ForgeMemoryReadResult<T> result = CreateReadResult<T>(hasEntry, entry?.Value, expired);

                    // Preserve the existing lazy cleanup behavior for every expired key owned by
                    // this agent while retaining the requested key's pre-purge outcome above.
                    PurgeExpiredLocked(agentId, now);
                    return result;
                }
            }

            /// <summary>Return all non-expired fact keys for an agent.</summary>
            public IReadOnlyList<string> GetKeys(string agentId)
            {
                if (agentId == null) return Array.Empty<string>();
                lock (SyncRoot)
                {
                    PurgeExpiredLocked(agentId, DateTimeOffset.UtcNow);
                    return GetKeysLocked(_data, agentId);
                }
            }

            public void ClearAgent(string agentId)
            {
                if (agentId == null) throw new ArgumentNullException(nameof(agentId));
                lock (SyncRoot)
                {
                    ClearAgentLocked(agentId);
                    RemoveAgentIfEmptyLocked(agentId);
                }
            }

            private static Entry CreateEntry(object content, TimeSpan? ttl)
            {
                DateTimeOffset expiresAt = DateTimeOffset.MaxValue;
                if (ttl.HasValue)
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    TimeSpan duration = ttl.Value;

                    // A non-positive TTL is already expired. Avoid adding extreme negative
                    // durations, which can underflow DateTimeOffset even though they are valid
                    // requests for an immediately expired entry.
                    if (duration <= TimeSpan.Zero)
                    {
                        expiresAt = now;
                    }
                    else
                    {
                        TimeSpan remaining = DateTimeOffset.MaxValue - now;
                        expiresAt = duration >= remaining
                            ? DateTimeOffset.MaxValue
                            : now + duration;
                    }
                }

                return new Entry
                {
                    Value = content,
                    HasTtl = ttl.HasValue,
                    ExpiresAt = expiresAt
                };
            }

            private void PurgeExpiredLocked(string agentId, DateTimeOffset now)
            {
                Dictionary<string, Entry> facts;
                if (!_data.TryGetValue(agentId, out facts)) return;

                List<string> expiredKeys = null;
                foreach (KeyValuePair<string, Entry> pair in facts)
                {
                    if (pair.Value.HasTtl && now >= pair.Value.ExpiresAt)
                    {
                        if (expiredKeys == null) expiredKeys = new List<string>();
                        expiredKeys.Add(pair.Key);
                    }
                }

                if (expiredKeys != null)
                {
                    for (int i = 0; i < expiredKeys.Count; i++)
                    {
                        facts.Remove(expiredKeys[i]);
                    }
                }

                if (facts.Count == 0)
                {
                    _data.Remove(agentId);
                    RemoveAgentIfEmptyLocked(agentId);
                }
            }

            internal void PurgeExpiredAllLocked(DateTimeOffset now)
            {
                if (_data.Count == 0) return;
                var agentIds = new List<string>(_data.Keys);
                for (int i = 0; i < agentIds.Count; i++)
                {
                    PurgeExpiredLocked(agentIds[i], now);
                }
            }

            internal bool HasAgentLocked(string agentId)
            {
                return _data.ContainsKey(agentId);
            }

            internal int GetEntryCountLocked()
            {
                return CountEntriesLocked(_data);
            }

            internal void ClearAgentLocked(string agentId)
            {
                _data.Remove(agentId);
            }

            internal void ClearAllLocked()
            {
                _data.Clear();
            }
        }

        // ─── Episodic Memory: timestamped events and experiences ───────────────

        public class EpisodicMemory
        {
            public const int MaximumEpisodesPerType = MaximumEpisodicEntriesPerType;

            private sealed class Episode
            {
                public string Type;
                public object Payload;
            }

            private static readonly Dictionary<string, List<Episode>> _data =
                new Dictionary<string, List<Episode>>(StringComparer.Ordinal);

            /// <summary>Record an experience, evicting the oldest episodes when a quota is reached.</summary>
            public void Add(string agentId, string type, object content)
            {
                if (!TryAdd(agentId, type, content))
                {
                    throw new InvalidOperationException(
                        "Episodic memory could not accept the entry because the global agent limit was reached.");
                }
            }

            /// <summary>Try to record an experience. Returns false if the global agent limit is reached.</summary>
            public bool TryAdd(string agentId, string type, object content)
            {
                if (agentId == null) throw new ArgumentNullException(nameof(agentId));
                if (type == null) throw new ArgumentNullException(nameof(type));

                lock (SyncRoot)
                {
                    List<Episode> episodes;
                    if (!_data.TryGetValue(agentId, out episodes))
                    {
                        if (!TryRegisterAgentLocked(agentId)) return false;
                        episodes = new List<Episode>();
                        _data.Add(agentId, episodes);
                    }

                    episodes.Add(new Episode { Type = type, Payload = content });

                    // First enforce the global FIFO bound, then the incoming type's FIFO bound.
                    while (episodes.Count > MaximumEpisodicEntriesPerAgent)
                    {
                        episodes.RemoveAt(0);
                    }

                    while (CountType(episodes, type) > MaximumEpisodicEntriesPerType)
                    {
                        int oldestOfType = FindOldestTypeIndex(episodes, type);
                        if (oldestOfType >= 0 && oldestOfType < episodes.Count)
                        {
                            episodes.RemoveAt(oldestOfType);
                        }
                        else
                        {
                            break;
                        }
                    }

                    return true;
                }
            }

            /// <summary>Retrieve all recorded experiences of a given type for an agent.</summary>
            public List<object> GetAll(string agentId, string type)
            {
                if (agentId == null || type == null) return new List<object>();
                lock (SyncRoot)
                {
                    List<Episode> episodes;
                    if (!_data.TryGetValue(agentId, out episodes)) return new List<object>();

                    var result = new List<object>(Math.Min(episodes.Count, MaximumEpisodicEntriesPerType));
                    for (int i = 0; i < episodes.Count; i++)
                    {
                        if (string.Equals(episodes[i].Type, type, StringComparison.Ordinal))
                        {
                            result.Add(episodes[i].Payload);
                        }
                    }
                    return result;
                }
            }

            /// <summary>Count episodes of a specific type. Returns 0 if none recorded.</summary>
            public int Count(string agentId, string type)
            {
                if (agentId == null || type == null) return 0;
                lock (SyncRoot)
                {
                    List<Episode> episodes;
                    return _data.TryGetValue(agentId, out episodes) ? CountType(episodes, type) : 0;
                }
            }

            /// <summary>Count total episodes across all types for an agent.</summary>
            public int TotalCount(string agentId)
            {
                if (agentId == null) return 0;
                lock (SyncRoot)
                {
                    List<Episode> episodes;
                    return _data.TryGetValue(agentId, out episodes) ? episodes.Count : 0;
                }
            }

            public void ClearAgent(string agentId)
            {
                if (agentId == null) throw new ArgumentNullException(nameof(agentId));
                lock (SyncRoot)
                {
                    ClearAgentLocked(agentId);
                    RemoveAgentIfEmptyLocked(agentId);
                }
            }

            private static int CountType(List<Episode> episodes, string type)
            {
                int count = 0;
                for (int i = 0; i < episodes.Count; i++)
                {
                    if (string.Equals(episodes[i].Type, type, StringComparison.Ordinal)) count++;
                }
                return count;
            }

            private static int FindOldestTypeIndex(List<Episode> episodes, string type)
            {
                for (int i = 0; i < episodes.Count; i++)
                {
                    if (string.Equals(episodes[i].Type, type, StringComparison.Ordinal)) return i;
                }
                return -1;
            }

            internal bool HasAgentLocked(string agentId)
            {
                return _data.ContainsKey(agentId);
            }

            internal int GetEntryCountLocked()
            {
                int count = 0;
                foreach (List<Episode> episodes in _data.Values)
                {
                    count += episodes.Count;
                }
                return count;
            }

            internal void ClearAgentLocked(string agentId)
            {
                _data.Remove(agentId);
            }

            internal void ClearAllLocked()
            {
                _data.Clear();
            }
        }

        // ─── Procedural Memory: skills, tactics, and how-to knowledge ─────────

        public class ProceduralMemory
        {
            private static readonly Dictionary<string, Dictionary<string, object>> _data =
                new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);

            /// <summary>Store or update a skill or tactic for an agent.</summary>
            public void Add(string agentId, string task, object instructions)
            {
                if (!TryAdd(agentId, task, instructions))
                {
                    throw new InvalidOperationException(
                        "Procedural memory capacity reached for this agent or the global agent limit.");
                }
            }

            /// <summary>
            /// Try to store or update instructions. Updating an existing task remains allowed at capacity.
            /// </summary>
            public bool TryAdd(string agentId, string task, object instructions)
            {
                if (agentId == null) throw new ArgumentNullException(nameof(agentId));
                if (task == null) throw new ArgumentNullException(nameof(task));

                lock (SyncRoot)
                {
                    if (!TryGetStoreBucket(_data, agentId, task, MaximumProceduralEntriesPerAgent,
                        out Dictionary<string, object> tasks, out bool keyExists)) return false;
                    if (keyExists) tasks[task] = instructions;
                    else tasks.Add(task, instructions);
                    return true;
                }
            }

            /// <summary>Retrieve stored skill/tactic instructions for an agent.</summary>
            public T Get<T>(string agentId, string task)
            {
                return GetResult<T>(agentId, task).Value;
            }

            /// <summary>Retrieve instructions and distinguish missing or incompatible values.</summary>
            public ForgeMemoryReadResult<T> GetResult<T>(string agentId, string task)
            {
                if (agentId == null || task == null)
                {
                    return new ForgeMemoryReadResult<T>(ForgeMemoryReadState.Missing, default(T));
                }

                lock (SyncRoot)
                {
                    Dictionary<string, object> tasks;
                    object value = null;
                    bool found = _data.TryGetValue(agentId, out tasks) && tasks.TryGetValue(task, out value);
                    return CreateReadResult<T>(found, value, expired: false);
                }
            }

            /// <summary>Return all known task names for an agent.</summary>
            public IReadOnlyList<string> GetTaskNames(string agentId)
            {
                if (agentId == null) return Array.Empty<string>();
                lock (SyncRoot) return GetKeysLocked(_data, agentId);
            }

            public void ClearAgent(string agentId)
            {
                if (agentId == null) throw new ArgumentNullException(nameof(agentId));
                lock (SyncRoot)
                {
                    ClearAgentLocked(agentId);
                    RemoveAgentIfEmptyLocked(agentId);
                }
            }

            internal bool HasAgentLocked(string agentId)
            {
                return _data.ContainsKey(agentId);
            }

            internal int GetEntryCountLocked()
            {
                return CountEntriesLocked(_data);
            }

            internal void ClearAgentLocked(string agentId)
            {
                _data.Remove(agentId);
            }

            internal void ClearAllLocked()
            {
                _data.Clear();
            }
        }
    }
}
