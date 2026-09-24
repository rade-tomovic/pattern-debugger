#:property AllowUnsafeBlocks=true
// Same bytes, same number of hops, same number of cache lines — spread over a different
// number of pages. Three arms, because the obvious two-arm version confounds two effects:
//   packed 64 B   — one line per slot, all of it on a handful of pages
//   spread 4096 B — one page per slot, and every line lands in the SAME cache set,
//                   because a power-of-two stride keeps bits 6-11 of the address constant
//   spread 4160 B — one page per slot, set index advancing by one line each hop,
//                   so the cache sets are used evenly and only translation is left
using System.Diagnostics;
using System.Runtime.InteropServices;

unsafe
{
    // A random cycle through `slots` 64-byte slots placed `stride` bytes apart.
    // Each slot holds the byte offset of the next one, so the chase is serial:
    // the CPU cannot prefetch a hop it has not resolved yet.
    static double Chase(int slots, int stride, int hops, Random rng)
    {
        nuint span = (nuint)slots * (nuint)stride;
        byte* buf = (byte*)NativeMemory.AllocZeroed(span);

        int[] order = Enumerable.Range(0, slots).ToArray();
        for (int i = slots - 1; i > 0; i--) { int j = rng.Next(i + 1); (order[i], order[j]) = (order[j], order[i]); }
        for (int i = 0; i < slots; i++)
            *(long*)(buf + (long)order[i] * stride) = (long)order[(i + 1) % slots] * stride;

        long p = 0;
        for (int i = 0; i < slots * 4; i++) p = *(long*)(buf + p);       // fault in and warm the caches
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < hops; i++) p = *(long*)(buf + p);
        sw.Stop();
        if (p == -1) Console.WriteLine("unreachable, but the JIT does not know that");
        NativeMemory.Free(buf);
        return sw.Elapsed.TotalNanoseconds / hops;
    }

    static double Median(double[] xs) { double[] c = (double[])xs.Clone(); Array.Sort(c); return c[c.Length / 2]; }

    var rng = new Random(1);
    const int Hops = 4_000_000;
    Chase(1024, 64, Hops, rng); Chase(1024, 4096, Hops, rng); Chase(1024, 4160, Hops, rng);   // warm up the JIT

    Console.WriteLine($"{"slots",6} {"data",8} {"packed 64 B",14} {"pages",6} {"spread 4096 B",14} {"spread 4160 B",14} {"pages",6}  4096/pk  4160/pk");
    foreach (int slots in new[] { 512, 1024, 2048, 4096, 8192 })
    {
        var packed = new double[5];
        var pow2 = new double[5];
        var odd = new double[5];
        for (int r = 0; r < 5; r++)
        {
            packed[r] = Chase(slots, 64, Hops, rng);
            pow2[r] = Chase(slots, 4096, Hops, rng);
            odd[r] = Chase(slots, 4160, Hops, rng);
        }
        double a = Median(packed), b = Median(pow2), c = Median(odd);
        Console.WriteLine($"{slots,6} {slots * 64 / 1024 + " KiB",8} {a,9:F2} ns/hop {slots * 64 / 4096 + 1,6} {b,9:F2} ns/hop {c,9:F2} ns/hop {slots,6}  {b / a,6:F2}x  {c / a,6:F2}x");
        Console.WriteLine($"       packed [{string.Join(", ", packed.Select(x => x.ToString("F2")))}]");
        Console.WriteLine($"       4096   [{string.Join(", ", pow2.Select(x => x.ToString("F2")))}]");
        Console.WriteLine($"       4160   [{string.Join(", ", odd.Select(x => x.ToString("F2")))}]");
    }
}
