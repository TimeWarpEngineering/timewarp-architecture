# Add real-domain showcase flag - agentic AI marketplace as in-tree dogfood domain

## Description

Amend the standing "no demo-content flags" rule (template-flags-architecture-axis-only) with
ONE recorded exception: a showcase-domain flag. Add a `real-domain` template flag, **default ON
during development, flipped OFF before release** so the shipped template stays lean while the
repo dogfoods a real domain daily.

Domain (Steve, 2026-07-23): **agentic AI marketplace / metered capability service** — unifies
the 104 program end to end: passkey humans + keyed agents (identity), credit ledger
(104-010-shaped, accounting flavor lives here), x402 metered endpoints, and the agent discovery
surface (104-017/019). Includes a robotics-flavored **device-fleet telemetry module** as the
future Akka.Streams/actor showcase (actor-per-device supervision + backpressured ingestion) —
sequenced after the 113 actor decision lands.

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
- [ ] Slice scaffolding for the marketplace domain under `web/features/` per tw-feature-placement
      grammar (identity/ledger/metering/discovery areas; device-fleet module deferred until the
      113 actor gate)

## Notes

Sequencing (Steve's priority pass, 2026-07-23): (1) gate 113-002 actor decision, (2) this
task's spec, (3) 107 YARP route generation BEFORE showcase slices multiply hand-maintained
routes, (4) 113 golden implementation + 104-032 durable identity/ledger, (5) 116 + publish
residuals in the background lane. Release checklist must include flipping the flag default OFF.
