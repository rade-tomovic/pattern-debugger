// Evidence for /systems/locks-internals/ — what a waiter on a
// System.Threading.SpinLock actually does while it waits, versus a bare CAS
// loop with no backoff. Run with:
//   dotnet run bench/locks-internals/spinlock-parks.cs -c Release
// One thread holds a lock for a fixed interval; one other thread waits for
// it. No Stopwatch, no CPU-time sampling — the only output is a count:
// voluntary context switches recorded during that one wait. A voluntary
// switch means the thread itself asked to give up its core (a syscall like
// futex-wait, or a plain sched_yield/nanosleep) rather than being preempted.
// Zero would mean "never gave up the core, ever" for the whole wait.

static class SpinPark
{
    const int HoldMs = 1000;

    // Sum every task in the process: the waiter is not the main thread.
    static (long vol, long invol) Switches()
    {
        long v = 0, iv = 0;
        foreach (var dir in Directory.GetDirectories("/proc/self/task"))
            try
            {
                foreach (var line in File.ReadLines(Path.Combine(dir, "status")))
                {
                    if (line.StartsWith("voluntary_ctxt_switches:")) v += long.Parse(line.Split(':')[1].Trim());
                    else if (line.StartsWith("nonvoluntary_ctxt_switches:")) iv += long.Parse(line.Split(':')[1].Trim());
                }
            }
            catch (IOException) { }
        return (v, iv);
    }

    // `enter` blocks until it gets the lock; `exit` releases it. The holder
    // takes the lock, the waiter arrives 50 ms later and is stuck for the rest
    // of the hold, and everything is measured over the waiter's wait only.
    static void Measure(string name, Action enter, Action exit)
    {
        var holderHasIt = new ManualResetEventSlim(false);
        var holder = new Thread(() => { enter(); holderHasIt.Set(); Thread.Sleep(HoldMs); exit(); }) { IsBackground = true };
        holder.Start();
        holderHasIt.Wait();
        Thread.Sleep(50);                       // the holder is definitely inside

        var (v0, i0) = Switches();
        var waiter = new Thread(() => { enter(); exit(); }) { IsBackground = true };
        waiter.Start();
        waiter.Join();
        var (v1, i1) = Switches();
        holder.Join();
        Console.WriteLine($"  {name,-22} voluntary context switches during the wait: {v1 - v0,4:N0}   involuntary: {i1 - i0,4:N0}");
    }

    sealed class NaiveSpinLock
    {
        int _state;
        public void Enter() { while (Interlocked.CompareExchange(ref _state, 1, 0) != 0) { } }
        public void Exit() => Volatile.Write(ref _state, 0);
    }

    public static void Main()
    {
        Console.WriteLine("=== one waiter, one holder ===");

        var mon = new object();
        Measure("lock (Monitor)", () => Monitor.Enter(mon), () => Monitor.Exit(mon));

        var spin = new SpinLock(enableThreadOwnerTracking: false);
        Measure("SpinLock", () => { bool t = false; spin.Enter(ref t); }, () => spin.Exit(false));

        var naive = new NaiveSpinLock();
        Measure("NaiveSpinLock", naive.Enter, naive.Exit);

        Console.WriteLine("PASS");
    }
}
