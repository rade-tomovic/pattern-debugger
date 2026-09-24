// The bug hunt for /systems/gc-internals/the-leak-hunt/. No Stopwatch anywhere in this file —
// everything reported is a byte count (well, a megabyte count) or an object count.
//   dotnet run bench/gc-internals/the-leak-hunt.cs -c Release -- broken
//   dotnet run bench/gc-internals/the-leak-hunt.cs -c Release -- lambda
//   dotnet run bench/gc-internals/the-leak-hunt.cs -c Release -- fixed
//   dotnet run bench/gc-internals/the-leak-hunt.cs -c Release -- delegates
// "broken" is expected to fail. That failure is the page.
using System;
using System.Collections.Generic;
using System.Linq;

Harness.Run(args);

// ── the code under review ────────────────────────────────────────────────────

// Raises an event whenever configuration is reloaded. One instance per process.
static class ConfigWatcher
{
    public static event Action<string>? Changed;

    public static void Publish(string key) => Changed?.Invoke(key);

    // diagnostics only — a multicast delegate's invocation list IS its subscriber list
    public static Delegate[] Subscribers => Changed?.GetInvocationList() ?? [];
}

// Registered as a scoped service: the DI container builds one per request.
sealed class PricingService
{
    private readonly byte[] _priceTable = new byte[8 * 1024];   // per-request working set
    private decimal _margin = 0.15m;

    public PricingService()
    {
        ConfigWatcher.Changed += OnConfigChanged;               // stay in sync with live config
    }

    private void OnConfigChanged(string key)
    {
        if (key == "pricing.margin") _margin = 0.20m;
    }

    public decimal Quote(int units)
    {
        _priceTable[units & 8191] = 1;
        return units * _margin;
    }
}

// ── the fix people reach for first, which does not work ──────────────────────

sealed class PricingServiceLambdaFix : IDisposable
{
    private readonly byte[] _priceTable = new byte[8 * 1024];
    private decimal _margin = 0.15m;

    public PricingServiceLambdaFix()
    {
        ConfigWatcher.Changed += key => OnConfigChanged(key);
    }

    public void Dispose() => ConfigWatcher.Changed -= key => OnConfigChanged(key);

    private void OnConfigChanged(string key)
    {
        if (key == "pricing.margin") _margin = 0.20m;
    }

    public decimal Quote(int units)
    {
        _priceTable[units & 8191] = 1;
        return units * _margin;
    }
}

// ── the fix ──────────────────────────────────────────────────────────────────

sealed class PricingServiceFixed : IDisposable
{
    private readonly byte[] _priceTable = new byte[8 * 1024];
    private readonly Action<string> _onConfigChanged;           // the exact delegate we subscribed
    private decimal _margin = 0.15m;

    public PricingServiceFixed()
    {
        _onConfigChanged = OnConfigChanged;
        ConfigWatcher.Changed += _onConfigChanged;
    }

    public void Dispose() => ConfigWatcher.Changed -= _onConfigChanged;

    private void OnConfigChanged(string key)
    {
        if (key == "pricing.margin") _margin = 0.20m;
    }

    public decimal Quote(int units)
    {
        _priceTable[units & 8191] = 1;
        return units * _margin;
    }
}

// ── the harness ──────────────────────────────────────────────────────────────

static class Harness
{
    const int Requests = 20_000;

