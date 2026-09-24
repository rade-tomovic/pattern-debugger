// Evidence for /systems/atomics-and-cas/cas-counter/ ("What Interlocked Actually Does") — run with:
//   dotnet run bench/atomics-and-cas/cas-counter.cs -c Release
//
//   1. six ways to add one to a shared counter, and how many locked
//      instructions each one executes per successful increment — a property
//      of the code, counted directly rather than inferred
//   2. a running maximum built from a CAS loop with an optimistic pre-check,
//      counting how often the loop skips the CAS entirely once the maximum
//      has mostly saturated
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

static class CasCounter
{
    static int counter;
    static long max;
    static readonly object gate = new();

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    struct PaddedCounter { [FieldOffset(0)] public long Value; }
    static readonly PaddedCounter[] slots = new PaddedCounter[16];

    // ── the six ways to add one ──────────────────────────────────────────────
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Plain() => counter++;                                    // 0 locked instructions — and wrong

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Atomic() => Interlocked.Increment(ref counter);          // exactly 1: lock xadd

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Cas()
    {
        int old;                                                        // >= 1: lock cmpxchg, once per attempt
        do { old = Volatile.Read(ref counter); }
        while (Interlocked.CompareExchange(ref counter, old + 1, old) != old);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Locked() { lock (gate) counter++; }                      // >= 1: the monitor's own fast-path CAS

    const int Batch = 64;
    static int local;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Batched()                                                // 1 per 64 calls: amortised
    {
        if (++local == Batch) { Interlocked.Add(ref counter, local); local = 0; }
    }
    static void BatchedFlush() { if (local != 0) { Interlocked.Add(ref counter, local); local = 0; } }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Partitioned(int id) => slots[id].Value++;                // 0 — no other core ever wants this line

    // ── a running maximum: the operation Interlocked has no method for ──────
    // MaxCas only calls CompareExchange when the candidate can possibly win;
    // MaxLocked takes the lock unconditionally to check. `attempts` and
    // `skips` count which branch actually ran — a fact about the code path
    // taken, not about how long it took.
    static long attempts, skips;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void MaxCas(long candidate)
    {
        long seen = Volatile.Read(ref max);
        if (candidate <= seen) { Interlocked.Increment(ref skips); return; }   // no CAS at all
        while (candidate > seen)
        {
            Interlocked.Increment(ref attempts);
            long prev = Interlocked.CompareExchange(ref max, candidate, seen);
            if (prev == seen) break;
            seen = prev;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void MaxLocked(long candidate)
    {
        lock (gate) { if (candidate > max) max = candidate; }             // always takes the lock
    }

    public static void Main()
    {
        // ── sanity: every variant actually reaches the right total ──────────
        counter = 0;
        for (int i = 0; i < 1000; i++) Atomic();
        for (int i = 0; i < 1000; i++) Cas();
        for (int i = 0; i < 1000; i++) Locked();
        for (int i = 0; i < 1000; i++) Batched();
        BatchedFlush();
        Console.WriteLine($"counter after 4000 correct increments: {counter} (expect 4000)");

        // ── the running maximum, 4,000,000 draws from the same stream ───────
        Console.WriteLine("\n=== running maximum: how often does the CAS loop skip the CAS? ===");
        var rng = new Random(7919);
        max = 0; attempts = 0; skips = 0;
        const int N = 4_000_000;
        for (int i = 0; i < N; i++) MaxCas(rng.Next(1, 1_000_000));
        Console.WriteLine($"  {N:N0} draws, range [1, 1,000,000)");
        Console.WriteLine($"  CAS attempted: {attempts,10:N0} ({attempts * 100.0 / N,5:F2}%)");
        Console.WriteLine($"  CAS skipped:   {skips,10:N0} ({skips * 100.0 / N,5:F2}%)");
        Console.WriteLine($"  final max: {max}");
        Console.WriteLine("  MaxLocked has no skip path — it takes the lock on every one of the");
        Console.WriteLine("  same 4,000,000 calls, whether or not the candidate could win.");
    }
}
