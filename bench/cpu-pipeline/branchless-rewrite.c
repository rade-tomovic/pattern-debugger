/* Cameo for /systems/cpu-pipeline/branchless-rewrite/ — what a C compiler does with
 * the same two loops that RyuJIT compiles to branches. Build with:
 *   gcc -O2 -fno-tree-vectorize -c bench/cpu-pipeline/branchless-rewrite.c -o /tmp/bl.o
 *   objdump -d --no-show-raw-insn /tmp/bl.o
 * -fno-tree-vectorize is belt and braces here: this loop's trip count is a runtime
 * size_t, which gcc's -O2 cost model declines to vectorise, so plain -O2 emits the
 * same cmov loop (only the operand order of one cmp differs). Give the loop a
 * compile-time-constant bound instead -- see sorted-array-branch.c -- and plain -O2
 * vectorises it (pcmpgtb/pand/paddq, no cmov). Verified with gcc 15.2 on this box;
 * check your own compiler's output before trusting either claim on a different one. */
#include <stddef.h>

long sum_if(const unsigned char *d, size_t n)
{
    long s = 0;
    for (size_t i = 0; i < n; i++)
        if (d[i] >= 128) s += d[i];
    return s;
}

long sum_ternary(const unsigned char *d, size_t n)
{
    long s = 0;
    for (size_t i = 0; i < n; i++)
        s += d[i] >= 128 ? d[i] : 0;
    return s;
}
