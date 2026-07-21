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
2. **Actor technology, if adopted**: Akka.NET (Steve's named candidate) vs Microsoft Orleans
   (virtual actors, first-class Aspire integration) vs Dapr actors. Weigh: Aspire wiring,
   persistence integration with the golden aggregate seam, licensing (Akka.NET moved to
   BSL/commercial licensing for larger orgs — check current terms before committing the
   template to it), template-consumer ergonomics.
3. **Persistence shape**: state-store EF (current seam) vs event sourcing (e.g. Marten on
   Postgres — would also answer the actor-journal question) vs hybrid (EF state store now,
   evented aggregates where actors land). Interacts hard with decision 1.
4. **Identity store placement**: does [[104-032]] (EF persistence for principals/credentials/
   agent keys) become the first consumer of the golden implementation, and does its
   store-contract test suite become the template's reference persistence test pattern?
5. **Seam packaging**: does the golden store implementation live in web-infrastructure only, or
   graduate into `TimeWarp.Foundation.*` packages (051 axis) so api-server/grpc-server share it?

## Checklist

- [ ] Wire Postgres into the AppHost: `Aspire.Hosting.PostgreSQL` in CPM + app-host csproj
      (guarded consistent with existing flag packaging), `#if postgres` `AddPostgres` +
      `AddDatabase` + `WithReference` into web-server (and api-server?), pgAdmin/pgweb optional;
      resource name via `ServiceNames` (TWA0007). `dev run` must come up with a live postgres.
- [ ] Connection flow: Aspire-injected connection string reaches `PostgresDbModule` in
      Development; document the non-Aspire path (compose/K8s from 070) too.
- [ ] Remove SQL Server: delete `sql-db-context.cs` placeholder, drop the CPM package, scrub the
      commented AddDbContextCheck line in web-server Program; note in docs that Postgres is the
      relational axis.
- [ ] Run the RFC (rfc/ subfolder, tw-rfc-ballot) over the five open decisions; fold resolutions
      back into THIS task's checklist (no separate apply-task).
- [ ] Implement the golden persistence implementation per resolutions (scope will be refined by
      the RFC fold-in): reference aggregate persisted end-to-end (entity config, migrations or
      schema-creation story, concurrency-token mapping fulfilling the sql-db-context two-party
      contract, integration tests against real Postgres via Aspire).
- [ ] Resolve the known child-entity gap documented in postgres-db-context Design (Unchanged
      root when only child entries change) as part of the golden model — first real multi-entity
      aggregate makes it non-latent.
- [ ] Fold identity stores in or explicitly sequence [[104-032]] after, per RFC decision 4.
- [ ] Documentation: ADR for the persistence decisions; developer how-to for "add your aggregate"
      as the golden path walkthrough.
- [ ] `dev build` 0/0 and full `dev test` green with postgres flag on AND off (template both ways).

## Notes

- Akka.NET licensing check is a hard gate before recommending it in a template others generate
  from — the template's consumers inherit the license posture.
- Orleans note for the RFC: `Aspire.Hosting.Orleans` exists first-party; actor-per-aggregate
  with Orleans grains + Postgres grain storage is the lowest-ops actor option in this stack.
- The enumeration-hardening task [[105]] and foundation packaging (051) touch the same
  foundation layers — coordinate, don't collide.
- Session: created 2026-07-21 out of the 112/104-032 persistence discussion.

## Session

- Created: 2026-07-21
