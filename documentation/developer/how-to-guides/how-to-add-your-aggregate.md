# How to add your aggregate

This guide walks a new **aggregate root** from domain model through EF Core mapping, host
registration, application use, and tests — the golden path every generated app inherits
([ADR-0009](../conceptual/architectural-decision-records/approved/0009-postgres-ef-golden-persistence-path.md)).

**Exemplar:** `Profile` under `source/container-apps/web/features/profile/` (domain files
`profile-domain.cs` / `profile-id-domain.cs`, mapping in
`profile-entity-type-configuration-infrastructure.cs`). Read those files alongside this guide.
The pattern SSOT for the domain-side golden pattern is the `tw-aggregate-pattern` skill
(`skills/tw-aggregate-pattern/SKILL.md`) — this guide is the human end-to-end walkthrough that
defers to it.

Prerequisites: the `postgres` template flag is on (default). AppHost provisions Postgres for
`dev run`. Without the flag, generated apps drop the Postgres plumbing files and keep
non-durable paths (e.g. in-memory identity) until you re-enable it.

## 1. Domain: TypedId + Entity + IAggregateRoot + private Invariants

Place aggregate types under `web/features/<slice>/` using the feature-cohesive domain filename
grammar (`<name>-domain.cs` for the aggregate, `<name>-id-domain.cs` for its typed id).

1. **Typed id** — `[TypedId] readonly partial record struct` (never a raw `Guid` as the public
   identity type):

   ```csharp
   [TypedId]
   public readonly partial record struct OrderId;
   ```

   See `features/profile/profile-id-domain.cs`.

2. **Aggregate type** — inherit `Entity<TId>`, implement `IAggregateRoot`:

   - Private constructor + static `Create(...)` with guard clauses (fail-closed construction)
   - Named mutations, no public setters
   - Nested `private sealed class Invariants : AbstractValidator<T>` (TWA0011 requires it;
     TWA0012 requires `private` so `AddValidatorsFromAssemblyContaining` never treats it as a
     request validator)

   ```csharp
   public sealed class Order : Entity<OrderId>, IAggregateRoot
   {
     private Order(OrderId id, /* … */) : base(id) { /* … */ }

     public static Order Create(/* … */) { /* guards + new Order(...) */ }

     public void SomeMutation(/* … */) { /* intention-revealing */ }

     private sealed class Invariants : AbstractValidator<Order>
     {
       public Invariants()
       {
         // Full rule set DomainInvariantsGuard will run on SaveChanges
       }
     }
   }
   ```

Mirror `features/profile/profile-domain.cs` and the pattern in the `tw-aggregate-pattern` skill.
Application code never writes `Version` — it is store-owned.

## 2. Infrastructure: IEntityTypeConfiguration under features/

Add a feature file that the infrastructure layer globs by suffix:

`source/container-apps/web/features/<slice>/<name>-entity-type-configuration-infrastructure.cs`

(or another registered `*-infrastructure.cs` name — see the feature-filename grammar).

Configure at least:

| Concern | What to do |
|---------|------------|
| Table + schema | `ToTable("orders", "orders")` — **schema-per-slice** on the single host context |
| Key | `HasKey(e => e.Id)` |
| TypedId | `HasConversion(id => id.Value, v => OrderId.From(v))` (host also runs `ConfigureTypedIdConventions`) |
| Required columns | max lengths aligned with domain constants where shared |
| **Concurrency** | nothing to do — `AggregateDbContext` configures it for you (see below) |

**Version concurrency is free — you get it with AggregateDbContext.** `AggregateDbContext`'s sealed
`ConfigureConventions` always registers a model-finalizing convention
(`AggregateVersionConvention`) that configures `.IsConcurrencyToken()` +
`PropertyAccessMode.Property` on `Version` for every mapped `IAggregateRoot`, after all your
model configuration has run. Do **not** call `.IsConcurrencyToken()` yourself — it is redundant
and there is nothing to forget: this is a one-party contract (kanban 121; ADR-0009 originally
shipped it two-party).

Profile reference: `features/profile/profile-entity-type-configuration-infrastructure.cs`
(schema/table `profiles`).

## 3. Registration: DbSet + ApplyConfigurationsFromAssembly

On the host context (`PostgresDbContext`):

