// The exercise for /systems/gc-internals/generations-in-action/. No Stopwatch anywhere in this
// file — everything reported is a byte count, an object count, or a collection count.
//   dotnet run bench/gc-internals/generations-in-action.cs -c Release
using System;
using System.Runtime.CompilerServices;

const int N = 2_000_000;                     // allocations (or loop turns) per pattern

Console.WriteLine($"runtime={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}  " +
                  $"cores={Environment.ProcessorCount}  serverGC={System.Runtime.GCSettings.IsServerGC}");

// ── the three patterns ───────────────────────────────────────────────────────

// A. churn — every object is dead before the next one is born
[MethodImpl(MethodImplOptions.NoInlining)]
static long Churn(int n)
{
    long sum = 0;
    for (int i = 0; i < n; i++)
    {
        var e = new Entry(i, i * 2);
        sum += e.Value;                      // used, then unreachable
    }
    return sum;
}

// B. retain — every object is kept forever (a cache with no eviction)
[MethodImpl(MethodImplOptions.NoInlining)]
static long Retain(int n, Entry[] keep)
{
    long sum = 0;
    for (int i = 0; i < n; i++)
    {
        var e = new Entry(i, i * 2);
        keep[i] = e;                         // still reachable when the loop ends
        sum += e.Value;
    }
    return sum;
}

// C. reuse — one mutable object, no allocation at all
[MethodImpl(MethodImplOptions.NoInlining)]
static long Reuse(int n, Entry scratch)
{
    long sum = 0;
    for (int i = 0; i < n; i++)
    {
        scratch.Key = i;
        scratch.Value = i * 2;
        sum += scratch.Value;
    }
    return sum;
}

// ── harness ──────────────────────────────────────────────────────────────────
var keep = new Entry[N];                     // allocated once, before any pattern runs
var scratch = new Entry(0, 0);

Console.WriteLine("\n  pattern | allocated MB | gen0 | gen1 | gen2 | heap after MB");
foreach (string pattern in new[] { "churn", "retain", "reuse" })
{
    Array.Clear(keep);
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    GC.WaitForPendingFinalizers();
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);

    long a0 = GC.GetAllocatedBytesForCurrentThread();
    int c0 = GC.CollectionCount(0), c1 = GC.CollectionCount(1), c2 = GC.CollectionCount(2);

    double sink = pattern switch
    {
        "churn" => Churn(N),
        "retain" => Retain(N, keep),
        _ => Reuse(N, scratch),
    };

    long alloc = GC.GetAllocatedBytesForCurrentThread() - a0;
    int g0 = GC.CollectionCount(0) - c0, g1 = GC.CollectionCount(1) - c1, g2 = GC.CollectionCount(2) - c2;
    Console.WriteLine($"  {pattern,-7} | {alloc / 1048576.0,12:F1} | {g0,4} | {g1,4} | {g2,4} | " +
                      $"{GC.GetTotalMemory(false) / 1048576.0,13:F1}");
    GC.KeepAlive(sink);
}

// ── the promotion ladder: one retained object, watched across collections ────
Console.WriteLine("\n=== one object that is never dropped, after each forced collection ===");
{
    var survivor = new Entry(1, 1);
    Console.WriteLine($"  freshly allocated                      gen={GC.GetGeneration(survivor)}");
    for (int i = 1; i <= 4; i++)
    {
        GC.Collect(0, GCCollectionMode.Forced, blocking: true);
        Console.WriteLine($"  after gen0 collection #{i}               gen={GC.GetGeneration(survivor)}");
    }
    GC.Collect(1, GCCollectionMode.Forced, blocking: true);
    Console.WriteLine($"  after a gen1 collection                gen={GC.GetGeneration(survivor)}");
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    Console.WriteLine($"  after a gen2 collection                gen={GC.GetGeneration(survivor)}");
    GC.KeepAlive(survivor);
}

// ── where the retained objects ended up ──────────────────────────────────────
Console.WriteLine("\n=== generation census of the 2,000,000 retained objects ===");
{
    Array.Clear(keep);
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    Retain(N, keep);
    var census = new int[3];
    foreach (var e in keep) census[GC.GetGeneration(e)]++;
    Console.WriteLine($"  gen0 {census[0]:N0}   gen1 {census[1]:N0}   gen2 {census[2]:N0}");
    Console.WriteLine($"  heap now {GC.GetTotalMemory(false) / 1048576.0:F1} MB");
    GC.KeepAlive(keep);
}

class Entry(int key, long value)
{
    public int Key = key;
    public long Value = value;
}
