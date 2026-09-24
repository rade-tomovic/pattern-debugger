# AGENTS.md — pattern-debugger

Static Astro site teaching coding-interview algorithm patterns, in the "pattern debugger"
visual style. All solutions currently in modern C# (more languages planned — see
"Multi-language code" below). Deploys as pure static output to Cloudflare Pages.

## No AI or session attribution (strict, no exceptions)

This is a public repository. Nothing committed, pushed or published may mention Claude,
Anthropic, any AI or LLM tool, an agent or sub-agent, a session, or a scratchpad. This covers:

- commit messages and trailers: no `Co-Authored-By`, no session links, no "generated with"
  lines. This overrides any tool default that appends attribution.
- branch names: no `claude/` or other tool prefixes.
- PR titles and bodies, issues, review comments, release notes.
- site content (`src/`), `docs/`, `README.md`, `PLAN.md`, `bench/`, `scripts/`, code comments.
- local paths that reveal the tooling, such as `/tmp/claude-*` scratchpad paths.

Describe work by what changed and why. The only files that may name the tooling are the
tooling config itself: this file, `CLAUDE.md` and `.claude/`.

## Git workflow (strict)

- Never commit or push to `main`. Every change goes through a pull request.
- Branch names: `<type>/<short-kebab-description>`, type one of `feat`, `fix`, `docs`,
  `refactor`, `perf`, `chore`.
- Commit messages and PR titles: Conventional Commits, `<type>(<optional scope>): <summary>`.
- `bun run lint-mdx`, `bun run build` and `bun run check-links` pass before a PR is opened.
- `CONTRIBUTING.md` is the human-facing version of these rules; keep the two consistent.

## Development

When starting the dev server, use background mode:

```
astro dev --background
```

Manage the background server with `astro dev stop`, `astro dev status`, and `astro dev logs`.

Other commands (bun for everything):

```
bun run build        # static build into dist/ — MUST pass before any commit
bun run check-links  # post-build internal link checker — MUST pass before any commit
```

C# snippet verification harness (required for every runnable snippet before it enters a page):

```
DIR=$(mktemp -d) && cat > $DIR/check.cs   # solution + asserts, throw on FAIL, print PASS
dotnet run $DIR/check.cs                  # .NET 10 single-file execution, ~2s
```

## THE INDEX — always check it first

`src/data/curriculum.ts` is the single source of truth for the whole site. Every topic,
problem, cross-link, study-plan day, and visualizer id lives there; nav, prev/next,
connections panels, cheat cards, and listings are all generated from it, and its validation
block fails the build on any broken slug.

