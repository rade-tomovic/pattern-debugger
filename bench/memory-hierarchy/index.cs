// Evidence for /systems/memory-hierarchy/ — run with:
//   dotnet run bench/memory-hierarchy/index.cs
//
// Real per-element cost, not sizeof: GC.GetAllocatedBytesForCurrentThread() counts
// the actual bytes the allocator handed out.
static class NodeCost
{
    public static void Main()
    {
        const int N = 1_000_000;

        long b0 = GC.GetAllocatedBytesForCurrentThread();
        var list = new List<int>(N);
        for (int i = 0; i < N; i++) list.Add(i);
        long b1 = GC.GetAllocatedBytesForCurrentThread();

        var ll = new LinkedList<int>();
        for (int i = 0; i < N; i++) ll.AddLast(i);
        long b2 = GC.GetAllocatedBytesForCurrentThread();

        long listBytes = (b1 - b0) / N, nodeBytes = (b2 - b1) / N;
        Console.WriteLine($"List<int>:       {listBytes} B/element");
        Console.WriteLine($"LinkedList<int>: {nodeBytes} B/node");
        if (listBytes != 4) throw new Exception($"FAIL: expected 4 B/element, got {listBytes}");
        if (nodeBytes != 48) throw new Exception($"FAIL: expected 48 B/node, got {nodeBytes}");
        Console.WriteLine($"PASS: LinkedList<int> costs {nodeBytes / (double)listBytes:F0}x the bytes per element");
    }
}
