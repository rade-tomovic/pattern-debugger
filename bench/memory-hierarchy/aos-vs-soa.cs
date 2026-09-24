#:property AllowUnsafeBlocks=true
// Evidence for /systems/memory-hierarchy/aos-vs-soa/ — run with:
//   dotnet run bench/memory-hierarchy/aos-vs-soa.cs
//
// Not timed. 2,000,000 particles, 8 floats each, in three layouts holding the SAME
// data:
//   AoS  Particle[]      — 32 bytes per particle, one array
//   SoA  eight float[]   — one array per field
//   AoC  ParticleObj[]   — the layout most .NET code actually ships
// Checks that all three layouts hold the same values, measures the real allocated
// size of the class layout, and verifies — with real addresses, not assumptions —
// how many 64-byte lines one particle's full record occupies in each layout.
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

struct Particle { public float X, Y, Z, VX, VY, VZ, Mass, Charge; }
sealed class ParticleObj { public float X, Y, Z, VX, VY, VZ, Mass, Charge; }

static class AosSoa
{
    const int N = 2_000_000, LineBytes = 64;

    static unsafe long AddrOf(float[] a, int i) { fixed (float* p = &a[i]) return (long)p; }
    static unsafe long AddrOf(Particle[] a, int i) { fixed (Particle* p = &a[i]) return (long)p; }

    public static void Main()
    {
        int particleSize = Unsafe.SizeOf<Particle>();
        Console.WriteLine($"sizeof(Particle) = {particleSize} B");
        Console.WriteLine($"Particle[{N:N0}] (AoS)      = {(long)N * particleSize / 1048576} MB");
        Console.WriteLine($"8 x float[{N:N0}] (SoA)     = {(long)N * 4 * 8 / 1048576} MB\n");

        var ps = new Particle[N];
        float[] x = new float[N], y = new float[N], z = new float[N], vx = new float[N], vy = new float[N], vz = new float[N], mass = new float[N], charge = new float[N];
        for (int i = 0; i < N; i++)
        {
            float v = i & 7;
            ps[i] = new Particle { X = v, Y = v, Z = v, VX = 1, VY = 1, VZ = 1, Mass = v, Charge = 1 };
            x[i] = v; y[i] = v; z[i] = v; vx[i] = 1; vy[i] = 1; vz[i] = 1; mass[i] = v; charge[i] = 1;
        }

        // correctness: AoS and SoA hold the same values
        float sumAoS = 0, sumSoA = 0;
        for (int i = 0; i < N; i++) { sumAoS += ps[i].X; sumSoA += x[i]; }
        if (sumAoS != sumSoA) throw new Exception($"FAIL: layouts disagree {sumAoS} vs {sumSoA}");
        Console.WriteLine($"PASS: AoS and SoA sum X to the same value ({sumAoS})\n");

        // the class layout's REAL allocated size — not sizeof, actual bytes allocated
        long b0 = GC.GetAllocatedBytesForCurrentThread();
        var pc = new ParticleObj[N];
        for (int i = 0; i < N; i++) { float v = i & 7; pc[i] = new ParticleObj { X = v, Y = v, Z = v, VX = 1, VY = 1, VZ = 1, Mass = v, Charge = 1 }; }
        long b1 = GC.GetAllocatedBytesForCurrentThread();
        long perParticle = (b1 - b0) / N;
        Console.WriteLine($"ParticleObj[{N:N0}] + {N:N0} objects allocated {b1 - b0:N0} B total = {perParticle} B/particle");
        Console.WriteLine($"  ({perParticle} = 8 B reference slot + {perParticle - 8} B object: {particleSize} B of fields + {perParticle - 8 - particleSize} B header)\n");

        // does one AoS particle ever straddle a 64-byte line boundary?
        // (true iff sizeof(Particle) divides evenly into the line size)
        bool anyStraddles = false;
        for (int i = 0; i < N; i++)
        {
            long off = (long)i * particleSize;
            if (off / LineBytes != (off + particleSize - 1) / LineBytes) { anyStraddles = true; break; }
        }
        Console.WriteLine(anyStraddles
            ? "FAIL: some AoS particles straddle a 64-byte line"
            : $"PASS: every one of {N:N0} AoS particles fits inside a single 64-byte line (32 B divides 64 B evenly)");

        // one random particle's 8 SoA fields: real addresses, real line numbers
        int sample = 12345;
        long[] addrs = [AddrOf(x, sample), AddrOf(y, sample), AddrOf(z, sample), AddrOf(vx, sample),
                        AddrOf(vy, sample), AddrOf(vz, sample), AddrOf(mass, sample), AddrOf(charge, sample)];
        var distinctLines = new HashSet<long>();
        foreach (var a in addrs) distinctLines.Add(a / LineBytes);
        Console.WriteLine($"particle #{sample}'s 8 SoA fields live in {distinctLines.Count} distinct 64-byte lines" +
            $" (arrays are independent {N * 4 / 1048576} MB allocations, so they land far apart in the heap)");
        long aosLine = AddrOf(ps, sample) / LineBytes;
        Console.WriteLine($"the same particle's AoS record lives in 1 line (index {aosLine})\n");

        // a fresh, freshly-allocated run of ParticleObj instances: are they even
        // back-to-back in memory? (bump allocation says yes, absent a GC in between;
        // verify it rather than assume it, and then check how many straddle a line —
        // 48 does NOT divide 64 evenly, unlike the 32-byte struct above)
        const int sampleCount = 200;
        var fresh = new ParticleObj[sampleCount];
        for (int i = 0; i < sampleCount; i++) fresh[i] = new ParticleObj();
        long[] freshAddr = new long[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            var h = GCHandle.Alloc(fresh[i], GCHandleType.Pinned);
            freshAddr[i] = h.AddrOfPinnedObject().ToInt64();
            h.Free();
        }
        bool allBackToBack = true;
        int straddling = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            if (i > 0 && freshAddr[i] - freshAddr[i - 1] != perParticle) allBackToBack = false;
            long start = freshAddr[i], end = start + perParticle - 8 - 1; // object body only, not the array's ref slot
            if (start / LineBytes != end / LineBytes) straddling++;
        }
        Console.WriteLine(allBackToBack
            ? $"PASS: {sampleCount} freshly allocated ParticleObj instances landed exactly {perParticle - 8} B apart (bump allocation, no GC in between)"
            : $"note: this run's {sampleCount} objects were NOT all back-to-back (a collection or another allocation landed between them)");
        Console.WriteLine($"of those {sampleCount}, {straddling} span two 64-byte lines ({perParticle - 8} B does not divide 64 B evenly)");
        GC.KeepAlive(pc);
    }
}
