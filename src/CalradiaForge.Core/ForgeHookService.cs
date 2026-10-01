using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Threading;
using CalradiaForge.Sdk;
#if NETFRAMEWORK
using MonoMod.RuntimeDetour;
#endif

namespace CalradiaForge.Core
{
    /// <summary>Explicit Prefix/Postfix hook lifecycle backed by MonoMod RuntimeDetour on the game target.</summary>
    /// <remarks>Registration is inert. On the Bannerlord target, Apply and Revert explicitly update the MonoMod detour chain; upstream documents synchronized chain edits, including removal from the hook currently executing. The disposable fixture covers serial self-removal only and is not a full host-lifecycle or general concurrency certification. These guarantees do not apply to raw ForgeDetour writes. No arbitrary target data is exposed over IPC.</remarks>
    public sealed class ForgeHookService : IForgeHookService, IForgeHookServiceLifecycle, IForgeHookServiceDisconnectGuard
    {
        sealed class Entry
        {
            internal string Id;
            internal string Owner;
            internal MethodInfo Target;
            internal ForgeHookCallback Prefix;
            internal ForgeHookCallback Postfix;
            internal int? Priority;
            internal string[] Before;
            internal string[] After;
            internal Type[] ParameterTypes;
            int stateCode;
            string detail;
            internal ForgeHookState State
            {
                get { return (ForgeHookState)Volatile.Read(ref stateCode); }
                set { Volatile.Write(ref stateCode, (int)value); }
            }
            internal string Detail
            {
                get { return Volatile.Read(ref detail); }
                set { Volatile.Write(ref detail, value); }
            }
            internal Func<bool> CanInvoke;
#if NETFRAMEWORK
            internal DispatchActivation Activation;
            internal readonly ConcurrentDictionary<string, DispatchActivation> RetiringActivations =
                new ConcurrentDictionary<string, DispatchActivation>(StringComparer.Ordinal);
#endif
        }

#if NETFRAMEWORK
        sealed class DispatchActivation
        {
            readonly object lifetimeGate = new object();
            int activeDispatches;
            bool retirementRequested;
            bool disposed;
            bool hookDisposed;
            bool targetCounted;
            bool reservationReleaseStarted;
            bool undoVerified;
            bool deferredUndoRequested;
            string disposalError;

            internal readonly Entry Entry;
            internal readonly ForgeHookService Service;
            internal readonly string DispatchId;
            internal Hook Hook;
            internal DynamicMethod DetourMethod;
            internal Func<Delegate, object, object[], object> OriginalInvoker;

            internal DispatchActivation(ForgeHookService service, Entry entry, string dispatchId,
                Func<Delegate, object, object[], object> originalInvoker)
            {
                Service = service;
                Entry = entry;
                DispatchId = dispatchId;
                OriginalInvoker = originalInvoker;
            }

            internal bool TryEnter()
            {
                lock (lifetimeGate)
                {
                    if (retirementRequested || disposed) return false;
                    activeDispatches++;
                    return true;
                }
            }

            internal void Exit()
            {
                bool finalizeRevert = false;
                lock (lifetimeGate)
                {
                    if (activeDispatches <= 0) throw new InvalidOperationException("Hook dispatch activation count underflow.");
                    activeDispatches--;
                    // Removal is deliberately not attempted from inside a callback. The
                    // caller must first finish the captured original/postfix path; the
                    // service then rechecks its exact host mutation gate before Undo.
                    finalizeRevert = activeDispatches == 0 && retirementRequested && !disposed;
                }
                if (finalizeRevert)
                {
                    try { Service.FinishDeferredRevert(this); }
                    catch (Exception error)
                    {
                        Entry.Detail = Bound("Deferred hook reversion remains unverified; the hook and target reservation are retained for an explicit retry: " + error.Message, 240);
                    }
                }
            }

            internal bool RequestRetirement()
            {
                lock (lifetimeGate)
                {
                    if (disposed) return false;
                    retirementRequested = true;
                    bool hasActiveDispatches = activeDispatches != 0;
                    deferredUndoRequested = hasActiveDispatches;
                    return hasActiveDispatches;
                }
            }

            internal void MarkUndoVerified()
            {
                lock (lifetimeGate)
                {
                    undoVerified = true;
                    deferredUndoRequested = false;
                }
            }

            internal bool DisposeRetired()
            {
                Hook hook;
                bool disposeHook;
                lock (lifetimeGate)
                {
                    if (disposed) return true;
                    if (!retirementRequested || activeDispatches != 0 || !undoVerified) return false;
                    disposeHook = !hookDisposed;
                    hook = Hook;
                }

                if (disposeHook)
                {
                    try { hook?.Dispose(); }
                    catch (Exception error)
                    {
                        MarkCleanupFailure("MonoMod hook disposal after verified removal failed; activation remains retained: " + error.Message);
                        return false;
                    }

                    lock (lifetimeGate)
                    {
                        Hook = null;
                        OriginalInvoker = null;
                        DetourMethod = null;
                        hookDisposed = true;
                    }
                }

                bool hasReservation;
                lock (lifetimeGate) hasReservation = targetCounted;
                if (hasReservation)
                {
                    if (!ReleaseTargetReservation()) return false;
                }

                CompleteRetirement();
                return true;
            }

            bool ReleaseTargetReservation()
            {
                lock (lifetimeGate)
                {
                    if (!targetCounted) return true;
                    if (reservationReleaseStarted) return false;
                    reservationReleaseStarted = true;
                }
                try
                {
                    ForgeDetour.UnregisterHookTarget(Entry.Target);
                    lock (lifetimeGate)
                    {
                        targetCounted = false;
                        reservationReleaseStarted = false;
                    }
                    return true;
                }
                catch (Exception error)
                {
                    lock (lifetimeGate) reservationReleaseStarted = false;
                    MarkCleanupFailure("Hook disposal succeeded but target reservation release failed; the activation remains retained: " + error.Message);
                    return false;
                }
            }

