/* Evidence for /systems/locks-internals/ — the C half, where the fast path is
 * visible. Build and run with:
 *   gcc -O2 -pthread -o /tmp/mutex bench/locks-internals/mutex.c
 *   scripts/bench-lock.sh /tmp/mutex 1 10000000 0 2000000 5   # timings, 5 passes
 *   scripts/bench-lock.sh strace -f -c -e trace=futex /tmp/mutex 1 10000000 0
 * usage: mutex <threads> <iters-per-thread> <spin> [getppid-calls] [reps]
 * strace makes the timings meaningless, so the syscall counts and the
 * nanoseconds come from separate runs; reps defaults to 1 so the traced run
 * does exactly one pass, and the timed runs pass 5 and report the median.
 */
#include <pthread.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include <unistd.h>
#include <sys/syscall.h>

static pthread_mutex_t m = PTHREAD_MUTEX_INITIALIZER;
static long counter;
static long iters, spin;

static double now_ms(void) {
    struct timespec t;
    clock_gettime(CLOCK_MONOTONIC, &t);
    return t.tv_sec * 1e3 + t.tv_nsec / 1e6;
}

static int cmp_double(const void *a, const void *b) {
    double x = *(const double *)a, y = *(const double *)b;
    return (x > y) - (x < y);
}

static void *work(void *arg) {
    (void)arg;
    for (long i = 0; i < iters; i++) {
        pthread_mutex_lock(&m);
        for (long k = 0; k < spin; k++) counter += 1;   /* the critical section */
        counter++;
        pthread_mutex_unlock(&m);
    }
    return NULL;
}

int main(int argc, char **argv) {
    if (argc < 4) { fprintf(stderr, "usage: mutex <threads> <iters-per-thread> <spin> [getppid-calls] [reps]\n"); return 2; }
    int threads = atoi(argv[1]);
    iters = atol(argv[2]);
    spin = atol(argv[3]);

    long n = (argc > 4) ? atol(argv[4]) : 2000000;   /* getppid calls; pass 0 under strace */
    int reps = (argc > 5) ? atoi(argv[5]) : 1;       /* timed passes; median is reported */
    if (reps < 1 || reps > 64) { fprintf(stderr, "reps must be 1..64\n"); return 2; }

    pthread_t t[64];
    double runs[64];
    for (int r = 0; r < reps; r++) {                 /* every pass is in this one process,
                                                      * so the first is the warm-up */
        double t0 = now_ms();
        for (int i = 0; i < threads; i++) pthread_create(&t[i], NULL, work, NULL);
        for (int i = 0; i < threads; i++) pthread_join(t[i], NULL);
        runs[r] = now_ms() - t0;
    }
    double sorted[64];
    for (int r = 0; r < reps; r++) sorted[r] = runs[r];
    qsort(sorted, reps, sizeof(double), cmp_double);
    double lock_ms = sorted[reps / 2];

    /* the other side of the boundary: the cheapest syscall glibc will not
     * cache, so this is one full user -> kernel -> user round trip. */
    long sink = 0;
    double t0 = now_ms();
    for (long i = 0; i < n; i++) sink += syscall(SYS_getppid);
    double sys_ms = now_ms() - t0;

    printf("threads=%d iters=%ld spin=%ld counter=%ld  lock: %.1f ms (%.1f ns/acq, median of %d)  getppid: %.1f ns/call  sink=%ld\n",
           threads, iters, spin, counter, lock_ms,
           lock_ms * 1e6 / (double)(iters * threads), reps,
           n ? sys_ms * 1e6 / (double)n : 0.0, sink);
    if (reps > 1) {
        printf("  every pass (ms):");
        for (int r = 0; r < reps; r++) printf(" %.1f", runs[r]);
        printf("\n");
    }
    return 0;
}
