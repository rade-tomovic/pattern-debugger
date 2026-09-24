# algo-ds-learning — Content Tree & Build Plan

A static learning site for coding-interview algorithm patterns. All solution code in **modern C#**
(collection expressions, tuples, pattern matching), compile-verified with .NET 10.
Target coverage: the patterns behind ~90% of company interviews.

**Stack:** bun · Astro 7 · TypeScript 7 · Tailwind 4 · Preact islands (interactive visualizers only) · MDX.
**Deploy:** Cloudflare Pages (pure static output).
**Design:** the "algorithm pattern debugger" aesthetic — dark, mono `//`-prefixed headings,
amber/cyan/purple pointer badges, step-player visualizers with watch panels
(ported from `~/Downloads/algorithm-visualizer.html`).

The machine-readable source of truth is [`src/data/curriculum.ts`](src/data/curriculum.ts) —
nav, prev/next, cross-links, and listings are generated from it. This file is the human view.
Shaped by the three-lens curriculum audit of 2026-08-27 (frequency, pedagogy, IA) — see git
history for the findings.

## Content Tree

Problems marked `[stretch]` sit in the extended track / capstones, not the first pass.
Sections and order below match `src/data/curriculum.ts` exactly.

This plan was written for the pattern tiers, and those are the ones expanded below. Three tiers
were added later: `/systems/` (16 topics — how a computer runs code, memory, concurrency),
`/networking/` (9 topics, expanded below since it is the newest), and `/system-design/` (12
topics plus the design drills). `curriculum.ts` is the authority for all of them.

```
/
├── foundations/
│   ├── big-o                        Big-O & data structure costs (.NET collection cost table)
│   ├── csharp-toolkit               C# interview toolkit (Dictionary, PriorityQueue, spans, idioms)
│   ├── loops-and-counting           for/while, < vs <=, mid handling, ceil div, modulo, dir arrays,
│   │                                mirror iteration, off-by-one defense, 0-1-2-element debugging
│   └── sorting                      Sorting for interviews (mechanics, quickselect, counting/bucket)
│
├── patterns/                        ── CORE (teaching order) ──
│   ├── two-pointers                 ▸ valid-palindrome (125) ▸ two-sum-ii (167) ▸ remove-duplicates (26)
│   │                                ▸ longest-palindromic-substring (5) ▸ container-with-most-water (11)
│   │                                ▸ three-sum (15) ▸ trapping-rain-water (42) [stretch]
│   ├── hashmap                      ▸ two-sum (1) ▸ valid-anagram (242) ▸ group-anagrams (49)
│   │                                ▸ longest-consecutive-sequence (128) ▸ subarray-sum-equals-k (560)
│   │                                ▸ insert-delete-getrandom (380) [stretch]
│   ├── sliding-window                ▸ max-sum-subarray-of-size-k ▸ longest-substring-without-repeating (3)
│   │                                ▸ longest-repeating-character-replacement (424) ▸ minimum-window-substring (76)
│   ├── binary-search                ▸ classic-binary-search (704) ▸ first-and-last-position (34)
│   │                                ▸ find-minimum-rotated (153) ▸ search-rotated-array (33) ▸ koko-eating-bananas (875)
│   ├── bfs-dfs                      ▸ flood-fill (733) ▸ level-order-traversal (102) ▸ number-of-islands (200)
│   │                                ▸ rotting-oranges (994) ▸ clone-graph (133)
│   ├── tree-traversals              ▸ invert-binary-tree (226) ▸ maximum-depth (104)
│   │                                ▸ diameter-of-binary-tree (543) ▸ iterative-traversals (94/144/145)
│   │                                ▸ validate-bst (98) ▸ lowest-common-ancestor-bst (235)
│   ├── linked-lists                 ▸ middle-of-linked-list (876) ▸ linked-list-cycle (141)
│   │                                ▸ reverse-linked-list (206) ▸ merge-two-sorted-lists (21)
│   │                                ▸ add-two-numbers (2) ▸ remove-nth-from-end (19)
│   │                                ▸ palindrome-linked-list (234) ▸ lru-cache (146) [stretch]
│   ├── array-techniques             ▸ range-sum-query (303) ▸ move-zeroes (283) ▸ sort-colors (75)
│   │                                ▸ maximum-subarray (53) ▸ majority-element (169)
│   │                                ▸ product-except-self (238) ▸ rotate-image (48)
│   │                                ▸ spiral-matrix (54) [stretch]
│   │
│   │                                ── DATA STRUCTURE PATTERNS ──
│   ├── stack-queue                  ▸ valid-parentheses (20) ▸ implement-queue-using-stacks (232)
│   │                                ▸ min-stack (155) ▸ evaluate-rpn (150)
│   │                                ▸ daily-temperatures (739, monotonic stack)
│   │                                ▸ min-remove-parentheses (1249) [stretch]
│   ├── heap-top-k                   ▸ kth-largest-in-stream (703) ▸ kth-largest-element (215)
│   │                                ▸ top-k-frequent (347) ▸ merge-k-sorted-lists (23) [stretch]
│   │                                ▸ find-median-data-stream (295) [stretch]
│   ├── trie                         ▸ implement-trie (208) ▸ design-add-search-words (211)
│   │                                ▸ word-search-ii (212) [stretch]
│   │
│   │                                ── ADVANCED PATTERNS ──
│   ├── backtracking                 ▸ letter-combinations (17) ▸ subsets (78) ▸ permutations (46)
│   │                                ▸ combination-sum (39) ▸ generate-parentheses (22) ▸ word-search (79)
│   ├── dynamic-programming          ▸ climbing-stairs (70) ▸ house-robber (198) ▸ unique-paths (62)
│   │                                ▸ coin-change (322) ▸ word-break (139)
│   │                                ▸ longest-increasing-subsequence (300)
│   ├── advanced-graphs              ▸ course-schedule (207) ▸ course-schedule-ii (210)
│   │                                ▸ number-of-provinces (547, union-find)
│   │                                ▸ network-delay-time (743, dijkstra)
│   ├── greedy                       ▸ best-time-to-buy-sell-stock (121) ▸ jump-game (55)
│   │                                ▸ gas-station (134) ▸ partition-labels (763)
│   ├── intervals                    ▸ merge-intervals (56) ▸ insert-interval (57)
│   │                                ▸ non-overlapping-intervals (435) ▸ meeting-rooms-ii (253)
│   └── bit-manipulation             ▸ single-number (136) ▸ missing-number (268)
│                                    ▸ number-of-1-bits (191) ▸ counting-bits (338)
│
├── systems/                         ── 16 topics, see curriculum.ts ──
├── networking/                      ── 9 topics, see curriculum.ts ──
│   ├── network-basics               Packets, best-effort delivery, bandwidth vs latency, why layers
│   ├── osi-model                    The 7 layers, the 4 that run, and encapsulation byte by byte
│   ├── link-layer                   Ethernet frames, MAC, what a switch learns, ARP, MTU
│   ├── ip-and-routing               CIDR by hand, routing tables, the default gateway, NAT, TTL
│   ├── ports-and-sockets            A port is a 16-bit field; the four-tuple; TIME_WAIT
│   │   └── one-port-many-connections   Four-tuple demultiplexing, counted
│   ├── tcp-and-udp                  Handshake, sequence numbers, windows, the byte stream, UDP
│   │   └── stream-vs-datagram          Two writes, one read — and why framing is yours
│   ├── dns                          The resolution walk, records, TTLs, the HttpClient trap
│   ├── application-protocols        HTTP as text, status classes, framing, TLS in outline
│   └── network-troubleshooting      The ladder: name, route, port, TLS, payload
├── system-design/                   ── 12 topics + drills, see curriculum.ts ──
│
├── deep-dives/
│   ├── two-sum-family               Two Pointers vs HashMap: space, sorting-destroys-indices, "key = what I need"
│   └── pattern-recognition          Master "see X → think Y" decision table, all 17 pattern topics
│
├── reference/
│   ├── study-plan                   five independent tracks: patterns (2h/day), systems, network, design
│   └── cheat-sheet                  All pattern cards on one page
│
└── /                                Landing: the pattern map (this tree, visual), study plan CTA
```

