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
    — task 129 (multi-family grammar machinery + api/grpc features/platform trees).
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
- Second exemplar replacement (Steve, 2026-07-28, from 129 placement rulings): api-server's
  `generic-pipeline-behavior.cs` (live wired placeholder, console writes) is replaced when real
  cross-cutting concerns land — x402 metering is a pipeline behavior; same
  replace-the-sample-with-real pattern as the web-server sample-options note above.

## Bamboo micro-factory reference data (Steve, 2026-10-05)

Real-world reference for the showcase domain: product families, machine capabilities and
pricing logic for quotes and machine time. The showcase's machines stay simulated or mocked per
the Description. These numbers make the simulated shop and its quotes believable; they are not a
purchase plan for the template.

### Small-scale parts list by stage

**Stage 1: Splitting** (entry point, ~$400–2,600)
- Electric bamboo splitter, single-blade, motor-driven
- Manual pole splitter (3–24 blades) as backup and for odd sizes
- Spare blades for the splitter
- Safety guards and feed table

**Stage 2: Sizing and planing** (~$1,000–3,000)
- Cross-cut saw, fine-tooth carbide (24+ TPI)
- Four-side planer (small workshop model)
- Node removal / flattening jig
- Thickness gauge / calipers

**Stage 3: Treatment and drying**
- Carbonizing boiler or treatment tank (borate solution)
- Kiln or solar dryer
- Moisture meter

**Stage 4: Pressing and finishing** (~$8,000–15,000)
- Flattening press line
- Glue coater
- Hot press (750–800 ton range for panels)
- Sanding machine
- Dust extraction system

**Stage 5: Precision** (optional, $20,000+)
- CNC router for cut parts
- Blade sharpening equipment

**Thai-built reference machines**
- Srinakharinwirot University splitting/slicing machine: ~90 THB/hour operating cost, 2.5-year
  payback.
- RMUTSB semi-automatic strip machine: 30,000 THB, 15-day payback, 1 mm minimum thickness.

### Break-even math

The figures come from two Thai university machine papers, not a factory quote. Treat them as
order-of-magnitude until a real machine is timed. Currency is THB. The labor rate is a
placeholder.

**Published anchors**

- Srinakharinwirot splitting/slicing machine (2021 paper):
  - Operating cost 90.1 THB/hour; break-even 156 hours/year.
  - Payback 2.5 years at 300 hours/year.
  - Throughput 7.12 sticks/min split, 5.65 slices/min, about 2.6–2.7× hand work.
- RMUTSB semi-automatic strip machine (2021 paper):
  - Capital 30,000 THB; power 356 THB/month.
  - Claimed payback 15 days.
  - Claimed throughput 117 strips/min at 60 cm × 7 mm (7,020/hour); the paper's hand baseline
    is 1,003/hour.
  - The 15-day payback only holds if those strips already have a buyer at a margin that covers
    30,000 THB in half a month. Do not plan on it.

**Model**

```
Fixed:
  C = machine cost (THB)
  H = hours/year actually run

Variable, per hour:
  P = kWh × THB/kWh
  L = operator THB/hour (loaded, not the posted wage)
  M = blades, belts, glue, borate, allocated per hour
  v = P + L + M

Revenue, per hour:
  Q = good strips/hour (after rejects)
  S = sale price per strip (THB)
  r = Q × S

Contribution:
  c = r − v
  Break-even hours/year = C / c          # only if c > 0
  Payback years         = C / (c × H)
```

**Worked example (replace every input)**

30,000 THB strip machine, run 4 hours/day for 250 days (H = 1,000). Power 1.5 kW at
4.5 THB/kWh. One operator at 150 THB/hour loaded. Consumables 20 THB/hour.

```
P = 1.5 × 4.5 = 6.75
L = 150
M = 20
v = 176.75 THB/hour
```

With a buyer: a 60 cm strip sells for 2 THB, at 200 good strips/hour (well under the paper's
claim):

```
r = 400
c = 400 − 176.75 = 223.25 THB/hour
Break-even hours = 30,000 / 223.25 ≈ 134 hours
Payback at 1,000 h/year ≈ 0.13 year
```

Same machine with no offtake: strips worth 0.5 THB, 80 good/hour:

```
r = 40
c = 40 − 176.75 < 0   → never pays back; throughput does not matter
```

**What actually sets payback**

1. **Offtake price, not machine speed.** China sets the ceiling on commodity strips. A 2 THB
   strip only exists when the shop is not competing with Anji on the same SKU.
2. **Reject rate.** Silica and nodes eat strips. Measure good output, not feed rate.
3. **Hours the machine can be fed.** A 15-day payback assumes the pole pile and the buyer are
   both already there.
4. **Labor dominates v.** The machine is the cheap line; an idle operator is the expensive one.

**Gate before buying stage 4**

Do not buy the press until stages 1–2 have 90 days of:
- logged good strips/hour;
- a named buyer and a price;
- c > 0 at that price.

A press at 8,000–15,000 USD only changes the product. It does not create the buyer.

### How this informs the showcase (when 118 is designed)

- **Product families and capabilities:** the stages map to the simulated shop's machines and the
  quote options an external agent can ask for (split, size/plane, treat/dry, press/finish,
  CNC cut).
- **Quote pricing:** the per-hour model (`v`, `Q`, reject rate) is a credible basis for
  machine-time quotes paid via x402.
- **V1 scope guard (unchanged):** one product family. Strips (stages 1–2) are the natural first
  family, which matches the "gate before stage 4" rule above.
- 118 still needs Steve's decision on the flag-rule amendment before any implementation.
