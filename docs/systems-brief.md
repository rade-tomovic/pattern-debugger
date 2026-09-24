# Systems Brief — how every systems & concurrency page gets written

The binding spec for the `machine`, `memory`, and `concurrency` sections (URL base `/systems`).

`docs/content-brief.md` still governs everything mechanical: frontmatter contracts, MDX safety,
component imports, C# style, link rules, the design system. **Read it first.** This brief
overrides it on exactly two things: **page anatomy** and **what counts as verified**.

Read, in order:
1. `docs/content-brief.md` — mechanics, MDX safety, voice
2. This file — anatomy and evidence rules
3. `src/data/curriculum.ts` — the content tree; the ONLY valid link targets
4. `src/pages/patterns/two-pointers/two-sum-ii.mdx` — the voice exemplar (ignore its anatomy)

## The reader — read this twice

Senior .NET engineer, 11 years, deep on C# and on real-world system design. **No CS degree and
no formal systems education.** They were never taught what a page table is, what the kernel
does, or what a cache line is — not because they couldn't handle it, but because nobody ever
put it in front of them.

That combination is the whole editorial problem:

- **Never condescend.** They have shipped more production code than the person writing this.
  Do not explain what a variable is, do not pad with encouragement, do not say "don't worry,
  this is complicated".
- **Never assume.** If a page uses "kernel", "syscall", "page", "cache line", "barrier",
  "register", "context switch" — the first page that uses it defines it in one clean sentence,
  and later pages link back rather than redefining. There is no "as you probably know".
- **No filler.** Same as the rest of the site: no "in this article we will", no summary
  paragraph restating what was just said.
- When a term has a famous confusing overload (`volatile`, "atomic", "thread-safe", "memory
  model", "stack"), say which meaning you're using and name the other one. Half this reader's
  confusion is inherited from words that mean two things.

## The four jobs every page does

The reader asked for all four of these outcomes. A page is not finished until it serves all
four, and the anatomy below is built so that each has a home:

| goal | where it lives |
| --- | --- |
| genuine mental model (fill the CS gap) | `## how it actually works` + `## the mental model` |
| debug real production problems | `## what this looks like in prod` |
| answer senior/staff interview questions | `## interview drills` (topic pages) |
| write faster code by design | the measurement tables and `## why` on exercise pages |

Never let a page become only one of the four. A `gc-internals` page that is all mechanism and
no production symptom has failed; so has one that is all "watch your allocations" folklore
with no mechanism underneath.

## File locations

- Topic page: `src/pages/systems/<topic-slug>/index.mdx`
- Exercise page: `src/pages/systems/<topic-slug>/<exercise-slug>.mdx`

All three sections (`machine`, `memory`, `concurrency`) share the `/systems` base, exactly like
`core`/`structures`/`advanced` share `/patterns`.

Frontmatter contracts are unchanged from `docs/content-brief.md`:

```
---
layout: '@layouts/TopicLayout.astro'
topic: <topic-slug>
---
```

```
---
layout: '@layouts/ProblemLayout.astro'
topic: <topic-slug>
problem: <exercise-slug>
---
```

NEVER touch `src/data/curriculum.ts`, `src/data/cheats.ts`, layouts, components, or
`global.css`. Only create your assigned MDX files, plus any runnable sources under
`bench/<topic-slug>/` that a page's code came from. (That directory is a historical name from
when this section carried benchmarks; it now just holds the runnable examples behind the pages.)

## Topic page anatomy

Sections in this order, lowercase headings, `##` level (no `#` h1 — the layout renders the
title, and headings get their `// ` prefix from CSS, never type it):

1. `## the ground floor` — **required on every page that leans on a term from another page.**
   3-6 bullets, one line each, defining exactly the terms this page assumes. Link the topic
   page that owns each term. On the three `machine` primer topics this section defines terms
   from nothing at all. If a page genuinely assumes nothing, omit the section rather than
   padding it.
2. `## core idea` — 2-4 sentences. What this thing is and why it exists. If the concept has
   sub-shapes, one comparison table.
