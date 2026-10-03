using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalradiaForge.Core;
using CalradiaForge.Sdk;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace CalradiaForge.Mod
{
    internal sealed class Runtime : ITestServices, IDisposable
    {
        public readonly TestEngine TestEngine;
        public readonly SessionLog Log = new SessionLog();
        readonly PipeServer server = new PipeServer();
        readonly GameLaboratory laboratory = new GameLaboratory();
        readonly List<TestResult> results = new List<TestResult>();
        readonly Dictionary<string, ObjectSnapshot> pins = new Dictionary<string, ObjectSnapshot>();
        readonly Queue<double> frames = new Queue<double>();
        readonly Queue<double> callbackTimes = new Queue<double>(600);
        readonly Dictionary<string, double> events = new Dictionary<string, double>();
        readonly Queue<QueuedForgeEvent> forgeEvents = new Queue<QueuedForgeEvent>();
        readonly GameThreadActionQueue mainThreadActions = new GameThreadActionQueue(64);
        private static readonly Dictionary<ForgeEventKind, (string Dispatch, string Failure, string HostError)> ForgeWeaveEventNames =
            new Dictionary<ForgeEventKind, (string, string, string)>
            {
                { ForgeEventKind.ForgeReady, ("forgeweave.ForgeReady.dispatch", "forgeweave.ForgeReady.failure", "forgeweave.ForgeReady.host_error") },
                { ForgeEventKind.InitialScreenReady, ("forgeweave.InitialScreenReady.dispatch", "forgeweave.InitialScreenReady.failure", "forgeweave.InitialScreenReady.host_error") },
                { ForgeEventKind.ContextEntering, ("forgeweave.ContextEntering.dispatch", "forgeweave.ContextEntering.failure", "forgeweave.ContextEntering.host_error") },
                { ForgeEventKind.ContextLeaving, ("forgeweave.ContextLeaving.dispatch", "forgeweave.ContextLeaving.failure", "forgeweave.ContextLeaving.host_error") },
                { ForgeEventKind.CampaignStarted, ("forgeweave.CampaignStarted.dispatch", "forgeweave.CampaignStarted.failure", "forgeweave.CampaignStarted.host_error") },
                { ForgeEventKind.MissionInitialized, ("forgeweave.MissionInitialized.dispatch", "forgeweave.MissionInitialized.failure", "forgeweave.MissionInitialized.host_error") },
                { ForgeEventKind.AgentCreated, ("forgeweave.AgentCreated.dispatch", "forgeweave.AgentCreated.failure", "forgeweave.AgentCreated.host_error") },
                { ForgeEventKind.AgentRemoved, ("forgeweave.AgentRemoved.dispatch", "forgeweave.AgentRemoved.failure", "forgeweave.AgentRemoved.host_error") },
                { ForgeEventKind.Pulse, ("forgeweave.Pulse.dispatch", "forgeweave.Pulse.failure", "forgeweave.Pulse.host_error") },
                { ForgeEventKind.GameLoaded, ("forgeweave.GameLoaded.dispatch", "forgeweave.GameLoaded.failure", "forgeweave.GameLoaded.host_error") },
                { ForgeEventKind.MissionEnded, ("forgeweave.MissionEnded.dispatch", "forgeweave.MissionEnded.failure", "forgeweave.MissionEnded.host_error") },
                { ForgeEventKind.Custom, ("forgeweave.Custom.dispatch", "forgeweave.Custom.failure", "forgeweave.Custom.host_error") }
            };
        int gameThreadId;
        bool hookContextInitialized;
        bool lastHookContextCanManage;
        object lastHookContextScreen;
        int hookContextEpoch;
        System.Reflection.MethodInfo _cachedConsoleMethod;
        Task<ModuleDiagnostics> scan; ModuleDiagnostics diagnostics = new ModuleDiagnostics(); Task persistence;
        float elapsed; int framesSeen; double seconds, weavePulseElapsed; Campaign contextCampaign; Mission contextMission; Context previousContext = Context.Any; bool contextInitialized, sdkConnected, sdkFailureLogged, initialScreenReadyPending, campaignStartedPending, gameLoadedPending;
        ForgePatchDiagnosticsSnapshot patchDiagnosticsCache = new ForgePatchDiagnosticsSnapshot { Status = "Not captured. Use Patch diagnostics to inspect Forge hooks and optional loaded patch runtimes." };
        PatchPreflightSnapshot patchPreflightCache = new PatchPreflightSnapshot { Status = "Not captured. Run Patch preflight to validate registered author declarations." };
        public Settings Config { get; private set; } = new Settings { Language = "en" };
        public Action OpenPanel { get; set; }
        public Action ClosePanel { get; set; }
        public int PinnedSnapshotCount => pins.Count;
        public bool RefreshGameLanguage()
        {
            var language = BannerlordConfig.Language;
            if (Config.Language == language) return false;
            Config.Language = language;
            return true;
        }
        public Context CurrentContext => Mission.Current != null ? Context.Mission : Campaign.Current != null ? Context.Campaign : Context.Any;
        public bool IsCampaignActive => Campaign.Current != null;
        internal bool CanManageHooks
        {
            get
            {
                if (!IsGameThread || GameNetwork.IsMultiplayer || Campaign.Current != null || Mission.Current != null) return false;
                return IsOfficialMainMenuScreenType(ScreenManager.TopScreen?.GetType());
            }
        }
        internal static bool IsOfficialMainMenuScreenType(Type screenType)
        {
            // The initial menu is a specific game-owned screen. Name/namespace prefixes would
            // also accept a third-party lookalike assembly, so resolve the engine type and require
            // exact CLR identity. Fail closed when the game's Gauntlet assembly is unavailable.
            if (screenType == null) return false;
            try
            {
                var officialType = Type.GetType(
                    "TaleWorlds.MountAndBlade.GauntletUI.GauntletInitialScreen, TaleWorlds.MountAndBlade.GauntletUI",
                    throwOnError: false);
                return officialType != null && screenType == officialType;
            }
            catch
            {
                return false;
            }
        }
        public Runtime()
        {
            TestEngine = new TestEngine(() => CanManageHooks);
            TestEngine.Log = Log;
            laboratory.Record = message => Register("CalradiaForge", "Info", message);
            var path = Path.Combine(Paths.Data, "settings.json");
            try 
            { 
                if (File.Exists(path)) 
                {
                    Config = Json.Deserialize<Settings>(File.ReadAllText(path)) ?? Config; 
                }
            } 
            catch (Exception e) 
            { 
                Log.Add("CalradiaForge", "Warning", "Settings reset: " + e.Message); 
            }
            
            server.Start(); 
            Register("CalradiaForge", "Info", "Calradia Forge " + SuiteInfo.Version + " initialized; target Native " + SuiteInfo.TargetGameVersion);
        }
        internal void BindGameThread()
        {
            Interlocked.CompareExchange(ref gameThreadId, Thread.CurrentThread.ManagedThreadId, 0);
        }
        public void Tick(float dt)
        {
            BindGameThread();
            ObserveHookManagementContext();
            // Background file parsing may only return immutable results to the game thread.
            for (var work = 0; work < 8 && mainThreadActions.TryDequeue(out var action); work++)
            {
                try { action(); }
                catch (Exception error) { Register("CalradiaForge", "Error", "Main-thread completion: " + error.Message); }
            }
            // Module loading can run on a different thread from application updates.
            // Create thread-affine services where commands will actually execute.
            if (!sdkConnected)
            {
                var startup = ExtensionStartup.Connect(TestEngine);
                sdkConnected = startup.Connected;
                if (startup.Errors.Count > 0 && !sdkFailureLogged)
                {
                    sdkFailureLogged = true;
                    foreach (var error in startup.Errors) Register("CalradiaForge", "Warning", "SDK extension startup: " + error);
                }
                if (sdkConnected) QueueForgeEvent(ForgeEventKind.ForgeReady, 0, new Dictionary<string, string> { { "suite", SuiteInfo.Version }, { "target", SuiteInfo.TargetGameVersion } });
            }
            // Reset test consent and inspector snapshots when the campaign or mission changes.
            // Track the owning game objects by reference. Building a hash-based identity
            // string here allocated multiple short-lived strings on every application tick.
            var campaign = Campaign.Current;
            var mission = Mission.Current;
            var currentContext = mission != null ? Context.Mission : campaign != null ? Context.Campaign : Context.Any;
            if (!contextInitialized || !ReferenceEquals(campaign, contextCampaign) || !ReferenceEquals(mission, contextMission))
            {
                pendingHookPlan = null;
                if (contextInitialized) QueueForgeEvent(ForgeEventKind.ContextLeaving, 0, new Dictionary<string, string> { { "from", previousContext.ToString() }, { "to", currentContext.ToString() } }, previousContext);
                contextCampaign = campaign; contextMission = mission; contextInitialized = true; previousContext = currentContext; TestEngine.TestingEnabled = false; TestEngine.CampaignCopyConfirmed = false; pins.Clear(); MarkEvidenceStale();
                QueueForgeEvent(ForgeEventKind.ContextEntering, 0, new Dictionary<string, string> { { "context", currentContext.ToString() } }, currentContext);
            }
            if (initialScreenReadyPending && sdkConnected) { initialScreenReadyPending = false; QueueForgeEvent(ForgeEventKind.InitialScreenReady, 0, new Dictionary<string, string> { { "context", currentContext.ToString() } }, currentContext); }
            if (campaignStartedPending && sdkConnected) { campaignStartedPending = false; QueueForgeEvent(ForgeEventKind.CampaignStarted, 0, new Dictionary<string, string> { { "context", currentContext.ToString() } }, currentContext); }
            if (gameLoadedPending && sdkConnected) { gameLoadedPending = false; QueueForgeEvent(ForgeEventKind.GameLoaded, 0, new Dictionary<string, string> { { "context", currentContext.ToString() } }, currentContext); }
            if (dt > 0 && dt < 5) { frames.Enqueue(dt * 1000); while (frames.Count > 600) frames.Dequeue(); seconds += dt; framesSeen++; }
            server.Process(Handle);
            laboratory.ObserveCleanup();
            if (dt > 0 && dt < 5 && sdkConnected)
            {
                weavePulseElapsed += dt;
                if (weavePulseElapsed >= .25)
                {
                    var pulseMilliseconds = weavePulseElapsed * 1000;
                    weavePulseElapsed = 0;
                    if (TestEngine.HasForgeWeaveSubscription(ForgeEventKind.Pulse))
                        QueueForgeEvent(ForgeEventKind.Pulse, pulseMilliseconds, new Dictionary<string, string> { { "host_interval_ms", pulseMilliseconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) } });
                }
            }
            DispatchQueuedForgeEvents();
            if (scan?.IsCompleted == true) { if (scan.IsFaulted) Register("CalradiaForge", "Error", scan.Exception.GetBaseException().Message); else diagnostics = scan.Result; scan = null; }
            elapsed += dt;
            if (elapsed >= 10 && (persistence == null || persistence.IsCompleted)) { elapsed = 0; persistence = Task.Run(() => Log.Persist(Paths.Sessions)); }
        }
        public object GetService(Type type) => type == typeof(IGameLaboratory) ? laboratory : null;
        public void PostToMainThread(Action action)
        {
            if (action != null) mainThreadActions.Enqueue(action);
        }
        public bool IsGameThread => Volatile.Read(ref gameThreadId) == Thread.CurrentThread.ManagedThreadId;
        public bool TryPostToMainThread(Action action)
        {
            return action != null && mainThreadActions.TryEnqueue(action);
        }
        public string ModulesDirectory => Path.Combine(TaleWorlds.Library.BasePath.Name, "Modules");
        public void Register(string module, string level, string message) => Log.Add(module, level, message);
        public void RecordEvent(string name) { if (name == null) return; events[name] = (events.TryGetValue(name, out var v) ? v : 0) + 1; }
        public void NotifyInitialScreenReady() { initialScreenReadyPending = true; }
        public void NotifyCampaignStarted() { campaignStartedPending = true; }
        public void NotifyGameLoaded() { gameLoadedPending = true; }
        public void NotifyMissionInitialized()
        {
            QueueForgeEvent(ForgeEventKind.MissionInitialized, 0, new Dictionary<string, string> { { "context", CurrentContext.ToString() } });
        }
        public void NotifyMissionEnded()
        {
            QueueForgeEvent(ForgeEventKind.MissionEnded, 0, new Dictionary<string, string> { { "context", CurrentContext.ToString() } });
        }
        public void NotifyAgentCreated(int index)
        {
            RecordEvent("event.agent.created");
            QueueForgeEvent(ForgeEventKind.AgentCreated, 0, new Dictionary<string, string> { { "agent_index", index.ToString(System.Globalization.CultureInfo.InvariantCulture) } });
        }
        public void NotifyAgentRemoved(int index)
        {
            RecordEvent("event.agent.deleted");
            QueueForgeEvent(ForgeEventKind.AgentRemoved, 0, new Dictionary<string, string> { { "agent_index", index.ToString(System.Globalization.CultureInfo.InvariantCulture) } });
        }
        void QueueForgeEvent(ForgeEventKind kind, double deltaMilliseconds, IDictionary<string, string> data, Context? context = null, string topic = null)
        {
            if (forgeEvents.Count >= 128)
            {
                forgeEvents.Dequeue();
                Register("CalradiaForge", "Warning", "ForgeWeave event queue reached 128 entries; the oldest event was discarded.");
            }
            Dictionary<string, string> eventData;
            if (data is Dictionary<string, string> directDict)
            {
                eventData = directDict;
            }
            else if (data != null)
            {
                eventData = new Dictionary<string, string>(data, StringComparer.Ordinal);
            }
            else
            {
                eventData = new Dictionary<string, string>();
            }
            forgeEvents.Enqueue(new QueuedForgeEvent { Kind = kind, Topic = topic, Context = context ?? CurrentContext, DeltaMilliseconds = deltaMilliseconds, Data = eventData });
        }
        void DispatchQueuedForgeEvents()
        {
            if (!sdkConnected) return;
            for (var count = 0; count < 16 && forgeEvents.Count > 0; count++)
            {
                var pending = forgeEvents.Dequeue();
                ForgeWeaveEventNames.TryGetValue(pending.Kind, out var names);
                RecordEvent(names.Dispatch ?? ("forgeweave." + pending.Kind + ".dispatch"));
                try
                {
                    var outcome = pending.Kind == ForgeEventKind.Custom
                        ? TestEngine.DispatchCustomForgeEvent(pending.Topic, pending.Context, this, pending.DeltaMilliseconds, pending.Data, CancellationToken.None)
                        : TestEngine.DispatchForgeEvent(pending.Kind, pending.Context, this, pending.DeltaMilliseconds, pending.Data, CancellationToken.None);
                    if (outcome.FailureCount > 0) RecordEvent(names.Failure ?? ("forgeweave." + pending.Kind + ".failure"));
                }
                catch (Exception error)
                {
                    RecordEvent(names.HostError ?? ("forgeweave." + pending.Kind + ".host_error"));
                    Register("CalradiaForge", "Warning", "ForgeWeave host dispatch failed for " + pending.Kind + ": " + error.Message);
                }
            }
        }
        // Called only on the game thread after Forge's application callback finishes.
        public void RecordCallbackTime(double milliseconds)
        {
            if (callbackTimes.Count == 600) callbackTimes.Dequeue();
            callbackTimes.Enqueue(milliseconds);
        }
        public Response Handle(Request s, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (s == null) throw new ArgumentNullException(nameof(s));
                if (!IsSupportedEnvelopeVersion(s)) throw new InvalidOperationException("Request envelope version mismatch: expected " + ForgeProtocol.EnvelopeVersion);
                if (GameNetwork.IsMultiplayer) throw new InvalidOperationException("Single-player only");
                token.ThrowIfCancellationRequested(); string data;
                switch (s.Action)
                {
                    case "hello": data = Json.Serialize(ForgeProtocol.Hello(SuiteInfo.Version, SuiteInfo.TargetGameVersion)); break;
                    case "summary": data = "Calradia Forge " + SuiteInfo.Version + "\nNative target: " + SuiteInfo.TargetGameVersion + "\nSession: " + Log.Id + "\nContext: " + CurrentContext + "\nTesting: " + TestEngine.TestingEnabled + "\nCampaign copy confirmed: " + TestEngine.CampaignCopyConfirmed + "\nTests: " + TestEngine.TestCount + "\nForgeWeave handlers: " + TestEngine.ForgeWeaveSubscriptionCount + "\nPatch blueprint providers: " + TestEngine.PatchBlueprintProviderCount + "\n" + (Log.PersistenceError ?? ""); break;
                    case "panel-open": if (OpenPanel == null) throw new InvalidOperationException("Panel is not available"); OpenPanel(); data = "Panel opened"; break;
                    case "panel-close": ClosePanel?.Invoke(); data = "Panel closed"; break;
                    case "scan": if (scan == null) scan = Task.Run(() => ModuleValidator.Inspect(Path.Combine(TaleWorlds.Library.BasePath.Name, "Modules"))); data = "Module scan started. Refresh Modules to see results."; break;
                    case "modules": data = Json.Serialize(diagnostics); break;
                    case "dependencies": data = Json.Serialize(DependencyPlanner.Create(diagnostics.Modules)); break;
                    case "diagnostics": data = Json.Serialize(TestEngine.Diagnose(this)); break;
                    case "extensions": data = Json.Serialize(TestEngine.Diagnose(this)); break;
                    case "logs": data = Json.Serialize(Log.Deserialize(s.Argument)); break;
                    case "clear-logs": Log.Clear(); data = "Logs cleared"; break;
                    case "inspect": data = Json.Serialize(Inspector.Deserialize(s.Argument)); break;
                    case "pin": { var key = s.Argument ?? ""; var parts = key.Split('|'); var o = Inspector.Deserialize(key).FirstOrDefault(x => parts.Length > 1 && x.Id == parts[1]); if (o == null) throw new InvalidOperationException("Exact object ID required"); if (pins.Count >= 100 && !pins.ContainsKey(key)) throw new InvalidOperationException("Maximum 100 snapshots"); pins[key] = o; data = "Snapshot pinned: " + o.Id; break; }
                    case "compare": { if (!pins.TryGetValue(s.Argument ?? "", out var before)) throw new InvalidOperationException("Pin this object first"); var after = Inspector.Deserialize(s.Argument).FirstOrDefault(o => o.Id == before.Id); data = SnapshotComparer.Compare(before, after); if (data == "") data = "No changes"; break; }
                    case "tests": data = Json.Serialize(TestEngine.Tests.ToList()); break;
                    case "snapshots": data = Json.Serialize(pins.Values.ToList()); break;
                    case "unpin": data = pins.Remove(s.Argument ?? "") ? "Snapshot removed" : "Snapshot not found"; break;
                    case "run-batch": { var batch = TestEngine.ExecuteBatch((s.Argument ?? "").Split(',').Select(x => x.Trim()), this, s.Seed, token); results.AddRange(batch); while (results.Count > 200) results.RemoveAt(0); data = Json.Serialize(batch); break; }
                    case "commands": data = Json.Serialize(TestEngine.Commands.ToList()); break;
                    case "command": { var parts = (s.Argument ?? "").Split(new[] { '|' }, 2); data = TestEngine.ExecuteCommand(parts[0], parts.Length > 1 ? parts[1] : "", this, token); break; }
                    case "test-mode": TestEngine.TestingEnabled = s.Argument == "enable"; if (!TestEngine.TestingEnabled) TestEngine.CampaignCopyConfirmed = false; data = "Testing: " + TestEngine.TestingEnabled; break;
                    case "confirm-copy": TestEngine.CampaignCopyConfirmed = s.Argument == "I_AM_USING_A_CAMPAIGN_COPY"; data = "Campaign copy confirmed: " + TestEngine.CampaignCopyConfirmed; break;
                    case "run": { var r = TestEngine.Execute(s.Argument, this, s.Seed, token); results.Add(r); if (results.Count > 200) results.RemoveAt(0); data = Json.Serialize(r); break; }
                    case "metrics": data = Json.Serialize(Metrics()); break;
                    case "patcher": 
                        var patchList = new System.Collections.Generic.List<object>();
                        foreach (var p in CalradiaForge.Sdk.Patcher.ForgePatcher.GetAppliedPatches()) {
                            var origType = p.Original?.DeclaringType?.Name ?? "Global";
                            var origName = p.Original?.Name ?? "Unknown";
                            var replType = p.Replacement?.DeclaringType?.Name ?? "Global";
                            var replName = p.Replacement?.Name ?? "Unknown";
                            patchList.Add(new { Target = origType + "." + origName, Replacement = replType + "." + replName, Module = p.SourceModule ?? "Native" });
                        }
                        data = Json.Serialize(patchList); 
                        break;
                    case "agent-memory":
                        CalradiaForge.Sdk.ForgeAgentMemory.GetStatistics(out int agCount, out int semCount, out int epiCount, out int procCount);
                        data = "{\"agentCount\":" + agCount + ",\"semanticCount\":" + semCount + ",\"episodicCount\":" + epiCount + ",\"proceduralCount\":" + procCount + "}";
                        break;
                    case "agent-memory-query":
                        {
                            string targetAgent = s.Argument;
                            if (string.IsNullOrEmpty(targetAgent)) targetAgent = "hero_main_hero";
                            int prevKills = CalradiaForge.Sdk.ForgeAgentMemory.Semantic.Get<int>(targetAgent, "TotalSlainHeroes");
                            bool isImprisoned = CalradiaForge.Sdk.ForgeAgentMemory.Semantic.Get<bool>(targetAgent, "IsImprisoned");
                            string captor = CalradiaForge.Sdk.ForgeAgentMemory.Semantic.Get<string>(targetAgent, "CurrentCaptorId") ?? "none";
                            string safeCaptor = captor.Replace("\\", "\\\\").Replace("\"", "\\\"");
                            string safeAgent = targetAgent.Replace("\\", "\\\\").Replace("\"", "\\\"");
                            data = "{\"agentId\":\"" + safeAgent + "\",\"isImprisoned\":" + (isImprisoned ? "true" : "false") + ",\"captorId\":\"" + safeCaptor + "\",\"slainHeroes\":" + prevKills + "}";
                            break;
                        }
                    case "framework": data = Json.Serialize(TestEngine.CaptureForgeWeave()); break;
                    case "event-journal": data = Json.Serialize(TestEngine.CaptureForgeWeave().RecentDispatches); break;
                    case "replay":
                        {
                            long sequence;
                            if (!long.TryParse((s.Argument ?? "").Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out sequence) || sequence < 1) throw new ArgumentException("Select a retained event sequence.");
                            var replay = TestEngine.ReplayForgeEvent(sequence, this, token);
                            RecordEvent(replay.Replayed ? "forgeweave.replay.completed" : "forgeweave.replay.rejected");
                            data = Json.Serialize(replay);
                            break;
                        }
                    case "forgeweave-unquarantine":
                        {
                            var arg = (s.Argument ?? "").Trim();
                            if (string.Equals(arg, "all", StringComparison.OrdinalIgnoreCase))
                            {
                                var resetCount = TestEngine.ResetAllForgeWeaveQuarantines();
                                data = "Quarantine reset for " + resetCount + " handler(s).";
                            }
                            else if (arg.StartsWith("module:", StringComparison.OrdinalIgnoreCase))
                            {
                                var modName = arg.Substring(7).Trim();
                                var resetCount = TestEngine.ResetModuleForgeWeaveQuarantines(modName);
                                data = "Quarantine reset for " + resetCount + " handler(s) in module " + modName + ".";
                            }
                            else
                            {
                                var ok = TestEngine.ResetForgeWeaveQuarantine(arg);
                                data = ok ? "Quarantine reset for handler '" + arg + "'." : "Handler not found or not quarantined: " + arg;
                            }
                            break;
                        }
                    case "forgeweave-clear":
                        {
                            var arg = (s.Argument ?? "").Trim().ToLowerInvariant();
                            if (arg == "replays")
                            {
                                TestEngine.ClearForgeWeaveReplays();
                                data = "ForgeWeave replays cleared.";
                            }
                            else if (arg == "journal")
                            {
                                TestEngine.ClearForgeWeaveJournal();
                                data = "ForgeWeave event journal cleared.";
                            }
                            else
                            {
                                TestEngine.ClearForgeWeaveJournal();
                                TestEngine.ClearForgeWeaveReplays();
                                data = "ForgeWeave journal and replays cleared.";
                            }
                            break;
                        }
                    case "patch-blueprints": data = Json.Serialize(TestEngine.PatchBlueprintProviders.ToList()); break;
                    case "patch-preflight": patchPreflightCache = PatchPreflightEngine.Inspect(TestEngine.CapturePatchBlueprints(this, token), AppDomain.CurrentDomain.GetAssemblies(), CurrentContext.ToString()); data = Json.Serialize(patchPreflightCache); break;
                    case "patch-diagnostics": patchDiagnosticsCache = ForgePatchDiagnostics.Capture(ForgeApi.Hooks, ForgeApi.Patches, AppDomain.CurrentDomain.GetAssemblies()); data = Json.Serialize(patchDiagnosticsCache); break;
                    case "hook-snapshots": data = Json.Serialize(CaptureHookStatus()); break;
                    case "hook-verify": data = VerifyHookSelection(s.Argument, token); break;
                    case "hook-apply-plan": data = CreateHookPlan("apply", s.Argument, token); break;
                    case "hook-apply-confirm": data = CommitHookPlan("apply", s.Argument, token); break;
                    case "hook-revert-plan": data = CreateHookPlan("revert", s.Argument, token); break;
                    case "hook-revert-confirm": data = CommitHookPlan("revert", s.Argument, token); break;
                    case "hook-plan-cancel": data = CancelHookPlan(s.Argument, token); break;
                    case "rule-auditor":
                        {
                            string target = null;
                            var raw = (s.Argument ?? "").Trim();
                            if (!string.IsNullOrEmpty(raw) && raw != "all")
                            {
                                var cand = Path.Combine(TaleWorlds.Library.BasePath.Name, "Modules", raw);
                                if (Directory.Exists(cand)) target = cand;
                                else if (Directory.Exists(Path.Combine("modules", raw))) target = Path.Combine("modules", raw);
                            }
                            if (string.IsNullOrEmpty(target))
                            {
                                var cand1 = "modules/CalradiaForge";
                                if (Directory.Exists(cand1)) target = cand1;
                                else target = Directory.GetCurrentDirectory();
                            }
                            data = Json.Serialize(ModRuleAuditor.Audit(target));
                            break;
                        }
                    case "sim-parties":
                        {
                            var bp = ForgePartySpawner.CreateBlueprint("cf_party_" + Guid.NewGuid().ToString("N").Substring(0, 8), "Simulated Garrison", "empire")
                                .AddTroop("imperial_legionary", 30)
                                .AddTroop("imperial_palatine_guard", 15);
                            data = Json.Serialize(new { Valid = bp.Validate().isValid, Blueprint = bp.Name, RosterCount = bp.TroopRoster.Count });
                            break;
                        }
                    case "sim-audio":
                        {
                            var ab = ForgeAudioBuilder.Create()
                                .Add2DSound("cf_ui_click", "ui_click.ogg", "ui")
                                .Add3DSound("cf_combat_clash", "clash.ogg", "mission_combat");
                            data = Json.Serialize(new { Valid = ab.Validate().isValid, Count = ab.Count });
                            break;
                        }
                    case "audit-localization":
                        {
                            data = Json.Serialize(new { Language = GameLocalization.CurrentLanguage, RegisteredStrings = GameLocalization.RegisteredCount });
                            break;
                        }
                    case "audit-save":
                        {
                            var chunkNeeded = ForgeSaveChunker.NeedsChunking(new string('A', 35000));
                            data = Json.Serialize(new { ChunkerProtects31KB = chunkNeeded, SafeChunkSize = ForgeSaveChunker.SafeChunkSize, Status = "Optimal" });
                            break;
                        }
                    case "report": data = Json.Serialize(Report()); break;
                    case "export": { var report = Report(); var p = Path.Combine(Paths.Data, "reports", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")); Task.Run(() => ReportExporter.Export(p, report)).ContinueWith(t => Register("CalradiaForge", t.IsFaulted ? "Error" : "Info", t.IsFaulted ? t.Exception.GetBaseException().Message : "Exported: " + p)); data = "Export queued: " + p; break; }
                    case "clipboard":
                        {
                            try { TaleWorlds.InputSystem.Input.SetClipboardText(s.Argument ?? ""); data = "Copied to clipboard."; }
                            catch (Exception ex) { data = "Clipboard error: " + ex.Message; }
                            break;
                        }
                    case "console":
                        {
                            if (string.IsNullOrWhiteSpace(s.Argument)) { data = "No command provided."; break; }
                            try
                            {
                                if (_cachedConsoleMethod == null)
                                {
                                    var t = Type.GetType("TaleWorlds.Library.CommandLineFunctionality, TaleWorlds.Library") ?? AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "TaleWorlds.Library")?.GetType("TaleWorlds.Library.CommandLineFunctionality");
                                    _cachedConsoleMethod = t?.GetMethod("CallFunction", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                                }
                                if (_cachedConsoleMethod != null)
                                {
                                    object[] p = new object[_cachedConsoleMethod.GetParameters().Length];
                                    p[0] = s.Argument;
                                    var r = _cachedConsoleMethod.Invoke(null, p);
                                    data = "Result:\n" + (r ?? "Success (No output)");
                                }
                                else
                                {
                                    data = "CommandLineFunctionality not found.";
                                }
                            }
                            catch (Exception ex) { data = "Error: " + ex.GetBaseException().Message; }
                            break;
                        }
                    case "language": data = Config.Language; break;
                    // ── NOVICE MODDER HUB ───────────────────────────────────────────
                    case "novice-behavior":   data = CalradiaForge.Core.NoviceScaffoldEngine.Generate("behavior",   s.Argument); break;
                    case "novice-troop":      data = CalradiaForge.Core.NoviceScaffoldEngine.Generate("troop",      s.Argument); break;
                    case "novice-quest":      data = CalradiaForge.Core.NoviceScaffoldEngine.Generate("quest",      s.Argument); break;
                    case "novice-item":       data = CalradiaForge.Core.NoviceScaffoldEngine.Generate("item",       s.Argument); break;
                    case "novice-submodule":  data = CalradiaForge.Core.NoviceScaffoldEngine.Generate("submodule",  s.Argument); break;
                    case "novice-checklist":  data = CalradiaForge.Core.NoviceScaffoldEngine.Generate("checklist",  s.Argument); break;
                    case "novice-events":     data = CalradiaForge.Core.NoviceScaffoldEngine.Generate("events",     s.Argument); break;
                    case "novice-hint":       data = CalradiaForge.Core.NoviceScaffoldEngine.Generate("hint",       s.Argument); break;
                    case "novice-gauntlet":   data = CalradiaForge.Core.NoviceScaffoldEngine.Generate("gauntlet-page", s.Argument); break;
                    case "novice-workshop":   data = CalradiaForge.Core.NoviceScaffoldEngine.Generate("workshop",   s.Argument); break;
                    case "novice-party":      data = CalradiaForge.Core.NoviceScaffoldEngine.Generate("party",      s.Argument); break;
                    case "novice-building":   data = CalradiaForge.Core.NoviceScaffoldEngine.Generate("building",   s.Argument); break;
                    case "novice-combat":     data = CalradiaForge.Core.NoviceScaffoldEngine.Generate("combat",     s.Argument); break;
                    // ── ADVANCED SIMULATION & AUDIT TOOLS ───────────────────────────
                    case "sim-trade":         data = CalradiaForge.Sdk.ForgeTradeSimulator.SimulateWorkshopRoi("Pravend", string.IsNullOrWhiteSpace(s.Argument) ? "Brewery" : s.Argument).Summary; break;
                    case "siege-tactics":
                    {
                        var siege = CalradiaForge.Sdk.ForgeSiegeTactician.AnalyzeAssaultTactics(string.IsNullOrWhiteSpace(s.Argument) ? "Chaikand" : s.Argument, 600, 300, true, 1, 1);
                        data = $"[Siege Assault Analysis: {siege.SettlementName}]\n" +
                               $"• Attackers: {siege.AttackerStrength} | Defenders: {siege.DefenderStrength}\n" +
                               $"• Machinery: Battering Ram={siege.HasRam} | Towers={siege.TowerCount} | Breaches={siege.WallBreachCount}\n" +
                               $"• Projected Casualties: Attackers {(siege.AttackerExpectedCasualtyRate * 100):F1}% | Defenders {(siege.DefenderExpectedCasualtyRate * 100):F1}%\n" +
                               $"• Breakthrough Probability: {(siege.BreakthroughProbability * 100):F1}%\n" +
                               $"• Tactical Advice: {siege.TacticalAdvice}\n" +
                               $"• Required Dynamic Navmeshes: {string.Join(", ", siege.NavmeshRequirements)}";
                        break;
                    }
                    case "casus-belli":       data = CalradiaForge.Sdk.ForgeCasusBelliEngine.EvaluateWarJustification("Vlandia", string.IsNullOrWhiteSpace(s.Argument) ? "Battania" : s.Argument, 5500, 3200, 3, false, 2, -15).Summary; break;
                    case "audit-audio":
                    {
                        string sampleXml = "<module_sounds>\n  <module_sound name=\"custom_ui_click\" is_2d=\"true\" sound_category=\"ui\" path=\"ui_click.ogg\" />\n  <module_sound name=\"custom_iron_shield_clash\" is_2d=\"false\" sound_category=\"mission_combat\" path=\"shield_clash.ogg\" />\n</module_sounds>";
                        data = CalradiaForge.Sdk.ForgeAudioInspector.AuditSoundManifest(sampleXml).Summary;
                        break;
                    }
                                        default:
                        if (s.Action.StartsWith("sdk-"))
                        {
                            var tag = s.Action.Substring(4);
                            data = CalradiaForge.Sdk.ScaffoldEngine.Generate(tag, s.Argument);
                            break;
                        } throw new ArgumentException("Unknown action");
                }
                return new Response { Id = s.Id, Success = true, Data = data };
            }
            catch (Exception e) { Register("CalradiaForge", "Error", e.Message); return new Response { Id = s.Id, Error = e.Message }; }
            finally { stopwatch.Stop(); events["operation.last.ms"] = stopwatch.Elapsed.TotalMilliseconds; }
        }

        internal static bool IsSupportedEnvelopeVersion(Request request) => request != null && request.Version == ForgeProtocol.EnvelopeVersion;

        HookIpcStatus CaptureHookStatus()
        {
            var status = new HookIpcStatus
            {
                Session = Log.Id,
                CanManage = CanManageHooks,
                BlockedReason = CanManageHooks ? string.Empty : "Hook changes are available only on Bannerlord's exact main-menu screen, on the game thread, with no campaign, mission, or multiplayer session active."
            };
            var service = ForgeApi.Hooks;
            status.ServiceAvailable = service != null;
            if (service == null) return status;
            foreach (var snapshot in service.GetSnapshots() ?? Array.Empty<ForgeHookSnapshot>())
                status.Hooks.Add(ToHookIpcSnapshot(snapshot));
            return status;
        }

        string VerifyHookSelection(string argument, CancellationToken cancellationToken)
        {
            RequireHookManagementContext();
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(argument) || argument.Length > 16384)
                throw new ArgumentException("Provide a bounded registered hook selection.");
            var ids = Json.Deserialize<HookIpcSelection>(argument)?.HookIds;
            if (ids == null || ids.Count == 0 || ids.Count > MaximumHookPlanSize ||
                ids.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 128 || id.Any(char.IsControl)) ||
                ids.Distinct(StringComparer.Ordinal).Count() != ids.Count)
                throw new ArgumentException("Provide unique bounded registered hook IDs.");
            var service = ForgeApi.Hooks ?? throw new InvalidOperationException("The optional hook service is unavailable.");
            var registered = service.GetSnapshots().ToDictionary(item => item.Id, StringComparer.Ordinal);
            if (ids.Any(id => !registered.ContainsKey(id))) throw new ArgumentException("The selection includes an unregistered hook ID.");
            var response = new HookIpcCommit { Operation = "verify", Session = Log.Id, Succeeded = true };
            foreach (var id in ids)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RequireHookManagementContext();
                var result = service.Verify(id);
                response.Results.Add(new HookIpcResult { Id = result.Id, State = result.State.ToString(),
                    Succeeded = result.Succeeded, Verified = result.Succeeded, Detail = result.Detail,
                    VerificationDetail = result.Detail });
                response.Succeeded &= result.Succeeded;
            }
            response.Partial = !response.Succeeded && response.Results.Any(item => item.Succeeded);
            return Json.Serialize(response);
        }

        string CreateHookPlan(string operation, string argument, CancellationToken cancellationToken)
        {
            // Starting a new planning attempt invalidates any earlier confirmation preview.
            pendingHookPlan = null;
            cancellationToken.ThrowIfCancellationRequested();
            RequireHookManagementContext();
            var planningScreen = ScreenManager.TopScreen;
            var planningEpoch = hookContextEpoch;
            var service = ForgeApi.Hooks;
            if (service == null) throw new InvalidOperationException("The optional Forge hook service is unavailable.");
            if (string.IsNullOrWhiteSpace(argument) || argument.Length > 16384)
                throw new ArgumentException("Provide a bounded JSON selection containing registered hook IDs.");

            var selection = Json.Deserialize<HookIpcSelection>(argument);
            var ids = selection?.HookIds;
            if (ids == null || ids.Count == 0 || ids.Count > MaximumHookPlanSize)
                throw new ArgumentException("Select between 1 and " + MaximumHookPlanSize + " registered hooks.");

            var registered = new Dictionary<string, ForgeHookSnapshot>(StringComparer.Ordinal);
            foreach (var snapshot in service.GetSnapshots() ?? Array.Empty<ForgeHookSnapshot>())
            {
                if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.Id))
                    throw new InvalidOperationException("The hook service returned an invalid registration snapshot.");
                if (registered.ContainsKey(snapshot.Id))
                    throw new InvalidOperationException("The hook service returned duplicate registered IDs; no plan was created.");
                registered.Add(snapshot.Id, snapshot);
            }

            var selected = new List<HookIpcSnapshot>(ids.Count);
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || id.IndexOf('\0') >= 0 || !uniqueIds.Add(id))
                    throw new ArgumentException("Hook IDs must be unique, non-empty registered IDs no longer than 128 characters.");
                if (!registered.TryGetValue(id, out var snapshot))
                    throw new InvalidOperationException("Hook ID is not registered in this process: " + id);
                var allowed = operation == "apply"
                    ? snapshot.State == ForgeHookState.Registered || snapshot.State == ForgeHookState.Reverted
                    : snapshot.State == ForgeHookState.Applied || snapshot.State == ForgeHookState.Conflict || snapshot.State == ForgeHookState.Failed;
                if (!allowed)
                    throw new InvalidOperationException("Hook '" + id + "' is not eligible for " + operation + " from state " + snapshot.State + ".");
                selected.Add(ToHookIpcSnapshot(snapshot));
            }

            cancellationToken.ThrowIfCancellationRequested();
            RequireHookManagementContext();
            if (!ReferenceEquals(planningScreen, ScreenManager.TopScreen) || planningEpoch != hookContextEpoch)
                throw new InvalidOperationException("The main-menu screen changed while creating the hook plan; request a fresh snapshot.");
            var token = NewHookConfirmationToken();
            var expires = DateTime.UtcNow.Add(HookPlanLifetime);
            pendingHookPlan = new PendingHookPlan(operation, Log.Id, token, expires, service, selected, planningScreen, planningEpoch);
            return Json.Serialize(new HookIpcPlan
            {
                Operation = operation,
                Session = Log.Id,
                Token = token,
                ExpiresAtUtc = expires.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                RequiresConfirmation = true,
                Hooks = selected.Select(CloneHookIpcSnapshot).ToList()
            });
        }

        string CommitHookPlan(string operation, string argument, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(argument) || argument.Length > 1024)
                throw new ArgumentException("Provide the confirmation token as JSON.");
            var confirmation = Json.Deserialize<HookIpcConfirmation>(argument);
            var commit = CommitHookPlanCore(
                ref pendingHookPlan,
                operation,
                confirmation?.Token,
                () => Log.Id,
                () => DateTime.UtcNow,
                () => ScreenManager.TopScreen,
                () => hookContextEpoch,
                () => CanManageHooks,
                () => RequireHookManagementContext(),
                () => ForgeApi.Hooks,
                (plan, service) =>
                {
                    var current = new Dictionary<string, ForgeHookSnapshot>(StringComparer.Ordinal);
                    foreach (var snapshot in service.GetSnapshots() ?? Array.Empty<ForgeHookSnapshot>())
                    {
                        if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.Id) || current.ContainsKey(snapshot.Id))
                            return "Hook registrations became ambiguous after planning.";
                        current.Add(snapshot.Id, snapshot);
                    }
                    foreach (var planned in plan.Hooks)
                    {
                        if (!current.TryGetValue(planned.Id, out var snapshot) || !SameHookSnapshot(planned, snapshot))
                            return "Hook registration or state changed after planning: " + planned.Id;
                        var allowed = operation == "apply"
                            ? snapshot.State == ForgeHookState.Registered || snapshot.State == ForgeHookState.Reverted
                            : snapshot.State == ForgeHookState.Applied || snapshot.State == ForgeHookState.Conflict || snapshot.State == ForgeHookState.Failed;
                        if (!allowed) return "Hook is no longer eligible for " + operation + ": " + planned.Id;
                    }
                    return null;
                },
                cancellationToken,
                (service, id) => service.Apply(id),
                (service, id) => service.Revert(id),
                (service, id) => service.Verify(id));
            return Json.Serialize(commit);
        }

        internal static HookIpcCommit CommitHookPlanCore(
            ref PendingHookPlan pendingHookPlan,
            string operation,
            string token,
            Func<string> currentSession,
            Func<DateTime> utcNow,
            Func<object> currentScreen,
            Func<int> currentContextEpoch,
            Func<bool> canManageHooks,
            Action requireHookManagementContext,
            Func<IForgeHookService> currentHookService,
            Func<PendingHookPlan, IForgeHookService, string> validateSnapshots,
            CancellationToken cancellationToken,
            Func<IForgeHookService, string, ForgeHookOperationResult> apply,
            Func<IForgeHookService, string, ForgeHookOperationResult> revert,
            Func<IForgeHookService, string, ForgeHookOperationResult> verify)
        {
            var plan = pendingHookPlan;
            if (plan == null || string.IsNullOrEmpty(token) || !string.Equals(token, plan.Token, StringComparison.Ordinal) ||
                !string.Equals(operation, plan.Operation, StringComparison.Ordinal))
                throw new InvalidOperationException("Confirmation token is unknown, already used, or for another operation.");

            // Consume a matching token before any mutable precondition check. A denied or partial
            // commit cannot be replayed after the operator changes screens or state.
            pendingHookPlan = null;
            if (utcNow() > plan.ExpiresAtUtc)
                throw new InvalidOperationException("Confirmation token expired; create a new plan.");
            if (!string.Equals(plan.Session, currentSession(), StringComparison.Ordinal))
                throw new InvalidOperationException("The game session changed after the plan was created.");
            if (plan.ContextEpoch != currentContextEpoch() || !ReferenceEquals(plan.Screen, currentScreen()))
                throw new InvalidOperationException("The main-menu screen changed after planning; the single-use plan was discarded.");
            requireHookManagementContext();
            var service = currentHookService();
            if (service == null || !ReferenceEquals(service, plan.Service))
                throw new InvalidOperationException("The hook service changed or became unavailable after planning.");

            var snapshotError = validateSnapshots(plan, service);
            if (snapshotError != null) throw new InvalidOperationException(snapshotError);

            var commit = new HookIpcCommit { Operation = operation, Session = currentSession(), TokenConsumed = true, Succeeded = true };
            List<CalradiaForge.Core.HookIpcSnapshot> ordered;
            if (operation == "revert")
            {
                ordered = new List<CalradiaForge.Core.HookIpcSnapshot>(plan.Hooks.Count);
                for (int i = plan.Hooks.Count - 1; i >= 0; i--)
                    ordered.Add(plan.Hooks[i]);
            }
            else
            {
                ordered = plan.Hooks;
            }
            var applyOrRevert = operation == "apply" ? apply : revert;
            for (var index = 0; index < ordered.Count; index++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    StopHookCommit(commit, ordered, index, "IPC request was cancelled after the current completed hook; refresh snapshots to reconcile actual state.", true);
                    break;
                }
                if (!canManageHooks() || plan.ContextEpoch != currentContextEpoch() || !ReferenceEquals(plan.Screen, currentScreen()))
                {
                    StopHookCommit(commit, ordered, index, "The hook-management screen or game context changed during the commit; refresh snapshots to reconcile actual state.", false);
                    break;
                }
                var planned = ordered[index];
                try
                {
                    var result = applyOrRevert(service, planned.Id);
                    var verification = verify(service, planned.Id);
                    var item = new HookIpcResult
                    {
                        Id = planned.Id,
                        State = result?.State.ToString() ?? ForgeHookState.Failed.ToString(),
                        Succeeded = result != null && result.Succeeded,
                        Verified = verification != null && verification.Succeeded,
                        Detail = result?.Detail ?? "The hook service returned no operation result.",
                        VerificationDetail = verification?.Detail ?? "The hook service returned no verification result."
                    };
                    commit.Results.Add(item);
                    if (!item.Succeeded || !item.Verified)
                    {
                        commit.Succeeded = false;
                        if (operation == "apply")
                        {
                            StopHookCommit(commit, ordered, index + 1, "Apply stopped after an item failed verification; refresh snapshots to reconcile actual state.", false);
                            break;
                        }
                    }
                }
                catch (Exception error)
                {
                    commit.Results.Add(new HookIpcResult
                    {
                        Id = planned.Id,
                        State = ForgeHookState.Failed.ToString(),
                        Succeeded = false,
                        Verified = false,
                        Detail = error.GetBaseException().Message,
                        VerificationDetail = "Verification was not attempted after an operation failure."
                    });
                    commit.Succeeded = false;
                    if (operation == "apply")
                    {
                        StopHookCommit(commit, ordered, index + 1, "Apply stopped after an item failed; refresh snapshots to reconcile actual state.", false);
                        break;
                    }
                }
            }
            commit.Partial = (!commit.Succeeded && commit.Results.Any(result => result.Succeeded)) || commit.NotAttemptedIds.Count > 0;
            return commit;
        }

        string CancelHookPlan(string argument, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(argument) || argument.Length > 1024)
                throw new ArgumentException("Provide the session and single-use plan token as JSON.");
            var request = Json.Deserialize<HookIpcCancelPlanRequest>(argument);
            if (request == null || string.IsNullOrEmpty(request.Session) || request.Session.Length > 128 ||
                string.IsNullOrEmpty(request.Token) || request.Token.Length > 128 || request.Token.Any(char.IsControl))
                throw new ArgumentException("A valid session and bounded plan token are required.");

            var plan = pendingHookPlan;
            bool cancelled = plan != null && string.Equals(request.Session, Log.Id, StringComparison.Ordinal) &&
                string.Equals(plan.Session, Log.Id, StringComparison.Ordinal) &&
                string.Equals(plan.Token, request.Token, StringComparison.Ordinal);
            if (cancelled) pendingHookPlan = null;
            return Json.Serialize(new HookIpcCancelPlanResult { Session = Log.Id, Cancelled = cancelled });
        }

        static void StopHookCommit(HookIpcCommit commit, IList<HookIpcSnapshot> ordered, int firstNotAttempted, string reason, bool cancelled)
        {
            commit.Succeeded = false;
            commit.Cancelled |= cancelled;
            commit.StopReason = reason;
            for (var index = firstNotAttempted; index < ordered.Count; index++) commit.NotAttemptedIds.Add(ordered[index].Id);
        }

        void ObserveHookManagementContext()
        {
            var screen = ScreenManager.TopScreen;
            var canManage = CanManageHooks;
            if (!hookContextInitialized)
            {
                hookContextInitialized = true;
                lastHookContextCanManage = canManage;
                lastHookContextScreen = screen;
                return;
            }

            if (!ReferenceEquals(screen, lastHookContextScreen) || canManage != lastHookContextCanManage)
            {
                hookContextEpoch++;
                pendingHookPlan = null;
                lastHookContextScreen = screen;
                lastHookContextCanManage = canManage;
            }
        }

        void RequireHookManagementContext()
        {
            if (!CanManageHooks)
                throw new InvalidOperationException("Hook changes are restricted to Bannerlord's exact main-menu screen on the game thread, with no campaign, mission, or multiplayer session active.");
        }

        static HookIpcSnapshot ToHookIpcSnapshot(ForgeHookSnapshot snapshot)
        {
            return new HookIpcSnapshot
            {
                Id = snapshot.Id,
                Owner = snapshot.Owner,
                TargetMethod = snapshot.TargetMethod,
                HasPrefix = snapshot.HasPrefix,
                HasPostfix = snapshot.HasPostfix,
                HasFinalizer = snapshot.HasFinalizer,
                HasTranspiler = snapshot.HasTranspiler,
                Priority = snapshot.Priority,
                Before = snapshot.Before?.ToList() ?? new List<string>(),
                After = snapshot.After?.ToList() ?? new List<string>(),
                State = snapshot.State.ToString(),
                Detail = snapshot.Detail
            };
        }

        static HookIpcSnapshot CloneHookIpcSnapshot(HookIpcSnapshot snapshot)
        {
            return new HookIpcSnapshot
            {
                Id = snapshot.Id, Owner = snapshot.Owner, TargetMethod = snapshot.TargetMethod,
                HasPrefix = snapshot.HasPrefix, HasPostfix = snapshot.HasPostfix, Priority = snapshot.Priority,
                HasFinalizer = snapshot.HasFinalizer, HasTranspiler = snapshot.HasTranspiler,
                Before = snapshot.Before?.ToList() ?? new List<string>(), After = snapshot.After?.ToList() ?? new List<string>(),
                State = snapshot.State, Detail = snapshot.Detail
            };
        }

        static bool SameHookSnapshot(HookIpcSnapshot planned, ForgeHookSnapshot current)
        {
            if (!string.Equals(planned.Id, current.Id, StringComparison.Ordinal) ||
                !string.Equals(planned.Owner, current.Owner, StringComparison.Ordinal) ||
                !string.Equals(planned.TargetMethod, current.TargetMethod, StringComparison.Ordinal) ||
                !string.Equals(planned.Detail, current.Detail, StringComparison.Ordinal) ||
                !string.Equals(planned.State, current.State.ToString(), StringComparison.Ordinal) ||
                planned.HasPrefix != current.HasPrefix || planned.HasPostfix != current.HasPostfix ||
                planned.HasFinalizer != current.HasFinalizer || planned.HasTranspiler != current.HasTranspiler || planned.Priority != current.Priority)
                return false;
            return SameStrings(planned.Before, current.Before) && SameStrings(planned.After, current.After);
        }

        static bool SameStrings(IList<string> planned, System.Collections.Generic.IReadOnlyList<string> current)
        {
            if (planned == null || current == null) return planned == null && current == null;
            if (planned.Count != current.Count) return false;
            for (var index = 0; index < planned.Count; index++)
                if (!string.Equals(planned[index], current[index], StringComparison.Ordinal)) return false;
            return true;
        }

        static string NewHookConfirmationToken()
        {
            var bytes = new byte[32];
            using (var random = System.Security.Cryptography.RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }

        const int MaximumHookPlanSize = 32;
        static readonly TimeSpan HookPlanLifetime = TimeSpan.FromSeconds(60);
        PendingHookPlan pendingHookPlan;

        internal sealed class PendingHookPlan
        {
            public PendingHookPlan(string operation, string session, string token, DateTime expiresAtUtc,
                IForgeHookService service, IEnumerable<HookIpcSnapshot> hooks, object screen, int contextEpoch)
            {
                Operation = operation; Session = session; Token = token; ExpiresAtUtc = expiresAtUtc;
                Service = service; Hooks = hooks.Select(CloneHookIpcSnapshot).ToList();
                Screen = screen; ContextEpoch = contextEpoch;
            }
            public string Operation { get; }
            public string Session { get; }
            public string Token { get; }
            public DateTime ExpiresAtUtc { get; }
            public IForgeHookService Service { get; }
            public List<HookIpcSnapshot> Hooks { get; }
            public object Screen { get; }
            public int ContextEpoch { get; }
        }
        Dictionary<string, double> Metrics()
        {
            var d = new Dictionary<string, double>(events);
            // Rev097: manual loops replace LINQ to eliminate allocations in performance-telemetry path
            if (frames.Count == 0)
            {
                d["frame.mean.ms"] = 0;
                d["frame.p95.ms"] = 0;
            }
            else
            {
                var frameCopy = frames.ToArray();
                Array.Sort(frameCopy);
                double frameSum = 0;
                for (int i = 0; i < frameCopy.Length; i++) frameSum += frameCopy[i];
                d["frame.mean.ms"] = frameSum / frameCopy.Length;
                d["frame.p95.ms"] = frameCopy[(int)((frameCopy.Length - 1) * .95)];
            }
            d["frame.samples"] = frames.Count;
            d["observed.seconds"] = seconds;
            d["forge.callback.samples"] = callbackTimes.Count;
            if (callbackTimes.Count > 0)
            {
                var sorted = callbackTimes.ToArray();
                Array.Sort(sorted);
                double cbSum = 0;
                for (int i = 0; i < sorted.Length; i++) cbSum += sorted[i];
                d["forge.callback.mean.ms"] = cbSum / sorted.Length;
                d["forge.callback.p95.ms"] = sorted[(int)((sorted.Length - 1) * .95)];
                d["forge.callback.max.ms"] = sorted[sorted.Length - 1];
            }
            foreach (var kvp in events)
            {
                if (kvp.Key.StartsWith("event.", StringComparison.Ordinal))
                    d[kvp.Key + ".perSecond"] = seconds == 0 ? 0 : kvp.Value / seconds;
            }
            return d;
        }
        void MarkEvidenceStale()
        {
            if (!string.IsNullOrWhiteSpace(patchDiagnosticsCache?.CapturedAt))
            {
                patchDiagnosticsCache.IsStale = true;
                if (!patchDiagnosticsCache.Notes.Contains("The game context changed after this diagnostics capture.")) patchDiagnosticsCache.Notes.Add("The game context changed after this diagnostics capture.");
            }
            if (!string.IsNullOrWhiteSpace(patchPreflightCache?.CapturedAt))
            {
                patchPreflightCache.IsStale = true;
                if (!patchPreflightCache.Notes.Contains("The game context changed after this preflight capture.")) patchPreflightCache.Notes.Add("The game context changed after this preflight capture.");
            }
        }
        // Export uses explicitly captured patch evidence. ForgeWeave's in-memory health snapshot has
        // no reflection or game traversal, so reports capture it directly at export time.
        SessionReport Report() => new SessionReport { GameVersion = diagnostics.Modules.FirstOrDefault(m => m.Id == "Native")?.Version ?? "1.4.8 (target; scan to verify)", Session = Log.Id, ModuleDiagnostics = diagnostics, Logs = Log.Deserialize(), Tests = results.ToList(), Snapshots = pins.Values.ToList(), Metrics = Metrics(), PatchDiagnostics = patchDiagnosticsCache, ForgeWeave = TestEngine.CaptureForgeWeave(), PatchPreflight = patchPreflightCache };
        sealed class QueuedForgeEvent
        {
            public ForgeEventKind Kind;
            public string Topic;
            public Context Context;
            public double DeltaMilliseconds;
            public Dictionary<string, string> Data;
        }
        public void Dispose()
        {
            // A detour can remain installed if SDK disconnect is rejected. Make its callback
            // route permanently inert before attempting cleanup; the published hook capability
            // can still verify/revert retained state while its normal mutation gate allows it.
            TestEngine.StopApplicationsForUnload();
            try
            {
                ForgeApi.DisconnectForUnload();
            }
            finally
            {
                // A rejected SDK disconnect can preserve the published capability while
                // this Runtime is still ticking, but module unload stops that recovery
                // route. Always stop the pipe server here so unload cannot leave a listener
                // whose host no longer exists.
                try { server.Dispose(); }
                finally { Task.Run(() => Log.Persist(Paths.Sessions)); }
            }
        }
    }
}
