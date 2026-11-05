---
name: tw-aggregate-pattern
description: "**TIMEWARP SKILL** — the golden aggregate-root pattern: typed id, `Entity<TId>` base, fail-closed `Create`, named mutations with no public setters, a private nested `Invariants` validator, and save-time enforcement via `DomainInvariantsGuard`/`AggregateDbContext`. Invoke before adding or reviewing an `IAggregateRoot`, or when TWA0011/TWA0012 fire. WHEN: add an aggregate, IAggregateRoot, aggregate root, TWA0011, TWA0012, Invariants validator, typed id."
when-to-use: aggregate root, IAggregateRoot, Entity<TId>, typed id, TypedId, Invariants validator, DomainInvariantsGuard, AggregateDbContext, TWA0011, TWA0012, contract agreement, TWA0039, TWA0040, TWA0041, TWA0042, TWA0043, NotAggregate, Computed, fail-closed construction, named mutation, concurrency token, Version
---

# Aggregate pattern (TWA0011/0012)

An aggregate is a domain entity that is the consistency boundary for a set of invariants.
Every aggregate root in this repository follows the same golden pattern. This skill is the
pattern SSOT, including the persistence walkthrough (EF mapping → host registration → SaveChanges → tests).

## Detection — when to invoke

| Signal | How to find it |
|--------|----------------|
| Adding a new `IAggregateRoot` | any domain type that owns its own consistency boundary |
| `TWA0011` / `TWA0012` diagnostic | analyzer output names the aggregate type |
| "Where does the invariants check run?" | save path, not construction |
| Reviewing a `Create`/mutation method for a domain type | fail-closed construction check |

## The golden pattern

- **Typed id.** The aggregate's id is a `[TypedId] readonly partial record struct` (e.g.
  `ProfileId`), never a raw `Guid`. See `web/features/profile/profile-id-domain.cs`.
- **`Entity<TId>` base.** The aggregate inherits `TimeWarp.Foundation.Entities.Entity<TId>`
  (get-only typed `Id`, identity-based equality, a store-owned `Version` concurrency token) and
  implements the marker interface `IAggregateRoot`.
- **Fail-closed construction.** A private constructor plus a static `Create(...)` factory with
  guard clauses — an aggregate can never exist half-initialized or with an obviously-invalid
  required field.
- **Named mutations, no public setters.** State changes are intention-revealing methods
  (`Rename`, `SetLanguage`, ...), never `{ get; set; }`.
- **Nested `Invariants` validator.** A `private sealed class Invariants : AbstractValidator<T>`
  declares the aggregate's full rule set. It stays `private` so contract-validator
  auto-registration (`AddValidatorsFromAssemblyContaining`) never picks it up as a request
  validator.
- **Save-time enforcement.** `DomainInvariantsGuard` discovers and runs the nested `Invariants`
  validator for every changed `IAggregateRoot` from `AggregateDbContext.SaveChanges(Async)`
  before the save proceeds. Host contexts (e.g. `PostgresDbContext`) inherit that base; they do
  not reimplement the hook. Guard clauses in `Create`/mutations and the save-time validator are
  **complementary, not redundant**: the former makes invalid states hard to construct, the
  latter makes them impossible to persist regardless of which code path produced them.
  Child-only mutations resolve to the owning root so invariants and `Version` still run.

## Placement

An aggregate's domain type is `<name>-domain.cs` in its owning slice, and its typed id is
`<name>-id-domain.cs` alongside it — both follow the `<name>[-<function>]-<layer>.cs` filename
grammar for the `domain` layer. See `tw-feature-placement` for the full grammar, registry, and
use-case-folder rules; this skill covers the aggregate's internal shape, not where the file
lives.

## Enforcement map

| Rule | Requires | Why |
|------|----------|-----|
| **TWA0011** | An `IAggregateRoot` must declare a nested `Invariants : AbstractValidator<T>` | Fail-closed: no validator means `DomainInvariantsGuard` cannot check the aggregate at save time |
| **TWA0012** | That nested `Invariants` must be `private` | Keeps it out of `AddValidatorsFromAssemblyContaining` auto-registration — it is a save-time domain check, not a request validator |
| **TWA0039–TWA0043** | The root agrees with its operation group's contracts (below) | Contracts are UI-first and the domain is domain-first; the build proves the root satisfies what the contracts ask for |

### Contract agreement (TWA0039–TWA0043)

