// Evidence for /systems/threads-and-scheduling/ — run with:
//   dotnet run bench/threads-and-scheduling/unix-file-async.cs -c Release
//
// Is FileStream.ReadAsync on Linux a kernel completion, or pool work? Clamp the
// pool to one worker, block that worker, and ask for 20 MB. If the read needed
// only a kernel registration it would complete anyway. It does not.
var path = Path.GetTempFileName();
File.WriteAllBytes(path, new byte[20 * 1024 * 1024]);

ThreadPool.SetMinThreads(1, 1);
ThreadPool.SetMaxThreads(1, 1);

var block = new ManualResetEventSlim();
ThreadPool.UnsafeQueueUserWorkItem(_ => block.Wait(), null);   // occupy the only worker
Thread.Sleep(300);

using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
var buf = new byte[20 * 1024 * 1024];
var t = fs.ReadAsync(buf, 0, buf.Length);
Console.WriteLine($"20 MB ReadAsync completed with the single pool worker blocked? {t.Wait(2000)}");
ThreadPool.GetAvailableThreads(out int w, out _);
Console.WriteLine($"pool max workers 1, available {w}, pending work items {ThreadPool.PendingWorkItemCount}");
block.Set();
Console.WriteLine($"after releasing the worker, read completed? {t.Wait(5000)}");
File.Delete(path);
