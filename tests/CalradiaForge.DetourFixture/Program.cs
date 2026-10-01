using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using CalradiaForge.Core;
using CalradiaForge.Sdk;
using CalradiaForge.Sdk.Patcher;

namespace CalradiaForge.DetourFixture
{
    /// <summary>
    /// Disposable-process smoke fixture for the experimental native detour backend.
    /// Calls Target serially only; no other thread invokes it while code bytes are written.
    /// </summary>
    internal static class Program
    {
        public static int RunFixture()
        {
            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                Console.Error.WriteLine("FAIL: fixture must run in an x64 (AMD64) process; detected " + RuntimeInformation.ProcessArchitecture + ".");
                return 2;
            }

            MethodInfo target = typeof(Program).GetMethod("Target", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo replacement = typeof(Program).GetMethod("Replacement", BindingFlags.Static | BindingFlags.NonPublic);
            if (target == null || replacement == null)
            {
                Console.Error.WriteLine("FAIL: fixture methods could not be resolved.");
                return 2;
            }

            MethodInfo hookTarget = typeof(Program).GetMethod("HookTarget", BindingFlags.Static | BindingFlags.NonPublic);
            if (hookTarget == null)
            {
                Console.Error.WriteLine("FAIL: hook fixture method could not be resolved.");
                return 2;
            }

            RuntimeHelpers.PrepareMethod(hookTarget.MethodHandle);
            int hookFixture = RunPrefixPostfixFixture(hookTarget);
            if (hookFixture != 0) return hookFixture;

            int selfRetiringHookFixture = RunSelfRetiringHookFixture();
            if (selfRetiringHookFixture != 0) return selfRetiringHookFixture;

            int voidHookFixture = RunVoidPrefixFixture();
            if (voidHookFixture != 0) return voidHookFixture;

            int typedInvokerFixture = RunTypedInvokerFixture();
            if (typedInvokerFixture != 0) return typedInvokerFixture;

            int duplicateHookIdFixture = RunDuplicateHookIdFixture(hookTarget);
            if (duplicateHookIdFixture != 0) return duplicateHookIdFixture;

            int callbackOrderingFixture = RunCallbackOrderingFixture();
            if (callbackOrderingFixture != 0) return callbackOrderingFixture;

            int contextGuardFixture = RunContextGuardFixture();
            if (contextGuardFixture != 0) return contextGuardFixture;

            int unloadCallbackFixture = RunUnloadCallbackShutdownFixture();
            if (unloadCallbackFixture != 0) return unloadCallbackFixture;

            int apiDisconnectGuardFixture = RunApiDisconnectGuardFixture();
            if (apiDisconnectGuardFixture != 0) return apiDisconnectGuardFixture;
            // ForgeApi.Disconnect intentionally blocks all later detour applications until
            // another host is connected. Restore an empty host before the remaining fixtures
            // exercise the raw ForgeDetour path in this disposable process.
            ForgeApi.Connect(new TestEngine());

            int reapplyOrderFixture = RunReapplyOrderFixture();
            if (reapplyOrderFixture != 0) return reapplyOrderFixture;

            int uncertainHookFixture = RunUncertainApplyFixture();
            if (uncertainHookFixture != 0) return uncertainHookFixture;

            bool patchMayBeActive = false;
            try
            {
                // Warm both methods before the write, then use the same direct
                // call site serially around the patch so no JIT compilation races the detour.
                Console.WriteLine("[Fixture] Stage 1/6: warm target method.");
                int before = Target(7);
                if (before != 15) return Fail("pre-patch target result", before, 15);
                int replacementBefore = Replacement(7);
                if (replacementBefore != 32) return Fail("replacement warm-up", replacementBefore, 32);
                Console.WriteLine("[Fixture] Stage 2/6: prepare methods.");
                RuntimeHelpers.PrepareMethod(target.MethodHandle);
                RuntimeHelpers.PrepareMethod(replacement.MethodHandle);

                Console.WriteLine("[Fixture] Stage 3/6: apply explicit patch batch.");
                int applied = ForgePatcher.ApplyAll(typeof(Program).Assembly);
                if (applied != 1) return Fail("explicit ApplyAll count", applied, 1);
                patchMayBeActive = true;

                Console.WriteLine("[Fixture] Stage 4/6: invoke patched target.");
                int during = Target(7);
                if (during != 32) return Fail("patched target result", during, 32);
                if (!ForgeDetour.IsPatched(target))
                {
                    Console.Error.WriteLine("FAIL: exact detour bytes were not reported as installed.");
                    return 1;
                }

                Console.WriteLine("[Fixture] Stage 5/6: revert patch batch.");
                ForgePatcher.RevertAll();
                patchMayBeActive = false;

                Console.WriteLine("[Fixture] Stage 6/6: verify restored target.");
                int after = Target(7);
                if (after != 15) return Fail("post-revert target result", after, 15);
                if (ForgeDetour.IsPatched(target) || ForgeDetour.Unpatch(target))
                {
                    Console.Error.WriteLine("FAIL: reverted target remained tracked or second revert succeeded.");
                    return 1;
                }

                Console.WriteLine("PASS: x64 serial target returned 15 -> 32 -> 15; exact revert verified.");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("FAIL: " + error);
                return 1;
            }
            finally
            {
                // Best-effort cleanup in this disposable process. If bytes were changed by
                // anything other than Forge, Unpatch intentionally refuses to overwrite them.
                if (patchMayBeActive)
                {
                    try { ForgePatcher.RevertAll(); }
                    catch (Exception error) { Console.Error.WriteLine("CLEANUP ERROR: " + error.Message); }
                    try { ForgeDetour.Unpatch(target); }
                    catch (Exception error) { Console.Error.WriteLine("CLEANUP ERROR: " + error.Message); }
                }
            }
        }

        private static int RunPrefixPostfixFixture(MethodInfo target)
        {
            MethodInfo replacement = typeof(Program).GetMethod("Replacement", BindingFlags.Static | BindingFlags.NonPublic);
            if (replacement == null) return Fail("hook collision replacement lookup", 0, 1);
            RuntimeHelpers.PrepareMethod(replacement.MethodHandle);

            Console.WriteLine("[HookFixture] Stage 1/10: verify inert registration and default-deny gate.");
            int before = HookTarget(7);
            if (before != 15) return Fail("pre-hook target result", before, 15);

            var deniedService = new ForgeHookService();
            IForgeHookHandle deniedHandle = deniedService.Register(new ForgeHookDefinition
            {
                Id = "fixture-denied",
                Owner = "DetourFixture",
                Target = target,
                Prefix = invocation => invocation.Arguments[0] = (int)invocation.Arguments[0] + 2
            });
            if (deniedHandle.Snapshot.State != ForgeHookState.Registered)
            {
                Console.Error.WriteLine("FAIL: registration applied a hook before explicit Apply.");
                return 1;
            }
            ForgeHookOperationResult denied = deniedHandle.Apply();
            if (denied.Succeeded || denied.State != ForgeHookState.Registered)
            {
                Console.Error.WriteLine("FAIL: a service without an explicit mutation gate allowed hook application.");
                return 1;
            }
            deniedService.Disconnect();
            if (HookTarget(7) != 15) return Fail("default-deny target result", HookTarget(7), 15);

            Console.WriteLine("[HookFixture] Stage 2/10: register permitted Prefix/Postfix callbacks.");
            bool mayMutate = true;
            var service = new ForgeHookService(() => mayMutate);
            IForgeHookHandle handle = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-prefix-postfix",
                Owner = "DetourFixture",
                Target = target,
                Prefix = invocation => invocation.Arguments[0] = (int)invocation.Arguments[0] + 2,
                Postfix = invocation => invocation.Result = (int)invocation.Result + 100
            });
            if (HookTarget(7) != 15 || handle.Snapshot.State != ForgeHookState.Registered)
            {
                Console.Error.WriteLine("FAIL: registering the hook changed the target before explicit Apply.");
                return 1;
            }

            Console.WriteLine("[HookFixture] Stage 3/10: apply and verify the hook.");
            ForgeHookOperationResult applied = handle.Apply();
            if (!applied.Succeeded || applied.State != ForgeHookState.Applied)
            {
                Console.Error.WriteLine("FAIL: explicit hook Apply failed: " + applied.Detail);
                return 1;
            }
            ForgeHookOperationResult verified = handle.Verify();
            if (!verified.Succeeded || verified.State != ForgeHookState.Applied)
            {
                Console.Error.WriteLine("FAIL: applied hook verification failed: " + verified.Detail);
                return 1;
            }

            Console.WriteLine("[HookFixture] Stage 4/10: refuse lifecycle disconnect outside the mutation gate.");
            mayMutate = false;
            string disconnectReason;
            bool disconnectRejected = false;
            try { service.Disconnect(); }
            catch (InvalidOperationException) { disconnectRejected = true; }
            if (!disconnectRejected || ((IForgeHookServiceDisconnectGuard)service).CanDisconnect(out disconnectReason) ||
                handle.Snapshot.State != ForgeHookState.Applied || HookTarget(7) != 15)
            {
                Console.Error.WriteLine("FAIL: an out-of-context callback ran, disconnect changed hook state, or the block reason was not reported.");
                return 1;
            }
            mayMutate = true;

            Console.WriteLine("[HookFixture] Stage 5/10: invoke the target and reject raw detour collision.");
            int during = HookTarget(7);
            if (during != 119) return Fail("prefix/original/postfix result", during, 119);
            bool rawCollisionRejected = false;
            try { ForgeDetour.Patch(target, replacement); }
            catch (InvalidOperationException error) { rawCollisionRejected = error.Message.Contains("RuntimeDetour"); }
            ForgeHookOperationResult hookAfterCollision = handle.Verify();
            if (!rawCollisionRejected || !hookAfterCollision.Succeeded || hookAfterCollision.State != ForgeHookState.Applied || HookTarget(7) != 119)
            {
                Console.Error.WriteLine("FAIL: raw ForgeDetour collision changed or invalidated the active RuntimeDetour hook.");
                return 1;
            }

            Console.WriteLine("[HookFixture] Stage 6/10: revert and verify the original method.");
            ForgeHookOperationResult reverted = handle.Revert();
            if (!reverted.Succeeded || reverted.State != ForgeHookState.Reverted)
            {
                Console.Error.WriteLine("FAIL: explicit hook revert failed: " + reverted.Detail);
                return 1;
            }
            int after = HookTarget(7);
            if (after != 15) return Fail("post-hook target result", after, 15);

