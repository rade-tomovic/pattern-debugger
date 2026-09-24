// Evidence for /systems/memory-model/ — does it matter WHICH two hardware threads
// run the store-buffer litmus? Run with:
//   scripts/bench-lock.sh env DOTNET_TieredCompilation=0 \
//     dotnet run bench/memory-model/smt-siblings.cs -c Release
//
// This box is an 8-core / 16-thread Ryzen. cpu0 and cpu8 are the two hardware
// threads of ONE physical core (SMT siblings); cpu2 and cpu4 are two different
// physical cores. The store buffer is a per-physical-core structure, so the two
// placements are not the same experiment — and the numbers say so.
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

static class Smt
{
    // sched_setaffinity(0, sizeof(mask), &mask) — pin the CALLING OS thread to one cpu.
    [DllImport("libc", SetLastError = true)]
    static extern int sched_setaffinity(int pid, nuint cpusetsize, ref ulong mask);

    static void PinSelf(int cpu)
    {
        ulong mask = 1UL << cpu;
        if (sched_setaffinity(0, sizeof(ulong), ref mask) != 0)
            throw new Exception($"sched_setaffinity(cpu {cpu}) failed: {Marshal.GetLastWin32Error()}");
    }

    static int x, y, r1, r2;

    [MethodImpl(MethodImplOptions.NoInlining)] static void SbA() { x = 1; r1 = y; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void SbB() { y = 1; r2 = x; }

    /// The same litmus as bench/memory-model/index.cs, with each of the three
    /// participants pinned to a named hardware thread.
    static long Litmus(string name, int iters, int cpuA, int cpuB, int cpuRef)
    {
        long hits = 0;
        var bar = new Barrier(3);
        var t1 = new Thread(() => { PinSelf(cpuA); for (int i = 0; i < iters; i++) { bar.SignalAndWait(); SbA(); bar.SignalAndWait(); } });
        var t2 = new Thread(() => { PinSelf(cpuB); for (int i = 0; i < iters; i++) { bar.SignalAndWait(); SbB(); bar.SignalAndWait(); } });
        var refr = new Thread(() =>
        {
            PinSelf(cpuRef);
            for (int i = 0; i < iters; i++)
            {
                x = 0; y = 0; r1 = -1; r2 = -1;
                bar.SignalAndWait();
                bar.SignalAndWait();
                if (r1 == 0 && r2 == 0) hits++;
            }
        });
        t1.Start(); t2.Start(); refr.Start();
        t1.Join(); t2.Join(); refr.Join();
        Console.WriteLine($"  {name,-52} {hits,9:N0} / {iters:N0}   ({hits * 100.0 / iters,7:F4}%)");
        return hits;
    }

    public static void Main()
    {
        const int Trials = 500_000;
        Console.WriteLine("=== store-buffer litmus, workers pinned. anomaly = neither thread saw the other's store ===");
        for (int rep = 0; rep < 3; rep++)
        {
            Console.WriteLine($"-- repetition {rep + 1}");
            Litmus("two different physical cores (cpu 2, cpu 4)", Trials, 2, 4, 6);
            Litmus("two SMT siblings of ONE physical core (cpu 2, cpu 10)", Trials, 2, 10, 6);
        }
    }
}
