// Evidence for /systems/threads-and-scheduling/ — run with:
//   dotnet run bench/threads-and-scheduling/index.cs
//
// Two questions: which thread actually ran my code, and what a parked thread costs
// in memory versus a pending await that has no thread behind it at all.
using System.Diagnostics;

static class Bench
{
    static string Who(string label)
    {
        var t = Thread.CurrentThread;
        return $"  {label,-44} tid {Environment.CurrentManagedThreadId,3}  pool={t.IsThreadPoolThread,-5} background={t.IsBackground,-5} name={t.Name ?? "(none)"}";
    }
    static (long rss, long vsz) Mem()
    {
        long r = 0, v = 0;
        foreach (var l in File.ReadLines("/proc/self/status"))
        {
            if (l.StartsWith("VmRSS:")) r = long.Parse(l.Split(':')[1].Trim().Split(' ')[0]);
            if (l.StartsWith("VmSize:")) v = long.Parse(l.Split(':')[1].Trim().Split(' ')[0]);
        }
        return (r, v);
    }
    static int OsThreads() => Directory.GetDirectories("/proc/self/task").Length;

    // ── part 1: which thread is this? ────────────────────────────────────────
    static async Task Identity()
    {
        ThreadPool.GetMinThreads(out int minW, out int minIo);
        ThreadPool.GetMaxThreads(out int maxW, out int maxIo);
        Console.WriteLine($"=== 1. who runs what ===  cores {Environment.ProcessorCount}, "
                          + $"pool min {minW}/{minIo}, max {maxW}/{maxIo}, live pool threads {ThreadPool.ThreadCount}");
        Console.WriteLine(Who("Main"));

        var t = new Thread(() => Console.WriteLine(Who("new Thread(...).Start()")));
        t.Start(); t.Join();

        var poolTid = 0;
        await Task.Run(() => { poolTid = Environment.CurrentManagedThreadId; Console.WriteLine(Who("Task.Run body")); });

        async Task Inner()
        {
            Console.WriteLine(Who("async method, before the await"));
            await Task.Delay(20);
            Console.WriteLine(Who("async method, after await Task.Delay"));
        }
        await Inner();

        // a Task that never touches a thread other than this one
        var tcs = new TaskCompletionSource<int>();
        var waiting = tcs.Task;
        Console.WriteLine($"  a TaskCompletionSource task: IsCompleted={waiting.IsCompleted} while nothing at all is running it");
        tcs.SetResult(7);
        Console.WriteLine($"  after SetResult on tid {Environment.CurrentManagedThreadId}: IsCompleted={waiting.IsCompleted}, Result={waiting.Result}");
        Console.WriteLine($"  pool threads created so far: {ThreadPool.ThreadCount}; work items completed: {ThreadPool.CompletedWorkItemCount}");
    }

    // ── part 2: a pending wait, with and without a thread under it ──────────
    // Not a race for speed — a census of what each shape actually holds: an OS
    // thread reserves address space and stays resident whether or not it is doing
    // anything, a pending await is a heap object with no thread at all.
    static void PendingWaits()
    {
        const int N = 1000;
        Console.WriteLine($"\n=== 2. {N} pending waits, two ways ===");
        var b0 = Mem();
        Console.WriteLine($"  baseline            RSS {b0.rss,8} KiB   VSZ {b0.vsz,10} KiB   OS threads {OsThreads(),5}");

        var gate = new ManualResetEventSlim(false);
        var ts = new Thread[N];
        for (int i = 0; i < N; i++) { ts[i] = new Thread(() => gate.Wait()) { IsBackground = true }; ts[i].Start(); }
        Thread.Sleep(300);
        var a1 = Mem();
        Console.WriteLine($"  {N} threads parked   RSS {a1.rss,8} KiB   VSZ {a1.vsz,10} KiB   OS threads {OsThreads(),5}");
        Console.WriteLine($"    per parked thread: {(a1.rss - b0.rss) * 1024.0 / N,8:F0} B resident, {(a1.vsz - b0.vsz) / 1024.0 / N,5:F1} MiB of address space reserved");
        gate.Set();
        foreach (var t in ts) t.Join();
        Thread.Sleep(300);
        var c0 = Mem();

        long g0 = GC.GetTotalAllocatedBytes();
        var cts = new CancellationTokenSource();
        var tasks = new Task[N];
        for (int i = 0; i < N; i++) tasks[i] = Task.Delay(Timeout.Infinite, cts.Token);
        Thread.Sleep(300);
        var d0 = Mem();
        Console.WriteLine($"  {N} pending awaits   RSS {d0.rss,8} KiB   VSZ {d0.vsz,10} KiB   OS threads {OsThreads(),5}");
        Console.WriteLine($"    per pending await: {(d0.rss - c0.rss) * 1024.0 / N,8:F0} B resident, {(GC.GetTotalAllocatedBytes() - g0) / (double)N,5:F0} B managed, no thread");
        cts.Cancel();
    }

    static async Task Main()
    {
        await Identity();
        PendingWaits();
    }
}
