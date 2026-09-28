using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalradiaForge.Desktop.Presentation;

namespace CalradiaForge.Desktop.Services
{
    /// <summary>Owns desktop tool work. It returns evidence DTOs and never accesses WPF controls.</summary>
    internal sealed class DesktopWorkspaceService : IDisposable
    {
        readonly DesktopAnalysisService analysis = new();
        readonly DesktopAssemblyService assemblies = new();
        readonly DesktopSimulationService simulations = new();
        readonly DesktopMetricsService metrics;
        readonly DesktopSessionService session;

        public DesktopWorkspaceService(DesktopMetricsService metrics, DesktopSessionService session = null)
        {
            this.metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            this.session = session ?? new DesktopSessionService(metrics);
        }

        public bool IsConnected => session.IsConnected;
        public DesktopSessionService Session => session;
        public IReadOnlyCollection<string> Capabilities => session.Capabilities;
        public string ConnectionError => session.LastError;
        public Task<bool> ConnectAsync(CancellationToken cancellation) => session.ConnectAsync(cancellation);
        public Task<bool> ReconnectAsync(CancellationToken cancellation) => session.ReconnectAsync(cancellation);

        public async Task<WorkspaceExecutionResult> ExecuteAsync(ToolDefinition tool, string input, CancellationToken cancellation)
        {
            if (tool == null) throw new ArgumentNullException(nameof(tool));
            cancellation.ThrowIfCancellationRequested();
            return await metrics.MeasureAsync("tool:" + tool.Id, async () =>
            {
                if (tool.Id == "AssemblyInspector") return await InspectAssemblyAsync(tool, input, cancellation).ConfigureAwait(true);
                if (tool.Kind == DesktopToolKind.AssemblyEditor) return await PatchAssemblyAsync(tool, input, cancellation).ConfigureAwait(true);
                if ((tool.Id == "SaveTypeDefinerAuditor" || tool.Id == "CampaignNamespaceGuard") && IsAssemblyFile(input))
                    return await AuditAssemblyAsync(tool, input, cancellation).ConfigureAwait(true);
                if (tool.Id == "TroopTreeVisualizer" || tool.Id == "ItemBalanceAnalyzer")
                    return await RunSimulationAsync(tool, () => simulations.SimulateTroopTree(input, cancellation), cancellation).ConfigureAwait(true);
                if (tool.Id == "AudioFmodMixerInspector" || tool.Id == "SoundXmlSynthesizer")
                    return await RunSimulationAsync(tool, () => simulations.AuditAudioWaveform(input, cancellation), cancellation).ConfigureAwait(true);
                if (tool.Id == "WorkshopEnterpriseSimulator" || tool.Id == "SettlementCalculator" || tool.Id == "UnderworldCrimeSimulator" || tool.Id == "DynasticSuccessionEvaluator")
                    return await RunSimulationAsync(tool, () => simulations.SimulateEconomyAndCampaign(input, cancellation), cancellation).ConfigureAwait(true);
                return tool.Kind switch
                {
                    DesktopToolKind.Analyzer => await AnalyzeAsync(tool, input, cancellation).ConfigureAwait(true),
                    DesktopToolKind.Generator => Generate(tool),
                    DesktopToolKind.Live => await QueryLiveAsync(tool, input, cancellation).ConfigureAwait(true),
                    DesktopToolKind.Report => Report(),
                    DesktopToolKind.Simulation => NotRun(tool, "Provide an explicit scenario through its dedicated simulator before a calculation can be performed."),
                    _ => NotRun(tool, "This reference has no executable desktop operation.")
                };
            }, "Short desktop operation only; it does not measure Bannerlord or another mod.").ConfigureAwait(true);
        }

        static async Task<WorkspaceExecutionResult> RunSimulationAsync(ToolDefinition tool, Func<(string Report, IReadOnlyList<WorkspaceEvidence> Evidence)> simFunc, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var (report, evidence) = await Task.Run(simFunc, cancellation).ConfigureAwait(true);
            cancellation.ThrowIfCancellationRequested();
            return new WorkspaceExecutionResult
            {
                Status = "Simulated",
                RawResult = report,
                EvidenceCount = evidence.Count,
                Evidence = evidence.Count == 0 ? [ new WorkspaceEvidence(tool.Title, "Simulated", "Execution completed.") ] : evidence.ToArray()
            };
        }

        static bool IsAssemblyFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            var ext = Path.GetExtension(path.Trim().Trim('"'));
            return string.Equals(ext, ".dll", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase);
        }

