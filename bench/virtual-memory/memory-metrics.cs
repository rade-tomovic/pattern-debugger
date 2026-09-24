// The same process at five moments, measured six ways. Every number is in MiB.
// The point is that "memory used" is at least four different quantities.
using System.Diagnostics;
using System.Runtime;

static long Kb(string key)
{
    foreach (string l in File.ReadLines("/proc/self/status"))
        if (l.StartsWith(key)) return long.Parse(l.Split(':')[1].Trim().Split(' ')[0]);
    return -1;
}

static void Row(string moment)
{
    GCMemoryInfo gc = GC.GetGCMemoryInfo();          // describes the LAST collection, not now
    Process p = Process.GetCurrentProcess(); p.Refresh();
    Console.WriteLine(
        $"{moment,-30} {Kb("VmSize:") / 1024,9:N0} {Kb("VmRSS:") / 1024,8:N0} " +
        $"{p.WorkingSet64 / 1048576,10:N0} {GC.GetTotalMemory(false) / 1048576,9:N0} " +
        $"{gc.HeapSizeBytes / 1048576,9:N0} {gc.TotalCommittedBytes / 1048576,9:N0}");
}

// Everything the GC could still see lives inside this method, so it is genuinely
// unreachable by the time the collection below runs.
static void AllocateTouchAndDrop()
{
    var arrays = new List<byte[]>();
    for (int i = 0; i < 8; i++) arrays.Add(new byte[1024L * 1024 * 1024 - 64]);   // 8 GiB claimed
    Row("allocated 8 GiB, untouched");

    foreach (byte[] a in arrays)
        for (long i = 0; i < a.LongLength; i += 4096) a[i] = 1;                   // one byte per page
    Row("touched one byte per page");
}

Console.WriteLine($"{"moment",-30} {"VmSize",9} {"VmRSS",8} {"WorkingSet",10} {"GC.Total",9} {"gc.Heap",9} {"gc.Commit",9}");
Row("startup");
AllocateTouchAndDrop();

GC.Collect(2, GCCollectionMode.Forced, true, true);
GC.WaitForPendingFinalizers();
Row("dropped + blocking gen2 GC");

GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
GC.Collect(2, GCCollectionMode.Forced, true, true);
Thread.Sleep(2000);
Row("+ LOH compaction, 2 s later");

GCMemoryInfo info = GC.GetGCMemoryInfo();
Console.WriteLine($"\nTotalAvailableMemoryBytes {info.TotalAvailableMemoryBytes / 1048576:N0} MiB   " +
                  $"MemoryLoadBytes {info.MemoryLoadBytes / 1048576:N0} MiB   " +
                  $"mapped regions {File.ReadLines("/proc/self/maps").Count():N0}");
