// Evidence for /systems/concurrency-hazards/ — run with:
//   dotnet run bench/concurrency-hazards/index.cs shapes
//   dotnet run bench/concurrency-hazards/index.cs lazy
//   dotnet run bench/concurrency-hazards/index.cs livelock
//   dotnet run bench/concurrency-hazards/index.cs starvation
//   dotnet run bench/concurrency-hazards/index.cs reentrancy
//   dotnet run bench/concurrency-hazards/index.cs async
//
//   shapes     — the two shapes: read-modify-write (a data race) and check-then-act
//                (a race condition with no data race — every access is atomic)
//   lazy       — four ways to initialise one field once, and how often each runs the factory twice
//   livelock   — abort-and-retry instead of ordering: what fraction of fixed-count attempts
//                are thrown away, at three backoff lengths
//   starvation — how unevenly an unfair lock hands out a fixed number of acquisitions
//   reentrancy — lock re-enters, SemaphoreSlim does not
//   async      — .Result under a single-threaded SynchronizationContext, fixed then broken
//
// No mode in this file times anything. "livelock" and "starvation" run a FIXED number of
// attempts/acquisitions, never a fixed wall-clock window, so nothing printed is a rate.
using System.Collections.Concurrent;

static class Hazards
{
    const int Threads = 4, Rounds = 20_000;

    // A monotonic step counter, not a clock — it shows real cross-thread ORDER, never duration.
    static int step;
    static void Log(string msg) =>
        Console.WriteLine($"  [{Interlocked.Increment(ref step),3}] tid {Environment.CurrentManagedThreadId,3}  {msg}");

    // ── mode: shapes ────────────────────────────────────────────────────────
    static int seats, admitted, plainCounter;
    static readonly object gate = new();

    // Every round: reset, rendezvous, all four threads run one body, rendezvous again.
    // Independent rounds are what make a rare interleaving countable.
    static (int bad, long total) Rounds4(Action body, Action reset, Func<bool> isBad)
    {
        int bad = 0; long total = 0;
        using var start = new Barrier(Threads + 1);
        using var done = new Barrier(Threads + 1);
        var ts = new Thread[Threads];
        for (int t = 0; t < Threads; t++)
            ts[t] = new Thread(() =>
            {
                for (int r = 0; r < Rounds; r++) { start.SignalAndWait(); body(); done.SignalAndWait(); }
            })
            { IsBackground = true };
        foreach (var x in ts) x.Start();
        for (int r = 0; r < Rounds; r++)
        {
            reset();
            start.SignalAndWait();
            done.SignalAndWait();
            if (isBad()) bad++;
            total += Volatile.Read(ref admitted) + Volatile.Read(ref plainCounter);
        }
        foreach (var x in ts) x.Join();
        return (bad, total);
    }

    static void ShapesMode()
    {
        Console.WriteLine($"=== shape 1: read-modify-write. {Threads} threads, one ++ each, {Rounds:N0} rounds ===");
        var (lostPlain, _) = Rounds4(
            body: () => plainCounter++,                                  // three operations, not one
            reset: () => { plainCounter = 0; admitted = 0; },
            isBad: () => Volatile.Read(ref plainCounter) != Threads);
        Console.WriteLine($"  count++ (no synchronisation)          rounds that lost an update: {lostPlain,6} / {Rounds}  ({lostPlain * 100.0 / Rounds:F2}%)");

        var (lostAtomic, _) = Rounds4(
            body: () => Interlocked.Increment(ref plainCounter),
            reset: () => { plainCounter = 0; admitted = 0; },
            isBad: () => Volatile.Read(ref plainCounter) != Threads);
        Console.WriteLine($"  Interlocked.Increment                 rounds that lost an update: {lostAtomic,6} / {Rounds}  ({lostAtomic * 100.0 / Rounds:F2}%)");

        Console.WriteLine($"\n=== shape 2: check-then-act. 1 seat, {Threads} threads, {Rounds:N0} rounds ===");
        Console.WriteLine("    every single access below is atomic. that is not the same as correct.");
        var (over, _) = Rounds4(
            body: () => { if (Volatile.Read(ref seats) < 1) { Interlocked.Increment(ref seats); Interlocked.Increment(ref admitted); } },
            reset: () => { seats = 0; admitted = 0; plainCounter = 0; },
            isBad: () => Volatile.Read(ref admitted) > 1);
        Console.WriteLine($"  atomic read, then atomic increment    rounds that oversold the seat: {over,6} / {Rounds}  ({over * 100.0 / Rounds:F2}%)");

        var (overLocked, _) = Rounds4(
            body: () => { lock (gate) { if (seats < 1) { seats++; admitted++; } } },
            reset: () => { seats = 0; admitted = 0; plainCounter = 0; },
            isBad: () => Volatile.Read(ref admitted) > 1);
        Console.WriteLine($"  one lock around check AND act         rounds that oversold the seat: {overLocked,6} / {Rounds}  ({overLocked * 100.0 / Rounds:F2}%)");
    }

