using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using CalradiaForge.Core;
using CalradiaForge.Sdk;
using CalradiaForge.Sdk.Patcher;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace CalradiaForge.DetourFixture
{
    internal static partial class Program
    {
        static readonly Exception FinalizerOriginalError = new InvalidOperationException("fixture original failure");
        static bool finalizerTargetThrows;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static int FinalizerTarget(int value)
        {
            if (finalizerTargetThrows) throw FinalizerOriginalError;
            return value + 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static int TranspilerTarget(int value) { return value + 1; }

        static void RequireHook(bool condition, string detail)
        {
            if (!condition) throw new InvalidOperationException("Hook extension fixture: " + detail);
        }

        static Exception CaptureHookFailure(Action action)
        {
            try { action(); return null; }
            catch (Exception error) { return error; }
        }

        static int RunHookExtensionsFixture()
        {
            try
            {
                RunFinalizerCases();
                RunTranspilerCases();
                Console.WriteLine("PASS: Finalizer failures, IL transformations/order/rebuild/coexistence, context guards, uncertain status, and lifecycle verified.");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("FAIL: " + error);
                return 1;
            }
            finally { finalizerTargetThrows = false; }
        }

        static void RunFinalizerCases()
        {
            MethodInfo target = typeof(Program).GetMethod(nameof(FinalizerTarget), BindingFlags.NonPublic | BindingFlags.Static);
            RuntimeHelpers.PrepareMethod(target.MethodHandle);
            bool allowed = true;
            var service = new ForgeHookService(() => allowed);
            int index = 0;
            Action<ForgeHookDefinition, Action> run = (definition, assertion) =>
            {
                definition.Id = "finalizer-case-" + ++index;
                definition.Owner = "DetourFixture";
                definition.Target = target;
                IForgeHookHandle handle = service.Register(definition);
                try
                {
                    RequireHook(handle.Snapshot.HasFinalizer && handle.Snapshot.State == ForgeHookState.Registered, "inert Finalizer metadata");
                    RequireHook(handle.Apply().Succeeded, "Finalizer apply");
                    assertion();
                }
                finally
                {
                    allowed = true;
                    finalizerTargetThrows = false;
                    RequireHook(handle.Revert().Succeeded, "Finalizer revert");
                }
            };

            run(new ForgeHookDefinition { Finalizer = invocation =>
            {
                RequireHook(invocation.Exception == null, "success pending error");
                invocation.Result = (int)invocation.Result + 10;
            } }, () => RequireHook(FinalizerTarget(2) == 13, "success Finalizer result"));

            run(new ForgeHookDefinition { Finalizer = invocation => RequireHook(ReferenceEquals(invocation.Exception, FinalizerOriginalError), "original error identity") }, () =>
            {
                finalizerTargetThrows = true;
                Exception error = CaptureHookFailure(() => FinalizerTarget(2));
                RequireHook(ReferenceEquals(error, FinalizerOriginalError) && error.StackTrace.Contains(nameof(FinalizerTarget)), "original failure identity and stack");
            });

            run(new ForgeHookDefinition { Finalizer = invocation => { invocation.Exception = null; invocation.Result = 42; } }, () =>
            {
                finalizerTargetThrows = true;
                RequireHook(FinalizerTarget(2) == 42, "exception suppression and valid result");
            });

            run(new ForgeHookDefinition { Finalizer = invocation => invocation.Exception = null }, () =>
            {
                finalizerTargetThrows = true;
                Exception error = CaptureHookFailure(() => FinalizerTarget(2));
                RequireHook(error is InvalidOperationException && ReferenceEquals(error.InnerException, FinalizerOriginalError), "suppression requires valid value-type result");
            });

            Exception replacement = new ApplicationException("fixture replacement failure");
            run(new ForgeHookDefinition { Finalizer = invocation => invocation.Exception = replacement }, () =>
                RequireHook(ReferenceEquals(CaptureHookFailure(() => FinalizerTarget(2)), replacement), "Finalizer replacement error"));

            Exception callbackError = new ApplicationException("fixture callback failure");
            foreach (bool prefixFailure in new[] { true, false })
            {
                bool postfixReached = false;
                run(new ForgeHookDefinition
                {
                    Prefix = prefixFailure ? (ForgeHookCallback)(invocation => { throw callbackError; }) : null,
                    Postfix = invocation => { postfixReached = true; if (!prefixFailure) throw callbackError; },
                    Finalizer = invocation => { RequireHook(ReferenceEquals(invocation.Exception, callbackError), "callback error visible to Finalizer"); invocation.Exception = null; invocation.Result = 50; }
                }, () => RequireHook(FinalizerTarget(2) == 50 && postfixReached != prefixFailure, "callback failure path and Postfix reachability"));
            }

            Exception cleanupError = new ApplicationException("fixture cleanup failure");
            run(new ForgeHookDefinition { Finalizer = invocation => { throw cleanupError; } }, () =>
            {
                RequireHook(ReferenceEquals(CaptureHookFailure(() => FinalizerTarget(2)), cleanupError), "cleanup failure after success");
                finalizerTargetThrows = true;
                var aggregate = CaptureHookFailure(() => FinalizerTarget(2)) as AggregateException;
                RequireHook(aggregate != null && aggregate.InnerExceptions.Count == 2 &&
                    ReferenceEquals(aggregate.InnerExceptions[0], FinalizerOriginalError) && ReferenceEquals(aggregate.InnerExceptions[1], cleanupError), "both original and Finalizer failures preserved");
            });

            int gatedCalls = 0;
            run(new ForgeHookDefinition { Prefix = invocation => allowed = false, Finalizer = invocation => gatedCalls++ }, () =>
                RequireHook(FinalizerTarget(2) == 3 && gatedCalls == 0, "host transition skips Finalizer"));
            service.Disconnect();
        }

        static void RewriteConstant(ILContext context, Func<int, int> transform)
        {
            var cursor = new ILCursor(context);
            int constant = 0;
            RequireHook(cursor.TryGotoNext(instruction => instruction.MatchLdcI4(out constant)), "observable IL constant exists");
            cursor.Next.OpCode = Mono.Cecil.Cil.OpCodes.Ldc_I4;
            cursor.Next.Operand = transform(constant);
        }

        static void RunTranspilerCases()
        {
            MethodInfo target = typeof(Program).GetMethod(nameof(TranspilerTarget), BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo replacement = typeof(Program).GetMethod(nameof(Replacement), BindingFlags.NonPublic | BindingFlags.Static);
            RuntimeHelpers.PrepareMethod(target.MethodHandle);
            bool allowed = true;
            var service = new ForgeHookService(() => allowed);
            int firstCalls = 0;
            int secondCalls = 0;
            bool runtimeRegistrationRejected = false;
            bool transpilerRegistrationRejected = false;
            var add = service.RegisterTranspiler(new ForgeHookDefinition { Id = "il-add", Owner = "DetourFixture", Target = target, Priority = -10, Before = new List<string> { "il-multiply" } }, context =>
            {
                firstCalls++;
                bool reentrantMutationRejected = false;
                try { service.Revert("il-add"); }
                catch (InvalidOperationException) { reentrantMutationRejected = true; }
                RequireHook(reentrantMutationRejected, "reentrant IL mutation rejected");
                try { service.Register(new ForgeHookDefinition { Id = "reentrant-runtime-hook", Owner = "DetourFixture", Target = target, Prefix = _ => { } }); }
                catch (InvalidOperationException) { runtimeRegistrationRejected = true; }
                try { service.RegisterTranspiler(new ForgeHookDefinition { Id = "reentrant-il-hook", Owner = "DetourFixture", Target = target }, nested => { }); }
                catch (InvalidOperationException) { transpilerRegistrationRejected = true; }
                RewriteConstant(context, value => value + 2);
            });
            var multiply = service.RegisterTranspiler(new ForgeHookDefinition { Id = "il-multiply", Owner = "DetourFixture", Target = target, Priority = 100, After = new List<string> { "il-add" } }, context =>
            {
                secondCalls++;
                RewriteConstant(context, value => value * 3);
            });
            try
            {
                RequireHook(add.Snapshot.HasTranspiler && !add.Snapshot.HasPrefix && firstCalls == 0 && TranspilerTarget(2) == 3, "inert transpiler registration and snapshot");
                allowed = false;
                RequireHook(!add.Apply().Succeeded && firstCalls == 0, "transpiler apply context guard");
                allowed = true;
                RequireHook(add.Apply().Succeeded && TranspilerTarget(2) == 5, "observable transformed IL");
                RequireHook(runtimeRegistrationRejected && transpilerRegistrationRejected &&
                    !service.GetSnapshots().Any(snapshot => snapshot.Id == "reentrant-runtime-hook" || snapshot.Id == "reentrant-il-hook"),
                    "runtime-hook and ILHook registration are rejected during transpiler execution");
                bool rawRejected = false;
                try { ForgeDetour.Patch(target, replacement); } catch (InvalidOperationException) { rawRejected = true; }
                RequireHook(rawRejected, "raw detour rejected while ILHook owns reservation");
                int beforeRebuild = firstCalls;
                RequireHook(multiply.Apply().Succeeded && TranspilerTarget(2) == 11 && firstCalls > beforeRebuild && secondCalls > 0, "Before/After overrides priority and reruns manipulator on rebuild");
                var runtime = service.Register(new ForgeHookDefinition { Id = "il-runtime", Owner = "DetourFixture", Target = target,
                    Prefix = invocation => invocation.Arguments[0] = (int)invocation.Arguments[0] + 1,
                    Postfix = invocation => invocation.Result = (int)invocation.Result + 10,
                    Finalizer = invocation => invocation.Result = (int)invocation.Result + 100 });
                RequireHook(runtime.Apply().Succeeded && TranspilerTarget(2) == 122, "ILHook and runtime callbacks coexist");
                RequireHook(runtime.Revert().Succeeded && TranspilerTarget(2) == 11, "runtime removal retains IL body");
                allowed = false;
                string reason;
                RequireHook(TranspilerTarget(2) == 11 && !add.Revert().Succeeded && !service.CanDisconnect(out reason), "transformed body remains active and context blocks management");
                beforeRebuild = firstCalls;
                int secondBeforeRebuild = secondCalls;
                using (var external = new ILHook(target, context => RewriteConstant(context, value => value + 4),
                    new DetourConfig("fixture-external-il", after: new[] { "CalradiaForge.Hook.il-multiply" }), false))
                {
                    external.Apply();
                    RequireHook(TranspilerTarget(2) == 15 && firstCalls > beforeRebuild && secondCalls > secondBeforeRebuild,
                        "external ILHook addition reconstructs explicitly applied Forge IL outside menu context");
                    beforeRebuild = firstCalls;
                    secondBeforeRebuild = secondCalls;
                    external.Undo();
                    RequireHook(TranspilerTarget(2) == 11 && firstCalls > beforeRebuild && secondCalls > secondBeforeRebuild,
                        "external ILHook removal reconstructs explicitly applied Forge IL outside menu context");
                }
                RequireHook(!multiply.Apply().Succeeded && !multiply.Revert().Succeeded && !service.CanDisconnect(out reason),
                    "external reconstruction permission does not reopen Forge menu management");
                allowed = true;
                beforeRebuild = firstCalls;
                RequireHook(multiply.Revert().Succeeded && TranspilerTarget(2) == 5 && firstCalls > beforeRebuild, "Undo rebuilds remaining IL chain");
                FieldInfo statusField = typeof(ForgeHookService).GetField("ilHookIsApplied", BindingFlags.NonPublic | BindingFlags.Instance);
                statusField.SetValue(service, new Func<ILHook, bool>(hook => { throw new InvalidOperationException("fixture unreadable ILHook status"); }));
                RequireHook(!add.Verify().Succeeded && !add.Revert().Succeeded, "unreadable IL status retains uncertain handle");
                rawRejected = false;
                try { ForgeDetour.Patch(target, replacement); } catch (InvalidOperationException) { rawRejected = true; }
                RequireHook(rawRejected, "uncertain IL state retains raw reservation");
                statusField.SetValue(service, new Func<ILHook, bool>(hook => hook.IsApplied));
                RequireHook(add.Revert().Succeeded && TranspilerTarget(2) == 3, "uncertain IL cleanup recovery");
                RequireHook(add.Apply().Succeeded && multiply.Apply().Succeeded && TranspilerTarget(2) == 11, "IL reapply after complete retirement");
                service.Disconnect();
                RequireHook(TranspilerTarget(2) == 3, "disconnect removes IL chain in reverse apply order");
                service.Reconnect();
                RequireHook(add.Apply().Succeeded, "reconnect permits known reverted IL handle");
                RequireHook(add.Revert().Succeeded, "reconnected IL cleanup");
            }
            finally { allowed = true; service.RevertAll(); }

            var failing = service.RegisterTranspiler(new ForgeHookDefinition { Id = "il-failure", Owner = "DetourFixture", Target = target }, context => { throw new ApplicationException("fixture manipulator failure"); });
            ForgeHookOperationResult failed = failing.Apply();
            RequireHook(!failed.Succeeded && (failed.State == ForgeHookState.Failed || failed.State == ForgeHookState.Conflict), "manipulator failure does not report apply success");
            RequireHook(failing.Revert().Succeeded && TranspilerTarget(2) == 3, "failed manipulator cleanup restores original");
            bool mixedRejected = false;
            try { service.RegisterTranspiler(new ForgeHookDefinition { Id = "il-mixed", Owner = "DetourFixture", Target = target, Finalizer = invocation => { } }, context => { }); }
            catch (ArgumentException) { mixedRejected = true; }
            RequireHook(mixedRejected, "transpiler metadata rejects runtime callbacks");
            service.Disconnect();
            RunTranspilerReentrancyDeadlockCase();
            RunCleanIlUnloadCase();
            RunFailedIlUndoCase();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static int ConcurrentTranspilerTarget(int value) => value + 1;

        static void RunTranspilerReentrancyDeadlockCase()
        {
            MethodInfo target = typeof(Program).GetMethod(nameof(ConcurrentTranspilerTarget), BindingFlags.Static | BindingFlags.NonPublic);
            RuntimeHelpers.PrepareMethod(target.MethodHandle);

            using (var callbackEntered = new ManualResetEventSlim(false))
            using (var applyHeldServiceGate = new ManualResetEventSlim(false))
            using (var releaseApplyGate = new ManualResetEventSlim(false))
            using (var externalApplied = new ManualResetEventSlim(false))
            using (var releaseExternal = new ManualResetEventSlim(false))
            using (var probeCompleted = new ManualResetEventSlim(false))
            using (var applyCompleted = new ManualResetEventSlim(false))
            using (var externalCompleted = new ManualResetEventSlim(false))
            {
                int applyThreadId = 0;
                int probeThreadId = 0;
                int pauseNextManipulator = 0;
                int probeWasFast = 0;
                int probeRejectedAllServiceOperations = 0;
                Exception probeError = null;
                Exception applyError = null;
                Exception externalError = null;
                ForgeHookOperationResult applyResult = null;
                var service = new ForgeHookService(() =>
                {
                    if (Thread.CurrentThread.ManagedThreadId == Volatile.Read(ref applyThreadId))
                    {
                        applyHeldServiceGate.Set();
                        // Apply has already entered the service lock here. Keep it there
                        // until the external rebuild callback has exercised its fail-fast path.
                        if (!releaseApplyGate.Wait(TimeSpan.FromSeconds(5))) return false;
                    }
                    return Thread.CurrentThread.ManagedThreadId != Volatile.Read(ref probeThreadId);
                });

                IForgeHookHandle baseHook = service.RegisterTranspiler(new ForgeHookDefinition
                {
                    Id = "concurrent-base", Owner = "fixture", Target = target
                }, context =>
                {
                    if (Interlocked.Exchange(ref pauseNextManipulator, 0) == 1)
                    {
                        callbackEntered.Set();
                        if (!applyHeldServiceGate.Wait(TimeSpan.FromSeconds(5)))
                            throw new TimeoutException("Concurrent service Apply did not reach its guarded host check.");

                        var probe = new Thread(() =>
                        {
                            Volatile.Write(ref probeThreadId, Thread.CurrentThread.ManagedThreadId);
                            try
                            {
                                int rejected = 0;
                                Action[] serviceOperations =
                                {
                                    () => service.GetSnapshots(),
                                    () => service.Apply("concurrent-inactive"),
                                    () => service.Verify("concurrent-inactive"),
                                    () => service.Revert("concurrent-inactive"),
                                    () => service.RevertOwner("fixture"),
                                    () => service.RevertAll(),
                                    () => service.Disconnect(),
                                    () => service.Reconnect()
                                };
                                foreach (Action operation in serviceOperations)
                                {
                                    try { operation(); }
                                    catch (InvalidOperationException) { rejected++; }
                                }
                                string reason;
                                bool canDisconnect = service.CanDisconnect(out reason);
                                Volatile.Write(ref probeRejectedAllServiceOperations,
                                    rejected == serviceOperations.Length && !canDisconnect &&
                                    reason.IndexOf("transpiler callback", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0);
                            }
                            catch (Exception error) { probeError = error; }
                            finally { probeCompleted.Set(); }
                        }) { IsBackground = true, Name = "CalradiaForge transpiler reentrancy probe" };
                        probe.Start();
                        Volatile.Write(ref probeWasFast,
                            probeCompleted.Wait(TimeSpan.FromMilliseconds(750)) ? 1 : 0);
                    }
                    RewriteConstant(context, value => value + 2);
                });

                IForgeHookHandle inactive = service.RegisterTranspiler(new ForgeHookDefinition
                {
                    Id = "concurrent-inactive", Owner = "fixture", Target = target
                }, context => { });
                IForgeHookHandle added = service.RegisterTranspiler(new ForgeHookDefinition
                {
                    Id = "concurrent-added", Owner = "fixture", Target = target
                }, context => { });

                RequireHook(baseHook.Apply().Succeeded, "concurrent fail-fast fixture base hook applies");
                Thread externalThread = new Thread(() =>
                {
                    try
                    {
                        using (var external = new ILHook(target, context => RewriteConstant(context, value => value + 4),
                            new DetourConfig("fixture-concurrent-external-il", after: new[] { "CalradiaForge.Hook.concurrent-base" }), false))
                        {
                            Interlocked.Exchange(ref pauseNextManipulator, 1);
                            external.Apply();
                            externalApplied.Set();
                            if (!releaseExternal.Wait(TimeSpan.FromSeconds(8)))
                                throw new TimeoutException("The concurrent external ILHook was not released for cleanup.");
                            external.Undo();
                        }
                    }
                    catch (Exception error) { externalError = error; }
                    finally { externalCompleted.Set(); }
                }) { IsBackground = true, Name = "CalradiaForge external IL rebuild" };

                Thread applyThread = null;
                try
                {
                    applyThread = new Thread(() =>
                    {
                        Volatile.Write(ref applyThreadId, Thread.CurrentThread.ManagedThreadId);
                        try { applyResult = added.Apply(); }
                        catch (Exception error) { applyError = error; }
                        finally { applyCompleted.Set(); }
                    }) { IsBackground = true, Name = "CalradiaForge concurrent service Apply" };
                    applyThread.Start();

                    bool applyReachedGate = applyHeldServiceGate.Wait(TimeSpan.FromSeconds(5));
                    if (applyReachedGate) externalThread.Start();
                    bool callbackEnteredGate = applyReachedGate && callbackEntered.Wait(TimeSpan.FromSeconds(5));
                    bool externalReturned = callbackEnteredGate && externalApplied.Wait(TimeSpan.FromSeconds(5));
                    // Always release Apply after the probe has had its bounded opportunity
                    // to fail fast; this also prevents a failed assertion from stranding a worker.
                    releaseApplyGate.Set();
                    bool applyReturned = applyCompleted.Wait(TimeSpan.FromSeconds(5));
                    RequireHook(applyReachedGate && callbackEnteredGate && externalReturned && applyReturned,
                        "bounded callback barrier releases both concurrent backend operations");
                    RequireHook(externalError == null && applyError == null && applyResult != null && applyResult.Succeeded,
                        "external reconstruction and concurrent Forge Apply complete without lock inversion");
                    RequireHook(Volatile.Read(ref probeWasFast) == 1 && probeCompleted.IsSet && probeError == null &&
                        Volatile.Read(ref probeRejectedAllServiceOperations) == 1,
                        "all public service operations fail fast during the active manipulator instead of waiting on gate");
                }
                finally
                {
                    releaseApplyGate.Set();
                    releaseExternal.Set();
                    if (applyThread != null && applyThread.IsAlive) applyThread.Join(TimeSpan.FromSeconds(5));
                    if (externalThread.IsAlive) externalThread.Join(TimeSpan.FromSeconds(5));
                }

                RequireHook(externalCompleted.IsSet && externalError == null,
                    "external ILHook Undo and bounded thread cleanup complete");
                RequireHook(added.Revert().Succeeded && baseHook.Revert().Succeeded,
                    "concurrent fail-fast fixture restores Forge-owned IL hooks");
                service.Disconnect();
                _ = inactive;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static int FailedUndoTarget(int value) => value + 1;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static int IlUnloadTarget(int value) => value + 1;

        static void RunCleanIlUnloadCase()
        {
            MethodInfo target = typeof(Program).GetMethod(nameof(IlUnloadTarget), BindingFlags.Static | BindingFlags.NonPublic);
            bool allowed = true;
            var service = new ForgeHookService(() => allowed);
            int rebuilds = 0;
            var first = service.RegisterTranspiler(new ForgeHookDefinition { Id = "unload-il-first", Owner = "fixture", Target = target }, context =>
            {
                rebuilds++;
                RewriteConstant(context, value => value + 2);
            });
            var second = service.RegisterTranspiler(new ForgeHookDefinition { Id = "unload-il-second", Owner = "fixture", Target = target }, context => RewriteConstant(context, value => value * 3));
            RequireHook(first.Apply().Succeeded && second.Apply().Succeeded && IlUnloadTarget(2) == 11, "clean IL unload initial chain");
            typeof(ForgeHookService).GetMethod("StopCallbacksForUnload", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(service, null);
            allowed = false;
            int before = rebuilds;
            RequireHook(!second.Revert().Succeeded && rebuilds == before && IlUnloadTarget(2) == 11, "shutdown cleanup still requires approved context");
            using (var external = new ILHook(target, context => RewriteConstant(context, value => value + 4),
                new DetourConfig("fixture-shutdown-external-il", after: new[] { "CalradiaForge.Hook.unload-il-second" }), false))
            {
                external.Apply();
                RequireHook(rebuilds > before && IlUnloadTarget(2) == 15,
                    "shutdown retained IL activation survives external addition outside approved context");
                before = rebuilds;
                external.Undo();
                RequireHook(rebuilds > before && IlUnloadTarget(2) == 11,
                    "shutdown retained IL activation survives external removal outside approved context");
            }
            string reason;
            RequireHook(!first.Apply().Succeeded && !second.Revert().Succeeded && !service.CanDisconnect(out reason),
                "shutdown reconstruction does not reopen application or off-context management");
            before = rebuilds;
            allowed = true;
            RequireHook(!first.Apply().Succeeded, "shutdown does not reopen applications");
            RequireHook(second.Revert().Succeeded && rebuilds > before && IlUnloadTarget(2) == 5, "explicit Undo permits remaining owned cleanup rebuild after callback shutdown");
            RequireHook(!second.Apply().Succeeded, "cleanup scope ends and application gate remains closed");
            service.Disconnect();
            RequireHook(IlUnloadTarget(2) == 3 && first.Snapshot.State == ForgeHookState.Reverted, "clean shutdown disconnect restores original IL body");
        }

        static void RunFailedIlUndoCase()
        {
            var target = typeof(Program).GetMethod(nameof(FailedUndoTarget), BindingFlags.Static | BindingFlags.NonPublic);
            var service = new ForgeHookService(() => true);
            bool failRebuild = false;
            var first = service.RegisterTranspiler(new ForgeHookDefinition { Id = "failed-undo-first", Owner = "fixture", Target = target }, context =>
            {
                if (failRebuild) throw new ApplicationException("fixture remaining IL rebuild failure");
                RewriteConstant(context, value => value + 2);
            });
            var second = service.RegisterTranspiler(new ForgeHookDefinition { Id = "failed-undo-second", Owner = "fixture", Target = target }, context => RewriteConstant(context, value => value * 3));
            RequireHook(first.Apply().Succeeded && second.Apply().Succeeded, "failed-Undo fixture applies its initial chain");
            failRebuild = true;
            RequireHook(!second.Revert().Succeeded, "remaining manipulator failure rejects Undo completion");
            failRebuild = false;
            RequireHook(!second.Revert().Succeeded && !second.Verify().Succeeded && second.Snapshot.State == ForgeHookState.Conflict,
                "IsApplied false after failed Undo must not certify restoration on a later revert");
            bool collision = false;
            try { ForgeDetour.Patch(target, typeof(Program).GetMethod(nameof(Replacement), BindingFlags.Static | BindingFlags.NonPublic)); }
            catch (InvalidOperationException) { collision = true; }
            RequireHook(collision, "failed IL Undo retains target reservation");
            bool disconnectRejected = false;
            try { service.Disconnect(); } catch (InvalidOperationException) { disconnectRejected = true; }
            RequireHook(disconnectRejected && second.Snapshot.State == ForgeHookState.Conflict, "disconnect rejects and preserves uncertain IL removal record");
            // This disposable host exits with the unresolved reservation; do not claim recovery or reuse its target.
        }
    }
}
