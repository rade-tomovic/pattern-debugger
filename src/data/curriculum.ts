/**
 * The single source of truth for the site's content tree.
 * Nav, landing page, prev/next links, cross-links, cheat-sheet cards, and the
 * study plan are all generated from this file. Every MDX page corresponds to
 * an entry here; every internal cross-link resolves against it (unknown slugs
 * throw at build time — see the validation block at the bottom).
 *
 * Shaped by the three-lens curriculum audit of 2026-08-27 (frequency,
 * pedagogy, IA) — see git history for the findings.
 */

export type Difficulty = 'easy' | 'medium' | 'hard';
export type Track = 'core' | 'stretch';

export type SectionId =
  | 'foundations'
  | 'core'
  | 'structures'
  | 'advanced'
  | 'machine'
  | 'memory'
  | 'concurrency'
  | 'network'
  | 'system-design'
  | 'deep-dives'
  | 'reference';

export interface Problem {
  /** URL segment: {section.base}/{topic.slug}/{slug}/ */
  slug: string;
  title: string;
  /** LeetCode number(s), when they exist */
  lc?: number | number[];
  difficulty: Difficulty;
  /** One-liner shown in listings */
  summary: string;
  /** core = the first pass through a topic · stretch = extended track / capstones */
  track?: Track;
  /** id of the interactive visualizer island on this page, if any */
  visualizer?: string;
  /** cross-links to other problems, as "topic-slug/problem-slug" refs */
  related?: string[];
}

export interface Connection {
  /** topic slug */
  to: string;
  /** why the reader should follow this link — rendered in the panel */
  reason: string;
}

export interface Topic {
  slug: string;
  title: string;
  /** short mono label used in nav chips, e.g. 'two_pointers' */
  tab: string;
  section: SectionId;
  summary: string;
  problems: Problem[];
  connections: Connection[];
}

export interface Section {
  id: SectionId;
  title: string;
  /** base URL path for topics in this section */
  base: string;
  blurb: string;
}

export interface StudyDay {
  day: number;
  track: 'core' | 'extended' | 'systems' | 'network' | 'design';
  focus: string;
  /** topic slugs covered this day */
  topics: string[];
}

export const sections: Section[] = [
  {
    id: 'foundations',
    title: 'Foundations',
    base: '/foundations',
    blurb: 'The ground everything else stands on — complexity, the C# toolkit, loop mechanics, sorting.',
  },
  {
    id: 'core',
    title: 'Core Patterns',
    base: '/patterns',
    blurb: 'The eight patterns behind the majority of interview problems. Learn these in order.',
  },
  {
    id: 'structures',
    title: 'Data Structure Patterns',
    base: '/patterns',
    blurb: 'Stack, queue, heap, and trie — the structures that unlock their own problem families.',
  },
  {
    id: 'advanced',
    title: 'Advanced Patterns',
    base: '/patterns',
    blurb: 'Backtracking, DP, graph algorithms, greedy, intervals, bits — the rest of the 90%.',
  },
  {
    id: 'machine',
    title: 'How a Computer Runs Code',
    base: '/systems',
    blurb:
      'The primer, assuming nothing: bits and addresses, what the OS and a thread actually are, and how a CPU executes one instruction.',
  },
  {
    id: 'memory',
    title: 'Memory & the Machine',
    base: '/systems',
    blurb:
      'Stack, heap, virtual memory, caches, the pipeline, the collector, and the JIT — where your program\'s time and space actually go.',
  },
  {
    id: 'concurrency',
    title: 'Concurrency & Locking',
    base: '/systems',
    blurb:
      'The memory model, atomics, what a lock is made of, the hazards, lock-free structures, and parallelism that actually scales.',
  },
  {
    id: 'network',
    title: 'How the Network Works',
    base: '/networking',
    blurb:
      'The other half of the machine, assuming nothing: packets and layers, Ethernet and IP, what a port actually is, TCP versus UDP, DNS, the protocols on top, and what to run when none of it works.',
  },
  {
    id: 'system-design',
    title: 'System Design',
    base: '/system-design',
    blurb:
      'The distributed layer: storage engines, transactions, replication, consistency, consensus, caching, queues and the drills that put them together.',
  },
  {
    id: 'deep-dives',
    title: 'Deep Dives',
    base: '/deep-dives',
    blurb: 'Cross-pattern analyses: tradeoffs, recognition, and how the patterns fit together.',
  },
  {
    id: 'reference',
    title: 'Reference',
    base: '/reference',
    blurb: 'The study plan and the master cheat sheet.',
  },
];

