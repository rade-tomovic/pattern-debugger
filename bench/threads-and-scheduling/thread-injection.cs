// Evidence for /systems/threads-and-scheduling/ — run with:
//   dotnet run bench/threads-and-scheduling/thread-injection.cs event
//   dotnet run bench/threads-and-scheduling/thread-injection.cs task
//
// 60 work items that block for ever, sampled at a fixed number of evenly spaced
// checks. "event" blocks on a ManualResetEventSlim, which the pool cannot see
// into; "task" blocks on an incomplete Task with .GetAwaiter().GetResult(),
// which the runtime recognises as a blocked thread. Same starvation, two
// injection behaviors — the point is the shape (one at a time vs. several at
// once), not how many wall-clock seconds either one takes.
string mode = args.Length > 0 ? args[0] : "event";
var gate = new ManualResetEventSlim(false);
var never = new TaskCompletionSource();

for (int i = 0; i < 60; i++)
{
    if (mode == "event") ThreadPool.UnsafeQueueUserWorkItem<object?>(_ => gate.Wait(), null, false);
    else                 ThreadPool.UnsafeQueueUserWorkItem<object?>(_ => never.Task.GetAwaiter().GetResult(), null, false);
}

Console.WriteLine($"mode={mode}  60 work items blocked  cores={Environment.ProcessorCount}");
for (int check = 1; check <= 11; check++)
{
    Thread.Sleep(1000);   // paces the checks; the elapsed time itself is not the point
    Console.WriteLine($"  check {check,2}   pool threads {ThreadPool.ThreadCount,3}   still queued {ThreadPool.PendingWorkItemCount,3}");
}
gate.Set();
never.SetResult();