    public static void Run(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "broken";
        if (mode == "delegates") { Delegates(); return; }
        Console.WriteLine($"runtime={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}  " +
                          $"mode={mode}");

        decimal total = 0;
        WeakReference? firstRequest = null;
        double baseline = Settled();
        Console.WriteLine("\n  requests | subscribers | live heap MB | request #1 still alive");
        Console.WriteLine($"  {0,8:N0} | {ConfigWatcher.Subscribers.Length,11:N0} | {baseline,12:F1} | n/a");

        for (int i = 1; i <= Requests; i++)
        {
            switch (mode)
            {
                case "fixed":
                {
                    using var svc = new PricingServiceFixed();  // scoped lifetime, disposed at the end
                    total += svc.Quote(i);
                    if (i == 1) firstRequest = new WeakReference(svc);
                    break;
                }
                case "lambda":
                {
                    using var svc = new PricingServiceLambdaFix();
                    total += svc.Quote(i);
                    if (i == 1) firstRequest = new WeakReference(svc);
                    break;
                }
                default:
                {
                    var svc = new PricingService();
                    total += svc.Quote(i);
                    if (i == 1) firstRequest = new WeakReference(svc);
                    break;
                }
            }

            if (i % 500 == 0) ConfigWatcher.Publish("pricing.margin");   // config reloads happen

            if (i % 5_000 == 0)
                Console.WriteLine($"  {i,8:N0} | {ConfigWatcher.Subscribers.Length,11:N0} | {Settled(),12:F1} | " +
                                  $"{firstRequest!.IsAlive}");
        }

        double growth = Settled() - baseline;
        Console.WriteLine($"\n  {Requests:N0} requests served, quote total {total:N0}");
        Console.WriteLine($"  live heap grew {growth:F1} MB ({growth * 1024 / Requests:F1} KB per request)");
        Console.WriteLine($"  subscribers still attached: {ConfigWatcher.Subscribers.Length:N0}");
        Console.WriteLine($"  who they are: {Census()}");
        Console.WriteLine($"  request #1's service still alive: {firstRequest!.IsAlive}");
        Console.WriteLine($"\n  a 100-request test would have grown the heap by " +
                          $"{growth * 1024 / Requests * 100:F0} KB — inside the noise of any assertion you would write");

        if (growth > 5.0)
            throw new Exception($"FAIL: live heap grew {growth:F1} MB across {Requests:N0} " +
                                $"request-scoped services that were all supposed to be garbage");

        Console.WriteLine("\nPASS: live heap is flat across the whole run");
    }

    // why `-=` with a lambda removes nothing, and `-=` with a method group works
    static void Delegates()
    {
        var sub = new Subscriber();
        Console.WriteLine($"  baseline: {ConfigWatcher.Subscribers.Length} subscriber(s)");

        // two delegates built from the same method group: distinct objects, but Equals
        Action<string> a = sub.OnChanged, b = sub.OnChanged;
        Console.WriteLine($"  two delegates from one method group: ReferenceEquals={ReferenceEquals(a, b)}, Equals={a.Equals(b)}");

        ConfigWatcher.Changed += a;
        ConfigWatcher.Changed -= b;                         // a different object, but equal → removed
        Console.WriteLine($"  method group  += then -=  leaves {ConfigWatcher.Subscribers.Length} subscriber(s)");

        ConfigWatcher.Changed += key => sub.OnChanged(key);
        ConfigWatcher.Changed -= key => sub.OnChanged(key); // a different lambda → not equal → not removed
        Console.WriteLine($"  lambda        += then -=  leaves {ConfigWatcher.Subscribers.Length} subscriber(s)");
        foreach (var d in ConfigWatcher.Subscribers)
            Console.WriteLine($"  still attached: target={d.Target?.GetType().Name}, method={d.Method.Name}");
    }

    // what gcroot would tell you, from inside the process: who is still subscribed
    static string Census()
    {
        var byType = new Dictionary<string, int>();
        foreach (var d in ConfigWatcher.Subscribers)
            byType[d.Target?.GetType().Name ?? "(static)"] =
                byType.GetValueOrDefault(d.Target?.GetType().Name ?? "(static)") + 1;
        return byType.Count == 0 ? "(nobody)"
            : string.Join(", ", byType.Select(kv => $"{kv.Key} × {kv.Value:N0}"));
    }

    // heap size once everything collectable has actually been collected
    static double Settled()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        return GC.GetTotalMemory(true) / 1048576.0;
    }
}

// used only by the delegate-identity demo above
sealed class Subscriber
{
    public void OnChanged(string key) { }
}
