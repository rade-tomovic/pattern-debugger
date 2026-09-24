// Evidence for /systems/lock-free-structures/concurrent-dictionary-traps/ — run with:
//   dotnet run bench/lock-free-structures/concurrent-dictionary-traps.cs
using System.Collections.Concurrent;

// ── the class under review ───────────────────────────────────────────────────
// Every field is a ConcurrentDictionary. No lock anywhere. Ships.
public sealed class TenantCache
{
    public sealed class Session(string tenant)
    {
        public readonly string Tenant = tenant;
        public bool Closed;
    }

    public static int SessionsOpened;                     // instrumentation for this bench only
    public static int SessionsClosed;

    readonly ConcurrentDictionary<string, Session> _sessions = new();
    readonly ConcurrentDictionary<string, int> _hits = new();

    static Session OpenSession(string tenant)
    {
        Interlocked.Increment(ref SessionsOpened);
        Thread.SpinWait(200);                             // a handshake, a TLS negotiation, a pool checkout
        return new Session(tenant);
    }

    public Session For(string tenant) => _sessions.GetOrAdd(tenant, OpenSession);

    public void RecordHit(string tenant) =>
        _hits[tenant] = _hits.TryGetValue(tenant, out int n) ? n + 1 : 1;

    public int Hits(string tenant) => _hits.TryGetValue(tenant, out int n) ? n : 0;

    public void CloseAll()
    {
        foreach (var s in _sessions.Values) { s.Closed = true; Interlocked.Increment(ref SessionsClosed); }
    }
}

// ── the same class, fixed ────────────────────────────────────────────────────
public sealed class FixedTenantCache
{
    public static int SessionsOpened;

    readonly ConcurrentDictionary<string, Lazy<TenantCache.Session>> _sessions = new();
    readonly ConcurrentDictionary<string, Counter> _hits = new();

    sealed class Counter { public int Value; }

    static TenantCache.Session OpenSession(string tenant)
    {
        Interlocked.Increment(ref SessionsOpened);
        Thread.SpinWait(200);
        return new TenantCache.Session(tenant);
    }

