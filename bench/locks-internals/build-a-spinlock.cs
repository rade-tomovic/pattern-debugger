// Evidence for /systems/locks-internals/build-a-spinlock/ — run with:
//   dotnet run bench/locks-internals/build-a-spinlock.cs -c Release
// Twelve lines of spinlock, checked for correctness against `lock` (Monitor)
// at 1x and 2x the machine's core count, then compared on ONE axis that
// needs no stopwatch: who actually gets the lock next. No Stopwatch and no
// CPU-time sampling anywhere in this file — the fairness question is
// answered entirely by counting owner changes, which is exact and needs no
// timing to be meaningful.
using System.Runtime.CompilerServices;

// ── the whole thing ─────────────────────────────────────────────────────────
// _state is 0 when free and 1 when held. CompareExchange writes 1 only if it
// reads 0, and reports what it saw. That single instruction IS the lock.
sealed class NaiveSpinLock
{
    int _state;

    public void Enter()
    {
        while (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
        { }                                          // burn a core until it frees up
    }

    public void Exit() => Volatile.Write(ref _state, 0);   // release-store: 1 -> 0
}

// The same lock, with the one change that matters: tell the CPU and then the
// OS scheduler that we are spinning. SpinWait escalates pause -> Thread.Yield
// -> Sleep(0) -> Sleep(1) as the wait gets longer.
sealed class PoliteSpinLock
{
    int _state;

    public void Enter()
    {
        var spin = new SpinWait();
        while (Interlocked.CompareExchange(ref _state, 1, 0) != 0) spin.SpinOnce();
    }

    public void Exit() => Volatile.Write(ref _state, 0);
}

static class Bench
{
    static long _count;
    static readonly NaiveSpinLock Naive = new();
    static readonly PoliteSpinLock Polite = new();
    static readonly object Gate = new();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void WithNaive(int n) { for (int i = 0; i < n; i++) { Naive.Enter(); try { _count++; } finally { Naive.Exit(); } } }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void WithPolite(int n) { for (int i = 0; i < n; i++) { Polite.Enter(); try { _count++; } finally { Polite.Exit(); } } }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void WithMonitor(int n) { for (int i = 0; i < n; i++) lock (Gate) _count++; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void WithNothing(int n) { for (int i = 0; i < n; i++) _count++; }

    // ── who gets the lock next? ─────────────────────────────────────────────
    // Every acquisition records the owner. If the owner changed since last
    // time, that is a handoff to a different thread; if it did not, the same
    // thread got the lock again and the lock word never had to leave its core.
    static int _lastOwner; static long _handoffs;
    static void Note() { int me = Environment.CurrentManagedThreadId; if (me != _lastOwner) { _handoffs++; _lastOwner = me; } _count++; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void NaiveNoted(int n) { for (int i = 0; i < n; i++) { Naive.Enter(); try { Note(); } finally { Naive.Exit(); } } }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PoliteNoted(int n) { for (int i = 0; i < n; i++) { Polite.Enter(); try { Note(); } finally { Polite.Exit(); } } }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void MonitorNoted(int n) { for (int i = 0; i < n; i++) lock (Gate) Note(); }

    static void RunToCompletion(Action<int> body, int threads, int totalOps)
    {
        int per = totalOps / threads;
        var ts = new Thread[threads];
        var start = new ManualResetEventSlim(false);
        for (int t = 0; t < threads; t++) { ts[t] = new Thread(() => { start.Wait(); body(per); }) { IsBackground = true }; ts[t].Start(); }
        Thread.Sleep(20);                       // let every thread reach the gate
        start.Set();
        foreach (var th in ts) th.Join();
    }

    public static void Main()
    {
        int cores = Environment.ProcessorCount;

        // ── 1. is it actually a lock, at the machine's own core count? ──────
        const int Ops = 2_000_000;
        foreach (var (name, body) in new (string, Action<int>)[] { ("no lock", WithNothing), ("NaiveSpinLock", WithNaive), ("PoliteSpinLock", WithPolite), ("lock (Monitor)", WithMonitor) })
        {
            _count = 0;
            RunToCompletion(body, cores, Ops);
            Console.WriteLine($"  {name,-16} {cores} threads x {Ops / cores:N0} increments -> {_count,10:N0}   {(_count == Ops ? "correct" : $"LOST {Ops - _count:N0}")}");
        }
        if (_count != Ops) throw new Exception("FAIL: Monitor lost an update");

        // ── 2. does it stay correct oversubscribed — more threads than cores? ─
        int over = cores * 2;
        Console.WriteLine($"\n  oversubscribed: {over} threads on {cores} cores");
        foreach (var (name, body) in new (string, Action<int>)[] { ("NaiveSpinLock", WithNaive), ("PoliteSpinLock", WithPolite), ("lock (Monitor)", WithMonitor) })
        {
            _count = 0;
            RunToCompletion(body, over, Ops);
            Console.WriteLine($"  {name,-16} {over} threads x {Ops / over:N0} increments -> {_count,10:N0}   {(_count == Ops ? "correct" : $"LOST {Ops - _count:N0}")}");
        }
        if (_count != Ops) throw new Exception("FAIL: a lock lost an update oversubscribed");

        // ── 3. fairness: how often does the lock change hands? ──────────────
        // Exact and needs no timing: count acquisitions and owner changes,
        // both incremented under the very lock being measured, so counting
        // costs no extra synchronization of its own.
        Console.WriteLine($"\n  handoffs, {cores} threads x {Ops / cores:N0} acquisitions each lock");
        Console.WriteLine($"  {"lock",-16} {"acquisitions",13} {"owner changes",14} {"acq per turn",13}");
        foreach (var (name, body) in new (string, Action<int>)[] { ("NaiveSpinLock", NaiveNoted), ("PoliteSpinLock", PoliteNoted), ("lock (Monitor)", MonitorNoted) })
        {
            _handoffs = 0; _lastOwner = 0; _count = 0;
            RunToCompletion(body, cores, Ops);
            Console.WriteLine($"  {name,-16} {_count,13:N0} {_handoffs,14:N0} {(double)_count / _handoffs,13:F1}");
        }

        Console.WriteLine($"\nchecksum {_count}");
        Console.WriteLine("PASS");
    }
}
