# Postgres + EF Core as the golden persistence path

* Status: accepted
* Architect: Steven T. Cramer
* Consulted: kanban 113 recon and soft-gate plan; dual actor spike (113-002); 114 axis 5
* Date: 2026-07-23

Technical Story: kanban 113 (golden persistence implementation), children 113-001…113-005;
unblocks 104-032 (identity EF store)

## Context and Problem Statement

The template's persistence story was half-built: `PostgresDbContext` already hosted the golden
aggregate SaveChanges seam (task 106 invariants + store-owned `Version`), and postgres plumbing
shipped behind a template flag, but AppHost provisioned no Postgres resource, SQL Server remnants
still sat in CPM, identity stores were in-memory only (restart-wipes made externally visible in
112), and open questions remained about actors, event sourcing, outbox, and where the golden
seam should live. What should every generated app inherit as the **default durable path** for
aggregates?

## Decision Drivers

* One relational axis — no dual SQL Server / Postgres teaching story
* Teach "add your aggregate" with a real end-to-end exemplar, not an empty DbContext
* Fail-closed aggregate rules (TWA0011/0012 + SaveChanges invariants) must not depend on a
  single host context implementation
* Day-one zero-setup for template consumers; grown apps must have a clear migration path
* Do not pull high-ops actor or outbox stacks into the default learning curve
* Identity (104-032) needs a stable seam and test pattern as the first product consumer

## Considered Options

* **A — Status quo drift**: keep Postgres plumbing + empty model; leave SQL Server placeholder;
  defer packaging and docs
* **B — Dual relational engines**: reintroduce SQL Server as a first-class flag beside Postgres
* **C — Event sourcing first** (e.g. Marten on Postgres) as the golden path
* **D — Actors as THE aggregate host** (Akka.NET or Orleans everywhere), EF optional
* **E — Postgres-only state-store EF golden path** with Foundation `GoldenDbContext`, optional
  Orleans later, outbox deferred, Profile as teaching aggregate

## Decision Outcome

Chosen option: **E — Postgres-only state-store EF golden path**, because it closes the half-built
seam without introducing a second paradigm, keeps the template approachable, and still leaves a
coherent upgrade path for contention and durable messaging when earned.

Concrete shape:

* **Postgres-only.** No SQL Server package, no `SqlDbContext`, no `sqlserver` template flag.
  AppHost provisions Postgres when the flag is on (113-001).
* **State-store EF Core** is the golden persistence shape. **No event sourcing** for now —
  revisit only if a concrete product need appears. Actors do not require event sourcing;
  Orleans grain-per-entity-ID over the same EF state store remains coherent (114 axis 5).
* **`GoldenDbContext`** lives in `TimeWarp.Foundation.Infrastructure` /
  `TimeWarp.Foundation.Persistence`. Hosts (`PostgresDbContext` today; future api/grpc or
  package consumers) inherit it so SaveChanges enforcement is not sealed inside one product
  context. Npgsql stays host-only.
* **Two-party `Version` contract.** The golden hook increments `Entity{TId}.Version` on every
  Modified aggregate root via the change tracker. The host mapping **must** call
  `.IsConcurrencyToken()` on `Version` for each concrete aggregate; without that half, the
  increment is silent bookkeeping and stale overwrites still succeed.
* **Child → root resolution.** Mutating only a child/owned entity marks the owning
  `IAggregateRoot` Modified so invariants and Version still run (113-003).
* **Schema-per-slice on a single DbContext by default.** Product tables use PostgreSQL schemas
  (e.g. Profile → schema/table `profiles`) via `ToTable` on one `PostgresDbContext`. A second
  DbContext per module is an earned exception when isolation or extraction demands it — not day
  one scaffolding.
* **EnsureCreated for template zero-setup**; **`Database.Migrate` for grown apps** that need
  schema evolution. Documented; not automated migration-only.
* **Actors optional.** EF remains the default aggregate host. Aggregates that earn
  single-writer / high-contention hosting (e.g. credit ledger) use **Orleans** (grain-per-entity-ID
  over direct EF — spike rejected `IPersistentState` as the primary path). **Akka.NET** is
  reserved as the candidate for task 118's device-fleet layer (supervision/streams), not the
  default entity host.
