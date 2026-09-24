// --- check ---
string before = MemoryFacts.Snapshot();
byte[] a = new byte[64 * 1024 * 1024];
for (int i = 0; i < a.Length; i += 4096) a[i] = 1;
string after = MemoryFacts.Snapshot();
Console.WriteLine(before);
Console.WriteLine(after);
if (!before.Contains("resident") || !after.Contains("major")) throw new Exception("FAIL shape");
long ResidentOf(string s) => long.Parse(s.Split('|')[1].Split(' ')[2].Replace(",", ""));
if (ResidentOf(after) - ResidentOf(before) < 60) throw new Exception($"FAIL resident did not grow: {ResidentOf(before)} -> {ResidentOf(after)}");
Console.WriteLine("PASS");

// Four numbers, from the two places that actually know them. Linux only:
// /proc is where the operating system keeps the truth about your process.
public static class MemoryFacts
{
    public static string Snapshot()
    {
        long virtualKb = FromStatus("VmSize:"), residentKb = FromStatus("VmRSS:");
        (long minor, long major) = Faults();
        long gcHeap = GC.GetTotalMemory(forceFullCollection: false);

        return $"virtual {virtualKb / 1024:N0} MiB | resident {residentKb / 1024:N0} MiB | " +
               $"gc heap {gcHeap / 1048576:N0} MiB | faults {minor:N0} minor, {major:N0} major";
    }

    static long FromStatus(string key)
    {
        foreach (string line in File.ReadLines("/proc/self/status"))
            if (line.StartsWith(key, StringComparison.Ordinal))
                return long.Parse(line.Split(':')[1].Trim().Split(' ')[0]);   // kB
        return -1;
    }

    // /proc/self/stat: field 10 is minflt, field 12 is majflt. Field 2 is the command
    // name and may contain spaces and brackets, so parse after the LAST ')'.
    static (long minor, long major) Faults()
    {
        string s = File.ReadAllText("/proc/self/stat");
        string[] f = s[(s.LastIndexOf(')') + 2)..].Split(' ');
        return (long.Parse(f[7]), long.Parse(f[9]));
    }
}

