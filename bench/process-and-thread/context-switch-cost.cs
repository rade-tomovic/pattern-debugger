// One round trip = thread A hands work to thread B and gets an answer back.
// Four ways to do it: park in the kernel, spin in user space, spin with both threads
// pinned to the SAME core, or don't switch at all.
// Each mode gets its own trip count, because they are orders of magnitude apart: 50,000
// parked round trips already cost about two seconds, and 100 same-core ones cost about
// eight hundred milliseconds, while a million spun ones cost a fifth of a second.
using System.Diagnostics;
using System.Runtime.InteropServices;

const int ParkedTrips = 50_000;
const int SpunTrips = 1_000_000;
const int SameCoreTrips = 100;
const int SameThreadTrips = 1_000_000;

// A. blocking handoff — each side parks; the kernel picks the other thread
double Parked()
{
    var toB = new ManualResetEventSlim(false, spinCount: 0);   // 0 = never spin, park immediately
    var toA = new ManualResetEventSlim(false, spinCount: 0);
    var b = new Thread(() =>
    {
        for (int i = 0; i < ParkedTrips; i++) { toB.Wait(); toB.Reset(); toA.Set(); }
    }) { IsBackground = true };
    b.Start();

    var sw = Stopwatch.StartNew();
    for (int i = 0; i < ParkedTrips; i++) { toB.Set(); toA.Wait(); toA.Reset(); }
    sw.Stop();
    b.Join();
    return sw.Elapsed.TotalNanoseconds / ParkedTrips;
}

// B. spinning handoff — both threads stay on-CPU; the flag travels through the cache.
//    coreA/coreB are Linux CPU numbers, or -1 to leave placement to the scheduler.
int turn = 0;
double Spun(int trips, int coreA, int coreB)
{
    turn = 0;
    var b = new Thread(() =>
    {
        if (coreB >= 0) Affinity.PinSelf(coreB);
        for (int i = 0; i < trips; i++)
        {
            while (Volatile.Read(ref turn) == 0) { }
            Volatile.Write(ref turn, 0);
        }
    }) { IsBackground = true };
    b.Start();
    if (coreA >= 0) Affinity.PinSelf(coreA);

    var sw = Stopwatch.StartNew();
    for (int i = 0; i < trips; i++)
    {
        Volatile.Write(ref turn, 1);
        while (Volatile.Read(ref turn) == 1) { }
    }
    sw.Stop();
    b.Join();
    if (coreA >= 0) Affinity.UnpinSelf();
    return sw.Elapsed.TotalNanoseconds / trips;
}

// C. no handoff at all — the same two writes, one thread, nobody to wake
double SameThread()
{
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < SameThreadTrips; i++)
    {
        Volatile.Write(ref turn, 1);
        Volatile.Write(ref turn, 0);
    }
    sw.Stop();
    return sw.Elapsed.TotalNanoseconds / SameThreadTrips;
}

// ── harness: warm up in process, then 11 timed runs, report the median and the run spread ──
static double Median(double[] xs) { var c = (double[])xs.Clone(); Array.Sort(c); return c[c.Length / 2]; }
static string Line(string name, double[] xs, string fmt) =>
    $"{name,-30}: median {Median(xs).ToString(fmt),11}   min {xs.Min().ToString(fmt)}   max {xs.Max().ToString(fmt)}"
    + $"   spread {(xs.Max() / xs.Min() - 1) * 100:F0}%   runs [{string.Join(", ", xs.Select(x => x.ToString(fmt)))}]";

for (int i = 0; i < 2; i++)                                    // warmup
{ Parked(); Spun(SpunTrips, -1, -1); Spun(SameCoreTrips, 0, 0); SameThread(); }

var parked = new double[11]; var spun = new double[11]; var oneCore = new double[11]; var same = new double[11];
for (int r = 0; r < 11; r++)
{
    parked[r] = Parked();
    spun[r] = Spun(SpunTrips, -1, -1);
    oneCore[r] = Spun(SameCoreTrips, 0, 0);
    same[r] = SameThread();
}

Console.WriteLine($"trips per run — parked {ParkedTrips}, spun {SpunTrips}, one-core {SameCoreTrips}, same-thread {SameThreadTrips}");
Console.WriteLine(Line("parked (kernel) ns/trip", parked, "F0"));
Console.WriteLine(Line("spun, scheduler places ns/trip", spun, "F0"));
Console.WriteLine(Line("spun, both on core 0 ns/trip", oneCore, "F0"));
Console.WriteLine(Line("same thread     ns/trip", same, "F1"));
Console.WriteLine($"median ratios — parked/spun {Median(parked) / Median(spun):F0}x   spun/same {Median(spun) / Median(same):F0}x"
                  + $"   one-core/parked {Median(oneCore) / Median(parked):F0}x");

// sched_setaffinity(0, ...) applies to the *calling thread*, which is what we want here:
// each side of the ping-pong pins itself.
static class Affinity
{
    [DllImport("libc", SetLastError = true)]
    static extern int sched_setaffinity(int pid, IntPtr cpuSetSize, ref ulong mask);

    public static void PinSelf(int cpu)
    {
        ulong mask = 1UL << cpu;
        if (sched_setaffinity(0, (IntPtr)sizeof(ulong), ref mask) != 0)
            throw new InvalidOperationException($"could not pin to cpu {cpu}");
    }

    public static void UnpinSelf()
    {
        ulong mask = (1UL << Environment.ProcessorCount) - 1;
        if (sched_setaffinity(0, (IntPtr)sizeof(ulong), ref mask) != 0)
            throw new InvalidOperationException("could not restore affinity");
    }
}
