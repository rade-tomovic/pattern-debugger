/**
 * Classic Binary Search (LC 704) — the canonical `while (L <= R)` exact-match
 * loop and the overflow-safe midpoint. Not in the artifact; written fresh in
 * its voice. Cells outside the current [L, R] window fade (dim); the
 * midpoint is purple; a hit scales up green.
 *
 * Input a=[-4,-1,0,3,5,9,12,15,20], target=9 — a slightly wider array than
 * the topic page's own [-1,0,3,5,9,12] example so the trace covers more
 * than one midpoint (two "keep going" steps before the hit).
 *
 * Used on: /patterns/binary-search/classic-binary-search/
 * (visualizer: 'binary-search-classic')
 */
import { P, type ArrayStep, type CellState } from '../types';

export function buildSteps(): ArrayStep[] {
  const a = [-4, -1, 0, 3, 5, 9, 12, 15, 20];
  const t = 9;
  const steps: ArrayStep[] = [];
  let L = 0;
  let R = a.length - 1;

  const dimOutside = (): Record<number, CellState> => {
    const c: Record<number, CellState> = {};
    for (let i = 0; i < a.length; i++) if (i < L || i > R) c[i] = 'dim';
    return c;
  };

  steps.push({
    data: a,
    note: `Classic binary search: find ${t} in a SORTED array. mid = L + (R−L)/2 avoids the (L+R)/2 overflow — halve the search space every step.`,
    pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')] },
    vars: { L, R, target: t },
  });

  while (L <= R) {
    const mid = L + ((R - L) >> 1);
    if (a[mid] === t) {
      steps.push({
        data: a,
        done: true,
        note: `mid=${mid}: a[${mid}]=${a[mid]} == ${t} → FOUND → return ${mid} ✓`,
        pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')], [mid]: [P('mid', 'purple')] },
        cells: { ...dimOutside(), [mid]: 'ok' },
        vars: { L, R, mid },
      });
      return steps;
    }
    if (a[mid]! < t) {
      steps.push({
        data: a,
        note: `mid=${mid}: a[${mid}]=${a[mid]} < ${t} → target is to the right → L = mid + 1`,
        pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')], [mid]: [P('mid', 'purple')] },
        cells: { ...dimOutside(), [mid]: 'mid' },
        vars: { L, R, mid },
      });
      L = mid + 1;
    } else {
      steps.push({
        data: a,
        note: `mid=${mid}: a[${mid}]=${a[mid]} > ${t} → target is to the left → R = mid − 1`,
        pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')], [mid]: [P('mid', 'purple')] },
        cells: { ...dimOutside(), [mid]: 'mid' },
        vars: { L, R, mid },
      });
      R = mid - 1;
    }
  }

  steps.push({
    data: a,
    done: true,
    note: `L > R → search space exhausted → ${t} not found → return -1`,
    cells: dimOutside(),
    vars: { L, R, result: -1 },
  });

  return steps;
}