1. Add `public DbSet<Order> Orders => Set<Order>();`
2. Keep `OnModelCreating` calling `base.OnModelCreating` and
   `modelBuilder.ApplyConfigurationsFromAssembly(typeof(PostgresDbContext).Assembly)` — the aggregate
   Version convention no longer depends on this ordering (it runs at model-finalizing time), but
   `base.OnModelCreating` is still good EF hygiene.
3. Keep `ConfigureTypedIdConventions()` in `OnConfigureConventions` if TypedIds are in the model.
   `ConfigureConventions` itself is sealed on `AggregateDbContext` — override `OnConfigureConventions`
   instead; the aggregate Version convention is always registered first regardless.

Feature `*-infrastructure.cs` files compile into the web-infrastructure assembly — you do **not**
hand-register each `IEntityTypeConfiguration` type next to the context.

```csharp
public sealed partial class PostgresDbContext : AggregateDbContext
{
  public DbSet<Profile> Profiles => Set<Profile>();
  // public DbSet<Order> Orders => Set<Order>();

  protected override void OnConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
    configurationBuilder.ConfigureTypedIdConventions();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(PostgresDbContext).Assembly);
  }
}
```

## 4. Application: load → mutate → SaveChanges

In a mediator handler (or other application service):

1. Resolve `PostgresDbContext` (or a store port that uses it — see §6).
2. Load the aggregate (`FindAsync` / query).
3. Call domain mutations (`Rename`, …).
4. `await db.SaveChangesAsync(ct)`.

You do **not** call `DomainInvariantsGuard` yourself. `AggregateDbContext.SaveChanges(Async)`:

- Resolves dirty children to their aggregate root
- Runs nested `Invariants` for every Added/Modified root
- Increments `Version` on Modified roots

Invalid state fails closed before SQL runs. Concurrent writers with a mapped concurrency token
surface as `DbUpdateConcurrencyException` (map to a product conflict type at the edge if needed).

## 5. Tests

| Layer | What | Where |
|-------|------|--------|
| Domain unit | Create/guards/mutations | `tests/…/web-domain-tests/` (see `profile-tests.cs`) |
| Model mapping | Schema, TypedId, `IsConcurrencyToken` | `web-infrastructure-tests` model tests (no live DB) |
| Aggregate SaveChanges hook | Version bump, child→root, missing Version | `foundation-infrastructure-tests` (`AggregateDbContext` harness) |
| Postgres integration | `Database.Migrate`, round-trip, concurrent update | `web-infrastructure-tests` style (Testcontainers or connection string; soft-skip when unavailable) |

Live Postgres tests should use an **ephemeral** database (Testcontainers without a shared data
volume, or a dedicated connection string). Do not reuse AppHost `WithDataVolume` state across
test runs (WAL/catalog drift lesson from 113-001).

Identity's dual-fixture **store-contract** suite is the reference pattern for port-backed
stores: shared abstract cases in `tests/libraries/timewarp-identity-tests/principal-store-contract-tests.cs`,
in-memory fixture in the same project, EF fixture in `web-infrastructure-tests` (Testcontainers;
CI fail-closed). Direct DbContext aggregates (Profile) stay on mapping + Postgres tests as above.

## 6. When to use a store port vs direct DbContext

| Use | When |
|-----|------|
| **Direct `PostgresDbContext` / `DbSet<T>`** | Product aggregate owned by this host; no multi-backend swap; handlers can depend on EF |
| **Port (`IPrincipalStore`, etc.)** | Library or multi-host seam; in-memory default + EF behind `postgres`; concurrency and snapshot semantics must be implementation-agnostic |

Identity keeps domain entities and port contracts in `TimeWarp.Identity` and leaves EF mapping
to the host infrastructure layer so the library stays persistence-free (`EfPrincipalStore` +
`features/identity/*-entity-type-configuration-infrastructure.cs`). Application handlers
depend on `IPrincipalStore`, not the DbContext. Profile is the simpler teaching path: host
DbContext is enough until a second implementation appears.

**Store-CAS vs aggregate Version (identity):** Principal/Credential deliberately do **not** implement
`IAggregateRoot`. The store owns optimistic concurrency (`EntityVersion.Next` +
`ConcurrencyConflictException` + snapshot-on-get). Host mapping still sets
`.IsConcurrencyToken()` as a DB race belt, but `AggregateDbContext` does not auto-increment Version
for these types — that avoids a double-bump if the store and the aggregate SaveChanges hook both advanced
Version. Use the Profile/`IAggregateRoot` path when the host DbContext is the only writer; use
store-CAS when a port must honor identical semantics across in-memory and EF backends.

