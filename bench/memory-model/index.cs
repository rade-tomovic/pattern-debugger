// Evidence for /systems/memory-model/ — run with:
//   scripts/bench-lock.sh dotnet run bench/memory-model/index.cs -c Release
//
// Four experiments, one file:
//   1. atomicity — a 16-byte struct assignment is several stores; a long is one
//   2. ordering  — the message-passing litmus (StoreStore + LoadLoad), which x86 forbids
//   3. ordering  — the store-buffer litmus (StoreLoad), which x86 permits, five ways
//   4. cost      — what a plain store, a volatile store and a full fence actually cost
using System.Diagnostics;
using System.Runtime.CompilerServices;

struct Pair { public long Lo, Hi; }          // 16 bytes: no hardware stores this in one go

static class MM
{
    // ── 1. atomicity ────────────────────────────────────────────────────────
    static Pair pair;
    static long word;
    static volatile bool running;

    static void Atomicity()
    {
        Console.WriteLine("=== 1. atomicity: is one C# assignment one machine store? ===");

        long reads = 0, torn = 0;
        running = true;
        var w = new Thread(() => { long n = 1; while (running) { pair = new Pair { Lo = n, Hi = n }; n++; } }) { IsBackground = true };
        var r = new Thread(() => { while (running) { Pair p = pair; reads++; if (p.Lo != p.Hi) torn++; } }) { IsBackground = true };
        w.Start(); r.Start(); Thread.Sleep(2000); running = false; w.Join(); r.Join();
        Console.WriteLine($"  16-byte struct (two longs written together)  {torn,14:N0} torn of {reads,14:N0} reads  ({torn * 100.0 / reads:F2}%)");

        long reads2 = 0, torn2 = 0;
        running = true;
        var w2 = new Thread(() => { while (running) { word = 0x0000_0000_0000_0000L; word = 0x7FFF_FFFF_FFFF_FFFFL; } }) { IsBackground = true };
        var r2 = new Thread(() => { while (running) { long v = word; reads2++; if (v != 0L && v != 0x7FFF_FFFF_FFFF_FFFFL) torn2++; } }) { IsBackground = true };
        w2.Start(); r2.Start(); Thread.Sleep(2000); running = false; w2.Join(); r2.Join();
        Console.WriteLine($"  aligned 8-byte long (one store)              {torn2,14:N0} torn of {reads2,14:N0} reads  ({torn2 * 100.0 / reads2:F2}%)");
    }

    // ── 2 & 3. the litmus tests ─────────────────────────────────────────────
    // Shared state, re-initialised by the referee thread between every trial.
    static int x, y, r1, r2;
    static volatile int vx, vy;
    static readonly object gate = new();

