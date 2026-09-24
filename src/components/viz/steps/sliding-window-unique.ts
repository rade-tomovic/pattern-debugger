/**
 * Longest Substring Without Repeating Characters — variable sliding window.
 * Faithful port of the artifact's `slidingWindowSteps()`: same input
 * ('abcabcbb'), same note text, same seen-set readout in the watch panel.
 * R expands every iteration; L shrinks (warn on the evicted cell) while the
 * incoming character is already in the window.
 *
 * Used on: /patterns/sliding-window/longest-substring-without-repeating/
 * (visualizer: 'sliding-window-unique')
 */
import { P, type ArrayStep, type CellState } from '../types';

export function buildSteps(): ArrayStep[] {
  const s = 'abcabcbb'.split('');
  const steps: ArrayStep[] = [];
  const seen = new Set<string>();
  let L = 0;
  let best = 0;

  const win = (l: number, r: number): Record<number, CellState> => {
    const c: Record<number, CellState> = {};
    for (let i = l; i <= r; i++) c[i] = 'win';
    return c;
  };
  const fmtSeen = () => '{' + [...seen].join(',') + '}';

  steps.push({
    data: s,
    note: 'Find longest substring without repeats. R expands the window, L shrinks it when a duplicate enters.',
    pointers: { 0: [P('L', 'amber'), P('R', 'cyan')] },
    vars: { L: 0, R: 0, best: 0, seen: '{}' },
  });

  for (let R = 0; R < s.length; R++) {
    while (seen.has(s[R]!)) {
      steps.push({
        data: s,
        note: `'${s[R]}' already in window → SHRINK: remove '${s[L]}', L++`,
        pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')] },
        cells: { ...win(L, R - 1), [L]: 'warn' },
        vars: { L, R, best, seen: fmtSeen() },
      });
      seen.delete(s[L]!);
      L++;
    }
    seen.add(s[R]!);
    const len = R - L + 1;
    const wasBest = len > best;
    best = Math.max(best, len);
    steps.push({
      data: s,
      note: `EXPAND: add '${s[R]}'. Window "${s.slice(L, R + 1).join('')}" len=${len}${wasBest ? ' → new best!' : ''}`,
      pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')] },
      cells: win(L, R),
      vars: { L, R, best, seen: fmtSeen() },
    });
  }

  steps.push({
    data: s,
    done: true,
    note: `Done. Longest unique substring length = ${best}. Each char added & removed at most once → O(n).`,
    cells: {},
    vars: { best },
  });

  return steps;
}
