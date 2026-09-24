## What and why

<!-- One concern per PR. Title format: `<type>: <imperative summary>` (feat, fix, docs, refactor, perf, chore). -->

## Checklist

- [ ] Searched `src/data/curriculum.ts` for the slug and LC number, and `src/pages` for `LC ###`; nothing is duplicated
- [ ] Every runnable C# snippet was run with .NET 10 and asserts at least three cases, including an edge case
- [ ] Every trace row and `<Cells>` snapshot was transcribed from an actual run
- [ ] The page follows the anatomy of its tier's brief in `docs/`
- [ ] `bun run lint-mdx`, `bun run build` and `bun run check-links` pass
