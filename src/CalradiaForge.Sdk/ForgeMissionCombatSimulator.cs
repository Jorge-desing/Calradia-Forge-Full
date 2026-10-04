using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Type of physical weapon damage inflicted in combat.
    /// Follows Bannerlord damage classification and armor resistance curves.
    /// </summary>
    public enum CombatDamageType
    {
        Cut = 0,
        Pierce = 1,
        Blunt = 2
    }

    /// <summary>
    /// Tactical role of a soldier within a formation.
    /// </summary>
    public enum CombatTroopRole
    {
        Infantry = 0,
        Ranged = 1,
        Cavalry = 2,
        HorseArcher = 3
    }

    /// <summary>
    /// Current operational combat state of a soldier.
    /// </summary>
    public enum CombatSoldierState
    {
        Active = 0,
        Routed = 1,
        Unconscious = 2,
        Dead = 3
    }

    /// <summary>
    /// Outcome verdict of a combat mission simulation.
    /// </summary>
    public enum CombatVerdict
    {
        Team0Victory = 0,
        Team1Victory = 1,
        Draw = 2,
        Timeout = 3
    }

    /// <summary>
    /// Immutable specification of a soldier entering combat.
    /// </summary>
    public class CombatSoldierDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public int TeamId { get; }
        public CombatTroopRole Role { get; }
        public float MaxHitPoints { get; }
        public float MaxStamina { get; }
        public float InitialMorale { get; }
        public float BodyArmor { get; }
        public float HeadArmor { get; }
        public float LegArmor { get; }
        public float BaseDamage { get; }
        public CombatDamageType DamageType { get; }
        public int CombatSkill { get; }
        public float AttackSpeed { get; }

        public CombatSoldierDefinition(
            string id,
            string name,
            int teamId,
            CombatTroopRole role,
            float maxHitPoints = 100f,
            float maxStamina = 100f,
            float initialMorale = 70f,
            float bodyArmor = 35f,
            float headArmor = 30f,
            float legArmor = 25f,
            float baseDamage = 35f,
            CombatDamageType damageType = CombatDamageType.Cut,
            int combatSkill = 120,
            float attackSpeed = 1.0f)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Soldier ID cannot be null or empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Soldier name cannot be null or empty.", nameof(name));
            if (teamId < 0 || teamId > 1) throw new ArgumentOutOfRangeException(nameof(teamId), "TeamId must be 0 or 1.");
            if (maxHitPoints <= 0f) throw new ArgumentOutOfRangeException(nameof(maxHitPoints), "MaxHitPoints must be positive.");
            if (maxStamina <= 0f) throw new ArgumentOutOfRangeException(nameof(maxStamina), "MaxStamina must be positive.");

            Id = id;
            Name = name;
            TeamId = teamId;
            Role = role;
            MaxHitPoints = maxHitPoints;
            MaxStamina = maxStamina;
            InitialMorale = Math.Max(0f, Math.Min(100f, initialMorale));
            BodyArmor = Math.Max(0f, bodyArmor);
            HeadArmor = Math.Max(0f, headArmor);
            LegArmor = Math.Max(0f, legArmor);
            BaseDamage = Math.Max(1f, baseDamage);
            DamageType = damageType;
            CombatSkill = Math.Max(1, Math.Min(350, combatSkill));
            AttackSpeed = Math.Max(0.1f, Math.Min(3.0f, attackSpeed));
        }
    }

    /// <summary>
    /// Dynamic, mutable runtime state of a soldier actively participating in a simulation.
    /// </summary>
    public class CombatSoldierRuntime
    {
        public CombatSoldierDefinition Definition { get; }
        public float CurrentHitPoints { get; set; }
        public float CurrentStamina { get; set; }
        public float CurrentMorale { get; set; }
        public CombatSoldierState State { get; set; }

        public float TotalDamageDealt { get; set; }
        public float TotalDamageAbsorbed { get; set; }
        public int CasualtiesInflicted { get; set; }
        public int HitsLanded { get; set; }
        public int HitsTaken { get; set; }
        public float CooldownRemaining { get; set; }

        public bool IsActive => State == CombatSoldierState.Active;

        public CombatSoldierRuntime(CombatSoldierDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentHitPoints = definition.MaxHitPoints;
            CurrentStamina = definition.MaxStamina;
            CurrentMorale = definition.InitialMorale;
            State = CombatSoldierState.Active;
            CooldownRemaining = 0f;
        }

        public float AverageArmor => (Definition.HeadArmor * 0.2f) + (Definition.BodyArmor * 0.55f) + (Definition.LegArmor * 0.25f);
    }

    /// <summary>
    /// Discrete event logged during a combat mission tick.
    /// </summary>
    public class CombatMissionEvent
    {
        public long Tick { get; }
        public float TimestampSeconds { get; }
        public string EventType { get; }
        public string AttackerId { get; }
        public string DefenderId { get; }
        public float Value { get; }
        public string Details { get; }

        public CombatMissionEvent(long tick, float timestampSeconds, string eventType, string attackerId, string defenderId, float value, string details)
        {
            Tick = tick;
            TimestampSeconds = timestampSeconds;
            EventType = eventType;
            AttackerId = attackerId;
            DefenderId = defenderId;
            Value = value;
            Details = details;
        }

        public override string ToString() =>
            $"[{TimestampSeconds:F2}s | T{Tick}] {EventType}: {AttackerId} -> {DefenderId} (Val={Value:F1}) {Details}";
    }

    /// <summary>
    /// Lifecycle interface for combat mission components, mirroring Bannerlord's AgentComponent / MissionBehavior.
    /// </summary>
    public interface ICombatMissionComponent
    {
        void OnMissionStart(IReadOnlyList<CombatSoldierRuntime> allSoldiers, CombatSimulationScenario scenario);
        void OnMissionTick(float dt, long tick, IReadOnlyList<CombatSoldierRuntime> team0, IReadOnlyList<CombatSoldierRuntime> team1, List<CombatMissionEvent> eventLog);
        void OnAgentHealthChanged(CombatSoldierRuntime agent, float oldHealth, float newHealth, CombatSoldierRuntime attacker, List<CombatMissionEvent> eventLog);
        void OnAgentRemoved(CombatSoldierRuntime agent, CombatSoldierRuntime killer, List<CombatMissionEvent> eventLog);
    }

    /// <summary>
    /// Component simulating soldier stamina consumption during active attacks and passive recovery in downtime.
    /// </summary>
    public class StaminaManagementComponent : ICombatMissionComponent
    {
        public void OnMissionStart(IReadOnlyList<CombatSoldierRuntime> allSoldiers, CombatSimulationScenario scenario) { }

        public void OnMissionTick(float dt, long tick, IReadOnlyList<CombatSoldierRuntime> team0, IReadOnlyList<CombatSoldierRuntime> team1, List<CombatMissionEvent> eventLog)
        {
            // Passive recovery for all active soldiers
            foreach (var soldier in team0)
            {
                if (!soldier.IsActive) continue;
                TickSoldierStamina(soldier, dt);
            }

            foreach (var soldier in team1)
            {
                if (!soldier.IsActive) continue;
                TickSoldierStamina(soldier, dt);
            }
        }

        static void TickSoldierStamina(CombatSoldierRuntime soldier, float dt)
        {
            if (soldier.CooldownRemaining > 0.1f)
            {
                // In active attack recovery: minor stamina regen
                soldier.CurrentStamina = Math.Min(soldier.Definition.MaxStamina, soldier.CurrentStamina + (8f * dt));
            }
            else
            {
                // In rest: higher recovery
                soldier.CurrentStamina = Math.Min(soldier.Definition.MaxStamina, soldier.CurrentStamina + (18f * dt));
            }
        }

        public void OnAgentHealthChanged(CombatSoldierRuntime agent, float oldHealth, float newHealth, CombatSoldierRuntime attacker, List<CombatMissionEvent> eventLog) { }
        public void OnAgentRemoved(CombatSoldierRuntime agent, CombatSoldierRuntime killer, List<CombatMissionEvent> eventLog) { }
    }

    /// <summary>
    /// Component managing psychological morale shocks from sudden casualties and routed states.
    /// </summary>
    public class MoraleShockComponent : ICombatMissionComponent
    {
        public void OnMissionStart(IReadOnlyList<CombatSoldierRuntime> allSoldiers, CombatSimulationScenario scenario) { }

        public void OnMissionTick(float dt, long tick, IReadOnlyList<CombatSoldierRuntime> team0, IReadOnlyList<CombatSoldierRuntime> team1, List<CombatMissionEvent> eventLog)
        {
            // Check for morale routing below 15f threshold
            CheckRout(team0, tick, dt, eventLog);
            CheckRout(team1, tick, dt, eventLog);
        }

        static void CheckRout(IReadOnlyList<CombatSoldierRuntime> team, long tick, float dt, List<CombatMissionEvent> eventLog)
        {
            for (int i = 0; i < team.Count; i++)
            {
                var s = team[i];
                if (s.IsActive && s.CurrentMorale < 15f)
                {
                    s.State = CombatSoldierState.Routed;
                    eventLog.Add(new CombatMissionEvent(
                        tick,
                        tick * dt,
                        "MoraleRout",
                        s.Definition.Id,
                        string.Empty,
                        s.CurrentMorale,
                        $"{s.Definition.Name} broke formation and routed (Morale={s.CurrentMorale:F1})."));
                }
            }
        }

        public void OnAgentHealthChanged(CombatSoldierRuntime agent, float oldHealth, float newHealth, CombatSoldierRuntime attacker, List<CombatMissionEvent> eventLog)
        {
            // Heavy wound moral penalty
            if (newHealth < agent.Definition.MaxHitPoints * 0.35f && oldHealth >= agent.Definition.MaxHitPoints * 0.35f)
            {
                agent.CurrentMorale = Math.Max(0f, agent.CurrentMorale - 10f);
            }
        }

        public void OnAgentRemoved(CombatSoldierRuntime agent, CombatSoldierRuntime killer, List<CombatMissionEvent> eventLog)
        {
            // Morale shock to teammates
            // Handled during mission loop with team context
        }
    }

    /// <summary>
    /// Configuration scenario specifying participants and parameters for a combat simulation.
    /// </summary>
    public class CombatSimulationScenario
    {
        public string ScenarioName { get; set; } = "Standard Skirmish";
        public float DeltaTime { get; set; } = 0.05f;
        public float MaxDurationSeconds { get; set; } = 60f;
        public int RandomSeed { get; set; } = 12345;
        public List<CombatSoldierDefinition> Team0Troops { get; } = new List<CombatSoldierDefinition>();
        public List<CombatSoldierDefinition> Team1Troops { get; } = new List<CombatSoldierDefinition>();
        public List<ICombatMissionComponent> AdditionalComponents { get; } = new List<ICombatMissionComponent>();

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ScenarioName)) throw new InvalidOperationException("ScenarioName must be specified.");
            if (DeltaTime <= 0.001f || DeltaTime > 1.0f) throw new InvalidOperationException("DeltaTime must be between 0.001 and 1.0 seconds.");
            if (MaxDurationSeconds <= 0.1f) throw new InvalidOperationException("MaxDurationSeconds must be greater than 0.1.");
            if (Team0Troops == null || Team0Troops.Count == 0) throw new InvalidOperationException("Team0 must contain at least 1 soldier.");
            if (Team1Troops == null || Team1Troops.Count == 0) throw new InvalidOperationException("Team1 must contain at least 1 soldier.");
        }
    }

    /// <summary>
    /// Complete analytical result of a mission combat simulation.
    /// </summary>
    public class CombatSimulationResult
    {
        public string ScenarioName { get; set; }
        public CombatVerdict Verdict { get; set; }
        public long TotalTicks { get; set; }
        public float ElapsedTimeSeconds { get; set; }

        public int Team0InitialCount { get; set; }
        public int Team0Survivors { get; set; }
        public int Team0Casualties { get; set; }
        public int Team0Routed { get; set; }

        public int Team1InitialCount { get; set; }
        public int Team1Survivors { get; set; }
        public int Team1Casualties { get; set; }
        public int Team1Routed { get; set; }

        public float Team0TotalDamageDealt { get; set; }
        public float Team1TotalDamageDealt { get; set; }
        public float Team0AverageRemainingMorale { get; set; }
        public float Team1AverageRemainingMorale { get; set; }
        public float Team0AverageRemainingStamina { get; set; }
        public float Team1AverageRemainingStamina { get; set; }

        public IReadOnlyList<CombatMissionEvent> Events { get; set; }
    }

    /// <summary>
    /// Pure, deterministic C# combat mission simulation engine.
    /// Operates offline with zero external cloud dependencies, tickets, or rate limits.
    /// </summary>
    public static class ForgeMissionCombatSimulator
    {
        /// <summary>
        /// Calculates absorbed damage according to Bannerlord's non-linear armor mitigation formulas.
        /// Cut: Highest armor absorption (factor 1.0).
        /// Pierce: Superior armor penetration (factor 0.65).
        /// Blunt: Consistent concussive impact (factor 0.40).
        /// </summary>
        public static float CalculateAbsorbedDamage(float rawDamage, float armor, CombatDamageType damageType, int combatSkill)
        {
            if (rawDamage <= 0f) return 0f;

            float armorEffectiveness;
            switch (damageType)
            {
                case CombatDamageType.Pierce:
                    armorEffectiveness = 0.65f;
                    break;
                case CombatDamageType.Blunt:
                    armorEffectiveness = 0.40f;
                    break;
                case CombatDamageType.Cut:
                default:
                    armorEffectiveness = 1.00f;
                    break;
            }

            float effectiveArmor = Math.Max(0f, armor) * armorEffectiveness;
            // Hyperbolic absorption curve: Multiplier = 100 / (100 + EffectiveArmor)
            float absorptionMultiplier = 100f / (100f + effectiveArmor);

            // Skill scaling: each point of skill gives +0.2% damage bonus
            float skillBonus = 1.0f + (Math.Max(1, combatSkill) * 0.002f);

            return rawDamage * absorptionMultiplier * skillBonus;
        }

        /// <summary>
        /// Executes a full mission combat simulation deterministically.
        /// </summary>
        public static CombatSimulationResult Simulate(CombatSimulationScenario scenario, CancellationToken cancellationToken = default)
        {
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            scenario.Validate();

            var rand = new Random(scenario.RandomSeed);
            var eventLog = new List<CombatMissionEvent>(500);

            var team0 = new List<CombatSoldierRuntime>(scenario.Team0Troops.Count);
            foreach (var t in scenario.Team0Troops) team0.Add(new CombatSoldierRuntime(t));

            var team1 = new List<CombatSoldierRuntime>(scenario.Team1Troops.Count);
            foreach (var t in scenario.Team1Troops) team1.Add(new CombatSoldierRuntime(t));

            var allSoldiers = new List<CombatSoldierRuntime>(team0.Count + team1.Count);
            allSoldiers.AddRange(team0);
            allSoldiers.AddRange(team1);

            // Assemble components
            var components = new List<ICombatMissionComponent>
            {
                new StaminaManagementComponent(),
                new MoraleShockComponent()
            };
            if (scenario.AdditionalComponents != null && scenario.AdditionalComponents.Count > 0)
            {
                components.AddRange(scenario.AdditionalComponents);
            }

            foreach (var comp in components)
            {
                comp.OnMissionStart(allSoldiers, scenario);
            }

            long currentTick = 0;
            float elapsedTime = 0f;
            float dt = scenario.DeltaTime;
            long maxTicks = (long)Math.Ceiling(scenario.MaxDurationSeconds / dt);

            while (elapsedTime < scenario.MaxDurationSeconds && currentTick < maxTicks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                currentTick++;
                elapsedTime = currentTick * dt;

                // 1. Tick components
                for (int i = 0; i < components.Count; i++)
                {
                    components[i].OnMissionTick(dt, currentTick, team0, team1, eventLog);
                }

                // 2. Decrement attack cooldowns
                for (int i = 0; i < allSoldiers.Count; i++)
                {
                    var s = allSoldiers[i];
                    if (s.CooldownRemaining > 0f)
                    {
                        s.CooldownRemaining = Math.Max(0f, s.CooldownRemaining - dt);
                    }
                }

                // 3. Process combat actions for Team 0
                ExecuteTeamAttacks(team0, team1, rand, dt, currentTick, elapsedTime, eventLog, components);

                // 4. Process combat actions for Team 1
                ExecuteTeamAttacks(team1, team0, rand, dt, currentTick, elapsedTime, eventLog, components);

                // 5. Check battle termination conditions
                int active0 = CountActive(team0);
                int active1 = CountActive(team1);

                if (active0 == 0 || active1 == 0)
                {
                    break;
                }
            }

            // Compile final result
            int finalActive0 = CountActive(team0);
            int finalActive1 = CountActive(team1);
            int routed0 = team0.Count(s => s.State == CombatSoldierState.Routed);
            int routed1 = team1.Count(s => s.State == CombatSoldierState.Routed);
            int dead0 = team0.Count(s => s.State == CombatSoldierState.Dead || s.State == CombatSoldierState.Unconscious);
            int dead1 = team1.Count(s => s.State == CombatSoldierState.Dead || s.State == CombatSoldierState.Unconscious);

            CombatVerdict verdict;
            if (finalActive0 > 0 && finalActive1 == 0)
            {
                verdict = CombatVerdict.Team0Victory;
                eventLog.Add(new CombatMissionEvent(currentTick, elapsedTime, "Victory", "Team 0", "Team 1", finalActive0, "Team 0 achieved tactical supremacy."));
            }
            else if (finalActive1 > 0 && finalActive0 == 0)
            {
                verdict = CombatVerdict.Team1Victory;
                eventLog.Add(new CombatMissionEvent(currentTick, elapsedTime, "Victory", "Team 1", "Team 0", finalActive1, "Team 1 achieved tactical supremacy."));
            }
            else if (finalActive0 == 0 && finalActive1 == 0)
            {
                verdict = CombatVerdict.Draw;
            }
            else
            {
                verdict = CombatVerdict.Timeout;
            }

            float totalDmg0 = team0.Sum(s => s.TotalDamageDealt);
            float totalDmg1 = team1.Sum(s => s.TotalDamageDealt);

            float avgMorale0 = team0.Count > 0 ? team0.Average(s => s.CurrentMorale) : 0f;
            float avgMorale1 = team1.Count > 0 ? team1.Average(s => s.CurrentMorale) : 0f;
            float avgStamina0 = team0.Count > 0 ? team0.Average(s => s.CurrentStamina) : 0f;
            float avgStamina1 = team1.Count > 0 ? team1.Average(s => s.CurrentStamina) : 0f;

            return new CombatSimulationResult
            {
                ScenarioName = scenario.ScenarioName,
                Verdict = verdict,
                TotalTicks = currentTick,
                ElapsedTimeSeconds = elapsedTime,
                Team0InitialCount = team0.Count,
                Team0Survivors = finalActive0,
                Team0Casualties = dead0,
                Team0Routed = routed0,
                Team1InitialCount = team1.Count,
                Team1Survivors = finalActive1,
                Team1Casualties = dead1,
                Team1Routed = routed1,
                Team0TotalDamageDealt = totalDmg0,
                Team1TotalDamageDealt = totalDmg1,
                Team0AverageRemainingMorale = avgMorale0,
                Team1AverageRemainingMorale = avgMorale1,
                Team0AverageRemainingStamina = avgStamina0,
                Team1AverageRemainingStamina = avgStamina1,
                Events = eventLog
            };
        }

        static void ExecuteTeamAttacks(
            List<CombatSoldierRuntime> attackingTeam,
            List<CombatSoldierRuntime> defendingTeam,
            Random rand,
            float dt,
            long tick,
            float time,
            List<CombatMissionEvent> eventLog,
            List<ICombatMissionComponent> components)
        {
            var activeDefenders = new List<CombatSoldierRuntime>(defendingTeam.Count);
            for (int i = 0; i < defendingTeam.Count; i++)
            {
                if (defendingTeam[i].IsActive) activeDefenders.Add(defendingTeam[i]);
            }

            if (activeDefenders.Count == 0) return;

            for (int i = 0; i < attackingTeam.Count; i++)
            {
                var attacker = attackingTeam[i];
                if (!attacker.IsActive || attacker.CooldownRemaining > 0f) continue;

                // Pick target
                int targetIndex = rand.Next(activeDefenders.Count);
                var defender = activeDefenders[targetIndex];

                // Check hit chance based on attacker combat skill vs defender average armor/skill
                float hitChance = 0.55f + ((attacker.Definition.CombatSkill - defender.Definition.CombatSkill) * 0.0015f);
                hitChance = Math.Max(0.20f, Math.Min(0.95f, hitChance));

                // Stamina penalty: if attacker stamina is low (<25f), attack effectiveness drops
                float staminaFactor = attacker.CurrentStamina < 25f ? 0.70f : 1.0f;

                // Reset attack cooldown (1.0 / AttackSpeed)
                float baseInterval = 1.0f / Math.Max(0.1f, attacker.Definition.AttackSpeed);
                // Introduce slight variance (+- 15%)
                float variance = 0.85f + ((float)rand.NextDouble() * 0.30f);
                attacker.CooldownRemaining = baseInterval * variance;

                // Consume attacker stamina
                attacker.CurrentStamina = Math.Max(0f, attacker.CurrentStamina - (14f * staminaFactor));

                if (rand.NextDouble() <= hitChance)
                {
                    // Hit landed!
                    float rawDmg = attacker.Definition.BaseDamage * staminaFactor;
                    float absorbedDmg = CalculateAbsorbedDamage(
                        rawDmg,
                        defender.AverageArmor,
                        attacker.Definition.DamageType,
                        attacker.Definition.CombatSkill);

                    float oldHp = defender.CurrentHitPoints;
                    float newHp = Math.Max(0f, oldHp - absorbedDmg);
                    defender.CurrentHitPoints = newHp;

                    attacker.TotalDamageDealt += absorbedDmg;
                    attacker.HitsLanded++;
                    defender.TotalDamageAbsorbed += absorbedDmg;
                    defender.HitsTaken++;

                    eventLog.Add(new CombatMissionEvent(
                        tick,
                        time,
                        "Hit",
                        attacker.Definition.Id,
                        defender.Definition.Id,
                        absorbedDmg,
                        $"{attacker.Definition.Name} hit {defender.Definition.Name} for {absorbedDmg:F1} ({attacker.Definition.DamageType}). HP: {oldHp:F1} -> {newHp:F1}"));

                    // Notify components of health change
                    for (int c = 0; c < components.Count; c++)
                    {
                        components[c].OnAgentHealthChanged(defender, oldHp, newHp, attacker, eventLog);
                    }

                    if (newHp <= 0f)
                    {
                        defender.State = CombatSoldierState.Dead;
                        attacker.CasualtiesInflicted++;
                        activeDefenders.RemoveAt(targetIndex);

                        eventLog.Add(new CombatMissionEvent(
                            tick,
                            time,
                            "Casualty",
                            attacker.Definition.Id,
                            defender.Definition.Id,
                            0f,
                            $"{defender.Definition.Name} was eliminated in combat."));

                        // Morale shock to defender's teammates
                        ApplyTeamCasualtyMoraleShock(defendingTeam, defender);

                        // Notify components of agent removed
                        for (int c = 0; c < components.Count; c++)
                        {
                            components[c].OnAgentRemoved(defender, attacker, eventLog);
                        }

                        if (activeDefenders.Count == 0) break;
                    }
                }
            }
        }

        static void ApplyTeamCasualtyMoraleShock(List<CombatSoldierRuntime> team, CombatSoldierRuntime casualty)
        {
            int remaining = CountActive(team);
            float shock = Math.Max(4f, (1.0f / Math.Max(1, remaining)) * 30f);

            for (int i = 0; i < team.Count; i++)
            {
                var s = team[i];
                if (s.IsActive)
                {
                    s.CurrentMorale = Math.Max(0f, s.CurrentMorale - shock);
                }
            }
        }

        static int CountActive(List<CombatSoldierRuntime> team)
        {
            int count = 0;
            for (int i = 0; i < team.Count; i++)
            {
                if (team[i].IsActive) count++;
            }
            return count;
        }

        /// <summary>
        /// Creates a pre-balanced realistic scenario representing historical Bannerlord troop clashes.
        /// </summary>
        public static CombatSimulationScenario CreateStandardScenario(string presetName = "LegionariesVsRaiders")
        {
            var scenario = new CombatSimulationScenario
            {
                ScenarioName = presetName,
                DeltaTime = 0.05f,
                MaxDurationSeconds = 60f,
                RandomSeed = 42
            };

            if (presetName == "CataphractsVsFians")
            {
                // 5 Elite Imperial Cataphracts vs 8 Battanian Fian Champions
                for (int i = 1; i <= 5; i++)
                {
                    scenario.Team0Troops.Add(new CombatSoldierDefinition(
                        $"cataphract_{i}",
                        $"Imperial Elite Cataphract #{i}",
                        0,
                        CombatTroopRole.Cavalry,
                        maxHitPoints: 120f,
                        maxStamina: 110f,
                        initialMorale: 85f,
                        bodyArmor: 52f,
                        headArmor: 50f,
                        legArmor: 40f,
                        baseDamage: 48f,
                        damageType: CombatDamageType.Pierce,
                        combatSkill: 220,
                        attackSpeed: 1.1f));
                }

                for (int i = 1; i <= 8; i++)
                {
                    scenario.Team1Troops.Add(new CombatSoldierDefinition(
                        $"fian_{i}",
                        $"Battanian Fian Champion #{i}",
                        1,
                        CombatTroopRole.Ranged,
                        maxHitPoints: 100f,
                        maxStamina: 95f,
                        initialMorale: 80f,
                        bodyArmor: 32f,
                        headArmor: 35f,
                        legArmor: 28f,
                        baseDamage: 42f,
                        damageType: CombatDamageType.Pierce,
                        combatSkill: 260,
                        attackSpeed: 1.3f));
                }
            }
            else
            {
                // Default: 6 Imperial Legionaries vs 6 Sea Raider Veterans
                for (int i = 1; i <= 6; i++)
                {
                    scenario.Team0Troops.Add(new CombatSoldierDefinition(
                        $"legionary_{i}",
                        $"Imperial Legionary #{i}",
                        0,
                        CombatTroopRole.Infantry,
                        maxHitPoints: 100f,
                        maxStamina: 100f,
                        initialMorale: 75f,
                        bodyArmor: 44f,
                        headArmor: 42f,
                        legArmor: 32f,
                        baseDamage: 36f,
                        damageType: CombatDamageType.Cut,
                        combatSkill: 160,
                        attackSpeed: 1.0f));
                }

                for (int i = 1; i <= 6; i++)
                {
                    scenario.Team1Troops.Add(new CombatSoldierDefinition(
                        $"raider_{i}",
                        $"Sea Raider Veteran #{i}",
                        1,
                        CombatTroopRole.Infantry,
                        maxHitPoints: 100f,
                        maxStamina: 100f,
                        initialMorale: 70f,
                        bodyArmor: 30f,
                        headArmor: 28f,
                        legArmor: 22f,
                        baseDamage: 40f,
                        damageType: CombatDamageType.Cut,
                        combatSkill: 150,
                        attackSpeed: 1.05f));
                }
            }

            return scenario;
        }
    }
}
