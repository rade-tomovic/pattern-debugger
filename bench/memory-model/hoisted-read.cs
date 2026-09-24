// Evidence for /systems/memory-model/ — the visibility half of the story:
// a plain field read out of a spin loop by the JIT, and the same loop with volatile.
//   scripts/bench-lock.sh dotnet run bench/memory-model/hoisted-read.cs -c Release
//   scripts/bench-lock.sh dotnet run bench/memory-model/hoisted-read.cs -c Release volatile
// Disassemble either loop with:
//   DOTNET_TieredCompilation=0 DOTNET_JitDisasm="SpinPlain SpinVolatile" \
//   DOTNET_JitDisasmDiffable=1 dotnet run bench/memory-model/hoisted-read.cs -c Release
using System.Diagnostics;
using System.Runtime.CompilerServices;

static class Hoist
{
    static bool stop;                      // a perfectly ordinary bool field
    static volatile bool vstop;            // the same field, one keyword different
    static long spins;

    [MethodImpl(MethodImplOptions.NoInlining)] static void SpinPlain()    { long n = 0; while (!stop)  n++; spins = n; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void SpinVolatile() { long n = 0; while (!vstop) n++; spins = n; }

    public static void Main(string[] args)
    {
        bool useVolatile = args.Length > 0 && args[0] == "volatile";
        var worker = new Thread(useVolatile ? SpinVolatile : SpinPlain) { IsBackground = true };
        var sw = Stopwatch.StartNew();
        worker.Start();
        Thread.Sleep(1000);                                   // let it get to tier-1 and spin
        if (useVolatile) vstop = true; else stop = true;      // the store the loop is waiting for
        bool exited = worker.Join(3000);
        Console.WriteLine($"{(useVolatile ? "volatile" : "plain   ")}: loop exited = {exited}, " +
                          $"{sw.ElapsedMilliseconds} ms after start, spins recorded = {spins:N0}");
        Environment.Exit(0);                                  // the plain worker is still spinning
    }
}
