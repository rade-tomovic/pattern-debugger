// Evidence for /systems/threads-and-scheduling/threadpool-starvation/ — run with:
//   dotnet run bench/threads-and-scheduling/threadpool-starvation.cs blocking
//   dotnet run bench/threads-and-scheduling/threadpool-starvation.cs async
//
// 300 concurrent "requests", each needing 20 ms of I/O that is already async.
// mode=blocking reaches it through a synchronous facade; mode=async awaits it.
// Every sample below is a count (pool threads, queue length, requests done) taken
// at a fixed number of evenly spaced checks — never a clock reading.
static class Service
{
    // ── the code under test ──────────────────────────────────────────────────
    // The I/O. Async all the way down, as every modern client library is.
    static async Task<int> QueryAsync(int id)
    {
        await Task.Delay(20);                       // stands in for the database round trip
        return id * 2;
    }

    // The synchronous facade somebody added so that callers did not have to change.
    static int Query(int id) => QueryAsync(id).GetAwaiter().GetResult();

    // The request handler. Runs on a thread-pool thread, exactly as in ASP.NET Core.
    static int Handle(int id) => Query(id) + 1;

    // The same handler, written the other way.
    static async Task<int> HandleAsync(int id) => await QueryAsync(id) + 1;

    // ── the harness ──────────────────────────────────────────────────────────
    const int Requests = 300;

    static async Task Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "blocking";
        // Pin the starting pool size the way a small container does (a host with many
        // cores, a pod limited to a few) so the shape of the incident does not depend
        // on how many cores happen to be under this run.
        ThreadPool.SetMinThreads(4, 4);
        int completed = 0;
        var stop = new ManualResetEventSlim();
        var samples = new List<string>();

        // The sampler runs on its own dedicated OS thread, so that the starving pool
        // cannot starve the measurement of itself.
        var sampler = new Thread(() =>
        {
            int check = 0;
            while (!stop.IsSet)
            {
                check++;
                samples.Add($"  check {check,2}   pool threads {ThreadPool.ThreadCount,3}   "
                            + $"queued {ThreadPool.PendingWorkItemCount,4}   done {Volatile.Read(ref completed),3}/{Requests}");
                stop.Wait(200);
            }
        }) { IsBackground = true };
        sampler.Start();

        var requests = new Task[Requests];
        for (int i = 0; i < Requests; i++)
        {
            int id = i;
            requests[i] = Task.Run(async () =>
            {
                _ = mode == "blocking" ? Handle(id) : await HandleAsync(id);
                Interlocked.Increment(ref completed);
            });
        }
        await Task.WhenAll(requests);
        stop.Set(); sampler.Join(2000);

        Console.WriteLine($"mode={mode}   requests={Requests}   cores={Environment.ProcessorCount}");
        foreach (var line in samples) Console.WriteLine(line);
        Console.WriteLine($"  pool threads at end {ThreadPool.ThreadCount}   ({samples.Count} checks to drain the queue)");
    }
}
