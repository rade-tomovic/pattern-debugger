// Evidence for /systems/threads-and-scheduling/ — run with:
//   dotnet run bench/threads-and-scheduling/sync-context.cs -c Release
//
// A SynchronizationContext with exactly one thread behind it — the shape WinForms,
// WPF and ASP.NET (the .NET Framework one) all had. Part 1 shows where each
// continuation lands. Part 2 blocks that one thread on .Result and the whole thing
// stops, with no lock in sight.
using System.Collections.Concurrent;

static string Who() => $"tid {Environment.CurrentManagedThreadId,2}   context {SynchronizationContext.Current?.GetType().Name ?? "none"}";

static async Task<int> LoadAsync()
{
    Console.WriteLine($"  LoadAsync    before await : {Who()}");
    await Task.Delay(50);                              // captures the current context
    Console.WriteLine($"  LoadAsync    after  await : {Who()}");
    return 1;
}

static async Task<int> LoadFreeAsync()
{
    Console.WriteLine($"  LoadFree     before await : {Who()}");
    await Task.Delay(50).ConfigureAwait(false);        // explicitly does not capture it
    Console.WriteLine($"  LoadFree     after  await : {Who()}");
    return 2;
}

var ctx = new OneThreadContext();
var ui = new Thread(() => { SynchronizationContext.SetSynchronizationContext(ctx); ctx.RunLoop(); })
         { IsBackground = true, Name = "UI" };
ui.Start();

Console.WriteLine("=== 1. where the continuation runs ===");
var done = new ManualResetEventSlim();
ctx.Post(async _ =>
{
    Console.WriteLine($"  callback on the UI thread: {Who()}");
    await LoadAsync();
    await LoadFreeAsync();
    done.Set();
}, null);
done.Wait();

Console.WriteLine("\n=== 2. the same LoadAsync(), blocked on with .Result ===");
var started = new ManualResetEventSlim();
var finished = new ManualResetEventSlim();
ctx.Post(_ =>
{
    started.Set();
    int v = LoadAsync().Result;                        // the continuation needs this very thread
    Console.WriteLine($"  got {v}");
    finished.Set();
}, null);
started.Wait();
bool ok = finished.Wait(3000);   // just an upper bound so the process does not hang forever
Console.WriteLine($"  did it ever complete? {ok}");
Console.WriteLine($"  UI thread state: {ui.ThreadState};   continuations waiting for the UI thread: {ctx.Queued}");

sealed class OneThreadContext : SynchronizationContext
{
    readonly BlockingCollection<(SendOrPostCallback cb, object? state)> _q = new();
    public override void Post(SendOrPostCallback d, object? state) => _q.Add((d, state));
    public void RunLoop() { foreach (var (cb, state) in _q.GetConsumingEnumerable()) cb(state); }
    public int Queued => _q.Count;
}
