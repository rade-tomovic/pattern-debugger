// Evidence for /systems/parallelism-patterns/scaling-curve/ — run with:
//   dotnet run bench/parallelism-patterns/scaling-curve.cs -c Release
// Four ways for four threads to add up to the same total. Correctness is checked by
// assertion, not eyeballed. The addresses printed at the end are the real layout the
// CLR chose for THIS run — they move between runs, but which cache line each one lands
// in relative to its neighbours does not.
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

static class Scaling
{
    const int PerThread = 250_000;
    const int Threads = 4;

    static long shared;
    static readonly long[] tight = new long[4];      // 4 per-thread counters, packed adjacent
    static readonly long[] padded = new long[4 * 8]; // 4 per-thread counters, 64 bytes apart

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SharedAtomic(int n) { for (int i = 0; i < n; i++) Interlocked.Increment(ref shared); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TightSlot(int n, int id) { for (int i = 0; i < n; i++) Interlocked.Increment(ref tight[id]); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PaddedSlot(int n, int id) { for (int i = 0; i < n; i++) Interlocked.Increment(ref padded[id * 8]); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void LocalThenPublish(int n)
    {
        long c = 0;
        for (int i = 0; i < n; i++) c++;          // the loop: a register, nothing else
        Interlocked.Add(ref shared, c);           // ONE atomic for the whole thread's work
    }

    static void RunOn(Action<int> body)
    {
        var ts = new Thread[Threads];
        for (int t = 0; t < Threads; t++) { int id = t; ts[t] = new Thread(() => body(id)); ts[t].Start(); }
        foreach (var th in ts) th.Join();
    }

    public static void Main()
    {
        long expect = (long)PerThread * Threads;

        shared = 0;
        RunOn(_ => SharedAtomic(PerThread));
        if (shared != expect) throw new Exception($"FAIL shared: {shared} != {expect}");

        Array.Clear(tight);
        RunOn(id => TightSlot(PerThread, id));
        long tightTotal = tight.Sum();
        if (tightTotal != expect) throw new Exception($"FAIL tight: {tightTotal} != {expect}");

        Array.Clear(padded);
        RunOn(id => PaddedSlot(PerThread, id));
        long paddedTotal = 0;
        for (int i = 0; i < 4; i++) paddedTotal += padded[i * 8];
        if (paddedTotal != expect) throw new Exception($"FAIL padded: {paddedTotal} != {expect}");

        shared = 0;
        RunOn(_ => LocalThenPublish(PerThread));
        if (shared != expect) throw new Exception($"FAIL local-then-publish: {shared} != {expect}");

        // Pin both arrays and print where they actually landed this run.
        var h1 = GCHandle.Alloc(tight, GCHandleType.Pinned);
        var h2 = GCHandle.Alloc(padded, GCHandleType.Pinned);
        long b1 = h1.AddrOfPinnedObject().ToInt64(), b2 = h2.AddrOfPinnedObject().ToInt64();
        for (int i = 0; i < 4; i++) Console.WriteLine($"tight[{i}]      0x{b1 + i * 8:X}  line {(b1 + i * 8) / 64}");
        for (int i = 0; i < 4; i++) Console.WriteLine($"padded[{i * 8,2}]  0x{b2 + i * 64:X}  line {(b2 + i * 64) / 64}");
        h1.Free();
        h2.Free();

        Console.WriteLine("PASS all four totals correct");
    }
}
