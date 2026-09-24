// Evidence for /systems/atomics-and-cas/false-sharing/ — run with:
//   dotnet run bench/atomics-and-cas/false-sharing.cs -c Release
//
//   1. the addresses, so "same cache line" is a fact and not a claim
//   2. how often two adjacent fields of a fresh object land on the same line,
//      across many rounds with a varying-size filler allocation in front —
//      i.e. how much of "does this class have the false-sharing bug" is
//      decided by the allocator rather than by the source code
using System.Runtime.InteropServices;

// two counters that a code review would never look at twice
sealed class Stats { public long Hits; public long Misses; }

// the same two counters, one cache line each
[StructLayout(LayoutKind.Explicit, Size = 128)]
struct PaddedStats
{
    [FieldOffset(0)] public long Hits;
    [FieldOffset(64)] public long Misses;
}

static class FalseSharing
{
    static readonly long[] cells = new long[4096];

    public static void Main()
    {
        // ── 1. the addresses ─────────────────────────────────────────────────
        Console.WriteLine("=== 1. where the counters actually are (line number = address / 64) ===");
        var h = GCHandle.Alloc(cells, GCHandleType.Pinned);
        long baseAddr = h.AddrOfPinnedObject().ToInt64();
        foreach (int idx in new[] { 0, 1, 2, 3, 8, 16, 24 })
            Console.WriteLine($"  cells[{idx,2}]  0x{baseAddr + idx * 8:X}   line {(baseAddr + idx * 8) / 64}   byte {(baseAddr + idx * 8) % 64,2} of that line");
        h.Free();

        var stats = new Stats();
        var hs = GCHandle.Alloc(stats, GCHandleType.Pinned);
        long sa = hs.AddrOfPinnedObject().ToInt64();
        // AddrOfPinnedObject on a plain object points at its first field, not at
        // the method-table pointer (verified: the MT pointer sits at ptr-8), so
        // Hits is at `sa` and Misses at `sa + 8`.
        Console.WriteLine($"  Stats.Hits    0x{sa:X}   line {sa / 64}   byte {sa % 64,2} of that line");
        Console.WriteLine($"  Stats.Misses  0x{sa + 8:X}   line {(sa + 8) / 64}   byte {(sa + 8) % 64,2} of that line");
        hs.Free();

        var padded = new PaddedStats();
        var hp = GCHandle.Alloc(padded, GCHandleType.Pinned);
        long pa = hp.AddrOfPinnedObject().ToInt64();
        Console.WriteLine($"  PaddedStats.Hits    0x{pa:X}   line {pa / 64}");
        Console.WriteLine($"  PaddedStats.Misses  0x{pa + 64:X}   line {(pa + 64) / 64}   (FieldOffset(64) forces this)");
        hp.Free();

        // ── 2. the object-address lottery ────────────────────────────────────
        // A fresh Stats per round, with a filler allocation of a varying size in
        // front of it, so the object lands at different offsets inside a line.
        // Hits is the object's first field and Misses the second; whether that
        // is one line or two is decided entirely by the address the allocator
        // happened to hand out.
        Console.WriteLine("\n=== 2. 32 rounds: does a fresh Stats straddle a cache line? ===");
        var rng = new Random(1);
        int sameLine = 0, twoLines = 0;
        for (int r = 0; r < 32; r++)
        {
            GC.KeepAlive(new byte[24 + 8 * rng.Next(0, 8)]);      // shift the next allocation
            var s = new Stats();
            var hr = GCHandle.Alloc(s, GCHandleType.Pinned);
            long addr = hr.AddrOfPinnedObject().ToInt64();
            long lineH = addr / 64, lineM = (addr + 8) / 64;      // Hits at +0, Misses at +8
            hr.Free();
            bool same = lineH == lineM;
            if (same) sameLine++; else twoLines++;
            Console.WriteLine($"  round {r,2}: Hits at byte {addr % 64,2} of its line, Misses at byte {(addr + 8) % 64,2}"
                            + $" -> {(same ? "SAME line" : "two lines")}");
        }
        Console.WriteLine($"\n  same line: {sameLine}/32   two lines: {twoLines}/32");
    }
}
