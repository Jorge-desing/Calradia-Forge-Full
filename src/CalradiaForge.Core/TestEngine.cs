using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CalradiaForge.Sdk;

namespace CalradiaForge.Core
{
    public sealed class TestEngine : IForgeRegistry, IForgeUiRegistry, IPatchBlueprintRegistry, IForgePatchService, IForgePatchServiceLifecycle, IForgeHookService, IForgeHookServiceLifecycle, IForgeHookServiceDisconnectGuard, IForgeEventRegistry, IForgeReplayRegistry, IForgeSettingsRegistry, IForgeLogger, IForgeInput, IForgeSaveManager, IForgeDebug, IForgeAgentManager, IForgeAnalysisRegistry, IForgeRuntimeCapabilities
    {
        readonly object registryGate=new object();
        readonly ForgePatchService forgePatchService=new ForgePatchService();
        readonly ForgeHookService forgeHookService;
        readonly Dictionary<string,ITestCase> tests=new Dictionary<string,ITestCase>();
        readonly Dictionary<string,ICommand> commands=new Dictionary<string,ICommand>();
        readonly Dictionary<string,IDiagnosticProvider> providers=new Dictionary<string,IDiagnosticProvider>();
        readonly Dictionary<string,IPatchBlueprintProvider> patchBlueprintProviders=new Dictionary<string,IPatchBlueprintProvider>();
        readonly Dictionary<string,IForgeAnalyzer> analyzers=new Dictionary<string,IForgeAnalyzer>();
        readonly Dictionary<string,ForgeUiPageDescriptor> uiPages=new Dictionary<string,ForgeUiPageDescriptor>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string,object> settingsData=new Dictionary<string,object>();
        readonly ForgeWeaveEngine forgeWeave=new ForgeWeaveEngine();
        readonly HashSet<string> ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string,Descriptor> descriptors=new Dictionary<string,Descriptor>(StringComparer.OrdinalIgnoreCase);
        ITestServices hostServices;
        bool testingEnabled,campaignCopyConfirmed;
        public TestEngine() : this(null) { }
        public TestEngine(Func<bool> canManageHooks) { forgeHookService = new ForgeHookService(canManageHooks); }
#if NETFRAMEWORK
        /// <summary>Registers a local IL manipulator through the shared hook service without exposing MonoMod in SDK contracts.</summary>
        public IForgeHookHandle RegisterTranspiler(ForgeHookDefinition metadata, MonoMod.Cil.ILContext.Manipulator manipulator)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));
            lock (registryGate)
            {
                if (string.IsNullOrWhiteSpace(metadata.Id)) throw new ArgumentException("Hook ID is required.", nameof(metadata));
                if (!ids.Add(metadata.Id)) throw new ArgumentException("Duplicate ID: " + metadata.Id);
                try { return forgeHookService.RegisterTranspiler(metadata, manipulator); }
                catch { ids.Remove(metadata.Id); throw; }
            }
        }