            void CompleteRetirement()
            {
                lock (lifetimeGate)
                {
                    disposalError = null;
                    disposed = true;
                }
                if (Entry.State == ForgeHookState.Reverted)
                    Entry.Detail = "MonoMod detour removal, hook disposal, and target-reservation release were verified.";
                Interlocked.CompareExchange(ref Entry.Activation, null, this);
                Entry.RetiringActivations.TryRemove(DispatchId, out _);
            }

            void MarkCleanupFailure(string message)
            {
                string detail = Bound(message, 240);
                lock (lifetimeGate)
                {
                    disposalError = detail;
                }
                Interlocked.CompareExchange(ref Entry.Activation, this, null);
                Entry.State = ForgeHookState.Conflict;
                Entry.Detail = detail;
            }

            internal void MarkTargetCounted()
            {
                lock (lifetimeGate) targetCounted = true;
            }

            internal bool IsRetiring { get { lock (lifetimeGate) return retirementRequested; } }
            internal bool IsDisposed { get { lock (lifetimeGate) return disposed; } }
            internal bool HasTargetReservation { get { lock (lifetimeGate) return targetCounted; } }
            internal bool HasActiveDispatches { get { lock (lifetimeGate) return activeDispatches != 0; } }
            internal bool DeferredUndoRequested { get { lock (lifetimeGate) return deferredUndoRequested; } }
            internal bool UndoVerified { get { lock (lifetimeGate) return undoVerified; } }
            internal string DisposalError { get { lock (lifetimeGate) return disposalError; } }
        }
#endif

#if NETFRAMEWORK
        static readonly ConcurrentDictionary<string, DispatchActivation> DispatchEntries = new ConcurrentDictionary<string, DispatchActivation>(StringComparer.Ordinal);
        static readonly object DispatchGate = new object();
#endif
        readonly object gate = new object();
        readonly object callbackInvocationGate = new object();
        readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        readonly List<string> applyOrder = new List<string>();
        readonly Func<bool> mayMutate;
#if NETFRAMEWORK
        // Kept per service so the disposable fixture can exercise an unreadable MonoMod status.
        Func<Hook, bool> hookIsApplied = hook => hook.IsApplied;
#endif
        bool accepting = true;
        int acceptsNewHooks = 1;
        int callbacksEnabled = 1;

        /// <summary>Creates a service that rejects mutations unless a host supplies an explicit safety gate.</summary>
        public ForgeHookService(Func<bool> mayMutate = null) { this.mayMutate = mayMutate ?? (() => false); }

        public IForgeHookHandle Register(ForgeHookDefinition definition)
        {
            ValidateDefinition(definition);
            lock (gate)
            {
                if (!accepting || Volatile.Read(ref acceptsNewHooks) == 0)
                    throw new InvalidOperationException("The hook service is disconnected or shutting down and rejects new hooks.");
                if (entries.ContainsKey(definition.Id)) throw new InvalidOperationException("Hook ID already exists: " + definition.Id);
                var entry = new Entry
                {
                    Id = definition.Id.Trim(), Owner = definition.Owner.Trim(), Target = definition.Target,
                    Prefix = definition.Prefix, Postfix = definition.Postfix, Priority = definition.Priority,
                    Before = (definition.Before ?? new List<string>()).Select(value => value.Trim()).ToArray(),
                    After = (definition.After ?? new List<string>()).Select(value => value.Trim()).ToArray(),
                    ParameterTypes = definition.Target.GetParameters().Select(parameter => parameter.ParameterType).ToArray(),
                    CanInvoke = mayMutate,
                    State = ForgeHookState.Registered, Detail = "Registered only; no detour has been applied."
                };
                entries.Add(entry.Id, entry);
                return new Handle(this, entry.Id);
            }
        }

        public IReadOnlyList<ForgeHookSnapshot> GetSnapshots(string owner = null)
        {
            lock (gate)
            {
                return applyOrder.Concat(entries.Keys.Except(applyOrder, StringComparer.OrdinalIgnoreCase))
                    .Where(id => entries.ContainsKey(id))
                    .Select(id => entries[id])
                    .Where(entry => string.IsNullOrWhiteSpace(owner) || string.Equals(entry.Owner, owner, StringComparison.OrdinalIgnoreCase))
                    .Select(Snapshot).ToArray();
            }
        }

