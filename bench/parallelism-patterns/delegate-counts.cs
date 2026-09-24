// Evidence for /systems/parallelism-patterns/ — run with:
//   dotnet run bench/parallelism-patterns/delegate-counts.cs -c Release
// Not a timing run. Counts how many times the body delegate actually gets called for
// the same million-element loop, per-item versus range-partitioned.
using System.Collections.Concurrent;

static class Counts
{
    const int N = 1_000_000;

    public static void Main()
    {
        Console.WriteLine($"processors visible to the runtime: {Environment.ProcessorCount}");

        long perItemCalls = 0;
        Parallel.For(0, N, _ => Interlocked.Increment(ref perItemCalls));
        if (perItemCalls != N) throw new Exception($"FAIL per-item: {perItemCalls} calls, expected {N}");

        long rangeCalls = 0, itemsSeen = 0;
        Parallel.ForEach(Partitioner.Create(0, N), range =>
        {
            Interlocked.Increment(ref rangeCalls);
            Interlocked.Add(ref itemsSeen, range.Item2 - range.Item1);
        });
        if (itemsSeen != N) throw new Exception($"FAIL range: saw {itemsSeen} items, expected {N}");

        Console.WriteLine($"Parallel.For(0, {N:N0}, body)              body invoked {perItemCalls:N0} times");
        Console.WriteLine($"Parallel.ForEach(Partitioner.Create(0,{N:N0}))  body invoked {rangeCalls} times");
        Console.WriteLine("PASS both totals correct");
    }
}
