// Evidence for /systems/il-jit-codegen/ (the "why a naive comparison lies" section) — run with:
//   DOTNET_JitDisasm=CallUnusedResult DOTNET_JitStdOutFile=/tmp/dce.txt \
//     dotnet run bench/il-jit-codegen/dead-code-elimination.cs -c Release
//
// CallUnusedResult computes Small(n) and never uses the result. Small is tiny
// enough to inline, so once it is sitting inside CallUnusedResult's body the
// optimiser can see that nothing observable happened and deletes the whole
// computation. If you time a loop that calls a method purely for its return
// value and never consume that value, you may be timing this: nothing.
using System;
using System.Runtime.CompilerServices;

class P
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CallUnusedResult(int n)
    {
        int x = Small(n); // result never read, never returned, never stored
    }

    static int Small(int n) => n * n + 3;

    static void Main()
    {
        for (long k = 0; k < 300_000_000; k++) CallUnusedResult((int)(k % 7));
        Console.WriteLine("done");
    }
}
