using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalradiaForge.Desktop.Presentation;
using CalradiaForge.Desktop.Services;
using CalradiaForge.Sdk;

internal static class DesktopSimulationServiceTests
{
    public static void RunAll()
    {
        Console.WriteLine("--- Running Desktop Simulation & Split Deck Unit Tests ---");
        TroopTreeCanonical();
        Console.WriteLine("PASS Desktop simulation renders canonical troop tree DAG and stat curves");
        TroopXmlDiff();
        Console.WriteLine("PASS Desktop simulation performs semantic XML diff for troop definitions");
        AgentMemorySampleReport();
        Console.WriteLine("PASS Desktop simulation reports CoALA-inspired sample memory tiers and quotas");
        AgentMemorySamplePresentation();
        Console.WriteLine("PASS Desktop agent-memory dashboard and catalog disclose simulated CoALA-inspired sample data");
        AudioWaveformAudit();
        Console.WriteLine("PASS Desktop simulation inspects acoustic waveforms and frequency spectrum");
        EconomyAndCampaignSimulation();
        Console.WriteLine("PASS Desktop simulation models 30-day workshop economy and rebellion risk");
        WorkspaceSimulationRouting().GetAwaiter().GetResult();
        Console.WriteLine("PASS Desktop workspace routes simulation tools to dedicated engines");
        SplitDeckLifecycle();
        Console.WriteLine("PASS Desktop shell manages Split Deck activation and pinned tool retention");
        TroopTreeCaptainPerks();
        Console.WriteLine("PASS Desktop simulation toggles captain perks and evaluates wage/cohesion modifiers");
        CoalaCognitiveMemoryConsolidation();
        Console.WriteLine("PASS Desktop simulation executes CoALA sleep memory consolidation and category filtering");
        KingdomDiplomacySenateVoteAndWarActions();
        Console.WriteLine("PASS Desktop simulation evaluates senate decrees, war declarations, and truce actions");
        GenericOperationFlightDeckDiagnostics();
        Console.WriteLine("PASS Desktop simulation validates generic operation flight deck diagnostic scans");
        VectorChartControlsTelemetry();
        Console.WriteLine("PASS Desktop simulation computes vector chart trajectories and radar axes");
        VectorBarChartAndHeatmapTelemetry();
        Console.WriteLine("PASS Desktop simulation verifies vector bar charts and tactical correlation heatmaps (Rev083)");
        Rev086StudioEnrichmentAndTimelineTelemetry();
        Console.WriteLine("PASS Desktop simulation verifies Rev086 pipeline steps, timeline ruler, and studio trajectories");
        Rev087SpecializedStudiosAndAquilaSealTelemetry();
        Console.WriteLine("PASS Desktop simulation verifies Rev087 specialized studio pipelines, area trajectories, and aquila seal telemetry");
        Rev088TroopCombatTradeVisualTelemetry();
        Console.WriteLine("PASS Desktop simulation verifies Rev088 troop progression, combat doctrine, and caravan trade visual telemetry");
        Rev089CampaignDiagnosticsDeliveryGauntletVisualTelemetry();
        Console.WriteLine("PASS Desktop simulation verifies Rev089 campaign, diagnostics, delivery, and gauntlet visual telemetry");
        Rev090AudioSecurityHierarchyGeneratorTelemetry();
        Console.WriteLine("PASS Desktop simulation verifies Rev090 audio, security, hierarchy, generator, and live telemetry");
        Rev091GenericOperationAndVisualTelemetry();
        Console.WriteLine("PASS Desktop simulation verifies Rev091 generic operation scan latency and visual telemetry");
        Rev092WorkshopGuildAndCommandPaletteVisualTelemetry();
        Console.WriteLine("PASS Desktop simulation verifies Rev092 workshop guild medallion and scenario cycling telemetry");
        Rev093AgentCognitiveMindAndProceduralRadarTelemetry();
        Console.WriteLine("PASS Desktop simulation verifies Rev093 CoALA cognitive mind medallion and procedural policy radar telemetry");
        Rev095FiveStudioMedallionUpgrades();
        Console.WriteLine("PASS Desktop simulation verifies Rev095 five thematic studio medallions registered and XAML templates upgraded");
        Rev096FiveResidualIconUpgrades();
        Console.WriteLine("PASS Desktop simulation verifies Rev096 five residual icon upgrades — troop crest, cohort badge, trade sigil, pipe seal, sentinel eye");
        Rev098MajorVisualAndStudioUpgrades();
        Console.WriteLine("PASS Desktop simulation verifies Rev098 major visual studio upgrades — anvil sigil, gauntlet seal, campaign astrolabe, delivery seal, diagnostics aegis");
        Rev099TacticalViewsAndUnretouchedFunctionsPolish();
        Console.WriteLine("PASS Desktop simulation verifies Rev099 tactical views and unretouched functions visual overhaul — palette, status, dossier, split deck, footer");
        Rev100HookWorkbenchAndPresentationViewsVisualOverhaul();
        Console.WriteLine("PASS Desktop simulation verifies Rev100 Hook Workbench and presentation views visual overhaul — bytecode matrix, header, window chrome, studio templates");
        Rev101TacticalStudiosFullCompositionOverhaul();
        Console.WriteLine("PASS Desktop simulation verifies Rev101 tactical studios full composition overhaul — dual medallions, KPI cards, mini-progress bars, and dossier consoles");
    }

    static void Rev095FiveStudioMedallionUpgrades()
    {
        // Verify all 5 new Rev095 medallion assets are referenced in ToolPageTemplates.xaml
        string xamlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "CalradiaForge.Desktop", "Resources", "Views", "ToolPageTemplates.xaml");
        string xaml = File.ReadAllText(xamlPath);

        string[] expectedAssets = {
            "calradia-acoustic-medallion-rev095.png",
            "calradia-cipher-seal-rev095.png",
            "calradia-hierarchy-seal-rev095.png",
            "calradia-diplomacy-medallion-rev095.png",
            "calradia-mechanism-medallion-rev095.png",
        };
        foreach (var asset in expectedAssets)
            Check(xaml.Contains(asset), $"Rev095: XAML must reference new asset '{asset}'.");

        // Verify exactly 9 DashboardTemplate DataTemplates are present (Rule C invariant)
        int templateCount = System.Text.RegularExpressions.Regex.Matches(
            xaml, @"<DataTemplate x:Key=""\w+DashboardTemplate""").Count;
        Check(templateCount == 9, $"Rev095: Rule C violated — expected exactly 9 DashboardTemplates, found {templateCount}.");

        // Verify no Run.Text binding missing Mode=OneWay (Propuesta 48 invariant) in upgraded templates
        string[] upgradeKeys = {
            "AudioMixerInspectorDashboardTemplate",
            "CodeSecurityAuditorDashboardTemplate",
            "ModuleHierarchyValidatorDashboardTemplate",
            "KingdomDiplomacyStudioDashboardTemplate",
            "ComponentGeneratorStudioDashboardTemplate",
        };
        // Simple check: no bare <Run Text="{Binding ... without Mode=OneWay} in the full XAML
        var runBindings = System.Text.RegularExpressions.Regex.Matches(
            xaml, @"<Run Text=""\{Binding [^}]+\}""");
        foreach (System.Text.RegularExpressions.Match m in runBindings)
        {
            Check(m.Value.Contains("Mode=OneWay"),
                $"Rev095: Propuesta 48 violated — Run.Text binding missing Mode=OneWay: '{m.Value}'.");
        }

