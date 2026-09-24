// Every fact on /systems/gc-internals/ comes from this file. No Stopwatch anywhere in it —
// everything reported is a byte count, an object count, a collection count, a generation
// number, or an address. Run it as:
//
//   dotnet run bench/gc-internals/index.cs -c Release
//   dotnet run bench/gc-internals/index.cs -c Release -- move        (run alone — heap-state sensitive)
//   env DOTNET_gcServer=1 dotnet run bench/gc-internals/index.cs -c Release -- mode
//   env DOTNET_gcServer=1 DOTNET_GCDynamicAdaptationMode=0 \
//       dotnet run bench/gc-internals/index.cs -c Release -- mode
//   DOTNET_TieredCompilation=0 DOTNET_JitDisasm="Bench:*" dotnet run bench/gc-internals/index.cs -c Release -- barrier
//
// Optional args select phases: bytes budget cycle promote finalizer-life move barrier
// concurrent-floor loh pool survival latency mode.  No args = all of them.
#:property AllowUnsafeBlocks=true

using System;
using System.Buffers;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;

var want = new HashSet<string>(args);
bool Phase(string name) => want.Count == 0 || want.Contains(name);

Console.WriteLine($"runtime={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}  " +
                  $"cores={Environment.ProcessorCount}  serverGC={GCSettings.IsServerGC}");

// ── bytes: what a `new` is charged, and where the LOH line is ────────────────
if (Phase("bytes"))
{
    Console.WriteLine("\n=== bytes charged per allocation ===");
    Report("new Node()", () => new Node());
    Report("new byte[64]", () => new byte[64]);

    Console.WriteLine("\n=== the exact large-object-heap boundary ===");
    for (int len = 84_973; len <= 84_978; len++)
    {
        var a = new byte[len];
        Console.WriteLine($"  new byte[{len}]   object size {len + 24,6} B   gen={GC.GetGeneration(a)}");
    }
}

static void Report(string what, Func<object> f)
{
    f();                                                     // warm the JIT
    long before = GC.GetAllocatedBytesForCurrentThread();
    object o = f();
    long after = GC.GetAllocatedBytesForCurrentThread();
    Console.WriteLine($"  {what,-14} charged {after - before,7} B   gen={GC.GetGeneration(o)}");
}

// ── budget: how many bytes fit between two gen0 collections ──────────────
if (Phase("budget"))
{
    Console.WriteLine("\n=== bytes allocated between consecutive gen0 collections ===");
    var gaps = new List<long>();
    int last = GC.CollectionCount(0);
    long mark = GC.GetAllocatedBytesForCurrentThread();
    Node? sink = null;
    while (gaps.Count < 12)
    {
        sink = new Node();
        int now = GC.CollectionCount(0);
        if (now != last)
        {
            long here = GC.GetAllocatedBytesForCurrentThread();
            gaps.Add(here - mark);
            mark = here;
            last = now;
        }
    }
    GC.KeepAlive(sink);
    gaps.RemoveAt(0);                                        // the first gap starts mid-budget
    var arr = gaps.ToArray();
    Array.Sort(arr);
    Console.WriteLine($"  median gap {arr[arr.Length / 2]:N0} B over {arr.Length} collections");
    Console.WriteLine("  all gaps: " + string.Join(" ", Array.ConvertAll(arr, v => (v / 1024 / 1024) + " MB")));
}

// ── cycle: reachability is not reference counting ────────────────────────────
if (Phase("cycle"))
{
    Console.WriteLine("\n=== a reference cycle is still garbage ===");
    var (a, b) = MakeCycle();
    Console.WriteLine($"  before GC: a alive={a.IsAlive}  b alive={b.IsAlive}");
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    Console.WriteLine($"  after  GC: a alive={a.IsAlive}  b alive={b.IsAlive}   (each still points at the other)");
}

[MethodImpl(MethodImplOptions.NoInlining)]
static (WeakReference, WeakReference) MakeCycle()
{
    var x = new Node();
    var y = new Node();
    x.Next = y;                                              // x → y
    y.Next = x;                                              // y → x — refcounting would never free these
    return (new WeakReference(x), new WeakReference(y));
}