#endif
        public bool TestingEnabled { get {lock(registryGate)return testingEnabled;} set {lock(registryGate)testingEnabled=value;} }
        public bool CampaignCopyConfirmed { get {lock(registryGate)return campaignCopyConfirmed;} set {lock(registryGate)campaignCopyConfirmed=value;} }
        public IEnumerable<Descriptor> Tests
        {
            get
            {
                lock(registryGate)
                {
                    var result = new Descriptor[tests.Count];
                    int i = 0;
                    foreach(var id in tests.Keys)
                    {
                        result[i++] = Copy(descriptors[id]);
                    }
                    return result;
                }
            }
        }
        public int TestCount { get { lock(registryGate) return tests.Count; } }
        public IEnumerable<Descriptor> Commands
        {
            get
            {
                lock(registryGate)
                {
                    var result = new Descriptor[commands.Count];
                    int i = 0;
                    foreach(var id in commands.Keys)
                    {
                        result[i++] = Copy(descriptors[id]);
                    }
                    return result;
                }
            }
        }
        public int CommandCount { get { lock(registryGate) return commands.Count; } }
        public IEnumerable<Descriptor> PatchBlueprintProviders
        {
            get
            {
                lock(registryGate)
                {
                    var result = new Descriptor[patchBlueprintProviders.Count];
                    int i = 0;
                    foreach(var id in patchBlueprintProviders.Keys)
                    {
                        result[i++] = Copy(descriptors[id]);
                    }
                    return result;
                }
            }
        }
        public int PatchBlueprintProviderCount { get { lock(registryGate) return patchBlueprintProviders.Count; } }
        public IForgePatchHandle ApplyMethodReplacement(string patchId,string owner,System.Reflection.MethodInfo target,System.Reflection.MethodInfo replacement) => forgePatchService.ApplyMethodReplacement(patchId,owner,target,replacement);
        public IReadOnlyList<ForgePatchSnapshot> GetSnapshots(string owner=null) => forgePatchService.GetSnapshots(owner);
        public ForgePatchVerification Verify(string patchId) => forgePatchService.Verify(patchId);
        public ForgePatchRevertResult Revert(string patchId) => forgePatchService.Revert(patchId);
        public IReadOnlyList<ForgePatchRevertResult> RevertOwner(string owner) => forgePatchService.RevertOwner(owner);
        public IReadOnlyList<ForgePatchRevertResult> RevertAll() => forgePatchService.RevertAll();
        public IForgeHookHandle Register(ForgeHookDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            lock (registryGate)
            {
                if (string.IsNullOrWhiteSpace(definition.Id)) throw new ArgumentException("Hook ID is required.", nameof(definition));
                if (!ids.Add(definition.Id)) throw new ArgumentException("Duplicate ID: " + definition.Id);
                try { return forgeHookService.Register(definition); }
                catch { ids.Remove(definition.Id); throw; }
            }
        }
        IReadOnlyList<ForgeHookSnapshot> IForgeHookService.GetSnapshots(string owner) => forgeHookService.GetSnapshots(owner);
        ForgeHookOperationResult IForgeHookService.Apply(string hookId) => forgeHookService.Apply(hookId);
        ForgeHookOperationResult IForgeHookService.Verify(string hookId) => forgeHookService.Verify(hookId);
        ForgeHookOperationResult IForgeHookService.Revert(string hookId) => forgeHookService.Revert(hookId);
        IReadOnlyList<ForgeHookOperationResult> IForgeHookService.RevertOwner(string owner) => forgeHookService.RevertOwner(owner);
        IReadOnlyList<ForgeHookOperationResult> IForgeHookService.RevertAll() => forgeHookService.RevertAll();
        public bool CanDisconnect(out string reason) => forgeHookService.CanDisconnect(out reason);
        internal void StopApplicationsForUnload()
        {
            forgeHookService.StopCallbacksForUnload();
            forgePatchService.StopApplicationsForUnload();
        }
        public void Reconnect() { forgePatchService.Reconnect(); forgeHookService.Reconnect(); }
        public void Disconnect() { forgeHookService.Disconnect(); forgePatchService.Disconnect(); }
        public IReadOnlyList<Descriptor> Analyzers { get {lock(registryGate)return analyzers.Keys.OrderBy(id=>id,StringComparer.Ordinal).Select(id=>Copy(descriptors[id])).ToArray();} }
        public IEnumerable<ForgeEventSubscription> ForgeWeaveSubscriptions => forgeWeave.Subscriptions;
        public int ForgeWeaveSubscriptionCount => forgeWeave.Count;
        public bool HasForgeWeaveSubscription(ForgeEventKind eventKind) => forgeWeave.HasSubscription(eventKind, null);
        public bool HasForgeWeaveSubscription(ForgeEventKind eventKind, string topic) => forgeWeave.HasSubscription(eventKind, topic);
        
        public void Register<T>(string moduleId, string displayName, T defaultSettings) where T : class
        {
            lock(registryGate) {
                if(!settingsData.ContainsKey(moduleId)) settingsData[moduleId] = defaultSettings;
            }
        }
        public T GetSettings<T>(string moduleId) where T : class
        {
            lock(registryGate) {
                if(settingsData.TryGetValue(moduleId, out var s)) return s as T;
                return null;
            }
        }
        public IReadOnlyList<object> GetAllSettings()
        {
            lock(registryGate)
            {
                var result = new object[settingsData.Count];
                settingsData.Values.CopyTo(result, 0);
                return result;
            }
        }
        
        public SessionLog Log { get; set; }
        
        public void LogInfo(string module, string message) => Log?.Add(module, "Info", message);
        public void LogWarning(string module, string message) => Log?.Add(module, "Warning", message);
        public void LogError(string module, string message, Exception ex = null) => Log?.Add(module, "Error", message + (ex != null ? "\n" + ex.ToString() : ""));
        
        // Returned records are detached copies of the bounded host retention queue. Extensions
        // name one existing sequence when replaying; they never provide callback data.
        public IReadOnlyList<ForgeReplayRecord> Records => forgeWeave.ReplayRecords;
        static Descriptor Copy(Descriptor d)=>new Descriptor { Id=d.Id,Module=d.Module,Name=d.Name,Context=d.Context,ChangesState=d.ChangesState };
        Descriptor Validate(Descriptor d)
        {
            if(d==null || string.IsNullOrWhiteSpace(d.Id) || string.IsNullOrWhiteSpace(d.Module)) throw new ArgumentException("ID and module required");
            if(!Enum.IsDefined(typeof(Context),d.Context))throw new ArgumentException("Invalid extension context");
            if(!ids.Add(d.Id))throw new ArgumentException("Duplicate ID: "+d.Id);
            var frozen=Copy(d);descriptors.Add(frozen.Id,frozen);return frozen;
        }
        public void Register(ITestCase p) { if(p==null)throw new ArgumentNullException(nameof(p));lock(registryGate){var d=Validate(p.Descriptor);tests.Add(d.Id,p);} }
        public void Register(ICommand p) { if(p==null)throw new ArgumentNullException(nameof(p));lock(registryGate){var d=Validate(p.Descriptor);commands.Add(d.Id,p);} }
        public void Register(IDiagnosticProvider p) { if(p==null)throw new ArgumentNullException(nameof(p));lock(registryGate){var d=Validate(p.Descriptor);providers.Add(d.Id,p);} }
        public void Register(IForgeAnalyzer analyzer)
        {
            if(analyzer==null)throw new ArgumentNullException(nameof(analyzer));
            if(analyzer.Descriptor==null)throw new ArgumentException("Analyzer descriptor required.");
            if(analyzer.Descriptor.ChangesState)throw new ArgumentException("Analyzers must be read-only.");
            lock(registryGate){var descriptor=Validate(analyzer.Descriptor);analyzers.Add(descriptor.Id,analyzer);}
        }
        public void Register(ForgeUiPageDescriptor page)
        {
            if(page==null)throw new ArgumentNullException(nameof(page));
            if(string.IsNullOrWhiteSpace(page.Id)||string.IsNullOrWhiteSpace(page.Owner)||string.IsNullOrWhiteSpace(page.Prefab)||page.ViewModelType==null)
                throw new ArgumentException("A UI page requires an ID, owning module, prefab, and ViewModel type.");
            lock(registryGate)
            {
                if(uiPages.ContainsKey(page.Id))throw new ArgumentException("Duplicate UI page ID: "+page.Id);
                uiPages.Add(page.Id,page);
            }
        }
        public IReadOnlyList<ForgeUiPageDescriptor> GetPages()
        {
            lock(registryGate)return uiPages.Values.OrderBy(page=>page.Owner,StringComparer.OrdinalIgnoreCase).ThenBy(page=>page.Id,StringComparer.OrdinalIgnoreCase).ToArray();
        }
        public ForgeUiPageDescriptor FindPage(string id)
        {
            lock(registryGate){ForgeUiPageDescriptor page;return uiPages.TryGetValue(id??"",out page)?page:null;}
        }
        public int RemoveOwner(string owner)
        {
            if(string.IsNullOrWhiteSpace(owner))return 0;
            lock(registryGate){var idsToRemove=uiPages.Where(pair=>string.Equals(pair.Value.Owner,owner,StringComparison.OrdinalIgnoreCase)).Select(pair=>pair.Key).ToArray();foreach(var id in idsToRemove)uiPages.Remove(id);return idsToRemove.Length;}
        }
        public void Clear(){lock(registryGate)uiPages.Clear();}
        public ForgeAnalysisResult Analyze(string analyzerId,ForgeAnalysisRequest request)
        {
            IForgeAnalyzer analyzer;
            lock(registryGate)
            {
                if(!analyzers.TryGetValue(analyzerId??"",out analyzer))
                    throw new ArgumentException("Unknown analyzer: "+(analyzerId??""));
            }
            return analyzer.Analyze(request??new ForgeAnalysisRequest {AnalyzerId=analyzerId});
        }
        public void Register(IPatchBlueprintProvider provider)
        {
            if(provider==null)throw new ArgumentNullException(nameof(provider));
            if(provider.Descriptor==null)throw new ArgumentException("Patch blueprint descriptor required");
            if(provider.Descriptor.ChangesState)throw new ArgumentException("Patch blueprint providers must be read-only");
            lock(registryGate){var descriptor=Validate(provider.Descriptor);patchBlueprintProviders.Add(descriptor.Id,provider);}
        }
        public void Register(IForgeEventHandler handler)
        {
            if(handler==null)throw new ArgumentNullException(nameof(handler));
            if(handler.Subscription==null || handler.Subscription.Descriptor==null)throw new ArgumentException("ForgeWeave subscription descriptor required.");
            lock(registryGate)
            {
                var descriptor=Validate(handler.Subscription.Descriptor);
                try {forgeWeave.Register(handler,descriptor);}
                catch {ids.Remove(descriptor.Id);descriptors.Remove(descriptor.Id);throw;}
            }
        }
        public bool Unregister(string id)
        {
            if(string.IsNullOrWhiteSpace(id))return false;
            lock(registryGate)
            {
                var removed=forgeWeave.Unregister(id);
                if(removed)
                {
                    ids.Remove(id);
                    descriptors.Remove(id);
                }
                return removed;
            }
        }
        public bool Unregister(IForgeEventHandler handler)
        {
            if(handler?.Subscription?.Descriptor?.Id==null)return false;
            return Unregister(handler.Subscription.Descriptor.Id);
        }
        public bool PublishCustom(string topic, IEnumerable<KeyValuePair<string, string>> data = null)
        {
            if (string.IsNullOrWhiteSpace(topic)) return false;
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            if (data != null)
            {
                foreach (var pair in data)
                {
                    if (pair.Key != null) dict[pair.Key] = pair.Value ?? "";
                }
            }
            var services = hostServices ?? NullTestServices.Instance;
            var result = forgeWeave.DispatchCustom(topic, services.CurrentContext, services, 0, dict, CancellationToken.None, AuthorizeForgeEvent);
            return result.InvokedCount > 0;
        }
        // The host calls this only from an official callback on its game thread. The framework
        // itself contains no timer, reflection, patch, or background dispatch mechanism.
        public ForgeWeaveDispatchResult DispatchForgeEvent(ForgeEventKind kind,ITestServices services,double deltaMilliseconds,IDictionary<string,string> data,CancellationToken token)
        {
            if(services==null)throw new ArgumentNullException(nameof(services));
            return DispatchForgeEvent(kind,services.CurrentContext,services,deltaMilliseconds,data,token);
        }
        // Lifecycle transitions preserve their source context even when Bannerlord has already
        // exposed the next context through ITestServices.CurrentContext.
        public ForgeWeaveDispatchResult DispatchForgeEvent(ForgeEventKind kind,Context context,ITestServices services,double deltaMilliseconds,IDictionary<string,string> data,CancellationToken token)
        {
            hostServices = services;
            return forgeWeave.Dispatch(kind,context,services,deltaMilliseconds,data,token,AuthorizeForgeEvent);
        }
        public ForgeWeaveDispatchResult DispatchCustomForgeEvent(string topic,ITestServices services,double deltaMilliseconds,IDictionary<string,string> data,CancellationToken token)
        {
            if(services==null)throw new ArgumentNullException(nameof(services));
            return DispatchCustomForgeEvent(topic,services.CurrentContext,services,deltaMilliseconds,data,token);
        }
        public ForgeWeaveDispatchResult DispatchCustomForgeEvent(string topic,Context context,ITestServices services,double deltaMilliseconds,IDictionary<string,string> data,CancellationToken token)
        {
            hostServices = services;
            return forgeWeave.DispatchCustom(topic,context,services,deltaMilliseconds,data,token,AuthorizeForgeEvent);
        }
        public ForgeWeaveSnapshot CaptureForgeWeave() => forgeWeave.Snapshot();
        public bool ResetForgeWeaveQuarantine(string id) => forgeWeave.ResetQuarantine(id);
        public int ResetAllForgeWeaveQuarantines() => forgeWeave.ResetAllQuarantines();
        public int ResetModuleForgeWeaveQuarantines(string module) => forgeWeave.ResetModuleQuarantines(module);
        public ForgeWeaveHandlerHealth GetForgeWeaveHandlerHealth(string id) => forgeWeave.GetHandlerHealth(id);
        public void ClearForgeWeaveJournal() => forgeWeave.ClearJournal();
        public void ClearForgeWeaveReplays() => forgeWeave.ClearReplays();
        public ForgeReplayResult Replay(long sequence,ITestServices services,CancellationToken cancellation)
        {
            return ReplayForgeEvent(sequence,services,cancellation);
        }
        public ForgeReplayResult ReplayForgeEvent(long sequence,ITestServices services,CancellationToken cancellation)
        {
            if(sequence<1)throw new ArgumentOutOfRangeException(nameof(sequence),"Replay sequence must be positive.");
            if(services==null)throw new ArgumentNullException(nameof(services));
            return forgeWeave.Replay(sequence,services,cancellation,AuthorizeForgeEvent);
        }
        // Providers run only when the game thread explicitly asks for a preflight capture.
        // The capture contains inert, copied declarations and does not apply a patch.
        public PatchBlueprintCapture CapturePatchBlueprints(ITestServices services,CancellationToken token)
        {
            if(services==null)throw new ArgumentNullException(nameof(services));
            const int maximumTotal=200,maximumPerProvider=100;
            KeyValuePair<string,IPatchBlueprintProvider>[] registered;
            Dictionary<string,Descriptor> registeredDescriptors;
            lock(registryGate)
            {
                registered=patchBlueprintProviders.OrderBy(pair=>pair.Key,StringComparer.Ordinal).ToArray();
                registeredDescriptors=registered.ToDictionary(pair=>pair.Key,pair=>Copy(descriptors[pair.Key]),StringComparer.OrdinalIgnoreCase);
            }
            var capture=new PatchBlueprintCapture {ProviderCount=registered.Length};
            foreach(var pair in registered)
            {
                if(token.IsCancellationRequested) {capture.Truncated=true;AddBlueprintFinding(capture,"Info","patch_blueprint_cancelled",registeredDescriptors[pair.Key].Module,"Patch blueprint capture was cancelled.","Run the preflight again when the session is responsive.");break;}
                var descriptor=registeredDescriptors[pair.Key];
                if(descriptor.Context!=Context.Any && descriptor.Context!=services.CurrentContext)
                {
                    AddBlueprintFinding(capture,"Info","patch_blueprint_context",descriptor.Module,"Patch blueprint provider requires "+descriptor.Context+" context.","Enter the required game context and run the preflight again.");
                    continue;
                }
                try
                {
                    var described=pair.Value.Describe(new PatchBlueprintRequest(services,token));
                    if(described==null)continue;
                    var local=0;
                    foreach(var blueprint in described)
                    {
                        if(token.IsCancellationRequested) {capture.Truncated=true;AddBlueprintFinding(capture,"Info","patch_blueprint_cancelled",descriptor.Module,"Patch blueprint capture was cancelled.","Run the preflight again when the session is responsive.");break;}
                        if(local++>=maximumPerProvider || capture.Declarations.Count>=maximumTotal)
                        {
                            capture.Truncated=true;
                            AddBlueprintFinding(capture,"Warning","patch_blueprint_limit",descriptor.Module,"Patch blueprint capture reached its safety limit.","Split large blueprint providers into focused groups.");
                            break;
                        }
                        if(blueprint==null) {AddBlueprintFinding(capture,"Warning","patch_blueprint_null",descriptor.Module,"A provider returned an empty blueprint.","Return a complete declaration or omit it.");continue;}
                        capture.Declarations.Add(new PatchBlueprintDeclaration {ProviderId=descriptor.Id,Module=descriptor.Module,ProviderName=descriptor.Name,Context=descriptor.Context,Blueprint=PatchBlueprintCopies.Copy(blueprint)});
                    }
                }
                catch(OperationCanceledException)
                {
                    capture.Truncated=true;
                    AddBlueprintFinding(capture,"Info","patch_blueprint_cancelled",descriptor.Module,"Patch blueprint capture was cancelled.","Run the preflight again when the session is responsive.");
                    break;
                }
                catch(Exception error)
                {
                    AddBlueprintFinding(capture,"Warning","patch_blueprint_provider_error",descriptor.Module,"Patch blueprint provider '"+descriptor.Id+"' failed: "+error.Message,"Keep providers brief and return declarations only.");
                }
                if(capture.Truncated)break;
            }
            return capture;
        }
        static void AddBlueprintFinding(PatchBlueprintCapture capture,string level,string code,string module,string message,string suggestion)
        {
            if(capture.Findings.Count<100)capture.Findings.Add(new Finding {Level=level,Code=code,Module=module,Message=message,Suggestion=suggestion});
        }
        void Authorize(Descriptor d,ITestServices s) => Authorize(d,s,s?.CurrentContext??Context.Any);
        void Authorize(Descriptor d,ITestServices s,Context context)
        {
            if(d.Context!=Context.Any && d.Context!=context) throw new InvalidOperationException("Wrong context");
            bool enabled,copyConfirmed;
            lock(registryGate) {enabled=testingEnabled;copyConfirmed=campaignCopyConfirmed;}
            if(d.ChangesState && (!enabled || (s.IsCampaignActive && !copyConfirmed))) throw new InvalidOperationException("Enable testing and confirm campaign copy");
        }
        string AuthorizeForgeEvent(ForgeEventSubscription subscription,ITestServices services,Context context)
        {
            if(subscription==null || subscription.Descriptor==null)return "ForgeWeave subscription descriptor is unavailable.";
            try {Authorize(subscription.Descriptor,services,context);return null;}
            catch(Exception error) {return error.Message;}
        }
        public TestResult Execute(string id,ITestServices services,int seed,CancellationToken token)
        {
            ITestCase p;Descriptor descriptor;
            lock(registryGate)
            {
                if(!tests.TryGetValue(id,out p)) throw new ArgumentException("Unknown test");
                descriptor=Copy(descriptors[id]);
            }
            Authorize(descriptor,services);
            var r=new TestResult { Id=id, Seed=seed,Context=services.CurrentContext.ToString(),StartedAt=DateTime.UtcNow.ToString("O") };
            var e=new TestExecution(services,seed,token); var stopwatch=Stopwatch.StartNew();
            try { token.ThrowIfCancellationRequested(); p.Prepare(e); token.ThrowIfCancellationRequested(); p.Execute(e); token.ThrowIfCancellationRequested(); p.Verify(e); token.ThrowIfCancellationRequested(); r.Status="Passed"; }
            catch(OperationCanceledException) { r.Status="Cancelled"; }
            catch(Exception ex) { r.Status="Failed"; r.Error=ex.ToString(); }
            finally { try { p.Cleanup(e); } catch(Exception ex) { r.CleanupError=ex.ToString(); r.Status="Failed"; } stopwatch.Stop(); r.Milliseconds=stopwatch.Elapsed.TotalMilliseconds; r.Steps=e.Steps; }
            try { services.Register(descriptor.Module,r.Status=="Passed"?"Info":"Warning",id+": "+r.Status); }
            catch(Exception ex) { r.Steps.Add("Log callback failed: "+ex.Message); }
            return r;
        }
        public string ExecuteCommand(string id,string argument,ITestServices s,CancellationToken token)
        {
            ICommand command;Descriptor descriptor;
            lock(registryGate)
            {
                if(!commands.TryGetValue(id,out command))throw new ArgumentException("Unknown command");
                descriptor=Copy(descriptors[id]);
            }
            Authorize(descriptor,s);token.ThrowIfCancellationRequested();return command.Execute(new TestExecution(s,148,token),argument);
        }
        public List<TestResult> ExecuteBatch(IEnumerable<string> testIds,ITestServices services,int seed,CancellationToken token)
        {
            if(testIds==null)throw new ArgumentNullException(nameof(testIds));
            var selected=testIds.Take(51).ToArray();
            if(selected.Length==0 || selected.Length>50)throw new ArgumentException("Select between 1 and 50 tests");
            // Validate the complete selection before any test can change game state.
            var selectedDescriptors=new List<Descriptor>();
            lock(registryGate)
            {
                foreach(var id in selected)
                {
                    if(id==null || !tests.ContainsKey(id))throw new ArgumentException("Unknown test: "+id);
                    selectedDescriptors.Add(Copy(descriptors[id]));
                }
            }
            foreach(var descriptor in selectedDescriptors)Authorize(descriptor,services);
            var output=new List<TestResult>();
            foreach(var id in selected) {if(token.IsCancellationRequested)break;var result=Execute(id,services,seed,token);output.Add(result);if(result.Status!="Passed")break;}
            return output;
        }
        public List<Finding> Diagnose(ITestServices s)
        {
            var output=new List<Finding>();
            KeyValuePair<IDiagnosticProvider,Descriptor>[] registered;
            lock(registryGate)registered=providers.OrderBy(pair=>pair.Key,StringComparer.Ordinal).Select(pair=>new KeyValuePair<IDiagnosticProvider,Descriptor>(pair.Value,Copy(descriptors[pair.Key]))).ToArray();
            foreach(var pair in registered) try { Authorize(pair.Value,s); output.AddRange(pair.Key.Inspect(new TestExecution(s,148,CancellationToken.None)).Take(1000)); } catch(Exception e) { output.Add(new Finding {Level="Warning", Code="extension_error", Module=pair.Value.Module, Message=e.Message}); }
            return output;
        }
        
        public bool Supports(ForgeRuntimeCapability capability) => false;
        public string DescribeUnavailable(ForgeRuntimeCapability capability) =>
            "The current Forge host does not implement "+capability+". Check ForgeApi.RuntimeCapabilities before calling this service.";
        ForgeCapabilityUnavailableException Unavailable(ForgeRuntimeCapability capability) =>
            new ForgeCapabilityUnavailableException(capability,DescribeUnavailable(capability));

        // These interfaces are present for source compatibility. Returning harmless-looking
        // defaults hid missing host implementations and led to invalid mods, so each call now
        // fails explicitly with a discoverable capability and never changes game state.
        public bool IsKeyDown(ForgeInputKey key) { throw Unavailable(ForgeRuntimeCapability.Input); }
        public bool IsKeyPressed(ForgeInputKey key) { throw Unavailable(ForgeRuntimeCapability.Input); }
        public bool IsKeyReleased(ForgeInputKey key) { throw Unavailable(ForgeRuntimeCapability.Input); }

        public void SyncData<T>(string id, ref T data) { throw Unavailable(ForgeRuntimeCapability.SaveSerialization); }
        public void RegisterClassDefinition(Type type, int localSaveId) { throw Unavailable(ForgeRuntimeCapability.SaveSerialization); }
        public void RegisterStructDefinition(Type type, int localSaveId) { throw Unavailable(ForgeRuntimeCapability.SaveSerialization); }
        public void RegisterContainerDefinition(Type type) { throw Unavailable(ForgeRuntimeCapability.SaveSerialization); }

        public void RenderDebugDirectionArrow(float[] position, float[] direction, uint color) { throw Unavailable(ForgeRuntimeCapability.DebugRendering); }
        public void RenderDebugText(string text, float[] position, uint color) { throw Unavailable(ForgeRuntimeCapability.DebugRendering); }

        public IForgeAgent MainAgent { get { throw Unavailable(ForgeRuntimeCapability.AgentInspection); } }
        public IReadOnlyList<IForgeAgent> GetActiveAgents() { throw Unavailable(ForgeRuntimeCapability.AgentInspection); }

        sealed class NullTestServices : ITestServices
        {
            public static readonly NullTestServices Instance = new NullTestServices();
            public Context CurrentContext => Context.Any;
            public bool IsCampaignActive => false;
            public object GetService(Type type) => null;
            public void Register(string module, string level, string message) { }
        }
    }
    public static class SnapshotComparer
    {
        public static string Compare(ObjectSnapshot before,ObjectSnapshot after)
        {
            if(before==null) return "Before snapshot is missing";
            if(after==null) return "Object no longer available";
            if(before.Type!=after.Type || before.Id!=after.Id) throw new ArgumentException("Different objects");
            return string.Join(Environment.NewLine,before.Properties.Keys.Union(after.Properties.Keys).Where(k=>Value(before,k)!=Value(after,k)).Select(k=>k+": "+Value(before,k)+" → "+Value(after,k)));
        }
        static string Value(ObjectSnapshot o,string k) => o.Properties.TryGetValue(k,out var v)?v:"—";
    }
}