Port rules that both implementations must honor (see `IPrincipalStore` Design region):
snapshot-on-get, `EntityVersion.Next` on successful update, distinct absence vs concurrency
failures.

## 7. When to earn Orleans

Default: **do not** introduce an actor framework for a new aggregate.

Earn **Orleans** (grain-per-entity-ID over the same EF aggregate) when you have evidence of:

- High-contention **single-writer** semantics on one entity id (textbook: credit ledger per
  principal)
- Benefit from in-memory hot state + serialized turns that plain EF optimistic concurrency
  cannot absorb without thrashing

Orleans is optional and not wired as the template default. **Akka.NET** is reserved for the
device-fleet / supervision-and-streams shape (task 118), not generic entity hosting. See ADR-0009
and the 113-002 spike notes.

## 8. Schema evolution (EF migrations)

Schema for `PostgresDbContext` is **migration-owned only** (ADR-0009 / task 147-007):

| Path | Who runs it |
|------|-------------|
| **Local / Aspire** | AppHost `AddEFMigrations` + `RunDatabaseUpdateOnStart` (no wait edge on web-server — task 155); re-run on demand via the `ef-database-update` dashboard command |
| **Publish / deploy** | `PublishAsMigrationScript` / `PublishAsMigrationBundle` artifacts under `efmigrations/` |
| **Tests** | Ephemeral DBs call `Database.Migrate()` / `MigrateAsync()` (never `EnsureCreated`) |

### Change model → add migration → run

After editing the model or an `IEntityTypeConfiguration`:

```bash
# From repo root (once per machine / after clone):
dotnet tool restore

dotnet ef migrations add <NameYourChange> \
  --project source/container-apps/web/projects/web-infrastructure/web-infrastructure.csproj \
  --startup-project source/container-apps/web/projects/web-server/web-server.csproj \
  --context PostgresDbContext \
  --output-dir ../../platform/postgres/migrations \
  --namespace TimeWarp.Architecture.Persistence.Migrations
```

- Migrations home: `source/container-apps/web/platform/postgres/migrations/` (do not kebab-rename
  EF scaffold files).
- Design-time factory: `postgres-db-context-design-time-factory-infrastructure.cs` resolves
  `ConnectionStrings:postgres-db` / env; uses a dummy connection only for offline scaffolding.
- Then `dev run`: AppHost resource `web-migrations` applies pending migrations on AppHost start
  (`RunDatabaseUpdateOnStart`, idempotent). There is no wait edge from web-server (task 155 —
  both `WaitFor` and `WaitForCompletion` broke restart/testing behavior), so on a fresh volume
  web-server can briefly start before the migration finishes; re-run on demand with the
  `ef-database-update` dashboard command on the `web-migrations` resource if needed.
- **Cutover wipe:** if a dogfood volume was created under the old EnsureCreated path (no
  `__EFMigrationsHistory`), drop the Aspire Postgres volume once, then restart.

Schema-per-slice table placement (`ToTable` schema) stays the same.

## Checklist (copy into the PR)

- [ ] TypedId + `Entity<TId>` + `IAggregateRoot` + private `Invariants`
- [ ] `IEntityTypeConfiguration` with schema, TypedId conversion (no `.IsConcurrencyToken()` needed
      — `AggregateDbContext` supplies it for every `IAggregateRoot`)
- [ ] `DbSet<>` + configurations discovered from assembly; `base.OnModelCreating` retained
- [ ] Handler path: load → mutate → `SaveChangesAsync` (no hand-rolled invariant calls)
- [ ] Domain unit tests + mapping and/or Postgres tests
- [ ] Design/`Purpose` regions honest if you touched existing files (TWA0004)

## Related

- [ADR-0009 — Postgres + EF golden persistence path](../conceptual/architectural-decision-records/approved/0009-postgres-ef-golden-persistence-path.md)
- Aggregate pattern skill (SSOT): `skills/tw-aggregate-pattern/SKILL.md`
- `source/foundation/foundation-infrastructure/persistence/aggregate-db-context.cs`
- Feature placement skill: `skills/tw-feature-placement/SKILL.md`
- Identity EF consumer (done): kanban 104-032 — `EfPrincipalStore`, dual-fixture contract tests

<!-- markdownlint-disable-file MD013 -->