        async Task<WorkspaceExecutionResult> AuditAssemblyAsync(ToolDefinition tool, string input, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(input)) return NotRun(tool, "Select a managed .NET PE assembly file (.dll or .exe).");
            var (report, findings) = await Task.Run(() => assemblies.Audit(input, cancellation), cancellation).ConfigureAwait(true);
            cancellation.ThrowIfCancellationRequested();
            var evidence = findings.Take(128).Select(item => new WorkspaceEvidence("Assembly Audit / " + item.RuleId, item.Severity, (item.Target ?? string.Empty) + " " + (item.Message ?? string.Empty))).ToArray();
            var hasViolations = findings.Any(item => string.Equals(item.Severity, "Violation", StringComparison.OrdinalIgnoreCase));
            return new WorkspaceExecutionResult
            {
                Status = hasViolations ? "Violations Found" : "Verified",
                RawResult = report,
                EvidenceCount = findings.Count,
                Evidence = evidence.Length == 0 ? [ new WorkspaceEvidence("AsmResolver / Assembly Audit", "Verified", "All architectural rules and safety constraints verified cleanly.") ] : evidence
            };
        }

        async Task<WorkspaceExecutionResult> InspectAssemblyAsync(ToolDefinition tool, string input, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(input)) return NotRun(tool, "Select a managed .NET PE assembly file.");
            var text = await Task.Run(() => assemblies.Inspect(input, cancellation), cancellation).ConfigureAwait(true);
            cancellation.ThrowIfCancellationRequested();
            return new WorkspaceExecutionResult { Status = "Verified", RawResult = text, EvidenceCount = 1,
                Evidence = [ new WorkspaceEvidence("AsmResolver / metadata", "Verified", "Parsed statically; no assembly load or execution.") ] };
        }

        async Task<WorkspaceExecutionResult> PatchAssemblyAsync(ToolDefinition tool, string input, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(input)) return NotRun(tool, "Select a JSON patch request to preview and apply the copy-only metadata operation.");
            // The service checks cancellation before the atomic output move. Once that
            // commit succeeds, retain the completed result even if cancellation races
            // with the UI continuation; reporting "cancelled" would hide a real copy.
            var text = await Task.Run(() => assemblies.PreviewAndWriteVersionPatch(input, cancellation), cancellation).ConfigureAwait(true);
            return new WorkspaceExecutionResult { Status = "Completed", RawResult = text, EvidenceCount = 1,
                Evidence = [ new WorkspaceEvidence("AsmResolver / copy-only patch", "Verified", "Output PE was reopened; source hash and backup were recorded.") ] };
        }

        async Task<WorkspaceExecutionResult> AnalyzeAsync(ToolDefinition tool, string input, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(input)) return NotRun(tool, "Select a supported local file or folder.");
            // Keep both analysis and its potentially large formatting/projection work off
            // the WPF dispatcher. Analysis is bounded, but a folder can still produce up
            // to 1,000 file results and the formatted ledger may be several thousand lines.
            return await Task.Run(() =>
            {
                cancellation.ThrowIfCancellationRequested();
                var result = analysis.Run(tool.Id, input, cancellation);
                cancellation.ThrowIfCancellationRequested();
                var evidence = result.Findings.Take(128)
                    .Select(item => new WorkspaceEvidence(
                        result.AnalyzerName,
                        item.Severity,
                        item.Evidence,
                        item.RuleId,
                        item.SourcePath,
                        item.Line,
                        item.Column,
                        item.Recommendation)).ToArray();
                return new WorkspaceExecutionResult
                {
                    Status = result.State.ToString(),
                    RawResult = analysis.Format(result),
                    EvidenceCount = result.Findings.Count,
                    Evidence = evidence.Length == 0 ? [ new WorkspaceEvidence(result.AnalyzerName, result.State.ToString(), result.Summary) ] : evidence
                };
            }, cancellation).ConfigureAwait(true);
        }

        WorkspaceExecutionResult Generate(ToolDefinition tool)
        {
            var extension = tool.Id.IndexOf("Xml", StringComparison.OrdinalIgnoreCase) >= 0 || tool.Id.IndexOf("Submodule", StringComparison.OrdinalIgnoreCase) >= 0 ? "xml" : "cs";
            var template = extension == "xml"
                ? "<!-- Editable Calradia Forge starting point for " + tool.Id + ". Validate this XML before use. -->" + Environment.NewLine + "<!-- Add only supported Bannerlord elements here. -->"
                : "// Editable Calradia Forge starting point for " + tool.Id + "." + Environment.NewLine + "// Review API availability and test in an isolated working copy." + Environment.NewLine + "namespace YourMod { internal static class " + Sanitize(tool.Id) + " { } }";
            return new WorkspaceExecutionResult
            {
                Status = "Template",
                RawResult = DesktopTemplateLocalizer.Prefix(tool.Id, template),
                EvidenceCount = 1,
                Evidence = [ new WorkspaceEvidence("Generator", "Template", "Editable " + extension + " starting point; it has not been compiled or installed.") ]
            };
        }

        async Task<WorkspaceExecutionResult> QueryLiveAsync(ToolDefinition tool, string input, CancellationToken cancellation)
        {
            if (tool.Id == "SaveInspector" || tool.Id == "ObjectInspector" || tool.Id == "SaveBinaryParser")
            {
                if (!session.IsConnected || string.Equals(input?.Trim(), "agent_memory", StringComparison.OrdinalIgnoreCase) || input?.IndexOf("memory", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return await RunSimulationAsync(tool, () => simulations.InspectAgentMemory(input, cancellation), cancellation).ConfigureAwait(true);
                }
            }

            if (!session.IsConnected) return NotRun(tool, "Connect to an active Forge session before requesting live evidence.");
            if (string.IsNullOrWhiteSpace(tool.PipeAction) || !session.Supports(tool.PipeAction))
                return new WorkspaceExecutionResult { Status = "Unsupported", RawResult = "The active session did not advertise the " + (tool.PipeAction ?? "requested") + " capability.", Evidence = [ new WorkspaceEvidence("IPC", "Unsupported", "No request was sent.") ] };
            cancellation.ThrowIfCancellationRequested();

            var normalizedArgument = input ?? string.Empty;
            if (tool.PipeAction == "command" && !normalizedArgument.Contains('|') && !string.IsNullOrWhiteSpace(normalizedArgument))
            {
                var trimmed = normalizedArgument.Trim();
                var firstSpace = trimmed.IndexOf(' ');
                var cmd = firstSpace > 0 ? trimmed.Substring(0, firstSpace) : trimmed;
                var arg = firstSpace > 0 ? trimmed.Substring(firstSpace + 1).Trim() : string.Empty;

                if (cmd.Equals("publish", StringComparison.OrdinalIgnoreCase)) cmd = "cf.forgeweave.publish";
                else if (cmd.Equals("unquarantine", StringComparison.OrdinalIgnoreCase)) cmd = "cf.forgeweave.unquarantine";
                else if (cmd.Equals("replay", StringComparison.OrdinalIgnoreCase)) cmd = "cf.forgeweave.replay";
                else if (cmd.Equals("handlers", StringComparison.OrdinalIgnoreCase)) cmd = "cf.forgeweave.handlers";
                else if (cmd.Equals("handler", StringComparison.OrdinalIgnoreCase)) cmd = "cf.forgeweave.handler";
                else if (cmd.Equals("status", StringComparison.OrdinalIgnoreCase)) cmd = "cf.forgeweave.status";
                else if (cmd.Equals("journal", StringComparison.OrdinalIgnoreCase)) cmd = "cf.forgeweave.journal";
                else if (cmd.Equals("clear", StringComparison.OrdinalIgnoreCase)) cmd = "cf.forgeweave.clear";

                normalizedArgument = cmd + "|" + arg;
            }

            var response = await session.SendAsync(new CalradiaForge.Core.Request { Action = tool.PipeAction, Argument = normalizedArgument }, cancellation).ConfigureAwait(true);

            if (response.Success && tool.PipeAction == "framework")
            {
                return FormatForgeWeaveLiveReport(response.Data ?? string.Empty);
            }

            return new WorkspaceExecutionResult
            {
                Status = response.Success ? "Completed" : "Rejected",
                RawResult = response.Success ? (response.Data ?? "The session returned no payload.") : (response.Error ?? "The session rejected the request."),
                EvidenceCount = 1,
                Evidence = [ new WorkspaceEvidence("Forge session", response.Success ? "Verified response" : "Rejected", "Action: " + tool.PipeAction) ]
            };
        }

        WorkspaceExecutionResult FormatForgeWeaveLiveReport(string jsonData)
        {
            CalradiaForge.Core.ForgeWeaveSnapshot snapshot = null;
            Exception primaryParseError = null;
            Exception fallbackParseError = null;
            try
            {
                snapshot = CalradiaForge.Core.Json.Deserialize<CalradiaForge.Core.ForgeWeaveSnapshot>(jsonData);
            }
            catch (Exception error)
            {
                primaryParseError = error;
                try
                {
                    snapshot = System.Text.Json.JsonSerializer.Deserialize<CalradiaForge.Core.ForgeWeaveSnapshot>(
                        jsonData,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception fallbackError) { fallbackParseError = fallbackError; }
            }

            var parseFailure = GetForgeWeaveSnapshotFailure(snapshot, primaryParseError, fallbackParseError);
            if (parseFailure != null)
            {
                return new WorkspaceExecutionResult
                {
                    Status = "Unparsed",
                    RawResult = jsonData,
                    EvidenceCount = 2,
                    Evidence =
                    [
                        new WorkspaceEvidence("ForgeWeave transport", "Received", "The framework request succeeded; the original payload is retained below."),
                        new WorkspaceEvidence("ForgeWeave snapshot", "Unparsed", parseFailure)
                    ]
                };
            }

            int handlerCount = snapshot.Handlers?.Count ?? 0;
            var lines = new List<string>(24 + handlerCount + 32)
            {
                "================================================================================",
                "CALRADIA FORGE · FORGEWEAVE APM TELEMETRY & EVENT MESH DASHBOARD",
                "================================================================================",
                $"Mesh Status:          {snapshot.Status ?? "Operational"}",
                $"Captured At:          {snapshot.CapturedAt ?? "Live"}",
                $"Handlers:             Total: {snapshot.HandlerCount}  |  Ready: {snapshot.ReadyHandlerCount}  |  Blocked: {snapshot.BlockedHandlerCount}  |  Quarantined: {snapshot.QuarantinedHandlerCount}",
                $"Dispatches:           Total: {snapshot.DispatchCount}  |  Invocations: {snapshot.InvocationCount}  |  Failures: {snapshot.FailureCount}  |  Budget Overruns: {snapshot.BudgetExceededCount}",
                $"Latency (ms):         Total: {snapshot.TotalMilliseconds:F2}  |  Mean: {snapshot.MeanMilliseconds:F2}  |  Max: {snapshot.MaxMilliseconds:F2}",
                $"Replay Lab:           Retained: {snapshot.ReplayRecordCount}  |  Attempts: {snapshot.ReplayAttemptCount}  |  Success: {snapshot.ReplaySuccessCount}  |  Rejected: {snapshot.ReplayRejectedCount}",
                string.Empty,
                "--------------------------------------------------------------------------------",
                "EVENT HANDLERS & CIRCUIT BREAKER TELEMETRY",
                "--------------------------------------------------------------------------------",
                string.Format("{0,-30} {1,-10} {2,8} {3,8} {4,8} {5,8} {6,8}  {7}", "Handler / Topic", "Circuit", "Invoc", "Fail", "P50(ms)", "P95(ms)", "P99(ms)", "Latency Histogram"),
                new string('-', 98)
            };

            var evidenceList = new List<WorkspaceEvidence>(handlerCount + 16)
            {
                new("ForgeWeave Mesh", snapshot.QuarantinedHandlerCount > 0 ? "Warning" : "Verified",
                    $"Handlers: {snapshot.HandlerCount} (Quarantined: {snapshot.QuarantinedHandlerCount}) | Dispatches: {snapshot.DispatchCount} | Mean: {snapshot.MeanMilliseconds:F2}ms")
            };

            foreach (var h in snapshot.Handlers ?? [])
            {
                var topic = string.IsNullOrWhiteSpace(h.Topic) ? h.Event.ToString() : h.Topic;
                var circuitState = string.IsNullOrWhiteSpace(h.CircuitState) ? "Closed" : h.CircuitState;
                var circuitBadge = circuitState.ToUpperInvariant();
                var bar = FormatHistogramBar(h.BucketUnder1Ms, h.Bucket1To5Ms, h.Bucket5To20Ms, h.BucketOver20Ms);

                var idDisplay = h.Id ?? "unknown";
                if (idDisplay.Length > 28) idDisplay = idDisplay.Substring(0, 25) + "...";

                lines.Add(string.Format("{0,-30} [{1,-8}] {2,8} {3,8} {4,8:F2} {5,8:F2} {6,8:F2}  {7}",
                    idDisplay, circuitBadge, h.InvocationCount, h.FailureCount, h.P50Milliseconds, h.P95Milliseconds, h.P99Milliseconds, bar));

                var sev = circuitState.Equals("Open", StringComparison.OrdinalIgnoreCase) ? "Violation" :
                          circuitState.Equals("HalfOpen", StringComparison.OrdinalIgnoreCase) ? "Warning" :
                          h.FailureCount > 0 ? "Warning" : "Verified";

                evidenceList.Add(new WorkspaceEvidence(
                    "Handler: " + (h.Id ?? "unnamed"),
                    sev,
                    $"[{circuitBadge}] Topic: {topic} | Inv:{h.InvocationCount} Fail:{h.FailureCount} P50:{h.P50Milliseconds:F2}ms P95:{h.P95Milliseconds:F2}ms P99:{h.P99Milliseconds:F2}ms | Hist: {bar}"));
            }

            if ((snapshot.RecentDispatches ?? []).Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add("--------------------------------------------------------------------------------");
                lines.Add("RECENT EVENT DISPATCHES");
                lines.Add("--------------------------------------------------------------------------------");
                foreach (var d in snapshot.RecentDispatches.Take(16))
                {
                    lines.Add($"#{d.Sequence:D4} [{d.DispatchedAt}] {d.Event} ({d.Milliseconds:F2} ms) -> {d.InvokedCount} invoked, {d.FailureCount} failed, {d.SkippedCount} skipped [{d.Status}]");
                }
            }

            if ((snapshot.Findings ?? []).Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add("--------------------------------------------------------------------------------");
                lines.Add("FRAMEWORK FINDINGS & ANOMALIES");
                lines.Add("--------------------------------------------------------------------------------");
                foreach (var f in snapshot.Findings)
                {
                    lines.Add($"[{f.Level}] {f.Code}: {f.Message} ({f.Suggestion})");
                    evidenceList.Add(new WorkspaceEvidence("Finding / " + f.Code, f.Level, f.Message,
                        f.Code, recommendation: f.Suggestion));
                }
            }

            var evidence = evidenceList.Take(128).ToArray();
            return new WorkspaceExecutionResult
            {
                Status = snapshot.QuarantinedHandlerCount > 0 ? "Degraded" : "Verified",
                RawResult = string.Join(Environment.NewLine, lines),
                EvidenceCount = evidence.Length,
                Evidence = evidence
            };
        }

        static string GetForgeWeaveSnapshotFailure(CalradiaForge.Core.ForgeWeaveSnapshot snapshot,
            Exception primaryParseError, Exception fallbackParseError)
        {
            const int maximumDiagnosticLength = 240;
            string reason;
            if (snapshot == null)
            {
                reason = primaryParseError != null || fallbackParseError != null
                    ? "The received payload is not a valid ForgeWeave snapshot. " +
                      DescribeParseError(primaryParseError) + " " + DescribeParseError(fallbackParseError)
                    : "The received JSON payload did not contain a snapshot object.";
            }
            else if (string.IsNullOrWhiteSpace(snapshot.Status) || string.IsNullOrWhiteSpace(snapshot.CapturedAt))
            {
                var missing = string.IsNullOrWhiteSpace(snapshot.Status) && string.IsNullOrWhiteSpace(snapshot.CapturedAt)
                    ? "Status and CapturedAt"
                    : string.IsNullOrWhiteSpace(snapshot.Status) ? "Status" : "CapturedAt";
                reason = "The JSON object is not a recognizable ForgeWeave snapshot; required field(s) missing: " + missing + ".";
            }
            else
            {
                return null;
            }

            reason = reason.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return reason.Length <= maximumDiagnosticLength ? reason : reason.Substring(0, maximumDiagnosticLength - 1) + "…";
        }

        static string DescribeParseError(Exception error)
        {
            if (error == null) return string.Empty;
            var message = (error.Message ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (message.Length > 72) message = message.Substring(0, 71) + "…";
            return error.GetType().Name + (message.Length == 0 ? string.Empty : ": " + message);
        }

        static string FormatHistogramBar(int b1, int b2, int b3, int b4)
        {
            var total = b1 + b2 + b3 + b4;
            if (total == 0) return "[....] (no data)";
            return $"[<1ms:{b1} | 1-5ms:{b2} | 5-20ms:{b3} | >20ms:{b4}]";
        }

        WorkspaceExecutionResult Report() => new()
        {
            Status = "Completed",
            RawResult = metrics.FormatRecent(),
            EvidenceCount = metrics.Recent.Count,
            Evidence = [ new WorkspaceEvidence("Desktop metrics", "Measured", "Only retained short desktop observations are shown.") ]
        };

        static WorkspaceExecutionResult NotRun(ToolDefinition tool, string reason) => new()
        {
            Status = "Not run",
            RawResult = "Not run — " + reason,
            DisabledReason = tool.ChangesState ? "State-changing work requires the in-game guarded test workflow." : null,
            Evidence = [ new WorkspaceEvidence("Desktop", "Not run", reason) ]
        };

        static string Sanitize(string value)
        {
            var chars = (value ?? "GeneratedTool").Where(char.IsLetterOrDigit).ToArray();
            return chars.Length == 0 ? "GeneratedTool" : new string(chars);
        }

        public void Dispose() => session.Dispose();
    }
}