The domain is hand-written and never generated from contracts. `ContractAgreementAnalyzer` runs in
the compilation that sees both `web-contracts` and `web-domain` (web-application) and checks the
root against the `[Operation]` group it serves. **Link:** the root's slice namespace is its
namespace without a trailing `.Domain` (`Features.Admin.Roles.Domain` → `Features.Admin.Roles`);
the root satisfies every group with an operation in that namespace, and then every operation in
the group is checked wherever it lives. Nothing is declared on the entity. **Names match exactly;
drift is an error, not a mapping.** The one name rule is the key: a contract `RoleId` maps to the
root's `Id`. Compatible types: the same type and nullability; a value wrapper and what it wraps
(`RoleId` ↔ `Guid` through `Value`, `PermissionGrant` ↔ `string` through its one property); two
collections with compatible elements; two enums with the same member names (the wire enum that
mirrors a domain enum, because contracts must not reference domain; values are not compared); and
`T?` against `T?` of compatible `T`. On the write side (details members, `Set` elements), `Guid?`
against `Guid` is still drift. A query response may widen the root's `T` to `T?`: the handler may
null a projected value for one audience (`FeedbackActivityEntry.AuthorPrincipalId` on the filer's
copy), and the member is still checked, not `[Computed]`. `T` against a root `T?` is drift on
both sides.

Query responses are walked past the top level. A member and its root member that are both classes
(or both collections of classes) are compared member by member. For example,
`FeedbackActivityEntry` projects from the owned `FeedbackActivity` child, and the message names the
child. A non-list response wraps its projection when its only non-`[Computed]` member has no root
member of its name and is a contract class (or a list of one). `GetFeedbackForAdmin.Response.Item`
(`FeedbackAdminView`) and `ListMyFeedback.Response.Items` are checked against the root one level
down, the way a `ListResponse<T>` is checked through `T`. A response that projects any other member
has no wrapper, so an unmatched class member there is reported as missing.

