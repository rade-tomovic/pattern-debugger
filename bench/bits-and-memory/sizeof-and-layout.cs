using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Four struct shapes, one question each: what does the runtime actually do
// with this field order? No timing anywhere in this file — every number
// below is a size, an offset, or a byte count the runtime states outright.
struct Naive   { public byte Flag; public long Ticks; public byte Kind; }
struct Packed  { public long Ticks; public byte Flag; public byte Kind; }
struct Point3  { public float X, Y, Z; }
struct WithRef { public byte Flag; public string Name; }

// Same fields as WithRef, but explicitly asking for declaration order.
[StructLayout(LayoutKind.Sequential)]
struct SeqRef { public byte Flag; public string Name; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct Squeezed { public byte Flag; public long Ticks; public byte Kind; }

static class Layout
{
    // byte distance from the start of a struct to one of its fields
    static nint Off<T>(ref T origin, ref byte field)
        => Unsafe.ByteOffset(ref Unsafe.As<T, byte>(ref origin), ref field);

    static long Allocated(Action f)
    {
        long before = GC.GetTotalAllocatedBytes(precise: true);
        f();
        return GC.GetTotalAllocatedBytes(precise: true) - before;
    }

    public static void Main()
    {
        // ---- part 1: sizes and field offsets, asked of the runtime itself ----
        Naive n = default; Packed p = default; Point3 t = default; WithRef w = default; SeqRef q = default;
        Console.WriteLine($"Naive    size={Unsafe.SizeOf<Naive>(),2}  Flag@{Off(ref n, ref n.Flag)} Ticks@{Off(ref n, ref Unsafe.As<long, byte>(ref n.Ticks))} Kind@{Off(ref n, ref n.Kind)}");
        Console.WriteLine($"Packed   size={Unsafe.SizeOf<Packed>(),2}  Ticks@{Off(ref p, ref Unsafe.As<long, byte>(ref p.Ticks))} Flag@{Off(ref p, ref p.Flag)} Kind@{Off(ref p, ref p.Kind)}");
        Console.WriteLine($"Point3   size={Unsafe.SizeOf<Point3>(),2}  X@{Off(ref t, ref Unsafe.As<float, byte>(ref t.X))} Y@{Off(ref t, ref Unsafe.As<float, byte>(ref t.Y))} Z@{Off(ref t, ref Unsafe.As<float, byte>(ref t.Z))}");
        Console.WriteLine($"WithRef  size={Unsafe.SizeOf<WithRef>(),2}  Flag@{Off(ref w, ref w.Flag)} Name@{Off(ref w, ref Unsafe.As<string, byte>(ref w.Name))}");
        Console.WriteLine($"SeqRef   size={Unsafe.SizeOf<SeqRef>(),2}  Flag@{Off(ref q, ref q.Flag)} Name@{Off(ref q, ref Unsafe.As<string, byte>(ref q.Name))}   (LayoutKind.Sequential requested)");
        Squeezed z = default;
        Console.WriteLine($"Squeezed size={Unsafe.SizeOf<Squeezed>(),2}  Flag@{Off(ref z, ref z.Flag)} Ticks@{Off(ref z, ref Unsafe.As<long, byte>(ref z.Ticks))} Kind@{Off(ref z, ref z.Kind)}   (Pack = 1)");

        // ---- part 2: the marshalled layout is a second, different answer ----
        Console.WriteLine($"\nSeqRef managed layout:    Flag@{Off(ref q, ref q.Flag)} Name@{Off(ref q, ref Unsafe.As<string, byte>(ref q.Name))}");
        Console.WriteLine($"SeqRef marshalled layout: Flag@{Marshal.OffsetOf<SeqRef>("Flag")} Name@{Marshal.OffsetOf<SeqRef>("Name")}   (Marshal.OffsetOf)");

        // ---- part 3: what an array of them actually costs, to the byte ----
        object keep = null!;
        Console.WriteLine($"\nnew Naive[100]    allocated {Allocated(() => keep = new Naive[100]),6:N0} bytes");
        Console.WriteLine($"new Packed[100]   allocated {Allocated(() => keep = new Packed[100]),6:N0} bytes");
        Console.WriteLine($"new Squeezed[100] allocated {Allocated(() => keep = new Squeezed[100]),6:N0} bytes");
        GC.KeepAlive(keep);

        // ---- part 4: how many elements of each share one 64-byte cache line ----
        const int line = 64;
        Console.WriteLine($"\nelements per {line}-byte cache line (line size / element size):");
        Console.WriteLine($"  Naive[]    {(double)line / Unsafe.SizeOf<Naive>(),5:F2}");
        Console.WriteLine($"  Packed[]   {(double)line / Unsafe.SizeOf<Packed>(),5:F2}");
        Console.WriteLine($"  Squeezed[] {(double)line / Unsafe.SizeOf<Squeezed>(),5:F2}");
    }
}