* **Outbox deferred.** Durable same-transaction outbox + dispatcher is not shipped. Cross-slice
  integration events stay substrate-agnostic contracts with the **in-process RiverBooks bridge**
  (114 axis 3) first; when durable delivery is earned, the publish contracts do not change.
* **Identity (104-032) is the first product durable consumer** of this seam — sequenced after
  113, not folded into it. The dual-fixture store-contract test pattern (one suite against
  in-memory and EF `IPrincipalStore` implementations) is the reference persistence test pattern
  for port-backed stores. Identity uses **store-CAS** (not `IAggregateRoot` + golden Version
  auto-increment): `EfPrincipalStore` owns `EntityVersion.Next` / `ConcurrencyConflictException`
  / snapshot-on-get in parity with `InMemoryPrincipalStore`; mapping still pairs
  `.IsConcurrencyToken()` as a DB race belt. Schema `identity` tables `principals` /
  `credentials`. Tokens and ceremony challenge stores stay in-memory (ephemeral by design).
* **Teaching aggregate: `Profile`.** Already `IAggregateRoot` + private `Invariants`; mapped
  end-to-end with concurrency and Postgres integration tests (113-004). Not Principal (104-032)
  and not the credit ledger (Orleans later).

### Positive Consequences

* One clear "add your aggregate" path from domain through EF to tests
* Golden enforcement reusable across hosts and published Foundation packages
* Template still runs with EnsureCreated + Aspire-wired Postgres; no forced migration ceremony
* Identity persistence (104-032) has an unblocked seam and an explicit test pattern to dogfood
* Actor and outbox complexity stay opt-in / deferred instead of polluting the default story

### Negative Consequences

* EnsureCreated does not evolve schema — grown apps must adopt migrations deliberately
* Single DbContext + table schemas is a soft module boundary until a second context is earned
* Concurrent writers without `.IsConcurrencyToken()` get a false sense of safety if they only
  notice Version moving
* Durable cross-process integration delivery remains a gap until outbox (or equivalent) is
  earned
* Orleans production wiring remains follow-on work; 104-032 EF `IPrincipalStore` is implemented
  (store-CAS, dual-fixture tests)

## Pros and Cons of the Options

### A — Status quo drift

* Good, because no migration cost now
* Bad, because AppHost/flag mismatch and empty model teach nothing
* Bad, because identity restart-wipes stay unaddressed

### B — Dual relational engines

* Good, because some enterprises standardize on SQL Server
* Bad, because every aggregate mapping and test path doubles
* Bad, because the template already chose Postgres plumbing as the flag

### C — Event sourcing first

* Good, because audit/replay and some actor journals align naturally
* Bad, because paradigm shift without team depth or a forcing product need
* Bad, because day-one template learning cost is high

### D — Actors everywhere

* Good, because single-writer serialization is free for every aggregate
* Bad, because cluster ops and serialization discipline dominate the template
* Bad, because most aggregates do not earn that cost (spike + soft-gate)

### E — Postgres EF golden path (chosen)

* Good, because matches 114 axis 5 and the existing golden aggregate pattern
* Good, because Foundation packaging (051) can ship the seam once
* Bad, because outbox and high-contention examples remain future tasks

## Links

* Parent task: kanban 113 (`113-golden-persistence-implementation-postgres-first-aspire-wired-with-actor-model-evaluation`)
* Children: 113-001 (AppHost Postgres + SQL Server removal), 113-002 (dual actor spike → Orleans
  optional), 113-003 (`GoldenDbContext` + child-entity gap), 113-004 (Profile mapping + tests),
  113-005 (this ADR + HowToAddYourAggregate)
* 114 axis decisions: `kanban/done/114-…/axis-decisions.md` (axis 3 outbox deferral → 113; axis 5
  state-store EF + optional actors; axis 5 addendum Orleans)
* First product consumer: kanban 104-032 (EF `IPrincipalStore` behind postgres flag)
* How-to: [HowToAddYourAggregate.md](../../../how-to-guides/HowToAddYourAggregate.md)
* Domain overview: `source/container-apps/web/web-domain/aggregates/overview.md`
* Related: [ADR-0008](0008-feature-cohesive-folders-with-filename-grammar-layer-composition.md)
  (feature placement of `*-infrastructure.cs` configs), golden aggregate analyzers TWA0011/0012
  (AGENTS.md)

<!-- markdownlint-disable-file MD013 -->
