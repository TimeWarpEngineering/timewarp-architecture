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

- [ ] Prereq: [[113-001-wire-postgres-into-aspire-apphost-and-remove-sql-server-remnants]]
      (a live postgres for the EF path).
- [ ] Shared substrate: the ledger aggregate (golden pattern: nested private Invariants,
      version token) + EF mapping + a thin repository/loader — written ONCE, used by both hosts.
- [ ] Spike A — Akka.NET: actor per principal (mailbox = single writer), EF load on start /
      SaveChanges per command, Aspire wiring, one integration event published.
- [ ] Spike B — Orleans: grain per principal, same EF state-store inside the grain (skip
      IPersistentState or use it as a thin wrapper — note which felt better), Aspire wiring
      (`Aspire.Hosting.Orleans`), same integration event.
- [ ] Concurrency demonstration: N parallel conflicting commands against one principal — show
      serialization (no version conflicts) vs the plain-EF baseline (retries/conflicts).
- [ ] Evaluation notes per candidate: template-consumer approachability (how much ceremony to
      first working aggregate), Aspire/dev-loop experience, testability under Fixie (can a spike
      test drive the actor without cluster scaffolding?), debugging story, AOT/trimming posture
      (no-runtime-reflection constraint from 114 axis 7), doc quality.
- [ ] Comparison write-up + recommendation into the 113 folder (`spike-actor-comparison.md`);
      decision recorded by Steve, not defaulted.
- [ ] Spike code lives on a throwaway branch/worktree — NOTHING lands in template source from
      this task.

## Notes

- Akka.NET context: independent of JVM Akka since upstream 2.6.20 (Apache cut-off); Steve knows
  the author. Orleans context: Microsoft first-party, virtual-actor model, state-store-first.
- If both feel wrong, "no actors in the template, EF golden path only" remains a valid outcome —
  the 114 axis-5 decision made actors optional-with-example, not mandatory.

## Session

- Created: 2026-07-22 (from 114 axis-5 in-chat decision)
