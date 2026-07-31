# Research Aspire and Jaribu assembly fixture strategy for zero Fixie

## Description

Research task (no product code required until findings land). Goal: decide whether **zero Fixie**
is the north star, and design a **cleaner lifetime/composition model** than today’s mix of:

- co-located Jaribu **single-file** runfiles (primary product path; proven tasks 134–136),
- **multi-file** `JARIBU_MULTI` aggregators under `tests/` (CI entry; MTP),
- class-scoped **`SetupOnce` / `CleanUpOnce`** (Jaribu ≥ 1.0.0-beta.14; timewarp-jaribu#19),
- Fixie + **TimeWarp.Fixie** assembly DI singletons for multi-host graphs (Web / Api / Yarp / Spa),
- hand-rolled `WebApplicationHost` fixed ports and serialized `dev test`.

We own **Jaribu** and **TimeWarp.Fixie**, so vendor lock-in is not the constraint — architecture
clarity is. Also re-evaluate **Aspire** (`Aspire.Hosting.Testing` is pinned, barely used): can
AppHost-driven multi-resource tests replace or shrink the multi-host Fixie graph without losing
“mock only externalities” / `configureServicesDelegate`?

**Out of scope for this task:** implementing zero Fixie, migrating suites, or shipping Jaribu
assembly hooks — deliver a written recommendation and follow-up task list only.

## Requirements

1. **Map current lifetimes** — document what Fixie/TimeWarp.Fixie assembly singletons give us
   today vs Jaribu class scope vs process-static Lazy; include YARP→Web ordering and SPA
   `BaseTest` / state host.
2. **Aspire fit** — survey `Aspire.Hosting.Testing` against real suites (endpoint single-service,
   YARP multi-host, postgres-backed, ingress-smoke). State clearly what Aspire replaces, what it
   cannot (DI substitution, fixed-port hand hosts), and whether a hybrid is cleaner.
3. **Jaribu fixture levels** — evaluate options for “assembly-level” or cleaner shared fixtures
   without copying Fixie DI:
   - pure co-located + per-file SetupOnce (no assembly hooks),
   - aggregator-scoped / run-scoped hooks (multi-class, one process),
   - explicit shared fixture module (not constructor DI),
   - assembly SetupOnce/CleanUpOnce in Jaribu upstream.
   Prefer the smallest model that enables **zero Fixie** without reintroducing undisposed Lazy.
4. **Single-file first** — preserve Jaribu’s single-file story as the default authoring mode;
   multi-file/`JARIBU_MULTI` remains CI composition, not the place humans write new tests.
5. **Recommendation** — north star (zero Fixie or not), preferred lifetime model, Aspire role,
   ordered follow-up tasks (Jaribu upstream vs TWA only). No calendar estimates.
6. **Write findings** under this folder (`findings.md` or equivalent); link prior art:
   `kanban/done/134-spike-jaribu-co-located-integration-tests/` (esp. Aspire survey + §8),
   tasks 135–136, Jaribu #19/#20.

## Checklist

- [ ] Inventory Fixie assembly DI + multi-host consumers (web/api/yarp/spa)
- [ ] Inventory Jaribu modes in-repo (standalone runfile, JARIBU_MULTI aggregator, SetupOnce)
- [ ] Aspire.Hosting.Testing: capabilities vs gaps for this monorepo (update or supersede 134 survey)
- [ ] Compare fixture-model options; pick a preferred design (with rejected alternatives)
- [ ] Zero-Fixie feasibility: remaining true blockers only (no calendar fluff)
- [ ] Write `findings.md` + recommended follow-up task titles (create via `ganda kanban create` only if ready to schedule)
- [ ] Kanban mutations committed

## Notes

### Context (session 2026-07-31)

- Product-slice co-location + family aggregators are **proven** (create-role, weather-forecast;
  SetupOnce dispose on api host).
- Discussion: zero Fixie is a valid goal because we own the stack; real gaps were assembly
  multi-host lifetime, SPA BaseTest pattern, and reshape-vs-port — not ownership.
- Hypothesis to test: Aspire may compose multi-resource scenarios more cleanly than Fixie DI
  singletons; single-file Jaribu stays the authoring unit; “assembly config” might be
  aggregator/run-scoped rather than Fixie-like constructor DI.
- Prior Aspire note (134 §8 Q3): deferred multi-resource tier; re-open with eyes on zero-Fixie.

### Non-goals

- Temporal estimates of any kind (see root `AGENTS.md` — Agent communication).
- Implementing migrations in this task.

## Session

- Created: 2026-07-31 (follow-up to task 136 + zero-Fixie / Aspire design conversation)

## Results

_Added after research completes._
