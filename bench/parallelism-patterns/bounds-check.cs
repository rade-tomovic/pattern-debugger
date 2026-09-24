// Evidence for /systems/parallelism-patterns/ — run with:
//   DOTNET_TieredCompilation=0 DOTNET_JitDisasm=SumAll dotnet run bench/parallelism-patterns/bounds-check.cs -c Release
//   DOTNET_TieredCompilation=0 DOTNET_JitDisasm=SumRange dotnet run bench/parallelism-patterns/bounds-check.cs -c Release
// Two inner loops over the same array. SumAll is the idiomatic `for (i = 0; i < arr.Length; i++)`
// the JIT can prove in range. SumRange takes its bound from a Partitioner range (a Tuple field)
// instead of the array's own Length — the shape a range-partitioned Parallel.ForEach body has.
using System.Runtime.CompilerServices;

static class Bounds
{
    static readonly int[] data = new int[1_000_000];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long SumAll()
    {
        long s = 0;
        for (int i = 0; i < data.Length; i++) s += data[i];
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long SumRange((int, int) range)
    {
        long s = 0;
        for (int i = range.Item1; i < range.Item2; i++) s += data[i];
        return s;
    }

    public static void Main()
    {
        for (int i = 0; i < data.Length; i++) data[i] = 1;
        long expect = data.Length;
        long a = SumAll();
        long b = SumRange((0, data.Length));
        if (a != expect || b != expect) throw new Exception($"FAIL: {a}, {b} != {expect}");
        Console.WriteLine("PASS both sums correct");
    }
}
