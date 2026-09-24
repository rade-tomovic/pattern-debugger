/* The bare user->kernel->user crossing, with as little else in the frame as possible.
   Build: gcc -O2 -o floor syscall-cost-floor.c    Run: scripts/bench-lock.sh ./floor  */
#define _GNU_SOURCE
#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include <fcntl.h>
#include <unistd.h>
#include <sys/syscall.h>

#define N 1000000
#define RUNS 7

static double ns_since(struct timespec s) {
  struct timespec e;
  clock_gettime(CLOCK_MONOTONIC, &e);
  return (e.tv_sec - s.tv_sec) * 1e9 + (e.tv_nsec - s.tv_nsec);
}
static int cmp(const void *a, const void *b) {
  double x = *(const double *)a, y = *(const double *)b;
  return x < y ? -1 : x > y;
}
static double median(double *xs) { qsort(xs, RUNS, sizeof(double), cmp); return xs[RUNS / 2]; }

int main(void) {
  struct timespec s;
  volatile long sink = 0;
  int fd = open("/dev/null", O_WRONLY);
  char b = 'x';
  double loop[RUNS], gpp[RUNS], wr[RUNS];

  for (int r = -2; r < RUNS; r++) {            /* r < 0 = warmup */
    clock_gettime(CLOCK_MONOTONIC, &s);
    for (int i = 0; i < N; i++) sink += i;                     /* loop overhead, no crossing */
    double l = ns_since(s) / N;

    clock_gettime(CLOCK_MONOTONIC, &s);
    for (int i = 0; i < N; i++) sink += syscall(SYS_getppid);  /* crossing, no work at all */
    double g = ns_since(s) / N;

    clock_gettime(CLOCK_MONOTONIC, &s);
    for (int i = 0; i < N; i++) sink += write(fd, &b, 1);      /* crossing + a device that discards */
    double w = ns_since(s) / N;

    if (r >= 0) { loop[r] = l; gpp[r] = g; wr[r] = w; }
  }
  printf("empty loop iteration : median %6.2f ns\n", median(loop));
  printf("syscall(SYS_getppid) : median %6.2f ns\n", median(gpp));
  printf("write(fd, 1 byte)    : median %6.2f ns\n", median(wr));
  return 0;
}