export const topics: Topic[] = [
  // ───────────────────────── foundations ─────────────────────────
  {
    slug: 'big-o',
    title: 'Big-O & Data Structure Costs',
    tab: 'big_o',
    section: 'foundations',
    summary:
      'What O(n) actually buys you, the cost table for every .NET collection, amortized analysis, and how to state complexity in an interview.',
    problems: [],
    connections: [
      { to: 'csharp-toolkit', reason: 'the cost table is about exactly these collections — meet them properly next' },
      { to: 'sorting', reason: 'the O(n log n) comparison floor explains half of all optimal complexities' },
      { to: 'hashmap', reason: 'the O(1) lookup is the single most-used complexity upgrade in interviews' },
      { to: 'memory-hierarchy', reason: 'the constant factor Big-O throws away — two O(n) loops, measurably different' },
    ],
  },
  {
    slug: 'csharp-toolkit',
    title: 'The C# Interview Toolkit',
    tab: 'csharp_toolkit',
    section: 'foundations',
    summary:
      'Dictionary, HashSet, List, StringBuilder, PriorityQueue, Stack/Queue, LinkedList, spans, tuples, pattern matching, collection expressions — the modern C# you reach for under pressure.',
    problems: [],
    connections: [
      { to: 'big-o', reason: 'every API here has a cost — the Big-O page is its price list' },
      { to: 'hashmap', reason: 'Dictionary and HashSet in anger — the first pattern that lives on them' },
      { to: 'heap-top-k', reason: 'PriorityQueue<TElement, TPriority> gets its own pattern family' },
      { to: 'stack-queue', reason: 'Stack<T> and Queue<T> as problem-solving tools, not just collections' },
      { to: 'stack-and-heap', reason: 'what these collections cost in memory, and where their contents actually live' },
    ],
  },
  {
    slug: 'loops-and-counting',
    title: 'Loops & Counting: Tips and Tricks',
    tab: 'loops',
    section: 'foundations',
    summary:
      'for vs while, < vs <=, middle-element handling, ceiling division, negative modulo, direction arrays, mirror iteration, off-by-one defense, and the 0-1-2-element debugging technique.',
    problems: [],
    connections: [
      { to: 'two-pointers', reason: 'mirror iteration is the two-pointer pattern in miniature' },
      { to: 'binary-search', reason: 'the L = mid infinite loop and the < vs <= decision live here' },
      { to: 'bfs-dfs', reason: 'direction arrays replace four copy-pasted if-blocks in every grid problem' },
      { to: 'array-techniques', reason: 'circular indexing and in-place iteration rules power the array toolbox' },
    ],
  },
  {
    slug: 'sorting',
    title: 'Sorting for Interviews',
    tab: 'sorting',
    section: 'foundations',
    summary:
      'What you must know about sorting without implementing it blind: comparison-sort floor, merge/quick mechanics, quickselect, counting & bucket sort, and when sorting is the setup move.',
    problems: [],
    connections: [
      { to: 'two-pointers', reason: 'sorting first is often the move that unlocks opposite-end pointers' },
      { to: 'binary-search', reason: 'sorted input is the license binary search runs on' },
      { to: 'intervals', reason: 'every interval problem starts with "sort by start" (or end — and the choice matters)' },
      { to: 'heap-top-k', reason: 'quickselect vs heap vs full sort is the standard top-K tradeoff conversation' },
    ],
  },

  // ───────────────────────── core patterns ─────────────────────────
  {
    slug: 'two-pointers',
    title: 'Two Pointers',
    tab: 'two_pointers',
    section: 'core',
    summary:
      'Two indices that converge, chase, or expand — turning O(n²) pair scans into O(n) walks on sorted or structured data.',
    connections: [
      { to: 'hashmap', reason: 'the other Two Sum tool — O(n) space buys unsorted input and original indices' },
      { to: 'two-sum-family', reason: 'the full pointers-vs-hashmap tradeoff analysis, one page, both tools' },
      { to: 'sliding-window', reason: 'a window is two same-direction pointers whose gap is the answer' },
      { to: 'linked-lists', reason: 'fast/slow pointers are this pattern walking a list instead of an array' },
      { to: 'array-techniques', reason: 'Dutch National Flag is read/write pointers upgraded to three regions' },
    ],
    problems: [
      {
        slug: 'valid-palindrome',
        title: 'Valid Palindrome',
        lc: 125,
        difficulty: 'easy',
        summary: 'Mirror pointers from both ends, skipping non-alphanumerics.',
        visualizer: 'two-pointers-palindrome',
      },
      {
        slug: 'two-sum-ii',
        title: 'Two Sum II (Sorted Input)',
        lc: 167,
        difficulty: 'easy',
        summary: 'Opposite-end pointers; each comparison discards a whole set of pairs.',
        visualizer: 'two-pointers-sum',
        related: ['hashmap/two-sum'],
      },
      {
        slug: 'remove-duplicates',
        title: 'Remove Duplicates from Sorted Array',
        lc: 26,
        difficulty: 'easy',
        summary: 'Same-direction read/write pointers — the in-place compaction template.',
        related: ['array-techniques/move-zeroes'],
      },
      {
        slug: 'longest-palindromic-substring',
        title: 'Longest Palindromic Substring',
        lc: 5,
        difficulty: 'medium',
        summary: 'Expand from every center — the third pointer shape, on a top-five interview question.',
      },
      {
        slug: 'container-with-most-water',
        title: 'Container With Most Water',
        lc: 11,
        difficulty: 'medium',
        summary: 'Converging pointers with a greedy proof: always move the shorter wall.',
      },
      {
        slug: 'three-sum',
        title: '3Sum',
        lc: 15,
        difficulty: 'medium',
        summary: 'Sort + outer loop + two pointers, with duplicate-skipping discipline.',
      },
      {
        slug: 'trapping-rain-water',
        title: 'Trapping Rain Water',
        lc: 42,
        difficulty: 'hard',
        track: 'stretch',
        summary: 'The capstone: converging pointers carrying maxLeft/maxRight invariants.',
      },
    ],
  },
  {
    slug: 'hashmap',
    title: 'HashMap & Frequency Counting',
    tab: 'hashmap',
    section: 'core',
    summary:
      'Trade O(n) space for O(1) lookups: value→index, value→count, prefixSum→count, and HashSet membership — the "key is what I need" mindset.',
    connections: [
      { to: 'two-pointers', reason: 'the O(1)-space alternative when input is sorted (or sortable)' },
      { to: 'two-sum-family', reason: 'when to prefer which Two Sum tool — the full tradeoff analysis' },
      { to: 'sliding-window', reason: 'the window\'s memory is a dictionary — frequency counts drive shrinking' },
      { to: 'array-techniques', reason: 'prefixSum→count is prefix sums (taught there) plus this pattern' },
      { to: 'linked-lists', reason: 'LRU Cache is a dictionary bolted onto a doubly linked list' },
      { to: 'bit-manipulation', reason: 'HashSet vs XOR: the same "find the loner" problem, O(n) vs O(1) space' },
    ],
    problems: [
      {
        slug: 'two-sum',
        title: 'Two Sum (Unsorted)',
        lc: 1,
        difficulty: 'easy',
        summary: 'The dictionary key answers: "who is waiting for this number?"',
        visualizer: 'hashmap-two-sum',
        related: ['two-pointers/two-sum-ii'],
      },
      {
        slug: 'valid-anagram',
        title: 'Valid Anagram',
        lc: 242,
        difficulty: 'easy',
        summary: 'value→count frequency table; int[26] as the specialized dictionary.',
      },
      {
        slug: 'group-anagrams',
        title: 'Group Anagrams',
        lc: 49,
        difficulty: 'medium',
        summary: 'Canonical form as the dictionary key — group by signature.',
      },
      {
        slug: 'longest-consecutive-sequence',
        title: 'Longest Consecutive Sequence',
        lc: 128,
        difficulty: 'medium',
        summary: 'HashSet membership + only start counting at sequence starts.',
      },
      {
        slug: 'subarray-sum-equals-k',
        title: 'Subarray Sum Equals K',
        lc: 560,
        difficulty: 'medium',
        summary: 'prefixSum→count: the hashmap remembers every running total seen so far.',
        related: ['array-techniques/range-sum-query'],
      },
      {
        slug: 'insert-delete-getrandom',
        title: 'Insert Delete GetRandom O(1)',
        lc: 380,
        difficulty: 'medium',
        track: 'stretch',
        summary: 'The dict+list swap-remove idiom — a design question in disguise.',
        related: ['linked-lists/lru-cache'],
      },
    ],
  },
  {
    slug: 'sliding-window',
    title: 'Sliding Window',
    tab: 'sliding_window',
    section: 'core',
    summary:
      'A window [L..R] that expands right and shrinks left over contiguous data — every element enters and leaves once, so O(n).',
    connections: [
      { to: 'two-pointers', reason: 'same two-index machinery — but the window between them is the answer' },
      { to: 'hashmap', reason: 'window state lives in a HashSet or frequency dictionary' },
      { to: 'array-techniques', reason: 'window vs prefix sum: the two ways to own "subarray" questions' },
    ],
    problems: [
      {
        slug: 'max-sum-subarray-of-size-k',
        title: 'Max Sum Subarray of Size K',
        difficulty: 'easy',
        summary: 'Fixed window: add the entering element, remove the leaving one.',
      },
      {
        slug: 'longest-substring-without-repeating',
        title: 'Longest Substring Without Repeating Characters',
        lc: 3,
        difficulty: 'medium',
        summary: 'Variable window, shrink while invalid; the HashSet is the window\'s memory.',
        visualizer: 'sliding-window-unique',
      },
      {
        slug: 'longest-repeating-character-replacement',
        title: 'Longest Repeating Character Replacement',
        lc: 424,
        difficulty: 'medium',
        summary: 'Window validity from a frequency count: len − maxFreq ≤ k.',
      },
      {
        slug: 'minimum-window-substring',
        title: 'Minimum Window Substring',
        lc: 76,
        difficulty: 'hard',
        summary: 'Shortest valid window: shrink while valid, record during shrinking.',
      },
    ],
  },
  {
    slug: 'binary-search',
    title: 'Binary Search',
    tab: 'binary_search',
    section: 'core',
    summary:
      'Not "find a value" — find where a condition flips. Exact match, boundary finding, and binary search on the answer space.',
    connections: [
      { to: 'loops-and-counting', reason: 'the < vs <= decision and the L = mid infinite-loop trap, dissected' },
      { to: 'sorting', reason: 'sorted input is the precondition — sometimes sorting first IS the algorithm' },
      { to: 'two-pointers', reason: 'converging L/R indices, same discipline: every step discards half or one' },
      { to: 'greedy', reason: 'binary search on the answer needs a monotonic CanDo(k) — usually a greedy check' },
    ],
    problems: [
      {
        slug: 'classic-binary-search',
        title: 'Classic Binary Search',
        lc: 704,
        difficulty: 'easy',
        summary: 'The canonical L <= R loop and the overflow-safe midpoint.',
        visualizer: 'binary-search-classic',
      },
      {
        slug: 'first-and-last-position',
        title: 'Find First and Last Position',
        lc: 34,
        difficulty: 'medium',
        summary: 'Boundary finding: on a match, keep searching the direction you care about.',
        visualizer: 'binary-search-boundary',
      },
      {
        slug: 'find-minimum-rotated',
        title: 'Find Minimum in Rotated Sorted Array',
        lc: 153,
        difficulty: 'medium',
        summary: 'The L < R boundary form meets the rotated array — converge on the pivot.',
      },
      {
        slug: 'search-rotated-array',
        title: 'Search in Rotated Sorted Array',
        lc: 33,
        difficulty: 'medium',
        summary: 'One half is always sorted — decide which, then decide if the target is in it.',
      },
      {
        slug: 'koko-eating-bananas',
        title: 'Koko Eating Bananas',
        lc: 875,
        difficulty: 'medium',
        summary: 'Binary search on the answer: monotonic CanFinish(speed) over a bounded range.',
      },
    ],
  },
  {
    slug: 'bfs-dfs',
    title: 'BFS & DFS',
    tab: 'bfs_dfs',
    section: 'core',
    summary:
      'The two traversal engines for graphs and grids: BFS for shortest/level-by-level, DFS for exhaustive exploration — plus multi-source BFS.',
    connections: [
      { to: 'tree-traversals', reason: 'level order is BFS on a tree; the recursive traversals are DFS specialized' },
      { to: 'advanced-graphs', reason: 'topological sort and Dijkstra are BFS with smarter queues' },
      { to: 'backtracking', reason: 'backtracking is DFS over a decision tree instead of a data structure' },
      { to: 'stack-queue', reason: 'BFS runs on a queue, iterative DFS on a stack — swap one, get the other' },
      { to: 'loops-and-counting', reason: 'direction arrays and bounds checks — the grid mechanics live here' },
    ],
    problems: [
      {
        slug: 'flood-fill',
        title: 'Flood Fill',
        lc: 733,
        difficulty: 'easy',
        summary: 'The grid-DFS warm-up: recolor everything reachable from one cell.',
      },
      {
        slug: 'level-order-traversal',
        title: 'Binary Tree Level Order Traversal',
        lc: 102,
        difficulty: 'medium',
        summary: 'BFS on a tree: snapshot queue.Count to process one level per round.',
      },
      {
        slug: 'number-of-islands',
        title: 'Number of Islands',
        lc: 200,
        difficulty: 'medium',
        summary: 'Scan every cell; flood-fill each unvisited land cell you hit.',
        visualizer: 'bfs-islands',
      },
      {
        slug: 'rotting-oranges',
        title: 'Rotting Oranges',
        lc: 994,
        difficulty: 'medium',
        summary: 'Multi-source BFS: enqueue all rotten oranges first, count the rounds.',
      },
      {
        slug: 'clone-graph',
        title: 'Clone Graph',
        lc: 133,
        difficulty: 'medium',
        summary: 'DFS + a visited map from original node → clone.',
      },
    ],
  },
  {
    slug: 'tree-traversals',
    title: 'Trees & Traversals',
    tab: 'trees',
    section: 'core',
    summary:
      'Preorder, inorder, postorder — recursive and iterative — and the tree problems interviews actually ask, each one a traversal wearing a costume.',
    connections: [
      { to: 'bfs-dfs', reason: 'level-by-level questions belong to BFS — it owns the tree problems this page doesn\'t' },
      { to: 'stack-queue', reason: 'iterative traversal is recursion with the call stack made explicit' },
    ],
    problems: [
      {
        slug: 'invert-binary-tree',
        title: 'Invert Binary Tree',
        lc: 226,
        difficulty: 'easy',
        summary: 'Swap children everywhere — your first "traversal in a costume".',
      },
      {
        slug: 'maximum-depth',
        title: 'Maximum Depth of Binary Tree',
        lc: 104,
        difficulty: 'easy',
        summary: 'Postorder in disguise: children answer first, parent combines.',
      },
      {
        slug: 'diameter-of-binary-tree',
        title: 'Diameter of Binary Tree',
        lc: 543,
        difficulty: 'easy',
        summary: 'Postorder + a global best — the shape behind a dozen hard tree problems.',
      },
      {
        slug: 'iterative-traversals',
        title: 'Iterative Traversals with a Stack',
        lc: [94, 144, 145],
        difficulty: 'medium',
        summary: 'Inorder (the important one), preorder, and postorder as reversed preorder.',
      },
      {
        slug: 'validate-bst',
        title: 'Validate Binary Search Tree',
        lc: 98,
        difficulty: 'medium',
        summary: 'Inorder must be strictly increasing — or pass down (min, max) bounds.',
      },
      {
        slug: 'lowest-common-ancestor-bst',
        title: 'Lowest Common Ancestor of a BST',
        lc: 235,
        difficulty: 'medium',
        summary: 'Walk from the root: the split point is the answer. (LC 236 for general trees.)',
      },
    ],
  },
  {
    slug: 'linked-lists',
    title: 'Linked Lists',
    tab: 'linked_lists',
    section: 'core',
    summary:
      'Fast/slow pointers, the dummy head, reversal, merging — and the combo problems interviews love to build from them.',
    connections: [
      { to: 'two-pointers', reason: 'fast/slow is the same-direction pointer shape without indices' },
      { to: 'heap-top-k', reason: 'Merge K Sorted Lists = this topic\'s merge + a heap picking the next head' },
      { to: 'hashmap', reason: 'LRU Cache pairs a dictionary with the doubly linked list built here' },
    ],
    problems: [
      {
        slug: 'middle-of-linked-list',
        title: 'Middle of the Linked List',
        lc: 876,
        difficulty: 'easy',
        summary: 'Fast at 2× speed ⇒ slow at the middle when fast hits the end.',
        visualizer: 'fast-slow-middle',
      },
      {
        slug: 'linked-list-cycle',
        title: 'Linked List Cycle',
        lc: 141,
        difficulty: 'easy',
        summary: 'Fast catches slow on a circular track ⇒ cycle exists — and why they must meet.',
      },
      {
        slug: 'reverse-linked-list',
        title: 'Reverse Linked List',
        lc: 206,
        difficulty: 'easy',
        summary: 'Save next, flip arrow, advance — the four-line mantra.',
        visualizer: 'reverse-list',
      },
      {
        slug: 'merge-two-sorted-lists',
        title: 'Merge Two Sorted Lists',
        lc: 21,
        difficulty: 'easy',
        summary: 'Dummy head + tail pointer: stitch the smaller node on, repeat.',
        related: ['heap-top-k/merge-k-sorted-lists'],
      },
      {
        slug: 'add-two-numbers',
        title: 'Add Two Numbers',
        lc: 2,
        difficulty: 'medium',
        summary: 'Dummy head again, plus the carry idiom — a top-ten interview question.',
      },
      {
        slug: 'remove-nth-from-end',
        title: 'Remove Nth Node From End',
        lc: 19,
        difficulty: 'medium',
        summary: 'Gap-of-n pointers + dummy head to survive removing the first node.',
      },
      {
        slug: 'palindrome-linked-list',
        title: 'Palindrome Linked List',
        lc: 234,
        difficulty: 'easy',
        summary: 'Combo: find middle, reverse second half, mirror-compare.',
      },
      {
        slug: 'lru-cache',
        title: 'LRU Cache',
        lc: 146,
        difficulty: 'medium',
        track: 'stretch',
        summary: 'The classic design combo: hashmap + doubly linked list (hand-rolled and LinkedList<T>).',
        related: ['hashmap/insert-delete-getrandom'],
      },
    ],
  },
  {
    slug: 'array-techniques',
    title: 'Array & Matrix Techniques',
    tab: 'arrays',
    section: 'core',
    summary:
      'Prefix sums, in-place read/write, Dutch National Flag, Kadane, Boyer-Moore, matrix walks — the toolbox for array questions that fit no other pattern.',
    connections: [
      { to: 'two-pointers', reason: 'read/write compaction and DNF are pointer patterns specialized to arrays' },
      { to: 'hashmap', reason: 'prefix sums + a dictionary = Subarray Sum Equals K' },
      { to: 'sliding-window', reason: 'the other way to own subarray questions — know when each applies' },
      { to: 'dynamic-programming', reason: 'Kadane is DP with the table compressed to one variable' },
      { to: 'greedy', reason: 'Best Time to Buy and Sell Stock is Kadane wearing a greedy costume' },
      { to: 'loops-and-counting', reason: 'matrix walks run on direction arrays and boundary discipline' },
    ],
    problems: [
      {
        slug: 'range-sum-query',
        title: 'Range Sum Query — Immutable',
        lc: 303,
        difficulty: 'easy',
        summary: 'Prefix sums proper: precompute once, answer any range in O(1).',
        related: ['hashmap/subarray-sum-equals-k'],
      },
      {
        slug: 'move-zeroes',
        title: 'Move Zeroes',
        lc: 283,
        difficulty: 'easy',
        summary: 'Read/write pointers recalled: stable compaction, zeros fall out the back.',
        related: ['two-pointers/remove-duplicates'],
      },
      {
        slug: 'sort-colors',
        title: 'Sort Colors (Dutch National Flag)',
        lc: 75,
        difficulty: 'medium',
        summary: 'Three regions, three pointers, one pass — and why mid doesn\'t advance on a swap with high.',
        visualizer: 'dutch-flag',
      },
      {
        slug: 'maximum-subarray',
        title: 'Maximum Subarray (Kadane)',
        lc: 53,
        difficulty: 'medium',
        summary: 'Extend or start fresh — DP compressed into one variable.',
        visualizer: 'kadane',
        related: ['greedy/best-time-to-buy-sell-stock'],
      },
      {
        slug: 'majority-element',
        title: 'Majority Element (Boyer-Moore)',
        lc: 169,
        difficulty: 'easy',
        summary: 'Pair up different elements and cancel them; the majority survives.',
      },
      {
        slug: 'product-except-self',
        title: 'Product of Array Except Self',
        lc: 238,
        difficulty: 'medium',
        summary: 'Prefix pass × suffix pass, no division.',
      },
      {
        slug: 'rotate-image',
        title: 'Rotate Image',
        lc: 48,
        difficulty: 'medium',
        summary: 'Transpose, then reverse rows — in-place matrix manipulation.',
      },
      {
        slug: 'spiral-matrix',
        title: 'Spiral Matrix',
        lc: 54,
        difficulty: 'medium',
        track: 'stretch',
        summary: 'Four shrinking boundaries — the layer-walk every matrix question reuses.',
      },
    ],
  },

  // ─────────────────── data structure patterns ───────────────────
  {
    slug: 'stack-queue',
    title: 'Stack & Queue',
    tab: 'stack_queue',
    section: 'structures',
    summary:
      'LIFO matching, design-a-stack problems, and the monotonic stack — the pattern behind every "next greater element" question.',
    connections: [
      { to: 'tree-traversals', reason: 'iterative traversal is this topic applied to trees' },
      { to: 'bfs-dfs', reason: 'the queue is BFS\'s engine; the stack is iterative DFS\'s' },
      { to: 'csharp-toolkit', reason: 'Stack<T> and Queue<T> API notes (TryPop, TryPeek) live there' },
    ],
    problems: [
      {
        slug: 'valid-parentheses',
        title: 'Valid Parentheses',
        lc: 20,
        difficulty: 'easy',
        summary: 'Push openers, match closers — the canonical stack problem.',
      },
      {
        slug: 'implement-queue-using-stacks',
        title: 'Implement Queue using Stacks',
        lc: 232,
        difficulty: 'easy',
        summary: 'Two stacks, amortized O(1) — flip the in-stack only when out-stack empties.',
      },
      {
        slug: 'min-stack',
        title: 'Min Stack',
        lc: 155,
        difficulty: 'medium',
        summary: 'A second stack that remembers the minimum at every depth.',
      },
      {
        slug: 'evaluate-rpn',
        title: 'Evaluate Reverse Polish Notation',
        lc: 150,
        difficulty: 'medium',
        summary: 'Operands wait on the stack; operators consume the top two.',
      },
      {
        slug: 'daily-temperatures',
        title: 'Daily Temperatures (Monotonic Stack)',
        lc: 739,
        difficulty: 'medium',
        summary: 'Indices wait on a decreasing stack until a warmer day resolves them.',
        visualizer: 'monotonic-stack',
      },
      {
        slug: 'min-remove-parentheses',
        title: 'Minimum Remove to Make Valid Parentheses',
        lc: 1249,
        difficulty: 'medium',
        track: 'stretch',
        summary: 'The modern follow-up to Valid Parentheses — mark the offenders, rebuild.',
      },
    ],
  },
  {
    slug: 'heap-top-k',
    title: 'Heap & Top-K',
    tab: 'heap',
    section: 'structures',
    summary:
      'PriorityQueue<TElement, TPriority> and the top-K family: keep a heap of size K, two heaps for medians — and when quickselect or buckets beat both.',
    connections: [
      { to: 'sorting', reason: 'heap vs quickselect vs full sort — the standard top-K decision' },
      { to: 'linked-lists', reason: 'Merge K Sorted Lists runs this heap over that topic\'s merge' },
      { to: 'intervals', reason: 'Meeting Rooms II is a min-heap of end times' },
      { to: 'advanced-graphs', reason: 'Dijkstra is BFS with this structure as the queue (lazy deletion required)' },
      { to: 'csharp-toolkit', reason: 'PriorityQueue API notes — comparers for max-heaps, no DecreaseKey' },
    ],
    problems: [
      {
        slug: 'kth-largest-in-stream',
        title: 'Kth Largest Element in a Stream',
        lc: 703,
        difficulty: 'easy',
        summary: 'The size-K min-heap idea in its purest form.',
      },
      {
        slug: 'kth-largest-element',
        title: 'Kth Largest Element in an Array',
        lc: 215,
        difficulty: 'medium',
        summary: 'Min-heap of size K — with the quickselect and K-Closest-Points variants.',
      },
      {
        slug: 'top-k-frequent',
        title: 'Top K Frequent Elements',
        lc: 347,
        difficulty: 'medium',
        summary: 'Frequency map + heap of size K; bucket sort gets O(n).',
      },
      {
        slug: 'merge-k-sorted-lists',
        title: 'Merge K Sorted Lists',
        lc: 23,
        difficulty: 'hard',
        track: 'stretch',
        summary: 'The heap always knows which of K heads is smallest.',
        related: ['linked-lists/merge-two-sorted-lists'],
      },
      {
        slug: 'find-median-data-stream',
        title: 'Find Median from Data Stream',
        lc: 295,
        difficulty: 'hard',
        track: 'stretch',
        summary: 'Two heaps balanced around the middle — the two-heaps sub-pattern.',
      },
    ],
  },
  {
    slug: 'trie',
    title: 'Trie (Prefix Tree)',
    tab: 'trie',
    section: 'structures',
    summary:
      'A tree of characters where paths are prefixes — the structure for autocomplete, word dictionaries, and prefix search.',
    connections: [
      { to: 'hashmap', reason: 'each node is a tiny dictionary — child maps all the way down' },
      { to: 'backtracking', reason: 'Word Search II needs that topic first — the trie prunes its DFS' },
      { to: 'tree-traversals', reason: 'a trie is a tree; search and insert are traversals with a map step' },
    ],
    problems: [
      {
        slug: 'implement-trie',
        title: 'Implement Trie',
        lc: 208,
        difficulty: 'medium',
        summary: 'Insert, search, startsWith — nodes with child maps and an end-of-word flag.',
      },
      {
        slug: 'design-add-search-words',
        title: 'Design Add and Search Words',
        lc: 211,
        difficulty: 'medium',
        summary: 'Trie search with \'.\' wildcards — DFS over all children.',
      },
      {
        slug: 'word-search-ii',
        title: 'Word Search II',
        lc: 212,
        difficulty: 'hard',
        track: 'stretch',
        summary: 'Trie + grid backtracking. Prerequisite: Word Search (backtracking).',
        related: ['backtracking/word-search'],
      },
    ],
  },

  // ─────────────────────── advanced patterns ───────────────────────
  {
    slug: 'backtracking',
    title: 'Backtracking',
    tab: 'backtracking',
    section: 'advanced',
    summary:
      'DFS over a decision tree: choose, explore, un-choose. Subsets, permutations, combinations — one template, different branching.',
    connections: [
      { to: 'bfs-dfs', reason: 'the engine is DFS — but over choices you generate, not nodes that exist' },
      { to: 'dynamic-programming', reason: 'memoize a backtrack over overlapping subproblems and you get DP' },
      { to: 'trie', reason: 'Word Search II sends this topic\'s grid DFS through a prefix tree' },
    ],
    problems: [
      {
        slug: 'letter-combinations',
        title: 'Letter Combinations of a Phone Number',
        lc: 17,
        difficulty: 'medium',
        summary: 'The gentlest instance: fixed depth, one choice per digit.',
      },
      {
        slug: 'subsets',
        title: 'Subsets',
        lc: 78,
        difficulty: 'medium',
        summary: 'Include-or-skip branching; the recursion tree IS the power set.',
      },
      {
        slug: 'permutations',
        title: 'Permutations',
        lc: 46,
        difficulty: 'medium',
        summary: 'Choose from the remaining pool at each level; un-choose on the way back.',
      },
      {
        slug: 'combination-sum',
        title: 'Combination Sum',
        lc: 39,
        difficulty: 'medium',
        summary: 'Reuse allowed: stay on the same index after choosing; move forward to skip.',
      },
      {
        slug: 'generate-parentheses',
        title: 'Generate Parentheses',
        lc: 22,
        difficulty: 'medium',
        summary: 'Constrained branching: open when you can, close when it stays valid.',
      },
      {
        slug: 'word-search',
        title: 'Word Search',
        lc: 79,
        difficulty: 'medium',
        summary: 'Grid DFS with mark/unmark — backtracking on a board.',
        related: ['trie/word-search-ii'],
      },
    ],
  },
  {
    slug: 'dynamic-programming',
    title: 'Dynamic Programming Basics',
    tab: 'dp',
    section: 'advanced',
    summary:
      'State + recurrence + base case. 1D, 2D, and string DP through the six problems that teach the whole method.',
    connections: [
      { to: 'array-techniques', reason: 'Kadane is the DP you already know — one-variable table' },
      { to: 'backtracking', reason: 'DP is memoized backtracking when subproblems overlap' },
      { to: 'greedy', reason: 'Jump Game solves both ways — learn which proof each needs' },
    ],
    problems: [
      {
        slug: 'climbing-stairs',
        title: 'Climbing Stairs',
        lc: 70,
        difficulty: 'easy',
        summary: 'Fibonacci in disguise — the "ways to reach i" recurrence.',
      },
      {
        slug: 'house-robber',
        title: 'House Robber',
        lc: 198,
        difficulty: 'medium',
        summary: 'Take-or-skip: dp[i] = max(dp[i-1], dp[i-2] + a[i]).',
      },
      {
        slug: 'unique-paths',
        title: 'Unique Paths',
        lc: 62,
        difficulty: 'medium',
        summary: '2D grid DP: cell = sum of the two ways in; rows compress to 1D.',
      },
      {
        slug: 'coin-change',
        title: 'Coin Change',
        lc: 322,
        difficulty: 'medium',
        summary: 'Min-coins per amount, built bottom-up — the unbounded knapsack shape.',
      },
      {
        slug: 'word-break',
        title: 'Word Break',
        lc: 139,
        difficulty: 'medium',
        summary: 'DP over string prefixes, with a word set doing the lookups.',
      },
      {
        slug: 'longest-increasing-subsequence',
        title: 'Longest Increasing Subsequence',
        lc: 300,
        difficulty: 'medium',
        summary: 'O(n²) DP first; the O(n log n) patience upgrade as an optional coda.',
      },
    ],
  },
  {
    slug: 'advanced-graphs',
    title: 'Graphs: Topo Sort, Union-Find, Dijkstra',
    tab: 'graphs',
    section: 'advanced',
    summary:
      'Beyond plain traversal: dependency ordering with Kahn\'s algorithm, connectivity with union-find, shortest paths with Dijkstra.',
    connections: [
      { to: 'bfs-dfs', reason: 'everything here is BFS/DFS with one upgrade — start there' },
      { to: 'heap-top-k', reason: 'Dijkstra\'s queue is a min-heap, with the C# lazy-deletion idiom' },
    ],
    problems: [
      {
        slug: 'course-schedule',
        title: 'Course Schedule',
        lc: 207,
        difficulty: 'medium',
        summary: 'Cycle detection = can all courses be finished? Kahn\'s in-degree BFS.',
      },
      {
        slug: 'course-schedule-ii',
        title: 'Course Schedule II',
        lc: 210,
        difficulty: 'medium',
        summary: 'Same algorithm, but the dequeue order IS the answer.',
        visualizer: 'topo-sort',
      },
      {
        slug: 'number-of-provinces',
        title: 'Number of Provinces (Union-Find)',
        lc: 547,
        difficulty: 'medium',
        summary: 'Union-find with path compression — components without traversal.',
      },
      {
        slug: 'network-delay-time',
        title: 'Network Delay Time (Dijkstra)',
        lc: 743,
        difficulty: 'medium',
        summary: 'BFS where the queue becomes a min-heap — and PriorityQueue\'s missing DecreaseKey.',
      },
    ],
  },
  {
    slug: 'greedy',
    title: 'Greedy',
    tab: 'greedy',
    section: 'advanced',
    summary:
      'Take the locally best move and prove you never regret it. Recognizing when greedy works — and when it silently doesn\'t.',
    connections: [
      { to: 'intervals', reason: 'interval scheduling is greedy\'s home turf — sort, then never regret' },
      { to: 'dynamic-programming', reason: 'when the greedy proof fails, DP is the fallback — Jump Game shows both' },
      { to: 'binary-search', reason: 'the CanDo(k) check inside binary-search-on-answer is usually greedy' },
      { to: 'array-techniques', reason: 'Buy-Sell Stock and Kadane are the same scan with different bookkeeping' },
    ],
    problems: [
      {
        slug: 'best-time-to-buy-sell-stock',
        title: 'Best Time to Buy and Sell Stock',
        lc: 121,
        difficulty: 'easy',
        summary: 'Track the cheapest buy so far; every day asks "sell today?"',
        related: ['array-techniques/maximum-subarray'],
      },
      {
        slug: 'jump-game',
        title: 'Jump Game',
        lc: 55,
        difficulty: 'medium',
        summary: 'Furthest-reachable frontier — one variable, one pass.',
      },
      {
        slug: 'gas-station',
        title: 'Gas Station',
        lc: 134,
        difficulty: 'medium',
        summary: 'If you run dry at i, no start before i works — restart at i+1.',
      },
      {
        slug: 'partition-labels',
        title: 'Partition Labels',
        lc: 763,
        difficulty: 'medium',
        summary: 'Greedy + hashmap: extend the partition to each char\'s last index.',
      },
    ],
  },
  {
    slug: 'intervals',
    title: 'Intervals',
    tab: 'intervals',
    section: 'advanced',
    summary:
      'Sort by start (usually), then sweep: merge, insert, count overlaps. The pattern behind every calendar question.',
    connections: [
      { to: 'sorting', reason: 'step one is always a sort — and start-vs-end is a real decision' },
      { to: 'greedy', reason: 'keep-the-earliest-end is a greedy proof you should be able to give' },
      { to: 'heap-top-k', reason: 'Meeting Rooms II tracks live meetings in a min-heap of end times' },
    ],
    problems: [
      {
        slug: 'merge-intervals',
        title: 'Merge Intervals',
        lc: 56,
        difficulty: 'medium',
        summary: 'Sort by start; extend the current merged interval or start a new one.',
      },
      {
        slug: 'insert-interval',
        title: 'Insert Interval',
        lc: 57,
        difficulty: 'medium',
        summary: 'Three phases: all-before, merge-overlapping, all-after.',
      },
      {
        slug: 'non-overlapping-intervals',
        title: 'Non-overlapping Intervals',
        lc: 435,
        difficulty: 'medium',
        summary: 'Sort by END — keep the interval that finishes earliest.',
      },
      {
        slug: 'meeting-rooms-ii',
        title: 'Meeting Rooms II',
        lc: 253,
        difficulty: 'medium',
        summary: 'Min-heap of end times = rooms in use; peak heap size is the answer.',
      },
    ],
  },
  {
    slug: 'bit-manipulation',
    title: 'Bit Manipulation',
    tab: 'bits',
    section: 'advanced',
    summary:
      'XOR cancellation, n & (n−1), and the handful of bit identities that solve an entire question category in three lines.',
    connections: [
      { to: 'hashmap', reason: 'XOR replaces the HashSet in "find the loner" — O(1) space instead of O(n)' },
      { to: 'dynamic-programming', reason: 'Counting Bits is DP where the recurrence is a bit shift' },
    ],
    problems: [
      {
        slug: 'single-number',
        title: 'Single Number',
        lc: 136,
        difficulty: 'easy',
        summary: 'XOR everything — pairs cancel, the loner survives.',
      },
      {
        slug: 'missing-number',
        title: 'Missing Number',
        lc: 268,
        difficulty: 'easy',
        summary: 'XOR indices against values — everything cancels except the gap.',
      },
      {
        slug: 'number-of-1-bits',
        title: 'Number of 1 Bits',
        lc: 191,
        difficulty: 'easy',
        summary: 'n & (n−1) clears the lowest set bit; count the clears.',
      },
      {
        slug: 'counting-bits',
        title: 'Counting Bits',
        lc: 338,
        difficulty: 'easy',
        summary: 'DP on bits: bits[i] = bits[i >> 1] + (i & 1).',
      },
    ],
  },


  // ───────────────────────── machine (the primer) ─────────────────────────
  {
    slug: 'bits-and-memory',
    title: 'Bits, Bytes & Addresses',
    tab: 'bits_memory',
    section: 'machine',
    summary:
      'Hex, two\'s complement, overflow as wraparound, alignment padding — and the one fact under everything: memory is a flat array of numbered bytes.',
    problems: [
      {
        slug: 'sizeof-and-layout',
        title: 'Struct Size & Padding',
        difficulty: 'easy',
        summary: 'Predict the size of four structs, then check — and meet the padding the compiler inserted without telling you.',
      },
    ],
    connections: [
      { to: 'process-and-thread', reason: 'what runs on top of those bytes — the process and thread that own them' },
      { to: 'stack-and-heap', reason: 'the two places those numbered bytes actually get handed out from' },
      { to: 'bit-manipulation', reason: 'the interview pattern that trades directly on this representation' },
    ],
  },
  {
    slug: 'process-and-thread',
    title: 'Processes, Threads & the Kernel',
    tab: 'process_thread',
    section: 'machine',
    summary:
      'What the OS actually does for you, why user mode and kernel mode are separate, and the fact that explains the whole concurrency section: threads share the heap but never the stack.',
    problems: [
      {
        slug: 'syscall-cost',
        title: 'Crossing Into the Kernel',
        difficulty: 'easy',
        summary: 'Follow one call from user mode into the kernel and back, and see everything the CPU has to do at the boundary.',
      },
    ],
    connections: [
      { to: 'cpu-execution', reason: 'one level down — what the CPU is doing while the scheduler is not looking' },
      { to: 'stack-and-heap', reason: 'the shared-heap/private-stack split, in full detail' },
      { to: 'virtual-memory', reason: 'a process is really an address space — here is what that means' },
      { to: 'threads-and-scheduling', reason: 'threads as .NET presents them: the pool, Tasks, and await' },
      { to: 'network-basics', reason: 'the other half of the machine: what happens to your bytes once they leave the process' },
    ],
  },
  {
    slug: 'cpu-execution',
    title: 'How a CPU Runs an Instruction',
    tab: 'cpu_execution',
    section: 'machine',
    summary:
      'Registers, the fetch-decode-execute loop, and what call and ret actually do to the stack — with the real disassembly to prove it.',
    problems: [
      {
        slug: 'read-the-disassembly',
        title: 'Read the Disassembly',
        difficulty: 'medium',
        summary: 'Compile five lines of C, disassemble the result, and map every instruction back to the source line that caused it.',
      },
      {
        slug: 'call-and-inline-cost',
        title: 'The Call and the Frame',
        difficulty: 'easy',
        summary: 'What call and ret actually do to the stack, and what disappears entirely once the JIT inlines them.',
      },
    ],
    connections: [
      { to: 'bits-and-memory', reason: 'the representation those instructions are pushing around' },
      { to: 'cpu-pipeline', reason: 'the same loop, except modern CPUs run several instructions at once and out of order' },
      { to: 'il-jit-codegen', reason: 'how your C# becomes the instructions you just read' },
    ],
  },

  // ───────────────────────── memory & the machine ─────────────────────────
  {
    slug: 'stack-and-heap',
    title: 'Stack vs Heap',
    tab: 'stack_heap',
    section: 'memory',
    summary:
      'Two allocators with different bills, and one piece of folklore to unlearn: a struct lives where it is declared, not "on the stack".',
    problems: [
      {
        slug: 'does-this-allocate',
        title: 'Does This Allocate?',
        difficulty: 'medium',
        summary: 'Predict the heap allocation of eight snippets — boxing, closures, spans, params — then check every one against what the runtime reports.',
      },
      {
        slug: 'stack-overflow-hunt',
        title: 'The Stack That Ran Out',
        difficulty: 'easy',
        summary: 'Find why a recursive method dies at a depth you can predict, and why you cannot catch it.',
      },
    ],
    connections: [
      { to: 'gc-internals', reason: 'the heap side of the story — who cleans up and what it costs' },
      { to: 'memory-hierarchy', reason: 'where your objects sit decides how fast the CPU can reach them' },
      { to: 'bits-and-memory', reason: 'the byte-level layout underneath every value you allocate' },
      { to: 'csharp-toolkit', reason: 'the collections whose allocation behaviour this explains' },
    ],
  },
  {
    slug: 'virtual-memory',
    title: 'Virtual Memory & Paging',
    tab: 'virtual_memory',
    section: 'memory',
    summary:
      'Every address your program sees is a lie the MMU maintains — pages, page faults, demand paging, and what "memory usage" really measures.',
    problems: [
      {
        slug: 'page-fault-walk',
        title: 'The First Touch',
        difficulty: 'medium',
        summary: 'A freshly allocated array is a promise, not memory. Follow what the kernel actually does the first time you touch each page.',
      },
    ],
    connections: [
      { to: 'memory-hierarchy', reason: 'the other translation layer between your index and the data' },
      { to: 'gc-internals', reason: 'why committed, resident, and "GC heap size" are three different numbers' },
      { to: 'process-and-thread', reason: 'the address space this page describes is what makes a process a process' },
    ],
  },
  {
    slug: 'memory-hierarchy',
    title: 'Caches & the Memory Hierarchy',
    tab: 'memory_hierarchy',
    section: 'memory',
    summary:
      'The cache line is the unit of everything. Locality is why two loops with identical Big-O differ by more than a factor of two.',
    problems: [
      {
        slug: 'loop-order',
        title: 'Two Loops, Same Big-O',
        difficulty: 'medium',
        summary: 'The same matrix multiply in two loop orders — identical operation counts, wildly different cache lines touched.',
      },
      {
        slug: 'stride-and-cache-lines',
        title: 'Finding the Cache Line',
        difficulty: 'medium',
        summary: 'Walk an array with a growing stride and work out from first principles where the 64-byte line boundary has to be.',
      },
      {
        slug: 'aos-vs-soa',
        title: 'Array of Structs vs Struct of Arrays',
        difficulty: 'medium',
        summary: 'Same data, same total bytes, two layouts — and the one the cache prefers when you only read a field.',
      },
    ],
    connections: [
      { to: 'cpu-pipeline', reason: 'the other half of why the CPU is rarely doing what you wrote' },
      { to: 'big-o', reason: 'the constant factor Big-O deliberately throws away — measured' },
      { to: 'atomics-and-cas', reason: 'false sharing: what happens when two threads want the same cache line' },
      { to: 'caching', reason: 'the same idea four orders of magnitude further out, in a distributed system' },
      { to: 'stack-and-heap', reason: 'contiguous versus pointer-chasing layouts, and who allocates which' },
    ],
  },
  {
    slug: 'cpu-pipeline',
    title: 'Branches, Pipelines & Speculation',
    tab: 'cpu_pipeline',
    section: 'memory',
    summary:
      'Pipelining, branch prediction, and out-of-order execution — the machinery that makes your code fast, and that makes a memory model necessary.',
    problems: [
      {
        slug: 'sorted-array-branch',
        title: 'What the Branch Predictor Learns',
        difficulty: 'medium',
        summary: 'The same loop over sorted and over shuffled data, and what a predictor can and cannot learn from each.',
      },
      {
        slug: 'branchless-rewrite',
        title: 'Removing the Branch',
        difficulty: 'medium',
        summary: 'Rewrite an unpredictable branch as arithmetic, and see what the CPU no longer has to guess.',
      },
    ],
    connections: [
      { to: 'memory-model', reason: 'the reordering you just saw is exactly why threads need ordering rules' },
      { to: 'memory-hierarchy', reason: 'stalls waiting on memory are what all this machinery exists to hide' },
      { to: 'il-jit-codegen', reason: 'what the JIT emits decides what the predictor has to work with' },
    ],
  },
  {
    slug: 'gc-internals',
    title: 'Garbage Collection',
    tab: 'gc_internals',
    section: 'memory',
    summary:
      'Allocation is a pointer bump; collection is the bill. Generations, the LOH, write barriers, and where p99 latency actually goes.',
    problems: [
      {
        slug: 'generations-in-action',
        title: 'Watching the Generations',
        difficulty: 'medium',
        summary: 'Three allocation patterns, three very different gen0/gen1/gen2 collection counts — and one that never collects at all.',
      },
      {
        slug: 'the-leak-hunt',
        title: 'The Leak That Was Not a Leak',
        difficulty: 'medium',
        summary: 'A service whose memory only grows, in a runtime with a garbage collector. Find what is still holding the reference.',
      },
    ],
    connections: [
      { to: 'stack-and-heap', reason: 'what gets allocated where — the input to everything the collector does' },
      { to: 'virtual-memory', reason: 'the OS-level memory the GC is managing on your behalf' },
      { to: 'il-jit-codegen', reason: 'write barriers and allocation are code the JIT emits for you' },
      { to: 'big-o', reason: 'the allocation cost hiding inside collection APIs you quote as O(1)' },
    ],
  },
  {
    slug: 'il-jit-codegen',
    title: 'From Source to Machine Code',
    tab: 'il_jit',
    section: 'memory',
    summary:
      'C# to IL to machine code: tiered compilation, inlining, bounds-check elimination, and the four reasons your micro-benchmark is lying to you.',
    problems: [
      {
        slug: 'bounds-check-elimination',
        title: 'The Bounds Check That Vanished',
        difficulty: 'medium',
        summary: 'Two index loops over the same array — one the JIT can prove safe, one it cannot.',
      },
    ],
    connections: [
      { to: 'cpu-execution', reason: 'the instructions all this machinery is trying to produce' },
      { to: 'cpu-pipeline', reason: 'why the JIT cares about branch shape and not just instruction count' },
      { to: 'big-o', reason: 'constant factors are decided here, long after the algorithm is chosen' },
    ],
  },

  // ───────────────────────── concurrency & locking ─────────────────────────
  {
    slug: 'threads-and-scheduling',
    title: 'Threads, the Pool & async/await',
    tab: 'threads_async',
    section: 'concurrency',
    summary:
      'OS threads, pool threads, and Tasks are three different things. The state machine await compiles into, and the starvation you cause by blocking on it.',
    problems: [
      {
        slug: 'threadpool-starvation',
        title: 'The Deadlock That Was Not a Deadlock',
        difficulty: 'medium',
        summary: 'A service stops responding under load, with every thread alive and none of them doing anything.',
      },
      {
        slug: 'async-state-machine',
        title: 'What await Compiles Into',
        difficulty: 'medium',
        summary: 'Decompile one async method and read the state machine it became, field by field.',
      },
    ],
    connections: [
      { to: 'process-and-thread', reason: 'the OS-level thread everything here is built on' },
      { to: 'concurrency-hazards', reason: 'what goes wrong once more than one of these runs at a time' },
      { to: 'locks-internals', reason: 'what actually happens to a thread that has to wait' },
      { to: 'parallelism-patterns', reason: 'using these threads for throughput instead of just concurrency' },
    ],
  },
  {
    slug: 'memory-model',
    title: 'Reordering, Visibility & the Memory Model',
    tab: 'memory_model',
    section: 'concurrency',
    summary:
      'Atomicity, visibility, and ordering are three separate guarantees that "thread-safe" mushes into one word. volatile gives you some of them.',
    problems: [
      {
        slug: 'the-reordering-test',
        title: 'Catching a Reordering',
        difficulty: 'hard',
        summary: 'Two threads, four lines, and an outcome that no sequential interleaving of those lines can explain.',
      },
    ],
    connections: [
      { to: 'cpu-pipeline', reason: 'the out-of-order execution that makes all of this necessary' },
      { to: 'atomics-and-cas', reason: 'the primitives that buy back the guarantees you just lost' },
      { to: 'locks-internals', reason: 'why taking a lock hands you ordering for free' },
      { to: 'consistency-models', reason: 'the identical argument between machines: two cores disagreeing is two replicas disagreeing' },
    ],
  },
  {
    slug: 'atomics-and-cas',
    title: 'Atomics & Compare-and-Swap',
    tab: 'atomics_cas',
    section: 'concurrency',
    summary:
      'Why count++ is three operations, what the CPU does to fuse them into one, and the cache line two threads should never share.',
    problems: [
      {
        slug: 'lost-updates',
        title: 'The Counter That Counted Wrong',
        difficulty: 'easy',
        summary: 'Two threads, one increment, and thousands of updates that quietly vanish.',
      },
      {
        slug: 'cas-counter',
        title: 'What Interlocked Actually Does',
        difficulty: 'medium',
        summary: 'Three ways to add one to a number, and what the CPU must do to make each one indivisible.',
      },
      {
        slug: 'false-sharing',
        title: 'Two Counters, One Cache Line',
        difficulty: 'hard',
        summary: 'Padding changes nothing about the logic and everything about the cache traffic between two cores. Work out why.',
      },
    ],
    connections: [
      { to: 'memory-model', reason: 'the ordering guarantees these operations carry with them' },
      { to: 'locks-internals', reason: 'a lock is built out of exactly this primitive' },
      { to: 'memory-hierarchy', reason: 'why false sharing costs so much — the cache line, again' },
      { to: 'lock-free-structures', reason: 'what you can build once compare-and-swap is in hand' },
    ],
  },
  {
    slug: 'locks-internals',
    title: 'What a Lock Is Made Of',
    tab: 'locks',
    section: 'concurrency',
    summary:
      'An atomic word, a wait queue, and a way to park a thread. The uncontended path never enters the kernel — which is why contention costs what it does.',
    problems: [
      {
        slug: 'lock-cost-ladder',
        title: 'The Fast Path and the Slow Path',
        difficulty: 'medium',
        summary: 'Uncontended lock, Interlocked, contended lock, SemaphoreSlim — what each one actually does when it cannot get straight in.',
      },
      {
        slug: 'build-a-spinlock',
        title: 'Build a Spinlock',
        difficulty: 'medium',
        summary: 'Twelve lines that prove the fast path of every lock is just a compare-and-swap.',
      },
    ],
    connections: [
      { to: 'atomics-and-cas', reason: 'the primitive the fast path is built from' },
      { to: 'concurrency-hazards', reason: 'everything that goes wrong once you have more than one lock' },
      { to: 'memory-model', reason: 'the ordering a lock gives you, and why most code needs nothing else' },
      { to: 'lock-free-structures', reason: 'what it takes to do without one, and when that is worth it' },
      { to: 'consensus', reason: 'what a lock means once it has to span machines, and why it needs a fencing token' },
    ],
  },
  {
    slug: 'concurrency-hazards',
    title: 'Races, Deadlock & Friends',
    tab: 'hazards',
    section: 'concurrency',
    summary:
      'Check-then-act and read-modify-write are the shapes almost every concurrency bug takes. Plus the four conditions every deadlock needs, and the async .Result trap.',
    problems: [
      {
        slug: 'deadlock-repro',
        title: 'Two Locks, Two Orders',
        difficulty: 'medium',
        summary: 'Reproduce a deadlock on demand, then fix it without adding a single lock.',
      },
      {
        slug: 'check-then-act',
        title: 'The Thread-Safe Class That Was Not',
        difficulty: 'medium',
        summary: 'Every method takes the lock. Every method is correct. The class is still broken.',
      },
    ],
    connections: [
      { to: 'locks-internals', reason: 'the tool most of these hazards are about misusing' },
      { to: 'threads-and-scheduling', reason: 'the async-specific version of the same traps' },
      { to: 'memory-model', reason: 'the difference between a data race and a race condition' },
    ],
  },
  {
    slug: 'lock-free-structures',
    title: 'Lock-Free Data Structures',
    tab: 'lock_free',
    section: 'concurrency',
    summary:
      'What lock-free actually promises — progress, not speed. A Treiber stack from one CAS loop, and the two traps in ConcurrentDictionary.',
    problems: [
      {
        slug: 'treiber-stack',
        title: 'A Stack Without Locks',
        difficulty: 'hard',
        summary: 'Build it out of a single compare-and-swap loop, then verify it still holds under real contention.',
      },
      {
        slug: 'concurrent-dictionary-traps',
        title: 'GetOrAdd Ran Twice',
        difficulty: 'medium',
        summary: 'A factory with a side effect, and a compound operation that was never atomic to begin with.',
      },
    ],
    connections: [
      { to: 'atomics-and-cas', reason: 'the single primitive everything on this page is built from' },
      { to: 'locks-internals', reason: 'the alternative, and the honest comparison against it' },
      { to: 'parallelism-patterns', reason: 'partitioning — usually a better answer than going lock-free' },
    ],
  },
  {
    slug: 'parallelism-patterns',
    title: 'Parallelism That Actually Scales',
    tab: 'parallelism',
    section: 'concurrency',
    summary:
      'Amdahl, coherence costs, and why adding threads can lower throughput. Partitioning is the answer; backpressure is not optional.',
    problems: [
      {
        slug: 'scaling-curve',
        title: 'Why More Threads Can Be Slower',
        difficulty: 'medium',
        summary: 'One shared counter and one partitioned counter, and the three separate forces that bend a scaling curve back down.',
      },
      {
        slug: 'producer-consumer',
        title: 'A Pipeline With Backpressure',
        difficulty: 'medium',
        summary: 'A bounded channel, a slow consumer, and what happens to memory when you forget the bound.',
      },
    ],
    connections: [
      { to: 'threads-and-scheduling', reason: 'the pool that is running all of this, and its limits' },
      { to: 'lock-free-structures', reason: 'the other way to cut synchronization cost' },
      { to: 'locks-internals', reason: 'contention is the thing every pattern here is dodging' },
    ],
  },


  // ───────────────────────── the network ─────────────────────────
  {
    slug: 'network-basics',
    title: 'What a Network Actually Is',
    tab: 'network_basics',
    section: 'network',
    summary:
      'From nothing: two machines, a link, and a message chopped into packets. Why the internet switches packets instead of holding a wire open, what bandwidth and latency each really cost, and why the whole thing had to be built in layers.',
    connections: [
      { to: 'osi-model', reason: 'the layers this page argues for, finally named and numbered' },
      { to: 'ip-and-routing', reason: 'the addressing that makes a packet deliverable across networks' },
      { to: 'process-and-thread', reason: 'the far end of every path is a socket read, and that is a syscall' },
    ],
    problems: [],
  },
  {
    slug: 'osi-model',
    title: 'The OSI Model & What Actually Runs',
    tab: 'osi_model',
    section: 'network',
    summary:
      'Seven layers on the exam, four in the code. The point of the model is encapsulation: watch one HTTP request grow a TCP header, an IP header and an Ethernet frame on the way out, and shed them again on the way in.',
    connections: [
      { to: 'link-layer', reason: 'layer 2, where the headers stop being abstract' },
      { to: 'ip-and-routing', reason: 'layer 3 — the only layer that spans the whole path' },
      { to: 'tcp-and-udp', reason: 'layer 4 — the choice between a stream and a message' },
      { to: 'application-protocols', reason: 'layer 7 — what all of it was carrying' },
    ],
    problems: [],
  },
  {
    slug: 'link-layer',
    title: 'The Link Layer: Ethernet, MAC & ARP',
    tab: 'link_layer',
    section: 'network',
    summary:
      'One hop at a time: MAC addresses, frames, what a switch learns and what a hub never did, how ARP turns an IP address into the MAC address of the next hop, and why MTU is the number that quietly breaks things.',
    connections: [
      { to: 'ip-and-routing', reason: 'the layer above, which decides which next hop the frame is for' },
      { to: 'osi-model', reason: 'where this layer sits in the stack, and what it hands upward' },
      { to: 'network-troubleshooting', reason: 'ARP tables and MTU are two of the first things you check' },
    ],
    problems: [],
  },
  {
    slug: 'ip-and-routing',
    title: 'IP Addresses, Subnets & Routing',
    tab: 'ip_routing',
    section: 'network',
    summary:
      'Addresses that carry structure: IPv4 and IPv6, what a subnet mask actually masks, CIDR arithmetic done by hand, the routing table consulted for every packet, default gateways, NAT, and what TTL and traceroute are really doing.',
    connections: [
      { to: 'link-layer', reason: 'the layer below — how a routed packet gets across one hop' },
      { to: 'tcp-and-udp', reason: 'the layer above, and the port numbers IP knows nothing about' },
      { to: 'dns', reason: 'where the address you are routing to came from in the first place' },
      { to: 'network-troubleshooting', reason: 'route, gateway, NAT — the middle third of the checklist' },
    ],
    problems: [],
  },
  {
    slug: 'ports-and-sockets',
    title: 'Ports & Sockets',
    tab: 'ports_sockets',
    section: 'network',
    summary:
      'A port is an integer in a header, not a thing. How the kernel uses the four-tuple to pick which socket gets a packet, why a listening socket and a connected socket are different objects, the ephemeral range, the accept queue, and where TIME_WAIT and port exhaustion come from.',
    connections: [
      { to: 'tcp-and-udp', reason: 'the protocols whose headers those port numbers live in' },
      { to: 'ip-and-routing', reason: 'the address half of the four-tuple' },
      { to: 'process-and-thread', reason: 'a socket is a kernel object behind a handle, and every read crosses the boundary' },
      { to: 'network-troubleshooting', reason: 'listening on the wrong interface is the single most common self-inflicted outage' },
    ],
    problems: [
      {
        slug: 'one-port-many-connections',
        title: 'One Port, Many Connections',
        difficulty: 'easy',
        summary: 'Open several connections to one listening port and print the four-tuples: predict what the kernel uses to tell them apart before you look.',
      },
    ],
  },
  {
    slug: 'tcp-and-udp',
    title: 'TCP & UDP',
    tab: 'tcp_udp',
    section: 'network',
    summary:
      'The transport layer doing its two jobs: TCP turning a lossy packet network into an ordered byte stream — handshake, sequence numbers, acknowledgements, retransmission, windows — and UDP declining to, plus how to tell which one a problem actually wants.',
    connections: [
      { to: 'ports-and-sockets', reason: 'the demultiplexing that makes both protocols usable at all' },
      { to: 'networking', reason: 'the applied version: congestion control, TLS, HTTP/2 and HTTP/3 on top of this' },
      { to: 'application-protocols', reason: 'what people actually put inside these segments' },
    ],
    problems: [
      {
        slug: 'stream-vs-datagram',
        title: 'Where Did My Message Boundary Go?',
        difficulty: 'easy',
        summary: 'Two sends, one receive — predict what TCP and UDP each hand the reader, then run both and see why framing is your job on one of them.',
      },
    ],
  },
  {
    slug: 'dns',
    title: 'DNS: Names into Addresses',
    tab: 'dns',
    section: 'network',
    summary:
      'The distributed lookup every request starts with: stub resolver, recursive resolver, root, TLD and authoritative servers; the records that matter; the TTL and the four caches between you and the answer; and the .NET traps that let a stale address outlive a failover.',
    connections: [
      { to: 'ip-and-routing', reason: 'the address a name resolves to, and what happens next with it' },
      { to: 'application-protocols', reason: 'the request that was waiting on the answer' },
      { to: 'caching', reason: 'a TTL is a cache-invalidation policy, and DNS is the oldest one you use daily' },
      { to: 'network-troubleshooting', reason: 'resolution is step one of the checklist for a reason' },
    ],
    problems: [],
  },
  {
    slug: 'application-protocols',
    title: 'The Protocols on Top',
    tab: 'app_protocols',
    section: 'network',
    summary:
      'What the stack was carrying: HTTP as text over a byte stream, request and response anatomy, statelessness and cookies, TLS in outline, WebSockets and gRPC for when request/response is the wrong shape, and the older protocols worth recognising.',
    connections: [
      { to: 'tcp-and-udp', reason: 'the byte stream every one of these is framed on top of' },
      { to: 'networking', reason: 'HTTP/1.1 vs 2 vs 3, TLS handshakes and connection reuse, in full' },
      { to: 'dns', reason: 'the lookup that has to finish before any of this can start' },
    ],
    problems: [],
  },
  {
    slug: 'network-troubleshooting',
    title: 'Seeing the Network',
    tab: 'net_tools',
    section: 'network',
    summary:
      'The toolbox and the order to reach for it — name, reachability, route, port, TLS, payload — what each tool proves versus merely suggests, worked as a decision tree from "the service cannot reach the database".',
    connections: [
      { to: 'dns', reason: 'step one of the checklist, and the step people skip' },
      { to: 'ports-and-sockets', reason: 'what a refused connection tells you that a timeout does not' },
      { to: 'ip-and-routing', reason: 'reading the route when the packet never comes back' },
      { to: 'reliability', reason: 'the timeouts and retries that hide these failures until they cannot' },
    ],
    problems: [],
  },

  // ───────────────────────── system design ─────────────────────────
  {
    slug: 'estimation',
    title: 'Back-of-Envelope Estimation',
    tab: 'estimation',
    section: 'system-design',
    summary:
      'Sizing a system before you build it: the numbers worth memorising, Little\'s Law, and why the mean latency is the least useful number on the dashboard.',
    problems: [],
    connections: [
      { to: 'storage-engines', reason: 'the first thing your estimate sizes is the thing holding the data' },
      { to: 'caching', reason: 'the cheapest way to make a number smaller before you design around it' },
      { to: 'reliability', reason: 'tail latency and queueing are the same arithmetic, one layer up' },
      { to: 'design-drills', reason: 'where these numbers actually get used, under time pressure' },
    ],
  },
  {
    slug: 'storage-engines',
    title: 'Storage Engines: B-Tree vs LSM',
    tab: 'storage',
    section: 'system-design',
    summary:
      'What a database actually does with a write: pages and B-trees, log-structured merge trees, the write-ahead log, and the three amplifications you trade between.',
    problems: [],
    connections: [
      { to: 'transactions', reason: 'the engine is what has to make ACID true' },
      { to: 'replication', reason: 'the write-ahead log is also the replication stream' },
      { to: 'memory-hierarchy', reason: 'a B-tree node is page-sized for exactly the reasons that page explains' },
      { to: 'big-o', reason: 'why a database index is a B-tree and not the binary tree the complexity table suggests' },
    ],
  },
  {
    slug: 'transactions',
    title: 'Transactions & Isolation Levels',
    tab: 'transactions',
    section: 'system-design',
    summary:
      'ACID past the acronym: what each isolation level actually permits, the four anomalies, MVCC versus locking, and why serializable is rarer than people think.',
    problems: [],
    connections: [
      { to: 'consistency-models', reason: 'isolation is the single-node version of the same argument' },
      { to: 'replication', reason: 'what your isolation guarantee means once there is more than one copy' },
      { to: 'concurrency-hazards', reason: 'write skew is check-then-act, wearing a database costume' },
    ],
  },
  {
    slug: 'replication',
    title: 'Replication',
    tab: 'replication',
    section: 'system-design',
    summary:
      'Leader and follower, synchronous versus asynchronous, replication lag and the reads it breaks, quorums, and what a failover actually costs you.',
    problems: [],
    connections: [
      { to: 'consistency-models', reason: 'replication lag is where abstract consistency becomes a support ticket' },
      { to: 'consensus', reason: 'who decides which replica is the leader, and what happens when they disagree' },
      { to: 'partitioning', reason: 'the other axis: replication copies data, partitioning splits it' },
    ],
  },
  {
    slug: 'partitioning',
    title: 'Partitioning & Sharding',
    tab: 'partitioning',
    section: 'system-design',
    summary:
      'Splitting data across machines: hash versus range, consistent hashing and why it exists, hot keys, rebalancing, and the secondary index problem nobody mentions.',
    problems: [],
    connections: [
      { to: 'replication', reason: 'every real system does both, and the interactions are where it hurts' },
      { to: 'caching', reason: 'a hot shard and a hot cache key are the same failure with different names' },
      { to: 'hashmap', reason: 'consistent hashing is the interview hashmap idea, stretched over a cluster' },
    ],
  },
  {
    slug: 'consistency-models',
    title: 'Consistency Models: CAP & PACELC',
    tab: 'consistency',
    section: 'system-design',
    summary:
      'Linearizable, sequential, causal, eventual — what each one promises, what it costs, and what CAP actually says as opposed to what it is usually quoted as saying.',
    problems: [],
    connections: [
      { to: 'consensus', reason: 'the price of linearizability, paid in round trips' },
      { to: 'replication', reason: 'the mechanism these models are describing' },
      { to: 'memory-model', reason: 'the identical argument one layer down: two cores disagreeing is two replicas disagreeing' },
    ],
  },
  {
    slug: 'consensus',
    title: 'Consensus: Raft, 2PC & Sagas',
    tab: 'consensus',
    section: 'system-design',
    summary:
      'Getting a cluster to agree: Raft leader election and log replication, why two-phase commit blocks, sagas and the outbox, and why a distributed lock needs a fencing token.',
    problems: [],
    connections: [
      { to: 'replication', reason: 'consensus is how a replicated log stays a single log' },
      { to: 'consistency-models', reason: 'what consensus buys, stated precisely' },
      { to: 'locks-internals', reason: 'a distributed lock is not a mutex, and this is where that bites' },
    ],
  },
  {
    slug: 'caching',
    title: 'Caching',
    tab: 'caching',
    section: 'system-design',
    summary:
      'Cache-aside, write-through, write-behind; eviction policies and why LRU is not always right; TTL jitter, stampedes, negative caching, and the invalidation problem.',
    problems: [],
    connections: [
      { to: 'memory-hierarchy', reason: 'the same idea as the CPU cache, four orders of magnitude further out' },
      { to: 'partitioning', reason: 'how a cache is spread, and what a hot key does to it' },
      { to: 'reliability', reason: 'a cache is also a dependency, and it fails in its own ways' },
    ],
  },
  {
    slug: 'queues-and-streams',
    title: 'Queues & Event Streams',
    tab: 'queues',
    section: 'system-design',
    summary:
      'Delivery guarantees and why exactly-once is a claim to read carefully; offsets and consumer groups, ordering, dead letters, change data capture, and the outbox pattern.',
    problems: [],
    connections: [
      { to: 'reliability', reason: 'a queue is the load-shedding and retry mechanism, made explicit' },
      { to: 'consistency-models', reason: 'ordering guarantees are consistency guarantees with a different vocabulary' },
      { to: 'parallelism-patterns', reason: 'a consumer group is a work-partitioning problem you have already met in-process' },
    ],
  },
  {
    slug: 'networking',
    title: 'The Network Path',
    tab: 'network_path',
    section: 'system-design',
    summary:
      'What a request actually crosses: TCP handshakes and congestion control, HTTP/1.1 vs 2 vs 3, TLS, keep-alive and pooling, and L4 versus L7 load balancing.',
    problems: [],
    connections: [
      { to: 'reliability', reason: 'every timeout you set is a statement about this path' },
      { to: 'caching', reason: 'the CDN is the first cache on it' },
      { to: 'process-and-thread', reason: 'a socket read is a syscall, and that boundary is where the cost starts' },
      { to: 'tcp-and-udp', reason: 'the handshake, the sequence numbers and the byte stream this page takes as given' },
      { to: 'dns', reason: 'the lookup that has to resolve before any of these round trips start' },
    ],
  },
  {
    slug: 'reliability',
    title: 'Timeouts, Retries & Circuit Breakers',
    tab: 'reliability',
    section: 'system-design',
    summary:
      'Keeping a system up when its dependencies are not: timeout budgets, retries with jitter, circuit breakers, bulkheads, load shedding, and rate limiting that actually works.',
    problems: [],
    connections: [
      { to: 'networking', reason: 'the failures these patterns are defending against' },
      { to: 'queues-and-streams', reason: 'the durable alternative to retrying in-process' },
      { to: 'estimation', reason: 'a timeout budget is arithmetic, not a guess' },
      { to: 'network-troubleshooting', reason: 'what to actually run when the dependency stops answering' },
    ],
  },
  {
    slug: 'design-drills',
    title: 'Design Drills',
    tab: 'drills',
    section: 'system-design',
    summary:
      'The interview set, worked end to end: clarify, estimate, sketch, then defend the tradeoffs and name how it fails.',
    problems: [
      {
        slug: 'url-shortener',
        title: 'Design a URL Shortener',
        difficulty: 'easy',
        summary: 'The classic opener: key generation, the read-heavy access pattern, and why the interesting part is not the hashing.',
      },
      {
        slug: 'rate-limiter',
        title: 'Design a Rate Limiter',
        difficulty: 'medium',
        summary: 'Token bucket versus sliding window, where the counter lives, and what happens when the limiter itself is distributed.',
      },
      {
        slug: 'news-feed',
        title: 'Design a News Feed',
        difficulty: 'medium',
        summary: 'Fan-out on write versus fan-out on read, the celebrity problem, and the hybrid every real system ends up with.',
      },
      {
        slug: 'chat-system',
        title: 'Design a Chat System',
        difficulty: 'hard',
        summary: 'Long-lived connections, delivery and read receipts, ordering within a conversation, and where the messages actually rest.',
      },
    ],
    connections: [
      { to: 'estimation', reason: 'every drill starts here, out loud, before any boxes are drawn' },
      { to: 'partitioning', reason: 'the answer to "what happens at ten times the load" in most drills' },
      { to: 'caching', reason: 'the first lever in every read-heavy drill' },
      { to: 'consistency-models', reason: 'what the interviewer is really probing when they ask about staleness' },
    ],
  },

  // ───────────────────────── deep dives ─────────────────────────
  {
    slug: 'two-sum-family',
    title: 'The Two Sum Family: Pointers vs HashMap',
    tab: 'two_sum_family',
    section: 'deep-dives',
    summary:
      'One problem, two tools: when sorting destroys information you need, when O(1) space wins, and the "key = what I need" framing.',
    problems: [],
    connections: [
      { to: 'two-pointers', reason: 'the O(1)-space side of the tradeoff, in full' },
      { to: 'hashmap', reason: 'the O(n)-space side — and why it keeps your original indices' },
    ],
  },
  {
    slug: 'pattern-recognition',
    title: 'Pattern Recognition: See X, Think Y',
    tab: 'recognition',
    section: 'deep-dives',
    summary:
      'The master decision table: read a problem statement, extract its signals, and name the pattern in under a minute.',
    problems: [],
    connections: [
      { to: 'two-pointers', reason: 'sorted + pairs → here' },
      { to: 'hashmap', reason: '"have I seen this before?" → here' },
      { to: 'sliding-window', reason: 'longest/shortest contiguous run → here' },
      { to: 'binary-search', reason: 'sorted, or a monotonic yes/no answer → here' },
      { to: 'bfs-dfs', reason: 'grids, shortest paths, reachability → here' },
      { to: 'tree-traversals', reason: 'anything shaped like a tree → here' },
      { to: 'linked-lists', reason: 'next-pointers and in-place list surgery → here' },
      { to: 'array-techniques', reason: 'subarray sums, in-place tricks, matrix walks → here' },
      { to: 'stack-queue', reason: 'matching, nesting, "next greater" → here' },
      { to: 'heap-top-k', reason: 'top K, Kth largest, streaming medians → here' },
      { to: 'trie', reason: 'prefixes, autocomplete, word dictionaries → here' },
      { to: 'backtracking', reason: 'enumerate all combinations/paths → here' },
      { to: 'dynamic-programming', reason: 'count the ways / min cost with overlapping choices → here' },
      { to: 'advanced-graphs', reason: 'dependencies, components, weighted paths → here' },
      { to: 'greedy', reason: 'one irreversible pass with a no-regret proof → here' },
      { to: 'intervals', reason: 'calendars, bookings, overlaps → here' },
      { to: 'bit-manipulation', reason: 'pairs cancel / no extra space allowed → here' },
    ],
  },

  // ───────────────────────── reference ─────────────────────────
  {
    slug: 'study-plan',
    title: 'Study Plan',
    tab: 'study_plan',
    section: 'reference',
    summary:
      'Five tracks, run in order or dipped into: the core patterns, the rest of the pattern catalogue, how the machine runs code, how the network moves bytes, and the distributed layer.',
    problems: [],
    connections: [
      { to: 'pattern-recognition', reason: 'the skill the whole plan builds toward — revisit it on day 7' },
      { to: 'cheat-sheet', reason: 'the night-before review once the plan is done' },
    ],
  },
  {
    slug: 'cheat-sheet',
    title: 'Master Cheat Sheet',
    tab: 'cheat_sheet',
    section: 'reference',
    summary:
      'Every pattern\'s recognition signals, key tricks, and traps on one page. The night-before review.',
    problems: [],
    connections: [
      { to: 'pattern-recognition', reason: 'the long-form version of this page\'s signal column' },
    ],
  },
];

