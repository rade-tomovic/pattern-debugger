/* Cross-check for /systems/atomics-and-cas/false-sharing/ — the same experiment
 * outside the CLR, so the effect can be attributed to the hardware rather than
 * to the JIT. Build and run with:
 *   gcc -O2 -pthread bench/atomics-and-cas/false-sharing.c -o /tmp/fsc
 *   scripts/bench-lock.sh /tmp/fsc
 *
 * `volatile` here is C's volatile: it only stops gcc caching the counter in a
 * register, which is what makes the loop a real memory read-modify-write, the
 * same shape RyuJIT emits for `cells[slot]++`. It provides no atomicity — the
 * atomic rows use __atomic_fetch_add, which compiles to `lock addq`.
 */
#include <stdio.h>
#include <pthread.h>
#include <time.h>

static volatile long cells[4096];
static long N = 50000000;
static int STRIDE = 1, ATOMIC = 0;

static void *work(void *arg) {
    long slot = (long)arg * STRIDE;
    if (ATOMIC)
        for (long i = 0; i < N; i++) __atomic_fetch_add((long *)&cells[slot], 1, __ATOMIC_SEQ_CST);
    else
        for (long i = 0; i < N; i++) cells[slot]++;
    return 0;
}

static double run(int threads) {
    pthread_t t[8];
    struct timespec a, b;
    clock_gettime(CLOCK_MONOTONIC, &a);
    for (long i = 0; i < threads; i++) pthread_create(&t[i], 0, work, (void *)i);
    for (int i = 0; i < threads; i++) pthread_join(t[i], 0);
    clock_gettime(CLOCK_MONOTONIC, &b);
    return (b.tv_sec - a.tv_sec) * 1e3 + (b.tv_nsec - a.tv_nsec) / 1e6;
}

int main(void) {
    for (int atomic = 0; atomic <= 1; atomic++) {
        ATOMIC = atomic;
        N = atomic ? 5000000 : 50000000;
        for (int threads = 1; threads <= 4; threads += threads == 1 ? 1 : 2)
            for (int s = 0; s <= 1; s++) {
                if (threads == 1 && s == 1) continue;
                STRIDE = s ? 8 : 1;
                double best = 1e18;
                for (int r = 0; r < 5; r++) { double m = run(threads); if (m < best) best = m; }
                printf("%s  %d thread(s)  %3d B apart  best of 5: %8.1f ms  %6.2f ns/op\n",
                       atomic ? "atomic" : "plain ", threads, STRIDE * 8, best, best * 1e6 / N);
            }
    }
    return 0;
}
