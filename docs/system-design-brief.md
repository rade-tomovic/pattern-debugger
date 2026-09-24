# System Design Brief — how every `/system-design` page gets written

The binding spec for the `system-design` section. `docs/content-brief.md` still governs the
mechanics: frontmatter contracts, MDX safety, component imports, link rules, the design system.
**Read it first.** This brief overrides it on page anatomy, and replaces `docs/systems-brief.md`
entirely — that one is for the hardware tier.

Read, in order:
1. `docs/content-brief.md` — mechanics, MDX safety, voice
2. This file — anatomy and evidence rules
3. `src/data/curriculum.ts` — the content tree; the ONLY valid link targets
4. `src/pages/systems/stack-and-heap/index.mdx` — the voice exemplar from the sibling section

## The reader

Senior .NET engineer, 11 years. **Strong on system design already** — they have built and run
real services, and they will not thank you for explaining what a load balancer is. That makes
this section different from the hardware tier, where the reader was a genuine beginner.

Write for someone who has *used* these things and wants the model underneath:

- They have used a cache. They may not have thought hard about stampedes, negative caching, or
  why TTL jitter exists.
- They have used a queue. They may not be able to say precisely why exactly-once delivery is a
  claim to read carefully rather than a feature to enable.
- They have used a database transaction. They may not be able to name what `READ COMMITTED`
  actually permits, or recognise write skew when it happens to them.

So: no 101 material, no defining "what is a database". Go straight to the mechanism and the
tradeoff. The value you add is precision, failure modes, and the vocabulary to defend a choice
out loud.

## No measurements

Same rule as the hardware tier, and here it is even easier to honour because there is nothing
to benchmark. **No durations, no throughput figures, no speed ratios, no "we measured".**

What you may and should use:

- **Published orders of magnitude, labelled as such**, where they drive a real decision: a
  cross-region round trip is tens of milliseconds against sub-millisecond within a datacenter;
  a disk seek is orders above a memory read. Present these as industry reference figures for
  *reasoning about relative cost*, never as something measured for this page. On `estimation`
  this is the whole point of the page and a labelled table is expected.
- **Capacity arithmetic** worked in full: requests per second from daily actives, bytes per day
  from bytes per row, replicas from a durability target. Show the working. The arithmetic is
  the skill, and the reader should be able to redo it with their own inputs.
- **Structural claims**: "this is one round trip per key rather than one per batch", "this
  holds a transaction open across a network call", "this design cannot serve a read from a
  follower without risking staleness".

## Page anatomy — concept topics

Sections in this order, lowercase headings, `##` level. No `#` h1, and never type a `// `
prefix in a heading — CSS injects it.

1. `## the ground floor` — **only if the page genuinely needs it.** 3-5 bullets defining terms
   this page assumes and an experienced engineer might still have fuzzy. Omit rather than pad;
   this reader does not need "a replica is a copy of the data".
2. `## core idea` — 2-4 sentences. What problem this exists to solve, and the one sentence a
   staff engineer would use to summarise it.
3. `## how it actually works` — **the longest section.** `###` subsections encouraged here and
   nowhere else. The mechanism, concretely: what is written where, in what order, what the
   failure window is, who waits for whom. **At least one ```text diagram is required** — a
   write path, a replication topology, a state machine, a timeline of two nodes disagreeing.
   For this material a picture is worth more than three paragraphs.
4. `## the tradeoff` — **the heart of a design page.** A table with the axes that actually
   differ, and a paragraph naming what you are buying and what you are paying. Never a
   both-sides shrug: say which default is right for a typical service, then when to depart from
   it. "It depends" is only acceptable when you also say *on what*.
5. `## how it fails` — the failure modes, in production terms: the symptom, the cause, and what
   it looks like on a dashboard or in a support ticket. This is what separates someone who has
   read about a technique from someone who has operated it, and it is what interviews probe.
6. `## in practice` — how this maps to things the reader actually uses: SQL Server, PostgreSQL,
   Redis, Kafka, RabbitMQ, Azure Service Bus, Cosmos DB, EF Core, `HttpClient`, Polly. Name real
   defaults and real footguns. Where .NET has a specific idiom or a specific trap, say so —
   this reader is a .NET engineer and generic advice is worth less to them.
7. `## the same idea elsewhere` — a short table mapping this concept to its cousins in other
   systems or layers, and the trap in each. Cross-layer links are the best rows here: a CPU
   cache line and a cache key, a memory barrier and a consensus round, a mutex and a
   distributed lock. **Drop any row you are not certain of.**
8. `## interview drills` — 4-6 drills. Each drill is:
   - `**Q.** the question as an interviewer asks it`
   - `- *weak answer* — …` (the plausible one that invites a follow-up, and why it is weak)
   - `- *strong answer* — …` (what lands, 2-3 sentences)
   - `- *follow-up* — …` (what they ask next, with its one-line answer)

   Real questions, not trivia. "What does CAP stand for" is trivia. "Your reads started
   returning stale data after the failover — walk me through why" is a drill.

