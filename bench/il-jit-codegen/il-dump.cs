// Evidence for /systems/il-jit-codegen/ — run with:
//   dotnet run bench/il-jit-codegen/il-dump.cs -c Release
//
// A minimal IL disassembler. It reads the method body bytes that Roslyn wrote
// into THIS assembly (MethodBody.GetILAsByteArray) and decodes them with the
// opcode table the runtime itself exposes (System.Reflection.Emit.OpCodes),
// resolving metadata tokens back to names through the defining Module.
// Nothing here is transcribed by hand: every line printed is bytes on disk.
using System.Reflection;
using System.Reflection.Emit;

// ── the decoder ──────────────────────────────────────────────────────────────
var single = new OpCode[0x100];
var doubl = new OpCode[0x100];
foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
{
    var op = (OpCode)f.GetValue(null)!;
    if (op.Size == 1) single[op.Value & 0xFF] = op;
    else doubl[op.Value & 0xFF] = op;
}

void Dump(MethodInfo m)
{
    var body = m.GetMethodBody()!;
    byte[] il = body.GetILAsByteArray()!;
    var mod = m.Module;
    var gm = m.DeclaringType!.GetGenericArguments();

    Console.WriteLine($".method {m.Name}   // {il.Length} bytes of IL, maxstack {body.MaxStackSize}, "
                      + $"{body.LocalVariables.Count} locals");
    foreach (var l in body.LocalVariables)
        Console.WriteLine($"  .locals  [{l.LocalIndex}] {Short(l.LocalType)}");

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
            case OperandType.InlineVar: arg = BitConverter.ToUInt16(il, p).ToString(); p += 2; break;
            case OperandType.InlineI: arg = BitConverter.ToInt32(il, p).ToString(); p += 4; break;
            case OperandType.InlineI8: arg = BitConverter.ToInt64(il, p).ToString(); p += 8; break;
            case OperandType.ShortInlineR: arg = BitConverter.ToSingle(il, p).ToString(); p += 4; break;
            case OperandType.InlineR: arg = BitConverter.ToDouble(il, p).ToString(); p += 8; break;
            case OperandType.ShortInlineBrTarget: arg = "IL_" + (p + 1 + (sbyte)il[p]).ToString("x4"); p += 1; break;
            case OperandType.InlineBrTarget: arg = "IL_" + (p + 4 + BitConverter.ToInt32(il, p)).ToString("x4"); p += 4; break;
            case OperandType.InlineString: arg = "\"" + mod.ResolveString(BitConverter.ToInt32(il, p)) + "\""; p += 4; break;
            case OperandType.InlineMethod:
                {
                    var mb = mod.ResolveMethod(BitConverter.ToInt32(il, p), gm, null);
                    arg = Short(mb!.DeclaringType!) + "::" + mb.Name; p += 4; break;
                }
            case OperandType.InlineField:
                {
                    var fb = mod.ResolveField(BitConverter.ToInt32(il, p), gm, null);
                    arg = Short(fb!.DeclaringType!) + "::" + fb.Name; p += 4; break;
                }
            case OperandType.InlineType:
            case OperandType.InlineTok:
                {
                    var t = mod.ResolveType(BitConverter.ToInt32(il, p), gm, null);
                    arg = Short(t); p += 4; break;
                }
            case OperandType.InlineSwitch:
                {
                    int n = BitConverter.ToInt32(il, p); p += 4;
                    var t = new string[n];
                    int baseAddr = p + 4 * n;
                    for (int k = 0; k < n; k++) { t[k] = "IL_" + (baseAddr + BitConverter.ToInt32(il, p)).ToString("x4"); p += 4; }
                    arg = "(" + string.Join(", ", t) + ")"; break;
                }
            default: throw new Exception("unhandled operand " + op.OperandType);
        }
        Console.WriteLine($"  IL_{at:x4}:  {op.Name,-13} {arg}");
    }
    Console.WriteLine();
}

static string Short(Type t) => t.FullName switch
{
    "System.Int32" => "int32",
    "System.Int64" => "int64",
    "System.String" => "string",
    "System.Object" => "object",
    "System.Boolean" => "bool",
    "System.Double" => "float64",
    _ => t.Name,
};

foreach (var name in new[] { "SumLoop", "Greet", "Box", "Add", "AddTwice", "ByLength", "ByCount" })
    Dump(typeof(Sample).GetMethod(name)!);

// ── the methods we want to look at ────────────────────────────────────────────
static class Sample
{
    public static int SumLoop(int[] a)
    {
        int total = 0;
        for (int i = 0; i < a.Length; i++)
            total += a[i];
        return total;
    }

    public static string Greet(string name) => "hello, " + name;

    public static object Box(int x) => x;

    public static int Add(int a, int b) => a + b;

    // the two loops from /systems/il-jit-codegen/bounds-check-elimination/
    public static int ByLength(int[] a)
    {
        int s = 0;
        for (int i = 0; i < a.Length; i++) s += a[i];
        return s;
    }

    public static int ByCount(int[] a, int n)
    {
        int s = 0;
        for (int i = 0; i < n; i++) s += a[i];
        return s;
    }

    public static int AddTwice(int a, int b) => Add(a, b) + Add(a, b);
}
