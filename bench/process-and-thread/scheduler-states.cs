// Part 1: the three thread states, read straight out of /proc.
// Part 2: what preemption looks like when there are more runnable threads than cores.
using System.Diagnostics;

static string State(string taskDir) => File.ReadAllText($"{taskDir}/stat").Split(") ")[1][0].ToString();
static string Name(string taskDir) => File.ReadAllText($"{taskDir}/comm").Trim();
static long Field(string taskDir, string key)
{
    foreach (var line in File.ReadAllLines($"{taskDir}/status"))
        if (line.StartsWith(key)) return long.Parse(line.Split('\t')[^1]);
    return 0;
}

var gate = new SemaphoreSlim(0);
var stopStates = false;
new Thread(() => { while (!Volatile.Read(ref stopStates)) { } }) { Name = "spinner", IsBackground = true }.Start();
new Thread(() => gate.Wait())                                   { Name = "waiter",  IsBackground = true }.Start();
new Thread(() => Thread.Sleep(60_000))                          { Name = "sleeper", IsBackground = true }.Start();
Thread.Sleep(300);

Console.WriteLine("part 1 — thread states (R = on or waiting for a CPU, S = blocked)");
foreach (var dir in Directory.GetDirectories("/proc/self/task"))
    if (Name(dir) is "spinner" or "waiter" or "sleeper")
        Console.WriteLine($"  tid {Path.GetFileName(dir),-6} {Name(dir),-8} state {State(dir)}");
stopStates = true;

int n = Environment.ProcessorCount * 2;   // deliberately oversubscribed: twice as many runnable
                                           // CPU-bound threads as this box has cores
var stop = false;
var threads = new List<Thread>();
for (int i = 0; i < n; i++)
{
    var t = new Thread(() => { long x = 0; while (!Volatile.Read(ref stop)) x++; }) { Name = $"spin{i}", IsBackground = true };
    threads.Add(t);
    t.Start();
}
Thread.Sleep(2000);      // a fixed sampling window — its length is not itself the point
long vol = 0, invol = 0;
foreach (var dir in Directory.GetDirectories("/proc/self/task"))
{
    if (!Name(dir).StartsWith("spin")) continue;
    vol += Field(dir, "voluntary_ctxt_switches:");
    invol += Field(dir, "nonvoluntary_ctxt_switches:");
}
stop = true;
foreach (var t in threads) t.Join();

Console.WriteLine();
Console.WriteLine($"part 2 — {n} CPU-bound threads on {Environment.ProcessorCount} cores (2x oversubscribed), sampled over a fixed window");
Console.WriteLine($"  voluntary switches (gave the CPU up)   : {vol}");
Console.WriteLine($"  involuntary switches (CPU taken away)  : {invol}");
