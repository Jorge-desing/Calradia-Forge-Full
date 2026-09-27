using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalradiaForge.Desktop.Presentation;
using CalradiaForge.Desktop.Services;

internal static class DesktopSimulationServiceTests
{
    public static void RunAll()
    {
        Console.WriteLine("--- Running Desktop Simulation & Split Deck Unit Tests ---");
        TroopTreeCanonical();
        Console.WriteLine("PASS Desktop simulation renders canonical troop tree DAG and stat curves");
        TroopXmlDiff();
        Console.WriteLine("PASS Desktop simulation performs semantic XML diff for troop definitions");
        AgentMemoryAudit();
        Console.WriteLine("PASS Desktop simulation audits CoALA agent cognitive memory tiers and quotas");
        AudioWaveformAudit();
        Console.WriteLine("PASS Desktop simulation inspects acoustic waveforms and frequency spectrum");
        EconomyAndCampaignSimulation();
        Console.WriteLine("PASS Desktop simulation models 30-day workshop economy and rebellion risk");
        WorkspaceSimulationRouting().GetAwaiter().GetResult();
        Console.WriteLine("PASS Desktop workspace routes simulation tools to dedicated engines");
        SplitDeckLifecycle();
        Console.WriteLine("PASS Desktop shell manages Split Deck activation and pinned tool retention");
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

    static void AgentMemoryAudit()
    {
        var service = new DesktopSimulationService();
        var (report, evidence) = service.InspectAgentMemory(string.Empty, CancellationToken.None);

        Check(report.Contains("COALA AGENT COGNITIVE MEMORY AUDIT"), "Report must identify CoALA architecture model.");
        Check(report.Contains("GLOBAL CAPACITY METER"), "Report must include global slot utilization meter.");
        Check(report.Contains("TIER 1: SEMANTIC MEMORY"), "Report must audit Tier 1 Semantic Memory.");
        Check(report.Contains("TIER 2: EPISODIC MEMORY"), "Report must audit Tier 2 Episodic Memory.");
        Check(report.Contains("TIER 3: PROCEDURAL MEMORY"), "Report must audit Tier 3 Procedural Memory.");
        Check(report.Contains("hero_rhagaea"), "Report must include seeded Lord profile data.");

        Check(evidence.Count >= 4, "Agent memory audit must produce comprehensive evidence items.");
        Check(evidence.Any(e => e.Source.Contains("Global Registry") && e.Status == "Verified"), "Global registry slot isolation must be verified.");
        Check(evidence.Any(e => e.Source.Contains("Semantic Decay") && e.Status == "Verified"), "Semantic TTL decay must be verified.");
        Check(evidence.Any(e => e.Source.Contains("Episodic FIFO") && e.Status == "Verified"), "Episodic FIFO caps must be verified.");
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
        Check(saveResult.RawResult.Contains("COALA AGENT COGNITIVE MEMORY AUDIT"), "Agent memory result must contain CoALA memory tiers.");
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
}
