---
name: add-language
description: Add a new solution language (Python, TypeScript, Java, Go…) site-wide — registers it, wraps existing code fences in <Solution>, translates and RUN-VERIFIES every snippet, and enables the global language picker. Use when the user wants solutions in another language.
---

# Add a solution language site-wide

The infrastructure already exists: `src/data/languages.ts` (registry),
`<Solution>` (per-block tabs), `LangPicker` (sidebar, auto-appears at 2+ languages).
This skill is about doing the rollout without shipping a single unverified snippet.

## 1. INDEX CHECK

Read `src/data/languages.ts` — the language may already be registered or partially rolled
out. Grep `src/pages` for existing fences of that language (` ```python `) to find any prior
partial work; finish/repair before expanding.

## 2. Verification toolchain first

Every translated snippet must be RUN-verified like the C# ones. Confirm the runtime exists
(`python3 --version`, `bun --version` for TS/JS, `go version`…) and establish the harness
one-liner for this language (equivalent of the dotnet single-file run: script + asserts +
PASS/FAIL). If no runtime is available, stop and tell the user — do not ship unverified code.
Record the harness command in `docs/content-brief.md` next to the dotnet one.

## 3. Register

Add `{ id, label }` to `languages` in `src/data/languages.ts`. The `id` must equal the fence
language exactly (it becomes `data-language` on the rendered `<pre>`); confirm Shiki
highlights that fence id (build a scratch page if unsure). The sidebar picker appears
automatically. Keep `defaultLang` as `csharp` unless the user says otherwise — and if it DOES
change, update the no-JS fallback selector in `Solution.astro`'s `<style>` (it names the
default language literally; the comment there points at this).

## 4. Wrap existing fences

Most pages predate `<Solution>` and use bare ```csharp fences. For every page being
translated: wrap each language-specific block (solutions AND topic-page templates AND inline
variant snippets) in `<Solution>` with blank lines around fences, adding the
`import Solution from '@components/Solution.astro';` line. Blocks that are conceptually
language-neutral (```text diagrams, grids) stay bare. This is mechanical — script-assist it,
but eyeball a sample; then `bun run build` before any translation starts.

## 5. Translate — idiomatic, not transliterated

Per topic (fan out one agent per topic for scale; get user opt-in before a large multi-agent
run): add the new-language fence to each `<Solution>` block. The code must be idiomatic for
that language (Python: dict/deque/heapq, negative indexing; TS: Map/Set, no C#-isms), same
algorithm and same variable names as the trace table uses (traces describe state, not syntax —
they must remain true for every language). Verify EVERY snippet with the step-2 harness,
same 3+ cases including 0/1/2-element edges.

Prose stays language-neutral except where it is deliberately language-specific (the C#
`PriorityQueue`/`LinkedList<T>` notes stay — add the new language's equivalent trap as an
extra sentence or Callout where it genuinely differs, e.g. Python's `heapq` being min-only).

## 6. Verify + gate

Spawn `content-verifier` per translated topic (it must re-run BOTH languages' snippets).
Then `bun run build && bun run check-links`, and a manual browser check of one page: tabs
render, picker switches all blocks, per-block tab overrides, choice survives reload
(localStorage).

## 7. Rollout order

If not doing the whole site at once: foundations `csharp-toolkit` gets a sibling page
decision (a per-language toolkit page is a separate conversation — flag it to the user),
then core patterns in reading order, then structures/advanced. Track progress in PLAN.md.
