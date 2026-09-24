// What `new byte[256 MiB]` actually costs the operating system, in page faults and
// resident bytes, at each of four moments. No timing here — just the counters.
// RSS is reported in kB, not MiB: the interesting steps move it by kilobytes and
// integer-MiB rounding would invent a megabyte that never happened. VmPTE is the
// kernel's count of the page-table pages this process owns — memory that is NOT
// part of VmRSS, which is why the read pass can build 512 KiB of leaf entries
// without the resident set noticing.
static (long min, long maj) Faults()
{
    string s = File.ReadAllText("/proc/self/stat");           // field 10 = minflt, 12 = majflt
    string[] f = s[(s.LastIndexOf(')') + 2)..].Split(' ');    // comm can contain spaces
    return (long.Parse(f[7]), long.Parse(f[9]));
}
static long StatusKb(string key)
{
    foreach (string l in File.ReadLines("/proc/self/status"))
        if (l.StartsWith(key, StringComparison.Ordinal))
            return long.Parse(l.Split(':')[1].Trim().Split(' ')[0]);
    return -1;
}

const int N = 256 * 1024 * 1024;      // 65,536 pages of 4 KiB
var f = Faults(); long rss = StatusKb("VmRSS:"), pte = StatusKb("VmPTE:");
void Step(string what)
{
    var now = Faults(); long r = StatusKb("VmRSS:"), p = StatusKb("VmPTE:");
    Console.WriteLine($"{what,-34} minor +{now.min - f.min,7:N0}  major +{now.maj - f.maj,3}  " +
                      $"VmRSS {rss,9:N0} -> {r,9:N0} kB ({r - rss,+9:N0})  VmPTE {pte,6:N0} -> {p,6:N0} kB");
    f = now; rss = r; pte = p;
}

Console.WriteLine($"process at rest: VmSize {StatusKb("VmSize:") / 1024:N0} MiB, VmRSS {StatusKb("VmRSS:"):N0} kB, VmPTE {StatusKb("VmPTE:"):N0} kB\n");

byte[] a = new byte[N];
Step("new byte[256 MiB]");

long sum = 0;
for (int i = 0; i < N; i += 4096) sum += a[i];
Step("read one byte per page");

for (int i = 0; i < N; i += 4096) a[i] = 1;
Step("write one byte per page");

for (int i = 0; i < N; i += 4096) a[i] = 2;
Step("write the same bytes again");

Console.WriteLine($"\nVmSize {StatusKb("VmSize:") / 1024:N0} MiB, VmRSS {StatusKb("VmRSS:"):N0} kB, VmPTE {StatusKb("VmPTE:"):N0} kB, sum of the read pass {sum}");

// Act 2: the read pass again, on a second fresh array, in eighths, with the runtime warm.
// The four steps above run while the JIT is still tiering these loops, which puts a few
// hundred kB of the runtime's own memory into the first read pass's VmRSS delta and makes
// it look as though read-touching a page costs something. Split the pass up and the steady
// state is exact: read-touching 8,192 pages moves VmRSS by tens of kB (a mapping to the
// shared zero page is not counted as resident at all) and VmPTE by exactly 64 kB, which is
// 8 bytes of leaf page-table entry per page.
byte[] b = new byte[N];
long pr = StatusKb("VmRSS:"), pp = StatusKb("VmPTE:");
Console.WriteLine("\nwarm runtime, second array, read one byte per page, in eighths of 8,192 pages:");
for (int q = 0; q < 8; q++)
{
    for (int i = q * (N / 8); i < (q + 1) * (N / 8); i += 4096) sum += b[i];
    long r = StatusKb("VmRSS:"), p = StatusKb("VmPTE:");
    Console.WriteLine($"  eighth {q}   VmRSS +{r - pr,6:N0} kB   VmPTE +{p - pp,5:N0} kB");
    pr = r; pp = p;
}
Console.WriteLine($"sum {sum}");
