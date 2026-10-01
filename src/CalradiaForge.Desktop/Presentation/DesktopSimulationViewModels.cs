using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace CalradiaForge.Desktop.Presentation
{
    // =========================================================================
    // TROOP TREE & AUDIO VISUALIZER VIEW MODELS (Rev097: large file split)
    // Additional classes split to companion files for readability:
    //   WorkshopEnterpriseItemViewModel, WorkshopDashboardViewModel (Workshop.cs)
    //   MemoryFactItem, AgentProfileViewModel, AgentMemoryDashboardViewModel (Workshop.cs)
    //   SecurityRuleCheckViewModel, CodeSecurityDashboardViewModel (Security.cs)
    //   ModulePipelineNodeViewModel, ModuleHierarchyDashboardViewModel (Security.cs)
    //   FactionStanceViewModel, KingdomDiplomacyDashboardViewModel (Security.cs)
    //   BlueprintNodeViewModel, ComponentGeneratorDashboardViewModel (Operations.cs)
    //   DiagnosticFindingViewModel, GenericOperationDashboardViewModel (Operations.cs)
    //   CombatContingentViewModel, CombatStudioDashboardViewModel (Operations.cs)
    //   TradeCommodityViewModel, CaravanTradeDashboardViewModel (Operations.cs)
    //   GauntletWidgetNodeViewModel, GauntletStudioDashboardViewModel (Operations.cs)
    //   SettlementExpeditionViewModel, CampaignStudioDashboardViewModel (Operations.cs)
    //   LivePipeTelemetryEventViewModel, LiveSessionDashboardViewModel (Operations.cs)
    //   PackageArtifactItemViewModel, DeliveryStudioDashboardViewModel (Operations.cs)
    //   DiagnosticFindingItemViewModel, HexDiffSnippetViewModel, DiagnosticsStudioDashboardViewModel (Operations.cs)
    // =========================================================================

    // =========================================================================
    // TROOP TREE VISUALIZER VIEW MODELS
    // =========================================================================

    internal sealed class StudioConsoleCommand(string command, string description, string family = "Workflow", bool isPinned = false)
    {
        public string Command { get; } = command;
        public string Description { get; } = description;
        public string Family { get; } = family;
        public bool IsPinned { get; set; } = isPinned;
        public string PinGlyph => IsPinned ? "★" : "☆";
    }

    internal sealed class TroopNodeViewModel : ObservableObject
    {
        public TroopNodeViewModel(
            string name,
            string id,
            int tier,
            int level,
            int hp,
            int wage,
            int cost,
            string archetype,
            string roleBadge,
            string iconKey,
            IReadOnlyList<TroopNodeViewModel> children = null)
        {
            Name = name ?? string.Empty;
            Id = id ?? string.Empty;
            Tier = tier;
            TierBadge = "TIER " + tier;
            LevelText = "Lvl " + level;
            Hp = hp;
            Wage = wage;
            Cost = cost;
            Archetype = archetype ?? "Infantry";
            RoleBadge = roleBadge ?? string.Empty;
            IconKey = iconKey ?? "GameIcon.crossed_swords";
            Children = children ?? Array.Empty<TroopNodeViewModel>();
            StatSummary = $"HP: {hp} · Wage: {wage}d · Cost: {cost}d";
        }

        public string Name { get; }
        public string Id { get; }
        public int Tier { get; }
        public string TierBadge { get; }
        public string LevelText { get; }
        public int Hp { get; }
        public int Wage { get; }
        public int Cost { get; }
        public string Archetype { get; }
        public string RoleBadge { get; }
        public string IconKey { get; }
        public IReadOnlyList<TroopNodeViewModel> Children { get; }
        public bool HasChildren => Children.Count > 0;
        public bool HasRoleBadge => !string.IsNullOrEmpty(RoleBadge);
        public string StatSummary { get; }
    }

    internal sealed class TroopTreeDashboardViewModel : ObservableObject
    {
        static readonly TroopNodeViewModel[] CanonicalStandardTreeRoots;
        static readonly TroopNodeViewModel[] CanonicalNobleTreeRoots;
        static readonly TroopNodeViewModel[] CanonicalHighlightedTroops;
        static readonly TroopNodeViewModel CanonicalDefaultSelected;
        static readonly TroopNodeViewModel CanonicalCataphract;

        static TroopTreeDashboardViewModel()
        {
            // Build canonical Imperial hierarchy once in static initializer
            var legionary = new TroopNodeViewModel("Imperial Legionary", "imperial_legionary", 4, 21, 130, 11, 200, "Infantry", "★ HEAVY SHIELDWALL", "GameIcon.crossed_swords");
            var veteranInfantry = new TroopNodeViewModel("Imperial Vet. Infantry", "imperial_veteran_infantryman", 3, 16, 120, 7, 100, "Infantry", "Broadshield & Spatha", "GameIcon.crossed_swords", [legionary]);

            var eliteMenavliaton = new TroopNodeViewModel("Imperial Elite Menavliaton", "imperial_elite_menavliaton", 4, 21, 125, 11, 200, "Infantry", "★ ANTI-CAVALRY", "GameIcon.crossed_swords");
            var menavliaton = new TroopNodeViewModel("Imperial Menavliaton", "imperial_menavliaton", 3, 16, 115, 7, 100, "Infantry", "2H Menavlion Spear", "GameIcon.crossed_swords", [eliteMenavliaton]);

            var infantry = new TroopNodeViewModel("Imperial Infantryman", "imperial_infantryman", 2, 11, 110, 4, 50, "Infantry", "Frontline Spear & Shield", "GameIcon.crossed_swords", [veteranInfantry, menavliaton]);

            var palatine = new TroopNodeViewModel("Imperial Palatine Guard", "imperial_palatine_guard", 4, 21, 120, 11, 200, "Ranged", "★ COMPOSITE BOW", "GameIcon.archery_target");
            var vetArcher = new TroopNodeViewModel("Imperial Vet. Archer", "imperial_veteran_archer", 3, 16, 110, 7, 100, "Ranged", "Bodkin Arrows", "GameIcon.archery_target", [palatine]);

            var sgtCrossbow = new TroopNodeViewModel("Imperial Sgt. Crossbow", "imperial_sergeant_crossbowman", 4, 21, 125, 12, 210, "Ranged", "★ PAVISE CROSSBOW", "GameIcon.archery_target");
            var crossbowman = new TroopNodeViewModel("Imperial Crossbowman", "imperial_crossbowman", 3, 16, 115, 8, 110, "Ranged", "Heavy Crossbow", "GameIcon.archery_target", [sgtCrossbow]);

            var archer = new TroopNodeViewModel("Imperial Archer", "imperial_archer", 2, 11, 100, 4, 50, "Ranged", "Simple Bow", "GameIcon.archery_target", [vetArcher, crossbowman]);

            var recruit = new TroopNodeViewModel("Imperial Recruit", "imperial_recruit", 1, 6, 100, 2, 20, "Infantry", "Common Levies", "GameIcon.crossed_swords", [infantry, archer]);

            var eliteCataphract = new TroopNodeViewModel("Imperial Elite Cataphract", "imperial_elite_cataphract", 6, 31, 160, 24, 450, "Cavalry", "★ BARDED WARHORSE & LANCE", "GameIcon.knight_banner");
            var cataphract = new TroopNodeViewModel("Imperial Cataphract", "imperial_cataphract", 5, 26, 145, 18, 320, "Cavalry", "Heavy Mail Armor", "GameIcon.knight_banner", [eliteCataphract]);
            var heavyHorse = new TroopNodeViewModel("Imperial Heavy Horseman", "imperial_heavy_horseman", 4, 21, 130, 13, 220, "Cavalry", "Kontos Lance", "GameIcon.knight_banner", [cataphract]);
            var equite = new TroopNodeViewModel("Imperial Equite", "imperial_equite", 3, 16, 115, 8, 120, "Cavalry", "Saddle Horse", "GameIcon.knight_banner", [heavyHorse]);
            var vigla = new TroopNodeViewModel("Imperial Vigla Recruit", "imperial_vigla_recruit", 2, 11, 105, 5, 60, "Cavalry", "Noble Youth", "GameIcon.knight_banner", [equite]);

            CanonicalStandardTreeRoots = [recruit];
            CanonicalNobleTreeRoots = [vigla];
            CanonicalHighlightedTroops = [recruit, infantry, veteranInfantry, legionary, archer, palatine, vigla, eliteCataphract];
            CanonicalDefaultSelected = legionary;
            CanonicalCataphract = eliteCataphract;
        }

        TroopNodeViewModel selectedTroop;
        string culture = "Empire (Calradic Dominance)";
        string archetypeSummary = "Combined Arms: 45% Infantry · 30% Ranged · 25% Heavy Cavalry";

        string selectedArchetypeFilter = "All";
        string simulatedFrontlineCohesion = "94.2% Cohesion · Shock Absorption: 3.8x (Formation Wedge)";
        string simulatedArmyCostEstimate = "Party Sample (x50): 10,000d Upfront · 550d/week Wage";
        bool captainPerksActive;
        string perkBonusSummary = "Captain Perks: INACTIVE (Vanilla baseline)";
        string perkBadgeBrushKey = "MutedTextBrush";
        static readonly string[] DefaultCombatRadarAxes = ["Armor", "Damage", "Speed", "Cohesion", "CostEff"];
        IReadOnlyList<double> selectedTroopRadarValues = [0.82, 0.70, 0.55, 0.94, 0.65];
        IReadOnlyList<double> captainPerksRadarValues = [0.88, 0.78, 0.58, 0.98, 0.75];

        public TroopTreeDashboardViewModel()
        {
            StandardTreeRoots = CanonicalStandardTreeRoots;
            NobleTreeRoots = CanonicalNobleTreeRoots;
            AllHighlightedTroops = CanonicalHighlightedTroops;
            selectedTroop = CanonicalDefaultSelected;
            SelectTroopCommand = new RelayCommand(p => { if (p is TroopNodeViewModel t) SelectedTroop = t; });
            FilterArchetypeCommand = new RelayCommand(p => { if (p is string s) SelectedArchetypeFilter = s; });
            SimulateBattleShockCommand = new RelayCommand(() => RecalculateBattleShock());
            ToggleCaptainPerksCommand = new RelayCommand(ToggleCaptainPerks);
            RecalculateBattleShock();
        }

        public string Culture { get => culture; private set => Set(ref culture, value); }
        public string ArchetypeSummary { get => archetypeSummary; private set => Set(ref archetypeSummary, value); }
        public string BalanceEnvelope => "Exponential-decay wage curve (1.62 elasticity) · Linear level progression (T1: 6 -> T6: 31) · 0 DAG cycles";
        public IReadOnlyList<TroopNodeViewModel> StandardTreeRoots { get; }
        public IReadOnlyList<TroopNodeViewModel> NobleTreeRoots { get; }
        public IReadOnlyList<TroopNodeViewModel> AllHighlightedTroops { get; }

        public IReadOnlyList<string> CombatRadarAxes => DefaultCombatRadarAxes;
        public IReadOnlyList<double> SelectedTroopRadarValues { get => selectedTroopRadarValues; private set => Set(ref selectedTroopRadarValues, value); }
        public IReadOnlyList<double> CaptainPerksRadarValues { get => captainPerksRadarValues; private set => Set(ref captainPerksRadarValues, value); }

        public bool CaptainPerksActive { get => captainPerksActive; private set => Set(ref captainPerksActive, value); }
        public string PerkBonusSummary { get => perkBonusSummary; private set => Set(ref perkBonusSummary, value); }
        public string PerkBadgeBrushKey { get => perkBadgeBrushKey; private set => Set(ref perkBadgeBrushKey, value); }
        public RelayCommand ToggleCaptainPerksCommand { get; }

        public void ToggleCaptainPerks()
        {
            CaptainPerksActive = !CaptainPerksActive;
            if (CaptainPerksActive)
            {
                PerkBonusSummary = "Captain Perks: ACTIVE (+15 HP, +10% Wage Efficiency, +5.0% Cohesion)";
                PerkBadgeBrushKey = "VerdigrisBrush";
            }
            else
            {
                PerkBonusSummary = "Captain Perks: INACTIVE (Vanilla baseline)";
                PerkBadgeBrushKey = "MutedTextBrush";
            }
            RecalculateBattleShock();
        }

        public string SelectedArchetypeFilter
        {
            get => selectedArchetypeFilter;
            set
            {
                if (Set(ref selectedArchetypeFilter, value))
                {
                    Raise(nameof(FilteredHighlightedTroops));
                    RecalculateBattleShock();
                }
            }
        }

        public IReadOnlyList<TroopNodeViewModel> FilteredHighlightedTroops =>
            string.IsNullOrEmpty(selectedArchetypeFilter) || selectedArchetypeFilter == "All"
                ? AllHighlightedTroops
                : AllHighlightedTroops.Where(t => t.Archetype.Equals(selectedArchetypeFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        public string SimulatedFrontlineCohesion { get => simulatedFrontlineCohesion; set => Set(ref simulatedFrontlineCohesion, value); }
        public string SimulatedArmyCostEstimate { get => simulatedArmyCostEstimate; set => Set(ref simulatedArmyCostEstimate, value); }
        public RelayCommand FilterArchetypeCommand { get; }
        public RelayCommand SimulateBattleShockCommand { get; }

        double troopReadinessIndex = 94.2;
        public double TroopReadinessIndex { get => troopReadinessIndex; set => Set(ref troopReadinessIndex, value); }

        IReadOnlyList<ForgeStepItem> troopProgressionPipelineSteps = new List<ForgeStepItem>
        {
            new("Levy Conscription", "Village Militia Pool", ForgeStepStatus.Completed, "TIER 1"),
            new("Drill & Conditioning", "Regimental Formations", ForgeStepStatus.Completed, "TIER 2-3"),
            new("Armory Outfitting", "Imperial Mail & Kontos", ForgeStepStatus.Active, "TIER 4-5"),
            new("Elite Banner Veteran", "Cataphract & Legionary", ForgeStepStatus.Pending, "TIER 6")
        };
        double battleSimulationTimestamp = 36.5;
        IReadOnlyList<double> troopStatCurveTrajectory = new List<double>
        {
            0.20, 0.28, 0.35, 0.44, 0.52, 0.61, 0.70, 0.78, 0.84, 0.88, 0.91, 0.93, 0.94, 0.95, 0.96, 0.98
        };

        public IReadOnlyList<ForgeStepItem> TroopProgressionPipelineSteps
        {
            get => troopProgressionPipelineSteps;
            set => Set(ref troopProgressionPipelineSteps, value);
        }

        public double BattleSimulationTimestamp
        {
            get => battleSimulationTimestamp;
            set => Set(ref battleSimulationTimestamp, value);
        }

        public IReadOnlyList<double> TroopStatCurveTrajectory
        {
            get => troopStatCurveTrajectory;
            set => Set(ref troopStatCurveTrajectory, value);
        }

        void RecalculateBattleShock()
        {
            var troop = SelectedTroop ?? CanonicalDefaultSelected;
            var isCav = troop.Archetype.Equals("Cavalry", StringComparison.OrdinalIgnoreCase);
            var isRanged = troop.Archetype.Equals("Ranged", StringComparison.OrdinalIgnoreCase);
            var cohesionBase = (isCav ? 96.8 : isRanged ? 88.4 : 93.5) + (CaptainPerksActive ? 5.0 : 0.0);
            cohesionBase = Math.Min(100.0, cohesionBase);
            TroopReadinessIndex = Math.Clamp(cohesionBase, 10.0, 100.0);
            var shockMulti = (isCav ? 4.2 : isRanged ? 1.6 : 3.4) + (CaptainPerksActive ? 0.6 : 0.0);
            var formation = isCav ? "Wedge" : isRanged ? "Loose" : "Shieldwall";
            simulatedFrontlineCohesion = $"{cohesionBase:0.0}% Cohesion · Shock Absorption: {shockMulti:0.0}x ({formation})";
            var upfrontCost = troop.Cost * 50;
            var weeklyWage = (int)(troop.Wage * 50 * (CaptainPerksActive ? 0.90 : 1.0));
            simulatedArmyCostEstimate = $"Party Sample (x50): {upfrontCost:N0}d Upfront · {weeklyWage:N0}d/week Wage";

            var armorVal = Math.Clamp((troop.Tier * 0.13) + (isCav ? 0.22 : isRanged ? 0.08 : 0.28), 0.15, 0.98);
            var dmgVal = Math.Clamp((troop.Tier * 0.12) + (isRanged ? 0.32 : isCav ? 0.26 : 0.20), 0.15, 0.98);
            var spdVal = isCav ? 0.92 : isRanged ? 0.62 : 0.52;
            var cohVal = Math.Clamp((isCav ? 0.88 : isRanged ? 0.76 : 0.94) + (CaptainPerksActive ? 0.05 : 0.0), 0.2, 1.0);
            var costVal = Math.Clamp(1.05 - (troop.Tier * 0.12) + (CaptainPerksActive ? 0.08 : 0.0), 0.25, 0.95);

            SelectedTroopRadarValues = [armorVal, dmgVal, spdVal, cohVal, costVal];
            CaptainPerksRadarValues = [
                Math.Clamp(armorVal + 0.06, 0.1, 1.0),
                Math.Clamp(dmgVal + 0.08, 0.1, 1.0),
                Math.Clamp(spdVal + 0.05, 0.1, 1.0),
                Math.Clamp(cohVal + 0.05, 0.1, 1.0),
                Math.Clamp(costVal + 0.10, 0.1, 1.0)
            ];

            var s1Status = ForgeStepStatus.Completed;
            var s2Status = troop.Tier >= 3 ? ForgeStepStatus.Completed : (troop.Tier == 2 ? ForgeStepStatus.Active : ForgeStepStatus.Pending);
            var s3Status = troop.Tier >= 5 ? ForgeStepStatus.Completed : (troop.Tier == 4 ? ForgeStepStatus.Active : ForgeStepStatus.Pending);
            var s4Status = troop.Tier >= 6 ? ForgeStepStatus.Completed : (troop.Tier == 5 ? ForgeStepStatus.Active : ForgeStepStatus.Pending);
            TroopProgressionPipelineSteps = new List<ForgeStepItem>
            {
                new("Levy Conscription", "Village Militia Pool", s1Status, "TIER 1"),
                new("Drill & Conditioning", "Regimental Formations", s2Status, "TIER 2-3"),
                new("Armory Outfitting", "Imperial Mail & Kontos", s3Status, "TIER 4-5"),
                new("Elite Banner Veteran", "Cataphract & Legionary", s4Status, "TIER 6")
            };

            BattleSimulationTimestamp = Math.Round(Math.Clamp(18.0 + (troop.Tier * 6.5) + (CaptainPerksActive ? 5.0 : 0.0), 10.0, 58.0), 1);

            var curve = new List<double>(16);
            var baseVal = Math.Clamp(cohesionBase / 100.0 * 0.40, 0.15, 0.50);
            var peakVal = Math.Clamp(baseVal + (troop.Tier * 0.08) + (CaptainPerksActive ? 0.06 : 0.0), 0.30, 0.98);
            for (int i = 0; i < 16; i++)
            {
                double t = i / 15.0;
                double s = t * t * (3.0 - 2.0 * t);
                double val = baseVal + (peakVal - baseVal) * s;
                curve.Add(Math.Round(Math.Clamp(val, 0.10, 1.0), 3));
            }
            TroopStatCurveTrajectory = curve;

            Raise(nameof(SimulatedFrontlineCohesion));
            Raise(nameof(SimulatedArmyCostEstimate));
            Raise(nameof(TroopStatDistributionBars));
        }

        public IReadOnlyList<ForgeBarDataPoint> TroopStatDistributionBars
        {
            get
            {
                var t = SelectedTroop ?? CanonicalDefaultSelected;
                var isCav = t.Archetype.Equals("Cavalry", StringComparison.OrdinalIgnoreCase);
                var isRanged = t.Archetype.Equals("Ranged", StringComparison.OrdinalIgnoreCase);
                var armorEst = (t.Tier * 14.0) + (isCav ? 24.0 : isRanged ? 12.0 : 28.0);
                var skillEst = (t.Tier * 26.0) + (isRanged ? 40.0 : 30.0);
                return
                [
                    new("Armor Est.", armorEst, null, "Protection rating"),
                    new("Proficiency", skillEst, null, "Weapon proficiency"),
                    new("Vitality", t.Hp, null, "Agent HP vitality"),
                    new("Tier Index", t.Tier * 20.0, null, $"Tier {t.Tier} power index"),
                    new("Wage Eff.", Math.Clamp(150.0 - t.Wage * 4.0, 20.0, 140.0), null, $"{t.Wage}d weekly upkeep")
                ];
            }
        }

        public void SetDynasticNobleScenario()
        {
            Culture = "Empire · Noble Dynastic Lineage Focus";
            ArchetypeSummary = "Noble Line: 100% Shock Heavy Cavalry (T2 Vigla -> T6 Elite Cataphract)";
            SelectedTroop = CanonicalCataphract;
            SelectedArchetypeFilter = "Cavalry";
        }

        public TroopNodeViewModel SelectedTroop
        {
            get => selectedTroop;
            set
            {
                if (Set(ref selectedTroop, value))
                {
                    RecalculateBattleShock();
                }
            }
        }

        public RelayCommand SelectTroopCommand { get; }
        public string StudioDocumentation => "Bannerlord Troop Hierarchy Engine: Evaluates 6-tier acyclic directed graphs (DAG), upgrade targets, equipment element loadouts, and wage/cost scaling curves across standard and noble lineages.";
        public string ArchitecturalInvariants => "1. Troop upgrade paths MUST form a strict acyclic DAG (no circular references).\n2. NPCCharacters.xml IDs must be globally unique.\n3. Equipment sets must reference valid Items.xml weapon/armor IDs.\n4. Level progression must follow Tier*5 + 1 formula.";
        public string StudioCaveat => "Avoid circular upgrade definitions in NPCCharacters.xml. Any cyclic dependency will cause an unhandled infinite recursion crash when opening the party recruitment screen.";
        public string QuickActionCommand => "cf.sim_tactics cavalry";
        public string QuickActionLabel => "Tactics Shock Sim";
        public string ScratchpadNotes { get; set; } = "Notes: Imperial cataphracts demonstrate 3.4x shock absorption in wedge formation.";
        public IReadOnlyList<StudioConsoleCommand> CuratedConsoleCommands { get; } =
        [
            new("campaign.give_troops imperial_legionary 10", "Add 10 Imperial Legionaries to the player party.", "Tactical Simulation"),
            new("campaign.give_troops imperial_elite_cataphract 5", "Add 5 Imperial Elite Cataphracts to the player party.", "Tactical Simulation"),
            new("cf.sim_tactics cavalry", "Calculate cavalry charge penetration and morale shock.", "Tactical Simulation", true),
            new("cf.sim_tactics infantry", "Model infantry shield wall cohesion and frontline attrition.", "Tactical Simulation"),
            new("cf.sim_tactics archery", "Calculate projectile lethality and armor penetration curves.", "Tactical Simulation"),
            new("cf.siege_tactics Chaikand", "Model wall breach thresholds and assault casualties.", "Tactical Simulation"),
            new("cf.novice_scaffold troop ImperialLegionary", "Generate valid NPCCharacters.xml troop loadout.", "Scaffolding"),
            new("cf.inspect_troop imperial_veteran_infantryman", "Inspect attributes, equipment elements, and skills.", "Workflow")
        ];
        public string PlaybookTitle => "Playbook: Custom Troop Tree Deployment";
        public IReadOnlyList<string> PlaybookSteps { get; } =
        [
            "1. Define character node in NPCCharacters.xml with unique StringId and tier.",
            "2. Bind upgrade targets ensuring strict acyclic DAG topology.",
            "3. Test recruit & party upgrade flow via in-game console."
        ];
        public string TroubleshootingHeader => "Troubleshooting: Infinite Recruitment Loops & Cyclic DAGs";
        public string TroubleshootingRemedy => "If the recruitment screen crashes or hangs, audit <upgrade_targets> for recursive loops (e.g. A->B->A). Run 'cf.novice_scaffold troop' to regenerate validated nodes.";
        public string ProceduralMacroAction => "cf.sim_tactics cavalry && campaign.give_troops imperial_legionary 10";
    }

    // =========================================================================
    // AUDIO WAVEFORM STUDIO VIEW MODELS
    // =========================================================================

    internal sealed class AudioBandViewModel(string name, string range, string dbfs, double normalized, double height, string brushKey)
    {
        public string Name { get; } = name;
        public string Range { get; } = range;
        public string Dbfs { get; } = dbfs;
        public double Normalized { get; } = normalized;
        public double Height { get; } = height;
        public string BrushKey { get; } = brushKey;
    }

    internal sealed class AudioWavePoint(double amplitude, double height)
    {
        public double Amplitude { get; } = amplitude;
        public double Height { get; } = height;
    }

    internal sealed class AudioStudioDashboardViewModel : ObservableObject
    {
        static readonly AudioBandViewModel[] DefaultBands = [
            new("Sub-Bass", "20-60 Hz", "-22 dBFS", 0.35, 42.0, "VerdigrisBrush"),
            new("Bass", "60-250 Hz", "-11 dBFS", 0.70, 84.0, "VerdigrisBrush"),
            new("Midrange", "250-2 kHz", "-4 dBFS", 0.95, 114.0, "BrassBrush"),
            new("Presence", "2-6 kHz", "-12 dBFS", 0.65, 78.0, "VerdigrisBrush"),
            new("Brilliance", "6-20 kHz", "-28 dBFS", 0.25, 30.0, "VerdigrisBrush")
        ];

        static readonly double[] RawAcousticPoints = [
            0.05, 0.15, 0.42, 0.88, 1.00, 0.76, 0.92, 0.64,
            0.48, 0.35, 0.22, 0.18, 0.32, 0.62, 0.78, 0.54,
            0.36, 0.24, 0.18, 0.12, 0.08, 0.05, 0.03, 0.01
        ];

        static readonly AudioWavePoint[] DefaultWaveform;

        static AudioStudioDashboardViewModel()
        {
            var rawPoints = RawAcousticPoints;
            DefaultWaveform = new AudioWavePoint[rawPoints.Length];
            for (var i = 0; i < rawPoints.Length; i++)
            {
                var p = rawPoints[i];
                DefaultWaveform[i] = new AudioWavePoint(p, Math.Max(4, p * 80.0));
            }
        }

        string targetAsset = "custom_iron_shield_clash.wav";
        string audioFormat = "16-bit Linear PCM · 44,100 Hz · Stereo";
        string durationText = "1.42 seconds (1,411.2 kbps bitrate)";
        string peakDbfs = "-1.4 dBFS (0 Digital Clipping)";
        string rmsDbfs = "-14.8 dBFS (High Punch)";
        string category = "mission_combat (3D Spatial Emitter)";
        string headroom = "+1.4 dB Headroom (Full Dynamic Range)";
        string activePreset = "Default Flat";

        public AudioStudioDashboardViewModel()
        {
            Bands = DefaultBands;
            Waveform = DefaultWaveform;
            ApplyPresetCommand = new RelayCommand(p =>
            {
                if (p is string preset) ApplyEqualizerPreset(preset);
            });
        }

        public string TargetAsset { get => targetAsset; private set => Set(ref targetAsset, value); }
        public string AudioFormat { get => audioFormat; private set => Set(ref audioFormat, value); }
        public string DurationText { get => durationText; private set => Set(ref durationText, value); }
        public string PeakDbfs { get => peakDbfs; private set => Set(ref peakDbfs, value); }
        public string RmsDbfs { get => rmsDbfs; private set => Set(ref rmsDbfs, value); }
        public string Category { get => category; private set => Set(ref category, value); }
        public string Headroom { get => headroom; private set => Set(ref headroom, value); }
        public string ActivePreset { get => activePreset; private set => Set(ref activePreset, value); }
        public double LeftVuLevel => 0.86;
        public double RightVuLevel => 0.82;
        public double MasterAudioHeadroomGauge => 78.5;
        public IReadOnlyList<AudioBandViewModel> Bands { get; }
        public IReadOnlyList<AudioWavePoint> Waveform { get; }
        public RelayCommand ApplyPresetCommand { get; }

        IReadOnlyList<ForgeStepItem> audioDspPipelineSteps = new List<ForgeStepItem>
        {
            new("Vorbis Ingest", "44.1kHz 16-bit PCM", ForgeStepStatus.Completed, "DECODED"),
            new("Channel DSP", "Stereo Panning Matrix", ForgeStepStatus.Completed, "ROUTED"),
            new("Parametric EQ", "5-Band Spectral Filter", ForgeStepStatus.Active, "FILTERING"),
            new("DSP Limiter", "-0.4 dBFS Headroom", ForgeStepStatus.Pending, "BUFFERED")
        };
        IReadOnlyList<double> audioSpectralDensityTrajectory = new List<double>
        {
            0.15, 0.42, 0.78, 0.95, 0.88, 0.72, 0.65, 0.58, 0.48, 0.38, 0.28, 0.22, 0.18, 0.12, 0.08, 0.04
        };
        double currentPlaybackTimestamp = 0.85;

        public IReadOnlyList<ForgeStepItem> AudioDspPipelineSteps
        {
            get => audioDspPipelineSteps;
            set => Set(ref audioDspPipelineSteps, value);
        }

        public IReadOnlyList<double> AudioSpectralDensityTrajectory
        {
            get => audioSpectralDensityTrajectory;
            set => Set(ref audioSpectralDensityTrajectory, value);
        }

        public double CurrentPlaybackTimestamp
        {
            get => currentPlaybackTimestamp;
            set => Set(ref currentPlaybackTimestamp, value);
        }

        public void ApplyEqualizerPreset(string presetName)
        {
            ActivePreset = presetName ?? "Default Flat";
            if (presetName == "Bass Boost")
            {
                PeakDbfs = "-0.8 dBFS (Optimized Bass Resonance)";
                RmsDbfs = "-12.2 dBFS (Heavy Siege Resonance)";
                Headroom = "+0.8 dB Headroom (Low-End Punch)";
                AudioDspPipelineSteps = new List<ForgeStepItem>
                {
                    new("Vorbis Ingest", "44.1kHz 16-bit PCM", ForgeStepStatus.Completed, "DECODED"),
                    new("Channel DSP", "Stereo Panning Matrix", ForgeStepStatus.Completed, "SUB-BASS"),
                    new("Parametric EQ", "Bass Boost Shelf (+6dB)", ForgeStepStatus.Active, "BOOSTED"),
                    new("DSP Limiter", "-0.8 dBFS Headroom", ForgeStepStatus.Pending, "HEADROOM")
                };
                AudioSpectralDensityTrajectory = new List<double>
                {
                    0.88, 0.98, 0.92, 0.82, 0.65, 0.50, 0.38, 0.30, 0.22, 0.18, 0.14, 0.10, 0.07, 0.05, 0.03, 0.02
                };
                CurrentPlaybackTimestamp = 0.45;
            }
            else if (presetName == "Voice Clarity")
            {
                PeakDbfs = "-2.1 dBFS (Dialogue Enhanced)";
                RmsDbfs = "-16.4 dBFS (Consonant Articulation)";
                Headroom = "+2.1 dB Headroom (Speech Intelligibility)";
                AudioDspPipelineSteps = new List<ForgeStepItem>
                {
                    new("Vorbis Ingest", "44.1kHz 16-bit PCM", ForgeStepStatus.Completed, "DECODED"),
                    new("Channel DSP", "Center Vocal Isolation", ForgeStepStatus.Completed, "MONO-CTR"),
                    new("Parametric EQ", "Midrange Articulation", ForgeStepStatus.Active, "VOICE EQ"),
                    new("DSP Limiter", "-2.1 dBFS Headroom", ForgeStepStatus.Pending, "CLEAR")
                };
                AudioSpectralDensityTrajectory = new List<double>
                {
                    0.08, 0.15, 0.28, 0.45, 0.72, 0.94, 0.98, 0.89, 0.75, 0.58, 0.42, 0.30, 0.20, 0.12, 0.08, 0.04
                };
                CurrentPlaybackTimestamp = 0.65;
            }
            else if (presetName == "Combat Punch")
            {
                PeakDbfs = "-0.4 dBFS (Transient Saturated)";
                RmsDbfs = "-10.8 dBFS (Maximum Impact Punch)";
                Headroom = "+0.4 dB Headroom (Sharp Edge Clash)";
                AudioDspPipelineSteps = new List<ForgeStepItem>
                {
                    new("Vorbis Ingest", "44.1kHz 16-bit PCM", ForgeStepStatus.Completed, "DECODED"),
                    new("Channel DSP", "3D Spatial Emitter", ForgeStepStatus.Completed, "SPATIAL"),
                    new("Parametric EQ", "High Transient Saturation", ForgeStepStatus.Completed, "SATURATED"),
                    new("DSP Limiter", "Fast-Attack Peak Limiter", ForgeStepStatus.Active, "LIMITING")
                };
                AudioSpectralDensityTrajectory = new List<double>
                {
                    0.45, 0.78, 0.95, 0.82, 0.60, 0.72, 0.88, 0.96, 0.90, 0.76, 0.65, 0.52, 0.40, 0.28, 0.18, 0.10
                };
                CurrentPlaybackTimestamp = 1.15;
            }
            else
            {
                PeakDbfs = "-1.4 dBFS (0 Digital Clipping)";
                RmsDbfs = "-14.8 dBFS (High Punch)";
                Headroom = "+1.4 dB Headroom (Full Dynamic Range)";
                AudioDspPipelineSteps = new List<ForgeStepItem>
                {
                    new("Vorbis Ingest", "44.1kHz 16-bit PCM", ForgeStepStatus.Completed, "DECODED"),
                    new("Channel DSP", "Stereo Panning Matrix", ForgeStepStatus.Completed, "ROUTED"),
                    new("Parametric EQ", "5-Band Spectral Filter", ForgeStepStatus.Active, "FILTERING"),
                    new("DSP Limiter", "-0.4 dBFS Headroom", ForgeStepStatus.Pending, "BUFFERED")
                };
                AudioSpectralDensityTrajectory = new List<double>
                {
                    0.15, 0.42, 0.78, 0.95, 0.88, 0.72, 0.65, 0.58, 0.48, 0.38, 0.28, 0.22, 0.18, 0.12, 0.08, 0.04
                };
                CurrentPlaybackTimestamp = 0.85;
            }
        }

        public void SetUiFanfareSample()
        {
            TargetAsset = "custom_quest_complete_jingle.ogg";
            AudioFormat = "Vorbis OGG · 44,100 Hz · Stereo";
            DurationText = "3.15 seconds (192 kbps VBR)";
            PeakDbfs = "-3.2 dBFS (0 Digital Clipping)";
            RmsDbfs = "-18.4 dBFS (Smooth Fanfare)";
            Category = "ui (2D Interface Sound)";
            Headroom = "+3.2 dB Headroom (Pristine Decay)";
            ActivePreset = "Voice Clarity";
        }

        public void CycleScenario(int index)
        {
            var presets = new[] { "Default Flat", "Bass Boost", "Voice Clarity", "Combat Punch" };
            var selected = presets[Math.Abs(index) % presets.Length];
            ApplyEqualizerPreset(selected);
        }
        public string StudioDocumentation => "TaleWorlds FMOD Audio Architecture: Manages 8-band frequency spectrums, decibel levels, waveform analysis, and mixer bus routing for 2D UI and 3D positional mission emitters.";
        public string ArchitecturalInvariants => "1. Audio assets must reside in Modules/<ModId>/ModuleSounds/ as Vorbis .ogg or PCM .wav.\n2. Manifest declared in ModuleData/module_sounds.xml registered under <XmlName id=\"Sounds\" />.\n3. sound_category must match an active TaleWorlds mixer bus (ui, mission_combat, ambient, voice).\n4. 3D emitters must specify valid min/max attenuation distances.";
        public string StudioCaveat => "Never use uncompressed 32-bit float WAV files for long background music or dialog lines. Use compressed 16-bit 44.1kHz Vorbis OGG to prevent audio buffer exhaustion.";
        public string QuickActionCommand => "sound.play_event custom_ui_click";
        public string QuickActionLabel => "Test 2D Click";
        public string ScratchpadNotes { get; set; } = "Notes: Vorbis 16-bit 44.1kHz audio files configured in ModuleSounds/ with 'ui' category.";
        public IReadOnlyList<StudioConsoleCommand> CuratedConsoleCommands { get; } =
        [
            new("sound.play_event custom_ui_click", "Trigger 2D UI click audio event through engine mixer.", "Workflow", true),
            new("sound.play_event custom_iron_shield_clash", "Trigger 3D combat shield clash sound event.", "Tactical Simulation"),
            new("cf.sim_audio test", "Execute test tone playback across all mixer categories.", "Workflow"),
            new("cf.audit_audio validate", "Audit module_sounds.xml for missing files and category errors.", "Diagnostics"),
            new("cf.dump_diagnostics", "Generate diagnostic telemetry dump of engine audio channels.", "Diagnostics"),
            new("cf.novice_events all", "Catalog audio-triggerable campaign events and listeners.", "Scaffolding")
        ];
        public string PlaybookTitle => "Playbook: Custom Sound Event Registration";
        public IReadOnlyList<string> PlaybookSteps { get; } =
        [
            "1. Place 16-bit 44.1kHz Vorbis .ogg in Modules/<ModId>/ModuleSounds/.",
            "2. Register sound entry in ModuleData/module_sounds.xml with sound_category.",
            "3. Trigger playback via TaleWorlds.Engine.SoundEvent.PlaySound2D()."
        ];
        public string TroubleshootingHeader => "Troubleshooting: Silent Audio & Bus Rejection";
        public string TroubleshootingRemedy => "If no audio plays, verify that sound_category matches an active TaleWorlds mixer bus ('ui', 'mission_combat', 'ambient'). Check for missing <XmlName id=\"Sounds\" /> in SubModule.xml.";
        public string ProceduralMacroAction => "cf.audit_audio validate && sound.play_event custom_ui_click";
    }

    // =========================================================================
    // WORKSHOP ENTERPRISE SIMULATOR VIEW MODELS
    // =========================================================================

}
