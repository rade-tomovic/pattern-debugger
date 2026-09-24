// What a process is made of, read out of /proc, plus the one address fact that matters:
// every thread sees the same heap object at the same address and its own stack somewhere else.
// Also asks glibc directly for the real stack size and guard size it built for a thread,
// rather than inferring them from /proc/self/maps (adjacent anonymous mappings with the same
// permissions get merged into one line there, which makes a single map entry an unreliable
// way to measure "the stack").
#:property AllowUnsafeBlocks=true
using System.Runtime.InteropServices;

[DllImport("libpthread.so.0", SetLastError = true)]
static extern int pthread_getattr_np(nint thread, nint attr);
[DllImport("libpthread.so.0")]
static extern int pthread_attr_getstack(nint attr, out nint stackaddr, out nuint stacksize);
[DllImport("libpthread.so.0")]
static extern int pthread_attr_getguardsize(nint attr, out nuint guardsize);
[DllImport("libpthread.so.0")]
static extern nint pthread_self();

(nint addr, nuint size, nuint guard) StackInfo()
{
    nint attr = Marshal.AllocHGlobal(128);
    pthread_getattr_np(pthread_self(), attr);
    pthread_attr_getstack(attr, out nint stackaddr, out nuint stacksize);
    pthread_attr_getguardsize(attr, out nuint guardsize);
    Marshal.FreeHGlobal(attr);
    return (stackaddr, stacksize, guardsize);
}

foreach (var line in File.ReadAllLines("/proc/self/status"))
    if (line.StartsWith("Threads:") || line.StartsWith("VmSize:") || line.StartsWith("VmRSS:"))
        Console.WriteLine("  " + line.Replace("\t", " "));
Console.WriteLine($"  open handles (fds):   {Directory.GetFiles("/proc/self/fd").Length}");
Console.WriteLine($"  mapped regions:       {File.ReadAllLines("/proc/self/maps").Length}");
Console.WriteLine();

string mapping = "";
(nint addr, nuint size, nuint guard) workerAStack = default;
int[] shared = new int[4];                       // one heap object
GCHandle pin = GCHandle.Alloc(shared, GCHandleType.Pinned);
nint heapAddr = pin.AddrOfPinnedObject();
var everyoneReported = new Barrier(3);           // hold all three stacks alive at once

void Report(string who, bool captureStack = false)
{
    int local = 0;                               // a stack slot owned by THIS thread alone
    unsafe
    {
        nint addr = (nint)(&local);
        Console.WriteLine($"  {who,-8}  stack slot @ 0x{addr:x12}   shared array @ 0x{heapAddr:x12}");
        if (captureStack)
        {
            workerAStack = StackInfo();
            mapping = Neighbourhood(workerAStack.addr);
        }
    }
    Interlocked.Increment(ref shared[0]);        // all three reach the same bytes
    everyoneReported.SignalAndWait();
}

// the /proc/self/maps entries around an address, with their sizes
static string Neighbourhood(nint addr)
{
    var lines = File.ReadAllLines("/proc/self/maps");
    for (int i = 0; i < lines.Length; i++)
    {
        var r = lines[i].Split(' ')[0].Split('-');
        long lo = (long)Convert.ToUInt64(r[0], 16), hi = (long)Convert.ToUInt64(r[1], 16);
        if (addr >= lo && addr < hi)
            return string.Join("\n", new[] { lines[Math.Max(0, i - 1)], lines[i] }.Select(l =>
            {
                var q = l.Split(' ')[0].Split('-');
                long a = (long)Convert.ToUInt64(q[0], 16), b = (long)Convert.ToUInt64(q[1], 16);
                return $"  {l.Split(' ')[0]} {l.Split(' ')[1]}   {(b - a) / 1024,7} KiB";
            }));
    }
    return "(not found)";
}

var a = new Thread(() => Report("worker A", captureStack: true));
var b = new Thread(() => Report("worker B"));
a.Start(); b.Start();
Report("main");
a.Join(); b.Join();
Console.WriteLine($"  shared[0] = {shared[0]}");
Console.WriteLine();
Console.WriteLine($"  worker A's stack, as glibc built it: base 0x{workerAStack.addr:x} " +
                   $"size {(ulong)workerAStack.size / 1024} KiB  guard {(ulong)workerAStack.guard / 1024} KiB");
Console.WriteLine("  /proc/self/maps around that base address:");
Console.WriteLine(mapping);
pin.Free();

// Private by convention, not by hardware: one address space means main can reach
// straight into a worker's live stack frame if the worker hands over the address.
Console.WriteLine();
nint borrowed = 0;
var handoff = new Barrier(2);
var worker = new Thread(() =>
{
    int secret = 41;                             // a local, on the worker's own stack
    unsafe { borrowed = (nint)(&secret); }
    handoff.SignalAndWait();                     // main writes through the pointer here
    handoff.SignalAndWait();
    Console.WriteLine($"  worker's local is now {secret} — main reached into this thread's stack");
});
worker.Start();
handoff.SignalAndWait();
unsafe { *(int*)borrowed = 42; }
handoff.SignalAndWait();
worker.Join();