        public ForgeHookOperationResult Apply(string hookId)
        {
            if (string.IsNullOrWhiteSpace(hookId)) throw new ArgumentException("A hook ID is required.", nameof(hookId));
            lock (gate)
            {
                lock (ForgeDetour.Gate)
                {
                    Entry entry;
                    if (!entries.TryGetValue(hookId, out entry)) return Result(hookId, ForgeHookState.Failed, false, "No registered hook exists for this ID.");
                    if (!accepting) return Result(entry, false, "The hook service is disconnected.");
                    if (Volatile.Read(ref acceptsNewHooks) == 0) return Result(entry, false, "The hook service is shutting down and rejects new hook applications.");
                    if (!MayMutate()) return Result(entry, false, "Hook changes are allowed only from the host's approved main-menu context.");
#if NETFRAMEWORK
                    if (entry.RetiringActivations.Count != 0 || (entry.Activation != null && entry.Activation.IsRetiring))
                        return Result(entry, false, "The previous hook activation is still completing dispatch or cleanup; reapply is blocked until its lease and target reservation are released.");
#endif
                    if (entry.State == ForgeHookState.Applied) return Verify(hookId);
                    if (entry.State == ForgeHookState.Conflict || entry.State == ForgeHookState.Failed)
                        return Result(entry, false, "Resolve the recorded conflict or failure before applying this hook again.");
                    if (IsRawDetourTracked(entry.Target)) return Result(entry, false, "The legacy ForgeDetour backend already owns this target.");

#if NETFRAMEWORK
                if (entry.RetiringActivations.Count != 0)
                    return Result(entry, false, "The previous hook activation is still completing dispatch or cleanup; reapply is blocked until its lease is released.");
                if (entry.Activation != null)
                    return Result(entry, false, "A previous hook activation is still retained; resolve its cleanup conflict before applying again.");
                DispatchActivation activation = null;
                try
                {
                    string dispatchId = Guid.NewGuid().ToString("N");
                    activation = new DispatchActivation(this, entry, dispatchId, BuildOriginalInvoker(entry.Target));
                    var method = BuildDetourMethod(entry, dispatchId);
                    activation.DetourMethod = method;
                    var configId = "CalradiaForge.Hook." + entry.Id;
                    var config = new DetourConfig(configId, entry.Priority,
                        PrefixIds(entry.Before), PrefixIds(entry.After));
                    if (!DispatchEntries.TryAdd(dispatchId, activation))
                        throw new InvalidOperationException("Unable to reserve the internal callback route.");
                    entry.Activation = activation;
                    var hook = new Hook(entry.Target, method, config, false);
                    activation.Hook = hook;
                    if (!hook.IsValid) throw new InvalidOperationException("MonoMod rejected the target/detour signature.");
                    // Reserve before MonoMod mutates its chain. ForgeDetour and this service
                    // share Gate, so raw writes cannot pass their check while apply is in flight.
                    ForgeDetour.RegisterHookTarget(entry.Target);
                    activation.MarkTargetCounted();
                    hook.Apply();
                    if (!hookIsApplied(hook) || !hook.IsValid) throw new InvalidOperationException("MonoMod did not confirm the hook after applying it.");
                    entry.State = ForgeHookState.Applied;
                    entry.Detail = "MonoMod RuntimeDetour chain applied and verified.";
                    if (!applyOrder.Contains(entry.Id, StringComparer.OrdinalIgnoreCase)) applyOrder.Add(entry.Id);
                    return Result(entry, true);
                }
                catch (Exception error)
                {
                    bool? applied = null;
                    Hook hook = activation?.Hook;
                    try { if (hook != null) applied = hookIsApplied(hook); } catch { }
                    if (hook != null && applied != false)
                    {
                        if (activation != null && !activation.HasTargetReservation)
                        {
                            ForgeDetour.RegisterHookTarget(entry.Target);
                            activation.MarkTargetCounted();
                        }
                        entry.State = ForgeHookState.Conflict;
                        entry.Detail = Bound("MonoMod apply failed and hook removal is unverified; the hook and callback remain retained: " + error.Message, 240);
                        if (!applyOrder.Contains(entry.Id, StringComparer.OrdinalIgnoreCase)) applyOrder.Add(entry.Id);
                        return Result(entry, false);
                    }
                    if (activation != null)
                    {
                        bool disposed = RetireActivation(entry, activation);
                        if (!disposed)
                        {
                            if (activation.DisposalError == null)
                            {
                                entry.State = ForgeHookState.Conflict;
                                entry.Detail = "MonoMod apply failed while the callback activation was still in flight; cleanup is deferred and reapply is blocked.";
                            }
                            if (!applyOrder.Contains(entry.Id, StringComparer.OrdinalIgnoreCase)) applyOrder.Add(entry.Id);
                            return Result(entry, false);
                        }
                    }
                    entry.State = ForgeHookState.Failed;
                    entry.Detail = Bound("MonoMod apply failed: " + error.Message, 240);
                    return Result(entry, false);
                }
#else
                entry.State = ForgeHookState.Unsupported;
                entry.Detail = "The RuntimeDetour backend is available only in the Bannerlord net472 host.";
                return Result(entry, false);
#endif
                }
            }
        }

        public ForgeHookOperationResult Verify(string hookId)
        {
            if (string.IsNullOrWhiteSpace(hookId)) throw new ArgumentException("A hook ID is required.", nameof(hookId));
            lock (gate)
            {
                Entry entry;
                if (!entries.TryGetValue(hookId, out entry)) return Result(hookId, ForgeHookState.Failed, false, "No registered hook exists for this ID.");
#if NETFRAMEWORK
                try
                {
                    DispatchActivation activation = entry.Activation;
                    if (activation != null && activation.DeferredUndoRequested)
                    {
                        entry.State = ForgeHookState.Applied;
                        entry.Detail = "Revert request is pending until the active dispatch exits in an approved host context; Undo is not yet verified.";
                        return Result(entry, false);
                    }
                    Hook hook = entry.Activation?.Hook;
                    if (hook == null)
                    {
                        bool inactive = entry.State == ForgeHookState.Registered || entry.State == ForgeHookState.Reverted;
                        if (!inactive)
                        {
                            entry.State = ForgeHookState.Conflict;
                            entry.Detail = "Forge has no retained RuntimeDetour handle for a hook recorded as active or uncertain.";
                        }
                        return Result(entry, inactive, entry.Detail);
                    }
                    bool valid = hook.IsValid;
                    bool applied = hookIsApplied(hook);
                    if (valid && applied && entry.State == ForgeHookState.Applied)
                        return Result(entry, true, "MonoMod reports the owned detour chain active.");
                    if (!applied && entry.State == ForgeHookState.Reverted)
                        return Result(entry, true, "MonoMod reports the owned detour removed.");
                    entry.State = valid && applied ? ForgeHookState.Applied : ForgeHookState.Conflict;
                    entry.Detail = "The live MonoMod hook state differs from Forge's recorded lifecycle state.";
                    return Result(entry, false);
                }
                catch (Exception error)
                {
                    entry.State = ForgeHookState.Conflict;
                    entry.Detail = Bound("MonoMod hook status could not be verified; handle retained for review: " + error.Message, 240);
                    return Result(entry, false);
                }
#else
                return Result(entry, entry.State == ForgeHookState.Registered, entry.State == ForgeHookState.Registered ? "Registered; this target framework cannot apply hooks." : entry.Detail);
#endif
            }
        }

