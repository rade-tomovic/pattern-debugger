// Evidence for /systems/parallelism-patterns/producer-consumer/ — run with:
//   DOTNET_GCHeapHardLimit=C000000 dotnet run bench/parallelism-patterns/producer-consumer.cs -c Release -- unbounded 0
//   DOTNET_GCHeapHardLimit=C000000 dotnet run bench/parallelism-patterns/producer-consumer.cs -c Release -- bounded 64
// 0xC000000 = 192 MiB of managed heap, which is this container standing in for a pod
// memory limit. The pipeline is identical in both modes except for the channel.
using System.Threading.Channels;

sealed class Msg
{
    public int Id;
    public byte[] Payload;                       // 8 KiB — a deserialized request body
    public Msg(int id) { Id = id; Payload = new byte[8192]; Payload[0] = (byte)id; }
}

static class Pipeline
{
    const int Items = 100_000;
    static int produced, consumed;
    static int peakDepth; static long peakHeap;
    public static long Sink;

    static async Task Produce(ChannelWriter<Msg> w)
    {
        try
        {
            for (int i = 0; i < Items; i++)
            {
                await w.WriteAsync(new Msg(i));  // unbounded: never waits. bounded: waits when full.
                Interlocked.Increment(ref produced);
            }
        }
        finally { w.Complete(); }                // without this a producer fault becomes a hang
    }

    static async Task Consume(ChannelReader<Msg> r)
    {
        await foreach (var m in r.ReadAllAsync())
        {
            Thread.SpinWait(1200);               // a fixed amount of CPU work: the slow stage
            Sink += m.Payload[0];
            Interlocked.Increment(ref consumed);
        }
    }

    public static async Task Main(string[] args)
    {
        bool bounded = args.Length > 0 && args[0] == "bounded";
        int cap = args.Length > 1 ? int.Parse(args[1]) : 64;
        Channel<Msg> ch = bounded
            ? Channel.CreateBounded<Msg>(new BoundedChannelOptions(cap) { FullMode = BoundedChannelFullMode.Wait })
            : Channel.CreateUnbounded<Msg>();
        Console.WriteLine($"mode: {(bounded ? $"bounded({cap})" : "unbounded")}   heap limit: {GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024)} MiB");
        Console.WriteLine(" sample  produced  consumed     depth   heap MB   gen2");

        var monitor = new Thread(() =>
        {
            int sample = 0;
            while (true)
            {
                Thread.Sleep(25);
                sample++;
                int p = Volatile.Read(ref produced), c = Volatile.Read(ref consumed);
                long mb = GC.GetTotalMemory(false) / (1024 * 1024);
                peakDepth = Math.Max(peakDepth, p - c); peakHeap = Math.Max(peakHeap, mb);
                Console.WriteLine($"  {sample,5}  {p,8}  {c,8}  {p - c,8}  {mb,8}  {GC.CollectionCount(2),5}");
                if (c >= Items) return;
            }
        }) { IsBackground = true };
        monitor.Start();

        try
        {
            var prod = Task.Run(() => Produce(ch.Writer));   // both stages genuinely concurrent
            var cons = Task.Run(() => Consume(ch.Reader));
            await Task.WhenAll(prod, cons);
            Console.WriteLine($"DONE   produced {produced:N0}   consumed {consumed:N0}   peak depth {peakDepth:N0}   peak heap {peakHeap} MB   gen2 collections {GC.CollectionCount(2)}   checksum {Sink}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAILED   produced {produced:N0}   consumed {consumed:N0}   peak depth {peakDepth:N0}   peak heap {peakHeap} MB   gen2 collections {GC.CollectionCount(2)}");
            Console.WriteLine(ex.GetType().FullName + ": " + ex.Message);
            Environment.Exit(1);
        }
    }
}
