// Cross-language check for /systems/memory-model/ — the same store-buffer litmus
// as bench/memory-model/index.cs, in Go. Run with:
//   scripts/bench-lock.sh go run bench/memory-model/storebuffer.go
package main

import (
	"fmt"
	"runtime"
	"sync/atomic"
)

var x, y, r1, r2 int32

// sense-reversing spin barrier for 3 participants
var arrived int32
var sense int32

func await(local *int32) {
	*local ^= 1
	if atomic.AddInt32(&arrived, 1) == 3 {
		atomic.StoreInt32(&arrived, 0)
		atomic.StoreInt32(&sense, *local)
	} else {
		for atomic.LoadInt32(&sense) != *local {
			runtime.Gosched()
		}
	}
}

func run(trials int, useAtomic bool) {
	atomic.StoreInt32(&arrived, 0)
	atomic.StoreInt32(&sense, 0)
	var s0, s1, s2 int32
	hits, first := 0, -1
	done := make(chan bool, 2)

	go func() {
		runtime.LockOSThread()
		for i := 0; i < trials; i++ {
			await(&s1)
			if useAtomic {
				atomic.StoreInt32(&x, 1)
				r1 = atomic.LoadInt32(&y)
			} else {
				x = 1
				r1 = y
			}
			await(&s1)
		}
		done <- true
	}()
	go func() {
		runtime.LockOSThread()
		for i := 0; i < trials; i++ {
			await(&s2)
			if useAtomic {
				atomic.StoreInt32(&y, 1)
				r2 = atomic.LoadInt32(&x)
			} else {
				y = 1
				r2 = x
			}
			await(&s2)
		}
		done <- true
	}()

	for i := 0; i < trials; i++ {
		x, y, r1, r2 = 0, 0, -1, -1
		await(&s0)
		await(&s0)
		if r1 == 0 && r2 == 0 {
			hits++
			if first < 0 {
				first = i
			}
		}
	}
	<-done
	<-done
	kind := "plain int32"
	if useAtomic {
		kind = "sync/atomic"
	}
	fmt.Printf("go   %-14s %9d / %d anomalies, first at trial %d\n", kind, hits, trials, first)
}

func main() {
	runtime.LockOSThread()
	run(500000, false)
	run(500000, true)
}
