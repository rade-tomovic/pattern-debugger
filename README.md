# // pattern debugger

Built and maintained by [AMPQ](https://ampq.dev/).

A static learning site for coding-interview algorithm patterns — step through every pattern
like a debugger — and for the systems, networking and design material underneath them. All
solution code in modern **C#**. On the pattern and systems tiers every snippet is
compile-verified with .NET 10 and every trace derived from actual execution; the networking
tier is a reference and is verified by reasoning rather than by running (see
`docs/networking-brief.md`).

**Live tree:** 60 topics across five tiers — 4 foundations, 17 pattern topics, 2 deep dives,
16 on how the machine runs code, 9 on how the network works, and 12 on system design — plus
126 problem and exercise pages, 13 interactive step-through visualizers, the study plan and
the master cheat sheet. See [PLAN.md](PLAN.md) for the full map.

## Stack

- [Astro 7](https://astro.build) static output, MDX content
- TypeScript 7 · Tailwind 4 · Preact islands (visualizers only)
- bun for everything
- Deploys to Cloudflare Pages (pure static, no server)

## Develop

```sh
bun install
bun run dev      # localhost:4321
bun run build    # static build into dist/
```

## Architecture notes

- `src/data/curriculum.ts` is the single source of truth: nav, prev/next, cross-links,
  cheat-sheet cards, and the study plan all generate from it. Broken slugs fail the build.
- Content pages are MDX under `src/pages/` with two layouts (`TopicLayout`, `ProblemLayout`)
  that pull all metadata from the curriculum — frontmatter carries only slugs.
- `docs/content-brief.md` is the authoring spec, with three sibling briefs that override it on
  anatomy and evidence rules per tier: `docs/systems-brief.md` (`/systems/`),
  `docs/networking-brief.md` (`/networking/`) and `docs/system-design-brief.md`
  (`/system-design/`). `docs/teaching-request.md` is the original requirements document.
- Visualizers are one Preact `TracePlayer` island plus pure step-builder functions per
  algorithm, ported from the original "algorithm pattern debugger" artifact.

## Contributing

Changes land through pull requests only, on typed branches (`feat/`, `fix/`, `docs/`,
`chore/`, ...) with Conventional Commits messages. See [CONTRIBUTING.md](CONTRIBUTING.md) for
the workflow, the build gate and the content verification rules.

## License

MIT. See [LICENSE](LICENSE).
