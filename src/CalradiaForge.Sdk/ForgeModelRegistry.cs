using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CalradiaForge.Sdk
{
    public enum ForgeGameModelCategory
    {
        PartySpeed,
        PartyWage,
        PartySizeLimit,
        SettlementProsperity,
        SettlementLoyalty,
        SettlementFood,
        ItemValue,
        TroopUpgradeXp
    }

    public sealed class ForgeModelModifier
    {
        public string Id { get; }
        public ForgeGameModelCategory Category { get; }
        public string SourceModule { get; }
        public string Description { get; }
        public float AdditiveBonus { get; }
        public float FactorMultiplier { get; }
        public Func<object, bool> Condition { get; }

        public ForgeModelModifier(
            string id,
            ForgeGameModelCategory category,
            string sourceModule,
            string description,
            float additiveBonus,
            float factorMultiplier,
            Func<object, bool> condition = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Modifier ID cannot be null or empty.", nameof(id));
            Id = id;
            Category = category;
            SourceModule = sourceModule ?? "Core";
            Description = description ?? id;
            AdditiveBonus = additiveBonus;
            FactorMultiplier = factorMultiplier;
            Condition = condition;
        }
    }

    public sealed class ForgeModelCalculationResult
    {
        public float InitialValue { get; }
        public float FinalValue { get; }
        public float TotalAdditive { get; }
        public float TotalFactors { get; }
        public List<string> AppliedModifiers { get; }

        public ForgeModelCalculationResult(float initial, float finalVal, float adds, float factors, IEnumerable<string> applied)
        {
            InitialValue = initial;
            FinalValue = finalVal;
            TotalAdditive = adds;
            TotalFactors = factors;
            AppliedModifiers = CopyAppliedModifiers(applied);
        }

        static List<string> CopyAppliedModifiers(IEnumerable<string> applied)
        {
            return applied == null ? new List<string>() : new List<string>(applied);
        }
    }

    /// <summary>
    /// Declarative GameModel Decorator Registry.
    /// Implements TaleWorlds ExplainedNumber math without requiring Harmony patches:
    /// Final = (Base + Sum(Adds)) * (1.0 + Sum(Factors))
    /// </summary>
    public sealed class ForgeModelRegistry
    {
        private static readonly ReadOnlyCollection<ForgeModelModifier> EmptyCategorySnapshot =
            new ReadOnlyCollection<ForgeModelModifier>(new List<ForgeModelModifier>());
        private readonly object _sync = new object();
        private readonly List<Registration> _modifiers = new List<Registration>();
        private readonly Dictionary<ForgeGameModelCategory, ReadOnlyCollection<ForgeModelModifier>> _categorySnapshots =
            new Dictionary<ForgeGameModelCategory, ReadOnlyCollection<ForgeModelModifier>>();
        private long _nextGeneration;

        /// <summary>
        /// Registers a modifier using the legacy, registry-wide lifetime. Registering an existing ID replaces it.
        /// </summary>
        public void Register(ForgeModelModifier modifier)
        {
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            lock (_sync)
            {
                Replace(modifier, null);
            }
        }

        /// <summary>
        /// Starts a disposable registration scope for one module. A scope can remove only the modifier
        /// generations it currently owns; later registrations that replace them are left untouched.
        /// </summary>
        public ForgeModelRegistrationScope BeginOwnerScope(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner ID cannot be null or empty.", nameof(ownerId));
            return new ForgeModelRegistrationScope(this, ownerId);
        }

        public bool Unregister(string modifierId)
        {
            if (string.IsNullOrEmpty(modifierId)) return false;
            lock (_sync)
            {
                bool removed = _modifiers.RemoveAll(entry => entry.Modifier.Id == modifierId) > 0;
                if (removed) _categorySnapshots.Clear();
                return removed;
            }
        }

        public IReadOnlyList<ForgeModelModifier> GetModifiers(ForgeGameModelCategory category)
        {
            // The public enum can be supplied through an arbitrary cast. Do not let undefined
            // values create unbounded cache entries; the defined categories are contiguous.
            int categoryIndex = (int)category;
            if (categoryIndex < (int)ForgeGameModelCategory.PartySpeed ||
                categoryIndex > (int)ForgeGameModelCategory.TroopUpgradeXp)
            {
                return EmptyCategorySnapshot;
            }

            lock (_sync)
            {
                if (_categorySnapshots.TryGetValue(category, out ReadOnlyCollection<ForgeModelModifier> snapshot))
                {
                    return snapshot;
                }

                var matches = new List<ForgeModelModifier>();
                foreach (Registration entry in _modifiers)
                {
                    if (entry.Modifier.Category == category) matches.Add(entry.Modifier);
                }

                snapshot = new ReadOnlyCollection<ForgeModelModifier>(matches);
                _categorySnapshots.Add(category, snapshot);
                return snapshot;
            }
        }

        public IReadOnlyList<ForgeModelModifier> GetAllModifiers()
        {
            lock (_sync)
            {
                var snapshot = new List<ForgeModelModifier>(_modifiers.Count);
                foreach (Registration entry in _modifiers)
                {
                    snapshot.Add(entry.Modifier);
                }
                return new ReadOnlyCollection<ForgeModelModifier>(snapshot);
            }
        }

        /// <summary>
        /// Evaluates registered modifiers for a given category following Bannerlord ExplainedNumber mathematics:
        /// Result = (Base + Adds) * (1.0 + Factors). Modifier conditions run on a stable snapshot outside the registry lock.
        /// </summary>
        public ForgeModelCalculationResult Evaluate(
            ForgeGameModelCategory category,
            float baseValue,
            object context = null,
            float minLimit = float.MinValue,
            float maxLimit = float.MaxValue)
        {
            IReadOnlyList<ForgeModelModifier> snapshot = GetModifiers(category);

            float totalAdds = 0f;
            float totalFactors = 0f;
            var applied = new List<string>();
            foreach (ForgeModelModifier modifier in snapshot)
            {
                if (modifier.Condition != null && !modifier.Condition(context)) continue;

                totalAdds += modifier.AdditiveBonus;
                totalFactors += modifier.FactorMultiplier;
                applied.Add($"{modifier.Id} (Adds: {modifier.AdditiveBonus:F2}, Factor: {modifier.FactorMultiplier:+0.0%;-0.0%;+0%})");
            }

            // ExplainedNumber formula: factors are additive with each other (non-compounding)
            float calculated = (baseValue + totalAdds) * (1.0f + totalFactors);

            if (calculated < minLimit) calculated = minLimit;
            if (calculated > maxLimit) calculated = maxLimit;

            return new ForgeModelCalculationResult(baseValue, calculated, totalAdds, totalFactors, applied);
        }

        private long Replace(ForgeModelModifier modifier, object ownerToken)
        {
            long generation = checked(_nextGeneration + 1);
            _modifiers.RemoveAll(entry => entry.Modifier.Id == modifier.Id);
            _modifiers.Add(new Registration(modifier, ownerToken, generation));
            _categorySnapshots.Clear();
            _nextGeneration = generation;
            return generation;
        }

        internal long RegisterOwned(ForgeModelRegistrationScope scope, ForgeModelModifier modifier)
        {
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            if (!string.Equals(modifier.SourceModule, scope.OwnerId, StringComparison.Ordinal))
            {
                throw new ArgumentException("Modifier source module must exactly match the registration scope owner.", nameof(modifier));
            }

            lock (_sync)
            {
                return Replace(modifier, scope.OwnerToken);
            }
        }

        internal void UnregisterOwned(object ownerToken, Dictionary<string, long> generations)
        {
            lock (_sync)
            {
                int removed = _modifiers.RemoveAll(entry =>
                    ReferenceEquals(entry.OwnerToken, ownerToken) &&
                    generations.TryGetValue(entry.Modifier.Id, out long generation) &&
                    entry.Generation == generation);
                if (removed > 0) _categorySnapshots.Clear();
            }
        }

        private sealed class Registration
        {
            public ForgeModelModifier Modifier { get; }
            public object OwnerToken { get; }
            public long Generation { get; }

            public Registration(ForgeModelModifier modifier, object ownerToken, long generation)
            {
                Modifier = modifier;
                OwnerToken = ownerToken;
                Generation = generation;
            }
        }

    }

    /// <summary>
    /// Owns the current modifier registrations for one module and removes only those generations when disposed.
    /// </summary>
    public sealed class ForgeModelRegistrationScope : IDisposable
    {
        private readonly ForgeModelRegistry _registry;
        private readonly Dictionary<string, long> _generations = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly object _sync = new object();
        private bool _disposed;

        internal object OwnerToken { get; } = new object();
        public string OwnerId { get; }

        internal ForgeModelRegistrationScope(ForgeModelRegistry registry, string ownerId)
        {
            _registry = registry;
            OwnerId = ownerId;
        }

        /// <summary>
        /// Registers or replaces a modifier owned by this scope. SourceModule must match OwnerId exactly.
        /// </summary>
        public void Register(ForgeModelModifier modifier)
        {
            lock (_sync)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(ForgeModelRegistrationScope));
                if (modifier == null) throw new ArgumentNullException(nameof(modifier));
                if (!string.Equals(modifier.SourceModule, OwnerId, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Modifier source module must exactly match the registration scope owner.", nameof(modifier));
                }
                _generations[modifier.Id] = _registry.RegisterOwned(this, modifier);
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed) return;
                _disposed = true;
                _registry.UnregisterOwned(OwnerToken, new Dictionary<string, long>(_generations, StringComparer.Ordinal));
                _generations.Clear();
            }
        }
    }
}
