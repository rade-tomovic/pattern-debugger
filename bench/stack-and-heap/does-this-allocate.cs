// Evidence for /systems/stack-and-heap/does-this-allocate/ — run with:
//   dotnet run bench/stack-and-heap/does-this-allocate.cs -c Release
//   dotnet run bench/stack-and-heap/does-this-allocate.cs
// (the second run is the same file with the JIT optimiser off: `dotnet run file.cs`
//  defaults to Debug, which stamps DebuggableAttribute(DisableOptimizations) on the assembly.)
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

struct Point { public int X, Y; public Point(int x, int y) { X = x; Y = y; } }
class Holder { public Point P; public Holder(Point p) { P = p; } }
interface IShape { int Area(); }
struct Square : IShape { public int Side; public Square(int s) { Side = s; } public int Area() => Side * Side; }

static class Probe
{
    public static long Sink;
    static readonly List<int> List = [1, 2, 3, 4, 5];
    static readonly IEnumerable<int> Sequence = List;

    // NoInlining on the callees: otherwise the JIT can fold a whole case away and
    // we would be measuring the optimiser rather than the language rule.
    [MethodImpl(MethodImplOptions.NoInlining)] static int TakeStruct(Point p) => p.X + p.Y;
    [MethodImpl(MethodImplOptions.NoInlining)] static int SumArray(params int[] xs)          { int s = 0; foreach (var x in xs) s += x; return s; }
    [MethodImpl(MethodImplOptions.NoInlining)] static int SumSpan(params ReadOnlySpan<int> xs){ int s = 0; foreach (var x in xs) s += x; return s; }
    [MethodImpl(MethodImplOptions.NoInlining)] static int AreaOf<T>(T shape) where T : IShape => shape.Area();
    [MethodImpl(MethodImplOptions.NoInlining)] static int AreaOf(IShape shape) => shape.Area();
    [MethodImpl(MethodImplOptions.NoInlining)] static int Scratch(int i) { Span<int> buf = stackalloc int[64]; buf[3] = i; int s = 0; foreach (var v in buf) s += v; return s; }

