/**
 * id → { kind, buildSteps } for every interactive visualizer referenced by
 * `visualizer:` in src/data/curriculum.ts (and listed in PLAN.md).
 *
 * Steps are built at BUILD time inside Viz.astro — an unknown id fails
 * `astro build` with the known-id list (see Viz.astro), so a page can never
 * silently ship a broken player.
 *
 * Faithful ports of the artifact's step builders (same input, same note
 * text): two-pointers-sum, fast-slow-middle, bfs-islands, hashmap-two-sum,
 * sliding-window-unique, binary-search-boundary, kadane.
 *
 * Written fresh, in the artifact's teaching voice, for patterns it didn't
 * cover: two-pointers-palindrome, reverse-list, binary-search-classic,
 * dutch-flag, monotonic-stack, topo-sort.
 *
 * Contract per builder: > 3 steps · `vars` on every step · last step
 * `done: true` · pointer colors follow the site law (amber=L/slow/i/write,
 * cyan=R/fast/read, purple=mid, green=success, red=reject/shrink).
 */
import type { VizSpec } from './types';
import { buildSteps as twoPointersSum } from './steps/two-pointers-sum';
import { buildSteps as twoPointersPalindrome } from './steps/two-pointers-palindrome';
import { buildSteps as fastSlowMiddle } from './steps/fast-slow-middle';
import { buildSteps as reverseList } from './steps/reverse-list';
import { buildSteps as bfsIslands } from './steps/bfs-islands';
import { buildSteps as hashmapTwoSum } from './steps/hashmap-two-sum';
import { buildSteps as slidingWindowUnique } from './steps/sliding-window-unique';
import { buildSteps as binarySearchClassic } from './steps/binary-search-classic';
import { buildSteps as binarySearchBoundary } from './steps/binary-search-boundary';
import { buildSteps as kadane } from './steps/kadane';
import { buildSteps as dutchFlag } from './steps/dutch-flag';
import { buildSteps as monotonicStack } from './steps/monotonic-stack';
import { buildSteps as topoSort } from './steps/topo-sort';

export const registry: Record<string, VizSpec> = {
  // ── two pointers ─────────────────────────────────────────────────────
  'two-pointers-palindrome': { kind: 'array', buildSteps: twoPointersPalindrome },
  'two-pointers-sum': { kind: 'array', buildSteps: twoPointersSum },

  // ── hashmap ──────────────────────────────────────────────────────────
  'hashmap-two-sum': { kind: 'array', buildSteps: hashmapTwoSum },

  // ── sliding window ───────────────────────────────────────────────────
  'sliding-window-unique': { kind: 'array', buildSteps: slidingWindowUnique },

  // ── binary search ────────────────────────────────────────────────────
  'binary-search-classic': { kind: 'array', buildSteps: binarySearchClassic },
  'binary-search-boundary': { kind: 'array', buildSteps: binarySearchBoundary },

  // ── linked lists ─────────────────────────────────────────────────────
  'fast-slow-middle': { kind: 'list', buildSteps: fastSlowMiddle },
  'reverse-list': { kind: 'list', buildSteps: reverseList },

  // ── array techniques ─────────────────────────────────────────────────
  kadane: { kind: 'array', buildSteps: kadane },
  'dutch-flag': { kind: 'array', buildSteps: dutchFlag },

  // ── bfs / grids ──────────────────────────────────────────────────────
  'bfs-islands': { kind: 'grid', buildSteps: bfsIslands },

  // ── stack & queue ────────────────────────────────────────────────────
  'monotonic-stack': { kind: 'array', buildSteps: monotonicStack },

  // ── graphs ───────────────────────────────────────────────────────────
  'topo-sort': { kind: 'array', buildSteps: topoSort },
};
