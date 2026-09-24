// Evidence for /systems/lock-free-structures/treiber-stack/ — how many 64-byte cache
// lines four TreiberStack objects land on, allocated back-to-back vs spaced apart.
//   dotnet run bench/lock-free-structures/stack-layout.cs
using System.Runtime.CompilerServices;

// The same stack as treiber-stack.cs, trimmed to what affects its layout:
// one reference field on top of the object header.
public sealed class TreiberStack<T>
{
    sealed class Node(T value) { public readonly T Value = value; public Node? Next; }
    Node? _head;
    public void Push(T value)
    {
        var node = new Node(value);
        Node? head = Volatile.Read(ref _head);
        while (true)
        {
            node.Next = head;
            Node? seen = Interlocked.CompareExchange(ref _head, node, head);
            if (seen == head) return;
            head = seen;
        }
    }
}

public static class Layout
{
    static object[]? _spacers;
    // The object reference IS the address; read them back to back, before any
    // collection can move things, and compare the deltas.
    static nint Addr(object o) => Unsafe.As<object, nint>(ref o);
    static int LinesTouched(TreiberStack<int>[] a) => new HashSet<nint>(a.Select(x => Addr(x) / 64)).Count;

    public static void Main()
    {
        var warm = new TreiberStack<int>(); warm.Push(1);

        long a0 = GC.GetAllocatedBytesForCurrentThread();
        var keep = new TreiberStack<int>[10_000];
        for (int i = 0; i < 10_000; i++) keep[i] = new TreiberStack<int>();
        long a1 = GC.GetAllocatedBytesForCurrentThread();
        // minus the 8-byte array slot each one is stored in
        Console.WriteLine($"bytes per TreiberStack object: {(a1 - a0 - 8L * 10_000) / 10_000.0:F1}");

        var packed = new TreiberStack<int>[4];
        for (int t = 0; t < 4; t++) packed[t] = new TreiberStack<int>();
        Console.WriteLine($"back to back: address deltas {string.Join(" ", Enumerable.Range(1, 3).Select(t => Addr(packed[t]) - Addr(packed[t - 1])))}"
                          + $"   64-byte lines touched by the 4 stacks: {LinesTouched(packed)}");

        var spaced = new TreiberStack<int>[4];
        _spacers = new object[4];
        for (int t = 0; t < 4; t++) { spaced[t] = new TreiberStack<int>(); _spacers[t] = new byte[128]; }
        Console.WriteLine($"with a spacer: address deltas {string.Join(" ", Enumerable.Range(1, 3).Select(t => Addr(spaced[t]) - Addr(spaced[t - 1])))}"
                          + $"   64-byte lines touched by the 4 stacks: {LinesTouched(spaced)}");
        GC.KeepAlive(keep);
    }
}
