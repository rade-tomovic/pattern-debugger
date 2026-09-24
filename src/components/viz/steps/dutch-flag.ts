/**
 * Sort Colors (Dutch National Flag) — three regions, three pointers, one
 * pass. Not in the artifact; written fresh in its voice.
 *
 * low/mid/high partition the array into four zones as they move:
 *   [0, low)     settled 0s        [low, mid)   settled 1s
 *   [mid, high]  unknown           (high, n)    settled 2s
 * On a 0: swap with low, advance BOTH low and mid (the swapped-in value at
 * mid, from low's old slot, is already known-classified — it was a 1 or
 * already scanned). On a 2: swap with high, advance ONLY high — mid does
 * NOT advance, because the value swapped in from high is unclassified and
 * must be examined next.
 *
 * Input a=[2,0,2,1,1,0] — matches the topic page's own Sort Colors example.
 *
 * Used on: /patterns/array-techniques/sort-colors/ (visualizer: 'dutch-flag')
 */
import { P, type ArrayStep, type CellState, type PointerBadge } from '../types';

export function buildSteps(): ArrayStep[] {
  const a = [2, 0, 2, 1, 1, 0];
  const n = a.length;
  const steps: ArrayStep[] = [];
  let low = 0;
  let mid = 0;
  let high = n - 1;

  const badges = (): Record<number, PointerBadge[]> => {
    const m: Record<number, PointerBadge[]> = {};
    const add = (i: number, b: PointerBadge) => {
      m[i] = (m[i] ?? []).concat([b]);
    };
    add(low, P('low', 'amber'));
    add(mid, P('mid', 'purple'));
    add(high, P('high', 'cyan'));
    return m;
  };

  steps.push({
    data: [...a],
    note: 'Sort Colors: partition into [0s | 1s | unknown | 2s] in one pass. low/mid walk up, high walks down.',
    pointers: badges(),
    vars: { low, mid, high },
  });

  while (mid <= high) {
    const v = a[mid]!;
    if (v === 0) {
      steps.push({
        data: [...a],
        note: `a[mid]=0 → swap with low (index ${low}) → grows the 0-region → low++, mid++`,
        pointers: badges(),
        cells: { [low]: 'win', [mid]: 'win' },
        vars: { low, mid, high },
      });
      const tmp = a[low]!;
      a[low] = a[mid]!;
      a[mid] = tmp;
      low++;
      mid++;
    } else if (v === 1) {
      steps.push({
        data: [...a],
        note: `a[mid]=1 → already in the middle region, correctly placed → mid++ only`,
        pointers: badges(),
        cells: { [mid]: 'win' },
        vars: { low, mid, high },
      });
      mid++;
    } else {
      steps.push({
        data: [...a],
        note: `a[mid]=2 → swap with high (index ${high}) → shrinks the unknown region → high-- (mid does NOT advance — the swapped-in value from high is unclassified, must be examined next)`,
        pointers: badges(),
        cells: { [mid]: 'warn', [high]: 'warn' },
        vars: { low, mid, high },
      });
      const tmp = a[mid]!;
      a[mid] = a[high]!;
      a[high] = tmp;
      high--;
    }
  }

  const c: Record<number, CellState> = {};
  for (let k = 0; k < n; k++) c[k] = 'ok';
  steps.push({
    data: [...a],
    done: true,
    note: `mid > high → all three regions settled: [${a.join(', ')}] ✓`,
    cells: c,
    vars: { result: `[${a.join(',')}]` },
  });

  return steps;
}
