// Evidence for /systems/stack-and-heap/stack-overflow-hunt/ — run with:
//   dotnet run bench/stack-and-heap/stack-overflow-hunt.cs -c Release
// The parent process re-executes itself (Environment.ProcessPath) for every case that
// is supposed to die, so one run produces the whole table below. This is the part of
// that file under review; the rest of Harness is the driver.
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

// ── the code under review ────────────────────────────────────────────────────
sealed class Comment
{
    public int Likes;
    public List<Comment> Replies = [];
}

static class Feed
{
    // Likes on a comment plus everything hanging off it.
    public static int TotalLikes(Comment c)
    {
        int total = c.Likes;
        foreach (var reply in c.Replies)
            total += TotalLikes(reply);
        return total;
    }
}

// ── the fix ──────────────────────────────────────────────────────────────────
static class FeedFixed
{
    public static int TotalLikes(Comment root)
    {
        int total = 0;
        var pending = new Stack<Comment>();
        pending.Push(root);
        while (pending.TryPop(out var c))
        {
            total += c.Likes;
            foreach (var reply in c.Replies)
                pending.Push(reply);
        }
        return total;
    }
}

// ── the fix people reach for second: keep the recursion, add a guard ─────────
static class FeedGuarded
{
    public static int MaxDepthSeen;
    public static int TotalLikes(Comment c, int depth = 1)
    {
        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
            throw new InvalidOperationException($"comment tree too deep (gave up at depth {depth})");
        if (depth > MaxDepthSeen) MaxDepthSeen = depth;
        int total = c.Likes;
        foreach (var reply in c.Replies)
            total += TotalLikes(reply, depth + 1);
        return total;
    }
}

static class Harness
{
    // Same walk, but every frame also holds a 256-byte scratch buffer.
    static int TotalLikesFat(Comment c)
    {
        Span<char> scratch = stackalloc char[128];
        scratch[0] = 'x';
        int total = c.Likes + (scratch[0] == 'x' ? 0 : 1);
        foreach (var reply in c.Replies)
            total += TotalLikesFat(reply);
        return total;
    }

    // A reply chain `depth` comments long: one reply per comment, all the way down.
    public static Comment Chain(int depth)
    {
        var root = new Comment { Likes = 1 };
        var cur = root;
        for (int i = 1; i < depth; i++) { var next = new Comment { Likes = 1 }; cur.Replies.Add(next); cur = next; }
        return root;
    }

