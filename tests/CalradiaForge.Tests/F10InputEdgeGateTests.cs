using System;
using CalradiaForge.Mod;

namespace CalradiaForge.Tests
{
    internal static class F10InputEdgeGateTests
    {
        internal static void Run(Action<string, Action> test)
        {
            test("F10 edge gate emits one toggle per continuous signal and rearms on release", VerifyContinuousSignalIsOnePress);
        }

        private static void VerifyContinuousSignalIsOnePress()
        {
            var gate = new F10InputEdgeGate();
            if (!gate.Poll(true, false, true))
                throw new Exception("The first pressed/immediate F10 signal must emit a toggle edge.");
            if (gate.Poll(true, false, true) || gate.Poll(false, false, true))
                throw new Exception("A continuous F10 signal must not emit a second toggle edge.");
            if (gate.Poll(false, false, false))
                throw new Exception("An F10 release must rearm the gate without toggling the panel.");

            if (!gate.Poll(false, true, false))
                throw new Exception("The IsKeyDown fallback must emit the next F10 edge.");
            if (gate.Poll(false, true, true))
                throw new Exception("Overlapping F10 down signals must remain one continuous press.");
            if (gate.Poll(false, false, false))
                throw new Exception("Releasing the overlapping F10 signal must not toggle the panel.");

            if (!gate.Poll(false, false, true) || gate.Poll(false, false, true))
                throw new Exception("The IsKeyDownImmediate fallback must emit only one edge until release.");
            if (gate.Poll(false, false, false))
                throw new Exception("The immediate-key release must only rearm the F10 gate.");

            if (!gate.Poll(true, false, false) || gate.Poll(true, false, false))
                throw new Exception("The normal IsKeyPressed signal must also be deduplicated.");

            gate.Reset();
            if (!gate.Poll(false, true, false))
                throw new Exception("Reset must rearm the F10 gate when the configured hotkey changes away and back.");
        }
    }
}
