/**
 * Step types for the interactive trace players.
 *
 * Ported from the "algorithm pattern debugger" artifact
 * (~/Downloads/algorithm-visualizer.html): a step is one debugger "pause" —
 * a full snapshot of the data structure, pointer badges, a teaching-voice
 * note, and the watch panel. Builders are PURE functions that run the real
 * algorithm at build time and record a snapshot per interesting moment; the
 * TracePlayer island just replays them.
 *
 * Three renderer kinds:
 *   'array' — row of square cells, pointer badges above, indices below
 *   'list'  — row of circular nodes joined by → arrows, badges above
 *   'grid'  — matrix of colored cells (flood fill / BFS)
 *
 * There is deliberately NO fourth kind: monotonic-stack and topo-sort are
 * 'array' visualizations plus one or two auxiliary chip rows (the stack
 * contents / the queue + output order) — that's what `ArrayStep.aux` is for.
 *
 * NOTE VOICE (keep it): notes narrate the *decision* at each step in the
 * artifact's teaching voice — CAPS for the key move ("SHRINK", "EXPAND",
 * "START FRESH"), the arithmetic spelled out ("3 + 7 = 10 == target"),
 * arrows for consequences ("→ L++"), and a ✓ on the terminal step.
 *
 * Pointer badge color semantics (site-wide law — do not repurpose):
 *   amber  = L / slow / i / write pointer
 *   cyan   = R / fast / read pointer
 *   purple = mid
 *   green  = success / answer
 *   red    = reject / shrink
 */

export type BadgeColor = 'amber' | 'cyan' | 'purple' | 'green' | 'red';

/** A small badge rendered above a cell/node, e.g. { t: 'L', c: 'amber' }. */
export interface PointerBadge {
  /** badge text: 'L', 'R', 'S', 'F', 'mid', 'ans', 'i', … */
  t: string;
  c: BadgeColor;
}

/**
 * Visual state of an array cell / list node (matches the static Cells.astro):
 *   ok   = green + scaled — the answer / a hit
 *   win  = subtle green — inside the current window / running subarray
 *   warn = red — rejected / about to be discarded
 *   mid  = purple — the midpoint cell
 *   dim  = faded — eliminated from the search space
 */
export type CellState = 'ok' | 'win' | 'warn' | 'mid' | 'dim';

/** One chip in an auxiliary row (stack contents, queue, output order…). */
export interface AuxItem {
  v: string | number;
  /** accent color for this chip's border/text; default is the muted look */
  c?: BadgeColor;
}

/**
 * A labeled row of chips rendered under the main visualization — the
 * artifact-style readout for secondary structures. Used by 'array' (and
 * 'list') steps: monotonic-stack renders its stack here, topo-sort its
 * queue and output order. An empty `items` renders as ∅.
 */
export interface AuxRow {
  /** short mono label, e.g. 'stack', 'queue', 'order' */
  label: string;
  items: AuxItem[];
}

interface StepBase {
  /** teaching-voice caption for this step (see NOTE VOICE above) */
  note: string;
  /** terminal step — the note box border turns green */
  done?: boolean;
  /** watch panel: name → value, rendered as `name = value` chips */
  vars: Record<string, string | number>;
}

/** kind: 'array' — cells with pointer badges and indices. */
export interface ArrayStep extends StepBase {
  /** cell values, left to right */
  data: (string | number)[];
  /** index → badges shown above that cell */
  pointers?: Record<number, PointerBadge[]>;
  /** index → visual state of that cell */
  cells?: Record<number, CellState>;
  /** auxiliary chip rows under the array (stack / queue / output readouts) */
  aux?: AuxRow[];
}

/**
 * kind: 'list' — same payload as 'array' (data = node values in list order),
 * rendered as circular nodes with → arrows and no index row.
 */
export type ListStep = ArrayStep;

/**
 * Visual state of a grid cell:
 *   water   = dark blue        land  = green
 *   visited = sunk/gray        front = amber, scaled (current BFS frontier)
 */
export type GridCellState = 'water' | 'land' | 'visited' | 'front';

/** kind: 'grid' — a matrix of colored cells. */
export interface GridStep extends StepBase {
  /** cell states, row-major; all rows the same length */
  grid: GridCellState[][];
  /** cell labels, same shape as `grid` ('' renders an empty cell) */
  values: (string | number)[][];
  /** [row, col] currently being scanned — cyan outline on top of its state */
  scan?: [number, number];
}

export type VizKind = 'array' | 'list' | 'grid';

/** Any step. ArrayStep/ListStep are structurally identical; kind picks the renderer. */
export type Step = ArrayStep | GridStep;

/**
 * A registry entry: the renderer kind plus a pure builder producing the
 * homogeneous step sequence for it. Builders run at BUILD time (inside
 * Viz.astro during `astro build`) — the serialized steps are what ships
 * to the client.
 *
 * Every builder must produce: > 3 steps, `vars` on every step, and a final
 * step with `done: true`.
 */
export type VizSpec =
  | { kind: 'array'; buildSteps: () => ArrayStep[] }
  | { kind: 'list'; buildSteps: () => ListStep[] }
  | { kind: 'grid'; buildSteps: () => GridStep[] };

/** Shorthand badge constructor, mirroring the artifact's `P(t, c)` helper. */
export const P = (t: string, c: BadgeColor): PointerBadge => ({ t, c });