        public ForgeHookOperationResult Revert(string hookId)
        {
            if (string.IsNullOrWhiteSpace(hookId)) throw new ArgumentException("A hook ID is required.", nameof(hookId));
            lock (gate)
            {
                lock (ForgeDetour.Gate)
                {
                    Entry entry;
                    if (!entries.TryGetValue(hookId, out entry)) return Result(hookId, ForgeHookState.Failed, false, "No registered hook exists for this ID.");
                    bool alreadyInactive = entry.State == ForgeHookState.Reverted || entry.State == ForgeHookState.Registered;
#if NETFRAMEWORK
                    alreadyInactive = alreadyInactive && entry.Activation == null && entry.RetiringActivations.Count == 0;
#endif
                    if (alreadyInactive)
                        return Result(entry, true, "The hook is already inactive; no detour change was needed.");
                    if (!MayMutate()) return Result(entry, false, "Hook changes are allowed only from the host's approved main-menu context.");
                    return RevertOwned(entry);
                }
            }
        }

        public IReadOnlyList<ForgeHookOperationResult> RevertOwner(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("An owner label is required.", nameof(owner));
            lock (gate)
                lock (ForgeDetour.Gate)
                {
                    var selected = ReverseIds().Where(id => entries[id].Owner.Equals(owner, StringComparison.OrdinalIgnoreCase))
                        .Select(id => entries[id]).ToArray();
                    if (!MayMutate()) return RevertBlockedByContext(selected);
                    return selected.Select(RevertOwned).ToArray();
                }
        }

        public IReadOnlyList<ForgeHookOperationResult> RevertAll()
        {
            lock (gate)
                lock (ForgeDetour.Gate)
                {
                    Entry[] selected = ReverseIds().Select(id => entries[id]).ToArray();
                    if (!MayMutate()) return RevertBlockedByContext(selected);
                    return selected.Select(RevertOwned).ToArray();
                }
        }

        ForgeHookOperationResult[] RevertBlockedByContext(IEnumerable<Entry> selected)
        {
            var results = new List<ForgeHookOperationResult>();
            foreach (Entry entry in selected)
            {
                if (entry.State == ForgeHookState.Failed && !HasRetainedActivation(entry))
                {
                    // A clean apply failure has no active detour or callback route to mutate.
                    // Clear that inert record while preserving the main-menu gate for any
                    // active or uncertain hook that would require touching RuntimeDetour.
                    results.Add(RevertOwned(entry));
                    continue;
                }

                if (entry.State != ForgeHookState.Applied && entry.State != ForgeHookState.Conflict &&
                    entry.State != ForgeHookState.Failed) continue;

                string detail = "Hook changes are allowed only from the host's approved main-menu context; no revert was attempted.";
                if (!string.IsNullOrWhiteSpace(entry.Detail)) detail += " Current record: " + entry.Detail;
                results.Add(Result(entry, false, Bound(detail, 240)));
            }
            return results.ToArray();
        }

        static bool HasRetainedActivation(Entry entry)
        {
#if NETFRAMEWORK
            return entry.Activation != null || entry.RetiringActivations.Count != 0;
#else
            return false;
#endif
        }

        public void Disconnect()
        {
            lock (gate)
            {
                lock (ForgeDetour.Gate)
                {
                    if (HasActiveDispatches())
                        throw new InvalidOperationException("Cannot disconnect the hook service while a hook dispatch is active; retry after it exits in an approved main-menu context.");
                    if (HasUnresolvedHooks() && !MayMutate())
                        throw new InvalidOperationException("Cannot disconnect the hook service while active or uncertain detours remain outside the approved game-thread main-menu context. Revert them there first.");
                    foreach (string id in ReverseIds())
                    {
                        Entry entry = entries[id];
                        bool retained = entry.State == ForgeHookState.Applied || entry.State == ForgeHookState.Conflict || entry.State == ForgeHookState.Failed;
#if NETFRAMEWORK
                        retained = retained || entry.Activation != null || entry.RetiringActivations.Count != 0;
#endif
                        if (retained)
                        {
                            ForgeHookOperationResult result = RevertOwned(entry);
                            bool cleanupPending = false;
#if NETFRAMEWORK
                            cleanupPending = entry.Activation != null || entry.RetiringActivations.Count != 0;
#endif
                            if (!result.Succeeded || cleanupPending)
                                throw new InvalidOperationException("Cannot disconnect the hook service because hook '" + id + "' is still active, pending, or uncertain: " + result.Detail);
                        }
                    }
                    // A dispatch may have started after the first preflight and before its
                    // route was detached. Never unpublish a service with pending cleanup.
                    if (HasActiveDispatches() || HasUnresolvedHooks())
                        throw new InvalidOperationException("Hook service disconnect was not completed because at least one detour remains active, pending, or uncertain; the service stays connected for explicit recovery.");
                    accepting = false;
                }
            }
        }

        /// <summary>
        /// Makes retained detours callback-inert and blocks new registrations/applications when
        /// the owning runtime begins unloading. Revert and verification remain available through
        /// the normal host mutation gate so a published service can still recover retained hooks.
        /// </summary>
        internal void StopCallbacksForUnload()
        {
            // Close the application path first. A concurrent Apply can at worst finish installing
            // an inert detour; callback invocation is separately serialized below.
            Interlocked.Exchange(ref callbacksEnabled, 0);
            Interlocked.Exchange(ref acceptsNewHooks, 0);
            // Drain a callback that acquired the gate before callbacksEnabled changed. Once this
            // method returns, no hook callback is still executing or can begin on a retained route.
            lock (callbackInvocationGate) { }
        }

        public bool CanDisconnect(out string reason)
        {
            lock (gate)
            {
                if (HasActiveDispatches())
                {
                    reason = "An active hook dispatch must finish before the service can disconnect.";
                    return false;
                }
                if (!HasUnresolvedHooks() || MayMutate())
                {
                    reason = string.Empty;
                    return true;
                }
                reason = "Active or uncertain detours can be removed only on the approved game-thread main-menu context.";
                return false;
            }
        }