    // What the ingest handler looks like in the service, try/catch and all.
    // NoInlining so the frame accounting below is about TotalLikes and nothing else.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Ingest(Comment root)
    {
        try
        {
            int likes = Feed.TotalLikes(root);
            Console.WriteLine($"scored thread: {likes} likes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LOG error: failed to score thread — {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("LOG finally: ingest finished");
        }
        Console.WriteLine("still alive");
    }

    // The same handler over the two variants, for the table.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void IngestVariant(string mode, Comment root)
    {
        try
        {
            int likes = mode == "fat" ? TotalLikesFat(root) : FeedGuarded.TotalLikes(root);
            Console.WriteLine($"scored thread: {likes} likes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LOG error: failed to score thread — {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("LOG finally: ingest finished");
        }
        Console.WriteLine($"still alive (guard saw depth {FeedGuarded.MaxDepthSeen})");
    }

    static void Run(string mode, int depth)
    {
        var root = Chain(depth);
        if (mode == "plain") Ingest(root);
        else IngestVariant(mode, root);
    }

    static readonly Regex Repeated = new(@"Repeated (\d+) times");
    static readonly Regex GaveUp = new(@"gave up at depth (\d+)");

    // The guarded walk exits cleanly, so its depth comes from the message it logged.
    static int GuardedDepth(int stackKiB, int depth = 400_000)
    {
        var (_, exit, o, e) = Child("guarded", depth, stackKiB);
        var m = GaveUp.Match(o);
        if (!m.Success || exit != 0) throw new Exception($"guarded run on {stackKiB} KiB reported nothing: exit={exit} {o} {e}");
        return int.Parse(m.Groups[1].Value);
    }

    static (int frames, int exit, string stdout, string stderr) Child(string mode, int depth, int? stackKiB, bool tieringOff = false)
    {
        var psi = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
        psi.ArgumentList.Add(mode);
        psi.ArgumentList.Add(depth.ToString());
        if (stackKiB is int k) psi.ArgumentList.Add(k.ToString());
        if (tieringOff) psi.Environment["DOTNET_TieredCompilation"] = "0";   // force fully optimised frames
        var p = Process.Start(psi)!;
        var err = p.StandardError.ReadToEndAsync();          // async on both pipes: a full one must not block the other
        var outp = p.StandardOutput.ReadToEndAsync();
        string o = outp.Result, e = err.Result;
        p.WaitForExit();
        var m = Repeated.Match(e);
        int frames = m.Success
            ? int.Parse(m.Groups[1].Value)                   // the runtime folded the repeats for us
            : Regex.Matches(e, @"TotalLikes\(Comment\)").Count;
        return (frames, p.ExitCode, o.Trim(), e);
    }

    static void Case(string label, string mode, int depth, int? stackKiB, double stackBytes, bool tieringOff = false)
    {
        var (frames, exit, o, _) = Child(mode, depth, stackKiB, tieringOff);
        string bytes = frames > 0 ? $"{stackBytes / frames,6:F1} B/frame" : "";
        Console.WriteLine($"  {label,-46} exit={exit,4}  frames={frames,7}  {bytes}  {o.Replace("\n", " | ")}");
    }

    // The main thread's stack reservation is whatever `ulimit -s` says on this
    // machine — read it back rather than assuming a number, so the table below
    // is correct on whatever box actually runs this file.
    static long MainThreadStackBytes()
    {
        var m = Regex.Match(File.ReadAllText("/proc/self/limits"), @"Max stack size\s+(\d+)");
        if (!m.Success) throw new Exception("could not read this process's stack limit from /proc/self/limits");
        return long.Parse(m.Groups[1].Value);
    }

    public static void Main(string[] args)
    {
        if (args.Length >= 2)                                 // running as a child
        {
            string mode = args[0]; int depth = int.Parse(args[1]);
            if (args.Length == 3)
            {
                var t = new Thread(() => Run(mode, depth), int.Parse(args[2]) * 1024);
                t.Start(); t.Join();
            }
            else Run(mode, depth);
            return;
        }

        const int Deep = 400_000;
        long mainStackBytes = MainThreadStackBytes();
        Console.WriteLine($"=== the broken walk on a {Deep:N0}-deep reply chain ===");
        Case($"main thread (this container's ulimit: {mainStackBytes / 1024.0:N0} KiB)", "plain", Deep, null, mainStackBytes);
        Case("thread, maxStackSize 1024 KiB", "plain", Deep, 1024, 1024.0 * 1024);
        Case("thread, maxStackSize 4096 KiB", "plain", Deep, 4096, 4096.0 * 1024);
        Case("thread, maxStackSize 32768 KiB", "plain", Deep, 32768, 32768.0 * 1024);
        Case("fat frame, 1024 KiB", "fat", Deep, 1024, 1024.0 * 1024);
        Case("1024 KiB, tiered compilation off", "plain", Deep, 1024, 1024.0 * 1024, tieringOff: true);

        Console.WriteLine("\n=== the same code on a 1,000-deep chain ===");
        Case("thread, maxStackSize 1024 KiB", "plain", 1_000, 1024, 1024.0 * 1024);

        Console.WriteLine("\n=== fix 2: keep the recursion, ask the runtime for headroom ===");
        Case("guarded, 1024 KiB", "guarded", Deep, 1024, 1024.0 * 1024);
        Case("guarded, 8192 KiB", "guarded", Deep, 8192, 8192.0 * 1024);

        // Is the headroom TryEnsureSufficientExecutionStack keeps back a fixed number of
        // bytes, or a fraction of the stack? Fit depth = (stack - reserve) / frame over
        // four stack sizes: a fixed reserve makes the fit's intercept the reserve itself.
        Console.WriteLine("\n=== what the guard actually reserves ===");
        int[] sizes = [1024, 2048, 4096, 8192];
        int[] depths = new int[sizes.Length];
        for (int i = 0; i < sizes.Length; i++) depths[i] = GuardedDepth(sizes[i]);
        double mx = sizes.Average(k => (double)k * 1024), my = depths.Average(d => (double)d);
        double sxy = 0, sxx = 0;
        for (int i = 0; i < sizes.Length; i++) { double dx = sizes[i] * 1024.0 - mx; sxy += dx * (depths[i] - my); sxx += dx * dx; }
        double slope = sxy / sxx, frame = 1.0 / slope, reserve = -(my - slope * mx) * frame;
        for (int i = 0; i < sizes.Length; i++)
        {
            double stack = sizes[i] * 1024.0;
            Console.WriteLine($"  {sizes[i],6} KiB  guard gave up at depth {depths[i],7}  implied reserve at {frame:F1} B/frame = {stack - depths[i] * frame,10:N0} B ({(stack - depths[i] * frame) / stack * 100,5:F1}% of the stack)");
        }
        Console.WriteLine($"  fit: frame = {frame:F1} B, reserve = {reserve:N0} B = {reserve / 1024:F1} KiB");

        Console.WriteLine("\n=== fix 1: an explicit stack on the heap ===");
        var deep = Chain(Deep);
        int total = FeedFixed.TotalLikes(deep);
        var small = Chain(5);
        Console.WriteLine($"  iterative on {Deep:N0} deep = {total} (correct: {total == Deep})");
        Console.WriteLine($"  agreement on a 5-deep chain: recursive={Feed.TotalLikes(small)} iterative={FeedFixed.TotalLikes(small)}");
        if (total != Deep || Feed.TotalLikes(small) != FeedFixed.TotalLikes(small)) throw new Exception("FAIL");

        Console.WriteLine("\n=== what the framework does about the same problem ===");
        string nested = new string('[', 200) + new string(']', 200);
        try { using var doc = JsonDocument.Parse(nested); Console.WriteLine("  parsed"); }
        catch (Exception ex) { Console.WriteLine($"  JsonDocument.Parse on 200 levels of nesting -> {ex.GetType().Name}: {ex.Message.Split('\n')[0]}"); }
        Console.WriteLine($"  JsonSerializerOptions.MaxDepth default = {new JsonSerializerOptions().MaxDepth}, JsonReaderOptions.MaxDepth default = {default(JsonReaderOptions).MaxDepth} (0 means 64)");
        Console.WriteLine("PASS");
    }
}
