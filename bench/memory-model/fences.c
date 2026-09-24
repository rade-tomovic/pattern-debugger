/* Evidence for /systems/memory-model/ — what a release store, a sequentially
 * consistent store and an acquire load cost on two different ISAs. Freestanding
 * so the same file compiles for both targets with no sysroot. Built with:
 *   clang -O2 -S -ffreestanding fences.c -o -
 *   clang -O2 -S -ffreestanding --target=aarch64-linux-gnu fences.c -o -
 */
int flag;
int data;

/* what C#'s `volatile` write promises: a release store */
void publish_release(int v) {
    data = v;
    __atomic_store_n(&flag, 1, __ATOMIC_RELEASE);
}

/* what Interlocked gives you: a sequentially consistent store */
void publish_seq_cst(int v) {
    data = v;
    __atomic_store_n(&flag, 1, __ATOMIC_SEQ_CST);
}

/* what C#'s `volatile` read promises: an acquire load */
int observe_acquire(void) {
    return __atomic_load_n(&flag, __ATOMIC_ACQUIRE);
}
