# How to add your aggregate

This guide walks a new **aggregate root** from domain model through EF Core mapping, host
registration, application use, and tests — the golden path every generated app inherits
([ADR-0009](../conceptual/architectural-decision-records/approved/0009-postgres-ef-golden-persistence-path.md)).

**Exemplar:** `Profile` under `source/container-apps/web/web-domain/aggregates/profile/` with
mapping in `source/container-apps/web/features/profile/`. Read those files alongside this guide.
Also read `web-domain/aggregates/overview.md` for the domain-side golden pattern (task 106).

Prerequisites: the `postgres` template flag is on (default). AppHost provisions Postgres for
`dev run`. Without the flag, generated apps drop the Postgres plumbing files and keep
non-durable paths (e.g. in-memory identity) until you re-enable it.

## 1. Domain: TypedId + Entity + IAggregateRoot + private Invariants

Place aggregate types under `web-domain/aggregates/<name>/` (or the matching feature-cohesive
domain files if your slice has already moved fully into `web/features/`).

1. **Typed id** — `[TypedId] readonly partial record struct` (never a raw `Guid` as the public
   identity type):

   ```csharp
   [TypedId]
   public readonly partial record struct OrderId;
   ```

   See `profile/profile-id.cs`.

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

Mirror `profile/profile.cs` and the checklist in `aggregates/overview.md`. Application code
never writes `Version` — it is store-owned.

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
| **Concurrency** | `Property(e => e.Version).IsConcurrencyToken().UsePropertyAccessMode(PropertyAccessMode.Property)` |

The Version line is the **host half** of the two-party concurrency contract. Without
`.IsConcurrencyToken()`, `GoldenDbContext` still increments Version on Modified roots, but the
UPDATE never compares the original value and concurrent overwrites succeed silently.

Profile reference: `features/profile/profile-entity-type-configuration-infrastructure.cs`
(schema/table `profiles`).

## 3. Registration: DbSet + ApplyConfigurationsFromAssembly

On the host context (`PostgresDbContext`):

1. Add `public DbSet<Order> Orders => Set<Order>();`
2. Keep `OnModelCreating` calling `base.OnModelCreating` (so the golden Version access-mode pin
   still runs) and `modelBuilder.ApplyConfigurationsFromAssembly(typeof(PostgresDbContext).Assembly)`.
3. Keep `ConfigureTypedIdConventions()` in `ConfigureConventions` if TypedIds are in the model.

Feature `*-infrastructure.cs` files compile into the web-infrastructure assembly — you do **not**
hand-register each `IEntityTypeConfiguration` type next to the context.

```csharp
public sealed partial class PostgresDbContext : GoldenDbContext
{
  public DbSet<Profile> Profiles => Set<Profile>();
  // public DbSet<Order> Orders => Set<Order>();

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
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

You do **not** call `DomainInvariantsGuard` yourself. `GoldenDbContext.SaveChanges(Async)`:

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
| Golden hook | Version bump, child→root, missing Version | `foundation-infrastructure-tests` (`GoldenDbContext` harness) |
| Postgres integration | EnsureCreated, round-trip, concurrent update | `web-infrastructure-tests` style (Testcontainers or connection string; soft-skip when unavailable) |

Live Postgres tests should use an **ephemeral** database (Testcontainers without a shared data
volume, or a dedicated connection string). Do not reuse AppHost `WithDataVolume` state across
test runs (WAL/catalog drift lesson from 113-001).

Identity's dual-fixture **store-contract** suite (one test body, in-memory + EF fixtures) is the
reference pattern once 104-032 lands for port-backed stores. Direct DbContext aggregates (Profile)
can stay on mapping + Postgres tests as above.

## 6. When to use a store port vs direct DbContext

| Use | When |
|-----|------|
| **Direct `PostgresDbContext` / `DbSet<T>`** | Product aggregate owned by this host; no multi-backend swap; handlers can depend on EF |
| **Port (`IPrincipalStore`, etc.)** | Library or multi-host seam; in-memory default + EF behind `postgres`; concurrency and snapshot semantics must be implementation-agnostic |

Identity keeps domain entities and port contracts in `TimeWarp.Identity` and leaves EF mapping
to the host infrastructure layer so the library stays persistence-free. Application handlers
depend on `IPrincipalStore`, not the DbContext. Profile is the simpler teaching path: host
DbContext is enough until a second implementation appears.

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

## 8. EnsureCreated vs Migrate

| Mode | Use |
|------|-----|
| **`Database.EnsureCreated`** | Template default (`PostgresDbContextStartupHostedService`). Fresh DB, zero migration ceremony, great for demos and early development |
| **`Database.Migrate`** | Grown apps that need schema evolution, team deploys, or non-destructive updates |

`EnsureCreated` does **not** apply migrations and will not upgrade an existing model. When you
outgrow it: add EF migrations for the host model, switch the startup path (or ops pipeline) to
`Migrate`, and treat production schema as migration-owned. Schema-per-slice table placement
stays the same either way.

## Checklist (copy into the PR)

- [ ] TypedId + `Entity<TId>` + `IAggregateRoot` + private `Invariants`
- [ ] `IEntityTypeConfiguration` with schema, TypedId conversion, **`.IsConcurrencyToken()`**
- [ ] `DbSet<>` + configurations discovered from assembly; `base.OnModelCreating` retained
- [ ] Handler path: load → mutate → `SaveChangesAsync` (no hand-rolled invariant calls)
- [ ] Domain unit tests + mapping and/or Postgres tests
- [ ] Design/`Purpose` regions honest if you touched existing files (TWA0004)

## Related

- [ADR-0009 — Postgres + EF golden persistence path](../conceptual/architectural-decision-records/approved/0009-postgres-ef-golden-persistence-path.md)
- `source/container-apps/web/web-domain/aggregates/overview.md`
- `source/foundation/foundation-infrastructure/persistence/golden-db-context.cs`
- Feature placement skill: `skills/tw-feature-placement/SKILL.md`
- Identity EF consumer (follow-on): kanban 104-032

<!-- markdownlint-disable-file MD013 -->
