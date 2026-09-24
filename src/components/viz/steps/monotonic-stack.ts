/**
 * Daily Temperatures — monotonic (decreasing) stack of waiting indices.
 * Not in the artifact; written fresh in its voice.
 *
 * The stack holds indices whose "how many days until warmer" answer isn't
 * known yet, temperatures strictly decreasing bottom to top. A warmer day
 * resolves everyone shorter than it, back to front — each resolution is
 * O(1) amortized because every index is pushed and popped at most once.
 *
 * Input temps=[73,74,75,71,69,72,76,73] — matches the topic page's own
 * Daily Temperatures example; final answer [1,1,4,2,1,1,0,0].
 *
 * Used on: /patterns/stack-queue/daily-temperatures/ (visualizer: 'monotonic-stack')
 */
import { P, type ArrayStep, type AuxItem, type CellState } from '../types';

export function buildSteps(): ArrayStep[] {
  const temps = [73, 74, 75, 71, 69, 72, 76, 73];
  const n = temps.length;
  const ans: number[] = new Array(n).fill(-1); // -1 = not yet resolved
  const stack: number[] = []; // indices, temps strictly decreasing bottom→top
  const steps: ArrayStep[] = [];

  const fmtAns = () => ans.map((v) => (v === -1 ? '·' : String(v))).join(',');
  const stackItems = (redIdx?: number): AuxItem[] =>
    stack.map((idx) => ({ v: idx, c: idx === redIdx ? 'red' : 'amber' }));

  steps.push({
    data: temps,
    note: 'Daily Temperatures: a DECREASING stack of waiting indices. A warmer day resolves everyone shorter than it, back to front.',
    aux: [{ label: 'stack', items: [] }],
    vars: { i: '—', answers: fmtAns() },
  });

  for (let i = 0; i < n; i++) {
    while (stack.length > 0 && temps[i]! > temps[stack[stack.length - 1]!]!) {
      const j = stack[stack.length - 1]!;
      steps.push({
        data: temps,
        note: `temp[${i}]=${temps[i]} > temp[${j}]=${temps[j]} → index ${j} waited ${i - j} day(s) for warmth → ans[${j}] = ${i - j}, pop`,
        pointers: { [i]: [P('i', 'cyan')], [j]: [P('j', 'amber')] },
        cells: { [i]: 'win', [j]: 'warn' },
        aux: [{ label: 'stack', items: stackItems(j) }],
        vars: { i, j, answers: fmtAns() },
      });
      stack.pop();
      ans[j] = i - j;
    }
    stack.push(i);
    steps.push({
      data: temps,
      note: `temp[${i}]=${temps[i]}: no warmer day seen yet → push ${i} onto the stack (wait)`,
      pointers: { [i]: [P('i', 'cyan')] },
      cells: { [i]: 'win' },
      aux: [{ label: 'stack', items: stackItems() }],
      vars: { i, answers: fmtAns() },
    });
  }

  for (const idx of stack) ans[idx] = 0; // never resolved → no warmer day ahead

  const c: Record<number, CellState> = {};
  for (let k = 0; k < n; k++) c[k] = 'ok';
  steps.push({
    data: temps,
    done: true,
    note: `Scan complete. Anything left on the stack never saw a warmer day → ans=0 there. Final: [${ans.join(', ')}] ✓`,
    cells: c,
    aux: [{ label: 'stack', items: [] }],
    vars: { answers: ans.join(',') },
  });

  return steps;
}
