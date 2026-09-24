/* Evidence for /systems/locks-internals/lock-cost-ladder/ — what a POSIX
 * semaphore costs on Linux, and whether it touches the kernel when nobody is
 * contending it. Build and run with:
 *   gcc -O2 -pthread -o /tmp/sem bench/locks-internals/sem.c
 *   scripts/bench-lock.sh /tmp/sem 5000000 7                  # timings, 7 passes
 *   scripts/bench-lock.sh strace -f -c -e trace=futex /tmp/sem 5000000 1
 * usage: sem <iters> [reps]
 */
#include <semaphore.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>

static sem_t s;
static long counter;

static double now_ms(void) {
    struct timespec t;
    clock_gettime(CLOCK_MONOTONIC, &t);
    return t.tv_sec * 1e3 + t.tv_nsec / 1e6;
}

static int cmp_double(const void *a, const void *b) {
    double x = *(const double *)a, y = *(const double *)b;
    return (x > y) - (x < y);
}

int main(int argc, char **argv) {
    if (argc < 2) { fprintf(stderr, "usage: sem <iters> [reps]\n"); return 2; }
    long iters = atol(argv[1]);
    int reps = (argc > 2) ? atoi(argv[2]) : 1;
    if (reps < 1 || reps > 64) { fprintf(stderr, "reps must be 1..64\n"); return 2; }

    /* pshared=0: an unnamed, process-local semaphore, initial count 1 — the
     * closest C has to `new SemaphoreSlim(1, 1)` used as a mutex. */
    if (sem_init(&s, 0, 1) != 0) { perror("sem_init"); return 1; }

    double runs[64], sorted[64];
    for (int r = 0; r < reps; r++) {
        double t0 = now_ms();
        for (long i = 0; i < iters; i++) {
            sem_wait(&s);
            counter++;
            sem_post(&s);
        }
        runs[r] = now_ms() - t0;
    }
    for (int r = 0; r < reps; r++) sorted[r] = runs[r];
    qsort(sorted, reps, sizeof(double), cmp_double);
    double ms = sorted[reps / 2];

    printf("sem_wait/sem_post, 1 thread: %.1f ms (%.1f ns/op, median of %d) counter=%ld\n",
           ms, ms * 1e6 / (double)iters, reps, counter);
    if (reps > 1) {
        printf("  every pass (ms):");
        for (int r = 0; r < reps; r++) printf(" %.1f", runs[r]);
        printf("\n");
    }

    /* Ownership: a POSIX semaphore has none. Post it without ever waiting on
     * it and the permit count just goes up — nothing checks who is posting. */
    int v = -1;
    sem_post(&s);                       /* nobody waited; this still succeeds */
    sem_getvalue(&s, &v);
    printf("after one stray sem_post the count is %d, and sem_post returned success\n", v);
    return 0;
}
