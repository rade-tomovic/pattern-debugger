// The control that explains why page-fault-walk.cs does not use `new byte[]`.
// Five rounds of: allocate 256 MiB, walk it, drop it, force a full collection.
// If the GC hands back memory that is already resident, the "first touch" of the
// next round is not a first touch at all — and the minor-fault count is the proof,
// with no clock needed to see it.
using System.Runtime;

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

const int N = 256 * 1024 * 1024;    // 65,536 pages

for (int round = 0; round < 5; round++)
{
    byte[]? a = new byte[N];
    long f0 = Minflt();
    for (int i = 0; i < N; i += 4096) a[i] = 1;
    long f1 = Minflt();

    Console.WriteLine($"round {round}: first touch minor faults {f1 - f0,7:N0}   RSS {RssMiB(),4:N0} MiB");

    a = null;
    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
    GC.Collect(2, GCCollectionMode.Forced, true, true);
    GC.WaitForPendingFinalizers();
}
