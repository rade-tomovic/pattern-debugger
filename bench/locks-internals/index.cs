// Evidence for /systems/locks-internals/ — run with:
//   scripts/bench-lock.sh dotnet run bench/locks-internals/index.cs -c Release
// Three experiments: what an uncontended acquire costs and whether the object
// header's state changes it; what happens to a lock as the critical section
// grows; and what striping buys.
using System.Diagnostics;
using System.Runtime.CompilerServices;

static class Bench
{
    static long _count;
    static readonly object Plain = new();
    static readonly object Hashed = new();          // GetHashCode() is called on this one
    static readonly Lock NewLock = new();           // System.Threading.Lock, .NET 9+

    static double Median(double[] xs) { var c = (double[])xs.Clone(); Array.Sort(c); return c[c.Length / 2]; }
    static string All(double[] xs) => string.Join(" ", xs.Select(x => x.ToString("F1")));

    // ── A. the uncontended fast path ─────────────────────────────────────────
    const int Ops = 20_000_000, Runs = 7;

    [MethodImpl(MethodImplOptions.NoInlining)] static void NoLock(int n)   { for (int i = 0; i < n; i++) _count++; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void PlainObj(int n) { for (int i = 0; i < n; i++) lock (Plain) _count++; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void HashedObj(int n){ for (int i = 0; i < n; i++) lock (Hashed) _count++; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void LockType(int n) { for (int i = 0; i < n; i++) lock (NewLock) _count++; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void Atomic(int n)   { for (int i = 0; i < n; i++) Interlocked.Increment(ref _count); }

    static void PartA()
    {
        _ = Hashed.GetHashCode();                   // forces a sync block for this object
        var names = new[] { "count++ (no lock)", "lock (plain object)", "lock (hash code taken)", "lock (System.Threading.Lock)", "Interlocked.Increment" };
        var bodies = new Action<int>[] { NoLock, PlainObj, HashedObj, LockType, Atomic };
        var t = new double[bodies.Length][];
        for (int i = 0; i < bodies.Length; i++) { t[i] = new double[Runs]; bodies[i](Ops / 10); bodies[i](Ops / 10); }
        var sw = new Stopwatch();
        for (int r = 0; r < Runs; r++)
            for (int i = 0; i < bodies.Length; i++) { sw.Restart(); bodies[i](Ops); t[i][r] = sw.Elapsed.TotalMilliseconds; }

        Console.WriteLine($"=== A. one thread, {Ops:N0} increments, median of {Runs} interleaved runs ===");
        for (int i = 0; i < bodies.Length; i++)
            Console.WriteLine($"  {names[i],-30} {Median(t[i]),7:F1} ms   {Median(t[i]) * 1e6 / Ops,5:F1} ns/op   runs: {All(t[i])}");
    }

    // ── B. what happens as the critical section grows ────────────────────────
    // Four threads, a fixed amount of work inside the lock, measured as
    // throughput plus the two counters that say whether anyone had to block.
    // /proc/self/status counts the MAIN thread only. The threads that park are
    // the other ones, so sum every task in the process.
    static (long vol, long invol) Switches()
    {
        long v = 0, iv = 0;
        foreach (var dir in Directory.GetDirectories("/proc/self/task"))
            try
            {
                foreach (var line in File.ReadLines(Path.Combine(dir, "status")))
                {
                    if (line.StartsWith("voluntary_ctxt_switches:")) v += long.Parse(line.Split(':')[1].Trim());
                    else if (line.StartsWith("nonvoluntary_ctxt_switches:")) iv += long.Parse(line.Split(':')[1].Trim());
                }
            }
            catch (IOException) { }                 // a thread exited mid-scan; its row is gone
        return (v, iv);
    }

    static readonly object HoldGate = new();
    static long _sink;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Hold(int iterations, int spinInside)
    {
        for (int i = 0; i < iterations; i++)
            lock (HoldGate)
            {
                long s = 0;
                for (int k = 0; k < spinInside; k++) s += k;   // the critical section
                _sink += s;
            }
    }

    // The workers stay alive until `linger` is set: a thread that has exited is
    // gone from /proc/self/task and takes its context-switch counts with it.
    static double RunThreads(int threads, Action body, ManualResetEventSlim? linger = null)
    {
        var ts = new Thread[threads];
        var start = new ManualResetEventSlim(false);
        var done = new CountdownEvent(threads);
        for (int i = 0; i < threads; i++)
        {
            ts[i] = new Thread(() => { start.Wait(); body(); done.Signal(); linger?.Wait(); }) { IsBackground = true };
            ts[i].Start();
        }
        Thread.Sleep(20);
        var sw = Stopwatch.StartNew();
        start.Set();
        done.Wait();
        double ms = sw.Elapsed.TotalMilliseconds;
        if (linger is null) foreach (var th in ts) th.Join();
        else _lingering = ts;
        return ms;
    }

    // Release the lingering workers and wait for them to actually exit, so the
    // next Switches() reading is not perturbed by threads leaving mid-sample.
    static Thread[]? _lingering;
    static void ReleaseLingering(ManualResetEventSlim linger)
    {
        linger.Set();
        if (_lingering is not null) foreach (var th in _lingering) th.Join();
        _lingering = null;
    }

    // Nine passes per cell, not five: the middle rows of this table straddle the
    // spin budget and are the noisiest measurement on the page.
    const int BRuns = 9;

    static void PartB()
    {
        Console.WriteLine($"\n=== B. 4 threads, one lock, critical section grown (median of {BRuns}) ===");
        Console.WriteLine($"  {"adds inside lock",16} {"acquisitions",13} {"1 thread",9} {"4 threads",10} {"4t/1t",7} {"contend/1k",11} {"vol.sw/1k",10}");
        // (spin inside the lock, acquisitions per thread) — the iteration count
        // shrinks as the critical section grows so every row takes ~0.3 s.
        (int spin, int iter)[] rows = [(0, 200_000), (10, 200_000), (100, 100_000), (1_000, 20_000), (10_000, 4_000)];
        RunThreads(4, () => Hold(20_000, 10));                                    // warm
        foreach (var (spin, iter) in rows)
        {
            // the same total number of acquisitions on one thread: the serial
            // cost of the work plus an always-uncontended lock. Anything the
            // 4-thread run spends above this is contention, not work.
            var one = new double[BRuns]; var four = new double[BRuns];
            for (int r = 0; r < BRuns; r++)
            {
                one[r] = RunThreads(1, () => Hold(4 * iter, spin));
                four[r] = RunThreads(4, () => Hold(iter, spin));
            }
            var linger = new ManualResetEventSlim(false);
            long c0 = Monitor.LockContentionCount; var (v0, i0) = Switches();
            RunThreads(4, () => Hold(iter, spin), linger);
            long c1 = Monitor.LockContentionCount; var (v1, i1) = Switches();
            ReleaseLingering(linger);
            double m1 = Median(one), m4 = Median(four);
            double perK = 1000.0 / (4.0 * iter);
            Console.WriteLine($"  {spin,16:N0} {4 * iter,13:N0} {m1,9:F1} {m4,10:F1} {m4 / m1,7:F2} {(c1 - c0) * perK,11:F1} {(v1 - v0) * perK,10:F1}");
            Console.WriteLine($"       1t runs: {All(one)}   4t runs: {All(four)}");
        }
    }

    // ── C. one lock vs striped locks ─────────────────────────────────────────
    const int Slots = 64, StripeOps = 2_000_000;
    static readonly long[] Cells = new long[Slots];
    static readonly object Single = new();
    static readonly object[] Stripes = Enumerable.Range(0, Slots).Select(_ => new object()).ToArray();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void OneLock(int n, int seed)
    {
        uint x = (uint)seed | 1;
        for (int i = 0; i < n; i++) { x ^= x << 13; x ^= x >> 17; x ^= x << 5; lock (Single) Cells[x % Slots]++; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Striped(int n, int seed)
    {
        uint x = (uint)seed | 1;
        for (int i = 0; i < n; i++) { x ^= x << 13; x ^= x >> 17; x ^= x << 5; uint s = x % Slots; lock (Stripes[s]) Cells[s]++; }
    }

    static void PartC()
    {
        Console.WriteLine($"\n=== C. 4 threads, {StripeOps:N0} updates spread over {Slots} counters ===");
        const int Threads = 4;
        var t1 = new double[Runs]; var t2 = new double[Runs];
        for (int w = 0; w < 2; w++) { RunThreads(Threads, () => OneLock(StripeOps / Threads, 7)); RunThreads(Threads, () => Striped(StripeOps / Threads, 7)); }
        for (int r = 0; r < Runs; r++)
        {
            t1[r] = RunThreads(Threads, () => OneLock(StripeOps / Threads, r + 1));
            t2[r] = RunThreads(Threads, () => Striped(StripeOps / Threads, r + 1));
        }
        Console.WriteLine($"  one lock for all {Slots} counters   {Median(t1),7:F1} ms   runs: {All(t1)}");
        Console.WriteLine($"  one lock per counter ({Slots})      {Median(t2),7:F1} ms   runs: {All(t2)}");
        Console.WriteLine($"  striped is {Median(t1) / Median(t2):F2}x the throughput");
    }

    public static void Main()
    {
        PartA(); PartB(); PartC();
        Console.WriteLine($"\nchecksum {_count} {_sink} {Cells.Sum()}");
    }
}