        public void Reconnect()
        {
            lock (gate)
            {
                if (accepting) return;
                foreach (Entry entry in entries.Values)
                {
#if NETFRAMEWORK
                    if (entry.Activation != null || entry.RetiringActivations.Count != 0 || entry.State == ForgeHookState.Conflict || entry.State == ForgeHookState.Failed)
                        throw new InvalidOperationException("The hook service retains an active, conflicted, or uncertain hook; resolve it before reconnecting.");
#else
                    if (entry.State == ForgeHookState.Conflict || entry.State == ForgeHookState.Failed)
                        throw new InvalidOperationException("The hook service retains an uncertain hook; resolve it before reconnecting.");
#endif
                }
                accepting = true;
            }
        }

        internal static bool HasAppliedTarget(MethodInfo target)
        {
#if NETFRAMEWORK
            return ForgeDetour.IsHookTargetReserved(target);
#else
            return false;
#endif
        }

        ForgeHookOperationResult RevertOwned(Entry entry)
        {
            bool retainedActivation = false;
#if NETFRAMEWORK
            retainedActivation = entry.Activation != null || entry.RetiringActivations.Count != 0;
            if (entry.Activation == null && entry.RetiringActivations.Count != 0)
                return Result(entry, false, "A retiring hook activation remains; cleanup is still pending and cannot be cleared as an inert failure.");
#endif
            if (entry.State == ForgeHookState.Failed && !retainedActivation)
            {
                applyOrder.RemoveAll(id => string.Equals(id, entry.Id, StringComparison.OrdinalIgnoreCase));
                entry.State = ForgeHookState.Reverted;
                entry.Detail = "No retained detour remains; the clean apply failure was cleared.";
                return Result(entry, true, entry.Detail);
            }
            if ((entry.State == ForgeHookState.Applied || entry.State == ForgeHookState.Conflict) && !retainedActivation)
            {
                entry.State = ForgeHookState.Conflict;
                entry.Detail = "No retained hook handle is available to verify removal; the active or uncertain hook remains a conflict.";
                return Result(entry, false);
            }
#if NETFRAMEWORK
            try
            {
                DispatchActivation activation = entry.Activation;
                if (activation != null)
                {
                    // Close the internal route before checking active leases. This is
                    // serialized with Dispatch's route lookup, so no new callback lease can
                    // start after this point.
                    DetachActivationForRevert(entry, activation);
                    if (activation.HasActiveDispatches)
                    {
                        entry.State = ForgeHookState.Applied;
                        entry.Detail = "Revert request is pending until the active dispatch exits in an approved host context; Undo is not yet verified.";
                        return Result(entry, false);
                    }
                    if (!MayMutate())
                    {
                        entry.State = ForgeHookState.Applied;
                        entry.Detail = "Revert remains pending; the approved game-thread main-menu gate must be true before Undo, verification, or disposal.";
                        return Result(entry, false);
                    }

                    Hook hook = activation.Hook;
                    if (!activation.UndoVerified)
                    {
                        if (hook != null)
                        {
                            bool applied = hookIsApplied(hook);
                            if (applied) hook.Undo();
                            if (hookIsApplied(hook) || !hook.IsValid)
                            {
                                entry.State = ForgeHookState.Conflict;
                                entry.Detail = "MonoMod could not confirm removal; the hook, callback, and target reservation remain retained.";
                                return Result(entry, false);
                            }
                        }
                        activation.MarkUndoVerified();
                    }

                    RemoveAppliedOrder(entry.Id);
                    entry.State = ForgeHookState.Reverted;
                    entry.Detail = "MonoMod removal was verified; hook disposal and target-reservation release are pending.";
                    bool disposedNow = activation.DisposeRetired();
                    if (disposedNow)
                        return Result(entry, true, entry.Detail);

                    string cleanupError = activation.DisposalError;
                    if (cleanupError != null) entry.Detail = cleanupError;
                    return Result(entry, false, entry.Detail);
                }

                RemoveAppliedOrder(entry.Id);
                entry.State = ForgeHookState.Reverted;
                entry.Detail = "MonoMod reports no retained applied detour for this record.";
                return Result(entry, true, entry.Detail);
            }
            catch (Exception error)
            {
                entry.State = ForgeHookState.Conflict;
                entry.Detail = Bound("MonoMod revert failed; callback and hook are retained for review: " + error.Message, 240);
                return Result(entry, false);
            }
#else
            entry.State = ForgeHookState.Unsupported;
            entry.Detail = "The RuntimeDetour backend is available only in the Bannerlord net472 host.";
            return Result(entry, false);
#endif
        }

#if NETFRAMEWORK
        void FinishDeferredRevert(DispatchActivation activation)
        {
            lock (gate)
            {
                lock (ForgeDetour.Gate)
                {
                    Entry entry;
                    if (!entries.TryGetValue(activation.Entry.Id, out entry) || !ReferenceEquals(entry.Activation, activation))
                        return;
                    if (!activation.IsRetiring || activation.HasActiveDispatches) return;

                    // A callback or the original target can transition away from the
                    // approved host context. Re-check on the exiting caller thread and
                    // retain the hook/reservation when the gate has closed.
                    if (!MayMutate())
                    {
                        entry.State = ForgeHookState.Applied;
                        entry.Detail = "Revert remains pending; the dispatch exited outside the approved game-thread main-menu context. No Undo or disposal was attempted.";
                        return;
                    }
                    RevertOwned(entry);
                }
            }
        }

        bool DetachActivationForRevert(Entry entry, DispatchActivation activation)
        {
            lock (DispatchGate)
            {
                entry.RetiringActivations.TryAdd(activation.DispatchId, activation);
                DispatchActivation routed;
                if (DispatchEntries.TryGetValue(activation.DispatchId, out routed) && ReferenceEquals(routed, activation))
                    DispatchEntries.TryRemove(activation.DispatchId, out _);
                return activation.RequestRetirement();
            }
        }
#endif

        bool MayMutate()
        {
            try { return mayMutate(); }
            catch { return false; }
        }

