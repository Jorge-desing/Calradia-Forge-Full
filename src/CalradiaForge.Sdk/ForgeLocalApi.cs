using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;

namespace CalradiaForge.Sdk.Api
{
    /// <summary>
    /// A local REST API server for external tools to interact with Calradia Forge.
    /// Exposes health, version info, and agent-memory statistics.
    /// Implements proper disposal to prevent memory leaks (no socket-held threads survive Stop).
    /// </summary>
    public sealed class ForgeLocalApi : IDisposable
    {
        private static readonly byte[] StatusResponseBytes = Encoding.UTF8.GetBytes("{\"status\":\"running\"}");
        private HttpListener _listener;
        private Thread _serverThread;
        private readonly WaitCallback _requestCallback;
        private volatile bool _isRunning;
        private bool _disposed;

        public ForgeLocalApi()
        {
            _requestCallback = HandleQueuedContext;
        }

        /// <summary>True when the listener thread is active and accepting connections.</summary>
        public bool IsRunning => _isRunning;

        public void Start(string url = "http://localhost:59999/")
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ForgeLocalApi));
            if (_isRunning) return;

            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _listener.Start();
            _isRunning = true;

            _serverThread = new Thread(Listen) { IsBackground = true, Name = "ForgeLocalApi" };
            _serverThread.Start();
        }

        private void Listen()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_requestCallback, context);
                }
                catch (HttpListenerException)
                {
                    break; // Listener stopped — exit cleanly
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Keep server alive on unexpected errors
                }
            }
        }

        private void HandleContext(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                // CORS header for local web tool usage
                response.Headers["Access-Control-Allow-Origin"] = "*";
                response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
                response.ContentType = "application/json; charset=utf-8";

                byte[] buffer;
                switch (request.Url?.AbsolutePath?.TrimEnd('/'))
                {
                    case "/info":
                    case "":
                        buffer = Encoding.UTF8.GetBytes(BuildInfoResponse());
                        break;
                    case "/status":
                        buffer = StatusResponseBytes;
                        break;
                    case "/agents":
                        buffer = Encoding.UTF8.GetBytes(BuildAgentsResponse());
                        break;
                    default:
                        response.StatusCode = 404;
                        buffer = Encoding.UTF8.GetBytes("{\"error\":\"not found\"}");
                        break;
                }

                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception)
            {
                // Request handling errors are isolated — never crash the listener thread
                try { context?.Response?.Abort(); } catch { }
            }
        }

        private static string BuildInfoResponse()
        {
            return $"{{\"version\":\"5.0.0\",\"sdk\":{ForgeApi.Version},\"status\":\"running\"}}";
        }

        private static string BuildAgentsResponse()
        {
            // Preserve the existing collection field while exposing only aggregate counts.
            // Never return agent IDs, keys, or stored payloads from the local endpoint.
            ForgeAgentMemory.GetStatistics(
                out int agentCount,
                out int semanticEntries,
                out int episodicEntries,
                out int proceduralEntries);

            return "{\"agents\":[],\"statistics\":{" +
                   "\"agentCount\":" + agentCount.ToString(CultureInfo.InvariantCulture) + "," +
                   "\"semanticEntries\":" + semanticEntries.ToString(CultureInfo.InvariantCulture) + "," +
                   "\"episodicEntries\":" + episodicEntries.ToString(CultureInfo.InvariantCulture) + "," +
                   "\"proceduralEntries\":" + proceduralEntries.ToString(CultureInfo.InvariantCulture) + "}}";
        }

        private void HandleQueuedContext(object state)
        {
            HandleContext((HttpListenerContext)state);
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            try { _listener?.Stop(); } catch { }
            try { _listener?.Close(); } catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
