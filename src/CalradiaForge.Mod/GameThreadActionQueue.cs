using System;
using System.Collections.Concurrent;
using System.Threading;

namespace CalradiaForge.Mod
{
    /// <summary>
    /// Bounded queue for requests from extensions plus guaranteed delivery for trusted
    /// in-process completions. Count reservations avoid ConcurrentQueue.Count's O(n) scan.
    /// </summary>
    internal sealed class GameThreadActionQueue
    {
        readonly ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
        readonly int capacity;
        int pendingCount;

        public GameThreadActionQueue(int capacity)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            this.capacity = capacity;
        }

        public int PendingCount => Volatile.Read(ref pendingCount);

        public bool TryEnqueue(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            while (true)
            {
                var current = Volatile.Read(ref pendingCount);
                if (current >= capacity) return false;
                if (Interlocked.CompareExchange(ref pendingCount, current + 1, current) == current) break;
            }

            actions.Enqueue(action);
            return true;
        }

        // Reserved for host-owned work such as UI teardown and async operation completion.
        // These must not disappear merely because extension requests filled the bounded queue.
        public void Enqueue(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            Interlocked.Increment(ref pendingCount);
            actions.Enqueue(action);
        }

        public bool TryDequeue(out Action action)
        {
            if (!actions.TryDequeue(out action)) return false;
            Interlocked.Decrement(ref pendingCount);
            return true;
        }
    }

    internal static class GameThreadActionDispatch
    {
        public static void RunOrPost(bool isGameThread, Action<Action> postToGameThread, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (isGameThread)
            {
                action();
                return;
            }

            if (postToGameThread == null) throw new ArgumentNullException(nameof(postToGameThread));
            postToGameThread(action);
        }
    }
}
