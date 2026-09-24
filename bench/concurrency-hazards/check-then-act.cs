// Evidence for /systems/concurrency-hazards/check-then-act/ — run with:
//   dotnet run bench/concurrency-hazards/check-then-act.cs race
//   dotnet run bench/concurrency-hazards/check-then-act.cs trace
//   dotnet run bench/concurrency-hazards/check-then-act.cs semaphore
//
//   race      — four threads, one unit of stock, 20,000 rounds: how often each version oversells
//   trace     — one oversold round, logged line by line in real cross-thread order
//   semaphore — the same fix written with a SemaphoreSlim instead of a lock: it hangs
//
// Nothing in this file is timed.
using System.Collections.Concurrent;

// ── the class under review ──────────────────────────────────────────────────
// Every method takes the lock. Every method is individually correct.
public sealed class Inventory
{
    readonly Dictionary<string, int> _stock = new();
    readonly object _gate = new();

    public void Restock(string sku, int qty) { lock (_gate) _stock[sku] = _stock.GetValueOrDefault(sku) + qty; }
    public int  Count(string sku)            { lock (_gate) return _stock.GetValueOrDefault(sku); }
    public void Remove(string sku, int qty)  { lock (_gate) _stock[sku] = _stock.GetValueOrDefault(sku) - qty; }

    // ...and this one composes two of them.
    public bool TryReserve(string sku, int qty)
    {
        if (Count(sku) < qty) return false;    // check
        Remove(sku, qty);                      // act
        return true;
    }

    // the fix: one critical section spanning both. Works because Monitor is reentrant:
    // Count and Remove re-enter the lock this thread already holds.
    public bool TryReserveFixed(string sku, int qty)
    {
        lock (_gate)
        {
            if (Count(sku) < qty) return false;
            Remove(sku, qty);
            return true;
        }
    }
}

// the fix people reach for first: swap the Dictionary for a ConcurrentDictionary.
// Reads (GetValueOrDefault) take no lock; writes (AddOrUpdate) still take a stripe lock.
public sealed class ConcurrentInventory
{
    readonly ConcurrentDictionary<string, int> _stock = new();

    public void Restock(string sku, int qty) => _stock.AddOrUpdate(sku, qty, (_, v) => v + qty);
    public int  Count(string sku)            => _stock.GetValueOrDefault(sku);
    public void Remove(string sku, int qty)  => _stock.AddOrUpdate(sku, -qty, (_, v) => v - qty);

    public bool TryReserve(string sku, int qty)
    {
        if (Count(sku) < qty) return false;    // still a check
        Remove(sku, qty);                      // still a separate act
        return true;
    }
}

// the same fix, with a SemaphoreSlim as the mutex
public sealed class SemaphoreInventory
{
    readonly Dictionary<string, int> _stock = new();
    readonly SemaphoreSlim _gate = new(1, 1);

    public void Restock(string sku, int qty) { _gate.Wait(); try { _stock[sku] = _stock.GetValueOrDefault(sku) + qty; } finally { _gate.Release(); } }
    public int Count(string sku) { _gate.Wait(); try { return _stock.GetValueOrDefault(sku); } finally { _gate.Release(); } }
    public void Remove(string sku, int qty) { _gate.Wait(); try { _stock[sku] = _stock.GetValueOrDefault(sku) - qty; } finally { _gate.Release(); } }

    public bool TryReserveFixed(string sku, int qty)
    {
        _gate.Wait();                            // takes the one permit
        try
        {
            if (Count(sku) < qty) return false;  // Count waits for a permit this thread is holding
            Remove(sku, qty);
            return true;
        }
        finally { _gate.Release(); }
    }
}

static class CheckThenAct
{
    const int Threads = 4, Rounds = 20_000;
    const string Sku = "WIDGET-1";

    // A monotonic step counter, not a clock — shows real cross-thread ORDER, never duration.
    static int step;
    static void Log(string msg) =>
        Console.WriteLine($"  [{Interlocked.Increment(ref step),3}] tid {Environment.CurrentManagedThreadId,3}  {msg}");

    // ── mode: race ──────────────────────────────────────────────────────────
    // Four threads rendezvous, then all four try to reserve the single unit in stock.
    // "Oversold" = more than one thread was told yes, i.e. stock went negative.
    static (int oversoldRounds, int worstStock, int totalReserved) Race(Func<object> factory, Func<object, bool> reserve, Action<object> restock)
    {
        object inv = factory();
        int oversold = 0, worst = 0, granted = 0, total = 0;

        using var start = new Barrier(Threads + 1);
        using var done = new Barrier(Threads + 1);
        var ts = new Thread[Threads];
        for (int t = 0; t < Threads; t++)
            ts[t] = new Thread(() =>
            {
                for (int r = 0; r < Rounds; r++)
                {
                    start.SignalAndWait();
                    if (reserve(inv)) Interlocked.Increment(ref granted);
                    done.SignalAndWait();
                }
            })
            { IsBackground = true };
        foreach (var x in ts) x.Start();

        for (int r = 0; r < Rounds; r++)
        {
            inv = factory();
            restock(inv);                       // exactly one unit in stock
            granted = 0;
            start.SignalAndWait();
            done.SignalAndWait();
            total += granted;
            if (granted > 1) { oversold++; if (granted > worst) worst = granted; }
        }
        foreach (var x in ts) x.Join();
        return (oversold, worst, total);
    }

