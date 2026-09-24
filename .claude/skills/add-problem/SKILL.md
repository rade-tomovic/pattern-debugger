---
name: add-problem
description: Add one or more problems to an existing pattern topic — index-checks curriculum.ts for duplicates first, then loops the full pipeline (curriculum entry → write per brief → dotnet-verify → adversarial verify → build + link check). Use when the user asks to add a problem, a LeetCode question, or exercises to a topic.
---

# Add problem(s) to an existing topic

Input: problem name(s)/LC number(s), and optionally the target topic. Follow every step in
order — steps 1 and 5 are the ones people skip and regret.

## 1. INDEX CHECK (never skip)

Read `src/data/curriculum.ts` and check whether each problem already exists anywhere:

- grep the curriculum for the LC number and for plausible slug forms of the name
- grep `src/pages` for `LC ###` and `(LC ###)` — the problem may already be covered as a
  "variants you can now solve" mention on a related page

If it exists as a full page → tell the user and offer to improve that page instead. If it
exists as a variant mention → ask (or decide, if obvious) whether it deserves promotion to a
full page; a promoted problem must REPLACE the mention (update that page's variants list).

## 2. Placement

Decide topic (if not given) and position in the topic's `problems` array. The list is a
difficulty ramp where each problem adds exactly ONE new idea over its predecessor — read the
existing problems' summaries and place accordingly, never just append. Decide `track`:
`'stretch'` for capstones/design combos beyond the 7-day core budget, omit for core.

## 3. Curriculum entry

Add the `Problem` object: slug (kebab, short), title, lc, difficulty, one-line summary in the
existing voice, track, and `related` refs where a genuine pair exists (add the reverse ref on
the other problem too). Only add a `visualizer` id if the problem truly needs motion AND you
will build the step builder (see /add-pattern step 7). Run `bun run build` — the curriculum
validation must pass before writing content.

## 4. Write the page

Read `docs/content-brief.md` (binding: anatomy, MDX safety, C# rules, trace discipline, and
the topic's mandates), plus the two exemplars
(`src/pages/patterns/two-pointers/index.mdx`, `.../two-sum-ii.mdx`). Write
`src/pages/patterns/<topic>/<slug>.mdx` with frontmatter
`{ layout: '@layouts/ProblemLayout.astro', topic, problem }`.

Non-negotiables: template-instance Callout after "how to think"; solution wrapped in
`<Solution>` (blank lines around the fence); full trace table with EVERY iteration; closing
`<Watch>` time/space chips; variants section.

## 5. Verify the C# before it ships

Every runnable snippet goes through the dotnet harness BEFORE it enters the page
(single-file `dotnet run`, asserts on 3+ cases including 0/1/2-element edges). Derive the
trace table by executing the verified code with per-iteration state printing — transcribe,
don't imagine.

## 6. Adversarial verification

Spawn the `content-verifier` agent on the new/changed files. Apply every blocker finding;
apply minors unless wrong. If the page introduced new tricks/bugs worth remembering, add them
to the topic's entry in `src/data/cheats.ts` (same voice, backtick code spans).

## 7. Gate

`bun run build && bun run check-links` — both must pass. If more than 3 problems are being
added at once, fan out: one `content-writer` agent per problem in parallel, then one
`content-verifier` per result, then this gate once at the end.
