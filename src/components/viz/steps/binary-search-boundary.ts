/**
 * Find First and Last Position — binary search for a BOUNDARY, not a value.
 * Faithful port of the artifact's `binarySearchSteps()`: same input
 * (a=[5,7,7,8,8,8,10], t=8), same note text. On a match, record the answer
 * but keep searching LEFT (R = mid - 1) for an earlier occurrence.
 *
 * Used on: /patterns/binary-search/first-and-last-position/
 * (visualizer: 'binary-search-boundary')
 */
import { P, type ArrayStep, type CellState } from '../types';

export function buildSteps(): ArrayStep[] {
  const a = [5, 7, 7, 8, 8, 8, 10];
  const t = 8;
  const steps: ArrayStep[] = [];
  let L = 0;
  let R = a.length - 1;
  let ans = -1;

  steps.push({
    data: a,
    note: `Find FIRST position of ${t}. On a match: record it, then keep searching LEFT.`,
    pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')] },
    vars: { L, R, ans },
  });

  while (L <= R) {
    const mid = L + ((R - L) >> 1);
    const dim: Record<number, CellState> = {};
    for (let i = 0; i < a.length; i++) if (i < L || i > R) dim[i] = 'dim';

    if (a[mid] === t) {
      ans = mid;
      steps.push({
        data: a,
        note: `mid=${mid}: a[${mid}]=${a[mid]} == ${t} → ans=${mid}, but look LEFT for earlier → R = mid−1`,
        pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')], [mid]: [P('mid', 'purple')] },
        cells: { ...dim, [mid]: 'mid' },
        vars: { L, R, mid, ans },
      });
      R = mid - 1;
    } else if (a[mid]! < t) {
      steps.push({
        data: a,
        note: `mid=${mid}: a[${mid}]=${a[mid]} < ${t} → L = mid+1`,
        pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')], [mid]: [P('mid', 'purple')] },
        cells: { ...dim, [mid]: 'warn' },
        vars: { L, R, mid, ans },
      });
      L = mid + 1;
    } else {
      steps.push({
        data: a,
        note: `mid=${mid}: a[${mid}]=${a[mid]} > ${t} → R = mid−1`,
        pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')], [mid]: [P('mid', 'purple')] },
        cells: { ...dim, [mid]: 'warn' },
        vars: { L, R, mid, ans },
      });
      R = mid - 1;
    }
  }

  steps.push({
    data: a,
    done: true,
    note: `L > R → exit. First occurrence of ${t} is index ${ans} ✓`,
    pointers: { [ans]: [P('ans', 'green')] },
    cells: { [ans]: 'ok' },
    vars: { ans },
  });

  return steps;
}
