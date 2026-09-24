// Evidence for /systems/il-jit-codegen/bounds-check-elimination/ — run with:
//   DOTNET_JitDisasm="ByLength ByCount ByCachedLen ByForeach ByLengthMinus1 TwoArrays \
//     TwoArraysGuarded ByCountSpan Reverse Gather GatherRaw" \
//     DOTNET_JitStdOutFile=/tmp/bce.txt \
//     dotnet run bench/il-jit-codegen/bounds-check-shapes.cs -c Release
//
// Eleven ways to read an int[] from front to back. Every one of them is compiled
// to tier 1 in this run (400,000 calls each is far past the promotion threshold),
// and the disassembly says which ones still carry a per-element bounds check.
// This file exists to be disassembled; the timings live in the sibling file
// bounds-check-elimination.cs.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class B
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ByLength(int[] a) { int s = 0; for (int i = 0; i < a.Length; i++) s += a[i]; return s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ByCount(int[] a, int n) { int s = 0; for (int i = 0; i < n; i++) s += a[i]; return s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ByCachedLen(int[] a) { int len = a.Length; int s = 0; for (int i = 0; i < len; i++) s += a[i]; return s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ByForeach(int[] a) { int s = 0; foreach (int v in a) s += v; return s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ByLengthMinus1(int[] a) { int s = 0; for (int i = 0; i <= a.Length - 1; i++) s += a[i]; return s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TwoArrays(int[] a, int[] b) { int s = 0; for (int i = 0; i < a.Length; i++) s += a[i] * b[i]; return s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TwoArraysGuarded(int[] a, int[] b)
    {
        if (b.Length < a.Length) throw new ArgumentException();
        int s = 0;
        for (int i = 0; i < a.Length; i++) s += a[i] * b[i];
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ByCountSpan(int[] a, int n)
    {
        var span = a.AsSpan(0, n);
        int s = 0;
        for (int i = 0; i < span.Length; i++) s += span[i];
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Reverse(int[] a) { int s = 0; for (int i = a.Length - 1; i >= 0; i--) s += a[i]; return s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Gather(int[] a, int[] idx) { int s = 0; for (int i = 0; i < idx.Length; i++) s += a[idx[i]]; return s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int GatherRaw(int[] a, int[] idx)
    {
        ref int r = ref MemoryMarshal.GetArrayDataReference(a);
        int s = 0;
        for (int i = 0; i < idx.Length; i++) s += Unsafe.Add(ref r, idx[i]);
        return s;
    }

    static void Main()
    {
        int[] a = new int[512], b = new int[512], idx = new int[512];
        for (int i = 0; i < a.Length; i++) { a[i] = i; b[i] = 1; idx[i] = i; }
        long t = 0;
        for (int k = 0; k < 400_000; k++)
        {
            t += ByLength(a); t += ByCount(a, a.Length); t += ByCachedLen(a); t += ByForeach(a);
            t += ByLengthMinus1(a); t += TwoArrays(a, b); t += TwoArraysGuarded(a, b);
            t += ByCountSpan(a, a.Length); t += Reverse(a);
            t += Gather(a, idx); t += GatherRaw(a, idx);
        }
        Console.WriteLine($"checksum {t} (ignore)");
    }
}
