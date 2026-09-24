// Evidence for /systems/locks-internals/ — reads the 4-byte header word that
// sits *behind* every object reference, in each of the states `lock` puts it
// in. Run with:
//   dotnet run bench/locks-internals/object-header.cs -c Release --property:AllowUnsafeBlocks=true
// No timing here, so no bench lock needed.
unsafe static class Header
{
    // __makeref gives a TypedReference whose first field is the address of the
    // variable; one more dereference is the object itself.
    static nint AddressOf(object o) { TypedReference tr = __makeref(o); return **(nint**)&tr; }

    // On 64-bit CoreCLR the 8 bytes before the object are 4 bytes of padding
    // and then the header word. Little-endian, so the header is the high half.
    static uint HeaderWord(object o) => (uint)(*(long*)(AddressOf(o) - 8) >> 32);

    static void Show(string what, object o) => Console.WriteLine($"  {what,-44} 0x{HeaderWord(o):X8}");

    public static void Main()
    {
        Console.WriteLine($"  this thread's managed id = {Environment.CurrentManagedThreadId}");

        var a = new object();
        Show("fresh object, never locked", a);
        lock (a)
        {
            Show("depth 1", a);
            lock (a) { Show("depth 2", a); lock (a) Show("depth 3", a); }
        }
        Show("after every release", a);

        // A different thread takes it, so the id field is visibly the owner's.
        var b = new object();
        var t = new Thread(() =>
        {
            lock (b) Show($"held by thread {Environment.CurrentManagedThreadId}", b);
        });
        t.Start(); t.Join();

        // Ask for a hash code and the same word has to hold that instead.
        var c = new object();
        int h = c.GetHashCode();
        Console.WriteLine($"  c.GetHashCode() = {h} = 0x{h:X8}");
        Show("after GetHashCode(), never locked", c);
        lock (c) Show("...then locked: inflated to a sync block", c);
        Show("...and it stays inflated after release", c);

        // Contention inflates it too, with no hash code involved.
        var d = new object();
        lock (d)
        {
            var w = new Thread(() => { lock (d) { } });
            w.Start();
            Thread.Sleep(250);                       // let the waiter actually block
            Show("a second thread is blocked on it", d);
        }
        Thread.Sleep(100);
        Show("after the waiter got in and out", d);

        if ((HeaderWord(a) | HeaderWord(c) & 0x08000000) == 0) throw new Exception("FAIL: header did not change");
        Console.WriteLine("PASS");
    }
}
