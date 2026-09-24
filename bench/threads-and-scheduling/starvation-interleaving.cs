// Evidence for /systems/threads-and-scheduling/threadpool-starvation/ — run with:
//   dotnet run bench/threads-and-scheduling/starvation-interleaving.cs clamped
//   dotnet run bench/threads-and-scheduling/starvation-interleaving.cs default
//
// The same bug, shrunk until every step fits in a table. "clamped" pins the pool to
// two workers and sends three requests, so injection cannot rescue it and a watchdog
// has to end the run. "default" leaves the pool alone and lets it recover. Every line
// gets the next number off one shared counter, so the interleaving across threads is
// exactly the order the runtime produced it in — not a clock reading.
using System.Collections.Concurrent;

string mode = args.Length > 0 ? args[0] : "clamped";
int requests = mode == "clamped" ? 3 : 6;

var log = new ConcurrentQueue<string>();
int step = 0;
void Log(string what) => log.Enqueue($"  step {Interlocked.Increment(ref step),2}  tid {Environment.CurrentManagedThreadId,3}  "
                                     + $"[pool threads {ThreadPool.ThreadCount}, queued {ThreadPool.PendingWorkItemCount}]  {what}");

if (mode == "clamped") { ThreadPool.SetMinThreads(2, 1); ThreadPool.SetMaxThreads(2, 1); }

// the watchdog: this bug does not end on its own, so something has to call time
var watchdog = new Thread(() =>
{
    Thread.Sleep(3000);   // long enough that "clamped" is provably stuck, not just slow
    Log("WATCHDOG: no progress — printing the log and exiting");
    foreach (var line in log) Console.WriteLine(line);
    Console.WriteLine($"  never finished: {requests} requests, pool threads {ThreadPool.ThreadCount}, queued {ThreadPool.PendingWorkItemCount}");
    Environment.Exit(0);
}) { IsBackground = true };
watchdog.Start();

async Task<int> QueryAsync(int id)
{
    Log($"request {id}: QueryAsync starts, awaiting the I/O");
    await Task.Delay(20);
    Log($"request {id}: continuation RESUMED — this needed a free pool thread");
    return id * 2;
}
int Query(int id)
{
    Log($"request {id}: blocking this pool thread in .GetAwaiter().GetResult()");
    int r = QueryAsync(id).GetAwaiter().GetResult();
    Log($"request {id}: unblocked");
    return r;
}

var tasks = new Task[requests];
for (int i = 0; i < requests; i++)
{
    int id = i;
    tasks[i] = Task.Run(() => { Log($"request {id}: handler starts"); Query(id); Log($"request {id}: handler done"); });
}
Log($"all {requests} requests queued");
await Task.WhenAll(tasks);
Log($"all {requests} requests complete");
foreach (var line in log) Console.WriteLine(line);
