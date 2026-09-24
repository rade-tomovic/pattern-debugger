// Watch a mapping's own /proc/self/maps "present" state, and its per-VMA smaps
// classification (Private_Dirty vs Shared_Dirty), change live as pages get touched
// and as the process forks. This box has no CAP_SYS_ADMIN, so /proc/self/pagemap's
// physical frame number always reads back redacted to zero for an unprivileged
// process -- which is itself worth seeing once, in act 1. Every other fact here
// (present/absent, and which smaps bucket a page falls in) needs no privilege at all
// and is exactly as informative: "present" proves demand paging, and "Shared_Dirty
// -> Private_Dirty" proves copy-on-write, without needing the frame number itself.
//   gcc -O2 -o pagemap-walk pagemap-walk.c && ./pagemap-walk
#define _GNU_SOURCE
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/mman.h>
#include <sys/wait.h>

static void show(const char *what, volatile void *addr) {
    int fd = open("/proc/self/pagemap", O_RDONLY);
    uint64_t e = 0;
    if (pread(fd, &e, 8, ((uintptr_t)addr / 4096) * 8) != 8) { perror("pread"); exit(1); }
    close(fd);
    uint64_t pfn = e & ((1ULL << 55) - 1);
    if ((e >> 63) & 1)
        printf("%-34s vaddr 0x%012lx  present=1  pfn(raw)=0x%06lx\n", what, (uintptr_t)addr, pfn);
    else
        printf("%-34s vaddr 0x%012lx  present=0  no physical page at all\n", what, (uintptr_t)addr);
}

// Find the /proc/self/smaps block whose "start-end" range contains addr, and print
// its KernelPageSize/Rss/Shared_Dirty/Private_Dirty lines -- the fields the kernel
// uses to say "how many mappers does each resident page in this VMA have".
static void smaps_for(const char *label, volatile void *addr) {
    FILE *f = fopen("/proc/self/smaps", "r");
    char line[256];
    uintptr_t want = (uintptr_t)addr, start, end;
    int in_block = 0;
    long rss = -1, shared_dirty = -1, private_dirty = -1;
    while (fgets(line, sizeof line, f)) {
        if (sscanf(line, "%lx-%lx", &start, &end) == 2) {
            in_block = (want >= start && want < end);
            if (in_block) { rss = shared_dirty = private_dirty = -1; }
            continue;
        }
        if (!in_block) continue;
        sscanf(line, "Rss: %ld kB", &rss);
        sscanf(line, "Shared_Dirty: %ld kB", &shared_dirty);
        sscanf(line, "Private_Dirty: %ld kB", &private_dirty);
        if (rss >= 0 && shared_dirty >= 0 && private_dirty >= 0) {
            printf("%-34s Rss %3ld kB   Shared_Dirty %3ld kB   Private_Dirty %3ld kB\n",
                   label, rss, shared_dirty, private_dirty);
            fclose(f);
            return;
        }
    }
    fclose(f);
    printf("%-34s (no resident page in this VMA yet)\n", label);
}

int main(void) {
    size_t len = 16 * 1024 * 1024;
    volatile char *p = mmap(NULL, len, PROT_READ | PROT_WRITE,
                            MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
    if (p == MAP_FAILED) { perror("mmap"); return 1; }
    printf("mmap gave us %zu MiB of address space at 0x%012lx\n\n",
           len / (1024 * 1024), (uintptr_t)p);

    uintptr_t v = (uintptr_t)p;
    printf("the MMU splits 0x%012lx into  pml4 %lu | pdpt %lu | pd %lu | pt %lu | offset %lu\n\n",
           v, (v >> 39) & 511, (v >> 30) & 511, (v >> 21) & 511, (v >> 12) & 511, v & 4095);

    printf("--- 1. demand paging (present bit, no privilege needed) ---\n");
    show("after mmap, before any touch", p);
    p[0] = 'A';
    show("after writing one byte", p);
    show("its neighbour, 1 MiB further in", p + 1024 * 1024);
    printf("(pfn(raw) reads 0 either way: unprivileged /proc/self/pagemap always redacts\n");
    printf(" the physical frame number to zero. present/absent does not need the frame.)\n");

    printf("\n--- 2. the shared zero page, seen through RSS instead of a frame number ---\n");
    volatile char *z = p + 8 * 1024 * 1024;
    smaps_for("before touching z[0..2]", z);
    char a = z[0], b = z[4096], c = z[8192];          /* three READ touches, three pages */
    printf("three pages read-touched, values %d %d %d\n", a, b, c);
    smaps_for("after 3 read touches", z);
    z[0] = 'Z';                                       /* now write to the first of them */
    smaps_for("after writing z[0] too", z);

    printf("\n--- 3. fork: one shared page becomes two private ones ---\n");
    smaps_for("parent, before fork", p);
    fflush(stdout);
    pid_t child = fork();
    if (child == 0) {
        smaps_for("child, right after fork (COW: shared)", p);
        p[0] = 'B';
        smaps_for("child, after its own write (COW broke)", p);
        fflush(stdout);
        _exit(0);
    }
    waitpid(child, NULL, 0);
    smaps_for("parent, after child's write (still its own copy)", p);
    return 0;
}
