/* The whole point of this file is one instruction: `syscall` on line ~9 of the disassembly.
   Everything else is glibc shuffling arguments from the C calling convention into the
   convention the kernel expects.

   Build:   gcc -O2 -static -o syscall-crossing syscall-crossing.c
   Look at: objdump -d syscall-crossing | awk '/<syscall>:/{f=1} f{print} f && /^$/{exit}'      */
#define _GNU_SOURCE
#include <sys/syscall.h>
#include <unistd.h>

int main(void) {
    char c = 'x';
    return (int)syscall(SYS_write, 1, &c, 1);
}
