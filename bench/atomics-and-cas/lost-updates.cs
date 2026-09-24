// Evidence for /systems/atomics-and-cas/lost-updates/ — run with:
//   dotnet run bench/atomics-and-cas/lost-updates.cs -c Release
//
//   1. the broken collector, run for real: how many increments vanish
//   2. the unit test that passes: how small the loop has to be to look correct
//   3. a scripted interleave: the three steps of one ++, forced to overlap
using System.Runtime.CompilerServices;

// ── the class as it would arrive in a pull request ───────────────────────────
sealed class RequestMetrics
{
    // volatile "so the other threads see it" — the belief this page is about
    private volatile int _total;
    private volatile int _errors;

    public void RecordSuccess() => _total++;
    public void RecordError() { _total++; _errors++; }

    public int Total => _total;
    public int Errors => _errors;
}

// ── the same class with the one-word fix ─────────────────────────────────────
sealed class AtomicRequestMetrics
{
    private int _total;
    private int _errors;

    public void RecordSuccess() => Interlocked.Increment(ref _total);
    public void RecordError() { Interlocked.Increment(ref _total); Interlocked.Increment(ref _errors); }

    public int Total => Volatile.Read(ref _total);
    public int Errors => Volatile.Read(ref _errors);
}

static class LostUpdates
{
    static void RunThreads(int threads, int perThread, Action<int> body)
    {
        var start = new ManualResetEventSlim(false);
        var ts = new Thread[threads];
        for (int t = 0; t < threads; t++)
        {
            ts[t] = new Thread(() => { start.Wait(); body(perThread); }) { IsBackground = true };
            ts[t].Start();
        }
        start.Set();
        foreach (var t in ts) t.Join();
    }

    // ── 3. the interleave, scripted ──────────────────────────────────────────
    // `total++` is load, add, store. Here those three steps are written out and
    // two threads are forced through them in the order the hardware sometimes
    // produces on its own, so the lost update is visible step by step.
    static int shared;
    static void ScriptedInterleave()
    {
        shared = 41;
        var aRead = new ManualResetEventSlim(false);
        var bStored = new ManualResetEventSlim(false);
        int regA = 0, regB = 0;

        var a = new Thread(() =>
        {
            regA = shared;                                     // step 1: load
            Console.WriteLine($"    step 1   A: load  -> regA={regA}                    shared={shared}");
            aRead.Set(); bStored.Wait();                       // let B run the whole ++
            regA = regA + 1;                                   // step 5: add, on the stale value
            Console.WriteLine($"    step 5   A: add   -> regA={regA}                    shared={shared}");
            shared = regA;                                     // step 6: store
            Console.WriteLine($"    step 6   A: store -> shared={shared}");
        });
        a.Start();
        aRead.Wait();
        regB = shared;                                         // step 2: load
        Console.WriteLine($"    step 2   B: load  -> regB={regB}                    shared={shared}");
        regB = regB + 1;                                       // step 3: add
        Console.WriteLine($"    step 3   B: add   -> regB={regB}                    shared={shared}");
        shared = regB;                                         // step 4: store
        Console.WriteLine($"    step 4   B: store -> shared={shared}");
        bStored.Set();
        a.Join();
        Console.WriteLine($"    two increments applied to 41, shared = {shared} (expected 43)");
    }

    public static void Main()
    {
        Console.WriteLine($"cores: {Environment.ProcessorCount}");

        // ── 1. the broken collector, at load ─────────────────────────────────
        Console.WriteLine("\n=== 1. 4 threads × 250,000 requests each (half of them errors) ===");
        for (int trial = 0; trial < 5; trial++)
        {
            var broken = new RequestMetrics();
            RunThreads(4, 250_000, n => { for (int i = 0; i < n; i++) { if ((i & 1) == 0) broken.RecordSuccess(); else broken.RecordError(); } });
            var fixedUp = new AtomicRequestMetrics();
            RunThreads(4, 250_000, n => { for (int i = 0; i < n; i++) { if ((i & 1) == 0) fixedUp.RecordSuccess(); else fixedUp.RecordError(); } });
            Console.WriteLine($"  trial {trial + 1}:  volatile int ++  Total={broken.Total,9:N0} ({1_000_000 - broken.Total,9:N0} lost)  Errors={broken.Errors,9:N0} (of 500,000)"
                            + $"   |  Interlocked  Total={fixedUp.Total,9:N0}  Errors={fixedUp.Errors,9:N0}");
        }

        // ── 2. how big does the loop have to be before the test fails? ───────
        Console.WriteLine("\n=== 2. 200 trials at each size: how many trials lost at least one update? ===");
        Console.WriteLine("      (2 threads, both calling RecordSuccess in a tight loop)");
        foreach (int per in new[] { 10, 100, 1_000, 10_000, 100_000, 1_000_000 })
        {
            int trials = per >= 100_000 ? 20 : 200;
            int bad = 0; long worst = 0;
            for (int t = 0; t < trials; t++)
            {
                var m = new RequestMetrics();
                RunThreads(2, per, n => { for (int i = 0; i < n; i++) m.RecordSuccess(); });
                long lost = 2L * per - m.Total;
                if (lost != 0) { bad++; worst = Math.Max(worst, lost); }
            }
            Console.WriteLine($"  {per,9:N0} increments per thread: {bad,4}/{trials} trials wrong, worst loss {worst,9:N0} ({worst * 100.0 / (2.0 * per),5:F1}%)");
        }

        // ── 3. the interleave ────────────────────────────────────────────────
        Console.WriteLine("\n=== 3. one lost update, scripted step by step ===");
        ScriptedInterleave();

        // ── 4. the fix, under the same pressure as section 1: exact totals ───
        Console.WriteLine("\n=== 4. the fix, same load as section 1 ===");
        var fixedMetrics = new AtomicRequestMetrics();
        RunThreads(4, 250_000, n => { for (int i = 0; i < n; i++) { if ((i & 1) == 0) fixedMetrics.RecordSuccess(); else fixedMetrics.RecordError(); } });
        Console.WriteLine($"  Interlocked  Total={fixedMetrics.Total,9:N0}  Errors={fixedMetrics.Errors,9:N0}");
    }
}
