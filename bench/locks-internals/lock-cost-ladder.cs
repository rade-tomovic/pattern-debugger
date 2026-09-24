// Evidence for /systems/locks-internals/lock-cost-ladder/ ("The Fast Path and
// the Slow Path") — run with:
//   dotnet run bench/locks-internals/lock-cost-ladder.cs -c Release
// Six ways to add one to a shared long, at one, two and four threads, doing
// the same total work every time. No Stopwatch anywhere on this page: the
// only outputs are correctness (does the final count match what it should)
// and Monitor.LockContentionCount — an exact, runtime-maintained count of how
// many times a thread failed to take a Monitor on the first try and had to
// wait. That single counter says which of the six rungs ever touch a
// Monitor internally and which never do, without timing anything.
static class Ladder
{
    static long _count;
    static readonly object Gate = new();
    static SpinLock _spin = new(enableThreadOwnerTracking: false);
    static readonly SemaphoreSlim Sem = new(1, 1);
    static readonly ReaderWriterLockSlim Rw = new();

    // ── the six ways to add one ──────────────────────────────────────────────
    static void Unsynchronized(int n) { for (int i = 0; i < n; i++) _count++; }
    static void MonitorLock(int n)    { for (int i = 0; i < n; i++) lock (Gate) _count++; }
    static void Interlocked_(int n)   { for (int i = 0; i < n; i++) Interlocked.Increment(ref _count); }
    static void SpinLock_(int n)
    {
        for (int i = 0; i < n; i++)
        {
            bool taken = false;
            try { _spin.Enter(ref taken); _count++; }
            finally { if (taken) _spin.Exit(useMemoryBarrier: false); }
        }
    }
    static void Semaphore_(int n)
    {
        for (int i = 0; i < n; i++) { Sem.Wait(); try { _count++; } finally { Sem.Release(); } }
    }
    static void RwLock(int n)
    {
        for (int i = 0; i < n; i++) { Rw.EnterWriteLock(); try { _count++; } finally { Rw.ExitWriteLock(); } }
    }

    const int Ops = 4_000_000;

    static void Run(string name, Action<int> body, int threads)
    {
        _count = 0;
        int per = Ops / threads;
        var ts = new Thread[threads];
        var start = new ManualResetEventSlim(false);
        for (int t = 0; t < threads; t++)
        {
            ts[t] = new Thread(() => { start.Wait(); body(per); }) { IsBackground = true };
            ts[t].Start();
        }
        Thread.Sleep(20);                       // let every thread reach the gate
        long c0 = Monitor.LockContentionCount;
        start.Set();
        foreach (var th in ts) th.Join();
        long c1 = Monitor.LockContentionCount;
        Console.WriteLine(
            $"{name,-24} {threads}t  checksum {_count,10:N0} / {Ops,10:N0}  monitor contentions {c1 - c0,7:N0}");
    }

    public static void Main()
    {
        var variants = new (string Name, Action<int> Body)[]
        {
            ("count++ (no lock)",     Unsynchronized),
            ("Interlocked.Increment", Interlocked_),
            ("lock (Monitor)",        MonitorLock),
            ("SpinLock",              SpinLock_),
            ("ReaderWriterLockSlim",  RwLock),
            ("SemaphoreSlim(1)",      Semaphore_),
        };
        int[] threadCounts = [1, 2, 4];

        var sink = Console.Out;
        Console.SetOut(TextWriter.Null);
        foreach (var v in variants) foreach (var t in threadCounts) Run(v.Name, v.Body, t);   // silent pass: run each variant once so a mid-run JIT compile can't be mistaken for a Monitor contention
        Console.SetOut(sink);

        foreach (var v in variants) foreach (var t in threadCounts) Run(v.Name, v.Body, t);   // the one printed pass

        if (_count == 0) throw new Exception("FAIL: nothing ran");
        Console.WriteLine("PASS");
    }
}
