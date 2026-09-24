---
name: add-pattern
description: Add a whole new pattern topic to the site — index-checks curriculum.ts first, then creates the topic entry (section, connections with reasons, study-plan slot), cheat card, topic page, problem pages, and optionally a visualizer, ending with the build + link gate. Use when the user asks for a new pattern, topic, or category.
---

# Add a new pattern topic

Input: the pattern name and (ideally) candidate problems. This is /add-problem's big sibling —
read that skill too; its steps 4-7 apply to every problem page here.

## 1. INDEX CHECK (never skip)

Read `src/data/curriculum.ts` fully. Confirm the pattern isn't already covered under another
name (e.g. "monotonic stack" lives inside `stack-queue`; "fast/slow" inside `linked-lists`;
"prefix sums" inside `array-techniques`). If it's a sub-pattern of an existing topic, extend
that topic instead — a new topic needs a genuinely distinct template and problem family.

## 2. Topic entry

Add the `Topic` to `curriculum.ts` in the right section (`core` order is user-mandated —
never reorder it; new topics almost always go in `structures` or `advanced`, positioned so
prerequisites come earlier in `readingOrder`). Fill: slug, title, mono `tab` label, summary,
and `connections` — every edge needs a REASON in the site's voice, and the validation block
requires the new topic to have at least one INBOUND edge, so add edges from related existing
topics too (only where genuinely justified; don't spray).

Also: add the topic to a `studyPlan` day (extend a day or add one), and to the
`pattern-recognition` topic's connections with its "signal → here" reason.

## 3. Problems

4-6 problems, easy → hard ramp, each adding exactly one idea; an easy on-ramp and (where the
family has one) a hard-ish capstone marked `track: 'stretch'`. Canonical LC representatives
over obscure equivalents. Then follow /add-problem steps 3-7 per problem.

## 4. Cheat card

Add the topic's entry to `src/data/cheats.ts` (signals / tricks / bugs, 3-5 items each,
backtick code spans, same voice as existing entries). The topic page and master cheat sheet
render it automatically — never write cheat content into MDX.

## 5. Topic page

`src/pages/patterns/<slug>/index.mdx` per the brief's topic anatomy: core idea (+ sub-shape
table if the pattern has shapes), when to reach for it, universal template(s) in C# with
WHY-comments (dotnet-verified with a stand-in), one intro line + `<ProblemList topic="...">`.
Match the two-pointers exemplar's voice exactly.

## 6. Content-brief mandate

Add a bullet for the new topic under "Topic-specific mandates" in `docs/content-brief.md`
(what its pages must cover / call out), so future contributors inherit the intent.

## 7. Visualizer (only if motion genuinely teaches)

If one problem deserves a step-player: add `visualizer: '<id>'` to that problem, create
`src/components/viz/steps/<id>.ts` (pure `buildSteps()`, note text in the artifact's teaching
voice, pointer color law: amber=L/slow/i/write, cyan=R/fast, purple=mid, green=success,
red=reject), register it in `src/components/viz/registry.ts`, and behavior-verify with
`bun -e` asserts (step count, `done` on last step, semantic end-state). An unregistered id
fails the build — that's the safety net, not the test.

## 8. Gate

Spawn `content-verifier` over all new files; apply findings. Then
`bun run build && bun run check-links`. Also update `PLAN.md`'s tree (human view of the index).
