#:property AllowUnsafeBlocks=true
// How much of walk B's ~34 ns/page is cold cache rather than "no fault"?
// Walk A (the first touch) drags 256 MiB of kernel zeroing through the caches,
// so the pass that follows it starts with everything evicted. Repeat the
// identical second-touch pass four times: whatever it settles to is the
// steady-state mapped-page cost, and the gap between pass 1 and pass 4 is the
// part of B that was cold cache.
using System.Diagnostics;
using System.Runtime.InteropServices;

const int Page = 4096;
const int Pages = 64 * 1024;               // 65,536 pages
const nuint Bytes = (nuint)Pages * Page;   // 256 MiB
const int Repeats = 4;

unsafe
{
    double[] Sample()
    {
        var sw = new Stopwatch();
        var r = new double[Repeats + 1];

        byte* p = (byte*)NativeMemory.Alloc(Bytes);

        sw.Restart();                                                        // A: first touch
        for (int i = 0; i < Pages; i++) p[(nuint)i * Page] = 1;
        r[0] = sw.Elapsed.TotalNanoseconds / Pages;

        for (int k = 1; k <= Repeats; k++)                                   // B, B, B, B
        {
            sw.Restart();
            for (int i = 0; i < Pages; i++) p[(nuint)i * Page] = (byte)k;
            r[k] = sw.Elapsed.TotalNanoseconds / Pages;
        }

        NativeMemory.Free(p);
        return r;
    }

    static double Median(double[] xs) { double[] c = (double[])xs.Clone(); Array.Sort(c); return c[c.Length / 2]; }

    for (int i = 0; i < 2; i++) Sample();          // warm up: JIT and glibc's arena habits

    const int Runs = 7;
    var cols = new double[Repeats + 1][];
    for (int k = 0; k <= Repeats; k++) cols[k] = new double[Runs];
    for (int run = 0; run < Runs; run++)
    {
        double[] s = Sample();
        for (int k = 0; k <= Repeats; k++) cols[k][run] = s[k];
    }

    string[] names = ["A  first touch", "B1 second touch", "B2 third touch", "B3 fourth touch", "B4 fifth touch"];
    for (int k = 0; k <= Repeats; k++)
        Console.WriteLine($"{names[k],-16} median {Median(cols[k]),7:F1} ns/page   runs [{string.Join(", ", cols[k].Select(x => x.ToString("F1")))}]");
    Console.WriteLine($"\nB1/B4 = {Median(cols[1]) / Median(cols[Repeats]):F2}x   A/B1 = {Median(cols[0]) / Median(cols[1]):F1}x   A/B4 = {Median(cols[0]) / Median(cols[Repeats]):F1}x");
}
