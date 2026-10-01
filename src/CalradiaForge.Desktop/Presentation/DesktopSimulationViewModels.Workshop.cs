using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace CalradiaForge.Desktop.Presentation
{
    // =========================================================================
    // WORKSHOP & AGENT MEMORY VIEW MODELS
    // =========================================================================

internal sealed class WorkshopEnterpriseItemViewModel
    {
        public WorkshopEnterpriseItemViewModel(
            string name,
            int profit,
            string profitText,
            string input,
            string output,
            string payback,
            double normalized,
            bool isOptimal,
            string rank)
        {
            Name = name;
            Profit = profit;
            ProfitText = profitText;
            Input = input;
            Output = output;
            Payback = payback;
            Normalized = normalized;
            BarWidth = Math.Max(20, normalized * 220.0);
            IsOptimal = isOptimal;
            Rank = rank;
        }

        public string Name { get; }
        public int Profit { get; }
        public string ProfitText { get; }
        public string Input { get; }
        public string Output { get; }
        public string Payback { get; }
        public double Normalized { get; }
        public double BarWidth { get; }
        public bool IsOptimal { get; }
        public string Rank { get; }
    }

    internal sealed class WorkshopDashboardViewModel : ObservableObject
    {
        static readonly WorkshopEnterpriseItemViewModel[] DefaultEnterprises = [
            new("Silversmith", 340, "+340 d/day", "Silver Ore (120d)", "Jewelry (310d)", "41.1 Days", 1.00, true, "RANK #1 · OPTIMAL"),
            new("Smithy", 290, "+290 d/day", "Iron Ore (45d)", "Tools & Weapons", "48.2 Days", 0.85, false, "RANK #2"),
            new("Brewery", 215, "+215 d/day", "Grain (12d)", "Beer (48d)", "65.1 Days", 0.63, false, "RANK #3"),
            new("Weaver", 195, "+195 d/day", "Wool (35d)", "Cloth (110d)", "71.8 Days", 0.57, false, "RANK #4"),
            new("Pottery", 185, "+185 d/day", "Clay (20d)", "Pottery (85d)", "75.6 Days", 0.54, false, "RANK #5"),
            new("Wood Workshop", 170, "+170 d/day", "Hardwood (25d)", "Bows & Shields", "82.3 Days", 0.50, false, "RANK #6"),
            new("Olive Press", 160, "+160 d/day", "Olives (28d)", "Oil (80d)", "87.5 Days", 0.47, false, "RANK #7")
        ];

        string settlement = "Marunath (Prosperity: 5,420 · Loyalty: 64.0/100 · Security: 72.0/100)";
        string prosperityChange = "+4.2 / day (Growing Economy)";
        string foodStorage = "184 (+14/day Surplus)";
        string garrison = "165 Regular Troops (Effective Deterrent)";
        string rebellionRisk = "8.6% (Stable · Threshold for Unrest: 45.0%)";
        string optimalEnterprise = "Silversmith (+340 d/day · 41.1d Payback Period)";

        int horizonDays = 30;
        double costMultiplier = 1.0;
        double taxRate = 0.10;
        int calculatedNetProfit = 306;
        double calculatedPaybackPeriod = 41.1;
        string simulationSummary = "Horizon: 30d · Insumos: 1.0x · Impuesto: 10% · Retorno Proyectado: +9,180d";
        IReadOnlyList<double> cumulativeProfitTrajectory = Array.Empty<double>();
        double rebellionRiskGaugeValue = 8.6;

        public WorkshopDashboardViewModel()
        {
            Enterprises = DefaultEnterprises;
            CycleHorizonCommand = new RelayCommand(() =>
            {
                HorizonDays = HorizonDays switch { 7 => 14, 14 => 30, 30 => 60, 60 => 90, _ => 7 };
            });
            CycleCostMultiplierCommand = new RelayCommand(() =>
            {
                CostMultiplier = CostMultiplier < 0.9 ? 1.0 : CostMultiplier < 1.1 ? 1.2 : CostMultiplier < 1.3 ? 1.5 : 0.8;
            });
            RecalculateEconomyCommand = new RelayCommand(() => RecalculateEconomy());
            RecalculateEconomy();
        }

        public string Settlement { get => settlement; private set => Set(ref settlement, value); }
        public string ProsperityChange { get => prosperityChange; private set => Set(ref prosperityChange, value); }
        public string FoodStorage { get => foodStorage; private set => Set(ref foodStorage, value); }
        public string Garrison { get => garrison; private set => Set(ref garrison, value); }
        public string RebellionRisk { get => rebellionRisk; private set => Set(ref rebellionRisk, value); }
        public string OptimalEnterprise { get => optimalEnterprise; private set => Set(ref optimalEnterprise, value); }
        public IReadOnlyList<WorkshopEnterpriseItemViewModel> Enterprises { get; }

        public IReadOnlyList<double> CumulativeProfitTrajectory { get => cumulativeProfitTrajectory; private set => Set(ref cumulativeProfitTrajectory, value); }
        public double RebellionRiskGaugeValue { get => rebellionRiskGaugeValue; private set => Set(ref rebellionRiskGaugeValue, value); }
        public double IndustrialEfficiencyGauge => 88.4;

        public int HorizonDays
        {
            get => horizonDays;
            set
            {
                if (Set(ref horizonDays, value)) RecalculateEconomy();
            }
        }

        public double CostMultiplier
        {
            get => costMultiplier;
            set
            {
                if (Set(ref costMultiplier, value)) RecalculateEconomy();
            }
        }

        public double TaxRate
        {
            get => taxRate;
            set
            {
                if (Set(ref taxRate, value)) RecalculateEconomy();
            }
        }

        public int CalculatedNetProfit => calculatedNetProfit;
        public double CalculatedPaybackPeriod => calculatedPaybackPeriod;
        public string SimulationSummary => simulationSummary;

        public RelayCommand CycleHorizonCommand { get; }
        public RelayCommand CycleCostMultiplierCommand { get; }
        public RelayCommand RecalculateEconomyCommand { get; }

        IReadOnlyList<ForgeStepItem> productionChainPipelineSteps = new List<ForgeStepItem>
        {
            new("Raw Ingestion", "Silver Ore & Fuel", ForgeStepStatus.Completed, "STOCKED"),
            new("Artisan Forge", "Smelting & Casting", ForgeStepStatus.Active, "PROCESSING"),
            new("Caravan Transit", "Export Routes", ForgeStepStatus.Pending, "EN ROUTE"),
            new("Ledger P&L", "+340d Profit Accrual", ForgeStepStatus.Pending, "SETTLED")
        };
        IReadOnlyList<double> enterpriseProfitMarginTrajectory = new List<double>
        {
            0.35, 0.42, 0.50, 0.58, 0.68, 0.75, 0.82, 0.88, 0.85, 0.89, 0.92, 0.94, 0.91, 0.93, 0.95, 0.96
        };
        double economicCycleTimelineDays = 18.0;

        public IReadOnlyList<ForgeStepItem> ProductionChainPipelineSteps
        {
            get => productionChainPipelineSteps;
            set => Set(ref productionChainPipelineSteps, value);
        }

        public IReadOnlyList<double> EnterpriseProfitMarginTrajectory
        {
            get => enterpriseProfitMarginTrajectory;
            set => Set(ref enterpriseProfitMarginTrajectory, value);
        }

        public double EconomicCycleTimelineDays
        {
            get => economicCycleTimelineDays;
            set => Set(ref economicCycleTimelineDays, value);
        }

        void RecalculateEconomy()
        {
            var baseProfit = 340.0;
            calculatedNetProfit = (int)Math.Round(baseProfit * costMultiplier * (1.0 - taxRate));
            calculatedPaybackPeriod = Math.Round(50000.0 / Math.Max(1, calculatedNetProfit), 1);
            var totalReturn = calculatedNetProfit * horizonDays;
            var taxStrain = taxRate > 0.15 ? 14.2 : taxRate > 0.10 ? 10.4 : 6.8;
            var costStrain = (costMultiplier - 1.0) * 4.0;
            var compositeRisk = Math.Round(Math.Max(1.0, taxStrain + costStrain), 1);
            RebellionRisk = $"{compositeRisk:0.0}% (Simulated · Threshold: 45.0%)";
            simulationSummary = $"Horizon: {horizonDays}d · Insumos: {costMultiplier:0.0}x · Impuesto: {taxRate * 100:0}% · Retorno Proyectado: +{totalReturn:N0}d";

            var days = Math.Max(7, horizonDays);
            var traj = new double[days];
            var runningTotal = 0.0;
            for (int d = 1; d <= days; d++)
            {
                var daily = calculatedNetProfit * (1.0 + Math.Sin(d * 0.45) * 0.08);
                runningTotal += daily;
                traj[d - 1] = Math.Round(runningTotal, 1);
            }
            CumulativeProfitTrajectory = traj;
            RebellionRiskGaugeValue = compositeRisk;

            // Dynamic 4-step pipeline and trajectory update
            var normProfit = Math.Clamp(calculatedNetProfit / 400.0, 0.15, 0.98);
            if (horizonDays <= 14)
            {
                ProductionChainPipelineSteps = new List<ForgeStepItem>
                {
                    new("Raw Ingestion", "Silver Ore & Fuel", ForgeStepStatus.Completed, "STOCKED"),
                    new("Artisan Forge", "Rapid Smelt Cycle", ForgeStepStatus.Active, "PROCESSING"),
                    new("Caravan Transit", "Local Market Transit", ForgeStepStatus.Pending, "SHORT ROUTE"),
                    new("Ledger P&L", $"+{calculatedNetProfit}d Accrual", ForgeStepStatus.Pending, "PENDING")
                };
                EconomicCycleTimelineDays = Math.Min(horizonDays, 8.5);
            }
            else if (horizonDays <= 30)
            {
                ProductionChainPipelineSteps = new List<ForgeStepItem>
                {
                    new("Raw Ingestion", "Silver Ore & Fuel", ForgeStepStatus.Completed, "STOCKED"),
                    new("Artisan Forge", "Smelting & Casting", ForgeStepStatus.Completed, "REFINED"),
                    new("Caravan Transit", "Export Routes", ForgeStepStatus.Active, "EN ROUTE"),
                    new("Ledger P&L", $"+{calculatedNetProfit}d Accrual", ForgeStepStatus.Pending, "AUDITING")
                };
                EconomicCycleTimelineDays = Math.Min(horizonDays, 18.0 * costMultiplier);
            }
            else
            {
                ProductionChainPipelineSteps = new List<ForgeStepItem>
                {
                    new("Raw Ingestion", "Silver Ore & Fuel", ForgeStepStatus.Completed, "STOCKED"),
                    new("Artisan Forge", "Smelting & Casting", ForgeStepStatus.Completed, "REFINED"),
                    new("Caravan Transit", "Multi-City Network", ForgeStepStatus.Completed, "DELIVERED"),
                    new("Ledger P&L", $"+{calculatedNetProfit}d Realized", ForgeStepStatus.Active, "SETTLED")
                };
                EconomicCycleTimelineDays = Math.Min(horizonDays, 45.0);
            }

            var profitPts = new List<double>(16);
            for (int i = 0; i < 16; i++)
            {
                var pt = normProfit * (0.65 + 0.35 * Math.Sin((i + 1) * 0.42));
                profitPts.Add(Math.Round(Math.Clamp(pt, 0.05, 0.98), 2));
            }
            EnterpriseProfitMarginTrajectory = profitPts;

            Raise(nameof(CalculatedNetProfit));
            Raise(nameof(CalculatedPaybackPeriod));
            Raise(nameof(SimulationSummary));
            Raise(nameof(WorkshopEconomicBreakdownBars));
        }

        public IReadOnlyList<ForgeBarDataPoint> WorkshopEconomicBreakdownBars
        {
            get
            {
                var mult = costMultiplier;
                return
                [
                    new("Gross Sales", 340.0, null, "Optimal enterprise daily turnover"),
                    new("Raw Insumos", 120.0 * mult, null, "Ore & grain commodity input"),
                    new("Guild Wages", 85.0 * mult, null, "Master artisan wages"),
                    new("Overhead", 25.0, null, "Facility upkeep & security"),
                    new("Net Daily", Math.Max(0.0, 340.0 - (120.0 * mult + 85.0 * mult + 25.0)), null, "Operating profit margin")
                ];
            }
        }

        public void SetEpicroteaScenario()
        {
            Settlement = "Epicrotea (Prosperity: 6,120 · Loyalty: 78.0/100 · Security: 85.0/100)";
            ProsperityChange = "+5.8 / day (Booming Industrial Sector)";
            FoodStorage = "240 (+22/day Surplus)";
            Garrison = "210 Regular Troops + 140 Heavy Militia";
            RebellionRisk = "4.2% (Very Stable · 0 Unrest)";
            OptimalEnterprise = "Smithy & Brewery (+580 d/day Combined Revenue)";
            RecalculateEconomy();
        }

        public void CycleScenario(int index)
        {
            if (index % 2 == 1)
            {
                SetEpicroteaScenario();
            }
            else
            {
                Settlement = "Marunath (Prosperity: 5,420 · Loyalty: 64.0/100 · Security: 72.0/100)";
                ProsperityChange = "+4.2 / day (Growing Economy)";
                FoodStorage = "184 (+14/day Surplus)";
                Garrison = "165 Regular Troops (Effective Deterrent)";
                RebellionRisk = "8.6% (Stable · Threshold for Unrest: 45.0%)";
                OptimalEnterprise = "Silversmith (+340 d/day · 41.1d Payback Period)";
                RecalculateEconomy();
            }
        }
        public string StudioDocumentation => "Bannerlord Dynamic Market Equilibrium & Workshop Simulation: Models settlement supply/demand elasticity, raw material consumption, worker wages, and daily production cycles.";
        public string ArchitecturalInvariants => "1. Workshop production must verify input item availability before deducting.\n2. Prevent ItemRoster underflow when consuming raw materials.\n3. Custom workshops must declare <WorkshopType> in workshops.xml and register via CampaignGameStarter.\n4. Keep daily production cycles stateless with respect to save persistence.";
        public string StudioCaveat => "Direct deduction of items from settlement ItemRoster without inventory quantity bounds checking causes silent save game inventory corruption.";
        public string QuickActionCommand => "cf.sim_economy workshops";
        public string QuickActionLabel => "Simulate Economy";
        public string ScratchpadNotes { get; set; } = "Notes: Pravend brewery capital reserves steady at 50,000 denars with 0 unrest.";
        public IReadOnlyList<StudioConsoleCommand> CuratedConsoleCommands { get; } =
        [
            new("campaign.give_workshop_to_player Pravend brewery", "Grant player ownership of a brewery in Pravend.", "Tactical Simulation"),
            new("cf.sim_economy workshops", "Simulate 30-day ROI and profit elasticity for all workshops.", "Tactical Simulation", true),
            new("cf.sim_trade Pravend", "Analyze trade good prices, local supply, and demand deltas.", "Tactical Simulation"),
            new("cf.sim_settlements all", "Audit settlement loyalty drift and rebellion risks.", "Tactical Simulation"),
            new("cf.sim_crime all", "Model alley extortion yields and crime decay.", "Tactical Simulation"),
            new("campaign.change_town_gold town_V1 50000", "Inject capital reserves into settlement treasury.", "Workflow"),
            new("cf.novice_scaffold item IronArmingSword", "Generate valid Items.xml weapon item for trade.", "Scaffolding")
        ];
        public string PlaybookTitle => "Playbook: Enterprise Balancing & Supply Calibration";
        public IReadOnlyList<string> PlaybookSteps { get; } =
        [
            "1. Query settlement primary production goods and village inputs.",
            "2. Select matching workshop type with favorable price elasticity.",
            "3. Inject starting capital and monitor 30-day profit margin."
        ];
        public string TroubleshootingHeader => "Troubleshooting: Zero Production & Inventory Underflow";
        public string TroubleshootingRemedy => "If daily production yields 0 denars, verify settlement warehouse has input trade goods. Avoid direct ItemRoster deductions without bounds checks to prevent save corruption.";
        public string ProceduralMacroAction => "cf.sim_economy workshops && cf.sim_trade Pravend";
    }

    // =========================================================================
    // CoALA AGENT COGNITIVE MEMORY INSPECTOR VIEW MODELS
    // =========================================================================

    internal sealed class MemoryFactItem
    {
        public MemoryFactItem(string key, string value, string ttl, string category, double recency = 1.0, double frequency = 0.5, double importance = 0.5)
        {
            Key = key;
            Value = value;
            Ttl = ttl;
            Category = category;
            HasTtl = !string.IsNullOrEmpty(ttl);
            Recency = Math.Clamp(recency, 0.0, 1.0);
            Frequency = Math.Clamp(frequency, 0.0, 1.0);
            Importance = Math.Clamp(importance, 0.0, 1.0);
            // CoALA / MIRIX Composite Utility Score: U = 0.4*Recency + 0.3*Frequency + 0.3*Importance
            UtilityScore = Math.Round((0.4 * Recency) + (0.3 * Frequency) + (0.3 * Importance), 2);
            UtilityBadge = $"[U: {UtilityScore:0.00}]";
            UtilityBrushKey = UtilityScore >= 0.70 ? "VerdigrisBrush" : UtilityScore >= 0.40 ? "BrassBrush" : "EmberBrush";
        }

        public string Key { get; }
        public string Value { get; }
        public string Ttl { get; }
        public string Category { get; }
        public bool HasTtl { get; }
        public double Recency { get; }
        public double Frequency { get; }
        public double Importance { get; }
        public double UtilityScore { get; }
        public string UtilityBadge { get; }
        public string UtilityBrushKey { get; }
    }

    internal sealed class AgentProfileViewModel
    {
        public AgentProfileViewModel(
            string heroId,
            string name,
            string culture,
            string workingMemory,
            IReadOnlyList<MemoryFactItem> semantic,
            IReadOnlyList<string> episodic,
            IReadOnlyList<string> procedural,
            string decayState = "T=0h Active")
        {
            HeroId = heroId;
            Name = name;
            Culture = culture;
            WorkingMemory = workingMemory;
            SemanticFacts = semantic ?? Array.Empty<MemoryFactItem>();
            EpisodicEvents = episodic ?? Array.Empty<string>();
            ProceduralTactics = procedural ?? Array.Empty<string>();
            FactCount = SemanticFacts.Count;
            EpisodicCount = EpisodicEvents.Count;
            ProceduralCount = ProceduralTactics.Count;
            DecayState = decayState;
            SummaryBadge = $"{FactCount} Facts · {EpisodicCount} Events · {ProceduralCount} Tactics";

            // CoALA Cognitive Architecture Salience & Consolidation (zero-allocations)
            MemoryFactItem bestFact = null;
            for (int i = 0; i < SemanticFacts.Count; i++)
            {
                var fact = SemanticFacts[i];
                if (bestFact == null || fact.UtilityScore > bestFact.UtilityScore)
                    bestFact = fact;
            }
            TopSalientFact = bestFact != null ? $"{bestFact.Key}: {bestFact.Value} {bestFact.UtilityBadge}" : "None";

            double consolidationBase = decayState.Contains("Pruned", StringComparison.OrdinalIgnoreCase) ? 92.4
                : decayState.Contains("Decay", StringComparison.OrdinalIgnoreCase) ? 71.5
                : 48.0;
            ConsolidationIndex = $"{consolidationBase:0.0}% (Cluster Cohesion)";
            ClusterSummary = $"{FactCount} Semantic Nodes · {EpisodicCount} Episodic Streams · {ProceduralCount} Tactic Graphs";

            ProceduralPolicyAxes = ["Combat", "Diplomacy", "Trade", "Governance", "Recon"];
            if (heroId == "hero_rhagaea" || culture == "Empire")
            {
                ProceduralPolicyValues = [0.85, 0.95, 0.70, 0.90, 0.65];
                ProceduralPolicyComparisonValues = [0.70, 0.80, 0.75, 0.85, 0.60];
            }
            else if (heroId == "hero_derthert" || culture == "Vlandia")
            {
                ProceduralPolicyValues = [0.90, 0.60, 0.65, 0.80, 0.50];
                ProceduralPolicyComparisonValues = [0.85, 0.65, 0.60, 0.75, 0.55];
            }
            else if (heroId == "hero_monchug" || culture == "Khuzait")
            {
                ProceduralPolicyValues = [0.95, 0.45, 0.55, 0.70, 0.85];
                ProceduralPolicyComparisonValues = [0.90, 0.50, 0.60, 0.65, 0.80];
            }
            else
            {
                ProceduralPolicyValues = [0.80, 0.70, 0.70, 0.75, 0.65];
                ProceduralPolicyComparisonValues = [0.75, 0.70, 0.70, 0.75, 0.65];
            }
        }

        public string HeroId { get; }
        public string Name { get; }
        public string Culture { get; }
        public string WorkingMemory { get; }
        public IReadOnlyList<MemoryFactItem> SemanticFacts { get; }
        public IReadOnlyList<string> EpisodicEvents { get; }
        public IReadOnlyList<string> ProceduralTactics { get; }
        public IReadOnlyList<string> ProceduralPolicyAxes { get; }
        public IReadOnlyList<double> ProceduralPolicyValues { get; }
        public IReadOnlyList<double> ProceduralPolicyComparisonValues { get; }
        public int FactCount { get; }
        public int EpisodicCount { get; }
        public int ProceduralCount { get; }
        public string DecayState { get; }
        public string SummaryBadge { get; }
        public string TopSalientFact { get; }
        public string ClusterSummary { get; }
        public string ConsolidationIndex { get; }
    }

    internal sealed class AgentMemoryDashboardViewModel : ObservableObject
    {
        static readonly AgentProfileViewModel[][] ScenarioAgentSets;

        static AgentMemoryDashboardViewModel()
        {
            // Scenario 0: Baseline (T=0h Active) - Full recency (R=1.0), active TTLs, high utility
            var rhagaea0 = new AgentProfileViewModel(
                "hero_rhagaea",
                "Empress Rhagaea",
                "Empire",
                "Sensory Focus: Onira Defense Vanguard · Stance: Hostile vs Khuzait · Target Evaluation: Critical",
                [
                    new("preference_culture", "Empire", "Permanent", "Belief", recency: 1.0, frequency: 1.0, importance: 0.95),
                    new("war_stance_khuzait", "Hostile", "TTL: 18h remaining", "Stance", recency: 1.0, frequency: 0.9, importance: 0.85),
                    new("player_disposition", "Allied (Relation: +64)", "Permanent", "Diplomacy", recency: 1.0, frequency: 0.8, importance: 0.9),
                    new("dynastic_priority", "Heir Protection (Ira)", "TTL: 72h", "Goal", recency: 1.0, frequency: 0.7, importance: 0.95)
                ],
                [
                    "[Combat] Defended Onira against Khuzait siege vanguard",
                    "[Diplomacy] Signed trade truce with Western Empire senate",
                    "[Dynasty] Arranged marriage treaty for Ira",
                    "[Logistics] Requisitioned 45 Imperial warhorses for cataphract replenishment"
                ],
                [
                    "formation_defense: Palatine archers on high ground, cataphracts in counter-charge flank",
                    "siege_defense: Position heavy infantry on ladders, menavliatons at breach bottleneck"
                ],
                "T=0h Active"
            );

            var derthert0 = new AgentProfileViewModel(
                "hero_derthert",
                "King Derthert",
                "Vlandia",
                "Sensory Focus: Charas Offensive March · Stance: Expansive vs Battania · Stance Evaluation: High",
                [
                    new("preference_culture", "Vlandia", "Permanent", "Belief", recency: 1.0, frequency: 1.0, importance: 0.9),
                    new("fief_allocation_stance", "Favor Royal House", "TTL: 12h remaining", "Stance", recency: 1.0, frequency: 0.7, importance: 0.75),
                    new("player_disposition", "Neutral (Relation: +12)", "Permanent", "Diplomacy", recency: 1.0, frequency: 0.5, importance: 0.6)
                ],
                [
                    "[Combat] Led heavy lance assault across Charas bridge",
                    "[Diplomacy] Levied emergency war subsidy from merchant guild",
                    "[Politics] Resolved disputed ownership of Sargot"
                ],
                [
                    "cavalry_charge: Deploy Vlandian banner knights in double wedge formation",
                    "feudal_levy: Summon baron vassals with 24h grace period"
                ],
                "T=0h Active"
            );

            var monchug0 = new AgentProfileViewModel(
                "hero_monchug",
                "Khan Monchug",
                "Khuzait",
                "Sensory Focus: Steppe Border Raids · Stance: Aggressive Maneuver · Target Evaluation: Fluid",
                [
                    new("preference_culture", "Khuzait", "Permanent", "Belief", recency: 1.0, frequency: 1.0, importance: 0.9),
                    new("border_raiding_stance", "Plunder Empire villages", "TTL: 6h remaining", "Stance", recency: 1.0, frequency: 0.8, importance: 0.8),
                    new("player_disposition", "Wary (Relation: -10)", "Permanent", "Diplomacy", recency: 1.0, frequency: 0.4, importance: 0.65)
                ],
                [
                    "[Combat] Encircled Battanian infantry square in open plains",
                    "[Skirmish] Intercepted caravan heading to Danustica"
                ],
                [
                    "feigned_retreat: Bait frontline into horse archer crossfire circle",
                    "nomad_encampment: Break camp within 2 hours of scout alert"
                ],
                "T=0h Active"
            );

            // Scenario 1: Decay Stage 1 (T+24h Decay) - Recency decayed to 0.79, temporary TTLs depleted
            var rhagaea1 = new AgentProfileViewModel(
                "hero_rhagaea",
                "Empress Rhagaea",
                "Empire",
                "Sensory Focus: Onira Defense Vanguard · Stance: Hostile vs Khuzait · Target Evaluation: Stable",
                [
                    new("preference_culture", "Empire", "Permanent", "Belief", recency: 0.79, frequency: 1.0, importance: 0.95),
                    new("war_stance_khuzait", "Hostile", "TTL: Expired (Evicted)", "Stance", recency: 0.20, frequency: 0.3, importance: 0.5),
                    new("player_disposition", "Allied (Relation: +64)", "Permanent", "Diplomacy", recency: 0.79, frequency: 0.8, importance: 0.9),
                    new("dynastic_priority", "Heir Protection (Ira)", "TTL: 48h remaining", "Goal", recency: 0.79, frequency: 0.7, importance: 0.95)
                ],
                [
                    "[Combat] Defended Onira against Khuzait siege vanguard",
                    "[Diplomacy] Signed trade truce with Western Empire senate",
                    "[Dynasty] Arranged marriage treaty for Ira"
                ],
                [
                    "formation_defense: Palatine archers on high ground, cataphracts in counter-charge flank",
                    "siege_defense: Position heavy infantry on ladders, menavliatons at breach bottleneck"
                ],
                "T+24h Decayed"
            );

            var derthert1 = new AgentProfileViewModel(
                "hero_derthert",
                "King Derthert",
                "Vlandia",
                "Sensory Focus: Charas Garrison Review · Stance: Consolidated · Stance Evaluation: Moderate",
                [
                    new("preference_culture", "Vlandia", "Permanent", "Belief", recency: 0.79, frequency: 1.0, importance: 0.9),
                    new("fief_allocation_stance", "Favor Royal House", "TTL: Expired (Evicted)", "Stance", recency: 0.15, frequency: 0.2, importance: 0.4),
                    new("player_disposition", "Neutral (Relation: +12)", "Permanent", "Diplomacy", recency: 0.79, frequency: 0.5, importance: 0.6)
                ],
                [
                    "[Combat] Led heavy lance assault across Charas bridge",
                    "[Diplomacy] Levied emergency war subsidy from merchant guild"
                ],
                [
                    "cavalry_charge: Deploy Vlandian banner knights in double wedge formation",
                    "feudal_levy: Summon baron vassals with 24h grace period"
                ],
                "T+24h Decayed"
            );

            var monchug1 = new AgentProfileViewModel(
                "hero_monchug",
                "Khan Monchug",
                "Khuzait",
                "Sensory Focus: Camp Relocation · Stance: Cautious Reconnaissance · Target Evaluation: Moderate",
                [
                    new("preference_culture", "Khuzait", "Permanent", "Belief", recency: 0.79, frequency: 1.0, importance: 0.9),
                    new("border_raiding_stance", "Plunder Empire villages", "TTL: Expired (Evicted)", "Stance", recency: 0.10, frequency: 0.2, importance: 0.35),
                    new("player_disposition", "Wary (Relation: -10)", "Permanent", "Diplomacy", recency: 0.79, frequency: 0.4, importance: 0.65)
                ],
                [
                    "[Combat] Encircled Battanian infantry square in open plains"
                ],
                [
                    "feigned_retreat: Bait frontline into horse archer crossfire circle",
                    "nomad_encampment: Break camp within 2 hours of scout alert"
                ],
                "T+24h Decayed"
            );

            // Scenario 2: Pruning Stage (T+72h Pruned) - Recency 0.50, low-utility facts pruned, consolidated
            var rhagaea2 = new AgentProfileViewModel(
                "hero_rhagaea",
                "Empress Rhagaea",
                "Empire",
                "Sensory Focus: Southern Empire Consolidate · Stance: Defensive Equilibrium · Pruned Low-Utility Facts",
                [
                    new("preference_culture", "Empire", "Permanent", "Belief", recency: 0.50, frequency: 1.0, importance: 0.95),
                    new("player_disposition", "Allied (Relation: +64)", "Permanent", "Diplomacy", recency: 0.50, frequency: 0.8, importance: 0.9),
                    new("dynastic_priority", "Heir Protection (Ira)", "TTL: Permanent Anchor", "Goal", recency: 0.50, frequency: 0.7, importance: 0.95)
                ],
                [
                    "[Consolidated Memory] Successfully held Onira; signed senate truce; secured Ira dynastic alliance"
                ],
                [
                    "formation_defense: Palatine archers on high ground, cataphracts in counter-charge flank"
                ],
                "T+72h Pruned"
            );

            var derthert2 = new AgentProfileViewModel(
                "hero_derthert",
                "King Derthert",
                "Vlandia",
                "Sensory Focus: Western Realm Assembly · Stance: Feudal Administration · Pruned Low-Utility Facts",
                [
                    new("preference_culture", "Vlandia", "Permanent", "Belief", recency: 0.50, frequency: 1.0, importance: 0.9),
                    new("player_disposition", "Neutral (Relation: +12)", "Permanent", "Diplomacy", recency: 0.50, frequency: 0.5, importance: 0.6)
                ],
                [
                    "[Consolidated Memory] Campaign at Charas concluded; merchant war taxes stabilized"
                ],
                [
                    "cavalry_charge: Deploy Vlandian banner knights in double wedge formation"
                ],
                "T+72h Pruned"
            );

            var monchug2 = new AgentProfileViewModel(
                "hero_monchug",
                "Khan Monchug",
                "Khuzait",
                "Sensory Focus: Steppe Pastures · Stance: Strategic Consolidation · Pruned Low-Utility Facts",
                [
                    new("preference_culture", "Khuzait", "Permanent", "Belief", recency: 0.50, frequency: 1.0, importance: 0.9),
                    new("player_disposition", "Wary (Relation: -10)", "Permanent", "Diplomacy", recency: 0.50, frequency: 0.4, importance: 0.65)
                ],
                [
                    "[Consolidated Memory] Raiding cycle completed; autumn camps established"
                ],
                [
                    "feigned_retreat: Bait frontline into horse archer crossfire circle"
                ],
                "T+72h Pruned"
            );

            ScenarioAgentSets = [
                [rhagaea0, derthert0, monchug0],
                [rhagaea1, derthert1, monchug1],
                [rhagaea2, derthert2, monchug2]
            ];
        }

        string capacitySummary = "6 / 100 Agent Slots Active · 6.0% Memory Utilization (T=0h Active)";
        double capacityPercentage = 6.0;
        int activeScenarioIndex;
        IReadOnlyList<AgentProfileViewModel> agents;
        AgentProfileViewModel selectedAgent;
        string searchQuery = string.Empty;
        string selectedCategoryFilter = "All";
        string consolidationFeedback = "Sleep Cycle Ready · 0 pending consolidation passes";
        string consolidationFeedbackBrushKey = "VerdigrisBrush";
        string newFactKey = string.Empty;
        string newFactValue = string.Empty;
        string newFactCategory = "Belief";
        string newFactTtl = "Permanent";
        readonly ObservableCollection<string> liveInjectedEpisodes = [];
        IReadOnlyList<double> memoryDecayTrajectory = Array.Empty<double>();

        public AgentMemoryDashboardViewModel()
        {
            agents = ScenarioAgentSets[0];
            selectedAgent = agents[0];
            SelectAgentCommand = new RelayCommand(p => { if (p is AgentProfileViewModel a) SelectedAgent = a; });
            SimulateMemoryDecayCommand = new RelayCommand(() => CycleScenario());
            InjectSimulatedEpisodeCommand = new RelayCommand(() =>
            {
                var stamp = DateTime.UtcNow.ToString("HH:mm:ss");
                var hero = SelectedAgent?.Name ?? "Active NPC";
                liveInjectedEpisodes.Insert(0, $"[{stamp} CoALA Live] Observed diplomatic council deliberation with {hero}");
                Raise(nameof(FilteredEpisodicEvents));
                Raise(nameof(FilteredFactsSummary));
            });
            ConsolidateMemoriesCommand = new RelayCommand(ConsolidateMemories);
            FilterCategoryCommand = new RelayCommand(p => { if (p is string cat) SelectedCategoryFilter = cat; });
            AddSemanticFactCommand = new RelayCommand(AddCustomFact, () => !string.IsNullOrWhiteSpace(NewFactKey));
            UpdateDecayTrajectory();
        }

        public IReadOnlyList<double> MemoryDecayTrajectory { get => memoryDecayTrajectory; private set => Set(ref memoryDecayTrajectory, value); }
        public double MemorySlotUtilizationGaugeValue => CapacityPercentage;
        public double CognitiveCacheHitRateGauge => 94.2;

        static readonly string[] DefaultRadarAxes = ["Combat", "Diplomacy", "Trade", "Governance", "Recon"];
        static readonly double[] DefaultRadarValues = [0.80, 0.70, 0.70, 0.75, 0.65];
        static readonly double[] DefaultRadarComparisonValues = [0.75, 0.70, 0.70, 0.75, 0.65];

        public IReadOnlyList<string> ProceduralPolicyRadarAxes => selectedAgent?.ProceduralPolicyAxes ?? DefaultRadarAxes;
        public IReadOnlyList<double> ProceduralPolicyRadarValues => selectedAgent?.ProceduralPolicyValues ?? DefaultRadarValues;
        public IReadOnlyList<double> ProceduralPolicyRadarComparisonValues => selectedAgent?.ProceduralPolicyComparisonValues ?? DefaultRadarComparisonValues;

        public string Architecture => "CoALA Cognitive Architecture (Short-Term Working + Bounded Episodic + Semantic Facts + Utility Decay)";
        public string CapacitySummary { get => capacitySummary; set => Set(ref capacitySummary, value); }
        public double CapacityPercentage { get => capacityPercentage; set => Set(ref capacityPercentage, value); }
        public IReadOnlyList<AgentProfileViewModel> Agents { get => agents; private set => Set(ref agents, value); }

        public string SelectedCategoryFilter
        {
            get => selectedCategoryFilter;
            set
            {
                if (Set(ref selectedCategoryFilter, value ?? "All"))
                {
                    Raise(nameof(FilteredSemanticFacts));
                    Raise(nameof(FilteredFactsSummary));
                }
            }
        }

        public string ConsolidationFeedback { get => consolidationFeedback; private set => Set(ref consolidationFeedback, value); }
        public string ConsolidationFeedbackBrushKey { get => consolidationFeedbackBrushKey; private set => Set(ref consolidationFeedbackBrushKey, value); }

        public string NewFactKey
        {
            get => newFactKey;
            set
            {
                if (Set(ref newFactKey, value))
                    AddSemanticFactCommand?.NotifyCanExecuteChanged();
            }
        }
        public string NewFactValue { get => newFactValue; set => Set(ref newFactValue, value); }
        public string NewFactCategory { get => newFactCategory; set => Set(ref newFactCategory, value); }
        public string NewFactTtl { get => newFactTtl; set => Set(ref newFactTtl, value); }

        public RelayCommand ConsolidateMemoriesCommand { get; }
        public RelayCommand FilterCategoryCommand { get; }
        public RelayCommand AddSemanticFactCommand { get; }

        public void AddCustomFact()
        {
            if (selectedAgent == null || string.IsNullOrWhiteSpace(newFactKey)) return;
            var key = newFactKey.Trim();
            var val = string.IsNullOrWhiteSpace(newFactValue) ? "Active Observation" : newFactValue.Trim();
            var cat = string.IsNullOrWhiteSpace(newFactCategory) ? "Belief" : newFactCategory.Trim();
            var ttl = string.IsNullOrWhiteSpace(newFactTtl) ? "Permanent" : newFactTtl.Trim();

            var currentFacts = new List<MemoryFactItem>(selectedAgent.SemanticFacts)
            {
                new(key, val, ttl, cat, 1.0, 0.7, 0.8)
            };

            var updatedAgent = new AgentProfileViewModel(
                selectedAgent.HeroId,
                selectedAgent.Name,
                selectedAgent.Culture,
                selectedAgent.WorkingMemory,
                currentFacts,
                selectedAgent.EpisodicEvents,
                selectedAgent.ProceduralTactics,
                selectedAgent.DecayState
            );

            var newAgentsList = new List<AgentProfileViewModel>(agents);
            var idx = newAgentsList.IndexOf(selectedAgent);
            if (idx >= 0) newAgentsList[idx] = updatedAgent;
            Agents = newAgentsList;
            SelectedAgent = updatedAgent;

            NewFactKey = string.Empty;
            NewFactValue = string.Empty;
            ConsolidationFeedback = $"Injected custom fact '{key}' into {updatedAgent.Name} cognitive tier.";
            ConsolidationFeedbackBrushKey = "VerdigrisBrush";
        }

        public void ConsolidateMemories()
        {
            if (selectedAgent == null) return;
            var agent = selectedAgent;
            var retainedFacts = new List<MemoryFactItem>();
            int evictedCount = 0;
            for (int i = 0; i < agent.SemanticFacts.Count; i++)
            {
                var f = agent.SemanticFacts[i];
                if (f.Ttl.Contains("Expired", StringComparison.OrdinalIgnoreCase) || f.UtilityScore < 0.35)
                {
                    evictedCount++;
                }
                else
                {
                    retainedFacts.Add(new MemoryFactItem(f.Key, f.Value, f.Ttl, f.Category, Math.Max(0.60, f.Recency), Math.Min(1.0, f.Frequency + 0.1), f.Importance));
                }
            }

            if (agent.EpisodicEvents.Count > 0 || liveInjectedEpisodes.Count > 0)
            {
                var sourceEp = liveInjectedEpisodes.Count > 0 ? liveInjectedEpisodes[0] : agent.EpisodicEvents[0];
                var distilledKey = $"consolidated_insight_{retainedFacts.Count + 1}";
                var distilledValue = sourceEp.Length > 42 ? sourceEp.Substring(0, 42) + "..." : sourceEp;
                retainedFacts.Add(new MemoryFactItem(distilledKey, distilledValue, "Permanent Anchor", "Goal", 1.0, 0.95, 0.95));
            }

            var consolidated = new AgentProfileViewModel(
                agent.HeroId,
                agent.Name,
                agent.Culture,
                $"{agent.WorkingMemory} · [Sleep Consolidation Active]",
                retainedFacts,
                agent.EpisodicEvents.Take(2).ToList(),
                agent.ProceduralTactics,
                "T+Consolidated (Sleep Complete)"
            );

            var newAgentsList = new List<AgentProfileViewModel>(agents);
            var idx = newAgentsList.IndexOf(agent);
            if (idx >= 0) newAgentsList[idx] = consolidated;
            Agents = newAgentsList;
            SelectedAgent = consolidated;

            ConsolidationFeedback = $"Sleep cycle consolidation complete: 1 insight crystallized, {evictedCount} stale facts pruned.";
            ConsolidationFeedbackBrushKey = "VerdigrisBrush";

            Raise(nameof(FilteredSemanticFacts));
            Raise(nameof(FilteredEpisodicEvents));
            Raise(nameof(FilteredProceduralTactics));
            Raise(nameof(FilteredFactsSummary));
            Raise(nameof(ActiveClusterOverview));
            Raise(nameof(ActiveConsolidationRate));
            Raise(nameof(ProceduralPolicyRadarAxes));
            Raise(nameof(ProceduralPolicyRadarValues));
            Raise(nameof(ProceduralPolicyRadarComparisonValues));
        }

        public string SearchQuery
        {
            get => searchQuery;
            set
            {
                if (Set(ref searchQuery, value))
                {
                    Raise(nameof(FilteredSemanticFacts));
                    Raise(nameof(FilteredEpisodicEvents));
                    Raise(nameof(FilteredProceduralTactics));
                    Raise(nameof(FilteredFactsSummary));
                }
            }
        }

        public IReadOnlyList<MemoryFactItem> FilteredSemanticFacts
        {
            get
            {
                var facts = SelectedAgent?.SemanticFacts;
                if (facts == null) return Array.Empty<MemoryFactItem>();
                var q = string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery.Trim();
                var cat = selectedCategoryFilter;
                var hasCatFilter = !string.IsNullOrEmpty(cat) && !cat.Equals("All", StringComparison.OrdinalIgnoreCase);

                var results = new List<MemoryFactItem>(facts.Count);
                for (int i = 0; i < facts.Count; i++)
                {
                    var f = facts[i];
                    if (hasCatFilter && !f.Category.Equals(cat, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (q != null &&
                        f.Key.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                        f.Value.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                        f.Category.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                    results.Add(f);
                }
                return results;
            }
        }

        public IReadOnlyList<string> FilteredEpisodicEvents
        {
            get
            {
                var list = new List<string>(SelectedAgent?.EpisodicEvents ?? Array.Empty<string>());
                for (int i = 0; i < liveInjectedEpisodes.Count; i++) list.Insert(0, liveInjectedEpisodes[i]);
                if (string.IsNullOrWhiteSpace(searchQuery)) return list;
                var q = searchQuery.Trim();
                return list.Where(e => e.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }
        }

        public IReadOnlyList<string> FilteredProceduralTactics
        {
            get
            {
                var tactics = SelectedAgent?.ProceduralTactics;
                if (tactics == null || string.IsNullOrWhiteSpace(searchQuery)) return tactics ?? Array.Empty<string>();
                var q = searchQuery.Trim();
                return tactics.Where(t => t.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }
        }

        public string FilteredFactsSummary =>
            $"Filter [{SelectedCategoryFilter}]: {FilteredSemanticFacts.Count}/{SelectedAgent?.SemanticFacts.Count ?? 0} Facts · {FilteredEpisodicEvents.Count} Episodes";

        public RelayCommand SimulateMemoryDecayCommand { get; }
        public RelayCommand InjectSimulatedEpisodeCommand { get; }

        public AgentProfileViewModel SelectedAgent
        {
            get => selectedAgent;
            set
            {
                if (Set(ref selectedAgent, value))
                {
                    Raise(nameof(ActiveClusterOverview));
                    Raise(nameof(ActiveConsolidationRate));
                    Raise(nameof(FilteredSemanticFacts));
                    Raise(nameof(FilteredEpisodicEvents));
                    Raise(nameof(FilteredProceduralTactics));
                    Raise(nameof(FilteredFactsSummary));
                    Raise(nameof(ProceduralPolicyRadarAxes));
                    Raise(nameof(ProceduralPolicyRadarValues));
                    Raise(nameof(ProceduralPolicyRadarComparisonValues));
                }
            }
        }

        public string ActiveClusterOverview => selectedAgent != null ? $"{selectedAgent.ClusterSummary} · Salience: {selectedAgent.ConsolidationIndex}" : "No active memory cluster";
        public string ActiveConsolidationRate => selectedAgent != null ? selectedAgent.ConsolidationIndex : "0.0%";

        public RelayCommand SelectAgentCommand { get; }

        public void CycleScenario(int index)
        {
            activeScenarioIndex = Math.Abs(index) % 3;
            var targetSet = ScenarioAgentSets[activeScenarioIndex];
            Agents = targetSet;
            SelectedAgent = targetSet[0];
            Raise(nameof(ActiveClusterOverview));
            Raise(nameof(ActiveConsolidationRate));
            Raise(nameof(ProceduralPolicyRadarAxes));
            Raise(nameof(ProceduralPolicyRadarValues));
            Raise(nameof(ProceduralPolicyRadarComparisonValues));
            if (activeScenarioIndex == 0)
            {
                CapacitySummary = "6 / 100 Agent Slots Active · 6.0% Memory Utilization (T=0h Active)";
                CapacityPercentage = 6.0;
            }
            else if (activeScenarioIndex == 1)
            {
                CapacitySummary = "6 / 100 Agent Slots Active · 4.8% Memory Utilization (T+24h Decay Stage)";
                CapacityPercentage = 4.8;
            }
            else
            {
                CapacitySummary = "6 / 100 Agent Slots Active · 3.2% Memory Utilization (T+72h Pruned & Consolidated)";
                CapacityPercentage = 3.2;
            }
            UpdateDecayTrajectory();
        }

        public void CycleScenario() => CycleScenario(activeScenarioIndex + 1);

        public void ApplyLiveIpcTelemetry(int liveAgentCount, int liveSemantic, int liveEpisodic, int liveProcedural)
        {
            CapacitySummary = $"Live Game Session · {liveAgentCount} Registered Cognitive NPCs · {liveSemantic} Active Facts · {liveEpisodic} Episodes";
            CapacityPercentage = Math.Min(100.0, (liveAgentCount / 2048.0) * 100.0);
            UpdateDecayTrajectory();
        }

        IReadOnlyList<ForgeStepItem> cognitiveConsolidationPipelineSteps = new List<ForgeStepItem>
        {
            new("Perception Ingest", "World Events & Ticks", ForgeStepStatus.Completed, "CAPTURED"),
            new("Working Focus", "Active Salience Slots", ForgeStepStatus.Completed, "INDEXED"),
            new("Episodic Trace", "FIFO Ring Buffer", ForgeStepStatus.Active, "RECORDING"),
            new("Schema Consolidation", "Semantic Schema", ForgeStepStatus.Pending, "QUEUED")
        };
        IReadOnlyList<double> memoryUtilityDecayCurve = new List<double>
        {
            0.95, 0.92, 0.88, 0.84, 0.79, 0.74, 0.69, 0.64, 0.59, 0.54, 0.49, 0.45, 0.41, 0.38, 0.35, 0.32
        };
        double episodicTimelineHours = 24.0;

        public IReadOnlyList<ForgeStepItem> CognitiveConsolidationPipelineSteps
        {
            get => cognitiveConsolidationPipelineSteps;
            set => Set(ref cognitiveConsolidationPipelineSteps, value);
        }

        public IReadOnlyList<double> MemoryUtilityDecayCurve
        {
            get => memoryUtilityDecayCurve;
            set => Set(ref memoryUtilityDecayCurve, value);
        }

        public double EpisodicTimelineHours
        {
            get => episodicTimelineHours;
            set => Set(ref episodicTimelineHours, value);
        }

        void UpdateDecayTrajectory()
        {
            var points = new double[24];
            var decayRate = activeScenarioIndex == 2 ? 0.06 : activeScenarioIndex == 1 ? 0.04 : 0.025;
            var baseF = 0.75;
            var baseI = 0.85;
            for (int t = 0; t < 24; t++)
            {
                var recency = Math.Exp(-decayRate * t);
                var u = (0.4 * recency) + (0.3 * baseF) + (0.3 * baseI);
                points[t] = Math.Round(u, 3);
            }
            MemoryDecayTrajectory = points;

            if (activeScenarioIndex == 0)
            {
                CognitiveConsolidationPipelineSteps = new List<ForgeStepItem>
                {
                    new("Perception Ingest", "World Events & Ticks", ForgeStepStatus.Completed, "CAPTURED"),
                    new("Working Focus", "Active Salience Slots", ForgeStepStatus.Completed, "INDEXED"),
                    new("Episodic Trace", "FIFO Ring Buffer", ForgeStepStatus.Active, "RECORDING"),
                    new("Schema Consolidation", "Semantic Schema", ForgeStepStatus.Pending, "QUEUED")
                };
                EpisodicTimelineHours = 8.0;
            }
            else if (activeScenarioIndex == 1)
            {
                CognitiveConsolidationPipelineSteps = new List<ForgeStepItem>
                {
                    new("Perception Ingest", "World Events & Ticks", ForgeStepStatus.Completed, "CAPTURED"),
                    new("Working Focus", "Active Salience Slots", ForgeStepStatus.Completed, "INDEXED"),
                    new("Episodic Trace", "FIFO Ring Buffer", ForgeStepStatus.Completed, "BUFFERED"),
                    new("Schema Consolidation", "Semantic Schema", ForgeStepStatus.Active, "CONSOLIDATING")
                };
                EpisodicTimelineHours = 24.0;
            }
            else
            {
                CognitiveConsolidationPipelineSteps = new List<ForgeStepItem>
                {
                    new("Perception Ingest", "World Events & Ticks", ForgeStepStatus.Completed, "CAPTURED"),
                    new("Working Focus", "Active Salience Slots", ForgeStepStatus.Completed, "INDEXED"),
                    new("Episodic Trace", "FIFO Ring Buffer", ForgeStepStatus.Completed, "PRUNED"),
                    new("Schema Consolidation", "Permanent Anchor", ForgeStepStatus.Completed, "CONSOLIDATED")
                };
                EpisodicTimelineHours = 72.0;
            }

            var curve16 = new List<double>(16);
            for (int i = 0; i < 16; i++)
            {
                var rec = Math.Exp(-decayRate * (i * 2.5));
                var u = (0.4 * rec) + (0.3 * baseF) + (0.3 * baseI);
                curve16.Add(Math.Round(Math.Clamp(u, 0.10, 1.0), 3));
            }
            MemoryUtilityDecayCurve = curve16;

            Raise(nameof(MemorySlotUtilizationGaugeValue));
        }
        public string StudioDocumentation => "CoALA / MIRIX Cognitive Memory Architecture: Real-time working memory, episodic event recording, semantic knowledge graph with multi-dimensional utility decay score U = 0.4*R + 0.3*F + 0.3*I, and universal cognitive dialogues.";
        public string ArchitecturalInvariants => "1. Episodic records are bounded at 50 entries per agent to prevent unbounded memory growth.\n2. Semantic decay occurs on modulo-24 anti-lag time-slicing.\n3. 100% stateless save persistence (0 SaveableTypeDefiner, resolve via Hero.StringId).\n4. Condition delegates in cognitive dialogues must be side-effect free.";
        public string StudioCaveat => "Never serialize Hero or Agent engine instances directly into persistent dictionaries. Always store StringIds to survive save game reload cycles.";
        public string QuickActionCommand => "cf.agent_memory_stats";
        public string QuickActionLabel => "Memory Telemetry";
        public string ScratchpadNotes { get; set; } = "Notes: CoALA cognitive decay utility score verified at U = 0.4*R + 0.3*F + 0.3*I across 2048 bounded slots.";
        public IReadOnlyList<StudioConsoleCommand> CuratedConsoleCommands { get; } =
        [
            new("cf.agent_memory_stats", "Inspect active tracked agents, fact counts, and decay state.", "CoALA Cognitive", true),
            new("cf.agent_memory_decay", "Trigger manual cognitive memory utility decay pass.", "CoALA Cognitive"),
            new("cf.agent_memory_query Hero_1", "Query episodic memories and semantic facts for Hero_1.", "CoALA Cognitive"),
            new("cf.agent_memory_salience Hero_1", "Evaluate memory salience ranking and retrieval scores for Hero_1.", "CoALA Cognitive"),
            new("cf.agent_memory_cluster", "Cluster active episodic memories by semantic theme.", "CoALA Cognitive"),
            new("cf.agent_memory_export", "Export cognitive memory graph to JSON diagnostic artifact.", "CoALA Cognitive"),
            new("campaign.print_all_heroes", "Print all living campaign heroes and StringIds.", "Workflow")
        ];
        public string PlaybookTitle => "Playbook: CoALA Cognitive NPC Memory Integration";
        public IReadOnlyList<string> PlaybookSteps { get; } =
        [
            "1. Register agent in ForgeAgentMemory via hero.StringId.",
            "2. Store episodic interactions and semantic belief facts with TTL.",
            "3. Evaluate multi-dimensional utility score U = 0.4*R + 0.3*F + 0.3*I for dialogue."
        ];
        public string TroubleshootingHeader => "Troubleshooting: Memory Leaks & Save Serialization";
        public string TroubleshootingRemedy => "Never serialize Hero or Agent engine instances directly. Always resolve via MBObjectManager.Instance.GetObject<Hero>(id) to prevent save game corruption across reload.";
        public string ProceduralMacroAction => "cf.agent_memory_stats && cf.agent_memory_salience hero_rhagaea";
    }

    // =========================================================================
    // CODE SECURITY & ASSEMBLY AUDITOR VIEW MODELS
    // =========================================================================

}
