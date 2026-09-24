#:property AllowUnsafeBlocks=true
// The settled second-touch pass costs tens of nanoseconds a page even though every
// page is mapped and every fault is paid. How much of that is translation, and how
// much is the 4096-byte stride itself?
//
// A stride of exactly one page holds address bits 6-11 constant, and those are the
// bits this box's L1 data cache uses to pick a set (32 KiB, 8 ways, 64 sets). So the
// plain walk puts all 65,536 lines in L1 set 0 and in 16 of the L2's 1,024 sets.
//
// Both arms below run the SAME machine code — one non-inlined method, called with a
// different mask — over the SAME 65,536 resident pages, touching exactly one 64-byte
// line per page:
//   mask 0   offset within the page is always 0        -> every line in L1 set 0
//   mask 63  offset rotates 0, 64, 128 ... 4032        -> lines spread over all 64 sets
// Same page count, same TLB pressure, same instruction count. The difference is the
// cache-set collision and nothing else.
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

const int Page = 4096;
const int Pages = 64 * 1024;               // 65,536 pages, 256 MiB
const nuint Bytes = (nuint)Pages * Page;

unsafe
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static double Walk(byte* p, int mask, byte v, Stopwatch sw)
    {
        sw.Restart();
        for (int i = 0; i < Pages; i++) p[(nuint)i * Page + (nuint)((i & mask) << 6)] = v;
        return sw.Elapsed.TotalNanoseconds / Pages;
    }

    static double Median(double[] xs) { double[] c = (double[])xs.Clone(); Array.Sort(c); return c[c.Length / 2]; }

    var sw = new Stopwatch();
    byte* p = (byte*)NativeMemory.Alloc(Bytes);

    // Make every page resident, by both offsets, so no arm can fault during timing.
    for (int i = 0; i < Pages; i++) p[(nuint)i * Page] = 1;
    for (int i = 0; i < Pages; i++) p[(nuint)i * Page + (nuint)((i & 63) << 6)] = 1;

    const int Warm = 3, Runs = 7;
    var fix = new double[Runs];
    var rot = new double[Runs];
    for (int r = 0; r < Warm + Runs; r++)
    {
        // Interleaved inside one loop: a slow patch of the machine cannot land on
        // one arm only.
        double a = Walk(p, 0, 2, sw);
        double b = Walk(p, 63, 3, sw);
        if (r >= Warm) { fix[r - Warm] = a; rot[r - Warm] = b; }
    }

    NativeMemory.Free(p);

    Console.WriteLine($"stride 4096, byte 0 of every page   median {Median(fix),6:F1} ns/page   runs [{string.Join(", ", fix.Select(x => x.ToString("F1")))}]");
    Console.WriteLine($"same pages, line rotated in page    median {Median(rot),6:F1} ns/page   runs [{string.Join(", ", rot.Select(x => x.ToString("F1")))}]");
    Console.WriteLine($"\nratio {Median(fix) / Median(rot):F2}x   direction held in all {Runs} runs: {fix.Zip(rot).All(t => t.First > t.Second)}");
}
