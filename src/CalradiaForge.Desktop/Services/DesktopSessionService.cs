using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalradiaForge.Desktop;

namespace CalradiaForge.Desktop.Services
{
    /// <summary>Asynchronous named-pipe boundary for the desktop. It exposes no game object or WPF control.</summary>
    internal sealed class DesktopSessionService : IDisposable
    {
        readonly PipeClient pipe;
        readonly DesktopMetricsService metrics;
        readonly Func<int?> processIdProvider;
        public DesktopSessionService(DesktopMetricsService metrics, PipeClient pipe = null, Func<int?> processIdProvider = null)
        {
            this.metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            this.pipe = pipe ?? new PipeClient();
            this.processIdProvider = processIdProvider ?? PipeClient.DetectBannerlordProcessId;
        }

        public bool IsConnected => pipe.Connected;
        public IReadOnlyCollection<string> Capabilities => pipe.Capabilities;
        public string LastError { get; private set; }

        public async Task<bool> ConnectAsync(CancellationToken cancellation)
        {
            LastError = null;
            var pid = processIdProvider();
            if (!pid.HasValue) { LastError = "No Bannerlord or official launcher process was found."; return false; }
            var connectionCompleted = false;
            try
            {
                return await metrics.MeasureAsync("ipc:connect", async () =>
                {
                    cancellation.ThrowIfCancellationRequested();
                    await pipe.Connect(pid.Value, cancellation).ConfigureAwait(true);
                    connectionCompleted = true;
                    cancellation.ThrowIfCancellationRequested();
                    return true;
                }, "Short named-pipe connection only; not a game-performance attribution.").ConfigureAwait(true);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { if (connectionCompleted) pipe.Disconnect(); LastError = "Connection was cancelled."; return false; }
            catch (OperationCanceledException error) { LastError = error.Message; return false; }
            catch (Exception error) { LastError = error.Message; return false; }
        }

        public async Task<bool> ReconnectAsync(CancellationToken cancellation)
        {
            LastError = null;
            var reconnectionCompleted = false;
            try
            {
                return await metrics.MeasureAsync("ipc:reconnect", async () =>
                {
                    cancellation.ThrowIfCancellationRequested();
                    var result = await pipe.TryAutoReconnect(processIdProvider(), 1500, cancellation).ConfigureAwait(true);
                    reconnectionCompleted = result;
                    cancellation.ThrowIfCancellationRequested();
                    if (!result) LastError = "Reconnection could not be established; the endpoint may be unavailable, reject the handshake, or time out.";
                    return result;
                }, "Short named-pipe reconnection only; not a game-performance attribution.").ConfigureAwait(true);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { if (reconnectionCompleted) pipe.Disconnect(); LastError = "Reconnection was cancelled."; return false; }
            catch (OperationCanceledException error) { LastError = error.Message; return false; }
            catch (Exception error) { LastError = error.Message; return false; }
        }

        public async Task<CalradiaForge.Core.Response> SendAsync(CalradiaForge.Core.Request request, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            return await pipe.Send(request, cancellation).ConfigureAwait(true);
        }

        public bool Supports(string capability) => pipe.Supports(capability);
        public void Dispose() => pipe.Dispose();
    }
}
