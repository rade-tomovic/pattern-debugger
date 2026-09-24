#define _GNU_SOURCE
#include <stdio.h>
#include <sys/mman.h>
#include <sys/resource.h>
int main(void) {
    size_t len = 64u << 20;                       /* 64 MiB = 16,384 pages */
    char *p = mmap(NULL, len, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
    for (size_t i = 0; i < len; i += 4096) p[i] = 1;
    struct rusage r; getrusage(RUSAGE_SELF, &r);
    printf("touched %zu pages, minor faults %ld, major faults %ld\n", len / 4096, r.ru_minflt, r.ru_majflt);
    return 0;
}
