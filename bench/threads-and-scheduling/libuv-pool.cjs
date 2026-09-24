// Evidence for /systems/threads-and-scheduling/ — run with:
//   node bench/threads-and-scheduling/libuv-pool.cjs
//
// Node does have a pool to starve: libuv keeps a small worker pool
// (UV_THREADPOOL_SIZE) behind fs, dns.lookup, crypto and zlib. Fill it with
// CPU-heavy work and file I/O queues up behind it — while the event loop
// itself, which is not routed through that pool, keeps servicing timers on
// schedule. The point is the ORDER things finish in, not how long any of them
// took, so this prints a sequence number, not a clock.
const fs = require('fs'), crypto = require('crypto');
console.log('UV_THREADPOOL_SIZE =', process.env.UV_THREADPOOL_SIZE ?? '(unset, libuv default 4)');

let seq = 0;
const next = () => ++seq;

// a control read with the pool otherwise idle
fs.readFile('/etc/hostname', () => console.log(next(), 'control fs.readFile (pool idle) completed'));

setTimeout(() => {
  // saturate the pool with 4 CPU-heavy jobs, then queue one more readFile and
  // one event-loop timer behind them
  for (let i = 0; i < 4; i++) {
    crypto.pbkdf2('p', 's', 900000, 64, 'sha512', () => console.log(next(), `pbkdf2 job finished, freeing a pool slot`));
  }
  setTimeout(() => console.log(next(), 'event-loop timer fired (not routed through the pool)'), 10);
  fs.readFile('/etc/hostname', () => console.log(next(), 'fs.readFile queued behind 4 pbkdf2 jobs completed'));
}, 50);
