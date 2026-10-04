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
        public double? LastRoundTripLatencyMs { get; private set; }

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
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { if (connectionCompleted) pipe.Disconnect(); LastError = "Connection was cancelled."; LastRoundTripLatencyMs = null; return false; }
            catch (OperationCanceledException error) { LastError = error.Message; LastRoundTripLatencyMs = null; return false; }
            catch (Exception error) { LastError = error.Message; LastRoundTripLatencyMs = null; return false; }
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
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { if (reconnectionCompleted) pipe.Disconnect(); LastError = "Reconnection was cancelled."; LastRoundTripLatencyMs = null; return false; }
            catch (OperationCanceledException error) { LastError = error.Message; LastRoundTripLatencyMs = null; return false; }
            catch (Exception error) { LastError = error.Message; LastRoundTripLatencyMs = null; return false; }
        }

        public async Task<CalradiaForge.Core.Response> SendAsync(CalradiaForge.Core.Request request, CancellationToken cancellation)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            cancellation.ThrowIfCancellationRequested();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var response = await pipe.Send(request, cancellation).ConfigureAwait(true);
                sw.Stop();
                LastRoundTripLatencyMs = sw.Elapsed.TotalMilliseconds;
                return response;
            }
            catch
            {
                sw.Stop();
                LastRoundTripLatencyMs = null;
                throw;
            }
        }

        public async Task<double?> PingAsync(CancellationToken cancellation = default)
        {
            if (!pipe.Connected) return null;
            try
            {
                var response = await SendAsync(new CalradiaForge.Core.Request { Action = "ping" }, cancellation).ConfigureAwait(true);
                return response != null ? LastRoundTripLatencyMs : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> QueryVariableAsync(string variableName, CancellationToken cancellation = default)
        {
            if (!pipe.Connected || string.IsNullOrWhiteSpace(variableName)) return null;
            try
            {
                var response = await SendAsync(new CalradiaForge.Core.Request { Action = "query-variable", Argument = variableName }, cancellation).ConfigureAwait(true);
                return response?.Success == true ? response.Data : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> QueryAgentMemoryAsync(string agentId, CancellationToken cancellation = default)
        {
            if (!pipe.Connected || string.IsNullOrWhiteSpace(agentId)) return null;
            try
            {
                var response = await SendAsync(new CalradiaForge.Core.Request { Action = "query-agent-memory", Argument = agentId }, cancellation).ConfigureAwait(true);
                return response?.Success == true ? response.Data : null;
            }
            catch
            {
                return null;
            }
        }

        public bool Supports(string capability) => pipe.Supports(capability);
        public void Dispose() => pipe.Dispose();
    }
}
