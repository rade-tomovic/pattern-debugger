/* Cross-check for context-switch-cost.cs: the same blocking ping-pong with nothing managed in
   the frame — plain pthreads, a mutex and a condition variable.
   Build: gcc -O2 -o pp context-switch-cost.c    Run: scripts/bench-lock.sh ./pp  */
#include <pthread.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>

#define N 50000
#define RUNS 7

static pthread_mutex_t m = PTHREAD_MUTEX_INITIALIZER;
static pthread_cond_t c = PTHREAD_COND_INITIALIZER;
static int turn = 0;

static void *side_b(void *arg) {
  long rounds = (long)arg;
  for (long i = 0; i < rounds; i++) {
    pthread_mutex_lock(&m);
    while (turn == 0) pthread_cond_wait(&c, &m);
    turn = 0;
    pthread_cond_signal(&c);
    pthread_mutex_unlock(&m);
  }
  return 0;
}

static int cmp(const void *a, const void *b) {
  double x = *(const double *)a, y = *(const double *)b;
  return x < y ? -1 : x > y;
}

static double one_run(void) {
  pthread_t t;
  struct timespec s, e;
  pthread_create(&t, 0, side_b, (void *)(long)N);
  clock_gettime(CLOCK_MONOTONIC, &s);
  for (int i = 0; i < N; i++) {
    pthread_mutex_lock(&m);
    turn = 1;
    pthread_cond_signal(&c);
    while (turn == 1) pthread_cond_wait(&c, &m);
    pthread_mutex_unlock(&m);
  }
  clock_gettime(CLOCK_MONOTONIC, &e);
  pthread_join(t, 0);
  return ((e.tv_sec - s.tv_sec) * 1e9 + (e.tv_nsec - s.tv_nsec)) / N;
}

int main(void) {
  double r[RUNS];
  one_run();                       /* warmup */
  for (int i = 0; i < RUNS; i++) r[i] = one_run();
  qsort(r, RUNS, sizeof(double), cmp);
  printf("pthread cond ping-pong: median %.0f ns/round-trip\n", r[RUNS / 2]);
  return 0;
}
