# Content Brief — how every page gets written

Read this fully before writing any page. Then read, in order:
1. `docs/teaching-request.md` — the user's requirements (voice, structure, non-negotiables)
2. `src/data/curriculum.ts` — the content tree; the ONLY valid link targets
3. `src/pages/patterns/two-pointers/index.mdx` — the topic-page exemplar
4. `src/pages/patterns/two-pointers/two-sum-ii.mdx` — the problem-page exemplar

The exemplars are the standard. Match their tone, density, heading style, and component usage
exactly. Your pages should be indistinguishable from them in voice.

## The reader

Senior .NET engineer, 11 years of experience. Talk to them like a peer: direct, no filler, no
"in this article we will". They know C# deeply — what they need is pattern recognition and
the reasoning that makes solutions fall out. Skip theory proofs unless the "why it works" is
genuinely non-obvious (then give the real argument, tight).

## File locations

- Pattern topics: `src/pages/patterns/<topic-slug>/index.mdx`, problems at
  `src/pages/patterns/<topic-slug>/<problem-slug>.mdx`
- Foundations: `src/pages/foundations/<topic-slug>.mdx`
- Deep dives: `src/pages/deep-dives/<topic-slug>.mdx`

NEVER touch `src/data/curriculum.ts`, layouts, components, or global.css. Only create your
assigned MDX files.

## Frontmatter contracts (exact)

Topic page:
```
---
layout: '@layouts/TopicLayout.astro'
topic: <topic-slug>
---
```

Problem page:
```
---
layout: '@layouts/ProblemLayout.astro'
topic: <topic-slug>
problem: <problem-slug>
---
```

Title, summary, breadcrumb, difficulty badges, related-problems panel, cheat-sheet card,
connections panel, and prev/next are ALL auto-generated from curriculum.ts. Never write them
in MDX. No `# h1` headings ever — start at `##`.

## Page anatomy

**Topic page** (sections in this order, lowercase headings):
1. `## core idea` — 2-3 sentences + a comparison table if the pattern has sub-shapes
2. `## when to reach for it` — recognition signals as a bullet list
3. `## universal template` (or `## universal templates`) — skeleton C# with WHY-comments;
   one skeleton per sub-shape. Everything below must visibly be an instance of these.
4. `## problems` — one intro line, then `<ProblemList topic="<slug>" />`
5. Optional: ONE extra section if the topic truly needs it (e.g. binary search's two loop
   templates side by side). Not more.

**Problem page** (sections in this order):
1. `## task` — the problem statement, clear and complete, with a one-line example in a
   ```text fence
2. `## how to think` — the reasoning BEFORE code: what signals point to the pattern, what
   the brute force costs, what insight collapses it. 2-4 paragraphs.
3. Template-instance callout (REQUIRED, right after "how to think"):
   `<Callout kind="note" label="template instance">` naming which skeleton this instantiates,
   the invariant, and what varies.
4. `## solution` — the solution code wrapped in `<Solution>` (import it), one fenced
   ```csharp block inside, complete and runnable as a method/class, with a BLANK LINE between
   the tags and the fence. Inline comments explain WHY, not what. When more languages are
   registered in `src/data/languages.ts`, additional fences (one per language) go inside the
   same `<Solution>` block and render as tabs.
5. `## trace` — a full step-by-step table for a concrete input. EVERY iteration gets a row —
   no "..." elisions. Columns show pointer positions / dict contents / stack state / window,
   whatever the state is. For array problems, add 1-3 `<Cells>` snapshots of key moments.
   For trees/graphs/lists, a small ASCII diagram in a ```text fence plus the table.
6. `## why it works` — the key insight/invariant argument. End with
   `<Watch items={[['time', 'O(…)'], ['space', 'O(…)']]} />` (REQUIRED).
7. `## common bugs` — 3-5 bullets of the mistakes people actually make (be specific to the
   problem, not generic).
8. `## variants you can now solve` — 2-4 bullets. Variants that have pages in curriculum.ts
   get markdown links; variants without pages get **bold name** + LC number, no link.

## Components (import only what you use)

```
import Callout from '@components/Callout.astro';      // kinds: insight|tip|trap|note, label="..."
import Watch from '@components/Watch.astro';          // items={[['k','v'],...]}
import Cells from '@components/Cells.astro';          // see below
import ProblemList from '@components/ProblemList.astro'; // topic pages only
import Solution from '@components/Solution.astro';    // wraps the solution fence(s) — language tabs
```

