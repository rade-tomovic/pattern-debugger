/**
 * Middle of the Linked List — fast & slow pointers.
 * Faithful port of the artifact's `fastSlowSteps()`: same list, same note
 * text. The artifact's loop guard `f+1 < a.length && f+2 <= a.length` is
 * two spellings of `f <= n-2`, simplified here to one; the emitted steps
 * are byte-identical.
 *
 * Used on: /patterns/linked-lists/middle-of-linked-list/ (visualizer: 'fast-slow-middle')
 */
import { P, type ListStep, type PointerBadge } from '../types';

export function buildSteps(): ListStep[] {
  const a = [1, 2, 3, 4, 5];
  const steps: ListStep[] = [];
  let s = 0;
  let f = 0;

  steps.push({
    data: a,
    note: 'Find middle: slow moves 1 step, fast moves 2. When fast hits the end, slow is at the middle.',
    pointers: { 0: [P('S', 'amber'), P('F', 'cyan')] },
    vars: { slow: 1, fast: 1 },
  });

  while (f + 1 < a.length) {
    s++;
    f = Math.min(f + 2, a.length - 1);
    const p: Record<number, PointerBadge[]> = {};
    p[s] = [P('S', 'amber')];
    p[f] = (p[f] ?? []).concat([P('F', 'cyan')]);
    steps.push({
      data: a,
      note: `slow → node ${a[s]}, fast → node ${a[f]}${f >= a.length - 1 ? ' (fast reached the end)' : ''}`,
      pointers: p,
      vars: { slow: a[s]!, fast: a[f]! },
    });
    if (f >= a.length - 1) break;
  }

  steps.push({
    data: a,
    done: true,
    note: `fast.next is null → stop. Middle = node ${a[s]} ✓ (fast at 2× speed ⇒ slow at ½ distance)`,
    pointers: { [s]: [P('S', 'amber')] },
    cells: { [s]: 'ok' },
    vars: { middle: a[s]! },
  });

  return steps;
}
