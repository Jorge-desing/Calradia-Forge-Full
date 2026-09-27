using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalradiaForge.Core;

namespace CalradiaForge.Desktop
{
    internal sealed class PipeClient : IDisposable
    {
        const int DefaultConnectTimeoutMs = 2000;
        const int DefaultResponseTimeoutMs = 20000;
        const int MaximumConnectTimeoutMs = 10000;
        const int MaximumReconnectTimeoutMs = 5000;
        const int MaximumResponseCharacters = 32 * 1024 * 1024;

        NamedPipeClientStream pipe;
        StreamReader responseStreamReader;
        BoundedLineReader responseReader;
        StreamWriter writer;
        readonly int maximumResponseCharacters;
        readonly SemaphoreSlim gate = new(1);
        bool disposed;

        public PipeClient() : this(MaximumResponseCharacters) { }

        internal PipeClient(int maximumResponseCharacters)
        {
            if (maximumResponseCharacters <= 0) throw new ArgumentOutOfRangeException(nameof(maximumResponseCharacters));
            this.maximumResponseCharacters = maximumResponseCharacters;
        }

        public bool Connected => pipe?.IsConnected == true;
        public IReadOnlyCollection<string> Capabilities { get; private set; } = [];
        public bool Supports(string action) => Capabilities.Contains(action, StringComparer.OrdinalIgnoreCase);
        public int LastPid { get; private set; }

        public static int? DetectBannerlordProcessId()
        {
            try
            {
                var process = System.Diagnostics.Process.GetProcessesByName("Bannerlord").FirstOrDefault()
                    ?? System.Diagnostics.Process.GetProcessesByName("Bannerlord.Native").FirstOrDefault()
                    ?? System.Diagnostics.Process.GetProcessesByName("TaleWorlds.MountAndBlade.Launcher").FirstOrDefault();
                return process?.Id;
            }
            catch
            {
                return null;
            }
        }

        public async Task Connect(int pid, CancellationToken cancellationToken = default, int timeoutMs = DefaultConnectTimeoutMs)
        {
            if (timeoutMs <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMs));
            timeoutMs = Math.Min(timeoutMs, MaximumConnectTimeoutMs);
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                await ConnectCore(pid, timeoutMs, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                Disconnect();
                throw;
            }
            finally
            {
                gate.Release();
            }
        }

        async Task ConnectCore(int pid, int timeoutMs, CancellationToken cancellationToken)
        {
            Disconnect();
            pipe = new NamedPipeClientStream(".", "CalradiaForge-" + pid, PipeDirection.InOut, PipeOptions.Asynchronous);
            using (var timeout = new CancellationTokenSource(timeoutMs))
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token))
            {
                try
                {
                    await pipe.ConnectAsync(linked.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Named-pipe connection was cancelled.", error, cancellationToken);
                }
                catch (OperationCanceledException error) when (timeout.IsCancellationRequested)
                {
                    throw new TimeoutException($"Named-pipe connection timed out after {timeoutMs} ms.", error);
                }
            }

            responseStreamReader = new StreamReader(pipe, new UTF8Encoding(false), false, 4096, true);
            responseReader = new BoundedLineReader(responseStreamReader, maximumResponseCharacters);
            writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true };
            var response = await SendCore(new Request { Action = "hello" }, cancellationToken, timeoutMs).ConfigureAwait(false);
            if (!response.Success || response.Version != 1) throw new IOException("Incompatible server");
            Capabilities = new HashSet<string>(Json.Deserialize<string[]>(response.Data) ?? [], StringComparer.OrdinalIgnoreCase);
            LastPid = pid;
        }

        public async Task Reconnect(CancellationToken cancellationToken = default)
        {
            var pid = LastPid > 0
                ? LastPid
                : DetectBannerlordProcessId() ?? throw new InvalidOperationException("No Bannerlord process found to reconnect to.");
            await Connect(pid, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryAutoReconnect(int? preferredPid = null, int timeoutMs = 1500, CancellationToken cancellationToken = default)
        {
            if (timeoutMs <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMs));
            timeoutMs = Math.Min(timeoutMs, MaximumReconnectTimeoutMs);
            if (disposed) return false;
            var pid = preferredPid ?? DetectBannerlordProcessId() ?? (LastPid > 0 ? LastPid : 0);
            if (pid <= 0) return false;

            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                await ConnectCore(pid, timeoutMs, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Disconnect();
                throw;
            }
            catch
            {
                Disconnect();
                return false;
            }
            finally
            {
                gate.Release();
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool PeekNamedPipe(
            Microsoft.Win32.SafeHandles.SafePipeHandle hNamedPipe,
            byte[] lpBuffer,
            uint nBufferSize,
            IntPtr lpBytesRead,
            IntPtr lpTotalBytesAvail,
            IntPtr lpBytesLeftThisMessage);

        public bool CheckHeartbeat()
        {
            if (disposed || pipe == null || !pipe.IsConnected)
            {
                if (pipe != null) Disconnect();
                return false;
            }

            try
            {
                if (pipe.SafePipeHandle == null || pipe.SafePipeHandle.IsInvalid || pipe.SafePipeHandle.IsClosed)
                {
                    Disconnect();
                    return false;
                }

                if (OperatingSystem.IsWindows())
                {
                    var ok = PeekNamedPipe(pipe.SafePipeHandle, null, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                    if (!ok)
                    {
                        Disconnect();
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                Disconnect();
                return false;
            }
        }

        public Task<Response> Send(Request request) => Send(request, CancellationToken.None);

        public async Task<Response> Send(Request request, CancellationToken cancellationToken)
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await SendCore(request, cancellationToken, DefaultResponseTimeoutMs).ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        }

        async Task<Response> SendCore(Request request, CancellationToken cancellationToken, int timeoutMs)
        {
            ThrowIfDisposed();
            if (!Connected) throw new IOException("Disconnected — connect to an active game process.");

            using (var timeout = new CancellationTokenSource(timeoutMs))
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token))
            {
                try
                {
                    var payload = Json.Serialize(request);
                    await writer.WriteLineAsync(payload.AsMemory(), linked.Token).ConfigureAwait(false);
                    var line = await responseReader.ReadLineAsync(linked.Token).ConfigureAwait(false);
                    if (line == null) throw new EndOfStreamException();
                    var response = Json.Deserialize<Response>(line);
                    if (response.Id != request.Id || response.Version != 1) throw new IOException("Protocol mismatch");
                    return response;
                }
                catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested)
                {
                    Disconnect();
                    throw new OperationCanceledException("Named-pipe request was cancelled.", error, cancellationToken);
                }
                catch (OperationCanceledException error) when (timeout.IsCancellationRequested)
                {
                    Disconnect();
                    throw new TimeoutException($"Named-pipe response timed out after {timeoutMs} ms.", error);
                }
                catch
                {
                    Disconnect();
                    throw;
                }
            }
        }

        void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(PipeClient));
        }

        public void Disconnect()
        {
            pipe?.Dispose();
            pipe = null;
            responseStreamReader = null;
            responseReader = null;
            writer = null;
            Capabilities = [];
        }

        public async Task<Response> QueryAgentMemoryStats(CancellationToken cancellationToken = default)
        {
            return await Send(new Request { Action = "agent-memory" }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Response> QueryAgentMemory(string agentId, CancellationToken cancellationToken = default)
        {
            return await Send(new Request { Action = "agent-memory-query", Argument = agentId }, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Disconnect();
        }
    }
}
