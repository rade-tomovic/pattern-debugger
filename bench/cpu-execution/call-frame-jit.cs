// The same add3/caller shape as the gcc example on the topic page, written in C# so
// RyuJIT can be asked to print what it generated. Not timed — this file exists to be
// dumped with DOTNET_JitDisasm.
//   DOTNET_JitDisasm="Caller Add3" DOTNET_TieredCompilation=0 dotnet run -c Release \
//     bench/cpu-execution/call-frame-jit.cs
using System.Runtime.CompilerServices;

static class P
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Add3(int a, int b, int c) => a + b + c;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Caller(int x) => Add3(x, x + 1, x + 2) * 2;

    static void Main() => Console.WriteLine(Caller(3));
}
