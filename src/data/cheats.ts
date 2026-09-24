/**
 * Cheat-sheet card content per topic slug. Rendered in three places from this
 * single source: the topic page's card, /reference/cheat-sheet, and the
 * pattern-recognition table. Strings support `backtick` spans, rendered as
 * inline code by <CheatCard>.
 *
 * Populated during the content fan-out; every pattern topic must have an entry
 * by the time the master cheat sheet ships.
 */

export interface Cheat {
  /** "you are probably looking at this pattern when…" */
  signals: string[];
  /** the moves that make solutions fall out */
  tricks: string[];
  /** the bugs everyone writes at least once */
  bugs: string[];
}

export const cheats: Record<string, Cheat> = {
  'big-o': {
    signals: [
      'interviewer states or implies a bound on `n` ("n <= 10^5") — that bound IS the complexity budget, read it before coding',
      'you\'re about to write `result += item` inside a loop building a string — stop, that\'s `O(n^2)`',
      'two solutions tie on time — the tiebreaker is space, or the constant factor from calling something in a loop',
      'asked to justify a Dictionary/HashSet claim of `O(1)` — the honest answer is "average case, `O(n)` worst on collisions"',
    ],
    tricks: [
      'read `n`\'s bound to pick the target complexity: `n <= 20` → `O(2^n)` intended, `n <= 5,000` → `O(n^2)` fine, `n <= 10^6` → need `O(n log n)`/`O(n)`',
      'amortized `O(1)` comes from growth *proportional* to size (doubling) — `List<T>.Add`, `Stack<T>.Push`, `Queue<T>.Enqueue`, and `StringBuilder.Append` all use this trick',
      'always state time AND space, unprompted, and name the dominant term only — `O(n)`, never `O(2n + log n)`',
      '`LinkedList<T>` is `O(1)` insert/delete only once you\'re already holding the `LinkedListNode<T>` — finding that node first is `O(n)`',
      '`PriorityQueue<TElement,TPriority>` has no `DecreaseKey` — Dijkstra-style algorithms work around it with lazy deletion (see network-delay-time)',
    ],
    bugs: [
      'building a string with `+=` in a loop instead of `StringBuilder` — silently turns `O(n)` into `O(n^2)`',
      'quoting `Dictionary`/`HashSet` operations as flat `O(1)` without the average-case caveat',
      'assuming `List<T>.Insert(0, x)` is cheap like `Add` — inserting at the front is `O(n)`, it shifts everything after it',
      'treating two `O(n)` solutions as equivalent when one calls a `Contains` on a `List<T>` (another hidden `O(n)`) inside the loop, making it actually `O(n^2)`',
      'forgetting that `Dictionary`/`HashSet` worst case is `O(n)` under adversarial collisions — rare in interviews, but the honest caveat to state out loud',
    ],
  },
  'csharp-toolkit': {
    signals: [
      'choosing a collection under time pressure, not sure which member avoids a double lookup or an exception',
      '`ContainsKey` immediately followed by the indexer, or `Contains` immediately followed by `Add`/`Remove` → collapse into `TryGetValue`/`Add`\'s bool return',
      'need a max-heap and only `PriorityQueue<TElement, TPriority>` (min-heap) is in scope',
      'building a string across a loop, or concatenating in a loop at all',
      'fixed small alphabet (`a`-`z`) doing frequency counts → `int[26]` beats `Dictionary<char, int>`',
    ],
    tricks: [
      '`TryGetValue`/`GetValueOrDefault` fold check-and-fetch into one hash, one call, no branch',
      '`HashSet<T>.Add` and `Dictionary` indexer assignment double as the membership check — no separate `Contains`/`ContainsKey` needed',
      '`TryPop`/`TryPeek`/`TryDequeue` return `false` on empty instead of throwing — no `Count > 0` guard to remember',
      'max-heap = min-heap `PriorityQueue<TElement, TPriority>` + `Comparer<T>.Create((a, b) => b.CompareTo(a))`',
      '`LinkedListNode<T>` held from `AddFirst`/`AddLast` makes `Remove(node)` + re-insert `O(1)` — no search',
    ],
    bugs: [
      '`d.ContainsKey(k)` then `d[k]` — two hashes where `TryGetValue` needs one',
      '`result += item` inside a loop — `O(n)` reads as `O(n^2)` because every `string` concat reallocates and copies',
      'assuming `PriorityQueue<TElement, TPriority>` is a max-heap by default — it\'s min-heap; forgetting to flip the comparer',
      'summing an `int[]` into an `int` accumulator and getting a silently wrapped (possibly negative) total instead of a compile error',
      'reaching for `Dictionary<char, int>` on a fixed lowercase alphabet when `int[26]` is faster and simpler',
    ],
  },
  'loops-and-counting': {
    signals: [
      'writing a loop whose exit condition can only be known from inside it → `while`, not `for`',
      'two indices about to converge, chase, or wrap → decide `<` vs `<=` before the body, not after',
      'computing an index from a formula (`mid`, `n - 1 - i`, `(i + 1) % n`) → plug in the smallest and largest legal input by hand first',
      'a grid problem says "check the neighbors" → direction arrays, not four copy-pasted `if` blocks',
      'about to submit → trace on 0, 1, and 2 elements before trusting anything bigger',
    ],
    tricks: [
      '`(a + b - 1) / b` — integer ceiling division, no `Math.Ceiling(double)` precision risk',
      '`((a % m) + m) % m` — forces a C# `%` result into `[0, m)`; C#\'s `%` is remainder, sign follows the dividend, not modulo',
      'if a branch ever sets `L = mid`, use ceil-mid `(L + R + 1) / 2` or `L` can stall forever when `R == L + 1`',
      '`dRow`/`dCol` parallel arrays replace four copy-pasted neighbor `if` blocks in every grid problem',
      'mirror iteration: one index `i` plus `n - 1 - i` compares from both ends without a second variable',
    ],
    bugs: [
      '`for (int i = 0; i <= arr.Length; i++)` — throws `IndexOutOfRangeException` on the last iteration; should be `i < arr.Length`',
      '`while (L < R) { mid = (L+R)/2; if (...) L = mid; ... }` — floor-mid with an `L = mid` branch infinite-loops the moment `R == L + 1`',
      'raw `a % m` used as an array index when `a` can be negative — C# hands back a negative remainder, not a wrapped index',
      '`while (left <= right)` on a converging two-pointer loop — at `left == right` you compare a single element against itself',
      'trusting a loop that "obviously" works on a big array without tracing the 0- and 1-element cases by hand first',
    ],
  },
  'sorting': {
    signals: [
      'problem needs sorted input for another pattern (two pointers, binary search) → sort first, `O(n log n)` up front',
      'need one order statistic (kth largest/smallest, median, top-K) not a full order → quickselect over full sort',
      '"sort by start" / "sort by end" appears anywhere → intervals, and the choice of key is the design decision',
      'keys are bounded integers or a small alphabet (frequencies, scores, bytes) → counting/bucket sort beats the comparison floor',
      'grouping/dedup by identity (anagrams) → sort each key\'s characters and use the result as a hashmap key',
    ],
    tricks: [
      'comparison floor: `n!` orderings, 2-way branching per comparison ⇒ no comparison sort beats `O(n log n)` worst case',
      'quickselect reuses `Partition` from quicksort but recurses into only the half containing `targetIndex = n - k` → `O(n)` average',
      'the merge step in merge sort IS `merge-two-sorted-lists` without the dummy head — write straight into `arr[k]`',
      'bucket-by-frequency (bucket index = count, `0..n`) turns top-K frequent into one `O(n)` pass instead of a heap or a sort',
      '`Comparer<T>.Create((a, b) => ...)` when you need an `IComparer<T>` to hand off, not just a one-off `Array.Sort` lambda',
    ],
    bugs: [
      'assuming `Array.Sort` / `List<T>.Sort` is stable — introsort is explicitly NOT guaranteed stable; use `OrderBy`/`ThenBy` when tie order matters',
      'sorting by the wrong key for intervals — merge/select problems need sort-by-start, elimination problems often need sort-by-end (`non-overlapping-intervals`)',
      'quickselect off-by-one on `targetIndex = arr.Length - k` — kth *largest* is the `(n-k)`th smallest, 0-indexed',
      'picking counting sort when the key range `k` dominates `n` — `O(n + k)` becomes worse than `O(n log n)` for a huge sparse range',
      'forgetting `Partition`\'s Lomuto pivot must be reset from `arr[hi]` each call — reusing a stale pivot value silently corrupts the partition',
    ],
  },
  'two-pointers': {
    signals: [
      'sorted array + pair/sum/closest question → opposite ends',
      '"remove/keep elements in place", "O(1) space" → read/write pointers',
      'palindrome or mirror comparison → both ends inward, or expand from center',
    ],
    tricks: [
      '`while (left < right)` strict — meeting means nothing left to compare',
      'read/write invariant: `[0, write)` is always the finished answer',
      '3Sum = sort + fix one element + two-pointer the rest',
      'skip duplicates by sliding a pointer while `nums[i] == nums[i - 1]`',
      'expand-from-center: try both odd (i, i) and even (i, i + 1) centers',
    ],
    bugs: [
      '`<=` in the converge loop — comparing an element with itself',
      'moving the wrong pointer on a tie (Container With Most Water: move the shorter wall)',
      'forgetting the array must be sorted for opposite-end sums',
      'returning values when the problem wants original indices — sorting destroyed them',
    ],
  },
  'hashmap': {
    signals: [
      '"have I seen this before?" / "how many times has X occurred?" -> `HashSet<T>` membership or `Dictionary<T,int>` frequency count',
      'unsorted array + pair/sum question, or sorting would destroy something you need (original indices) -> value->index dictionary',
      '"how many subarrays / sub-ranges sum to k" -> prefixSum->count dictionary',
      'grouping or bucketing items by a derived property ("same letters", "same remainder") -> canonical-key grouping',
    ],
    tricks: [
      '"key = what I need": ask what you wish you already knew about the elements you\'ve passed - that\'s the dictionary key',
      'seed `prefixCount[0] = 1` before scanning subarray-sum problems - covers a run that starts at index 0',
      '`int[26]` beats `Dictionary<char, int>` when the key space is small and fixed (lowercase letters, digits)',
      'check the complement/target BEFORE inserting the current element into the map, so nothing ever pairs with itself',
      'swap-with-last (`values[idx] = values[^1]`, then drop the tail) turns O(n) arbitrary removal into O(1)',
    ],
    bugs: [
      'inserting into the dictionary before checking for the complement - lets an element pair with itself',
      'forgetting the `prefixCount[0] = 1` seed - silently undercounts subarrays that start at index `0`',
      'sorting an array to "make it easier" when the problem needs original indices - sorting is exactly what destroys them',
      '`dict[key]` throws `KeyNotFoundException` on a miss - reach for `TryGetValue`/`GetValueOrDefault` instead of guarding manually',
    ],
  },
  'sliding-window': {
    signals: [
      '"contiguous subarray/substring" — not any subsequence, a **contiguous** run',
      '"longest/shortest ... such that" or "minimum window containing" over a run',
      '"size k" fixed-length subarray/substring',
      '"at most/exactly k distinct" or "k replacements" — a count-threshold validity rule',
      'brute force would recompute a sum/set/count from scratch per start index — O(n·k) or worse',
    ],
    tricks: [
      'fixed window: seed once, then `windowSum += nums[right] - nums[left]` per slide — O(1), never resum',
      'longest → shrink while `!IsValid()` (hold the window open as long as legal)',
      'shortest → shrink while `IsValid()`, recording the length on every step down before it breaks',
      '`freq[c - \'A\']` int[26] beats `Dictionary<char,int>` when the alphabet is fixed and small',
      'need/have counting: bump `have` only when a char count *reaches* its quota, drop it only when a count falls *below* quota — not on every +/-1',
    ],
    bugs: [
      'using `if` instead of `while` around the shrink when a single removal isn\'t guaranteed to fix validity',
      'off-by-one on window length: it\'s `right - left + 1`, not `right - left`',
      'resetting `left` to 0 on invalidity instead of continuing to advance it — kills the O(n) bound',
      'recording the best length after shrinking instead of before, in shortest-window problems — measures the wrong window',
      'confusing a fixed-k problem (no shrink loop needed) with a variable-window one (needs the while loop)',
    ],
  },
  'binary-search': {
    signals: [
      '`"sorted array"` + find a value/index → exact match',
      '"first/last occurrence", "insertion point" → boundary form, `while (left < right)`',
      '"minimize/maximize X such that ..." with a cheap feasibility check → binary search the answer',
      'rotated sorted array → still binary search, just a smarter discard rule (find the sorted half first)',
    ],
    tricks: [
      'overflow-safe midpoint: `left + (right - left) / 2`, never `(left + right) / 2`',
      'boundary form: `right = mid` keeps mid in range, `left = mid + 1` discards it — never mix them up',
      'rotated array: compare `nums[mid]` to an endpoint (`nums[right]` or `nums[left]`) to find which half is actually sorted',
      'binary search the answer: `left`/`right` span candidate answers, not array indices; a monotonic `CanDo(mid)` replaces the array comparison',
      'on a match with duplicates, don\'t return immediately — record it and keep narrowing toward the boundary you actually want',
    ],
    bugs: [
      '`left = mid` in the boundary form when `right - left == 1` — mid floors to `left`, so it never advances: infinite loop',
      '`(left + right) / 2` instead of the overflow-safe form',
      '`while (left < right)` when the last remaining element still needs checking (should be `<=`), or vice versa',
      'ceiling division written as `pile / speed` instead of `(pile + speed - 1) / speed` in "search the answer" problems',
      'assuming the input must be sorted — the real precondition is a monotonic predicate, which sorted order is only one example of',
    ],
  },
  'bfs-dfs': {
    signals: [
      'grid/graph + "shortest path", "fewest steps", "minimum moves" on unweighted edges → BFS',
      'grid/graph + "reachable", "connected", "does a path exist" → DFS (or BFS, take your pick)',
      'several starting points act at once (rot, fire, gates, infection) → multi-source BFS',
      'a tree, and the question is level-by-level or "rightmost/leftmost per level" → BFS with the `queue.Count` snapshot',
      'a graph with cycles that needs copying/transforming → DFS plus a `Dictionary` from original to result',
    ],
    tricks: [
      '`int size = queue.Count;` before the inner loop — the level-boundary snapshot every BFS variant needs',
      'direction arrays (`dRow`/`dCol`) replace four copy-pasted bounds-check blocks',
      'multi-source BFS = seed the queue with every source before round one, then it\'s the same loop',
      'mark visited (or rotten, or cloned) WHEN YOU ENQUEUE, never on dequeue — otherwise duplicates flood the queue',
      'a `Dictionary<Node, Node>` doubles as both the visited set and the answer when cloning a graph',
    ],
    bugs: [
      '`visited[nr, nc] = true` on dequeue instead of enqueue — still correct, but duplicate work explodes',
      'recursing before registering a node as visited/cloned — infinite loop the moment there\'s a cycle',
      'reading `queue.Count` inside the inner loop instead of snapshotting it first — levels bleed together',
      'forgetting a `newColor == startColor` (or equivalent) no-op guard — relabeling loops forever on a same-value cycle',
      'returning a round counter without checking the goal was actually reached (`fresh == 0`, etc.) — silently wrong on the unreachable case',
    ],
  },
  'tree-traversals': {
    signals: [
      'Problem hands you a `TreeNode` (not a graph/grid) and talks about parent/child structure directly',
      '"Every node\'s answer depends on its children" (height, sum, balanced-check) -> postorder',
      '"Give me the values in sorted order" on a tree the problem calls or implies is a BST -> inorder',
      '"Do this without recursion" / recursion-depth or stack-overflow worry -> same traversal with an explicit `Stack<TreeNode>`',
      'Two node references + a BST -> walk from the root, let ordering pick the direction, no need to search both sides',
    ],
    tricks: [
      'One recursive skeleton, three traversals: only the position of the `output.Add(node.Val)` line changes (before, between, after the two recursive calls)',
      'Postorder = "children answer first, parent combines" — whenever a node\'s return value needs `left` AND `right` already computed, you\'re doing postorder even if you didn\'t plan it',
      'Diameter-style problems: check-and-update a running best at EVERY node (`left + right`), but still `return` a smaller number (`1 + max(left, right)`) up to the parent — two different questions, one walk',
      'BST bounds-threading: pass `(lower, upper)` DOWN through the recursion instead of comparing only to the immediate parent — `node.Val <= lower || node.Val >= upper` catches violations from any ancestor, not just the direct one',
      'Postorder iteratively = preorder with push order flipped (`left` before `right`), producing `node,right,left`, then `.Reverse()`',
    ],
    bugs: [
      'Off-by-one between edges and nodes: `MaxDepth` counts NODES (single node = depth 1), diameter counts EDGES (`left + right`, no `+1`) — mixing the two conventions up is the #1 tree bug',
      'Comparing a node only to its immediate parent instead of threading full bounds down — passes locally, fails globally, and only shows up on trees where a deceptive node sits several levels from the ancestor it actually violates',
      'Seeding BST bounds with `int.MinValue`/`int.MaxValue` instead of `long` — breaks the moment a real node value equals the sentinel',
      'Forgetting the `+ 1` (height) or including it where it doesn\'t belong (diameter\'s `left + right` needs no `+1`, `Height`\'s return does)',
      'Swapping two fields with plain sequential assignment (`a.Left = a.Right; a.Right = a.Left;`) instead of a tuple/temp swap — the second line reads back what the first line just overwrote',
    ],
  },
  'linked-lists': {
    signals: [
      'a position relative to the list\'s *length* is needed without knowing the length up front (middle, kth-from-end) → fast/slow or a gap-of-`n` pointer pair',
      'the head itself might move, get deleted, or not exist yet (delete-at-head, merge, build a new list) → dummy head',
      '"reverse", "reorder", "rotate", or "in groups of k" in the prompt → `prev`/`curr`/`next` iterative reversal',
      'a palindrome/mirror/symmetry question on a list → chain find-middle + reverse-second-half + compare',
      'a design question needing O(1) get/put with recency or order tracking → hashmap + doubly linked list (LRU Cache)',
    ],
    tricks: [
      '`while (fast != null && fast.Next != null)` — check both before touching `fast.Next.Next`',
      'dummy head kills the first-node special case: `var dummy = new ListNode(0, head)`, always `return dummy.Next`',
      'the four-line reversal mantra: save `next`, flip the arrow, advance `prev`, advance `curr`',
      'gap-of-`n`: walk `fast` forward `n` steps first, *then* slide both until `fast.Next == null` — `slow` lands one before the target',
      '`LinkedList<T>`/`LinkedListNode<T>` already gives O(1) `AddFirst`/`Remove`/`RemoveLast` — know it before hand-rolling a doubly linked list from scratch',
    ],
    bugs: [
      'forgetting `fast.Next != null` in a fast/slow loop guard → `NullReferenceException` on `fast.Next.Next`',
      'comparing `slow.Val == fast.Val` instead of `slow == fast` for cycle detection — values can coincide without a cycle',
      'not saving `curr.Next` before `curr.Next = prev` during reversal — the rest of the list is unrecoverably gone',
      'off-by-one on the gap size in remove-nth-from-end — removes the wrong node or leaves `slow` pointing *at* the target instead of before it',
      'returning `head` instead of `prev` after an iterative reversal — `head` is now the new *tail*, pointing at `null`',
    ],
  },
  'array-techniques': {
    signals: [
      '"answer many range-sum queries on a fixed array" that never changes → precompute `prefix` once, O(1) per query',
      '"in-place" / "O(1) extra space" over a small fixed alphabet of values → read/write pointers, or a multi-region partition',
      '"maximum/best contiguous subarray" → single-pass running accumulator (Kadane, DP compressed to one variable)',
      '"majority" / "appears more than n/2 times" → Boyer-Moore vote-cancel, O(1) space instead of a frequency map',
      '"every element except itself", no division allowed → prefix pass x suffix pass folded into one output array',
    ],
    tricks: [
      'prefix sum: `prefix[right + 1] - prefix[left]` answers any range in O(1) after one O(n) precompute',
      'Dutch National Flag: `mid` advances after a swap with `low` (a known value lands) but NOT after a swap with `high` (the swapped-in value is unexamined)',
      'Kadane: `current = Math.Max(nums[i], current + nums[i])` — restart beats dragging a negative run forward',
      'Boyer-Moore: when `count` hits 0 the *next* element becomes candidate even if wrong — the true majority always outlasts every challenger combined',
      'product-except-self: fold prefix products into the output array on the way forward, multiply in the running suffix product on the way back — no division, no second array',
    ],
    bugs: [
      'Dutch National Flag: advancing `mid` after a swap with `high` — the swapped-in value is unexamined and might itself need reclassifying',
      'Kadane: seeding `best`/`current` with `0` instead of `nums[0]` — silently wrong on an all-negative array',
      'product-except-self: dividing by `nums[i]` to build the answer — breaks the instant any element is `0`',
      'rotate-image: transposing with the inner loop starting at `j = 0` instead of `j = i + 1` — swaps every off-diagonal pair twice and undoes the transpose',
      'spiral-matrix: skipping the `top <= bottom` / `left <= right` guards on the last two edges — double-counts cells once the shape narrows to a single row or column',
    ],
  },
  'stack-queue': {
    signals: [
      'nesting/matching rule ("innermost closes first") → LIFO stack',
      '"next greater/smaller element", "days until warmer" → monotonic stack',
      '"design a stack/queue that also supports X in O(1)" → stack-backed design',
      'undo/backtrack semantics: the most recent action reverses first → stack',
    ],
    tricks: [
      '`while (stack.Count > 0 && Breaks(...))` not `if` — one arrival can resolve several waiting entries at once',
      'push the **index**, not the value, whenever you need `i - j` or need to know which slot to write back into',
      'two stacks in opposite roles = a queue: `_in` absorbs pushes, `_out` serves pops, refill only when `_out` is empty (amortized `O(1)`)',
      'a second stack in lockstep with the first turns any "running X" query (min, max) into `O(1)` — push `Math.Min(val, prevMin)` alongside every value',
      '`stack.TryPop(out var top)` / `TryPeek` skip the explicit `Count == 0` check before every pop',
    ],
    bugs: [
      'checking `stack.Count == 0` only at the end and skipping it inside the loop — popping an empty stack throws instead of failing gracefully',
      'using `if` instead of `while` against the stack top — misses multi-resolve cases (one warm day beating several waiting days at once)',
      'mutating a string/array while iterating over it instead of marking offenders and rebuilding afterward — shifts every index past the edit point',
      'transferring between two stacks on every call instead of only when the target is empty — still correct, but throws away the amortized `O(1)` argument',
    ],
  },
  'heap-top-k': {
    signals: [
      '"Kth largest/smallest", "top K", "K closest" in the prompt → bounded heap of size K',
      'several already-sorted sources need combining → K-way merge (one heap slot per source)',
      '"median" of a growing/streaming sequence → two heaps, balanced around the middle',
      'comparison-based structure needed, no bounded value range → heap beats bucket/counting sort',
    ],
    tricks: [
      'push everything, evict when `heap.Count > k` — never bother checking before the push',
      'max-heap = min-heap + `Comparer<int>.Create((a, b) => b.CompareTo(a))`',
      'K-way merge: the instant you pop `node`, refill with `node.Next` — one heap slot per live source',
      'two-heap median: push into `_lo` first, unconditionally shuttle its max to `_hi`, then undo the shuttle if that left `_hi` too big',
      'quickselect (`O(n)` avg) and bucket sort (`O(n)`) both beat a heap when their preconditions hold — know when to reach for them instead',
    ],
    bugs: [
      'evicting with `heap.Count >= k` instead of `> k` — caps the heap one element short',
      'forgetting to re-enqueue the next candidate after a pop in a K-way merge — a source goes silent mid-stream',
      '`(_lo.Peek() + _hi.Peek()) / 2` without the `.0` — integer division truncates the median',
      'reaching for `Remove`/`DecreaseKey` on `PriorityQueue` expecting an efficient update — it doesn\'t exist cheaply; lazy deletion is the real idiom',
      'keying the heap by the wrong thing — raw value vs. a derived count/frequency/distance — same template, silently wrong priority',
    ],
  },
  'trie': {
    signals: [
      'problem statement says "prefix", "starts with", or hands you a dictionary/list of words to query repeatedly',
      'autocomplete-style question: "what words begin with X?"',
      'a wildcard character (e.g. `.` = any letter) appears inside a word-search question',
      'many words share long prefixes and a `HashSet<string>` would repeat the same scan work',
    ],
    tricks: [
      'node = tiny dictionary of "what char can I go to next": `Dictionary<char, TrieNode>` (any alphabet) or `TrieNode?[26]` (lowercase only, `c - \'a\'` indexing, no hashing)',
      'one `bool IsWord` flag per node separates "path exists" (prefix) from "path is a complete stored word" — `Search` needs both, `StartsWith`/prefix-check needs only the first',
      'wildcard search = DFS that branches over every child at a `.` instead of one `TryGetValue` lookup, short-circuiting the moment a branch returns true',
      'Word Search II: store the whole word (or null) at its terminal node, add it to results and null the field on match — that\'s how you avoid reporting the same word twice across different grid paths',
      'cost of every trie op is O(word length), not O(number of words stored) — that\'s the entire reason it beats a flat list/set',
    ],
    bugs: [
      'checking only "does the path exist" and skipping `IsWord` in `Search` — makes a stored prefix look like a stored word',
      'indexing `Children[c]` directly instead of `TryGetValue` — throws or silently auto-creates a node you didn\'t mean to',
      'in wildcard search, not short-circuiting the `.` branch loop (or ANDing instead of ORing branch results) — either misses valid matches or wastes work exploring every child',
      'Word Search II: rebuilding the trie per grid cell instead of once up front, or forgetting to null the matched word\'s field — duplicate results or O(cells × words) waste',
      'Word Search II specifically: skipping the mark-visited-before-recurse / restore-after-backtrack discipline from plain Word Search — the trie on top makes that bug much harder to spot',
    ],
  },
  'dynamic-programming': {
    signals: [
      '"count the ways" / "minimum or maximum cost or value" over a sequence of choices → DP',
      'a brute-force recursion re-derives the exact same arguments over and over (`ways(n-2)` from two branches, `dp[a-5]` feeding multiple later `a`s) → overlapping subproblems',
      'optimal substructure: the best answer for the whole input is built from the best answers to strictly smaller versions of the same problem, not just any answer to them',
      'a grid with only right/down moves, or a string question about splitting or matching → 2D or string DP — the family that also includes LCS (`1143`) and Edit Distance',
    ],
    tricks: [
      'name the state as a sentence first (`dp[i]` = ...) — the recurrence falls out once the sentence is precise',
      'write the brute-force recursion, memoize it top-down to prove it\'s right, then flip to a bottom-up loop once it works',
      'if `dp[i]` only ever reads `dp[i-1]`/`dp[i-2]`, roll the table down to 2-3 variables — O(n) space becomes O(1)',
      'Kadane (Maximum Subarray) is DP already compressed to one variable — recognize it before reaching for a fancier tool',
      'fixed predecessors (`i-1`, `i-2`, Climbing Stairs / House Robber) vs. variable predecessors (every coin, every dictionary word, every `j < i`) is the fork between the two solution shapes',
    ],
    bugs: [
      'wrong loop order — reading a `dp` entry before it\'s filled, or accidentally computing the "combinations" variant instead of "minimum" by swapping which loop is outermost (Coin Change vs. Coin Change II)',
      'off-by-one on the base case — indexing `dp[i-1]`/`dp[i-2]` before the array holds anything real',
      'an "impossible" sentinel that can look like a real answer, or that overflows once you add `1` to it (`int.MaxValue + 1`)',
      'returning `dp[n-1]` when the true answer is `max(dp)` over every state — the optimum doesn\'t always end at the last index (LIS)',
    ],
  },
  'advanced-graphs': {
    signals: [
      '"prerequisites", "dependencies", "can all X be finished" → topological sort (Kahn\'s)',
      '"how many groups/components/provinces" from pairwise connections → union-find',
      'weighted edges + shortest/cheapest/fastest path from one source → Dijkstra',
      'BFS/DFS almost fits but order, grouping, or cost gets in the way → one level up from `bfs-dfs`',
    ],
    tricks: [
      'Kahn\'s: track `inDegree`, seed the queue with every node at `0`, decrement neighbors on dequeue, enqueue when a neighbor hits exactly `0`',
      'cycle check is just `processed == numCourses` (or `order.Count == numCourses`) — no separate DFS pass needed',
      'union-find: path compression (`parent[x] = Find(parent[x])`) + union by size collapses `Find` to near-O(1) (technically `O(α(n))`)',
      'Dijkstra\'s lazy deletion: `PriorityQueue<TElement, TPriority>` has no `DecreaseKey` — push a fresh `(node, betterDist)` duplicate and skip stale pops with `if (d > dist[node]) continue;`',
      'Dijkstra only works with non-negative weights — a negative edge breaks the "pop = final" guarantee that makes the greedy step valid',
    ],
    bugs: [
      'building the dependency edge backwards (`graph[a].Add(b)` instead of `graph[b].Add(a)`) — silently reverses the whole order',
      'forgetting the `processed == numCourses` check and returning `true` unconditionally after the BFS loop drains',
      'skipping the `d > dist[node]` staleness check in Dijkstra — a stale, inflated distance gets used to relax neighbors as if current',
      'forgetting to update `size[]` after a union-find merge, or skipping path compression — `Find` degrades back toward `O(n)`',
      '1-indexed node labels (LC 743 numbers nodes `1..n`) sized as `0..n-1` — off-by-one array bounds or a dropped last node',
    ],
  },
  'greedy': {
    signals: [
      'a single forward pass with one running variable (a min, a frontier, a running total) is all the state you need — no need to remember every option seen, just the best one',
      'the question asks for a max/min produced by an irreversible sequence of choices: `maximum profit from one transaction`, `minimum jumps`, `can you complete the circuit`',
      'you can state a never-regret or exchange argument in one sentence (`swapping this choice for any other can\'t help`) — if you can\'t, it\'s probably `dynamic-programming`, not greedy',
      'sorting first turns the problem into one clean left-to-right pass',
    ],
    tricks: [
      '`Math.Max`/`Math.Min` fold replaces a whole DP table when `dp[i]` only ever depends on `dp[i-1]` — that\'s the buy-sell-stock/Kadane compression',
      'restart-past-failure: if the run fails starting at `i`, every start `< i` also fails, so jump straight to `start = i + 1` and reset — no need to re-check the discarded range',
      'extend-the-boundary-until-it-closes: `end = Math.Max(end, lastIndex[x])`, cut the moment `i == end`',
      'frontier tracking: `farthest = Math.Max(farthest, i + nums[i])`, bail the instant `i > farthest`',
      'when the greedy proof doesn\'t hold, don\'t force it — fall back to the full DP table and compare costs (`Jump Game` shows both side by side)',
    ],
    bugs: [
      'assuming greedy always works because the loop compiles and returns something plausible — it needs an actual never-regret/exchange proof, not vibes',
      'forgetting the floor/failure case: unreachable frontier isn\'t `false` unless you check it, `total < cost` must map to `-1`, an all-decreasing price array must floor at `0`',
      'getting the order of `Update` vs `Combine` backwards on a min/max fold — e.g. updating `minPrice` before scoring today\'s profit can silently change what a step computes',
      'off-by-one on when a greedy boundary is allowed to close: `i == end` vs `i >= end`, or `nums.Length` vs `nums.Length - 1` as the goal index',
    ],
  },
  'intervals': {
    signals: [
      'input is a list of `[start, end]` (or `[start, end)`) pairs — meetings, bookings, log windows, version ranges',
      '"merge overlapping" / "combine ranges" → sort by start',
      '"max non-conflicting" / "minimum removals" → sort by end, greedy select',
      '"how many rooms/resources needed at once" → sort by start, heap of end times (or the start/end event sweep)',
    ],
    tricks: [
      'the whole topic is one sort plus one linear sweep — the sort key (start vs end) IS the design decision, make it first',
      'merge check: `current.Start <= last.End`; select check: `current.Start >= lastEnd` — note the flipped-feeling inequality',
      'touching endpoints (`current.Start == last.End`) count as overlap when merging, but do NOT conflict when selecting — the two problems disagree with each other on purpose, check the statement',
      'Meeting Rooms II = the select template\'s start-sort, with a min-heap of end times standing in for a single `lastEnd`',
      'the chronological event-sweep (`starts[]` and `ends[]` sorted independently, walked with two pointers) reconstructs the same answer as the heap without paying `O(log n)` per operation',
    ],
    bugs: [
      'sorting by the wrong key — merge needs start, select needs end; getting it backwards still compiles and still "runs," just returns a plausible-looking wrong answer',
      '`<` vs `<=` at the overlap/conflict check — flips whether touching intervals count, and it\'s easy to copy the wrong convention from a sibling problem in this same topic',
      'advancing the "kept" tracker (`lastEnd`) when you actually dropped an interval — the survivor has to stay the earlier-finishing one, or the greedy guarantee breaks',
      'mutating an interval `int[]` in place (`last[1] = ...`) without noticing it\'s a live reference back into the caller\'s own input array',
    ],
  },
  'bit-manipulation': {
    signals: [
      '"constant/no extra space" where a `HashSet` would normally solve it → look for pair-cancellation via XOR',
      '"how many 1 bits", "power of two", "reverse the bits" → the `n & (n - 1)` family',
      'problem forbids `+`/`-` and wants the result built from `&`, `|`, `^`, `<<`, `>>` instead',
      'an answer is needed for every integer `0..n`, and each one is one bit-shift away from a smaller one already computed → bit-DP',
    ],
    tricks: [
      '`x ^ x == 0` and `x ^ 0 == x` — XOR cancels duplicate pairs; commutative + associative means order never matters',
      '`n & (n - 1)` clears the lowest set bit — loop until `n == 0`, count the clears, done in `popcount(n)` steps not 32',
      '`n & -n` isolates the lowest set bit instead of clearing it',
      'seed the accumulator with `n` (or fold in the full index range) before XOR-ing indices against values — Missing Number',
      '`bits[i] = bits[i >> 1] + (i & 1)` — bit-DP: drop the lowest bit, add it back if it was set',
    ],
    bugs: [
      'assuming XOR cancellation generalizes past pairs — three copies don\'t vanish, `x ^ x ^ x == x`',
      'reaching for a `HashSet<int>` when the prompt says "constant extra space" — it passes, but it\'s the O(n)-space solution you were steered away from',
      'signed right shift (`>>`) sign-extending a negative value into an infinite loop — use `uint` or the `>>>` operator',
      'off-by-one on array size — an inclusive `0..n` range needs `n + 1` slots, not `n`',
      'Gauss-sum (`n * (n + 1) / 2`) without `long` — the multiply overflows `int` for large enough `n`',
    ],
  },

  // ───────────────────────── systems ─────────────────────────

  "bits-and-memory": {
    signals: [
      "A counter, byte total, or duration on a dashboard reading about −2.1 billion — an `int` accumulator wrapped past `int.MaxValue` and nothing threw.",
      "A memory graph that grows with row count faster than the field sizes say it should — the gap is struct padding, ~8 bytes per row at a time.",
      "`IndexOutOfRangeException` from a binary search that only ever fires on the largest tenant — `(lo + hi) / 2` overflowed to a negative midpoint.",
      "Bytes that arrive at a consumer reversed, or a `Guid` that changes identity when it round-trips through `ToByteArray()` — a host-endianness assumption escaped the process.",
      "Hex in a log, a dump, or a stack trace — whoever wrote it chose base 16 because byte boundaries mattered.",
    ],
    tricks: [
      "`-x` is `~x + 1`; from there `x & -x` isolates the lowest set bit and `x & (x - 1)` clears it.",
      "Midpoints as `lo + (hi - lo) / 2`, never `(lo + hi) / 2`; any accumulator over unbounded input is `long`, not `int`.",
      "Declare struct fields widest first — `long`, then `int`, then `byte`/`bool` — and the padding collapses: 24 bytes became 16 with no other change.",
      "`Unsafe.SizeOf<T>()` asserted in a test pins the size of a hot struct, so the next field someone appends shows up in review instead of on the memory graph.",
      "`BinaryPrimitives.Write*BigEndian` / `Read*BigEndian` at every I/O boundary; keep `BitConverter` and raw `MemoryMarshal.AsBytes` strictly inside the process.",
    ],
    bugs: [
      "\"The struct is 10 bytes, the fields add to 10.\" Alignment padding made it 24 — of four structs on the exercise page, only `Point3` matched the arithmetic.",
      "\"Declaration order is what I get.\" True until the struct holds an object reference: the CLR then lays it out automatically and ignores even an explicit `LayoutKind.Sequential`.",
      "\"Overflow throws.\" C# integer arithmetic is `unchecked` by default and wraps silently; only a `checked` region — or a constant expression, which is folded in checked mode — fails loudly.",
      "\"A reference is a handle, not an address.\" It is an address; the GC just rewrites it when it moves the object, which is the entire reason `fixed` and `GCHandle` exist.",
      "\"`Pack = 1` is the fast option.\" It removes padding, but it also removes the guarantee that a naturally aligned load is a single access — some architectures fault on the misaligned load it creates, and that guarantee is what the memory model's atomicity is stated in terms of. Reorder the fields instead.",
    ],
  },
  "process-and-thread": {
    signals: [
      "thread count far above core count and `nonvoluntary_ctxt_switches` in `/proc/PID/task/*/status` climbing — the machine is spending its turns switching, not working",
      "CPU utilization flat and low while p99 doubles — utilization counts *running* threads and says nothing about runnable-but-queued or blocked ones",
      "`sy` in `top` (or `stime` in `/proc/PID/stat`) is a large fraction of CPU — the time is going at the user/kernel boundary, not in your C#",
      "a dump full of pool threads parked in `WaitOne`/`.Result` — each one is a reserved 8 MiB stack plus a scheduler entry, not idle capacity",
      "two threads disagree about something that \"is just a local\" — it was captured by a lambda or an `async` state machine and quietly promoted to the heap",
    ],
    tricks: [
      "count crossings before optimizing code: `strace -c -f -p PID` for a few seconds gives a syscall histogram — trust its *counts*, never its timings",
      "buffer or batch, because a syscall costs roughly the same whatever it carries: a 4 KiB write costs about what a 1-byte write costs",
      "size CPU-bound work queues to `Environment.ProcessorCount` — past the core count, extra threads only divide the same cores into shorter turns",
      "read the two switch counters per thread: `voluntary_ctxt_switches` rising means waiting, `nonvoluntary_ctxt_switches` rising means oversubscription",
      "price a handoff before you design around it: staying on the core is ~hundreds of ns, parking and being woken is ~tens of µs, a full time slice is ~ms",
    ],
    bugs: [
      "believing the kernel is a program running beside yours — it mostly runs *on your thread* in kernel mode, only when called, faulted, or interrupted into",
      "believing locals are private by hardware — every thread shares one address space, the privacy is an addressing convention, and captured locals move to the heap",
      "believing `async` removes the syscall — `WriteAsync` still crosses the boundary; what changes is which thread is parked while it completes",
      "reading a thread's 8 MiB stack as 8 MiB of RAM — that is reserved address space, only touched pages become resident, and the real cost is scheduling",
      "timing anything under `strace` or a debugger — every syscall becomes at least two extra stops for the tracer (entry and exit), so a traced run is good for counting crossings, not for reasoning about how expensive each one is",
    ],
  },
  "cpu-execution": {
    signals: [
      "a profiler puts real self time in a method whose body is three lines — the question is whether something stopped it being **inlined**, not whether those three lines are slow",
      "a stack trace has fewer frames than the source has calls — inlined methods never pushed a return address, so there is nothing left to walk",
      "a method's numbers change with no code change between two runs — check the build configuration first: `dotnet run file.cs` defaults to **Debug**, which turns the JIT optimiser off and leaves every local round-tripping through a stack slot instead of a register",
      "throughput drops after you ship the *second* implementation of an interface, with no change to the hot path — the JIT's guarded-devirtualisation guess just stopped hitting",
      "the same DLL behaves differently per core on ARM64 versus x64 — the JIT generated different machine code for a different ISA, so numbers carried over from x64 are not evidence",
    ],
    tricks: [
      "`DOTNET_JitDisasm=MethodName dotnet run -c Release` prints the machine code the JIT actually generated — it settles \"did it inline\" in seconds where reasoning does not",
      "count a loop's *simultaneously live* values, not its lines: 16 general-purpose registers is the whole budget, and past it the compiler spills the rest to stack slots — every access to a spilled value is now an explicit load or store, where a register operand needed neither",
      "keep interfaces and virtual calls out of the innermost loop and let the abstraction live one level out — a monomorphic call site gets a cheap type check plus an inlined body, a polymorphic one gets a real dispatch",
      "disassemble the **linked** binary (`objdump -d --no-show-raw-insn`), never the `.o` — an unlinked object shows every `call` pointing at the instruction after itself",
      "read the instruction count off `DOTNET_JitDisasm`, not a stopwatch, when you want to compare two codegens for the same source — the count is exact and does not drift between runs the way a clock reading does",
    ],
    bugs: [
      "\"a method call is expensive\" — `call`/`ret` is two instructions; what actually grows is everything the call boundary forces around it, arguments marshalled into named registers, live values pushed into callee-saved registers and popped back after. The real cost is the optimisation lost across the boundary, not `call`/`ret` itself",
      "\"registers are just fast memory\" — they have no addresses, which is the point: they are named inside the instruction, so there is nothing to look up",
      "\"interface calls are slow\" *and* \"interface calls are free\" — both wrong; the JIT bets on the types it profiled, and the bet is a compare-and-branch when it wins and a real dispatch when it loses",
      "reading a Debug build's numbers, or the first pass before tiered compilation has promoted the method, as if either were steady-state — both show the JIT's warmup policy, not your code",
      "drawing conclusions from `-O0` or tier-0 disassembly — those are transcriptions made for a debugger and will never run in production",
    ],
  },
  "stack-and-heap": {
    signals: [
      "p99 latency doubles under load while the mean stays flat and gen0/gen1/gen2 counts climb — you are looking at allocation *rate*, not slow code",
      "a container restarts with exit code 134 (SIGABRT) and no exception in the log — that is the stack running out; 137 is the OOM killer and a different problem",
      "a hot-path signature that takes `object`, an interface, `IEnumerable<T>` or `params object[]` — every call site boxes or allocates an array you never asked for",
      "a lambda declared inside a loop: one closure object plus one delegate per iteration (88 bytes, once per pass through the loop)",
      "`list[i].Field = x` on a `List<struct>` refuses to compile (CS1612) — the indexer returns a copy, so the mutation would be lost",
    ],
    tricks: [
      "measure instead of guessing: `GC.GetAllocatedBytesForCurrentThread()` before and after is exact, per-thread, and cheap enough for production",
      "a `static` lambda (or one hoisted out of the loop) turns 88 B per iteration into 0 — Roslyn caches non-capturing delegates in a static field",
      "take `where T : IShape` instead of an `IShape` parameter — the generic version keeps the struct on the frame, the interface version boxes it (24 B)",
      "bounded scratch → `stackalloc` + `Span<T>` (zero heap bytes, zero gen0 collections, versus `new int[64]`'s array header plus payload on every call); bigger or input-sized → `ArrayPool<T>`, and remember `Rent` does not zero and may hand back a longer array",
      "recursion whose depth follows the input → an explicit `Stack<T>` on the heap; if the recursion must stay, guard it with `RuntimeHelpers.TryEnsureSufficientExecutionStack()` and cap depth at the API boundary the way `System.Text.Json` caps at 64",
    ],
    bugs: [
      "\"structs are on the stack, classes on the heap\" — a struct lives where its *variable* lives: a struct field of a class is on the heap, and so is a struct in an array or a closure",
      "\"a local is thread-private\" — capturing it moves it into a heap object; its address is in the GC heap, not in `[stack]`, and it is shared like anything else there",
      "\"allocation is expensive, avoid `new`\" — the allocation is a pointer bump; the bill is the collection later, so the number to move is bytes allocated per operation",
      "\"`catch (Exception)` covers everything\" — a stack overflow runs no `catch` and no `finally`; the process calls `FailFast` and dies with nothing in the log",
      "\"this method doesn't allocate\", concluded from testing it alone — the JIT deletes a box that cannot escape the method; put the same object in a list, a field or a `Task`, as production does, and the 24 bytes come back",
    ],
  },
  "virtual-memory": {
    signals: [
      "container killed with exit code 137 while the GC log shows a small heap — the limit is enforced against *resident* pages, not the managed heap",
      "`VmSize` in the tens of GiB seconds after startup — that is the runtime's address-space reservation, never itself a memory problem",
      "the first pass over a freshly allocated buffer behaves differently from every later pass over the same memory, or a p99 that only misbehaves in the first minute after a deploy — you are watching first-touch page faults, one trap per untouched 4 KiB page, not your code",
      "rising `maj_flt` in `/proc/PID/stat` with idle CPU and high iowait — the service is waiting on a disk for memory it thought it had",
      "RSS flat and high right after a full blocking GC dropped `GC.GetTotalMemory` to nothing — freed pages the runtime has not handed back to the OS",
    ],
    tricks: [
      "`new byte[n]` buys address space, not memory — the real bill is one minor fault per 4 KiB page, charged to whichever thread touches it first",
      "pre-touch or warm up once instead of faulting on the request path; a warm `ArrayPool` rental wins because its pages are already resident and mapped, so the walk never reaches the kernel — a cold rental faults exactly like a fresh `new byte[]`",
      "never read a fresh buffer before writing it — the read fault maps the shared zero page, and the later write then has to tear down that read-only mapping before it can install a writable one, so it costs more than a first write alone would",
      "measure with the kernel's own counters (`/proc/self/stat` fields 10 and 12, or `getrusage`); faults are not syscalls and `strace` shows none of them",
      "compare `GC.GetTotalMemory` against `VmRSS` before tuning: allocation-rate problems and residency problems have different fixes",
    ],
    bugs: [
      "\"the array is allocated, so the memory is used\" — untouched pages are not resident; 8 GiB of untouched arrays moved `VmRSS` by tens of MiB, not gigabytes",
      "\"RSS went down after the GC\" — usually it does not; the collector frees objects, not necessarily pages, and the container keeps counting them",
      "clearing or \"initializing\" a freshly allocated array to be safe — the kernel already guarantees zeros, and the read pass maps the shared zero page while the write pass right after has to tear that mapping down first, roughly doubling the fault bill of a plain first write",
      "assuming \"first touch\" still means first touch on a buffer the allocator recycled — after a couple of rounds the GC hands back pages that are already resident, the fault count drops to zero, and nothing in the code says it stopped exercising the path you meant to test",
      "reading `VmSize`/`VIRT` as memory usage, or expecting a syscall trace to explain a page-fault storm",
    ],
  },
  "memory-hierarchy": {
    signals: [
      "Two loops with the same Big-O and the same operation count run at noticeably different real cost — the constant Big-O throws away is *cache lines fetched*, not instructions",
      "Runtime grows much faster than the input while the work count stays linear — the working set just crossed L2 or L3",
      "A hot scan got slower after somebody added a field nobody reads to the record",
      "The hot path walks a `LinkedList<T>`, an array of `class` elements, or any reference array that has been sorted, filtered or rebuilt",
      "The profiler shows one flat `for` body with no expensive callee — cache stalls have no hot method to blame",
    ],
    tricks: [
      "Count lines, not elements: cost ≈ `bytes touched ÷ 64` at the latency of whichever level holds them",
      "Make the inner loop run *along* memory before touching the algorithm — `i,k,j` reuses each cache line for sixteen consecutive elements where `i,j,k` pulls a new line for every one",
      "Shrink the record: `struct` over `class` removes the object header and the reference chase — 32 bytes of struct payload costs 56 bytes as a `class` element (8 B reference + 16 B header + 32 B fields)",
      "Split the scanned field into its own array (SoA / columnar) when you scan repeatedly; keep AoS when you look records up by index",
      "Tile the loop so loaded data is reused before eviction, and pad power-of-two dimensions to break cache-set conflicts",
    ],
    bugs: [
      "Trusting capacity and stopping there — a reused chunk can need a fraction of a cache's total size and still not fit, because a set-associative cache bounds *which* lines a stride can land in, not just how many bytes it needs",
      "Picking `LinkedList<T>` for \"O(1) insertion\": every node is a separate heap object, so a traversal is a pointer chase that misses cache per node, where `List<int>` is one contiguous run the prefetcher can follow",
      "Assuming SoA always wins — reading one whole record at a time flips it: AoS's record fits in a single 64-byte line, SoA scatters that same record's fields across up to eight independent arrays and up to eight different lines",
      "Blaming page count for a stride that happens to be a round number of bytes — a larger matrix can cross *more* 4 KiB pages and still be the faster size; it is the stride's shared factor with the page size, not how many pages it touches, that decides which cache sets get hit",
      "Treating 64 bytes as a universal constant instead of reading `coherency_line_size`, and sampling only power-of-two strides so set conflicts look like line behaviour",
    ],
  },
  "cpu-pipeline": {
    signals: [
      "A hot loop costs many more cycles per element than it has instructions, and the working set already fits in L1 — suspect the branch predictor or a dependency chain, not the cache",
      "Re-ordering or grouping the *input* changes the runtime of a loop whose code you never touched",
      "A filter, validation or tokenizing loop over data that accepts roughly half of what it sees — the predicate is a coin flip",
      "A running total that will not speed up however you tune the loop: one accumulator is a latency chain, and the unit idles between steps",
      "Correct on x64, intermittently wrong on ARM64 with no code change — that is the reordering half of this page, and it belongs to `/systems/memory-model/`",
    ],
    tricks: [
      "Diagnose by changing the data, not the code: re-run with the same array sorted or grouped, everything else fixed. If the shape of the result changes it was prediction; if it does not, look at the cache",
      "Make the branch boring rather than absent — hoist a per-request condition out of a per-item loop, split one loop into two uniform ones, or partition so the predicate is constant per run",
      "Break a dependency chain into 2-4 independent accumulators: a single accumulator makes every addition wait for the previous one, so the loop can never run faster than one link of that chain no matter how many arithmetic units the core has — confirm it with `DOTNET_JitDisasm`, each chain should land in its own register",
      "`DOTNET_JitDisasm=YourMethod` with `-c Release` tells you whether you got `jl` or `cmov` — never infer it from the syntax",
      "Go branchless (`~((v - 128) >> 31)`) or vector (`Vector256`) only when the predicate is genuinely random and the body is small — then the comparison becomes data instead of control flow",
    ],
    bugs: [
      "Blaming the cache for what the predictor did. Re-order the same array with the working set unchanged before you touch the layout",
      "\"Branches are expensive.\" A predictor needs only a handful of history bits to learn a periodic pattern, so a predictable branch costs next to nothing; a branchless mask removes the guess but adds guaranteed extra work on *every* element, predictable or not — worth it only when the predicate is genuinely close to random",
      "Assuming `cond ? a : b` is branchless in C#. RyuJIT compiled it to *two* jumps inside this loop, even though it emits `cmovg` for a standalone `a > b ? a : b` outside one",
      "Trusting a synthetic loop that replays one small fixed array thousands of times — a large enough history table starts memorising that specific repeated sequence, which is a fact about the test's shape, not about how predictable real traffic is",
      "Reaching for a branchless rewrite before checking whether the branch was ever a problem — a predicate that is mostly one-directional on real data costs next to nothing once the predictor has seen it, and unconditional arithmetic trades that near-free branch for guaranteed cost on every element",
    ],
  },
  "gc-internals": {
    signals: [
      "p99 latency doubled while mean and CPU stayed flat — that shape is a pause, and a stop-the-world collection is the first suspect",
      "`gen-2-gc-count` climbing monotonically in `dotnet-counters monitor System.Runtime` while the heap grows — objects are being promoted faster than they die",
      "memory grows forever in a runtime that has a garbage collector — nothing failed to free, something is still reachable from a static, an event, a `Timer` or a captured closure",
      "a per-request buffer sized anywhere near 100 KB in a review diff — 85,000 bytes is the LOH line, and crossing it turns cheap gen0 collections into full gen2 ones",
      "the pod is OOM-killed while `GC.GetTotalMemory` looks fine — GC heap size, committed bytes and resident set are three different numbers",
    ],
    tricks: [
      "measure bytes, not milliseconds: `GC.GetAllocatedBytesForCurrentThread` around an operation gives an exact allocation delta with zero timer noise, and `GC.CollectionCount(0/1/2)` says which generation your change moved",
      "pool anything ≥ 85,000 bytes with `ArrayPool<T>.Shared.Rent`/`Return` — allocate once, promote once, never collect again (`Rent` returns a dirty, possibly larger buffer, so use only the length you asked for)",
      "keep the live set small, not the allocation rate: a blocking collection's mark work is proportional to what survives it, not to how fast you allocate, so an unevicting cache is a permanent latency tax rather than a memory one",
      "own unmanaged handles through `SafeHandle` and implement `IDisposable` only — and if you really must write `~Type()`, call `GC.SuppressFinalize(this)` in `Dispose`",
      "every `+=` on something longer-lived than the subscriber needs a matching `-=` on a path that always runs — a `Dispose`, a `finally`, or a scope the DI container owns",
    ],
    bugs: [
      "\"allocation is slow, avoid `new`\" — allocation is a pointer bump; the bill is survival, so the levers are allocation *rate* and object lifetime, not the cost of `new`",
      "reaching for `GC.Collect()` when memory grows — the objects are reachable, so a forced collection keeps every one of them and you have paid a stop-the-world pause to learn nothing",
      "unsubscribing with a lambda (`x -= s => Handle(s)`) — delegate equality is `(target, method)`, so two lambdas never match and `-=` silently removes nothing",
      "adding an empty finalizer \"to be safe\" — an object with a finalizer is not freed by the collection that finds it unreachable, only queued; its bytes wait for a second collection after the finalizer thread runs, and because surviving a collection promotes, that costs it an extra generation on top",
      "reading gen0 collection count as a health metric — the count says how often, only pause time and the gen1/gen2 columns say what it cost",
    ],
  },
  "il-jit-codegen": {
    signals: [
      "p99 spikes for about a minute after every deploy and then settles on its own — a fresh process runs every hot method at tier 0 until it has been called a few dozen times *and* the start-up grace period expires",
      "a tight timing loop prints an implausibly small per-operation cost, or zero — the JIT proved the result was never used and deleted the work you meant to measure",
      "the profiler blames a method that never appears in the stack traces, or charges an outsized share to a line that cannot cost it — inlining moved the cost into the caller",
      "two implementations rank differently in Debug and Release, or before and after warmup — the build and the tier change which one wins, not just by how much",
      "a JIT listing whose header says `Instrumented Tier0` or `MinOpts` when you thought you were reading the optimised code",
    ],
    tricks: [
      "`DOTNET_JitDisasm=MethodName dotnet run -c Release` prints the exact instructions — did it inline, did the bounds check go, is it vectorised, answered in ten seconds",
      "write the loop shape the JIT already proves safe: `for (int i = 0; i < a.Length; i++)`, or pass a `Span<T>` instead of an array plus a separate count",
      "break the dependency chain before reaching for anything exotic — a single accumulator makes every addition wait on the one before it; independent accumulators give the out-of-order scheduler more than one ready to run at a time, with no unsafe code",
      "measure with BenchmarkDotNet: warm up in-process, consume every result so nothing gets deleted, report a median of several runs, and check allocations alongside the clock",
      "`DOTNET_TieredCompilation=0`, `DOTNET_TC_CallCountingDelayMs` and `DOTNET_TC_CallCountThreshold` move the tier-0/tier-1 boundary while you investigate",
    ],
    bugs: [
      "\"compiled languages are fast, JIT languages are slow\" — the JIT is a compiler that knows which CPU it is on, which types actually showed up and how often each block ran",
      "sprinkling `[MethodImpl(MethodImplOptions.AggressiveInlining)]` — the inliner accepted an 829-byte-IL callee here; a refusal usually means a `try`/`catch`, and `using` and `lock` compile into one",
      "\"bounds checks cost extra\" — the idiomatic loop has none left to pay for, and even a check that genuinely survives is two instructions (a `cmp` and a never-taken `jae`) sitting next to work the loop was already doing",
      "caching `a.Length` in a local, or rewriting `foreach` as `for` — the JIT already knew the length could not change, so no check goes away either way, and `foreach` over an array compiles to the identical loop as the equivalent `for`",
      "reading `Stopwatch.ElapsedMilliseconds` (a `long`) on a sub-millisecond workload and concluding two implementations are the same speed",
    ],
  },

  // ─────────────────────── concurrency ───────────────────────

  "threads-and-scheduling": {
    signals: [
      "CPU flat at 15% while `threadpool-queue-length` climbs and requests time out — that shape is starvation, not a slow dependency",
      "`ThreadPool.ThreadCount` climbing one worker at a time: the gate thread's hill-climbing injection adds a single worker per check, and demand keeps arriving faster than that",
      "dozens of `.NET TP Worker` threads in state `S` under `futex_do_wait` in `/proc/PID/task/*/wchan`, or parked beneath `Task.Result` in `clrstack -all`",
      "an `AggregateException` wrapping the exception you meant to catch — somebody reached the Task with `.Result` instead of `await`",
      "types whose names contain `d__` at the top of a memory profile: suspended async methods, one per in-flight operation, each pinning its locals",
    ],
    tricks: [
      "`await` all the way down; delete the synchronous facade rather than wrapping it in `Task.Run` — that spends two pool threads per request instead of one",
      "`ThreadPool.SetMinThreads` is the 3 a.m. tourniquet, not the fix: it removes the injection delay and keeps the one-thread-per-request design",
      "`ConfigureAwait(false)` in library code only — it turns off context capture, which is a different mechanism from thread supply, and ASP.NET Core has no context anyway",
      "long-running or blocking-by-design loops get `new Thread` or `TaskCreationOptions.LongRunning`, never a pool work item",
      "`ValueTask<T>` where the common path completes synchronously (0 B on that path); plain `Task<int>` everywhere else, because a suspending `ValueTask` costs more, not less",
    ],
    bugs: [
      "\"A `Task` is a lightweight thread\" — it is a promise. `Task.Delay` is a timer entry, `ReadAsync` is a kernel registration, `TaskCompletionSource` is nothing at all until you call `SetResult`",
      "Load-testing at or below `Environment.ProcessorCount`, where sync-over-async looks perfect because the pool never runs out",
      "Believing `ConfigureAwait(false)` fixes pool starvation — it fixes the single-thread `SynchronizationContext` deadlock, which is a different bug in a different framework",
      "`async void`: there is no Task for `SetException` to fault, so the exception is rethrown on the pool and kills the process",
      "Assuming an `async` method starts on another thread — the stub calls `MoveNext` synchronously on yours, so everything before the first suspending `await` is your caller's time",
    ],
  },
  "memory-model": {
    signals: [
      "a duplicate nobody can explain — the same job ran twice, two log lines a millisecond apart — and no interleaving of the source produces it",
      "a `while (!_stop) { }` spin loop that never exits after another thread sets the flag, CPU pinned at 100% forever",
      "a reader sees a `long`, `double`, `decimal` or `struct` value nobody ever wrote — the old half next to the new half",
      "a concurrency test that creates its threads inside the loop and has never once failed — thread startup is thousands of times wider than the window the bug needs",
      "the failure started on the ARM64 fleet (Graviton, Apple Silicon) and the identical binary is still clean on x86",
    ],
    tricks: [
      "name which of the three guarantees you are missing before picking a tool — atomicity → `Interlocked`, visibility → `volatile`/`Volatile.Read`, ordering → a fence or `lock`",
      "to make a race reproduce: two long-lived threads meeting at a `Barrier`, hundreds of thousands of trials — a thread per trial destroys the window and the test passes forever",
      "`Interlocked.MemoryBarrier()` is the only thing that orders a store against a *later* load; it emits `lock or dword ptr [rsp], 0` — a locked read-modify-write of a byte nobody reads, whose only job is the `lock` prefix's drain of the store buffer",
      "collapse every announce-then-check handshake into one word and one `Interlocked.CompareExchange` — no fence to forget, and somebody always wins instead of both backing off",
      "reach for `lock`, `Lazy<T>` or a concurrent collection first: uncontended, it buys all three guarantees at once, and contention is almost never what's actually hurting you",
    ],
    bugs: [
      "believing `volatile` fixes a store followed by a load — it is release/acquire ordering, and a store followed by a *later* load on another core is exactly the pair release and acquire leave unconstrained; the failure rate drops from unfenced code but never reaches zero",
      "`volatile int i; i++` — `volatile` gives ordering and visibility, never atomicity; the increment is still three operations",
      "quoting x86's behaviour as \"the memory model\" — x86-64 forbids three of the four reorderings and ARM64 forbids none, so code that is accidentally correct here is genuinely broken there",
      "blaming stale caches — caches are coherent; the culprits are the per-core store buffer and a JIT that hoisted your read out of the loop",
      "sprinkling `Thread.Sleep(0)` or `Thread.Yield()` until the symptom goes away — neither is a documented ordering guarantee, and you have converted a reproducible bug into one that comes back after a runtime upgrade",
    ],
  },
  "atomics-and-cas": {
    signals: [
      "a total that is quietly short — request counts, rate limits, \"processed exactly once\" claims that drift by a few percent under load and are exact on your laptop",
      "throughput that stops rising when you add cores while every CPU is busy and no lock shows as contended — one cache line is serialising the service",
      "`volatile` on a field that is incremented: somebody thought about threads and stopped one guarantee short",
      "per-worker counters in a tight `long[]`, or a producer's `head` next to a consumer's `tail` in one object — false sharing waiting for a second core",
      "unexplained variance in throughput between deployments with no code change — two hot fields landing on one cache line some days and two on others, decided by wherever the allocator happened to put the object",
    ],
    tricks: [
      "use the `Interlocked` method when one exists (`Increment`/`Add`/`Exchange` = exactly one `lock`-prefixed instruction, always); a CAS loop with a strictly *pure* body only when it does not",
      "`CompareExchange` returns the value that was there — start the retry from that instead of re-reading it",
      "batch the atomic: accumulate in a local and publish one `Interlocked.Add` per N — the other N-1 increments touch a thread-local value only, no shared state and no locked instruction at all",
      "partition before you argue about primitives — one counter per thread or partition, summed on read, executes zero locked instructions per increment and asks no other core to give up a line, which is the shape that keeps scaling as cores are added",
      "give a hot shared counter its own line with `StructLayout(LayoutKind.Explicit)` and `FieldOffset(64)`, and write down why — unexplained padding is the first thing a reviewer deletes",
    ],
    bugs: [
      "\"`count++` is one instruction so it is atomic\" — `inc dword ptr [rax]` still loads, adds and stores; only the `lock` prefix holds the cache line across the sequence",
      "expecting `volatile` to fix a lost update: it buys visibility and release/acquire ordering, never atomicity",
      "making every field atomic and calling the object thread-safe — `if (Interlocked.Read(ref n) < limit) Interlocked.Increment(ref n)` is still check-then-act",
      "side effects inside a CAS loop: the body runs once per *attempt*, not once per success — a competing writer landing between your read and your swap costs you another full pass, side effects included",
      "assuming the GC removes ABA — it removes the freed-and-reused-address half, not the value-went-A-then-B-then-A half; a node re-published from a pool still fools a CAS",
      "padding everything: 64 bytes per counter is right for eight worker slots and a disaster for a million-element array",
    ],
  },
  "locks-internals": {
    signals: [
      "Throughput is flat while CPU sits well under 100% and `monitor-lock-contention-count` climbs in `dotnet-counters` — one lock is the ceiling and adding pods will not move it",
      "The scaling curve bends down: four threads take longer than one on the same total work, which is the default outcome for a shared lock, not a mystery",
      "A dump where most stacks sit in `Monitor.Wait`, `SemaphoreSlim.Wait` or `.Result`, thread count climbing one worker at a time — a lock problem that has become thread-pool starvation",
      "One core pinned at 100% while nothing completes: something is spinning, and in a container it is eating the CPU quota the lock holder needs to release",
      "p99 latency spikes with a flat mean and flat CPU — most requests resolve on the uncontended fast path, a handful of instructions with no kernel call in it; the unlucky ones exhaust the spin budget and park",
    ],
    tricks: [
      "Measure before you guess: `Monitor.LockContentionCount` before/after is exact, free, and already exported as `monitor-lock-contention-count` for `dotnet-counters` on a live process",
      "Prove the ceiling first — run the same total work on one thread. If N threads are not faster, the serialized section is the bottleneck and no primitive swap will fix it",
      "Stripe: N locks keyed by `hash % N` so unrelated keys stop meeting on the same lock word at all. That is what `ConcurrentDictionary` does internally",
      "Shrink the critical section before you change the primitive: allocation, logging, JSON and every I/O call belong outside the lock",
      "Pick by semantics, not speed: one word → `Interlocked` (no lock word, nothing that can park); must cross an `await` → `SemaphoreSlim`; N-at-a-time → a semaphore; everything else → `lock`",
    ],
    bugs: [
      "Believing contention means blocking. A lock can cost every thread real time while `Monitor.LockContentionCount` barely moves — most acquisitions resolve inside the spin, so the cost is the lock word's cache line moving between cores, which a low contention count will not show you",
      "`lock (this)`, `lock (typeof(T))`, `lock (\"key\")` — types are process-wide and string literals are interned, so you are sharing a lock with code you have never seen. Use a `private static readonly object` or a `System.Threading.Lock`",
      "\"`SpinLock` is the fast lock.\" It is fast only when threads ≤ cores and the section is a handful of instructions; on a preemptible thread-pool thread the holder can be descheduled while everyone else burns a full time slice",
      "\"`SemaphoreSlim(1)` is just a lighter `lock`.\" It carries its own internal `Monitor` plus a waiter list and cancellation support, so contended it registers far more monitor contentions than `lock` doing the identical logical operation — and it is not reentrant, so re-entering deadlocks where `lock` would succeed",
      "Holding a lock across an `await`, an HTTP call or a DB round trip. The hold time goes from a handful of instructions to a network round trip, every other thread queues behind it, and a performance problem becomes an outage",
    ],
  },
  "concurrency-hazards": {
    signals: [
      "Latency for one group of endpoints goes vertical while **CPU drops to near zero** and thread count climbs — that is a deadlock, not a slow dependency",
      "Data that cannot exist: stock at −1, two rows past a unique constraint, a balance that disagrees with the sum of its ledger entries",
      "A diff where an `if` reads shared state and the next line writes it — check-then-act, however many locks are in the vicinity",
      "`.Result`, `.Wait()` or `.GetAwaiter().GetResult()` on any path that can run on a request thread",
      "A `SemaphoreSlim` that arrived in the same commit that made a class's methods `async`",
    ],
    tricks: [
      "Size the critical section to the **invariant**, not to the field — one `lock` spanning check *and* act",
      "Break circular wait: sort the locks by a stable id and always take the smaller first; `RuntimeHelpers.GetHashCode(obj)` when there is no natural key",
      "Expose the compound operation as one method (`TryReserve`) instead of letting callers compose `Count` + `Remove`",
      "`Lazy<T>` instead of hand-rolled double-checked locking; a `static readonly` field when the value needs no runtime input",
      "Take a dump *before* restarting: `dotnet-dump collect`, then `clrstack -all` and `syncblk` — the wait-for cycle reads straight off it",
    ],
    bugs: [
      "\"Every method takes the lock, so the class is thread-safe\" — per-operation safety never composes into a correct two-step operation",
      "Swapping `Dictionary` for `ConcurrentDictionary` to fix a compound operation: the oversell rate goes *up*, never down — a lock-free read removes the accidental staggering that was masking the bug, it does not add the missing synchronisation",
      "`Monitor.TryEnter` with a timeout sold as the deadlock fix — it turns a hang into a retry storm where close to half of every attempt is wasted, regardless of backoff length, once two threads settle into lockstep, and it leaves the missing lock order in place",
      "Translating `lock` to `SemaphoreSlim` mechanically: permits have no owner, so a locked method calling a locked method self-deadlocks on one thread with no contention",
      "Trusting a clean load test: the same buggy transfer deadlocks within a few hundred transfers over 2 accounts and only eventually over 1,000 — more accounts means fewer collisions and a longer survival, not correctness, so realism just makes it slower to reproduce",
    ],
  },
  "lock-free-structures": {
    signals: [
      "A `GetOrAdd` factory that opens something, and a connection or socket count that climbs faster than the key count after every deploy",
      "An in-memory counter that is quietly low under load — `d[k] = d[k] + 1` on a `ConcurrentDictionary` is a read, an add and a write racing against every other thread's, none of it atomic as a whole",
      "Flat mean, ugly p99, flat CPU on a path that takes a `lock`: the tail is the lock holder being descheduled, not the lock being slow",
      "A CAS loop whose body logs, allocates into shared state or sends something — every retry runs it again",
      "Reads of a rarely-written map going through one `lock`, so four threads read one at a time",
    ],
    tricks: [
      "Read \"lock-free\" as a progress guarantee: buy it for the tail, never for the mean — under real contention a CAS stack and `lock` + `Stack<T>` both serialize on the same cache line",
      "Store `Lazy<T>` values in a `ConcurrentDictionary`: several `Lazy` objects may be built, only the winner is ever `.Value`'d, so an expensive factory runs exactly once",
      "For hot counters keep a small class per key and `Interlocked.Increment` its field — the entry is written once and never replaced, so every increment after the first touches only that field, no dictionary operation at all",
      "Read-mostly data wants an immutable snapshot plus one `Volatile.Write`: readers synchronise on nothing at all — no CAS, no lock, just a read of whatever snapshot is current",
      "Before hand-rolling anything lock-free, partition — four structures with one owner each beat one structure with four threads, with no new algorithm",
    ],
    bugs: [
      "\"Every method is thread-safe\" never meant \"any two methods are\": `ContainsKey` then set, `TryGetValue` then write, `Count` then add are all windows",
      "\"Lock-free is faster.\" It is a progress guarantee; contended, both designs serialize on the same cache line",
      "Pooling nodes to save the 32-byte allocation brings ABA back in managed code — the GC removes address reuse, not re-publication",
      "`AddOrUpdate` is atomic but its update delegate is a CAS retry loop — it can run more than once per call whenever another thread's write lands mid-retry, so it must be pure",
      "`Lazy<T>` with `ExecutionAndPublication` caches a thrown factory exception forever; a transient failure poisons the entry permanently",
    ],
  },
  "parallelism-patterns": {
    signals: [
      "throughput per core *falls* as you add cores — total RPS barely moves for double the CPU, and the dashboard calls that flat line growth",
      "a `Parallel.For` that is slower than the `for` loop it replaced: the body is smaller than the delegate call wrapped around it",
      "three worker threads idle while one finishes — an equal split of index ranges over unequal work",
      "memory climbing linearly under load while CPU, latency and error rate stay flat, then an OOM kill and a clean restart",
      "a profiler that shows time inside `Interlocked`/`Monitor` rather than inside your code, on a workload with no obvious lock",
    ],
    tricks: [
      "hold TOTAL work fixed and vary the thread count — a speedup below 1.00 is proof of coherence, because Amdahl's floor is 1.00 and it cannot predict a slowdown",
      "partition the state, not just the loop: a local accumulator plus one `Interlocked.Add` per thread issues exactly one locked instruction total instead of one per increment — it beats a shared counter by construction, not by tuning",
      "pad per-thread slots to a cache line — `slots[id * 8]` for `long` — or your 'private' counters share a line and scale exactly like a shared one",
      "`Parallel.ForEach` over `Partitioner.Create(0, n)` ranges instead of `Parallel.For` per item — dozens of delegate calls instead of one per element, on identical work — and batch channel messages so a hand-off is paid once per batch, not once per item",
      "give every queue a capacity and a `FullMode`, export queue depth as a gauge, and alert on how long producers spend waiting on the bound",
    ],
    bugs: [
      "structuring a load test around N operations *per thread* instead of holding total work fixed — it hides negative scaling completely; a shape doing a quarter of the work looks identical to one that scales",
      "reading 'no shared variable' as 'no sharing' — four `long`s in a `long[]` are one cache line, and the hardware contends on lines, not variables",
      "adding consumers instead of bounding the queue: more consumers narrow the rate gap, they do not close it, and any positive gap sustained long enough fills any queue",
      "reading `Channel.CreateUnbounded` as 'no limit' rather than 'the limit is now the pod's memory, enforced by termination'",
      "assuming `await` yields — `WriteAsync` on an unbounded channel always completes synchronously, so the producer's loop never gives up its thread",
      "tuning the lock instead of removing the sharing: an uncontended lock's fast path is already a handful of instructions with no kernel call in it — the shared cache line behind it was always the problem, not the primitive",
    ],
  },

  // ─────────────────────── system design ───────────────────────

  "estimation": {
    signals: [
      "an interviewer asks \"how would you size this\" or \"what happens at 10x traffic\" before any design has been drawn",
      "a dashboard shows a healthy average latency while support tickets say otherwise — check whether timeouts are excluded from the histogram",
      "a connection-pool-exhaustion or thread-pool-starvation incident where `p99` climbed while CPU stayed low",
      "someone sizes capacity by dividing a daily total by 86,400 with no peak factor applied",
      "a request fans out to many parallel backend calls and the aggregate latency is worse than any single dependency's own numbers suggest",
    ],
    tricks: [
      "`L = λW` (Little's Law) turns a request rate + a hold time into the concurrency a pool, semaphore, or connection limit actually needs — exact for any stable queueing system, not an approximation",
      "state every input as a named assumption (DAU, peak factor, row size, replication factor) before doing arithmetic — that's the actual skill being graded, not the final number",
      "fan-out latency is a max() across parallel calls, not a mean — `P(≥1 slow) = 1 − (1 − p)^N` explains why a 1%-slow dependency makes a 100-way fan-out slow 63% of the time",
      "reach for a labelled published latency ladder (same-datacenter round trip ≈ 0.5ms, cross-region ≈ tens of ms) to turn a design choice into an actual number, never claim anything was measured",
      "utilization near 100% is not '20% more load than 80%' — wait time relative to service time is `ρ/(1−ρ))`, which is 4x at 80% and 99x at 99%",
    ],
    bugs: [
      "computing QPS as `daily total ÷ 86,400` with no peak-to-average ratio — the resulting number sizes for exactly the wrong hour",
      "quoting `p99 < 200ms` from a dashboard that computes it by averaging per-instance `p99` values — percentiles don't average, and a small number of grey-failure hosts can dominate the true merged tail",
      "trusting a flat average-latency graph while errors/timeouts climb — timed-out requests are often excluded from the latency histogram, so the average silently reflects only the fast survivors",
      "sizing a database or `HttpClient` connection pool off average query latency instead of its `p99` — under-sized for precisely the moments (a lock wait, a slow query) headroom is needed most",
      "forgetting the replication-factor and index-overhead multiplier when estimating storage — raw `rows × row size` is routinely 2-4x under the physically stored bytes",
    ],
  },
  "storage-engines": {
    signals: [
      "you are looking at this problem when write latency climbs slowly over months on a table with a random (`Guid`) key — page-split fragmentation, not raw volume",
      "a support ticket says reads are slow only for old / rarely-touched rows on an LSM-backed store — that is read amplification, not disk trouble",
      "a delete-heavy workload against Cassandra/ScyllaDB/RocksDB shows read latency creeping up while row count stays flat — tombstone accumulation",
      "`VACUUM` or autovacuum tuning comes up on a high-churn Postgres table — MVCC dead-tuple space amplification",
      "writes start getting throttled or rejected under sustained load on an LSM engine — compaction cannot keep pace, the backpressure valve",
    ],
    tricks: [
      "map every symptom onto one of the three amplifications — write, read, space — before proposing a fix",
      "for a B-tree, ask whether the write pattern is sequential (fine) or scattered (page splits, buffer-pool thrash)",
      "for an LSM, ask what compaction strategy is set — size-tiered trades space/read cost for write throughput, leveled is the reverse",
      "remember durability (`WAL` fsync'd) and 'the data page is on disk' are different claims — recovery replays the WAL, it doesn't imply every page write landed",
      "a sequential key generator (`NEWSEQUENTIALID()`, EF Core sequential-guid) is the standard fix for `Guid`-primary-key write degradation",
    ],
    bugs: [
      "claiming LSM engines reduce total write amplification — they usually raise it; what they buy is sequential I/O on the write path, paid back later by compaction",
      "treating a delete as a removal in an LSM — it is a tombstone, and it only disappears once compaction merges away everything it shadows",
      "assuming 'the write succeeded' means the data page is physically on disk — it means the WAL entry is fsynced; the page can still be dirty in the buffer pool",
      "sizing a B-tree node independent of the storage/VM page size — the node is page-sized because the unit of I/O and the unit of the structure have to match, same reasoning as a CPU cache line one layer down",
      "assuming a Bloom filter answering 'true' means the key is present — it only rules out the levels it says 'false' for; a 'true' still requires the actual read",
    ],
  },
  "transactions": {
    signals: [
      "reconciliation numbers don't add up between two reads in the same job — non-repeatable read under `READ COMMITTED`",
      "a uniqueness invariant is violated even though every write individually passed its check — write skew, not corruption",
      "switching a transaction to `SERIALIZABLE` causes a wave of `40001` (Postgres) or deadlock `1205` (SQL Server) errors under load",
      "Postgres table bloat and creeping query latency correlate with a long-running or forgotten open transaction, not with any one slow query",
      "a client got a commit ack, then a failover, then the write is gone — durability got conflated with replication",
    ],
    tricks: [
      "name the anomaly (dirty read / non-repeatable read / phantom / write skew), never just the isolation level",
      "default to `READ COMMITTED` backed by MVCC (Postgres, or SQL Server with RCSI) for typical CRUD; escalate only when you can name the specific invariant at risk",
      "prefer a unique constraint, `SELECT ... FOR UPDATE`, or an optimistic concurrency token (`rowversion`/ETag) over raising isolation for the whole transaction",
      "wrap serialization-failure retries (`40001`, deadlock `1205`) in Polly rather than hand-rolling backoff — it's expected traffic at that isolation level, not an incident",
      "when a write has to reach both a database and a queue/broker, reach for the outbox pattern, never a distributed transaction across the two",
    ],
    bugs: [
      "treating \"snapshot\" and \"serializable\" as the same guarantee across SQL Server, PostgreSQL, and Oracle — Oracle's `SERIALIZABLE` is actually snapshot isolation, not predicate locking",
      "assuming `READ COMMITTED` prevents non-repeatable reads or phantoms — it prevents dirty reads only",
      "assuming ACID durability means the write reached every replica — it means committed and recoverable via that node's own `fsync`ed log",
      "assuming snapshot isolation is safe from write skew because it prevents non-repeatable reads and phantoms — it doesn't touch write skew at all",
      "reaching for `WITH (NOLOCK)` on SQL Server as a free performance win without realizing it's `READ UNCOMMITTED`",
    ],
  },
  "replication": {
    signals: [
      "a user says their own save 'disappeared' seconds after they made it — a read-your-writes violation, not a bug in the write",
      "the replication-lag / LSN-gap metric climbs and does not recover — apply path can't keep up or the standby has a blocking query/lock",
      "a failover just happened and recent records are missing — expected data loss under async replication, not corruption",
      "two nodes are both writing and diverging — split-brain from an un-fenced old leader after a partition",
      "quorum writes start failing cluster-wide during a partial network event — not enough reachable replicas to satisfy `W`",
    ],
    tricks: [
      "separate network lag (received, durable on the follower) from apply lag (replayed into queryable state) — they are different numbers and vendors conflate them in their docs too",
      "fix read-your-writes and monotonic reads with routing or a log-position watermark carried by the client — never assume async replication gives either for free",
      "`R + W > N` guarantees overlap (you'll read back the latest ack'd write), not linearizability — you still need a total order on writes to know which returned version is newest",
      "fence leadership with a monotonically increasing epoch checked on the receiving side — a lease timeout alone stops the cluster from waiting, not the old leader from acting",
      "default to asynchronous leader-follower for read scaling and DR; only pay for synchronous or quorum writes when the business states an acknowledged write may never be lost",
    ],
    bugs: [
      "collapsing read-your-writes, monotonic reads, and consistent prefix into one thing called 'eventual consistency' — three separate guarantees, three separate mechanisms",
      "treating `R + W > N` as linearizability by itself — it gives overlap, not an agreed order on concurrent writes",
      "assuming synchronous replication protects the leader's own durability — it only guarantees a follower has the write; the leader still depends on its own `fsync`",
      "assuming a lease/election timeout is sufficient to prevent split-brain — it stops the cluster from waiting on a slow leader, not the leader from resuming writes when it wakes back up",
      "assuming a 'synchronous' or 'readable' secondary (e.g. SQL Server sync-commit) is caught up for reads — sync there means log-hardened, not applied; apply lag is a separate, still-live number",
    ],
  },
  "partitioning": {
    signals: [
      "a single table/collection is approaching a size or request-rate ceiling one machine cannot serve, and the proposed fix is 'split it across machines'",
      "a dashboard shows one node's CPU or queue depth diverging sharply from its siblings while the aggregate metric looks healthy — that is a hot partition, not a capacity problem",
      "an interviewer asks how you'd add a secondary index (query by a field that is not the partition key) to a sharded store",
      "a scaling event (adding or removing a node) causes cluster-wide latency to degrade, not just latency on the node that changed",
      "a monotonically increasing key (auto-increment ID, timestamp) is the partition/shard/row key and writes are concentrating in one place",
    ],
    tricks: [
      "consistent hashing bounds how many keys move on a membership change to roughly `1/N`; it does NOT balance load by itself — that's virtual nodes, a separate mechanism",
      "a hot key survives any number of added shards, because it is still one key owned by one shard — fix the key (salt/suffix + fan-in on read), not the shard count",
      "local secondary index = write stays atomic with the row, read pays with scatter-gather; global secondary index = read is a single targeted lookup, write pays with a cross-partition, usually eventually-consistent, update",
      "size partition count off BOTH storage (`data size / target partition size`) and throughput (`provisioned rate / per-partition ceiling`) — the throughput number can dominate even when the data would fit in fewer partitions",
      "range partitioning plus a monotonic key is a hotspot by construction — salt or hash-prefix the key, or accept hash partitioning and give up native range scans",
    ],
    bugs: [
      "saying 'consistent hashing balances load' — it bounds movement on a membership change, says nothing about load, and does nothing for a hot key regardless",
      "confusing SQL Server/PostgreSQL table partitioning (one instance, multiple filegroups) with sharding (multiple instances) — they share a keyword and nothing else",
      "adding more shards or nodes to fix one overloaded partition key — the hot key stays on exactly one shard no matter how many exist",
      "treating Raft/consensus as solving cross-partition transactions — it orders one replicated log, it does not make a two-partition operation atomic; that still needs 2PC (which blocks) or a saga",
      "assuming a Kafka partition assignment for a key is stable across a partition-count change — the default partitioner hashes modulo the CURRENT partition count, so resizing a live topic changes future routing for existing keys",
    ],
  },
  "consistency-models": {
    signals: [
      "a support ticket says \"I saved it and then it disappeared\" right after a failover or deploy — that's a stale read off a replica that hadn't caught up, not data loss",
      "someone on the team says \"we chose AP\" or \"CAP means pick two of three\" — stop and ask what happens when there's no partition, which is PACELC's else-branch and where most designs actually live",
      "a design needs to justify why a read can come from a follower/secondary/cache — that's the moment to name which consistency model you're actually promising, not just say \"eventually consistent\"",
      "an \"exactly-once\" claim appears in a design doc for a queue or webhook — that's the tell to ask for the idempotency mechanism underneath it",
      "two writes to the same key from different clients need reconciling (merge, last-write-wins, manual) — that's a quorum or ordering guarantee that wasn't strong enough for what you needed",
    ],
    tricks: [
      "reach for causal consistency + read-your-writes/monotonic-reads as the default; reserve linearizable for the few operations that truly need one global order (unique-ID allocation, balance check-then-write, leader election, idempotency-key lookup)",
      "quorum overlap is arithmetic, not intuition: `W + R > N` guarantees a read quorum shares a replica with the last write quorum — check this number before trusting a multi-replica read",
      "when asked to explain CAP, answer with PACELC instead: partition → availability or consistency; else (normal operation) → latency or consistency — the second branch is what an interviewer is usually probing for",
      "distinguish isolation (one transaction manager, one node) from consistency models (multiple copies converging) — serializable inside a DB says nothing about a write that also touches a queue or a second service",
      "when a vendor advertises a named consistency level (Cosmos DB's Session, etc.), read what it's scoped to (per session token, per partition) before assuming it's a global property",
    ],
    bugs: [
      "stating CAP as \"pick two of three\" — partition tolerance was never optional once there's more than one node; there's no meaningful all-three \"CA\" system",
      "treating \"exactly-once delivery\" as an achievable guarantee over a network rather than at-least-once + idempotent processing (or effectively-once inside one system's own transactional boundary)",
      "assuming a synchronous replica's reads can't be stale — `synchronous_commit` on PostgreSQL (and similar settings elsewhere) waits for the WAL to be received/flushed on the standby, not for it to be replayed, so the standby's own reads can still lag",
      "using wall-clock timestamps for last-write-wins conflict resolution without accounting for clock skew — \"latest by timestamp\" and \"actually happened last\" are different things across machines",
      "assuming Raft (or any leader-based consensus) makes follower reads linearizable for free — a stale follower read is still possible unless the leader confirms it's still current",
    ],
  },
  "consensus": {
    signals: [
      "you need multiple machines to agree on one value/log and survive a minority dying — that's consensus (Raft/Paxos); needing atomicity across systems you don't jointly deploy is a saga's job, not 2PC's",
      "a read right after a confirmed write comes back stale — check whether it hit a follower, or a leader that no longer holds a majority",
      "a distributed transaction is holding locks for minutes with no progress — suspect a 2PC coordinator that died between votes and decision",
      "a job ran twice, or two workers both think they own the same resource — suspect a lock lease that expired mid-pause with no fencing token behind it",
    ],
    tricks: [
      "quorum size is `floor(N/2) + 1`; odd `N` dominates the even size below it (`N=4` buys nothing over `N=3`) — place the majority so no single failure domain contains it",
      "a linearizable read needs a leadership check (read-index round or a bounded lease), not just a log lookup — Raft guarantees the log agrees, not that every read is fresh",
      "fencing tokens fix lease hazards that shortening the lease cannot — every write to the protected resource carries a monotonically increasing token, and the resource rejects anything lower than the highest it's seen",
      "outbox pattern for the dual-write problem: write the event row in the same local transaction as the state change, relay it separately, and make the consumer idempotent since delivery is at-least-once regardless",
    ],
    bugs: [
      "\"Raft/Paxos means every read is strongly consistent\" — false; the log is linearizable, reads are only linearizable if you add a leadership check on top",
      "\"two-phase commit is consensus\" — false; it has one fixed coordinator and no quorum, so a coordinator crash between PREPARE and the decision blocks every participant indefinitely",
      "\"CAP means pick two of three, always\" — CAP is a statement about behavior during an actual partition; PACELC is the frame for the normal-operation latency/consistency tradeoff you're always paying",
      "\"Kafka is exactly-once\" — Kafka's exactly-once semantics stop at its own boundary; a side effect a consumer performs outside Kafka is at-least-once and needs its own idempotency key",
      "\"a shorter lease TTL fixes the paused-holder problem\" — it only shrinks the window; a fencing token is the actual fix, because a pause can always outlast whatever TTL you pick",
    ],
  },
  "caching": {
    signals: [
      "DB CPU/connections spike in a sharp, repeating sawtooth aligned to a TTL interval",
      "one query pattern suddenly saturates the store, all identical, all at once, tied to a single popular key",
      "reads are fast but occasionally return a value one write behind, and it self-resolves after a while (a TTL, not a fix)",
      "the service falls over harder when the cache goes down than it would have with no cache at all",
      "one cache node/shard is pegged while the rest of the cluster sits idle",
    ],
    tricks: [
      "default to cache-aside; only pay for write-through when a read immediately after a write must never miss or see a stale value",
      "TTL jitter — randomize expiry by a percentage band so keys don't expire in lockstep",
      "stampede: request coalescing (one in-flight repopulation per key, e.g. `SET key:lock NX PX 5000`) or probabilistic early expiry ahead of the deadline",
      "negative-cache known-missing keys with a shorter TTL so absent-key traffic doesn't bypass the cache entirely",
      "on write: write the store, then delete the cache key (never update it in place) — repopulation happens on the next read",
    ],
    bugs: [
      "'delete the cache after writing the store' still races: a concurrent reader's stale read can land in the cache after the delete, stuck until TTL",
      "treating cache+store as one atomic write — they're two systems, and the reader gets to observe the state between them, same as any replica-consistency problem",
      "consistent hashing reduces which keys move on a node change; it does not spread one hot key's traffic across nodes",
      "LRU alone lets a single sequential scan evict an entire hot working set built over hours",
      "sizing a cache by average value size instead of concurrently-resident key count at your TTL",
    ],
  },
  "queues-and-streams": {
    signals: [
      "the interviewer or ticket says \"we need this to be exactly-once\" — that phrase is the signal to slow down and ask which boundary they mean",
      "consumer lag is climbing on one partition while every other partition in the group is flat — a poison message, not a broker outage",
      "a customer support ticket reports a duplicate charge or duplicate email that correlates with a deploy or consumer restart",
      "a service needs to write to its own database and notify other services about that write, and someone proposes wrapping both in one distributed transaction",
      "downstream has a gap in its event history that lines up with an incident window — retention outran a down consumer",
    ],
    tricks: [
      "separate \"delivery guarantee\" from \"processing guarantee\": the broker gives at-least-once, your handler supplies idempotency to get effectively-once",
      "dedup on a stable key — partition + offset, or a business idempotency key — checked with a unique constraint before the side effect runs, never after",
      "put the event row in the same local transaction as the business row (the outbox pattern) instead of reaching for a distributed transaction across two systems",
      "size partition count from the parallelism you want (consumer count), not from a throughput number, and provision headroom before the topic has data",
      "route the poison message to a DLQ and commit past it explicitly — Kafka will not do this for you the way a RabbitMQ DLX or Service Bus max-delivery-count does",
    ],
    bugs: [
      "saying \"Kafka gives exactly-once\" without naming the boundary — it covers producer-to-broker dedup and a consume-transform-produce step inside Kafka, not a write to an external database",
      "assuming ordering holds across a whole topic instead of scoping the claim to one partition, or forgetting that two related events need the same partition key to land in order at all",
      "treating a visibility timeout or `PeekLock` lease as a real mutual-exclusion lock — a lease can expire mid-processing without revoking the first holder's belief that it still owns the job",
      "believing a distributed transaction (2PC) across the database and the broker is the fix for the dual-write problem, instead of the outbox pattern most teams actually reach for",
      "expecting Kafka to dead-letter a failing record automatically the way RabbitMQ or Service Bus does, and getting a frozen partition instead",
    ],
  },
  "networking": {
    signals: [
      "p99 to a downstream is fine on average but spikes hard right after a deploy or failover of that downstream",
      "a service is intermittently throwing connection errors under load that look like a network outage but the downstream is healthy",
      "you're deciding L4 vs L7 for a load balancer, or whether HTTP/2 is worth it for a lossy client population",
      "someone wants retries at both the client (Polly) and the load balancer without checking what that combination does under a real outage",
      "small, frequent request/response pairs show extra fixed latency a raw ping between the same hosts doesn't",
    ],
    tricks: [
      "count round trips, not milliseconds — a cold connection pays TCP (1 RTT) + TLS 1.3 (1 RTT) before your request even goes out; a warm pooled one pays none of that",
      "separate application-level head-of-line blocking (what HTTP/2 fixed) from transport-level HOL blocking (what only HTTP/3/QUIC fixes, because TCP is one ordered byte stream)",
      "size a connection pool by dividing target concurrency by streams-per-connection: HTTP/1.1 needs ~1 connection per concurrent call, HTTP/2 needs `ceil(concurrency / max-concurrent-streams)`",
      "ask what an L4 balancer can't retry on — it has no concept of a failed request, only a connection, so retries/routing/TLS termination are all L7-only",
      "for `HttpClient`, reuse via `IHttpClientFactory` (pooled `SocketsHttpHandler`, handler rotated on a lifetime) — never `new HttpClient()` per call, never one static instance forever",
    ],
    bugs: [
      "\"HTTP/2 fixed head-of-line blocking\" — only the application-level kind; it reintroduced the transport-level kind by putting every stream on one ordered TCP connection",
      "\"TLS 1.3 0-RTT is just a free speedup\" — early data on a resumed connection is replayable, since it isn't bound to a fresh handshake; safe for idempotent requests only",
      "\"`using var client = new HttpClient()` per call is the safe, correct pattern\" — disposing it doesn't free the socket promptly, and it exhausts ephemeral ports under load",
      "\"a static `HttpClient` held forever is the fix\" — it never re-resolves DNS while its pooled connections stay healthy, so it can keep hitting a retired backend",
      "the parallel mistake to consistent-hashing folklore: treating L4's connection-hashing as request-level balancing — it isn't, it can't see requests at all",
    ],
  },
  "reliability": {
    signals: [
      "dashboards show latency/error-rate spiking on a near-periodic cadence, not smoothly — matches a fixed retry backoff schedule (a retry storm)",
      "one dependency slows down and completely unrelated endpoints start timing out too — shared thread/connection pool exhausted, no bulkhead",
      "a support ticket, not a metric: a customer was charged twice after reported flakiness — a non-idempotent write got retried",
      "a circuit breaker keeps cycling open → half-open → open instead of settling closed",
      "traffic graphs show the rate limiter correctly enforcing its per-window cap and the downstream still falls over — fixed-window boundary spike",
    ],
    tricks: [
      "shrink the timeout at every hop down the call chain — never a fixed number independent of what the caller above already budgeted",
      "retry with FULL jitter (`random(0, base * 2^attempt)`), cap total attempts and total elapsed time, and only when the call is idempotent",
      "put the circuit breaker inside the retry loop (so each attempt can trip it) and bound the half-open probe count, or the probe itself reproduces the storm",
      "give each dependency its own thread/connection pool (a bulkhead) instead of one shared pool across all of them",
      "shed load at the edge before it's queued; token bucket when bursts are legitimate, sliding window when they're not, never fixed window if the boundary spike matters",
    ],
    bugs: [
      "retrying a non-idempotent write \"to be safe\" — a timeout means the response was lost, not that the write didn't commit; that's a correctness bug, not resilience",
      "retries with no jitter, or a jitter window too narrow to decorrelate clients — synchronizes the outage instead of spreading it",
      "a circuit breaker with only open/closed and no half-open — either it never reopens, or it reopens at full traffic and reproduces the overload",
      "claiming \"exactly-once delivery\" as achieved — what's actually available is at-least-once plus idempotent processing, or effectively-once within one system's boundary",
      "a fixed-window rate limiter where the boundary-spike doubling actually matters to the callee's capacity",
    ],
  },

  // ───────────────────────── the network ─────────────────────────
  'network-basics': {
    signals: [
      'A retry loop that fixed the flakiness and started double-charging people — the duplicate came from your retry, not from the wire',
      'Latency that scales with the number of items while CPU, memory and link utilisation all stay flat: you are paying round trips, not bytes',
      '`SocketException` on connect, or a request that simply never returns, arriving as a steady background rate rather than as an outage',
      '"It works from my laptop but not from the pod" — a different path means different links, different caps and different rules',
      'Small requests succeed and large ones hang: something on the path caps how big a chunk may be, and the sender is not being told',
    ],
    tricks: [
      'Count round trips before optimising anything — one query instead of N, concurrent issue for independent calls, or move the logic to where the data is',
      'Give every cross-machine call a deadline, and make the operation behind it safe to run twice; an idempotency key beats any amount of retry tuning',
      'Read any header as fixed fields at known offsets — that is what lets you decode `/proc/net/*` or a capture without a parser',
      'When a symptom makes no sense at your layer, drop a layer: layering hides retries, size caps and silent drops by design',
      'Ask which of bandwidth, latency or round-trip count a proposed change actually moves — most "make it faster" tickets only move one of the three',
    ],
    bugs: [
      'Believing a timeout means the far end did nothing — silence covers "never arrived" and "done, reply lost" equally',
      'Treating packet loss as a fault to escalate rather than as normal operation on a best-effort network',
      'Assuming order: two packets to one destination can take different paths, so packet 2 can arrive before packet 1',
      'Thinking more bandwidth fixes a chatty protocol — thirty sequential queries cost thirty round trips at any link width',
      'Assuming duplicates come from the wire; they come overwhelmingly from whatever is retrying above it',
    ],
  },
  'osi-model': {
    signals: [
      'someone in the incident channel says "this is layer 4, not layer 7" — they have claimed nothing ever parsed an HTTP message, so no status code will ever exist',
      'the failure has a size threshold: small requests succeed, large ones hang forever — that is encapsulation overhead meeting a path MTU, not a code path',
      'an interface reports an MTU below 1500 (`/sys/class/net/<iface>/mtu`) — something is wrapping your packets in its own headers',
      'the exception already names the layer: `SocketError.HostNotFound` (layer 7, a name), `ConnectionRefused` (layer 4 answered no), `TimedOut` (dropped below)',
      'the interview opens with "walk me through what happens when you type a URL" — it is asking you to walk encapsulation down and back up',
    ],
    tricks: [
      'count the bytes out loud: an 88-byte GET becomes a 108-byte segment (+20 TCP), a 128-byte packet (+20 IPv4), a 146-byte frame (+14 Ethernet, +4 FCS)',
      'each header carries a small integer naming the layer above — EtherType `0x0800` → IPv4, IP protocol `6` → TCP / `17` → UDP / `1` → ICMP, destination port → the socket the four-tuple matches',
      'before asking what a box *should* do, ask what it can *see*: a switch cannot know an IP, a router cannot know a port, a layer-4 balancer cannot know a URL path',
      'say frame / packet / segment / datagram when the layer matters — captures, MTU arguments and fragmentation questions all turn on which one you mean',
      'header cost is per message, not per byte: 58 bytes of framing whatever you send, which is the whole mechanical argument for batching over chattiness',
    ],
    bugs: [
      'reading the seven layers as a description of running software — layers 5 and 6 have no separate implementation anywhere, and the stack that ships is four layers',
      'filing TLS at layer 6 and defending it — it runs over TCP, is negotiated by the application, and hands up a byte stream, which makes it application code',
      'assuming a TCP or IPv4 header is always 20 bytes — both carry a 4-bit length field and can reach 60, and Linux commonly negotiates the TCP timestamp option, taking the header to 32',
      'conflating a refused connection with a timeout — a refusal means something answered and said no, silence means the packet was dropped, and they have different fixes',
      'expecting a layer to act on something it cannot see, e.g. asking a layer-4 load balancer to route on a URL path, or a switch to care about an IP address',
    ],
  },
  'link-layer': {
    signals: [
      'small requests succeed and large ones hang with no error at all → something on the path carries less than 1500 bytes',
      'a failed-over virtual IP where some clients recover at once and others keep timing out → stale ARP entries still pointing at the old MAC',
      '`ip neigh` showing `FAILED` or `INCOMPLETE` for your own gateway → the failure is below IP, so routes and firewalls are the wrong place to be standing',
      '`SocketError.TimedOut` rather than `ConnectionRefused` on a LAN → frames are reaching a MAC address that no longer answers',
      'a container or overlay interface reporting an MTU of 1400 instead of 1500 → something is encapsulating your packets and taking headroom',
    ],
    tricks: [
      'the routing table answers exactly one question for layer 2 — which next hop — and ARP resolves *that*, never the destination',
      'read the interface MTU at both ends and inside every tunnel between them before touching a line of application config',
      '`ip neigh show` first on any LAN problem: it separates "nothing on this segment answered" from every layer above IP',
      'an entry for an off-subnet address in your ARP cache is a signal, not noise — your cache should only ever hold your own subnets',
      'MSS is MTU − 20 − 20 for plain IPv4 and TCP, so a 1400-byte MTU means 1360 of payload, not 1460',
    ],
    bugs: [
      'believing the destination MAC in your frame is the destination server\'s — off-subnet it is the gateway\'s, and the server\'s MAC is never known to you at all',
      'reading a MAC address as if it carried location: the OUI names a vendor, never a place, which is precisely why IP had to exist on top of it',
      'expecting a switch to contain broadcasts — it floods `ff:ff:ff:ff:ff:ff` out every port by definition; only a router or a VLAN boundary divides a broadcast domain',
      'treating a gratuitous ARP as a guarantee: it is one best-effort broadcast, and anything that missed it keeps the old mapping until its own entry is re-probed',
      'blocking all ICMP as a security measure and then wondering why large payloads hang — that is the feedback channel path MTU discovery runs on',
    ],
  },
  'ip-and-routing': {
    signals: [
      '"The address is right and the security group is open, but the packet never arrives" — read the routing table before anything else; a more specific prefix beat the default route and handed the packet to a neighbour that is not there.',
      'Small requests succeed, large responses hang until the client\'s timeout — path MTU black hole: something on the path has an MTU below 1500 and the ICMP type 3 code 4 that would shrink the sender is being dropped.',
      'The first request after an idle period throws `SocketException` or `HttpRequestException` and the retry succeeds — a NAT translation row expired underneath a pooled connection.',
      'The server logs a source port the client never chose, or one source address for an entire fleet — something on the path is NATing, and any allowlist or per-IP rate limit built on it is wrong.',
      '`169.254.x.x` on an interface — DHCP failed and the host self-assigned a link-local address; nothing routable will work until that is fixed.',
    ],
    tricks: [
      'Network is `address AND mask`; broadcast is `address OR NOT mask`; hosts are `2^(32 - prefix) - 2`. Show the AND in binary — the `/22` boundary sits six bits into the third octet, not on a dot.',
      '`/proc/net/route` hex fields are little-endian: read the byte pairs right to left, so `010200C0` is `192.0.2.1` and `00FFFFFF` is `255.255.255.0`.',
      'Forwarding is longest-prefix match, so `0.0.0.0/0` only wins when nothing else matches — when routing looks wrong, hunt for the route that is *more specific*, not the one that is missing.',
      'Reach for `IPNetwork.Parse("10.1.0.0/22")` and `.Contains(...)` (.NET 8+) before hand-rolling masks; if you do hand-roll, guard `prefix == 0`, because C# masks a 32-bit shift count to 5 bits and `<< 32` is a no-op.',
      'Set `SocketsHttpHandler.PooledConnectionIdleTimeout` below whatever NAT sits in front of you instead of retrying past the symptom.',
    ],
    bugs: [
      '"NAT is a firewall." It evaluates no rules — blocking unsolicited inbound is a side effect of there being no table row, and every outbound flow opens a bidirectional path for as long as its row lives.',
      '"The router rewrites the addresses to get the packet to the next hop." It rebuilds the L2 header and decrements the TTL; the L3 addresses travel end to end untouched. NAT is the exception, not the rule.',
      'Dropping all ICMP "for security" — that kills type 3 code 4 and with it path MTU discovery; on IPv6 it also kills NDP, which disables the protocol rather than hardening it.',
      'Reading a prefix as if the boundary landed on an octet dot, so `10.1.2.37/22` gets called `10.1.2.0` instead of `10.1.0.0`.',
      'Treating an empty traceroute hop as a drop, and TTL as a duration. The hop is a router declining or rate-limiting Time Exceeded, and TTL counts hops — the name is inherited from a design that counted seconds.',
    ],
  },
  'ports-and-sockets': {
    signals: [
      '"works on my laptop, connection refused from the pod" → a listener bound to `127.0.0.1` instead of `0.0.0.0`',
      '`SocketException` with `SocketError.AddressNotAvailable` ("Cannot assign requested address") → ephemeral ports gone, on the *client* side',
      'clients report connections that establish then go silent, while your request count sits below what they sent → a full accept queue, not a network fault',
      'a growing pile of `TIME-WAIT` rows whose local port is ephemeral and whose peer is one dependency → a connection-per-call client',
      'hundreds of `ESTAB` rows sharing one local `address:port` → normal; that is exactly what a working server looks like',
    ],
    tricks: [
      'the demux key is `(protocol, src addr, src port, dst addr, dst port)` — the server port is in every key, so it distinguishes nothing',
      'RST means something answered and said no; silence until a timeout means something dropped it — different diagnoses, never conflate them',
      'bind port 0 and read `LocalEndPoint` back: the only race-free way to get a port the kernel agrees is free',
      'the fix for port exhaustion is connection reuse — `IHttpClientFactory`, a long-lived `SocketsHttpHandler`, the ADO.NET pool — not a kernel knob',
      '`SO_REUSEADDR` on Linux = bind over `TIME_WAIT`; `SO_REUSEPORT` = several sockets on one endpoint; Windows means something else again — name the platform every time',
    ],
    bugs: [
      'thinking a busy server runs out of ports — it runs out of file descriptors; the *client* is what spends a finite range',
      '`SO_LINGER` with a zero timeout to "fix" `TIME_WAIT` — it replaces the FIN with an RST and discards unsent data',
      'lowering `net.ipv4.tcp_fin_timeout` to shorten `TIME_WAIT` — that knob governs `FIN_WAIT_2` and does nothing here',
      'reading `listen(backlog)` as a cap on concurrent connections rather than on completed-but-un-accepted ones',
      'assuming `accept()` returns connections in the order the clients dialled — it is handshake-completion order',
    ],
  },
  'tcp-and-udp': {
    signals: [
      'Sockets piling up in `CLOSE_WAIT` that never clear, or "too many open files" on a service under ordinary load',
      'Deserialization errors that correlate with message *size* or with load on a protocol nobody has touched in a year — the framing bug',
      '`SocketException` with `SocketError.ConnectionReset` mid-stream, or `ConnectionRefused` on connect: something answered and said no',
      'An established connection that simply stops moving data with no loss on the path — somebody is advertising a zero window',
      'A pooled connection that fails on its first use *after* an idle period rather than during it',
    ],
    tricks: [
      'Frame it yourself: a 4-byte big-endian length prefix plus `ReadExactly`/`ReadAtLeast`, or a delimiter you actually escape',
      'Read refused versus timed out as two different diagnoses — `RST` means something is there and said no; silence means the packet was dropped',
      'Census your own states from inside the process: `IPGlobalProperties.GetActiveTcpConnections()` grouped by `TcpState`',
      '`Socket.NoDelay = true` for small latency-sensitive writes — better still, stop splitting one logical message across several `Send` calls',
      'Reach for UDP only when retransmission is worthless (media), the exchange fits one datagram and you already retry (DNS), or you need multicast',
    ],
    bugs: [
      '"One `Send` equals one `Receive`" — TCP has no message boundaries, and two writes may arrive as one read or as three',
      'Treating a short read as a disconnect: only a read that returns `0` is end of stream',
      'Blaming the peer for a pile of `CLOSE_WAIT` — the kernel already ACKed their `FIN`, it is your code that never called `Close`',
      '"UDP is TCP without the handshake" — it also has no ordering, no retransmission, no flow control and no congestion control',
      'Sizing a UDP receive buffer to the typical datagram: the overflow is discarded, not queued, and on Linux silently',
    ],
  },
  'dns': {
    signals: [
      'After a cutover, a flat, non-decaying floor of traffic keeps arriving at the retired backend — a cache tail *decays*, a pinned `HttpClient` connection does not',
      '`SocketException` with `SocketError.HostNotFound` — you never reached the network at all; `ConnectionRefused` and `TimedOut` on the same call mean the name resolved fine',
      'A ticket saying "I created the record and it still says it does not exist" — that is negative caching, governed by the zone\'s `SOA` `MINIMUM`',
      '"It resolves from my laptop but not from the pod" — a `search` list, `options ndots:5`, or a name that only exists inside the cluster',
      'Small answers resolve and large ones fail — UDP 53 is permitted through the firewall and TCP 53 is not, so the `TC` fallback dies',
    ],
    tricks: [
      'Lower the TTL and wait out the **old** TTL *before* the change — the new, shorter value cannot reach a cache that is not asking yet',
      '`dig @server name` bypasses every cache in the chain; compare it against `dig name` to split a broken resolver from a broken zone, and use `dig +trace` to walk the delegation from the root',
      'Ask twice and watch the TTL column — a number that went *down* was served from cache, one back at its full value was just fetched',
      'Reach for `IHttpClientFactory`, or set `SocketsHttpHandler.PooledConnectionLifetime` by hand; its default is `Timeout.InfiniteTimeSpan`, so a busy pooled connection never re-resolves',
      'Write the trailing dot: `api.internal.` is absolute, skips the search list entirely, and turns several queries into one',
    ],
    bugs: [
      '"DNS propagation takes 48 hours" — nothing propagates. Four independent caches each expire on their own countdown, started when each of them last asked',
      '"Always use a singleton `HttpClient`" — that fixes ephemeral-port exhaustion and pins DNS forever. Both halves of the trap have to be stated, and `IHttpClientFactory` is what closes both',
      '"DNS is UDP" — UDP 53 first, TCP 53 on the `TC` flag or by request, `AXFR` always TCP, DoT on 853 and DoH on 443',
      'A `CNAME` at a zone apex, or beside a `TXT`/`MX` — a `CNAME` cannot coexist with any other record at the same name, which is why apex-behind-CDN needs `ALIAS`/`ANAME`/flattening',
      'Treating `NXDOMAIN`, `SERVFAIL` and a timeout as one failure — they eliminate completely different things, and only the first two prove anything answered',
    ],
  },
  'application-protocols': {
    signals: [
      'A `502`, `503` or `504` in a user\'s screenshot with nothing matching in your own logs — the status was manufactured by a hop in front of you, and which of the three it is tells you which hop failed how',
      'A hand-rolled socket reader that parses fine on loopback and corrupts messages the first time it crosses a real network: one read was assumed to be one message',
      '`EnsureSuccessStatusCode` throwing an `HttpRequestException` that has collapsed `401`, `404`, `429` and `503` into one thing your retry policy cannot reason about',
      'A `Set-Cookie` from one call turning up on another caller\'s request — a cookie container living on a handler that `IHttpClientFactory` shares by design',
      'Browsers still hitting a URL you retired weeks ago, because a `301` told them never to ask again',
    ],
    tricks: [
      'Read the message as bytes: request line, header lines, one empty line, then a body framed by `Content-Length` or `Transfer-Encoding: chunked` — those are the only two framings there are',
      'Triage on the first digit before anything else: `4xx` means stop, `5xx` and `429` mean maybe, and *maybe* still depends on whether the method is idempotent',
      'Pick a redirect with two questions: is it permanent (`301`/`308`) or not (`302`/`307`), and must the method survive (`307`/`308` only)',
      'Pin the public hostname in configuration instead of building absolute URLs from the incoming `Host` header, which is attacker-controlled request data',
      'Rule out Server-Sent Events before reaching for WebSockets, and turn `UseCookies` off on any handler that `IHttpClientFactory` shares',
    ],
    bugs: [
      '"`401` means you are not allowed" — it means *unauthenticated* and must carry `WWW-Authenticate`; `403` is the one where retrying with the same identity is pointless by definition',
      '"`502` means the server is down" — it means the hop in front could not get a usable response out of it; a `504` means it reached you and gave up waiting, and your process is very likely still running that request',
      '"One `Send` is one message" — TCP has no message boundaries, which is exactly why HTTP delimits headers with a blank line and frames the body with a length or with chunks',
      '"A message with both `Content-Length` and `Transfer-Encoding` just picks one" — two devices on the path picking differently is request smuggling; reject the message rather than resolve it',
      '"The port number is part of the protocol" — it is a convention: HTTPS on 8443 is still HTTPS, and an open 443 proves only that something is listening',
    ],
  },
  'network-troubleshooting': {
    signals: [
      '`SocketError.ConnectionRefused` in the log — an `RST` answered your `SYN`, so a live kernel was reached and the network is not the problem',
      '`SocketError.TimedOut`, or a call that simply never returns — silence, so the packet was dropped: a security group, a routing hole, or the return path',
      'a `502` from the gateway while your own service\'s request log stays empty — every rung passed to the proxy and none past it',
      'small requests succeed and anything with a large body hangs on an already-established connection — a *size* threshold, which only MTU black-holing produces',
      'works from your laptop, fails from the pod — two resolvers, two route tables, two rule sets; it was never the same test',
    ],
    tricks: [
      'climb in order — name → route → port → TLS → payload — and remember a failure eliminates every rung above it',
      'resolve once, then test the **address**; `dig` versus `getent hosts` separates the zone from `/etc/hosts` and the name service switch',
      '`nc -vz host port` answers rung 3 and nothing else — `curl -v` mixes rungs 3, 4 and 5 and hides which one broke',
      '`ss -ltn` on the server settles `0.0.0.0` versus `127.0.0.1`; a persistently non-zero `Recv-Q` on a LISTEN row is a starved accept loop, not a network fault',
      'when the two ends disagree, `tcpdump` on both: no `SYN` here means it never left, a `SYN` there with no answer means the return path is blocked',
    ],
    bugs: [
      'treating refused and timed out as one failure — they eliminate opposite halves of the search space',
      'concluding anything from a failed `ping`: ICMP is filtered by default in most clouds, and dropping it is also what creates the MTU black hole',
      'running the whole checklist from your laptop when the pod is the thing that is failing',
      'an `HttpClient.Timeout` shorter than the connect timeout — you get a `TaskCanceledException` that names no rung at all',
      '`openssl s_client` without `-servername`: you get whatever default certificate the server has and then debug a name mismatch you caused',
    ],
  },

  "design-drills": {
    signals: [
      "the interviewer says \"design X\" and starts sketching before you've asked a single question — that's the tell to slow down, not speed up",
      "you're asked what happens \"at ten times the load\" — they're checking whether your bottleneck analysis was ever real or just decorative",
      "you're asked to defend a specific number choice (why 100 followers not 10,000; why 6 chars not 4) — they want to see the arithmetic, not the answer",
      "you're asked \"what would you page on\" — this is the how-it-fails section, graded on whether your failure modes map to real symptoms",
      "the follow-up abandons your first design entirely (\"now assume it's global\") — they're testing whether your defaults were principled or accidental",
    ],
    tricks: [
      "always ask read:write ratio, consistency requirement, and scale before drawing a single box — it changes which half of the design matters",
      "work every capacity number from a stated assumption, out loud — `daily actives × action/day / 86,400` for QPS, `rows × row-size` for storage, and say which number you assumed",
      "draw the write path and read path as two separate diagrams — in every one of these four drills they're asymmetric, and that asymmetry is the design",
      "when defending a tradeoff, finish with \"and here's what would make me switch\" — a bare default with no exit condition reads as dogma, not judgment",
      "state delivery/consistency guarantees precisely: at-least-once + idempotency key, not \"exactly-once\"; per-conversation order, not global order — precise language is itself a signal",
    ],
    bugs: [
      "jumping straight to the sketch and skipping clarify first — the single most common way a strong candidate loses points early",
      "claiming \"exactly-once delivery\" as a feature you'll just enable, rather than at-least-once plus idempotent processing",
      "sizing a design around storage bytes when the real constraint was QPS (or vice versa) — always work both and see which one actually binds",
      "treating fan-out-on-write as strictly better because reads are cheap — ignoring the celebrity write-amplification blowup it creates",
      "assuming a local atomic (`Interlocked.Increment`, an in-process counter) stays correct once there's more than one instance — it silently multiplies the effective limit by the instance count",
    ],
  },
};

export function cheatFor(slug: string): Cheat | undefined {
  return cheats[slug];
}
