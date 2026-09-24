// The five-line loop as a named static method, so RyuJIT's disassembly dump can find it.
// Two things this file exists to get right:
//   * SumTo is a static method on class P, so the JIT names it P:SumTo(int):int and the
//     DOTNET_JitDisasm filter below matches. A local function inside top-level statements is
//     named Program:<<Main>$>g__SumTo|0_0(int):int and needs DOTNET_JitDisasm='*SumTo*'.
//   * Main calls it. An uncompiled method is never printed.
//
//   DOTNET_JitDisasm=SumTo DOTNET_TieredCompilation=0 dotnet run -c Release \
//     bench/cpu-execution/read-the-disassembly-jit.cs
//
// This file is not timed. It exists to be dumped with DOTNET_JitDisasm — see the exercise page.
using System.Runtime.CompilerServices;

static class P
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int SumTo(int n)
    {
        int total = 0;
        for (int i = 1; i <= n; i++)
            total += i;
        return total;
    }

    static void Main() => Console.WriteLine(SumTo(1000));
}
