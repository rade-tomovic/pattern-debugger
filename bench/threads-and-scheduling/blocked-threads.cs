// Evidence for /systems/threads-and-scheduling/threadpool-starvation/ — run with:
//   dotnet run bench/threads-and-scheduling/blocked-threads.cs -c Release
//
// Twenty work items that block on an async call, then a census of every OS thread
// in the process taken from /proc/self/task: its name, its Linux scheduler state,
// and the kernel function it is parked in.
static async Task<int> QueryAsync(int id) { await Task.Delay(2000); return id; }

for (int i = 0; i < 20; i++)
    ThreadPool.UnsafeQueueUserWorkItem<object?>(_ => QueryAsync(0).GetAwaiter().GetResult(), null, false);

Thread.Sleep(1500);
Console.WriteLine($"pool threads {ThreadPool.ThreadCount}, work items still queued {ThreadPool.PendingWorkItemCount}");

var census = new Dictionary<string, int>();
foreach (var dir in Directory.GetDirectories("/proc/self/task"))
{
    string comm = File.ReadAllText(Path.Combine(dir, "comm")).Trim();
    string stat = File.ReadAllText(Path.Combine(dir, "stat"));
    char state = stat[(stat.LastIndexOf(')') + 2)];              // the field after the (comm) parens
    string wchan = File.ReadAllText(Path.Combine(dir, "wchan")).Trim();
    string key = $"{comm,-18} state {state}   parked in {wchan}";
    census[key] = census.GetValueOrDefault(key) + 1;
}
foreach (var row in census.OrderByDescending(r => r.Value)) Console.WriteLine($"  {row.Value,3} x  {row.Key}");
