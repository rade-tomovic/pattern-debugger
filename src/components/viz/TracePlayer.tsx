/**
 * The one Preact island of the site: a debugger-style step player.
 *
 * Port of the artifact's `Player` class + renderArray/renderList/renderGrid
 * (~/Downloads/algorithm-visualizer.html), restyled with Tailwind utilities
 * on the site's tokens. Steps are precomputed at build time by the pure
 * builders in ./steps/ and arrive fully serialized as props — this component
 * only replays them.
 *
 * Keyboard: focus the player (tab or click), then ArrowLeft/ArrowRight step.
 * Autoplay advances every ~1100ms, matching the artifact.
 * prefers-reduced-motion: global.css kills all transitions/animations with
 * an !important base rule, so the cell transition utilities used here are
 * inert for those users; stepping itself is discrete, not animated.
 */
import { useEffect, useState } from 'preact/hooks';
import type { ComponentChildren } from 'preact';
import type {
  ArrayStep,
  AuxRow,
  BadgeColor,
  CellState,
  GridCellState,
  GridStep,
  PointerBadge,
  Step,
  VizKind,
} from './types';

interface Props {
  kind: VizKind;
  steps: Step[];
}

const PLAY_INTERVAL_MS = 1100;

/* ── style maps (mirror Cells.astro + the artifact's CSS) ─────────────── */

const badgeBg: Record<BadgeColor, string> = {
  amber: 'bg-amber',
  cyan: 'bg-cyan',
  purple: 'bg-purple',
  green: 'bg-green',
  red: 'bg-red',
};

const cellCls: Record<CellState, string> = {
  ok: 'bg-green-dim border-green scale-108',
  win: 'bg-win-dim border-green',
  warn: 'bg-red-dim border-red',
  mid: 'bg-purple-dim border-purple',
  dim: 'opacity-30',
};

const auxAccent: Record<BadgeColor, string> = {
  amber: 'border-amber text-amber',
  cyan: 'border-cyan text-cyan',
  purple: 'border-purple text-purple',
  green: 'border-green text-green',
  red: 'border-red text-red',
};

/* grid cell palette lifted verbatim from the artifact's g-* classes */
const gridCls: Record<GridCellState, string> = {
  water: 'bg-[#1a2233] text-[#3d4a66]',
  land: 'bg-[#3a5a2f] text-[#c9e8b8]',
  visited: 'bg-[#26303f] text-[#5a6a85] border-[#2e3a4f]',
  front: 'bg-amber text-bg scale-108',
};

/* ── sub-renderers, one per kind ──────────────────────────────────────── */

function Badges({ badges }: { badges: PointerBadge[] }) {
  return (
    <div class="flex h-[18px] gap-0.5 font-mono text-[0.72rem] font-bold">
      {badges.map((p) => (
        <span class={`ptr-badge ${badgeBg[p.c]}`}>{p.t}</span>
      ))}
    </div>
  );
}

function AuxRows({ aux }: { aux: AuxRow[] }) {
  return (
    <div class="mt-3 flex flex-col items-start gap-1.5">
      {aux.map((row) => (
        <div class="flex flex-wrap items-center gap-1.5 font-mono text-[0.72rem]">
          <span class="text-muted">{row.label} =</span>
          {row.items.length === 0 && <span class="text-muted opacity-60">∅</span>}
          {row.items.map((item) => (
            <span
              class={`bg-panel2 rounded border px-1.5 py-0.5 ${
                item.c ? auxAccent[item.c] : 'border-line text-ink'
              }`}
            >
              {item.v}
            </span>
          ))}
        </div>
      ))}
    </div>
  );
}

function ArrayViz({ step, list }: { step: ArrayStep; list?: boolean }) {
  return (
    <div>
      <div class="flex gap-1.5">
        {step.data.map((v, i) => (
          <>
            <div class="flex flex-col items-center gap-1">
              <Badges badges={step.pointers?.[i] ?? []} />
              <div
                class={`bg-panel2 border-line flex h-11 items-center justify-center border font-mono text-[0.95rem] transition-all ${
                  list ? 'w-11 rounded-full' : 'min-w-11 rounded-lg px-1'
                } ${(step.cells?.[i] && cellCls[step.cells[i]]) ?? ''}`}
              >
                {v}
              </div>
              {!list && <div class="text-muted font-mono text-[0.65rem]">{i}</div>}
            </div>
            {list && i < step.data.length - 1 && (
              <div class="flex flex-col">
                <div class="h-[18px]" />
                <div class="text-muted flex h-11 items-center px-0.5">→</div>
              </div>
            )}
          </>
        ))}
      </div>
      {step.aux && step.aux.length > 0 && <AuxRows aux={step.aux} />}
    </div>
  );
}

