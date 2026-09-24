// Evidence for /systems/il-jit-codegen/bounds-check-elimination/ — run with:
//   scripts/bench-lock.sh dotnet run bench/il-jit-codegen/bounds-check-elimination.cs -c Release
//
// Four loops that read the same 512 ints and produce the same total. They differ
// only in what the JIT can PROVE about the index:
//   ByLength  — i < a.Length          → provably in range, no check emitted
//   ByCount   — i < n (a parameter)   → not provable, so the JIT clones the loop
//   Gather    — a[idx[i]]             → index comes from memory: one check per element
//   GatherRaw — Unsafe.Add(ref r, …)  → identical memory traffic, check removed by hand
// ByLength vs ByCount prices "the JIT could not prove it".
// Gather vs GatherRaw prices one surviving bounds check, in isolation: the two
// loop bodies differ by exactly `cmp r8d, edx` + `jae` and nothing else.
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

const int N = 512;            // 2 KiB — L1-resident, so this is not a cache measurement
const int Reps = 200_000;     // 102,400,000 element reads per timed run
const int Runs = 7;
const double GHz = 3.27;      // re-measured for this page — bench/cpu-execution/clock-rate.c

var a = new int[N];
var idx = new int[N];
for (int i = 0; i < N; i++) { a[i] = i; idx[i] = i; }   // idx[i] == i: same access order as the direct loops

[MethodImpl(MethodImplOptions.NoInlining)]
static int ByLength(int[] a)
{
    int s = 0;
    for (int i = 0; i < a.Length; i++) s += a[i];
    return s;
}

[MethodImpl(MethodImplOptions.NoInlining)]
static int ByCount(int[] a, int n)
{
    int s = 0;
    for (int i = 0; i < n; i++) s += a[i];
    return s;
}

[MethodImpl(MethodImplOptions.NoInlining)]
static int Gather(int[] a, int[] idx)
{
    int s = 0;
    for (int i = 0; i < idx.Length; i++) s += a[idx[i]];
    return s;
}

[MethodImpl(MethodImplOptions.NoInlining)]
static int GatherRaw(int[] a, int[] idx)
{
    ref int r = ref MemoryMarshal.GetArrayDataReference(a);
    int s = 0;
    for (int i = 0; i < idx.Length; i++) s += Unsafe.Add(ref r, idx[i]);
    return s;
}

static long RunByLength(int[] a, int[] idx, int reps) { long t = 0; for (int k = 0; k < reps; k++) t += ByLength(a); return t; }
static long RunByCount(int[] a, int[] idx, int reps) { long t = 0; for (int k = 0; k < reps; k++) t += ByCount(a, a.Length); return t; }
static long RunGather(int[] a, int[] idx, int reps) { long t = 0; for (int k = 0; k < reps; k++) t += Gather(a, idx); return t; }
static long RunGatherRaw(int[] a, int[] idx, int reps) { long t = 0; for (int k = 0; k < reps; k++) t += GatherRaw(a, idx); return t; }

var variants = new (string Name, Func<int[], int[], int, long> Run, double[] Ms)[]
{
    ("i < a.Length",       RunByLength,  new double[Runs]),
    ("i < n (parameter)",  RunByCount,   new double[Runs]),
    ("a[idx[i]] checked",  RunGather,    new double[Runs]),
    ("a[idx[i]] raw ref",  RunGatherRaw, new double[Runs]),
};

long sink = 0;

// Warm up: every variant runs the full workload twice before anything is timed, which
// is far past both the 30-call promotion threshold and the 100 ms call-counting delay,
// so all four are executing tier-1 code by the time the Stopwatch starts.
for (int w = 0; w < 2; w++)
    foreach (var (_, run, _) in variants) sink += run(a, idx, Reps);

var sw = new Stopwatch();
for (int r = 0; r < Runs; r++)
    foreach (var (_, run, ms) in variants)
    {
        sw.Restart();
        sink += run(a, idx, Reps);
        ms[r] = sw.Elapsed.TotalMilliseconds;
    }

static double Median(double[] xs) { var c = (double[])xs.Clone(); Array.Sort(c); return c[c.Length / 2]; }

long reads = (long)N * Reps;
Console.WriteLine($"{N} ints, {Reps:N0} reps = {reads:N0} element reads per timed run, {Runs} timed runs");
Console.WriteLine($"checksums: {ByLength(a)} {ByCount(a, N)} {Gather(a, idx)} {GatherRaw(a, idx)}  (all four must match)");
Console.WriteLine($"\n{"loop",-20}{"median ms",11}{"ns/element",12}{"cycles",9}   all runs (ms)");
foreach (var (name, _, ms) in variants)
{
    double med = Median(ms);
    Console.WriteLine($"{name,-20}{med,11:F1}{med * 1e6 / reads,12:F3}{med * 1e6 / reads * GHz,9:F2}   "
                      + string.Join(" ", ms.Select(x => x.ToString("F0"))));
}

double provable = Median(variants[0].Ms), cloned = Median(variants[1].Ms);
double checkedGather = Median(variants[2].Ms), rawGather = Median(variants[3].Ms);
Console.WriteLine($"\nprovable vs cloned      : {cloned / provable:F3}x  (direction held every run: "
                  + $"{variants[0].Ms.Zip(variants[1].Ms).All(p => p.Second >= p.First)})");
Console.WriteLine($"checked vs raw gather   : {checkedGather / rawGather:F3}x  (direction held every run: "
                  + $"{variants[2].Ms.Zip(variants[3].Ms).All(p => p.First >= p.Second)})");
Console.WriteLine($"one bounds check costs  : {(checkedGather - rawGather) * 1e6 / reads:F3} ns = "
                  + $"{(checkedGather - rawGather) * 1e6 / reads * GHz:F3} cycles per element");
Console.WriteLine($"checksum {sink} (ignore)");