If the topic has exercises in `curriculum.ts`, add `## exercises` with
`<ProblemList topic="<slug>" />` before the drills. Only `design-drills` has any.

## Page anatomy — the design drills

For the four problem pages under `design-drills`. These use `ProblemLayout`.

1. `## the brief` — the question exactly as an interviewer states it, in one or two lines.
2. `## clarify first` — the questions to ask before drawing anything, and why each one changes
   the design. This is the most-skipped and highest-scoring part of a real interview.
3. `## the numbers` — capacity arithmetic from stated assumptions, worked. State the
   assumptions as assumptions.
4. `## the sketch` — the design, with a required ```text diagram of the components and the
   request path. Then the walk-through: what happens on a write, what happens on a read.
5. `## the tradeoffs` — where you chose, what you rejected, and why. Link the concept topics
   that own each decision.
6. `## how it fails` — what breaks first under ten times the load, what breaks when a
   dependency dies, and what you would page on.
7. `## what they ask next` — the three follow-ups this design invites, each with a short answer.

## Accuracy — the claims that get these pages rejected

This material is unusually easy to state confidently and wrongly, because the folklore is
widespread. The sibling section shipped several confident falsehoods before they were caught.
Be precise about exactly these:

- **CAP.** It is not "pick two of three". It says that *when a network partition occurs*, a
  system must choose between availability and linearizable consistency. There is no meaningful
  "CA" system. Prefer PACELC, which adds the part that matters in normal operation: even when
  there is no partition, you trade latency against consistency.
- **Exactly-once.** Exactly-once *delivery* over an unreliable network is not achievable in
  general. What real systems provide is at-least-once delivery plus idempotent processing, or
  effectively-once semantics within one system's boundary via deduplication and transactional
  offsets. Say which you mean.
- **Isolation levels.** Name the anomaly, not just the level. `READ COMMITTED` permits
  non-repeatable reads and phantoms; snapshot isolation prevents those but permits write skew;
  only serializable prevents write skew. Note that many engines' default is not what people
  assume, and that "snapshot" and "serializable" mean different things across engines.
- **ACID durability** means committed and recoverable, which usually means an `fsync`ed log —
  not that the data has reached every replica.
- **Consistent hashing** reduces the keys that move when a node joins or leaves. It does not by
  itself balance load; that needs virtual nodes, and hot keys defeat it regardless.
- **Raft** elects a leader and replicates a log. It does not make your reads linearizable for
  free — a follower read can be stale, and a leader must confirm it is still the leader.
- **Two-phase commit** is not consensus and it blocks: if the coordinator dies at the wrong
  moment, participants hold locks until it returns.

Where you are not certain of a claim, state it as the reasoning it is, or cut it. A confident
falsehood in front of a reader who will repeat it in an interview is the worst thing this
section can ship.

## MDX safety (build breaks otherwise)

- **Never a raw `<` or `{` in prose, headings, bullets, or TABLE CELLS.** This material wants
  to write `R + W > N`, `p99 < 200ms`, `Dictionary<string, User>`, `List<T>`. Every one goes in
  backticks or a fence. A `<` in a table cell breaks the build exactly as hard as one in a
  paragraph.
- Component props use JSX braces — that is the one legal `{`.
- Blank lines around fenced blocks inside JSX components.
- No `#` h1. Never type `// ` in a heading.
- Run `node scripts/mdx-lint.mjs <files>` on everything you write; it must be clean.

## Components

```
import Callout from '@components/Callout.astro';      // kinds: insight|tip|trap|note
import Watch from '@components/Watch.astro';          // items={[['k','v'],...]}
import ProblemList from '@components/ProblemList.astro';
```

`<Watch>` on these pages carries durable quantities — a quorum condition, a replication factor,
a delivery guarantee — never a latency you claim to have observed.

## Design system

Unchanged. Dark only. The five accents keep their meanings: **amber** = the write/leader side,
**cyan** = the read/follower side, **purple** = the middle or coordinator, **green** =
success/committed, **red** = failure/rejected/partitioned. Never introduce a colour.

## Links

Internal links only to paths derivable from `curriculum.ts`, always with a trailing slash.
Every page links at least two others. The cross-layer links into `/systems/` are the most
valuable thing this section can offer a reader who has just read the hardware tier — a
distributed lock against a mutex, replica disagreement against core disagreement, a cache key
against a cache line. Make them explicitly, in prose, not just in the connections panel.

## Cheat-card data (returned, not written into MDX)

For each topic, return `{ signals: string[], tricks: string[], bugs: string[] }`, 3-5 each:

- `signals` — "you are looking at this problem when…": the symptom, the ticket, the metric
- `tricks` — the moves that resolve it, and the default worth reaching for first
- `bugs` — what people get wrong, including the confident-but-wrong folklore above
