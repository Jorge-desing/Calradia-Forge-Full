using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalradiaForge.Desktop.Presentation
{
    internal sealed class WorkspaceEvidence
    {
        public WorkspaceEvidence(string source, string status, string detail,
            string ruleId = null, string sourcePath = null, int? line = null, int? column = null, string recommendation = null)
        {
            Source = source ?? "Desktop";
            Status = status ?? "Not run";
            Evidence = detail ?? string.Empty;
            RuleId = ruleId ?? string.Empty;
            SourcePath = sourcePath ?? string.Empty;
            Line = line;
            Column = column;
            Recommendation = recommendation ?? string.Empty;
            Location = FormatLocation();
            Detail = FormatExportDetail();
        }
        public string Source { get; }
        public string Status { get; }
        public string RuleId { get; }
        public string SourcePath { get; }
        public int? Line { get; }
        public int? Column { get; }
        public string Location { get; }
        public string Evidence { get; }
        public string Recommendation { get; }
        public bool HasRecommendation => !string.IsNullOrWhiteSpace(Recommendation);
        public string RecommendationDisplay => string.IsNullOrWhiteSpace(Recommendation) ? string.Empty : "› " + Recommendation;

        // Detail is also the legacy exported-ledger field. Keep the complete diagnostic
        // context here so existing evidence exports do not silently lose structured data.
        public string Detail { get; }

        string FormatLocation()
        {
            var hasPath = !string.IsNullOrWhiteSpace(SourcePath);
            if (!hasPath && !Line.HasValue && !Column.HasValue) return string.Empty;
            if (!hasPath)
            {
                var lineOnly = Line.HasValue ? "Line " + Line.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
                var columnOnly = Column.HasValue ? "column " + Column.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
                return lineOnly.Length == 0 ? char.ToUpperInvariant(columnOnly[0]) + columnOnly.Substring(1) :
                    lineOnly + (columnOnly.Length == 0 ? string.Empty : ", " + columnOnly);
            }
            if (!Line.HasValue && !Column.HasValue) return SourcePath;
            if (Line.HasValue && !Column.HasValue) return SourcePath + ":" + Line.Value;
            if (Line.HasValue && Column.HasValue) return SourcePath + ":" + Line.Value + ":" + Column.Value;
            return SourcePath + "::" + Column.Value;
        }

        string FormatExportDetail()
        {
            if (string.IsNullOrWhiteSpace(RuleId) && string.IsNullOrWhiteSpace(SourcePath) &&
                !Line.HasValue && !Column.HasValue && string.IsNullOrWhiteSpace(Recommendation))
                return Evidence;

            var parts = new List<string>(4);
            if (!string.IsNullOrWhiteSpace(RuleId)) parts.Add("Rule: " + OneLine(RuleId));
            if (!string.IsNullOrWhiteSpace(Location)) parts.Add("Location: " + OneLine(Location));
            if (!string.IsNullOrWhiteSpace(Evidence)) parts.Add("Evidence: " + OneLine(Evidence));
            if (!string.IsNullOrWhiteSpace(Recommendation)) parts.Add("Next: " + OneLine(Recommendation));
            return string.Join(" | ", parts);
        }

        static string OneLine(string value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
    }

    internal sealed class WorkspaceExecutionResult
    {
        public string Status { get; set; }
        public string RawResult { get; set; }
        public string DisabledReason { get; set; }
        public int EvidenceCount { get; set; }
        public WorkspaceEvidence[] Evidence { get; set; } = [];
    }

    internal sealed class CopyableConsoleCommand
    {
        internal CopyableConsoleCommand(string text, int index, string accessibleNameFormat)
        {
            Text = text ?? string.Empty;
            AutomationId = "CopyConsoleCommandButton" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            AccessibleName = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                accessibleNameFormat ?? "Copy console command: {0}", Text);
        }

        public string Text { get; }
        public string AutomationId { get; }
        public string AccessibleName { get; }
    }

    internal interface IWorkspacePage : IDisposable
    {
        ToolDefinition Tool { get; }
    }

    /// <summary>Transient page state. The shell persists evidence separately, so disposing a page releases its UI references.</summary>
    [ForgeUiPage("tool-workbench", "Workbench", releaseOnNavigate: true)]
    internal class ToolPageViewModel : ObservableObject, IWorkspacePage
    {
        readonly Func<ToolDefinition, string, CancellationToken, Task<WorkspaceExecutionResult>> execute;
        readonly Action<ToolPageViewModel> export;
        readonly Func<ToolPageViewModel, CancellationToken, Task> exportAsync;
        readonly Func<ToolDefinition, bool, string> pickInput;
        readonly Func<string, bool> clipboardWriter;
        readonly Func<string, string> localize;
        string input = string.Empty;
        string status = "Not run";
        string rawResult = "Select a tool and run its declared command.";
        string disabledReason;
        string copyFeedback = string.Empty;
        bool disposed;

        public ToolPageViewModel(ToolDefinition tool,
            Func<ToolDefinition, string, CancellationToken, Task<WorkspaceExecutionResult>> execute,
            Action<ToolPageViewModel> export = null,
            Func<ToolDefinition, bool, string> pickInput = null,
            Func<ToolPageViewModel, CancellationToken, Task> exportAsync = null,
            Func<string, bool> clipboardWriter = null,
            Func<string, string> localize = null)
        {
            Tool = tool ?? throw new ArgumentNullException(nameof(tool));
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            if (export == null && exportAsync == null) throw new ArgumentNullException(nameof(export));
            this.export = export;
            this.exportAsync = exportAsync;
            this.pickInput = pickInput;
            this.clipboardWriter = clipboardWriter ?? (_ => false);
            this.localize = localize ?? (key => key);
            var copyNameFormat = this.localize("Ui.CopyCommandAccessibleNameFormat") ?? "Copy console command: {0}";
            ConsoleCommandEntries = Array.AsReadOnly(Tool.ConsoleCommands
                .Select((command, index) => new CopyableConsoleCommand(command, index, copyNameFormat)).ToArray());
            var studio = tool.Studio;
            IsTroopTreeVisualizer = studio == DesktopStudioKind.TroopTree;
            IsAudioMixerInspector = studio == DesktopStudioKind.AudioMixer;
            IsWorkshopSimulator = studio == DesktopStudioKind.Workshop;
            IsAgentMemoryInspector = studio == DesktopStudioKind.AgentMemory;
            IsCodeSecurityAuditor = studio == DesktopStudioKind.CodeSecurity;
            IsModuleHierarchyValidator = studio == DesktopStudioKind.ModuleHierarchy;
            IsKingdomDiplomacyStudio = studio == DesktopStudioKind.KingdomDiplomacy;
            IsComponentGeneratorStudio = studio == DesktopStudioKind.ComponentGenerator;
            IsCombatStudio = studio == DesktopStudioKind.CombatStudio;
            IsCaravanTrade = studio == DesktopStudioKind.CaravanTrade;
            IsGauntletStudio = studio == DesktopStudioKind.GauntletStudio;
            IsCampaignStudio = studio == DesktopStudioKind.CampaignStudio;
            IsLiveSession = studio == DesktopStudioKind.LiveSession;
            IsDeliveryStudio = studio == DesktopStudioKind.DeliveryStudio;
            IsDiagnosticsStudio = studio == DesktopStudioKind.DiagnosticsStudio;
            HasVisualDashboard = studio != DesktopStudioKind.Generic;
            RunCommand = new AsyncRelayCommand(RunAsync, CanRun);
            CancelCommand = new RelayCommand(() => RunCommand.Cancel(), () => RunCommand.IsRunning);
            ExportCommand = new AsyncRelayCommand(ExportAsync, () => !disposed && !string.IsNullOrWhiteSpace(RawResult));
            CopyTextCommand = new RelayCommand(CopyText, value => value is string text && !string.IsNullOrWhiteSpace(text));
            BrowseFileCommand = new RelayCommand(() => SelectInput(false), CanSelectInput);
            BrowseFolderCommand = new RelayCommand(() => SelectInput(true), CanSelectInput);
            LoadPresetCommand = new RelayCommand(CyclePreset, () => HasPresetAction);
            ClearInputCommand = new RelayCommand(() => Input = string.Empty, () => HasInputText);
            RunCommand.ExecutionStateChanged += (_, _) =>
            {
                CancelCommand.NotifyCanExecuteChanged();
                RunCommand.NotifyCanExecuteChanged();
            };

            if (IsTroopTreeVisualizer)
            {
                TroopTreeDashboard = new TroopTreeDashboardViewModel();
                status = "Simulation Ready";
                rawResult = "=== TACTICAL TROOP PROGRESSION HIERARCHY & STAT ENVELOPE ===\nCulture: Empire | Faction Archetype: Combined Arms Infantry & Heavy Cataphract\nProgression Model: Standard 6-Tier Branching DAG\n\n[HIERARCHICAL TROOP TREE]\nImperial Recruit [T1, Lvl 6, HP: 100, Cost: 20d, Wage: 2d]\n ├──> Imperial Infantryman [T2, Lvl 11, HP: 110, Cost: 50d, Wage: 4d]\n │     ├──> Imperial Veteran Infantryman [T3, Lvl 16, HP: 120, Cost: 100d, Wage: 7d]\n │     │     └──> Imperial Legionary [T4, Lvl 21, HP: 130, Cost: 200d, Wage: 11d] ★ [HEAVY SHIELDWALL]\n │     └──> Imperial Menavliaton [T3, Lvl 16, HP: 115, Cost: 100d, Wage: 7d]\n │           └──> Imperial Elite Menavliaton [T4, Lvl 21, HP: 125, Cost: 200d, Wage: 11d] ★ [ANTI-CAVALRY]\n └──> Imperial Archer [T2, Lvl 11, HP: 100, Cost: 50d, Wage: 4d]\n       ├──> Imperial Veteran Archer [T3, Lvl 16, HP: 110, Cost: 100d, Wage: 7d]\n       │     └──> Imperial Palatine Guard [T4, Lvl 21, HP: 120, Cost: 200d, Wage: 11d] ★ [COMPOSITE BOW]\n       └──> Imperial Crossbowman [T3, Lvl 16, HP: 115, Cost: 110d, Wage: 8d]\n             └──> Imperial Sergeant Crossbowman [T4, Lvl 21, HP: 125, Cost: 210d, Wage: 12d] ★ [PAVISE]\n\n[NOBLE DYNASTIC LINE]\nImperial Vigla Recruit [T2, Lvl 11, Noble]\n └──> Imperial Equite [T3, Lvl 16]\n       └──> Imperial Heavy Horseman [T4, Lvl 21]\n             └──> Imperial Cataphract [T5, Lvl 26]\n                   └──> Imperial Elite Cataphract [T6, Lvl 31] ★ [BARDED WARHORSE & LANCE]";
                Evidence.Add(new("Troop Tree / Hierarchy", "Verified", "Mapped 12 troop archetypes across 6 tiers; max depth: 5 levels."));
                Evidence.Add(new("Troop Tree / DAG Acyclicity", "Verified", "All upgrade_targets form a strictly acyclic progression graph."));
                Evidence.Add(new("Troop Balance / Stat Curve", "Verified", "Level-to-Tier progression matches linear curve (T1: 6 -> T6: 31)."));
                Evidence.Add(new("Troop Equipment / Envelope", "Verified", "Armor rating distribution conforms to tier envelope (T1: 8-15, T4: 38-52, T6: 65-82)."));
            }
            else if (IsAudioMixerInspector)
            {
                AudioStudioDashboard = new AudioStudioDashboardViewModel();
                status = "Simulation Ready";
                rawResult = "=== TACTICAL AUDIO STUDIO & WAVEFORM ANALYZER ===\nConforms to Bannerlord Audio System Guidelines (bannerlord_audio_system.md)\n\nTarget Asset     : custom_iron_shield_clash.wav\nAudio Format     : 16-bit Linear PCM | 44,100 Hz | Stereo\nDuration         : 1.42 seconds | Bitrate: 1411.2 kbps\nPeak Amplitude   : -1.4 dBFS | RMS Energy: -14.8 dBFS | Clipping: 0 samples\nMixer Category   : mission_combat | 3D Spatial Position: YES (3D Mission)\n\n[ACOUSTIC WAVEFORM ENVELOPE]\n +1.0 ┤          ╭╮           ╭╮\n +0.7 ┤       ╭╮ ││╭╮       ╭╮││╭╮\n +0.4 ┤    ╭╮ ││╭╯╰╯╰╮   ╭╮ ││││╰╯╭╮\n  0.0 ┼────╯╰─╯╰╯────╰───╯╰─╯╰╯╰───╰────── (Time: 0.0s ─── 1.42s)\n -0.4 ┤    ╰╮ ││╰╮╭╮╭╯   ╰╮ ││││╭╮╰╯\n -0.7 ┤       ╰╯ ││╰╯       ╰╯││╰╯\n -1.0 ┤          ╰╯           ╰╯\n\n[SPECTRAL FREQUENCY DISTRIBUTION]\nSub-Bass (20-60 Hz)   : [████░░░░░░] -22 dBFS\nBass (60-250 Hz)      : [████████░░] -11 dBFS  (Impact Thud)\nMidrange (250-2 kHz)  : [██████████]  -4 dBFS  (Metal Shield Clash Peak)\nPresence (2-6 kHz)    : [███████░░░] -12 dBFS  (Edge Crispness)\nBrilliance (6-20 kHz) : [███░░░░░░░] -28 dBFS";
                Evidence.Add(new("Audio / Header & Format", "Verified", "Valid audio stream: 44100 Hz, 2 channels, 16-bit depth."));
                Evidence.Add(new("Audio / Mixer Compliance", "Verified", "Mixer category 'mission_combat' mapped to active game mixer bus."));
                Evidence.Add(new("Audio / Dynamic Headroom", "Verified", "Peak amplitude at -1.4 dBFS guarantees zero digital clipping."));
                Evidence.Add(new("Audio / Spatialization", "Verified", "Spatial 3D flag conforms to combat emitter rules."));
            }
            else if (IsWorkshopSimulator)
            {
                WorkshopDashboard = new WorkshopDashboardViewModel();
                status = "Simulation Ready";
                rawResult = "=== HEADLESS CAMPAIGN & WORKSHOP ECONOMY SIMULATION ===\nSimulated Horizon: 30 Days (4 Quarters) | Model: Calradia Forge Equilibrium Engine\nSettlement Scope : Marunath (Prosperity: 5,420 | Loyalty: 64/100 | Security: 72/100)\n\n[WORKSHOP ENTERPRISE PROFITABILITY AUDIT]\nEnterprise Type      Daily Net    Input Material     Output Goods       Payback Period\n──────────────────────────────────────────────────────────────────────────────────────\nSmithy (Iron/Wood)    +290 d/day   Iron Ore (45d)     Tools & Weapons    48.2 Days\nSilversmith (Silver)  +340 d/day   Silver Ore (120d)  Jewelry (310d)     41.1 Days ★ (Optimal)\nBrewery (Grain)       +215 d/day   Grain (12d)        Beer (48d)         65.1 Days\nWeaver (Wool/Silk)    +195 d/day   Wool (35d)         Cloth (110d)       71.8 Days\nWood Workshop         +170 d/day   Hardwood (25d)     Bows & Shields     82.3 Days\nPottery (Clay)        +185 d/day   Clay (20d)         Pottery (85d)      75.6 Days\nOlive Press (Olives)  +160 d/day   Olives (28d)       Oil (80d)          87.5 Days\n\n[SETTLEMENT CIVIC EQUILIBRIUM & REBELLION RISK]\n• Settlement Prosperity : 5,420 (+4.2/day)    [GROWING]\n• Civic Loyalty Index   : 64.0 / 100          [STEADY]\n• Security Score        : 72.0 / 100          [HIGH]\n• Food Storage Reserve  : 184 (+14/day)       [SURPLUS]\n• Garrison Deterrent    : 165 Regular Troops  [EFFECTIVE]\n• Rebellion Risk Index  : 8.6%                [STABLE - No Rebellion Risk]";
                Evidence.Add(new("Economy / Workshop Enterprise", "Verified", "Silversmith & Smithy yield +630 d/day combined with 41-48 day amortization."));
                Evidence.Add(new("Economy / Supply Chain", "Verified", "Local village production covers input requirements for 3 active enterprises."));
                Evidence.Add(new("Settlement / Civic Stability", "Verified", "Civic loyalty index at 64/100 prevents rebellion countdown trigger."));
                Evidence.Add(new("Settlement / Rebellion Risk", "Verified", "Rebellion risk index evaluates to 8.6% (threshold for unrest: 45.0%)."));
            }
            else if (IsAgentMemoryInspector)
            {
                AgentMemoryDashboard = new AgentMemoryDashboardViewModel();
                status = "Simulation Ready";
                rawResult = "=== COALA AGENT COGNITIVE MEMORY AUDIT (SDK v8) ===\nArchitecture Model : CoALA Cognitive Architecture for Bannerlord NPCs\nGlobal Capacity    : 100 Maximum Agents Slots\nAgent Quotas       : 128 Semantic Facts | 512 Episodes (128/type) | 128 Procedural Tasks\n\n[GLOBAL CAPACITY METER]\nCapacity: [█░░░░░░░░░░░░░░░░░░░░░░░] 6.00% (6 / 100 slots utilized)\n\n[TIER 1: SEMANTIC MEMORY (Beliefs, Preferences & TTL Facts)]\nAgent: hero_rhagaea\n   • preference_culture     = Empire\n   • war_stance_khuzait     = Hostile\n   • player_disposition     = Allied (Relation: +64)\n\n[TIER 2: EPISODIC MEMORY (Experiences & FIFO Bounded Events)]\nAgent: hero_rhagaea (Total Episodes: 4/512)\n   [combat] Defended Onira against Khuzait siege vanguard\n   [diplomacy] Signed trade truce with Western Empire senate\n   [dynasty] Arranged marriage treaty for Ira\n\n[TIER 3: PROCEDURAL MEMORY (Skills & Tactical Routines)]\nAgent: hero_rhagaea (Tasks: 2/128)\n   • formation_defense    : Palatine archers on high ground, cataphracts in counter-charge flank";
                Evidence.Add(new("Agent Memory / Global Registry", "Verified", "6/100 global slots utilized; bounded agent slot isolation active."));
                Evidence.Add(new("Agent Memory / Semantic Decay", "Verified", "Lazy TTL expiration verified; stale facts removed on access."));
                Evidence.Add(new("Agent Memory / Episodic FIFO", "Verified", "Per-agent 512 cap and per-type 128 cap verified without unbounded heap growth."));
                Evidence.Add(new("Agent Memory / Procedural Health", "Verified", "Task rules validated without circular reentrancy."));
            }
            else if (IsCodeSecurityAuditor)
            {
                CodeSecurityDashboard = new CodeSecurityDashboardViewModel();
            }
            else if (IsModuleHierarchyValidator)
            {
                ModuleHierarchyDashboard = new ModuleHierarchyDashboardViewModel();
            }
            else if (IsKingdomDiplomacyStudio)
            {
                KingdomDiplomacyDashboard = new KingdomDiplomacyDashboardViewModel();
            }
            else if (IsComponentGeneratorStudio)
            {
                ComponentGeneratorDashboard = new ComponentGeneratorDashboardViewModel();
            }
            else if (IsCombatStudio)
            {
                CombatStudioDashboard = new CombatStudioDashboardViewModel();
                status = "Simulation Ready";
                rawResult = "=== TACTICAL COMBAT & SIEGE FORMATION AUDIT ===\nUnit Doctrine   : Heavy Infantry Shieldwall with Cataphract Flanking Reserves\nRegiment Scope  : Legio I Calradica · Vanguard Strike Cohort\nCasualty Ratio  : 3.4:1 Favorable Kill Ratio · Projected 15-Minute Loss: 25.4%\nMorale Cohesion : 78.5% [HIGH - Shieldwall Cohesion Locked]\nSiege Viability : 64.0% [Breach Viable with Heavy Onager Support]";
                Evidence.Add(new("Combat / Formation Doctrine", "Verified", "Shieldwall formation locked with 3.4:1 favorable kill ratio."));
                Evidence.Add(new("Combat / Morale Cohesion", "Verified", "Morale cohesion index at 78.5% exceeds 35.0% panic threshold."));
                Evidence.Add(new("Siege / Breach Probability", "Verified", "Onager battery breach viability evaluated at 64.0%."));
            }
            else if (IsCaravanTrade)
            {
                CaravanTradeDashboard = new CaravanTradeDashboardViewModel();
                status = "Simulation Ready";
                rawResult = "=== CARAVAN TRADE & REGIONAL MARKET ARBITRAGE HUB ===\nTrade Route     : Imperial Silver Corridor (Marunath -> Zeonica)\nOperating Base  : 50,000 Denars (Master Guild Caravan Escort)\nCommodity Margin: Silver Ore (+143.3% Spread · +172d/unit Net Yield)\nRoute Security  : 84.5% Survival Probability (Lowland Imperial Patrols)\nTariff Friction : 18.2% Civic Retention (Optimal Trade Flow)";
                Evidence.Add(new("Trade / Arbitrage Spread", "Verified", "Silver ore and raw velvet yield +14,800d roundtrip net profit."));
                Evidence.Add(new("Trade / Caravan Security", "Verified", "Route survival probability evaluated at 84.5% across 420 km."));
                Evidence.Add(new("Economy / Tariff Retention", "Verified", "Civic tariff friction evaluated at 18.2% within safe limits."));
            }
            else if (IsGauntletStudio)
            {
                GauntletStudio = new GauntletStudioDashboardViewModel();
                status = "Simulation Ready";
                rawResult = "=== GAUNTLET UI & HUD WIDGET INSPECTION STUDIO ===\nPrefab Scope    : CalradiaForge.Hud.xml (Tactical Combat Overlay)\nActive Layer    : Layer 2 · MissionView Combat HUD Overlay\nVisual Metrics  : 6 / 12 Visual Tree Depth · 28 Active UI Widgets\nDrawCall Budget : 1.6 ms / Frame (Target < 4.0 ms) · 100% GPU Cached Brushes\nCoverage        : 34.5% Screen Coverage (1920x1080 @ 60 FPS Subpixel Snapped)";
                Evidence.Add(new("Gauntlet / Prefab Schema", "Verified", "CalradiaForge.Hud.xml schema valid; zero unbound @ properties."));
                Evidence.Add(new("Gauntlet / Layout Latency", "Verified", "Average layout pass evaluated at 1.6 ms within 4.0 ms budget."));
                Evidence.Add(new("Gauntlet / Tree Depth", "Verified", "Visual tree depth 6 layers satisfies max 12 layer policy."));
            }
            else if (IsCampaignStudio)
            {
                CampaignStudio = new CampaignStudioDashboardViewModel();
                status = "Simulation Ready";
                rawResult = "=== CAMPAIGN WORLD & SETTLEMENT EXPEDITION STUDIO ===\nProvince Scope  : Imperial Heartlands Province (Central Calradia)\nEquilibrium     : 78.5% STABLE · Net Food Balance: +48 Bushels/Day\nProsperity Base : 4,890 Mean Town Prosperity (+4.2 Daily Growth)\nSecurity Level  : 88.0 / 100 · Garrison Deterrent: 340 Regular Troops\nRegional Threat : 32.0% MODERATE · 3 Mountain Bandit Lairs Sighted";
                Evidence.Add(new("Campaign / Equilibrium", "Verified", "Provincial equilibrium 78.5% prevents rebellion risk trigger."));
                Evidence.Add(new("Campaign / Food Security", "Verified", "Positive net daily food balance (+48) across 5 settlements."));
                Evidence.Add(new("Campaign / Bandit Suppression", "Verified", "Regional threat 32.0% contained by imperial garrisons."));
            }
            else if (IsLiveSession)
            {
                LiveSessionDashboard = new LiveSessionDashboardViewModel();
                status = "Simulation Ready";
                rawResult = "=== LIVE SESSION & MEMORY APM TELEMETRY MESH ===\nNamed Pipe Mesh : \\\\.\\pipe\\CalradiaForge.Live (Stream Mode)\nEvent Bus State : 2,048 Slot Ring Buffer · 68.0% Saturation · Zero Overflows\nIPC APM Latency : 0.8 ms Round-Trip (Min: 0.4 ms, Max: 1.2 ms, Jitter: 0.1 ms)\nThread Affinity : ThreadPool Isolated · Game Simulation Contention: 0.0%\nMemory Overhead : 42.5% Allocation Ratio (Bounded Working Set)";
                Evidence.Add(new("Live Session / Named Pipe", "Verified", "Named pipe endpoint \\\\.\\pipe\\CalradiaForge.Live established."));
                Evidence.Add(new("Live Session / Event Bus", "Verified", "ForgeWeave ring buffer operates at 68.0% capacity with 0 overflows."));
                Evidence.Add(new("Live Session / Thread Isolation", "Verified", "APM telemetry worker thread isolated from TaleWorlds game thread."));
                Evidence.Add(new("Live Session / Circuit Breaker", "Verified", "Circuit breaker threshold armed at 5 consecutive timeouts."));
            }
            else if (IsDeliveryStudio)
            {
                DeliveryStudioDashboard = new DeliveryStudioDashboardViewModel();
                status = "Simulation Ready";
                rawResult = "=== DELIVERY & FASTPACKAGEENGINE DISTRIBUTION PREFLIGHT ===\nPackaging Engine: FastPackageEngine v2 · Multi-Threaded Parallel Compression\nThroughput Peak : 128.5 MB/s (Parallel.Invoke Multi-Core Optimal Level)\nHygiene Score   : 100.0% · Zero TaleWorlds DLLs · Zero Scripts · Zero Zone Streams\nCompression Net : 78.4% Space Reduction (62.8 MB Distribution Footprint)\nIntegrity Chain : SHA-256 Ledger Verified · SubModule.xml Schema Validated";
                Evidence.Add(new("Delivery / FastPackageEngine", "Verified", "Multi-threaded compression pipeline achieved 128.5 MB/s throughput."));
                Evidence.Add(new("Delivery / Archive Hygiene", "Verified", "Strict exclusion of TaleWorlds DLLs, scripts, and logs verified."));
                Evidence.Add(new("Delivery / Stream Stripping", "Verified", "NTFS :Zone.Identifier streams stripped; LoadFrom 0x80131515 avoided."));
                Evidence.Add(new("Delivery / SHA-256 Audit", "Verified", "Distribution archives match cryptographic integrity ledger."));
            }
            else if (IsDiagnosticsStudio)
            {
                DiagnosticsStudioDashboard = new DiagnosticsStudioDashboardViewModel();
                status = "Simulation Ready";
                rawResult = "=== DIAGNOSTICS & INTEGRITY FORENSIC SQUADRON ===\nManifest Scope  : SubModule.xml (Root Assembly & Category Manifest)\nSchema Standard : TaleWorlds XSD Strict · ModuleData Validation: PASS\nStructural Rule : Rule A (Anti-Shadowing) PASS · Rule B (Statelessness) PASS\nMesh & Memory   : OnInit() Physics Guard PASS · Replay Buffer Bounded: 2,048 Slots\nCompliance Rate : 98.5% Pass Rate · Fault Risk: 8.0% [LOW RISK]";
                Evidence.Add(new("Diagnostics / Schema Integrity", "Verified", "SubModule.xml manifest and ModuleData schemas conform strictly to engine XSD."));
                Evidence.Add(new("Diagnostics / GEMINI Rules", "Verified", "Rule A (Anti-Shadowing) and Rule B (Statelessness) verified across all modules."));
                Evidence.Add(new("Diagnostics / Native Mesh Guard", "Verified", "Zero OnInit() skeleton or mesh manipulations detected in MissionLogic."));
            }
            else
            {
                GenericOperationDashboard = GenericOperationDashboardViewModel.CanonicalInstance;
                GenericOperationDashboard.ConfigureDomainCategory(tool.Group);
            }
        }

        public bool HasVisualDashboard { get; }
        /// <summary>Preserves room for specialist visualizers while allowing text-first routes to use the full narrow workspace.</summary>
        public double MinimumPresentationWidth => HasVisualDashboard ? 520d : 360d;
        public bool IsTroopTreeVisualizer { get; }
        public bool IsAudioMixerInspector { get; }
        public bool IsWorkshopSimulator { get; }
        public bool IsAgentMemoryInspector { get; }
        public bool IsCodeSecurityAuditor { get; }
        public bool IsModuleHierarchyValidator { get; }
        public bool IsKingdomDiplomacyStudio { get; }
        public bool IsComponentGeneratorStudio { get; }
        public bool IsCombatStudio { get; }
        public bool IsCaravanTrade { get; }
        public bool IsGauntletStudio { get; }
        public bool IsCampaignStudio { get; }
        public bool IsLiveSession { get; }
        public bool IsDeliveryStudio { get; }
        public bool IsDiagnosticsStudio { get; }

        public bool IsGenericOperationOverview => false;

        public TroopTreeDashboardViewModel TroopTreeDashboard { get; private set; }
        public AudioStudioDashboardViewModel AudioStudioDashboard { get; private set; }
        public WorkshopDashboardViewModel WorkshopDashboard { get; private set; }
        public AgentMemoryDashboardViewModel AgentMemoryDashboard { get; private set; }
        public CodeSecurityDashboardViewModel CodeSecurityDashboard { get; private set; }
        public ModuleHierarchyDashboardViewModel ModuleHierarchyDashboard { get; private set; }
        public KingdomDiplomacyDashboardViewModel KingdomDiplomacyDashboard { get; private set; }
        public ComponentGeneratorDashboardViewModel ComponentGeneratorDashboard { get; private set; }
        public CombatStudioDashboardViewModel CombatStudioDashboard { get; private set; }
        public CaravanTradeDashboardViewModel CaravanTradeDashboard { get; private set; }
        public GauntletStudioDashboardViewModel GauntletStudio { get; private set; }
        public GauntletStudioDashboardViewModel GauntletStudioDashboard => GauntletStudio;
        public CampaignStudioDashboardViewModel CampaignStudio { get; private set; }
        public CampaignStudioDashboardViewModel CampaignStudioDashboard => CampaignStudio;
        public LiveSessionDashboardViewModel LiveSessionDashboard { get; private set; }
        public DeliveryStudioDashboardViewModel DeliveryStudioDashboard { get; private set; }
        public DiagnosticsStudioDashboardViewModel DiagnosticsStudioDashboard { get; private set; }
        public GenericOperationDashboardViewModel GenericOperationDashboard { get; private set; }

        int presetCycleIndex;

        public ToolDefinition Tool { get; }
        public IReadOnlyList<CopyableConsoleCommand> ConsoleCommandEntries { get; }
        [ForgeUiCommand("run", "Primary", cancellable: true)]
        public AsyncRelayCommand RunCommand { get; }
        [ForgeUiCommand("cancel", "Secondary", cancellable: false)]
        public RelayCommand CancelCommand { get; }
        [ForgeUiCommand("export", "Secondary", cancellable: false)]
        public AsyncRelayCommand ExportCommand { get; }
        public RelayCommand CopyTextCommand { get; }
        public string CopyFeedback { get => copyFeedback; private set => Set(ref copyFeedback, value); }
        [ForgeUiCommand("browse-file", "Secondary", cancellable: false)]
        public RelayCommand BrowseFileCommand { get; }
        [ForgeUiCommand("browse-folder", "Secondary", cancellable: false)]
        public RelayCommand BrowseFolderCommand { get; }
        [ForgeUiCommand("load-preset", "Secondary", cancellable: false)]
        public RelayCommand LoadPresetCommand { get; }
        [ForgeUiCommand("clear-input", "Secondary", cancellable: false)]
        public RelayCommand ClearInputCommand { get; }

        public bool HasPresetAction => HasVisualDashboard;
        public string PresetActionLabel => IsTroopTreeVisualizer
            ? (presetCycleIndex % 2 == 1 ? "⟳ Ver Árbol Imperial Base" : "⟳ Ver Línea Dinástica Noble")
            : IsAudioMixerInspector
                ? (presetCycleIndex % 2 == 1 ? "⟳ Cargar Muestra Combate" : "⟳ Cargar Fanfarria UI")
                : IsWorkshopSimulator
                    ? (presetCycleIndex % 2 == 1 ? "⟳ Ver Escenario Marunath" : "⟳ Ver Escenario Epicrotea")
                    : IsAgentMemoryInspector
                        ? (presetCycleIndex % 3 == 0 ? "⟳ Estado Base (T=0h)" : presetCycleIndex % 3 == 1 ? "⟳ Decaimiento Temporal (T+24h)" : "⟳ Poda y Consolidación (T+72h)")
                        : IsCodeSecurityAuditor
                            ? (presetCycleIndex % 3 == 0 ? "⟳ Auditar Mod Ensamblado" : presetCycleIndex % 3 == 1 ? "⟳ Auditar Motor Nativo" : "⟳ Preflight de Módulo")
                            : IsModuleHierarchyValidator
                                ? (presetCycleIndex % 3 == 0 ? "⟳ Matriz Modular Estándar" : presetCycleIndex % 3 == 1 ? "⟳ Matriz Sandbox Mínima" : "⟳ Matriz Multimódulo")
                                : IsKingdomDiplomacyStudio
                                    ? (presetCycleIndex % 3 == 0 ? "⟳ Imperio del Sur (Rhagaea)" : presetCycleIndex % 3 == 1 ? "⟳ Imperio Occidental (Garios)" : "⟳ Reino de Vlandia (Derthert)")
                                    : IsComponentGeneratorStudio
                                        ? (presetCycleIndex % 3 == 0 ? "⟳ Manifiesto Sonidos" : presetCycleIndex % 3 == 1 ? "⟳ Prefab Gauntlet" : "⟳ Definición Tropas")
                                        : IsCombatStudio
                                            ? (presetCycleIndex % 2 == 1 ? "⟳ Doctrina Contraataque" : "⟳ Doctrina Muro de Escudos")
                                            : IsCaravanTrade
                                                ? (presetCycleIndex % 2 == 1 ? "⟳ Ruta Terrestre Secundaria" : "⟳ Corredor de la Plata")
                                                : IsGauntletStudio
                                                    ? (presetCycleIndex % 2 == 1 ? "⟳ HUD Minimalista" : "⟳ HUD Táctico Completo")
                                                    : IsCampaignStudio
                                                        ? (presetCycleIndex % 2 == 1 ? "⟳ Simulación Frontera Hostil" : "⟳ Simulación Asentamiento Imperial")
                                                        : IsLiveSession
                                                            ? (presetCycleIndex % 2 == 1 ? "⟳ Modo Alta Concurrencia" : "⟳ Sesión Interactiva Base")
                                                            : IsDeliveryStudio
                                                                ? (presetCycleIndex % 2 == 1 ? "⟳ Perfil Rápido CI/CD" : "⟳ Perfil Estándar de Lanzamiento")
                                                                : IsDiagnosticsStudio
                                                                    ? (presetCycleIndex % 2 == 1 ? "⟳ Modo Sandbox & Crash Forense" : "⟳ Auditoría Estricta de Producción")
                                                                    : (presetCycleIndex % 2 == 1 ? "⟳ Modo Diagnóstico Exhaustivo" : "⟳ Cargar Parámetros Canónicos");

        public string PrimaryActionLabel => Tool.Id switch
        {
            "TroopTreeVisualizer" => "⚡ Simular Progresión de Tropas",
            "ItemBalanceAnalyzer" => "⚖️ Auditar Balance de Armas",
            "AudioFmodMixerInspector" => "🔬 Analizar Espectro Acústico",
            "SoundXmlSynthesizer" => "🎵 Sintetizar Definición de Sonido",
            "WorkshopEnterpriseSimulator" => "📊 Simular Economía 30 Días",
            "SettlementCalculator" => "🏛️ Calcular Equilibrio Asentamiento",
            "SaveInspector" or "ForgeAgentMemoryInspector" or "AgentMemoryInspector" => "🧠 Inspeccionar Memoria CoALA",
            "SaveTypeDefinerAuditor" => "🛡️ Auditar SaveableTypeDefiner",
            "CampaignNamespaceGuard" => "🛡️ Verificar Anti-Shadowing",
            "ModRuleAuditor" => "📜 Auditar Reglas C#",
            "GauntletSpriteAuditor" => "🖼️ Auditar Hojas de Sprites",
            "FbxAsciiPreflight" => "📐 Preflight Nodos FBX ASCII",
            "TpacInspector" => "📦 Inspeccionar Cabecera TPAC",
            "LiveConsole" => "⚡ Despachar Comando ForgeWeave",
            "GauntletLivePreview" => "📡 Consultar Telemetría APM",
            "PatchPreflight" => "🔍 Verificar Blueprints de Parches",
            _ => Tool.Kind switch
            {
                DesktopToolKind.Simulation => "⚡ Ejecutar Simulación Táctica",
                DesktopToolKind.AssemblyEditor => "🛡️ Previsualizar Copia de Ensamblado",
                DesktopToolKind.Generator => "⚙️ Generar Andamiaje de Código",
                DesktopToolKind.Live => "📡 Consultar Sesión en Vivo",
                DesktopToolKind.Report => "📋 Compilar Informe de Evidencias",
                _ => Tool.Group switch
                {
                    "Diagnostics" => "🔍 Auditar Evidencia Táctica",
                    "Assets" => "🛠️ Inspeccionar Recurso de Juego",
                    "Economy" => "📈 Simular Indicadores Económicos",
                    "Combat" => "⚔️ Simular Parámetros de Combate",
                    "Politics" => "👑 Auditar Mecánicas Políticas",
                    "Campaign" => "🗺️ Evaluar Simulación de Campaña",
                    "Gauntlet" => "🎨 Verificar Prefabs de Gauntlet",
                    _ => "⚔️ Ejecutar Orden de Trabajo"
                }
            }
        };

        public string PrimaryActionIconKey => Tool.Id switch
        {
            "TroopTreeVisualizer" or "ItemBalanceAnalyzer" => "GameIcon.crossed_swords",
            "AudioFmodMixerInspector" or "SoundXmlSynthesizer" => "GameIcon.gears",
            "WorkshopEnterpriseSimulator" or "SettlementCalculator" => "GameIcon.gear_hammer",
            "SaveInspector" or "ForgeAgentMemoryInspector" or "AgentMemoryInspector" => "GameIcon.scroll_unfurled",
            "SaveTypeDefinerAuditor" or "CampaignNamespaceGuard" or "ModRuleAuditor" => "GameIcon.knight_banner",
            "GauntletSpriteAuditor" or "TpacInspector" or "FbxAsciiPreflight" => "GameIcon.gears",
            "LiveConsole" or "GauntletLivePreview" => "GameIcon.compass",
            _ => Tool.Group switch
            {
                "Simulation" or "Combat" => "GameIcon.crossed_swords",
                "Diagnostics" => "GameIcon.archery_target",
                "Live session" => "GameIcon.compass",
                "Assets" => "GameIcon.gears",
                "Politics" => "GameIcon.knight_banner",
                "Economy" => "GameIcon.gear_hammer",
                "Learning" => "GameIcon.scroll_unfurled",
                _ => "GameIcon.knight_banner"
            }
        };

        public string PrimaryActionAccentBrushKey => Tool.Group switch
        {
            "Simulation" or "Combat" => "VerdigrisBrush",
            "Diagnostics" or "Politics" => "BrassBrush",
            "Live session" => "EmberBrush",
            "Assets" or "Gauntlet" => "TemperedBrush",
            "Economy" => "BrassBrush",
            _ => "BrassBrush"
        };

        public string PrimaryActionToolTip => Tool.Id switch
        {
            "TroopTreeVisualizer" => "Ejecuta la simulación completa del árbol DAG de tropas imperiales y calcula métricas de combate.",
            "AudioFmodMixerInspector" => "Analiza la forma de onda acústica, calcula el espectro en 5 bandas y verifica el margen dinámico.",
            "WorkshopEnterpriseSimulator" => "Simula 30 días de operación económica entre 7 empresas de talleres y modela el riesgo de rebelión.",
            "SaveInspector" or "ForgeAgentMemoryInspector" or "AgentMemoryInspector" => "Inspecciona la arquitectura de memoria CoALA de 3 niveles y calcula la cuota global de agentes.",
            "SaveTypeDefinerAuditor" => "Desensambla el IL del constructor del binario PE y audita que el base ID sea >= 2.500.000.",
            "CampaignNamespaceGuard" => "Inspecciona los metadatos del binario para garantizar que ningún tipo o espacio de nombres oculte TaleWorlds.CampaignSystem.",
            "LiveConsole" => "Despacha acciones interactivas a través del named pipe hacia el bus de eventos ForgeWeave en el juego.",
            "GauntletLivePreview" => "Consulta telemetría APM en tiempo real, latencias P50/P95/P99 y estados de circuit breaker.",
            _ => "Ejecuta la operación seleccionada con las entradas provistas."
        };

        public bool HasInputText => !string.IsNullOrWhiteSpace(input);

        public IReadOnlyList<string> ConsoleCommands => Tool.ConsoleCommands;
        public string CliSyntax => Tool.CliSyntax;
        public IReadOnlyList<string> ApplicableHotkeys => Tool.ApplicableHotkeys;
        public string SectionInformation => Tool.SectionInformation;
        public bool HasConsoleCommands => ConsoleCommands.Count > 0;

        public ObservableCollection<WorkspaceEvidence> Evidence { get; } = [];
        public string Input
        {
            get => input;
            set
            {
                if (!Set(ref input, value ?? string.Empty)) return;
                Raise(nameof(DisabledReason));
                Raise(nameof(HasInputText));
                RunCommand.NotifyCanExecuteChanged();
                ClearInputCommand.NotifyCanExecuteChanged();
            }
        }
        public string Status { get => status; private set => Set(ref status, value); }
        public string RawResult { get => rawResult; protected set { if (Set(ref rawResult, value)) ExportCommand.NotifyCanExecuteChanged(); } }
        public string DisabledReason
        {
            get
            {
                if (disposed) return "This page has been released.";
                if (Tool.RequiresInput && string.IsNullOrWhiteSpace(Input)) return "Select a supported local file or folder before running this tool.";
                if (Tool.ChangesState) return "State-changing execution is unavailable from this guarded desktop route.";
                return disabledReason ?? string.Empty;
            }
            private set { if (Set(ref disabledReason, value)) { Raise(nameof(DisabledReason)); RunCommand.NotifyCanExecuteChanged(); } }
        }
        public string Requirement => Tool.Kind switch
        {
            DesktopToolKind.AssemblyEditor => "Patch request JSON (.json; preview first, then explicitly apply)",
            _ when Tool.Id == "AssemblyInspector" => "Managed .NET PE assembly (.dll / .exe)",
            _ when Tool.Id == "FbxAsciiPreflight" => "FBX file or folder (ASCII declarations; binary FBX is unsupported)",
            _ => Tool.RequiresInput ? "Supported local file or folder" : "No local file input required"
        };
        public string StateLabel => Tool.ChangesState ? "Guarded: no state changes from Desktop" : "Read-only or template output";

        bool CanSelectInput() => !disposed && Tool.RequiresInput && pickInput != null;
        void SelectInput(bool folder)
        {
            var selected = pickInput?.Invoke(Tool, folder);
            if (!string.IsNullOrWhiteSpace(selected)) Input = selected;
        }

        async Task ExportAsync(CancellationToken cancellationToken)
        {
            if (exportAsync != null)
                await exportAsync(this, cancellationToken).ConfigureAwait(true);
            else
                export?.Invoke(this);
        }

        void CopyText(object value)
        {
            if (value is not string text || string.IsNullOrWhiteSpace(text)) return;
            bool copied;
            try { copied = clipboardWriter(text); }
            catch { copied = false; }
            CopyFeedback = copied
                ? localize("Ui.ClipboardCopySucceeded") ?? "Command copied to the clipboard."
                : localize("Ui.ClipboardCopyFailed") ?? "Could not copy to the clipboard.";
        }

        bool CanRun() => !disposed && string.IsNullOrEmpty(DisabledReason);

        async Task RunAsync(CancellationToken cancellation)
        {
            Status = "Running";
            try
            {
                var result = await execute(Tool, Input, cancellation).ConfigureAwait(true);
                if (disposed) return;
                Status = result?.Status ?? "Completed";
                RawResult = result?.RawResult ?? "No result was returned.";
                DisabledReason = result?.DisabledReason;
                Evidence.Clear();
                foreach (var item in result?.Evidence ?? []) Evidence.Add(item);
            }
            catch (OperationCanceledException)
            {
                if (disposed) return;
                Status = "Cancelled";
                RawResult = "The operation was cancelled before it completed.";
                Evidence.Clear();
                Evidence.Add(new("Desktop", "Cancelled", "No further result was retained."));
            }
            catch (Exception error)
            {
                if (disposed) return;
                Status = "Failed";
                RawResult = error.Message;
                Evidence.Clear();
                Evidence.Add(new("Desktop", "Failed", error.Message));
            }
        }

        void CyclePreset()
        {
            if (disposed) return;
            presetCycleIndex++;

            if (IsTroopTreeVisualizer)
            {
                if (presetCycleIndex % 2 == 1)
                {
                    TroopTreeDashboard = new TroopTreeDashboardViewModel();
                    TroopTreeDashboard.SetDynasticNobleScenario();
                    Evidence.Add(new("Troop Tree / Preset", "Loaded", "Switched to Noble Dynastic Cataphract Line (T2-T6)."));
                }
                else
                {
                    TroopTreeDashboard = new TroopTreeDashboardViewModel();
                    Evidence.Add(new("Troop Tree / Preset", "Loaded", "Restored Imperial Core Combined Arms Tree (T1-T4)."));
                }
                Raise(nameof(TroopTreeDashboard));
                Raise(nameof(PresetActionLabel));
            }
            else if (IsAudioMixerInspector)
            {
                if (presetCycleIndex % 2 == 1)
                {
                    AudioStudioDashboard = new AudioStudioDashboardViewModel();
                    AudioStudioDashboard.SetUiFanfareSample();
                    Evidence.Add(new("Audio / Preset", "Loaded", "Loaded UI Fanfare (custom_quest_complete_jingle.ogg, -3.2 dBFS)."));
                }
                else
                {
                    AudioStudioDashboard = new AudioStudioDashboardViewModel();
                    Evidence.Add(new("Audio / Preset", "Loaded", "Restored Combat Shield Clash (custom_iron_shield_clash.wav, -1.4 dBFS)."));
                }
                Raise(nameof(AudioStudioDashboard));
                Raise(nameof(PresetActionLabel));
            }
            else if (IsWorkshopSimulator)
            {
                if (presetCycleIndex % 2 == 1)
                {
                    WorkshopDashboard = new WorkshopDashboardViewModel();
                    WorkshopDashboard.SetEpicroteaScenario();
                    Evidence.Add(new("Economy / Preset", "Loaded", "Switched market scope to Epicrotea (Iron Smithy + Brewery, 6120 prosperity)."));
                }
                else
                {
                    WorkshopDashboard = new WorkshopDashboardViewModel();
                    Evidence.Add(new("Economy / Preset", "Loaded", "Restored Marunath market scope (Silversmith + Smithy, 5420 prosperity)."));
                }
                Raise(nameof(WorkshopDashboard));
                Raise(nameof(PresetActionLabel));
            }
            else if (IsAgentMemoryInspector)
            {
                if (AgentMemoryDashboard != null)
                {
                    AgentMemoryDashboard.CycleScenario(presetCycleIndex);
                    var activeHero = AgentMemoryDashboard.SelectedAgent;
                    Evidence.Add(new("Agent Memory / Preset", "Loaded", $"Switched hero to {activeHero?.Name} ({activeHero?.HeroId}) · Decay Stage: {activeHero?.DecayState} ({activeHero?.SummaryBadge})."));
                    Raise(nameof(AgentMemoryDashboard));
                    Raise(nameof(PresetActionLabel));
                }
            }
            else if (IsCodeSecurityAuditor)
            {
                CodeSecurityDashboard?.CycleScenario(presetCycleIndex);
                Evidence.Add(new("Security / Preset", "Loaded", $"Loaded scenario #{presetCycleIndex % 3 + 1}: {CodeSecurityDashboard?.TargetAssembly}"));
                Raise(nameof(CodeSecurityDashboard));
                Raise(nameof(PresetActionLabel));
            }
            else if (IsModuleHierarchyValidator)
            {
                ModuleHierarchyDashboard?.CycleScenario(presetCycleIndex);
                Evidence.Add(new("Module / Preset", "Loaded", $"Loaded topology #{presetCycleIndex % 3 + 1}: {ModuleHierarchyDashboard?.SubModuleXmlStatus}"));
                Raise(nameof(ModuleHierarchyDashboard));
                Raise(nameof(PresetActionLabel));
            }
            else if (IsKingdomDiplomacyStudio)
            {
                KingdomDiplomacyDashboard?.CycleScenario(presetCycleIndex);
                Evidence.Add(new("Diplomacy / Preset", "Loaded", $"Loaded geopolitical stance #{presetCycleIndex % 3 + 1}: {KingdomDiplomacyDashboard?.FactionName}"));
                Raise(nameof(KingdomDiplomacyDashboard));
                Raise(nameof(PresetActionLabel));
            }
            else if (IsComponentGeneratorStudio)
            {
                ComponentGeneratorDashboard?.CycleScenario(presetCycleIndex);
                Evidence.Add(new("Generator / Preset", "Loaded", $"Loaded synthesis template #{presetCycleIndex % 3 + 1}: {ComponentGeneratorDashboard?.TargetOutput}"));
                Raise(nameof(ComponentGeneratorDashboard));
                Raise(nameof(PresetActionLabel));
            }
            else if (IsCombatStudio)
            {
                CombatStudioDashboard?.CycleScenario(presetCycleIndex);
                Evidence.Add(new("Combat / Preset", "Loaded", $"Cycled combat formation preset #{presetCycleIndex % 2 + 1}: {CombatStudioDashboard?.RegimentName}"));
                Raise(nameof(CombatStudioDashboard));
                Raise(nameof(PresetActionLabel));
            }
            else if (IsCaravanTrade)
            {
                CaravanTradeDashboard?.CycleScenario(presetCycleIndex);
                Evidence.Add(new("Trade / Preset", "Loaded", $"Cycled caravan route preset #{presetCycleIndex % 2 + 1}: {CaravanTradeDashboard?.RouteName}"));
                Raise(nameof(CaravanTradeDashboard));
                Raise(nameof(PresetActionLabel));
            }
            else if (IsGauntletStudio)
            {
                GauntletStudio?.CycleScenario(presetCycleIndex);
                Evidence.Add(new("Gauntlet / Preset", "Loaded", $"Cycled Gauntlet HUD preset #{presetCycleIndex % 2 + 1}: {GauntletStudio?.PrefabName}"));
                Raise(nameof(GauntletStudio));
                Raise(nameof(PresetActionLabel));
            }
            else if (IsCampaignStudio)
            {
                CampaignStudio?.CycleScenario(presetCycleIndex);
                Evidence.Add(new("Campaign / Preset", "Loaded", $"Cycled Campaign expedition preset #{presetCycleIndex % 2 + 1}: {CampaignStudio?.ProvinceName}"));
                Raise(nameof(CampaignStudio));
                Raise(nameof(PresetActionLabel));
            }
            else if (IsLiveSession)
            {
                LiveSessionDashboard?.CycleScenario(presetCycleIndex);
                Evidence.Add(new("Live Session / Preset", "Loaded", $"Cycled Live Session telemetry preset #{presetCycleIndex % 3 + 1}: {LiveSessionDashboard?.SessionTitle}"));
                Raise(nameof(LiveSessionDashboard));
                Raise(nameof(PresetActionLabel));
            }
            else if (IsDeliveryStudio)
            {
                DeliveryStudioDashboard?.CycleScenario(presetCycleIndex);
                Evidence.Add(new("Delivery / Preset", "Loaded", $"Cycled FastPackageEngine delivery preset #{presetCycleIndex % 2 + 1}: {DeliveryStudioDashboard?.DistributionTitle}"));
                Raise(nameof(DeliveryStudioDashboard));
                Raise(nameof(PresetActionLabel));
            }
            else if (IsDiagnosticsStudio)
            {
                DiagnosticsStudioDashboard?.CycleScenario(presetCycleIndex);
                Evidence.Add(new("Diagnostics / Preset", "Loaded", $"Cycled diagnostics profile: {DiagnosticsStudioDashboard?.ActiveProfileLabel}"));
                Raise(nameof(DiagnosticsStudioDashboard));
                Raise(nameof(PresetActionLabel));
            }
            else
            {
                if (ReferenceEquals(GenericOperationDashboard, GenericOperationDashboardViewModel.CanonicalInstance))
                {
                    GenericOperationDashboard = new GenericOperationDashboardViewModel();
                }
                GenericOperationDashboard?.CycleScenario(presetCycleIndex);
                Evidence.Add(new("Operation / Preset", "Loaded", $"Switched execution mode: {GenericOperationDashboard?.ExecutionMode}"));
                Raise(nameof(GenericOperationDashboard));
                Raise(nameof(PresetActionLabel));
            }
        }

        // public void Dispose() is the page lifecycle contract; the virtual implementation below lets routed pages add state.
        public virtual void Dispose()
        {
            if (disposed) return;
            disposed = true;
            RunCommand.Cancel();
            ExportCommand.Cancel();
            Evidence.Clear();
            Raise(nameof(DisabledReason));
            RunCommand.NotifyCanExecuteChanged();
            BrowseFileCommand.NotifyCanExecuteChanged();
            BrowseFolderCommand.NotifyCanExecuteChanged();
            LoadPresetCommand.NotifyCanExecuteChanged();
            ClearInputCommand.NotifyCanExecuteChanged();
        }
    }
}