3. `## how it actually works` — **the teaching section, and the longest.** `###` subsections
   are allowed and encouraged here (nowhere else). Mechanism, not vocabulary: what the
   hardware/OS/runtime physically does, in what order, and what it costs. Diagrams go in
   ```text fences (memory layouts, pipelines, page-table walks, lock state machines). At least
   one diagram per topic page — a picture of the mechanism is the point of these pages.
4. `## the mental model` — the compressed, memorable version: the one table, picture, or
   three-line rule worth carrying around. This is what the reader should still have in their
   head six months from now. Optionally close with `<Watch items={...} />` carrying the key
   quantities (sizes, latencies, counts).
5. `## why you should care` — where this shows up in real .NET services. Concrete: the metric
   that moves, the incident shape, the code review you can now do. 2-4 paragraphs or a table.
6. `## the same idea in other languages` — see the rules below.
7. `## exercises` — one intro line, then `<ProblemList topic="<slug>" />`. If the topic has no
   exercises, omit this section entirely (do not render an empty list).
8. `## interview drills` — 4-6 drills. Each drill is:
   - `**Q.** the question as an interviewer says it`
   - `- *weak answer* — …` (the plausible answer that gets a follow-up, and why it's weak)
   - `- *strong answer* — …` (what actually lands, 2-3 sentences)
   - `- *follow-up* — …` (the question they'll hit you with next, with its one-line answer)

   Drills must be questions a senior/staff interviewer actually asks, not trivia. "What's the
   size of a cache line" is trivia. "Why did adding threads make it slower" is a drill.

## Exercise page anatomy

Three kinds. `curriculum.ts` names which kind each exercise is; the summary field says it too.

### Kind A — predict, then work it out

The reader commits to an answer, then derives the real one by reasoning about what the machine
does. No stopwatch, ever. The payoff is a mechanism they can re-derive later, not a number they
would have to take on faith.

1. `## the question` — the setup, then the prediction prompt:
   `<Callout kind="insight" label="predict first">` asking them to commit to something
   *checkable by reasoning*: which of two loops touches more cache lines, which interleaving
   loses the update, what the field offsets will be, whether the branch is predictable. Never
   ask for a speed factor.
2. `## the code` — the complete, runnable code inside `<Solution>`, one ```csharp fence (blank
   lines around the fence).
3. `## work it out` — **the teaching section.** Walk the mechanism by hand: the addresses
   touched and which 64-byte lines they fall in; the exact interleaving; the field layout the
   alignment rules force; what the predictor sees. Use a ```text diagram or a table of steps.
   This replaces the stopwatch, and it is better than the stopwatch, because it transfers.
4. `## the answer` — what actually happens and why the prediction was right or wrong, tied
   back to the reasoning above. Where the answer is a *fact the machine can state* (a size, an
   offset, a field order, a count of lost updates, an emitted instruction), show the real
   program output in a ```text fence. Facts are welcome; durations are not.
5. `## why it works that way` — the general rule this instance is a case of. Close with
   `<Watch items={...} />` (required) carrying the durable quantities: cache-line size, field
   offsets, the invariant. Never a timing.
6. `## what this looks like in prod` — the symptom, qualitatively.
7. `## the same idea in other languages`
8. `## common bugs` — 3-5 bullets on how people get the *reasoning* wrong. Specific.

### Kind B — bug hunt

1. `## the code` — the broken code inside `<Solution>`, presented as something that would pass
   review. It must look reasonable; a bug hunt with an obvious bug teaches nothing.
2. `## find it` — `<Callout kind="insight" label="before you scroll">` naming exactly what to
   look for (the interleaving, the missing guarantee, the lifetime).
3. `## the failure` — **the demonstrated failure.** Paste the real output of the run where it
   broke, in a ```text fence. Then the failure table: for concurrency bugs an interleaving
   table (`step | thread A | thread B | shared state`), for lifetime/memory bugs a state table.
   Every row is a real step of a real execution.
4. `## why it breaks` — mechanism.
5. `## the fix` — corrected code inside `<Solution>`, then why *this* fix and what's wrong with
   the two fixes people reach for first. Close with `<Watch items={...} />` (required).
6. `## what this looks like in prod`
7. `## the same idea in other languages`
8. `## common bugs`

### Kind C — read the machine

For pages where the lesson is "here is what your code actually became".

1. `## the source`
2. `## what the machine got` — the **real** tool output (objdump, IL, decompiled state machine)
   in a ```text fence, trimmed to what matters but never edited or idealized.
3. `## line by line` — a mapping table: `output line | what it does | which source line`.
4. `## why it looks like that` — close with `<Watch items={...} />` (required).
5. `## what this looks like in prod`
6. `## the same idea in other languages`
7. `## common bugs`

## No measurements. At all.

This site teaches how the machine works. It is not a benchmark corpus, and an earlier version
of these pages lost sight of that — burying the mechanism under tables of milliseconds that no
reader can reproduce and that taught nothing a diagram would not teach better.

**The hard rule: no page contains a duration, a throughput, or a speed ratio.** Concretely,
none of these ever appear:

- a time in ms, µs, ns, seconds, or cycles-as-elapsed-time
- a speed comparison: "1.6× faster", "twice as slow", "an order of magnitude quicker"
- a throughput: ops/sec, MB/s, requests/sec
- a measurement table, a `label="the machine"` callout, or any statement about the box a
  number came from
- vaguer forms of the same thing: "measurably slower", "a noticeable win", "much faster in
  practice"

**What you may state instead**, and should:

- **Ordering and direction as a consequence of mechanism.** "Column-major traversal touches a
  new cache line on every element, row-major touches one per sixteen" is a fact about the
  hardware, derivable and durable. "Column-major is 1.6× slower" is a fact about one afternoon
  on one machine.
- **Counts, sizes, offsets and layouts.** `sizeof` results, field offsets, cache-line size,
  page size, how many allocations a loop performs, how many lost updates a race produced.
  These are facts the machine will state identically tomorrow, and a program may print them.
- **Asymptotic and structural claims.** O-notation, "this is one syscall per call rather than
  one per batch", "this holds the lock across the I/O".
- **Published orders of magnitude, clearly labelled as such**, in the one place they earn their
  keep: the latency ladder on `memory-hierarchy` (L1 ≈ 1 ns, main memory ≈ 80 ns, SSD ≈ 100 µs).
  Present them in a table titled as published, industry-standard figures for *reasoning about
  relative cost*, never as something measured here. This is the single exception, and it exists
  because the ratios between the rungs are the entire lesson of the memory hierarchy.

If you catch yourself wanting to prove a claim with a stopwatch, that is the signal to explain
the mechanism instead. If the mechanism cannot be explained, cut the claim.

## Verification protocol (mandatory, extends content-brief.md)

`dotnet` 10 is on PATH. Run a single file with `dotnet run <file>`. There is no timing
harness and no lock, because nothing on these pages is timed.

- **Every ```csharp block with runnable logic** is verified by single-file execution, exactly
  as in `docs/content-brief.md`: write it to a temp dir, run it, assert, print PASS. A snippet
  that does not compile is the one defect a reader will notice immediately.
- **Facts a program can state must come from running it**, not from memory: `sizeof` results,
  field offsets, allocation counts, the number of updates a race loses, the exact exception
  type and message, what `Unsafe.SizeOf` reports. Run it, paste the real output.
- **Bug-hunt pages must actually fail.** Run the broken code and capture the real failing
  output before writing the page. If a race won't reproduce, make it reproduce (more
  iterations, more threads, a `Thread.Yield()` in the window) and say in the page what you had
  to do — that is itself the lesson about why races survive testing. Then run the fix and
  confirm it holds under the same pressure.
- **C and assembly cameos** are compiled here, not written by hand:
  `gcc -O2 -S` or `gcc -O2 -c` + `objdump -d`. Paste real output. x86-64 only — say so. Keep a
  cameo under ~25 lines and annotate every line you keep. Use a cameo only where the managed
  runtime hides the mechanism (raw addresses, manual free, what a barrier compiles to, the
  actual call/ret). Never as decoration.
- **Every claim about the runtime, the OS or the CPU must be one you can defend**, not one that
  merely sounds right. The failure mode these pages have shown, repeatedly, is a plausible
  mechanism asserted as the cause of something real: "the SOH is compacted on every collection"
  (it is not), "gcc -O2 vectorises this loop" (it does not), "reordering the fields removes all
  fourteen padding bytes" (it does not). Where you can check it by running something, check it.
  Where you cannot, state it as the reasoning it is — or cut it.

## `## the same idea in other languages` — rules

A markdown table, in a `##` section, never inside a `<Callout>` (callouts are `not-prose` and
will render the table unstyled).

```
| language | what it's called | the trap |
| --- | --- | --- |
```

- Cover the languages where the concept genuinely differs — usually Java, Go, Python, C/C++.
  Four rows is typical. JavaScript/TypeScript when it's meaningful (workers, the event loop).
- **Every row must say something falsifiable and specific.** "Java has threads too" is filler.
  "Java's `volatile` gives acquire/release ordering, so it fixes what C#'s `volatile` fixes —
  but C's `volatile` gives no ordering at all and does not make anything atomic" is a row.
- The `trap` column is the point: where an engineer carrying intuition from one language gets
  burned in another.
- **If you are not certain of a language's exact semantics, drop the row.** A wrong claim about
  Go's memory model is worse than a three-row table. This site's credibility is claim-by-claim.
- The reader works in .NET. The table exists to prove the concept is not a .NET quirk — not to
  teach Go.

## MDX safety (build breaks otherwise — repeated because it bites hardest here)

These pages are full of the exact characters that break MDX.

- **Never a raw `<` or `{` in prose, headings, table cells, or bullets.** These pages will want
  to write `Dictionary<K,V>`, `Interlocked.CompareExchange<T>`, `i < n`, `x & (x-1)`,
  `1 << 20`, `std::memory_order`, `ref struct`. Every one of those goes in backticks or a
  fence. `<` inside a table cell breaks the build just as hard as in a paragraph.
- Component props use JSX braces — that's the one legal `{`.
- Blank lines around fenced blocks inside JSX components (`<Solution>`, `<Callout>`).
- No `#` h1. No typed `// ` in headings (CSS injects it).
- Arrows, em dashes, `≈`, `×`, `µ` in prose are fine and encouraged.

## Design system

Unchanged and non-negotiable. Dark only. The five accents keep their fixed meanings —
**amber** = the write/slow/left side, **cyan** = the read/fast/right side, **purple** = middle,
**green** = success/correct, **red** = failure/reject. On these pages that maps naturally:
green for the fixed version, red for the broken one, amber/cyan for two racing threads. Never
introduce a color, never use one against its meaning.

`<Callout>` kinds: `insight` (amber) · `tip` (green) · `trap` (red) · `note` (cyan).

## Cheat-card data (returned, not written into MDX)

For every topic you write, return a cheat object:
`{ signals: string[], tricks: string[], bugs: string[] }`, 3-5 items each, backticks for code
spans. For systems topics read the three groups as:

- `signals` — "you are looking at this phenomenon when…" (the symptom in a profiler, a metric,
  a stack trace, a review diff)
- `tricks` — the moves that fix or exploit it
- `bugs` — what people get wrong about it, including the confident-but-wrong beliefs

Same voice as `src/data/cheats.ts`. Do NOT also write these into the MDX — they render on the
topic page and the master cheat sheet from the registry.

## Per-topic mandates

Non-negotiable content per topic. Everything else is the writer's judgement.

### machine — the primer (assume nothing)

- **bits-and-memory** — bit/byte/word; hex and why it exists; two's complement and why negation
  is `~x + 1`; overflow as wraparound; what an *address* is; that RAM is one flat numbered
  array of bytes and everything else is a story told about it; alignment and padding, and the struct whose size is not the sum of its fields; endianness (and that it only bites at I/O boundaries).
  Must state plainly that a C# reference *is* an address the runtime manages.
- **process-and-thread** — what an OS actually does; user mode vs kernel mode and why the
  boundary exists; a syscall as the only door between them, and why crossing it is not free; process = an
  address space + handles; thread = a stack + registers + a scheduling entity; **why threads
  share the heap but never the stack** (this single fact explains most of the concurrency
  section); context switch and what it saves; runnable/running/blocked; preemption and the
  time slice. Ends by linking `stack-and-heap` and `threads-and-scheduling`.
- **cpu-execution** — registers vs memory; the fetch-decode-execute loop; the clock and what a
  cycle buys you; what an ISA is; the stack pointer, a call frame, and what `call`/`ret`
  actually do; why a call is not free: the frame it builds and the registers it must preserve. This is the natural home of the first C/asm
  cameo — a five-line C function, its real disassembly, annotated.

### memory

- **stack-and-heap** — the two allocators and their real difference (bump-and-forget vs
  managed lifetime); value vs reference types and the truth that `struct` lives *where it is
  declared*, not "on the stack" (a `struct` field of a class is on the heap); boxing, shown with the allocation it forces; `ref`/`in`/`out`; `Span<T>`, `stackalloc`, `ref struct`
  and why the compiler restricts them; the fixed stack size and what actually overflows it.
  Must correct the "structs are stack, classes are heap" folklore explicitly — the reader has
  certainly been taught it.
- **virtual-memory** — every process sees its own flat address space and none of it is real;
  pages, the page table, the MMU and TLB; minor vs major faults; demand paging and why a fresh
  large array is cheap until touched; copy-on-write; what "committed" vs "resident" vs
  "working set" mean, mapped onto the metrics a .NET service actually reports; why the OOM
  killer and `GC` pressure are different problems.
- **memory-hierarchy** — the ladder with published latencies (labelled as published); the
  cache line as the unit of transfer and the single most useful fact on the page; spatial and
  temporal locality; why row-major traversal wins, derived from which cache lines each order touches; the stride
  walk that exposes the 64-byte line; AoS vs SoA; a first mention of false sharing forward-linked to
  `atomics-and-cas`; prefetching; why `LinkedList<T>` loses to `List<T>` in practice even when
  Big-O says otherwise — link `/foundations/big-o/`.
- **cpu-pipeline** — pipelining as an assembly line; what a stall is; branch prediction, and why a mispredict costs the whole pipeline
  (worked through with the sorted/unsorted array); speculative and
  out-of-order execution — and the crucial hand-off: **the CPU reordering your loads and
  stores is the same machinery that makes the memory model necessary**, forward-link
  `memory-model`. SIMD in one honest paragraph.
- **gc-internals** — allocation as a pointer bump; what "garbage" means and why reachability
  not counting; generations and the generational hypothesis; what a collection physically does
  (mark, then relocate/compact) and why that implies stop-the-world; write barriers and the
  card table in one plain paragraph; LOH and why 85,000 bytes matters; workstation vs server
  vs background GC mapped to real service shapes; finalizers vs `IDisposable` and why a
  finalizer *extends* an object's life; `ArrayPool`/pooling as the escape hatch. Symptoms
  section must cover: p99 latency spikes with a flat mean, rising gen2 counts, and the
  static-event-handler leak.
- **il-jit-codegen** — C# → IL → machine code, with the real IL shown; tiered compilation and
  what "warmed up" means; inlining; bounds-check elimination shown in the emitted code, with the
  idiomatic `for (int i = 0; i < arr.Length; i++)` the JIT recognises and the shapes that
  defeat it.

### concurrency

- **threads-and-scheduling** — OS thread vs thread-pool thread vs `Task`; what a `Task` is not
  (it is not a thread); the pool's queue, work-stealing, and its slow injection of new threads;
  blocking vs async at the OS level (a blocked thread is a parked stack, an awaited operation
  is a registered callback); the `async`/`await` state machine shown decompiled; sync context
  and `ConfigureAwait(false)`; **thread-pool starvation** as the flagship production incident.
- **memory-model** — the three separate guarantees (atomicity, visibility, ordering) that
  "thread-safe" mushes together; why a store may not be visible; store buffers; compiler *and*
  CPU reordering; what `volatile` does and does not do in C#; `Volatile.Read`/`Write`;
  acquire/release in plain language; x86's strong ordering vs ARM's weak ordering and why
  "it worked on my laptop" is a memory-model story; that `lock` gives you ordering for free,
  which is why most code never needs any of this.
- **atomics-and-cas** — why `count++` is three operations; `Interlocked` and what the CPU does
  to make it atomic (the cache-line lock, not a bus lock, on modern hardware); the CAS loop
  shape; `CompareExchange` in full; the ABA problem and why GC-managed references soften it;
  **false sharing** — two counters on one cache line, and the invalidation traffic that
  follows, shown through the layout rather than a stopwatch; when an atomic beats
  a lock and when contention makes it worse.
- **locks-internals** — what a mutex is physically made of: an atomic word, a wait queue, and a
  parking mechanism; the fast path (uncontended CAS, no kernel), the slow path (spin, then
  park via futex/`WaitOnAddress`); user↔kernel transition cost; `Monitor`/`lock` — the object
  header, thin locks, inflation to a sync block; `SpinLock`, `SemaphoreSlim`,
  `ReaderWriterLockSlim` and when each is right; lock granularity and striping; lock convoys;
  and what contention actually looks like in a thread dump or a profiler's blocked-time view. Build-a-spinlock exercise proves the fast path
  is just CAS.
- **concurrency-hazards** — data race vs race condition (not the same thing); check-then-act
  and read-modify-write as the two shapes almost every bug takes; the four Coffman conditions
  and that breaking any one is enough; lock ordering as the practical fix; livelock,
  starvation, priority inversion; reentrancy and why `lock` is reentrant in C# but a
  `SemaphoreSlim` is not; async deadlock via `.Result`; correct double-checked locking, and
  `Lazy<T>` as the answer you should actually reach for.
- **lock-free-structures** — what "lock-free" formally promises (system-wide progress) and
  what it does not (it is not "faster"); the CAS-loop skeleton; a Treiber stack built and
  verified; why the hard part is memory reclamation, and why .NET's GC removes the problem
  that forces hazard pointers and epochs in C++; `ConcurrentDictionary`'s striped locks —
  and its two real traps (`GetOrAdd`'s factory can run more than once; compound operations are
  not atomic); immutable/persistent structures and copy-on-write as the boring option that
  usually wins.
- **parallelism-patterns** — Amdahl's law with the arithmetic done; the Universal Scalability
  Law's second term (coherence) and why throughput can *fall* past a core count; partitioning
  as the real answer to contention; `Parallel.For` and its partitioner; work stealing;
  producer/consumer with `Channel<T>` and why backpressure is not optional; batching to amortize
  synchronization; and why a scaling curve bends — Amdahl's serial fraction first, then
  coherence traffic, then SMT siblings sharing one core's execution units. Reason it out; do
  not chart it.

## Cross-links you must make

These pages are worthless as islands. Every page links at least two others by real path
(trailing slash, resolvable in `curriculum.ts`). The spine the whole section hangs on:

`process-and-thread` (threads share the heap, not the stack) → `stack-and-heap` (so what is a
heap) → `memory-hierarchy` (so why is memory slow) → `cpu-pipeline` (so why does the CPU
reorder) → `memory-model` (so why can't I see your write) → `atomics-and-cas` (so how do we
make one thing indivisible) → `locks-internals` (so how is a lock built from that) →
`concurrency-hazards` (so how does it still go wrong).

Say those hand-offs out loud in the prose at the end of the relevant section. A reader should
never wonder why the next page exists.
