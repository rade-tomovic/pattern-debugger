// Evidence for /systems/atomics-and-cas/ — the same increment at three levels.
//
//   IL:  dotnet run bench/atomics-and-cas/increment-shapes.cs -c Release
//   asm: DOTNET_TieredCompilation=0 DOTNET_JitDisasmDiffable=1 \
//        DOTNET_JitDisasm='PlainStatic PlainUse Atomic Cas CasLoop LoopPlain LoopAtomic LoopCell' \
//        dotnet run bench/atomics-and-cas/increment-shapes.cs -c Release
//
// The IL decoder is the small one from bench/il-jit-codegen/il-dump.cs, cut down
// to the opcodes these methods use: it reads the bytes Roslyn wrote into this
// assembly and names them with the runtime's own opcode table. Nothing is typed
// by hand. JitDisasmDiffable replaces address constants with 0xD1FFAB1E so the
// output is stable between runs.
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

public static class Shapes
{
    public static int Count;

    [MethodImpl(MethodImplOptions.NoInlining)] public static void PlainStatic() => Count++;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int PlainUse() { Count++; return Count; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Atomic() => Interlocked.Increment(ref Count);
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Cas(int expected, int desired) => Interlocked.CompareExchange(ref Count, desired, expected);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CasLoop(int add)
    {
        int old, want;
        do
        {
            old = Volatile.Read(ref Count);      // read the current value
            want = old + add;                    // compute the new one from it
        }
        while (Interlocked.CompareExchange(ref Count, want, old) != old);  // publish only if nobody moved it
        return want;
    }

    // the shape the false-sharing benchmark runs: a plain increment of one
    // array element, to confirm the JIT keeps it in memory rather than hoisting
    // it into a register for the whole loop
    public static readonly long[] Cells = new long[4096];
    [MethodImpl(MethodImplOptions.NoInlining)] public static void LoopCell(int n, int slot) { for (int i = 0; i < n; i++) Cells[slot]++; }

    [MethodImpl(MethodImplOptions.NoInlining)] public static void LoopPlain(int n) { for (int i = 0; i < n; i++) Count++; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void LoopAtomic(int n) { for (int i = 0; i < n; i++) Interlocked.Increment(ref Count); }

    // ── a minimal IL disassembler ────────────────────────────────────────────
    static readonly OpCode[] single = new OpCode[0x100];
    static readonly OpCode[] doubl = new OpCode[0x100];

    static string Short(Type t) => t.Name;

    static void Dump(MethodInfo m)
    {
        byte[] il = m.GetMethodBody()!.GetILAsByteArray()!;
        var mod = m.Module;
        Console.WriteLine($".method {m.Name}   // {il.Length} bytes of IL");
        int p = 0;
        while (p < il.Length)
        {
            int at = p;
            OpCode op;
            if (il[p] == 0xFE) { op = doubl[il[p + 1]]; p += 2; }
            else { op = single[il[p]]; p += 1; }

            string arg = "";
            switch (op.OperandType)
            {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineI: arg = ((sbyte)il[p]).ToString(); p += 1; break;
                case OperandType.ShortInlineVar: arg = il[p].ToString(); p += 1; break;
                case OperandType.InlineI: arg = BitConverter.ToInt32(il, p).ToString(); p += 4; break;
                case OperandType.ShortInlineBrTarget: arg = "IL_" + (p + 1 + (sbyte)il[p]).ToString("x4"); p += 1; break;
                case OperandType.InlineBrTarget: arg = "IL_" + (p + 4 + BitConverter.ToInt32(il, p)).ToString("x4"); p += 4; break;
                case OperandType.InlineField:
                    { var f = mod.ResolveField(BitConverter.ToInt32(il, p)); arg = Short(f!.DeclaringType!) + "::" + f.Name; p += 4; break; }
                case OperandType.InlineMethod:
                    { var mb = mod.ResolveMethod(BitConverter.ToInt32(il, p)); arg = Short(mb!.DeclaringType!) + "::" + mb.Name; p += 4; break; }
                default: Console.WriteLine($"  (unhandled operand {op.OperandType})"); return;
            }
            Console.WriteLine($"  IL_{at:x4}:  {op.Name,-16} {arg}");
        }
        Console.WriteLine();
    }

    public static void Main()
    {
        foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var op = (OpCode)f.GetValue(null)!;
            if (op.Size == 1) single[op.Value & 0xFF] = op;
            else doubl[op.Value & 0xFF] = op;
        }

        // make sure everything is jitted, so the asm dump has something to print
        PlainStatic(); PlainUse(); Atomic(); Cas(0, 0); CasLoop(1); LoopPlain(2); LoopAtomic(2); LoopCell(2, 0);

        var flags = BindingFlags.Public | BindingFlags.Static;
        foreach (var name in new[] { "PlainStatic", "Atomic", "CasLoop" })
            Dump(typeof(Shapes).GetMethod(name, flags)!);

        Console.WriteLine($"Count = {Count}");
    }
}
