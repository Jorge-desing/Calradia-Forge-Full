using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
        NamedPipeServerStream active; Task task;
        public string Name { get; }="CalradiaForge-"+System.Diagnostics.Process.GetCurrentProcess().Id;
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
                        using(var reader=new StreamReader(pipe,new UTF8Encoding(false),false,4096,true))
                        using(var writer=new StreamWriter(pipe,new UTF8Encoding(false),4096,true) {AutoFlush=true})
                        while(pipe.IsConnected && !shutdown.IsCancellationRequested)
                        {
                            var text=await ReadBounded(reader); if(text==null) break;
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
        static async Task<string> ReadBounded(StreamReader r)
        {
            var line = await r.ReadLineAsync();
            if (line != null && line.Length > 65536) throw new InvalidDataException("Request too large");
            return line;
        }
        public void Process(Func<Request,CancellationToken,Response> execute)
        {
            // Process one queued request per frame without waiting for the client.
            if(!queue.TryDequeue(out var p)) return;
            try { if(p.Cancellation.IsCancellationRequested) {p.Result.TrySetResult(new Response{Id=p.Request.Id,Error="Cancelled before execution"});return;} p.Result.TrySetResult(execute(p.Request,p.Cancellation.Token)); }
            catch(Exception e){p.Result.TrySetResult(new Response{Id=p.Request.Id,Error=e.Message});}
            finally { p.Release(); }
        }
        public void Dispose() {shutdown.Cancel(); active?.Dispose();while(queue.TryDequeue(out var pending)){pending.Cancellation.Cancel();pending.Result.TrySetCanceled();pending.Release();}}
    }
}
