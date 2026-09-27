using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CalradiaForge.Sdk
{
    /// <summary>A thread-affine registry of explicitly named, versioned mod services.</summary>
    public sealed class SharedLibraryRegistry : IDisposable
    {
        readonly int thread = Thread.CurrentThread.ManagedThreadId;
        readonly Dictionary<string, ModuleLibrary> modules = new Dictionary<string, ModuleLibrary>(StringComparer.OrdinalIgnoreCase);
        readonly List<ISharedServiceMonitor> monitors = new List<ISharedServiceMonitor>();
        readonly Queue<Action> notifications = new Queue<Action>();
        bool notifying;
        bool disposing;
        bool disposed;
        long nextGeneration;

        /// <summary>Opens a provider or consumer scope on the current registry thread.</summary>
        public ModuleLibrary OpenModule(string moduleId)
        {
            CheckCanMutate(); ValidateId(moduleId);
            if (modules.ContainsKey(moduleId)) throw new InvalidOperationException("Module library already registered: " + moduleId);
            var module = new ModuleLibrary(this, moduleId);
            modules.Add(moduleId, module);
            NotifyChanged();
            return module;
        }

        internal void Check()
        {
            if (Thread.CurrentThread.ManagedThreadId != thread)
                throw new InvalidOperationException("Shared libraries must be accessed on their registry thread.");
            if (disposed) throw new ObjectDisposedException(nameof(SharedLibraryRegistry));
        }

        internal void CheckCanMutate()
        {
            Check();
            if (disposing) throw new InvalidOperationException("The shared-library registry is disconnecting and cannot accept new changes.");
        }

        internal static void ValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 160 || id.Any(c => !char.IsLetterOrDigit(c) && c != '.' && c != '_' && c != '-'))
                throw new ArgumentException("Use a nonempty library ID of at most 160 letters, digits, dots, underscores or hyphens.", nameof(id));
        }

        internal bool TryFind(string id, out ModuleLibrary module)
        {
            Check(); ValidateId(id);
            return modules.TryGetValue(id, out module);
        }

        internal long NextRegistrationGeneration()
        {
            CheckCanMutate();
            return ++nextGeneration;
        }

        internal void Remove(ModuleLibrary module)
        {
            Check();
            if (modules.TryGetValue(module.ModuleId, out var current) && ReferenceEquals(current, module)) modules.Remove(module.ModuleId);
        }

        internal void RegisterMonitor(ISharedServiceMonitor monitor)
        {
            CheckCanMutate();
            monitors.Add(monitor);
        }

        internal void RemoveMonitor(ISharedServiceMonitor monitor)
        {
            Check();
            monitors.Remove(monitor);
        }

        internal void RemoveOwnedMonitors(ModuleLibrary owner)
        {
            foreach (var monitor in monitors.ToArray())
            {
                if (!ReferenceEquals(monitor.Owner, owner)) continue;
                monitor.DisposeFromOwner();
                monitors.Remove(monitor);
            }
        }

        internal void QueueNotification(Action notification)
        {
            if (!disposing && !disposed) notifications.Enqueue(notification);
        }

        internal void NotifyChanged()
        {
            Check();
            if (disposing || disposed) return;

            // Refresh every monitor after the complete registry mutation, then drain callbacks.
            // Mutations made by a callback refresh Current immediately; their callbacks are
            // appended to this queue and are never dispatched recursively.
            foreach (var monitor in monitors.ToArray())
                if (!monitor.IsDisposed) monitor.RefreshAndQueue();

            DrainNotifications();
        }

        void DrainNotifications()
        {
            if (notifying || disposing || disposed) return;
            notifying = true;
            try
            {
                while (notifications.Count > 0 && !disposing && !disposed)
                    notifications.Dequeue()();
            }
            finally
            {
                notifying = false;
            }
        }

        public void Dispose()
        {
            if (disposed || disposing) return;
            Check();
            disposing = true;
            foreach (var module in modules.Values.ToArray()) module.DisposeFromRegistry();
            modules.Clear();
            foreach (var monitor in monitors.ToArray()) monitor.DisposeFromOwner();
            monitors.Clear();
            notifications.Clear();
            disposed = true;
            disposing = false;
        }
    }

    /// <summary>Owns registrations and watches for one module. Dispose on module unload.</summary>
    public sealed class ModuleLibrary : IDisposable
    {
        readonly SharedLibraryRegistry registry;
        Dictionary<string, SharedRegistration> services = new Dictionary<string, SharedRegistration>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<SharedServiceBatch> batches = new HashSet<SharedServiceBatch>();
        bool disposed;
        public string ModuleId { get; }
        internal SharedLibraryRegistry Registry => registry;
        internal ModuleLibrary(SharedLibraryRegistry registry, string moduleId) { this.registry = registry; ModuleId = moduleId; }

        internal void Check()
        {
            registry.Check();
            if (disposed) throw new ObjectDisposedException("Module library: " + ModuleId);
        }

        internal void CheckCanMutate()
        {
            Check();
            registry.CheckCanMutate();
        }

        internal static Version Normalize(Version version)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));
            if (version.Major < 1) throw new ArgumentException("Shared API versions must have major version 1 or later.", nameof(version));
            return new Version(version.Major, Math.Max(0, version.Minor), Math.Max(0, version.Build), Math.Max(0, version.Revision));
        }

        internal static void CheckContract<T>() where T : class
        {
            if (!typeof(T).IsInterface) throw new ArgumentException("Publish and require a shared interface contract, not an implementation class.");
        }

        internal static string DescribeContract(Type contract)
        {
            return "'" + contract.AssemblyQualifiedName + "'";
        }

        /// <summary>Publishes one interface implementation. Its author retains disposal ownership.</summary>
        public IDisposable Provide<T>(string serviceId, Version apiVersion, T implementation) where T : class
        {
            CheckCanMutate(); SharedLibraryRegistry.ValidateId(serviceId); CheckContract<T>();
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            var version = Normalize(apiVersion);
            if (services.ContainsKey(serviceId)) throw new InvalidOperationException("Service already registered: " + ModuleId + "/" + serviceId);
            var registration = new SharedRegistration(this, serviceId, version, typeof(T), implementation, registry.NextRegistrationGeneration());
            services.Add(serviceId, registration);
            registry.NotifyChanged();
            return registration;
        }

        /// <summary>Resolves a service without throwing when its provider or registration is unavailable.</summary>
        /// <param name="providerModule">The exact module identifier that owns the service.</param>
        /// <param name="serviceId">The explicit provider service identifier.</param>
        /// <param name="minimumVersion">The minimum compatible service API version.</param>
        /// <returns>An immutable result; invalid arguments and lifetime/thread errors still throw.</returns>
        public SharedServiceResolution<T> Resolve<T>(string providerModule, string serviceId, Version minimumVersion) where T : class
        {
            Check();
            SharedLibraryRegistry.ValidateId(providerModule);
            SharedLibraryRegistry.ValidateId(serviceId);
            CheckContract<T>();
            var minimum = Normalize(minimumVersion);
            return ResolveValidated<T>(providerModule, serviceId, minimum);
        }

        /// <summary>Resolves an explicit provider and contract; major versions must match or throws.</summary>
        public SharedService<T> Require<T>(string providerModule, string serviceId, Version minimumVersion) where T : class
        {
            var resolution = Resolve<T>(providerModule, serviceId, minimumVersion);
            if (!resolution.TryGetService(out var service)) throw new InvalidOperationException(resolution.Diagnostic);
            return service;
        }

        /// <summary>Watches an explicit provider and contract. The initial state is available through Current.</summary>
        public SharedServiceMonitor<T> Watch<T>(string providerModule, string serviceId, Version minimumVersion) where T : class
        {
            CheckCanMutate();
            SharedLibraryRegistry.ValidateId(providerModule);
            SharedLibraryRegistry.ValidateId(serviceId);
            CheckContract<T>();
            var minimum = Normalize(minimumVersion);
            var monitor = new SharedServiceMonitor<T>(this, providerModule, serviceId, minimum, ResolveValidated<T>(providerModule, serviceId, minimum));
            registry.RegisterMonitor(monitor);
            return monitor;
        }

        internal SharedServiceResolution<T> ResolveValidated<T>(string providerModule, string serviceId, Version minimum) where T : class
        {
            if (!registry.TryFind(providerModule, out var provider))
            {
                var message = "Provider module '" + providerModule + "' is unavailable while resolving service '" + serviceId + "' for contract " + DescribeContract(typeof(T)) + " (minimum API " + minimum + "). Declare the provider module in the consumer's SubModule.xml and verify its exact Id and load order.";
                return new SharedServiceResolution<T>(SharedServiceResolutionStatus.ProviderMissing, message, 0, null);
            }

            if (!provider.services.TryGetValue(serviceId, out var registration))
            {
                var message = "Provider module '" + providerModule + "' has no registered service '" + serviceId + "' for contract " + DescribeContract(typeof(T)) + " (minimum API " + minimum + "). Verify the service Id and that the provider registers it after Forge becomes available.";
                return new SharedServiceResolution<T>(SharedServiceResolutionStatus.ServiceMissing, message, 0, null);
            }

            if (registration.Contract != typeof(T))
            {
                var message = "Shared service contract mismatch for provider '" + providerModule + "', service '" + serviceId + "': provider publishes " + DescribeContract(registration.Contract) + " at API " + registration.Version + ", while the consumer requests " + DescribeContract(typeof(T)) + " with minimum API " + minimum + ". Reference the same shared contract assembly from both modules and package/load that assembly only once with the provider.";
                return new SharedServiceResolution<T>(SharedServiceResolutionStatus.ContractMismatch, message, registration.Generation, null);
            }

            if (registration.Version.Major != minimum.Major || registration.Version.CompareTo(minimum) < 0)
            {
                var message = "Incompatible shared API for provider '" + providerModule + "', service '" + serviceId + "', contract " + DescribeContract(registration.Contract) + ": provider offers " + registration.Version + ", consumer requires major " + minimum.Major + " and at least " + minimum + ". Align the provider's published API version and the consumer's minimum version, or update both modules to a compatible contract release.";
                return new SharedServiceResolution<T>(SharedServiceResolutionStatus.IncompatibleVersion, message, registration.Generation, null);
            }

            return new SharedServiceResolution<T>(SharedServiceResolutionStatus.Available, string.Empty, registration.Generation, new SharedService<T>(this, registration));
        }

        /// <summary>Starts staging an atomic set of services owned by this module.</summary>
        public SharedServiceBatch BeginPublication()
        {
            CheckCanMutate();
            var batch = new SharedServiceBatch(this);
            batches.Add(batch);
            return batch;
        }

        internal void Commit(SharedServiceBatch batch, IList<StagedSharedService> staged)
        {
            CheckCanMutate();
            if (!batches.Contains(batch)) throw new ObjectDisposedException(nameof(SharedServiceBatch));
            if (staged.Count == 0) throw new InvalidOperationException("A shared service publication must contain at least one service.");

            // Build a replacement dictionary first. Any validation/allocation failure leaves the registry untouched.
            var updated = new Dictionary<string, SharedRegistration>(services, StringComparer.OrdinalIgnoreCase);
            foreach (var item in staged)
                if (updated.ContainsKey(item.ServiceId)) throw new InvalidOperationException("Service already registered: " + ModuleId + "/" + item.ServiceId);

            var registrations = new List<SharedRegistration>(staged.Count);
            foreach (var item in staged)
            {
                var registration = new SharedRegistration(this, item.ServiceId, item.ApiVersion, item.Contract, item.Implementation, registry.NextRegistrationGeneration());
                updated.Add(item.ServiceId, registration);
                registrations.Add(registration);
            }

            services = updated;
            batch.MarkCommitted(registrations);
            registry.NotifyChanged();
        }

        internal void Release(SharedServiceBatch batch, IList<SharedRegistration> registrations, bool committed)
        {
            Check();
            if (!batches.Remove(batch)) return;
            if (!committed) return;

            var updated = new Dictionary<string, SharedRegistration>(services, StringComparer.OrdinalIgnoreCase);
            var removed = false;
            foreach (var registration in registrations)
            {
                if (!updated.TryGetValue(registration.Id, out var current) || !ReferenceEquals(current, registration)) continue;
                updated.Remove(registration.Id);
                registration.Invalidate();
                removed = true;
            }
            services = updated;
            if (removed) registry.NotifyChanged();
        }

        internal void Withdraw(SharedRegistration registration)
        {
            Check();
            if (services.TryGetValue(registration.Id, out var current) && ReferenceEquals(current, registration))
            {
                services.Remove(registration.Id);
                registration.Invalidate();
                registry.NotifyChanged();
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            Check();
            DisposeCore(true);
        }

        internal void DisposeFromRegistry()
        {
            if (disposed) return;
            DisposeCore(false);
        }

        void DisposeCore(bool notify)
        {
            foreach (var registration in services.Values) registration.Invalidate();
            services.Clear();
            foreach (var batch in batches.ToArray()) batch.DisposeFromProvider();
            batches.Clear();
            registry.RemoveOwnedMonitors(this);
            registry.Remove(this);
            disposed = true;
            if (notify) registry.NotifyChanged();
        }
    }

    /// <summary>Non-throwing outcome states returned by optional shared service resolution.</summary>
    public enum SharedServiceResolutionStatus
    {
        Available,
        ProviderMissing,
        ServiceMissing,
        ContractMismatch,
        IncompatibleVersion
    }

    /// <summary>An immutable optional-resolution result tied to one service registration.</summary>
    public sealed class SharedServiceResolution<T> where T : class
    {
        readonly SharedService<T> service;
        internal long RegistrationGeneration { get; }
        public SharedServiceResolutionStatus Status { get; }
        public string Diagnostic { get; }
        public bool IsAvailable => Status == SharedServiceResolutionStatus.Available;

        internal SharedServiceResolution(SharedServiceResolutionStatus status, string diagnostic, long generation, SharedService<T> service)
        { Status = status; Diagnostic = diagnostic; RegistrationGeneration = generation; this.service = service; }

        /// <summary>Returns the checked service handle when Status is Available.</summary>
        public bool TryGetService(out SharedService<T> resolvedService)
        {
            resolvedService = service;
            return IsAvailable;
        }

        internal bool SameRegistration(SharedServiceResolution<T> other)
        {
            return other != null && Status == other.Status && RegistrationGeneration == other.RegistrationGeneration;
        }
    }

    internal sealed class StagedSharedService
    {
        public string ServiceId { get; }
        public Version ApiVersion { get; }
        public Type Contract { get; }
        public object Implementation { get; }
        public StagedSharedService(string serviceId, Version apiVersion, Type contract, object implementation)
        { ServiceId = serviceId; ApiVersion = apiVersion; Contract = contract; Implementation = implementation; }
    }

    /// <summary>Stages and owns a set of service registrations as one disposable lifetime lease.</summary>
    public sealed class SharedServiceBatch : IDisposable
    {
        readonly ModuleLibrary provider;
        readonly List<StagedSharedService> staged = new List<StagedSharedService>();
        List<SharedRegistration> registrations = new List<SharedRegistration>();
        bool committed;
        bool disposed;
        internal SharedServiceBatch(ModuleLibrary provider) { this.provider = provider; }

        /// <summary>Adds one interface implementation to the pending publication.</summary>
        public void Add<T>(string serviceId, Version apiVersion, T implementation) where T : class
        {
            CheckOpen();
            SharedLibraryRegistry.ValidateId(serviceId);
            ModuleLibrary.CheckContract<T>();
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            if (staged.Any(item => StringComparer.OrdinalIgnoreCase.Equals(item.ServiceId, serviceId)))
                throw new InvalidOperationException("Service already staged in this publication: " + serviceId);
            staged.Add(new StagedSharedService(serviceId, ModuleLibrary.Normalize(apiVersion), typeof(T), implementation));
        }

        /// <summary>Validates and publishes every staged service before notifying monitors.</summary>
        public void Commit()
        {
            CheckOpen();
            if (committed) throw new InvalidOperationException("This shared service publication has already been committed.");
            provider.Commit(this, staged);
        }

        void CheckOpen()
        {
            provider.CheckCanMutate();
            if (disposed) throw new ObjectDisposedException(nameof(SharedServiceBatch));
        }

        internal void MarkCommitted(List<SharedRegistration> committedRegistrations)
        {
            registrations = committedRegistrations;
            committed = true;
            staged.Clear();
        }

        internal void DisposeFromProvider()
        {
            disposed = true;
            staged.Clear();
            registrations.Clear();
        }

        public void Dispose()
        {
            if (disposed) return;
            provider.Release(this, registrations, committed);
            disposed = true;
            staged.Clear();
            registrations.Clear();
        }
    }

    /// <summary>Reports the previous and current optional-resolution snapshots for one watch transition.</summary>
    public sealed class SharedServiceChangedEventArgs<T> : EventArgs where T : class
    {
        public SharedServiceResolution<T> Previous { get; }
        public SharedServiceResolution<T> Current { get; }
        internal SharedServiceChangedEventArgs(SharedServiceResolution<T> previous, SharedServiceResolution<T> current)
        { Previous = previous; Current = current; }
    }

    internal interface ISharedServiceMonitor
    {
        ModuleLibrary Owner { get; }
        bool IsDisposed { get; }
        void RefreshAndQueue();
        void DisposeFromOwner();
    }

    /// <summary>Tracks a service's current resolution and synchronously reports later registration changes.</summary>
    /// <remarks>Event arguments are immutable snapshots of their transition. A reentrant registry mutation
    /// takes effect immediately, while its notifications are queued until the active callback pass completes;
    /// therefore a later handler can receive an older transition snapshot than <see cref="Current"/>. Resolve
    /// again for the latest state, and treat service handles as revocable when their registration is withdrawn.</remarks>
    public sealed class SharedServiceMonitor<T> : IDisposable, ISharedServiceMonitor where T : class
    {
        readonly ModuleLibrary owner;
        readonly string providerModule;
        readonly string serviceId;
        readonly Version minimumVersion;
        SharedServiceResolution<T> current;
        EventHandler<SharedServiceChangedEventArgs<T>> changed;
        EventHandler<SharedServiceChangedEventArgs<T>>[] changedHandlers;
        Exception lastNotificationError;
        bool disposed;

        internal SharedServiceMonitor(ModuleLibrary owner, string providerModule, string serviceId, Version minimumVersion, SharedServiceResolution<T> initial)
        { this.owner = owner; this.providerModule = providerModule; this.serviceId = serviceId; this.minimumVersion = minimumVersion; current = initial; }

        ModuleLibrary ISharedServiceMonitor.Owner => owner;
        bool ISharedServiceMonitor.IsDisposed => disposed;

        /// <summary>Gets the latest state snapshot. Access is restricted to the registry thread.</summary>
        public SharedServiceResolution<T> Current
        {
            get { Check(); return current; }
        }

        /// <summary>Gets the most recent transition's aggregate callback error, or null if it had none.</summary>
        public Exception LastNotificationError
        {
            get { Check(); return lastNotificationError; }
        }

        /// <summary>Raised after the initial snapshot when availability or registration generation changes.</summary>
        /// <remarks>Callbacks run synchronously on the registry thread. Reentrant mutations apply immediately,
        /// but their notifications are queued to avoid recursive dispatch. Args are transition snapshots and can
        /// be older than <see cref="Current"/> by the time a later handler runs.</remarks>
        public event EventHandler<SharedServiceChangedEventArgs<T>> Changed
        {
            add
            {
                Check();
                if (value == null) return;
                var updated = changed + value;
                var snapshot = CreateHandlerSnapshot(updated);
                changed = updated;
                changedHandlers = snapshot;
            }
            remove
            {
                Check();
                if (value == null || changed == null) return;
                var updated = changed - value;
                if (ReferenceEquals(updated, changed)) return;
                var snapshot = CreateHandlerSnapshot(updated);
                changed = updated;
                changedHandlers = snapshot;
            }
        }

        static EventHandler<SharedServiceChangedEventArgs<T>>[] CreateHandlerSnapshot(
            EventHandler<SharedServiceChangedEventArgs<T>> handlers)
        {
            if (handlers == null) return null;
            var invocationList = handlers.GetInvocationList();
            var snapshot = new EventHandler<SharedServiceChangedEventArgs<T>>[invocationList.Length];
            for (var index = 0; index < invocationList.Length; index++)
                snapshot[index] = (EventHandler<SharedServiceChangedEventArgs<T>>)invocationList[index];
            return snapshot;
        }

        void Check()
        {
            owner.Check();
            if (disposed) throw new ObjectDisposedException(nameof(SharedServiceMonitor<T>));
        }

        void ISharedServiceMonitor.RefreshAndQueue()
        {
            if (disposed) return;
            var next = owner.ResolveValidated<T>(providerModule, serviceId, minimumVersion);
            var previous = current;
            if (previous.SameRegistration(next)) return;
            current = next;
            var args = new SharedServiceChangedEventArgs<T>(previous, next);
            owner.Registry.QueueNotification(() => Deliver(args));
        }

        void Deliver(SharedServiceChangedEventArgs<T> args)
        {
            if (disposed) return;
            lastNotificationError = null;
            var handlers = changedHandlers;
            if (handlers == null || handlers.Length == 0) return;
            List<Exception> errors = null;
            foreach (var handler in handlers)
            {
                if (disposed) break;
                try { handler(this, args); }
                catch (Exception error)
                {
                    if (errors == null) errors = new List<Exception>();
                    errors.Add(error);
                }
            }
            if (errors != null) lastNotificationError = new AggregateException("One or more shared-service monitor callbacks failed.", errors);
        }

        void ISharedServiceMonitor.DisposeFromOwner()
        {
            disposed = true;
            changed = null;
            changedHandlers = null;
            lastNotificationError = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            Check();
            owner.Registry.RemoveMonitor(this);
            ((ISharedServiceMonitor)this).DisposeFromOwner();
        }
    }

    internal sealed class SharedRegistration : IDisposable
    {
        readonly ModuleLibrary provider;
        public string Id { get; }
        public Version Version { get; }
        public Type Contract { get; }
        public long Generation { get; }
        object implementation;
        public SharedRegistration(ModuleLibrary provider, string id, Version version, Type contract, object implementation, long generation)
        { this.provider = provider; Id = id; Version = version; Contract = contract; this.implementation = implementation; Generation = generation; }
        public object Get()
        {
            provider.Check();
            return implementation ?? throw new ObjectDisposedException("Shared service: " + provider.ModuleId + "/" + Id);
        }
        public void Invalidate() { implementation = null; }
        public void Dispose()
        {
            if (implementation == null) return;
            provider.Withdraw(this);
        }
    }

    /// <summary>A checked reference to one registration generation; reacquire after replacement.</summary>
    public sealed class SharedService<T> where T : class
    {
        readonly ModuleLibrary consumer;
        readonly SharedRegistration registration;
        public Version ApiVersion => registration.Version;
        internal SharedService(ModuleLibrary consumer, SharedRegistration registration) { this.consumer = consumer; this.registration = registration; }

        /// <summary>Invokes on the registry thread after checking consumer and provider lifetimes.</summary>
        public TResult Use<TResult>(Func<T, TResult> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            consumer.Check();
            return operation((T)registration.Get());
        }
    }
}
