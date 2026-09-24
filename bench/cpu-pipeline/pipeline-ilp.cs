// Companion to /systems/cpu-pipeline/ -- run with:
//   dotnet run bench/cpu-pipeline/pipeline-ilp.cs
//
// Two loops doing the same arithmetic-heavy step (one 64-bit multiply, one 64-bit add), the
// only difference being how many independent copies of that step run side by side. The page
// reads the JIT's own disassembly of these two methods to show that Chain4's four steps live in
// four different registers with no dependency between them, while Chain1 is one register a
// single chain long -- a structural fact about the code, not a timing.
const long M = 25214903917L, C = 11L;

static long Chain1(long n, long s)
{
    long a = s;
    for (long i = 0; i < n; i++) a = a * M + C;      // step i+1 needs step i's `a`
    return a;
}

static long Chain4(long n, long s)
{
    long a = s, b = s + 1, c = s + 2, d = s + 3;    // four chains that never read each other
    for (long i = 0; i < n; i++) { a = a * M + C; b = b * M + C; c = c * M + C; d = d * M + C; }
    return a + b + c + d;
}

// Sanity check: Chain1(0, s) and Chain4(0, s) both skip the loop entirely, so they should
// reduce to plain arithmetic on the seed with no steps applied.
long a1 = Chain1(0, 7);
long a4 = Chain4(0, 7);
if (a1 != 7) throw new Exception($"FAIL: Chain1(0,7) should be the untouched seed, got {a1}");
if (a4 != 7 + 8 + 9 + 10) throw new Exception($"FAIL: Chain4(0,7) should be the four untouched seeds summed, got {a4}");
Console.WriteLine($"Chain1(0,7) = {a1}");
Console.WriteLine($"Chain4(0,7) = {a4}");

// Run each enough times that the JIT promotes them to optimised code before you disassemble --
// e.g. DOTNET_JitDisasm="*Chain1*" dotnet run bench/cpu-pipeline/pipeline-ilp.cs -c Release
long sink = 0;
for (int i = 0; i < 200; i++) { sink += Chain1(50_000, 1); sink += Chain4(50_000, 1); }
Console.WriteLine($"checksum {sink}");
Console.WriteLine("PASS");
