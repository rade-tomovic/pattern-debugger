// Evidence for /systems/il-jit-codegen/benchmarks-that-lie/ — run with:
//   scripts/bench-lock.sh env DOTNET_TieredCompilation=0 \
//     dotnet run bench/il-jit-codegen/dead-code.cs -c Release
// and, for the disassembly of the three timed loops:
//   DOTNET_TieredCompilation=0 DOTNET_JitDisasm=Main DOTNET_JitStdOutFile=/tmp/dce.txt \
//     dotnet run bench/il-jit-codegen/dead-code.cs -c Release
//
// The same 100,000,000 square roots, timed three ways. Tiering is off so every
// loop is compiled straight to fully optimised code — which is what a warmed-up
// benchmark is running anyway.
//   1. result discarded            → the optimiser is free to delete the work
//   2. result consumed, same input → the work is loop-invariant and can be hoisted
//   3. result consumed, new input  → the only one that measures a square root
using System;
using System.Diagnostics;

class D
{
    const int N = 100_000_000;

    static double Hypot(double a, double b) => Math.Sqrt(a * a + b * b);

    static void Main()
    {
        double x = 1.5, y = 2.5;
        var sw = new Stopwatch();

        sw.Restart();
        for (int i = 0; i < N; i++) Hypot(x, y);
        double discarded = sw.Elapsed.TotalMilliseconds;

        double sink = 0;
        sw.Restart();
        for (int i = 0; i < N; i++) sink += Hypot(x, y);
        double invariant = sw.Elapsed.TotalMilliseconds;

        double sink2 = 0;
        sw.Restart();
        for (int i = 0; i < N; i++) sink2 += Hypot(i, y);
        double real = sw.Elapsed.TotalMilliseconds;

        Console.WriteLine($"1. result discarded      {discarded,8:F1} ms  {discarded * 1e6 / N,6:F2} ns/call");
        Console.WriteLine($"2. consumed, same input  {invariant,8:F1} ms  {invariant * 1e6 / N,6:F2} ns/call");
        Console.WriteLine($"3. consumed, new input   {real,8:F1} ms  {real * 1e6 / N,6:F2} ns/call");
        Console.WriteLine($"checksum {sink} {sink2} (ignore)");
    }
}