// ── promote: the promotion ladder, watched directly ───────────────────────────
if (Phase("promote"))
{
    Console.WriteLine("\n=== one object that is never dropped, after each forced collection ===");
    var survivor = new Node();
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

// ── finalizer-life: a finalizer costs the object one extra collection ────────
if (Phase("finalizer-life"))
{
    Console.WriteLine("\n=== how many collections until the bytes come back ===");
    Console.WriteLine($"  main thread id = {Environment.CurrentManagedThreadId}");
    var (pShort, pLong) = MakePlain();
    var (fShort, fLong) = MakeFinalizable();

    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    Console.WriteLine($"  after 1st GC:  plain unreachable={!pShort.IsAlive} gone={!pLong.IsAlive}   " +
                      $"finalizable unreachable={!fShort.IsAlive} gone={!fLong.IsAlive}");
    GC.WaitForPendingFinalizers();
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    Console.WriteLine($"  after 2nd GC:  plain unreachable={!pShort.IsAlive} gone={!pLong.IsAlive}   " +
                      $"finalizable unreachable={!fShort.IsAlive} gone={!fLong.IsAlive}");
    Console.WriteLine($"  finalizer ran {Finalizable.Ran} time(s), on thread id {Finalizable.RanOnThread}");

    Console.WriteLine("\n=== 500,000 objects allocated and dropped, with and without a finalizer ===");
    Console.WriteLine("  kind             | gen0 | gen1 | gen2");
    foreach (bool finalizable in new[] { false, true })
    {
        var c0 = GC.CollectionCount(0); var c1 = GC.CollectionCount(1); var c2 = GC.CollectionCount(2);
        ChurnFinalizable(500_000, finalizable);
        Console.WriteLine($"  {(finalizable ? "~Finalizer()" : "plain"),-16} | {GC.CollectionCount(0) - c0,4} | " +
                          $"{GC.CollectionCount(1) - c1,4} | {GC.CollectionCount(2) - c2,4}");
    }
}

// short weak ref = cleared as soon as the object is unreachable
// long weak ref (trackResurrection) = cleared only when the memory is actually reclaimed
[MethodImpl(MethodImplOptions.NoInlining)]
static (WeakReference, WeakReference) MakePlain()
{
    var o = new Node();
    return (new WeakReference(o, false), new WeakReference(o, true));
}

[MethodImpl(MethodImplOptions.NoInlining)]
static (WeakReference, WeakReference) MakeFinalizable()
{
    var o = new Finalizable();
    return (new WeakReference(o, false), new WeakReference(o, true));
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void ChurnFinalizable(int count, bool finalizable)
{
    object? last = null;
    for (int i = 0; i < count; i++) last = finalizable ? new Finalizable() : new Node();
    GC.KeepAlive(last);
}

// ── move: does the collector relocate an object, and which collections do it ─────────────
// The address of a managed object is not observable through the public API, so this reads the
// reference itself out of a TypedReference. Run this phase alone — it is heap-state sensitive.
if (Phase("move"))
{
    Console.WriteLine("\n=== does this object move? (address read straight out of the reference) ===");
    var keep = new Node[64];
    for (int i = 0; i < keep.Length; i++) keep[i] = new Node();
    var o = new Node();
    nint a = Addr(o);
    Console.WriteLine($"  freshly allocated             gen={GC.GetGeneration(o)}  addr=0x{a:x}");
    void Step(string label)
    {
        nint b = Addr(o);
        Console.WriteLine($"  {label,-33} gen={GC.GetGeneration(o)}  addr=0x{b:x}  moved={(b != a),-5} delta={b - a}");
        a = b;
    }
    GC.Collect(0, GCCollectionMode.Forced, blocking: true);                      Step("after blocking gen0");
    GC.Collect(1, GCCollectionMode.Forced, blocking: true);                      Step("after blocking gen1");
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);                      Step("after blocking gen2");
    GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);    Step("after blocking gen2, compacting");
    GC.Collect(2, GCCollectionMode.Default, blocking: false);
    Thread.Sleep(500);                                                           Step("after background gen2");
    GC.KeepAlive(keep);
}

static unsafe nint Addr(object o)
{
    TypedReference tr = __makeref(o);
    return **(nint**)(&tr);
}

