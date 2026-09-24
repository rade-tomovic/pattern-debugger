// Evidence for /systems/il-jit-codegen/ (the tiering section) — run with:
//   DOTNET_JitDisasm=SumLoop DOTNET_JitStdOutFile=/tmp/tiers.txt \
//     dotnet run bench/il-jit-codegen/tiered-codegen.cs -c Release
//
// One method, compiled twice by the same process: once the moment it is first
// called, and again after the runtime decides it is hot. The env vars ask the
// JIT to print every version of SumLoop it emits, in the order it emits them.
// 20 million calls is enough wall time for the call-counting delay to expire and
// the promotion to happen while the program is still running.
using System;
using System.Runtime.CompilerServices;

class P
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SumLoop(int[] a)
    {
        int s = 0;
        for (int i = 0; i < a.Length; i++) s += a[i];
        return s;
    }

    static void Main()
    {
        int[] a = new int[64];
        for (int i = 0; i < a.Length; i++) a[i] = i;
        int t = 0;
        for (int k = 0; k < 20_000_000; k++) t += SumLoop(a);
        Console.WriteLine(t);
    }
}
