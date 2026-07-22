# Dual actor spike — same aggregate on Akka.NET and Orleans over EF state-store

## Description

Decides 113's actor-technology question **empirically** (Steve, 114 axis 5, 2026-07-22): build
the SAME example aggregate as an actor-hosted single-writer on **Akka.NET** and on **Orleans**,
~a day each, and pick with hands on rather than from summaries. Both are license-clean
(Akka.NET Apache 2.0 — BSL was JVM Akka only; Orleans MIT), so the choice rides on ergonomics
and fit.

Constraints fixed by the 114 axis decisions:

- **State-store EF only** — the actor is a concurrency/lifetime shell around the SAME golden
  aggregate (invariants, version token) and `PostgresDbContext` path: load on activation,
  mutate via domain methods, `SaveChanges`. NO Akka.Persistence / NO `JournaledGrain`
  (event sourcing explicitly out of scope).
- **Integration events** publish through the axis-3 substrate-agnostic seam — the spike should
  show the aggregate emitting one integration event from inside the actor via the abstract
  publish seam, proving the losing spike leaves no residue.
- Example aggregate: a simplified **credit ledger** (per-principal balance; credit/debit
  commands; overdraw invariant) — the textbook high-contention single-writer, and the real
  future consumer (104-010).

## Checklist

- [x] Prereq: [[113-001-wire-postgres-into-aspire-apphost-and-remove-sql-server-remnants]]
      (a live postgres for the EF path).
- [x] Shared substrate: the ledger aggregate (golden pattern: nested private Invariants,
      version token) + EF mapping + a thin repository/loader — written ONCE, used by both hosts.
- [x] Spike A — Akka.NET: actor per principal (mailbox = single writer), EF load on start /
      SaveChanges per command, Aspire wiring, one integration event published.
- [x] Spike B — Orleans: grain per principal, same EF state-store inside the grain (skip
      IPersistentState or use it as a thin wrapper — note which felt better), Aspire wiring
      (`Aspire.Hosting.Orleans`), same integration event.
- [x] Concurrency demonstration: N parallel conflicting commands against one principal — show
      serialization (no version conflicts) vs the plain-EF baseline (retries/conflicts).
- [x] Evaluation notes per candidate: template-consumer approachability (how much ceremony to
      first working aggregate), Aspire/dev-loop experience, testability under Fixie (can a spike
      test drive the actor without cluster scaffolding?), debugging story, AOT/trimming posture
      (no-runtime-reflection constraint from 114 axis 7), doc quality.
- [x] Comparison write-up into the 113 folder (`spike-actor-comparison.md`);
      decision recorded by Steve, not defaulted.
- [x] Spike code lives on a throwaway branch/worktree — NOTHING lands in template source from
      this task.

## Notes

- Akka.NET context: independent of JVM Akka since upstream 2.6.20 (Apache cut-off); Steve knows
  the author. Orleans context: Microsoft first-party, virtual-actor model, state-store-first.
- If both feel wrong, "no actors in the template, EF golden path only" remains a valid outcome —
  the 114 axis-5 decision made actors optional-with-example, not mandatory.

## Session

- Created: 2026-07-22 (from 114 axis-5 in-chat decision)

### Implementation plan (Phase 2, 2026-07-23)

Worktree `spike-113-002`, branch `spike/113-002-dual-actor`; code under `spikes/113-002-dual-actor/`
with OWN slnx + cascade-stopper Directory.Build.props + spike-only Directory.Packages.props (root
CPM/analyzers untouched; root slnx never edited). Golden pattern via ProjectReference to
foundation-domain/-application (same source, not lagging packages). Known duplication: sealed
PostgresDbContext forces replicating the ~40-line SaveChanges hook (calling the SAME
DomainInvariantsGuard/EntityVersion seams) — recorded as seam-packaging finding for 113 decision 5.

Layout: ledger-substrate (aggregate w/ nested private Invariants + overdraw guard, LedgerDbContext
w/ golden hook + IsConcurrencyToken, thin store, IIntegrationEventPublisher seam + recording impl —
ZERO actor refs, substrate-agnostic proof is structural); akka-host (Akka/Akka.Hosting/DI 1.5.70,
local ActorSystem, supervisor + child-per-principal, Ask pattern, NO Persistence/Sharding);
orleans-host (Orleans 10.2.2, UseLocalhostClustering, grain-per-principal, direct EF — NO storage
provider; IPersistentState assessed in a 1h box); spike-tests (Fixie/Shouldly/TimeWarp.Fixie
matching repo pins — the evaluation criterion is the REAL template stack); optional app-host
(Aspire, ephemeral postgres).

Concurrency demonstration = repeatable Fixie tests: baseline plain-EF 50-parallel-debits asserts
≥1 DbUpdateConcurrencyException + retry-count report; Akka and Orleans same shape through one
actor/grain assert ZERO conflicts, balance exact, Version+50, 50 recorded events. Postgres:
ephemeral container port 5433 (NEVER the dev-run volume — 113-001 WAL lesson), EnsureCreated per
run, SPIKE_POSTGRES_CONNECTION override.

Verified versions: Akka 1.5.70 (net6 TFM — net10 forward-compat only, first-hour risk gate),
Orleans 10.2.2 (first-class net10.0 + source-gen serializers), Aspire.Hosting.Orleans 13.4.6
(Akka has no Aspire integration — itself a finding). Phases: 0 scaffold (1-2h) → 1 substrate+
baseline green (½d) → 2 Akka (1d cap) → 3 Orleans (1d cap) → 4 Aspire wiring (2-3h, skippable)
→ 5 comparison+Results+teardown. Stuck rule: 75% of box without green test → document blocker
as THE approachability finding, move on. Akka first.

Write-up: spike-actor-comparison.md into the 113 PARENT folder on dev — factual observations per
task criteria incl. measured startup, ceremony counts, debugging notes, AOT posture (Orleans
source-gen vs Akka HOCON/reflection, shared EF caveat), IPersistentState verdict, substrate-residue
proof, "no actors" outcome kept alive. NO recommendation decision (clearly-labeled lean OK; Steve
gates per axis 5).

- Plan: 2026-07-23 (plan agent; versions verified on nuget.org)


## Results

**Spike complete 2026-07-23 — branch `spike/113-002-dual-actor` @ `fe0296e` (worktree torn
down, branch retained). Both candidates GREEN in ~1 hour each; no stuck-rule triggers; all five
Fixie demonstrations re-verified independently by the orchestrator (baseline 795 retries vs 0
conflicts through either actor; balance/version/events exact).**

Full findings: `../113-golden-persistence-implementation-*/spike-actor-comparison.md` — ceremony
counts (Orleans ~88 glue lines vs Akka ~114 + hand-written router), measured
domain-error-propagation difference (Akka Ask swallows by default; Orleans propagates with real
stack), IPersistentState verdict SKIP under EF-only, substrate-residue proof, seam-packaging
finding (sealed PostgresDbContext → hook duplication → 113 decision 5), "no actors" outcome
kept fully alive, in-proc bias caveat, implementer lean toward Orleans (labeled, not decided).

**OPEN: the decision itself — Steve gates per axis 5.** Options: Orleans example / Akka example
/ no actors in the template (EF golden path only, actors documented as consumer choice).

## Session

- Orchestrated 2026-07-23: plan (versions nuget-verified) + build (phased, time-boxed) +
  orchestrator verification re-run.
