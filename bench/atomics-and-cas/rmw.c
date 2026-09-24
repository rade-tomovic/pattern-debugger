/* Cameo for /systems/atomics-and-cas/ — what a read-modify-write compiles to
 * with and without the lock prefix. Built and disassembled on this box with:
 *   gcc -O2 -c bench/atomics-and-cas/rmw.c -o /tmp/rmw.o
 *   objdump -dr --no-show-raw-insn /tmp/rmw.o
 * x86-64 only; the page pastes that output verbatim.
 */
#include <stdatomic.h>

void plain_inc(int *p) { (*p)++; }

void atomic_inc(int *p) { __atomic_fetch_add(p, 1, __ATOMIC_SEQ_CST); }

int cas(int *p, int expected, int desired) {
    return __atomic_compare_exchange_n(p, &expected, desired, 0,
                                       __ATOMIC_SEQ_CST, __ATOMIC_SEQ_CST);
}
