// Evidence for /systems/memory-hierarchy/loop-order/ — run with:
//   dotnet run bench/memory-hierarchy/loop-order.cs
//
// C = A x B, three loop orders. Every version executes exactly n^3 multiply-adds
// and reads exactly the same values; only the ORDER in which the addresses are
// visited differs. Nothing here is timed: the file checks that all three orders
// compute the same matrix, then works out — from this box's real cache geometry,
// read from /sys — how many of the lines a column walk touches can actually live
// in L1 and in L2 at once, for eight sizes.
using System.Runtime.CompilerServices;

static class LoopOrder
{
    // i,j,k — the textbook order: one dot product at a time.
    // b[k * n + j] walks DOWN a column: consecutive reads are n floats apart.
    static void Ijk(float[] a, float[] b, float[] c, int n)
    {
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                float s = 0;
                for (int k = 0; k < n; k++) s += a[i * n + k] * b[k * n + j];
                c[i * n + j] = s;
            }
    }

    // i,k,j — same arithmetic, accumulated into C instead of a register.
    // Both b[k * n + j] and c[i * n + j] now walk ALONG a row.
    static void Ikj(float[] a, float[] b, float[] c, int n)
    {
        Array.Clear(c);
        for (int i = 0; i < n; i++)
            for (int k = 0; k < n; k++)
            {
                float aik = a[i * n + k];
                for (int j = 0; j < n; j++) c[i * n + j] += aik * b[k * n + j];
            }
    }

    // j,k,i — the worst case: a[i * n + k] and c[i * n + j] BOTH walk down columns.
    static void Jki(float[] a, float[] b, float[] c, int n)
    {
        Array.Clear(c);
        for (int j = 0; j < n; j++)
            for (int k = 0; k < n; k++)
            {
                float bkj = b[k * n + j];
                for (int i = 0; i < n; i++) c[i * n + j] += a[i * n + k] * bkj;
            }
    }

    static bool ReadCache(int level, string type, out int lineBytes, out int ways, out int sets)
    {
        for (int idx = 0; idx < 8; idx++)
        {
            string dir = $"/sys/devices/system/cpu/cpu0/cache/index{idx}";
            if (!Directory.Exists(dir)) break;
            int lvl = int.Parse(File.ReadAllText($"{dir}/level").Trim());
            string t = File.ReadAllText($"{dir}/type").Trim();
            if (lvl == level && t == type)
            {
                lineBytes = int.Parse(File.ReadAllText($"{dir}/coherency_line_size").Trim());
                ways = int.Parse(File.ReadAllText($"{dir}/ways_of_associativity").Trim());
                sets = int.Parse(File.ReadAllText($"{dir}/number_of_sets").Trim());
                return true;
            }
        }
        lineBytes = ways = sets = 0;
        return false;
    }

    static long Gcd(long x, long y) { while (y != 0) { (x, y) = (y, x % y); } return x; }

    public static void Main()
    {
        // --- correctness: all three orders must compute the same C ---
        // (float addition is not associative, so compare with a tolerance)
        const int n = 256;
        var rng = new Random(1);
        float[] a = new float[n * n], b = new float[n * n], c = new float[n * n];
        for (int i = 0; i < a.Length; i++) { a[i] = (float)rng.NextDouble(); b[i] = (float)rng.NextDouble(); }

        Ijk(a, b, c, n);
        float[] refC = (float[])c.Clone();
        foreach (var f in new Action<float[], float[], float[], int>[] { Ikj, Jki })
        {
            f(a, b, c, n);
            for (int i = 0; i < c.Length; i++)
                if (Math.Abs(c[i] - refC[i]) > 1e-2f * Math.Max(1f, Math.Abs(refC[i])))
                    throw new Exception($"FAIL: orders disagree at {i}: {c[i]} vs {refC[i]}");
        }
        Console.WriteLine("PASS: i,j,k / i,k,j / j,k,i all compute the same C\n");

        // --- geometry: read this box's real L1d and L2 from /sys, no assumptions ---
        if (!ReadCache(1, "Data", out int l1Line, out int l1Ways, out int l1Sets) ||
            !ReadCache(2, "Unified", out int l2Line, out int l2Ways, out int l2Sets))
        {
            Console.WriteLine("cache geometry not available on this box — skipping the set-conflict table");
            return;
        }
        int pageBytes = Environment.SystemPageSize;
        Console.WriteLine($"L1d: {l1Sets} sets x {l1Ways} ways x {l1Line} B = {l1Sets * l1Ways * l1Line / 1024} KB (one way = {l1Sets * l1Line} B)");
        Console.WriteLine($"L2:  {l2Sets} sets x {l2Ways} ways x {l2Line} B = {l2Sets * l2Ways * l2Line / 1024} KB (one way = {l2Sets * l2Line} B)");
        Console.WriteLine($"page size: {pageBytes} B\n");

        // A cache line lives in exactly one SET, chosen by a slice of bits in the
        // middle of its address. The number of distinct sets a strided walk can
        // reach, times the ways per set, bounds how many lines of that walk the
        // cache can hold at once. Both caches are physically indexed and a C#
        // program cannot see physical addresses — but bits below the page size ARE
        // knowable, because virtual and physical addresses share them.
        long PageOffsetSets(long strideBytes, int sets) => Math.Min(pageBytes / Gcd(strideBytes, pageBytes), sets);
        long L1LinesHeld(long strideBytes) => PageOffsetSets(strideBytes, l1Sets) * l1Ways;
        // For L2, only the bits inside the page offset are knowable from the stride;
        // the rest of the set index comes from the physical frame number the kernel
        // handed out, which a managed program cannot read. That unknown part can
        // spread a walk over some further sets, bounded by how many page-offset-only
        // sets one page already covers — so this is an upper bound, not a measurement.
        int knownSetsPerPage = Math.Min(pageBytes / l2Line, l2Sets);
        int unknownSpread = l2Sets / knownSetsPerPage;
        long L2LinesHeldAtMost(long strideBytes) => PageOffsetSets(strideBytes, knownSetsPerPage) * unknownSpread * l2Ways;

        Console.WriteLine($"{"n",6} {"column step (B)",16} {"lines a column needs",22} {"L1 can hold",12} {"L2 can hold at most",20}");
        foreach (int sz in (int[])[512, 640, 704, 768, 896, 1024, 1088, 1152])
        {
            long stride = 4L * sz;
            long needed = sz;
            long l1 = L1LinesHeld(stride);
            long l2 = L2LinesHeldAtMost(stride);
            string flag = l2 < needed ? "  <-- L2 cannot hold the reuse set" : "";
            Console.WriteLine($"{sz,6} {stride,16} {needed,22} {l1,12} {l2,20}{flag}");
        }
    }
}
