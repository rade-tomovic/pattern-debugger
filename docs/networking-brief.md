# Networking Brief — how every `/networking` page gets written

The binding spec for the `network` section (URL base `/networking`).

`docs/content-brief.md` still governs everything mechanical: frontmatter contracts, MDX safety,
component imports, C# style, link rules, the design system. **Read it first.** This brief
overrides it on page anatomy and on what counts as verified. It is the sibling of
`docs/systems-brief.md` — same reader, same posture, different subject — and where this file is
silent, `docs/systems-brief.md` is the fallback authority. The one place it does **not** follow
its sibling: this section is not execution-verified (see "Evidence rules"), so ignore
`systems-brief.md`'s verification protocol entirely.

Read, in order:
1. `docs/content-brief.md` — mechanics, MDX safety, voice
2. `docs/systems-brief.md` — the posture for a reader with no formal CS background
3. This file — anatomy, accuracy rules, per-topic mandates
4. `src/data/curriculum.ts` — the content tree; the ONLY valid link targets
5. `src/pages/systems/process-and-thread/index.mdx` — the voice and anatomy exemplar
6. `src/pages/system-design/networking/index.mdx` — the *applied* page this section feeds; do
   not duplicate it (see "The line against `/system-design/networking/`" below)

## The reader — read this twice

Senior .NET engineer, 11 years. Ships and operates real services. **Never studied networking,
at all.** They have used `HttpClient`, opened a firewall port because a runbook told them to,
and read a `SocketException` stack trace — but nobody ever explained what a packet is, what a
port actually *is*, or what happens between typing a hostname and the first byte arriving.

That combination is the editorial problem:

- **Never condescend.** They have shipped more production code than the person writing this
  page. No encouragement, no "don't worry", no "as you probably know".
- **Never assume.** If a page uses "packet", "frame", "header", "socket", "MTU", "hop",
  "resolver", "handshake" — the first page that uses it defines it in one clean sentence, and
  later pages link back rather than redefining. `network-basics` and `osi-model` define from
  absolutely nothing.
