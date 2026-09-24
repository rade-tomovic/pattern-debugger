#:property AllowUnsafeBlocks=true
// Evidence for /systems/stack-and-heap/ — run with:
//   dotnet run bench/stack-and-heap/index.cs -c Release
using System.Globalization;

struct Point { public int X, Y; }
class Node { public Point P; public int Id; }

static class Bench
{
    const int N = 1_000_000;
    public static long Sink;

    // ── part 1: where does each thing actually live? ─────────────────────────
    static string Region(nint addr)
    {
        var a = (ulong)addr;                       // re-read every time: the map changes as we run
        foreach (var line in File.ReadAllLines("/proc/self/maps"))
        {
            int dash = line.IndexOf('-'), sp = line.IndexOf(' ');
            var lo = ulong.Parse(line[..dash], NumberStyles.HexNumber);
            var hi = ulong.Parse(line[(dash + 1)..sp], NumberStyles.HexNumber);
            if (a >= lo && a < hi)
                return $"{(hi - lo) / 1024,6} KiB mapping {(line.Contains("[stack]") ? "[stack]" : "(anonymous)")}";
        }
        return "(not mapped)";
    }

    static unsafe void Show(string what, void* p)
        => Console.WriteLine($"  {what,-36} 0x{(nint)p:X}   {Region((nint)p)}");

    static unsafe void OnOtherThread(string label) { int local = 0; Show(label, &local); }

    static unsafe void Addresses()
    {
        Console.WriteLine("=== 1. addresses, and the mapping each one falls in ===");
        Point localPoint = default;
        int localInt = 0;
        Span<int> scratch = stackalloc int[16];
        var node = new Node { P = new Point { X = 3, Y = 4 }, Id = 9 };
        var arr = new int[8];

        Show("local int", &localInt);
        Show("local Point (a struct)", &localPoint);
        fixed (int* q = &scratch[0]) Show("stackalloc int[16]", q);
        fixed (Point* q = &node.P) Show("node.P — struct field of a class", q);
        fixed (int* q = &node.Id) Show("node.Id — int field of a class", q);
        fixed (int* q = &arr[0]) Show("arr[0] — int[8]", q);
        Console.WriteLine($"  two stack locals are {(nint)(&localPoint) - (nint)(&localInt)} bytes apart");
        GC.KeepAlive(node); GC.KeepAlive(arr);

        Console.WriteLine("\n=== 2. a thread's stack is its own mapping, sized when the thread is created ===");
        var done = new ManualResetEventSlim();
        ThreadPool.UnsafeQueueUserWorkItem(_ => { OnOtherThread("thread-pool thread"); done.Set(); }, null);
        done.Wait();
        // A small explicit size, on purpose: a stack this small never sits next to
        // another big anonymous mapping the kernel could fold it into when listing
        // /proc/self/maps, so the reported width is a clean read of the reservation.
        var t2 = new Thread(() => OnOtherThread("new Thread(f, 256 * 1024)"), 256 * 1024); t2.Start(); t2.Join();
    }

    // ── part 2b: the promotion — a local a lambda mentions is not on the stack ─
    static unsafe void Promotion()
    {
        Console.WriteLine("\n=== 2b. two int locals, declared side by side ===");
        int plain = 1;
        int captured = 2;
        Func<int> read = () => captured + 1;                 // the only difference
        // AsPointer, not `fixed`: the compiler refuses to take the address of a
        // captured local (CS1686), because it is no longer a local at all.
        nint c = (nint)System.Runtime.CompilerServices.Unsafe.AsPointer(ref captured);
        Console.WriteLine($"  {"int captured (a lambda reads it)",-36} 0x{c:X}   {Region(c)}");
        Console.WriteLine($"  {"int plain",-36} 0x{(nint)(&plain):X}   {Region((nint)(&plain))}");
        Sink += read();
    }

    // ── part 3: what boxing costs, in bytes ───────────────────────────────────
    static List<int> FillInts()    { var l = new List<int>(N);    for (int i = 0; i < N; i++) l.Add(i); return l; }
    static List<object> FillBoxed(){ var l = new List<object>(N); for (int i = 0; i < N; i++) l.Add(i); return l; }

    static long Allocated(Action f)
    {
        f(); f();                                  // warm: tier-0 JIT allocates too
        long before = GC.GetAllocatedBytesForCurrentThread();
        f();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static void Main()
    {
        Addresses();
        Promotion();

        Console.WriteLine("\n=== 3. sizes the GC actually charged ===");
        object? keep = null;
        Console.WriteLine($"  new object()          {Allocated(() => keep = new object()),3} bytes");
        Console.WriteLine($"  (object)42  [boxing]  {Allocated(() => keep = (object)42),3} bytes");
        Console.WriteLine($"  new Node()            {Allocated(() => keep = new Node()),3} bytes   (2 ints in a Point + 1 int)");
        Console.WriteLine($"  new int[8]            {Allocated(() => keep = new int[8]),3} bytes");
        GC.KeepAlive(keep);

        Console.WriteLine("\n=== 4. one million ints, as ints and as objects ===");
        long fillIntBytes = Allocated(() => keep = FillInts());
        long fillBoxBytes = Allocated(() => keep = FillBoxed());
        GC.KeepAlive(keep);

        // how many collections does one fill of each provoke?
        int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
        keep = FillInts();
        string gcInts = $"gen0 +{GC.CollectionCount(0) - g0}, gen1 +{GC.CollectionCount(1) - g1}, gen2 +{GC.CollectionCount(2) - g2}";
        g0 = GC.CollectionCount(0); g1 = GC.CollectionCount(1); g2 = GC.CollectionCount(2);
        keep = FillBoxed();
        string gcBoxed = $"gen0 +{GC.CollectionCount(0) - g0}, gen1 +{GC.CollectionCount(1) - g1}, gen2 +{GC.CollectionCount(2) - g2}";
        GC.KeepAlive(keep);

        Console.WriteLine($"  fill List<int>     {fillIntBytes,12:N0} bytes   collections: {gcInts}");
        Console.WriteLine($"  fill List<object>  {fillBoxBytes,12:N0} bytes   collections: {gcBoxed}");
        Console.WriteLine($"  bytes ratio {(double)fillBoxBytes / fillIntBytes:F2}x");
        Console.WriteLine("PASS");
    }
}