    // GetOrAdd may build several Lazy objects; only the one that wins the race is
    // ever .Value'd by anybody, and Lazy itself guarantees the factory runs once.
    public TenantCache.Session For(string tenant) =>
        _sessions.GetOrAdd(tenant, t => new Lazy<TenantCache.Session>(() => OpenSession(t),
                                                                     LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    // One reference per key, then an atomic increment on a field nobody replaces.
    public void RecordHit(string tenant) =>
        Interlocked.Increment(ref _hits.GetOrAdd(tenant, _ => new Counter()).Value);

    public int Hits(string tenant) => _hits.TryGetValue(tenant, out var c) ? Volatile.Read(ref c.Value) : 0;
}

public static class Bench
{
    const int Threads = 4, HitsPerThread = 100_000;
    static readonly string[] Tenants = ["acme", "globex", "initech", "umbrella", "soylent", "tyrell", "wayne", "cyberdyne"];

    static void Parallel(Action<int> body)
    {
        var gate = new Barrier(Threads);
        var ths = new Thread[Threads];
        for (int t = 0; t < Threads; t++)
        {
            int id = t;
            ths[t] = new Thread(() => { gate.SignalAndWait(); body(id); });
            ths[t].Start();
        }
        foreach (var th in ths) th.Join();
    }

    // ── 1. the broken class, under four threads ─────────────────────────────
    static void Broken()
    {
        var cache = new TenantCache();
        TenantCache.SessionsOpened = 0; TenantCache.SessionsClosed = 0;
        Parallel(_ =>
        {
            for (int i = 0; i < HitsPerThread; i++)
            {
                string tenant = Tenants[i % Tenants.Length];
                cache.For(tenant);
                cache.RecordHit(tenant);
            }
        });
        cache.CloseAll();

        int expected = Threads * HitsPerThread / Tenants.Length;
        int total = 0;
        foreach (var t in Tenants) total += cache.Hits(t);
        Console.WriteLine($"  tenants                {Tenants.Length}");
        Console.WriteLine($"  sessions opened        {TenantCache.SessionsOpened}");
        Console.WriteLine($"  sessions closed        {TenantCache.SessionsClosed}");
        Console.WriteLine($"  sessions never closed  {TenantCache.SessionsOpened - TenantCache.SessionsClosed}");
        Console.WriteLine($"  hits recorded          {Threads * HitsPerThread:N0}");
        Console.WriteLine($"  hits counted           {total:N0}   ({100.0 * total / (Threads * HitsPerThread):F1}% of them)");
        Console.WriteLine($"  per tenant, expected {expected:N0}: {string.Join(" ", Tenants.Select(t => cache.Hits(t).ToString("N0")))}");
    }

    // ── 2. one key, two threads, every read and write logged ────────────────
    // The log is an array of (sequence, thread, what, value). The sequence number
    // is an Interlocked.Increment, so entries are globally ordered even though the
    // operations they describe are not.
    static void LogInterleaving()
    {
        var d = new ConcurrentDictionary<string, int>();
        d["k"] = 0;
        const int Per = 2_000;
        var log = new (int Seq, int Thread, string What, int Value)[4 * Per * 2];
        int next = -1, threads = 2;

        var gate = new Barrier(threads);
        var ths = new Thread[threads];
        for (int t = 0; t < threads; t++)
        {
            int id = t;
            ths[t] = new Thread(() =>
            {
                gate.SignalAndWait();
                for (int i = 0; i < Per; i++)
                {
                    d.TryGetValue("k", out int n);
                    log[Interlocked.Increment(ref next)] = (0, id, "read  k ->", n);
                    d["k"] = n + 1;
                    log[Interlocked.Increment(ref next)] = (0, id, "write k <-", n + 1);
                }
            });
            ths[t].Start();
        }
        foreach (var th in ths) th.Join();

        int count = next + 1;
        // Find the first lost update: two threads read the same value, then both
        // write the same value back. One of the two increments never happened.
        for (int i = 0; i + 1 < count; i++)
        {
            if (log[i].What[0] != 'r') continue;
            for (int j = i + 1; j < Math.Min(i + 4, count); j++)
            {
                if (log[j].What[0] != 'r' || log[j].Thread == log[i].Thread || log[j].Value != log[i].Value) continue;
                int wa = -1, wb = -1;
                for (int k = i + 1; k < Math.Min(i + 8, count); k++)
                {
                    if (log[k].What[0] != 'w') continue;
                    if (wa < 0 && log[k].Thread == log[i].Thread) wa = k;
                    if (wb < 0 && log[k].Thread == log[j].Thread) wb = k;
                }
                if (wa < 0 || wb < 0) continue;
                Console.WriteLine($"  first lost update — entries {i}..{Math.Max(wa, wb)} of {count}:");
                for (int k = i; k <= Math.Max(wa, wb); k++)
                    Console.WriteLine($"    #{k,-5} thread {log[k].Thread}   {log[k].What} {log[k].Value}");
                Console.WriteLine($"  final value {d["k"]}, should be {threads * Per}, lost {threads * Per - d["k"]}");
                return;
            }
        }
        Console.WriteLine($"  no lost update found in this run; final value {d["k"]} of {threads * Per}");
    }

    // ── 3. how often does GetOrAdd's factory run? ───────────────────────────
    static void FactoryRuns(string label, int spin)
    {
        int runs = 0;
        var d = new ConcurrentDictionary<int, string>();
        Parallel(_ =>
        {
            for (int k = 0; k < 200; k++)
                d.GetOrAdd(k, key => { Interlocked.Increment(ref runs); Thread.SpinWait(spin); return "v" + key; });
        });
        Console.WriteLine($"  {label,-34} {d.Count} keys, factory ran {runs} times ({runs / (double)d.Count:F2}x)");
    }

    // ── 4. the fixed class, same pressure ───────────────────────────────────
    static void Fixed()
    {
        var cache = new FixedTenantCache();
        FixedTenantCache.SessionsOpened = 0;
        Parallel(_ =>
        {
            for (int i = 0; i < HitsPerThread; i++)
            {
                string tenant = Tenants[i % Tenants.Length];
                cache.For(tenant);
                cache.RecordHit(tenant);
            }
        });
        int total = 0;
        foreach (var t in Tenants) total += cache.Hits(t);
        Console.WriteLine($"  sessions opened        {FixedTenantCache.SessionsOpened} (one per tenant)");
        Console.WriteLine($"  hits counted           {total:N0} of {Threads * HitsPerThread:N0}");
        if (FixedTenantCache.SessionsOpened != Tenants.Length || total != Threads * HitsPerThread)
            throw new Exception("FAIL: the fix does not hold");
        Console.WriteLine("  PASS");
    }

    // ── 5. AddOrUpdate is atomic; its delegate is a CAS retry loop ──────────
    static void AddOrUpdateDelegateRuns()
    {
        var d = new ConcurrentDictionary<string, int>();
        int calls = 0, ops = 0;
        Parallel(_ =>
        {
            for (int i = 0; i < HitsPerThread; i++)
            {
                d.AddOrUpdate("k", 1, (_, n) => { Interlocked.Increment(ref calls); return n + 1; });
                Interlocked.Increment(ref ops);
            }
        });
        Console.WriteLine($"  {ops:N0} calls, update delegate ran {calls:N0} times ({calls / (double)ops:F2}x), final value {d["k"]:N0}");
    }

    // ── 7. Lazy caches a thrown factory exception — or does not ─────────────
    static void LazyFailureModes()
    {
        foreach (var mode in new[] { LazyThreadSafetyMode.ExecutionAndPublication, LazyThreadSafetyMode.PublicationOnly })
        {
            int calls = 0;
            var lazy = new Lazy<string>(() => { calls++; if (calls < 3) throw new InvalidOperationException("boom " + calls); return "ok after " + calls; }, mode);
            var seen = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                try { seen.Add(lazy.Value); } catch (Exception e) { seen.Add("threw: " + e.Message); }
            }
            Console.WriteLine($"  {mode,-24} factory ran {calls}x   {string.Join(" | ", seen)}");
        }
    }

    public static void Main()
    {
        Console.WriteLine($"cores {Environment.ProcessorCount}, {Threads} threads, {HitsPerThread:N0} iterations each");
        Console.WriteLine("\n=== 1. the broken cache ===");
        Broken();
        Console.WriteLine("\n=== 2. one key, two threads, every access logged ===");
        LogInterleaving();
        Console.WriteLine("\n=== 3. GetOrAdd factory runs, 200 fresh keys, 4 threads ===");
        FactoryRuns("factory does real work (SpinWait 200)", 200);
        FactoryRuns("factory is trivial", 0);
        Console.WriteLine("\n=== 4. the fixed cache, same pressure ===");
        Fixed();
        Console.WriteLine("\n=== 5. AddOrUpdate: one atomic operation, a delegate that reruns ===");
        AddOrUpdateDelegateRuns();
        Console.WriteLine("\n=== 6. Lazy<T> and a factory that throws twice before succeeding ===");
        LazyFailureModes();
    }
}
