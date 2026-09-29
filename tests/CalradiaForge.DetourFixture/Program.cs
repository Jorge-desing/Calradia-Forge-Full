using System;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        private static int Main()
        {
            if (!Environment.Is64BitProcess)
            {
                Console.Error.WriteLine("FAIL: fixture must run as x64.");
                return 2;
            }

            MethodInfo target = typeof(Program).GetMethod("Target", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo replacement = typeof(Program).GetMethod("Replacement", BindingFlags.Static | BindingFlags.NonPublic);
            if (target == null || replacement == null)
            {
                Console.Error.WriteLine("FAIL: fixture methods could not be resolved.");
                return 2;
            }

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

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static int Target(int value)
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
