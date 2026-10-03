using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using CalradiaForge.Mod;

namespace CalradiaForge.Tests
{
    internal static class F10InputEdgeGateTests
    {
        internal static void Run(Action<string, Action> test)
        {
            test("F10 edge gate emits one toggle per continuous signal and rearms on release", VerifyContinuousSignalIsOnePress);
            test("Crash report JSON preserves Windows paths and control characters", VerifyCrashReportEscaping);
        }

        private static void VerifyCrashReportEscaping()
        {
            var timestamp = new DateTime(2026, 10, 2, 12, 30, 45, DateTimeKind.Utc);
            const string exceptionText = "System.Exception: C:\\mods\\bad\"path\r\n\t\u0001";
            var formatter = typeof(SubModule).GetMethod("BuildCrashReport", BindingFlags.NonPublic | BindingFlags.Static);
            if (formatter == null)
                throw new Exception("SubModule must retain the tested crash-report formatter.");

            var json = (string)formatter.Invoke(null, new object[] { timestamp, true, exceptionText });
            var serializer = new DataContractJsonSerializer(typeof(CrashReport));
            CrashReport report;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                report = (CrashReport)serializer.ReadObject(stream);

            if (report.Timestamp != timestamp.ToString("O") ||
                !report.IsTerminating ||
                report.Exception != exceptionText)
            {
                throw new Exception("The JSON crash report must preserve its timestamp, termination flag and exception text.");
            }
        }

        [DataContract]
        private sealed class CrashReport
        {
            [DataMember(Name = "Timestamp")]
            public string Timestamp { get; set; }

            [DataMember(Name = "IsTerminating")]
            public bool IsTerminating { get; set; }

            [DataMember(Name = "Exception")]
            public string Exception { get; set; }
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
