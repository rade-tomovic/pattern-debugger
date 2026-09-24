// What a function call costs, shown in instructions rather than time. The same three
// additions reached five ways; every variant must agree on the answer, and the
// DOTNET_JitDisasm dump (see the exercise page) shows what changed in the machine code.
// CallMono and CallPoly are separate methods, each with its own call site, so each gets
// its own profile: CallMono only ever sees Adder, CallPoly alternates Adder/OtherAdder.
//   dotnet run -c Release bench/cpu-execution/call-and-inline-cost.cs
using System.Runtime.CompilerServices;

const int N = 4000;

// A. no call at all — the body written where it is used
static long OpenCoded()
{
    long total = 0;
    for (int i = 0; i < N; i++) total += i + 1 + 2;
    return total;
}

// B. a small static method the JIT is free to inline
static long Inlined()
{
    long total = 0;
    for (int i = 0; i < N; i++) total += Ops.Add(i, 1, 2);
    return total;
}

// C. identical method, inlining forbidden — the only difference is the call
static long NotInlined()
{
    long total = 0;
    for (int i = 0; i < N; i++) total += Ops.AddNoInline(i, 1, 2);
    return total;
}

// D. an interface call site that only ever sees one concrete type
static long CallMono(IAdder[] items)
{
    long total = 0;
    for (int i = 0; i < N; i++) total += items[i & 1].Add(i, 1, 2);
    return total;
}

// E. the same shape, but the call site sees two concrete types
static long CallPoly(IAdder[] items)
{
    long total = 0;
    for (int i = 0; i < N; i++) total += items[i & 1].Add(i, 1, 2);
    return total;
}

IAdder[] oneType = [new Adder(), new Adder()];
IAdder[] twoTypes = [new Adder(), new OtherAdder()];

// Call every variant enough times that tiered compilation promotes each one to
// fully-optimised code before the JitDisasm dump (see the page) is taken of it.
// CallMono only ever runs against oneType, CallPoly only ever against twoTypes,
// so their profiles stay clean and separate.
long sink = 0;
for (int w = 0; w < 3; w++)
{
    sink += OpenCoded() + Inlined() + NotInlined();
    sink += CallMono(oneType) + CallPoly(twoTypes);
}

long a = OpenCoded();
long b = Inlined();
long c = NotInlined();
long d = CallMono(oneType);
long e = CallPoly(twoTypes);

if (a != b || b != c || c != d || d != e)
    throw new Exception($"FAIL: variants disagree: open={a} inlined={b} notinlined={c} mono={d} poly={e}");

Console.WriteLine($"PASS all five variants agree: {a} (sink {sink}, ignore)");

interface IAdder { int Add(int a, int b, int c); }
sealed class Adder : IAdder { public int Add(int a, int b, int c) => a + b + c; }
sealed class OtherAdder : IAdder { public int Add(int a, int b, int c) => c + b + a; }

static class Ops
{
    public static int Add(int a, int b, int c) => a + b + c;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int AddNoInline(int a, int b, int c) => a + b + c;
}
