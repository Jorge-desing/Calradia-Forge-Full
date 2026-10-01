using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace CalradiaForge.Sdk
{
    /// <summary>Lifecycle state for a registered runtime callback or local IL hook.</summary>
    public enum ForgeHookState { Registered, Applied, Reverted, Conflict, Failed, Unsupported }

    /// <summary>Mutable call data shared by Prefix, Postfix, and Finalizer callbacks for one invocation.</summary>
    /// <remarks>Arguments may be changed by a Prefix. RunOriginal defaults to true; Result is returned when the original is skipped or replaced.</remarks>
    public sealed class ForgeHookInvocation
    {
        public ForgeHookInvocation(object instance, object[] arguments)
        {
            Instance = instance;
            Arguments = arguments ?? Array.Empty<object>();
            RunOriginal = true;
        }

        public object Instance { get; }
        public object[] Arguments { get; }
        public bool RunOriginal { get; set; }
        public object Result { get; set; }
        /// <summary>Pending failure available to a Finalizer; assign null to suppress it or another exception to replace it.</summary>
        public Exception Exception { get; set; }
    }

    /// <summary>Callback executed synchronously on the thread that invoked the target method.</summary>
    public delegate void ForgeHookCallback(ForgeHookInvocation invocation);

    /// <summary>Describes a method target and optional Prefix, Postfix, and Finalizer callbacks.</summary>
    /// <remarks>Only closed, supported managed method signatures are executable. The callbacks and target remain in-process objects and are never accepted over IPC.</remarks>
    public sealed class ForgeHookDefinition
    {
        public string Id { get; set; }
        public string Owner { get; set; }
        public MethodInfo Target { get; set; }
        public ForgeHookCallback Prefix { get; set; }
        public ForgeHookCallback Postfix { get; set; }
        /// <summary>Runs after the callback/original path, including failures, while the host callback gate permits execution.</summary>
        /// <remarks>A pending failure is exposed through Exception. Preserve it, replace it, or assign null to suppress it; a nonvoid target still requires a valid Result. A throwing Finalizer preserves an existing failure together with its own failure in an AggregateException. Without a Finalizer, the existing Prefix/Postfix failure policy is unchanged.</remarks>
        public ForgeHookCallback Finalizer { get; set; }
        public int? Priority { get; set; }
        public List<string> Before { get; set; } = new List<string>();
        public List<string> After { get; set; } = new List<string>();
    }

    /// <summary>Immutable point-in-time metadata for one registered hook.</summary>
    public sealed class ForgeHookSnapshot
    {
        public ForgeHookSnapshot(string id, string owner, string targetMethod, bool hasPrefix, bool hasPostfix,
            int? priority, IEnumerable<string> before, IEnumerable<string> after, ForgeHookState state, string detail)
            : this(id, owner, targetMethod, hasPrefix, hasPostfix, priority, before, after, state, detail, false)
        {
        }

        public ForgeHookSnapshot(string id, string owner, string targetMethod, bool hasPrefix, bool hasPostfix,
            int? priority, IEnumerable<string> before, IEnumerable<string> after, ForgeHookState state, string detail, bool hasFinalizer)
            : this(id, owner, targetMethod, hasPrefix, hasPostfix, priority, before, after, state, detail, hasFinalizer, false)
        {
        }

        public ForgeHookSnapshot(string id, string owner, string targetMethod, bool hasPrefix, bool hasPostfix,
            int? priority, IEnumerable<string> before, IEnumerable<string> after, ForgeHookState state, string detail, bool hasFinalizer, bool hasTranspiler)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A hook ID is required.", nameof(id));
            if (!Enum.IsDefined(typeof(ForgeHookState), state)) throw new ArgumentOutOfRangeException(nameof(state));
            Id = id;
            Owner = owner ?? string.Empty;
            TargetMethod = targetMethod ?? string.Empty;
            HasPrefix = hasPrefix;
            HasPostfix = hasPostfix;
            HasFinalizer = hasFinalizer;
            HasTranspiler = hasTranspiler;
            Priority = priority;
            Before = new ReadOnlyCollection<string>((before ?? Enumerable.Empty<string>()).ToArray());
            After = new ReadOnlyCollection<string>((after ?? Enumerable.Empty<string>()).ToArray());
            State = state;
            Detail = detail ?? string.Empty;
        }

        public string Id { get; }
        public string Owner { get; }
        public string TargetMethod { get; }
        public bool HasPrefix { get; }
        public bool HasPostfix { get; }
        public bool HasFinalizer { get; }
        public bool HasTranspiler { get; }
        public int? Priority { get; }
        public IReadOnlyList<string> Before { get; }
        public IReadOnlyList<string> After { get; }
        public ForgeHookState State { get; }
        public string Detail { get; }
    }

    /// <summary>Immutable result of applying, verifying, or reverting one registered hook.</summary>
    public sealed class ForgeHookOperationResult
    {
        public ForgeHookOperationResult(string id, ForgeHookState state, bool succeeded, string detail)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A hook ID is required.", nameof(id));
            if (!Enum.IsDefined(typeof(ForgeHookState), state)) throw new ArgumentOutOfRangeException(nameof(state));
            Id = id;
            State = state;
            Succeeded = succeeded;
            Detail = detail ?? string.Empty;
        }
        public string Id { get; }
        public ForgeHookState State { get; }
        public bool Succeeded { get; }
        public string Detail { get; }
    }

    /// <summary>Optional capability for explicitly registered Prefix, Postfix, and Finalizer hooks and lifecycle management of local IL hooks.</summary>
    /// <remarks>Hooks are never applied during discovery or registration. Apply and Revert are explicit host operations; owners are labels, not authorization boundaries. Callbacks run synchronously on the target caller's thread.</remarks>
    public interface IForgeHookService
    {
        IForgeHookHandle Register(ForgeHookDefinition definition);
        IReadOnlyList<ForgeHookSnapshot> GetSnapshots(string owner = null);
        ForgeHookOperationResult Apply(string hookId);
        ForgeHookOperationResult Verify(string hookId);
        ForgeHookOperationResult Revert(string hookId);
        IReadOnlyList<ForgeHookOperationResult> RevertOwner(string owner);
        IReadOnlyList<ForgeHookOperationResult> RevertAll();
        void Disconnect();
    }

    /// <summary>Optional lifecycle for hook services that can reopen after every prior hook is verified reverted.</summary>
    public interface IForgeHookServiceLifecycle
    {
        void Reconnect();
    }

    /// <summary>Optional preflight for lifecycle transitions that must safely remove active hooks.</summary>
    /// <remarks>Hosts call this before clearing the published SDK capability so an unsafe disconnect can be rejected without orphaning live detours.</remarks>
    public interface IForgeHookServiceDisconnectGuard
    {
        bool CanDisconnect(out string reason);
    }

    /// <summary>Handle for inspecting, applying, verifying, and reverting one registered hook.</summary>
    /// <remarks><see cref="IDisposable.Dispose"/> requests a revert and throws if the service cannot confirm it. When called from an in-flight hook callback, the revert request can be deferred until that dispatch exits; in that case <c>Dispose</c> may return while the snapshot remains <see cref="ForgeHookState.Applied"/> and reversion is not yet verified. Call <see cref="Revert"/> directly when the caller needs the structured pending/failure result.</remarks>
    public interface IForgeHookHandle : IDisposable
    {
        ForgeHookSnapshot Snapshot { get; }
        ForgeHookOperationResult Apply();
        ForgeHookOperationResult Verify();
        ForgeHookOperationResult Revert();
    }
}
