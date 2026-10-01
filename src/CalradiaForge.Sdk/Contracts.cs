using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using CalradiaForge.Sdk.Patcher;

namespace CalradiaForge.Sdk
{
    // Game-independent contracts allow extensions to be tested outside Bannerlord.
    public enum Context { Any, Campaign, Mission }
    public sealed class Descriptor
    {
        public string Id { get; set; }
        public string Module { get; set; }
        public string Name { get; set; }
        public Context Context { get; set; }
        public bool ChangesState { get; set; }
    }
    public sealed class Finding
    {
        public string Level { get; set; } = "Info";
        public string Code { get; set; }
        public string Module { get; set; }
        public string File { get; set; }
        public string Message { get; set; }
        public string Suggestion { get; set; }
    }
    public interface ITestServices
    {
        Context CurrentContext { get; }
        bool IsCampaignActive { get; }
        object GetService(Type type);
        void Register(string module, string level, string message);
    }
    public sealed class TestExecution
    {
        public ITestServices Services { get; }
        public CancellationToken Cancellation { get; }
        public int Seed { get; }
        public Random Random { get; }
        public List<string> Steps { get; } = new List<string>();
        public TestExecution(ITestServices services, int seed, CancellationToken cancellation)
        { Services = services; Seed = seed; Random = new Random(seed); Cancellation = cancellation; }
        public void Verify(bool condition, string description)
        { Cancellation.ThrowIfCancellationRequested(); Steps.Add(description); if (!condition) throw new InvalidOperationException(description); }
    }
    public interface ITestCase
    {
        Descriptor Descriptor { get; }
        void Prepare(TestExecution execution);
        void Execute(TestExecution execution);
        void Verify(TestExecution execution);
        void Cleanup(TestExecution execution);
    }
    public interface ICommand { Descriptor Descriptor { get; } string Execute(TestExecution execution, string argument); }
    public interface IDiagnosticProvider { Descriptor Descriptor { get; } IEnumerable<Finding> Inspect(TestExecution execution); }
    public interface IForgeRegistry
    {
        void Register(ITestCase test);
        void Register(ICommand command);
        void Register(IDiagnosticProvider provider);
    }

    /// <summary>Describes the outcome of automatic extension discovery.</summary>
    public enum ForgeAutoRegisterState
    {
        Completed,
        CompletedWithErrors,
        HostUnavailable
    }

    /// <summary>A bounded diagnostic for one failed automatic registration.</summary>
    public sealed class ForgeAutoRegisterFailure
    {
        internal ForgeAutoRegisterFailure(string typeName, string contract, string reason)
        {
            TypeName = typeName;
            Contract = contract;
            Reason = reason;
        }

        /// <summary>Gets the discovered type name, truncated to a bounded length.</summary>
        public string TypeName { get; }

        /// <summary>Gets the failed contract or registration stage.</summary>
        public string Contract { get; }

        /// <summary>Gets a bounded diagnostic that excludes exception payloads and paths.</summary>
        public string Reason { get; }
    }

    /// <summary>Immutable summary of one explicit assembly auto-registration pass.</summary>
    public sealed class ForgeAutoRegisterReport
    {
        internal ForgeAutoRegisterReport(
            ForgeAutoRegisterState state,
            int scannedTypeCount,
            int candidateCount,
            int registeredCount,
            int failureCount,
            int omittedFailureCount,
            IList<ForgeAutoRegisterFailure> failures)
        {
            State = state;
            ScannedTypeCount = scannedTypeCount;
            CandidateCount = candidateCount;
            RegisteredCount = registeredCount;
            FailureCount = failureCount;
            OmittedFailureCount = omittedFailureCount;
            Failures = new ReadOnlyCollection<ForgeAutoRegisterFailure>(failures.ToArray());
        }

        /// <summary>Gets the final discovery state.</summary>
        public ForgeAutoRegisterState State { get; }

        /// <summary>Gets the number of types inspected.</summary>
        public int ScannedTypeCount { get; }

        /// <summary>Gets the number of individual registration candidates.</summary>
        public int CandidateCount { get; }

        /// <summary>Gets the number of registrations completed successfully.</summary>
        public int RegisteredCount { get; }

        /// <summary>Gets the total number of registration or discovery failures.</summary>
        public int FailureCount { get; }

        /// <summary>Gets how many failures were omitted from the bounded detail list.</summary>
        public int OmittedFailureCount { get; }

        /// <summary>Gets a read-only list containing at most 64 failure details.</summary>
        public IReadOnlyList<ForgeAutoRegisterFailure> Failures { get; }
    }

    // A patch blueprint describes intent only. Forge never applies, removes, or reorders a patch.
    // It deliberately uses no Harmony, TaleWorlds, or other mod types so authors can validate
    // their intended target before choosing an implementation dependency.
    public enum PatchHookKind { Prefix, Postfix, Transpiler, Finalizer }
    public enum PatchMemberKind { Method, Constructor }
    public sealed class TypeReference
    {
        public string AssemblyName { get; set; }
        public string FullName { get; set; }
        public static TypeReference From(Type type)
        {
            if(type==null)return null;
            var genericOwner=type.IsGenericParameter?(type.DeclaringMethod?.DeclaringType??type.DeclaringType):null;
            var assembly=type.IsGenericParameter?(genericOwner?.Assembly??type.Module?.Assembly):type.Assembly;
            var fullName=type.IsGenericParameter
                ? (type.DeclaringMethod==null?"!":"!!")+type.GenericParameterPosition
                : type.FullName??type.Name;
            return new TypeReference {AssemblyName=assembly?.GetName().Name,FullName=fullName};
        }
    }
    public sealed class MethodReference
    {
        public string AssemblyName { get; set; }
        public string DeclaringType { get; set; }
        public PatchMemberKind MemberKind { get; set; } = PatchMemberKind.Method;
        public string MemberName { get; set; }
        public int GenericArity { get; set; }
        public List<TypeReference> ParameterTypes { get; set; } = new List<TypeReference>();
        public TypeReference ReturnType { get; set; }
        public bool? IsStatic { get; set; }
        public static MethodReference From(MethodBase method)
        {
            if(method==null)return null;
            method=NormalizeMethod(method);
            var info=method as MethodInfo;
            return new MethodReference {
                AssemblyName=method.DeclaringType?.Assembly.GetName().Name,
                DeclaringType=method.DeclaringType?.FullName,
                MemberKind=method.IsConstructor?PatchMemberKind.Constructor:PatchMemberKind.Method,
                MemberName=method.IsConstructor?null:method.Name,
                GenericArity=info!=null && info.IsGenericMethod?info.GetGenericArguments().Length:0,
                ParameterTypes=new List<TypeReference>(Array.ConvertAll(method.GetParameters(),parameter=>TypeReference.From(parameter.ParameterType))),
                ReturnType=info==null?null:TypeReference.From(info.ReturnType),
                IsStatic=method.IsStatic
            };
        }

