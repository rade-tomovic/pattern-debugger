// One .NET process that has done nothing, described by every counter this page uses.
// Everything /proc knows is captured FIRST, in four reads back to back, and printed
// afterwards — so all of it describes the same process at the same instant.
string status = File.ReadAllText("/proc/self/status");
string rollup = File.ReadAllText("/proc/self/smaps_rollup");
string[] maps = File.ReadAllLines("/proc/self/maps");
long gcHeap = GC.GetTotalMemory(false);

static long Kb(string blob, string key)
{
    foreach (string l in blob.Split('\n'))
        if (l.StartsWith(key, StringComparison.Ordinal))
            return long.Parse(l.Split(':')[1].Trim().Split(' ')[0]);
    return -1;
}

// A maps line is "start-end perms offset dev inode path".
static (ulong start, ulong end, string perms, string path) Parse(string l)
{
    string[] f = l.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    string[] r = f[0].Split('-');
    return (Convert.ToUInt64(r[0], 16), Convert.ToUInt64(r[1], 16), f[1], f.Length >= 6 ? f[5] : "");
}

Console.WriteLine("--- the four answers to \"how much memory\", one process, one instant ---");
Console.WriteLine($"VmSize            {Kb(status, "VmSize:") / 1024,9:N0} MiB");
Console.WriteLine($"VmRSS             {Kb(status, "VmRSS:") / 1024,9:N0} MiB");
Console.WriteLine($"Pss               {Kb(rollup, "Pss:") / 1024,9:N0} MiB");
Console.WriteLine($"GC.GetTotalMemory {gcHeap / 1048576,9:N0} MiB");

Console.WriteLine("\n--- the runtime, mapped rather than read ---");
long coreLib = 0;
foreach (string l in maps)
{
    (ulong start, ulong end, string perms, string path) = Parse(l);
    if (!path.EndsWith("System.Private.CoreLib.dll", StringComparison.Ordinal) &&
        !path.EndsWith("libcoreclr.so", StringComparison.Ordinal)) continue;
    if (path.EndsWith("System.Private.CoreLib.dll", StringComparison.Ordinal)) coreLib += (long)(end - start);
    Console.WriteLine($"{l}   [{(end - start) / 1048576.0:F1} MiB]");
}
Console.WriteLine($"CoreLib mapped total {coreLib / 1048576.0:F1} MiB");

Console.WriteLine("\n--- the two biggest reservations (no permissions at all) ---");
foreach (var m in maps.Select(Parse).Where(m => m.perms.StartsWith("---"))
                      .OrderByDescending(m => m.end - m.start).Take(2))
    Console.WriteLine($"{m.start:x}-{m.end:x} {m.perms}   {(m.end - m.start) / 1048576.0,9:N0} MiB");

Console.WriteLine("\n--- /proc/self/smaps_rollup ---");
foreach (string l in rollup.Split('\n'))
    if (l.StartsWith("Rss:") || l.StartsWith("Pss:") || l.StartsWith("Shared_C") ||
        l.StartsWith("Shared_D") || l.StartsWith("Private_C") || l.StartsWith("Private_D") ||
        l.StartsWith("Anonymous:"))
        Console.WriteLine(l);