// ── barrier: what the JIT emits for a reference store vs an int store ────────
// Two one-line methods, called once each so the JIT compiles them and DOTNET_JitDisasm can
// dump the real machine code. No timing — the disassembly itself is the evidence.
if (Phase("barrier"))
{
    Console.WriteLine("\n=== triggering JIT compilation of the two barrier-comparison methods ===");
    var h = new Node();
    Bench.StoreRef(h, new Node());
    Bench.StoreInt(h, 1);
    Console.WriteLine("  compiled — rerun with DOTNET_JitDisasm=\"Bench:*\" to see the assembly");
}

// ── concurrent-floor: background GC declines below a heap-size floor ─────────
// GC.GetGCMemoryInfo().Concurrent reports whether the collection the runtime actually ran was
// concurrent (background) or not — a fact the runtime states about itself, not a timing.
if (Phase("concurrent-floor"))
{
    Console.WriteLine("\n=== does a non-blocking GC.Collect(2) actually run in the background? ===");
    Console.WriteLine("  live objects | live heap MB | Concurrent | Compacted");
    foreach (int live in new[] { 100_000, 400_000, 1_600_000, 6_400_000 })
    {
        var keep = new Node[live];
        for (int i = 0; i < live; i++) keep[i] = new Node { Id = i };
        GC.Collect(2, GCCollectionMode.Default, blocking: false);
        Thread.Sleep(200);
        var info = GC.GetGCMemoryInfo();
        Console.WriteLine($"  {live,12:N0} | {GC.GetTotalMemory(false) / 1048576.0,12:F1} | " +
                          $"{info.Concurrent,-10} | {info.Compacted}");
        GC.KeepAlive(keep);
    }
}