        // A closed generic MethodInfo describes one constructed invocation, while a
        // blueprint identifies the reusable declaration that reflection can resolve
        // from the loaded assembly. Convert both closed method arguments and a closed
        // generic declaring type back to their definitions before capturing its shape.
        static MethodBase NormalizeMethod(MethodBase method)
        {
            var info=method as MethodInfo;
            if(info!=null && info.IsGenericMethod && !info.IsGenericMethodDefinition)
                method=info.GetGenericMethodDefinition();

            var declaringType=method.DeclaringType;
            if(declaringType==null || !declaringType.IsConstructedGenericType)return method;

            try
            {
                var definition=declaringType.GetGenericTypeDefinition();
                const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly;
                IEnumerable<MethodBase> candidates;
                if(method.IsConstructor && method.IsStatic)
                {
                    var initializer=definition.TypeInitializer;
                    candidates=initializer==null?Enumerable.Empty<MethodBase>():new MethodBase[]{initializer};
                }
                else candidates=method.IsConstructor
                    ? definition.GetConstructors(flags).Cast<MethodBase>()
                    : definition.GetMethods(flags).Cast<MethodBase>();
                var token=method.MetadataToken;
                var module=method.Module;
                var matches=candidates.Where(candidate=>candidate.MetadataToken==token && candidate.Module==module).ToList();
                if(matches.Count==1)return matches[0];
            }
            catch(Exception error)
            {
                throw new ArgumentException("Cannot capture a member declared on a constructed generic type. Pass the member from its generic type definition so MethodReference.From can preserve the open signature.",nameof(method),error);
            }

            throw new ArgumentException("Cannot uniquely map a member declared on a constructed generic type to its definition. Pass the member from the generic type definition so MethodReference.From can preserve the open signature.",nameof(method));
        }
    }
    public sealed class PatchBlueprint
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public PatchHookKind Hook { get; set; }
        public MethodReference Target { get; set; }
        public MethodReference PatchMethod { get; set; }
        public int? Priority { get; set; }
        public List<string> Before { get; set; } = new List<string>();
        public List<string> After { get; set; } = new List<string>();
        public string Rationale { get; set; }
    }
    public sealed class PatchBlueprintRequest
    {
        public ITestServices Services { get; }
        public CancellationToken Cancellation { get; }
        public PatchBlueprintRequest(ITestServices services,CancellationToken cancellation)
        { Services=services??throw new ArgumentNullException(nameof(services));Cancellation=cancellation; }
    }
    public interface IPatchBlueprintProvider
    {
        Descriptor Descriptor { get; }
        IEnumerable<PatchBlueprint> Describe(PatchBlueprintRequest request);
    }
    // This additive registry intentionally sits beside IForgeRegistry to preserve SDK v1 consumers.
    public interface IPatchBlueprintRegistry { void Register(IPatchBlueprintProvider provider); }

    // ForgeWeave is an event framework, not a method-patching API. The host raises only
    // events it receives through documented game callbacks, and extensions receive the
    // supplied event on that same host thread.
    public enum ForgeEventKind
    {
        ForgeReady,
        InitialScreenReady,
        ContextEntering,
        ContextLeaving,
        CampaignStarted,
        MissionInitialized,
        AgentCreated,
        AgentRemoved,
        Pulse,
        GameLoaded,
        MissionEnded,
        Custom
    }
    public enum ForgeEventAccess { Observe, Diagnostics, CampaignWrite, MissionWrite }
    public enum ForgeBudgetPolicy { Ignore, Warn }
    public enum ForgeCircuitBreakerPolicy { AutoRecover, PermanentQuarantine }
    // Replay is opt-in per handler. ObserveOnly is reserved for handlers that cannot change
    // game state; Live still goes through the host's normal context and writer authorization.
    public enum ForgeReplayMode { Disabled, ObserveOnly, Live }
    // Filters are declarative and bounded. Forge compares copied scalar event data before it
    // authorizes or invokes a handler; extensions never supply a callback predicate to the host.
    public sealed class ForgeEventFilter
    {
        public Dictionary<string,string> RequiredData { get; set; } = new Dictionary<string,string>();
        public Dictionary<string,string> ExcludedData { get; set; } = new Dictionary<string,string>();
    }
    public sealed class ForgeEventSubscription
    {
        // Descriptor.Id is the stable, globally unique subscription ID.
        public Descriptor Descriptor { get; set; }
        public ForgeEventKind Event { get; set; }
        // Topic string for Custom events. Supports exact match or trailing wildcard '*' (e.g. "mymod.*").
        public string Topic { get; set; } = string.Empty;
        // Larger priorities run first. Before/After can only refine equal priorities.
        public int Priority { get; set; }
        public List<string> Before { get; set; } = new List<string>();
        public List<string> After { get; set; } = new List<string>();
        public ForgeEventAccess Access { get; set; } = ForgeEventAccess.Observe;
        // Disabled is deliberately the safe default. A retained host event never invokes an
        // extension again until that extension explicitly declares a replay mode.
        public ForgeReplayMode ReplayMode { get; set; } = ForgeReplayMode.Disabled;
        // An empty filter matches every event of the declared kind. Required pairs use exact
        // ordinal comparison, including during a retained replay.
        public ForgeEventFilter Filter { get; set; } = new ForgeEventFilter();
        // Zero disables the budget. Warn records an over-budget outcome without aborting or
        // quarantining user code; Ignore keeps the measurement out of budget findings.
        public int BudgetMilliseconds { get; set; }
        public ForgeBudgetPolicy BudgetPolicy { get; set; } = ForgeBudgetPolicy.Warn;
        // The host bounds this to a small range. Repeated failures quarantine the handler.
        public int FailureLimit { get; set; } = 3;
        // Circuit breaker policy. AutoRecover allows probationary HalfOpen probes after a cooldown period.
        public ForgeCircuitBreakerPolicy CircuitBreakerPolicy { get; set; } = ForgeCircuitBreakerPolicy.AutoRecover;
        // Base cooldown in seconds before a quarantined handler attempts self-recovery (min 1, max 300, default 5).
        public int CircuitBreakerCooldownSeconds { get; set; } = 5;
        // Pulse handlers must opt into a bounded interval so a forgotten callback cannot run
        // once per game frame. Other event kinds use zero because they are host-driven.
        public int MinimumIntervalMilliseconds { get; set; }
    }
    public sealed class ForgeEvent
    {
        bool propagationStopped;
        public ForgeEventKind Kind { get; private set; }
        public string Topic { get; private set; } = string.Empty;
        public Context Context { get; private set; }
        public long Sequence { get; private set; }
        public double DeltaMilliseconds { get; private set; }
        // Hosts can attach only copied, bounded scalar data. ForgeWeave never exposes game
        // objects that an extension could retain outside the callback.
        public IReadOnlyDictionary<string,string> Data { get; private set; }
        public CancellationToken Cancellation { get; private set; }
        public DateTime RaisedAt { get; private set; }
        // Replayed events retain their own dispatch sequence while naming the original retained
        // host event that supplied their copied scalar data.
        public bool IsReplay { get; private set; }
        public long? SourceSequence { get; private set; }
        public bool PropagationStopped => propagationStopped;
        public string StopReason { get; private set; }
        public ForgeEvent(ForgeEventKind kind,ITestServices services,long sequence,double deltaMilliseconds,IEnumerable<KeyValuePair<string,string>> data,CancellationToken cancellation,string topic = null)
        {
            if(services==null)throw new ArgumentNullException(nameof(services));
            Initialize(kind,services.CurrentContext,sequence,deltaMilliseconds,data,cancellation,false,null,topic);
        }
        // A host may preserve the logical source context for a lifecycle transition even when
        // the live game service has already entered the next context. This overload still
        // accepts only copied scalar data and exposes no service or game object to handlers.
        public ForgeEvent(ForgeEventKind kind,Context context,long sequence,double deltaMilliseconds,IEnumerable<KeyValuePair<string,string>> data,CancellationToken cancellation,string topic = null)
        {
            Initialize(kind,context,sequence,deltaMilliseconds,data,cancellation,false,null,topic);
        }
        // This overload is for a host replaying a record it retained itself. It does not expose
        // a way for an extension to inject data into the host's replay registry.
        public ForgeEvent(ForgeEventKind kind,Context context,long sequence,double deltaMilliseconds,IEnumerable<KeyValuePair<string,string>> data,CancellationToken cancellation,bool isReplay,long? sourceSequence,string topic = null)
        {
            Initialize(kind,context,sequence,deltaMilliseconds,data,cancellation,isReplay,sourceSequence,topic);
        }
        void Initialize(ForgeEventKind kind,Context context,long sequence,double deltaMilliseconds,IEnumerable<KeyValuePair<string,string>> data,CancellationToken cancellation,bool isReplay,long? sourceSequence,string topic = null)
        {
            if(!Enum.IsDefined(typeof(ForgeEventKind),kind))throw new ArgumentException("Invalid ForgeWeave event.",nameof(kind));
            if(!Enum.IsDefined(typeof(Context),context))throw new ArgumentException("Invalid ForgeWeave context.",nameof(context));
            if(isReplay && (!sourceSequence.HasValue || sourceSequence.Value<1))throw new ArgumentException("A replay event requires a retained source sequence.",nameof(sourceSequence));
            if(!isReplay && sourceSequence.HasValue)throw new ArgumentException("Only replay events can declare a source sequence.",nameof(sourceSequence));
            Kind=kind;Context=context;Sequence=sequence;Topic=Bound(topic??"",128);
            DeltaMilliseconds=double.IsNaN(deltaMilliseconds)||double.IsInfinity(deltaMilliseconds)||deltaMilliseconds<0?0:Math.Min(deltaMilliseconds,60000);
            Data=CopyData(data);Cancellation=cancellation;RaisedAt=DateTime.UtcNow;IsReplay=isReplay;SourceSequence=sourceSequence;
        }
        // This only stops later ForgeWeave handlers for the current event. It never blocks,
        // cancels, or changes the game's original callback.
        public void StopPropagation(string reason)
        {
            propagationStopped=true;
            reason=reason??"";
            StopReason=reason.Length>256?reason.Substring(0,256)+"…":reason;
        }
        static IReadOnlyDictionary<string,string> CopyData(IEnumerable<KeyValuePair<string,string>> source)
        {
            var values=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var pair in source??new KeyValuePair<string,string>[0])
            {
                if(values.Count>=32)break;
                var key=Bound(pair.Key,64);
                if(key.Length==0 || values.ContainsKey(key))continue;
                values.Add(key,Bound(pair.Value,512));
            }
            return new ReadOnlyDictionary<string,string>(values);
        }
        static string Bound(string value,int maximum)
        {
            value=value??"";return value.Length<=maximum?value:value.Substring(0,maximum)+"…";
        }
    }
    public interface IForgeEventHandler
    {
        ForgeEventSubscription Subscription { get; }
        void Handle(ForgeEvent @event);
    }
    // An additive registry keeps the original IForgeRegistry source-compatible.
    public interface IForgeEventRegistry
    {
        /// <summary>Registers a handler synchronously on the host connection thread.</summary>
        /// <remarks>If this call throws, consumers cannot assume the host made no change. Implementations should validate before mutation; callers must treat the outcome as uncertain unless the host contract proves rollback.</remarks>
        void Register(IForgeEventHandler handler);
        bool Unregister(string id);
        /// <summary>Unregisters the handler by its subscription ID; this is not an identity-based rollback operation.</summary>
        bool Unregister(IForgeEventHandler handler);
        bool PublishCustom(string topic, IEnumerable<KeyValuePair<string, string>> data = null);
    }
    // A replay registry enumerates only copies of records retained by the host. Replay accepts
    // a sequence rather than event data, so extensions cannot inject arbitrary callbacks.
    public interface IForgeReplayRegistry
    {
        IReadOnlyList<ForgeReplayRecord> Records { get; }
        ForgeReplayResult Replay(long sequence,ITestServices services,CancellationToken cancellation);
    }
    
    public interface IForgeSettingsRegistry
    {
        void Register<T>(string moduleId, string displayName, T defaultSettings) where T : class;
        T GetSettings<T>(string moduleId) where T : class;
        IReadOnlyList<object> GetAllSettings();
    }
    
    public interface IForgeLogger
    {
        void LogInfo(string module, string message);
        void LogWarning(string module, string message);
        void LogError(string module, string message, Exception ex = null);
    }
    public sealed class ForgeHandlerOutcome
    {
        public string HandlerId { get; set; }
        public string Module { get; set; }
        public ForgeReplayMode ReplayMode { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public double Milliseconds { get; set; }
        public bool BudgetExceeded { get; set; }
    }
    // Values returned to callers are detached copies. The engine retains its own bounded copy,
    // so changing a record returned from this property can never alter a later replay.
    public sealed class ForgeReplayRecord
    {
        public long Sequence { get; set; }
        public ForgeEventKind Event { get; set; }
        public Context Context { get; set; }
        public double DeltaMilliseconds { get; set; }
        public string RaisedAt { get; set; }
        public Dictionary<string,string> Data { get; set; } = new Dictionary<string,string>();
        public string OriginalStatus { get; set; }
        public int OriginalInvokedCount { get; set; }
        public int OriginalSkippedCount { get; set; }
        public int OriginalFailureCount { get; set; }
        public bool OriginalPropagationStopped { get; set; }
        public string OriginalStopReason { get; set; }
        public double OriginalMilliseconds { get; set; }
        public string DispatchedAt { get; set; }
        public List<ForgeHandlerOutcome> OriginalHandlerOutcomes { get; set; } = new List<ForgeHandlerOutcome>();
    }
    public sealed class ForgeReplayResult
    {
        public long SourceSequence { get; set; }
        public long? ReplaySequence { get; set; }
        public ForgeEventKind Event { get; set; }
        public Context Context { get; set; }
        public bool Replayed { get; set; }
        public string Status { get; set; }
        public string RejectionReason { get; set; }
        public int InvokedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailureCount { get; set; }
        public int BudgetExceededCount { get; set; }
        public int QuarantinedCount { get; set; }
        public bool PropagationStopped { get; set; }
        public string StopReason { get; set; }
        public double Milliseconds { get; set; }
        public List<ForgeHandlerOutcome> HandlerOutcomes { get; set; } = new List<ForgeHandlerOutcome>();
    }
    public enum ForgeInputKey { LeftMouseButton, RightMouseButton, MiddleMouseButton, Space, Enter, Escape, W, A, S, D, Q, E, R, F, Z, X, C, V, LeftShift, LeftControl, LeftAlt, Tab, Up, Down, Left, Right }
    public interface IForgeInput { bool IsKeyDown(ForgeInputKey key); bool IsKeyPressed(ForgeInputKey key); bool IsKeyReleased(ForgeInputKey key); }

    public interface IForgeSaveManager
    {
        void SyncData<T>(string id, ref T data);
        void RegisterClassDefinition(Type type, int localSaveId);
        void RegisterStructDefinition(Type type, int localSaveId);
        void RegisterContainerDefinition(Type type);
    }

    public interface IForgeDebug
    {
        void RenderDebugDirectionArrow(float[] position, float[] direction, uint color);
        void RenderDebugText(string text, float[] position, uint color);
    }
    
    public enum ForgeControllerType { None, Player, AI }
    public interface IForgeAgent
    {
        bool IsMainAgent { get; }
        ForgeControllerType Controller { get; }
        float Health { get; }
        float Age { get; }
        bool IsActive { get; }
    }
    public interface IForgeAgentManager
    {
        IForgeAgent MainAgent { get; }
        IReadOnlyList<IForgeAgent> GetActiveAgents();
    }

    public static class ForgeApi
    {
        public const int Version = 13;
        static readonly object availabilityGate=new object();
        static readonly object connectionGate=new object();
        static IForgeRegistry registry;
        static Action<IForgeRegistry,long,int> registryChanged;
        static long registryGeneration;
        static int registryThreadId;
        static readonly Dictionary<Action<IForgeRegistry>, ManagedAvailabilitySubscription> managedAvailabilitySubscribers =
            new Dictionary<Action<IForgeRegistry>, ManagedAvailabilitySubscription>();
        sealed class ManagedAvailabilitySubscription
        {
            internal readonly Action<IForgeRegistry> Callback;
            internal readonly Action<IForgeRegistry> Handler;
            internal readonly object DeliveryGate = new object();

            internal ManagedAvailabilitySubscription(Action<IForgeRegistry> callback)
            {
                Callback = callback;
                Handler = OnAvailabilityEvent;
            }

            private void OnAvailabilityEvent(IForgeRegistry availableRegistry)
            {
                // Connect recognizes this wrapper and supplies the captured generation.
                // It must never invoke managed callbacks without that lifecycle check.
            }
        }
        static IPatchBlueprintRegistry patchBlueprints;
        static IForgePatchService patches;
        static IForgeHookService hooks;
        static IForgeEventRegistry events;
        static IForgeReplayRegistry replays;
        static IForgeSettingsRegistry settings;
        static IForgeLogger logger;
        static IForgeInput input;
        static IForgeSaveManager saveManager;
        static IForgeDebug debug;
        static IForgeAgentManager agentManager;
        static IForgeAnalysisRegistry analyses;
        static IForgeRuntimeCapabilities runtimeCapabilities;
        static IForgeUiRegistry ui;
        static SharedLibraryRegistry libraries;
        public static IForgeRegistry Registry { get {lock(availabilityGate)return registry;} }
        public static IPatchBlueprintRegistry PatchBlueprints { get {lock(availabilityGate)return patchBlueprints;} }
        /// <summary>Gets the optional explicit method-patching capability when the connected host provides it.</summary>
        /// <remarks>Check this property at runtime; <see cref="Version"/> is a compile-time constant and is not a capability probe.</remarks>
        public static IForgePatchService Patches { get {lock(availabilityGate)return patches;} }
        /// <summary>Gets the optional explicitly managed Prefix/Postfix/Finalizer hook capability.</summary>
        /// <remarks>Check this capability at runtime. Hook registration is inert until an explicit host Apply operation succeeds. ILContext transpiler authoring is available only through the Core net472 adapter and is not part of this SDK contract.</remarks>
        public static IForgeHookService Hooks { get {lock(availabilityGate)return hooks;} }
        public static IForgeEventRegistry Events { get {lock(availabilityGate)return events;} }
        public static bool PublishCustomEvent(string topic, IEnumerable<KeyValuePair<string, string>> data = null) => Events?.PublishCustom(topic, data) ?? false;
        public static IForgeReplayRegistry Replays { get {lock(availabilityGate)return replays;} }
        public static IForgeSettingsRegistry Settings { get {lock(availabilityGate)return settings;} }
        public static IForgeLogger Logger { get {lock(availabilityGate)return logger;} }
        public static IForgeInput Input { get {lock(availabilityGate)return input;} }
        public static IForgeSaveManager SaveManager { get {lock(availabilityGate)return saveManager;} }
        public static IForgeDebug Debug { get {lock(availabilityGate)return debug;} }
        public static IForgeAgentManager AgentManager { get {lock(availabilityGate)return agentManager;} }
        public static IForgeAnalysisRegistry Analyses { get {lock(availabilityGate)return analyses;} }
        public static IForgeRuntimeCapabilities RuntimeCapabilities { get {lock(availabilityGate)return runtimeCapabilities;} }
        /// <summary>Gets the standalone Gauntlet extension page catalog when the game host is connected.</summary>
        public static IForgeUiRegistry UI { get {lock(availabilityGate)return ui;} }
        public static SharedLibraryRegistry Libraries { get {lock(availabilityGate)return libraries;} }
        public static ForgeModelRegistry Models { get; } = new ForgeModelRegistry();
        public static class Diplomacy
        {
            public static float CalculateWarScore(int militaryStrength, int gold, int activeWars, int tributeReceived, int tributePaid) =>
                ForgeDiplomacy.CalculateWarScore(militaryStrength, gold, activeWars, tributeReceived, tributePaid);
            public static int CalculatePeaceTribute(int casualtiesInflicted, int casualtiesSuffered, int settlementsCaptured, int settlementsLost) =>
                ForgeDiplomacy.CalculatePeaceTribute(casualtiesInflicted, casualtiesSuffered, settlementsCaptured, settlementsLost);
            public static float EvaluateAllianceStability(int relation, int commonEnemies, int geographicDistanceFactor) =>
                ForgeDiplomacy.EvaluateAllianceStability(relation, commonEnemies, geographicDistanceFactor);
        }
        public static class Settlements
        {
            public static float CalculateDailyLoyaltyDelta(bool cultureMismatch, bool governorCultureMatch, int foodSurplus, int taxes, int corruption) =>
                ForgeSettlementSystem.CalculateDailyLoyaltyDelta(cultureMismatch, governorCultureMatch, foodSurplus, taxes, corruption);
            public static float CalculateDailySecurityDelta(int garrisonSize, int militiaSize, int activeUnderworldRackets, bool banditLairNearby) =>
                ForgeSettlementSystem.CalculateDailySecurityDelta(garrisonSize, militiaSize, activeUnderworldRackets, banditLairNearby);
            public static (bool isCritical, float dangerIndex, string status) EvaluateRebellionRisk(float currentLoyalty, int militiaSize, int garrisonSize) =>
                ForgeSettlementSystem.EvaluateRebellionRisk(currentLoyalty, militiaSize, garrisonSize);
        }
        public static class Underworld
        {
            public static (int dailyGold, float dailyCrimeGain) CalculateAlleyDailyYield(int thugCount, int settlementProsperity, float securityLevel) =>
                ForgeUnderworldSystem.CalculateAlleyDailyYield(thugCount, settlementProsperity, securityLevel);
            public static (int netProfitPerUnit, float riskFactor) CalculateSmugglingMargin(int purchasePrice, int destinationPrice, float tariffRate, float borderGuardBribery) =>
                ForgeUnderworldSystem.CalculateSmugglingMargin(purchasePrice, destinationPrice, tariffRate, borderGuardBribery);
            public static float CalculateCrimeDecay(float currentCrimeRating, int settlementSecurity, bool hasActiveRackets) =>
                ForgeUnderworldSystem.CalculateCrimeDecay(currentCrimeRating, settlementSecurity, hasActiveRackets);
        }
        public static class Progression
        {
            public static float CalculateLearningRate(int attributeValue, int focusPoints, int currentSkillLevel) =>
                ForgeProgressionSystem.CalculateLearningRate(attributeValue, focusPoints, currentSkillLevel);
            public static int GetClanTierThreshold(int tier) =>
                ForgeProgressionSystem.GetClanTierThreshold(tier);
            public static int EvaluateClanTier(float currentRenown) =>
                ForgeProgressionSystem.EvaluateClanTier(currentRenown);
            public static bool DoesPerkApplyToRole(string perkId, string role) =>
                ForgeProgressionSystem.DoesPerkApplyToRole(perkId, role);
            public static float EvaluateDynasticSuccession(int age, int leadership, int martialSkill, int relationWithLords) =>
                ForgeProgressionSystem.EvaluateDynasticSuccession(age, leadership, martialSkill, relationWithLords);
        }
        public static class CasusBelli
        {
            public static ForgeCasusBelliEngine.CasusBelliAnalysis EvaluateWarJustification(
                string attackerKingdom, string targetKingdom, int attackerStrength, int targetStrength,
                int borderingSettlements, bool hasMarriagePact, int pastRaidsCount, int diplomaticRelation) =>
                ForgeCasusBelliEngine.EvaluateWarJustification(attackerKingdom, targetKingdom, attackerStrength, targetStrength, borderingSettlements, hasMarriagePact, pastRaidsCount, diplomaticRelation);
        }
        public static class Siege
        {
            public static ForgeSiegeTactician.SiegeAssaultAnalysis AnalyzeAssaultTactics(
                string settlementName, int attackers, int defenders, bool hasRam, int towerCount, int wallBreaches) =>
                ForgeSiegeTactician.AnalyzeAssaultTactics(settlementName, attackers, defenders, hasRam, towerCount, wallBreaches);
        }
        public static class Combat
        {
            public static float CalculateMoraleShock(int casualtiesInflicted, int initialTroopCount, bool isFlanked, bool isCommanderKilled) =>
                ForgeCombatTactics.CalculateMoraleShock(casualtiesInflicted, initialTroopCount, isFlanked, isCommanderKilled);
            public static float CalculateChargeDistanceThreshold(int cavalryCount, int enemyInfantryBracing, bool hasSpears) =>
                ForgeCombatTactics.CalculateChargeDistanceThreshold(cavalryCount, enemyInfantryBracing, hasSpears);
        }
        public static class Trade
        {
            public static (bool isValid, int allowedAmount, string reason) ValidateItemTransfer(int currentCount, int requestedAmount) =>
                ForgeTradeSystem.ValidateItemTransfer(currentCount, requestedAmount);
            public static int CalculatePriceBySupplyDemand(int basePrice, int localSupply, int idealDemand) =>
                ForgeTradeSystem.CalculatePriceBySupplyDemand(basePrice, localSupply, idealDemand);
            public static int CalculateWorkshopDailyNet(int rawMaterialUnitCost, int outputUnitPrice, int productionVolume, int dailyWageOverhead) =>
                ForgeTradeSystem.CalculateWorkshopDailyNet(rawMaterialUnitCost, outputUnitPrice, productionVolume, dailyWageOverhead);
        }
        public static class Parties
        {
            public static ForgePartyBlueprint CreateBlueprint(string partyId, string name, string factionId) =>
                ForgePartySpawner.CreateBlueprint(partyId, name, factionId);
        }
        public static class Hud
        {
            public static (bool isVisibleOnScreen, float screenX, float screenY, float depth) ProjectWorldToScreen(
                float worldX, float worldY, float worldZ,
                float camX, float camY, float camZ,
                float camPitchRad, float camYawRad,
                float fovDeg, int screenWidth, int screenHeight) =>
                ForgeHudProjector.ProjectWorldToScreen(worldX, worldY, worldZ, camX, camY, camZ, camPitchRad, camYawRad, fovDeg, screenWidth, screenHeight);
            public static float CalculateTrackDecay(float terrainDecayMultiplier, int partySize, float daysPassed) =>
                ForgeHudProjector.CalculateTrackDecay(terrainDecayMultiplier, partySize, daysPassed);
        }
        public static event Action<IForgeRegistry> Available;
        public static event Action<string> UiPagesRemoved;
        // Register atomically with the current availability check. This avoids losing a
        // handler when Forge connects between a module's check and event subscription.
        public static void RegisterWhenAvailable(Action<IForgeRegistry> subscriber)
        {
            if(subscriber==null)throw new ArgumentNullException(nameof(subscriber));
            ManagedAvailabilitySubscription subscription;
            IForgeRegistry current;
            long generation;
            lock(availabilityGate)
            {
                ManagedAvailabilitySubscription previous;
                if(managedAvailabilitySubscribers.TryGetValue(subscriber,out previous))
                {
                    Available-=previous.Handler;
                    managedAvailabilitySubscribers.Remove(subscriber);
                }
                subscription=new ManagedAvailabilitySubscription(subscriber);
                managedAvailabilitySubscribers.Add(subscriber,subscription);
                Available+=subscription.Handler;
                current=registry;
                generation=registryGeneration;
            }
            if(current!=null)TryDeliverAvailability(subscription,current,generation);
        }
        public static void UnregisterWhenAvailable(Action<IForgeRegistry> subscriber)
        {
            if(subscriber==null)return;
            ManagedAvailabilitySubscription subscription;
            lock(availabilityGate)
            {
                if(!managedAvailabilitySubscribers.TryGetValue(subscriber,out subscription))return;
                managedAvailabilitySubscribers.Remove(subscriber);
                Available-=subscription.Handler;
            }
            // Wait for a delivery that already passed its generation/lifetime check. The
            // callback runs outside availabilityGate; Monitor reentrancy permits a callback
            // to unregister itself without deadlocking.
            lock(subscription.DeliveryGate) { }
        }
        private static bool TryDeliverAvailability(ManagedAvailabilitySubscription subscription,IForgeRegistry expectedRegistry,long expectedGeneration)
        {
            lock(subscription.DeliveryGate)
            {
                lock(availabilityGate)
                {
                    ManagedAvailabilitySubscription current;
                    if(registryGeneration!=expectedGeneration || !ReferenceEquals(registry,expectedRegistry) ||
                        !managedAvailabilitySubscribers.TryGetValue(subscription.Callback,out current) ||
                        !ReferenceEquals(current,subscription))return false;
                }
                subscription.Callback(expectedRegistry);
                return true;
            }
        }
        private static bool IsCurrentAvailabilityGeneration(IForgeRegistry expectedRegistry,long expectedGeneration)
        {
            lock(availabilityGate)
                return registryGeneration==expectedGeneration && ReferenceEquals(registry,expectedRegistry);
        }
        // Internal lifecycle subscribers receive both connection and disconnection changes.
        // The generation lets callbacks ignore an older snapshot delivered after a newer host.
        internal static void RegisterRegistryChangeListener(Action<IForgeRegistry,long,int> subscriber)
        {
            if(subscriber==null)throw new ArgumentNullException(nameof(subscriber));
            IForgeRegistry current;
            long generation;
            int connectionThreadId;
            lock(availabilityGate)
            {
                registryChanged-=subscriber;
                registryChanged+=subscriber;
                current=registry;
                generation=registryGeneration;
                connectionThreadId=registryThreadId;
            }
            subscriber(current,generation,connectionThreadId);
        }
        internal static void UnregisterRegistryChangeListener(Action<IForgeRegistry,long,int> subscriber)
        {
            if(subscriber==null)return;
            lock(availabilityGate)registryChanged-=subscriber;
        }
        internal static bool IsRegistryConnectionThread(IForgeRegistry expectedRegistry)
        {
            lock(availabilityGate)
                return expectedRegistry!=null && ReferenceEquals(registry,expectedRegistry) && registryThreadId==Thread.CurrentThread.ManagedThreadId;
        }
        /// <summary>Removes declarative Gauntlet pages owned by a module and notifies the host to close an active page.</summary>
        public static int UnregisterUiPages(string moduleId)
        {
            if (string.IsNullOrWhiteSpace(moduleId)) return 0;
            var current = UI;
            var removed = current?.RemoveOwner(moduleId) ?? 0;
            if (removed > 0) UiPagesRemoved?.Invoke(moduleId);
            return removed;
        }
        public static void Connect(IForgeRegistry registry)
        {
            lock(connectionGate) ConnectCore(registry);
        }

        /// <summary>Disconnects Forge during host unload while keeping raw patch applications closed if cleanup is rejected.</summary>
        /// <remarks>Unlike an ordinary disconnect retry, unload must not reopen application routes after teardown begins.</remarks>
        public static void DisconnectForUnload()
        {
            lock(connectionGate) DisconnectCore(true);
        }

        static void ConnectCore(IForgeRegistry registry)
        {
            if(registry==null)throw new ArgumentNullException(nameof(registry));
            IForgeRegistry priorRegistry;
            IForgePatchService priorPatches;
            IForgeHookService priorHooks;
            lock(availabilityGate)
            {
                priorRegistry=ForgeApi.registry;
                priorPatches=patches;
                priorHooks=hooks;
            }
            if(!ReferenceEquals(priorRegistry,registry))
            {
                EnsureHookDisconnectAllowed(priorHooks, "replace the Forge registry");
                ForgeDetour.StopAcceptingApplications();
                try
                {
                    priorHooks?.Disconnect();
                    var unresolvedHooks=UnresolvedHooks(priorHooks);
                    if(unresolvedHooks.Length>0)
                        throw new InvalidOperationException("The previous Forge hook service retains active or uncertain hooks: "+string.Join(", ",unresolvedHooks.Select(snapshot=>snapshot?.Id??"<invalid>"))+". Resolve them before replacing the registry.");
                    // Some composite registries (for example TestEngine) expose both
                    // optional services and perform both cleanups in one Disconnect call.
                    if(priorPatches!=null && !ReferenceEquals(priorHooks,priorPatches)) priorPatches.Disconnect();
                    var unresolvedPatches=UnresolvedPatches(priorPatches);
                    if(unresolvedPatches.Length>0)
                        throw new InvalidOperationException("The previous Forge patch service retains active or uncertain patches: "+string.Join(", ",unresolvedPatches.Select(snapshot=>snapshot?.PatchId??"<invalid>"))+". Resolve them before replacing the registry.");
                    try { ForgeDetour.UnpatchAll(); }
                    catch { /* Exact status remains in ForgeDetour snapshots; do not retry during this transition. */ }
                    ForgePatcher.RemoveVerifiedRevertedRecords();
                    var outstanding=ForgeDetour.GetTrackedSnapshots();
                    if(outstanding.Count>0)
                        throw new InvalidOperationException("The previous Forge connection retains patch conflicts or uncertain detours. Resolve them before replacing the registry.");
                }
                catch(Exception transitionError)
                {
                    // Reconnect is idempotent for a service that never completed its
                    // disconnect. Attempt it even when active snapshots remain; a service
                    // that did disconnect must prove it can safely reopen, while a conflict
                    // keeps raw applications closed and the original exception visible.
                    var restoreErrors=new List<Exception>();
                    bool lifecyclesRestored=ReconnectServices(priorPatches,priorHooks,restoreErrors);
                    bool canResumeApplications=lifecyclesRestored && restoreErrors.Count==0 &&
                        ForgeDetour.GetTrackedSnapshots().Count==0;
                    if(canResumeApplications) ForgeDetour.AllowApplications();
                    if(restoreErrors.Count>0 && UnresolvedPatches(priorPatches).Length==0 && UnresolvedHooks(priorHooks).Length==0)
                    {
                        restoreErrors.Insert(0,transitionError);
                        throw new AggregateException("The previous Forge host remains published, but one or more clean services could not be restored after connection replacement failed.",restoreErrors);
                    }
                    throw;
                }
            }
            // Reopen the incoming host's optional service only after its prior records prove
            // cleanly reverted. Do this before publishing it or reopening direct detours.
            var incomingPatchLifecycle=registry as IForgePatchServiceLifecycle;
            var incomingHookLifecycle=registry as IForgeHookServiceLifecycle;
            bool incomingPatchesAttempted=false;
            bool incomingHooksAttempted=false;
            try
            {
                if(incomingPatchLifecycle!=null)
                {
                    incomingPatchesAttempted=true;
                    if(incomingHookLifecycle!=null && ReferenceEquals(incomingPatchLifecycle,incomingHookLifecycle))
                        incomingHooksAttempted=true;
                    incomingPatchLifecycle.Reconnect();
                }
                if(incomingHookLifecycle!=null && !ReferenceEquals(incomingPatchLifecycle,incomingHookLifecycle))
                {
                    incomingHooksAttempted=true;
                    incomingHookLifecycle.Reconnect();
                }
            }
            catch(Exception reconnectError)
            {
                // A replacement failure must not leave the previous, already-cleaned host
                // published with its lifecycle closed and raw applications paused.
                if(!ReferenceEquals(priorRegistry,registry)&&priorRegistry!=null)
                {
                    var rollbackErrors=new List<Exception>();
                    IForgeHookService incomingHookService=registry as IForgeHookService;
                    IForgePatchService incomingPatchService=registry as IForgePatchService;
                    if(incomingHooksAttempted)
                    {
                        try { incomingHookService?.Disconnect(); }
                        catch(Exception error) { rollbackErrors.Add(error); }
                    }
                    bool sharedLifecycleAttempt=incomingPatchesAttempted&&incomingHooksAttempted&&
                        ReferenceEquals(incomingPatchLifecycle,incomingHookLifecycle);
                    if(incomingPatchesAttempted && !(sharedLifecycleAttempt&&ReferenceEquals(incomingPatchService,incomingHookService)))
                    {
                        try { incomingPatchService?.Disconnect(); }
                        catch(Exception error) { rollbackErrors.Add(error); }
                    }
                    ReconnectLifecycles(priorRegistry as IForgePatchServiceLifecycle,
                        priorRegistry as IForgeHookServiceLifecycle,rollbackErrors);
                    if(rollbackErrors.Count==0) ForgeDetour.AllowApplications();
                    else
                    {
                        rollbackErrors.Insert(0,reconnectError);
                        throw new AggregateException("The incoming Forge host failed to reconnect and the previous host could not be fully restored.",rollbackErrors);
                    }
                }
                throw;
            }
            SharedLibraryRegistry previous;
            Action<IForgeRegistry> subscribers;
            Action<IForgeRegistry,long,int> lifecycleSubscribers;
            long generation;
            int connectionThreadId;
            lock(availabilityGate)
            {
                ForgeDetour.AllowApplications();
                previous=libraries;
                libraries=new SharedLibraryRegistry();
                ForgeApi.registry=registry;
                patchBlueprints=registry as IPatchBlueprintRegistry;
                patches=registry as IForgePatchService;
                hooks=registry as IForgeHookService;
                events=registry as IForgeEventRegistry;
                replays=registry as IForgeReplayRegistry;
                settings=registry as IForgeSettingsRegistry;
                logger=registry as IForgeLogger;
                input=registry as IForgeInput;
                saveManager=registry as IForgeSaveManager;
                debug=registry as IForgeDebug;
                agentManager=registry as IForgeAgentManager;
                analyses=registry as IForgeAnalysisRegistry;
                runtimeCapabilities=registry as IForgeRuntimeCapabilities;
                ui=registry as IForgeUiRegistry;
                subscribers=Available;
                lifecycleSubscribers=registryChanged;
                generation=++registryGeneration;
                registryThreadId=Thread.CurrentThread.ManagedThreadId;
                connectionThreadId=registryThreadId;
            }
            previous?.Dispose();
            if(subscribers==null && lifecycleSubscribers==null)return;
            var errors=new List<Exception>();
            if(lifecycleSubscribers!=null)
            {
                foreach(var listener in lifecycleSubscribers.GetInvocationList().Cast<Action<IForgeRegistry,long,int>>())
                {
                    try { listener(registry,generation,connectionThreadId); }
                    catch(Exception ex) { errors.Add(ex); }
                }
            }
            if(subscribers!=null)
            {
                foreach(var s in subscribers.GetInvocationList().Cast<Action<IForgeRegistry>>())
                {
                    var managed=s.Target as ManagedAvailabilitySubscription;
                    try
                    {
                        if(managed!=null)TryDeliverAvailability(managed,registry,generation);
                        else s(registry);
                    }
                    catch(Exception ex) { errors.Add(ex); }
                    if(managed!=null && !IsCurrentAvailabilityGeneration(registry,generation))break;
                }
            }
            if(errors.Count>0)throw new AggregateException("One or more ForgeWeave subscribers threw an exception during initialization.",errors);
        }
        /// <summary>Discovers supported extension contracts in the explicitly supplied assembly only.</summary>
        /// <param name="assembly">The extension assembly to inspect.</param>
        /// <param name="moduleId">The owning Bannerlord module ID used to validate page resources.</param>
        public static void AutoRegister(System.Reflection.Assembly assembly, string moduleId = null)
        {
            var report=AutoRegisterWithReport(assembly,moduleId);
            var currentLogger=Logger;
            if(currentLogger==null)return;
            for(var i=0;i<report.Failures.Count;i++)
            {
                var failure=report.Failures[i];
                currentLogger.LogError(moduleId ?? "ForgeApi",
                    $"Auto-registration failed for {failure.TypeName} ({failure.Contract}): {failure.Reason}",
                    new InvalidOperationException(failure.Reason));
            }
        }

        /// <summary>Discovers supported contracts in one explicit assembly and returns bounded results.</summary>
        /// <param name="assembly">The extension assembly to inspect.</param>
        /// <param name="moduleId">The owning Bannerlord module ID used to validate page resources.</param>
        /// <returns>An immutable report including successful registrations and bounded failures.</returns>
        public static ForgeAutoRegisterReport AutoRegisterWithReport(System.Reflection.Assembly assembly, string moduleId = null)
        {
            if(assembly==null)throw new ArgumentNullException(nameof(assembly));
            IForgeRegistry targetRegistry;
            IForgeUiRegistry targetUi;
            IForgeAnalysisRegistry targetAnalyses;
            IForgeEventRegistry targetEvents;
            lock(availabilityGate)
            {
                targetRegistry=registry;
                targetUi=ui;
                targetAnalyses=analyses;
                targetEvents=events;
            }

            var failures=new List<ForgeAutoRegisterFailure>();
            var failureCount=0;
            var omittedFailureCount=0;
            const int maximumFailureDetails=64;
            Action<string,string,string> addFailure=(typeName,contract,reason)=>
            {
                failureCount++;
                if(failures.Count<maximumFailureDetails)
                {
                    failures.Add(new ForgeAutoRegisterFailure(
                        BoundText(typeName,180),BoundText(contract,48),BoundText(reason,240)));
                }
                else omittedFailureCount++;
            };

            if(targetRegistry==null)
            {
                addFailure("<assembly>","host","No Forge registry is connected.");
                return new ForgeAutoRegisterReport(ForgeAutoRegisterState.HostUnavailable,0,0,0,
                    failureCount,omittedFailureCount,failures);
            }

            Type[] types;
            try
            {
                types=assembly.GetTypes();
            }
            catch(ReflectionTypeLoadException error)
            {
                types=error.Types==null?Array.Empty<Type>():error.Types.Where(type=>type!=null).ToArray();
                var loaderErrors=error.LoaderExceptions;
                var reportedLoaderErrors=0;
                if(loaderErrors!=null)
                {
                    for(var loaderIndex=0;loaderIndex<loaderErrors.Length;loaderIndex++)
                    {
                        var loaderError=loaderErrors[loaderIndex];
                        if(loaderError==null)continue;
                        reportedLoaderErrors++;
                        var loadError=loaderError as TypeLoadException;
                        addFailure(
                            loadError==null?"<assembly>":BoundText(loadError.TypeName,180),
                            "type-load",
                            "Type loading failed ("+loaderError.GetType().Name+").");
                    }
                }
                if(reportedLoaderErrors==0)
                    addFailure("<assembly>","type-load","One or more assembly types could not be loaded.");
            }
            catch(Exception error)
            {
                addFailure("<assembly>","type-load","Assembly discovery failed ("+error.GetType().Name+").");
                return new ForgeAutoRegisterReport(ForgeAutoRegisterState.CompletedWithErrors,0,0,0,
                    failureCount,omittedFailureCount,failures);
            }

            var candidateCount=0;
            var registeredCount=0;
            for(var i=0;i<types.Length;i++)
            {
                var type=types[i];
                if(type==null || type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)continue;

                bool isUiPage;
                try
                {
                    isUiPage=type.GetCustomAttributes(typeof(ForgeUiPageAttribute),false).Length>0;
                }
                catch(Exception error)
                {
                    addFailure(type.FullName,"metadata","Type metadata could not be inspected ("+error.GetType().Name+").");
                    continue;
                }

                if(isUiPage)
                {
                    candidateCount++;
                    if(targetUi==null)
                    {
                        addFailure(type.FullName,"ForgeUiPage","The connected host does not expose the Forge UI registry.");
                    }
                    else
                    {
                        try
                        {
                            targetUi.Register(ForgeUiDiscovery.Describe(type,moduleId));
                            registeredCount++;
                        }
                        catch(Exception error)
                        {
                            addFailure(type.FullName,"ForgeUiPage","Registration failed ("+error.GetType().Name+").");
                        }
                    }
                }

                string contract=null;
                if(typeof(ITestCase).IsAssignableFrom(type))contract="ITestCase";
                else if(typeof(ICommand).IsAssignableFrom(type))contract="ICommand";
                else if(typeof(IDiagnosticProvider).IsAssignableFrom(type))contract="IDiagnosticProvider";
                else if(typeof(IForgeAnalyzer).IsAssignableFrom(type))contract="IForgeAnalyzer";
                else if(typeof(IForgeEventHandler).IsAssignableFrom(type))contract="IForgeEventHandler";
                if(contract==null)continue;
                candidateCount++;

                if(type.GetConstructor(Type.EmptyTypes)==null)
                {
                    addFailure(type.FullName,contract,"A public parameterless constructor is required.");
                    continue;
                }

                try
                {
                    var instance=Activator.CreateInstance(type);
                    switch(contract)
                    {
                        case "ITestCase": targetRegistry.Register((ITestCase)instance); break;
                        case "ICommand": targetRegistry.Register((ICommand)instance); break;
                        case "IDiagnosticProvider": targetRegistry.Register((IDiagnosticProvider)instance); break;
                        case "IForgeAnalyzer":
                            if(targetAnalyses==null)throw new InvalidOperationException("The host does not expose an analyzer registry.");
                            targetAnalyses.Register((IForgeAnalyzer)instance); break;
                        case "IForgeEventHandler":
                            if(targetEvents==null)throw new InvalidOperationException("The host does not expose an event registry.");
                            targetEvents.Register((IForgeEventHandler)instance); break;
                    }
                    registeredCount++;
                }
                catch(Exception error)
                {
                    var cause=error is TargetInvocationException && error.InnerException!=null?error.InnerException:error;
                    addFailure(type.FullName,contract,"Registration failed ("+cause.GetType().Name+").");
                }
            }

            var state=failureCount==0?ForgeAutoRegisterState.Completed:ForgeAutoRegisterState.CompletedWithErrors;
            return new ForgeAutoRegisterReport(state,types.Length,candidateCount,registeredCount,
                failureCount,omittedFailureCount,failures);
        }

        private static string BoundText(string value,int maximumLength)
        {
            if(string.IsNullOrEmpty(value))return "Unknown";
            return value.Length<=maximumLength?value:value.Substring(0,maximumLength);
        }
        public static void Disconnect()
        {
            lock(connectionGate) DisconnectCore();
        }

        static void DisconnectCore(bool keepApplicationsClosedOnFailure = false)
        {
            IForgeRegistry priorRegistry;
            IForgePatchService previousPatches;
            IForgeHookService previousHooks;
            lock(availabilityGate)
            {
                priorRegistry=registry;
                previousPatches=patches;
                previousHooks=hooks;
            }

            // Do not unpublish the hook capability until its teardown has completed and the
            // service reports no active or uncertain records. If cleanup throws or leaves a
            // conflict, clients must retain a route to inspect and recover the service.
            if(keepApplicationsClosedOnFailure) ForgeDetour.StopAcceptingApplications();
            EnsureHookDisconnectAllowed(previousHooks, "disconnect ForgeApi");
            ForgeDetour.StopAcceptingApplications();
            SharedLibraryRegistry previous;
            IForgeUiRegistry previousUi;
            Action<IForgeRegistry,long,int> lifecycleSubscribers;
            long generation;
            int connectionThreadId;
            try
            {
                previousHooks?.Disconnect();
                var unresolvedHooks=UnresolvedHooks(previousHooks);
                if(unresolvedHooks.Length>0)
                    throw new InvalidOperationException("Forge hook disconnection retained active or uncertain hooks: "+string.Join(", ",unresolvedHooks.Select(snapshot=>snapshot?.Id??"<invalid>"))+". The hook service remains published for recovery.");

                // Patch cleanup also precedes unpublishing. Keep its capability available when
                // a conflict or uncertain write still needs explicit inspection and recovery.
                if(previousPatches!=null && !ReferenceEquals(previousHooks,previousPatches)) previousPatches.Disconnect();
                var unresolvedPatches=UnresolvedPatches(previousPatches);
                if(unresolvedPatches.Length>0)
                    throw new InvalidOperationException("Forge patch disconnection retained active or uncertain patches: "+string.Join(", ",unresolvedPatches.Select(snapshot=>snapshot?.PatchId??"<invalid>"))+". The patch service remains published for recovery.");
            }
            catch(Exception transitionError)
            {
                // Reconnect is idempotent when a service rejected cleanup before changing
                // its accepting state. If a service did disconnect and cannot reopen due to
                // a conflict, preserve the original error and leave raw applications closed.
                var restoreErrors=new List<Exception>();
                bool lifecyclesRestored=ReconnectServices(previousPatches,previousHooks,restoreErrors);
                bool canResumeApplications=!keepApplicationsClosedOnFailure && lifecyclesRestored && restoreErrors.Count==0 &&
                    ForgeDetour.GetTrackedSnapshots().Count==0;
                if(canResumeApplications) ForgeDetour.AllowApplications();
                else
                {
                    if(restoreErrors.Count>0 && UnresolvedPatches(previousPatches).Length==0 && UnresolvedHooks(previousHooks).Length==0)
                    {
                        restoreErrors.Insert(0,transitionError);
                        throw new AggregateException("Forge disconnection was not published; one or more clean services could not be restored, so raw patch applications remain closed.",restoreErrors);
                    }
                }
                throw;
            }

            lock(availabilityGate)
            {
                // The connection gate serializes Connect/Disconnect, but verify the snapshot
                // before clearing public capability references so unexpected mutation cannot
                // detach a service that was not the one just cleaned up.
                if(!ReferenceEquals(registry,priorRegistry)||!ReferenceEquals(hooks,previousHooks))
                    throw new InvalidOperationException("The Forge connection changed while disconnect cleanup was running; published services were retained.");
                previous=libraries;
                previousUi=ui;
                connectionThreadId=registryThreadId;
                libraries=null;patchBlueprints=null;patches=null;hooks=null;events=null;replays=null;settings=null;logger=null;input=null;saveManager=null;debug=null;agentManager=null;analyses=null;runtimeCapabilities=null;ui=null;registry=null;
                registryThreadId=0;
                UiPagesRemoved=null;
                lifecycleSubscribers=registryChanged;
                generation=++registryGeneration;
            }
            var errors=new List<Exception>();
            try { previous?.Dispose(); }
            catch(Exception ex) { errors.Add(ex); }
            ForgeDetour.UnpatchAllForLifecycle();
            try { ForgePatcher.RemoveVerifiedRevertedRecords(); }
            catch(Exception ex) { errors.Add(ex); }
            try { previousUi?.Clear(); }
            catch(Exception ex) { errors.Add(ex); }
            if(lifecycleSubscribers==null)
            {
                if(errors.Count>0)throw new AggregateException("One or more Forge services failed during disconnection.",errors);
                return;
            }
            foreach(var listener in lifecycleSubscribers.GetInvocationList().Cast<Action<IForgeRegistry,long,int>>())
            {
                try { listener(null,generation,connectionThreadId); }
                catch(Exception ex) { errors.Add(ex); }
            }
            if(errors.Count>0)throw new AggregateException("One or more Forge services or lifecycle subscribers failed during disconnection.",errors);
        }

        static void EnsureHookDisconnectAllowed(IForgeHookService service,string operation)
        {
            var guard=service as IForgeHookServiceDisconnectGuard;
            string reason;
            if(guard!=null&&!guard.CanDisconnect(out reason))
                throw new InvalidOperationException("Cannot "+operation+" safely: "+(string.IsNullOrWhiteSpace(reason)?"active or uncertain hooks remain":reason));
        }

        static ForgeHookSnapshot[] UnresolvedHooks(IForgeHookService service)
        {
            if(service==null)return Array.Empty<ForgeHookSnapshot>();
            return (service.GetSnapshots()??Array.Empty<ForgeHookSnapshot>())
                .Where(snapshot=>snapshot==null||snapshot.State==ForgeHookState.Applied||snapshot.State==ForgeHookState.Conflict||snapshot.State==ForgeHookState.Failed)
                .ToArray();
        }

        static ForgePatchSnapshot[] UnresolvedPatches(IForgePatchService service)
        {
            if(service==null)return Array.Empty<ForgePatchSnapshot>();
            return (service.GetSnapshots()??Array.Empty<ForgePatchSnapshot>())
                .Where(snapshot=>snapshot==null||snapshot.State==ForgePatchState.Applied||snapshot.State==ForgePatchState.Conflict||snapshot.State==ForgePatchState.Failed)
                .ToArray();
        }

        static bool ReconnectServices(IForgePatchService patchService,IForgeHookService hookService,IList<Exception> errors)
        {
            IForgePatchServiceLifecycle patchLifecycle=patchService as IForgePatchServiceLifecycle;
            IForgeHookServiceLifecycle hookLifecycle=hookService as IForgeHookServiceLifecycle;
            bool restored=(patchService==null||patchLifecycle!=null)&&(hookService==null||hookLifecycle!=null);
            return ReconnectLifecycles(patchLifecycle,hookLifecycle,errors)&&restored;
        }

        static bool ReconnectLifecycles(IForgePatchServiceLifecycle patchLifecycle,IForgeHookServiceLifecycle hookLifecycle,IList<Exception> errors)
        {
            bool restored=true;
            if(patchLifecycle!=null)
            {
                try { patchLifecycle.Reconnect(); }
                catch(Exception error) { errors?.Add(error); restored=false; }
            }
            // Hosts such as TestEngine implement both lifecycle interfaces on the same
            // object; one idempotent call restores both halves and avoids duplicate work.
            if(hookLifecycle!=null && !ReferenceEquals(patchLifecycle,hookLifecycle))
            {
                try { hookLifecycle.Reconnect(); }
                catch(Exception error) { errors?.Add(error); restored=false; }
            }
            return restored;
        }
    }
}
