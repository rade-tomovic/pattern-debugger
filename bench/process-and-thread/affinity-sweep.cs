// Does thread placement explain why the spun-handoff row moves?
// Same Spun() body as context-switch-cost.cs, same trip count. Here it is run once for
// every possible pair of the four cores, plus once with placement left to the scheduler,
// so a placement effect would show up as one pair being consistently different.
using System.Diagnostics;
using System.Runtime.InteropServices;

const int Trips = 1_000_000;
const int Runs = 5;

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

static double Median(double[] xs) { var c = (double[])xs.Clone(); Array.Sort(c); return c[c.Length / 2]; }

// every unordered pair of the four cores, then the unpinned control
var pairs = new List<(int a, int b)>();
for (int i = 0; i < Environment.ProcessorCount; i++)
    for (int j = i + 1; j < Environment.ProcessorCount; j++)
        pairs.Add((i, j));
pairs.Add((-1, -1));

for (int w = 0; w < 2; w++) Spun(Trips, -1, -1);               // warmup

Console.WriteLine($"{Trips} trips per run, {Runs} timed runs per placement, {Environment.ProcessorCount} cores");
foreach (var (a, b) in pairs)
{
    var xs = new double[Runs];
    for (int r = 0; r < Runs; r++) xs[r] = Spun(Trips, a, b);
    string name = a < 0 ? "unpinned" : $"cores ({a},{b})";
    Console.WriteLine($"{name,-16}: median {Median(xs),6:F0} ns   min {xs.Min(),6:F0}   max {xs.Max(),6:F0}"
                      + $"   spread {(xs.Max() / xs.Min() - 1) * 100,3:F0}%   runs [{string.Join(", ", xs.Select(x => x.ToString("F0")))}]");
}

// sched_setaffinity(0, ...) applies to the *calling thread*: each side pins itself.
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