        bool HasUnresolvedHooks() => entries.Values.Any(entry =>
#if NETFRAMEWORK
            entry.Activation != null || entry.RetiringActivations.Count != 0 ||
#endif
            entry.State == ForgeHookState.Applied || entry.State == ForgeHookState.Conflict);

#if NETFRAMEWORK
        bool HasActiveDispatches() => entries.Values.Any(entry =>
            (entry.Activation != null && entry.Activation.HasActiveDispatches) ||
            entry.RetiringActivations.Values.Any(activation => activation.HasActiveDispatches));
#else
        bool HasActiveDispatches() => false;
#endif

        IEnumerable<string> ReverseIds()
        {
            var ids = applyOrder.AsEnumerable().Reverse().ToList();
            var seen = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
            // A clean Apply failure is intentionally not added to applyOrder because no
            // hook was left active. Include its inert Failed record in collective lifecycle
            // operations; conflicts remain governed by their retained apply-order record.
            foreach (Entry entry in entries.Values)
                if (entry.State == ForgeHookState.Failed && seen.Add(entry.Id)) ids.Add(entry.Id);
            return ids;
        }

        ForgeHookSnapshot Snapshot(Entry entry) => new ForgeHookSnapshot(entry.Id, entry.Owner, Identity(entry.Target),
            entry.Prefix != null, entry.Postfix != null, entry.Priority, entry.Before, entry.After, entry.State, entry.Detail);

        static ForgeHookOperationResult Result(Entry entry, bool succeeded, string detail = null) =>
            new ForgeHookOperationResult(entry.Id, entry.State, succeeded, detail ?? entry.Detail);

        static ForgeHookOperationResult Result(string id, ForgeHookState state, bool succeeded, string detail) =>
            new ForgeHookOperationResult(id, state, succeeded, detail);

        static void ValidateDefinition(ForgeHookDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            ValidateToken(definition.Id, "hook ID");
            ValidateToken(definition.Owner, "owner");
            if (definition.Target == null) throw new ArgumentNullException(nameof(definition.Target));
            if (definition.Prefix == null && definition.Postfix == null) throw new ArgumentException("At least one Prefix or Postfix callback is required.");
            if (definition.Priority.HasValue && (definition.Priority.Value < -1000 || definition.Priority.Value > 1000)) throw new ArgumentOutOfRangeException(nameof(definition.Priority));
            ValidateOrder(definition.Before, definition.Id, nameof(definition.Before));
            ValidateOrder(definition.After, definition.Id, nameof(definition.After));
            ValidateNoContradictoryOrder(definition.Before, definition.After);
            MethodInfo target = definition.Target;
            if (target.ContainsGenericParameters || target.DeclaringType == null || target.DeclaringType.ContainsGenericParameters)
                throw new ArgumentException("Open generic methods and declaring types are not supported.", nameof(definition.Target));
            if (target.IsAbstract || target.IsConstructor || target.IsGenericMethodDefinition || (target.Attributes & MethodAttributes.PinvokeImpl) != 0 ||
                (target.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0 || target.CallingConvention.HasFlag(CallingConventions.VarArgs))
                throw new ArgumentException("Abstract, constructor, P/Invoke, internal-call, and varargs targets are not supported.", nameof(definition.Target));
            if (!target.IsStatic && target.DeclaringType.IsValueType) throw new ArgumentException("Value-type instance methods are not supported.", nameof(definition.Target));
            if (UnsupportedType(target.ReturnType)) throw new ArgumentException("By-ref, pointer, runtime-special, and byref-like return types are not supported.", nameof(definition.Target));
            ParameterInfo[] parameters = target.GetParameters();
            if (parameters.Length > 12 || parameters.Any(parameter => UnsupportedType(parameter.ParameterType)))
                throw new ArgumentException("The target has by-ref, pointer, runtime-special, byref-like, or too many parameters for the managed hook adapter.", nameof(definition.Target));
        }

        static bool UnsupportedType(Type type)
        {
            // Compare runtime-special types before querying Type flags: some runtimes throw
            // NotSupportedException when IsPointer/IsByRef is read for these signature types.
            if (type == typeof(TypedReference) || type == typeof(ArgIterator) || type == typeof(RuntimeArgumentHandle))
                return true;

            try
            {
                if (type == null || type.IsPointer || type.IsByRef) return true;

                // Reject runtime-special element/generic shapes before building a delegate or
                // emitting an adapter that would have to box them.
                if (type.HasElementType && UnsupportedType(type.GetElementType())) return true;
                if (type.IsGenericType && type.GetGenericArguments().Any(UnsupportedType)) return true;
                return type.GetCustomAttributesData().Any(attribute =>
                    attribute.AttributeType.FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute");
            }
            catch (NotSupportedException)
            {
                return true;
            }
        }

        static void ValidateToken(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.Any(char.IsControl) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("A " + label + " must be 1-128 printable characters without surrounding whitespace.", label);
        }

        static void ValidateOrder(IEnumerable<string> values, string ownId, string parameter)
        {
            if (values == null) return;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string value in values)
            {
                ValidateToken(value, parameter);
                string effectiveId = EffectiveOrderId(value);
                if (effectiveId.Equals(EffectiveHookId(ownId), StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("A hook cannot order itself.", parameter);
                if (!seen.Add(effectiveId)) throw new ArgumentException("Duplicate detour order ID: " + value, parameter);
            }
        }

        static string EffectiveHookId(string id) => "CalradiaForge.Hook." + id;
        static string EffectiveOrderId(string id) => id.StartsWith("CalradiaForge.Hook.", StringComparison.OrdinalIgnoreCase)
            ? id : "CalradiaForge.Hook." + id;

        static void ValidateNoContradictoryOrder(IEnumerable<string> before, IEnumerable<string> after)
        {
            if (before == null || after == null) return;
            var beforeIds = new HashSet<string>(before.Select(EffectiveOrderId), StringComparer.OrdinalIgnoreCase);
            if (after.Select(EffectiveOrderId).Any(beforeIds.Contains))
                throw new ArgumentException("A hook cannot place the same hook ID in both Before and After ordering lists.");
        }

#if NETFRAMEWORK
        static string[] PrefixIds(IEnumerable<string> ids) => ids.Select(id => id.StartsWith("CalradiaForge.Hook.", StringComparison.OrdinalIgnoreCase) ? id : "CalradiaForge.Hook." + id).ToArray();

        static Type GetOriginalDelegateType(MethodInfo target)
        {
            Type[] parameters = target.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
            Type[] originalParameters = target.IsStatic ? parameters : new[] { target.DeclaringType }.Concat(parameters).ToArray();
            Type returnType = target.ReturnType;
            Type[] originalSignature = originalParameters.Concat(new[] { returnType }).ToArray();
            return returnType == typeof(void)
                ? Expression.GetActionType(originalParameters)
                : Expression.GetDelegateType(originalSignature);
        }

        static Func<Delegate, object, object[], object> BuildOriginalInvoker(MethodInfo target)
        {
            Type originalDelegate = GetOriginalDelegateType(target);
            Type[] parameters = target.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
            Type returnType = target.ReturnType;
            var method = new DynamicMethod("CalradiaForge_Original_" + target.Name, typeof(object),
                new[] { typeof(Delegate), typeof(object), typeof(object[]) }, typeof(ForgeHookService), true);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, originalDelegate);
            if (!target.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, target.DeclaringType);
            }
            for (int i = 0; i < parameters.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);
                if (parameters[i].IsValueType) il.Emit(OpCodes.Unbox_Any, parameters[i]);
                else il.Emit(OpCodes.Castclass, parameters[i]);
            }
            il.Emit(OpCodes.Callvirt, originalDelegate.GetMethod("Invoke"));
            if (returnType == typeof(void)) il.Emit(OpCodes.Ldnull);
            else if (returnType.IsValueType) il.Emit(OpCodes.Box, returnType);
            il.Emit(OpCodes.Ret);
            return (Func<Delegate, object, object[], object>)method.CreateDelegate(typeof(Func<Delegate, object, object[], object>));
        }