- **Name the overloads.** This subject is full of words that mean two things: "packet" (any
  chunk vs. the layer-3 PDU specifically), "address" (IP vs. MAC vs. a memory address, which
  this reader has just spent fifteen pages on), "socket" (the kernel object vs. `Socket` the
  C# class vs. `1.2.3.4:80` written down), "port" (the number vs. the switch port on the wall),
  "gateway" (the router vs. the API gateway they deploy). Say which you mean, name the other.
- **No filler.** No "in this article we will", no closing paragraph restating the page.

## The four jobs every page does

| goal | where it lives |
| --- | --- |
| genuine mental model (fill the gap) | `## how it actually works` + `## the mental model` |
| debug a real production incident | `## why you should care` |
| answer senior/staff interview questions | `## interview drills` |
| know which command to type | the concrete commands in `## how it actually works`, and the whole of `network-troubleshooting` |

A page that is all protocol vocabulary and no incident has failed. So has one that is all
"just check the firewall" folklore with no mechanism underneath.

## File locations

- Topic page: `src/pages/networking/<topic-slug>/index.mdx`
- Exercise page: `src/pages/networking/<topic-slug>/<exercise-slug>.mdx`

Frontmatter contracts are unchanged:

```
---
layout: '@layouts/TopicLayout.astro'
topic: <topic-slug>
---
```

```
---
layout: '@layouts/ProblemLayout.astro'
topic: <topic-slug>
problem: <exercise-slug>
---
```

NEVER touch `src/data/curriculum.ts`, `src/data/cheats.ts`, layouts, components, or
`global.css`. Create ONLY your assigned MDX files — nothing under `bench/`, no scratch programs,
no harness.

## Topic page anatomy

Sections in this order, lowercase `##` headings. No `#` h1. Never type a `// ` prefix in a
heading — CSS injects it.

1. `## the ground floor` — **required on every page in this section.** 3-6 bullets, one line
   each, defining exactly the terms this page leans on, linking the page that owns each. The
   first two pages define from nothing; every later page assumes only what an earlier page
   defined.
2. `## core idea` — 2-4 sentences. What this layer/protocol/mechanism exists to solve, and the
   one sentence worth carrying. If there are sub-shapes, one comparison table.
3. `## how it actually works` — **the teaching section, and the longest.** `###` subsections
   are encouraged here and nowhere else. Mechanism, concretely: what is in the header, who
   sends what next, who waits, what the kernel does with it. **At least two ```text diagrams
   are required** — a header layout with byte offsets, a packet's path across hops, a state
   machine, a timeline of two ends exchanging flights. This subject is drawn, not described.
4. `## the mental model` — the compressed version: the one table or three-line rule worth
   having six months from now. Optionally close with `<Watch items={...} />` carrying durable
   quantities (a header size, a port range, a record type, a mask width).
5. `## why you should care` — where this shows up in a real .NET service: the incident shape,
   the exception type and message, the metric that moves, the config knob that was wrong.
   Concrete enough to be recognised on a bad day. 2-4 paragraphs or a table.
6. `## the same idea elsewhere` — a short table. Cross-layer rows are the best ones: a port and
   a process id, an ARP cache and a TLB, a DNS TTL and a cache TTL, a retransmit and a retry
   with jitter. **Drop any row you are not certain of.**
7. `## exercises` — one intro line then `<ProblemList topic="<slug>" />`. **Only** on the two
   topics that have exercises (`ports-and-sockets`, `tcp-and-udp`). Omit entirely elsewhere —
   never render an empty list.
8. `## interview drills` — 4-6 drills, each:
   - `**Q.** the question as an interviewer says it`
   - `- *weak answer* — …` (the plausible one that invites a follow-up, and why it is weak)
   - `- *strong answer* — …` (what lands, 2-3 sentences)
   - `- *follow-up* — …` (what they ask next, with its one-line answer)

   Real questions. "How many layers does OSI have" is trivia. "Your service can reach the
   database from your laptop but not from the pod — walk me through what you check" is a drill.

## Exercise page anatomy

Both exercises in this section are **Kind A — predict, then work it out**, exactly as defined
in `docs/systems-brief.md`:

1. `## the question` — the setup, then `<Callout kind="insight" label="predict first">` asking
   for something checkable by reasoning (which field distinguishes two connections; how many
   reads the receiver gets). Never ask for a speed factor.
2. `## the code` — complete runnable code inside `<Solution>`, one ```csharp fence, blank lines
   around the fence.
3. `## work it out` — the mechanism by hand: the four-tuple table, the send/receive timeline.
4. `## the answer` — what the program prints, in a ```text fence introduced as *what this
   prints* (not as a captured session), and then why the prediction was right or wrong. For both
   exercises in this section the behaviour is already given under "Evidence rules" below — use
   those values, do not invent others.
5. `## why it works that way` — the general rule, closing with `<Watch items={...} />`
   (required, durable quantities only).
6. `## what this looks like in prod`
7. `## the same idea in other languages`
8. `## common bugs` — 3-5 bullets on how people get the reasoning wrong.

## No measurements. At all.

Identical to the rest of the site, and it bites hardest here because networking folklore is
made of numbers. **No page contains a duration, a throughput, or a speed ratio.** No ms, no
µs, no Mbps, no "twice as fast", no ping times, no "a round trip to us-east is 80 ms".

What you may and must state instead:

- **Counts of round trips.** "A cold TLS 1.3 connection costs one round trip for TCP and one
  for TLS before the request goes out" is structural and durable. It is also the only honest
  way to talk about network cost.
- **Sizes, offsets, ranges, and field widths.** A TCP header is 20 bytes without options; an
  IPv4 header is 20 bytes without options; the standard Ethernet payload MTU is 1500 bytes; a
  port is a 16-bit unsigned integer, so 0–65535; the registered ephemeral range and the
  common Linux default (`32768–60999`, from `/proc/sys/net/ipv4/ip_local_port_range`).
  These are facts the machine will state identically tomorrow.
- **Ordering and causation.** "The SYN cannot carry application data in the common case", "the
  resolver answers from cache without contacting the authoritative server until the TTL
  expires", "an RST arrives immediately, a dropped SYN produces a timeout" — that last
  distinction is the single most useful diagnostic fact in the section, and it is qualitative.
- **Published orders of magnitude, labelled as published**, only where a decision hangs on the
  ratio: intra-datacenter round trips are sub-millisecond against tens of milliseconds
  cross-region. Label them as industry reference figures for reasoning about relative cost,
  never as something measured for this page. Prefer to skip them entirely.

## Evidence rules

This section is a **reference tier**, and its owner has said so explicitly: pages are read to
build a model, not executed. **Do not run anything, and do not build a verification harness.**
That is a deliberate departure from `docs/systems-brief.md` and from `docs/content-brief.md`'s
dotnet harness — this brief overrides both on this point.

What that changes:

- **Code snippets are illustrative, not harness-verified.** Write them as if they had to compile
  — modern C#, correct namespaces, nullable-aware, no pseudo-code — but do not run them, and keep
  them short enough that correctness is visible by reading. Prefer a snippet that shows the API
  shape over one that proves a behaviour.
- **Never present output as captured.** No block may be introduced as "running this prints" or
  "here is the output". Where showing output helps, introduce it as *what this would print* and
  label the fence accordingly. The same applies to anything transcript-shaped from a command
  line: `dig`, `ss`, `ping`, `traceroute`, `tcpdump`, `curl -v` output must be introduced as an
  **annotated schematic** with placeholder names, never as a session someone had.
- **Invent no specifics that read as measurements or captures.** No timings anywhere (see the
  next section), no invented IP addresses presented as a real host's, no fabricated cache hit
  counts. Placeholder addresses in examples must be from the documentation ranges — `192.0.2.0/24`,
  `198.51.100.0/24`, `203.0.113.0/24` — or the private ranges, and should be introduced as
  examples.
- **Facts stay conservative.** Because nothing is being checked by execution, the bar on claims
  rises rather than falls: state the mechanism, state the field widths and the ordering, and cut
  anything you would only have believed after seeing it run. The Accuracy section below is the
  list that matters most, and it is all reasoning, not measurement.

### Facts about the authoring container, already checked — use these verbatim if you want them

These were read from this machine before writing began. They are the only machine-specific
values you may state as real, and you should present them as "on the Linux box these pages were
written on", not as universal:

```text
/proc/sys/net/ipv4/ip_local_port_range   32768   60999

/etc/resolv.conf
  nameserver 8.8.8.8
  nameserver 8.8.4.4
  options timeout:2 attempts:3

/proc/net/route  (hex fields are little-endian)
  Iface  Destination  Gateway   Mask
  eth0   00000000     010200C0  00000000     → default via 192.0.2.1
  eth0   000200C0     00000000  00FFFFFF     → 192.0.2.0/24 on-link

/sys/class/net/eth0/mtu   1400        (a tunnelled interface, below the 1500 Ethernet default)
/sys/class/net/lo/mtu     65536
```

Two behavioural facts, also already checked over loopback, which the two exercise pages are
built on and may state as what happens:

- TCP: two 3-byte writes (`AAA` then `BBB`) followed by one read on the far end returned all six
  bytes, `AAABBB`, in a single read.
- UDP: the same two sends arrived as two datagrams, and the first receive returned exactly `AAA`.

## Accuracy — the claims that get these pages rejected

This subject's folklore is dense and confidently wrong. Be exactly right about these:

- **OSI is a reference model, not an implementation.** The internet runs the TCP/IP stack,
  which has four layers (link, internet, transport, application). Say plainly that layers 5
  and 6 have no separate implementation in practice — TLS is the usual example people file
  under "layer 6", and it is really an application-layer protocol that presents a byte stream.
  Do not present the seven layers as a description of running software.
- **A port is not owned by a process in the way people think.** A port number is a 16-bit
  field in a TCP or UDP header. The kernel demultiplexes an incoming segment using the
  four-tuple (source IP, source port, destination IP, destination port) plus protocol; a
  listening socket is matched on the destination pair only after no established connection
  matches. One listening port serves an unbounded number of concurrent connections — the
  connections are distinguished by the *client* side of the tuple, not by the server port.
- **`SO_REUSEADDR` is not "let two servers share a port".** On Linux it mainly lets you bind
  while an old connection sits in `TIME_WAIT`. `SO_REUSEPORT` is the different option that
  lets multiple sockets accept on one port. Windows semantics differ again
  (`ExclusiveAddressUse`). Name the platform for every claim of this shape.
- **`TIME_WAIT` sits on the end that closes first**, is there to absorb delayed duplicates and
  to let the final ACK be retransmitted, and lasts 2×MSL. Client-side ephemeral port
  exhaustion is the usual production symptom, and the fix is connection reuse — not lowering
  the timeout, and never `SO_LINGER` with a zero timeout, which sends an RST and discards
  unsent data.
- **TCP gives you a byte stream with no message boundaries.** Two writes may arrive as one
  read or three. Framing is the application's job. This is the single most valuable thing an
  application developer learns from this section.
- **UDP is not "TCP without the handshake".** It has no ordering, no retransmission, no flow
  control and no congestion control; a datagram either arrives whole or does not arrive.
  Anything built on it that needs reliability rebuilds those in the application (QUIC does
  exactly this, in user space, and it is what HTTP/3 rides on).
- **NAT is not a firewall**, although it incidentally blocks unsolicited inbound. What it does
  is rewrite the source address and port and keep a translation table so replies can be mapped
  back. Say that this is why the source port a server sees is often not the one the client
  chose.
- **DNS is not "one lookup".** The stub resolver asks a recursive resolver; the recursive
  resolver walks root → TLD → authoritative unless a cache short-circuits it. Caches exist at
  four levels at least (the process, the OS, the recursive resolver, and any intermediate),
  each with its own TTL handling, which is why a record change appears to take effect at
  different times for different people. A low TTL is a request, not a guarantee — resolvers
  may clamp it.
- **DNS is not TCP-only or UDP-only.** Queries go over UDP port 53 and fall back to TCP when
  the response does not fit or the resolver requests it; zone transfers use TCP. DoH and DoT
  exist and use 443 and 853.
- **A CNAME cannot coexist with other records at the same name**, and cannot be at a zone
  apex — which is why hosting `example.com` (not `www`) behind a CDN needs ALIAS/ANAME or the
  provider's flattening. This one is worth stating because it bites in practice.
- **An `HttpClient` you keep forever pins DNS results** unless you set
  `SocketsHttpHandler.PooledConnectionLifetime`; a new `HttpClient` per request exhausts
  ephemeral ports. Both halves of that trap must appear, and `IHttpClientFactory` is the
  answer that solves both. Do not repeat the older "always singleton" advice unqualified.
- **A refused connection and a timed-out connection are different diagnoses.** RST means
  something answered and said no (nothing listening on that port, or a firewall configured to
  reject); silence to the timeout means the packet was dropped (a security group, a routing
  hole, a wrong address). Never conflate them.
- **MTU and fragmentation.** IPv4 routers may fragment, IPv6 routers never do — the sender
  must discover the path MTU. Path MTU discovery relies on ICMP, and dropping all ICMP is how
  people create the classic "small requests work, large ones hang" black hole.

Where you are not certain, state it as the reasoning it is, or cut it. A confident falsehood a
reader repeats in an interview is the worst thing this section can ship.

## The line against `/system-design/networking/`

`/system-design/networking/` ("The Network Path") already exists and covers, in depth and for a
reader who already ships services: the TCP and TLS 1.3 handshake round-trip accounting, 0-RTT
and its replay hazard, congestion control and slow start, HTTP/1.1 vs HTTP/2 vs HTTP/3 and the
two flavours of head-of-line blocking, keep-alive and connection pooling, and L4 vs L7 load
balancing.

**Do not re-teach any of that.** This section is the layer beneath it. Where a page reaches the
boundary, hand off explicitly in prose with a link to `/system-design/networking/`. Concretely:

- `tcp-and-udp` teaches the handshake as a mechanism (what SYN, SYN-ACK, ACK are, sequence
  numbers, the retransmit) and hands off congestion control and the round-trip cost accounting.
- `application-protocols` teaches HTTP's request/response anatomy, statelessness, cookies,
  status classes, and what TLS adds in outline — and hands off HTTP/2 vs 3 and the TLS
  handshake round trips.
- Neither page discusses load balancers beyond one sentence naming that a hop exists.

## Links

Internal links only to paths derivable from `curriculum.ts`, always with a trailing slash.
**Every page links at least three others**, and at least one of those is outside this section:
into `/systems/` (a socket read is a syscall — `/systems/process-and-thread/`; an ARP cache is
a cache like any other — `/systems/memory-hierarchy/`) or into `/system-design/`
(`/system-design/networking/`, `/system-design/caching/`, `/system-design/reliability/`).
Make those hand-offs in prose, not only in the connections panel.

Paths in this section are `/networking/<topic-slug>/` and
`/networking/<topic-slug>/<exercise-slug>/`.

## MDX safety (build breaks otherwise)

These pages are full of exactly the characters that break MDX.

- **Never a raw `<` or `{` in prose, headings, bullets, or TABLE CELLS.** This material wants
  to write `<html>`, `Dictionary<string, IPAddress>`, `payload < MTU`, `Socket<T>`,
  `<CR><LF>`, `192.168.0.0/16 <- gateway`. Every one goes in backticks or a fence. A `<` in a
  table cell breaks the build exactly as hard as one in a paragraph. **HTTP header and
  protocol examples belong in fences, always.**
- Component props use JSX braces — the one legal `{`.
- Blank lines around fenced blocks inside JSX components (`<Solution>`, `<Callout>`).
- No `#` h1. Never type `// ` in a heading.
- Arrows, em dashes, `≈`, `×` in prose are fine.
- **Run `node scripts/mdx-lint.mjs <your files>` before you finish. It must be clean.**

## Design system

Unchanged. Dark only, five accents with fixed meanings — **amber** = the sending/client/write
side, **cyan** = the receiving/server/read side, **purple** = the middle (a router, a resolver,
a NAT), **green** = success/established/answered, **red** = failure/reset/dropped. Never
introduce a colour, never use one against its meaning.

`<Callout>` kinds: `insight` (amber) · `tip` (green) · `trap` (red) · `note` (cyan).

## Cheat-card data (returned, NOT written into MDX)

For every topic you write, return a cheat object at the end of your report:

```
'<topic-slug>': { signals: [...], tricks: [...], bugs: [...] }
```

3-5 items each, backticks for code spans, same voice as `src/data/cheats.ts`. Read the groups
as:

- `signals` — "you are looking at this when…": the exception message, the log line, the
  metric, the ticket wording
- `tricks` — the moves that resolve it, and the default worth reaching for first
- `bugs` — what people get wrong, including the confident-but-wrong folklore above

The orchestrator writes these into `src/data/cheats.ts`. Do not edit that file yourself, and
do not duplicate this content in the MDX.

## Per-topic mandates

Non-negotiable content. Everything else is the writer's judgement.

### network-basics — "What a Network Actually Is" (assume nothing)

Two machines and a wire, then the whole internet. Must cover: what a link physically is (one
sentence, no more); why messages are **chopped into packets** and switched rather than a
circuit held open — the sharing argument, done concretely; that a packet is bytes with a
header and a payload, and a header is just fields at known offsets; **best-effort delivery** —
the network may drop, duplicate, delay or reorder, and every guarantee above it is built out
of that; **bandwidth versus latency** as two independent things (the truck-full-of-disks
intuition is allowed, without numbers) and why adding bandwidth does not fix a chatty
protocol; hosts, links, routers and what "hop" means; LAN vs WAN vs the internet as a network
of networks; and finally the argument **for layering**: nobody can change all of it at once,
so each layer talks to its peer and treats everything below as a pipe. Ends by handing off to
`osi-model`. This page defines "packet", "header", "payload", "host", "hop", "protocol".

### osi-model — "The OSI Model & What Actually Runs"

The seven layers named with one honest line each on what they are for, then the four-layer
TCP/IP stack that is actually implemented, then the correspondence and where it is loose.
**Encapsulation is the point of the page**: take one small HTTP GET and show it, in a ```text
diagram, gaining a TCP header, then an IP header, then an Ethernet frame header and trailer —
with the byte counts — and being unwrapped in reverse on arrival. Name the PDU at each layer
(frame, packet, segment/datagram, message) and say plainly that people call all of them
"packets" in conversation. A table of "layer → what addresses it uses → what it can and cannot
see → example protocols → what device operates here" is expected. State clearly what the model
gets wrong (layers 5-6, TLS's real position, that the model came from a competing stack that
lost). Close with why the model is still worth knowing: it is the shared vocabulary in every
incident channel and every interview.

### link-layer — "The Link Layer: Ethernet, MAC & ARP"

One hop, and only one hop. MAC address: 48 bits, burned in, flat (no structure to route on) —
and why that flatness is exactly why IP has to exist. The Ethernet frame layout with byte
offsets in a diagram; the payload range and where 1500 comes from. What a switch does that a
hub did not: learn a MAC-to-port table from source addresses, flood on a miss, and thereby
create separate collision domains. Broadcast domain, and that ARP is a broadcast. **ARP in
full**: host has an IP for the next hop, needs a MAC, broadcasts "who has 192.168.1.1", the
owner answers, the answer is cached with a timeout — show the exchange as a diagram. Say
explicitly that ARP resolves *the next hop*, not the destination, when the destination is off
the subnet; this is the fact that makes routing click. Then MTU: what it is, what happens when
a payload exceeds it, and a forward reference to path MTU discovery on `ip-and-routing`. One
paragraph on Wi-Fi as a link layer with different rules (collision *avoidance*, a shared
medium, retries below IP). VLANs in one paragraph. Mention IPv6 uses NDP rather than ARP.

### ip-and-routing — "IP Addresses, Subnets & Routing"

IPv4 addresses as 32 bits with a written form; dotted quad ↔ binary conversion shown once.
**Subnet masks and CIDR done by hand**: given `10.1.2.37/22`, derive the network address, the
broadcast address, the usable range and the host count, showing the bitwise AND — this
arithmetic is a required worked example, and it is the one thing interviewers actually ask.
Private ranges (`10/8`, `172.16/12`, `192.168/16`), loopback, link-local, and what
`0.0.0.0` means in a bind versus in a route. The routing table: longest-prefix match, the
default route, and a real decoded `/proc/net/route` from this container. What a router does to
a packet (decrement TTL, re-frame for the next link, leave the addresses alone) versus what
NAT does (rewrite source address and port, keep a translation table) — and the consequence
that the port a server sees is often not the port the client picked. TTL and how traceroute
exploits it (mechanism only — no fabricated traceroute output). ICMP as the network's
error-reporting channel, and why blanket-dropping it breaks path MTU discovery. IPv6 in a
tight section: 128 bits, the `::` compression rule, no NAT by design, no router fragmentation,
and why dual-stack means your resolver's answer order matters.

### ports-and-sockets — "Ports & Sockets"

**The page the reader most needs.** A port is a 16-bit unsigned field in the TCP or UDP header
— nothing more; it does not exist at the IP layer, which is why ICMP has no ports. The
**four-tuple** as the demultiplexing key, with a worked table of four simultaneous connections
to one server port, differing only in client port. Listening socket versus connected socket as
two different kernel objects, and the matching order (established connections first, listener
only if nothing matches). The port ranges: well-known 0–1023 and the privileged-bind rule on
Unix, registered, and the ephemeral range with this container's actual
`/proc/sys/net/ipv4/ip_local_port_range` value read and shown. Binding: `127.0.0.1` versus
`0.0.0.0` and why "it works on my machine but not from the pod" is usually this. The accept
queue (SYN queue and accept queue, `listen(backlog)`), and what a full queue looks like from
the client. `TIME_WAIT`: which end has it, why it exists, how long, and the ephemeral-port
exhaustion incident it causes — with connection reuse as the fix and an explicit warning
against the two folk remedies (`SO_LINGER` zero, cranking the timeout). `SO_REUSEADDR` vs
`SO_REUSEPORT` vs Windows `ExclusiveAddressUse`, each with its platform named. What a socket
is in C#: `Socket`, `TcpListener`, `TcpClient`, and that the handle is a kernel object — link
`/systems/process-and-thread/`. A runnable snippet that binds port 0, prints the assigned port,
accepts, and prints both endpoints. Exercise: `one-port-many-connections`.

### tcp-and-udp — "TCP & UDP"

The transport layer's job: take a best-effort packet service and offer something usable.
**TCP mechanism, in order**: the three-way handshake with initial sequence numbers and what
each flag means; sequence and acknowledgement numbers counting *bytes*, not packets;
cumulative ACKs; retransmission on loss (timeout-based and duplicate-ACK-triggered, named as
fast retransmit) — one diagram of a lost segment and its recovery is required; the receive
window as **flow control** and how a zero window stalls a sender; then the four-way close,
`FIN`/`ACK`, and the state machine (a ```text state diagram covering at least
`LISTEN`/`SYN_SENT`/`ESTABLISHED`/`FIN_WAIT`/`TIME_WAIT`/`CLOSE_WAIT`) — plus what a pile of
`CLOSE_WAIT` sockets means about *your* code, which is a real production tell. `RST` and what
it means. Nagle and delayed ACK named, with the interaction in one honest paragraph. **The
byte-stream truth**: no message boundaries, framing is yours — length prefix or delimiter —
and this is the section's most valuable practical lesson. Then UDP: 8-byte header, datagram
boundaries preserved, no ordering, no retransmission, no flow or congestion control; what it
is right for (DNS, discovery, telemetry, media, QUIC); and that QUIC rebuilds reliability in
user space on top of it. A comparison table. Hand off congestion control and slow start to
`/system-design/networking/` explicitly. Exercise: `stream-vs-datagram`.

