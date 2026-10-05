using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalradiaForge.Core;

namespace CalradiaForge.Mod
{
    /// <summary>Reads bounded UTF-8 lines without buffering an untrusted full line first.</summary>
    internal sealed class BoundedPipeLineReader
    {
        internal const int MaximumLineCharacters = 65536;
        internal static readonly TimeSpan DefaultIncompleteLineTimeout = TimeSpan.FromSeconds(15);

        readonly Stream stream;
        readonly int maximumLineCharacters;
        readonly byte[] bytes = new byte[4096];
        readonly char[] decoded = new char[4];
        readonly Decoder decoder = new UTF8Encoding(false).GetDecoder();
        int bufferedBytes;
        int nextByte;
        bool endOfStream;
        bool skipOptionalLineFeed;

        internal BoundedPipeLineReader(Stream stream, int maximumLineCharacters = MaximumLineCharacters)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            if (maximumLineCharacters < 1) throw new ArgumentOutOfRangeException(nameof(maximumLineCharacters));
            this.maximumLineCharacters = maximumLineCharacters;
        }

        internal async Task<string> ReadLineAsync(TimeSpan incompleteLineTimeout, CancellationToken cancellationToken)
        {
            if (incompleteLineTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(incompleteLineTimeout));
            var line = new StringBuilder(Math.Min(maximumLineCharacters, 256));
            var deadlineSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var deadline = Task.Delay(incompleteLineTimeout, deadlineSource.Token);

            try
            {
                while (true)
                {
                    if (nextByte >= bufferedBytes)
                    {
                        if (endOfStream)
                        {
                            FlushDecoder(line);
                            if (line.Length == 0) return null;
                            EnsureLineLength(line);
                            return line.ToString();
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        var read = stream.ReadAsync(bytes, 0, bytes.Length, cancellationToken);
                        if (await Task.WhenAny(read, deadline).ConfigureAwait(false) != read)
                        {
                            try { stream.Dispose(); } catch { /* Preserve the primary timeout/cancellation result if stream disposal also fails. */ }
                            ObservePendingRead(read);
                            cancellationToken.ThrowIfCancellationRequested();
                            throw new TimeoutException("The IPC request line did not finish within " +
                                incompleteLineTimeout.TotalSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " seconds.");
                        }

                        bufferedBytes = await read.ConfigureAwait(false);
                        nextByte = 0;
                        if (bufferedBytes == 0)
                        {
                            endOfStream = true;
                            continue;
                        }
                    }

                    int bytesUsed;
                    int charsUsed;
                    bool completed;
                    decoder.Convert(bytes, nextByte, 1, decoded, 0, decoded.Length, false,
                        out bytesUsed, out charsUsed, out completed);
                    if (bytesUsed == 0) throw new InvalidDataException("Unable to decode the IPC request stream.");
                    nextByte += bytesUsed;

                    for (var index = 0; index < charsUsed; index++)
                    {
                        var character = decoded[index];
                        if (skipOptionalLineFeed)
                        {
                            skipOptionalLineFeed = false;
                            if (character == '\n') continue;
                        }

                        if (character == '\r')
                        {
                            skipOptionalLineFeed = true;
                            EnsureLineLength(line);
                            return line.ToString();
                        }

                        if (character == '\n')
                        {
                            EnsureLineLength(line);
                            return line.ToString();
                        }

                        line.Append(character);
                        if (line.Length > maximumLineCharacters + 1 ||
                            (line.Length == maximumLineCharacters + 1 && character != '\r'))
                            throw new InvalidDataException("Request too large.");
                    }
                }
            }
            finally
            {
                deadlineSource.Cancel();
                deadlineSource.Dispose();
            }
        }

        void FlushDecoder(StringBuilder line)
        {
            int bytesUsed;
            int charsUsed;
            bool completed;
            decoder.Convert(bytes, 0, 0, decoded, 0, decoded.Length, true,
                out bytesUsed, out charsUsed, out completed);
            for (var index = 0; index < charsUsed; index++)
            {
                line.Append(decoded[index]);
                if (line.Length > maximumLineCharacters) throw new InvalidDataException("Request too large.");
            }
        }

        void EnsureLineLength(StringBuilder line)
        {
            if (line.Length > maximumLineCharacters) throw new InvalidDataException("Request too large.");
        }

        static void ObservePendingRead(Task<int> read)
        {
            read.ContinueWith(task => { var ignored = task.Exception; },
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    // The transport thread never accesses game objects.
    internal sealed class PendingRequest
    {
        public Request Request;
        public CancellationTokenSource Cancellation;
        public TaskCompletionSource<Response> Result=new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);
        int owners=2;
        public PendingRequest(CancellationToken shutdown){Cancellation=CancellationTokenSource.CreateLinkedTokenSource(shutdown);}
        // Transport and game-thread processing each release exactly one reference.
        public void Release(){if(Interlocked.Decrement(ref owners)==0)Cancellation.Dispose();}
    }
    internal sealed class PipeServer : IDisposable
    {
        readonly ConcurrentQueue<PendingRequest> queue=new ConcurrentQueue<PendingRequest>();
        readonly CancellationTokenSource shutdown=new CancellationTokenSource();
        readonly TimeSpan incompleteLineTimeout;
        NamedPipeServerStream active; Task task;
        public string Name { get; }="CalradiaForge-"+System.Diagnostics.Process.GetCurrentProcess().Id;
        public PipeServer(TimeSpan? incompleteLineTimeout = null)
        {
            this.incompleteLineTimeout = incompleteLineTimeout ?? BoundedPipeLineReader.DefaultIncompleteLineTimeout;
            if (this.incompleteLineTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(incompleteLineTimeout));
        }
        public void Start() { task=Task.Run(Listen); }
        async Task Listen()
        {
            while(!shutdown.IsCancellationRequested)
            {
                try
                {
                    var security=new PipeSecurity(); security.SetAccessRuleProtection(true,false);
                    security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,PipeAccessRights.FullControl,AccessControlType.Allow));
                    using(var pipe=new NamedPipeServerStream(Name,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous,65536,65536,security))
                    {
                        active=pipe; await pipe.WaitForConnectionAsync(shutdown.Token);
                        var reader = new BoundedPipeLineReader(pipe);
                        using(var writer=new StreamWriter(pipe,new UTF8Encoding(false),4096,true) {AutoFlush=true})
                        while(pipe.IsConnected && !shutdown.IsCancellationRequested)
                        {
                            var text=await reader.ReadLineAsync(incompleteLineTimeout, shutdown.Token); if(text==null) break;
                            var s=Json.Deserialize<Request>(text);
                            if(s==null)throw new InvalidDataException("Null request");
                            if(queue.Count>=32) { await writer.WriteLineAsync(Json.Serialize(new Response {Id=s.Id,Error="Queue full"})); continue; }
                            var p=new PendingRequest(shutdown.Token) {Request=s};
                            queue.Enqueue(p);
                            try
                            {
                                using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token))
                                {
                                    var completed=await Task.WhenAny(p.Result.Task,Task.Delay(15000,deadline.Token));
                                    if(completed!=p.Result.Task){p.Cancellation.Cancel();await writer.WriteLineAsync(Json.Serialize(new Response{Id=s.Id,Error="Timeout; operation cancellation requested"}));}
                                    else {deadline.Cancel();await writer.WriteLineAsync(Json.Serialize(await p.Result.Task));}
                                }
                            }
                            finally {p.Release();}
                        }
                    }
                }
                catch(Exception) when(!shutdown.IsCancellationRequested) { await Task.Delay(250); }
                catch(OperationCanceledException) { break; }
                catch(ObjectDisposedException) { break; }
                catch(IOException) when(shutdown.IsCancellationRequested) { break; }
            }
        }
        public void Process(Func<Request,CancellationToken,Response> execute)
        {
            if (execute == null) return;
            // Process one queued request per frame without waiting for the client.
            if(!queue.TryDequeue(out var p)) return;
            try { if(p.Cancellation.IsCancellationRequested) {p.Result.TrySetResult(new Response{Id=p.Request?.Id,Error="Cancelled before execution"});return;} p.Result.TrySetResult(execute(p.Request,p.Cancellation.Token)); }
            catch(Exception e){p.Result.TrySetResult(new Response{Id=p.Request?.Id,Error=e.Message});}
            finally { p.Release(); }
        }
        public void Dispose()
        {
            UnloadCleanupRunner.Run(new[]
            {
                new KeyValuePair<string, Action>("cancel IPC shutdown", shutdown.Cancel),
                new KeyValuePair<string, Action>("close active IPC pipe", () => active?.Dispose()),
                new KeyValuePair<string, Action>("drain pending IPC requests", () => DrainPendingRequests(queue, ReportDisposeFailure))
            }, ReportDisposeFailure);
        }

        internal static void DrainPendingRequests(ConcurrentQueue<PendingRequest> pendingRequests, Action<string, Exception> reportFailure)
        {
            if (pendingRequests == null) return;
            while (pendingRequests.TryDequeue(out var pending))
            {
                UnloadCleanupRunner.Run(new[]
                {
                    new KeyValuePair<string, Action>("cancel pending IPC request", pending.Cancellation.Cancel),
                    new KeyValuePair<string, Action>("complete pending IPC response", () => pending.Result.TrySetCanceled()),
                    new KeyValuePair<string, Action>("release pending IPC request", pending.Release)
                }, reportFailure);
            }
        }

        static void ReportDisposeFailure(string name, Exception error)
        {
            try
            {
                TaleWorlds.Library.Debug.Print("[CalradiaForge] IPC cleanup step '" + name + "' failed: " + error, 0, TaleWorlds.Library.Debug.DebugColor.Red);
            }
            catch { /* Diagnostics are best effort; disposal continues regardless of logging availability. */ }
        }
    }
}
