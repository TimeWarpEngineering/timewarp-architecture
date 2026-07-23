# Golden persistence implementation — Postgres-first, Aspire-wired, with actor-model evaluation

## Description

Rethink the template's persistence story end-to-end and ship the **golden implementation** —
the pattern every generated app inherits. Triggered 2026-07-21: the task-112 public share made
the in-memory identity cliff externally visible ([[104-032]]), and recon showed the persistence
layer is half-built.

**State of the world (recon 2026-07-21):**

- `PostgresDbContext` (web-infrastructure) exists, deliberately entity-free, and already
  implements the golden-aggregate enforcement seam from 106: `DomainInvariantsGuard.EnsureValid`
  + `EntityVersion.Next` increment per Added/Modified `IAggregateRoot` on SaveChanges, with a
  documented two-party concurrency contract (host must map `.IsConcurrencyToken()`).
- Postgres plumbing ships behind the `postgres` flag: `PostgresDbModule`, environment check,
  health check, schema-creation hosted service.
- **The AppHost provisions NO postgres resource** — no `AddPostgres`, no `Aspire.Hosting.PostgreSQL`
  package, no `#if postgres` block in program.cs. The flag ships plumbing with nothing to connect
  to in `dev run`.
- SQL Server is already dead: `SqlDbContext` is a documented unregistered placeholder;
  `Microsoft.EntityFrameworkCore.SqlServer` sits unused in CPM.
- No actor framework anywhere (no Akka.NET, Orleans, Dapr actors).
- Identity stores are in-memory singletons ([[104-032]] tracks their persistence).

## Settled decisions (Steve, 2026-07-21)

- **Postgres over SQL Server.** Remove the `SqlDbContext` placeholder and the SqlServer CPM
  package; `postgres` stays the single relational axis. No sqlserver template flag.
- FastEndpoints-generated endpoints are the server surface the persistence seam serves (109/110
  landed that).
- The golden aggregate pattern (106: `IAggregateRoot`, private `Invariants`, TWA0011/0012,
  store-owned `Version`) is the domain-side contract the store must enforce — that part is not
  up for re-litigation.

## Open decisions — RFC material (`rfc/` subfolder, tw-rfc-ballot)

1. **Actor model adoption scope**: none (plain EF repositories) / opt-in for high-contention or
   long-lived aggregates / actors as THE aggregate host everywhere. "Consider does not mean have
   to use it" — the RFC must include an honest no-actors baseline and articulate what concrete
   problem actors solve for a *template* (single-writer serialization per aggregate, in-memory
   hot state, at the cost of cluster ops + serialization discipline + a much steeper template
   learning curve).
2. **Actor technology, if adopted**: Akka.NET (Steve's named candidate; **Apache 2.0** — the
   BSL change was JVM Akka/Lightbend only, Akka.NET stayed Apache per Petabridge, ports frozen
   at upstream 2.6.20) vs Microsoft Orleans (MIT, virtual actors, first-class Aspire
   integration) vs Dapr actors. Licensing is NOT a differentiator — corrected 2026-07-22 after
   an earlier false "Akka.NET went BSL" claim biased the framing. Weigh instead: Aspire wiring,
   persistence integration with the golden aggregate seam, template-consumer approachability,
   upstream-port freeze implications, support/community (Steve knows the Akka.NET author).
3. **Persistence shape**: state-store EF (current seam) vs event sourcing (e.g. Marten on
   Postgres — would also answer the actor-journal question) vs hybrid (EF state store now,
   evented aggregates where actors land). Interacts hard with decision 1.
3b. **Outbox for integration events** (from 114 axis 3, Steve 2026-07-21): the template's
   cross-slice channel is the RiverBooks bridge (in-process integration events); THIS task
   decides whether/when delivery gets the FSH outbox treatment (same-transaction write +
   hosted dispatcher + dead-letter), and what the no-postgres fallback is (in-memory dispatch).
   The publish seam is contract-first/substrate-agnostic by design — actor adoption (decision 1)
   would swap the substrate (mediator vs Akka event bus vs Orleans streams), not the contracts.
4. **Identity store placement**: does [[104-032]] (EF persistence for principals/credentials/
   agent keys) become the first consumer of the golden implementation, and does its
   store-contract test suite become the template's reference persistence test pattern?
5. **Seam packaging**: does the golden store implementation live in web-infrastructure only, or
   graduate into `TimeWarp.Foundation.*` packages (051 axis) so api-server/grpc-server share it?

## Checklist

- [x] Mechanical track (RFC-independent) → [[113-001-wire-postgres-into-aspire-apphost-and-remove-sql-server-remnants]]
- [x] Actor decisions 1–2 (scope + tech) → [[113-002-dual-actor-spike-same-aggregate-on-akkanet-and-orleans-over-ef-state-store]]
      + Steve 2026-07-23 (Orleans optional; EF default; Akka.NET reserved for 118 fleet)
