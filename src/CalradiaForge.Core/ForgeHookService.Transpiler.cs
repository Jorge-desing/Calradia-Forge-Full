#if NETFRAMEWORK
using System;
using System.Threading;
using CalradiaForge.Sdk;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace CalradiaForge.Core
{
    public sealed partial class ForgeHookService
    {
        /// <summary>Registers an inert, local IL manipulator using the normal hook handle and lifecycle.</summary>
        /// <remarks>The metadata must have no runtime callbacks. Initial application and management require the approved host context. After explicit application, MonoMod may reconstruct that activation on another caller's thread, even after the host context changes or gameplay callbacks shut down, while the activation remains owned and its cleanup is not uncertain. Manipulators must tolerate repeated execution, use only the supplied IL and stable configuration, and never access live game state. Transformed method bodies remain active until explicit reversion. ILContext is never exposed through SDK or IPC.</remarks>
        public IForgeHookHandle RegisterTranspiler(ForgeHookDefinition metadata, ILContext.Manipulator manipulator)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));
            if (manipulator == null) throw new ArgumentNullException(nameof(manipulator));
            if (metadata.Prefix != null || metadata.Postfix != null || metadata.Finalizer != null)
                throw new ArgumentException("Transpiler metadata cannot contain Prefix, Postfix, or Finalizer callbacks.", nameof(metadata));
            if (metadata.Target != null && metadata.Target.GetMethodBody() == null)
                throw new ArgumentException("A transpiler requires a managed method body.", nameof(metadata));
            lock (gate)
            {
                IForgeHookHandle handle = RegisterCore(metadata, false);
                entries[metadata.Id].Transpiler = manipulator;
                return handle;
            }
        }

        void InvokeTranspiler(DispatchActivation activation, ILContext context)
        {
            Entry entry = activation.Entry;
            lock (callbackInvocationGate)
            {
                DispatchActivation retained;
                bool owned = ReferenceEquals(Volatile.Read(ref entry.Activation), activation) ||
                    (entry.RetiringActivations.TryGetValue(activation.DispatchId, out retained) && ReferenceEquals(retained, activation));
                // Authorization belongs to this backend activation, not a reusable handle or
                // the current screen or gameplay callback gate. Rebuilding from original IL
                // must repeat its transformation until verified removal; uncertain Undo
                // never grants permission to reconstruct or reuse an activation.
                bool reconstructionAllowed = Volatile.Read(ref activation.IlRebuildAuthorized);
                if (!owned || activation.IlUndoUncertain ||
                    (!reconstructionAllowed && !CallbackAllowed(entry) && !CleanupRebuildAllowed(entry)))
                    throw new InvalidOperationException("IL chain rebuild rejected outside the approved host context or during unload.");
                Interlocked.Increment(ref activeManipulators);
                try { entry.Transpiler(context); }
                finally { Interlocked.Decrement(ref activeManipulators); }
            }
        }

        bool CleanupRebuildAllowed(Entry entry)
        {
            // Shutdown closes runtime callbacks and new applications permanently. An
            // activation that never completed application has no reconstruction permission;
            // only synchronous owned Undo may rebuild it on its approved caller thread.
            if (Volatile.Read(ref callbacksEnabled) != 0 ||
                Volatile.Read(ref ilCleanupThread) != Thread.CurrentThread.ManagedThreadId)
                return false;
            try { return entry.CanInvoke != null && entry.CanInvoke(); }
            catch { return false; }
        }

        void UndoIlBackend(ILHook hook)
        {
            if (!Monitor.IsEntered(gate) || !MayMutate())
                throw new InvalidOperationException("IL cleanup requires the owned service lock and approved host context.");
            int caller = Thread.CurrentThread.ManagedThreadId;
            if (Interlocked.CompareExchange(ref ilCleanupThread, caller, 0) != 0)
                throw new InvalidOperationException("A nested IL cleanup rebuild is unavailable.");
            try { hook.Undo(); }
            finally { Volatile.Write(ref ilCleanupThread, 0); }
        }
    }
}
#endif
