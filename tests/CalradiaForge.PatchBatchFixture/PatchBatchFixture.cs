using CalradiaForge.Sdk.Patcher;
using System.Runtime.CompilerServices;

namespace CalradiaForge.TestFixtures
{
    /// <summary>Two exact declarations for deterministic shared-registry batch tests.</summary>
    public static class PatchBatchMethods
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static int TargetOne(int value) { return value + 10; }

        [ForgePatch(typeof(PatchBatchMethods), nameof(TargetOne))]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static int ReplacementOne(int value) { return value + 20; }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static int TargetTwo(int value) { return value + 30; }

        [ForgePatch(typeof(PatchBatchMethods), nameof(TargetTwo))]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static int ReplacementTwo(int value) { return value + 40; }
    }
}