/**
 * The study plan, as five independent tracks. `core` and `extended` are the
 * pattern catalogue and are the only days budgeted at 2 hours each; the
 * systems, network and design tracks are read-and-redraw days with no problems
 * to grind and no fixed budget. Day numbers are a reading order, not a deadline
 * — nobody is expected to run this end to end in a fortnight.
 * Stretch-tier problems are excluded from daily time budgets.
 */
export const studyPlan: StudyDay[] = [
  { day: 1, track: 'core', focus: 'Foundations skim + first pattern', topics: ['big-o', 'csharp-toolkit', 'two-pointers'] },
  { day: 2, track: 'core', focus: 'Dictionaries in anger + the Two Sum tradeoff', topics: ['hashmap', 'two-sum-family'] },
  { day: 3, track: 'core', focus: 'Windows + loop mechanics', topics: ['sliding-window', 'loops-and-counting'] },
  { day: 4, track: 'core', focus: 'Boundaries, not values', topics: ['binary-search'] },
  { day: 5, track: 'core', focus: 'Pointer surgery on lists', topics: ['linked-lists'] },
  { day: 6, track: 'core', focus: 'Trees two ways', topics: ['tree-traversals', 'bfs-dfs'] },
  { day: 7, track: 'core', focus: 'Array toolbox + recognition review', topics: ['array-techniques', 'pattern-recognition', 'cheat-sheet'] },
  { day: 8, track: 'extended', focus: 'Sorting payoffs + stacks', topics: ['sorting', 'stack-queue'] },
  { day: 9, track: 'extended', focus: 'Heaps and the top-K family', topics: ['heap-top-k'] },
  { day: 10, track: 'extended', focus: 'The decision tree', topics: ['backtracking'] },
  { day: 11, track: 'extended', focus: 'Prefix trees (+ Word Search II capstone)', topics: ['trie'] },
  { day: 12, track: 'extended', focus: 'DP fundamentals', topics: ['dynamic-programming'] },
  { day: 13, track: 'extended', focus: 'Graphs beyond traversal', topics: ['advanced-graphs'] },
  { day: 14, track: 'extended', focus: 'Greedy, intervals, bits — the closers', topics: ['greedy', 'intervals', 'bit-manipulation'] },
  { day: 15, track: 'systems', focus: 'The machine, from nothing', topics: ['bits-and-memory', 'process-and-thread'] },
  { day: 16, track: 'systems', focus: 'How one instruction runs', topics: ['cpu-execution'] },
  { day: 17, track: 'systems', focus: 'Where your data actually lives', topics: ['stack-and-heap'] },
  { day: 18, track: 'systems', focus: 'The address space is a lie', topics: ['virtual-memory'] },
  { day: 19, track: 'systems', focus: 'Why memory is the bottleneck', topics: ['memory-hierarchy'] },
  { day: 20, track: 'systems', focus: 'Why the CPU cheats', topics: ['cpu-pipeline'] },
  { day: 21, track: 'systems', focus: 'The collector and the bill', topics: ['gc-internals'] },
  { day: 22, track: 'systems', focus: 'What the JIT did to your code', topics: ['il-jit-codegen'] },
  { day: 23, track: 'systems', focus: 'Threads, the pool, and await', topics: ['threads-and-scheduling'] },
  { day: 24, track: 'systems', focus: 'Three guarantees, not one', topics: ['memory-model'] },
  { day: 25, track: 'systems', focus: 'Making one thing indivisible', topics: ['atomics-and-cas'] },
  { day: 26, track: 'systems', focus: 'Inside a lock', topics: ['locks-internals'] },
  { day: 27, track: 'systems', focus: 'How it goes wrong', topics: ['concurrency-hazards'] },
  { day: 28, track: 'systems', focus: 'Doing without locks', topics: ['lock-free-structures'] },
  { day: 29, track: 'systems', focus: 'Scaling past one core', topics: ['parallelism-patterns'] },
  { day: 30, track: 'network', focus: 'Packets, links, and why layers', topics: ['network-basics', 'osi-model'] },
  { day: 31, track: 'network', focus: 'One hop: frames, MACs and ARP', topics: ['link-layer'] },
  { day: 32, track: 'network', focus: 'Addresses that carry structure', topics: ['ip-and-routing'] },
  { day: 33, track: 'network', focus: 'What a port actually is', topics: ['ports-and-sockets'] },
  { day: 34, track: 'network', focus: 'A stream built on top of loss', topics: ['tcp-and-udp'] },
  { day: 35, track: 'network', focus: 'Names into addresses', topics: ['dns'] },
  { day: 36, track: 'network', focus: 'What the stack was carrying', topics: ['application-protocols'] },
  { day: 37, track: 'network', focus: 'Making it visible when it breaks', topics: ['network-troubleshooting'] },
  { day: 38, track: 'network', focus: 'The applied path, end to end', topics: ['networking'] },
  { day: 39, track: 'design', focus: 'Sizing it before building it', topics: ['estimation'] },
  { day: 40, track: 'design', focus: 'What a database does with a write', topics: ['storage-engines'] },
  { day: 41, track: 'design', focus: 'ACID past the acronym', topics: ['transactions'] },
  { day: 42, track: 'design', focus: 'More than one copy', topics: ['replication'] },
  { day: 43, track: 'design', focus: 'More than one machine', topics: ['partitioning'] },
  { day: 44, track: 'design', focus: 'What consistency costs', topics: ['consistency-models'] },
  { day: 45, track: 'design', focus: 'Getting a cluster to agree', topics: ['consensus'] },
  { day: 46, track: 'design', focus: 'Not doing the work twice', topics: ['caching'] },
  { day: 47, track: 'design', focus: 'Decoupling with queues', topics: ['queues-and-streams'] },
  { day: 48, track: 'design', focus: 'Staying up when dependencies do not', topics: ['reliability'] },
  { day: 49, track: 'design', focus: 'Putting it together under pressure', topics: ['design-drills'] },
];

