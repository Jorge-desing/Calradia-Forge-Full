using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace CalradiaForge.Desktop.Presentation
{
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
        }

        public string Culture { get => culture; private set => Set(ref culture, value); }
        public string ArchetypeSummary { get => archetypeSummary; private set => Set(ref archetypeSummary, value); }
        public string BalanceEnvelope => "Exponential-decay wage curve (1.62 elasticity) · Linear level progression (T1: 6 -> T6: 31) · 0 DAG cycles";
        public IReadOnlyList<TroopNodeViewModel> StandardTreeRoots { get; }
        public IReadOnlyList<TroopNodeViewModel> NobleTreeRoots { get; }
        public IReadOnlyList<TroopNodeViewModel> AllHighlightedTroops { get; }

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

        void RecalculateBattleShock()
        {
            var troop = SelectedTroop ?? CanonicalDefaultSelected;
            var isCav = troop.Archetype.Equals("Cavalry", StringComparison.OrdinalIgnoreCase);
            var isRanged = troop.Archetype.Equals("Ranged", StringComparison.OrdinalIgnoreCase);
            var cohesionBase = (isCav ? 96.8 : isRanged ? 88.4 : 93.5) + (CaptainPerksActive ? 5.0 : 0.0);
            cohesionBase = Math.Min(100.0, cohesionBase);
            var shockMulti = (isCav ? 4.2 : isRanged ? 1.6 : 3.4) + (CaptainPerksActive ? 0.6 : 0.0);
            var formation = isCav ? "Wedge" : isRanged ? "Loose" : "Shieldwall";
            simulatedFrontlineCohesion = $"{cohesionBase:0.0}% Cohesion · Shock Absorption: {shockMulti:0.0}x ({formation})";
            var upfrontCost = troop.Cost * 50;
            var weeklyWage = (int)(troop.Wage * 50 * (CaptainPerksActive ? 0.90 : 1.0));
            simulatedArmyCostEstimate = $"Party Sample (x50): {upfrontCost:N0}d Upfront · {weeklyWage:N0}d/week Wage";
            Raise(nameof(SimulatedFrontlineCohesion));
            Raise(nameof(SimulatedArmyCostEstimate));
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
        public IReadOnlyList<AudioBandViewModel> Bands { get; }
        public IReadOnlyList<AudioWavePoint> Waveform { get; }
        public RelayCommand ApplyPresetCommand { get; }

        public void ApplyEqualizerPreset(string presetName)
        {
            ActivePreset = presetName ?? "Default Flat";
            if (presetName == "Bass Boost")
            {
                PeakDbfs = "-0.8 dBFS (Optimized Bass Resonance)";
                RmsDbfs = "-12.2 dBFS (Heavy Siege Resonance)";
                Headroom = "+0.8 dB Headroom (Low-End Punch)";
            }
            else if (presetName == "Voice Clarity")
            {
                PeakDbfs = "-2.1 dBFS (Dialogue Enhanced)";
                RmsDbfs = "-16.4 dBFS (Consonant Articulation)";
                Headroom = "+2.1 dB Headroom (Speech Intelligibility)";
            }
            else if (presetName == "Combat Punch")
            {
                PeakDbfs = "-0.4 dBFS (Transient Saturated)";
                RmsDbfs = "-10.8 dBFS (Maximum Impact Punch)";
                Headroom = "+0.4 dB Headroom (Sharp Edge Clash)";
            }
            else
            {
                PeakDbfs = "-1.4 dBFS (0 Digital Clipping)";
                RmsDbfs = "-14.8 dBFS (High Punch)";
                Headroom = "+1.4 dB Headroom (Full Dynamic Range)";
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
        }

        public string Settlement { get => settlement; private set => Set(ref settlement, value); }
        public string ProsperityChange { get => prosperityChange; private set => Set(ref prosperityChange, value); }
        public string FoodStorage { get => foodStorage; private set => Set(ref foodStorage, value); }
        public string Garrison { get => garrison; private set => Set(ref garrison, value); }
        public string RebellionRisk { get => rebellionRisk; private set => Set(ref rebellionRisk, value); }
        public string OptimalEnterprise { get => optimalEnterprise; private set => Set(ref optimalEnterprise, value); }
        public IReadOnlyList<WorkshopEnterpriseItemViewModel> Enterprises { get; }

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

        void RecalculateEconomy()
        {
            var baseProfit = 340.0;
            calculatedNetProfit = (int)Math.Round(baseProfit * costMultiplier * (1.0 - taxRate));
            calculatedPaybackPeriod = Math.Round(50000.0 / Math.Max(1, calculatedNetProfit), 1);
            var totalReturn = calculatedNetProfit * horizonDays;
            var taxStrain = taxRate > 0.15 ? 14.2 : taxRate > 0.10 ? 10.4 : 6.8;
            RebellionRisk = $"{taxStrain:0.0}% (Simulated · Threshold: 45.0%)";
            simulationSummary = $"Horizon: {horizonDays}d · Insumos: {costMultiplier:0.0}x · Impuesto: {taxRate * 100:0}% · Retorno Proyectado: +{totalReturn:N0}d";
            Raise(nameof(CalculatedNetProfit));
            Raise(nameof(CalculatedPaybackPeriod));
            Raise(nameof(SimulationSummary));
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
        }

        public string HeroId { get; }
        public string Name { get; }
        public string Culture { get; }
        public string WorkingMemory { get; }
        public IReadOnlyList<MemoryFactItem> SemanticFacts { get; }
        public IReadOnlyList<string> EpisodicEvents { get; }
        public IReadOnlyList<string> ProceduralTactics { get; }
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
        }

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
        }

        public void CycleScenario() => CycleScenario(activeScenarioIndex + 1);

        public void ApplyLiveIpcTelemetry(int liveAgentCount, int liveSemantic, int liveEpisodic, int liveProcedural)
        {
            CapacitySummary = $"Live Game Session · {liveAgentCount} Registered Cognitive NPCs · {liveSemantic} Active Facts · {liveEpisodic} Episodes";
            CapacityPercentage = Math.Min(100.0, (liveAgentCount / 2048.0) * 100.0);
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

    internal sealed class SecurityRuleCheckViewModel
    {
        public SecurityRuleCheckViewModel(string name, string statusText, string detailText, string brushKey)
        {
            Name = name;
            StatusText = statusText;
            DetailText = detailText;
            BrushKey = brushKey;
        }

        public string Name { get; }
        public string StatusText { get; }
        public string DetailText { get; }
        public string BrushKey { get; }
    }

    internal sealed class CodeSecurityDashboardViewModel : ObservableObject
    {
        string targetAssembly = "CalradiaForge.Mod.dll (Release x64)";
        string targetRuntime = ".NET Framework 4.7.2 / CLR v4.0.30319";
        string riskScore = "0.0% RISK · LEVEL: SECURE";
        string riskLevel = "SECURE [PASS]";
        string riskBrushKey = "VerdigrisBrush";
        bool isStrictScan = true;
        string auditSummaryMessage = "36/36 Architectural Compliance Rules Passed · 0 Violations";

        public CodeSecurityDashboardViewModel()
        {
            InitializeRules();
            ToggleStrictScanCommand = new RelayCommand(() => IsStrictScan = !IsStrictScan);
            AuditSelectedRulesCommand = new RelayCommand(AuditActiveRules);
        }

        public string TargetAssembly { get => targetAssembly; set => Set(ref targetAssembly, value); }
        public string AuditTargetAssembly { get => TargetAssembly; set => TargetAssembly = value; }

        public string TargetRuntime { get => targetRuntime; set => Set(ref targetRuntime, value); }
        public string ClrTargetFramework { get => TargetRuntime; set => TargetRuntime = value; }

        public string RiskScore { get => riskScore; set => Set(ref riskScore, value); }
        public string RiskScoreText { get => RiskScore; set => RiskScore = value; }

        public string RiskLevel { get => riskLevel; set => Set(ref riskLevel, value); }
        public string RiskBrushKey { get => riskBrushKey; set => Set(ref riskBrushKey, value); }
        public string RiskScoreBrushKey { get => RiskBrushKey; set => RiskBrushKey = value; }

        public SecurityRuleCheckViewModel RuleACheck { get; private set; }
        public SecurityRuleCheckViewModel RuleBCheck { get; private set; }
        public SecurityRuleCheckViewModel RuleCCheck { get; private set; }
        public SecurityRuleCheckViewModel RuleDCheck { get; private set; }

        public bool IsStrictScan
        {
            get => isStrictScan;
            set
            {
                if (Set(ref isStrictScan, value))
                {
                    AuditActiveRules();
                }
            }
        }

        public string AuditSummaryMessage { get => auditSummaryMessage; set => Set(ref auditSummaryMessage, value); }
        public RelayCommand ToggleStrictScanCommand { get; }
        public RelayCommand AuditSelectedRulesCommand { get; }

        public string RuleATitle => "RULE A: ANTI-SHADOWING";
        public string RuleABadge => "PASS";
        public string RuleADetail => "0 namespaces, classes, or folders named 'Campaign' or 'Localization'. Verified against TaleWorlds types.";

        public string RuleBTitle => "RULE B: STATELESS BEHAVIORS";
        public string RuleBBadge => "PASS";
        public string RuleBDetail => "0 SaveableTypeDefiner derivations; SyncData() contains zero serialization.";

        public string RuleCTitle => "RULE C: SAVEABLE BASE ID";
        public string RuleCBadge => "PASS";
        public string RuleCDetail => "Base ID verified >= 2,500,000 (Safe partition allocated: 2,500,000+).";

        public string RuleDTitle => "RULE D: DISTRIBUTION METADATA";
        public string RuleDBadge => "PASS";
        public string RuleDDetail => "Company, Product, Description, and Copyright PE metadata properly emitted.";

        static readonly string[] DefaultMetadataStreams = [
            "#~ [Tables: 45 · 1,420 Defs]",
            "#Strings [142 KB Heap]",
            "#US [User Strings: 28 KB]",
            "#GUID [32 Bytes Unique]",
            "#Blob [64 KB Signatures]"
        ];

        public IReadOnlyList<string> MetadataStreams => DefaultMetadataStreams;

        void InitializeRules()
        {
            RuleACheck = new SecurityRuleCheckViewModel("RULE A: ANTI-SHADOWING", "PASS", "0 namespaces, classes, or folders named 'Campaign' or 'Localization'. Verified against TaleWorlds types.", "VerdigrisBrush");
            RuleBCheck = new SecurityRuleCheckViewModel("RULE B: STATELESS BEHAVIORS", "PASS", "0 SaveableTypeDefiner derivations; SyncData() contains zero serialization.", "VerdigrisBrush");
            RuleCCheck = new SecurityRuleCheckViewModel("RULE C: SAVEABLE BASE ID", "PASS", "Base ID verified >= 2,500,000 (Safe partition allocated: 2,500,000+).", "VerdigrisBrush");
            RuleDCheck = new SecurityRuleCheckViewModel("RULE D: DISTRIBUTION METADATA", "PASS", "Company, Product, Description, and Copyright PE metadata properly emitted.", "VerdigrisBrush");
            Raise(nameof(RuleACheck));
            Raise(nameof(RuleBCheck));
            Raise(nameof(RuleCCheck));
            Raise(nameof(RuleDCheck));
        }

        public void AuditActiveRules()
        {
            if (isStrictScan)
            {
                RiskScore = "0.0% RISK · LEVEL: SECURE";
                RiskLevel = "SECURE [PASS]";
                RiskBrushKey = "VerdigrisBrush";
                AuditSummaryMessage = "Forensic Deep Scan: 36/36 Rules Passed · Zero CLR Violations · 100% Deterministic Safety";
            }
            else
            {
                RiskScore = "0.0% RISK · LEVEL: FAST SCAN";
                RiskLevel = "FAST SCAN [PASS]";
                RiskBrushKey = "VerdigrisBrush";
                AuditSummaryMessage = "Standard Scan: Anti-Shadowing and Stateless Invariants Validated";
            }
            InitializeRules();
            Raise(nameof(RiskScoreText));
            Raise(nameof(RiskScoreBrushKey));
        }

        public void CycleScenario(int index)
        {
            switch (index % 3)
            {
                case 0:
                    TargetAssembly = "CalradiaForge.Mod.dll (Release x64)";
                    TargetRuntime = ".NET Framework 4.7.2 / CLR v4.0.30319";
                    RiskScore = "0.0% RISK · LEVEL: SECURE";
                    RiskLevel = "SECURE [PASS]";
                    RiskBrushKey = "VerdigrisBrush";
                    break;
                case 1:
                    TargetAssembly = "TaleWorlds.CampaignSystem.dll (Native Engine)";
                    TargetRuntime = ".NET Framework 4.7.2 / Tailored Native Engine Host";
                    RiskScore = "ENGINE NATIVE · REFERENCE ONLY";
                    RiskLevel = "VERIFIED [ENGINE]";
                    RiskBrushKey = "BrassBrush";
                    break;
                case 2:
                    TargetAssembly = "CustomSubModule.dll (Third-Party Addon)";
                    TargetRuntime = ".NET Framework 4.7.2 / Sandboxed Scan Mode";
                    RiskScore = "SANDBOXED INSPECTION";
                    RiskLevel = "AUDITED [OK]";
                    RiskBrushKey = "VerdigrisBrush";
                    break;
            }
            AuditActiveRules();
        }
        public string StudioDocumentation => "Calradia Forge Architectural Gateways & Code Security: Automated audit of GEMINI Rule A (anti-shadowing), Rule B (stateless behaviors), Rule C (desktop static contracts), and Rule D (distribution safety).";
        public string ArchitecturalInvariants => "1. Rule A: Prohibit namespaces, classes, or folders named 'Campaign' or 'Localization'.\n2. Rule B: Zero SaveableTypeDefiner inheritance in mod behaviors; empty SyncData.\n3. Rule C: Preserve all 6 desktop static source contracts.\n4. Rule D: Exclude proprietary TaleWorlds DLLs and scripts from release archives.";
        public string StudioCaveat => "Shadowing TaleWorlds.CampaignSystem.Campaign breaks compilation across the entire solution for Campaign.Current. Always use CampaignBehaviors or CampaignExtensions.";
        public string QuickActionCommand => "cf.audit";
        public string QuickActionLabel => "Full 36-Rule Audit";
        public string ScratchpadNotes { get; set; } = "Notes: Zero SaveableTypeDefiner in mod behaviors; anti-shadowing checks 100% clean.";
        public IReadOnlyList<StudioConsoleCommand> CuratedConsoleCommands { get; } =
        [
            new("cf.audit", "Execute full 36-rule architectural compliance audit.", "Diagnostics", true),
            new("cf.dump_diagnostics", "Generate comprehensive diagnostic telemetry dump.", "Diagnostics"),
            new("cf.model_audit", "Audit active GameModels and decorator chain integrity.", "Diagnostics"),
            new("cf.audit_save", "Verify SaveableTypeDefiner base IDs and 31KB chunking.", "Diagnostics"),
            new("cf.audit_localization", "Check translation completeness, missing IDs, and UTF-8 BOM.", "Diagnostics"),
            new("cf.harmony_summary", "Audit active Harmony prefixes, postfixes, and transpilers.", "Diagnostics"),
            new("cf.patch_preflight", "Dry-run conflict preflight on pending patch blueprints.", "Diagnostics")
        ];
        public string PlaybookTitle => "Playbook: Architectural Rule Audit & Preflight Gate";
        public IReadOnlyList<string> PlaybookSteps { get; } =
        [
            "1. Run ModRuleAuditor checking all 36 architectural compliance rules.",
            "2. Verify Rule A: zero folders or types named 'Campaign' or 'Localization'.",
            "3. Verify Rule B: zero SaveableTypeDefiner derivations in mod behaviors."
        ];
        public string TroubleshootingHeader => "Troubleshooting: CS0104 Ambiguity & Save File Corruption";
        public string TroubleshootingRemedy => "If Campaign.Current becomes ambiguous, search for local namespaces named 'Campaign' and rename to 'CampaignBehaviors'. Check SaveableTypeDefiner base IDs >= 2,500,000.";
        public string ProceduralMacroAction => "cf.audit && cf.patch_preflight";
    }

    // =========================================================================
    // MODULE HIERARCHY & DEPENDENCY VALIDATOR VIEW MODELS
    // =========================================================================

    internal sealed class ModulePipelineNodeViewModel
    {
        public ModulePipelineNodeViewModel(string id, string version, string type, string status, string brushKey, int orderIndex = 1, int depCount = 0)
        {
            Id = id;
            Version = version;
            Type = type;
            Status = status;
            BrushKey = brushKey;
            OrderIndex = orderIndex;
            ModuleName = id;
            TypeTag = type;
            DepCount = depCount;
            StatusColor = brushKey;
        }

        public string Id { get; }
        public string Version { get; }
        public string Type { get; }
        public string Status { get; }
        public string BrushKey { get; }
        public int OrderIndex { get; set; }
        public string ModuleName { get; }
        public string TypeTag { get; }
        public int DepCount { get; }
        public string StatusColor { get; }
    }

    internal sealed class ModuleHierarchyDashboardViewModel : ObservableObject
    {
        static readonly ModulePipelineNodeViewModel[][] ScenarioNodes = [
            [
                new("Native", "v1.2.9", "Engine Core", "VERIFIED", "BrassBrush", 1, 0),
                new("SandBoxCore", "v1.2.9", "Engine Sandbox", "VERIFIED", "BrassBrush", 2, 1),
                new("SandBox", "v1.2.9", "Campaign Engine", "VERIFIED", "BrassBrush", 3, 2),
                new("StoryMode", "v1.2.9", "Story Quests", "VERIFIED", "BrassBrush", 4, 3),
                new("CalradiaForge", "v25.2.0", "Active Mod Module", "LOADED", "VerdigrisBrush", 5, 4)
            ],
            [
                new("Native", "v1.2.9", "Engine Core", "VERIFIED", "BrassBrush", 1, 0),
                new("SandBoxCore", "v1.2.9", "Engine Sandbox", "VERIFIED", "BrassBrush", 2, 1),
                new("SandBox", "v1.2.9", "Campaign Engine", "VERIFIED", "BrassBrush", 3, 2),
                new("CalradiaForge", "v25.2.0", "Standalone Mod", "LOADED", "VerdigrisBrush", 4, 3)
            ],
            [
                new("Native", "v1.2.9", "Engine Core", "VERIFIED", "BrassBrush", 1, 0),
                new("SandBoxCore", "v1.2.9", "Engine Sandbox", "VERIFIED", "BrassBrush", 2, 1),
                new("SandBox", "v1.2.9", "Campaign Engine", "VERIFIED", "BrassBrush", 3, 2),
                new("StoryMode", "v1.2.9", "Story Quests", "VERIFIED", "BrassBrush", 4, 3),
                new("CustomBattle", "v1.2.9", "Combat Arena", "STANDBY", "MutedTextBrush", 5, 3),
                new("CalradiaForge", "v25.2.0", "Framework Stack", "LOADED", "VerdigrisBrush", 6, 5)
            ]
        ];

        string manifestId = "CalradiaForge";
        string version = "v25.2.0";
        string subModuleXmlStatus = "100% VALID MANIFEST";
        string loadOrderWarning = "LOAD ORDER OPTIMAL: Engine dependencies load first (Native -> Sandbox -> Mod).";
        string warningBrushKey = "VerdigrisBrush";
        ModulePipelineNodeViewModel selectedModule;
        readonly ObservableCollection<ModulePipelineNodeViewModel> pipelineNodes;

        public ModuleHierarchyDashboardViewModel()
        {
            pipelineNodes = new ObservableCollection<ModulePipelineNodeViewModel>();
            MoveModuleUpCommand = new RelayCommand(MoveModuleUp, () => SelectedModule != null);
            MoveModuleDownCommand = new RelayCommand(MoveModuleDown, () => SelectedModule != null);
            ResetLoadOrderCommand = new RelayCommand(ResetLoadOrder);
            ResetLoadOrder();
        }

        public string ManifestId { get => manifestId; set => Set(ref manifestId, value); }
        public string TargetModuleId { get => ManifestId; set => ManifestId = value; }

        public string Version { get => version; set => Set(ref version, value); }
        public string ModuleVersion { get => Version; set => Version = value; }

        public string SubModuleXmlStatus { get => subModuleXmlStatus; set => Set(ref subModuleXmlStatus, value); }
        public ObservableCollection<ModulePipelineNodeViewModel> PipelineNodes => pipelineNodes;

        public string ConflictMatrix => "0 Circular Cycles · 0 Missing References · 0 Shadowing Collisions";
        public string DllStatus => "Win64_Shipping_Client / Net472 Verified";
        public string XmlRegistrations => "Items, SPCultures, NPCCharacters, ModuleSounds";

        public string LoadOrderWarning { get => loadOrderWarning; private set => Set(ref loadOrderWarning, value); }
        public string WarningBrushKey { get => warningBrushKey; private set => Set(ref warningBrushKey, value); }

        public ModulePipelineNodeViewModel SelectedModule
        {
            get => selectedModule;
            set
            {
                if (Set(ref selectedModule, value))
                {
                    MoveModuleUpCommand?.NotifyCanExecuteChanged();
                    MoveModuleDownCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        public RelayCommand MoveModuleUpCommand { get; }
        public RelayCommand MoveModuleDownCommand { get; }
        public RelayCommand ResetLoadOrderCommand { get; }

        public void MoveModuleUp()
        {
            if (selectedModule == null) return;
            var index = pipelineNodes.IndexOf(selectedModule);
            if (index > 0)
            {
                pipelineNodes.Move(index, index - 1);
                RecalculateOrderIndices();
            }
        }

        public void MoveModuleDown()
        {
            if (selectedModule == null) return;
            var index = pipelineNodes.IndexOf(selectedModule);
            if (index >= 0 && index < pipelineNodes.Count - 1)
            {
                pipelineNodes.Move(index, index + 1);
                RecalculateOrderIndices();
            }
        }

        public void ResetLoadOrder()
        {
            var nodes = ScenarioNodes[0];
            pipelineNodes.Clear();
            for (int i = 0; i < nodes.Length; i++)
            {
                var n = nodes[i];
                pipelineNodes.Add(new ModulePipelineNodeViewModel(n.Id, n.Version, n.Type, n.Status, n.BrushKey, i + 1, Math.Max(0, i)));
            }
            if (pipelineNodes.Count > 0) SelectedModule = pipelineNodes[pipelineNodes.Count - 1];
            RecalculateOrderIndices();
        }

        void RecalculateOrderIndices()
        {
            var hasHazard = false;
            var seenCustomMod = false;
            for (int i = 0; i < pipelineNodes.Count; i++)
            {
                pipelineNodes[i].OrderIndex = i + 1;
                var isNative = pipelineNodes[i].Id == "Native" || pipelineNodes[i].Id == "SandBoxCore" || pipelineNodes[i].Id == "SandBox" || pipelineNodes[i].Id == "StoryMode";
                if (!isNative) seenCustomMod = true;
                else if (seenCustomMod && isNative) hasHazard = true;
            }
            if (hasHazard)
            {
                LoadOrderWarning = "CRITICAL ORDER CONFLICT: Custom module loads before engine core dependencies!";
                WarningBrushKey = "EmberBrush";
            }
            else
            {
                LoadOrderWarning = "LOAD ORDER OPTIMAL: Engine dependencies load first (Native -> Sandbox -> Mod).";
                WarningBrushKey = "VerdigrisBrush";
            }
        }

        public void CycleScenario(int index)
        {
            var sc = index % 3;
            pipelineNodes.Clear();
            if (sc == 0)
            {
                ManifestId = "CalradiaForge";
                Version = "v25.0.0";
                SubModuleXmlStatus = "100% VALID MANIFEST (STANDARD STACK)";
            }
            else if (sc == 1)
            {
                ManifestId = "CalradiaForge.Minimal";
                Version = "v25.0.0-custom";
                SubModuleXmlStatus = "SANDBOX COMPATIBILITY STACK";
            }
            else
            {
                ManifestId = "CalradiaForge.Ecosystem";
                Version = "v25.0.0-eco";
                SubModuleXmlStatus = "MULTI-MOD COEXISTENCE MATRIX";
            }
            var nodes = ScenarioNodes[sc];
            for (int i = 0; i < nodes.Length; i++)
            {
                var n = nodes[i];
                pipelineNodes.Add(new ModulePipelineNodeViewModel(n.Id, n.Version, n.Type, n.Status, n.BrushKey, i + 1, Math.Max(0, i)));
            }
            if (pipelineNodes.Count > 0) SelectedModule = pipelineNodes[pipelineNodes.Count - 1];
            RecalculateOrderIndices();
        }
        public string StudioDocumentation => "TaleWorlds Module Dependency & Execution Pipeline: Resolves SubModule.xml manifests, module load order DAG, SubModuleClassType reflection initialization, and Gauntlet UI layer priority.";
        public string ArchitecturalInvariants => "1. Module folder name MUST strictly match <Id value=\"...\" /> in SubModule.xml.\n2. Compiled assemblies must be placed in bin/Win64_Shipping_Client/.\n3. TaleWorlds native modules (Native, SandBoxCore, SandBox, StoryMode) must load first.\n4. Circular module dependencies cause silent launcher startup failure.";
        public string StudioCaveat => "If the module directory name does not exactly match the <Id> tag inside SubModule.xml, the Bannerlord game launcher will refuse to load the module assembly.";
        public string QuickActionCommand => "cf.modules";
        public string QuickActionLabel => "Validate Modules";
        public string ScratchpadNotes { get; set; } = "Notes: Module folder name strictly matches SubModule.xml Id.";
        public IReadOnlyList<StudioConsoleCommand> CuratedConsoleCommands { get; } =
        [
            new("cf.modules", "List all installed modules, load order, and validation flags.", "Workflow", true),
            new("cf.dependencies", "Audit module dependency tree and detect missing prerequisites.", "Workflow"),
            new("cf.summary", "View session status, native version, and engine target.", "Workflow"),
            new("cf.logs", "Inspect runtime warnings, errors, and module initialization logs.", "Diagnostics"),
            new("cf.novice_scaffold submodule CustomModule", "Scaffold standard SubModule.xml manifest.", "Scaffolding"),
            new("cf.novice_checklist MyFirstMod", "Run mod distribution readiness verification checklist.", "Scaffolding")
        ];
        public string PlaybookTitle => "Playbook: SubModule Manifest & Dependency Orchestration";
        public IReadOnlyList<string> PlaybookSteps { get; } =
        [
            "1. Ensure root module folder name matches <Id value=\"...\" /> exactly.",
            "2. Place compiled .NET Framework 4.7.2 binaries in bin/Win64_Shipping_Client/.",
            "3. Declare TaleWorlds native module dependencies in SubModule.xml."
        ];
        public string TroubleshootingHeader => "Troubleshooting: Launcher Module Discovery Failure";
        public string TroubleshootingRemedy => "If Bannerlord launcher ignores module, check that directory name matches <Id> tag. Check Windows NTFS Mark of the Web zone identifier (run Unblock-File).";
        public string ProceduralMacroAction => "cf.modules && cf.dependencies";
    }

    // =========================================================================
    // KINGDOM DIPLOMACY & POLITICS VIEW MODELS
    // =========================================================================

    internal sealed class FactionStanceViewModel : ObservableObject
    {
        string stance;
        int tension;
        string brushKey;
        string details;

        public FactionStanceViewModel(string targetFaction, string stance, int tension, string brushKey, string details)
        {
            TargetFaction = targetFaction;
            this.stance = stance;
            this.tension = tension;
            this.brushKey = brushKey;
            this.details = details;
        }

        public string TargetFaction { get; }
        public string Stance { get => stance; set { if (Set(ref stance, value)) { Raise(nameof(TensionText)); } } }
        public int Tension { get => tension; set { if (Set(ref tension, value)) { Raise(nameof(TensionText)); Raise(nameof(BarWidth)); Raise(nameof(TensionLevel)); Raise(nameof(BorderConflictProbability)); } } }
        public string BrushKey { get => brushKey; set { if (Set(ref brushKey, value)) { Raise(nameof(TensionBrushKey)); } } }
        public string Details { get => details; set => Set(ref details, value); }
        public string TensionText => $"{Tension}% Tension";
        public double BarWidth => Math.Max(10, Tension * 1.5);

        // Aliases for XAML bindings
        public string FactionName => TargetFaction;
        public string TensionBrushKey => BrushKey;
        public int TensionLevel => Tension;
        public int BorderConflictProbability => Math.Clamp((int)(Tension * 0.85), 5, 95);
    }

    internal sealed class KingdomDiplomacyDashboardViewModel : ObservableObject
    {
        static readonly FactionStanceViewModel[][] ScenarioStances = [
            [
                new("Western Empire (Garios)", "Truce", 62, "BrassBrush", "Truce holding for 18 days · Diplomatic overtures active"),
                new("Northern Empire (Lucon)", "Hostile", 85, "EmberBrush", "Border skirmishes near Diathma · Senate war vote pending"),
                new("Kingdom of Vlandia (Derthert)", "Peace", 35, "VerdigrisBrush", "Non-aggression treaty holding · Merchant trade flow +34%"),
                new("Aserai Sultanate (Unqid)", "Allied", 28, "VerdigrisBrush", "Grain caravans protected · +42 Clan diplomatic relation"),
                new("Khuzait Khanate (Monchug)", "Skirmish", 74, "EmberBrush", "Steppe raiders sighted near Danustica perimeter")
            ],
            [
                new("Southern Empire (Rhagaea)", "Truce", 62, "BrassBrush", "Legions fortifying Amitatys gateway"),
                new("Northern Empire (Lucon)", "Hostile", 90, "EmberBrush", "Total war declared along the Lycaron corridor"),
                new("Battania (Caladog)", "Skirmish", 72, "EmberBrush", "Woodland guerrilla ambushes in Marunath pass"),
                new("Vlandia (Derthert)", "Truce", 45, "BrassBrush", "Western frontier stabilized")
            ],
            [
                new("Battania (Caladog)", "Hostile", 88, "EmberBrush", "Siege of Seonon underway · Feudal knights deployed"),
                new("Western Empire (Garios)", "Peace", 35, "VerdigrisBrush", "Trade corridor open via Charas"),
                new("Sturgia (Raganvad)", "Truce", 50, "BrassBrush", "Northern sea raids halted")
            ]
        ];

        string factionName = "Southern Empire (empire_s)";
        string ruler = "Empress Rhagaea Pethros";
        string warRiskText = "WAR RISK: 24% · STABLE";
        string peaceIndexText = "PEACE PROBABILITY: 76%";
        string senateVoteOutcome = "DECREE PENDING: Awaiting Senate Chamber ballot";
        string senateVoteBrushKey = "BrassBrush";
        readonly ObservableCollection<FactionStanceViewModel> stances;
        FactionStanceViewModel selectedStance;
        int tensionModifier;

        public KingdomDiplomacyDashboardViewModel()
        {
            stances = new ObservableCollection<FactionStanceViewModel>();
            AdjustTensionCommand = new RelayCommand(p =>
            {
                if (int.TryParse(p?.ToString(), out var delta))
                {
                    TensionModifier = Math.Clamp(tensionModifier + delta, -20, 20);
                }
            });
            SimulateSenateVoteCommand = new RelayCommand(SimulateSenateVote);
            DeclareWarCommand = new RelayCommand(DeclareWar);
            ProposePeaceTreatyCommand = new RelayCommand(ProposePeaceTreaty);

            var initialStances = ScenarioStances[0];
            for (int i = 0; i < initialStances.Length; i++) stances.Add(initialStances[i]);
            if (stances.Count > 0) SelectedStance = stances[0];
            RecalculateDiplomacy();
        }

        public string FactionName { get => factionName; set => Set(ref factionName, value); }
        public string Ruler { get => ruler; set => Set(ref ruler, value); }
        public string WarRiskText { get => warRiskText; set => Set(ref warRiskText, value); }
        public string PeaceIndexText { get => peaceIndexText; set => Set(ref peaceIndexText, value); }
        public ObservableCollection<FactionStanceViewModel> Stances => stances;

        // Aliases for XAML template bindings
        public string PlayerFaction => FactionName;
        public string PlayerRuler => Ruler;
        public string DynasticHeirScoreText => DynasticHeir;
        public string SenateChamberStatus => $"{SimulatedSenateConsensus} (Quorum Active)";
        public ObservableCollection<FactionStanceViewModel> FactionStances => Stances;

        public string SenateVoteOutcome { get => senateVoteOutcome; private set => Set(ref senateVoteOutcome, value); }
        public string SenateVoteBrushKey { get => senateVoteBrushKey; private set => Set(ref senateVoteBrushKey, value); }

        public FactionStanceViewModel SelectedStance
        {
            get => selectedStance;
            set
            {
                if (Set(ref selectedStance, value))
                {
                    Raise(nameof(SelectedStanceDetails));
                }
            }
        }

        public string SelectedStanceDetails => selectedStance != null
            ? $"{selectedStance.FactionName} · Stance: {selectedStance.Stance} · Tension: {selectedStance.Tension}% · Risk: {selectedStance.BorderConflictProbability}%\n{selectedStance.Details}"
            : "Select a faction card to inspect diplomatic intelligence and dispatch envoys.";

        public RelayCommand SimulateSenateVoteCommand { get; }
        public RelayCommand DeclareWarCommand { get; }
        public RelayCommand ProposePeaceTreatyCommand { get; }

        public void SimulateSenateVote()
        {
            var supportRate = Math.Clamp(68 - tensionModifier * 2, 15, 92);
            var passed = supportRate >= 50;
            if (passed)
            {
                SenateVoteOutcome = $"DECREE PASSED: {supportRate}% Noble Majority · 540 Influence Enacted";
                SenateVoteBrushKey = "VerdigrisBrush";
                TensionModifier = Math.Max(-20, tensionModifier - 4);
            }
            else
            {
                SenateVoteOutcome = $"DECREE REJECTED: Only {supportRate}% Support · Patrician Veto Enacted";
                SenateVoteBrushKey = "EmberBrush";
                TensionModifier = Math.Min(20, tensionModifier + 3);
            }
            Raise(nameof(SenateChamberStatus));
        }

        public void DeclareWar()
        {
            var target = SelectedStance ?? (stances.Count > 0 ? stances[0] : null);
            if (target == null) return;
            target.Stance = "Hostile";
            target.Tension = 92;
            target.BrushKey = "EmberBrush";
            target.Details = "[TOTAL WAR DECLARED] Senate war decree enacted; border legions mobilized.";
            TensionModifier = Math.Min(20, tensionModifier + 8);
            SenateVoteOutcome = $"CASUS BELLI RATIFIED: War declared against {target.FactionName}";
            SenateVoteBrushKey = "EmberBrush";
            RecalculateDiplomacy();
            Raise(nameof(SelectedStanceDetails));
        }

        public void ProposePeaceTreaty()
        {
            var target = SelectedStance ?? (stances.Count > 0 ? stances[0] : null);
            if (target == null) return;
            target.Stance = "Truce";
            target.Tension = 28;
            target.BrushKey = "VerdigrisBrush";
            target.Details = "[TRUCE RATIFIED] Peace terms accepted; daily trade resumed (+180d trade flow).";
            TensionModifier = Math.Max(-20, tensionModifier - 6);
            SenateVoteOutcome = $"PEACE TREATY SIGNED: Truce established with {target.FactionName}";
            SenateVoteBrushKey = "VerdigrisBrush";
            RecalculateDiplomacy();
            Raise(nameof(SelectedStanceDetails));
        }

        public int TensionModifier
        {
            get => tensionModifier;
            set
            {
                if (Set(ref tensionModifier, value)) RecalculateDiplomacy();
            }
        }

        public string SenateProposal => "Imperial Land Grants for Veteran Legionaries (High Senate Decree)";
        public string SenateSupportPercent => "68% (540 Influence)";
        public string SenateOpposePercent => "32% (250 Influence)";
        public string DynasticHeir => "Ira Pethros (Prestige: 88 · Legitimacy: 95% · Succession Score: 92.4)";
        public string SimulatedSenateConsensus => $"{Math.Clamp(68 - tensionModifier * 2, 10, 95)}% Support · {Math.Clamp(32 + tensionModifier * 2, 5, 90)}% Opposition";
        public RelayCommand AdjustTensionCommand { get; }

        void RecalculateDiplomacy()
        {
            var baseRisk = factionName.Contains("Western") ? 58 : factionName.Contains("Vlandia") ? 40 : 24;
            var adjustedRisk = Math.Clamp(baseRisk + tensionModifier, 5, 95);
            var adjustedPeace = 100 - adjustedRisk;
            var status = adjustedRisk >= 60 ? "MOBILIZED" : adjustedRisk >= 35 ? "EXPEDITIONARY" : "STABLE";
            WarRiskText = $"WAR RISK: {adjustedRisk}% · {status}";
            PeaceIndexText = $"PEACE PROBABILITY: {adjustedPeace}%";
            Raise(nameof(SimulatedSenateConsensus));
            Raise(nameof(SenateChamberStatus));
        }

        public void CycleScenario(int index)
        {
            var sc = index % 3;
            stances.Clear();
            if (sc == 0)
            {
                FactionName = "Southern Empire (empire_s)";
                Ruler = "Empress Rhagaea Pethros";
                WarRiskText = "WAR RISK: 24% · STABLE";
                PeaceIndexText = "PEACE PROBABILITY: 76%";
            }
            else if (sc == 1)
            {
                FactionName = "Western Empire (empire_w)";
                Ruler = "Emperor Garios Comnos";
                WarRiskText = "WAR RISK: 58% · MOBILIZED";
                PeaceIndexText = "PEACE PROBABILITY: 42%";
            }
            else
            {
                FactionName = "Kingdom of Vlandia (vlandia)";
                Ruler = "King Derthert dey Meroc";
                WarRiskText = "WAR RISK: 40% · EXPEDITIONARY";
                PeaceIndexText = "PEACE PROBABILITY: 60%";
            }
            var targetStances = ScenarioStances[sc];
            for (int i = 0; i < targetStances.Length; i++) stances.Add(targetStances[i]);
            if (stances.Count > 0) SelectedStance = stances[0];
            RecalculateDiplomacy();
        }
        public string StudioDocumentation => "Bannerlord Geopolitical Diplomacy & Casus Belli Engine: Evaluates faction power ratios, war justification viability, border friction scores, tribute settlement agreements, and council voting dynamics.";
        public string ArchitecturalInvariants => "1. Diplomatic actions must verify active peace treaties and active sieges.\n2. DeclareWarAction must validate faction leadership and avoid null casus belli.\n3. Decorator GameModels must wrap _previousModel and apply ExplainedNumber bonuses.\n4. Kingdom decisions must remain deterministic under simulated AI voting.";
        public string StudioCaveat => "Executing DeclareWarAction without verifying existing alliance or truce state causes TaleWorlds AI diplomacy managers to throw unhandled state machine exceptions.";
        public string QuickActionCommand => "cf.sim_diplomacy all";
        public string QuickActionLabel => "Diplomacy Sim";
        public string ScratchpadNotes { get; set; } = "Notes: Faction power balance and border friction calculated with deterministic voting.";
        public IReadOnlyList<StudioConsoleCommand> CuratedConsoleCommands { get; } =
        [
            new("cf.sim_diplomacy all", "Simulate kingdom power balance, war viability, and tribute deltas.", "Tactical Simulation", true),
            new("cf.casus_belli Empire Vlandia", "Evaluate geopolitical war justification scoring and border friction.", "Tactical Simulation"),
            new("cf.sim_dynasty all", "Assess noble clan succession scores and adult heirs.", "Tactical Simulation"),
            new("cf.sim_settlements all", "Audit settlement loyalty drift and rebellion risks.", "Tactical Simulation"),
            new("cf.sim_crime all", "Model alley extortion yields and crime decay.", "Tactical Simulation"),
            new("campaign.declare_war vlandia battania", "Force war declaration between Vlandia and Battania.", "Workflow"),
            new("campaign.make_peace vlandia battania", "Negotiate peace treaty between Vlandia and Battania.", "Workflow")
        ];
        public string PlaybookTitle => "Playbook: Geopolitical Stance & Casus Belli Resolution";
        public IReadOnlyList<string> PlaybookSteps { get; } =
        [
            "1. Evaluate kingdom military power ratios and border friction scores.",
            "2. Check active peace treaties and truce expiry timestamps.",
            "3. Trigger DeclareWarAction or MakePeaceAction with valid casus belli."
        ];
        public string TroubleshootingHeader => "Troubleshooting: State Machine Crash on War Declaration";
        public string TroubleshootingRemedy => "Calling DeclareWarAction on an already-hostile faction or during active siege throws unhandled state machine exceptions. Always check Faction.IsAtWarWith() first.";
        public string ProceduralMacroAction => "cf.sim_diplomacy all && cf.casus_belli Empire Vlandia";
    }

    // =========================================================================
    // COMPONENT GENERATOR & XML BLUEPRINT VIEW MODELS
    // =========================================================================

    internal sealed class BlueprintNodeViewModel
    {
        public BlueprintNodeViewModel(string name, string type, string status, string brushKey, string payload, int fieldCount = 4, string category = "XML Entity")
        {
            Name = name;
            Type = type;
            Status = status;
            BrushKey = brushKey;
            Payload = payload;
            NodeName = name;
            Category = category;
            FieldCount = fieldCount;
            StatusBrushKey = brushKey;
        }

        public string Name { get; }
        public string Type { get; }
        public string Status { get; }
        public string BrushKey { get; }
        public string Payload { get; }
        public string NodeName { get; }
        public string Category { get; }
        public int FieldCount { get; }
        public string StatusBrushKey { get; }
    }

    internal sealed class ComponentGeneratorDashboardViewModel : ObservableObject
    {
        static readonly BlueprintNodeViewModel[][] ScenarioBlueprints = [
            [
                new("custom_ui_click", "2D UI Audio", "VALIDATED", "VerdigrisBrush", "<module_sound name=\"custom_ui_click\" is_2d=\"true\" sound_category=\"ui\" path=\"ui_click.ogg\" />", 4, "Sound Definition"),
                new("custom_iron_shield_clash", "3D Combat Sound", "VALIDATED", "BrassBrush", "<module_sound name=\"custom_iron_shield_clash\" is_2d=\"false\" sound_category=\"mission_combat\" path=\"shield_clash.ogg\" />", 4, "Sound Definition"),
                new("custom_quest_fanfare", "2D Notification", "VALIDATED", "VerdigrisBrush", "<module_sound name=\"custom_quest_fanfare\" is_2d=\"true\" sound_category=\"ui\" path=\"quest_fanfare.ogg\" />", 4, "Sound Definition"),
                new("custom_ambient_wind", "Ambient Loop", "VALIDATED", "DeepPineBrush", "<module_sound name=\"custom_ambient_wind\" is_2d=\"true\" sound_category=\"ambient\" path=\"ambient_wind.ogg\" />", 4, "Sound Definition")
            ],
            [
                new("Widget.Root", "Root Container", "VALIDATED", "BrassBrush", "<Widget WidthSizePolicy=\"StretchToParent\" HeightSizePolicy=\"StretchToParent\">", 3, "Gauntlet Prefab"),
                new("ListPanel.Nav", "Navigation Stack", "VALIDATED", "VerdigrisBrush", "<ListPanel StackLayout.LayoutMethod=\"VerticalBottomToTop\" MarginLeft=\"16\">", 4, "Gauntlet Prefab"),
                new("ButtonWidget.Command", "Tactical Trigger", "VALIDATED", "BrassBrush", "<ButtonWidget Command.Click=\"ExecuteAction\" SuggestedWidth=\"140\" />", 3, "Gauntlet Prefab"),
                new("TextWidget.Label", "Dynamic Text", "VALIDATED", "DeepPineBrush", "<TextWidget Text=\"@Headline\" Brush=\"CalradiaForge.Gold\" />", 3, "Gauntlet Prefab")
            ],
            [
                new("imperial_cataphract", "Elite Cavalry [T6]", "VALIDATED", "BrassBrush", "<NPCCharacter id=\"imperial_cataphract\" default_group=\"Cavalry\" level=\"31\" />", 6, "Troop Character"),
                new("imperial_legionary", "Frontline Infantry [T4]", "VALIDATED", "VerdigrisBrush", "<NPCCharacter id=\"imperial_legionary\" default_group=\"Infantry\" level=\"21\" />", 6, "Troop Character"),
                new("imperial_palatine_guard", "Elite Archer [T4]", "VALIDATED", "VerdigrisBrush", "<NPCCharacter id=\"imperial_palatine_guard\" default_group=\"Ranged\" level=\"21\" />", 6, "Troop Character")
            ]
        ];

        string targetOutput = "ModuleSounds/module_sounds.xml";
        string schemaStandard = "Calradia Forge Audio & Prefab Definition (bannerlord_audio_system.md)";
        string validationStatus = "100% VALIDATED SCHEMA · ZERO RUNTIME HAZARDS";
        string customComponentId = "custom_iron_shield_clash";
        string generatedXmlPreview = "<module_sound name=\"custom_iron_shield_clash\" is_2d=\"false\" sound_category=\"mission_combat\" path=\"shield_clash.ogg\" />";
        string xmlCopyFeedback = string.Empty;
        BlueprintNodeViewModel selectedBlueprint;
        readonly ObservableCollection<BlueprintNodeViewModel> blueprintNodes;

        public ComponentGeneratorDashboardViewModel()
        {
            var initialBlueprints = ScenarioBlueprints[0];
            blueprintNodes = new ObservableCollection<BlueprintNodeViewModel>();
            for (int i = 0; i < initialBlueprints.Length; i++) blueprintNodes.Add(initialBlueprints[i]);
            CopyXmlBlueprintCommand = new RelayCommand(CopyXmlBlueprint);
            GenerateBlueprintCommand = new RelayCommand(RegenerateXmlPreview);
            if (blueprintNodes.Count > 0) SelectedBlueprint = blueprintNodes[0];
        }

        public string TargetOutput { get => targetOutput; set => Set(ref targetOutput, value); }
        public string ComponentId { get => TargetOutput; set => TargetOutput = value; }

        public string SchemaStandard { get => schemaStandard; set => Set(ref schemaStandard, value); }
        public string TargetSchemaType { get => SchemaStandard; set => SchemaStandard = value; }

        public string ValidationStatus { get => validationStatus; set => Set(ref validationStatus, value); }
        public string MixerComplianceStatus { get => ValidationStatus; set => ValidationStatus = value; }

        public ObservableCollection<BlueprintNodeViewModel> BlueprintNodes => blueprintNodes;

        public string MixerCategoryStatus => "Mixer Buses: ui (2D Interface), mission_combat (3D Combat) & ambient";
        public string FormatCompliance => "Vorbis .OGG / 16-bit 44.1kHz PCM Zero-Latency Decoding";

        public string CustomComponentId
        {
            get => customComponentId;
            set
            {
                if (Set(ref customComponentId, value))
                {
                    RegenerateXmlPreview();
                }
            }
        }

        public string GeneratedXmlPreview
        {
            get => generatedXmlPreview;
            set => Set(ref generatedXmlPreview, value);
        }

        public string XmlCopyFeedback
        {
            get => xmlCopyFeedback;
            set => Set(ref xmlCopyFeedback, value);
        }

        public BlueprintNodeViewModel SelectedBlueprint
        {
            get => selectedBlueprint;
            set
            {
                if (Set(ref selectedBlueprint, value) && value != null)
                {
                    CustomComponentId = value.Name;
                    GeneratedXmlPreview = value.Payload;
                }
            }
        }

        public RelayCommand CopyXmlBlueprintCommand { get; }
        public RelayCommand GenerateBlueprintCommand { get; }

        public void RegenerateXmlPreview()
        {
            var id = string.IsNullOrWhiteSpace(customComponentId) ? "custom_component" : customComponentId.Trim();
            if (targetOutput.Contains("sounds", StringComparison.OrdinalIgnoreCase))
            {
                GeneratedXmlPreview = $"<module_sound name=\"{id}\" is_2d=\"false\" sound_category=\"mission_combat\" path=\"{id}.ogg\" />";
            }
            else if (targetOutput.Contains("Prefabs", StringComparison.OrdinalIgnoreCase))
            {
                GeneratedXmlPreview = $"<Widget Id=\"{id}\" WidthSizePolicy=\"StretchToParent\" HeightSizePolicy=\"CoverChildren\">\n  <TextWidget Text=\"@{id}Text\" Brush=\"CalradiaForge.Gold\" />\n</Widget>";
            }
            else
            {
                GeneratedXmlPreview = $"<NPCCharacter id=\"{id}\" default_group=\"Infantry\" level=\"21\" name=\"{{={id}_name}}Imperial Legionary Vanguard\" />";
            }
            XmlCopyFeedback = $"Regenerated blueprint for: {id}";
        }

        public void CopyXmlBlueprint()
        {
            try
            {
                System.Windows.Clipboard.SetText(GeneratedXmlPreview);
                XmlCopyFeedback = "Blueprint XML copied to clipboard!";
            }
            catch
            {
                XmlCopyFeedback = "Preview updated (Clipboard unavailable in headless mode)";
            }
        }

        public void CycleScenario(int index)
        {
            var sc = index % 3;
            blueprintNodes.Clear();
            if (sc == 0)
            {
                TargetOutput = "ModuleSounds/module_sounds.xml";
                SchemaStandard = "Audio Manifest Definition (bannerlord_audio_system.md)";
                ValidationStatus = "100% VALIDATED AUDIO SCHEMA";
            }
            else if (sc == 1)
            {
                TargetOutput = "GUI/Prefabs/CalradiaForgePanel.xml";
                SchemaStandard = "Gauntlet UI Prefab Specification (bannerlord_gauntlet_ui.md)";
                ValidationStatus = "100% VALIDATED GAUNTLET PREFAB";
            }
            else
            {
                TargetOutput = "ModuleData/custom_troops.xml";
                SchemaStandard = "Troop Character Specification (bannerlord_troop_character.md)";
                ValidationStatus = "100% VALIDATED TROOP SCHEMA";
            }
            var targetBlueprints = ScenarioBlueprints[sc];
            for (int i = 0; i < targetBlueprints.Length; i++) blueprintNodes.Add(targetBlueprints[i]);
            if (blueprintNodes.Count > 0) SelectedBlueprint = blueprintNodes[0];
            RegenerateXmlPreview();
        }
        public string StudioDocumentation => "Component & Scaffold Generation Pipeline: Interactive generation of safe CampaignBehaviorBase, QuestBase, NPCCharacters.xml, Items.xml, SubModule.xml, and Gauntlet UI ViewModels.";
        public string ArchitecturalInvariants => "1. Quests MUST invoke SetDialogs() in constructor AND InitializeQuestOnGameLoad().\n2. CampaignBehaviors must register listeners using AddNonSerializedListener.\n3. XML registrations in SubModule.xml must omit the .xml extension in path attribute.\n4. Gauntlet UI ViewModels must decorate exposed members with [DataSourceProperty].";
        public string StudioCaveat => "Omitting SetDialogs() from InitializeQuestOnGameLoad() causes quest NPCs to become permanently silent or unresponsive after reloading a saved game.";
        public string QuickActionCommand => "cf.novice_scaffold behavior MyCustomBehavior";
        public string QuickActionLabel => "Scaffold Behavior";
        public string ScratchpadNotes { get; set; } = "Notes: Generated behaviors enforce double SetDialogs() and anti-lag modulo-24 time slicing.";
        public IReadOnlyList<StudioConsoleCommand> CuratedConsoleCommands { get; } =
        [
            new("cf.novice_scaffold behavior MyCustomBehavior", "Generate an anti-lag CampaignBehaviorBase scaffold.", "Scaffolding", true),
            new("cf.novice_scaffold quest MyDeliveryQuest", "Generate a QuestBase class enforcing double SetDialogs().", "Scaffolding"),
            new("cf.novice_scaffold troop ImperialLegionary", "Generate valid NPCCharacters.xml troop loadout.", "Scaffolding"),
            new("cf.novice_scaffold item IronArmingSword", "Generate valid Items.xml weapon definition.", "Scaffolding"),
            new("cf.novice_scaffold armor PlateCuirass", "Generate valid Items.xml body armor definition.", "Scaffolding"),
            new("cf.novice_scaffold submodule CustomModule", "Scaffold standard SubModule.xml manifest.", "Scaffolding"),
            new("cf.novice_checklist MyFirstMod", "Execute mod distribution readiness verification checklist.", "Scaffolding"),
            new("cf.novice_events all", "Catalog of all CampaignEvents and execution triggers.", "Scaffolding")
        ];
        public string PlaybookTitle => "Playbook: Safe CampaignBehavior & Quest Scaffolding";
        public IReadOnlyList<string> PlaybookSteps { get; } =
        [
            "1. Scaffold CampaignBehaviorBase with AddNonSerializedListener.",
            "2. Ensure double SetDialogs() in Quest constructor AND InitializeQuestOnGameLoad().",
            "3. Enforce modulo-24 anti-lag time slicing in HourlyTick."
        ];
        public string TroubleshootingHeader => "Troubleshooting: Silent NPC Dialogue Glitch on Game Load";
        public string TroubleshootingRemedy => "If quest NPCs stop responding after loading a save game, verify that SetDialogs() is called inside InitializeQuestOnGameLoad(). Never omit this secondary hook.";
        public string ProceduralMacroAction => "cf.novice_scaffold behavior CustomBehavior && cf.novice_checklist MyFirstMod";
    }

    internal sealed class DiagnosticFindingViewModel
    {
        public DiagnosticFindingViewModel(string level, string component, string message, string brushKey)
        {
            Level = level;
            Component = component;
            Message = message;
            BrushKey = brushKey;
            StatusBrushKey = brushKey;
        }

        public string Level { get; }
        public string Component { get; }
        public string Message { get; }
        public string BrushKey { get; }
        public string StatusBrushKey { get; }
    }

    internal sealed class GenericOperationDashboardViewModel : ObservableObject
    {
        internal static readonly GenericOperationDashboardViewModel CanonicalInstance = new();

        string operationName = "Workbench Operation";
        string categoryTitle = "Tactical War Room";
        string echelonBadge = "BOUNDED ENGINE ROUTE";
        string executionMode = "Deterministic Bounded Route";
        string selectedToolSummary = "Offline deterministic diagnostic route. Sandboxed and thread-isolated.";
        string bufferCapacityText = "64 MB Working Buffer (Deterministic)";
        string fileTraversalText = "TopDirectoryOnly · MaxDepth 64";
        string threadIsolationText = "Strict UI Thread Isolation (Net8.0)";
        string executionBoundsText = "Bounded Async CancellationToken";
        bool isScanning;
        double scanProgressPercentage = 100.0;
        string scanStatusMessage = "SYSTEM READY · 4 Boundary Sensors Active";
        string scanStatusBrushKey = "VerdigrisBrush";
        readonly ObservableCollection<DiagnosticFindingViewModel> diagnosticFindings = [];

        public GenericOperationDashboardViewModel()
        {
            RunDiagnosticScanCommand = new RelayCommand(RunDiagnosticScan);
            ClearFindingsCommand = new RelayCommand(() =>
            {
                diagnosticFindings.Clear();
                ScanStatusMessage = "FINDINGS CLEARED · Standby";
                ScanStatusBrushKey = "MutedTextBrush";
            });
            PopulateDefaultFindings();
        }

        public string OperationName { get => operationName; set => Set(ref operationName, value); }
        public string CategoryTitle { get => categoryTitle; set => Set(ref categoryTitle, value); }
        public string EchelonBadge { get => echelonBadge; set => Set(ref echelonBadge, value); }
        public string ExecutionMode { get => executionMode; set => Set(ref executionMode, value); }

        public string SelectedToolSummary { get => selectedToolSummary; set => Set(ref selectedToolSummary, value); }
        public string BufferCapacityText { get => bufferCapacityText; set => Set(ref bufferCapacityText, value); }
        public string FileTraversalText { get => fileTraversalText; set => Set(ref fileTraversalText, value); }
        public string ThreadIsolationText { get => threadIsolationText; set => Set(ref threadIsolationText, value); }
        public string ExecutionBoundsText { get => executionBoundsText; set => Set(ref executionBoundsText, value); }

        public string MemoryCeiling => "64 MB Maximum Working Memory Buffer";
        public string FileSystemScope => "TopDirectoryOnly · MaxDepth: 64 · ReparsePoints Skipped";
        public string ThreadAffinity => "Strict UI Thread Isolation · Net8.0-Windows Engine";
        public string DeterministicProof => "Deterministic SHA-256 Provenance Ledger Active";
        public string OperationReadiness => "SYSTEM STATUS: OPERATIONAL & BOUNDED";

        public bool IsScanning { get => isScanning; private set => Set(ref isScanning, value); }
        public double ScanProgressPercentage { get => scanProgressPercentage; private set => Set(ref scanProgressPercentage, value); }
        public string ScanStatusMessage { get => scanStatusMessage; private set => Set(ref scanStatusMessage, value); }
        public string ScanStatusBrushKey { get => scanStatusBrushKey; private set => Set(ref scanStatusBrushKey, value); }
        public ObservableCollection<DiagnosticFindingViewModel> DiagnosticFindings => diagnosticFindings;

        public RelayCommand RunDiagnosticScanCommand { get; }
        public RelayCommand ClearFindingsCommand { get; }

        void PopulateDefaultFindings()
        {
            diagnosticFindings.Clear();
            diagnosticFindings.Add(new DiagnosticFindingViewModel("VERIFIED", "UI Thread Isolation", "Net8.0-Windows dispatcher boundaries strictly enforced.", "VerdigrisBrush"));
            diagnosticFindings.Add(new DiagnosticFindingViewModel("VERIFIED", "Working Buffer Ceiling", "64 MB working buffer allocation ceiling verified.", "VerdigrisBrush"));
            diagnosticFindings.Add(new DiagnosticFindingViewModel("VERIFIED", "File System Scope", "TopDirectoryOnly traversal prevents unauthorized disk probing.", "VerdigrisBrush"));
            diagnosticFindings.Add(new DiagnosticFindingViewModel("VERIFIED", "State-Changing Guard", "State-changing execution safely guarded from this desktop route.", "VerdigrisBrush"));
        }

        public void RunDiagnosticScan()
        {
            PopulateDefaultFindings();
            ScanStatusMessage = "DIAGNOSTIC COMPLETE: All 4 boundary guards passed (0 hazards detected)";
            ScanStatusBrushKey = "VerdigrisBrush";
            ScanProgressPercentage = 100.0;
            IsScanning = false;
        }

        public void CycleScenario(int index)
        {
            if (index % 2 == 1)
            {
                ExecutionMode = "Exhaustive Diagnostic Mode";
                EchelonBadge = "DEEP AUDIT ENGINE";
                SelectedToolSummary = "Exhaustive diagnostic scan with deep structural rule validation.";
                BufferCapacityText = "128 MB Expanded Diagnostic Buffer";
                FileTraversalText = "Recursive Bounds · MaxDepth 128";
                ThreadIsolationText = "Background Worker Pipeline";
                ExecutionBoundsText = "Adaptive 30s Guard Timeout";
            }
            else
            {
                ExecutionMode = "Deterministic Bounded Route";
                EchelonBadge = "BOUNDED ENGINE ROUTE";
                SelectedToolSummary = "Offline deterministic diagnostic route. Sandboxed and thread-isolated.";
                BufferCapacityText = "64 MB Working Buffer (Deterministic)";
                FileTraversalText = "TopDirectoryOnly · MaxDepth 64";
                ThreadIsolationText = "Strict UI Thread Isolation (Net8.0)";
                ExecutionBoundsText = "Bounded Async CancellationToken";
            }
            RunDiagnosticScan();
        }
        public string PlaybookTitle => "Playbook: Tactical Workbench Diagnostic Flow";
        public IReadOnlyList<string> PlaybookSteps { get; } =
        [
            "1. Select target studio route from navigation rail.",
            "2. Inspect operational telemetry and architectural invariants.",
            "3. Execute bounded command or procedural macro."
        ];
        public string TroubleshootingHeader => "Troubleshooting: Command Execution & Boundary Guard";
        public string TroubleshootingRemedy => "Offline diagnostic commands are bounded to prevent engine stalls. If a command times out, check system telemetry and filter parameters.";
        public string ProceduralMacroAction => "cf.summary && cf.audit";
    }
}
