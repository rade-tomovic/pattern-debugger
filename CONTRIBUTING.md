# Contributing

Thanks for helping. The site's whole value is that every trace and every snippet is correct,
so most of this guide is about how to keep it that way.

## Prerequisites

- [bun](https://bun.sh) (package manager and script runner)
- Node.js 22.12 or newer
- [.NET 10 SDK](https://dotnet.microsoft.com/download) for verifying C# snippets and running
  the benchmarks under `bench/`

```sh
bun install
bun run dev          # localhost:4321
```

## Workflow

`main` is only changed through pull requests. Never push to it directly.

1. Branch from `main` with a typed name: `<type>/<short-kebab-description>`.
2. Commit with [Conventional Commits](https://www.conventionalcommits.org) messages:
   `<type>: <imperative summary>`, optionally scoped, e.g. `feat(graphs): add Course Schedule II`.
3. Run the gate (below) locally.
4. Open a pull request against `main`. The PR title follows the same `<type>: <summary>` form,
   because PRs are squash-merged and the title becomes the commit message on `main`.
   The `Build` check (MDX lint, build, link check) must pass before merging. PRs from
   branches in this repository also get a Cloudflare Pages preview deploy; PRs from forks
   are built and checked but not deployed.

| type | use for | branch example |
| --- | --- | --- |
| `feat` | new topic, problem, exercise, visualizer or site feature | `feat/add-course-schedule-ii` |
| `fix` | wrong trace, wrong complexity, broken link, rendering bug | `fix/two-sum-ii-trace-row-4` |
| `docs` | briefs, README, PLAN, this guide | `docs/networking-brief-scope` |
| `refactor` | restructuring with no visible change | `refactor/cells-component-props` |
| `perf` | build or page performance | `perf/lazy-load-trace-player` |
| `chore` | dependencies, CI, tooling, config | `chore/bump-astro` |

Keep a PR to one concern. A new problem page and an unrelated fix on another page are two PRs.

## The gate

All three must pass before a PR is opened:

```sh
bun run lint-mdx      # fast MDX hazard check, no build needed
bun run build         # static build; curriculum validation fails it on any broken slug
bun run check-links   # every internal link in dist/ resolves
```

## Adding or changing content

**Check the index first.** `src/data/curriculum.ts` is the single source of truth for topics,
problems, cross-links, the study plan and visualizer ids. Before adding a problem, search it
for the slug and the LeetCode number, and grep `src/pages` for `LC ###`: the problem may
already exist under another topic or as a variant mention on a related page. Improve the
existing entry instead of duplicating it.

**Follow the brief for the tier you are writing in.**

| section | brief |
| --- | --- |
| `/patterns/`, `/foundations/`, `/deep-dives/` | `docs/content-brief.md` |
| `/systems/` | `docs/systems-brief.md` |
| `/networking/` | `docs/networking-brief.md` |
| `/system-design/` | `docs/system-design-brief.md` |

Pattern pages must match the exemplars in structure and voice:
`src/pages/patterns/two-pointers/index.mdx` and `src/pages/patterns/two-pointers/two-sum-ii.mdx`.

**Verify every runnable C# snippet** with .NET 10 single-file execution before it goes into a
page: the solution plus at least three asserted cases (the page's trace input, an empty or
single-element edge, a normal case), throwing on mismatch.

```sh
DIR=$(mktemp -d) && $EDITOR $DIR/check.cs
dotnet run $DIR/check.cs
```

**Derive traces from execution.** Add per-iteration state printing to the verified code and
transcribe the output into the trace table. Every row and every `<Cells>` snapshot must match
a real run.

**Other registries** that must stay in sync with the curriculum:

| file | owns |
| --- | --- |
| `src/data/cheats.ts` | per-topic cheat cards |
| `src/components/viz/registry.ts` | visualizer id to step builder |
| `src/data/languages.ts` | registered solution languages |

## Style rules the build or review will reject

- **MDX safety.** No raw `<` or `{` in prose or tables. Put `Dictionary<char, int>` or
  `i < n` in backticks. No `#` h1 headings (layouts render the title). Leave blank lines
  around fenced code inside JSX components such as `<Solution>`.
- **Frontmatter** carries only `layout`, `topic` and, on problem pages, `problem`. All other
  metadata renders from the curriculum.
- **Design system.** No new colors. The five accents have fixed meanings: amber is the
  left, slow, `i` or write pointer; cyan is right, fast or read; purple is mid; green is
  success or the answer; red is reject or shrink. Headings get their `// ` prefix from CSS,
  so never type it.
- **Solutions** sit inside `<Solution>` with one fence per language.

## Review checklist

The PR template repeats this. A reviewer will check that:

- the index was checked and nothing is duplicated
- every snippet was run and every trace came from that run
- the page follows its brief's anatomy
- the gate passes
