#:property AllowUnsafeBlocks=true
// Touch one byte in every 4 KiB page of a 256 MiB region, four ways. The work is
// identical in all four: 65,536 single-byte accesses, 4096 bytes apart. The only thing
// that changes is what the page tables already say about those pages before the walk
// starts. No timing here on purpose — the question this file answers is "how many times
// does the kernel get involved, and does it do the same thing every time", and both of
// those are counts, not durations.
using System.Runtime.InteropServices;

const int Page = 4096;
const int Pages = 64 * 1024;               // 65,536 pages
const nuint Bytes = (nuint)Pages * Page;   // 256 MiB

// /proc/self/stat field 10 is minflt, field 12 is majflt. comm (field 2) can hold
// spaces and brackets, so start parsing after the last ')'.
static (long min, long maj) Faults()
{
    string s = File.ReadAllText("/proc/self/stat");
    string[] f = s[(s.LastIndexOf(')') + 2)..].Split(' ');
    return (long.Parse(f[7]), long.Parse(f[9]));
}

// Resident set: how much of this process is actually in physical RAM right now.
static long RssKb()
{
    foreach (string l in File.ReadLines("/proc/self/status"))
        if (l.StartsWith("VmRSS:")) return long.Parse(l.Split(':')[1].Trim().Split(' ')[0]);
    return -1;
}

long sink = 0;   // consumes the read pass so nothing can be optimized away

unsafe
{
    // NativeMemory.Alloc of 256 MiB goes straight to mmap, and NativeMemory.Free
    // munmaps it — so every walk below starts from page tables that genuinely say
    // "nothing here", the same guarantee page-fault-walk-recycled.cs shows a plain
    // new byte[] / drop / GC loop stops giving you after round 2.

    // A — first write touch of a fresh region
    byte* p = (byte*)NativeMemory.Alloc(Bytes);
    long minA0 = Faults().min, rssA0 = RssKb();
    for (int i = 0; i < Pages; i++) p[(nuint)i * Page] = 1;
    long minA1 = Faults().min, rssA1 = RssKb();

    // B — second write touch of the SAME region, same instructions
    long minB0 = minA1, rssB0 = rssA1;
    for (int i = 0; i < Pages; i++) p[(nuint)i * Page] = 2;
    long minB1 = Faults().min, rssB1 = RssKb();
    NativeMemory.Free(p);

    // C — first READ touch of a second, fresh region
    byte* q = (byte*)NativeMemory.Alloc(Bytes);
    long minC0 = Faults().min, rssC0 = RssKb();
    long s = 0;
    for (int i = 0; i < Pages; i++) s += q[(nuint)i * Page];
    long minC1 = Faults().min, rssC1 = RssKb();
    sink += s;

    // D — now WRITE to the pages C just read
    long minD0 = minC1, rssD0 = rssC1;
    for (int i = 0; i < Pages; i++) q[(nuint)i * Page] = 3;
    long minD1 = Faults().min, rssD1 = RssKb();
    NativeMemory.Free(q);

    void Line(string name, long f0, long f1, long rss0, long rss1) =>
        Console.WriteLine($"{name,-32} minflt +{f1 - f0,7:N0}   VmRSS {rss0,9:N0} -> {rss1,9:N0} kB ({rss1 - rss0,+9:N0})");

    Line("A first touch, write", minA0, minA1, rssA0, rssA1);
    Line("B second touch, write", minB0, minB1, rssB0, rssB1);
    Line("C first touch, read", minC0, minC1, rssC0, rssC1);
    Line("D write after the read pass", minD0, minD1, rssD0, rssD1);
    Console.WriteLine($"\nmajor faults so far: {Faults().maj}, sink {sink}");

    if (minA1 - minA0 < Pages) throw new Exception($"FAIL: A should fault every page, got {minA1 - minA0}");
    if (minB1 - minB0 > 10) throw new Exception($"FAIL: B should barely fault, got {minB1 - minB0}");
    if (minC1 - minC0 < Pages) throw new Exception($"FAIL: C should fault every page, got {minC1 - minC0}");
    if (minD1 - minD0 < Pages) throw new Exception($"FAIL: D should fault every page again, got {minD1 - minD0}");
    if (rssD1 - rssD0 < 250_000) throw new Exception("FAIL: D should grow the resident set by roughly a whole region, same as A did");
    Console.WriteLine("PASS");
}