- [x] Persistence shape (decision 3 / 114 axis 5) — state-store EF; no event sourcing;
      schema-per-slice on golden seam
- [ ] Decision 3b (outbox): DEFER durable outbox; document substrate-agnostic publish intent only
- [ ] Decision 4: SEQUENCE [[104-032]] as first product consumer + dual-fixture store-contract
      pattern (not folded into 113)
- [ ] Decision 5: extract GoldenDbContext to Foundation.Infrastructure; host PostgresDbContext
      thins out → [[113-003-extract-goldendbcontext-to-foundation-and-fix-child-entity-savechanges-gap]]
- [ ] Child-entity gap fixed in golden SaveChanges path + automated test → 113-003
- [ ] Reference aggregate (Profile) mapped end-to-end: config, IsConcurrencyToken, EnsureCreated,
      Postgres integration tests → [[113-004-map-profile-on-postgresdbcontext-with-concurrency-and-postgres-integration-tests]]
- [ ] ADR + HowToAddYourAggregate + dual-flag verification → [[113-005-adr-howtoaddyouraggregate-and-113-closeout-verification]]
- [ ] Parent Notes updated; 104-032 unblocked explicitly
- [ ] `dev build` 0/0 and `dev test` green with postgres flag on AND off

## Notes

- Akka.NET licensing RESOLVED (2026-07-22): Apache 2.0 — the BSL move was JVM Akka only.
- Orleans note: `Aspire.Hosting.Orleans` exists first-party; lowest-ops actor option when earned.
- The enumeration-hardening task [[105]] and foundation packaging (051) touch the same
  foundation layers — coordinate, don't collide.
- Session: created 2026-07-21 out of the 112/104-032 persistence discussion.

### Actor gate resolved (Steve, 2026-07-23 — closes RFC open decisions 1 and 2)

- **Scope**: actors remain optional; EF golden path is the default. Aggregates that earn actor
  hosting (high-contention single-writer, e.g. the credit ledger) use **Orleans**
  (grain-per-entity-ID over direct EF — IPersistentState rejected per spike).
- **Technology**: Orleans for entity-ID-keyed aggregate hosting. Akka.NET reserved as the
  candidate for 118's device-fleet layer (supervision/streams territory).
- Evidence: 113-002 spike + `spike-actor-comparison.md`; branch `spike/113-002-dual-actor`.

### Implementation plan (Phase 2, 2026-07-23) — remaining work

**Principle:** smallest EF golden path that (a) teaches “add your aggregate,” (b) unblocks
104-032, (c) does not re-open settled axes or pull Orleans / outbox / ledger into this task.

**No formal `tw-rfc-ballot`** for remaining 3b/4/5 — same as 114 (in-chat / soft-gate leans).

| # | Decision | Lean (Steve soft-gate; silence = accept) |
|---|----------|------------------------------------------|
| 3b | Outbox | **DEFER** full outbox stack; no table/dispatcher now; document substrate-agnostic publish |
| 4 | Identity first consumer | **SEQUENCE 104-032 after 113** — 113 ships seam + teaching Profile + pattern; 104-032 dogfoods |
| 5 | Seam packaging | **GoldenDbContext abstract base in Foundation.Infrastructure** + EF package; host thins out |

**Reference aggregate:** `Profile` (already `IAggregateRoot` + private Invariants). Not credit ledger
(Orleans later). Not Principal (104-032). Schema story: keep EnsureCreated; document Migrate for
grown apps. Schema-per-slice via table schemas on single PostgresDbContext until a second module
earns isolation.

**Child-entity gap:** resolve ownership/parent navigations in SaveChanges so child-only mutations
mark root Modified, run invariants, bump Version. Test with infrastructure test model.

**Children:**

1. [[113-003-extract-goldendbcontext-to-foundation-and-fix-child-entity-savechanges-gap]]
2. [[113-004-map-profile-on-postgresdbcontext-with-concurrency-and-postgres-integration-tests]]
3. [[113-005-adr-howtoaddyouraggregate-and-113-closeout-verification]]

**Out of 113:** Orleans production wiring; Akka packages; FSH outbox; full 104-032; credit ledger;
event sourcing; multi-DbContext-per-slice scaffolding; migrations-as-only-path.

**Verification:** `dev build` 0/0; `dev test` postgres on AND off; Aspire/Docker for real Postgres
concurrency tests; ephemeral volumes (113-001 WAL lesson).

## Session

- Created: 2026-07-21
- Reopened + plan: 2026-07-23 (orchestrator; closed too early after 113-001/002)
