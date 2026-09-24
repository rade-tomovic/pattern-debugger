// Evidence for /systems/il-jit-codegen/ (the inlining section) — run with:
//   DOTNET_JitDisasm="Call*" DOTNET_JitStdOutFile=/tmp/inl.txt \
//     dotnet run bench/il-jit-codegen/inlining.cs -c Release
//
// Eight callees, each with exactly one call site, and a Call* wrapper per callee
// marked NoInlining so the wrapper itself is always compiled as its own method
// and its disassembly shows whether the callee's `call` survived.
// The program prints each callee's IL size (read out of its own metadata), so the
// size column in the page's table is measured, not estimated.
// Four of the eight carry an exception-handling region — try/catch, try/finally,
// using, lock — because the folklore says all four block inlining and only one of
// them does. Every EH region here has an observable side effect (a write to `g`),
// so none of them can be deleted as empty before the inliner sees it.
//
// Re-run with DOTNET_JitInlineMethodsWithEH=0 prepended to the same command and all
// four EH callees are refused instead — that is the switch that governs the split.
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

class Inl
{
    // tiny and pure
    static int Small(int x) => x * 2 + 1;

    // argument validation: a throw, but on a branch of its own
    static int Guarded(int x)
    {
        if (x < 0) throw new ArgumentOutOfRangeException(nameof(x));
        return x * 2 + 1;
    }

    // smaller than Guarded, but it carries an exception handler
    static int WithTry(int x)
    {
        try { return x * 2 + 1; }
        catch (Exception) { return -1; }
    }

    // the side effects the four EH callees below perform, so no `finally` and no
    // `Dispose` can be optimised away as empty before the inliner gets a look.
    static readonly int[] g = new int[4];
    static readonly object gate = new object();

    sealed class Bump : IDisposable { public void Dispose() => g[1]++; }

    // same arithmetic, a try/finally whose finally does real work
    static int TryFinallyWork(int x)
    {
        try { return x * 2 + 1; }
        finally { g[0]++; }
    }

    // same arithmetic behind a `using`, which the C# compiler turns into try/finally
    static int UsingReal(int x)
    {
        using var b = new Bump();
        return x * 2 + 1;
    }

    // same arithmetic behind a `lock`, which becomes Monitor.Enter + try/finally + Monitor.Exit
    static int LockMethod(int x)
    {
        lock (gate) { g[2]++; return x * 2 + 1; }
    }

    // two big straight-line callees, to find where the size budget gives up
    static int Big60(int x)
    {
        int a = x;
        a = a * 3 + (x ^ 0) - (a >> 1);
        a = a * 4 + (x ^ 1) - (a >> 1);
        a = a * 5 + (x ^ 2) - (a >> 1);
        a = a * 6 + (x ^ 3) - (a >> 1);
        a = a * 7 + (x ^ 4) - (a >> 1);
        a = a * 8 + (x ^ 5) - (a >> 1);
        a = a * 9 + (x ^ 6) - (a >> 1);
        a = a * 10 + (x ^ 7) - (a >> 1);
        a = a * 11 + (x ^ 8) - (a >> 1);
        a = a * 12 + (x ^ 9) - (a >> 1);
        a = a * 13 + (x ^ 10) - (a >> 1);
        a = a * 14 + (x ^ 11) - (a >> 1);
        a = a * 15 + (x ^ 12) - (a >> 1);
        a = a * 16 + (x ^ 13) - (a >> 1);
        a = a * 17 + (x ^ 14) - (a >> 1);
        a = a * 18 + (x ^ 15) - (a >> 1);
        a = a * 19 + (x ^ 16) - (a >> 1);
        a = a * 20 + (x ^ 17) - (a >> 1);
        a = a * 21 + (x ^ 18) - (a >> 1);
        a = a * 22 + (x ^ 19) - (a >> 1);
        a = a * 23 + (x ^ 20) - (a >> 1);
        a = a * 24 + (x ^ 21) - (a >> 1);
        a = a * 25 + (x ^ 22) - (a >> 1);
        a = a * 26 + (x ^ 23) - (a >> 1);
        a = a * 27 + (x ^ 24) - (a >> 1);
        a = a * 28 + (x ^ 25) - (a >> 1);
        a = a * 29 + (x ^ 26) - (a >> 1);
        a = a * 30 + (x ^ 27) - (a >> 1);
        a = a * 31 + (x ^ 28) - (a >> 1);
        a = a * 32 + (x ^ 29) - (a >> 1);
        a = a * 33 + (x ^ 30) - (a >> 1);
        a = a * 34 + (x ^ 31) - (a >> 1);
        a = a * 35 + (x ^ 32) - (a >> 1);
        a = a * 36 + (x ^ 33) - (a >> 1);
        a = a * 37 + (x ^ 34) - (a >> 1);
        a = a * 38 + (x ^ 35) - (a >> 1);
        a = a * 39 + (x ^ 36) - (a >> 1);
        a = a * 40 + (x ^ 37) - (a >> 1);
        a = a * 41 + (x ^ 38) - (a >> 1);
        a = a * 42 + (x ^ 39) - (a >> 1);
        a = a * 43 + (x ^ 40) - (a >> 1);
        a = a * 44 + (x ^ 41) - (a >> 1);
        a = a * 45 + (x ^ 42) - (a >> 1);
        a = a * 46 + (x ^ 43) - (a >> 1);
        a = a * 47 + (x ^ 44) - (a >> 1);
        a = a * 48 + (x ^ 45) - (a >> 1);
        a = a * 49 + (x ^ 46) - (a >> 1);
        a = a * 50 + (x ^ 47) - (a >> 1);
        a = a * 51 + (x ^ 48) - (a >> 1);
        a = a * 52 + (x ^ 49) - (a >> 1);
        a = a * 53 + (x ^ 50) - (a >> 1);
        a = a * 54 + (x ^ 51) - (a >> 1);
        a = a * 55 + (x ^ 52) - (a >> 1);
        a = a * 56 + (x ^ 53) - (a >> 1);
        a = a * 57 + (x ^ 54) - (a >> 1);
        a = a * 58 + (x ^ 55) - (a >> 1);
        a = a * 59 + (x ^ 56) - (a >> 1);
        a = a * 60 + (x ^ 57) - (a >> 1);
        a = a * 61 + (x ^ 58) - (a >> 1);
        a = a * 62 + (x ^ 59) - (a >> 1);
        return a;
    }