### dns — "DNS: Names into Addresses"

The whole resolution walk, concretely: application calls the stub resolver; stub asks the
configured recursive resolver (show this container's real `/etc/resolv.conf`); the recursive
resolver, on a cache miss, asks a root server for the TLD's nameservers, the TLD server for
the zone's authoritative nameservers, and the authoritative server for the record — a ```text
diagram of that walk is required. Iterative versus recursive stated precisely (the resolver
does the iterating; the stub asks recursively). The namespace as a tree, zones, delegation,
and the trailing dot. Record types worth knowing: `A`, `AAAA`, `CNAME`, `NS`, `MX`, `TXT`,
`SRV`, `PTR`, `SOA` — one line each on what it is *for*, plus the CNAME-at-apex rule and the
"CNAME cannot coexist" rule. **TTL and the caches**: four levels, each capable of serving a
stale answer, why a change appears to take effect at different times for different users, why
you lower the TTL *before* a migration, and negative caching via the SOA minimum. Transport:
UDP 53 with TCP fallback, `EDNS0`, and DoT/DoH named. Failure modes: `NXDOMAIN` vs `SERVFAIL`
vs a timeout, and what each tells you. **.NET specifics, required**: `Dns.GetHostAddressesAsync`
(run it and show real output), the `HttpClient` DNS-pinning trap and
`SocketsHttpHandler.PooledConnectionLifetime`, `IHttpClientFactory`, and why "restart the pod"
fixes a stale-DNS incident. Teach what `dig` *asks* and what its answer sections mean, using an
explicitly-labelled annotated schematic — the tool is not installed here, so nothing may be
presented as a real transcript.

### application-protocols — "The Protocols on Top"

What all the layers were carrying. **HTTP first and mostly**: it is text over a byte stream —
show a real request and response, byte for byte, in a fence, including the CRLF line endings
and the blank line before the body; the request line, methods and what "safe" and "idempotent"
actually mean; status classes with the ones that get misused (`401` vs `403`, `301` vs `302`
vs `307`, `502` vs `503` vs `504`); headers that matter (`Host` and why HTTP/1.1 required it,
`Content-Length` versus `Transfer-Encoding: chunked` as the two framing schemes, `Connection`,
`Content-Type`); statelessness and cookies as the bolt-on that fixes it. Prove the "it's just
text" claim by writing a raw request over a socket — but since outbound HTTP here goes through
a proxy, do it against a **loopback listener you write yourself** that prints exactly what it
received. TLS in outline only: what it adds (confidentiality, integrity, server identity via a
certificate chain), where it sits, that HTTPS is HTTP over it on port 443, and SNI in one
sentence — then hand off the handshake round trips to `/system-design/networking/`.
WebSockets: the HTTP upgrade, then a framed bidirectional channel, and when it beats polling.
gRPC in a paragraph: HTTP/2, binary, streaming, contract-first. Then a short table of protocols
worth recognising with their default ports: SSH 22, SMTP 25/587, DNS 53, HTTP 80, HTTPS 443,
IMAPS 993, plus whichever of `SQL Server 1433`, `PostgreSQL 5432`, `Redis 6379`, `RabbitMQ
5672` the reader will meet. Say once, plainly, that the port number is a convention, not a
property of the protocol.

### network-troubleshooting — "Seeing the Network"

The page that pays for the section. Structure it as **the ladder, in order, with what each
rung proves**: (1) does the name resolve, (2) is the address routable from *here*, (3) is
something listening on that port, (4) does the TLS handshake complete, (5) is the payload
what you think. For each rung: the question, the command an engineer would type, what a pass
proves, and — the valuable half — **what a failure at that rung eliminates**. Required
distinctions: connection refused (RST — something is there and said no) versus timeout
(silence — dropped, so suspect a security group or a route) versus DNS failure versus TLS
failure versus a 502 from a proxy that never reached your service. A ```text decision tree
from "the service cannot reach the database" is required. Cover the tools by what they prove:
`dig`/`nslookup`, `ping` (and that ICMP being blocked proves nothing), `traceroute`, `ss`,
`curl -v`, `openssl s_client`, `nc`, `tcpdump`/Wireshark, and the .NET side —
`SocketException` and its `SocketError` values (`ConnectionRefused`, `TimedOut`,
`HostNotFound`), `HttpRequestException`, and `IPGlobalProperties` for listing listeners and
connections from inside the process. **Half these tools are not installed in the authoring
container**: teach what each one asks and what its answer means, use explicitly-labelled
annotated schematics rather than pasted transcripts, and where a fact can be produced from
`/proc/net/*` or C#, produce it for real and paste that instead. Close with the container/pod
cases the reader will actually hit: bound to `127.0.0.1` instead of `0.0.0.0`, a service DNS
name that only resolves inside the cluster, a security group that drops rather than rejects,
and MTU black-holing where small requests succeed and large ones hang.

## Exercise mandates

### ports-and-sockets / one-port-many-connections

Predict: three clients connect to one listening port — what does the kernel use to tell the
three connections apart, and how many sockets exist on the server? Then a runnable program
that starts a listener on `127.0.0.1:0`, opens three concurrent clients, accepts all three,
and prints each accepted socket's `LocalEndPoint` and `RemoteEndPoint` — showing three
identical local endpoints and three distinct remote ports. Add a fourth listing from
`IPGlobalProperties.GetActiveTcpConnections()` or `/proc/net/tcp` if it strengthens the point.
Work it out as a four-tuple table. The answer section shows the real captured output. The rule:
the four-tuple is the identity; a server port is not a resource that gets used up by
connections — the *client's* ephemeral ports are.

### tcp-and-udp / stream-vs-datagram

Predict: the sender does two `Write`s of 3 bytes each; how many bytes does one `Read` return
over TCP, and over UDP? Runnable program doing both over loopback (a small delay before the
first read to make coalescing reliable — and the page must say that this is what makes the
demonstration deterministic, which is itself the lesson: TCP is *allowed* to do either).
Answer shows the real output: TCP hands over `AAABBB` in one read, UDP hands over `AAA` then
`BBB`. Then the fix for TCP: a 4-byte length prefix, shown as code, with the read loop that
handles a partial read — and the note that `ReadExactly`/`ReadAtLeast` exist in modern .NET
for exactly this. Common bugs: assuming one write equals one read; assuming a read that
returns fewer bytes than asked means the connection ended; forgetting that a zero-length read
is the only end-of-stream signal.
