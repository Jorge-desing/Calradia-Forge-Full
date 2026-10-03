using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace CalradiaForge.Desktop.Presentation
{
    // =========================================================================
    // CODE SECURITY, MODULE HIERARCHY & KINGDOM DIPLOMACY VIEW MODELS
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
        public double StatelessSecurityScoreGauge => 100.0;

        IReadOnlyList<ForgeStepItem> securityAuditPipelineSteps = new List<ForgeStepItem>
        {
            new("Metadata Preflight", "PE Assembly Headers", ForgeStepStatus.Completed, "100%"),
            new("Anti-Shadowing", "Rule A Verification", ForgeStepStatus.Completed, "PASS"),
            new("Stateless SyncData", "Rule B Save Guard", ForgeStepStatus.Completed, "PASS"),
            new("Distribution Clearance", "Rule D & PE Meta", ForgeStepStatus.Active, "VERIFIED")
        };
        IReadOnlyList<double> securityRiskDensityTrajectory = new List<double>
        {
            98.5, 99.0, 99.2, 99.5, 99.8, 100.0, 100.0, 100.0, 99.9, 100.0, 100.0, 100.0, 100.0, 100.0, 100.0, 100.0
        };

        public IReadOnlyList<ForgeStepItem> SecurityAuditPipelineSteps { get => securityAuditPipelineSteps; set => Set(ref securityAuditPipelineSteps, value); }

        public IReadOnlyList<double> SecurityRiskDensityTrajectory { get => securityRiskDensityTrajectory; set => Set(ref securityRiskDensityTrajectory, value); }

        double auditScanElapsedMs = 84.5;
        public double AuditScanElapsedMs { get => auditScanElapsedMs; set => Set(ref auditScanElapsedMs, value); }

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
                    AuditScanElapsedMs = 84.5;
                    break;
                case 1:
                    TargetAssembly = "TaleWorlds.CampaignSystem.dll (Native Engine)";
                    TargetRuntime = ".NET Framework 4.7.2 / Tailored Native Engine Host";
                    RiskScore = "ENGINE NATIVE · REFERENCE ONLY";
                    RiskLevel = "VERIFIED [ENGINE]";
                    RiskBrushKey = "BrassBrush";
                    AuditScanElapsedMs = 142.0;
                    break;
                case 2:
                    TargetAssembly = "CustomSubModule.dll (Third-Party Addon)";
                    TargetRuntime = ".NET Framework 4.7.2 / Sandboxed Scan Mode";
                    RiskScore = "SANDBOXED INSPECTION";
                    RiskLevel = "AUDITED [OK]";
                    RiskBrushKey = "VerdigrisBrush";
                    AuditScanElapsedMs = 62.8;
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
            new("cf.patch_diagnostics", "Inspect registered Forge hook and patch diagnostics without applying changes.", "Diagnostics"),
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
        public double ModuleIntegrityScoreGauge => 98.5;

        IReadOnlyList<ForgeStepItem> moduleResolutionPipelineSteps = new List<ForgeStepItem>
        {
            new("Discovery", "SubModule.xml Parser", ForgeStepStatus.Completed, "FOUND"),
            new("Dependencies", "Native & SandBox Graph", ForgeStepStatus.Completed, "VERIFIED"),
            new("Topological Sort", "Cycle-Free Ordering", ForgeStepStatus.Completed, "OPTIMAL"),
            new("Assembly Link", "MBSubModuleBase Load", ForgeStepStatus.Active, "LOADED")
        };
        IReadOnlyList<double> moduleLoadLatencyTrajectory = new List<double>
        {
            12.4, 14.8, 15.2, 13.5, 16.0, 15.8, 18.2, 22.4, 21.0, 24.5, 23.8, 25.1, 26.0, 28.4, 27.5, 29.0
        };

        public IReadOnlyList<ForgeStepItem> ModuleResolutionPipelineSteps { get => moduleResolutionPipelineSteps; set => Set(ref moduleResolutionPipelineSteps, value); }

        public IReadOnlyList<double> ModuleLoadLatencyTrajectory { get => moduleLoadLatencyTrajectory; set => Set(ref moduleLoadLatencyTrajectory, value); }

        double bootSequenceElapsedMs = 460.0;
        public double BootSequenceElapsedMs { get => bootSequenceElapsedMs; set => Set(ref bootSequenceElapsedMs, value); }

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
                BootSequenceElapsedMs = 460.0;
            }
            else if (sc == 1)
            {
                ManifestId = "CalradiaForge.Minimal";
                Version = "v25.0.0-custom";
                SubModuleXmlStatus = "SANDBOX COMPATIBILITY STACK";
                BootSequenceElapsedMs = 310.0;
            }
            else
            {
                ManifestId = "CalradiaForge.Ecosystem";
                Version = "v25.0.0-eco";
                SubModuleXmlStatus = "MULTI-MOD COEXISTENCE MATRIX";
                BootSequenceElapsedMs = 680.0;
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
        public double ImperialStabilityGauge => 76.5;

        public IReadOnlyList<ForgeBarDataPoint> KingdomPowerComparisonBars { get; } =
        [
            new("Vlandia", 8200.0, null, "Western Kingdom · 38 Clans"),
            new("W. Empire", 7400.0, null, "Garios Legions · 32 Clans"),
            new("S. Empire", 6800.0, null, "Rhagaea Crown · 29 Clans"),
            new("Aserai", 6200.0, null, "Sultanate · 26 Clans"),
            new("Battania", 5100.0, null, "High King Caladog · 22 Clans"),
            new("Sturgia", 4900.0, null, "Grand Prince Raganvad · 21 Clans")
        ];

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

        double senateConsensusGaugeValue = 68.0;
        double warRiskGaugeValue = 24.0;
        static readonly string[] DefaultDiplomaticRadarAxes = ["Military", "Clans", "Tribute", "Stability", "CasusBelli"];
        IReadOnlyList<double> factionPowerRadarValues = [0.75, 0.65, 0.45, 0.80, 0.35];

        public double SenateConsensusGaugeValue { get => senateConsensusGaugeValue; private set => Set(ref senateConsensusGaugeValue, value); }
        public double WarRiskGaugeValue { get => warRiskGaugeValue; private set => Set(ref warRiskGaugeValue, value); }
        public IReadOnlyList<string> DiplomaticRadarAxes => DefaultDiplomaticRadarAxes;
        public IReadOnlyList<double> FactionPowerRadarValues { get => factionPowerRadarValues; private set => Set(ref factionPowerRadarValues, value); }

        IReadOnlyList<ForgeStepItem> diplomaticResolutionPipelineSteps = new List<ForgeStepItem>
        {
            new("Imperial Envoy", "Diplomatic Courier", ForgeStepStatus.Completed, "DESPATCHED"),
            new("Senate Chamber", "Council Ballot", ForgeStepStatus.Active, "DEBATING"),
            new("Treaty Draft", "Territorial Accord", ForgeStepStatus.Pending, "DRAFTING"),
            new("Aquila Ratification", "Porphyry Wax Seal", ForgeStepStatus.Pending, "RATIFIED")
        };
        IReadOnlyList<double> geopoliticalTensionTrajectory = new List<double>
        {
            0.45, 0.52, 0.58, 0.64, 0.70, 0.74, 0.79, 0.82, 0.85, 0.88, 0.84, 0.80, 0.76, 0.72, 0.68, 0.65
        };
        double treatyTruceTimelineDays = 45.0;

        public IReadOnlyList<ForgeStepItem> DiplomaticResolutionPipelineSteps { get => diplomaticResolutionPipelineSteps; set => Set(ref diplomaticResolutionPipelineSteps, value); }

        public IReadOnlyList<double> GeopoliticalTensionTrajectory { get => geopoliticalTensionTrajectory; set => Set(ref geopoliticalTensionTrajectory, value); }

        public double TreatyTruceTimelineDays { get => treatyTruceTimelineDays; set => Set(ref treatyTruceTimelineDays, value); }

        void RecalculateDiplomacy()
        {
            var baseRisk = factionName.Contains("Western") ? 58 : factionName.Contains("Vlandia") ? 40 : 24;
            var adjustedRisk = Math.Clamp(baseRisk + tensionModifier, 5, 95);
            var adjustedPeace = 100 - adjustedRisk;
            var status = adjustedRisk >= 60 ? "MOBILIZED" : adjustedRisk >= 35 ? "EXPEDITIONARY" : "STABLE";
            WarRiskText = $"WAR RISK: {adjustedRisk}% · {status}";
            PeaceIndexText = $"PEACE PROBABILITY: {adjustedPeace}%";

            SenateConsensusGaugeValue = Math.Clamp(68 - tensionModifier * 2, 10, 95);
            WarRiskGaugeValue = adjustedRisk;

            var mil = factionName.Contains("Western") ? 0.88 : factionName.Contains("Vlandia") ? 0.82 : 0.74;
            var clans = factionName.Contains("Western") ? 0.70 : factionName.Contains("Vlandia") ? 0.85 : 0.68;
            var trib = Math.Clamp(0.50 - (tensionModifier * 0.015), 0.15, 0.90);
            var stab = Math.Clamp(1.0 - (adjustedRisk / 100.0), 0.10, 0.95);
            var casus = Math.Clamp(adjustedRisk / 100.0, 0.10, 0.95);
            FactionPowerRadarValues = [mil, clans, trib, stab, casus];

            if (adjustedRisk >= 75)
            {
                DiplomaticResolutionPipelineSteps = new List<ForgeStepItem>
                {
                    new("Imperial Envoy", "Diplomatic Courier", ForgeStepStatus.Completed, "DESPATCHED"),
                    new("Senate Chamber", "Veto Enacted", ForgeStepStatus.Failed, "REJECTED"),
                    new("Treaty Draft", "Hostile Breakdown", ForgeStepStatus.Failed, "WAR"),
                    new("Aquila Ratification", "Casus Belli Mobilized", ForgeStepStatus.Failed, "CANCELLED")
                };
                TreatyTruceTimelineDays = 0.0;
            }
            else if (adjustedRisk >= 40)
            {
                DiplomaticResolutionPipelineSteps = new List<ForgeStepItem>
                {
                    new("Imperial Envoy", "Diplomatic Courier", ForgeStepStatus.Completed, "DESPATCHED"),
                    new("Senate Chamber", "Senate Debate Active", ForgeStepStatus.Active, "CONTESTED"),
                    new("Treaty Draft", "Territorial Accord", ForgeStepStatus.Pending, "DRAFTING"),
                    new("Aquila Ratification", "Imperial Porphyry Seal", ForgeStepStatus.Pending, "AWAITING")
                };
                TreatyTruceTimelineDays = Math.Clamp(60.0 - (tensionModifier * 2.0), 5.0, 120.0);
            }
            else
            {
                DiplomaticResolutionPipelineSteps = new List<ForgeStepItem>
                {
                    new("Imperial Envoy", "Diplomatic Courier", ForgeStepStatus.Completed, "DESPATCHED"),
                    new("Senate Chamber", "Noble Accord Passed", ForgeStepStatus.Completed, "RATIFIED"),
                    new("Treaty Draft", "Non-Aggression Accord", ForgeStepStatus.Completed, "TRUCE"),
                    new("Aquila Ratification", "Aquila Imperial Seal", ForgeStepStatus.Active, "ENFORCED")
                };
                TreatyTruceTimelineDays = Math.Clamp(95.0 - (tensionModifier * 1.5), 10.0, 120.0);
            }

            var baseTen = adjustedRisk / 100.0;
            var tenPts = new List<double>(16);
            for (int i = 0; i < 16; i++)
            {
                var val = baseTen + 0.12 * Math.Sin((i + 1) * 0.48);
                tenPts.Add(Math.Round(Math.Clamp(val, 0.05, 0.98), 2));
            }
            GeopoliticalTensionTrajectory = tenPts;

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

}
