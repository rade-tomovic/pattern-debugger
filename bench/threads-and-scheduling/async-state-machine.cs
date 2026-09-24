// Evidence for /systems/threads-and-scheduling/async-state-machine/ — run with:
//   dotnet run bench/threads-and-scheduling/async-state-machine.cs -c Release
//   dotnet run bench/threads-and-scheduling/async-state-machine.cs
// (the second one is the same file in Debug, where the generated type changes shape)
//
// Nothing here is transcribed by hand. The type, its fields and both IL listings are
// read out of THIS assembly at run time: the [AsyncStateMachine] attribute names the
// generated type, MethodBody.GetILAsByteArray returns the bytes Roslyn wrote, and the
// decoder below walks them against the runtime's own opcode table (OpCodes).
#pragma warning disable IL2026, IL2070, IL2075
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

static class Machine
{
    // ── the method we are going to take apart ────────────────────────────────
    public static async Task<int> FetchAsync(int id)
    {
        int local = id * 2;
        int fetched = await StepAsync(local);
        return fetched + local;
    }
    static async Task<int> StepAsync(int x) { await Task.Delay(1); return x + 1; }

    // ── a minimal IL decoder ─────────────────────────────────────────────────
    static readonly OpCode[] One = new OpCode[0x100], Two = new OpCode[0x100];
    static void BuildTable()
    {
        foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var op = (OpCode)f.GetValue(null)!;
            if (op.Size == 1) One[op.Value & 0xFF] = op; else Two[op.Value & 0xFF] = op;
        }
    }
    static string Short(Type t) => t.FullName switch
    {
        "System.Int32" => "int32", "System.String" => "string", "System.Object" => "object",
        "System.Boolean" => "bool", "System.Void" => "void", _ => t.Name,
    };
    static void Dump(MethodBase m)
    {
        var body = m.GetMethodBody()!;
        byte[] il = body.GetILAsByteArray()!;
        var mod = m.Module;
        var gm = m.DeclaringType!.GetGenericArguments();
        Console.WriteLine($".method {m.DeclaringType.Name}::{m.Name}   // {il.Length} bytes of IL, {body.LocalVariables.Count} locals");
        foreach (var l in body.LocalVariables) Console.WriteLine($"  .locals [{l.LocalIndex}] {Short(l.LocalType)}");
        int p = 0;
        while (p < il.Length)
        {
            int at = p;
            OpCode op;
            if (il[p] == 0xFE) { op = Two[il[p + 1]]; p += 2; } else { op = One[il[p]]; p += 1; }
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
                    { var mb = mod.ResolveMethod(BitConverter.ToInt32(il, p), gm, null); arg = Short(mb!.DeclaringType!) + "::" + mb.Name; p += 4; break; }
                case OperandType.InlineField:
                    { var fb = mod.ResolveField(BitConverter.ToInt32(il, p), gm, null); arg = Short(fb!.DeclaringType!) + "::" + fb.Name; p += 4; break; }
                case OperandType.InlineType:
                case OperandType.InlineTok:
                    { var t = mod.ResolveType(BitConverter.ToInt32(il, p), gm, null); arg = Short(t); p += 4; break; }
                default: throw new Exception("unhandled operand " + op.OperandType);
            }
            Console.WriteLine($"  IL_{at:x4}:  {op.Name,-13} {arg}");
        }
        Console.WriteLine();
    }

    // ── what the shapes cost ─────────────────────────────────────────────────
    public static long Sink;
    static readonly Task<int> Already = Task.FromResult(41);

    [MethodImpl(MethodImplOptions.NoInlining)] static int Plain(int id) => id * 2 + 41;
    [MethodImpl(MethodImplOptions.NoInlining)] static async Task<int> NoSuspendTask(int id) { int local = id * 2; return await Already + local; }
    [MethodImpl(MethodImplOptions.NoInlining)] static async ValueTask<int> NoSuspendValueTask(int id) { int local = id * 2; return await Already + local; }
    [MethodImpl(MethodImplOptions.NoInlining)] static async Task<int> SuspendsTask(int id) { int local = id * 2; await Task.Yield(); return local + 1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static async ValueTask<int> SuspendsValueTask(int id) { int local = id * 2; await Task.Yield(); return local + 1; }
    [MethodImpl(MethodImplOptions.NoInlining)] static Task<int> ViaTaskRun(int id) => Task.Run(() => id * 2 + 41);

    static void A(int n) { for (int i = 0; i < n; i++) Sink += Plain(i); }
    static void B(int n) { for (int i = 0; i < n; i++) Sink += NoSuspendTask(i).GetAwaiter().GetResult(); }
    static void C(int n) { for (int i = 0; i < n; i++) Sink += NoSuspendValueTask(i).GetAwaiter().GetResult(); }
    static void D(int n) { for (int i = 0; i < n; i++) Sink += SuspendsTask(i).GetAwaiter().GetResult(); }
    static void E(int n) { for (int i = 0; i < n; i++) Sink += SuspendsValueTask(i).GetAwaiter().GetResult(); }
    static void F(int n) { for (int i = 0; i < n; i++) Sink += ViaTaskRun(i).GetAwaiter().GetResult(); }

    // marginal bytes: run n, then 2n, subtract — one-time costs cancel out
    static long BytesPerOp(Action<int> body, int n = 20_000)
    {
        body(n); body(n); body(n);
        long a0 = GC.GetAllocatedBytesForCurrentThread(); body(n);
        long a1 = GC.GetAllocatedBytesForCurrentThread(); body(2 * n);
        long a2 = GC.GetAllocatedBytesForCurrentThread();
        return ((a2 - a1) - (a1 - a0)) / n;
    }
    public static void Main()
    {
        BuildTable();
        var outer = typeof(Machine).GetMethod(nameof(FetchAsync))!;
        var sm = outer.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;

        Console.WriteLine($"=== 1. the type Roslyn generated for FetchAsync ===");
        Console.WriteLine($"  [AsyncStateMachine(typeof({sm.Name}))]");
        Console.WriteLine($"  full name  : {sm.FullName}");
        Console.WriteLine($"  kind       : {(sm.IsValueType ? "struct" : "class")}, private nested, implements {string.Join(", ", sm.GetInterfaces().Select(i => i.Name))}");
        Console.WriteLine($"  fields     :");
        foreach (var f in sm.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            Console.WriteLine($"    {Short(f.FieldType),-28} {f.Name}");
        foreach (var mm in sm.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            Console.WriteLine($"  method     : {mm.Name} ({mm.GetMethodBody()?.GetILAsByteArray()?.Length ?? 0} bytes of IL)");

        Console.WriteLine("\n=== 2. what is left of FetchAsync itself ===");
        Dump(outer);
        Console.WriteLine("=== 3. MoveNext — the method your body actually became ===");
        Dump(sm.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!);

        Console.WriteLine("=== 4. heap bytes per call ===");
        Console.WriteLine($"  plain synchronous method                        {BytesPerOp(A),4} B/op");
        Console.WriteLine($"  async Task<int>, awaits an already-done task    {BytesPerOp(B),4} B/op");
        Console.WriteLine($"  async ValueTask<int>, awaits an already-done    {BytesPerOp(C),4} B/op");
        Console.WriteLine($"  async Task<int>, suspends at await Task.Yield() {BytesPerOp(D),4} B/op");
        Console.WriteLine($"  async ValueTask<int>, suspends                  {BytesPerOp(E),4} B/op");
        Console.WriteLine($"  Task.Run(() => ...) capturing a local           {BytesPerOp(F),4} B/op");
        Console.WriteLine($"  checksum {Sink}");
    }
}