        // Verify new assets are registered in .csproj
        string csprojPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "CalradiaForge.Desktop", "CalradiaForge.Desktop.csproj");
        string csproj = File.ReadAllText(csprojPath);
        foreach (var asset in expectedAssets)
            Check(csproj.Contains(asset), $"Rev095: .csproj must include Resource entry for '{asset}'.");
    }

    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    static void TroopTreeCanonical()
    {
        var service = new DesktopSimulationService();
        var (report, evidence) = service.SimulateTroopTree(string.Empty, CancellationToken.None);

        Check(!string.IsNullOrWhiteSpace(report), "Troop tree report must not be empty.");
        Check(report.Contains("Imperial Recruit") && report.Contains("Imperial Legionary"), "Canonical tree must contain core Imperial infantry progression.");
        Check(report.Contains("Imperial Palatine Guard"), "Canonical tree must contain archer progression line.");
        Check(report.Contains("STAT ENVELOPE & BALANCE METRICS"), "Report must include statistical balance envelope.");

        Check(evidence.Any(e => e.Source.Contains("Hierarchy")), "Evidence must verify hierarchy.");
        Check(evidence.Any(e => e.Source.Contains("Stat Curve")), "Evidence must verify statistical progression curves.");
        Check(evidence.Any(e => e.Source.Contains("Envelope")), "Evidence must verify equipment envelope.");
    }

    static void TroopXmlDiff()
    {
        var root = Path.Combine(Path.GetTempPath(), "CalradiaForgeDiff-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var fileA = Path.Combine(root, "troops_v1.xml");
            var fileB = Path.Combine(root, "troops_v2.xml");

            var xmlA = @"<?xml version=""1.0"" encoding=""utf-8""?>
<NPCCharacters>
  <NPCCharacter id=""imperial_recruit"" name=""Recruit"" level=""6"" default_group=""Infantry"" />
  <NPCCharacter id=""imperial_archer"" name=""Archer"" level=""11"" default_group=""Ranged"" />
</NPCCharacters>";

            var xmlB = @"<?xml version=""1.0"" encoding=""utf-8""?>
<NPCCharacters>
  <NPCCharacter id=""imperial_recruit"" name=""Recruit"" level=""7"" default_group=""Infantry"" />
  <NPCCharacter id=""imperial_crossbowman"" name=""Crossbow"" level=""12"" default_group=""Ranged"" />
</NPCCharacters>";

            File.WriteAllText(fileA, xmlA, Encoding.UTF8);
            File.WriteAllText(fileB, xmlB, Encoding.UTF8);

            var service = new DesktopSimulationService();
            var (report, evidence) = service.SimulateTroopTree($"{fileA}|{fileB}", CancellationToken.None);

            Check(report.Contains("[SEMANTIC SUMMARY]"), "Diff report must contain semantic summary.");
            Check(report.Contains("imperial_crossbowman"), "Diff report must detect added crossbowman entity.");
            Check(report.Contains("imperial_archer"), "Diff report must detect removed archer entity.");
            Check(report.Contains("level: '6' => '7'"), "Diff report must detect mutated level attribute.");

            Check(evidence.Any(e => e.Source.Contains("Added") && e.Detail.Contains("1")), "Evidence must record 1 added entity.");
            Check(evidence.Any(e => e.Source.Contains("Removed") && e.Detail.Contains("1")), "Evidence must record 1 removed entity.");
            Check(evidence.Any(e => e.Source.Contains("Modified") && e.Detail.Contains("1")), "Evidence must record 1 modified entity.");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    static void AgentMemorySampleReport()
    {
        var service = new DesktopSimulationService();
        var (report, evidence) = service.InspectAgentMemory(string.Empty, CancellationToken.None);

        Check(report.Contains("AGENT MEMORY SAMPLE"), "Report must identify itself as a sample, not a live audit.");
        Check(report.Contains("Bounded tiered memory inspired by CoALA concepts; not a CoALA language-agent runtime"), "Report must describe the CoALA relationship without claiming a full CoALA runtime.");
        Check(report.Contains("not a live registry snapshot"), "Report must disclose that registry capacity is sample data.");
        Check(report.Contains("GLOBAL CAPACITY METER"), "Report must include global slot utilization meter.");
        Check(report.Contains("TIER 1: SEMANTIC MEMORY"), "Report must audit Tier 1 Semantic Memory.");
        Check(report.Contains("TIER 2: EPISODIC MEMORY"), "Report must audit Tier 2 Episodic Memory.");
        Check(report.Contains("TIER 3: PROCEDURAL MEMORY"), "Report must audit Tier 3 Procedural Memory.");
        Check(report.Contains("hero_rhagaea"), "Report must include seeded Lord profile data.");

        Check(evidence.Count >= 4, "Agent memory audit must produce comprehensive evidence items.");
        Check(evidence.All(e => e.Status == "Sample"), "Sample-memory evidence must not claim live verification.");
        Check(evidence.Any(e => e.Source.Contains("Global Registry") && e.Status == "Sample"), "Global registry evidence must be labeled as sample data.");
        Check(evidence.Any(e => e.Source.Contains("Semantic Decay") && e.Status == "Sample"), "Semantic TTL evidence must be labeled as sample data.");
        Check(evidence.Any(e => e.Source.Contains("Episodic FIFO") && e.Status == "Sample"), "Episodic FIFO evidence must be labeled as sample data.");
    }

    static void AgentMemorySamplePresentation()
    {
        var dashboard = new AgentMemoryDashboardViewModel();
        Check(dashboard.Architecture.Contains("simulated", StringComparison.OrdinalIgnoreCase) &&
              dashboard.Architecture.Contains("inspired by CoALA", StringComparison.OrdinalIgnoreCase) &&
              dashboard.Architecture.Contains("not a live registry", StringComparison.OrdinalIgnoreCase),
            "Dashboard description must identify illustrative memory data as simulated, CoALA-inspired, and not live.");
        Check(dashboard.StudioDocumentation.Contains("simulated sample data", StringComparison.OrdinalIgnoreCase) &&
              dashboard.StudioDocumentation.Contains("not a CoALA runtime", StringComparison.OrdinalIgnoreCase),
            "Dashboard documentation must not claim a CoALA runtime.");
        Check(dashboard.StudioCaveat.Contains("simulated sample only", StringComparison.OrdinalIgnoreCase),
            "Visible dashboard caveat must disclose the sample-only status.");
        Check(dashboard.MaximumAgentSlots == ForgeAgentMemory.MaximumAgents && dashboard.MaximumAgentSlots == 2048,
            "Dashboard capacity must use the SDK's 2,048-agent limit.");
        Check(dashboard.MemorySlotUtilizationGaugeValue == 3 &&
              Math.Abs(dashboard.MemorySlotUtilizationPercentage - (3.0 / ForgeAgentMemory.MaximumAgents * 100.0)) < 0.0001,
            "Dashboard slot gauge must show its three visible sample profiles against the SDK capacity.");
        Check(dashboard.CapacitySummary.EndsWith(ForgeAgentMemory.MaximumAgents.ToString("N0", System.Globalization.CultureInfo.CurrentCulture), StringComparison.Ordinal),
            "Dashboard capacity summary must report the SDK maximum.");
        dashboard.ApplyLiveIpcTelemetry(1024, 0, 0, 0);
        Check(dashboard.CapacityPercentage == 50.0,
            "Dashboard telemetry must calculate utilization from the SDK agent limit.");

        string xamlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "CalradiaForge.Desktop", "Resources", "Views", "ToolPageTemplates.xaml");
        string templates = File.ReadAllText(xamlPath);
        int memoryStart = templates.IndexOf("x:Key=\"AgentMemoryInspectorDashboardTemplate\"", StringComparison.Ordinal);
        int memoryEnd = templates.IndexOf("x:Key=\"CodeSecurityAuditorDashboardTemplate\"", memoryStart, StringComparison.Ordinal);
        Check(memoryStart >= 0 && memoryEnd > memoryStart, "Memory dashboard XAML template must remain discoverable for its capacity contract.");
        string memoryTemplate = templates.Substring(memoryStart, memoryEnd - memoryStart);
        Check(memoryTemplate.Contains("Maximum=\"{Binding AgentMemoryDashboard.MaximumAgentSlots}\"", StringComparison.Ordinal) &&
              memoryTemplate.Contains("CenterText=\"{Binding AgentMemoryDashboard.MemorySlotUtilizationLabel}\"", StringComparison.Ordinal) &&
              !memoryTemplate.Contains("Maximum=\"100\"", StringComparison.Ordinal),
            "Memory dashboard gauge must bind the SDK capacity instead of a hard-coded 100-slot maximum.");

        var tool = new ToolCatalog().Find("SaveInspector");
        Check(tool != null && tool.KindLabel == "SIMULATED SAMPLE" &&
              tool.Purpose.Contains("simulated agent-memory sample", StringComparison.OrdinalIgnoreCase) &&
              tool.Purpose.Contains("not a live registry or CoALA runtime", StringComparison.OrdinalIgnoreCase),
            "Catalog wording must describe the memory route as a simulated sample, not a live CoALA runtime.");
        Check(tool.AvailabilityReason.Contains("Sample-only", StringComparison.OrdinalIgnoreCase) &&
              tool.AvailabilityReason.Contains("does not query a live Forge memory registry", StringComparison.OrdinalIgnoreCase),
            "Catalog availability must not imply that the memory sample reads live session data.");
        Check(tool.SectionInformation.Contains("muestra simulada", StringComparison.OrdinalIgnoreCase) &&
              tool.SectionInformation.Contains("no inspecciona un registro en vivo", StringComparison.OrdinalIgnoreCase),
            "Catalog details must disclose the simulated memory sample.");
    }

    static void AudioWaveformAudit()
    {
        var root = Path.Combine(Path.GetTempPath(), "CalradiaForgeAudio-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var wavPath = Path.Combine(root, "impact_shield.wav");
            using (var fs = File.Create(wavPath))
            using (var bw = new BinaryWriter(fs))
            {
                // RIFF header
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + 1000); // chunk size
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                // fmt subchunk
                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16); // subchunk size
                bw.Write((short)1); // PCM
                bw.Write((short)2); // 2 channels
                bw.Write(44100); // 44.1kHz sample rate
                bw.Write(44100 * 2 * 2); // byte rate
                bw.Write((short)4); // block align
                bw.Write((short)16); // 16 bits
                // data subchunk
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(1000); // data size
                bw.Write(new byte[1000]); // zeroed PCM samples
            }

            var service = new DesktopSimulationService();
            var (report, evidence) = service.AuditAudioWaveform(wavPath, CancellationToken.None);

            Check(report.Contains("TACTICAL AUDIO STUDIO & WAVEFORM ANALYZER"), "Report must identify tactical audio studio.");
            Check(report.Contains("ACOUSTIC WAVEFORM ENVELOPE"), "Report must render acoustic waveform envelope.");
            Check(report.Contains("SPECTRAL FREQUENCY DISTRIBUTION"), "Report must render 5-band frequency spectrum.");
            Check(report.Contains("44") && report.Contains("Hz") && report.Contains("Stereo"), "Report must correctly read WAV sample rate and channels.");

            Check(evidence.Any(e => e.Source.Contains("WAV Header") && e.Status == "Verified"), "RIFF WAVE header must be verified.");
            Check(evidence.Any(e => e.Source.Contains("Mixer Compliance")), "Mixer compliance must be audited.");
            Check(evidence.Any(e => e.Source.Contains("Dynamic Headroom")), "Peak headroom must be audited.");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    static void EconomyAndCampaignSimulation()
    {
        var service = new DesktopSimulationService();
        var (report, evidence) = service.SimulateEconomyAndCampaign(string.Empty, CancellationToken.None);

        Check(report.Contains("HEADLESS CAMPAIGN & WORKSHOP ECONOMY SIMULATION"), "Report must identify headless economy simulation.");
        Check(report.Contains("WORKSHOP ENTERPRISE PROFITABILITY AUDIT"), "Report must audit workshop enterprises.");
        Check(report.Contains("Silversmith") && report.Contains("Smithy"), "Report must model key profitable workshop types.");
        Check(report.Contains("30-DAY DAILY NET PROFIT TRAJECTORY"), "Report must render 30-day profit trajectory chart.");
        Check(report.Contains("SETTLEMENT CIVIC EQUILIBRIUM & REBELLION RISK"), "Report must model civic equilibrium.");
        Check(report.Contains("Rebellion Risk Index"), "Report must calculate Rebellion Risk Index.");

        Check(evidence.Any(e => e.Source.Contains("Workshop Enterprise")), "Workshop profitability must be evidenced.");
        Check(evidence.Any(e => e.Source.Contains("Civic Stability")), "Civic equilibrium must be evidenced.");
    }

    static async Task WorkspaceSimulationRouting()
    {
        var catalog = new ToolCatalog();
        var metrics = new DesktopMetricsService();
        using var workspace = new DesktopWorkspaceService(metrics);

        var troopTool = catalog.Tools.First(t => t.Id == "TroopTreeVisualizer");
        var troopResult = await workspace.ExecuteAsync(troopTool, string.Empty, CancellationToken.None);
        Check(troopResult.Status == "Simulated", "Troop tree tool must return Simulated status.");
        Check(troopResult.RawResult.Contains("Imperial Recruit"), "Troop tree result must contain progression DAG.");

        var audioTool = catalog.Tools.First(t => t.Id == "AudioFmodMixerInspector");
        var audioResult = await workspace.ExecuteAsync(audioTool, string.Empty, CancellationToken.None);
        Check(audioResult.Status == "Simulated", "Audio tool must return Simulated status.");
        Check(audioResult.RawResult.Contains("ACOUSTIC WAVEFORM ENVELOPE"), "Audio result must contain waveform envelope.");

        var econTool = catalog.Tools.First(t => t.Id == "WorkshopEnterpriseSimulator");
        var econResult = await workspace.ExecuteAsync(econTool, string.Empty, CancellationToken.None);
        Check(econResult.Status == "Simulated", "Economy tool must return Simulated status.");
        Check(econResult.RawResult.Contains("WORKSHOP ENTERPRISE PROFITABILITY AUDIT"), "Economy result must contain enterprise audit.");

        var saveTool = catalog.Tools.First(t => t.Id == "SaveInspector");
        var saveResult = await workspace.ExecuteAsync(saveTool, "agent_memory", CancellationToken.None);
        Check(saveResult.Status == "Simulated", "SaveInspector with agent_memory query must return Simulated status.");
        Check(saveResult.RawResult.Contains("AGENT MEMORY SAMPLE")
            && saveResult.RawResult.Contains("not a live registry snapshot"), "Agent memory result must retain its sample-only qualification.");
    }

    static void SplitDeckLifecycle()
    {
        var catalog = new ToolCatalog();
        var metrics = new DesktopMetricsService();
        var theme = new DesktopThemeService();
        var pref = new DesktopPreferenceService();
        using var workspace = new DesktopWorkspaceService(metrics);
        using var shell = new DesktopShellViewModel(catalog, workspace, metrics, new DesktopLocalizationService(), theme, pref);

        Check(!shell.IsSplitDeckActive, "Split Deck must be inactive by default.");
        Check(!shell.HasPinnedDeckContent, "Pinned deck must have no content initially.");

        shell.ToggleSplitDeckCommand.Execute(null);
        Check(shell.IsSplitDeckActive, "ToggleSplitDeckCommand must activate Split Deck.");
        Check(shell.HasPinnedDeckContent, "Activating Split Deck with active page must pin current tool.");

        var pinnedTitle = shell.PinnedToolTitle;
        Check(!string.IsNullOrWhiteSpace(pinnedTitle), "Pinned tool title must be populated.");

        shell.ClearPinnedDeckCommand.Execute(null);
        Check(shell.PinnedTool == null, "ClearPinnedDeckCommand must clear pinned tool.");
        Check(shell.PinnedEvidence.Count == 0, "ClearPinnedDeckCommand must clear pinned evidence.");

        shell.CloseSplitDeckCommand.Execute(null);
        Check(!shell.IsSplitDeckActive, "CloseSplitDeckCommand must deactivate Split Deck.");
    }

    static void TroopTreeCaptainPerks()
    {
        var vm = new TroopTreeDashboardViewModel();
        Check(!vm.CaptainPerksActive, "Captain perks must be inactive by default.");
        Check(vm.PerkBonusSummary.Contains("INACTIVE"), "Perk bonus summary must show INACTIVE initially.");

        vm.ToggleCaptainPerksCommand.Execute(null);
        Check(vm.CaptainPerksActive, "Captain perks must be active after toggle.");
        Check(vm.PerkBonusSummary.Contains("ACTIVE"), "Perk bonus summary must show ACTIVE.");
        Check(vm.SimulatedFrontlineCohesion.Contains("Cohesion"), "Cohesion summary must be updated.");
        Check(vm.SimulatedArmyCostEstimate.Contains("Wage"), "Army cost estimate must be updated with perk efficiency.");

        vm.ToggleCaptainPerksCommand.Execute(null);
        Check(!vm.CaptainPerksActive, "Captain perks must toggle back to inactive.");
        Check(vm.PerkBonusSummary.Contains("INACTIVE"), "Perk bonus summary must return to INACTIVE.");
    }

    static void CoalaCognitiveMemoryConsolidation()
    {
        var vm = new AgentMemoryDashboardViewModel();
        Check(vm.SelectedAgent != null, "Selected agent must be populated by default.");

        // Category filter test
        vm.FilterCategoryCommand.Execute("Belief");
        Check(vm.SelectedCategoryFilter == "Belief", "Category filter must be set to Belief.");
        Check(vm.FilteredSemanticFacts.All(f => f.Category.Equals("Belief", StringComparison.OrdinalIgnoreCase)), "All filtered facts must be in Belief category.");

        vm.FilterCategoryCommand.Execute("All");
        Check(vm.FilteredSemanticFacts.Count >= 2, "All filter must restore all semantic facts.");

        // Add custom fact test
        vm.NewFactKey = "strategic_ambush_site";
        vm.NewFactValue = "High ground advantage near Jalmarys mountain pass";
        vm.NewFactCategory = "Stance";
        vm.NewFactTtl = "Permanent Anchor";
        vm.AddSemanticFactCommand.Execute(null);
        Check(vm.SelectedAgent.SemanticFacts.Any(f => f.Key == "strategic_ambush_site"), "Injected semantic fact must be present in agent memory.");
        Check(vm.ConsolidationFeedback.Contains("strategic_ambush_site"), "Consolidation feedback must confirm injected fact.");

        // Sleep cycle consolidation test
        vm.ConsolidateMemoriesCommand.Execute(null);
        Check(vm.ConsolidationFeedback.Contains("Sleep cycle consolidation complete"), "Consolidation feedback must confirm sleep cycle completion.");
        Check(vm.SelectedAgent.WorkingMemory.Contains("Sleep Consolidation Active"), "Working memory must reflect active consolidation.");
    }

    static void KingdomDiplomacySenateVoteAndWarActions()
    {
        var vm = new KingdomDiplomacyDashboardViewModel();
        Check(!string.IsNullOrWhiteSpace(vm.PlayerFaction), "PlayerFaction must be populated.");
        Check(!string.IsNullOrWhiteSpace(vm.PlayerRuler), "PlayerRuler must be populated.");
        Check(vm.FactionStances.Count > 0, "FactionStances must contain seeded realm stances.");

        // Senate voting test
        vm.SimulateSenateVoteCommand.Execute(null);
        Check(!string.IsNullOrWhiteSpace(vm.SenateVoteOutcome), "Senate vote outcome must be populated.");
        Check(vm.SenateVoteOutcome.Contains("DECREE"), "Senate vote outcome must reflect decree decision.");

        // War declaration & truce test
        var initialStance = vm.FactionStances[0];
        vm.SelectedStance = initialStance;
        Check(!string.IsNullOrWhiteSpace(vm.SelectedStanceDetails), "Selected stance details must be populated.");

        vm.DeclareWarCommand.Execute(null);
        Check(initialStance.Stance == "Hostile", "DeclareWarCommand must set target stance to Hostile.");
        Check(initialStance.Tension >= 90, "DeclareWarCommand must elevate tension.");
        Check(vm.SenateVoteOutcome.Contains("CASUS BELLI RATIFIED"), "Senate vote outcome must reflect casus belli ratification.");

        vm.ProposePeaceTreatyCommand.Execute(null);
        Check(initialStance.Stance == "Truce", "ProposePeaceTreatyCommand must set target stance to Truce.");
        Check(initialStance.Tension <= 30, "ProposePeaceTreatyCommand must reduce tension.");
        Check(vm.SenateVoteOutcome.Contains("PEACE TREATY SIGNED"), "Senate vote outcome must reflect peace treaty signing.");
    }

    static void GenericOperationFlightDeckDiagnostics()
    {
        var vm = new GenericOperationDashboardViewModel();
        Check(vm.DiagnosticFindings.Count == 4, "Flight deck must contain 4 baseline boundary telemetry findings.");
        Check(vm.ScanProgressPercentage == 100.0, "Initial scan progress must be 100%.");

        vm.RunDiagnosticScanCommand.Execute(null);
        Check(vm.ScanStatusMessage.Contains("DIAGNOSTIC COMPLETE"), "RunDiagnosticScanCommand must report complete status.");
        Check(vm.DiagnosticFindings.Count == 4, "Diagnostic findings must be intact after scan.");

        vm.ClearFindingsCommand.Execute(null);
        Check(vm.DiagnosticFindings.Count == 0, "ClearFindingsCommand must clear all diagnostic findings.");
        Check(vm.ScanStatusMessage.Contains("FINDINGS CLEARED"), "ClearFindingsCommand must update status message.");

        vm.RunDiagnosticScanCommand.Execute(null);
        Check(vm.DiagnosticFindings.Count == 4, "Re-running scan must repopulate verified boundary findings.");
    }

    static void VectorChartControlsTelemetry()
    {
        // 1. Troop Tree Pentagonal Radar
        var troopVm = new TroopTreeDashboardViewModel();
        Check(troopVm.CombatRadarAxes != null && troopVm.CombatRadarAxes.Count == 5, "Troop tree must define 5 combat radar axes.");
        Check(troopVm.SelectedTroopRadarValues != null && troopVm.SelectedTroopRadarValues.Count == 5, "Selected troop must expose 5 radar values.");
        Check(troopVm.CaptainPerksRadarValues != null && troopVm.CaptainPerksRadarValues.Count == 5, "Captain perks overlay must expose 5 radar values.");
        Check(troopVm.SelectedTroopRadarValues.All(v => v >= 0.0 && v <= 1.0), "Troop radar values must be normalized in [0, 1].");
        Check(troopVm.CaptainPerksRadarValues.All(v => v >= 0.0 && v <= 1.0), "Captain perks radar values must be normalized in [0, 1].");

        var initialLegionaryDmg = troopVm.SelectedTroopRadarValues[1];
        if (troopVm.AllHighlightedTroops.Count > 1)
        {
            troopVm.SelectedTroop = troopVm.AllHighlightedTroops[0];
            Check(troopVm.SelectedTroopRadarValues[1] != initialLegionaryDmg, "Selecting recruit must update radar damage value reactively.");
        }

        troopVm.ToggleCaptainPerksCommand.Execute(null);
        Check(troopVm.CaptainPerksActive, "ToggleCaptainPerksCommand must activate captain perks.");
        Check(troopVm.CaptainPerksRadarValues.All(v => v >= 0.0 && v <= 1.0), "Toggling captain perks discount must maintain normalized radar bounds.");

        // 2. Workshop 30-Day Cumulative Sparkline & Rebellion Risk Gauge
        var wsVm = new WorkshopDashboardViewModel();
        Check(wsVm.CumulativeProfitTrajectory != null && wsVm.CumulativeProfitTrajectory.Count >= 30, "Workshop must expose 30-day cumulative profit trajectory.");
        Check(wsVm.RebellionRiskGaugeValue >= 0.0 && wsVm.RebellionRiskGaugeValue <= 100.0, "Rebellion risk gauge must be bounded in [0, 100].");

        var initialRisk = wsVm.RebellionRiskGaugeValue;
        wsVm.CostMultiplier = 1.5;
        Check(wsVm.RebellionRiskGaugeValue != initialRisk, "Modifying cost multiplier must reactively update rebellion risk gauge.");

        // 3. Kingdom Diplomacy Radar & Ring Gauges
        var diploVm = new KingdomDiplomacyDashboardViewModel();
        Check(diploVm.SenateConsensusGaugeValue >= 0.0 && diploVm.SenateConsensusGaugeValue <= 100.0, "Senate consensus gauge must be bounded in [0, 100].");
        Check(diploVm.WarRiskGaugeValue >= 0.0 && diploVm.WarRiskGaugeValue <= 100.0, "War risk gauge must be bounded in [0, 100].");
        Check(diploVm.DiplomaticRadarAxes != null && diploVm.DiplomaticRadarAxes.Count == 5, "Diplomatic radar must define 5 geopolitical axes.");
        Check(diploVm.FactionPowerRadarValues != null && diploVm.FactionPowerRadarValues.Count == 5, "Faction power radar must expose 5 normalized values.");
        Check(diploVm.FactionPowerRadarValues.All(v => v >= 0.0 && v <= 1.0), "Faction power radar values must be normalized in [0, 1].");

        var initialDiploRisk = diploVm.WarRiskGaugeValue;
        diploVm.DeclareWarCommand.Execute(null);
        Check(diploVm.WarRiskGaugeValue > initialDiploRisk, "Declaring war must escalate war risk gauge.");

        var escalatedRisk = diploVm.WarRiskGaugeValue;
        diploVm.ProposePeaceTreatyCommand.Execute(null);
        Check(diploVm.WarRiskGaugeValue < escalatedRisk, "Proposing peace must de-escalate war risk gauge.");

        // 4. CoALA Agent Memory Decay Sparkline & Slot Utilization Gauge
        var memVm = new AgentMemoryDashboardViewModel();
        Check(memVm.MemoryDecayTrajectory != null && memVm.MemoryDecayTrajectory.Count == 24, "Memory decay trajectory must expose 24 hourly intervals.");
        Check(memVm.MemorySlotUtilizationGaugeValue >= 0.0 && memVm.MemorySlotUtilizationGaugeValue <= memVm.MaximumAgentSlots, "Memory slot utilization gauge must be bounded by the SDK agent capacity.");
        Check(memVm.MemoryDecayTrajectory[0] > memVm.MemoryDecayTrajectory[23], "Memory utility trajectory must exhibit decay over time.");

        // 5. Generic Operation IPC Latency Sparkline & Buffer Gauge
        var opVm = new GenericOperationDashboardViewModel();
        Check(opVm.ExecutionLatencyTrajectory != null && opVm.ExecutionLatencyTrajectory.Count == 16, "Generic operation must expose 16-sample latency trajectory.");
        Check(opVm.BufferUtilizationGaugeValue >= 0.0 && opVm.BufferUtilizationGaugeValue <= 100.0, "Buffer utilization gauge must be bounded in [0, 100].");

        var initialBuf = opVm.BufferUtilizationGaugeValue;
        opVm.CycleScenario(1);
        Check(opVm.BufferUtilizationGaugeValue != initialBuf, "Cycling scenario must reactively update buffer gauge.");

        // 6. Tactical Combat & Siege Studio Dashboard (Rev074)
        var combatVm = new CombatStudioDashboardViewModel();
        Check(combatVm.CombatRadarAxes != null && combatVm.CombatRadarAxes.Count == 5, "Combat studio must expose 5 tactical doctrine axes.");
        Check(combatVm.DoctrineRadarValues != null && combatVm.DoctrineRadarValues.Count == 5, "Combat studio doctrine values must have 5 entries.");
        Check(combatVm.CounterDoctrineRadarValues != null && combatVm.CounterDoctrineRadarValues.Count == 5, "Combat studio counter-doctrine values must have 5 entries.");
        Check(combatVm.DoctrineRadarValues.All(v => v >= 0.0 && v <= 1.0), "Doctrine radar values must be normalized in [0, 1].");
        Check(combatVm.CounterDoctrineRadarValues.All(v => v >= 0.0 && v <= 1.0), "Counter-doctrine radar values must be normalized in [0, 1].");
        Check(combatVm.LossesTrajectory != null && combatVm.LossesTrajectory.Count == 16, "Losses trajectory must contain 16 intervals.");
        Check(combatVm.MoraleCohesionGaugeValue >= 0.0 && combatVm.MoraleCohesionGaugeValue <= 100.0, "Morale cohesion gauge must be bounded in [0, 100].");
        Check(combatVm.SiegeBreachGaugeValue >= 0.0 && combatVm.SiegeBreachGaugeValue <= 100.0, "Siege breach gauge must be bounded in [0, 100].");
        Check(combatVm.Contingents != null && combatVm.Contingents.Count == 4, "Combat studio must provide 4 battle cohorts.");
        Check(!string.IsNullOrWhiteSpace(combatVm.StudioDocumentation), "Combat studio must provide forensic documentation.");
        Check(!string.IsNullOrWhiteSpace(combatVm.ArchitecturalInvariants), "Combat studio must declare architectural invariants.");

        // 7. Caravan & Market Trade Hub Dashboard (Rev074)
        var tradeVm = new CaravanTradeDashboardViewModel();
        Check(tradeVm.TradeRadarAxes != null && tradeVm.TradeRadarAxes.Count == 5, "Trade hub must expose 5 route profile axes.");
        Check(tradeVm.RouteRadarValues != null && tradeVm.RouteRadarValues.Count == 5, "Trade route radar values must have 5 entries.");
        Check(tradeVm.CompetitiveRouteRadarValues != null && tradeVm.CompetitiveRouteRadarValues.Count == 5, "Competitive route radar values must have 5 entries.");
        Check(tradeVm.RouteRadarValues.All(v => v >= 0.0 && v <= 1.0), "Trade route radar values must be normalized in [0, 1].");
        Check(tradeVm.PriceSpreadTrajectory != null && tradeVm.PriceSpreadTrajectory.Count == 16, "Price spread trajectory must contain 16 intervals.");
        Check(tradeVm.CaravanSurvivalGaugeValue >= 0.0 && tradeVm.CaravanSurvivalGaugeValue <= 100.0, "Caravan survival gauge must be bounded in [0, 100].");
        Check(tradeVm.TariffFrictionGaugeValue >= 0.0 && tradeVm.TariffFrictionGaugeValue <= 100.0, "Tariff friction gauge must be bounded in [0, 100].");
        Check(tradeVm.Commodities != null && tradeVm.Commodities.Count == 5, "Caravan trade hub must expose 5 arbitrage goods.");
        Check(!string.IsNullOrWhiteSpace(tradeVm.StudioDocumentation), "Trade hub must provide forensic documentation.");
        Check(!string.IsNullOrWhiteSpace(tradeVm.ArchitecturalInvariants), "Trade hub must declare architectural invariants.");

        // 8. Adaptive Domain Category Telemetry for Generic Flight Deck (Rev074)
        opVm.ConfigureDomainCategory("Politics");
        Check(opVm.ActiveCategoryGroup == "Politics", "Active category group must be Politics.");
        Check(opVm.CategoryRadarAxes != null && opVm.CategoryRadarAxes.Count == 5, "Politics category must provide 5 radar axes.");
        Check(opVm.CategoryRadarValues != null && opVm.CategoryRadarValues.Count == 5, "Politics category must provide 5 radar values.");
        Check(opVm.CategoryTrendTrajectory != null && opVm.CategoryTrendTrajectory.Count == 16, "Politics category must provide 16-point trend trajectory.");
        Check(opVm.CategoryHealthGaugeValue == 91.2, "Politics health gauge value must be 91.2.");

        opVm.ConfigureDomainCategory("Assets");
        Check(opVm.ActiveCategoryGroup == "Assets", "Active category group must be Assets.");
        Check(opVm.CategoryHealthGaugeValue == 92.5, "Assets health gauge value must be 92.5.");

        // 9. Gauntlet UI & HUD Studio Dashboard (Rev075)
        var gauntletVm = new GauntletStudioDashboardViewModel();
        Check(gauntletVm.GauntletRadarAxes != null && gauntletVm.GauntletRadarAxes.Count == 5, "Gauntlet studio must expose 5 UI telemetry axes.");
        Check(gauntletVm.HudRadarValues != null && gauntletVm.HudRadarValues.Count == 5, "Gauntlet HUD radar values must have 5 entries.");
        Check(gauntletVm.AlternativeHudRadarValues != null && gauntletVm.AlternativeHudRadarValues.Count == 5, "Alternative HUD radar values must have 5 entries.");
        Check(gauntletVm.HudRadarValues.All(v => v >= 0.0 && v <= 1.0), "HUD radar values must be normalized in [0, 1].");
        Check(gauntletVm.AlternativeHudRadarValues.All(v => v >= 0.0 && v <= 1.0), "Alternative HUD radar values must be normalized in [0, 1].");
        Check(gauntletVm.DrawCallLatencyTrajectory != null && gauntletVm.DrawCallLatencyTrajectory.Count == 16, "DrawCall trajectory must contain 16 intervals.");
        Check(gauntletVm.VisualTreeDepthGaugeValue >= 0.0 && gauntletVm.VisualTreeDepthGaugeValue <= 100.0, "Visual tree depth gauge must be bounded in [0, 100].");
        Check(gauntletVm.ScreenCoverageGaugeValue >= 0.0 && gauntletVm.ScreenCoverageGaugeValue <= 100.0, "Screen coverage gauge must be bounded in [0, 100].");
        Check(gauntletVm.Widgets != null && gauntletVm.Widgets.Count == 5, "Gauntlet studio must expose 5 active widgets.");
        Check(gauntletVm.GauntletRenderPipelineSteps != null && gauntletVm.GauntletRenderPipelineSteps.Count == 4, "Gauntlet studio must expose 4 render pipeline steps.");
        Check(!string.IsNullOrWhiteSpace(gauntletVm.StudioDocumentation), "Gauntlet studio must provide forensic documentation.");
        Check(!string.IsNullOrWhiteSpace(gauntletVm.ArchitecturalInvariants), "Gauntlet studio must declare architectural invariants.");

        // 10. Campaign World & Expedition Studio Dashboard (Rev075)
        var campaignVm = new CampaignStudioDashboardViewModel();
        Check(campaignVm.CampaignRadarAxes != null && campaignVm.CampaignRadarAxes.Count == 5, "Campaign studio must expose 5 province profile axes.");
        Check(campaignVm.SettlementRadarValues != null && campaignVm.SettlementRadarValues.Count == 5, "Settlement radar values must have 5 entries.");
        Check(campaignVm.FrontierRadarValues != null && campaignVm.FrontierRadarValues.Count == 5, "Frontier radar values must have 5 entries.");
        Check(campaignVm.SettlementRadarValues.All(v => v >= 0.0 && v <= 1.0), "Settlement radar values must be normalized in [0, 1].");
        Check(campaignVm.FrontierRadarValues.All(v => v >= 0.0 && v <= 1.0), "Frontier radar values must be normalized in [0, 1].");
        Check(campaignVm.ProsperityTrajectory != null && campaignVm.ProsperityTrajectory.Count == 16, "Prosperity trajectory must contain 16 intervals.");
        Check(campaignVm.ProvinceEquilibriumTrajectory != null && campaignVm.ProvinceEquilibriumTrajectory.Count == 16, "Province equilibrium trajectory must contain 16 intervals.");
        Check(campaignVm.EquilibriumGaugeValue >= 0.0 && campaignVm.EquilibriumGaugeValue <= 100.0, "Equilibrium gauge must be bounded in [0, 100].");
        Check(campaignVm.BanditThreatGaugeValue >= 0.0 && campaignVm.BanditThreatGaugeValue <= 100.0, "Bandit threat gauge must be bounded in [0, 100].");
        Check(campaignVm.Settlements != null && campaignVm.Settlements.Count == 5, "Campaign studio must expose 5 provincial settlements.");
        Check(!string.IsNullOrWhiteSpace(campaignVm.StudioDocumentation), "Campaign studio must provide forensic documentation.");
        Check(!string.IsNullOrWhiteSpace(campaignVm.ArchitecturalInvariants), "Campaign studio must declare architectural invariants.");

        // 11. Live Session & Memory APM Telemetry Studio Dashboard (Rev076)
        var liveVm = new LiveSessionDashboardViewModel();
        Check(liveVm.LiveSessionRadarAxes != null && liveVm.LiveSessionRadarAxes.Count == 5, "Live session studio must expose 5 APM telemetry axes.");
        Check(liveVm.ActiveSessionRadarValues != null && liveVm.ActiveSessionRadarValues.Count == 5, "Active session radar values must have 5 entries.");
        Check(liveVm.HighConcurrencyRadarValues != null && liveVm.HighConcurrencyRadarValues.Count == 5, "High concurrency radar values must have 5 entries.");
        Check(liveVm.ActiveSessionRadarValues.All(v => v >= 0.0 && v <= 1.0), "Active session radar values must be normalized in [0, 1].");
        Check(liveVm.HighConcurrencyRadarValues.All(v => v >= 0.0 && v <= 1.0), "High concurrency radar values must be normalized in [0, 1].");
        Check(liveVm.PipeLatencyTrajectory != null && liveVm.PipeLatencyTrajectory.Count == 16, "Pipe latency trajectory must contain 16 intervals.");
        Check(liveVm.MemoryGaugeValue >= 0.0 && liveVm.MemoryGaugeValue <= 100.0, "Memory gauge must be bounded in [0, 100].");
        Check(liveVm.RingBufferGaugeValue >= 0.0 && liveVm.RingBufferGaugeValue <= 100.0, "Ring buffer gauge must be bounded in [0, 100].");
        Check(liveVm.Events != null && liveVm.Events.Count == 5, "Live session studio must expose 5 default events.");
        Check(!string.IsNullOrWhiteSpace(liveVm.StudioDocumentation), "Live session studio must provide forensic documentation.");
        Check(!string.IsNullOrWhiteSpace(liveVm.ArchitecturalInvariants), "Live session studio must declare architectural invariants.");

        // 12. Delivery & Distribution Studio Dashboard (Rev076)
        var deliveryVm = new DeliveryStudioDashboardViewModel();
        Check(deliveryVm.DeliveryRadarAxes != null && deliveryVm.DeliveryRadarAxes.Count == 5, "Delivery studio must expose 5 delivery radar axes.");
        Check(deliveryVm.ReleaseRadarValues != null && deliveryVm.ReleaseRadarValues.Count == 5, "Release profile radar values must have 5 entries.");
        Check(deliveryVm.QuickProfileRadarValues != null && deliveryVm.QuickProfileRadarValues.Count == 5, "Quick profile radar values must have 5 entries.");
        Check(deliveryVm.ReleaseRadarValues.All(v => v >= 0.0 && v <= 1.0), "Release radar values must be normalized in [0, 1].");
        Check(deliveryVm.QuickProfileRadarValues.All(v => v >= 0.0 && v <= 1.0), "Quick profile radar values must be normalized in [0, 1].");
        Check(deliveryVm.ThroughputTrajectory != null && deliveryVm.ThroughputTrajectory.Count == 16, "Throughput trajectory must contain 16 intervals.");
        Check(deliveryVm.ArchiveHygieneGaugeValue >= 0.0 && deliveryVm.ArchiveHygieneGaugeValue <= 100.0, "Archive hygiene gauge must be bounded in [0, 100].");
        Check(deliveryVm.CompressionRatioGaugeValue >= 0.0 && deliveryVm.CompressionRatioGaugeValue <= 100.0, "Compression ratio gauge must be bounded in [0, 100].");
        Check(deliveryVm.Packages != null && deliveryVm.Packages.Count == 4, "Delivery studio must expose 4 package artifacts.");
        Check(deliveryVm.DeliveryPipelineSteps != null && deliveryVm.DeliveryPipelineSteps.Count == 4, "Delivery studio must expose 4 distribution pipeline steps.");
        Check(!string.IsNullOrWhiteSpace(deliveryVm.StudioDocumentation), "Delivery studio must provide forensic documentation.");
        Check(!string.IsNullOrWhiteSpace(deliveryVm.ArchitecturalInvariants), "Delivery studio must declare architectural invariants.");

        // 13. Diagnostics & Integrity Studio Dashboard (Rev077)
        var diagVm = new DiagnosticsStudioDashboardViewModel();
        Check(diagVm.DiagnosticsRadarAxes != null && diagVm.DiagnosticsRadarAxes.Count == 5, "Diagnostics studio must expose 5 integrity radar axes.");
        Check(diagVm.StrictRadarValues != null && diagVm.StrictRadarValues.Count == 5, "Strict profile radar values must have 5 entries.");
        Check(diagVm.SandboxRadarValues != null && diagVm.SandboxRadarValues.Count == 5, "Sandbox profile radar values must have 5 entries.");
        Check(diagVm.StrictRadarValues.All(v => v >= 0.0 && v <= 1.0), "Strict radar values must be normalized in [0, 1].");
        Check(diagVm.SandboxRadarValues.All(v => v >= 0.0 && v <= 1.0), "Sandbox radar values must be normalized in [0, 1].");
        Check(diagVm.ScanLatencyTrajectory != null && diagVm.ScanLatencyTrajectory.Count == 16, "Scan latency trajectory must contain 16 intervals.");
        Check(diagVm.DiagnosticsMemoryTrajectory != null && diagVm.DiagnosticsMemoryTrajectory.Count == 16, "Diagnostics memory trajectory must contain 16 intervals.");
        Check(diagVm.ComplianceScoreGaugeValue >= 0.0 && diagVm.ComplianceScoreGaugeValue <= 100.0, "Compliance score gauge must be bounded in [0, 100].");
        Check(diagVm.FaultRiskGaugeValue >= 0.0 && diagVm.FaultRiskGaugeValue <= 100.0, "Fault risk gauge must be bounded in [0, 100].");
        Check(diagVm.Findings != null && diagVm.Findings.Count == 4, "Diagnostics studio must expose 4 initial strict findings.");
        Check(diagVm.HexDumpSnippets != null && diagVm.HexDumpSnippets.Count == 4, "Diagnostics studio must expose 4 hex dump snippets.");
        Check(!string.IsNullOrWhiteSpace(diagVm.StudioDocumentation), "Diagnostics studio must provide forensic documentation.");
        Check(!string.IsNullOrWhiteSpace(diagVm.ArchitecturalInvariants), "Diagnostics studio must declare architectural invariants.");

        diagVm.CycleScenario(1);
        Check(diagVm.ActiveProfileLabel == "Development Sandbox & Crash Forensics", "Cycling scenario must switch to Sandbox profile.");
        Check(diagVm.ComplianceScoreGaugeValue == 84.0, "Sandbox compliance score must be 84.0.");
        Check(diagVm.FaultRiskGaugeValue == 22.5, "Sandbox fault risk must be 22.5.");
        Check(diagVm.OverallHealthStatus.Contains("ANOMALY ISOLATED"), "Sandbox health status must indicate anomaly isolated.");

        diagVm.CycleScenario(2);
        Check(diagVm.ActiveProfileLabel == "Strict Production Audit", "Cycling scenario back to 0/2 must switch to Strict profile.");
        Check(diagVm.ComplianceScoreGaugeValue == 98.5, "Strict compliance score must be 98.5.");
        Check(diagVm.FaultRiskGaugeValue == 8.0, "Strict fault risk must be 8.0.");
    }

    static void VectorBarChartAndHeatmapTelemetry()
    {
        // 1. Troop tree stat distribution bars
        var troopVm = new TroopTreeDashboardViewModel();
        Check(troopVm.TroopStatDistributionBars != null && troopVm.TroopStatDistributionBars.Count == 5, "Troop tree must expose 5 stat distribution bars.");
        Check(troopVm.TroopStatDistributionBars.All(b => b.Value > 0), "Troop stat distribution bars must have positive values.");
        Check(troopVm.TroopStatDistributionBars.Any(b => b.Label == "Vitality"), "Troop stat distribution bars must include Vitality.");

        // 2. Workshop economic breakdown bars
        var workshopVm = new WorkshopDashboardViewModel();
        Check(workshopVm.WorkshopEconomicBreakdownBars != null && workshopVm.WorkshopEconomicBreakdownBars.Count == 5, "Workshop must expose 5 economic breakdown bars.");
        Check(workshopVm.WorkshopEconomicBreakdownBars.Any(b => b.Label == "Net Daily"), "Workshop breakdown must include Net Daily.");

        // 3. Kingdom power comparison bars
        var kingdomVm = new KingdomDiplomacyDashboardViewModel();
        Check(kingdomVm.KingdomPowerComparisonBars != null && kingdomVm.KingdomPowerComparisonBars.Count == 6, "Kingdom diplomacy must expose 6 power comparison bars.");
        Check(kingdomVm.KingdomPowerComparisonBars.Any(b => b.Label == "Vlandia"), "Kingdom power bars must include Vlandia.");

        // 4. Operation throughput bars, pipeline steps & trajectory
        var genericVm = new GenericOperationDashboardViewModel();
        Check(genericVm.OperationThroughputBars != null && genericVm.OperationThroughputBars.Count == 5, "Generic operation must expose 5 throughput bars.");
        Check(genericVm.OperationThroughputBars.Any(b => b.Label == "Memory Audit"), "Generic operation bars must include Memory Audit.");
        Check(genericVm.OperationPipelineSteps != null && genericVm.OperationPipelineSteps.Count == 4, "Generic operation must expose 4 execution pipeline steps.");
        Check(genericVm.OperationThroughputTrajectory != null && genericVm.OperationThroughputTrajectory.Count == 16, "Generic operation must expose 16-point throughput trajectory.");

        // 5. Combat studio weapon breakdown & formation heatmap
        var combatVm = new CombatStudioDashboardViewModel();
        Check(combatVm.WeaponDamageBreakdownBars != null && combatVm.WeaponDamageBreakdownBars.Count == 5, "Combat studio must expose 5 weapon damage breakdown bars.");
        Check(combatVm.FormationCombatHeatmap != null && combatVm.FormationCombatHeatmap.Count == 3 && combatVm.FormationCombatHeatmap.All(r => r.Count == 5), "Combat studio formation heatmap must be 3x5 matrix.");
        Check(combatVm.FormationHeatmapRows != null && combatVm.FormationHeatmapRows.Count == 3, "Combat studio formation heatmap must have 3 row headers.");
        Check(combatVm.FormationHeatmapCols != null && combatVm.FormationHeatmapCols.Count == 5, "Combat studio formation heatmap must have 5 col headers.");

        // 6. Caravan trade commodity margins & seasonal heatmap
        var tradeVm = new CaravanTradeDashboardViewModel();
        Check(tradeVm.CommodityProfitMarginBars != null && tradeVm.CommodityProfitMarginBars.Count == 6, "Caravan trade must expose 6 commodity margin bars.");
        Check(tradeVm.SeasonalTradeLiquidityHeatmap != null && tradeVm.SeasonalTradeLiquidityHeatmap.Count == 4 && tradeVm.SeasonalTradeLiquidityHeatmap.All(r => r.Count == 4), "Caravan trade liquidity heatmap must be 4x4 matrix.");
        Check(tradeVm.TradeHeatmapRows != null && tradeVm.TradeHeatmapRows.Count == 4, "Caravan trade heatmap must have 4 row headers.");
        Check(tradeVm.TradeHeatmapCols != null && tradeVm.TradeHeatmapCols.Count == 4, "Caravan trade heatmap must have 4 col headers.");
    }

    static void Rev086StudioEnrichmentAndTimelineTelemetry()
    {
        // 1. CodeSecurityDashboardViewModel
        var securityVm = new CodeSecurityDashboardViewModel();
        Check(securityVm.SecurityAuditPipelineSteps != null && securityVm.SecurityAuditPipelineSteps.Count == 4,
            "CodeSecurity dashboard must expose 4 audit pipeline steps.");
        Check(securityVm.SecurityRiskDensityTrajectory != null && securityVm.SecurityRiskDensityTrajectory.Count == 16,
            "CodeSecurity dashboard must expose 16-point risk density trajectory.");

        // 2. ModuleHierarchyDashboardViewModel
        var hierarchyVm = new ModuleHierarchyDashboardViewModel();
        Check(hierarchyVm.ModuleResolutionPipelineSteps != null && hierarchyVm.ModuleResolutionPipelineSteps.Count == 4,
            "ModuleHierarchy dashboard must expose 4 resolution pipeline steps.");
        Check(hierarchyVm.ModuleLoadLatencyTrajectory != null && hierarchyVm.ModuleLoadLatencyTrajectory.Count == 16,
            "ModuleHierarchy dashboard must expose 16-point load latency trajectory.");

        // 3. ComponentGeneratorDashboardViewModel
        var generatorVm = new ComponentGeneratorDashboardViewModel();
        Check(generatorVm.BlueprintSynthesisPipelineSteps != null && generatorVm.BlueprintSynthesisPipelineSteps.Count == 4,
            "ComponentGenerator dashboard must expose 4 synthesis pipeline steps.");
        Check(generatorVm.SynthesisThroughputTrajectory != null && generatorVm.SynthesisThroughputTrajectory.Count == 16,
            "ComponentGenerator dashboard must expose 16-point synthesis throughput trajectory.");

        // 4. LiveSessionDashboardViewModel
        var sessionVm = new LiveSessionDashboardViewModel();
        Check(sessionVm.PipeConnectionPipelineSteps != null && sessionVm.PipeConnectionPipelineSteps.Count == 4,
            "LiveSession dashboard must expose 4 pipe connection pipeline steps.");
        Check(sessionVm.PipeEventThroughputTrajectory != null && sessionVm.PipeEventThroughputTrajectory.Count == 16,
            "LiveSession dashboard must expose 16-point event throughput trajectory.");
        Check(sessionVm.CurrentPacketTimestamp > 0.0,
            "LiveSession dashboard must expose non-zero current packet timestamp.");

        // 5. ForgeTimelineRuler
        var ruler = new ForgeTimelineRuler
        {
            TotalDuration = 60,
            CurrentTime = 42.5,
            MajorInterval = 10,
            MinorInterval = 2,
            UnitLabel = "s",
            MarkerLabel = "TEST_PACKET"
        };
        Check(ruler.TotalDuration == 60, "Timeline ruler TotalDuration must be 60.");
        Check(ruler.CurrentTime == 42.5, "Timeline ruler CurrentTime must be 42.5.");
        Check(ruler.MajorInterval == 10, "Timeline ruler MajorInterval must be 10.");
        Check(ruler.MinorInterval == 2, "Timeline ruler MinorInterval must be 2.");
    }

    static void Rev087SpecializedStudiosAndAquilaSealTelemetry()
    {
        // 1. AudioStudioDashboardViewModel
        var audioVm = new AudioStudioDashboardViewModel();
        Check(audioVm.AudioDspPipelineSteps != null && audioVm.AudioDspPipelineSteps.Count == 4,
            "AudioStudio dashboard must expose 4 DSP pipeline steps.");
        Check(audioVm.AudioSpectralDensityTrajectory != null && audioVm.AudioSpectralDensityTrajectory.Count == 16,
            "AudioStudio dashboard must expose 16-point spectral density trajectory.");
        Check(audioVm.CurrentPlaybackTimestamp > 0.0,
            "AudioStudio dashboard must expose non-zero current playback timestamp.");
        audioVm.ApplyEqualizerPreset("Voice Clarity");
        Check(audioVm.AudioDspPipelineSteps.Any(s => s.Title == "Parametric EQ" && s.Status == ForgeStepStatus.Active),
            "Equalizer preset must update DSP pipeline step status.");

        // 2. WorkshopDashboardViewModel
        var workshopVm = new WorkshopDashboardViewModel();
        Check(workshopVm.ProductionChainPipelineSteps != null && workshopVm.ProductionChainPipelineSteps.Count == 4,
            "Workshop dashboard must expose 4 production chain pipeline steps.");
        Check(workshopVm.EnterpriseProfitMarginTrajectory != null && workshopVm.EnterpriseProfitMarginTrajectory.Count == 16,
            "Workshop dashboard must expose 16-point profit margin trajectory.");
        Check(workshopVm.EconomicCycleTimelineDays > 0.0,
            "Workshop dashboard must expose non-zero economic cycle timeline days.");
        workshopVm.HorizonDays = 7;
        Check(workshopVm.ProductionChainPipelineSteps.Any(s => s.Title == "Artisan Forge" && s.Status == ForgeStepStatus.Active),
            "Changing workshop horizon must update pipeline steps.");

        // 3. AgentMemoryDashboardViewModel
        var memoryVm = new AgentMemoryDashboardViewModel();
        Check(memoryVm.CognitiveConsolidationPipelineSteps != null && memoryVm.CognitiveConsolidationPipelineSteps.Count == 4,
            "AgentMemory dashboard must expose 4 cognitive consolidation pipeline steps.");
        Check(memoryVm.MemoryUtilityDecayCurve != null && memoryVm.MemoryUtilityDecayCurve.Count == 16,
            "AgentMemory dashboard must expose 16-point utility decay curve.");
        Check(memoryVm.EpisodicTimelineHours > 0.0,
            "AgentMemory dashboard must expose non-zero episodic timeline hours.");
        memoryVm.CycleScenario(1);
        Check(memoryVm.CognitiveConsolidationPipelineSteps.Any(s => s.Title == "Schema Consolidation" && s.Status == ForgeStepStatus.Active),
            "Cycling agent memory scenario must update consolidation pipeline steps.");

        // 4. KingdomDiplomacyDashboardViewModel
        var kingdomVm = new KingdomDiplomacyDashboardViewModel();
        Check(kingdomVm.DiplomaticResolutionPipelineSteps != null && kingdomVm.DiplomaticResolutionPipelineSteps.Count == 4,
            "KingdomDiplomacy dashboard must expose 4 diplomatic resolution pipeline steps.");
        Check(kingdomVm.GeopoliticalTensionTrajectory != null && kingdomVm.GeopoliticalTensionTrajectory.Count == 16,
            "KingdomDiplomacy dashboard must expose 16-point geopolitical tension trajectory.");
        Check(kingdomVm.TreatyTruceTimelineDays >= 0.0,
            "KingdomDiplomacy dashboard must expose valid treaty truce timeline days.");
        kingdomVm.CycleScenario(1);
        Check(kingdomVm.GeopoliticalTensionTrajectory.Any(p => p > 0.50),
            "Cycling kingdom scenario must update tension trajectory.");
    }

    static void Rev088TroopCombatTradeVisualTelemetry()
    {
        // 1. TroopTreeDashboardViewModel
        var troopVm = new TroopTreeDashboardViewModel();
        Check(troopVm.TroopProgressionPipelineSteps != null && troopVm.TroopProgressionPipelineSteps.Count == 4,
            "TroopTree dashboard must expose 4 progression pipeline steps.");
        Check(troopVm.TroopStatCurveTrajectory != null && troopVm.TroopStatCurveTrajectory.Count == 16,
            "TroopTree dashboard must expose 16-point stat curve trajectory.");
        Check(troopVm.BattleSimulationTimestamp > 0.0,
            "TroopTree dashboard must expose non-zero battle simulation timestamp.");
        troopVm.ToggleCaptainPerksCommand.Execute(null);
        Check(troopVm.TroopStatCurveTrajectory.Count == 16,
            "Toggling captain perks must recalculate stat curve trajectory.");
        troopVm.SetDynasticNobleScenario();
        Check(troopVm.TroopProgressionPipelineSteps.Any(s => s.Title == "Elite Banner Veteran" && (s.Status == ForgeStepStatus.Active || s.Status == ForgeStepStatus.Completed)),
            "Setting dynastic noble scenario must update progression pipeline steps to advanced tier.");

        // 2. CombatStudioDashboardViewModel
        var combatVm = new CombatStudioDashboardViewModel();
        Check(combatVm.BattleDoctrinePipelineSteps != null && combatVm.BattleDoctrinePipelineSteps.Count == 4,
            "CombatStudio dashboard must expose 4 battle doctrine pipeline steps.");
        Check(combatVm.CombatPressureTrajectory != null && combatVm.CombatPressureTrajectory.Count == 16,
            "CombatStudio dashboard must expose 16-point combat pressure trajectory.");
        Check(combatVm.BattlePhaseElapsedMinutes > 0.0,
            "CombatStudio dashboard must expose non-zero elapsed battle minutes.");
        combatVm.CycleScenario(1);
        Check(combatVm.BattleDoctrinePipelineSteps.Any(s => s.Title == "Flank Shock Cavalry" && s.Status == ForgeStepStatus.Active),
            "Cycling combat scenario must activate cavalry shock doctrine phase.");
        Check(combatVm.CombatPressureTrajectory.Count == 16,
            "Cycled combat scenario must maintain 16-point combat pressure trajectory.");

        // 3. CaravanTradeDashboardViewModel
        var tradeVm = new CaravanTradeDashboardViewModel();
        Check(tradeVm.CaravanExpeditionPipelineSteps != null && tradeVm.CaravanExpeditionPipelineSteps.Count == 4,
            "CaravanTrade dashboard must expose 4 caravan expedition pipeline steps.");
        Check(tradeVm.ArbitrageYieldTrajectory != null && tradeVm.ArbitrageYieldTrajectory.Count == 16,
            "CaravanTrade dashboard must expose 16-point arbitrage yield trajectory.");
        Check(tradeVm.RouteTransitDays > 0.0,
            "CaravanTrade dashboard must expose non-zero route transit days.");
        tradeVm.CycleScenario(1);
        Check(tradeVm.CaravanExpeditionPipelineSteps.Any(s => s.Title == "Market Arbitrage" && s.Status == ForgeStepStatus.Active),
            "Cycling trade scenario must advance expedition to market arbitrage phase.");
        Check(tradeVm.ArbitrageYieldTrajectory.Count == 16,
            "Cycled trade scenario must maintain 16-point yield trajectory.");
    }

    static void Rev089CampaignDiagnosticsDeliveryGauntletVisualTelemetry()
    {
        // 1. GauntletStudioDashboardViewModel
        var gauntletVm = new GauntletStudioDashboardViewModel();
        Check(gauntletVm.GauntletRenderPipelineSteps != null && gauntletVm.GauntletRenderPipelineSteps.Count == 4,
            "GauntletStudio dashboard must expose 4 render pipeline steps.");
        Check(gauntletVm.LayoutPassDensityTrajectory != null && gauntletVm.LayoutPassDensityTrajectory.Count == 16,
            "GauntletStudio dashboard must expose 16-point layout pass density trajectory.");
        Check(gauntletVm.FrameRenderBudgetMs > 0.0,
            "GauntletStudio dashboard must expose non-zero frame render budget ms.");
        gauntletVm.CycleScenario(1);
        Check(gauntletVm.FrameRenderBudgetMs > 10.0,
            "Cycling gauntlet scenario must update frame render budget for complex HUD.");
        Check(gauntletVm.LayoutPassDensityTrajectory.Count == 16,
            "Cycled gauntlet scenario must maintain 16-point density trajectory.");

        // 2. CampaignStudioDashboardViewModel
        var campaignVm = new CampaignStudioDashboardViewModel();
        Check(campaignVm.CampaignPacificationPipelineSteps != null && campaignVm.CampaignPacificationPipelineSteps.Count == 4,
            "CampaignStudio dashboard must expose 4 pacification pipeline steps.");
        Check(campaignVm.ProvinceEquilibriumTrajectory != null && campaignVm.ProvinceEquilibriumTrajectory.Count == 16,
            "CampaignStudio dashboard must expose 16-point equilibrium trajectory.");
        Check(campaignVm.CampaignSeasonElapsedDays > 0.0,
            "CampaignStudio dashboard must expose non-zero campaign season elapsed days.");
        campaignVm.CycleScenario(1);
        Check(campaignVm.CampaignPacificationPipelineSteps.Any(s => s.Title == "Tribute Settlement" && s.Status == ForgeStepStatus.Active),
            "Cycling campaign scenario must activate contested tribute settlement phase.");
        Check(campaignVm.CampaignSeasonElapsedDays > 60.0,
            "Cycling campaign scenario must advance season elapsed days.");

        // 3. DeliveryStudioDashboardViewModel
        var deliveryVm = new DeliveryStudioDashboardViewModel();
        Check(deliveryVm.DeliveryPipelineSteps != null && deliveryVm.DeliveryPipelineSteps.Count == 4,
            "DeliveryStudio dashboard must expose 4 delivery pipeline steps.");
        Check(deliveryVm.CompressionThroughputTrajectory != null && deliveryVm.CompressionThroughputTrajectory.Count == 16,
            "DeliveryStudio dashboard must expose 16-point compression throughput trajectory.");
        Check(deliveryVm.PackagingElapsedSeconds > 0.0,
            "DeliveryStudio dashboard must expose non-zero packaging elapsed seconds.");
        deliveryVm.CycleScenario(1);
        Check(deliveryVm.CompressionThroughputTrajectory.Count == 16,
            "Cycled delivery scenario must maintain 16-point throughput trajectory.");
        Check(deliveryVm.PackagingElapsedSeconds < 5.0,
            "CI/CD Quick profile must reduce packaging duration.");

        // 4. DiagnosticsStudioDashboardViewModel
        var diagVm = new DiagnosticsStudioDashboardViewModel();
        Check(diagVm.ForensicAuditPipelineSteps != null && diagVm.ForensicAuditPipelineSteps.Count == 4,
            "DiagnosticsStudio dashboard must expose 4 forensic audit pipeline steps.");
        Check(diagVm.AnomalyDensityTrajectory != null && diagVm.AnomalyDensityTrajectory.Count == 16,
            "DiagnosticsStudio dashboard must expose 16-point anomaly density trajectory.");
        Check(diagVm.ForensicScanElapsedMs > 0.0,
            "DiagnosticsStudio dashboard must expose non-zero forensic scan elapsed ms.");
        diagVm.CycleScenario(1);
        Check(diagVm.ForensicAuditPipelineSteps.Any(s => s.Title == "Certification" && s.Status == ForgeStepStatus.Active),
            "Cycling diagnostics scenario must activate sandbox triage certification phase.");
        Check(diagVm.ForensicScanElapsedMs > 200.0,
            "Sandbox/deep inspection scenario must update forensic scan latency.");
    }

    static void Rev090AudioSecurityHierarchyGeneratorTelemetry()
    {
        // 1. AudioStudioDashboardViewModel
        var audioVm = new AudioStudioDashboardViewModel();
        audioVm.CycleScenario(1);
        Check(audioVm.ActivePreset == "Bass Boost", "Cycling audio scenario to 1 must activate Bass Boost preset.");
        Check(audioVm.PeakDbfs.Contains("-0.8 dBFS"), "Bass Boost preset must configure -0.8 dBFS peak.");
        Check(audioVm.CurrentPlaybackTimestamp == 0.45, "Bass Boost preset must position playback chronometer at 0.45s.");
        audioVm.CycleScenario(2);
        Check(audioVm.ActivePreset == "Voice Clarity", "Cycling audio scenario to 2 must activate Voice Clarity preset.");
        Check(audioVm.PeakDbfs.Contains("-2.1 dBFS"), "Voice Clarity preset must configure -2.1 dBFS peak.");
        audioVm.CycleScenario(3);
        Check(audioVm.ActivePreset == "Combat Punch", "Cycling audio scenario to 3 must activate Combat Punch preset.");

        // 2. CodeSecurityDashboardViewModel
        var securityVm = new CodeSecurityDashboardViewModel();
        Check(securityVm.AuditScanElapsedMs > 0.0, "CodeSecurity dashboard must expose non-zero scan elapsed ms.");
        securityVm.CycleScenario(1);
        Check(securityVm.AuditScanElapsedMs == 142.0, "Engine native reference scenario must record 142.0 ms scan latency.");
        Check(securityVm.TargetAssembly.Contains("TaleWorlds.CampaignSystem.dll"), "Scenario 1 must target TaleWorlds.CampaignSystem.dll.");
        securityVm.CycleScenario(2);
        Check(securityVm.AuditScanElapsedMs == 62.8, "Sandboxed third-party scan must record 62.8 ms latency.");

        // 3. ModuleHierarchyDashboardViewModel
        var hierarchyVm = new ModuleHierarchyDashboardViewModel();
        Check(hierarchyVm.BootSequenceElapsedMs > 0.0, "ModuleHierarchy dashboard must expose non-zero boot sequence elapsed ms.");
        hierarchyVm.CycleScenario(1);
        Check(hierarchyVm.BootSequenceElapsedMs == 310.0, "Minimal stack scenario must record 310.0 ms boot latency.");
        Check(hierarchyVm.ManifestId == "CalradiaForge.Minimal", "Scenario 1 must target CalradiaForge.Minimal.");
        hierarchyVm.CycleScenario(2);
        Check(hierarchyVm.BootSequenceElapsedMs == 680.0, "Multi-mod ecosystem scenario must record 680.0 ms boot latency.");

        // 4. ComponentGeneratorDashboardViewModel
        var generatorVm = new ComponentGeneratorDashboardViewModel();
        Check(generatorVm.SynthesisElapsedMs > 0.0, "ComponentGenerator dashboard must expose non-zero synthesis elapsed ms.");
        generatorVm.CycleScenario(1);
        Check(generatorVm.SynthesisElapsedMs == 45.2, "Gauntlet prefab synthesis scenario must record 45.2 ms synthesis time.");
        Check(generatorVm.TargetOutput.Contains("Prefabs"), "Scenario 1 must target Gauntlet prefabs.");
        generatorVm.CycleScenario(2);
        Check(generatorVm.SynthesisElapsedMs == 19.8, "Troop character synthesis scenario must record 19.8 ms synthesis time.");

        // 5. LiveSessionDashboardViewModel
        var liveVm = new LiveSessionDashboardViewModel();
        liveVm.CycleScenario(1);
        Check(liveVm.CurrentPacketTimestamp == 54.0, "High concurrency scenario must record 54.0s packet timestamp.");
        Check(liveVm.SessionTitle.Contains("HighLoad"), "Scenario 1 must switch to high concurrency APM session.");
        Check(liveVm.PipeEventThroughputTrajectory != null && liveVm.PipeEventThroughputTrajectory.Count == 16, "High-load session must expose 16-point throughput trajectory.");
        liveVm.CycleScenario(2);
        Check(liveVm.CurrentPacketTimestamp == 18.2, "Diagnostics loopback scenario must record 18.2s packet timestamp.");
        Check(liveVm.SessionTitle.Contains("Diagnostics"), "Scenario 2 must switch to loopback diagnostic session.");
    }

    static void Rev091GenericOperationAndVisualTelemetry()
    {
        var genericVm = new GenericOperationDashboardViewModel();
        Check(genericVm.ScanLatencyMs == 1.2, "Generic operation dashboard must initialize with 1.2 ms scan latency.");
        genericVm.CycleScenario(1);
        Check(genericVm.ScanLatencyMs == 3.8, "Exhaustive diagnostic mode scenario must record 3.8 ms scan latency.");
        Check(genericVm.ExecutionMode == "Exhaustive Diagnostic Mode", "Scenario 1 must switch to Exhaustive Diagnostic Mode.");
        genericVm.CycleScenario(2);
        Check(genericVm.ScanLatencyMs == 1.2, "Deterministic bounded route scenario must record 1.2 ms scan latency.");
        Check(genericVm.ExecutionMode == "Deterministic Bounded Route", "Scenario 2 must restore Deterministic Bounded Route.");
    }

    static void Rev092WorkshopGuildAndCommandPaletteVisualTelemetry()
    {
        var workshopVm = new WorkshopDashboardViewModel();
        Check(workshopVm.Settlement.Contains("Marunath"), "Workshop dashboard must initialize with Marunath settlement.");
        Check(workshopVm.EconomicCycleTimelineDays == 18.0, "Workshop dashboard must initialize with 18.0 days timeline.");
        workshopVm.CycleScenario(1);
        Check(workshopVm.Settlement.Contains("Epicrotea"), "Scenario 1 must switch to Epicrotea settlement.");
        Check(workshopVm.OptimalEnterprise.Contains("Smithy & Brewery"), "Epicrotea scenario must identify Smithy & Brewery as optimal.");
        workshopVm.CycleScenario(2);
        Check(workshopVm.Settlement.Contains("Marunath"), "Scenario 2 must restore Marunath settlement.");
        Check(workshopVm.OptimalEnterprise.Contains("Silversmith"), "Marunath scenario must restore Silversmith as optimal.");
    }

    static void Rev093AgentCognitiveMindAndProceduralRadarTelemetry()
    {
        var memVm = new AgentMemoryDashboardViewModel();
        Check(memVm.ProceduralPolicyRadarAxes != null && memVm.ProceduralPolicyRadarAxes.Count == 5,
            "AgentMemory dashboard must expose 5 procedural policy radar axes.");
        Check(memVm.ProceduralPolicyRadarValues != null && memVm.ProceduralPolicyRadarValues.Count == 5,
            "AgentMemory dashboard must expose 5 procedural policy radar values.");
        Check(memVm.ProceduralPolicyRadarComparisonValues != null && memVm.ProceduralPolicyRadarComparisonValues.Count == 5,
            "AgentMemory dashboard must expose 5 comparison baseline values.");

        // Baseline agent: Empress Rhagaea
        Check(memVm.SelectedAgent.Name.Contains("Rhagaea"), "Initial agent must be Empress Rhagaea.");
        Check(memVm.ProceduralPolicyRadarValues[1] == 0.95, "Rhagaea must have 0.95 diplomacy score.");
        Check(memVm.ProceduralPolicyRadarValues[0] == 0.85, "Rhagaea must have 0.85 combat score.");

        // Select King Derthert
        memVm.SelectAgentCommand.Execute(memVm.Agents[1]);
        Check(memVm.SelectedAgent.Name.Contains("Derthert"), "Selected agent must be King Derthert.");
        Check(memVm.ProceduralPolicyRadarValues[0] == 0.90, "Derthert must have 0.90 combat score.");
        Check(memVm.ProceduralPolicyRadarValues[1] == 0.60, "Derthert must have 0.60 diplomacy score.");

        // Select Khan Monchug
        memVm.SelectAgentCommand.Execute(memVm.Agents[2]);
        Check(memVm.SelectedAgent.Name.Contains("Monchug"), "Selected agent must be Khan Monchug.");
        Check(memVm.ProceduralPolicyRadarValues[0] == 0.95, "Monchug must have 0.95 combat score.");
        Check(memVm.ProceduralPolicyRadarValues[4] == 0.85, "Monchug must have 0.85 recon score.");

        // Scenario cycling preserves procedural radar coherence
        memVm.CycleScenario(1);
        Check(memVm.ProceduralPolicyRadarAxes.Count == 5, "Scenario 1 must preserve 5 policy axes.");
        Check(memVm.ProceduralPolicyRadarValues.Count == 5, "Scenario 1 must preserve 5 policy values.");
    }

    static void Rev096FiveResidualIconUpgrades()
    {
        // Rev096: Verify 5 thematic icons replaced all remaining generic aquila/centurion placeholders.
        string xamlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "CalradiaForge.Desktop", "Resources", "Views", "ToolPageTemplates.xaml");
        string csprojPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "CalradiaForge.Desktop", "CalradiaForge.Desktop.csproj");
        string xaml = File.ReadAllText(xamlPath);
        string csproj = File.ReadAllText(csprojPath);

        // Rule C: exactly 9 DashboardTemplates
        int templateCount = System.Text.RegularExpressions.Regex.Matches(
            xaml, @"<DataTemplate x:Key=""\w+DashboardTemplate""").Count;
        Check(templateCount == 9, $"Rule C: expected 9 DashboardTemplates, got {templateCount}.");

        // Propuesta 48: zero TwoWay Run.Text bindings
        int twoway = System.Text.RegularExpressions.Regex.Matches(
            xaml, @"<Run[^>]+Text=""\{Binding[^""]*Mode=TwoWay[^""]*\}""").Count;
        Check(twoway == 0, $"Propuesta 48: found {twoway} TwoWay Run.Text bindings — must be 0.");

        // Rev096: all 5 new thematic icons referenced in XAML and registered in csproj
        string[] rev096Assets = {
            "calradia-troop-crest-rev096",
            "calradia-cohort-badge-rev096",
            "calradia-trade-sigil-rev096",
            "calradia-pipe-seal-rev096",
            "calradia-sentinel-eye-rev096",
        };
        foreach (var asset in rev096Assets)
        {
            Check(xaml.Contains(asset), $"XAML must reference {asset} (Rev096 residual icon upgrade).");
            Check(csproj.Contains(asset + ".png"), $"csproj must register {asset}.png as Resource.");
        }

        // TroopTreeVisualizer zone must no longer use aquila-seal-rev087
        int troopZoneStart = xaml.IndexOf("TroopTreeVisualizerDashboardTemplate", StringComparison.Ordinal);
        int troopZoneEnd = xaml.IndexOf("AudioMixerInspectorDashboardTemplate", StringComparison.Ordinal);
        string troopZone = xaml.Substring(troopZoneStart, troopZoneEnd - troopZoneStart);
        Check(!troopZone.Contains("aquila-seal-rev087"),
            "TroopTreeVisualizer zone must not contain aquila-seal-rev087 after Rev096 upgrade.");

        // 28x28 centurion-rev090 must be gone (upgraded to 36x36 pipe-seal)
        Check(!xaml.Contains("calradia-centurion-medallion-rev090.png\" Height=\"28\" Width=\"28\""),
            "28x28 centurion-rev090 icon must be replaced by 36x36 pipe-seal-rev096 in Rev096.");
    }

    static void Rev098MajorVisualAndStudioUpgrades()
    {
        // Rev098: Verify 5 new thematic studio medallions and visual upgrades across untracked/placeholder panels.
        string xamlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "CalradiaForge.Desktop", "Resources", "Views", "ToolPageTemplates.xaml");
        string hookXamlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "CalradiaForge.Desktop", "Presentation", "HookWorkbenchControl.xaml");
        string csprojPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "CalradiaForge.Desktop", "CalradiaForge.Desktop.csproj");

        string xaml = File.ReadAllText(xamlPath);
        string hookXaml = File.ReadAllText(hookXamlPath);
        string csproj = File.ReadAllText(csprojPath);

        // 1. All 5 new Rev098 assets must be registered in csproj
        string[] rev098Assets = {
            "calradia-anvil-weave-sigil-rev098.png",
            "calradia-gauntlet-sigil-rev098.png",
            "calradia-campaign-astrolabe-rev098.png",
            "calradia-delivery-seal-rev098.png",
            "calradia-diagnostics-aegis-rev098.png",
        };
        foreach (var asset in rev098Assets)
        {
            Check(csproj.Contains(asset), $"csproj must register new Rev098 asset '{asset}'.");
        }

        // 2. HookWorkbenchControl header must reference anvil-weave-sigil-rev098
        Check(hookXaml.Contains("calradia-anvil-weave-sigil-rev098.png"),
            "HookWorkbenchControl.xaml must reference calradia-anvil-weave-sigil-rev098.png in header.");

        // 3. ToolPageTemplates must reference the 4 upgraded studio medallions
        Check(xaml.Contains("calradia-gauntlet-sigil-rev098.png"), "ToolPageTemplates must reference gauntlet-sigil-rev098.");
        Check(xaml.Contains("calradia-campaign-astrolabe-rev098.png"), "ToolPageTemplates must reference campaign-astrolabe-rev098.");
        Check(xaml.Contains("calradia-delivery-seal-rev098.png"), "ToolPageTemplates must reference delivery-seal-rev098.");
        Check(xaml.Contains("calradia-diagnostics-aegis-rev098.png"), "ToolPageTemplates must reference diagnostics-aegis-rev098.");

        // 4. Old bare 28x28 laurel-crest placeholders must be completely eliminated from ToolPageTemplates
        Check(!xaml.Contains("calradia-laurel-crest-rev089.png\" Height=\"28\" Width=\"28\""),
            "ToolPageTemplates must not retain unboxed 28x28 laurel-crest placeholders after Rev098 studio upgrades.");

        // 5. Invariants: Exactly 9 DashboardTemplates & zero TwoWay Run.Text bindings
        int templateCount = System.Text.RegularExpressions.Regex.Matches(
            xaml, @"<DataTemplate x:Key=""\w+DashboardTemplate""").Count;
        Check(templateCount == 9, $"Rule C invariant: expected exactly 9 DashboardTemplates, got {templateCount}.");

        int twoway = System.Text.RegularExpressions.Regex.Matches(
            xaml, @"<Run[^>]+Text=""\{Binding[^""]*Mode=TwoWay[^""]*\}""").Count;
        Check(twoway == 0, $"Propuesta 48: found {twoway} TwoWay Run.Text bindings — must be 0.");
    }
