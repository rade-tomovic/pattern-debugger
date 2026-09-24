// Evidence for /systems/memory-model/the-reordering-test/ — run with:
//   scripts/bench-lock.sh dotnet run bench/memory-model/the-reordering-test.cs -c Release
//
// The claim handshake below is Dekker's opening move. Every sequential interleaving
// of the four lines elects at most one leader. The hardware elects two.
using System.Diagnostics;
using System.Runtime.CompilerServices;

static class Claim
{
    // ── the shared state, in each of the spellings under test ───────────────
    static bool aWants, bWants;                       // the reviewed version
    static volatile bool va, vb;                      // "just add volatile"
    static int claimed;                               // the CompareExchange version
    static readonly object gate = new();

    static int aRan, bRan;                            // who entered the critical section

    // ── the four bodies. NoInlining so the emitted code can be disassembled ──
    [MethodImpl(MethodImplOptions.NoInlining)] static void A_Broken()  { aWants = true;  if (!bWants) aRan = 1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void B_Broken()  { bWants = true;  if (!aWants) bRan = 1; }

    [MethodImpl(MethodImplOptions.NoInlining)] static void A_Volatile(){ va = true;      if (!vb) aRan = 1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void B_Volatile(){ vb = true;      if (!va) bRan = 1; }

    [MethodImpl(MethodImplOptions.NoInlining)] static void A_VolCls()  { Volatile.Write(ref aWants, true); if (!Volatile.Read(ref bWants)) aRan = 1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void B_VolCls()  { Volatile.Write(ref bWants, true); if (!Volatile.Read(ref aWants)) bRan = 1; }

    [MethodImpl(MethodImplOptions.NoInlining)] static void A_Fenced()  { aWants = true; Interlocked.MemoryBarrier(); if (!bWants) aRan = 1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void B_Fenced()  { bWants = true; Interlocked.MemoryBarrier(); if (!aWants) bRan = 1; }

    [MethodImpl(MethodImplOptions.NoInlining)] static void A_Cas()     { if (Interlocked.CompareExchange(ref claimed, 1, 0) == 0) aRan = 1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void B_Cas()     { if (Interlocked.CompareExchange(ref claimed, 1, 0) == 0) bRan = 1; }

    [MethodImpl(MethodImplOptions.NoInlining)] static void A_Lock()    { lock (gate) { aWants = true; if (!bWants) aRan = 1; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static void B_Lock()    { lock (gate) { bWants = true; if (!aWants) bRan = 1; } }

    /// One trial: reset the shared state, release both threads into the window at
    /// the same instant, wait for both, then look at who ran. The Barrier is what
    /// makes the race reproducible — without it the two threads drift apart and
    /// the window in which the anomaly is possible almost never lines up.
    static (long both, long neither, long one, long ms) Run(string name, int trials, Action a, Action b)
    {
        long both = 0, neither = 0, one = 0; long firstDouble = -1;
        var bar = new Barrier(3);
        var t1 = new Thread(() => { for (int i = 0; i < trials; i++) { bar.SignalAndWait(); a(); bar.SignalAndWait(); } });
        var t2 = new Thread(() => { for (int i = 0; i < trials; i++) { bar.SignalAndWait(); b(); bar.SignalAndWait(); } });
        t1.Start(); t2.Start();
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < trials; i++)
        {
            aWants = false; bWants = false; va = false; vb = false; claimed = 0; aRan = 0; bRan = 0;
            bar.SignalAndWait();
            bar.SignalAndWait();
            int ran = aRan + bRan;
            if (ran == 2) { both++; if (firstDouble < 0) firstDouble = i; }
            else if (ran == 0) neither++;
            else one++;
        }
        t1.Join(); t2.Join();
        Console.WriteLine($"  {name,-42} both:{both,8:N0}  exactly one:{one,9:N0}  neither:{neither,9:N0}   first double at trial {firstDouble}");
        return (both, neither, one, sw.ElapsedMilliseconds);
    }

    /// The test an engineer actually writes: spin up two threads, join them, check.
    /// Thread creation costs tens of microseconds, so the two bodies almost never
    /// execute inside the same nanosecond-wide window and the bug does not appear.
    static long NaiveTest(int trials)
    {
        long both = 0;
        for (int i = 0; i < trials; i++)
        {
            aWants = false; bWants = false; aRan = 0; bRan = 0;
            var t1 = new Thread(A_Broken); var t2 = new Thread(B_Broken);
            t1.Start(); t2.Start(); t1.Join(); t2.Join();
            if (aRan + bRan == 2) both++;
        }
        return both;
    }

    public static void Main()
    {
        Console.WriteLine($"JIT optimiser disabled: {typeof(Claim).Assembly.GetCustomAttributes(typeof(DebuggableAttribute), false) is [DebuggableAttribute d] && d.IsJITOptimizerDisabled}");
        const int Trials = 500_000;

        const int NaiveTrials = 20_000;
        long naive = NaiveTest(NaiveTrials);
        Console.WriteLine($"\n=== the test you would have written: fresh threads per trial ===");
        Console.WriteLine($"  two threads started and joined per trial      both:{naive,8:N0} / {NaiveTrials:N0}");

        Console.WriteLine($"\n=== the reviewed version, {Trials:N0} trials ===");
        var broken = Run("plain bool fields", Trials, A_Broken, B_Broken);
        if (broken.both != 0)
            Console.WriteLine($"  FAIL: two leaders elected in {broken.both:N0} of {Trials:N0} trials " +
                              $"({broken.both * 100.0 / Trials:F3}%) — the rebuild ran twice.");
        else
            Console.WriteLine("  PASS: no double election on this run.");

        Console.WriteLine("\n=== the fixes people reach for first ===");
        Run("volatile bool fields", Trials, A_Volatile, B_Volatile);
        Run("Volatile.Write / Volatile.Read", Trials, A_VolCls, B_VolCls);

        Console.WriteLine("\n=== fixes that hold ===");
        Run("Interlocked.MemoryBarrier() after the write", Trials, A_Fenced, B_Fenced);
        Run("one word, Interlocked.CompareExchange", Trials, A_Cas, B_Cas);
        Run("lock (gate) around both lines", Trials, A_Lock, B_Lock);

        // Every row has printed; now fail the process for real, so the harness exits
        // nonzero rather than reporting a bug it swallowed.
        if (broken.both != 0)
            throw new Exception($"two leaders elected in {broken.both:N0} of {Trials:N0} trials — the reviewed code is broken.");
    }
}
