// Evidence for /systems/il-jit-codegen/benchmarks-that-lie/ — run with:
//   scripts/bench-lock.sh dotnet run bench/il-jit-codegen/fp-add-latency.cs -c Release
//
// The unrolled-loop result on that page rests on one claim: the plain loop is limited
// by the latency of its own accumulator, not by how many instructions it executes.
// This file measures that latency directly instead of inferring it from the loop.
//
//   * Dependent   — one accumulator, every add waits for the previous one. This is the
//                   latency of vaddsd.
//   * Independent — four accumulators, so four adds can be in flight. Four chains at
//                   this latency saturate at one add per cycle; this is NOT the core's
//                   peak FP-add throughput, which would need more chains to find.
//   * SumSquares  — the page's plain loop, over a short array and a long one, to show
//                   the per-element cost converging on the dependent-chain latency once
//                   the call boundaries stop mattering.
//
// Reported in ns so no assumed clock frequency enters the numbers.
using System.Diagnostics;
using System.Runtime.CompilerServices;

const int ChainReps = 20_000_000;   // x8 adds per iteration = 160,000,000 adds per timed run
const int Runs = 7;
const long ChainAdds = (long)ChainReps * 8;

[MethodImpl(MethodImplOptions.NoInlining)]
static double Dependent(double seed, double c, int reps)
{
    double s = seed;
    for (int i = 0; i < reps; i++)
    {
        s += c; s += c; s += c; s += c;
        s += c; s += c; s += c; s += c;
    }
    return s;
}

[MethodImpl(MethodImplOptions.NoInlining)]
static double Independent(double seed, double c, int reps)
{
    double a = seed, b = seed, d = seed, e = seed;
    for (int i = 0; i < reps; i++)
    {
        a += c; b += c; d += c; e += c;
        a += c; b += c; d += c; e += c;
    }
    return a + b + d + e;
}

// byte-for-byte the plain loop from benchmarks-that-lie.cs
[MethodImpl(MethodImplOptions.NoInlining)]
static double SumSquares(double[] xs)
{
    double s = 0;
    for (int i = 0; i < xs.Length; i++) s += xs[i] * xs[i];
    return s;
}

static double Median(double[] xs) { var t = (double[])xs.Clone(); Array.Sort(t); return t[t.Length / 2]; }

static double[] MakeArray(int n)
{
    var a = new double[n];
    var rng = new Random(1);
    for (int i = 0; i < n; i++) a[i] = rng.NextDouble();
    return a;
}

double sink = 0;
var sw = new Stopwatch();

// ── warm every method to tier 1 with the workload it is about to be timed on ──
var small = MakeArray(256);
var large = MakeArray(4096);
for (int w = 0; w < 2; w++)
{
    sink += Dependent(1, 1e-9, ChainReps);
    sink += Independent(1, 1e-9, ChainReps);
    for (int k = 0; k < 40_000; k++) { sink += SumSquares(small); sink += SumSquares(large); }
}

var dep = new double[Runs];
var ind = new double[Runs];
for (int r = 0; r < Runs; r++)
{
    sw.Restart(); sink += Dependent(1, 1e-9, ChainReps);   dep[r] = sw.Elapsed.TotalMilliseconds;
    sw.Restart(); sink += Independent(1, 1e-9, ChainReps); ind[r] = sw.Elapsed.TotalMilliseconds;
}

// same total element count for both array lengths: 8,192,000 elements per timed run
const long Elements = 8_192_000;
double[] SweepOne(double[] a)
{
    int reps = (int)(Elements / a.Length);
    var ms = new double[Runs];
    for (int r = 0; r < Runs; r++)
    {
        sw.Restart();
        double t = 0;
        for (int k = 0; k < reps; k++) t += SumSquares(a);
        ms[r] = sw.Elapsed.TotalMilliseconds;
        sink += t;
    }
    return ms;
}
var sm = SweepOne(small);
var lg = SweepOne(large);

Console.WriteLine($"chains: {ChainAdds:N0} vaddsd per timed run | loops: {Elements:N0} elements per timed run | {Runs} runs each");
Console.WriteLine($"\n{"what is timed",-38}{"median ms",11}{"ns/add",10}   all runs (ms)");
foreach (var (name, ms, n) in new (string, double[], long)[]
{
    ("dependent chain, 1 accumulator",   dep, ChainAdds),
    ("independent chains, 4 accumulators", ind, ChainAdds),
    ("SumSquares, 256-element array",    sm,  Elements),
    ("SumSquares, 4096-element array",   lg,  Elements),
})
{
    double med = Median(ms);
    Console.WriteLine($"{name,-38}{med,11:F1}{med * 1e6 / n,10:F3}   " + string.Join(" ", ms.Select(x => x.ToString("F0"))));
}

double dm = Median(dep), im = Median(ind);
Console.WriteLine($"\nindependent is {dm / im:F2}x the dependent chain (direction held every run: "
                  + $"{dep.Zip(ind).All(x => x.First > x.Second)})");
Console.WriteLine($"4096-element loop reaches {Median(lg) * 1e6 / Elements / (dm * 1e6 / ChainAdds) * 100:F0}% of the dependent-chain latency per element");
Console.WriteLine($"checksum {sink:F3} (ignore)");
