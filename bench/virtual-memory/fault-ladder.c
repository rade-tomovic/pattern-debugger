// How many page faults one walk over a 64 MiB region costs, by kind. Every row walks
// one byte per 4 KiB page; only what the page tables (and the page cache) already hold
// changes. No timing here — this file reports the kernel's own fault counters and
// nothing else, because a count is a fact tomorrow's run will repeat and a duration is
// not.
//   gcc -O2 -o fault-ladder fault-ladder.c && ./fault-ladder
#define _GNU_SOURCE
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/mman.h>
#include <sys/resource.h>

#define PAGES 16384                        /* 64 MiB in 4 KiB pages */
#define LEN   ((size_t)PAGES * 4096)

static long majflt(void) { struct rusage r; getrusage(RUSAGE_SELF, &r); return r.ru_majflt; }
static long minflt(void) { struct rusage r; getrusage(RUSAGE_SELF, &r); return r.ru_minflt; }
static volatile long sink;

static void report(const char *what, long mi, long ma) {
    printf("%-42s minflt %7ld  majflt %7ld\n", what, mi, ma);
}

int main(void) {
    long m0, j0, mi, ma;

    /* A - anonymous memory, first touch is a WRITE: the kernel must find a frame and zero it */
    {
        char *p = mmap(NULL, LEN, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
        m0 = minflt(); j0 = majflt();
        for (size_t i = 0; i < PAGES; i++) p[i * 4096] = 1;
        mi = minflt() - m0; ma = majflt() - j0;
        munmap(p, LEN);
    }
    report("A anonymous, first touch is a write", mi, ma);

    /* B - anonymous memory, first touch is a READ: the kernel maps the shared zero page */
    {
        char *p = mmap(NULL, LEN, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
        m0 = minflt(); j0 = majflt();
        long acc = 0;
        for (size_t i = 0; i < PAGES; i++) acc += p[i * 4096];
        mi = minflt() - m0; ma = majflt() - j0; sink += acc;
        munmap(p, LEN);
    }
    report("B anonymous, first touch is a read", mi, ma);

    /* C - the same walk again, nothing left to fault */
    {
        char *p = mmap(NULL, LEN, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
        for (size_t i = 0; i < PAGES; i++) p[i * 4096] = 1;
        m0 = minflt(); j0 = majflt();
        for (size_t i = 0; i < PAGES; i++) p[i * 4096] = 2;
        mi = minflt() - m0; ma = majflt() - j0;
        report("C anonymous, already resident", mi, ma);
        munmap(p, LEN);
    }

    /* a real file on this box's disk, 64 MiB of non-zero bytes */
    const char *path = "/var/tmp/vm-fault-ladder.bin";
    int fd = open(path, O_RDWR | O_CREAT | O_TRUNC, 0644);
    if (fd < 0) { perror("open"); return 1; }
    char *buf = malloc(1 << 20); memset(buf, 0xA5, 1 << 20);
    for (int i = 0; i < 64; i++) if (write(fd, buf, 1 << 20) != (1 << 20)) { perror("write"); return 1; }
    fsync(fd);

    /* D - file-backed, page cache dropped: every fault waits for the disk.
           MADV_RANDOM turns readahead off, so one fault means exactly one page. */
    {
        posix_fadvise(fd, 0, 0, POSIX_FADV_DONTNEED);
        char *p = mmap(NULL, LEN, PROT_READ, MAP_PRIVATE, fd, 0);
        madvise(p, LEN, MADV_RANDOM);
        m0 = minflt(); j0 = majflt();
        long acc = 0;
        for (size_t i = 0; i < PAGES; i++) acc += p[i * 4096];
        mi = minflt() - m0; ma = majflt() - j0; sink += acc;
        munmap(p, LEN);
    }
    report("D file-backed, cold, readahead off", mi, ma);

    /* E - same file, cold again, but the kernel's default readahead left on: what a real
           sequential reader experiences. It maps a batch of neighbouring pages per fault. */
    {
        posix_fadvise(fd, 0, 0, POSIX_FADV_DONTNEED);
        char *p = mmap(NULL, LEN, PROT_READ, MAP_PRIVATE, fd, 0);
        m0 = minflt(); j0 = majflt();
        long acc = 0;
        for (size_t i = 0; i < PAGES; i++) acc += p[i * 4096];
        mi = minflt() - m0; ma = majflt() - j0; sink += acc;
        munmap(p, LEN);
    }
    report("E file-backed, cold, readahead on", mi, ma);

    /* F - same file, still warm in the page cache, mapped fresh: no disk at all */
    {
        char *p = mmap(NULL, LEN, PROT_READ, MAP_PRIVATE, fd, 0);
        madvise(p, LEN, MADV_RANDOM);
        m0 = minflt(); j0 = majflt();
        long acc = 0;
        for (size_t i = 0; i < PAGES; i++) acc += p[i * 4096];
        mi = minflt() - m0; ma = majflt() - j0; sink += acc;
        munmap(p, LEN);
    }
    report("F file-backed, warm page cache", mi, ma);

    close(fd); unlink(path);
    return 0;
}
