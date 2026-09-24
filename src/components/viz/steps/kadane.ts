/**
 * Maximum Subarray — Kadane's algorithm.
 * Faithful port of the artifact's `kadaneSteps()`: same input
 * (a=[-2,1,-3,4,-1,2,1,-5,4]), same note text. At every element: extend the
 * running subarray, or start fresh because the running sum was dragging it
 * down. `win` cells trace the current running subarray; `ok` cells mark the
 * best one at the end.
 *
 * Used on: /patterns/array-techniques/maximum-subarray/ (visualizer: 'kadane')
 */
import { P, type ArrayStep, type CellState } from '../types';

export function buildSteps(): ArrayStep[] {
  const a = [-2, 1, -3, 4, -1, 2, 1, -5, 4];
  const steps: ArrayStep[] = [];
  let cur = a[0]!;
  let best = a[0]!;
  let start = 0;
  let bs = 0;
  let be = 0;

  steps.push({
    data: a,
    note: 'Kadane: at each element decide — extend the running subarray, or start fresh here?',
    pointers: { 0: [P('i', 'amber')] },
    cells: { 0: 'win' },
    vars: { current: cur, max: best },
  });

  for (let i = 1; i < a.length; i++) {
    const ext = cur + a[i]!;
    const fresh = a[i]! > ext;
    if (fresh) {
      cur = a[i]!;
      start = i;
    } else {
      cur = ext;
    }
    const wasBest = cur > best;
    if (wasBest) {
      best = cur;
      bs = start;
      be = i;
    }
    const c: Record<number, CellState> = {};
    for (let k = start; k <= i; k++) c[k] = 'win';
    steps.push({
      data: a,
      note: fresh
        ? `i=${i}: max(${a[i]}, ${ext}) → ${a[i]} — running sum was dragging us down, START FRESH`
        : `i=${i}: max(${a[i]}, ${ext}) → ${ext} — EXTEND${wasBest ? ' → new max!' : ''}`,
      pointers: { [i]: [P('i', 'amber')] },
      cells: c,
      vars: { current: cur, max: best },
    });
  }

  const c: Record<number, CellState> = {};
  for (let k = bs; k <= be; k++) c[k] = 'ok';
  steps.push({
    data: a,
    done: true,
    note: `Done. Max subarray sum = ${best} → [${a.slice(bs, be + 1).join(', ')}] ✓`,
    cells: c,
    vars: { max: best },
  });

  return steps;
}
