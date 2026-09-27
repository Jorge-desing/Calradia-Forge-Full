using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CalradiaForge.Desktop.Services
{
    internal sealed class DesktopMeasurement
    {
        public string Operation { get; set; }
        public double ElapsedMilliseconds { get; set; }
        public long? AllocatedBytes { get; set; }
        public string Limit { get; set; }
    }

    /// <summary>Short, opt-in measurements. It records observations and never infers a game-wide performance claim.</summary>
    internal sealed class DesktopMetricsService
    {
        readonly Queue<DesktopMeasurement> recent = [];
        const int MaximumMeasurements = 64;
        public IReadOnlyCollection<DesktopMeasurement> Recent => recent.ToArray();

        public DesktopMeasurementScope Start(string operation, string limit) =>
            new(this, operation, limit, GC.GetAllocatedBytesForCurrentThread(), Stopwatch.StartNew());

        internal void Complete(string operation, string limit, long before, Stopwatch watch)
        {
            watch.Stop();
            recent.Enqueue(new DesktopMeasurement {
                Operation = operation,
                ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds,
                AllocatedBytes = Math.Max(0, GC.GetAllocatedBytesForCurrentThread() - before),
                Limit = limit ?? "Short desktop operation only; not a game-wide performance attribution."
            });
            TrimRecent();
        }

        internal void CompleteAsync(string operation, string limit, Stopwatch watch)
        {
            watch.Stop();
            recent.Enqueue(new DesktopMeasurement {
                Operation = operation,
                ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds,
                AllocatedBytes = null,
                Limit = limit ?? "Short desktop operation only; not a game-wide performance attribution."
            });
            TrimRecent();
        }

        void TrimRecent()
        {
            while (recent.Count > MaximumMeasurements) recent.Dequeue();
        }

        public async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action, string limit)
        {
            var watch = Stopwatch.StartNew();
            try { return await action().ConfigureAwait(true); }
            finally
            {
                // GC.GetAllocatedBytesForCurrentThread is thread-local. An async
                // continuation may resume on another thread, so a before/after
                // delta would not describe the operation's total allocations.
                CompleteAsync(operation, limit, watch);
            }
        }

        public string FormatRecent()
        {
            if (recent.Count == 0) return "Not run — no short desktop measurements have been recorded.";
            var lines = new List<string>(recent.Count + 1) { "MEASURED DESKTOP OPERATIONS" };
            foreach (var item in recent)
            {
                string allocation = item.AllocatedBytes.HasValue
                    ? item.AllocatedBytes.Value + " B"
                    : "not measured (async thread-local counter)";
                lines.Add(item.Operation + " | " + item.ElapsedMilliseconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ms | " + allocation + " | " + item.Limit);
            }
            return string.Join(Environment.NewLine, lines);
        }
    }

    internal sealed class DesktopMeasurementScope(DesktopMetricsService owner, string operation, string limit, long before, Stopwatch watch) : IDisposable
    {
        bool completed;

        public void Dispose()
        {
            if (completed) return;
            completed = true;
            owner.Complete(operation, limit, before, watch);
        }
    }
}
