using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace CalradiaForge.Sdk
{
    public enum CampaignEventKind
    {
        OnHeroCreated,
        OnHeroKilled,
        OnHeroPrisonerTaken,
        OnSettlementEntered,
        OnCharacterCreationIsOver,
        OnGameLoaded,
        OnNewGameCreated,
        OnDailyTick
    }

    /// <summary>
    /// Wraps Bannerlord CampaignEvents in ForgeEvent subscriptions.
    /// All dispatch errors are logged via ForgeApi.Logger so modders can diagnose
    /// callback failures in the Calradia Forge log viewer instead of losing them silently.
    /// </summary>
    public static class ForgeCampaignEvents
    {
        internal static readonly ConcurrentDictionary<ForgeEvent, List<Action<object[]>>> Subscribers =
            new ConcurrentDictionary<ForgeEvent, List<Action<object[]>>>();

        public static void Subscribe(ForgeEvent eventType, Action<object[]> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var list = Subscribers.GetOrAdd(eventType, _ => new List<Action<object[]>>());
            lock (list)
            {
                list.Add(callback);
            }
        }

        public static bool Unsubscribe(ForgeEvent eventType, Action<object[]> callback)
        {
            if (callback == null) return false;
            if (Subscribers.TryGetValue(eventType, out var list))
            {
                lock (list)
                {
                    return list.Remove(callback);
                }
            }
            return false;
        }

        public static void ClearSubscribers()
        {
            Subscribers.Clear();
        }

        /// <summary>
        /// Subscribes a delegate handler to the ForgeWeave event mesh with execution budgets and filtering.
        /// </summary>
        public static IForgeEventHandler SubscribeWeave(
            string handlerId,
            ForgeEventKind kind,
            Action<ForgeEvent> handler,
            int priority = 0,
            int budgetMs = 10,
            int minIntervalMs = 0,
            ForgeEventAccess access = ForgeEventAccess.Observe,
            ForgeReplayMode replayMode = ForgeReplayMode.Disabled,
            IDictionary<string, string> requiredData = null,
            IDictionary<string, string> excludedData = null)
        {
            var eventHandler = CreateWeaveHandler(handlerId, kind, handler, priority, budgetMs, minIntervalMs, access, replayMode, requiredData, excludedData);
            var registry = ForgeApi.Events;
            if (registry == null)
                throw new InvalidOperationException("ForgeWeave cannot register a handler because no connected host exposes an event registry.");
            if (!ForgeApi.IsRegistryConnectionThread(ForgeApi.Registry))
                throw new InvalidOperationException("ForgeWeave handlers must be registered on the connected host's connection thread.");
            registry.Register(eventHandler);
            return eventHandler;
        }

        /// <summary>
        /// Subscribes to ForgeWeave when a compatible host is connected and keeps the handler
        /// registered across host reconnections until the returned handle is disposed.
        /// </summary>
        public static ForgeWeaveRegistration SubscribeWeaveWhenAvailable(
            string handlerId,
            ForgeEventKind kind,
            Action<ForgeEvent> handler,
            int priority = 0,
            int budgetMs = 10,
            int minIntervalMs = 0,
            ForgeEventAccess access = ForgeEventAccess.Observe,
            ForgeReplayMode replayMode = ForgeReplayMode.Disabled,
            IDictionary<string, string> requiredData = null,
            IDictionary<string, string> excludedData = null)
        {
            var eventHandler = CreateWeaveHandler(handlerId, kind, handler, priority, budgetMs, minIntervalMs, access, replayMode, requiredData, excludedData);
            var handle = new ForgeWeaveRegistration(eventHandler);
            handle.Start();
            return handle;
        }

        static DelegateForgeEventHandler CreateWeaveHandler(
            string handlerId,
            ForgeEventKind kind,
            Action<ForgeEvent> handler,
            int priority,
            int budgetMs,
            int minIntervalMs,
            ForgeEventAccess access,
            ForgeReplayMode replayMode,
            IDictionary<string, string> requiredData,
            IDictionary<string, string> excludedData)
        {
            if (string.IsNullOrWhiteSpace(handlerId)) throw new ArgumentNullException(nameof(handlerId));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var subscription = new ForgeEventSubscription
            {
                Descriptor = new Descriptor { Id = handlerId, Module = handlerId, Name = handlerId },
                Event = kind,
                Priority = priority,
                Access = access,
                ReplayMode = replayMode,
                BudgetMilliseconds = budgetMs,
                MinimumIntervalMilliseconds = minIntervalMs
            };

            if (requiredData != null)
            {
                foreach (var kvp in requiredData)
                    subscription.Filter.RequiredData[kvp.Key] = kvp.Value;
            }
            if (excludedData != null)
            {
                foreach (var kvp in excludedData)
                    subscription.Filter.ExcludedData[kvp.Key] = kvp.Value;
            }

            return new DelegateForgeEventHandler(subscription, handler);
        }

        /// <summary>
        /// Unregisters a handler from the ForgeWeave event mesh by handler instance.
        /// </summary>
        public static bool UnsubscribeWeave(IForgeEventHandler handler)
        {
            return handler != null && (ForgeApi.Events?.Unregister(handler) ?? false);
        }

        /// <summary>
        /// Unregisters a handler from the ForgeWeave event mesh by handler ID.
        /// </summary>
        public static bool UnsubscribeWeave(string handlerId)
        {
            return !string.IsNullOrWhiteSpace(handlerId) && (ForgeApi.Events?.Unregister(handlerId) ?? false);
        }

        public static void Dispatch(ForgeEvent eventType, params object[] args)
        {
            if (!Subscribers.TryGetValue(eventType, out var list)) return;

            Action<object[]>[] snapshot;
            lock (list)
            {
                snapshot = list.ToArray();
            }

            foreach (var callback in snapshot)
            {
                try
                {
                    callback(args);
                }
                catch (Exception ex)
                {
                    // Bug fix (v4.1.0): errors were silently swallowed before.
                    // Now they are routed to ForgeApi.Logger so modders can see them
                    // in the Calradia Forge log view without crashing other handlers.
                    ForgeApi.Logger?.LogError(
                        "ForgeCampaignEvents",
                        $"Dispatch callback threw an exception for event sequence {eventType.Sequence}: {ex.Message}",
                        ex);
                }
            }
        }
    }

    public enum ForgeWeaveRegistrationState
    {
        WaitingForHost,
        Registered,
        HostUnsupported,
        Failed,
        Disposed
    }

    /// <summary>Tracks one ForgeWeave handler through host availability and reconnection.</summary>
    public sealed class ForgeWeaveRegistration : IDisposable
    {
        sealed class PendingUnregistration
        {
            public IForgeEventRegistry Registry;
            public int ThreadId;
            public bool InProgress;
        }

        readonly object gate = new object();
        readonly IForgeEventHandler handler;
        readonly List<PendingUnregistration> pendingUnregistrations = new List<PendingUnregistration>();
        IForgeEventRegistry activeRegistry;
        int activeThreadId;
        ForgeWeaveRegistrationState state = ForgeWeaveRegistrationState.WaitingForHost;
        Exception error;
        bool disposing;
        bool unresolvedRegistrationSideEffects;
        long lastGeneration = -1;

        internal ForgeWeaveRegistration(IForgeEventHandler handler)
        {
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public ForgeWeaveRegistrationState State { get { lock (gate) return state; } }
        public Exception Error { get { lock (gate) return error; } }
        public IForgeEventHandler Handler => handler;

        internal void Start()
        {
            try
            {
                ForgeApi.RegisterRegistryChangeListener(OnRegistryChanged);
            }
            catch (Exception ex)
            {
                lock (gate)
                {
                    if (state != ForgeWeaveRegistrationState.Disposed)
                    {
                        state = ForgeWeaveRegistrationState.Failed;
                        error = ex;
                    }
                }
            }
        }

        void OnRegistryChanged(IForgeRegistry registry, long generation, int connectionThreadId)
        {
            lock (gate)
            {
                if (state == ForgeWeaveRegistrationState.Disposed || disposing || generation <= lastGeneration) return;
                lastGeneration = generation;

                if (unresolvedRegistrationSideEffects)
                {
                    state = ForgeWeaveRegistrationState.Failed;
                    return;
                }

                var currentThreadId = Thread.CurrentThread.ManagedThreadId;
                if (activeRegistry != null && activeThreadId != currentThreadId)
                {
                    state = ForgeWeaveRegistrationState.Failed;
                    error = new InvalidOperationException("ForgeWeave host changes must be handled on the thread that registered the active handler.");
                    return;
                }

                if (registry is IForgeEventRegistry && connectionThreadId != currentThreadId)
                {
                    state = ForgeWeaveRegistrationState.Failed;
                    error = new InvalidOperationException("ForgeWeave handlers must be registered on the connected host's connection thread.");
                    return;
                }

                var pendingError = RetryPendingUnregistrations();
                if (state == ForgeWeaveRegistrationState.Disposed)
                {
                    if (pendingError != null) error = Combine(error, pendingError);
                    return;
                }
                // A pending host cleanup can reenter Connect and install a newer handler.
                // Preserve that callback's state even when the older cleanup then fails.
                if (generation != lastGeneration)
                {
                    if (pendingError != null) error = Combine(error, pendingError);
                    return;
                }
                if (pendingError != null)
                {
                    state = ForgeWeaveRegistrationState.Failed;
                    error = pendingError;
                    return;
                }

                var cleanupError = UnregisterActiveHandler();
                if (state == ForgeWeaveRegistrationState.Disposed)
                {
                    if (cleanupError != null) error = Combine(error, cleanupError);
                    return;
                }
                // Unregister is host code. It can synchronously reconnect Forge, which
                // delivers a newer generation while this callback is still on the stack.
                // Do not let this older callback overwrite that newer registration.
                if (generation != lastGeneration)
                {
                    if (cleanupError != null) error = Combine(error, cleanupError);
                    return;
                }
                if (cleanupError != null)
                {
                    state = ForgeWeaveRegistrationState.Failed;
                    error = cleanupError;
                    return;
                }
                if (registry == null)
                {
                    state = ForgeWeaveRegistrationState.WaitingForHost;
                    error = null;
                    return;
                }

                var eventRegistry = registry as IForgeEventRegistry;
                if (eventRegistry == null)
                {
                    state = ForgeWeaveRegistrationState.HostUnsupported;
                    error = Combine(cleanupError, new InvalidOperationException("The connected Forge host does not expose an event registry."));
                    return;
                }

                try
                {
                    eventRegistry.Register(handler);
                    if (state == ForgeWeaveRegistrationState.Disposed)
                    {
                        // Dispose may run reentrantly from a host's Register call before
                        // activeRegistry is assigned. Track the just-created registration
                        // before cleanup so a failed Unregister can be retried by Dispose.
                        activeRegistry = eventRegistry;
                        activeThreadId = currentThreadId;
                        var unregisterError = UnregisterActiveHandler();
                        if (unregisterError != null)
                        {
                            state = ForgeWeaveRegistrationState.Failed;
                            error = unregisterError;
                        }
                        return;
                    }
                    // Register is host code and may reenter Connect/Disconnect. A newer
                    // callback can register this handler with the replacement host before
                    // this older Register returns. Withdraw the stale registration instead
                    // of overwriting the newer activeRegistry reference.
                    if (generation != lastGeneration)
                    {
                        var stale = new PendingUnregistration
                        {
                            Registry = eventRegistry,
                            ThreadId = currentThreadId
                        };
                        pendingUnregistrations.Add(stale);
                        var staleCleanupError = RetryUnregistration(stale);
                        if (generation != lastGeneration)
                        {
                            if (staleCleanupError != null) error = Combine(error, staleCleanupError);
                            return;
                        }
                        if (staleCleanupError != null)
                        {
                            state = ForgeWeaveRegistrationState.Failed;
                            error = staleCleanupError;
                        }
                        return;
                    }
                    activeRegistry = eventRegistry;
                    activeThreadId = currentThreadId;
                    state = ForgeWeaveRegistrationState.Registered;
                    error = cleanupError;
                }
                catch (Exception ex)
                {
                    // Register can throw after changing host state, including after a
                    // reentrant connection or Dispose. Its contract does not promise
                    // atomicity, and Unregister(handler) is ID-based; an automatic rollback
                    // could therefore remove a different handler that owns the same ID.
                    // Keep any newer known registration, fail closed, and require review.
                    unresolvedRegistrationSideEffects = true;
                    error = new InvalidOperationException(
                        "ForgeWeave registration threw and may have left host-side effects. Automatic reconnect and ID-based rollback are disabled; inspect the host registration before recovery.", ex);
                    if (state != ForgeWeaveRegistrationState.Disposed)
                        state = ForgeWeaveRegistrationState.Failed;
                }
            }
        }

        Exception UnregisterActiveHandler()
        {
            var previousRegistry = activeRegistry;
            if (previousRegistry == null) return null;
            try
            {
                previousRegistry.Unregister(handler);
                // Preserve a registration installed by a reentrant, newer host callback.
                // Keep the old reference when Unregister throws so a later reconnect or
                // Dispose can retry instead of silently abandoning the active handler.
                if (ReferenceEquals(activeRegistry, previousRegistry))
                {
                    activeRegistry = null;
                    activeThreadId = 0;
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        Exception RetryPendingUnregistrations()
        {
            Exception failure = null;
            // Host Unregister calls may reenter and mutate this list. Iterate a snapshot,
            // then let RetryUnregistration ignore entries already removed by the callback.
            var pendingSnapshot = pendingUnregistrations.ToArray();
            for (var index = pendingSnapshot.Length - 1; index >= 0; index--)
            {
                var cleanupError = RetryUnregistration(pendingSnapshot[index]);
                if (cleanupError != null) failure = Combine(failure, cleanupError);
            }
            return failure;
        }

        Exception RetryUnregistration(PendingUnregistration pending)
        {
            if (!pendingUnregistrations.Contains(pending)) return null;
            if (pending.InProgress) return null;
            if (pending.ThreadId != Thread.CurrentThread.ManagedThreadId)
                return new InvalidOperationException("ForgeWeave handlers must be unregistered on the thread that registered them.");

            pending.InProgress = true;
            try
            {
                pending.Registry.Unregister(handler);
                pendingUnregistrations.Remove(pending);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                pending.InProgress = false;
            }
        }

        static Exception Combine(Exception first, Exception second)
        {
            if (first == null) return second;
            if (second == null) return first;
            return new AggregateException(first, second);
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (state == ForgeWeaveRegistrationState.Disposed) return;
                if (disposing) return;
                if (activeRegistry != null && activeThreadId != Thread.CurrentThread.ManagedThreadId)
                {
                    error = new InvalidOperationException("Dispose must run on the thread that registered the active ForgeWeave handler.");
                    throw (InvalidOperationException)error;
                }
                disposing = true;
                try
                {
                    var pendingError = RetryPendingUnregistrations();
                    var activeError = UnregisterActiveHandler();
                    var cleanupError = Combine(pendingError, activeError);
                    if (cleanupError != null)
                    {
                        error = cleanupError;
                        throw new InvalidOperationException("ForgeWeave could not unregister the active handler.", cleanupError);
                    }
                    ForgeApi.UnregisterRegistryChangeListener(OnRegistryChanged);
                    state = ForgeWeaveRegistrationState.Disposed;
                    if (!unresolvedRegistrationSideEffects) error = null;
                }
                finally
                {
                    disposing = false;
                }
            }
        }
    }

    internal sealed class DelegateForgeEventHandler : IForgeEventHandler
    {
        public ForgeEventSubscription Subscription { get; }
        private readonly Action<ForgeEvent> _handler;

        public DelegateForgeEventHandler(ForgeEventSubscription subscription, Action<ForgeEvent> handler)
        {
            Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void Handle(ForgeEvent @event) => _handler(@event);
    }
}