function GridViz({ step }: { step: GridStep }) {
  const cols = step.grid[0]?.length ?? 0;
  return (
    <div class="grid gap-[5px]" style={{ gridTemplateColumns: `repeat(${cols}, 38px)` }}>
      {step.grid.map((row, r) =>
        row.map((state, c) => (
          <div
            class={`border-line flex h-[38px] w-[38px] items-center justify-center rounded-md border font-mono text-[0.8rem] transition-all ${gridCls[state]} ${
              step.scan && step.scan[0] === r && step.scan[1] === c
                ? 'outline-cyan outline-2 outline-offset-1'
                : ''
            }`}
          >
            {step.values[r]?.[c] ?? ''}
          </div>
        )),
      )}
    </div>
  );
}

function CtrlButton({ onClick, children }: { onClick: () => void; children: ComponentChildren }) {
  return (
    <button
      type="button"
      onClick={onClick}
      class="bg-panel2 text-ink border-line hover:border-amber hover:text-amber cursor-pointer rounded-md border px-3.5 py-1.5 font-mono text-[0.85rem] transition-colors"
    >
      {children}
    </button>
  );
}

/* ── the player ───────────────────────────────────────────────────────── */

export default function TracePlayer({ kind, steps }: Props) {
  const [i, setI] = useState(0);
  const [playing, setPlaying] = useState(false);
  const last = steps.length - 1;
  const step = steps[Math.min(i, last)]!;

  // autoplay tick — the artifact's setInterval(…, 1100)
  useEffect(() => {
    if (!playing) return;
    const t = setInterval(() => setI((cur) => Math.min(cur + 1, last)), PLAY_INTERVAL_MS);
    return () => clearInterval(t);
  }, [playing, last]);

  // pause when the end is reached
  useEffect(() => {
    if (playing && i >= last) setPlaying(false);
  }, [i, playing, last]);

  const next = () => setI((cur) => Math.min(cur + 1, last));
  const prev = () => setI((cur) => Math.max(cur - 1, 0));
  const reset = () => {
    setPlaying(false);
    setI(0);
  };
  const togglePlay = () => {
    if (playing) {
      setPlaying(false);
    } else {
      if (i >= last) setI(0); // replay from the start, like the artifact
      setPlaying(true);
    }
  };

  const onKeyDown = (e: KeyboardEvent) => {
    if (e.key === 'ArrowRight') {
      next();
      e.preventDefault();
    } else if (e.key === 'ArrowLeft') {
      prev();
      e.preventDefault();
    }
  };

  return (
    <div
      class="bg-panel border-line rounded-[10px] border px-4 pt-5 pb-3.5"
      tabIndex={0}
      role="group"
      aria-label="step player — arrow keys step when focused"
      onKeyDown={onKeyDown}
    >
      {/* stage */}
      <div class="flex min-h-[120px] items-center overflow-x-auto pb-1.5">
        <div class="mx-auto w-max">
          {kind === 'grid' ? (
            <GridViz step={step as GridStep} />
          ) : (
            <ArrayViz step={step as ArrayStep} list={kind === 'list'} />
          )}
        </div>
      </div>

      {/* controls */}
      <div class="mt-4 flex flex-wrap items-center gap-2">
        <CtrlButton onClick={reset}>⏮ reset</CtrlButton>
        <CtrlButton onClick={prev}>◀ back</CtrlButton>
        <CtrlButton onClick={togglePlay}>{playing ? '⏸ pause' : '▶ play'}</CtrlButton>
        <CtrlButton onClick={next}>step ▶</CtrlButton>
        <span class="text-muted ml-auto font-mono text-[0.78rem]">
          step {i + 1}/{steps.length}
        </span>
      </div>

      {/* note */}
      <div
        class={`bg-panel2 text-ink mt-3 min-h-[42px] rounded-r-md border-l-[3px] px-3 py-2.5 font-mono text-[0.84rem] ${
          step.done ? 'border-green' : 'border-amber'
        }`}
        aria-live="polite"
      >
        {step.note}
      </div>

      {/* watch panel */}
      <div class="mt-2.5 flex flex-wrap gap-1.5">
        {Object.entries(step.vars).map(([k, v]) => (
          <div class="watch-chip" key={k}>
            <b>{k}</b> = {v}
          </div>
        ))}
      </div>
    </div>
  );
}
