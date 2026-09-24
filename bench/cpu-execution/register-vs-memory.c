// Same loop twice. The only difference is where the accumulator lives:
// a register the compiler chose, or a 4-byte slot on the stack.
//   gcc -O2 -fno-stack-protector -fno-tree-vectorize -c register-vs-memory.c -o rvm.o
//   objdump -d --no-show-raw-insn rvm.o
// This file is not timed. It exists to be disassembled — see the topic page.
#define N 4096            // 16 KiB of ints — fits in L1d, so this is not a cache test

static int a[N];

__attribute__((noinline)) int sum_register(void)
{
    int total = 0;                       // compiler is free to keep this in a register
    for (int i = 0; i < N; i++)
        total += a[i];
    return total;
}

__attribute__((noinline)) int sum_memory(void)
{
    volatile int total = 0;              // volatile = must live in memory, reloaded every time
    for (int i = 0; i < N; i++)
        total += a[i];
    return total;
}

int main(void)
{
    for (int i = 0; i < N; i++) a[i] = i & 7;
    return sum_register() == sum_memory() ? 0 : 1;   // both must agree
}
