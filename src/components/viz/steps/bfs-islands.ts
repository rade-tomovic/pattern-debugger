/**
 * Number of Islands — BFS flood fill on a grid.
 * Faithful port of the artifact's `bfsSteps()`: same 4×5 grid, same note
 * text, same snapshot moments (scan hit → per-layer expansion → island
 * sunk → final count). State precedence per cell: front > visited >
 * land/water; the scanned cell additionally gets the cyan outline.
 *
 * Used on: /patterns/bfs-dfs/number-of-islands/ (visualizer: 'bfs-islands')
 */
import type { GridCellState, GridStep } from '../types';

type Coord = [number, number];

export function buildSteps(): GridStep[] {
  const G = [
    [1, 1, 0, 0, 0],
    [1, 1, 0, 0, 0],
    [0, 0, 1, 0, 0],
    [0, 0, 0, 1, 1],
  ];
  const R = 4;
  const C = 5;
  const steps: GridStep[] = [];
  const vis: boolean[][] = G.map((row) => row.map(() => false));
  let count = 0;

  /** Snapshot the whole grid: base land/water, overlaid by visited, overlaid by the BFS frontier. */
  const snap = (scan: Coord | null, front: Coord[] | null): Pick<GridStep, 'grid' | 'values' | 'scan'> => {
    const grid: GridCellState[][] = [];
    const values: string[][] = [];
    for (let r = 0; r < R; r++) {
      const gRow: GridCellState[] = [];
      const vRow: string[] = [];
      for (let c = 0; c < C; c++) {
        let state: GridCellState = G[r]![c] ? 'land' : 'water';
        if (vis[r]![c]) state = 'visited';
        if (front?.some(([fr, fc]) => fr === r && fc === c)) state = 'front';
        gRow.push(state);
        vRow.push(G[r]![c] ? '1' : '0');
      }
      grid.push(gRow);
      values.push(vRow);
    }
    return scan ? { grid, values, scan } : { grid, values };
  };

  steps.push({
    ...snap(null, null),
    note: 'Number of Islands: scan every cell. When we hit unvisited land, count++ and BFS-flood the whole island.',
    vars: { islands: 0 },
  });

  for (let r = 0; r < R; r++) {
    for (let c = 0; c < C; c++) {
      if (G[r]![c] === 1 && !vis[r]![c]) {
        count++;
        steps.push({
          ...snap([r, c], [[r, c]]),
          note: `(${r},${c}) is unvisited land → new island #${count}! Start BFS flood from here.`,
          vars: { islands: count },
        });
        const q: Coord[] = [[r, c]];
        vis[r]![c] = true;
        while (q.length) {
          const layer = [...q];
          q.length = 0;
          const added: Coord[] = [];
          for (const [cr, cc] of layer) {
            for (const [dr, dc] of [
              [-1, 0],
              [1, 0],
              [0, -1],
              [0, 1],
            ] as const) {
              const nr = cr + dr;
              const nc = cc + dc;
              if (nr >= 0 && nr < R && nc >= 0 && nc < C && G[nr]![nc] === 1 && !vis[nr]![nc]) {
                vis[nr]![nc] = true;
                added.push([nr, nc]);
                q.push([nr, nc]);
              }
            }
          }
          if (added.length) {
            steps.push({
              ...snap(null, added),
              note: `BFS expands to ${added.length} neighbor${added.length > 1 ? 's' : ''} — mark visited WHEN enqueueing, not when dequeuing.`,
              vars: { islands: count },
            });
          }
        }
        steps.push({
          ...snap(null, null),
          note: `Island #${count} fully flooded (sunk). Continue scanning.`,
          vars: { islands: count },
        });
      }
    }
  }

  steps.push({
    ...snap(null, null),
    done: true,
    note: `Scan complete. Total islands = ${count} ✓`,
    vars: { islands: count },
  });

  return steps;
}
