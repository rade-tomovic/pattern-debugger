// What is actually behind the single ".NET Timer" thread — run with:
//   dotnet run bench/threads-and-scheduling/timer-queues.cs
//
// who-waits.cs shows one .NET Timer thread servicing a hundred thousand pending
// Task.Delays. This asks what that thread is walking: how many timer queues the
// runtime keeps, and what shape each one is. Reflection into System.Private.CoreLib,
// so it is the runtime's own data structure, not a description of one.
// Not a timing benchmark — no numbers here move with load.
using System.Reflection;

const BindingFlags All = BindingFlags.Instance | BindingFlags.Static
                       | BindingFlags.Public | BindingFlags.NonPublic;

var corelib = typeof(object).Assembly;
var tq = corelib.GetType("System.Threading.TimerQueue")!;
var tqt = corelib.GetType("System.Threading.TimerQueueTimer")!;

var instances = (Array)tq.GetProperty("Instances", All)!.GetValue(null)!;
Console.WriteLine($"cores {Environment.ProcessorCount}   TimerQueue.Instances.Length {instances.Length}");
Console.WriteLine($"  fields holding pending timers : "
    + string.Join(", ", tq.GetFields(All).Where(f => f.FieldType == tqt).Select(f => f.Name)));
Console.WriteLine($"  TimerQueueTimer link fields   : "
    + string.Join(", ", tqt.GetFields(All).Where(f => f.FieldType == tqt).Select(f => f.Name)));

// put 1,000 deadlines in flight and count what each queue is holding
var cts = new CancellationTokenSource();
var pending = new Task[1000];
for (int i = 0; i < pending.Length; i++) pending[i] = Task.Delay(60_000, cts.Token);
Thread.Sleep(300);

var next = tqt.GetField("_next", All)!;
var shortF = tq.GetField("_shortTimers", All)!;
var longF = tq.GetField("_longTimers", All)!;
int Len(object? head) { int n = 0; while (head is not null) { n++; head = next.GetValue(head); } return n; }

Console.WriteLine($"\n1000 pending Task.Delay(60s), all started from this one thread:");
for (int i = 0; i < instances.Length; i++)
{
    var q = instances.GetValue(i)!;
    Console.WriteLine($"  queue[{i}]   _shortTimers {Len(shortF.GetValue(q)),5}   _longTimers {Len(longF.GetValue(q)),5}");
}
cts.Cancel();
