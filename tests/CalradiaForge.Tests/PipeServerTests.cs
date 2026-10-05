using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalradiaForge.Mod;

namespace CalradiaForge.Tests
{
    internal static class PipeServerTests
    {
        public static void Run(Action<string, Action> test)
        {
            test("IPC request reader accepts exactly 65,536 UTF-16 characters", ExactLimit);
            test("IPC request reader accepts an exact-limit CRLF line", ExactLimitCrLf);
            test("IPC request reader preserves CR, LF, and CRLF line framing", MixedLineTerminators);
            test("IPC request reader rejects over-limit content before CRLF", OverLimitCrLf);
            test("IPC request reader counts decoded UTF-16 characters", Utf16Limit);
            test("IPC request reader observes cancellation", Cancellation);
            test("IPC request reader times out and reports its configured deadline", IncompleteLineTimeout);
            test("IPC listener accepts the next client after an incomplete-line timeout", ListenerContinuesAfterTimeout);
            test("IPC disposal drains every request when a cancellation callback throws", DrainContinuesAfterCancellationFailure);
        }

        static void ExactLimit()
        {
            var content = new string('x', BoundedPipeLineReader.MaximumLineCharacters);
            Require(ReadLine(content + "\n") == content, "An exact-limit UTF-8 line should be accepted.");
        }

        static void ExactLimitCrLf()
        {
            var content = new string('x', BoundedPipeLineReader.MaximumLineCharacters);
            Require(ReadLine(content + "\r\n") == content, "CRLF must not count as request content beyond the configured limit.");
        }