    // Each worker body is NoInlining so its emitted code can be dumped with
    // DOTNET_JitDisasm and checked for compiler reordering.
    [MethodImpl(MethodImplOptions.NoInlining)] static void MpWrite()  { x = 42; y = 1; }              // publish
    [MethodImpl(MethodImplOptions.NoInlining)] static void MpRead()   { r1 = y; r2 = x; }             // observe
    [MethodImpl(MethodImplOptions.NoInlining)] static void SbA()      { x = 1; r1 = y; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void SbB()      { y = 1; r2 = x; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void SbAVol()   { vx = 1; r1 = vy; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void SbBVol()   { vy = 1; r2 = vx; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void SbAVolCls(){ Volatile.Write(ref x, 1); r1 = Volatile.Read(ref y); }
    [MethodImpl(MethodImplOptions.NoInlining)] static void SbBVolCls(){ Volatile.Write(ref y, 1); r2 = Volatile.Read(ref x); }
    [MethodImpl(MethodImplOptions.NoInlining)] static void SbAFence() { x = 1; Interlocked.MemoryBarrier(); r1 = y; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void SbBFence() { y = 1; Interlocked.MemoryBarrier(); r2 = x; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void SbALock()  { lock (gate) { x = 1; r1 = y; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static void SbBLock()  { lock (gate) { y = 1; r2 = x; } }

    /// Runs `iters` trials of a two-thread litmus test and counts the trials whose
    /// outcome matched `anomaly`. A Barrier keeps the two workers in lockstep: without
    /// it they drift apart and the interesting window never lines up.
    static long Litmus(string name, int iters, Action a, Action b, Func<bool> anomaly, bool volatileReset)
    {
        long hits = 0; long first = -1;
        var bar = new Barrier(3);
        var t1 = new Thread(() => { for (int i = 0; i < iters; i++) { bar.SignalAndWait(); a(); bar.SignalAndWait(); } });
        var t2 = new Thread(() => { for (int i = 0; i < iters; i++) { bar.SignalAndWait(); b(); bar.SignalAndWait(); } });
        t1.Start(); t2.Start();
        for (int i = 0; i < iters; i++)
        {
            if (volatileReset) { vx = 0; vy = 0; } else { x = 0; y = 0; }
            r1 = -1; r2 = -1;
            bar.SignalAndWait();                 // release both workers into the window
            bar.SignalAndWait();                 // both have finished; the state is stable
            if (anomaly()) { hits++; if (first < 0) first = i; }
        }
        t1.Join(); t2.Join();
        Console.WriteLine($"  {name,-46} {hits,9:N0} / {iters:N0}   ({hits * 100.0 / iters,7:F4}%)  first at trial {first}");
        return hits;
    }

    // ── 4. what ordering costs on one thread ────────────────────────────────
    static int sink;
    [MethodImpl(MethodImplOptions.NoInlining)] static void CostPlain(int n)    { for (int i = 0; i < n; i++) { x = i; sink = y; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static void CostVolatile(int n) { for (int i = 0; i < n; i++) { Volatile.Write(ref x, i); sink = Volatile.Read(ref y); } }
    [MethodImpl(MethodImplOptions.NoInlining)] static void CostFence(int n)    { for (int i = 0; i < n; i++) { x = i; Interlocked.MemoryBarrier(); sink = y; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static void CostXchg(int n)     { for (int i = 0; i < n; i++) { Interlocked.Exchange(ref x, i); sink = y; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static void CostLock(int n)     { for (int i = 0; i < n; i++) { lock (gate) { x = i; sink = y; } } }

    static double Median(double[] v) { var c = (double[])v.Clone(); Array.Sort(c); return c[c.Length / 2]; }
    static string All(double[] v) => string.Join(" ", v.Select(t => t.ToString("F1")));

    public static void Main()
    {
        Console.WriteLine($"JIT optimiser disabled: {typeof(MM).Assembly.GetCustomAttributes(typeof(DebuggableAttribute), false) is [DebuggableAttribute d] && d.IsJITOptimizerDisabled}");
        Atomicity();

        const int Trials = 500_000;
        Console.WriteLine($"\n=== 2. ordering: message passing — anomaly is r1==1 (saw the flag) && r2!=42 (missed the data) ===");
        Litmus("plain fields, no barriers anywhere", Trials, MpWrite, MpRead, () => r1 == 1 && r2 != 42, false);

        Console.WriteLine($"\n=== 3. ordering: the store buffer — anomaly is r1==0 && r2==0 (neither thread saw the other's store) ===");
        Litmus("plain int fields", Trials, SbA, SbB, () => r1 == 0 && r2 == 0, false);
        Litmus("volatile int fields", Trials, SbAVol, SbBVol, () => r1 == 0 && r2 == 0, true);
        Litmus("Volatile.Write / Volatile.Read", Trials, SbAVolCls, SbBVolCls, () => r1 == 0 && r2 == 0, false);
        Litmus("Interlocked.MemoryBarrier() between them", Trials, SbAFence, SbBFence, () => r1 == 0 && r2 == 0, false);
        Litmus("lock (gate) around both lines", Trials, SbALock, SbBLock, () => r1 == 0 && r2 == 0, false);

        const int Ops = 20_000_000, Runs = 9;
        Console.WriteLine($"\n=== 4. what ordering costs, single thread, {Ops:N0} store+load pairs ===");
        double[] p = new double[Runs], v = new double[Runs], f = new double[Runs], xc = new double[Runs], lk = new double[Runs];
        for (int i = 0; i < 3; i++) { CostPlain(Ops); CostVolatile(Ops); CostFence(Ops); CostXchg(Ops); CostLock(Ops); }
        var sw = new Stopwatch();
        for (int r = 0; r < Runs; r++)                                        // interleaved, not five separate passes
        {
            sw.Restart(); CostPlain(Ops);    p[r]  = sw.Elapsed.TotalMilliseconds;
            sw.Restart(); CostVolatile(Ops); v[r]  = sw.Elapsed.TotalMilliseconds;
            sw.Restart(); CostFence(Ops);    f[r]  = sw.Elapsed.TotalMilliseconds;
            sw.Restart(); CostXchg(Ops);     xc[r] = sw.Elapsed.TotalMilliseconds;
            sw.Restart(); CostLock(Ops);     lk[r] = sw.Elapsed.TotalMilliseconds;
        }
        void Row(string n, double[] t) => Console.WriteLine($"  {n,-40} {Median(t),7:F1} ms  {Median(t) * 1e6 / Ops,6:F2} ns/op   runs: {All(t)}");
        Row("plain store + plain load", p);
        Row("Volatile.Write + Volatile.Read", v);
        Row("store + Interlocked.MemoryBarrier + load", f);
        Row("Interlocked.Exchange + load", xc);
        Row("lock { store; load }", lk);
        Console.WriteLine($"  checksum {sink + x + y + r1 + r2}");
    }
}
