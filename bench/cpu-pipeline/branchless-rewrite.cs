// Evidence for /systems/cpu-pipeline/branchless-rewrite/ -- run with:
//   dotnet run bench/cpu-pipeline/branchless-rewrite.cs
//
// Three ways to write "keep v if v >= 128, else 0", checked against the full byte
// domain and, for the AVX2 version, many random 32-byte blocks. No timing anywhere.
// Three ways to write "keep v if v >= 128, else 0" -- checked against every one of the 256
// possible byte values, not sampled. The vector version operates on 32 bytes at once, so it is
// checked separately, against many random 32-byte blocks.
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

// A. the branch -- the source of truth this exercise checks the other two against.
static long Branchy(byte v) => v >= 128 ? v : 0;

// B. the ternary -- same logic, written as an expression instead of a statement.
static long Ternary(byte v) => v >= 128 ? v : 0;

// C. the mask -- pure arithmetic, no comparison that can be "taken" or "not taken" at all.
static long Masked(byte v)
{
    int x = v;
    int keep = ~((x - 128) >> 31);   // 0 when x < 128, -1 (all ones) when x >= 128
    return x & keep;
}

int mismatches = 0;
for (int b = 0; b <= 255; b++)
{
    long expected = Branchy((byte)b);
    if (Ternary((byte)b) != expected) { mismatches++; Console.WriteLine($"ternary mismatch at {b}"); }
    if (Masked((byte)b) != expected)  { mismatches++; Console.WriteLine($"masked mismatch at {b}"); }
}
Console.WriteLine($"checked all 256 byte values: {mismatches} mismatches");
if (mismatches != 0) throw new Exception("FAIL: a branchless rewrite disagreed with the branch somewhere in the byte domain");

// D. 32 bytes at once: the comparison becomes a per-byte mask, the mask selects, and
// SumAbsoluteDifferences (the psadbw instruction) folds 32 masked bytes into four 64-bit lanes.
// x86-64 with AVX2, which is what this box has.
if (Avx2.IsSupported)
{
    var rng = new Random(2024);
    for (int trial = 0; trial < 1000; trial++)
    {
        var block = new byte[32];
        rng.NextBytes(block);
        var threshold = Vector256.Create((byte)128);
        var v = Vector256.LoadUnsafe(ref block[0]);
        var keep = Avx2.CompareEqual(Avx2.Max(v, threshold), v);   // v >= 128, per byte
        var kept = Avx2.And(v, keep);
        var lanes = Avx2.SumAbsoluteDifferences(kept, Vector256<byte>.Zero).AsUInt64();
        long simdSum = 0;
        for (int lane = 0; lane < 4; lane++) simdSum += (long)lanes[lane];

        long scalarSum = 0;
        for (int i = 0; i < 32; i++) scalarSum += Branchy(block[i]);
        if (simdSum != scalarSum)
            throw new Exception($"FAIL: simd/scalar mismatch on trial {trial}: {simdSum} vs {scalarSum}");
    }
    Console.WriteLine("1,000 random 32-byte blocks: simd sum matches scalar sum every time");
}
Console.WriteLine("PASS");
