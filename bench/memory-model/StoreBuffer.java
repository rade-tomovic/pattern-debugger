// Cross-language check for /systems/memory-model/ — the same store-buffer litmus
// as bench/memory-model/index.cs, in Java, with the same kind of spinning barrier
// so the window the two threads meet in is comparable. Run with:
//   scripts/bench-lock.sh java bench/memory-model/StoreBuffer.java
import java.util.concurrent.atomic.AtomicInteger;

public class StoreBuffer {
    static int x, y, r1, r2;
    static volatile int vx, vy;

    // sense-reversing spin barrier for 3 participants: spins rather than parking,
    // which is what keeps both workers inside the same few nanoseconds.
    static final AtomicInteger arrived = new AtomicInteger();
    static volatile int sense;

    static void await(int[] local) {
        local[0] ^= 1;
        if (arrived.incrementAndGet() == 3) { arrived.set(0); sense = local[0]; }
        else while (sense != local[0]) Thread.onSpinWait();
    }

    // second experiment: what does the keyword cost on one thread? if Java's
    // volatile store carries a full fence on x86 it must show up here, the same
    // way Interlocked.MemoryBarrier() does in the C# file.
    static int sink;
    static void costPlain(int n)    { for (int i = 0; i < n; i++) { x = i;  sink = y;  } }
    static void costVolatile(int n) { for (int i = 0; i < n; i++) { vx = i; sink = vy; } }

    static void cost() {
        final int ops = 20_000_000, runs = 9;
        double[] p = new double[runs], v = new double[runs];
        for (int i = 0; i < 3; i++) { costPlain(ops); costVolatile(ops); }
        for (int r = 0; r < runs; r++) {
            long t0 = System.nanoTime(); costPlain(ops);    p[r] = (System.nanoTime() - t0) / 1e6;
            t0 = System.nanoTime();      costVolatile(ops); v[r] = (System.nanoTime() - t0) / 1e6;
        }
        java.util.Arrays.sort(p); java.util.Arrays.sort(v);
        System.out.printf("java plain  store+load  %7.1f ms   %5.2f ns/op%n", p[runs/2], p[runs/2] * 1e6 / ops);
        System.out.printf("java volatile store+load %6.1f ms   %5.2f ns/op   ratio %.1fx  (sink %d)%n",
                          v[runs/2], v[runs/2] * 1e6 / ops, v[runs/2] / p[runs/2], sink);
    }

    public static void main(String[] a) throws Exception {
        run(500_000, false);
        run(500_000, true);
        cost();
    }

    static void run(int trials, boolean useVolatile) throws Exception {
        arrived.set(0); sense = 0;
        final int[] s1 = {0}, s2 = {0}, s0 = {0};
        long hits = 0; int first = -1;
        Thread t1 = new Thread(() -> {
            for (int i = 0; i < trials; i++) { await(s1); if (useVolatile) { vx = 1; r1 = vy; } else { x = 1; r1 = y; } await(s1); }
        });
        Thread t2 = new Thread(() -> {
            for (int i = 0; i < trials; i++) { await(s2); if (useVolatile) { vy = 1; r2 = vx; } else { y = 1; r2 = x; } await(s2); }
        });
        t1.start(); t2.start();
        for (int i = 0; i < trials; i++) {
            x = 0; y = 0; vx = 0; vy = 0; r1 = -1; r2 = -1;
            await(s0); await(s0);
            if (r1 == 0 && r2 == 0) { hits++; if (first < 0) first = i; }
        }
        t1.join(); t2.join();
        System.out.printf("java %-14s %,9d / %,d anomalies, first at trial %d%n",
                          useVolatile ? "volatile int" : "plain int", hits, trials, first);
    }
}