        static void MixedLineTerminators()
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("first\rsecond\r\nthird\n")))
            {
                var reader = new BoundedPipeLineReader(stream);
                Require(reader.ReadLineAsync(TimeSpan.FromSeconds(2), CancellationToken.None).GetAwaiter().GetResult() == "first",
                    "A bare CR must terminate a request line as StreamReader.ReadLine did.");
                Require(reader.ReadLineAsync(TimeSpan.FromSeconds(2), CancellationToken.None).GetAwaiter().GetResult() == "second",
                    "A CR followed by LF must form one delimiter without creating an empty request.");
                Require(reader.ReadLineAsync(TimeSpan.FromSeconds(2), CancellationToken.None).GetAwaiter().GetResult() == "third",
                    "A following LF-delimited request must remain intact.");
                Require(reader.ReadLineAsync(TimeSpan.FromSeconds(2), CancellationToken.None).GetAwaiter().GetResult() == null,
                    "The reader must report EOF after all framed requests.");
            }
        }

        static void OverLimitCrLf()
        {
            var content = new string('x', BoundedPipeLineReader.MaximumLineCharacters + 1);
            Throws<InvalidDataException>(() => ReadLine(content + "\r\n"));
        }

        static void Utf16Limit()
        {
            var exact = string.Concat(EnumerableRepeat("\U0001F600", BoundedPipeLineReader.MaximumLineCharacters / 2));
            Require(exact.Length == BoundedPipeLineReader.MaximumLineCharacters, "The supplementary-character fixture must contain exactly the UTF-16 character limit.");
            Require(ReadLine(exact + "\n") == exact, "The reader must count decoded UTF-16 characters rather than UTF-8 bytes.");

            var over = exact + "\U0001F600";
            Throws<InvalidDataException>(() => ReadLine(over + "\r\n"));
        }

        static void Cancellation()
        {
            using (var stream = new BlockingReadStream())
            using (var cancellation = new CancellationTokenSource())
            {
                var reader = new BoundedPipeLineReader(stream);
                var pending = reader.ReadLineAsync(TimeSpan.FromSeconds(5), cancellation.Token);
                cancellation.CancelAfter(50);
                Throws<OperationCanceledException>(() => pending.GetAwaiter().GetResult());
            }
        }

        static void IncompleteLineTimeout()
        {
            var timeout = TimeSpan.FromMilliseconds(75);
            using (var stream = new BlockingReadStream())
            {
                var error = Throws<TimeoutException>(() => new BoundedPipeLineReader(stream).ReadLineAsync(timeout, CancellationToken.None).GetAwaiter().GetResult());
                Require(error.Message.Contains("0.075 seconds"), "Timeout diagnostics must identify the configured incomplete-line deadline.");
                Require(stream.IsDisposed, "A timed-out read must dispose the underlying stream to release the pending read.");
            }
        }

        static void ListenerContinuesAfterTimeout()
        {
            using (var server = new PipeServer(TimeSpan.FromMilliseconds(100)))
            {
                server.Start();
                using (var first = Connect(server.Name, TimeSpan.FromSeconds(4)))
                {
                    first.WriteByte((byte)'{');
                    first.Flush();
                    Thread.Sleep(500);
                }

                using (var second = Connect(server.Name, TimeSpan.FromSeconds(4)))
                {
                    Require(second.IsConnected, "The listener should accept a new client after timing out an incomplete line.");
                }
            }
        }

        static void DrainContinuesAfterCancellationFailure()
        {
            using (var shutdown = new CancellationTokenSource())
            {
                var pendingRequests = new System.Collections.Concurrent.ConcurrentQueue<PendingRequest>();
                var first = new PendingRequest(shutdown.Token);
                var second = new PendingRequest(shutdown.Token);
                var laterCallbackRan = false;
                first.Cancellation.Token.Register(() => throw new InvalidOperationException("synthetic cancellation callback failure"));
                second.Cancellation.Token.Register(() => laterCallbackRan = true);
                pendingRequests.Enqueue(first);
                pendingRequests.Enqueue(second);

                var errors = new List<string>();
                PipeServer.DrainPendingRequests(pendingRequests, (name, error) => errors.Add(name));

                Require(pendingRequests.IsEmpty, "A cancellation callback failure must not leave later requests queued.");
                Require(first.Result.Task.IsCanceled && second.Result.Task.IsCanceled,
                    "Each drained request must complete as canceled even when cancellation callbacks fail.");
                Require(laterCallbackRan, "Cancellation of one request must not prevent cancellation callbacks for later requests.");
                Require(errors.Contains("cancel pending IPC request"), "The failed per-request cancellation step must be reported.");

                // The transport thread owns the remaining reference after the queue is drained.
                first.Release();
                second.Release();
            }
        }

        static string ReadLine(string input)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(input)))
                return new BoundedPipeLineReader(stream).ReadLineAsync(TimeSpan.FromSeconds(2), CancellationToken.None).GetAwaiter().GetResult();
        }

        static string[] EnumerableRepeat(string value, int count)
        {
            var values = new string[count];
            for (var index = 0; index < count; index++) values[index] = value;
            return values;
        }

        static NamedPipeClientStream Connect(string name, TimeSpan timeout)
        {
            var timer = Stopwatch.StartNew();
            while (timer.Elapsed < timeout)
            {
                var client = new NamedPipeClientStream(".", name, PipeDirection.Out, PipeOptions.Asynchronous);
                try
                {
                    client.Connect(200);
                    return client;
                }
                catch (TimeoutException)
                {
                    client.Dispose();
                    Thread.Sleep(25);
                }
                catch (IOException)
                {
                    client.Dispose();
                    Thread.Sleep(25);
                }
            }
            throw new TimeoutException("The pipe server did not accept a client within " + timeout.TotalSeconds + " seconds.");
        }

        static T Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T error) { return error; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + " to be thrown.");
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        sealed class BlockingReadStream : Stream
        {
            readonly TaskCompletionSource<int> pendingRead = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration cancellationRegistration;
            volatile bool disposed;

            public bool IsDisposed => disposed;
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (cancellationToken.CanBeCanceled)
                    cancellationRegistration = cancellationToken.Register(() => pendingRead.TrySetCanceled());
                return pendingRead.Task;
            }

            protected override void Dispose(bool disposing)
            {
                disposed = true;
                cancellationRegistration.Dispose();
                pendingRead.TrySetResult(0);
                base.Dispose(disposing);
            }
        }
    }
}