| Id | Severity | Requires |
|----|----------|----------|
| TWA0039 | error | Each member of an `I*Details` interface a command implements exists on the root with a compatible type |
| TWA0040 | error | A `Set<Name>` command with one list member has a public `Set<Name>(collection)` mutation (the root's own name drops out: `SetRolePermissions` → `Role.SetPermissions`) |
| TWA0041 | error | Each query-response member (a `ListResponse<T>` is checked through `T`) maps to a root member; a handler-derived value is `[Computed]` on the member or the response |
| TWA0042 | warning | Any other command says why with `[NotAggregate("reason")]`: a delete by route id; an operation grouped here for hosts that stores elsewhere; or a named mutation no check covers (a status transition, a thread append), with the reason naming the mutation |
| TWA0043 | warning | A group whose operations sit in two roots' slice namespaces is ambiguous; give each root its own group |

When a check fires, fix the side that drifted: rename the contract member or the domain member so
both say the same thing, or add the missing mutation. `[Computed]` and `[NotAggregate]` live in
`TimeWarp.Architecture.Attributes` and state a fact about the contract; they are not a way to
silence drift.

## Exemplar

`web/features/profile/profile-domain.cs` + `profile-id-domain.cs` — read both before adding a
new aggregate. Its EF mapping (`profile-entity-type-configuration-infrastructure.cs` —
table/schema `profiles`, TypedId key conversion) is applied by `PostgresDbContext` via
`ApplyConfigurationsFromAssembly`. `Version`'s `.IsConcurrencyToken()`
is supplied for free by the `AggregateDbContext` `Version` convention — an aggregate's own
mapping does not declare it.

`web/features/admin/roles/role-domain.cs` is the exemplar for owned child collections:

- Value-record children (`PermissionGrant`, `AgentScopeGrant`) sit over private backing lists.
  The mapping is `OwnsMany` with field access on those lists and keys made of the owner id plus
  the value, so there is no surrogate id.
- Full-replace setters keep a child instance that is already present, so EF writes only the
  difference.
- A catalog the domain cannot reference (contracts, other libraries) is copied into the domain
  (`role-catalog-domain.cs`). A test that sees both assemblies fails when the copy drifts.
- A rule that must answer with a status code (protected core, 409) stays in the application
  layer. The domain only throws.

## Persistence golden path (Postgres + EF)

The template's default durable path is **Postgres-only state-store EF**. No SQL Server dual
story, no event sourcing as the default, no in-app `EnsureCreated`. Hosts inherit
`AggregateDbContext` so SaveChanges enforcement is not sealed inside one product context.
Npgsql stays host-only.

- **Schema-per-slice** on a single `PostgresDbContext` (`ToTable("orders", "orders")`). A second
  DbContext per module is an earned exception.
- **EF migrations are the only schema path.** Committed migrations live under
  `source/container-apps/web/platform/postgres/migrations/`. Never invent schema at startup.
- **Local / Aspire:** AppHost `AddEFMigrations` + `RunDatabaseUpdateOnStart` with **no wait
  edge** from web-server to the migration resource (both `WaitFor` and `WaitForCompletion`
  break restart or testing). On a fresh volume the app can briefly serve before migrate
  finishes; re-run on demand with the `ef-database-update` dashboard command. A **startup
  reader** waits until no migration is pending (`IPendingMigrationsProbe` /
  `EfPendingMigrationsProbe`, EF `GetPendingMigrationsAsync`) — not merely until its table exists,
  because a new column on an existing table fails the read too. A failing EF query logs Error
  inside EF before any catch runs, and a boot must log none.
- **Inspect data locally:** in Development the Postgres resource carries `WithRepl()`, so the
  dashboard offers an authenticated psql shell (terminal dock) on the running server.
- **Publish / deploy:** `PublishAsMigrationScript` / `PublishAsMigrationBundle`.
- **Tests:** ephemeral DBs call `Database.Migrate()` / `MigrateAsync()`. Do not reuse AppHost
  `WithDataVolume` state across runs.
- **Actors / outbox** are earned exceptions (Orleans grain-per-entity-ID over the same EF
  store for high-contention single-writer ids), not the default.

## Add an aggregate — walkthrough

Prerequisites: the `postgres` template flag is on (default).

### 1. Domain

Place `<name>-domain.cs` and `<name>-id-domain.cs` under `web/features/<slice>/`. Follow
**The golden pattern** above. Application code never writes `Version` — it is store-owned.

### 2. Infrastructure mapping

Add `<name>-entity-type-configuration-infrastructure.cs` in the same slice:

| Concern | What to do |
|---------|------------|
| Table + schema | `ToTable("orders", "orders")` — schema-per-slice on the single host context |
| Key | `HasKey(e => e.Id)` |
| TypedId | `HasConversion(id => id.Value, v => OrderId.From(v))` |
| Concurrency | nothing — `AggregateDbContext` configures `Version` for every `IAggregateRoot` |

Do **not** call `.IsConcurrencyToken()` yourself on an aggregate mapping.

### 3. Host registration

On `PostgresDbContext`: add `public DbSet<Order> Orders => Set<Order>();`, keep
`base.OnModelCreating` and `ApplyConfigurationsFromAssembly`. Override
`OnConfigureConventions` (not sealed `ConfigureConventions`) for TypedId conventions.
Feature `*-infrastructure.cs` files compile into web-infrastructure — do not hand-register
each `IEntityTypeConfiguration`.

### 4. Application

Load → named mutation → `SaveChangesAsync`. Do **not** call `DomainInvariantsGuard`
yourself. Invalid state fails closed before SQL. Concurrent writers surface as
`DbUpdateConcurrencyException`.

### 5. Tests

| Layer | What |
|-------|------|
| Domain unit | Create/guards/mutations (`profile-tests.cs`) |
| Model mapping | Schema, TypedId, concurrency token (no live DB) |
| SaveChanges hook | Version bump, child→root (`foundation-infrastructure-tests`) |
| Postgres integration | `Database.Migrate`, round-trip, concurrent update on an ephemeral DB |

### 6. Store port vs direct DbContext

| Use | When |
|-----|------|
| Direct `PostgresDbContext` / `DbSet<T>` | Product aggregate owned by this host; Profile teaching path |
| Port (`IPrincipalStore`, …) | Multi-backend seam; in-memory + EF must share snapshot-on-get and CAS semantics |

Identity Principal/Credential are **not** `IAggregateRoot`. The store owns optimistic
concurrency (`EntityVersion.Next` + `ConcurrencyConflictException`). Host mapping still
sets `.IsConcurrencyToken()` as a DB race belt, but `AggregateDbContext` does not
auto-increment `Version` for those types — that avoids a double-bump.

### 7. Schema evolution

After editing the model or an `IEntityTypeConfiguration`:

```bash
dev db add-migration <NameYourChange>
```

That wraps `dotnet tool restore` plus:

```bash
dotnet ef migrations add <NameYourChange> \
  --project source/container-apps/web/projects/web-infrastructure/web-infrastructure.csproj \
  --startup-project source/container-apps/web/projects/web-server/web-server.csproj \
  --context PostgresDbContext \
  --output-dir ../../platform/postgres/migrations \
  --namespace TimeWarp.Architecture.Persistence.Migrations
```

Do not kebab-rename EF scaffold files. Removing a mapped entity requires a migration that
drops the unused tables — never an out-of-band DROP against `__EFMigrationsHistory`.

### Checklist

- [ ] TypedId + `Entity<TId>` + `IAggregateRoot` + private `Invariants`
- [ ] `IEntityTypeConfiguration` with schema and TypedId conversion (no `.IsConcurrencyToken()`)
- [ ] `DbSet<>` + configurations discovered from assembly
- [ ] Handler path: load → mutate → `SaveChangesAsync`
- [ ] Domain unit tests + mapping and/or Postgres tests
- [ ] Purpose/Design regions honest (TWA0004)

## Related skills and pointers

- `tw-feature-placement` — filename grammar and layer membership (`<name>[-<function>]-<layer>.cs`,
  the `domain` layer, registry)
- `tw-slice-isolation` — which slice an aggregate belongs to before placement
- `AggregateDbContext` Design region — SaveChanges invariants, Version convention, child→root
  resolution (`source/foundation/foundation-infrastructure/persistence/aggregate-db-context.cs`)