`<Cells>` — static array snapshot (the artifact's cell visualization):
```
<Cells
  data={[1, 3, 5, 7, 11]}
  pointers={{ 0: [{ t: 'L', c: 'amber' }], 4: [{ t: 'R', c: 'cyan' }] }}
  cells={{ 4: 'warn', 1: 'ok' }}
  caption="what this moment shows"
/>
```
Pointer color semantics (match the artifact everywhere): **amber** = L / slow / i / write ·
**cyan** = R / fast / read · **purple** = mid · **green** = answer/success · **red** = reject.
Cell states: `ok` (green, the answer) · `win` (in-window/kept) · `warn` (rejected/discarded)
· `mid` (purple) · `dim` (out of search space).

## MDX safety (build breaks if you violate this)

- NEVER write a raw `<` or `{` in prose or tables. `Dictionary<char, int>`, `i < n`,
  `len - maxFreq <= k` MUST be inside backticks or code fences. MDX parses raw `<`/`{` as JSX
  and the build fails.
- Component props use JSX braces as shown above — that's fine.
- Em dashes, apostrophes, unicode arrows (→ ⇒ ½ ×) in prose are fine.

## Links

- Internal links ONLY to paths derivable from curriculum.ts:
  `/patterns/<topic>/`, `/patterns/<topic>/<problem>/`, `/foundations/<slug>/`,
  `/deep-dives/<slug>/`, `/reference/<slug>/` — always with trailing slash.
- Every problem page should link at least one other page inline where it genuinely helps.
- No external links (no leetcode.com — LC numbers are plain text).

## C# rules

- Modern C#: collection expressions `[1, 2]`, list patterns `is [x, ..]`, tuples,
  pattern matching, `var` where obvious, target-typed `new()`.
- Interview-idiomatic: explicit loops over LINQ when LINQ would hide the complexity story;
  mention the LINQ one-liner in prose when it's what you'd write on the job.
- Shared types, declared exactly like this wherever needed (in the solution block, below the
  method, with a comment `// standard interview definition`):
  `public class ListNode(int val = 0, ListNode? next = null) { public int Val = val; public ListNode? Next = next; }`
  `public class TreeNode(int val = 0, TreeNode? left = null, TreeNode? right = null) { public int Val = val; public TreeNode? Left = left; public TreeNode? Right = right; }`
- Nullable-aware (`ListNode?`), no `!` suppressions unless truly warranted.

## Verification protocol (MANDATORY before a snippet goes into a page)

Every ```csharp block that contains runnable logic must be compile-verified and
behavior-verified with .NET 10 single-file execution:

```bash
DIR=$(mktemp -d) && cat > $DIR/check.cs <<'EOF'
// paste solution + a main section calling it with 3+ cases:
// the page's trace example, an edge case (empty/single), and a normal case.
// throw new Exception("FAIL ...") on mismatch; print PASS at the end.
EOF
dotnet run $DIR/check.cs
```

Template skeletons with placeholders (e.g. `Keep(...)`) are exempt from behavior checks but
must still compile with a trivial stand-in. The trace table in the page MUST match what the
verified code actually does — derive the table by hand-executing the verified code, step by
step. If your trace and the code disagree, your page is wrong.

## Trace discipline (the #1 quality bar)

The step-by-step trace is the product. It must be *exact*: real pointer values, real dict
contents (`{5→0, 7→1}`), real stack/queue snapshots per iteration, matching the verified
code's actual execution on the stated input. Choose inputs big enough to be interesting
(5-9 elements), small enough to trace fully. Show the "aha" moment (a shrink, a swap, a
backtrack) explicitly.

## Topic-specific mandates (from the curriculum audit — non-negotiable)

- **two-pointers**: index.mdx and two-sum-ii.mdx already exist — do NOT rewrite them. Write the
  remaining problem pages. On longest-palindromic-substring: LC 647 as count-variant note. On
  remove-duplicates: cross-link move-zeroes as the recall problem.
- **hashmap**: two-sum links prominently to /deep-dives/two-sum-family/. subarray-sum-equals-k
  must be self-contained: teach the running-total idea in "how to think" and forward-link
  range-sum-query as the formal prefix-sum home. Topic page: mention Contains Duplicate (217)
  as the trivial warm-up in one line. valid-anagram: `int[26]` beats Dictionary note.
- **sliding-window**: topic page shows BOTH templates (fixed via add/remove, variable via
  expand/shrink) and states the two shrink disciplines: longest→shrink while INVALID,
  shortest→shrink while VALID. longest-substring page: mention the char→lastIndex jump variant.
- **binary-search**: topic page MUST present the two loop templates side by side —
  `while (L <= R)` exact-match vs `while (L < R)` boundary — and when each applies.
  find-minimum-rotated comes before search-rotated (already ordered in curriculum). On
  first-and-last-position: First Bad Version (278) variant note. Common bugs must include the
  `L = mid` infinite loop.
- **bfs-dfs**: level-order-traversal defines TreeNode inline (first tree the reader meets) and
  covers Right Side View (199) as a named variant section. number-of-islands: mark-visited
  WHEN ENQUEUEING trap front and center. rotting-oranges: multi-source + minutes counting.
  Topic page mentions Pacific Atlantic (417) and Max Area of Island (695) as variants.
- **tree-traversals**: the topic page itself teaches the three recursive traversals (one
  function, the visit line moves — show all three in one block) as template material.
  iterative-traversals covers inorder (the important one) + preorder + postorder-as-reversed-
  preorder, with Kth Smallest in BST (230) as the payoff variant note. diameter: mention
  Binary Tree Maximum Path Sum (124) as the hard sibling and Balanced (110)/Same Tree (100) as
  freebies. lowest-common-ancestor-bst: LC 236 (general tree) variant explained briefly.
- **linked-lists**: reverse-linked-list: the four-line mantra + Reverse in k-Groups (25) and
  Reverse Between (92) as variant notes. linked-list-cycle: include WHY fast must catch slow +
  Cycle II (142) cycle-start variant. palindrome-linked-list: Reorder List (143) variant note.
  lru-cache: show BOTH the hand-rolled doubly-linked list and the `LinkedList<T>`/
  `LinkedListNode<T>` version — the .NET built-in is the idiomatic win, interviewers may demand
  the hand-rolled one.
- **array-techniques**: range-sum-query is the formal prefix-sum unit (backlink
  subarray-sum-equals-k). move-zeroes framed as recall of remove-duplicates (spaced
  repetition, say so). maximum-subarray: Kadane IS DP — say it, link dynamic-programming;
  Max Product Subarray (152) variant note + Buy-Sell Stock correspondence. sort-colors: why
  mid does NOT advance after swapping with high. spiral-matrix: Set Matrix Zeroes (73) note.
- **stack-queue**: topic page notes queue mechanics live in BFS (link) — this topic is
  stack-heavy on purpose. implement-queue-using-stacks: amortized O(1) argument.
  daily-temperatures: monotonic stack in full; variants Next Greater Element (496),
  Asteroid Collision (735), Largest Rectangle (84) as notes.
- **heap-top-k**: topic page covers PriorityQueue API: comparer-based max-heap, NO DecreaseKey
  (lazy deletion pointer to network-delay-time). kth-largest-element: quickselect alternative +
  K Closest Points (973) as same-lesson variant. top-k-frequent: bucket-sort O(n) alternative +
  Task Scheduler (621) note. find-median: two-heaps balance invariant.
- **trie**: children as `Dictionary<char, TrieNode>` vs `TrieNode?[26]` tradeoff on topic page.
  word-search-ii: prerequisite callout (Callout kind="trap") pointing to
  /patterns/backtracking/word-search/ FIRST; teach trie-pruning (remove word on match).
- **backtracking**: topic page has THE template (choose → explore → un-choose) with the
  decision-tree picture in a text fence. letter-combinations is the gentle opener. subsets:
  include/skip framing. combination-sum: reuse = stay on same index; Combination Sum II (40)
  dupes variant note. permutations: Permutations II (47) note. subsets: Subsets II (90) note.
  generate-parentheses: constrained branching (open < n, close < open).
- **dynamic-programming**: topic page teaches the method: state → recurrence → base → order,
  memoization vs tabulation, and "DP = backtracking + overlapping subproblems". climbing-stairs:
  Min Cost Climbing (746) note. house-robber: House Robber II (213) circular trick note.
  unique-paths: 2D table → 1D rolling compression shown. LIS: O(n²) main, patience-sorting
  O(n log n) as clearly-optional coda. Topic cheat mentions LCS (1143) and Edit Distance as
  the 2D-string family.
- **advanced-graphs**: course-schedule: Kahn's BFS primary, DFS-coloring alternative sketched.
  network-delay-time: MUST teach the lazy-deletion idiom (enqueue duplicates, skip stale pops
  with a dist check) — this is the .NET-specific trap. number-of-provinces: union-find with
  path compression + union by size; mention Redundant Connection (684).
- **greedy**: topic page: what makes greedy VALID (never-regret/exchange argument, lightly).
  best-time-to-buy-sell-stock: REQUIRED callout on the Kadane correspondence (link
  maximum-subarray). jump-game: greedy frontier AND the DP fallback comparison; Jump Game II
  (45) note. gas-station: the restart-past-failure proof.
- **intervals**: topic page spine: sort by START to merge, sort by END to select — make the
  distinction the core idea. non-overlapping-intervals: the sort-by-end proof; Min Arrows (452)
  note. meeting-rooms-ii: heap approach primary, chronological start/end events alternative;
  Meeting Rooms I (252) one-line mention.
- **bit-manipulation**: topic page: identities table (`x^x=0`, `x^0=x`, `n&(n-1)`, `n&-n`,
  shifts, mask building). missing-number: also the Gauss-sum alternative. counting-bits:
  the `i >> 1` recurrence; Reverse Bits (190) + Sum of Two Integers (371) notes.
- **foundations/big-o**: growth-rate table; .NET collection cost table (List indexer/Add
  amortized, Dictionary avg/worst, HashSet, SortedDictionary, LinkedList, PriorityQueue,
  Stack/Queue); amortized analysis via List growth; the string-concat-in-a-loop trap
  (StringBuilder); how to state complexity out loud in an interview.
- **foundations/csharp-toolkit**: the collections above with the exact members that matter
  (`TryGetValue`, `GetValueOrDefault`, `TryPop/TryPeek`, PriorityQueue comparers,
  `LinkedList<T>`+`LinkedListNode<T>`); tuples/deconstruction; pattern matching incl. list
  patterns; collection expressions; index/range (`s[^1]`, `s[1..]`); char arithmetic +
  `int[26]`; overflow and `long`; a short "implementation strings" aside on approaching
  Roman-to-Integer (13) / Longest Common Prefix (14) / atoi (8) style questions.
- **foundations/loops-and-counting**: cover the FULL list from teaching-request.md: for vs
  while; `<` vs `<=` per context (arrays, two pointers, binary search); middle element —
  odd/even, left-mid `(L+R)/2` vs right-mid `(L+R+1)/2`, when the middle is skipped naturally;
  ceiling division `(a + b - 1) / b`; circular indexing incl. C#'s negative `%` and the
  `((a % m) + m) % m` idiom; direction arrays; mirror iteration `i` vs `n-1-i`; the three bug
  classes (off-by-one, binary-search infinite loop `L = mid`, negative modulo); the
  trace-with-0-1-2-elements debugging technique. Each trick: tiny C# example + when it bites.
- **foundations/sorting**: comparison floor; merge sort mechanics (the merge step IS
  merge-two-sorted-lists — link it); quicksort → quickselect (link kth-largest-element);
  counting/bucket (link top-k-frequent); stability; `Array.Sort` (introsort, unstable) vs
  `OrderBy` (stable); custom comparers `(a, b) => ...` examples; "sort first" as a strategy.
- **deep-dives/two-sum-family**: the full tradeoff per the teaching request: Two Pointers
  (sorted, O(1) space) vs HashMap (unsorted, O(n) space); when sorting destroys information
  (original indices); the "key = what I need" framing; a decision table; the family tree
  167 → 1 → 15 → 560 with links; what to say when the interviewer asks "what if it's sorted?"
- **deep-dives/pattern-recognition**: the master table: signal phrase → pattern (linked) →
  first question to ask yourself → canonical problem (linked). Cover ALL 17 pattern topics.
  Then a keyword table (statement words → likely pattern). Then classify 2-3 unseen problems
  as worked examples of using the table.

## Cheat-card data (returned, not written into MDX)

For each pattern/foundations topic you write, return a cheat object:
`{ signals: string[], tricks: string[], bugs: string[] }` — 3-5 items each, backticks for
code spans. Same voice as the two-pointers entry in `src/data/cheats.ts`. These render on the
topic page and the master cheat sheet — do NOT also write them into the MDX.
