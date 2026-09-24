// Evidence for /systems/memory-model/ — what the JIT emits on x86-64 for the
// four ways C# lets you talk about ordering. Dump it with:
//   DOTNET_TieredCompilation=0 DOTNET_JitDisasm="LitmusA ClaimA Publish WriteVolatile
//   ReadVolatile FullFence ExchangeIt" DOTNET_JitDisasmDiffable=1 \
//   dotnet run bench/memory-model/jit-fences.cs -c Release
using System.Runtime.CompilerServices;

static class Fences
{
    static int x, y, r1;
    static bool aWants, bWants;
    static volatile int vol;
    public static int Sink;

    [MethodImpl(MethodImplOptions.NoInlining)] static void LitmusA()        { x = 1; r1 = y; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void ClaimA()         { aWants = true; if (!bWants) r1 = 1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void Publish()        { x = 42; y = 1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void WriteVolatile(int v) => vol = v;
    [MethodImpl(MethodImplOptions.NoInlining)] static int  ReadVolatile()   => vol;
    [MethodImpl(MethodImplOptions.NoInlining)] static void FullFence()      { x = 1; Interlocked.MemoryBarrier(); r1 = y; }
    [MethodImpl(MethodImplOptions.NoInlining)] static int  ExchangeIt(int v) => Interlocked.Exchange(ref x, v);

    public static void Main()
    {
        aWants = false; bWants = false;
        LitmusA(); ClaimA(); Publish(); WriteVolatile(7); Sink += ReadVolatile(); FullFence(); Sink += ExchangeIt(3);
        Console.WriteLine($"checksum {Sink + x + y + r1 + (aWants ? 1 : 0) + (bWants ? 1 : 0)}");
    }
}