            Console.WriteLine("[HookFixture] Stage 7/10: confirm a second revert is idempotent.");
            ForgeHookOperationResult secondRevert = handle.Revert();
            if (!secondRevert.Succeeded || secondRevert.State != ForgeHookState.Reverted)
            {
                Console.Error.WriteLine("FAIL: second revert was not idempotent: " + secondRevert.Detail);
                return 1;
            }
            Console.WriteLine("[HookFixture] Stage 8/10: apply a raw detour and reject RuntimeDetour collision.");
            ForgeDetour.Patch(target, replacement);
            var blockedService = new ForgeHookService(() => true);
            IForgeHookHandle blockedHandle = blockedService.Register(new ForgeHookDefinition
            {
                Id = "fixture-hook-blocked-by-raw",
                Owner = "DetourFixture",
                Target = target,
                Prefix = invocation => invocation.Arguments[0] = (int)invocation.Arguments[0] + 2
            });
            ForgeHookOperationResult blocked = blockedHandle.Apply();
            if (blocked.Succeeded || blocked.State != ForgeHookState.Registered ||
                !blocked.Detail.Contains("ForgeDetour") || HookTarget(7) != 32 || !ForgeDetour.IsPatched(target))
            {
                Console.Error.WriteLine("FAIL: RuntimeDetour applied over the active raw ForgeDetour target.");
                return 1;
            }
            blockedService.Disconnect();

            Console.WriteLine("[HookFixture] Stage 9/10: revert raw detour and verify restoration.");
            if (!ForgeDetour.Unpatch(target) || ForgeDetour.IsPatched(target) || HookTarget(7) != 15)
            {
                Console.Error.WriteLine("FAIL: raw detour collision fixture did not restore the target after rejection.");
                return 1;
            }

