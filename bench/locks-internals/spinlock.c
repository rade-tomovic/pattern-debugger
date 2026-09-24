/* no includes: this must compile for two targets with one set of headers (none) */
static int lock_word;                      /* 0 = free, 1 = held */

void acquire(void) {
    int expected = 0;
    while (!__atomic_compare_exchange_n(&lock_word, &expected, 1, 1,
                                        __ATOMIC_ACQUIRE, __ATOMIC_RELAXED))
        expected = 0;
}

void release(void) {
    __atomic_store_n(&lock_word, 0, __ATOMIC_RELEASE);
}
