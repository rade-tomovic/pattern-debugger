// Evidence for /systems/locks-internals/build-a-spinlock/ — does a call-free
// tight loop still starve the Go scheduler? Run with:
//   go run bench/locks-internals/go-preemption.go
// Pre-1.14 semantics would hang for the full 3 s; go1.24.7 here prints a
// scheduling delay in the tens of microseconds.
package main

import (
	"fmt"
	"runtime"
	"time"
)

func main() {
	runtime.GOMAXPROCS(1) // one P: only one goroutine can run at a time
	x := 0
	go func() {
		for i := 0; i < 1<<40; i++ { // no calls, no allocation, nothing to yield on
			x++
		}
	}()
	time.Sleep(10 * time.Millisecond) // let the tight loop get going
	ran := make(chan time.Duration, 1)
	t0 := time.Now()
	go func() { ran <- time.Since(t0) }()
	select {
	case d := <-ran:
		fmt.Printf("second goroutine ran after %v -> the tight loop WAS preempted\n", d)
	case <-time.After(3 * time.Second):
		fmt.Println("second goroutine never ran in 3s -> no preemption")
	}
	fmt.Println("go version:", runtime.Version(), "x =", x > 0)
}