// ───────────────────────── helpers ─────────────────────────

const sectionById = new Map(sections.map((s) => [s.id, s]));
const topicBySlugMap = new Map(topics.map((t) => [t.slug, t]));

export function topicBySlug(slug: string): Topic | undefined {
  return topicBySlugMap.get(slug);
}

export function topicPath(topic: Topic): string {
  return `${sectionById.get(topic.section)!.base}/${topic.slug}/`;
}

export function problemPath(topic: Topic, problem: Problem): string {
  return `${sectionById.get(topic.section)!.base}/${topic.slug}/${problem.slug}/`;
}

export function topicsInSection(id: SectionId): Topic[] {
  return topics.filter((t) => t.section === id);
}

/** Resolve a "topic-slug/problem-slug" ref (used by Problem.related). */
export function resolveProblemRef(ref: string): { topic: Topic; problem: Problem; path: string } {
  const [topicSlug, problemSlug] = ref.split('/');
  const topic = topicBySlugMap.get(topicSlug ?? '');
  const problem = topic?.problems.find((p) => p.slug === problemSlug);
  if (!topic || !problem) {
    throw new Error(`curriculum: unresolvable problem ref "${ref}"`);
  }
  return { topic, problem, path: problemPath(topic, problem) };
}

export function formatLc(lc: number | number[] | undefined): string | undefined {
  if (lc === undefined) return undefined;
  return Array.isArray(lc) ? lc.join(' · ') : String(lc);
}