    // ── mode: lazy ──────────────────────────────────────────────────────────
    static int built;
    static object? instance;
    static volatile object? volatileInstance;
    static Lazy<object>? lazy, lazyPub;

    static object Build() { Interlocked.Increment(ref built); return new object(); }

    static int LazyRace(int variant)
    {
        int multi = 0;
        using var start = new Barrier(Threads + 1);
        using var done = new Barrier(Threads + 1);
        var ts = new Thread[Threads];
        for (int t = 0; t < Threads; t++)
            ts[t] = new Thread(() =>
            {
                for (int r = 0; r < Rounds; r++)
                {
                    start.SignalAndWait();
                    switch (variant)
                    {
                        case 0: if (instance is null) instance = Build(); break;
                        case 1:
                            if (volatileInstance is null)
                                lock (gate) { if (volatileInstance is null) volatileInstance = Build(); }
                            break;
                        case 2: _ = lazy!.Value; break;
                        case 3: _ = lazyPub!.Value; break;
                    }
                    done.SignalAndWait();
                }
            })
            { IsBackground = true };
        foreach (var x in ts) x.Start();
        for (int r = 0; r < Rounds; r++)
        {
            instance = null; volatileInstance = null; built = 0;
            lazy = new Lazy<object>(Build);
            lazyPub = new Lazy<object>(Build, LazyThreadSafetyMode.PublicationOnly);
            start.SignalAndWait();
            done.SignalAndWait();
            if (Volatile.Read(ref built) > 1) multi++;
        }
        foreach (var x in ts) x.Join();
        return multi;
    }

    static void LazyMode()
    {
        Console.WriteLine($"=== initialise one field once. {Threads} threads racing, {Rounds:N0} independent rounds ===");
        string[] names =
        [
            "if (x is null) x = Build()        ",
            "double-checked lock, volatile field",
            "Lazy<T> (default, ExecutionAndPublication)",
            "Lazy<T> (PublicationOnly)         ",
        ];
        for (int v = 0; v < 4; v++)
        {
            int multi = LazyRace(v);
            Console.WriteLine($"  {names[v],-42}  rounds where the factory ran more than once: {multi,6} / {Rounds}  ({multi * 100.0 / Rounds,5:F2}%)");
        }
    }

    // ── mode: livelock ──────────────────────────────────────────────────────
    static readonly object lockA = new(), lockB = new();
    static long acquired, abandoned;
    const int AttemptsPerThread = 200_000;

    // FIXED number of attempts per thread — not a time budget — so "abandoned" is a count,
    // exactly like a lost-update count, not a rate.
    static void PoliteRetry(bool aFirst, int spin)
    {
        object first = aFirst ? lockA : lockB, second = aFirst ? lockB : lockA;
        for (int i = 0; i < AttemptsPerThread; i++)
        {
            bool g1 = false, g2 = false;
            try
            {
                Monitor.TryEnter(first, 0, ref g1);
                if (g1) { Thread.SpinWait(spin); Monitor.TryEnter(second, 0, ref g2); }
                if (g1 && g2) { Interlocked.Increment(ref acquired); continue; }
            }
            finally { if (g2) Monitor.Exit(second); if (g1) Monitor.Exit(first); }
            Interlocked.Increment(ref abandoned);     // "you go first" — release everything and retry
            Thread.SpinWait(spin);
        }
    }

    static void LivelockMode()
    {
        Console.WriteLine($"=== abort-and-retry instead of a lock order: two threads, opposite orders, {AttemptsPerThread:N0} attempts each ===");
        foreach (int spin in new[] { 50, 200, 1000 })
        {
            Volatile.Write(ref acquired, 0); Volatile.Write(ref abandoned, 0);
            var t1 = new Thread(() => PoliteRetry(true, spin)) { IsBackground = true };
            var t2 = new Thread(() => PoliteRetry(false, spin)) { IsBackground = true };
            t1.Start(); t2.Start(); t1.Join(); t2.Join();
            long a = Volatile.Read(ref acquired), b = Volatile.Read(ref abandoned);
            Console.WriteLine($"  spin={spin,5}:  completed {a,10:N0}   abandoned {b,10:N0}   of {a + b,10:N0} total attempts   ({b * 100.0 / (a + b):F1}% wasted)");
        }
    }

