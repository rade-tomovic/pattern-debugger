// The JIT's real machine code for a Treiber push and for the same CAS shape on
// an int field. Dumped with DOTNET_JitDisasm — see /systems/lock-free-structures/
// for the trimmed, annotated output.
using System.Runtime.CompilerServices;

public sealed class TreiberStack
{
    sealed class Node(int value) { public readonly int Value = value; public Node? Next; }
    Node? _head;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Push(int value)
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

    int _counter;

    // The same CAS shape, on an int field instead of a reference field.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int CasInt(int add)
    {
        int old = Volatile.Read(ref _counter);
        while (true)
        {
            int seen = Interlocked.CompareExchange(ref _counter, old + add, old);
            if (seen == old) return old + add;
            old = seen;
        }
    }

    public bool TryPop(out int value)
    {
        Node? head = Volatile.Read(ref _head);
        while (head is not null)
        {
            Node? seen = Interlocked.CompareExchange(ref _head, head.Next, head);
            if (seen == head) { value = head.Value; return true; }
            head = seen;
        }
        value = 0; return false;
    }
}

public static class Program
{
    public static void Main()
    {
        var s = new TreiberStack();
        for (int i = 0; i < 100; i++) { s.Push(i); s.CasInt(i); }
        int sum = 0;
        while (s.TryPop(out int v)) sum += v;
        Console.WriteLine($"sum {sum}");
    }
}
