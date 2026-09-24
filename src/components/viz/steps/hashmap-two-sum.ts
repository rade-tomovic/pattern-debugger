/**
 * Two Sum (unsorted) — hashmap "key is what I need".
 * Faithful port of the artifact's `hashMapSteps()`: same input, same note
 * text, same fmt() readout of the `need` dictionary in the watch panel.
 * i badge amber (write/current), the resolved answer pair badge + cells green.
 *
 * Used on: /patterns/hashmap/two-sum/ (visualizer: 'hashmap-two-sum')
 */
import { P, type ArrayStep } from '../types';

export function buildSteps(): ArrayStep[] {
  const a = [2, 5, 9, 1, 4];
  const t = 6;
  const steps: ArrayStep[] = [];
  const need: Record<number, number> = {};
  const fmt = () => '{' + Object.entries(need).map(([k, v]) => `${k}→${v}`).join(', ') + '}';

  steps.push({
    data: a,
    note: `Two Sum unsorted, target ${t}. Dictionary key = WHAT I NEED, value = index of who needs it.`,
    vars: { need: '{}' },
  });

  for (let i = 0; i < a.length; i++) {
    const want = a[i]!;
    const j = need[want];
    if (j !== undefined) {
      steps.push({
        data: a,
        done: true,
        note: `a[${i}]=${want} — someone was waiting for a ${want}! (index ${j} needed it) → return [${j}, ${i}] ✓`,
        pointers: { [i]: [P('i', 'amber')], [j]: [P('ans', 'green')] },
        cells: { [i]: 'ok', [j]: 'ok' },
        vars: { i, need: fmt() },
      });
      return steps;
    }
    need[t - want] = i;
    steps.push({
      data: a,
      note: `a[${i}]=${want}: is ${want} in need? No. Record: "I need a ${t - want}" → need[${t - want}] = ${i}`,
      pointers: { [i]: [P('i', 'amber')] },
      cells: { [i]: 'win' },
      vars: { i, need: fmt() },
    });
  }

  return steps;
}
