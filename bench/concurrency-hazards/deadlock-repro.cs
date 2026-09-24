// Evidence for /systems/concurrency-hazards/deadlock-repro/ — run with:
//   dotnet run bench/concurrency-hazards/deadlock-repro.cs log
//   dotnet run bench/concurrency-hazards/deadlock-repro.cs odds
//
//   log  — force the interleaving with a rendezvous, print who holds what, let it hang,
//          and have a watchdog print both thread states before killing the process
//   odds — how many transfers land before the same code deadlocks on its own, as a
//          function of how many accounts there are to collide over — and that the ordered
//          version does not deadlock at all across the same number of attempts
//
// Nothing in this file is timed. A wall-clock poll is used only internally to detect "no
// forward progress", never printed and never the quantity being reported.
using System.Diagnostics;

class Account(int id, long balance)
{
    public readonly int Id = id;
    public long Balance = balance;
}

static class Deadlock
{
    // A monotonic step counter, not a clock — shows real cross-thread ORDER, never duration.
    static int step;
    static void Log(string msg) =>
        Console.WriteLine($"  [{Interlocked.Increment(ref step),3}] tid {Environment.CurrentManagedThreadId,3}  {msg}");

    // ── the code under test ─────────────────────────────────────────────────
    static void TransferNaive(Account from, Account to, long amount)
    {
        lock (from)
        {
            lock (to)
            {
                from.Balance -= amount;
                to.Balance += amount;
            }
        }
    }

    // the fix: always take the two locks in the same global order
    static void TransferOrdered(Account from, Account to, long amount)
    {
        Account first = from.Id < to.Id ? from : to;
        Account second = from.Id < to.Id ? to : from;
        lock (first)
        {
            lock (second)
            {
                from.Balance -= amount;
                to.Balance += amount;
            }
        }
    }

    // ── mode: log ───────────────────────────────────────────────────────────
    static void LogMode()
    {
        var a = new Account(1, 1000);
        var b = new Account(2, 1000);
        using var bothHoldOne = new Barrier(2);

        Log("main: account 1 and account 2 created, both threads about to start");

        var t1 = new Thread(() =>
        {
            Log("thread A: Transfer(1 -> 2), taking lock on account 1");
            lock (a)
            {
                Log("thread A: HOLDS 1");
                bothHoldOne.SignalAndWait();
                Log("thread A: now wants lock on account 2 (held by B)");
                lock (b) { Log("thread A: got 2 — this line never prints"); }
            }
        })
        { Name = "A", IsBackground = true };

        var t2 = new Thread(() =>
        {
            Log("thread B: Transfer(2 -> 1), taking lock on account 2");
            lock (b)
            {
                Log("thread B: HOLDS 2");
                bothHoldOne.SignalAndWait();
                Log("thread B: now wants lock on account 1 (held by A)");
                lock (a) { Log("thread B: got 1 — this line never prints"); }
            }
        })
        { Name = "B", IsBackground = true };

        t1.Start();
        t2.Start();

        bool finished = t1.Join(3000) & t2.Join(1);   // watchdog cap, not a measurement
        Log($"main: both threads returned before the watchdog fired? {finished}");
        Log($"main: thread A state = {t1.ThreadState}   alive = {t1.IsAlive}");
        Log($"main: thread B state = {t2.ThreadState}   alive = {t2.IsAlive}");
        Log($"main: balances unchanged: account 1 = {a.Balance}, account 2 = {b.Balance}");
        Log("WATCHDOG: neither thread will ever run again. killing the process.");
        Console.Out.Flush();
        Environment.Exit(2);
    }

    // ── mode: odds ──────────────────────────────────────────────────────────
    static long done;

    static (bool deadlocked, long transfers) Race(int accounts, int threads, int seed, bool ordered, int budgetMs)
    {
        var acc = new Account[accounts];
        for (int i = 0; i < accounts; i++) acc[i] = new Account(i, 1_000_000);
        Volatile.Write(ref done, 0);

        var ts = new Thread[threads];
        for (int t = 0; t < threads; t++)
        {
            int s = seed * 31 + t;
            ts[t] = new Thread(() =>
            {
                var rnd = new Random(s);
                while (true)
                {
                    int x = rnd.Next(accounts), y = rnd.Next(accounts);
                    if (x == y) continue;
                    if (ordered) TransferOrdered(acc[x], acc[y], 1);
                    else TransferNaive(acc[x], acc[y], 1);
                    Interlocked.Increment(ref done);
                }
            })
            { IsBackground = true };
        }
        foreach (var x in ts) x.Start();

        // Stall detector: a wall-clock poll used only to decide "has progress stopped", and
        // to cap a run that isn't going to deadlock. Neither the poll interval nor the cap
        // is printed — only the transfer COUNT reached before the stall (or the cap) is.
        var sw = Stopwatch.StartNew();
        long last = -1;
        while (sw.ElapsedMilliseconds < budgetMs)
        {
            Thread.Sleep(500);
            long cur = Interlocked.Read(ref done);
            if (cur == last) return (true, cur);
            last = cur;
        }
        return (false, Interlocked.Read(ref done));
    }

    static void OddsMode()
    {
        Console.WriteLine("=== the SAME buggy code, by how many accounts it has to collide over ===");
        Console.WriteLine("    4 threads, random distinct pairs, one trial per size");
        foreach (int n in new[] { 2, 8, 64, 1000 })
        {
            var (d, tr) = Race(n, 4, seed: n, ordered: false, budgetMs: 20000);
            Console.WriteLine($"  accounts {n,5}: {(d ? "DEADLOCKED" : "no stall")} — transfers completed before the stall: {tr,12:N0}");
        }

        Console.WriteLine("\n=== the ordered version, same pressure, same detector, same account count ===");
        var (dOrdered, trOrdered) = Race(1000, 4, seed: 1000, ordered: true, budgetMs: 20000);
        Console.WriteLine($"  accounts  1000, ordered locks: {(dOrdered ? "DEADLOCKED" : "no stall — detector hit its cap")} — transfers completed: {trOrdered,12:N0}");
    }

    public static void Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "log";
        switch (mode)
        {
            case "log": LogMode(); break;
            case "odds": OddsMode(); break;
            default: Console.WriteLine("modes: log | odds"); break;
        }
    }
}
