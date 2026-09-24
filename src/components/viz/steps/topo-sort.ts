/**
 * Course Schedule II — topological sort via Kahn's algorithm (BFS with an
 * in-degree count instead of a visited set). Not in the artifact; written
 * fresh in its voice.
 *
 * Cells are course nodes; the value shown in each cell is its LIVE
 * in-degree (prerequisites still owed), not a fixed label — watch it drop
 * to 0 and the course join the queue. Two aux rows track the mechanism:
 * 'queue' (ready-to-take, not yet ordered) and 'order' (the answer,
 * building left to right).
 *
 * Graph: LC 210's own example 2 — numCourses=4,
 * prerequisites=[[1,0],[2,0],[3,1],[3,2]] (course 1 needs 0, course 2 needs
 * 0, course 3 needs 1 and 2) → edges (prerequisite→course) 0→1, 0→2, 1→3,
 * 2→3. A valid order exists (no cycle): [0, 1, 2, 3].
 *
 * Used on: /patterns/advanced-graphs/course-schedule-ii/ (visualizer: 'topo-sort')
 */
import type { ArrayStep, AuxItem, CellState } from '../types';

export function buildSteps(): ArrayStep[] {
  const numCourses = 4;
  // [from, to] = "from is a prerequisite of to" — derived from LC 210's
  // prerequisites=[[1,0],[2,0],[3,1],[3,2]] (pairs are [course, prereq]).
  const edges: [number, number][] = [
    [0, 1],
    [0, 2],
    [1, 3],
    [2, 3],
  ];
  const adj: number[][] = Array.from({ length: numCourses }, () => []);
  const indeg: number[] = new Array(numCourses).fill(0);
  for (const [from, to] of edges) {
    adj[from]!.push(to);
    indeg[to]!++;
  }

  const steps: ArrayStep[] = [];
  const queue: number[] = [];
  const order: number[] = [];

  const queueItems = (): AuxItem[] => queue.map((v) => ({ v, c: 'amber' as const }));
  const orderItems = (): AuxItem[] => order.map((v) => ({ v, c: 'green' as const }));
  const cellsFor = (highlight?: { idx: number; c: CellState }): Record<number, CellState> => {
    const c: Record<number, CellState> = {};
    for (const v of order) c[v] = 'ok';
    for (const v of queue) if (!(v in c)) c[v] = 'win';
    if (highlight) c[highlight.idx] = highlight.c;
    return c;
  };
  const auxRows = () => [
    { label: 'queue', items: queueItems() },
    { label: 'order', items: orderItems() },
  ];

  steps.push({
    data: [...indeg],
    note: `Course Schedule II (Kahn's algorithm): edges ${edges.map(([f, t]) => `${f}→${t}`).join(', ')}. Cell value = in-degree = prerequisites still owed. Enqueue every course that starts at in-degree 0.`,
    aux: [
      { label: 'queue', items: [] },
      { label: 'order', items: [] },
    ],
    vars: { queue: '[]', order: '[]' },
  });

  for (let c = 0; c < numCourses; c++) {
    if (indeg[c] === 0) queue.push(c);
  }
  steps.push({
    data: [...indeg],
    note: `Courses starting at in-degree 0: [${queue.join(', ')}] → enqueue them all.`,
    cells: cellsFor(),
    aux: auxRows(),
    vars: { queue: `[${queue.join(',')}]`, order: '[]' },
  });

  while (queue.length > 0) {
    const u = queue.shift()!;
    order.push(u);
    steps.push({
      data: [...indeg],
      note: `Dequeue ${u} → course ${u} has no unresolved prerequisites → append to order.`,
      cells: cellsFor({ idx: u, c: 'ok' }),
      aux: auxRows(),
      vars: { u, queue: `[${queue.join(',')}]`, order: `[${order.join(',')}]` },
    });

    for (const v of adj[u]!) {
      indeg[v]!--;
      const enqueued = indeg[v] === 0;
      if (enqueued) queue.push(v);
      steps.push({
        data: [...indeg],
        note: `${u}→${v}: course ${v}'s in-degree drops to ${indeg[v]}${
          enqueued ? ` → 0 → enqueue ${v}` : ' — still waiting on another prerequisite'
        }`,
        cells: cellsFor({ idx: v, c: enqueued ? 'win' : 'mid' }),
        aux: auxRows(),
        vars: { edge: `${u}→${v}`, indegree: indeg[v]!, queue: `[${queue.join(',')}]` },
      });
    }
  }

  const success = order.length === numCourses;
  steps.push({
    data: [...indeg],
    done: true,
    note: success
      ? `Order has all ${numCourses} courses → no cycle → valid order: [${order.join(', ')}] ✓`
      : `Only ${order.length}/${numCourses} courses ordered → a cycle remains → impossible.`,
    cells: cellsFor(),
    aux: [
      { label: 'queue', items: [] },
      { label: 'order', items: orderItems() },
    ],
    vars: { order: `[${order.join(',')}]` },
  });

  return steps;
}
