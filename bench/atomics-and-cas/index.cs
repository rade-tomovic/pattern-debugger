// Evidence for /systems/atomics-and-cas/ — run with:
//   dotnet run bench/atomics-and-cas/index.cs -c Release
//
// Three things, in one process:
//   1. what a plain ++ loses when four threads run it
//   2. how many times a CAS loop has to go round under contention, at 1/2/4 threads
//   3. a scripted ABA: a compare-and-swap that succeeds on a world that changed
using System.Runtime.CompilerServices;

static class Bench
{
    // ── shared state ─────────────────────────────────────────────────────────
    static int counter;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Plain(int n) { for (int i = 0; i < n; i++) counter++; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Atomic(int n) { for (int i = 0; i < n; i++) Interlocked.Increment(ref counter); }

    static void RunOn(int threads, int totalOps, Action<int> body)
    {
        int per = totalOps / threads;
        var start = new ManualResetEventSlim(false);
        var ts = new Thread[threads];
        for (int t = 0; t < threads; t++)
        {
            ts[t] = new Thread(() => { start.Wait(); body(per); }) { IsBackground = true };
            ts[t].Start();
        }
        start.Set();
        foreach (var t in ts) t.Join();
    }

    // ── how often does a CAS have to retry? ──────────────────────────────────
    static long retries;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CasCounted(int n)
    {
        long local = 0;
        for (int i = 0; i < n; i++)
        {
            int old;
            do { old = Volatile.Read(ref counter); local++; }
            while (Interlocked.CompareExchange(ref counter, old + 1, old) != old);
        }
        Interlocked.Add(ref retries, local - n);   // attempts beyond the first
    }

    // ── ABA, scripted ─────────────────────────────────────────────────────────
    // A stack of handles: `head` is an index, `next[i]` is the index under i.
    // Indices are exactly what a C++ lock-free stack does with raw addresses —
    // a value that can be reused for a different node.
    static int head = -1;
    static readonly int[] next = new int[8];
    static readonly string[] name = ["A", "B", "C"];

    static void Push(int i) { next[i] = head; head = i; }          // single-threaded setup
    static int PopUnsafe()                                          // the racy pop, no CAS loop
    {
        int h = head;
        if (h < 0) return -1;
        int under = next[h];                                        // ← the read the race lands between
        if (Interlocked.CompareExchange(ref head, under, h) != h) return -2;
        return h;
    }
    static string Dump()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = head, guard = 0; i >= 0 && guard < 8; i = next[i], guard++)
            sb.Append(sb.Length == 0 ? "" : " -> ").Append(name[i]);
        return sb.Length == 0 ? "(empty)" : sb.ToString();
    }

    static void AbaDemo()
    {
        head = -1; Push(0); Push(1); Push(2);                       // A, then B, then C on top
        Console.WriteLine($"  start                    stack: {Dump()}   head={name[head]}");

        var t1ReadIt = new ManualResetEventSlim(false);
        var t2Done = new ManualResetEventSlim(false);
        int popped = -9;

        var t1 = new Thread(() =>
        {
            int h = head;                                           // reads C
            int under = next[h];                                    // reads B
            t1ReadIt.Set(); t2Done.Wait();                          // ← the window, held open on purpose
            popped = Interlocked.CompareExchange(ref head, under, h) == h ? h : -2;
        });
        t1.Start();
        t1ReadIt.Wait();

        // thread 2, in the window: pop C, pop B, push C back.
        int c = PopUnsafe(); int b = PopUnsafe(); Push(c);
        Console.WriteLine($"  thread 2 popped {name[c]}, popped {name[b]}, pushed {name[c]} back");
        Console.WriteLine($"  before thread 2's CAS…    stack: {Dump()}   head={name[head]}");
        t2Done.Set(); t1.Join();

        Console.WriteLine($"  thread 1's CAS returned  popped={(popped >= 0 ? name[popped] : popped.ToString())}");
        Console.WriteLine($"  after                    stack: {Dump()}   head={name[head]}");
    }

    public static void Main()
    {
        Console.WriteLine($"cores: {Environment.ProcessorCount}");

        // 1. lost updates
        Console.WriteLine("\n=== 1. four threads, 1,000,000 increments each ===");
        for (int trial = 0; trial < 3; trial++)
        {
            counter = 0; RunOn(4, 4_000_000, Plain);
            int plain = counter;
            counter = 0; RunOn(4, 4_000_000, Atomic);
            int atomic = counter;
            Console.WriteLine($"  trial {trial + 1}: plain ++ = {plain,9:N0} ({4_000_000 - plain,9:N0} lost, {(4_000_000 - plain) * 100.0 / 4_000_000,5:F1}%)   Interlocked = {atomic,9:N0}");
        }

        // 2. CAS retries at 1, 2, 4 threads on one shared counter
        Console.WriteLine("\n=== 2. CAS attempts per successful increment ===");
        const int Ops = 400_000;
        foreach (int t in new[] { 1, 2, 4 })
        {
            counter = 0; retries = 0;
            RunOn(t, Ops, CasCounted);
            Console.WriteLine($"  {t} thread(s): {Ops:N0} increments, {retries,10:N0} retries — {(Ops + retries) / (double)Ops:F3} attempts per increment");
        }

        // 3. ABA
        Console.WriteLine("\n=== 3. ABA, scripted ===");
        AbaDemo();
    }
}