// ── loh: allocating just above 85,000 bytes changes which collection you get ──
if (Phase("loh"))
{
    Console.WriteLine("\n=== ≈2 GB of array churn, 84,000-byte vs 86,000-byte arrays ===");
    Console.WriteLine("  size    | gen0 | gen1 | gen2");
    foreach (int size in new[] { 84_000, 86_000 })
    {
        int iterations = (int)(2_000_000_000L / size);
        var c0 = GC.CollectionCount(0); var c1 = GC.CollectionCount(1); var c2 = GC.CollectionCount(2);
        ChurnArrays(iterations, size);
        Console.WriteLine($"  {size,-7:N0} | {GC.CollectionCount(0) - c0,4} | {GC.CollectionCount(1) - c1,4} | " +
                          $"{GC.CollectionCount(2) - c2,4}");
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void ChurnArrays(int iterations, int size)
{
    byte[]? last = null;
    for (int i = 0; i < iterations; i++) { last = new byte[size]; last[0] = 1; }
    GC.KeepAlive(last);
}

// ── pool: the escape hatch ───────────────────────────────────────────────────
if (Phase("pool"))
{
    Console.WriteLine("\n=== 20,000 × 86,000-byte buffers: new vs ArrayPool ===");
    Console.WriteLine("  strategy   | bytes allocated | gen2");
    foreach (bool pooled in new[] { false, true })
    {
        long b0 = GC.GetAllocatedBytesForCurrentThread();
        var c2 = GC.CollectionCount(2);
        Buffers(20_000, pooled);
        Console.WriteLine($"  {(pooled ? "ArrayPool" : "new"),-10} | {GC.GetAllocatedBytesForCurrentThread() - b0,15:N0} | " +
                          $"{GC.CollectionCount(2) - c2,4}");
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void Buffers(int iterations, bool pooled)
{
    for (int i = 0; i < iterations; i++)
    {
        byte[] buf = pooled ? ArrayPool<byte>.Shared.Rent(86_000) : new byte[86_000];
        buf[0] = 1; buf[85_999] = 2;
        if (pooled) ArrayPool<byte>.Shared.Return(buf);
    }
}

// ── survival: same allocations, same stores, different survival rate ─────────
if (Phase("survival"))
{
    Console.WriteLine("\n=== 2,000,000 allocations, identical work, only the survival rate changes ===");
    Console.WriteLine("  survivors        | gen0 | gen1 | gen2");
    var keeper = new Node[1_000_000];                        // allocated once, outside every timed region
    foreach (int slots in new[] { 1, 20_000, 200_000, 1_000_000 })
    {
        Array.Clear(keeper);
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        var c0 = GC.CollectionCount(0); var c1 = GC.CollectionCount(1); var c2 = GC.CollectionCount(2);
        Churn(keeper, slots, 2_000_000);
        Console.WriteLine($"  {slots,9:N0} ({slots / 20_000.0,4:F1}%) | {GC.CollectionCount(0) - c0,4} | " +
                          $"{GC.CollectionCount(1) - c1,4} | {GC.CollectionCount(2) - c2,4}");
    }
}

// exactly `count` allocations and `count` array stores every time;
// `slots` decides how many of those objects are still reachable at the end
[MethodImpl(MethodImplOptions.NoInlining)]
static void Churn(Node[] keeper, int slots, int count)
{
    int slot = 0;
    for (int i = 0; i < count; i++)
    {
        keeper[slot] = new Node { Id = i };
        slot++;
        if (slot == slots) slot = 0;
    }
}

// ── latency: how many pauses a request stream causes, allocating vs pooling ──
if (Phase("latency"))
{
    Console.WriteLine("\n=== 200,000 'requests', each needing an 8 KB buffer ===");
    Console.WriteLine("  strategy   | gen0 | gen1 | gen2");
    foreach (bool pooled in new[] { false, true })
    {
        Serve(pooled, 20_000);                                // warmup, discarded
        var c0 = GC.CollectionCount(0); var c1 = GC.CollectionCount(1); var c2 = GC.CollectionCount(2);
        Serve(pooled, 200_000);
        Console.WriteLine($"  {(pooled ? "ArrayPool" : "new"),-10} | {GC.CollectionCount(0) - c0,4} | " +
                          $"{GC.CollectionCount(1) - c1,4} | {GC.CollectionCount(2) - c2,4}");
    }
}

// one "request": get an 8 KB buffer, touch it
[MethodImpl(MethodImplOptions.NoInlining)]
static void Serve(bool pooled, int count)
{
    int sink = 0;
    for (int i = 0; i < count; i++)
    {
        byte[] buf = pooled ? ArrayPool<byte>.Shared.Rent(8192) : new byte[8192];
        for (int j = 0; j < 8192; j += 64) buf[j] = (byte)i;
        for (int j = 0; j < 8192; j += 64) sink += buf[j];
        if (pooled) ArrayPool<byte>.Shared.Return(buf);
    }
    GC.KeepAlive(sink);
}

// ── mode: workstation vs server GC on the same 4-thread workload ─────────────
// One process per mode (set DOTNET_gcServer / DOTNET_GCDynamicAdaptationMode before running) —
// the mode is fixed at process start and cannot be switched mid-run.
if (Phase("mode"))
{
    Console.WriteLine("\n=== 4 threads × 2,000,000 allocations, 1% surviving ===");
    var cfg = GC.GetConfigurationVariables();
    Console.WriteLine($"  serverGC={GCSettings.IsServerGC}  " +
                      $"heapCount={cfg["HeapCount"]}  " +
                      $"DATAS(GCDynamicAdaptationMode)={cfg["GCDynamicAdaptationMode"]}");
    var keepers = new Node[4][];
    for (int t = 0; t < 4; t++) keepers[t] = new Node[20_000];
    foreach (var k in keepers) Array.Clear(k);
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    var c0 = GC.CollectionCount(0); var c1 = GC.CollectionCount(1); var c2 = GC.CollectionCount(2);
    var threads = new Thread[4];
    for (int t = 0; t < 4; t++)
    {
        var k = keepers[t];
        threads[t] = new Thread(() => Churn(k, 20_000, 2_000_000));
        threads[t].Start();
    }
    foreach (var th in threads) th.Join();
    Console.WriteLine($"  gen0={GC.CollectionCount(0) - c0}  gen1={GC.CollectionCount(1) - c1}  " +
                      $"gen2={GC.CollectionCount(2) - c2}  heap after={GC.GetTotalMemory(false) / 1048576.0:F1} MB");
}

Console.WriteLine($"\nprocess totals: heap {GC.GetTotalMemory(false):N0} B");

class Node { public int Id; public Node? Next; }

// the two one-line methods the disassembly on the page comes from
static class Bench
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void StoreRef(Node h, Node n) => h.Next = n;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void StoreInt(Node h, int i) => h.Id = i;
}

class Finalizable
{
    public static int Ran;
    public static int RanOnThread;
    ~Finalizable() { Ran++; RanOnThread = Environment.CurrentManagedThreadId; }
}
