// Evidence for /systems/threads-and-scheduling/ — run with:
//   scripts/bench-lock.sh dotnet run bench/threads-and-scheduling/queue-ordering.cs -c Release
//
// The pool has two kinds of queue: one global FIFO queue, and one local
// work-stealing queue per worker thread. Part 1 clamps the pool to a single
// worker so the ordering is observable at all; part 2 puts the workers back and
// watches one worker's local queue get raided by the others.
using System.Collections.Concurrent;
using System.Diagnostics;

static class Queues
{
    static List<int> Order(bool fromPoolThread, bool preferLocal)
    {
        var order = new List<int>();
        var done = new ManualResetEventSlim();
        void QueueFive()
        {
            for (int i = 0; i < 5; i++)
            {
                int n = i;
                ThreadPool.UnsafeQueueUserWorkItem<object?>(
                    _ => { lock (order) { order.Add(n); if (order.Count == 5) done.Set(); } }, null, preferLocal);
            }
        }
        if (fromPoolThread) ThreadPool.UnsafeQueueUserWorkItem<object?>(_ => QueueFive(), null, false);
        else { var t = new Thread(QueueFive); t.Start(); t.Join(); }
        done.Wait();
        return order;
    }

    static List<int> OrderViaTaskRun(bool fromPoolThread)
    {
        var order = new List<int>();
        var done = new ManualResetEventSlim();
        void QueueFive()
        {
            for (int i = 0; i < 5; i++)
            {
                int n = i;
                _ = Task.Run(() => { lock (order) { order.Add(n); if (order.Count == 5) done.Set(); } });
            }
        }
        if (fromPoolThread) ThreadPool.UnsafeQueueUserWorkItem<object?>(_ => QueueFive(), null, false);
        else { var t = new Thread(QueueFive); t.Start(); t.Join(); }
        done.Wait();
        return order;
    }

    public static void Main()
    {
        // one worker: with nobody to steal from us, the queue discipline is the only thing left
        ThreadPool.SetMinThreads(1, 1);
        ThreadPool.SetMaxThreads(1, 1);
        Console.WriteLine("=== 1. five work items, queued in the order 0 1 2 3 4, one worker thread ===");
        Console.WriteLine($"  queued from a plain Thread   (global queue) : {string.Join(" ", Order(false, false))}");
        Console.WriteLine($"  queued from a pool thread, preferLocal:false: {string.Join(" ", Order(true, false))}");
        Console.WriteLine($"  queued from a pool thread, preferLocal:true : {string.Join(" ", Order(true, true))}");
        Console.WriteLine($"  Task.Run from a plain Thread                : {string.Join(" ", OrderViaTaskRun(false))}");
        Console.WriteLine($"  Task.Run from a pool thread                 : {string.Join(" ", OrderViaTaskRun(true))}");

        // put the workers back and watch the local queue get raided
        ThreadPool.SetMaxThreads(32767, 1000);
        ThreadPool.SetMinThreads(Environment.ProcessorCount, 1);
        Console.WriteLine($"\n=== 2. one pool thread queues 8 items of 30 ms each; {Environment.ProcessorCount} workers available ===");
        var ran = new ConcurrentQueue<string>();
        var done = new CountdownEvent(8);
        ThreadPool.UnsafeQueueUserWorkItem<object?>(_ =>
        {
            ran.Enqueue($"queued by tid{Environment.CurrentManagedThreadId}");
            for (int i = 0; i < 8; i++)
            {
                int n = i;
                _ = Task.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds < 30) { }        // real work, so stealing has time to happen
                    ran.Enqueue($"item{n} on tid{Environment.CurrentManagedThreadId}");
                    done.Signal();
                });
            }
        }, null, false);
        done.Wait();
        Console.WriteLine("  " + string.Join("\n  ", ran));
        Console.WriteLine($"  distinct worker threads that ran an item: "
            + $"{ran.Where(s => s.StartsWith("item")).Select(s => s.Split("tid")[1]).Distinct().Count()}");
    }
}
