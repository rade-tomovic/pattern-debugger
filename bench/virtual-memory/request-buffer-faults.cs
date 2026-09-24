// Does `new byte[64 MiB]` per request really cost 16,384 page faults per request?
// Simulate the request loop: allocate 64 MiB, touch every page, drop it, collect.
// Report the kernel's own minor-fault counter for each round.
using System.Diagnostics;

static long Minflt()
{
    string s = File.ReadAllText("/proc/self/stat");
    return long.Parse(s[(s.LastIndexOf(')') + 2)..].Split(' ')[7]);
}
static long RssMiB()
{
    foreach (string l in File.ReadLines("/proc/self/status"))
        if (l.StartsWith("VmRSS:")) return long.Parse(l.Split(':')[1].Trim().Split(' ')[0]) / 1024;
    return -1;
}

const int N = 64 * 1024 * 1024;     // 16,384 pages

for (int round = 0; round < 6; round++)
{
    long f0 = Minflt();
    byte[]? a = new byte[N];
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < N; i += 4096) a[i] = 1;
    double ns = sw.Elapsed.TotalNanoseconds / (N / 4096);
    long f1 = Minflt();

    Console.WriteLine($"round {round}: allocate + touch 64 MiB  minor faults {f1 - f0,7:N0}   {ns,7:F1} ns/page   RSS {RssMiB(),4:N0} MiB");

    a = null;
    GC.Collect(2, GCCollectionMode.Forced, true, true);
    GC.WaitForPendingFinalizers();
}