        static DynamicMethod BuildDetourMethod(Entry entry, string dispatchId)
        {
            Type[] parameters = entry.ParameterTypes;
            Type[] originalParameters = entry.Target.IsStatic ? parameters : new[] { entry.Target.DeclaringType }.Concat(parameters).ToArray();
            Type returnType = entry.Target.ReturnType;
            Type originalDelegate = GetOriginalDelegateType(entry.Target);
            Type[] detourParameters = new[] { originalDelegate }.Concat(originalParameters).ToArray();
            var method = new DynamicMethod("CalradiaForge_Hook_" + dispatchId, returnType, detourParameters, typeof(ForgeHookService), true);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, dispatchId);
            if (entry.Target.IsStatic) il.Emit(OpCodes.Ldnull);
            else il.Emit(OpCodes.Ldarg_1);
            il.Emit(entry.Target.IsStatic ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            EmitObjectArray(il, parameters.Length, entry.Target.IsStatic ? 1 : 2, parameters);
            il.Emit(OpCodes.Call, typeof(ForgeHookService).GetMethod(nameof(Dispatch), BindingFlags.NonPublic | BindingFlags.Static));
            if (returnType == typeof(void)) il.Emit(OpCodes.Pop);
            else if (returnType.IsValueType) il.Emit(OpCodes.Unbox_Any, returnType);
            else il.Emit(OpCodes.Castclass, returnType);
            il.Emit(OpCodes.Ret);
            return method;
        }