    static void RaceMode()
    {
        Console.WriteLine($"=== {Threads} threads, 1 unit in stock, {Rounds:N0} independent rounds ===");
        Console.WriteLine("    a round is oversold when more than one thread was told yes");

        var (o1, w1, t1) = Race(() => new Inventory(), o => ((Inventory)o).TryReserve(Sku, 1), o => ((Inventory)o).Restock(Sku, 1));
        Console.WriteLine($"  Inventory.TryReserve            (lock per method) oversold {o1,6} / {Rounds}  ({o1 * 100.0 / Rounds,5:F2}%)  worst round: {w1} threads told yes, total sold {t1:N0} of {Rounds:N0} units");

        var (o2, w2, t2) = Race(() => new ConcurrentInventory(), o => ((ConcurrentInventory)o).TryReserve(Sku, 1), o => ((ConcurrentInventory)o).Restock(Sku, 1));
        Console.WriteLine($"  ConcurrentInventory.TryReserve  (lock-free reads) oversold {o2,6} / {Rounds}  ({o2 * 100.0 / Rounds,5:F2}%)  worst round: {w2} threads told yes, total sold {t2:N0} of {Rounds:N0} units");

        var (o3, w3, t3) = Race(() => new Inventory(), o => ((Inventory)o).TryReserveFixed(Sku, 1), o => ((Inventory)o).Restock(Sku, 1));
        Console.WriteLine($"  Inventory.TryReserveFixed       (one lock, both)  oversold {o3,6} / {Rounds}  ({o3 * 100.0 / Rounds,5:F2}%)  worst round: {w3} threads told yes, total sold {t3:N0} of {Rounds:N0} units");
    }

    // ── mode: trace ─────────────────────────────────────────────────────────
    // Two threads, one unit, and a rendezvous placed in the gap between check and act
    // so the interleaving that the race mode hits by luck happens on purpose.
    static void TraceMode()
    {
        var inv = new Inventory();
        inv.Restock(Sku, 1);
        Log($"stock({Sku}) = {inv.Count(Sku)}");

        using var bothChecked = new Barrier(2);
        var ts = new Thread[2];
        for (int t = 0; t < 2; t++)
        {
            string name = t == 0 ? "A" : "B";
            ts[t] = new Thread(() =>
            {
                int have = inv.Count(Sku);
                Log($"thread {name}: Count() -> {have}   (lock taken and RELEASED)");
                bothChecked.SignalAndWait();          // the gap, made visible
                Log($"thread {name}: {have} >= 1, so reserving");
                inv.Remove(Sku, 1);
                Log($"thread {name}: Remove() done, told the customer YES");
            })
            { IsBackground = true, Name = name };
        }
        foreach (var x in ts) x.Start();
        foreach (var x in ts) x.Join();
        Log($"stock({Sku}) = {inv.Count(Sku)}   <- one unit, two customers");
    }

    // ── mode: semaphore ─────────────────────────────────────────────────────
    static void SemaphoreMode()
    {
        var inv = new SemaphoreInventory();
        inv.Restock(Sku, 5);
        Log($"stock({Sku}) = {inv.Count(Sku)}");
        Log("one thread, no contention, calling TryReserveFixed");

        var t = new Thread(() =>
        {
            Log("worker: entering TryReserveFixed");
            bool ok = inv.TryReserveFixed(Sku, 1);
            Log($"worker: returned {ok}");
        })
        { IsBackground = true };
        t.Start();
        bool finished = t.Join(3000);   // watchdog cap, not a measurement
        Log($"main: worker returned before the watchdog fired? {finished}");
        Log($"main: worker state = {t.ThreadState}   alive = {t.IsAlive}");
        Log("WATCHDOG: the thread is waiting for a permit it is holding itself. killing the process.");
        Console.Out.Flush();
        Environment.Exit(2);
    }

    public static void Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "race";
        switch (mode)
        {
            case "race": RaceMode(); break;
            case "trace": TraceMode(); break;
            case "semaphore": SemaphoreMode(); break;
            default: Console.WriteLine("modes: race | trace | semaphore"); break;
        }
    }
}