/** Flat reading order: foundations → core → structures → advanced → deep dives → reference */
export const readingOrder: Topic[] = sections.flatMap((s) => topicsInSection(s.id));

export interface PageRef {
  title: string;
  path: string;
}

/** prev/next across the full reading order, descending into problems */
export function flatPages(): PageRef[] {
  const pages: PageRef[] = [];
  for (const t of readingOrder) {
    pages.push({ title: t.title, path: topicPath(t) });
    for (const p of t.problems) pages.push({ title: p.title, path: problemPath(t, p) });
  }
  return pages;
}

const normalize = (p: string) => (p.endsWith('/') ? p.slice(0, -1) : p);

export function prevNext(path: string): { prev?: PageRef; next?: PageRef } {
  const pages = flatPages();
  const i = pages.findIndex((p) => normalize(p.path) === normalize(path));
  if (i === -1) return {};
  return { prev: pages[i - 1], next: pages[i + 1] };
}

// ───────────────────────── validation ─────────────────────────
// Runs once at module load (i.e. during `astro build`): a broken link graph
// fails the build instead of shipping.

function validate(): void {
  const slugsSeen = new Set<string>();
  for (const t of topics) {
    if (slugsSeen.has(t.slug)) throw new Error(`curriculum: duplicate topic slug "${t.slug}"`);
    slugsSeen.add(t.slug);
    const problemSlugs = new Set<string>();
    for (const p of t.problems) {
      if (problemSlugs.has(p.slug)) {
        throw new Error(`curriculum: duplicate problem slug "${t.slug}/${p.slug}"`);
      }
      problemSlugs.add(p.slug);
    }
  }

  const inbound = new Map<string, number>(topics.map((t) => [t.slug, 0]));
  for (const t of topics) {
    for (const c of t.connections) {
      if (!topicBySlugMap.has(c.to)) {
        throw new Error(`curriculum: topic "${t.slug}" connects to unknown slug "${c.to}"`);
      }
      if (c.to === t.slug) throw new Error(`curriculum: topic "${t.slug}" connects to itself`);
      inbound.set(c.to, (inbound.get(c.to) ?? 0) + 1);
    }
    for (const p of t.problems) {
      for (const ref of p.related ?? []) resolveProblemRef(ref); // throws if broken
    }
  }

  // Every learnable topic must be reachable through at least one connections panel.
  for (const t of topics) {
    if (t.section === 'reference') continue;
    if ((inbound.get(t.slug) ?? 0) === 0) {
      throw new Error(`curriculum: topic "${t.slug}" has no inbound connections — it is unreachable`);
    }
  }

  for (const d of studyPlan) {
    for (const slug of d.topics) {
      if (!topicBySlugMap.has(slug)) {
        throw new Error(`curriculum: study plan day ${d.day} references unknown topic "${slug}"`);
      }
    }
  }
}

validate();
