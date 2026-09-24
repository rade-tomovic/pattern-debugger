# Original Teaching Request (requirements doc)

## About the learner

Senior Software Engineer, 11 years experience, primarily .NET back-end. Strong on system
design and real-world engineering; needs sharper algorithm pattern recognition for coding
interviews. All code must be **C#** (modern syntax — collection expressions, tuples, pattern
matching, etc.).

## Patterns to cover (in this order)

1. **Two Pointers** — opposite ends, same direction (slow/fast), expand from middle
2. **HashMap / Frequency Counting** — value→index, value→count, prefixSum→count, HashSet
3. **Sliding Window** — fixed size and variable size
4. **Binary Search** — exact match, boundary finding (first true / last true), binary search on the answer
5. **BFS & DFS** — graph traversal, grid problems, multi-source BFS
6. **Tree Traversals** — preorder, inorder, postorder (recursive and iterative with a stack)
7. **Linked Lists** — fast/slow pointers, dummy head, reverse, merge, combo patterns
8. **Array Techniques** — prefix sum, in-place read/write, Dutch National Flag, Kadane, Boyer-Moore voting

## Structure for each pattern

1. **Core idea** in 2-3 sentences — what it is, when to use it
2. **Template/skeleton** code in C#
3. **2-4 problems** per pattern, easy → hard. For each problem:
   - State the task clearly
   - "How to think" — reasoning and intuition before writing code
   - Full C# solution
   - **Step-by-step trace** with a concrete example — array state, pointers, data structures at each iteration
   - "Why it works" — the key insight that makes the approach correct
4. **Cheat sheet** at the end — when to recognize the pattern, key tricks, common bugs
5. **Connections** between patterns (e.g. sliding window uses a hashmap inside; Two Pointers
   vs HashMap for Two Sum and when to prefer which)

## Additional topics (after the patterns)

**Looping and counting tips/tricks:**
- When to use `for` vs `while`
- Loop boundary rules: `<` vs `<=` and when each is correct (arrays, two pointers, binary search)
- Middle element handling: odd vs even length, left-mid vs right-mid, when middle is skipped naturally
- Ceiling vs floor division and the `(a + b - 1) / b` trick
- Circular array indexing with modulo
- Direction arrays for grid problems
- Mirror iteration for comparing from both ends
- Common loop bugs (off-by-one, infinite loops in binary search, negative modulo)
- The "trace with 0, 1, 2 elements" debugging technique

**Two Sum tradeoff analysis:**
- Two Pointers (sorted, O(1) space) vs HashMap (unsorted, O(n) space)
- When sorting destroys information you need (original indices)
- The "key = what I need" framing for the HashMap approach

## Teaching style

- Direct and practical. Skip theory proofs unless the "why it works" is non-obvious.
- Step-by-step traces are essential — never skip them. Show pointer positions, array state,
  dictionary contents at each step.
- Tables for comparisons and quick references.
- Universal template first, then each problem as an instance of the template.
- When patterns connect, explicitly call it out.

## Study plan context

Roughly 7 days × 2 hours per day for the core track. A suggested schedule is helpful.

## Scope expansion (agreed 2026-08-27)

Beyond the 8 core patterns, the site also covers — good enough for ~90% of companies:
- Stack/Queue/Heap/Trie (incl. monotonic stack, top-K)
- Backtracking + DP basics
- Advanced graphs (topological sort, union-find, Dijkstra primer)
- Greedy, intervals, bit manipulation, sorting overview, Big-O fundamentals