    // ── mode: starvation ────────────────────────────────────────────────────
    // Monitor is not fair: a thread that releases the lock and immediately asks for it
    // again often wins it back before a thread already queued gets a turn.
    // FIXED total number of acquisitions, not a time window — every thread races to grab
    // as many of a fixed pool of 4,000,000 turns as it can; the split is the evidence.
    const long TotalAcquisitions = 4_000_000;

    static void StarvationMode()
    {
        Console.WriteLine($"=== 4 threads, one lock, racing for a fixed pool of {TotalAcquisitions:N0} acquisitions, no work inside the critical section ===");
        var counts = new long[Threads];
        long remaining = TotalAcquisitions;
        var ts = new Thread[Threads];
        for (int t = 0; t < Threads; t++)
        {
            int id = t;
            ts[t] = new Thread(() =>
            {
                while (Interlocked.Decrement(ref remaining) >= 0) { lock (gate) { counts[id]++; } }
            })
            { IsBackground = true };
        }
        foreach (var x in ts) x.Start();
        foreach (var x in ts) x.Join();
        long max = counts.Max(), min = counts.Min();
        Console.WriteLine($"  acquisitions per thread: {string.Join("  ", counts.Select(c => c.ToString("N0")))}   max/min = {(double)max / min:F2}x");
    }

    // ── mode: reentrancy ────────────────────────────────────────────────────
    static readonly SemaphoreSlim permit = new(1, 1);

    static void ReentrancyMode()
    {
        lock (gate)
        {
            lock (gate)                                  // same thread, same object: allowed
            {
                Console.WriteLine($"  lock: entered twice from one thread. Monitor.IsEntered = {Monitor.IsEntered(gate)}");
            }
        }

        permit.Wait();
        bool second = permit.Wait(500);                  // same thread, same semaphore: NOT allowed
        Console.WriteLine($"  SemaphoreSlim(1,1): first Wait() succeeded, second Wait(500 ms) returned {second}");
        Console.WriteLine($"  SemaphoreSlim.CurrentCount = {permit.CurrentCount}   (the permit this thread is holding itself)");
        if (second) throw new Exception("FAIL: SemaphoreSlim was reentrant");
        permit.Release();
        Console.WriteLine("  PASS");
    }

    // ── mode: async ─────────────────────────────────────────────────────────
    // A single-threaded message pump: what WinForms, WPF and legacy ASP.NET install.
    // Console apps and ASP.NET Core have no SynchronizationContext, so this deadlock
    // cannot be reproduced without building one.
    sealed class PumpContext : SynchronizationContext
    {
        readonly BlockingCollection<(SendOrPostCallback cb, object? state)> _queue = new();
        public int Posted;
        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref Posted);
            _queue.Add((d, state));
        }
        public void Pump(CancellationToken ct)
        {
            try { foreach (var (cb, st) in _queue.GetConsumingEnumerable(ct)) cb(st); }
            catch (OperationCanceledException) { }
        }
    }

    static async Task<int> GetAsync(bool configureAwaitFalse)
    {
        await Task.Delay(50).ConfigureAwait(!configureAwaitFalse);
        return 42;
    }

    static void AsyncMode()
    {
        foreach (bool cfg in new[] { true, false })
        {
            var ctx = new PumpContext();
            var cts = new CancellationTokenSource();
            int result = -1;
            var pumpThread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(ctx);
                Log($"pump thread: context installed, calling GetAsync(ConfigureAwait(false) = {cfg}).Result");
                result = GetAsync(cfg).Result;                      // the blocking call
                Log($"pump thread: .Result returned {result}");
            })
            { IsBackground = true };
            pumpThread.Start();
            bool ok = pumpThread.Join(2000);   // watchdog cap, not a measurement
            Log($"main: ConfigureAwait(false) = {cfg} -> pump thread returned before the watchdog fired? {ok}   result = {result}   continuations posted to the pump queue: {Volatile.Read(ref ctx.Posted)}");
            Log($"main: pump thread state = {pumpThread.ThreadState}");
            cts.Cancel();
            if (!ok)
            {
                Log("WATCHDOG: the continuation is queued to the thread that is blocked. killing the process.");
                Console.Out.Flush();
                Environment.Exit(2);
            }
        }
    }

    public static void Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "shapes";
        switch (mode)
        {
            case "shapes": ShapesMode(); break;
            case "lazy": LazyMode(); break;
            case "livelock": LivelockMode(); break;
            case "starvation": StarvationMode(); break;
            case "reentrancy": ReentrancyMode(); break;
            case "async": AsyncMode(); break;
            default: Console.WriteLine("modes: shapes | lazy | livelock | starvation | reentrancy | async"); break;
        }
    }
}