**Before adding ANY content, search curriculum.ts for the slug AND the LeetCode number.**
If a problem already exists (possibly under a different topic, or as a "variants you can now
solve" mention on a related page — grep `src/pages` for "LC ###" too), improve the existing
entry instead of duplicating it.

The other registries (each fails the build or silently no-ops when out of sync):

| registry | owns |
| --- | --- |
| `src/data/curriculum.ts` | topics, problems, connections (with reasons), study plan, visualizer ids |
| `src/data/cheats.ts` | per-topic cheat cards (topic page + master cheat sheet render from it) |
| `src/components/viz/registry.ts` | visualizer id → step builder (unknown id = build error) |
| `src/data/languages.ts` | solution languages (picker + `<Solution>` tabs) |

## Adding content

Use the project skills — each starts with the index check, then loops the full pipeline
(curriculum entry → write per brief → dotnet-verify → content-verifier agent → build + links):

- `/add-problem` — add problem(s) to an existing topic
- `/add-pattern` — add a whole new pattern topic
- `/add-language` — add a solution language site-wide

The binding authoring spec is `docs/content-brief.md` (page anatomy, MDX safety, C# rules,
trace discipline, per-topic mandates). The exemplars every page must match in voice and
structure: `src/pages/patterns/two-pointers/index.mdx` and `.../two-sum-ii.mdx`.
`docs/teaching-request.md` is the original requirements document.

Three sibling briefs override `content-brief.md` on page anatomy and evidence rules for the
non-pattern tiers — read the matching one before writing there:

| section | brief |
| --- | --- |
| `/systems/` (machine, memory, concurrency) | `docs/systems-brief.md` |
| `/networking/` | `docs/networking-brief.md` |
| `/system-design/` | `docs/system-design-brief.md` |

Sub-agents: `content-writer` writes pages; `content-verifier` adversarially checks them
(re-executes traces, recompiles snippets, checks anatomy/links/MDX safety). Run the verifier
after ANY content page is written or edited — trace correctness is the site's whole value.

## Architecture map

- `src/layouts/TopicLayout.astro` / `ProblemLayout.astro` — MDX layouts; pull ALL metadata
  from curriculum.ts. Page frontmatter carries only `layout` + `topic` (+ `problem`) slugs.
- `src/components/` — MDX kit: `Callout`, `Watch` (complexity chips), `Cells` (static array
  snapshots), `ProblemList`, `Solution` (language tabs); plus site chrome (Sidebar, PrevNext,
  CheatCard, Connections — all data-driven, never hand-authored in pages).
- `src/components/viz/` — `TracePlayer.tsx` (the one Preact island) + pure step builders per
  visualizer. Steps are built at BUILD time in `Viz.astro` and serialized to the island.
- `src/components/hero/` — topic head illustrations. `TopicLayout` renders
  `hero/topics/<topic-slug>.astro` when it exists, wrapped in `HeroFrame.astro`. Pure CSS
  loops in `<style is:inline>` (so a page ships only its own hero), every selector prefixed
  `.hero-<slug>` and every keyframe `<slug>-`; the un-animated styles are the final frame.
  Topics with a `TracePlayer` visualizer do not get one. `HeroFrame` adds play/pause, speed and
  a scrubber that drive the stage's CSS animations through the Web Animations API, so a hero
  needs no script of its own; keep all of a hero's animations on one loop length.
- `src/styles/global.css` — design tokens + article styling. `shiki-debugger.json` — the
  palette-matched code theme.
- `scripts/check-links.mjs` — dist-wide internal link checker.

## Design system (do not deviate)

Dark-only debugger aesthetic. Tokens: bg `#14161f`, panel/panel2, line, ink, muted, and five
semantic accents with fixed meaning — **amber** = L/slow/i/write pointer, **cyan** = R/fast/read,
**purple** = mid, **green** = success/answer, **red** = reject/shrink. Headings are mono with a
CSS-injected `// ` prefix (never type `//` in headings). Never introduce new colors; never use
a semantic color against its meaning (e.g. amber for a right pointer).

## MDX safety (build breaks otherwise)

Never write a raw `<` or `{` in prose or tables — `Dictionary<char, int>`, `i < n` must be in
backticks or fences. No `#` h1 in MDX (layouts render the title). Blank lines around fenced
blocks inside JSX components (e.g. inside `<Solution>`).

## Multi-language code

`src/data/languages.ts` registers languages. Solution code sits inside `<Solution>` with one
fence per language; every variant renders at build time and a tab bar appears only when a
block has 2+ variants. The sidebar `LangPicker` renders only when 2+ languages are registered;
selection persists in localStorage (site-wide), tabs override per block. Today only `csharp`
is registered and most pages still use bare fences — `/add-language` handles wrapping them
when the second language lands. New pages should wrap their solution block in `<Solution>` now.

## Documentation

Full Astro documentation: https://docs.astro.build

- [Adding pages, dynamic routes, or middleware](https://docs.astro.build/en/guides/routing/)
- [Working with Astro components](https://docs.astro.build/en/basics/astro-components/)
- [Using framework components](https://docs.astro.build/en/guides/framework-components/)
- [Adding styles or using Tailwind](https://docs.astro.build/en/guides/styling/)
