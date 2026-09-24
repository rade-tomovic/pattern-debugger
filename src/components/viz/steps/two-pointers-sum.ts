/**
 * Two Sum II (sorted input) — opposite-end pointers.
 * Faithful port of the artifact's `twoPointersSteps()`: same input, same
 * note text, same snapshot-before-move rhythm (each step's note narrates
 * the decision, then the pointer moves for the next step).
 *
 * Used on: /patterns/two-pointers/two-sum-ii/ (visualizer: 'two-pointers-sum')
 */
import { P, type ArrayStep } from '../types';

export function buildSteps(): ArrayStep[] {
  const a = [1, 3, 5, 7, 11];
  const t = 10;
  const steps: ArrayStep[] = [];
  let L = 0;
  let R = 4;

  steps.push({
    data: a,
    note: `Start. Array is SORTED — that's the license to use opposite-end pointers. Target = ${t}.`,
    pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')] },
    vars: { L, R, target: t },
  });

  while (L < R) {
    const sum = a[L]! + a[R]!;
    if (sum === t) {
      steps.push({
        data: a,
        done: true,
        note: `${a[L]} + ${a[R]} = ${sum} == target → return [${L}, ${R}] ✓`,
        pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')] },
        cells: { [L]: 'ok', [R]: 'ok' },
        vars: { L, R, sum },
      });
      break;
    }
    if (sum < t) {
      steps.push({
        data: a,
        note: `${a[L]} + ${a[R]} = ${sum} < ${t} → need bigger → L++ (everything paired with ${a[L]} is too small)`,
        pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')] },
        cells: { [L]: 'warn' },
        vars: { L, R, sum },
      });
      L++;
    } else {
      steps.push({
        data: a,
        note: `${a[L]} + ${a[R]} = ${sum} > ${t} → need smaller → R-- (everything paired with ${a[R]} is too big)`,
        pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')] },
        cells: { [R]: 'warn' },
        vars: { L, R, sum },
      });
      R--;
    }
  }

  return steps;
}