        static void EmitObjectArray(ILGenerator il, int length, int firstArg, Type[] types)
        {
            il.Emit(OpCodes.Ldc_I4, length);
            il.Emit(OpCodes.Newarr, typeof(object));
            for (int i = 0; i < length; i++)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg, (short)(firstArg + i));
                if (types[i].IsValueType) il.Emit(OpCodes.Box, types[i]);
                il.Emit(OpCodes.Stelem_Ref);
            }
        }

        static object Dispatch(Delegate original, string dispatchId, object instance, bool isStatic, object[] args)
        {
            DispatchActivation activation = null;
            bool entered;
            lock (DispatchGate)
                entered = DispatchEntries.TryGetValue(dispatchId, out activation) && activation.TryEnter();
            if (!entered)
                return InvokeOriginalFallback(original, instance, args, isStatic);

            try
            {
                Entry entry = activation.Entry;
                // Keep this lease's exact original delegate and invoker alive across callbacks.
                // A callback can Undo its own detour; Hook.Dispose must wait for this Dispatch.
                Func<Delegate, object, object[], object> originalInvoker = Volatile.Read(ref activation.OriginalInvoker);
                if (originalInvoker == null) return InvokeOriginalFallback(original, instance, args, isStatic);
                object[] callbackArgs = args ?? Array.Empty<object>();
                // Detours can outlive the menu context in which they were installed. Keep the
                // detour inert outside the same game-thread context used for Apply/Revert.
                if (!activation.Service.CallbackAllowed(entry)) return InvokeOriginal(originalInvoker, original, instance, callbackArgs);
                object[] originalArgs = entry.Prefix == null ? callbackArgs : (object[])callbackArgs.Clone();
                var invocation = new ForgeHookInvocation(instance, callbackArgs);
                try
                {
                    if (entry.Prefix != null && !activation.Service.TryInvokeCallback(entry, entry.Prefix, invocation))
                        return InvokeOriginal(originalInvoker, original, instance, originalArgs);
                }
                catch (Exception error)
                {
                    invocation = new ForgeHookInvocation(instance, originalArgs);
                    entry.Detail = Bound("Prefix callback failed; original call continued: " + error.Message, 240);
                }
                // A Prefix can synchronously leave the approved host context. In that case,
                // discard its argument edits/cancellation and preserve the original call.
                if (!activation.Service.CallbackAllowed(entry)) return InvokeOriginal(originalInvoker, original, instance, originalArgs);
                if (!ArgumentsValid(entry.ParameterTypes, invocation.Arguments)) invocation = new ForgeHookInvocation(instance, originalArgs);
                object result;
                if (invocation.RunOriginal)
                {
                    result = InvokeOriginal(originalInvoker, original, instance, invocation.Arguments);
                }
                else if (entry.Target.ReturnType == typeof(void))
                {
                    // A void Prefix can safely suppress the original call without supplying a result.
                    result = null;
                }
                else if (ResultValid(entry.Target.ReturnType, invocation.Result))
                {
                    result = invocation.Result;
                }
                else
                {
                    entry.Detail = "Prefix supplied an invalid result; original call continued.";
                    result = InvokeOriginal(originalInvoker, original, instance, invocation.Arguments);
                }
                invocation.Result = result;
                // The original method may transition away from the main menu. Do not run
                // another user callback after that transition.
                if (!activation.Service.CallbackAllowed(entry)) return result;
                try
                {
                    if (entry.Postfix != null && !activation.Service.TryInvokeCallback(entry, entry.Postfix, invocation))
                        return result;
                }
                catch (Exception error)
                {
                    entry.Detail = Bound("Postfix callback failed; original result was preserved: " + error.Message, 240);
                    return result;
                }
                if (entry.Target.ReturnType == typeof(void)) return null;
                return ResultValid(entry.Target.ReturnType, invocation.Result) ? invocation.Result : result;
            }
            finally
            {
                activation.Exit();
            }
        }

        bool CallbackAllowed(Entry entry)
        {
            if (Volatile.Read(ref callbacksEnabled) == 0) return false;
            try { return entry.CanInvoke != null && entry.CanInvoke(); }
            catch { return false; }
        }

        bool TryInvokeCallback(Entry entry, ForgeHookCallback callback, ForgeHookInvocation invocation)
        {
            lock (callbackInvocationGate)
            {
                // Serialize the final permission check and callback entry against unload. Once
                // StopCallbacksForUnload returns, no retained detour can begin user callback code.
                if (!CallbackAllowed(entry)) return false;
                callback(invocation);
                return true;
            }
        }

        static object InvokeOriginal(Func<Delegate, object, object[], object> originalInvoker, Delegate original, object instance, object[] args)
        {
            return originalInvoker(original, instance, args ?? Array.Empty<object>());
        }

        static object InvokeOriginalFallback(Delegate original, object instance, object[] args, bool isStatic)
        {
            var call = isStatic ? (args ?? Array.Empty<object>()) : new[] { instance }.Concat(args ?? Array.Empty<object>()).ToArray();
            try { return original.DynamicInvoke(call); }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }

        static bool ArgumentsValid(Type[] parameterTypes, object[] values)
        {
            if (parameterTypes.Length != values.Length) return false;
            for (int i = 0; i < parameterTypes.Length; i++) if (!ResultValid(parameterTypes[i], values[i])) return false;
            return true;
        }

        static bool ResultValid(Type type, object value) => value == null ? !type.IsValueType || Nullable.GetUnderlyingType(type) != null : type.IsInstanceOfType(value);

        bool RetireActivation(Entry entry, DispatchActivation activation)
        {
            if (activation == null) return true;
            try
            {
                DetachActivationForRevert(entry, activation);
                if (activation.HasActiveDispatches || !MayMutate()) return false;
                Hook hook = activation.Hook;
                if (hook != null && hookIsApplied(hook)) hook.Undo();
                if (hook != null && hookIsApplied(hook))
                {
                    entry.State = ForgeHookState.Conflict;
                    entry.Detail = "Apply cleanup could not verify that MonoMod removed the hook; activation and target reservation remain retained.";
                    return false;
                }
                activation.MarkUndoVerified();
                return activation.DisposeRetired();
            }
            catch (Exception error)
            {
                entry.State = ForgeHookState.Conflict;
                entry.Detail = Bound("Apply cleanup remains unverified; hook and target reservation are retained: " + error.Message, 240);
                return false;
            }
        }

        void RemoveAppliedOrder(string hookId)
        {
            applyOrder.RemoveAll(id => string.Equals(id, hookId, StringComparison.OrdinalIgnoreCase));
        }
#endif

        static bool IsRawDetourTracked(MethodInfo target)
        {
#if NETFRAMEWORK
            try { return ForgeDetour.IsTracked(target); } catch { return true; }
#else
            return false;
#endif
        }

        static string Identity(MethodInfo method) => method.DeclaringType.FullName + "." + method.Name + "(" +
            string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.FullName ?? parameter.ParameterType.Name)) + ")";
        static string Bound(string value, int maximum) => string.IsNullOrEmpty(value) || value.Length <= maximum ? value ?? string.Empty : value.Substring(0, maximum);

        sealed class Handle : IForgeHookHandle
        {
            readonly ForgeHookService service;
            readonly string id;
            internal Handle(ForgeHookService service, string id) { this.service = service; this.id = id; }
            public ForgeHookSnapshot Snapshot => service.GetSnapshots().FirstOrDefault(snapshot => string.Equals(snapshot.Id, id, StringComparison.OrdinalIgnoreCase))
                ?? new ForgeHookSnapshot(id, string.Empty, string.Empty, false, false, null, null, null, ForgeHookState.Failed, "Hook registration no longer exists.");
            public ForgeHookOperationResult Apply() => service.Apply(id);
            public ForgeHookOperationResult Verify() => service.Verify(id);
            public ForgeHookOperationResult Revert() => service.Revert(id);
            public void Dispose()
            {
                ForgeHookOperationResult result = service.Revert(id);
                bool requestDeferred = !result.Succeeded && result.State == ForgeHookState.Applied &&
                    result.Detail.StartsWith("Revert request is pending until the active dispatch exits", StringComparison.Ordinal);
                if (!result.Succeeded && !requestDeferred)
                    throw new InvalidOperationException("Hook handle disposal could not verify reversion for '" + id + "': " + result.Detail);
            }
        }
    }
}
