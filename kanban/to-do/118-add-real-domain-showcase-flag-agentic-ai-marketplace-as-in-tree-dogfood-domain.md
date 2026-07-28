# Add real-domain showcase flag - agentic AI marketplace as in-tree dogfood domain

## Description

Amend the standing "no demo-content flags" rule (template-flags-architecture-axis-only) with
ONE recorded exception: a showcase-domain flag. Add a `real-domain` template flag, **default ON
during development, flipped OFF before release** so the shipped template stays lean while the
repo dogfoods a real domain daily.

Domain (Steve, 2026-07-23): **agentic AI marketplace / metered capability service** — unifies
the 104 program end to end: passkey humans + keyed agents (identity), credit ledger
(104-010-shaped, accounting flavor lives here), x402 metered endpoints, and the agent discovery
surface (104-017/019). **Expanded vision (Steve, 2026-07-23): "agent Alibaba" for a bamboo microfactory.** A small
Thailand bamboo products shop — CNC machine, 3D printer, 2–3 humanoid robots (all
simulated/mocked) — whose fabrication capabilities are sold THROUGH the agent marketplace:
an external buyer's agent discovers the shop (104-017/019 discovery surface), registers a key,
pays via x402 for quotes/machine time, submits a design, tracks the job; humans passkey in to
approve and oversee. Two layers with separate sequencing:

1. **Marketplace layer (priority one, pure software, no hardware dependency)**: catalog, quote,
   order/job, ledger, metering — every 104 program piece gets a real noun.
2. **Fleet layer (the actor showcase, AFTER the 113 gate)**: simulated devices as supervised
   actors — realistic state machines (idle→setup→running→fault), fault injection demoing
   supervision/restart, backpressured telemetry ingestion. Humanoids are fleet flavor
   (telemetry + work-order execution), NOT general task planners — scope guard.

V1 scope guard: ONE product family, quote→pay→job→"ship" happy path; expand only after the
loop closes end to end. Bamboo angle: authentic, sustainable, memorable — no template demos a
microfactory.

Rejected alternatives (recorded): sibling showcase repo (duplicate-work cost while the template
is unstable; in-tree default-OFF is a viable permanent end state); ecommerce and accounting as
domains (accounting flavor survives inside the ledger).

## Checklist

- [ ] Record the rule amendment in AGENTS.md + agent memory: flags remain architecture-axis
      only; `real-domain` is the ONE sanctioned showcase exception (default ON in dev, OFF at
      release)
- [ ] Gate via `sources.modifiers` folder exclusion per axis-6 (in-file `#if` only where truly
      line-granular — AppHost/YARP seams)
- [ ] Add a `real-domain off` cell to the 115 template-smoke CI gate from day one (both flag
      states generate + restore + build 0/0)
- [ ] State flag-combination constraints: requires `web` + `postgres`; handle degenerate combos
      per the 113-001 orphan-container lesson (declare inside the enabling blocks)
- [ ] Enforcement so platform code never depends on showcase types (TWA0009 posture / review
      rule) — protects the OFF path between CI smokes
- [ ] Mechanical release-flip guard: release workflow fails if template.json ships
      real-domain default ON (no agreement-by-memory on the flip)
- [ ] Slice scaffolding for the marketplace domain under `web/features/` per tw-feature-placement
      grammar (identity/ledger/metering/discovery areas; device-fleet module deferred until the
      113 actor gate)

## Notes

Sequencing (Steve's priority pass, 2026-07-23): (1) gate 113-002 actor decision, (2) this
task's spec, (3) 107 YARP route generation BEFORE showcase slices multiply hand-maintained
routes, (4) 113 golden implementation + 104-032 durable identity/ledger, (5) 116 + publish
residuals in the background lane. Release checklist must include flipping the flag default OFF.

Actor-gate outcome feeding this task (2026-07-23): marketplace/aggregate layer uses the EF
golden path with **Orleans** for aggregates that earn actor hosting; the fleet layer evaluates
**Akka.NET** where supervision/streams genuinely fit (spike evidence: 113 folder).
- Web-server exemplar replacement (Steve, 2026-07-27, from the 126-011 residue review): when
  the marketplace layer lands, replace the `web-server/configuration/` sample exemplars
  (`sample-options.cs` + validator, `sample-environment-check.cs`) with REAL domain options
  using the same AddFluentValidatedOptions/ValidateOnStart and environment-check patterns
  (e.g. x402 pricing/metering options, fleet endpoints config) — the live-compiled teaching
  value stays, the "sample" goes.
- **Host-role mapping (Steve + orchestrator, 2026-07-28)** — the marketplace activates the
  original BFF/SaaS split rather than replacing it:
  - **web = the human plane**: passkey ceremonies, approving agent key registrations,
    overseeing jobs. Cookie/passkey/session auth surface (platform/identity-host).
  - **api = the agent plane**: discovery surface, agent-token auth, x402 metered endpoints,
    quote/order/job APIs. Bearer/x402 auth surface, separate ingress route space (TWA0017),
    separate scaling profile. Marketplace-layer endpoints target api-server from day one — not
    web-by-default-because-dogfooding-lives-there. Prereq: axis-1 extraction for the api family
    (task filed separately).
  - **grpc = the fleet plane** (stage 2): backpressured telemetry ingestion from the simulated
    devices.
- **Boundary caution (Steve, 2026-07-28): the agent/human overlap is bigger than it looks.**
  Do not treat web=humans-only as a hard wall. Expect agent-driven UX for the human
  counterpart — an agent composing/serving custom surfaces for its human (tailored approval
  views, job dashboards, negotiated-quote summaries), agent-initiated flows that *land* in the
  human plane for a passkey approval, and humans delegating mid-flow back to agents. Design the
  seam so the agent plane can project UX into the human plane rather than assuming each plane
  owns its audience exclusively. This territory is new but the industry is converging on it —
  **agent traffic now exceeds human traffic**: Cloudflare (CEO Matthew Prince, 2026-06-03,
  Cloudflare Radar data spanning ~1/5 of all websites) reports 57.5% of HTTP requests are
  automated vs 42.5% human — the first crossover ever recorded, arriving ~18 months before
  Prince's own end-of-2027 projection, driven by agentic AI. Coverage:
  https://www.nbcnews.com/tech/tech-news/bot-web-traffic-overtaken-human-web-traffic-data-shows-rcna348522
  and https://www.tomshardware.com/tech-industry/artificial-intelligence/bots-have-now-passed-human-traffic-online-cloudflare-boss-laments-says-agentic-traffic-wasnt-expected-to-eclipse-real-people-until-next-year
  (discovery via https://x.com/coinbureau/status/2081585618384273841, 2026-07-28).
