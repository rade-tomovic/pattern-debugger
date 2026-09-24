// Evidence for /systems/threads-and-scheduling/ — run with:
//   dotnet run bench/threads-and-scheduling/who-waits.cs -c Release
//
// A thousand pending awaits on the timer queue, one socket receive genuinely
// parked in the kernel with nothing sent to it, then a listing of every OS
// thread the process actually has, read straight out of /proc/self/task.
using System.Net;
using System.Net.Sockets;

var pending = new Task[1000];
for (int i = 0; i < 1000; i++) pending[i] = Task.Delay(60_000);   // a deadline in the timer queue

// A loopback socket pair. Nothing is ever sent on it, so the receive cannot complete.
using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
listener.Listen(1);
using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
await client.ConnectAsync((IPEndPoint)listener.LocalEndPoint!);
using var server = await listener.AcceptAsync();

var buffer = new byte[16];
var receiving = client.ReceiveAsync(buffer, SocketFlags.None);   // parks in epoll, nothing to read

Thread.Sleep(300);
Console.WriteLine($"receive still pending? {!receiving.IsCompleted}");
Console.WriteLine($"{pending.Count(t => !t.IsCompleted)} pending awaits + 1 pending socket receive; OS threads in this process:");
foreach (var dir in Directory.GetDirectories("/proc/self/task").OrderBy(d => int.Parse(Path.GetFileName(d))))
    Console.WriteLine($"  tid {Path.GetFileName(dir),6}   {File.ReadAllText(Path.Combine(dir, "comm")).Trim()}");

// Same census with a hundred times the work in flight.
var many = new Task[100_000];
for (int i = 0; i < 100_000; i++) many[i] = Task.Delay(60_000);
Thread.Sleep(300);
Console.WriteLine($"\n{many.Count(t => !t.IsCompleted)} pending awaits: {Directory.GetDirectories("/proc/self/task").Length} OS threads");