            Console.WriteLine("[HookFixture] Stage 10/10: verify collision cleanup and an externally removed hook handle.");
            service.Disconnect();
            if (HookTarget(7) != 15 || ForgeDetour.GetTrackedSnapshots().Count != 0) return Fail("final restored target state", HookTarget(7), 15);
            if (!RunExternallyRemovedHookFixture()) return 1;
            Console.WriteLine("PASS: Prefix/Postfix lifecycle, guarded disconnect, idempotent revert, external cleanup, and bidirectional backend exclusion.");
            return 0;
        }

        private static int selfRevertingPrefixCalls;
        private static int selfRevertingPrefixTargetCalls;
        private static int selfRevertingPrefixPostfixCalls;
        private static int selfDisposingPostfixCalls;
        private static int selfDisposingPostfixTargetCalls;

        private static int RunSelfRetiringHookFixture()
        {
            MethodInfo prefixTarget = typeof(Program).GetMethod("SelfRevertingPrefixTarget", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo postfixTarget = typeof(Program).GetMethod("SelfDisposingPostfixTarget", BindingFlags.Static | BindingFlags.NonPublic);
            if (prefixTarget == null || postfixTarget == null) return Fail("self-retiring hook target lookup", 0, 1);
            RuntimeHelpers.PrepareMethod(prefixTarget.MethodHandle);
            RuntimeHelpers.PrepareMethod(postfixTarget.MethodHandle);
            selfRevertingPrefixCalls = selfRevertingPrefixTargetCalls = selfRevertingPrefixPostfixCalls = 0;
            selfDisposingPostfixCalls = selfDisposingPostfixTargetCalls = 0;

            var service = new ForgeHookService(() => true);
            IForgeHookHandle prefixHandle = null;
            IForgeHookHandle postfixHandle = null;
            bool prefixRevertPendingInsideCallback = false;
            bool prefixReservationRetainedDuringCallback = false;
            bool postfixDisposePendingInsideCallback = false;
            ForgeHookOperationResult prefixReapplyDuringCallback = null;
            ForgeHookOperationResult prefixVerifyDuringCallback = null;
            string prefixRevertDetail = string.Empty;
            prefixHandle = service.Register(new ForgeHookDefinition
            {
                Id = "fixture.self-reverting-prefix",
                Owner = "DetourFixture",
                Target = prefixTarget,
                Prefix = invocation =>
                {
                    selfRevertingPrefixCalls++;
                    invocation.Arguments[0] = (int)invocation.Arguments[0] + 1;
                    ForgeHookOperationResult reverted = prefixHandle.Revert();
                    prefixRevertPendingInsideCallback = !reverted.Succeeded && reverted.State == ForgeHookState.Applied &&
                        reverted.Detail.IndexOf("Undo is not yet verified", StringComparison.OrdinalIgnoreCase) >= 0;
                    prefixRevertDetail = reverted.Detail;
                    if (!prefixRevertPendingInsideCallback || prefixHandle.Snapshot.State != ForgeHookState.Applied)
                        throw new InvalidOperationException("Self-revert from Prefix did not remain pending until Dispatch exited: " + reverted.Detail);
                    prefixVerifyDuringCallback = prefixHandle.Verify();
                    prefixReapplyDuringCallback = prefixHandle.Apply();
                    prefixReservationRetainedDuringCallback = IsHookTargetReserved(prefixTarget);
                },
                Postfix = invocation =>
                {
                    selfRevertingPrefixPostfixCalls++;
                    invocation.Result = (int)invocation.Result + 100;
                }
            });
            postfixHandle = service.Register(new ForgeHookDefinition
            {
                Id = "fixture.self-disposing-postfix",
                Owner = "DetourFixture",
                Target = postfixTarget,
                Postfix = invocation =>
                {
                    selfDisposingPostfixCalls++;
                    invocation.Result = (int)invocation.Result + 100;
                    postfixHandle.Dispose();
                    postfixDisposePendingInsideCallback = postfixHandle.Snapshot.State == ForgeHookState.Applied &&
                        postfixHandle.Snapshot.Detail.IndexOf("Revert request is pending", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            });

            try
            {
                Console.WriteLine("[SelfRetireFixture] Prefix reverts itself during dispatch; current invocation must finish with its captured original invoker.");
                if (!prefixHandle.Apply().Succeeded) return Fail("self-reverting Prefix apply", 0, 1);
                int prefixResult;
                try { prefixResult = SelfRevertingPrefixTarget(2); }
                catch (Exception error)
                {
                    Console.Error.WriteLine("FAIL: Prefix self-retirement threw '" + error.GetBaseException().Message + "'; revert state=" + prefixHandle.Snapshot.State + "; revert detail=" + prefixRevertDetail);
                    return 1;
                }
                if (prefixResult != 110 || !prefixRevertPendingInsideCallback || prefixVerifyDuringCallback == null ||
                    prefixVerifyDuringCallback.Succeeded || prefixVerifyDuringCallback.State != ForgeHookState.Applied ||
                    !prefixReservationRetainedDuringCallback || prefixHandle.Snapshot.State != ForgeHookState.Reverted ||
                    selfRevertingPrefixCalls != 1 || selfRevertingPrefixPostfixCalls != 1 || selfRevertingPrefixTargetCalls != 1)
                {
                    Console.Error.WriteLine("FAIL: Prefix self-revert was reported complete before verification or invalidated the current dispatch.");
                    return 1;
                }
                if (prefixReapplyDuringCallback == null || prefixReapplyDuringCallback.Succeeded ||
                    prefixReapplyDuringCallback.Detail.IndexOf("still completing", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    Console.Error.WriteLine("FAIL: reapply was not blocked while the previous activation still owned an active dispatch lease.");
                    return 1;
                }
                ForgeHookOperationResult prefixReapplied = prefixHandle.Apply();
                ForgeHookOperationResult prefixReappliedRevert = prefixReapplied.Succeeded ? prefixHandle.Revert() : prefixReapplied;
                if (!prefixReapplied.Succeeded || !prefixReappliedRevert.Succeeded || prefixHandle.Snapshot.State != ForgeHookState.Reverted)
                {
                    Console.Error.WriteLine("FAIL: the hook could not be reapplied after the retired activation released its lease: " + prefixReappliedRevert.Detail);
                    return 1;
                }
                int prefixAfterRetire = SelfRevertingPrefixTarget(2);
                if (prefixAfterRetire != 9 || selfRevertingPrefixCalls != 1 || selfRevertingPrefixPostfixCalls != 1 || selfRevertingPrefixTargetCalls != 2)
                {
                    Console.Error.WriteLine("FAIL: reverted Prefix/Postfix callbacks ran again on a later target invocation.");
                    return 1;
                }

                Console.WriteLine("[SelfRetireFixture] Postfix disposes its own handle; active result completes and future calls are unhooked.");
                if (!postfixHandle.Apply().Succeeded) return Fail("self-disposing Postfix apply", 0, 1);
                int postfixResult = SelfDisposingPostfixTarget(2);
                if (postfixResult != 109 || !postfixDisposePendingInsideCallback || postfixHandle.Snapshot.State != ForgeHookState.Reverted ||
                    selfDisposingPostfixCalls != 1 || selfDisposingPostfixTargetCalls != 1)
                {
                    Console.Error.WriteLine("FAIL: Postfix self-dispose was reported complete inside the active callback or did not preserve the result.");
                    return 1;
                }
                int postfixAfterRetire = SelfDisposingPostfixTarget(2);
                if (postfixAfterRetire != 9 || selfDisposingPostfixCalls != 1 || selfDisposingPostfixTargetCalls != 2)
                {
                    Console.Error.WriteLine("FAIL: disposed Postfix callback ran again on a later target invocation.");
                    return 1;
                }

                Console.WriteLine("PASS: self-reverting Prefix and self-disposing Postfix finish the active serial dispatch and release future callback routes.");
                return 0;
            }
            finally
            {
                service.Disconnect();
            }
        }

        /// <summary>Opt-in serial microbenchmark; run only through Run-DetourFixture.bat --benchmark.</summary>
        public static int RunHookBenchmark()
        {
            const int warmupIterations = 5000;
            const int measuredIterations = 25000;
            const int sampleCount = 5;

            AppDomain.MonitoringIsEnabled = true;

            Console.WriteLine("# serial hook benchmark; x64 disposable host; same target measured before and after apply");
            Console.WriteLine("# callbacks are no-op; target JIT is prepared before the first measured call");
            Console.WriteLine("# allocated bytes use AppDomain counters in this single-threaded isolated host");
            Console.WriteLine("record,scenario,phase,sample,iterations,total_ms,ns_per_call,allocated_bytes,bytes_per_call,gen0_collections,checksum");

            int result = RunHookBenchmarkScenario("prefix", "BenchmarkPrefixTarget", true, false,
                warmupIterations, measuredIterations, sampleCount);
            if (result != 0) return result;
            result = RunHookBenchmarkScenario("postfix", "BenchmarkPostfixTarget", false, true,
                warmupIterations, measuredIterations, sampleCount);
            if (result != 0) return result;
            result = RunHookBenchmarkScenario("prefix_postfix", "BenchmarkBothTarget", true, true,
                warmupIterations, measuredIterations, sampleCount);
            return result;
        }

        private static int RunHookBenchmarkScenario(string scenario, string targetName, bool includePrefix,
            bool includePostfix, int warmupIterations, int measuredIterations, int sampleCount)
        {
            MethodInfo target = typeof(Program).GetMethod(targetName, BindingFlags.Static | BindingFlags.NonPublic);
            if (target == null) return BenchmarkFailure(scenario, "target method could not be resolved");
            RuntimeHelpers.PrepareMethod(target.MethodHandle);
            var call = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), target);

            BenchmarkSample[] directSamples = MeasureBenchmarkPhase(scenario, "direct", call,
                warmupIterations, measuredIterations, sampleCount);

            bool mayMutate = true;
            var service = new ForgeHookService(() => mayMutate);
            IForgeHookHandle handle = service.Register(new ForgeHookDefinition
            {
                Id = "benchmark-" + scenario,
                Owner = "DetourFixture.Benchmark",
                Target = target,
                Prefix = includePrefix ? (ForgeHookCallback)NoOpHookCallback : null,
                Postfix = includePostfix ? (ForgeHookCallback)NoOpHookCallback : null
            });

            long applyStart = Stopwatch.GetTimestamp();
            ForgeHookOperationResult applied = handle.Apply();
            double applyMs = ElapsedMilliseconds(applyStart);
            WriteBenchmarkOperation(scenario, "apply", applyMs);
            if (!applied.Succeeded)
            {
                mayMutate = true;
                service.Disconnect();
                return BenchmarkFailure(scenario, "explicit apply failed: " + applied.Detail);
            }

            BenchmarkSample firstCall = MeasureOne(call, 3);
            if (call(3) != 7)
            {
                handle.Revert();
                service.Disconnect();
                return BenchmarkFailure(scenario, "hook changed the benchmark target result");
            }
            BenchmarkSample[] hookedSamples = MeasureBenchmarkPhase(scenario, "hooked", call,
                warmupIterations, measuredIterations, sampleCount);
            WriteBenchmarkSummary(scenario, "direct", directSamples);
            WriteBenchmarkSummary(scenario, "hooked", hookedSamples);
            WriteBenchmarkRow("call", scenario, "after_apply_first_call", 0, 1, firstCall);

            long revertStart = Stopwatch.GetTimestamp();
            ForgeHookOperationResult reverted = handle.Revert();
            double revertMs = ElapsedMilliseconds(revertStart);
            WriteBenchmarkOperation(scenario, "revert", revertMs);
            if (!reverted.Succeeded || call(3) != 7)
            {
                mayMutate = true;
                service.Disconnect();
                return BenchmarkFailure(scenario, "revert did not restore the direct target");
            }
            service.Disconnect();
            return 0;
        }

        private static BenchmarkSample[] MeasureBenchmarkPhase(string scenario, string phase, Func<int, int> call,
            int warmupIterations, int measuredIterations, int sampleCount)
        {
            int checksum = 0;
            for (int i = 0; i < warmupIterations; i++) checksum += call((i & 127) + 1);
            Console.WriteLine("# " + scenario + " " + phase + " warmup_checksum=" + checksum.ToString(CultureInfo.InvariantCulture));

            var samples = new BenchmarkSample[sampleCount];
            for (int sample = 0; sample < sampleCount; sample++)
            {
                samples[sample] = MeasureLoop(call, measuredIterations);
                WriteBenchmarkRow("call", scenario, phase, sample + 1, measuredIterations, samples[sample]);
            }
            return samples;
        }

        private static BenchmarkSample MeasureLoop(Func<int, int> call, int iterations)
        {
            long allocatedBefore = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
            int gen0Before = GC.CollectionCount(0);
            long start = Stopwatch.GetTimestamp();
            int checksum = 0;
            for (int i = 0; i < iterations; i++) checksum += call((i & 127) + 1);
            long stop = Stopwatch.GetTimestamp();
            long allocated = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - allocatedBefore;
            int gen0 = GC.CollectionCount(0) - gen0Before;
            double elapsedMs = (stop - start) * 1000.0 / Stopwatch.Frequency;
            return new BenchmarkSample(elapsedMs, elapsedMs * 1000000.0 / iterations,
                allocated, (double)allocated / iterations, gen0, checksum);
        }

        private static BenchmarkSample MeasureOne(Func<int, int> call, int value)
        {
            long allocatedBefore = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
            int gen0Before = GC.CollectionCount(0);
            long start = Stopwatch.GetTimestamp();
            int checksum = call(value);
            long stop = Stopwatch.GetTimestamp();
            long allocated = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - allocatedBefore;
            int gen0 = GC.CollectionCount(0) - gen0Before;
            double elapsedMs = (stop - start) * 1000.0 / Stopwatch.Frequency;
            return new BenchmarkSample(elapsedMs, elapsedMs * 1000000.0,
                allocated, allocated, gen0, checksum);
        }

        private static void WriteBenchmarkSummary(string scenario, string phase, BenchmarkSample[] samples)
        {
            double[] ns = new double[samples.Length];
            double[] bytes = new double[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                ns[i] = samples[i].NanosecondsPerCall;
                bytes[i] = samples[i].BytesPerCall;
            }
            Array.Sort(ns);
            Array.Sort(bytes);
            double p50Ns = ns[(ns.Length - 1) / 2];
            double p95Ns = ns[(int)Math.Ceiling(ns.Length * 0.95) - 1];
            double p50Bytes = bytes[(bytes.Length - 1) / 2];
            double p95Bytes = bytes[(int)Math.Ceiling(bytes.Length * 0.95) - 1];
            string count = samples.Length.ToString(CultureInfo.InvariantCulture);
            Console.WriteLine(string.Join(",", "summary", scenario, phase, "p50", count, string.Empty,
                p50Ns.ToString("F1", CultureInfo.InvariantCulture), string.Empty, p50Bytes.ToString("F2", CultureInfo.InvariantCulture), string.Empty, string.Empty));
            Console.WriteLine(string.Join(",", "summary", scenario, phase, "p95", count, string.Empty,
                p95Ns.ToString("F1", CultureInfo.InvariantCulture), string.Empty, p95Bytes.ToString("F2", CultureInfo.InvariantCulture), string.Empty, string.Empty));
        }

        private static void WriteBenchmarkRow(string record, string scenario, string phase, int sample,
            int iterations, BenchmarkSample measurement)
        {
            Console.WriteLine(string.Join(",", record, scenario, phase, sample.ToString(CultureInfo.InvariantCulture),
                iterations.ToString(CultureInfo.InvariantCulture), measurement.ElapsedMilliseconds.ToString("F3", CultureInfo.InvariantCulture),
                measurement.NanosecondsPerCall.ToString("F1", CultureInfo.InvariantCulture),
                measurement.AllocatedBytes.ToString(CultureInfo.InvariantCulture), measurement.BytesPerCall.ToString("F2", CultureInfo.InvariantCulture),
                measurement.Gen0Collections.ToString(CultureInfo.InvariantCulture), measurement.Checksum.ToString(CultureInfo.InvariantCulture)));
        }

        private static void WriteBenchmarkOperation(string scenario, string operation, double elapsedMs)
        {
            Console.WriteLine(string.Join(",", "operation", scenario, operation, "0", "1",
                elapsedMs.ToString("F3", CultureInfo.InvariantCulture), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));
        }

        private static double ElapsedMilliseconds(long startTimestamp) =>
            (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;

        private static int BenchmarkFailure(string scenario, string detail)
        {
            Console.Error.WriteLine("BENCHMARK FAIL: " + scenario + ": " + detail);
            return 1;
        }

        private static void NoOpHookCallback(ForgeHookInvocation invocation) { }

        private sealed class BenchmarkSample
        {
            internal BenchmarkSample(double elapsedMilliseconds, double nanosecondsPerCall,
                long allocatedBytes, double bytesPerCall, int gen0Collections, int checksum)
            {
                ElapsedMilliseconds = elapsedMilliseconds;
                NanosecondsPerCall = nanosecondsPerCall;
                AllocatedBytes = allocatedBytes;
                BytesPerCall = bytesPerCall;
                Gen0Collections = gen0Collections;
                Checksum = checksum;
            }

            internal double ElapsedMilliseconds { get; }
            internal double NanosecondsPerCall { get; }
            internal long AllocatedBytes { get; }
            internal double BytesPerCall { get; }
            internal int Gen0Collections { get; }
            internal int Checksum { get; }
        }

        private static bool RunExternallyRemovedHookFixture()
        {
            MethodInfo target = typeof(Program).GetMethod("ExternallyRemovedHookTarget", BindingFlags.Static | BindingFlags.NonPublic);
            RuntimeHelpers.PrepareMethod(target.MethodHandle);
            var service = new ForgeHookService(() => true);
            IForgeHookHandle handle = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-externally-removed",
                Owner = "DetourFixture",
                Target = target,
                Prefix = invocation => invocation.Arguments[0] = (int)invocation.Arguments[0] + 2
            });
            try
            {
                if (!handle.Apply().Succeeded) return false;
                FieldInfo entriesField = typeof(ForgeHookService).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);
                var entries = (System.Collections.IDictionary)entriesField.GetValue(service);
                object entry = entries["fixture-externally-removed"];
                FieldInfo activationField = entry.GetType().GetField("Activation", BindingFlags.Instance | BindingFlags.NonPublic);
                object activation = activationField.GetValue(entry);
                FieldInfo hookField = activation?.GetType().GetField("Hook", BindingFlags.Instance | BindingFlags.NonPublic);
                object hook = hookField?.GetValue(activation);
                if (hook == null)
                {
                    Fail("external hook handle lookup", 0, 1);
                    return false;
                }
                hook.GetType().GetMethod("Undo", BindingFlags.Instance | BindingFlags.Public).Invoke(hook, null);

                ForgeHookOperationResult reverted = handle.Revert();
                if (!reverted.Succeeded || reverted.State != ForgeHookState.Reverted || activationField.GetValue(entry) != null || hookField.GetValue(activation) != null ||
                    ExternallyRemovedHookTarget(7) != 15)
                {
                    Console.Error.WriteLine("FAIL: the inactive-hook cleanup did not dispose and clear the retained MonoMod handle.");
                    return false;
                }

                service.Disconnect();
                service.Reconnect();
                Console.WriteLine("PASS: externally removed hook handle was disposed and service reconnect succeeded.");
                return true;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("FAIL: externally removed hook cleanup: " + error.GetBaseException().Message);
                return false;
            }
            finally
            {
                service.Disconnect();
            }
        }

        private static int voidTargetCalls;
        private static int voidPostfixCalls;
        private static int guardedTargetCalls;
        private static int guardedPrefixCalls;
        private static int guardedPostfixCalls;
        private static int cancelledValueTargetCalls;
        private static bool contextTransitionAllowed;
        private static int contextTransitionTargetCalls;
        private static int contextTransitionPrefixCalls;
        private static int contextTransitionPostfixCalls;

        private static int RunVoidPrefixFixture()
        {
            MethodInfo target = typeof(Program).GetMethod("VoidHookTarget", BindingFlags.Static | BindingFlags.NonPublic);
            if (target == null) return Fail("void hook target lookup", 0, 1);
            RuntimeHelpers.PrepareMethod(target.MethodHandle);
            voidTargetCalls = 0;
            voidPostfixCalls = 0;

            var service = new ForgeHookService(() => true);
            var handle = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-void-prefix",
                Owner = "DetourFixture",
                Target = target,
                Prefix = invocation => invocation.RunOriginal = false,
                Postfix = _ => voidPostfixCalls++
            });

            try
            {
                Console.WriteLine("[HookFixture] Void stage 1/2: Prefix suppresses a void target and Postfix still runs.");
                ForgeHookOperationResult applied = handle.Apply();
                if (!applied.Succeeded) return Fail("void Prefix apply", 0, 1);
                VoidHookTarget();
                if (voidTargetCalls != 0 || voidPostfixCalls != 1)
                {
                    Console.Error.WriteLine("FAIL: void Prefix did not suppress the original while preserving Postfix dispatch.");
                    return 1;
                }

                Console.WriteLine("[HookFixture] Void stage 2/2: revert restores the original void target.");
                ForgeHookOperationResult reverted = handle.Revert();
                if (!reverted.Succeeded || reverted.State != ForgeHookState.Reverted) return Fail("void Prefix revert", 0, 1);
                VoidHookTarget();
                if (voidTargetCalls != 1 || voidPostfixCalls != 1)
                {
                    Console.Error.WriteLine("FAIL: void target was not restored after hook revert.");
                    return 1;
                }

                service.Disconnect();
                Console.WriteLine("PASS: void Prefix cancellation and verified restoration.");
                return 0;
            }
            finally
            {
                service.Disconnect();
            }
        }

        private static int RunTypedInvokerFixture()
        {
            var target = new TypedInvokerTarget();
            Type targetType = typeof(TypedInvokerTarget);
            MethodInfo instanceMethod = targetType.GetMethod("Compute", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo voidMethod = targetType.GetMethod("Record", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo throwingMethod = targetType.GetMethod("ThrowOriginal", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo referenceMethod = targetType.GetMethod("GetReference", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo nullReferenceMethod = targetType.GetMethod("GetNullReference", BindingFlags.Instance | BindingFlags.Public);
            if (instanceMethod == null || voidMethod == null || throwingMethod == null ||
                referenceMethod == null || nullReferenceMethod == null)
                return Fail("typed invoker instance target lookup", 0, 1);

            RuntimeHelpers.PrepareMethod(instanceMethod.MethodHandle);
            RuntimeHelpers.PrepareMethod(voidMethod.MethodHandle);
            RuntimeHelpers.PrepareMethod(throwingMethod.MethodHandle);
            RuntimeHelpers.PrepareMethod(referenceMethod.MethodHandle);
            RuntimeHelpers.PrepareMethod(nullReferenceMethod.MethodHandle);

            int computePostfixCalls = 0;
            int voidPostfixCalls = 0;
            int throwingPostfixCalls = 0;
            int referencePostfixCalls = 0;
            int nullReferencePostfixCalls = 0;
            var service = new ForgeHookService(() => true);
            IForgeHookHandle compute = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-typed-instance-return",
                Owner = "DetourFixture",
                Target = instanceMethod,
                Prefix = invocation =>
                {
                    if (!ReferenceEquals(invocation.Instance, target)) throw new InvalidOperationException("typed instance receiver mismatch");
                    invocation.Arguments[0] = (int)invocation.Arguments[0] + 3;
                },
                Postfix = invocation =>
                {
                    if (!ReferenceEquals(invocation.Instance, target)) throw new InvalidOperationException("typed postfix receiver mismatch");
                    computePostfixCalls++;
                    invocation.Result = (int)invocation.Result + 100;
                }
            });
            IForgeHookHandle voidHandle = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-typed-instance-void",
                Owner = "DetourFixture",
                Target = voidMethod,
                Prefix = invocation =>
                {
                    if (!ReferenceEquals(invocation.Instance, target)) throw new InvalidOperationException("typed void receiver mismatch");
                    invocation.Arguments[0] = (string)invocation.Arguments[0] + "-prefix";
                },
                Postfix = invocation =>
                {
                    if (!ReferenceEquals(invocation.Instance, target)) throw new InvalidOperationException("typed void postfix receiver mismatch");
                    voidPostfixCalls++;
                }
            });
            IForgeHookHandle throwing = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-typed-instance-throw",
                Owner = "DetourFixture",
                Target = throwingMethod,
                Prefix = invocation =>
                {
                    if (!ReferenceEquals(invocation.Instance, target)) throw new InvalidOperationException("typed throwing receiver mismatch");
                },
                Postfix = _ => throwingPostfixCalls++
            });
            IForgeHookHandle reference = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-typed-reference-return",
                Owner = "DetourFixture",
                Target = referenceMethod,
                Postfix = _ => referencePostfixCalls++
            });
            IForgeHookHandle nullReference = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-typed-null-reference-return",
                Owner = "DetourFixture",
                Target = nullReferenceMethod,
                Postfix = _ => nullReferencePostfixCalls++
            });

            try
            {
                Console.WriteLine("[HookFixture] Typed invoker stage 1/5: instance receiver and value return.");
                if (!compute.Apply().Succeeded || target.Compute(4) != 115 || target.ComputeCalls != 1 || computePostfixCalls != 1)
                {
                    Console.Error.WriteLine("FAIL: typed invoker did not pass the instance receiver, updated arguments, or value result.");
                    return 1;
                }

                Console.WriteLine("[HookFixture] Typed invoker stage 2/5: instance void method and callbacks.");
                if (!voidHandle.Apply().Succeeded)
                    return Fail("typed instance void apply", 0, 1);
                target.Record("payload");
                if (target.RecordCalls != 1 || target.LastRecord != "payload-prefix" || voidPostfixCalls != 1)
                {
                    Console.Error.WriteLine("FAIL: typed invoker did not invoke the instance void target and its callbacks correctly.");
                    return 1;
                }

                Console.WriteLine("[HookFixture] Typed invoker stage 3/5: preserve a normal reference return.");
                if (!reference.Apply().Succeeded || !ReferenceEquals(target.GetReference(), target.ReferenceResult) ||
                    referencePostfixCalls != 1)
                {
                    Console.Error.WriteLine("FAIL: typed invoker did not preserve the original reference return or run its Postfix once.");
                    return 1;
                }

                Console.WriteLine("[HookFixture] Typed invoker stage 4/5: preserve a null reference return.");
                if (!nullReference.Apply().Succeeded || target.GetNullReference() != null || nullReferencePostfixCalls != 1)
                {
                    Console.Error.WriteLine("FAIL: typed invoker did not preserve the null reference return or run its Postfix once.");
                    return 1;
                }

                Console.WriteLine("[HookFixture] Typed invoker stage 5/5: original exception propagates without reflection wrapping.");
                if (!throwing.Apply().Succeeded)
                    return Fail("typed exception apply", 0, 1);
                Exception caught = null;
                try { target.ThrowOriginal(); }
                catch (Exception error) { caught = error; }
                if (caught == null || caught.GetType() != typeof(InvalidOperationException) ||
                    caught.Message != TypedInvokerTarget.OriginalExceptionMessage || target.ThrowCalls != 1 || throwingPostfixCalls != 0)
                {
                    Console.Error.WriteLine("FAIL: original exception was swallowed, wrapped in TargetInvocationException, or ran Postfix after failure: " +
                        (caught == null ? "<none>" : caught.GetType().FullName + ": " + caught.Message));
                    return 1;
                }

                IReadOnlyList<ForgeHookOperationResult> reverted = service.RevertAll();
                bool allReverted = reverted.Count == 5;
                for (int i = 0; i < reverted.Count; i++)
                    allReverted &= reverted[i].Succeeded && reverted[i].State == ForgeHookState.Reverted;
                if (!allReverted || target.Compute(4) != 9 || target.RecordCalls != 1 ||
                    computePostfixCalls != 1 || voidPostfixCalls != 1 ||
                    !ReferenceEquals(target.GetReference(), target.ReferenceResult) || target.GetNullReference() != null ||
                    referencePostfixCalls != 1 || nullReferencePostfixCalls != 1)
                {
                    Console.Error.WriteLine("FAIL: typed invoker hooks were not reverted and original instance behavior restored.");
                    return 1;
                }

                service.Disconnect();
                Console.WriteLine("PASS: typed invoker supports instance value/reference/null/void returns and propagates original exceptions directly.");
                return RunUnsupportedSpecialSignatureFixture();
            }
            finally
            {
                service.Disconnect();
            }
        }

        private static int RunUnsupportedSpecialSignatureFixture()
        {
            Console.WriteLine("[HookFixture] Typed invoker validation: reject runtime-only signature types during registration, before IL emission.");
            AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("CalradiaForge.DetourFixture.SpecialSignatures"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("SpecialSignatures");
            TypeBuilder declaringType = module.DefineType("SpecialSignatureTargets", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            MethodBuilder[] emittedTargets =
            {
                declaringType.DefineMethod("TypedReferenceReturn", MethodAttributes.Public | MethodAttributes.Static, typeof(TypedReference), Type.EmptyTypes),
                declaringType.DefineMethod("ArgIteratorParameter", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { typeof(ArgIterator) }),
                declaringType.DefineMethod("RuntimeArgumentHandleParameter", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { typeof(RuntimeArgumentHandle) })
            };
            for (int i = 0; i < emittedTargets.Length; i++) emittedTargets[i].GetILGenerator().Emit(OpCodes.Ret);
            Type runtimeType = declaringType.CreateType();
            MethodInfo[] targets = new MethodInfo[emittedTargets.Length];
            for (int i = 0; i < emittedTargets.Length; i++) targets[i] = runtimeType.GetMethod(emittedTargets[i].Name, BindingFlags.Public | BindingFlags.Static);
            string[] labels = { "TypedReference return", "ArgIterator parameter", "RuntimeArgumentHandle parameter" };
            var service = new ForgeHookService(() => true);
            try
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    bool rejected = false;
                    try
                    {
                        service.Register(new ForgeHookDefinition
                        {
                            Id = "fixture-special-signature-" + i,
                            Owner = "DetourFixture",
                            Target = targets[i],
                            Prefix = _ => { }
                        });
                    }
                    catch (ArgumentException) { rejected = true; }
                    catch (Exception error)
                    {
                        Console.Error.WriteLine("FAIL: " + labels[i] + " escaped controlled signature validation with " +
                            error.GetType().FullName + ": " + error.Message + Environment.NewLine + error.StackTrace);
                        return 1;
                    }

                    if (!rejected)
                    {
                        Console.Error.WriteLine("FAIL: " + labels[i] + " was not rejected before hook IL generation.");
                        return 1;
                    }
                    if (service.GetSnapshots().Count != 0)
                    {
                        Console.Error.WriteLine("FAIL: " + labels[i] + " rejection left a registered hook behind.");
                        return 1;
                    }
                }
                Console.WriteLine("PASS: TypedReference, ArgIterator, and RuntimeArgumentHandle signatures are rejected at registration.");
                return 0;
            }
            finally
            {
                service.Disconnect();
            }
        }

        private static int RunDuplicateHookIdFixture(MethodInfo target)
        {
            Console.WriteLine("[HookFixture] Registration validation: reject duplicate IDs case-insensitively before any detour is applied.");
            var service = new ForgeHookService(() => true);
            try
            {
                IForgeHookHandle first = service.Register(new ForgeHookDefinition
                {
                    Id = "fixture-duplicate-id",
                    Owner = "DetourFixture",
                    Target = target,
                    Prefix = _ => { }
                });
                bool duplicateRejected = false;
                try
                {
                    service.Register(new ForgeHookDefinition
                    {
                        Id = "FIXTURE-DUPLICATE-ID",
                        Owner = "DetourFixture",
                        Target = target,
                        Postfix = _ => { }
                    });
                }
                catch (InvalidOperationException)
                {
                    duplicateRejected = true;
                }

                IReadOnlyList<ForgeHookSnapshot> snapshots = service.GetSnapshots();
                if (!duplicateRejected || first.Snapshot.State != ForgeHookState.Registered ||
                    snapshots.Count != 1 || snapshots[0].State != ForgeHookState.Registered)
                {
                    Console.Error.WriteLine("FAIL: duplicate hook ID registration was not rejected while preserving the original registration.");
                    return 1;
                }

                service.Disconnect();
                Console.WriteLine("PASS: duplicate hook IDs are rejected without mutating the registered hook.");
                return 0;
            }
            finally
            {
                service.Disconnect();
            }
        }

        private static readonly List<string> callbackOrder = new List<string>();

        private static int RunCallbackOrderingFixture()
        {
            MethodInfo target = typeof(Program).GetMethod("CallbackOrderTarget", BindingFlags.Static | BindingFlags.NonPublic);
            if (target == null) return Fail("callback-order target lookup", 0, 1);
            RuntimeHelpers.PrepareMethod(target.MethodHandle);
            var service = new ForgeHookService(() => true);

            try
            {
                Console.WriteLine("[HookFixture] Callback ordering: verify priority, Before, and After on the same target.");
                var low = service.Register(new ForgeHookDefinition
                {
                    Id = "fixture-priority-low",
                    Owner = "DetourFixture",
                    Target = target,
                    Priority = 10,
                    Prefix = _ => callbackOrder.Add("low-prefix"),
                    Postfix = _ => callbackOrder.Add("low-postfix")
                });
                var high = service.Register(new ForgeHookDefinition
                {
                    Id = "fixture-priority-high",
                    Owner = "DetourFixture",
                    Target = target,
                    Priority = 20,
                    Prefix = _ => callbackOrder.Add("high-prefix"),
                    Postfix = _ => callbackOrder.Add("high-postfix")
                });

                if (!low.Apply().Succeeded || !high.Apply().Succeeded)
                    return Fail("priority pair apply", 0, 1);
                callbackOrder.Clear();
                int priorityResult = CallbackOrderTarget(4);
                if (priorityResult != 9 ||
                    !MatchesCallbackOrder("high-prefix|low-prefix|target|low-postfix|high-postfix"))
                    return Fail("priority order (Prefix outer-to-inner and Postfix inner-to-outer)", 0, 1);
                if (!high.Revert().Succeeded || !low.Revert().Succeeded)
                    return Fail("priority pair revert", 0, 1);

                callbackOrder.Clear();
                var beforeA = service.Register(new ForgeHookDefinition
                {
                    Id = "fixture-before-a",
                    Owner = "DetourFixture",
                    Target = target,
                    Priority = 10,
                    Before = new List<string> { "fixture-before-b" },
                    Prefix = _ => callbackOrder.Add("before-a")
                });
                var beforeB = service.Register(new ForgeHookDefinition
                {
                    Id = "fixture-before-b",
                    Owner = "DetourFixture",
                    Target = target,
                    Priority = 20,
                    Prefix = _ => callbackOrder.Add("before-b")
                });
                if (!beforeB.Apply().Succeeded || !beforeA.Apply().Succeeded)
                    return Fail("Before pair apply", 0, 1);
                callbackOrder.Clear();
                int beforeResult = CallbackOrderTarget(4);
                if (beforeResult != 9 ||
                    !MatchesCallbackOrder("before-a|before-b|target"))
                    return Fail("Before order overrides priority and application order", 0, 1);
                if (!beforeA.Revert().Succeeded || !beforeB.Revert().Succeeded)
                    return Fail("Before pair revert", 0, 1);

                callbackOrder.Clear();
                var afterA = service.Register(new ForgeHookDefinition
                {
                    Id = "fixture-after-a",
                    Owner = "DetourFixture",
                    Target = target,
                    Priority = 20,
                    Prefix = _ => callbackOrder.Add("after-a")
                });
                var afterB = service.Register(new ForgeHookDefinition
                {
                    Id = "fixture-after-b",
                    Owner = "DetourFixture",
                    Target = target,
                    Priority = 10,
                    After = new List<string> { "fixture-after-a" },
                    Prefix = _ => callbackOrder.Add("after-b")
                });
                if (!afterB.Apply().Succeeded || !afterA.Apply().Succeeded)
                    return Fail("After pair apply", 0, 1);
                callbackOrder.Clear();
                int afterResult = CallbackOrderTarget(4);
                if (afterResult != 9 ||
                    !MatchesCallbackOrder("after-a|after-b|target"))
                    return Fail("After order overrides priority and application order", 0, 1);
                if (!afterA.Revert().Succeeded || !afterB.Revert().Succeeded)
                    return Fail("After pair revert", 0, 1);

                callbackOrder.Clear();
                int restoredResult = CallbackOrderTarget(4);
                if (restoredResult != 9 || !MatchesCallbackOrder("target"))
                    return Fail("callback-order target restoration", restoredResult, 9);

                service.Disconnect();
                Console.WriteLine("PASS: same-target Prefix/Postfix dispatch honors priority, Before, and After, and restores the target.");
                return 0;
            }
            finally
            {
                service.Disconnect();
                callbackOrder.Clear();
            }
        }

        private static bool MatchesCallbackOrder(string expected)
        {
            string actual = string.Join("|", callbackOrder.ToArray());
            if (string.Equals(actual, expected, StringComparison.Ordinal)) return true;
            Console.Error.WriteLine("FAIL: callback order expected '" + expected + "', got '" + actual + "'.");
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int CallbackOrderTarget(int value)
        {
            callbackOrder.Add("target");
            return value * 2 + 1;
        }

        private sealed class TypedInvokerTarget
        {
            internal const string OriginalExceptionMessage = "fixture original instance exception";
            internal int ComputeCalls;
            internal int RecordCalls;
            internal int ThrowCalls;
            internal string LastRecord;
            internal readonly object ReferenceResult = new object();

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public int Compute(int value)
            {
                ComputeCalls++;
                return value * 2 + 1;
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public void Record(string value)
            {
                RecordCalls++;
                LastRecord = value;
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public int ThrowOriginal()
            {
                ThrowCalls++;
                throw new InvalidOperationException(OriginalExceptionMessage);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public object GetReference()
            {
                return ReferenceResult;
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public object GetNullReference()
            {
                return null;
            }
        }

        private static int RunContextGuardFixture()
        {
            MethodInfo guardedTarget = typeof(Program).GetMethod("GuardedHookTarget", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo valueTarget = typeof(Program).GetMethod("CancelledValueHookTarget", BindingFlags.Static | BindingFlags.NonPublic);
            if (guardedTarget == null || valueTarget == null) return Fail("context-guard target lookup", 0, 1);
            RuntimeHelpers.PrepareMethod(guardedTarget.MethodHandle);
            RuntimeHelpers.PrepareMethod(valueTarget.MethodHandle);
            bool allowed = true;
            guardedTargetCalls = guardedPrefixCalls = guardedPostfixCalls = cancelledValueTargetCalls = 0;

            var service = new ForgeHookService(() => allowed);
            var guarded = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-context-guard",
                Owner = "DetourFixture",
                Target = guardedTarget,
                Prefix = invocation => { guardedPrefixCalls++; invocation.Arguments[0] = (int)invocation.Arguments[0] + 1; },
                Postfix = invocation => { guardedPostfixCalls++; invocation.Result = (int)invocation.Result + 100; }
            });
            var cancelledValue = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-value-cancel",
                Owner = "DetourFixture",
                Target = valueTarget,
                Prefix = invocation => { invocation.RunOriginal = false; invocation.Result = 40; },
                Postfix = invocation => invocation.Result = (int)invocation.Result + 2
            });

            try
            {
                Console.WriteLine("[HookFixture] Context stage 1/3: callbacks are skipped outside the approved game context while the original target remains callable.");
                if (!guarded.Apply().Succeeded) return Fail("context guard apply", 0, 1);
                allowed = false;
                int outside = GuardedHookTarget(2);
                if (outside != 9 || guardedTargetCalls != 1 || guardedPrefixCalls != 0 || guardedPostfixCalls != 0)
                {
                    Console.Error.WriteLine("FAIL: an out-of-context call invoked a hook callback or failed to preserve the original target.");
                    return 1;
                }
                bool disposeFailureSurfaced = false;
                try { guarded.Dispose(); }
                catch (InvalidOperationException) { disposeFailureSurfaced = true; }
                if (!disposeFailureSurfaced || guarded.Snapshot.State != ForgeHookState.Applied)
                {
                    Console.Error.WriteLine("FAIL: disposing an unrevertible hook handle silently hid failure or changed its state.");
                    return 1;
                }

                Console.WriteLine("[HookFixture] Context stage 2/3: callbacks resume only in the approved context; non-void Prefix cancellation returns a validated result.");
                allowed = true;
                int inside = GuardedHookTarget(2);
                if (inside != 110 || guardedTargetCalls != 2 || guardedPrefixCalls != 1 || guardedPostfixCalls != 1)
                {
                    Console.Error.WriteLine("FAIL: in-context Prefix/Postfix dispatch did not resume after the guard reopened.");
                    return 1;
                }
                if (!cancelledValue.Apply().Succeeded || CancelledValueHookTarget(2) != 42 || cancelledValueTargetCalls != 0)
                {
                    Console.Error.WriteLine("FAIL: non-void Prefix cancellation did not return its validated result and skip the target.");
                    return 1;
                }

                Console.WriteLine("[HookFixture] Context stage 3/3: verified revert restores both original targets.");
                if (!cancelledValue.Revert().Succeeded || !guarded.Revert().Succeeded ||
                    CancelledValueHookTarget(2) != 9 || GuardedHookTarget(2) != 9 || cancelledValueTargetCalls != 1)
                {
                    Console.Error.WriteLine("FAIL: verified revert did not restore the original target behavior.");
                    return 1;
                }
                service.Disconnect();
                Console.WriteLine("PASS: out-of-context dispatch is inert, failed handle disposal is visible, and revert restores targets.");
                return RunContextTransitionFixture();
            }
            finally
            {
                allowed = true;
                service.Disconnect();
            }
        }

        private static int RunContextTransitionFixture()
        {
            MethodInfo target = typeof(Program).GetMethod("ContextTransitionHookTarget", BindingFlags.Static | BindingFlags.NonPublic);
            if (target == null) return Fail("context-transition target lookup", 0, 1);
            RuntimeHelpers.PrepareMethod(target.MethodHandle);
            contextTransitionAllowed = true;
            contextTransitionTargetCalls = contextTransitionPrefixCalls = contextTransitionPostfixCalls = 0;
            var service = new ForgeHookService(() => contextTransitionAllowed);
            IForgeHookHandle handle = null;
            ForgeHookOperationResult revertRequest = null;
            ForgeHookOperationResult verifyWhilePending = null;
            bool reservationRetainedAtCallback = false;
            handle = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-context-transition",
                Owner = "DetourFixture",
                Target = target,
                Prefix = invocation =>
                {
                    contextTransitionPrefixCalls++;
                    invocation.Arguments[0] = (int)invocation.Arguments[0] + 1;
                    revertRequest = handle.Revert();
                    verifyWhilePending = handle.Verify();
                    reservationRetainedAtCallback = IsHookTargetReserved(target);
                },
                Postfix = invocation => { contextTransitionPostfixCalls++; invocation.Result = (int)invocation.Result + 100; }
            });
            try
            {
                Console.WriteLine("[HookFixture] Context transition: defer self-revert, then retain the activation when the original target leaves the approved context.");
                if (!handle.Apply().Succeeded) return Fail("context-transition apply", 0, 1);
                int result = ContextTransitionHookTarget(2);
                if (result != 10 || contextTransitionTargetCalls != 1 || contextTransitionPrefixCalls != 1 || contextTransitionPostfixCalls != 0 ||
                    revertRequest == null || revertRequest.Succeeded || revertRequest.State != ForgeHookState.Applied ||
                    verifyWhilePending == null || verifyWhilePending.Succeeded || verifyWhilePending.State != ForgeHookState.Applied ||
                    handle.Snapshot.State != ForgeHookState.Applied || !reservationRetainedAtCallback || !IsHookTargetReserved(target) ||
                    handle.Snapshot.Detail.IndexOf("No Undo or disposal was attempted", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    Console.Error.WriteLine("FAIL: Postfix ran after context transition or deferred cleanup mutated/forgot the activation outside the approved context.");
                    return 1;
                }
                ForgeHookOperationResult rejectedRetry = handle.Revert();
                if (rejectedRetry.Succeeded || handle.Snapshot.State != ForgeHookState.Applied || !IsHookTargetReserved(target))
                {
                    Console.Error.WriteLine("FAIL: explicit revert mutated the hook while the host gate remained closed.");
                    return 1;
                }
                contextTransitionAllowed = true;
                ForgeHookOperationResult retried = handle.Revert();
                if (!retried.Succeeded || retried.State != ForgeHookState.Reverted || IsHookTargetReserved(target) || ContextTransitionHookTarget(2) != 9)
                {
                    Console.Error.WriteLine("FAIL: approved explicit retry did not verify revert and release the target reservation: " + retried.Detail);
                    return 1;
                }
                service.Disconnect();
                Console.WriteLine("PASS: callbacks are rechecked; deferred Undo waits for a live approved gate and remains recoverable after a context transition.");
                return 0;
            }
            finally
            {
                contextTransitionAllowed = true;
                service.Disconnect();
            }
        }

        private static int RunUnloadCallbackShutdownFixture()
        {
            MethodInfo target = typeof(Program).GetMethod("GuardedHookTarget", BindingFlags.Static | BindingFlags.NonPublic);
            if (target == null) return Fail("unload callback target lookup", 0, 1);
            RuntimeHelpers.PrepareMethod(target.MethodHandle);
            guardedTargetCalls = guardedPrefixCalls = guardedPostfixCalls = 0;

            bool allowMutations = true;
            bool closeGateAfterPreflight = false;
            int gateChecks = 0;
            var engine = new TestEngine(() =>
            {
                if (closeGateAfterPreflight && ++gateChecks == 2) return false;
                return allowMutations;
            });
            ForgeApi.Connect(engine);
            IForgeHookService hooks = ForgeApi.Hooks;
            var retained = hooks.Register(new ForgeHookDefinition
            {
                Id = "fixture-unload-callback-retained",
                Owner = "DetourFixture",
                Target = target,
                Prefix = invocation => { guardedPrefixCalls++; invocation.Arguments[0] = (int)invocation.Arguments[0] + 1; },
                Postfix = invocation => { guardedPostfixCalls++; invocation.Result = (int)invocation.Result + 100; }
            });
            var pending = hooks.Register(new ForgeHookDefinition
            {
                Id = "fixture-unload-callback-pending",
                Owner = "DetourFixture",
                Target = typeof(Program).GetMethod("ContextTransitionHookTarget", BindingFlags.Static | BindingFlags.NonPublic),
                Prefix = invocation => { }
            });
            MethodInfo rawTarget = typeof(Program).GetMethod("Target", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo rawReplacement = typeof(Program).GetMethod("Replacement", BindingFlags.Static | BindingFlags.NonPublic);
            RuntimeHelpers.PrepareMethod(rawTarget.MethodHandle);
            RuntimeHelpers.PrepareMethod(rawReplacement.MethodHandle);

            try
            {
                Console.WriteLine("[HookFixture] Runtime unload: disable retained callbacks before a disconnect that may fail, while preserving explicit cleanup.");
                if (!retained.Apply().Succeeded || GuardedHookTarget(2) != 110 ||
                    guardedTargetCalls != 1 || guardedPrefixCalls != 1 || guardedPostfixCalls != 1)
                {
                    Console.Error.WriteLine("FAIL: unload fixture did not establish a live Prefix/Postfix hook before shutdown.");
                    return 1;
                }

                MethodInfo stopApplications = typeof(TestEngine).GetMethod("StopApplicationsForUnload", BindingFlags.Instance | BindingFlags.NonPublic);
                if (stopApplications == null)
                {
                    Console.Error.WriteLine("FAIL: TestEngine does not expose the internal runtime unload application gate.");
                    return 1;
                }
                stopApplications.Invoke(engine, null);

                // Model a disconnect that passes preflight, then loses its host mutation
                // permission before service teardown. The unload path must keep every apply
                // route closed even though ForgeApi leaves its recovery capability published.
                closeGateAfterPreflight = true;
                gateChecks = 0;
                bool disconnectRejected = false;
                try { ForgeApi.DisconnectForUnload(); }
                catch (InvalidOperationException) { disconnectRejected = true; }
                closeGateAfterPreflight = false;
                allowMutations = true;
                bool recoveryPublished = ReferenceEquals(ForgeApi.Registry, engine) && ReferenceEquals(ForgeApi.Hooks, hooks);

                int afterShutdown = GuardedHookTarget(2);
                ForgeHookOperationResult blockedApply = hooks.Apply(pending.Snapshot.Id);
                bool registrationBlocked = false;
                try
                {
                    hooks.Register(new ForgeHookDefinition
                    {
                        Id = "fixture-unload-callback-late-registration",
                        Owner = "DetourFixture",
                        Target = target,
                        Prefix = invocation => { }
                    });
                }
                catch (InvalidOperationException) { registrationBlocked = true; }

                bool patchApplyBlocked = false;
                try { engine.ApplyMethodReplacement("fixture-unload-patch", "DetourFixture", rawTarget, rawReplacement); }
                catch (InvalidOperationException) { patchApplyBlocked = true; }
                bool rawApplyBlocked = false;
                bool unexpectedRawPatch = false;
                try
                {
                    ForgeDetour.Patch(rawTarget, rawReplacement);
                    unexpectedRawPatch = true;
                }
                catch (InvalidOperationException) { rawApplyBlocked = true; }
                finally
                {
                    if (unexpectedRawPatch)
                    {
                        try { ForgeDetour.Unpatch(rawTarget); }
                        catch (Exception error) { Console.Error.WriteLine("CLEANUP ERROR: " + error.Message); }
                    }
                }
                ForgeHookOperationResult verified = hooks.Verify(retained.Snapshot.Id);
                ForgeHookOperationResult reverted = retained.Revert();
                int afterRevert = GuardedHookTarget(2);
                pending.Revert();
                ForgeApi.DisconnectForUnload();

                if (!disconnectRejected || !recoveryPublished || ForgeApi.Registry != null || ForgeApi.Hooks != null ||
                    afterShutdown != 9 || guardedTargetCalls != 3 || guardedPrefixCalls != 1 || guardedPostfixCalls != 1 ||
                    blockedApply.Succeeded || blockedApply.State != ForgeHookState.Registered || !registrationBlocked ||
                    !patchApplyBlocked || !rawApplyBlocked || unexpectedRawPatch ||
                    !verified.Succeeded || !reverted.Succeeded || afterRevert != 9)
                {
                    Console.Error.WriteLine("FAIL: unload did not fail closed across hook, patch, and raw routes, or recovery/revert did not remain available.");
                    return 1;
                }

                Console.WriteLine("PASS: failed unload leaves retained callbacks inert, closes hook/patch/raw applications, and preserves verify/revert recovery.");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("FAIL: runtime unload callback gate: " + error.GetBaseException().Message);
                return 1;
            }
            finally
            {
                closeGateAfterPreflight = false;
                allowMutations = true;
                if (ReferenceEquals(ForgeApi.Registry, engine))
                {
                    try { retained.Revert(); }
                    catch (Exception error) { Console.Error.WriteLine("CLEANUP ERROR: " + error.Message); }
                    try { pending.Revert(); }
                    catch (Exception error) { Console.Error.WriteLine("CLEANUP ERROR: " + error.Message); }
                    try { ForgeApi.DisconnectForUnload(); }
                    catch (Exception error) { Console.Error.WriteLine("CLEANUP ERROR: " + error.GetBaseException().Message); }
                }
            }
        }

        private static int RunApiDisconnectGuardFixture()
        {
            MethodInfo target = typeof(Program).GetMethod("ApiDisconnectGuardHookTarget", BindingFlags.Static | BindingFlags.NonPublic);
            if (target == null) return Fail("API-disconnect guard target lookup", 0, 1);
            RuntimeHelpers.PrepareMethod(target.MethodHandle);
            apiDisconnectAllowed = true;

            int disconnectGateChecks = 0;
            bool closeGateAfterPreflight = false;
            var engine = new TestEngine(() =>
            {
                if (closeGateAfterPreflight)
                {
                    disconnectGateChecks++;
                    if (disconnectGateChecks == 2) return false;
                }
                return apiDisconnectAllowed;
            });
            ForgeApi.Connect(engine);
            IForgeHookService publishedHooks = ForgeApi.Hooks;
            IForgePatchService publishedPatches = ForgeApi.Patches;
            IForgeHookHandle handle = null;
            bool requestDisconnectInsideCallback = false;
            ForgeHookOperationResult selfRevertRequest = null;
            bool disconnectRejectedInsideCallback = false;
            bool servicesRetainedInsideCallback = false;

            try
            {
                if (publishedHooks == null || publishedPatches == null)
                    return Fail("published test-engine hook/patch services", 0, 1);
                handle = publishedHooks.Register(new ForgeHookDefinition
                {
                    Id = "fixture-api-disconnect-guard",
                    Owner = "DetourFixture",
                    Target = target,
                    Postfix = invocation =>
                    {
                        invocation.Result = (int)invocation.Result + 100;
                        if (!requestDisconnectInsideCallback) return;
                        selfRevertRequest = handle.Revert();
                        try { ForgeApi.Disconnect(); }
                        catch (InvalidOperationException) { disconnectRejectedInsideCallback = true; }
                        servicesRetainedInsideCallback = ReferenceEquals(ForgeApi.Registry, engine) &&
                            ReferenceEquals(ForgeApi.Hooks, publishedHooks) && ReferenceEquals(ForgeApi.Patches, publishedPatches);
                    }
                });

                Console.WriteLine("[HookFixture] SDK disconnect guard: retain published services and recovery handle while gate is closed.");
                if (!handle.Apply().Succeeded || ApiDisconnectGuardHookTarget(7) != 115)
                {
                    Console.Error.WriteLine("FAIL: the API-disconnect guard hook did not apply before gate closure.");
                    return 1;
                }

                apiDisconnectAllowed = false;
                bool rejected = false;
                try { ForgeApi.Disconnect(); }
                catch (InvalidOperationException) { rejected = true; }
                if (!rejected || !ReferenceEquals(ForgeApi.Registry, engine) ||
                    !ReferenceEquals(ForgeApi.Hooks, publishedHooks) || !ReferenceEquals(ForgeApi.Patches, publishedPatches) ||
                    handle.Snapshot.State != ForgeHookState.Applied || ApiDisconnectGuardHookTarget(7) != 15)
                {
                    Console.Error.WriteLine("FAIL: a rejected SDK disconnect detached a service or lost the active hook recovery route.");
                    return 1;
                }

                apiDisconnectAllowed = true;
                Console.WriteLine("[HookFixture] SDK disconnect retry: restore raw application acceptance if the host gate closes after preflight.");
                closeGateAfterPreflight = true;
                disconnectGateChecks = 0;
                bool retryRejected = false;
                try { ForgeApi.Disconnect(); }
                catch (InvalidOperationException) { retryRejected = true; }
                closeGateAfterPreflight = false;
                MethodInfo rawTarget = typeof(Program).GetMethod("Target", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo rawReplacement = typeof(Program).GetMethod("Replacement", BindingFlags.Static | BindingFlags.NonPublic);
                RuntimeHelpers.PrepareMethod(rawTarget.MethodHandle);
                RuntimeHelpers.PrepareMethod(rawReplacement.MethodHandle);
                bool rawApplicationsRestored = false;
                bool rawPatchMayBeActive = false;
                try
                {
                    ForgeDetour.Patch(rawTarget, rawReplacement);
                    rawPatchMayBeActive = true;
                    int patchedResult = Target(7);
                    bool unpatched = ForgeDetour.Unpatch(rawTarget);
                    rawPatchMayBeActive = false;
                    rawApplicationsRestored = patchedResult == 32 && unpatched && Target(7) == 15;
                }
                catch (InvalidOperationException) { }
                finally
                {
                    if (rawPatchMayBeActive)
                    {
                        try { ForgeDetour.Unpatch(rawTarget); }
                        catch (Exception error) { Console.Error.WriteLine("CLEANUP ERROR: " + error.Message); }
                    }
                }
                if (!retryRejected || !rawApplicationsRestored || !ReferenceEquals(ForgeApi.Registry, engine) ||
                    !ReferenceEquals(ForgeApi.Hooks, publishedHooks) || handle.Snapshot.State != ForgeHookState.Applied)
                {
                    Console.Error.WriteLine("FAIL: a post-preflight disconnect rejection stranded raw applications or unpublished the recovery service.");
                    return 1;
                }

                Console.WriteLine("[HookFixture] SDK disconnect race: reject disconnect inside a self-reverting active callback, then finish cleanup after Dispatch.");
                requestDisconnectInsideCallback = true;
                int inCallbackResult = ApiDisconnectGuardHookTarget(7);
                requestDisconnectInsideCallback = false;
                if (inCallbackResult != 115 || selfRevertRequest == null || selfRevertRequest.Succeeded ||
                    selfRevertRequest.State != ForgeHookState.Applied ||
                    selfRevertRequest.Detail.IndexOf("Undo is not yet verified", StringComparison.OrdinalIgnoreCase) < 0 ||
                    !disconnectRejectedInsideCallback || !servicesRetainedInsideCallback ||
                    !ReferenceEquals(ForgeApi.Hooks, publishedHooks) || handle.Snapshot.State != ForgeHookState.Reverted ||
                    IsHookTargetReserved(target) || ApiDisconnectGuardHookTarget(7) != 15)
                {
                    Console.Error.WriteLine("FAIL: SDK disconnect detached a service during a pending active dispatch, or deferred hook cleanup did not finish after dispatch exit.");
                    return 1;
                }

                ForgeApi.Disconnect();
                if (ForgeApi.Registry != null || ForgeApi.Hooks != null || ForgeApi.Patches != null ||
                    handle.Snapshot.State != ForgeHookState.Reverted)
                {
                    Console.Error.WriteLine("FAIL: a clean retry did not revert and disconnect the SDK services.");
                    return 1;
                }

                Console.WriteLine("PASS: rejected disconnect preserves SDK services/handle; approved revert then disconnect succeeds.");
                return 0;
            }
            finally
            {
                apiDisconnectAllowed = true;
                if (ForgeApi.Registry != null && ReferenceEquals(ForgeApi.Registry, engine))
                {
                    try { handle?.Revert(); } catch { }
                    try { ForgeApi.Disconnect(); } catch (Exception error) { Console.Error.WriteLine("CLEANUP ERROR: " + error.GetBaseException().Message); }
                }
            }
        }

        private static bool apiDisconnectAllowed;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int ApiDisconnectGuardHookTarget(int value)
        {
            return value * 2 + 1;
        }

        private static int RunReapplyOrderFixture()
        {
            MethodInfo firstTarget = typeof(Program).GetMethod("OrderHookTargetA", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo secondTarget = typeof(Program).GetMethod("OrderHookTargetB", BindingFlags.Static | BindingFlags.NonPublic);
            if (firstTarget == null || secondTarget == null) return Fail("reapply-order target lookup", 0, 1);
            RuntimeHelpers.PrepareMethod(firstTarget.MethodHandle);
            RuntimeHelpers.PrepareMethod(secondTarget.MethodHandle);

            var service = new ForgeHookService(() => true);
            var first = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-order-a",
                Owner = "DetourFixture",
                Target = firstTarget,
                Postfix = invocation => invocation.Result = (int)invocation.Result + 100
            });
            var second = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-order-b",
                Owner = "DetourFixture",
                Target = secondTarget,
                Postfix = invocation => invocation.Result = (int)invocation.Result + 200
            });

            try
            {
                Console.WriteLine("[HookFixture] Reapply order: Apply A/B, revert A, reapply A, verify reverse ledger order.");
                if (!first.Apply().Succeeded || !second.Apply().Succeeded ||
                    !first.Revert().Succeeded || !first.Apply().Succeeded)
                {
                    Console.Error.WriteLine("FAIL: the two-hook reapply-order setup could not complete.");
                    return 1;
                }

                FieldInfo orderField = typeof(ForgeHookService).GetField("applyOrder", BindingFlags.Instance | BindingFlags.NonPublic);
                var appliedOrder = orderField?.GetValue(service) as System.Collections.Generic.IList<string>;
                if (appliedOrder == null || appliedOrder.Count != 2 ||
                    appliedOrder[0] != "fixture-order-b" || appliedOrder[1] != "fixture-order-a")
                {
                    Console.Error.WriteLine("FAIL: a re-applied hook did not move to the end of the application-order ledger.");
                    return 1;
                }

                var reverted = service.RevertAll();
                bool allReverted = reverted.Count == 2;
                for (int i = 0; i < reverted.Count; i++)
                    allReverted &= reverted[i].Succeeded && reverted[i].State == ForgeHookState.Reverted;
                if (!allReverted || OrderHookTargetA(7) != 15 || OrderHookTargetB(7) != 15 || appliedOrder.Count != 0)
                {
                    Console.Error.WriteLine("FAIL: RevertAll did not clean the re-applied hooks and their order ledger.");
                    return 1;
                }

                Console.WriteLine("PASS: reapplication refreshes reverse-cleanup order and RevertAll empties the ledger.");
                return 0;
            }
            finally
            {
                service.Disconnect();
            }
        }

        private static int injectedIsAppliedReads;

        private static int RunUncertainApplyFixture()
        {
            MethodInfo target = typeof(Program).GetMethod("UncertainHookTarget", BindingFlags.Static | BindingFlags.NonPublic);
            if (target == null) return Fail("uncertain hook target lookup", 0, 1);
            RuntimeHelpers.PrepareMethod(target.MethodHandle);

            int before = UncertainHookTarget(7);
            if (before != 15) return Fail("pre-uncertain-hook target result", before, 15);

            Console.WriteLine("[HookFixture] Uncertain stage 1/2: inject an IsApplied status exception after Apply.");
            var service = new ForgeHookService(() => true);
            IForgeHookHandle handle = service.Register(new ForgeHookDefinition
            {
                Id = "fixture-uncertain-apply-status",
                Owner = "DetourFixture",
                Target = target,
                Prefix = invocation => invocation.Arguments[0] = (int)invocation.Arguments[0] + 2,
                Postfix = invocation => invocation.Result = (int)invocation.Result + 100
            });

            FieldInfo statusReader = typeof(ForgeHookService).GetField("hookIsApplied", BindingFlags.Instance | BindingFlags.NonPublic);
            if (statusReader == null || !statusReader.FieldType.IsGenericType || statusReader.FieldType.GetGenericTypeDefinition() != typeof(Func<,>))
                return Fail("uncertain hook status-reader seam", 0, 1);
            object originalStatusReader = statusReader.GetValue(service);

            injectedIsAppliedReads = 0;
            statusReader.SetValue(service, CreateInjectedStatusReader(statusReader));

            try
            {
                ForgeHookOperationResult apply = handle.Apply();
                if (apply.Succeeded || apply.State != ForgeHookState.Conflict ||
                    !apply.Detail.Contains("fixture-injected IsApplied status failure") || injectedIsAppliedReads != 2)
                {
                    Console.Error.WriteLine("FAIL: an IsApplied exception did not leave the applied hook in an explicitly uncertain state: " + apply.Detail);
                    return 1;
                }

                int during = UncertainHookTarget(7);
                if (during != 119) return Fail("uncertain but applied hook result", during, 119);
                if (!IsHookTargetReserved(target))
                {
                    Console.Error.WriteLine("FAIL: uncertain MonoMod status released the target reservation.");
                    return 1;
                }

                Console.WriteLine("[HookFixture] Uncertain stage 2/2: disconnect and verify cleanup of the reserved hook.");
                service.Disconnect();

                int after = UncertainHookTarget(7);
                if (after != 15 || handle.Snapshot.State != ForgeHookState.Reverted ||
                    IsHookTargetReserved(target) || injectedIsAppliedReads != 4)
                {
                    Console.Error.WriteLine("FAIL: disconnect did not query, remove, and release the uncertain hook; state=" + handle.Snapshot.State + ", status reads=" + injectedIsAppliedReads);
                    return 1;
                }

                Console.WriteLine("PASS: an IsApplied status exception kept the target reserved until disconnect verified cleanup.");

                Console.WriteLine("[HookFixture] Uncertain stage 3/4: inject a Verify status failure and retain conflict state.");
                service.Reconnect();
                MethodInfo verifyTarget = typeof(Program).GetMethod("VerifyStatusHookTarget", BindingFlags.Static | BindingFlags.NonPublic);
                RuntimeHelpers.PrepareMethod(verifyTarget.MethodHandle);
                if (VerifyStatusHookTarget(7) != 15) return Fail("pre-verify-status target result", VerifyStatusHookTarget(7), 15);
                IForgeHookHandle verifyHandle = service.Register(new ForgeHookDefinition
                {
                    Id = "fixture-verify-status-failure",
                    Owner = "DetourFixture",
                    Target = verifyTarget,
                    Prefix = invocation => invocation.Arguments[0] = (int)invocation.Arguments[0] + 2
                });
                if (!verifyHandle.Apply().Succeeded) return Fail("hook apply before Verify status injection", 0, 1);
                statusReader.SetValue(service, CreateThrowingStatusReader(statusReader));
                ForgeHookOperationResult verify = verifyHandle.Verify();
                if (verify.Succeeded || verify.State != ForgeHookState.Conflict ||
                    !verify.Detail.Contains("fixture-injected Verify status failure") || verifyHandle.Snapshot.State != ForgeHookState.Conflict)
                {
                    Console.Error.WriteLine("FAIL: Verify status exception did not update the retained service snapshot: " + verify.Detail);
                    return 1;
                }

                Console.WriteLine("[HookFixture] Uncertain stage 4/4: restore status reader and clean the conflicted handle.");
                statusReader.SetValue(service, originalStatusReader);
                service.Disconnect();
                if (VerifyStatusHookTarget(7) != 15 || verifyHandle.Snapshot.State != ForgeHookState.Reverted)
                {
                    Console.Error.WriteLine("FAIL: conflicted Verify status could not be cleaned up after the reader recovered.");
                    return 1;
                }
                Console.WriteLine("PASS: Verify uncertainty was visible in snapshots and resolved by verified disconnect cleanup.");
                return 0;
            }
            finally
            {
                // The fixture is serial and disposable; make a second cleanup attempt if an assertion fails.
                try
                {
                    statusReader.SetValue(service, originalStatusReader);
                    service.Disconnect();
                }
                catch (Exception error) { Console.Error.WriteLine("CLEANUP ERROR: " + error.GetBaseException().Message); }
            }
        }

        private static Delegate CreateInjectedStatusReader(FieldInfo statusReader)
        {
            Type hookType = statusReader.FieldType.GetGenericArguments()[0];
            ParameterExpression hook = Expression.Parameter(hookType, "hook");
            MethodInfo reader = typeof(Program).GetMethod("ReadInjectedIsAppliedStatus", BindingFlags.Static | BindingFlags.NonPublic);
            if (reader == null) throw new InvalidOperationException("The fixture IsApplied reader was not found.");
            Expression body = Expression.Call(reader, Expression.Convert(hook, typeof(object)));
            return Expression.Lambda(statusReader.FieldType, body, hook).Compile();
        }

        private static Delegate CreateThrowingStatusReader(FieldInfo statusReader)
        {
            Type hookType = statusReader.FieldType.GetGenericArguments()[0];
            ParameterExpression hook = Expression.Parameter(hookType, "hook");
            MethodInfo reader = typeof(Program).GetMethod("ThrowInjectedVerifyStatus", BindingFlags.Static | BindingFlags.NonPublic);
            if (reader == null) throw new InvalidOperationException("The fixture Verify status failure was not found.");
            Expression body = Expression.Call(reader, Expression.Convert(hook, typeof(object)));
            return Expression.Lambda(statusReader.FieldType, body, hook).Compile();
        }

        private static bool ReadInjectedIsAppliedStatus(object hook)
        {
            if (++injectedIsAppliedReads == 1)
                throw new InvalidOperationException("fixture-injected IsApplied status failure");

            PropertyInfo isApplied = hook.GetType().GetProperty("IsApplied", BindingFlags.Instance | BindingFlags.Public);
            if (isApplied == null) throw new InvalidOperationException("MonoMod IsApplied status was not found.");
            return (bool)isApplied.GetValue(hook, null);
        }

        private static bool ThrowInjectedVerifyStatus(object hook)
        {
            throw new InvalidOperationException("fixture-injected Verify status failure");
        }

        private static bool IsHookTargetReserved(MethodInfo target)
        {
            MethodInfo reservationCheck = typeof(ForgeHookService).GetMethod("HasAppliedTarget", BindingFlags.Static | BindingFlags.NonPublic);
            if (reservationCheck == null) throw new InvalidOperationException("The hook reservation check was not found.");
            return (bool)reservationCheck.Invoke(null, new object[] { target });
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int Target(int value)
        {
            return value * 2 + 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int HookTarget(int value)
        {
            return value * 2 + 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int BenchmarkPrefixTarget(int value)
        {
            return value * 2 + 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int BenchmarkPostfixTarget(int value)
        {
            return value * 2 + 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int BenchmarkBothTarget(int value)
        {
            return value * 2 + 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int UncertainHookTarget(int value)
        {
            return value * 2 + 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int VerifyStatusHookTarget(int value)
        {
            return value * 2 + 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void VoidHookTarget()
        {
            voidTargetCalls++;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int GuardedHookTarget(int value)
        {
            guardedTargetCalls++;
            return value + 7;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int SelfRevertingPrefixTarget(int value)
        {
            selfRevertingPrefixTargetCalls++;
            return value + 7;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int SelfDisposingPostfixTarget(int value)
        {
            selfDisposingPostfixTargetCalls++;
            return value + 7;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int CancelledValueHookTarget(int value)
        {
            cancelledValueTargetCalls++;
            return value + 7;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int ContextTransitionHookTarget(int value)
        {
            contextTransitionTargetCalls++;
            contextTransitionAllowed = false;
            return value + 7;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int OrderHookTargetA(int value)
        {
            return value * 2 + 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int OrderHookTargetB(int value)
        {
            return value * 2 + 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int ExternallyRemovedHookTarget(int value)
        {
            return value * 2 + 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        [ForgePatch(typeof(Program), nameof(Target))]
        private static int Replacement(int value)
        {
            return value * 4 + 4;
        }

        private static int Fail(string stage, int actual, int expected)
        {
            Console.Error.WriteLine("FAIL: " + stage + " returned " + actual + "; expected " + expected + ".");
            return 1;
        }
    }
}
