/* C cameo for /systems/cpu-pipeline/ and /systems/cpu-pipeline/sorted-array-branch/.
 * Same predicate as the C# exercise, three ways -- compile the same file three ways and
 * disassemble sum_big to see which one still contains a branch:
 *
 *   gcc -O2                                        -c sorted-array-branch.c   (auto-vectorised)
 *   gcc -O2 -fno-tree-vectorize                     -c sorted-array-branch.c   (a cmov, no branch)
 *   gcc -O2 -fno-tree-vectorize -fno-if-conversion  -c sorted-array-branch.c   (an actual branch)
 *
 * then `objdump -d --no-show-raw-insn <file>.o`. N is a compile-time constant on purpose --
 * that is what lets plain -O2 vectorise the loop at all; give sum_big a runtime trip count
 * instead and -O2 falls back to the cmov form even without -fno-tree-vectorize. */
#define N 16384

__attribute__((noinline)) static long sum_big(const unsigned char *d, int reps)
{
    long s = 0;
    for (int r = 0; r < reps; r++)
        for (int i = 0; i < N; i++)
            if (d[i] >= 128) s += d[i];
    return s;
}

int main(void)
{
    static unsigned char data[N];
    unsigned int state = 12345;
    for (int i = 0; i < N; i++) { state = state * 1103515245u + 12345u; data[i] = (unsigned char)((state >> 16) & 0xFF); }
    return (int)(sum_big(data, 1) & 0xFF);   /* keeps sum_big from being optimised away entirely */
}