static void Rev099TacticalViewsAndUnretouchedFunctionsPolish()
    {
        string basePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "CalradiaForge.Desktop");
        string paletteXaml = File.ReadAllText(Path.Combine(basePath, "Presentation", "CommandPaletteControl.xaml"));
        string statusXaml = File.ReadAllText(Path.Combine(basePath, "Presentation", "WorkbenchStatusControl.xaml"));
        string dossierXaml = File.ReadAllText(Path.Combine(basePath, "Presentation", "ToolDossierControl.xaml"));
        string workspaceXaml = File.ReadAllText(Path.Combine(basePath, "Presentation", "WorkbenchWorkspaceControl.xaml"));
        string footerXaml = File.ReadAllText(Path.Combine(basePath, "Presentation", "WorkbenchFooterControl.xaml"));
        string navXaml = File.ReadAllText(Path.Combine(basePath, "Presentation", "WorkbenchNavigationControl.xaml"));
        string texturesDir = Path.Combine(basePath, "Resources", "Textures");

        // 1. CommandPaletteControl must feature the 36x36 framed medallion header and category left accent
        Check(paletteXaml.Contains("calradia-astrolabe-dial-rev086.png"), "CommandPaletteControl must reference astrolabe dial.");
        Check(paletteXaml.Contains("CategoryAccentBrushKey"), "CommandPaletteControl list items must feature category accent stripe.");

        // 2. WorkbenchStatusControl must have framed circular icon badges and 3px tactile status bars
        Check(statusXaml.Contains("CornerRadius=\"10\""), "WorkbenchStatusControl must use rounded circular badges for status icons.");
        Check(statusXaml.Contains("Height=\"3\""), "WorkbenchStatusControl cards must use 3px tactile status indicators.");

        // 3. ToolDossierControl must feature the 42x42 framed imperial wax seal and terminal syntax blocks
        Check(dossierXaml.Contains("imperial-wax-seal-rev085.png"), "ToolDossierControl must reference imperial-wax-seal.");
        Check(dossierXaml.Contains("DossierStatelessCertificationBadge"), "ToolDossierControl must retain DossierStatelessCertificationBadge.");

        // 4. WorkbenchWorkspaceControl Split Deck must feature tactical-dial-plate-rev084.png framed medallion
        Check(workspaceXaml.Contains("tactical-dial-plate-rev084.png"), "WorkbenchWorkspaceControl Split Deck must reference tactical-dial-plate-rev084.png.");

        // 5. WorkbenchFooterControl must feature framed dial badge and .NET 8.0 WPF pill
        Check(footerXaml.Contains("calradia-astrolabe-dial-rev086.png"), "WorkbenchFooterControl must reference astrolabe dial badge.");
        Check(footerXaml.Contains(".NET 8.0 WPF"), "WorkbenchFooterControl must feature .NET 8.0 WPF pill.");

        // 6. WorkbenchNavigationControl must feature framed gear hammer badge
        Check(navXaml.Contains("GameIcon.gear_hammer"), "WorkbenchNavigationControl must retain gear_hammer icon.");

        // 7. Texture file sizes must prove high-resolution upgrade (>350 KB)
        string[] upgradedTextures = {
            "calradia-mind-medallion-rev093.png",
            "imperial-wax-seal-rev085.png",
            "calradia-astrolabe-dial-rev086.png",
            "calradia-aquila-seal-rev087.png",
        };
        foreach (var tex in upgradedTextures)
        {
            string fullPath = Path.Combine(texturesDir, tex);
            Check(File.Exists(fullPath), $"Texture '{tex}' must exist.");
            long bytes = new FileInfo(fullPath).Length;
            Check(bytes > 350_000, $"Texture '{tex}' must be upgraded to high-resolution (>350 KB), found {bytes} bytes.");
        }
    }

    static void Rev100HookWorkbenchAndPresentationViewsVisualOverhaul()
    {
        string basePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "CalradiaForge.Desktop");
        string hookXaml = File.ReadAllText(Path.Combine(basePath, "Presentation", "HookWorkbenchControl.xaml"));
        string headerXaml = File.ReadAllText(Path.Combine(basePath, "Presentation", "WorkbenchHeaderControl.xaml"));
        string mainXaml = File.ReadAllText(Path.Combine(basePath, "MainWindow.xaml"));
        string shellXaml = File.ReadAllText(Path.Combine(basePath, "Presentation", "WorkbenchShellView.xaml"));
        string templatesXaml = File.ReadAllText(Path.Combine(basePath, "Resources", "Views", "ToolPageTemplates.xaml"));
        string csproj = File.ReadAllText(Path.Combine(basePath, "CalradiaForge.Desktop.csproj"));
        string texturesDir = Path.Combine(basePath, "Resources", "Textures");

        // 1. New Rev100 master textures registered in csproj
        Check(csproj.Contains("calradia-bytecode-matrix-rev100.png"), "csproj must register calradia-bytecode-matrix-rev100.png.");
        Check(csproj.Contains("calradia-tactical-emblem-rev100.png"), "csproj must register calradia-tactical-emblem-rev100.png.");

        // 2. HookWorkbenchControl must feature dual medallions and live interceptor badges
        Check(hookXaml.Contains("calradia-anvil-weave-sigil-rev098.png"), "HookWorkbenchControl must retain anvil-weave-sigil-rev098.");
        Check(hookXaml.Contains("calradia-bytecode-matrix-rev100.png"), "HookWorkbenchControl must reference new calradia-bytecode-matrix-rev100.png.");
        Check(hookXaml.Contains("IL BYTECODE WEAVER · LIVE INTERCEPTOR"), "HookWorkbenchControl must feature live interceptor badge.");
        Check(hookXaml.Contains("ACTIVE CONTRACT"), "HookWorkbenchControl must feature active contract eligibility badge.");
        Check(hookXaml.Contains("CALRADIA BYTECODE AUDIT CONSOLE · REALTIME LOG"), "HookWorkbenchControl must feature realtime bytecode console header.");

        // 3. WorkbenchHeaderControl must feature refined brand group and tactical chip
        Check(headerXaml.Contains("CornerRadius=\"5\""), "WorkbenchHeaderControl must use refined border corner radius.");
        Check(headerXaml.Contains("TACTICAL"), "WorkbenchHeaderControl must retain TACTICAL badge.");
        Check(headerXaml.Contains("Width=\"126\""), "HeaderBrandGroup must allocate 126px width to prevent label clipping.");
        Check(headerXaml.Contains("MaxWidth=\"64\""), "TACTICAL badge must support up to 64px width.");
        Check(mainXaml.Contains("Fill=\"{DynamicResource VerdigrisBrush}\""), "MainWindow title bar badge must feature Verdigris activity LED indicator.");
        Check(templatesXaml.Contains("calradia-cohort-badge-rev096.png"), "CombatStudioViewTemplate must feature cohort badge.");

        // 4. MainWindow and WorkbenchShellView must feature version badge and architectural dividing rule
        Check(mainXaml.Contains("v25.2.0 · BANNERLORD PRO"), "MainWindow titlebar must feature v25.2.0 Bannerlord Pro badge.");
        Check(shellXaml.Contains("Grid.Column=\"1\""), "WorkbenchShellView must retain column 1 separator.");

        // 5. ToolPageTemplates must feature tactical sovereign emblem and preserve invariants
        Check(templatesXaml.Contains("calradia-tactical-emblem-rev100.png"), "ToolPageTemplates must reference calradia-tactical-emblem-rev100.png.");
        int templateCount = System.Text.RegularExpressions.Regex.Matches(
            templatesXaml, @"<DataTemplate x:Key=""\w+DashboardTemplate""").Count;
        Check(templateCount == 9, $"Rule C invariant: expected exactly 9 DashboardTemplates, got {templateCount}.");

        int twoway = System.Text.RegularExpressions.Regex.Matches(
            templatesXaml, @"<Run[^>]+Text=""\{Binding[^""]*Mode=TwoWay[^""]*\}""").Count;
        Check(twoway == 0, $"Propuesta 48: found {twoway} TwoWay Run.Text bindings — must be 0.");

        // 6. Texture file sizes must prove high-resolution master grade (>350 KB)
        string[] rev100Textures = {
            "calradia-bytecode-matrix-rev100.png",
            "calradia-tactical-emblem-rev100.png",
            "tool-card-top-corners-v1.png",
        };
        foreach (var tex in rev100Textures)
        {
            string fullPath = Path.Combine(texturesDir, tex);
            Check(File.Exists(fullPath), $"Texture '{tex}' must exist.");
            long bytes = new FileInfo(fullPath).Length;
            Check(bytes > 350_000, $"Texture '{tex}' must be upgraded to high-resolution (>350 KB), found {bytes} bytes.");
        }
    }

    static void Rev101TacticalStudiosFullCompositionOverhaul()
    {
        string basePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CalradiaForge.Desktop");
        string templatesXaml = File.ReadAllText(Path.Combine(basePath, "Resources", "Views", "ToolPageTemplates.xaml"));
        string csproj = File.ReadAllText(Path.Combine(basePath, "CalradiaForge.Desktop.csproj"));

        // 1. Rule C Invariant: exactly 9 DashboardTemplates
        int templateCount = System.Text.RegularExpressions.Regex.Matches(
            templatesXaml, @"<DataTemplate x:Key=""\w+DashboardTemplate""").Count;
        Check(templateCount == 9, $"Rule C invariant: expected exactly 9 DashboardTemplates, got {templateCount}.");

        // 2. Propuesta 48 Invariant: zero TwoWay Run.Text bindings
        int twoway = System.Text.RegularExpressions.Regex.Matches(
            templatesXaml, @"<Run[^>]+Text=""\{Binding[^""]*Mode=TwoWay[^""]*\}""").Count;
        Check(twoway == 0, $"Propuesta 48: found {twoway} TwoWay Run.Text bindings — must be 0.");

        // 3. Structural assertions for all 12 Tactical Studios
        var studioMedallionPairs = new (string TemplateKey, string PrimaryMedallion, string SecondaryMedallion)[]
        {
            ("WorkshopSimulatorDashboardTemplate", "calradia-guild-medallion-rev092.png", "calradia-tactical-emblem-rev100.png"),
            ("KingdomDiplomacyStudioDashboardTemplate", "calradia-diplomacy-medallion-rev095.png", "calradia-aquila-seal-rev087.png"),
            ("CaravanTradeViewTemplate", "calradia-trade-sigil-rev096.png", "calradia-astrolabe-dial-rev086.png"),
            ("GauntletStudioViewTemplate", "calradia-gauntlet-sigil-rev098.png", "calradia-anvil-weave-sigil-rev098.png"),
            ("CampaignStudioViewTemplate", "calradia-campaign-astrolabe-rev098.png", "calradia-astrolabe-dial-rev086.png"),
            ("DeliveryStudioViewTemplate", "calradia-delivery-seal-rev098.png", "imperial-wax-seal-rev085.png"),
            ("DiagnosticsStudioViewTemplate", "calradia-diagnostics-aegis-rev098.png", "calradia-sentinel-eye-rev096.png"),
            ("LiveSessionViewTemplate", "calradia-pipe-seal-rev096.png", "calradia-bytecode-matrix-rev100.png"),
            ("AgentMemoryInspectorDashboardTemplate", "calradia-mind-medallion-rev093.png", "calradia-tactical-emblem-rev100.png"),
            ("CodeSecurityAuditorDashboardTemplate", "calradia-cipher-seal-rev095.png", "calradia-bytecode-matrix-rev100.png"),
            ("ModuleHierarchyValidatorDashboardTemplate", "calradia-hierarchy-seal-rev095.png", "calradia-aquila-seal-rev087.png"),
            ("ComponentGeneratorStudioDashboardTemplate", "calradia-mechanism-medallion-rev095.png", "calradia-anvil-weave-sigil-rev098.png")
        };

        foreach (var (templateKey, primary, secondary) in studioMedallionPairs)
        {
            int start = templatesXaml.IndexOf(templateKey, StringComparison.Ordinal);
            Check(start >= 0, $"Studio template '{templateKey}' must exist in ToolPageTemplates.xaml.");

            int nextTemplate = templatesXaml.IndexOf("<DataTemplate x:Key=", start + templateKey.Length, StringComparison.Ordinal);
            if (nextTemplate < 0)
            {
                nextTemplate = templatesXaml.IndexOf("<DataTemplate DataType=", start + templateKey.Length, StringComparison.Ordinal);
            }
            int end = nextTemplate >= 0 ? nextTemplate : templatesXaml.Length;
            string templateSlice = templatesXaml.Substring(start, end - start);

            Check(templateSlice.Contains(primary), $"Studio '{templateKey}' must feature primary medallion '{primary}'.");
            Check(templateSlice.Contains(secondary), $"Studio '{templateKey}' must feature secondary medallion '{secondary}'.");
            Check(templateSlice.Contains("1,1,1,3") || templateSlice.Contains("0,0,0,3") || templateSlice.Contains("BorderThickness=\"3\""),
                $"Studio '{templateKey}' must feature tactical KPI cards with 3px accent borders.");
            Check(templateSlice.Contains("ProgressBar") && templateSlice.Contains("Height=\"4\""),
                $"Studio '{templateKey}' must feature proportional 4px mini-progress bars.");
            Check(templateSlice.Contains("Consolas") && templateSlice.Contains("$ "),
                $"Studio '{templateKey}' must feature dossier console blocks with Consolas and prompt '$ '.");
        }

        // 4. Verify all 19 medallion assets are registered in csproj
        string[] allMedallionAssets = {
            "calradia-guild-medallion-rev092.png", "calradia-tactical-emblem-rev100.png",
            "calradia-diplomacy-medallion-rev095.png", "calradia-aquila-seal-rev087.png",
            "calradia-trade-sigil-rev096.png", "calradia-astrolabe-dial-rev086.png",
            "calradia-gauntlet-sigil-rev098.png", "calradia-anvil-weave-sigil-rev098.png",
            "calradia-campaign-astrolabe-rev098.png", "calradia-delivery-seal-rev098.png",
            "imperial-wax-seal-rev085.png", "calradia-diagnostics-aegis-rev098.png",
            "calradia-sentinel-eye-rev096.png", "calradia-pipe-seal-rev096.png",
            "calradia-bytecode-matrix-rev100.png", "calradia-mind-medallion-rev093.png",
            "calradia-cipher-seal-rev095.png", "calradia-hierarchy-seal-rev095.png",
            "calradia-mechanism-medallion-rev095.png"
        };
        foreach (var asset in allMedallionAssets)
        {
            Check(csproj.Contains(asset), $"CalradiaForge.Desktop.csproj must register Resource entry for '{asset}'.");
        }
    }
}
