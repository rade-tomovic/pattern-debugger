/**
 * Reverse Linked List — the four-line mantra: save next, flip arrow,
 * advance prev, advance curr. Not in the artifact; written fresh in its
 * voice. Node positions stay fixed while prev/curr/next badges walk the
 * list (arrows can't visually flip in this renderer), and the note text
 * narrates the actual pointer surgery at each node; the `win` trail marks
 * how much of the list has been rewired so far. The final `done` step shows
 * the list in its true reversed order.
 *
 * Input: [1, 2, 3, 4, 5] — matches the topic page's own example.
 *
 * Used on: /patterns/linked-lists/reverse-linked-list/ (visualizer: 'reverse-list')
 */
import { P, type CellState, type ListStep, type PointerBadge } from '../types';

export function buildSteps(): ListStep[] {
  const orig = [1, 2, 3, 4, 5];
  const n = orig.length;
  const steps: ListStep[] = [];

  const badges = (prev: number | null, curr: number, next: number | null): Record<number, PointerBadge[]> => {
    const m: Record<number, PointerBadge[]> = {};
    const add = (i: number | null, b: PointerBadge) => {
      if (i === null) return;
      m[i] = (m[i] ?? []).concat([b]);
    };
    add(prev, P('prev', 'amber'));
    add(curr, P('curr', 'cyan'));
    add(next, P('next', 'purple'));
    return m;
  };

  const trail = (upto: number): Record<number, CellState> => {
    const c: Record<number, CellState> = {};
    for (let k = 0; k <= upto; k++) c[k] = 'win';
    return c;
  };

  let prev: number | null = null;
  let curr = 0;

  steps.push({
    data: orig,
    note: 'Reverse the list: prev = null, curr = head. Mantra for every node: save next, flip the arrow, advance prev, advance curr.',
    pointers: badges(prev, curr, n > 1 ? 1 : null),
    vars: { prev: 'null', curr: orig[curr]! },
  });

  while (curr < n) {
    const next: number | null = curr + 1 < n ? curr + 1 : null;
    const prevLabel = prev === null ? 'null' : String(orig[prev]!);
    const nextLabel = next === null ? 'null' : String(orig[next]!);
    steps.push({
      data: orig,
      note: `At node ${orig[curr]}: save next=${nextLabel} → flip node ${orig[curr]}.next = ${prevLabel} → advance prev to ${orig[curr]}, curr to ${nextLabel}`,
      pointers: badges(prev, curr, next),
      cells: trail(curr),
      vars: { prev: prevLabel, curr: orig[curr]!, next: nextLabel },
    });
    prev = curr;
    curr = next === null ? n : next;
  }

  const reversed = [...orig].reverse();
  steps.push({
    data: reversed,
    done: true,
    note: `curr = null → stop. prev is the new head. List fully reversed: ${reversed.join(' → ')} ✓`,
    vars: { newHead: reversed[0]! },
  });

  return steps;
}
