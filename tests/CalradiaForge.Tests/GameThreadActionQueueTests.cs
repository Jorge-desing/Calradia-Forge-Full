using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalradiaForge.Mod;

namespace CalradiaForge.Tests
{
    internal static class GameThreadActionQueueTests
    {
        public static void Run(Action<string, Action> test)
        {
            test("Game-thread action queue enforces its extension-request capacity", Capacity);
            test("Game-thread action queue delivers trusted completions beyond request capacity", TrustedCompletion);
            test("UI page removal defers Gauntlet teardown to the game thread", UiPageRemovalDispatch);
            test("Game-thread queue capacity accounting stays bounded under concurrent producers", ConcurrentCapacity);
            test("SubModule unload cleanup continues after an individual failure", UnloadCleanupIsolation);
        }

        static void Capacity()
        {
            var queue = new GameThreadActionQueue(2);
            var result = new List<int>();
            Require(queue.TryEnqueue(() => result.Add(1)), "First request should fit.");
            Require(queue.TryEnqueue(() => result.Add(2)), "Second request should fit.");
            Require(!queue.TryEnqueue(() => result.Add(3)), "Request queue should reject work at its configured limit.");
            Require(queue.PendingCount == 2, "Reserved queue count should match the accepted requests.");
            while (queue.TryDequeue(out var action)) action();
            Require(result.SequenceEqual(new[] { 1, 2 }), "Accepted actions should retain FIFO order.");
            Require(queue.PendingCount == 0, "Dequeue should release each capacity reservation.");
        }

        static void TrustedCompletion()
        {
            var queue = new GameThreadActionQueue(1);
            var result = new List<int>();
            Require(queue.TryEnqueue(() => result.Add(1)), "Extension request should fit.");
            queue.Enqueue(() => result.Add(2));
            Require(queue.PendingCount == 2, "Host completion should remain deliverable when the bounded request queue is full.");
            while (queue.TryDequeue(out var action)) action();
            Require(result.SequenceEqual(new[] { 1, 2 }), "Trusted completion should run after previously queued work.");
        }

        static void UiPageRemovalDispatch()
        {
            Action queued = null;
            var invoked = false;
            GameThreadActionDispatch.RunOrPost(false, action => queued = action, () => invoked = true);
            Require(!invoked && queued != null, "Off-thread page removal must enqueue the teardown callback.");
            queued();
            Require(invoked, "The queued teardown must run when the game thread drains its queue.");

            var inline = false;
            GameThreadActionDispatch.RunOrPost(true, _ => throw new Exception("Game-thread callbacks should not be requeued."), () => inline = true);
            Require(inline, "Page removal already on the game thread should execute inline.");

            var source = File.ReadAllText(Path.Combine(FindWorkspaceRoot(), "src", "CalradiaForge.Mod", "SubModule.cs"));
            var start = source.IndexOf("void OnUiPagesRemoved(string moduleId)", StringComparison.Ordinal);
            var end = start < 0 ? -1 : source.IndexOf("\n        void Close()", start, StringComparison.Ordinal);
            Require(start >= 0 && end > start, "Could not locate the UI-page removal handler.");
            var handler = source.Substring(start, end - start);
            Require(handler.Contains("GameThreadActionDispatch.RunOrPost"), "UI-page removal must use the game-thread dispatcher.");
            Require(handler.IndexOf("string.Equals(extensionPageOwner", StringComparison.Ordinal) > handler.IndexOf("() =>", StringComparison.Ordinal),
                "Active page owner must be read inside the dispatched callback.");
        }

        static void ConcurrentCapacity()
        {
            const int capacity = 64;
            var queue = new GameThreadActionQueue(capacity);
            var accepted = 0;
            Parallel.For(0, 4096, _ =>
            {
                if (queue.TryEnqueue(() => { })) System.Threading.Interlocked.Increment(ref accepted);
            });
            Require(accepted == capacity, "Concurrent producers must accept exactly the bounded capacity.");
            Require(queue.PendingCount == capacity, "Concurrent capacity counter exceeded or fell below its limit.");
            var dequeued = 0;
            while (queue.TryDequeue(out _)) dequeued++;
            Require(dequeued == capacity && queue.PendingCount == 0, "All accepted actions should be dequeued exactly once.");
        }

        static void UnloadCleanupIsolation()
        {
            var events = new List<string>();
            var failures = new List<string>();
            UnloadCleanupRunner.Run(new[]
            {
                new KeyValuePair<string, Action>("first", () => events.Add("first")),
                new KeyValuePair<string, Action>("cancel pending panel work", () => throw new InvalidOperationException("cancel fault")),
                new KeyValuePair<string, Action>("finalize panel ViewModel", () => events.Add("finalized")),
                new KeyValuePair<string, Action>("last", () => events.Add("last"))
            }, (name, error) => failures.Add(name + ":" + error.Message));

            Require(events.SequenceEqual(new[] { "first", "finalized", "last" }), "A failing cancellation must not prevent ViewModel finalization or later cleanup steps.");
            Require(failures.SequenceEqual(new[] { "cancel pending panel work:cancel fault" }), "The cleanup failure should be reported with its step name and exception.");

            var source = File.ReadAllText(Path.Combine(FindWorkspaceRoot(), "src", "CalradiaForge.Mod", "SubModule.cs"));
            var start = source.IndexOf("protected override void OnSubModuleUnloaded()", StringComparison.Ordinal);
            var end = start < 0 ? -1 : source.IndexOf("public override void OnMissionBehaviorInitialize", start, StringComparison.Ordinal);
            Require(start >= 0 && end > start, "Could not locate module-unload cleanup wiring.");
            var unload = source.Substring(start, end - start);
            Require(unload.Contains("\"cancel pending panel work\"") && unload.Contains("\"finalize panel ViewModel\""),
                "Unload must isolate panel cancellation and ViewModel finalization as separate cleanup actions.");

            var runtimeSource = File.ReadAllText(Path.Combine(FindWorkspaceRoot(), "src", "CalradiaForge.Mod", "Runtime.cs"));
            var disposeStart = runtimeSource.IndexOf("public void Dispose()", StringComparison.Ordinal);
            var disposeEnd = disposeStart < 0 ? -1 : runtimeSource.IndexOf("static void ReportRuntimeCleanupFailure", disposeStart, StringComparison.Ordinal);
            Require(disposeStart >= 0 && disposeEnd > disposeStart, "Could not locate runtime cleanup wiring.");
            var dispose = runtimeSource.Substring(disposeStart, disposeEnd - disposeStart);
            Require(dispose.Contains("UnloadCleanupRunner.Run") && dispose.Contains("\"stop SDK applications\"") &&
                    dispose.Contains("\"disconnect SDK\"") && dispose.Contains("\"dispose IPC server\"") &&
                    dispose.Contains("\"schedule session log persistence\""),
                "Runtime.Dispose must isolate engine stop, SDK disconnect, IPC disposal, and log persistence.");
        }

        static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        static string FindWorkspaceRoot()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory })
            {
                var current = Path.GetFullPath(start);
                for (var depth = 0; depth < 8; depth++)
                {
                    if (File.Exists(Path.Combine(current, "CalradiaForge.sln"))) return current;
                    var parent = Directory.GetParent(current);
                    if (parent == null) break;
                    current = parent.FullName;
                }
            }
            throw new DirectoryNotFoundException("Could not locate CalradiaForge.sln for the game-thread source contract test.");
        }
    }
}
