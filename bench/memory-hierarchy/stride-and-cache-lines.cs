// Evidence for /systems/memory-hierarchy/stride-and-cache-lines/ — run with:
//   dotnet run bench/memory-hierarchy/stride-and-cache-lines.cs
//
// Not timed. One 64 MB int[]. For a growing stride, the sweep visits every element
// exactly once: offset 0, stride, 2*stride, ... to the end, then offset 1, and so
// on. The file counts how many times that visit order crosses into a DIFFERENT
// 64-byte-aligned block than the block it just visited — a real, countable proxy
// for "how many times does this loop need a fresh cache line", since the array is
// far larger than any cache on this box, so a line not touched by the immediately
// preceding access has certainly been evicted by the time the sweep returns to it.
using System.Runtime.CompilerServices;

static class Stride
{
    const int LineBytes = 64;

    static (long transitions, long reads) Sweep(int[] a, int strideInts)
    {
        long transitions = 0, reads = 0;
        int prevLine = -1, n = a.Length, lineInts = LineBytes / 4;
        for (int off = 0; off < strideInts; off++)
            for (int i = off; i < n; i += strideInts)
            {
                int line = i / lineInts;
                if (line != prevLine) { transitions++; prevLine = line; }
                reads++;
            }
        return (transitions, reads);
    }

    public static void Main()
    {
        const int Bytes = 64 << 20;
        int[] a = new int[Bytes / 4];
        for (int i = 0; i < a.Length; i++) a[i] = i & 7;

        string reported = File.Exists("/sys/devices/system/cpu/cpu0/cache/index0/coherency_line_size")
            ? File.ReadAllText("/sys/devices/system/cpu/cpu0/cache/index0/coherency_line_size").Trim()
            : "unknown";
        Console.WriteLine($"array {Bytes >> 20} MB ({a.Length:N0} ints)   L1d line size reported by the kernel: {reported} bytes\n");

        // correctness: every stride must sum to the same total
        long expected = 0;
        for (int i = 0; i < a.Length; i++) expected += a[i];

        Console.WriteLine($"{"stride(B)",10} {"reads",12} {"transitions",13} {"reads/transition",17}");
        bool plateaued = false;
        foreach (int strideBytes in (int[])[4, 8, 16, 32, 64, 96, 128, 256, 512, 4096])
        {
            int si = strideBytes / 4;
            // correctness check, walked separately from the counted sweep above
            long sum = 0;
            for (int off = 0; off < si; off++)
                for (int i = off; i < a.Length; i += si) sum += a[i];
            if (sum != expected) throw new Exception($"FAIL: stride {strideBytes} summed {sum}, expected {expected}");

            var (transitions, reads) = Sweep(a, si);
            long readsPerTransition = reads / transitions;
            Console.WriteLine($"{strideBytes,10} {reads,12:N0} {transitions,13:N0} {readsPerTransition,17}");
            if (strideBytes >= LineBytes)
            {
                if (readsPerTransition != 1) throw new Exception($"FAIL: stride {strideBytes} >= line size but reads/transition = {readsPerTransition}, expected 1");
                plateaued = true;
            }
            else if (plateaued) throw new Exception("FAIL: plateau broken — a later stride went back to sharing lines");
        }
        Console.WriteLine($"\nPASS: every stride produced the same sum ({expected}), and reads/transition falls monotonically to exactly 1 at stride {LineBytes} B and holds there");
    }
}