Note: `advanced-graphs` (topic slug and directory `patterns/advanced-graphs`) is the renamed
"graphs" topic — Topo Sort, Union-Find, Dijkstra beyond plain traversal.

**Page anatomy — topic page:** core idea (2-3 sentences) → when to reach for it →
universal C# template(s) → problem list → cheat sheet card (recognition signals, key tricks,
common bugs) → connections panel (cross-links with the *reason* for each link).

**Page anatomy — problem page:** task statement → "how to think" (reasoning before code) →
**template instance callout (REQUIRED)** — right after "how to think", names which topic-page
skeleton this problem instantiates, its invariant, and what varies → full C# solution →
step-by-step trace (table; every pointer/dict/window state per iteration) → why it works
(ends with a required time/space `Watch`) → common bugs → variants you can now solve →
connections.

## Interactive Visualizers (Preact islands, hybrid strategy)

Static trace tables everywhere; step-players (ported from the artifact's `Player` model:
steps + note + watch vars + ◀ ▶ keys) only where motion teaches:

| id | used on |
|---|---|
| `two-pointers-palindrome` | valid-palindrome |
| `two-pointers-sum` | two-sum-ii |
| `hashmap-two-sum` | two-sum |
| `sliding-window-unique` | longest-substring-without-repeating |
| `binary-search-classic` | classic-binary-search |
| `binary-search-boundary` | first-and-last-position |
| `bfs-islands` | number-of-islands |
| `fast-slow-middle` | middle-of-linked-list |
| `reverse-list` | reverse-linked-list |
| `dutch-flag` | sort-colors |
| `kadane` | maximum-subarray |
| `monotonic-stack` | daily-temperatures |
| `topo-sort` | course-schedule-ii |

## Build Phases

1. ✅ Scaffold: bun + Astro 7 + TS 7 + Tailwind 4 + Preact + MDX, git init
2. ✅ Content tree (this file + curriculum.ts) → multi-lens critique for coverage/ordering
   (three-lens audit: new problems, reorders, core/stretch tracks, connection reasons,
   advanced-graphs rename, study plan data)
3. ✅ Design system: port artifact tokens to Tailwind theme; layout, nav, MDX component kit
   (Callout, TraceTable, CheatSheet, Connections, ComplexityBadge); exemplar pages
4. 🚧 Content fan-out: topic + problem MDX written per topic, in parallel; every C# snippet
   compiled and behavior-asserted with .NET 10; traces verified step-by-step; links
   validated against curriculum.ts (see `docs/content-brief.md` for the exact per-page
   contract, including the required template-instance callout)
5. 🚧 Visualizer islands: shared step-player + per-pattern step builders
6. Landing page, study plan, cheat sheet, pattern-recognition table
7. `astro build` clean + link check + Lighthouse pass; ready for Cloudflare Pages
</content>
