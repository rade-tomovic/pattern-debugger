// Evidence for /systems/il-jit-codegen/ — run with:
//   scripts/bench-lock.sh dotnet run bench/il-jit-codegen/index.cs -c Release
// and again with tiering switched off, for the second table:
//   scripts/bench-lock.sh env DOTNET_TieredCompilation=0 dotnet run bench/il-jit-codegen/index.cs -c Release
//
// What "warmed up" means, in milliseconds. Score() sums 256 doubles — deliberately
// under the 1000-iteration on-stack-replacement patchpoint, so a single call can
// never be promoted mid-flight and the method only speeds up when the RUNTIME
// decides to recompile it. We call it in batches and print the per-call cost of
// each batch, so the tier-0 → tier-1 handover is visible as a step in a column
// of numbers rather than as a claim.
using System.Diagnostics;
using System.Runtime.CompilerServices;

const int N = 256;
const int Batch = 20_000;
const int Batches = 24;

var data = new double[N];
for (int i = 0; i < N; i++) data[i] = i * 0.5;

[MethodImpl(MethodImplOptions.NoInlining)]
static double Score(double[] xs)
{
    double s = 0;
    for (int i = 0; i < xs.Length; i++) s += xs[i] * xs[i];
    return s;
}

double sink = 0;
var sw = Stopwatch.StartNew();

// The very first call: this is where the JIT actually compiles the method, so the
// compile time lands inside this measurement and nowhere else.
sw.Restart();
sink += Score(data);
double firstCallUs = sw.Elapsed.TotalMilliseconds * 1000;

var perCall = new double[Batches];
for (int b = 0; b < Batches; b++)
{
    sw.Restart();
    for (int k = 0; k < Batch; k++) sink += Score(data);
    perCall[b] = sw.Elapsed.TotalMilliseconds * 1e6 / Batch;   // ns per call
}

string tiering = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "(default: on)";
Console.WriteLine($"DOTNET_TieredCompilation = {tiering}");
Console.WriteLine($"first call to Score(): {firstCallUs:F1} us  (this is the JIT compiling it)");
Console.WriteLine($"\n{"batch",6}{"calls so far",14}{"ns per call",13}{"ns per element",16}");
for (int b = 0; b < Batches; b++)
    Console.WriteLine($"{b + 1,6}{(b + 1) * Batch,14:N0}{perCall[b],13:F0}{perCall[b] / N,16:F2}");

// Batch 1 also pays process startup, and the batch the promotion lands in is a
// mix of both code versions, so the two plateaus are read away from the edges:
// batches 2-5 are always still tier-0, the last six are always tier-1.
static double Median(double[] xs) { var c = (double[])xs.Clone(); Array.Sort(c); return c[c.Length / 2]; }
double tier0 = Median(perCall[1..5]);
double tier1 = Median(perCall[^6..]);
int promoted = Array.FindIndex(perCall, x => x < (tier0 + tier1) / 2) + 1;
Console.WriteLine($"\ntier-0 plateau (batches 2-5) {tier0:F0} ns/call");
Console.WriteLine($"tier-1 plateau (last 6)      {tier1:F0} ns/call");
Console.WriteLine($"speedup on promotion         {tier0 / tier1:F2}x, first seen in batch {promoted} "
                  + $"(~{promoted * Batch:N0} calls in)");
Console.WriteLine($"checksum {sink:F0} (ignore)");
