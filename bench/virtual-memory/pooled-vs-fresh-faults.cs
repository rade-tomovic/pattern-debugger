// Is ArrayPool a page-fault optimization, or only an allocation one?
// Two request loops, each in its own process so neither inherits the other's
// warm pages. Both hand out a 64 MiB buffer, write one byte to every page,
// and give it back. Only the kernel's minor-fault counter is reported --
// no timing, so this file makes no claim that needs a stopwatch.
//
//   dotnet run pooled-vs-fresh-faults.cs -- fresh
//   dotnet run pooled-vs-fresh-faults.cs -- pooled
using System.Buffers;

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
bool pooled = args.Length > 0 && args[0] == "pooled";

Console.WriteLine(pooled ? "ArrayPool.Shared.Rent / Return" : "new byte[64 MiB] / drop / blocking gen2 GC");

for (int round = 0; round < 6; round++)
{
    long f0 = Minflt();

    byte[] a = pooled ? ArrayPool<byte>.Shared.Rent(N) : new byte[N];
    for (int i = 0; i < N; i += 4096) a[i] = 1;

    long f1 = Minflt();
    Console.WriteLine($"round {round}: minor faults {f1 - f0,7:N0}   RSS {RssMiB(),4:N0} MiB");

    if (pooled)
    {
        ArrayPool<byte>.Shared.Return(a);
    }
    else
    {
        a = null!;
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
    }
}
