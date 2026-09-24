// Evidence for /systems/threads-and-scheduling/async-state-machine/ — run with:
//   dotnet run bench/threads-and-scheduling/async-stack-trace.cs -c Release
//
// Three async frames deep, throwing after the await — so every frame in the trace
// has already returned once. What the runtime prints, and what .Result does to it.
static async Task<int> Level3(int x) { await Task.Yield(); throw new InvalidOperationException("boom"); }
static async Task<int> Level2(int x) => await Level3(x) + 1;
static async Task<int> Level1(int x) => await Level2(x) + 1;

try { await Level1(1); }
catch (Exception e) { Console.WriteLine(e.ToString()); }

Console.WriteLine("\n--- the same failure, reached through .Result ---");
try { _ = Level1(1).Result; }
catch (Exception e) { Console.WriteLine($"{e.GetType().Name}: {e.Message}\n  inner: {e.InnerException?.GetType().Name}: {e.InnerException?.Message}"); }