    // ── the eight snippets ───────────────────────────────────────────────────
    [MethodImpl(MethodImplOptions.NoInlining)] static void S1 (int n) { for (int i = 0; i < n; i++) Sink += TakeStruct(new Point(i, 2)); }
    [MethodImpl(MethodImplOptions.NoInlining)] static void S2 (int n) { for (int i = 0; i < n; i++) { object o = i; Sink += (int)o; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static int  Unbox(int i) { object o = i; return (int)o; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void S2b(int n) { for (int i = 0; i < n; i++) Sink += Unbox(i); }
    [MethodImpl(MethodImplOptions.NoInlining)] static void S3 (int n) { for (int i = 0; i < n; i++) { var h = new Holder(new Point(i, 2)); Sink += h.P.X; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static void S4 (int n) { for (int i = 0; i < n; i++) { int captured = i; Func<int> f = () => captured + 1; Sink += f(); } }
    [MethodImpl(MethodImplOptions.NoInlining)] static void S4b(int n) { for (int i = 0; i < n; i++) { Func<int> f = static () => 7; Sink += f(); } }
    [MethodImpl(MethodImplOptions.NoInlining)] static void S5 (int n) { for (int i = 0; i < n; i++) Sink += Scratch(i); }
    [MethodImpl(MethodImplOptions.NoInlining)] static void S5b(int n) { for (int i = 0; i < n; i++) { var buf = new int[64]; buf[3] = i; int s = 0; foreach (var v in buf) s += v; Sink += s; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static void S6 (int n) { for (int i = 0; i < n; i++) { int s = 0; foreach (var v in List) s += v; Sink += s; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static void S6b(int n) { for (int i = 0; i < n; i++) { int s = 0; foreach (var v in Sequence) s += v; Sink += s; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static void S7 (int n) { for (int i = 0; i < n; i++) Sink += SumArray(i, 2, 3); }
    [MethodImpl(MethodImplOptions.NoInlining)] static void S7b(int n) { for (int i = 0; i < n; i++) Sink += SumSpan(i, 2, 3); }
    [MethodImpl(MethodImplOptions.NoInlining)] static void S8 (int n) { for (int i = 0; i < n; i++) Sink += AreaOf(new Square(i)); }
    [MethodImpl(MethodImplOptions.NoInlining)] static void S8b(int n) { for (int i = 0; i < n; i++) Sink += AreaOf((IShape)new Square(i)); }

    // ── the harness ──────────────────────────────────────────────────────────
    // Marginal bytes per operation: run n, then 2n, subtract. Anything paid once
    // (JIT, first-call statics, the delegate cache) is in both and cancels out.
    static long BytesPerOp(Action<int> body, int n = 20_000)
    {
        body(n); body(n); body(n);                                  // warm: tier-0 -> tier-1
        long a0 = GC.GetAllocatedBytesForCurrentThread(); body(n);
        long a1 = GC.GetAllocatedBytesForCurrentThread(); body(2 * n);
        long a2 = GC.GetAllocatedBytesForCurrentThread();
        return ((a2 - a1) - (a1 - a0)) / n;
    }

    static void Row(string name, Action<int> body) => Console.WriteLine($"  {name,-52} {BytesPerOp(body),4} B/op");

    // ── the same question for a 64-int scratch buffer, three ways ─────────────
    const int Calls = 1_000_000;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Rented(int i)
    {
        int[] buf = ArrayPool<int>.Shared.Rent(64);
        buf.AsSpan(0, 64).Clear();                                  // Rent does not zero
        buf[3] = i; int s = 0; for (int k = 0; k < 64; k++) s += buf[k];
        ArrayPool<int>.Shared.Return(buf);
        return s;
    }
    [MethodImpl(MethodImplOptions.NoInlining)] static void BufStack(int n) { for (int i = 0; i < n; i++) Sink += Scratch(i); }
    [MethodImpl(MethodImplOptions.NoInlining)] static void BufHeap (int n) { for (int i = 0; i < n; i++) { var b = new int[64]; b[3] = i; int s = 0; for (int k = 0; k < 64; k++) s += b[k]; Sink += s; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static void BufPool (int n) { for (int i = 0; i < n; i++) Sink += Rented(i); }

    public static void Main()
    {
        Console.WriteLine($"JIT optimiser disabled: {typeof(Probe).Assembly.GetCustomAttributes(typeof(DebuggableAttribute), false) is [DebuggableAttribute d] && d.IsJITOptimizerDisabled}");
        Console.WriteLine("=== heap bytes per operation ===");
        Row("1  struct local, passed by value",                 S1);
        Row("2  object o = i          (boxing, in this loop)",  S2);
        Row("2b the same box inside a method that unboxes it",  S2b);
        Row("3  new Holder(new Point(i, 2))",                   S3);
        Row("4  lambda capturing a local",                      S4);
        Row("4b lambda capturing nothing",                      S4b);
        Row("5  stackalloc int[64] in a called method",         S5);
        Row("5b new int[64]",                                   S5b);
        Row("6  foreach over List<int>",                        S6);
        Row("6b foreach over the same list as IEnumerable<int>",S6b);
        Row("7  params int[]",                                  S7);
        Row("7b params ReadOnlySpan<int>",                      S7b);
        Row("8  struct through a generic constraint",           S8);
        Row("8b the same struct through its interface",         S8b);

        Console.WriteLine("\n=== a 64-int scratch buffer, three ways: heap bytes per call ===");
        Row("stackalloc int[64]",                    BufStack);
        Row("new int[64]",                           BufHeap);
        Row("ArrayPool<int>.Shared.Rent(64)",         BufPool);

        Console.WriteLine($"\n=== gen0 collections provoked by {Calls:N0} calls ===");
        int g0 = GC.CollectionCount(0); BufHeap(Calls);
        int gcHeap = GC.CollectionCount(0) - g0;
        g0 = GC.CollectionCount(0); BufStack(Calls);
        int gcStack = GC.CollectionCount(0) - g0;
        g0 = GC.CollectionCount(0); BufPool(Calls);
        int gcPool = GC.CollectionCount(0) - g0;
        Console.WriteLine($"  new int[64] {gcHeap}   stackalloc {gcStack}   ArrayPool {gcPool}");
        Console.WriteLine($"  checksum {Sink}");
    }
}