    static int Big90(int x)
    {
        int a = x;
        a = a * 3 + (x ^ 0) - (a >> 1);
        a = a * 4 + (x ^ 1) - (a >> 1);
        a = a * 5 + (x ^ 2) - (a >> 1);
        a = a * 6 + (x ^ 3) - (a >> 1);
        a = a * 7 + (x ^ 4) - (a >> 1);
        a = a * 8 + (x ^ 5) - (a >> 1);
        a = a * 9 + (x ^ 6) - (a >> 1);
        a = a * 10 + (x ^ 7) - (a >> 1);
        a = a * 11 + (x ^ 8) - (a >> 1);
        a = a * 12 + (x ^ 9) - (a >> 1);
        a = a * 13 + (x ^ 10) - (a >> 1);
        a = a * 14 + (x ^ 11) - (a >> 1);
        a = a * 15 + (x ^ 12) - (a >> 1);
        a = a * 16 + (x ^ 13) - (a >> 1);
        a = a * 17 + (x ^ 14) - (a >> 1);
        a = a * 18 + (x ^ 15) - (a >> 1);
        a = a * 19 + (x ^ 16) - (a >> 1);
        a = a * 20 + (x ^ 17) - (a >> 1);
        a = a * 21 + (x ^ 18) - (a >> 1);
        a = a * 22 + (x ^ 19) - (a >> 1);
        a = a * 23 + (x ^ 20) - (a >> 1);
        a = a * 24 + (x ^ 21) - (a >> 1);
        a = a * 25 + (x ^ 22) - (a >> 1);
        a = a * 26 + (x ^ 23) - (a >> 1);
        a = a * 27 + (x ^ 24) - (a >> 1);
        a = a * 28 + (x ^ 25) - (a >> 1);
        a = a * 29 + (x ^ 26) - (a >> 1);
        a = a * 30 + (x ^ 27) - (a >> 1);
        a = a * 31 + (x ^ 28) - (a >> 1);
        a = a * 32 + (x ^ 29) - (a >> 1);
        a = a * 33 + (x ^ 30) - (a >> 1);
        a = a * 34 + (x ^ 31) - (a >> 1);
        a = a * 35 + (x ^ 32) - (a >> 1);
        a = a * 36 + (x ^ 33) - (a >> 1);
        a = a * 37 + (x ^ 34) - (a >> 1);
        a = a * 38 + (x ^ 35) - (a >> 1);
        a = a * 39 + (x ^ 36) - (a >> 1);
        a = a * 40 + (x ^ 37) - (a >> 1);
        a = a * 41 + (x ^ 38) - (a >> 1);
        a = a * 42 + (x ^ 39) - (a >> 1);
        a = a * 43 + (x ^ 40) - (a >> 1);
        a = a * 44 + (x ^ 41) - (a >> 1);
        a = a * 45 + (x ^ 42) - (a >> 1);
        a = a * 46 + (x ^ 43) - (a >> 1);
        a = a * 47 + (x ^ 44) - (a >> 1);
        a = a * 48 + (x ^ 45) - (a >> 1);
        a = a * 49 + (x ^ 46) - (a >> 1);
        a = a * 50 + (x ^ 47) - (a >> 1);
        a = a * 51 + (x ^ 48) - (a >> 1);
        a = a * 52 + (x ^ 49) - (a >> 1);
        a = a * 53 + (x ^ 50) - (a >> 1);
        a = a * 54 + (x ^ 51) - (a >> 1);
        a = a * 55 + (x ^ 52) - (a >> 1);
        a = a * 56 + (x ^ 53) - (a >> 1);
        a = a * 57 + (x ^ 54) - (a >> 1);
        a = a * 58 + (x ^ 55) - (a >> 1);
        a = a * 59 + (x ^ 56) - (a >> 1);
        a = a * 60 + (x ^ 57) - (a >> 1);
        a = a * 61 + (x ^ 58) - (a >> 1);
        a = a * 62 + (x ^ 59) - (a >> 1);
        a = a * 63 + (x ^ 60) - (a >> 1);
        a = a * 64 + (x ^ 61) - (a >> 1);
        a = a * 65 + (x ^ 62) - (a >> 1);
        a = a * 66 + (x ^ 63) - (a >> 1);
        a = a * 67 + (x ^ 64) - (a >> 1);
        a = a * 68 + (x ^ 65) - (a >> 1);
        a = a * 69 + (x ^ 66) - (a >> 1);
        a = a * 70 + (x ^ 67) - (a >> 1);
        a = a * 71 + (x ^ 68) - (a >> 1);
        a = a * 72 + (x ^ 69) - (a >> 1);
        a = a * 73 + (x ^ 70) - (a >> 1);
        a = a * 74 + (x ^ 71) - (a >> 1);
        a = a * 75 + (x ^ 72) - (a >> 1);
        a = a * 76 + (x ^ 73) - (a >> 1);
        a = a * 77 + (x ^ 74) - (a >> 1);
        a = a * 78 + (x ^ 75) - (a >> 1);
        a = a * 79 + (x ^ 76) - (a >> 1);
        a = a * 80 + (x ^ 77) - (a >> 1);
        a = a * 81 + (x ^ 78) - (a >> 1);
        a = a * 82 + (x ^ 79) - (a >> 1);
        a = a * 83 + (x ^ 80) - (a >> 1);
        a = a * 84 + (x ^ 81) - (a >> 1);
        a = a * 85 + (x ^ 82) - (a >> 1);
        a = a * 86 + (x ^ 83) - (a >> 1);
        a = a * 87 + (x ^ 84) - (a >> 1);
        a = a * 88 + (x ^ 85) - (a >> 1);
        a = a * 89 + (x ^ 86) - (a >> 1);
        a = a * 90 + (x ^ 87) - (a >> 1);
        a = a * 91 + (x ^ 88) - (a >> 1);
        a = a * 92 + (x ^ 89) - (a >> 1);
        return a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)] public static int CallSmall(int x) => Small(x) + Small(x + 1);
    [MethodImpl(MethodImplOptions.NoInlining)] public static int CallGuarded(int x) => Guarded(x) + 1;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int CallWithTry(int x) => WithTry(x) + 1;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int CallTryFinallyWork(int x) => TryFinallyWork(x) + 1;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int CallUsingReal(int x) => UsingReal(x) + 1;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int CallLockMethod(int x) => LockMethod(x) + 1;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int CallBig60(int x) => Big60(x);
    [MethodImpl(MethodImplOptions.NoInlining)] public static int CallBig90(int x) => Big90(x);

    static void Main()
    {
        foreach (var name in new[] { "Small", "Guarded", "WithTry", "TryFinallyWork", "UsingReal", "LockMethod", "Big60", "Big90" })
        {
            var m = typeof(Inl).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;
            Console.WriteLine($"{name,-15} {m.GetMethodBody()!.GetILAsByteArray()!.Length,5} bytes of IL");
        }

        // enough calls to push every Call* wrapper to tier 1
        long t = 0;
        for (int k = 0; k < 3_000_000; k++)
        {
            t += CallSmall(k); t += CallGuarded(k); t += CallWithTry(k);
            t += CallTryFinallyWork(k); t += CallUsingReal(k); t += CallLockMethod(k);
            t += CallBig60(k); t += CallBig90(k);
        }
        Console.WriteLine($"checksum {t} (ignore); side effects {g[0]}/{g[1]}/{g[2]} (ignore)");
    }
}
