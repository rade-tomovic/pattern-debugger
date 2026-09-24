// Evidence for /systems/lock-free-structures/ — run with:
//   dotnet run bench/lock-free-structures/index.cs
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;

public static class Bench
{
    // ── 1. how ConcurrentDictionary is actually built ───────────────────────
    // It is a table of buckets guarded by a much smaller array of Monitor
    // objects; bucket b is guarded by lock b % locks.Length. Read the live
    // object's private fields by reflection to see the real shape.
    static (int buckets, int locks) Shape(ConcurrentDictionary<int, int> d)
    {
        object tables = typeof(ConcurrentDictionary<int, int>)
            .GetField("_tables", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(d)!;
        var b = (Array)tables.GetType().GetField("_buckets", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(tables)!;
        var l = (Array)tables.GetType().GetField("_locks", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(tables)!;
        return (b.Length, l.Length);
    }

    // ── 2. GetOrAdd runs the factory more than once ──────────────────────────
    static void FactoryRuns(int threads)
    {
        int runs = 0;
        var d = new ConcurrentDictionary<int, string>();
        var gate = new Barrier(threads);
        var ths = new Thread[threads];
        for (int t = 0; t < threads; t++)
        {
            ths[t] = new Thread(() =>
            {
                gate.SignalAndWait();
                for (int k = 0; k < 200; k++)
                    d.GetOrAdd(k, key => { Interlocked.Increment(ref runs); Thread.SpinWait(200); return "v" + key; });
            });
            ths[t].Start();
        }
        foreach (var th in ths) th.Join();
        Console.WriteLine($"  {threads} threads, 200 keys: {d.Count} entries, factory ran {runs} times");
    }

    // ── 3. a copy-on-write Set allocates the whole map, every time ───────────
    // Dictionary<int,int>(_snapshot) copies every entry into the new instance,
    // so bytes allocated per Set should scale with n, the entry count.
    static long BytesPerCowSet(int n)
    {
        var snapshot = new Dictionary<int, int>();
        for (int k = 0; k < n; k++) snapshot[k] = k;
        GC.Collect(); GC.WaitForPendingFinalizers();
        long a0 = GC.GetAllocatedBytesForCurrentThread();
        var copy = new Dictionary<int, int>(snapshot) { [0] = 1 };   // one CowMap.Set
        long a1 = GC.GetAllocatedBytesForCurrentThread();
        GC.KeepAlive(copy);
        return a1 - a0;
    }

    // ── 4. an immutable, structurally-shared map does not copy the whole tree ─
    // ImmutableDictionary<K,V> is a balanced binary tree of nodes; SetItem
    // rebuilds only the O(log n) nodes on the path to the changed key and
    // reuses every other node by reference. Bytes allocated per SetItem should
    // grow much more slowly than n.
    static long BytesPerImmutableSet(int n)
    {
        var builder = ImmutableDictionary.CreateBuilder<int, int>();
        for (int k = 0; k < n; k++) builder[k] = k;
        var snapshot = builder.ToImmutable();
        GC.Collect(); GC.WaitForPendingFinalizers();
        long a0 = GC.GetAllocatedBytesForCurrentThread();
        var next = snapshot.SetItem(0, 1);                            // one persistent-map "write"
        long a1 = GC.GetAllocatedBytesForCurrentThread();
        GC.KeepAlive(next);
        return a1 - a0;
    }

    public static void Main()
    {
        Console.WriteLine("=== 1. ConcurrentDictionary is striped locks, and the stripes grow ===");
        Console.WriteLine($"  ProcessorCount = {Environment.ProcessorCount}");
        var empty = new ConcurrentDictionary<int, int>();
        Console.WriteLine($"  {"empty",8}: buckets {Shape(empty).buckets,7}  locks {Shape(empty).locks,5}");
        foreach (int n in new[] { 100, 1_000, 10_000, 100_000 })
        {
            var d = new ConcurrentDictionary<int, int>();
            for (int i = 0; i < n; i++) d[i] = i;
            var (b, l) = Shape(d);
            Console.WriteLine($"  {n,8}: buckets {b,7}  locks {l,5}");
        }

        Console.WriteLine("\n=== 2. GetOrAdd runs the factory more than once ===");
        FactoryRuns(2);
        FactoryRuns(4);

        Console.WriteLine("\n=== 3. bytes allocated by one Set/SetItem, as the map grows ===");
        Console.WriteLine("  entries      copy-on-write Set    ImmutableDictionary.SetItem");
        foreach (int n in new[] { 100, 1_000, 10_000, 100_000 })
        {
            long cow = BytesPerCowSet(n);
            long imm = BytesPerImmutableSet(n);
            Console.WriteLine($"  {n,8}     {cow,10:N0} B            {imm,10:N0} B");
        }

        Console.WriteLine("\nPASS");
    }
}
