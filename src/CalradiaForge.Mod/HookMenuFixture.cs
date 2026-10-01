using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using CalradiaForge.Core;
using CalradiaForge.Sdk;
using MonoMod.Cil;

namespace CalradiaForge.Mod
{
    /// <summary>Explicit console fixture that changes only its own managed target.</summary>
    internal static class HookMenuFixture
    {
        const string CallbackId = "cf.fixture.callback";
        const string TranspilerId = "cf.fixture.il";
        const string Owner = "CalradiaForge.MenuFixture";
        static int prefixCalls;
        static int postfixCalls;
        static int finalizerCalls;
        static int finalizerFailures;
        static int rebuilds;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static int ForgeMainMenuFixtureTarget() => 10;

        internal static string Register(TestEngine engine, Func<bool> canManage)
        {
            RequireMenu(canManage);
            if (engine == null) throw new InvalidOperationException("The connected hook fixture host is unavailable.");
            IForgeHookService service = engine;
            MethodInfo target = typeof(HookMenuFixture).GetMethod(nameof(ForgeMainMenuFixtureTarget), BindingFlags.Static | BindingFlags.NonPublic);
            ForgeHookSnapshot[] snapshots = service.GetSnapshots().ToArray();
            foreach (ForgeHookSnapshot snapshot in snapshots.Where(snapshot => snapshot.Id == CallbackId || snapshot.Id == TranspilerId))
                if (snapshot.Owner != Owner || !snapshot.TargetMethod.StartsWith(typeof(HookMenuFixture).FullName + "." + nameof(ForgeMainMenuFixtureTarget) + "(", StringComparison.Ordinal))
                    throw new InvalidOperationException("A fixture ID is already registered to another target or owner.");

            if (!snapshots.Any(snapshot => snapshot.Id == CallbackId))
                service.Register(new ForgeHookDefinition
                {
                    Id = CallbackId, Owner = Owner, Target = target,
                    Prefix = invocation => Interlocked.Increment(ref prefixCalls),
                    Postfix = invocation => { Interlocked.Increment(ref postfixCalls); invocation.Result = (int)invocation.Result + 1; },
                    Finalizer = invocation =>
                    {
                        Interlocked.Increment(ref finalizerCalls);
                        if (invocation.Exception != null) Interlocked.Increment(ref finalizerFailures);
                    }
                });
            if (!snapshots.Any(snapshot => snapshot.Id == TranspilerId))
                engine.RegisterTranspiler(new ForgeHookDefinition { Id = TranspilerId, Owner = Owner, Target = target }, context =>
                {
                    var cursor = new ILCursor(context);
                    if (!cursor.TryGotoNext(instruction => instruction.MatchLdcI4(10)))
                        throw new InvalidOperationException("The owned main-menu fixture target no longer contains its expected constant.");
                    cursor.Next.OpCode = Mono.Cecil.Cil.OpCodes.Ldc_I4;
                    cursor.Next.Operand = 20;
                    Interlocked.Increment(ref rebuilds);
                });

            return "Registered the owned fixture IDs cf.fixture.callback and cf.fixture.il without applying them. Prepare an Apply plan in Hook Workbench or with cf.hook_apply, confirm only after reviewing the plan token, then run cf.hook_fixture run: expected 10 before, 21 with both applied, and 10 after both are reverted.";
        }

        internal static string Run(Func<bool> canManage)
        {
            RequireMenu(canManage);
            int prefixesBefore = Volatile.Read(ref prefixCalls);
            int postfixesBefore = Volatile.Read(ref postfixCalls);
            int finalizersBefore = Volatile.Read(ref finalizerCalls);
            int failuresBefore = Volatile.Read(ref finalizerFailures);
            int value = ForgeMainMenuFixtureTarget();
            return "Owned fixture target returned " + value + ". Expected: 10 inactive, 11 callbacks only, 20 IL only, 21 both. This call: Prefix=" +
                (Volatile.Read(ref prefixCalls) - prefixesBefore) + ", Postfix=" + (Volatile.Read(ref postfixCalls) - postfixesBefore) +
                ", Finalizer=" + (Volatile.Read(ref finalizerCalls) - finalizersBefore) + ", pending failures=" +
                (Volatile.Read(ref finalizerFailures) - failuresBefore) + ". IL rebuilds=" + Volatile.Read(ref rebuilds) + ".";
        }

        static void RequireMenu(Func<bool> canManage)
        {
            if (canManage == null || !canManage())
                throw new InvalidOperationException("The owned hook fixture is available only in the approved game-thread main-menu context.");
        }
    }
}
