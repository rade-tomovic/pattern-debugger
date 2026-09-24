// Evidence for /systems/lock-free-structures/treiber-stack/ — run with:
//   dotnet run bench/lock-free-structures/treiber-stack.cs

// ── the thing being built ────────────────────────────────────────────────────
// A Treiber stack: one mutable field, and every change to it goes through one
// CompareExchange. No lock, no wait queue, no parking.
public sealed class TreiberStack<T>
{
    sealed class Node(T value) { public readonly T Value = value; public Node? Next; }

    Node? _head;

    public void Push(T value)
    {
        var node = new Node(value);
        Node? head = Volatile.Read(ref _head);
        while (true)
        {
            node.Next = head;                                   // wire it up BEFORE publishing
            Node? seen = Interlocked.CompareExchange(ref _head, node, head);
            if (seen == head) return;                            // won: the node is now the head
            head = seen;                                         // lost: retry from what is really there
        }
    }

    public bool TryPop(out T value)
    {
        Node? head = Volatile.Read(ref _head);
        while (head is not null)
        {
            Node? seen = Interlocked.CompareExchange(ref _head, head.Next, head);
            if (seen == head) { value = head.Value; return true; }
            head = seen;
        }
        value = default!;                                        // empty
        return false;
    }
}

public static class Bench
{
    // ── 1. correctness under real contention ────────────────────────────────
    // Every thread pushes a disjoint block of integers, then four more threads
    // pop until empty. A lost update shows up as a missing or duplicated integer.
    static void VerifyUnderContention()
    {
        const int Threads = 4, PerThread = 200_000;
        var stack = new TreiberStack<int>();
        var producers = new Thread[Threads];
        var start = new Barrier(Threads);
        for (int t = 0; t < Threads; t++)
        {
            int id = t;
            producers[t] = new Thread(() =>
            {
                start.SignalAndWait();
                for (int i = 0; i < PerThread; i++) stack.Push(id * PerThread + i);
            });
            producers[t].Start();
        }
        foreach (var th in producers) th.Join();

        var seen = new int[Threads * PerThread];
        int popped = 0;
        var consumers = new Thread[Threads];
        var start2 = new Barrier(Threads);
        for (int t = 0; t < Threads; t++)
        {
            consumers[t] = new Thread(() =>
            {
                start2.SignalAndWait();
                while (stack.TryPop(out int v)) { Interlocked.Increment(ref seen[v]); Interlocked.Increment(ref popped); }
            });
            consumers[t].Start();
        }
        foreach (var th in consumers) th.Join();

        int missing = 0, duplicated = 0;
        foreach (int c in seen) { if (c == 0) missing++; else if (c > 1) duplicated++; }
        Console.WriteLine($"  pushed {Threads * PerThread:N0} by {Threads} threads, popped {popped:N0} by {Threads} threads");
        Console.WriteLine($"  missing {missing}, duplicated {duplicated}, stack empty: {!stack.TryPop(out _)}");
        if (missing != 0 || duplicated != 0 || popped != Threads * PerThread) throw new Exception("FAIL: the stack lost or duplicated items");
    }

    // ── 2. how often does the CAS actually go round? ─────────────────────────
    // Every attempt — whether it succeeds or not — increments a counter before
    // trying the CAS. attempts / successful-operations is how many times a
    // thread had to retry on average. The counter is itself a contended
    // Interlocked.Increment, so it widens the very window it is measuring —
    // read these as an upper bound on the retry rate, not an exact one.
    sealed class CountingStack
    {
        sealed class Node(int value) { public readonly int Value = value; public Node? Next; }
        Node? _head;
        public long Attempts;
        public void Push(int value)
        {
            var node = new Node(value);
            Node? head = Volatile.Read(ref _head);
            while (true)
            {
                node.Next = head;
                Interlocked.Increment(ref Attempts);
                Node? seen = Interlocked.CompareExchange(ref _head, node, head);
                if (seen == head) return;
                head = seen;
            }
        }
        public bool TryPop(out int value)
        {
            Node? head = Volatile.Read(ref _head);
            while (head is not null)
            {
                Interlocked.Increment(ref Attempts);
                Node? seen = Interlocked.CompareExchange(ref _head, head.Next, head);
                if (seen == head) { value = head.Value; return true; }
                head = seen;
            }
            value = 0; return false;
        }
    }

    static double AttemptsPerOp(int threads)
    {
        var s = new CountingStack();
        int per = 200_000 / threads;
        var ths = new Thread[threads];
        var start = new Barrier(threads);
        for (int t = 0; t < threads; t++)
        {
            ths[t] = new Thread(() =>
            {
                start.SignalAndWait();
                for (int i = 0; i < per; i++) { s.Push(i); s.TryPop(out _); }
            });
            ths[t].Start();
        }
        foreach (var th in ths) th.Join();
        return s.Attempts / (double)(per * threads * 2);
    }

    public static void Main()
    {
        Console.WriteLine("=== 1. correctness under contention ===");
        VerifyUnderContention();

        Console.WriteLine("\n=== 2. attempts per successful operation, three trials at each thread count ===");
        foreach (int t in new[] { 1, 2, 4 })
            Console.WriteLine($"  {t} thread(s): {AttemptsPerOp(t):F3}  {AttemptsPerOp(t):F3}  {AttemptsPerOp(t):F3}");

        Console.WriteLine("\nPASS");
    }
}
