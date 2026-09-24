// Evidence for /systems/il-jit-codegen/ (the OSR section) — run with:
//   DOTNET_JitDisasm=LongLoop DOTNET_JitStdOutFile=/tmp/osr.txt \
//     dotnet run bench/il-jit-codegen/osr-demo.cs -c Release
//
// One call, one loop, fifty million trips around the inner array sum. LongLoop
// is called exactly once, so call-counting can never promote it — the only way
// this method gets fast is on-stack replacement: the tier-0 loop counts its own
// iterations and, once the patchpoint fires, the JIT compiles a second version
// starting at the loop and jumps into it mid-flight, without ever returning.
using System;
using System.Runtime.CompilerServices;

class P
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long LongLoop(int[] a)
    {
        long s = 0;
        for (int k = 0; k < 50_000_000; k++)
            for (int i = 0; i < a.Length; i++)
                s += a[i];
        return s;
    }

    static void Main()
    {
        int[] a = new int[8];
        for (int i = 0; i < a.Length; i++) a[i] = i;
        Console.WriteLine(LongLoop(a));
    }
}
