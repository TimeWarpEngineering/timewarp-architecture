# Modernize foundation entity primitives to golden aggregate pattern

## Description

The entity primitives in `source/foundation/foundation-domain/entities/base/` and the demo
aggregate (`source/container-apps/web/web-domain/aggregates/profile/profile.cs`) predate the
patterns established in the 104 identity wave (typed ids, fail-closed factories, guarded
mutations). This repo IS the architecture — modernize the base primitives so every generated app
inherits the golden pattern, and complete the domain-invariants pattern whose enforcement half was
never ported from the original reference implementation.

Golden reference in-repo: `source/libraries/timewarp-identity` (Principal/Credential — private
ctor, fail-closed `Create`, named mutations, `[TypedId]` value-type ids over Guid).

## Requirements

### 1. Replace `BaseEntity` with `Entity<TId>`

Current `base-entity.cs` is `public Guid Guid { get; set; }` — defects:
- Publicly settable identity (any code can reassign an entity's id)
- Raw Guid (primitive obsession the `[TypedId]` generator eliminates — no compile-time
  protection against passing the wrong entity's id)
- Property named `Guid` collides with `System.Guid`, forcing qualification at call sites
- Provides no identity-based equality — the one thing an entity base is for

New shape: `abstract class Entity<TId> where TId : struct, IEquatable<TId>` with get-only typed
`Id`, protected ctor, equality = exact type + Id, and a store-owned `long Version` concurrency
token. Guid remains the underlying id type — wrapped in `[TypedId]` value types.

The `Version` token closes the 104-002 RFC D6 last-write-wins debt (stale-overwrite races)
uniformly for app entities. TypedId `New()` already mints v7 Guids for index-friendliness.

### 2. `[TypedId]` repo-wide

Extend TypedId use beyond identity: `ProfileId` and every future aggregate id is a
`[TypedId] readonly partial record struct`. Never raw Guid on any entity or contract.

### 3. Complete the nested-Invariants pattern (enforcement half)

The nested `private class Invariants : AbstractValidator<T>` convention (see profile.cs) is the
declaration half of a two-part pattern. The enforcement half was never ported and must be built:

- **Domain invariants guard**: service that validates changed aggregate roots inside the
  `DbContext.SaveChangesAsync` override, before base save. Violation throws
  `DomainInvariantViolationException` — invalid state can never be persisted regardless of which
  code path mutated the entity.
- **Fail-closed discovery**: an aggregate root with NO nested Invariants validator is itself an
  error (must-have-a-validator semantics).
- **Prefer-analyzers upgrade**: add a TWA diagnostic "aggregate root must declare a nested
  Invariants validator" so the fail-closed check moves from runtime to build time.
- Invariants validators stay privately nested so contract-validator auto-registration
  (`AddValidatorsFromAssemblyContaining`) does not pick them up.
- **Composition, not replacement**: fail-closed `Create` factories + named mutations (identity
  style) make invalid states hard to construct; the save-time guard makes them impossible to
  persist. Both.
- **Shared rule fragments** (evaluate): single source of truth for rules that appear in both
  contract validators and domain invariants (e.g. email shape, max lengths) — attribute-tagged
  meta classes or equivalent. Relates to TWA0002/0003 (contract nullability must agree with
  validator presence rules); consider whether existing analyzer/generator machinery should
  bridge contract seam <-> domain invariants.

### 4. Rewrite `Profile` as the template exemplar

- `ProfileId` typed id; inherit `Entity<ProfileId>`
- Private ctor + static `Create` with guard clauses (fail closed)
- Private setters; named mutation methods (e.g. `Rename`, `SetTheme`)
- Real rules in the nested Invariants validator (currently empty)
- Evaluate value objects/enums for `Language`, `Region`, `Theme` instead of raw strings

### 5. Delete the commented-out nopCommerce sketches

`aggregates/catalog/product.cs` and `category.cs` (~600 lines commented out) teach the wrong
architecture: int ids, `int FooId` + cast-property enum pattern, comma-separated id lists in
strings, God-entity property sprawl. Git history preserves them. Also a license-provenance
concern (nopCommerce-derived code in this repo) — deleting resolves it.

### 6. Fix or retire `ValueObject`

Defects in `value-object.cs`:
- `GetHashCode` XOR-aggregates components — order-insensitive, so ("a","b") and ("b","a")
  collide; and `Aggregate` throws `InvalidOperationException` on an empty components sequence
- Decision: fix (HashCode accumulator loop) or retire in favor of `record` /
  `readonly record struct` guidance, which provides structural equality without the base class

### 7. `BaseEvent` doc fix

Purpose region says "MediatR notification pipeline"; the repo uses TimeWarp.Mediator (NOT
MediatR). Fix the region; confirm the type is actually used or remove it.

## Checklist

- [ ] `Entity<TId>` base (typed get-only Id, equality, `long Version`)
- [ ] `ProfileId` via `[TypedId]`; Profile inherits `Entity<ProfileId>`
- [ ] Profile: private ctor, `Create` factory, named mutations, real Invariants rules
- [ ] Domain invariants guard + `SaveChangesAsync` hook + `DomainInvariantViolationException`
- [ ] Fail-closed: missing Invariants validator on aggregate root = error
- [ ] TWA analyzer: aggregate root must declare nested Invariants (build-time)
- [ ] Evaluate shared rule fragments between contract validators and domain invariants
- [ ] Delete product.cs / category.cs commented sketches
- [ ] ValueObject: fix hash defects or retire for records
- [ ] BaseEvent Purpose region corrected (TimeWarp.Mediator, not MediatR)
- [ ] Reconcile all touched `#region Design` blocks
- [ ] Tests: entity equality, Version semantics, invariants guard happy + violation paths

## Notes

### Implementation plan (2026-07-19)

#### Investigation summary (facts the plan relies on)

- `Profile` (`TimeWarp.Architecture.Entities.Profile`) is constructed nowhere. `get-profile-handler.cs` builds contract `Response` objects from the mock factory; the endpoint and SPA only touch the contract. The rewrite breaks no seam. `web-infrastructure/global-usings.cs` imports the namespace globally but uses no type from it.
- `PostgresDbContext` exists (`source/container-apps/web/web-infrastructure/persistence/postgres-db-context.cs`), is `sealed partial`, entity-free, no `SaveChangesAsync` override — the natural home for the enforcement hook. web-infrastructure references Npgsql EF 10.0.3. foundation-application/infrastructure have NO EF reference — guard core must be EF-agnostic; hook lives in the host EF layer.
- foundation-domain's only third-party dep is `TimeWarp.Mediator` (used solely by `BaseEvent : INotification`) — deleting BaseEvent makes foundation-domain dependency-free (leanness prerequisite for 104-028).
- foundation-application gets FluentValidation transitively via foundation-contracts (pinned 12.1.1); web-domain already references FluentValidation directly.
- TypedId generator: `timewarp-architecture-analyzers` project (PackageId `TimeWarp.Architecture.Generators`), attached per-project via `OutputItemType="Analyzer"` ProjectReference (dual-mode pattern in `timewarp-identity.csproj`). web-domain does NOT have it yet. TWE006 enforces the `readonly partial record struct` shape; generated partials are exempt from TWA0001.
- Next free convention-analyzer ID: **TWA0011** (registry: `AnalyzerReleases.Unshipped.md`).
- Tests: Fixie + Shouldly + TimeWarp.Fixie; `tests/foundation/foundation-application-tests` and `tests/container-apps/web/web-domain-tests` do not exist yet — create + add to `timewarp-architecture.slnx` inside the correct `<!--#if -->` conditional folders.

#### Ordered work items

### 1. foundation-domain: new primitives, delete old ones

Directory: `source/foundation/foundation-domain/entities/base/`

1. **New `entity.cs`** — `namespace TimeWarp.Foundation.Entities;`
   - `public abstract class Entity<TId> : IEquatable<Entity<TId>> where TId : struct, IEquatable<TId>`
   - `protected Entity(TId id)` ctor; `public TId Id { get; }` (get-only; EF binds via ctor-parameter name match)
   - `public long Version { get; private set; }` — store-owned concurrency token; Design region documents hosts map it with `.IsConcurrencyToken()` (closes 104-002 RFC D6 LWW debt); application code never writes it
   - Equality: `Equals(object?)` + `Equals(Entity<TId>?)` = exact runtime type + `Id.Equals`; `GetHashCode() => HashCode.Combine(GetType(), Id)`; `==`/`!=` operators
2. **New `i-aggregate-root.cs`** — `public interface IAggregateRoot;` (CA1040 suppression, pattern: assembly-marker.cs). Marks consistency-boundary roots for the invariants guard and TWA0011.
3. **Delete `base-entity.cs`** (no remaining users after Profile rewrite).
4. **Delete `base-event.cs`**; remove `TimeWarp.Mediator` PackageReference from `foundation-domain.csproj` and `global using TimeWarp.Mediator;` from `global-usings.cs`. Update csproj `<Description>`.
5. **Delete `value-object.cs`**.

### 2. web-domain: TypedId wiring + Profile exemplar + deletions

1. **`web-domain.csproj`**: add generator reference (copy dual-mode block from `timewarp-identity.csproj`).
2. **New `aggregates/profile/profile-id.cs`** — `[TypedId] public readonly partial record struct ProfileId;` (mirrors `principal-id.cs` incl. region style).
3. **Rewrite `aggregates/profile/profile.cs`**:
   - `public sealed class Profile : Entity<ProfileId>, IAggregateRoot`
   - `public const int MaxDisplayNameLength = 100;` (single source of truth for future contract validators)
   - Private ctor; `public static Profile Create(string displayName, string language, string region, string theme)` — fail-closed guard clauses, mints `ProfileId.New()`
   - Private setters + named mutations: `Rename`, `SetLanguage`, `SetRegion`, `SetTheme`, `EnableNotifications`/`DisableNotifications`
   - `private sealed class Invariants : AbstractValidator<Profile>` with real rules: DisplayName NotEmpty + MaximumLength(MaxDisplayNameLength); Language/Region/Theme NotEmpty
   - Rewrite Purpose/Design regions (drop CA1852 pragma if sealed satisfies it; keep "private so AddValidatorsFromAssemblyContaining skips it" rationale)
4. **Delete** `aggregates/catalog/product.cs` and `category.cs` (and empty `catalog/` folder).
5. **Delete `abstractions/i-invariants.cs`** — only referenced inside the deleted category sketch; markers are now `IAggregateRoot` + nested-validator convention.

### 3. foundation-application: invariants guard (EF-agnostic core)

1. **`exceptions/domain-invariant-violation-exception.cs`** — carries aggregate type name + failed rules.
2. **`exceptions/missing-invariants-validator-exception.cs`** — fail-closed discovery failure; message points at the convention and TWA0011.
3. **`services/domain-invariants-guard.cs`** — `public static class DomainInvariantsGuard`:
   - `EnsureValid(IEnumerable<object>)` + `EnsureValid(object)`
   - Discovery: nested types (public+non-public) assignable to `IValidator<T>`; instantiate once; cache in `static ConcurrentDictionary<Type, IValidator>`
   - Fail-closed: no nested validator → `MissingInvariantsValidatorException`; validation failure → `DomainInvariantViolationException`
   - Static pure function (no DI interface) — no dependencies, hook calls it without ctor plumbing; Design region records the choice.
4. **`foundation-application.csproj`** — explicit `<PackageReference Include="FluentValidation" />` (already pinned; don't rely on transitive flow for a published package).

### 4. web-infrastructure: SaveChanges enforcement hook

Edit `persistence/postgres-db-context.cs`:
- Override `SaveChangesAsync(bool, CancellationToken)` + `SaveChanges(bool)`: entries `Added or Modified` where `Entity is IAggregateRoot` → `DomainInvariantsGuard.EnsureValid`, then base. Deleted skipped by design.
- Update Design region; `SqlDbContext` untouched (one Design sentence noting consumers copy the same override).
- Recommended: attach generators reference to `web-infrastructure.csproj` so TypedId EF ValueConverter pass emits ProfileId converters when a DbSet appears.

### 5. Analyzer: TWA0011 (+ TWA0012)

New `source/analyzers/timewarp-architecture-convention-analyzers/aggregate-invariants-analyzer.cs`:
- **TWA0011** — "Aggregate root must declare a nested Invariants validator", Category Design, Warning (build-blocking under warnings-as-errors), on class identifier.
- `RegisterSymbolAction(SymbolKind.NamedType)`; standard preamble.
- Semantic check: non-abstract class implementing interface simple-named `IAggregateRoot` (name-based, same approach as contract-nullability analyzer) → must contain nested type whose base chain includes `AbstractValidator` arity-1 with type arg = containing type.
- **TWA0012** — "Nested Invariants validator must be private" (keeps it out of AddValidatorsFromAssemblyContaining).
- Bookkeeping: `AnalyzerReleases.Unshipped.md` rows; csproj Description "TWA0002–0012"; Directory.Build.props comment.
- Runtime fail-closed check intentionally duplicates TWA0011 (analyzer = build-time upgrade, runtime = backstop); record in both Design regions.

### 6. Tests

| Project | Status | Cases |
|---|---|---|
| `tests/foundation/foundation-domain-tests` | exists | `entity-tests.cs`: equality (same type+Id, `==`, hash), same Id different types NOT equal, different Ids, nulls; Version defaults 0, no public setter |
| `tests/foundation/foundation-application-tests` | NEW (copy csproj/testing-convention/global-usings from foundation-domain-tests; add to slnx in `!foundationPackages` block) | guard: valid passes; violation throws with failed rule visible; missing validator throws (fail-closed); cache correctness on repeat; private nested validator found |
| `tests/container-apps/web/web-domain-tests` | NEW (add to slnx web tests folder) | `profile-id-tests.cs`: New() non-empty/distinct, JSON round-trip (light); `profile-tests.cs`: Create happy + rejects null/whitespace; mutations update state + reject invalid |
| `tests/analyzers/timewarp-architecture-analyzers-tests` | exists | TWA0011/0012 via `CSharpAnalyzerTest` + stubs (copy FluentValidation stub; add IAggregateRoot stub): root w/ private nested → clean; root w/o → TWA0011; non-root → clean; abstract root → clean; public nested → TWA0012 |
| SaveChanges integration | skipped | context entity-free, no EF InMemory/Sqlite pinned (no new packages); override is thin glue over directly-tested guard. Revisit when real entity model lands. |

### 7. Docs, regions, closeout

- Reconcile Purpose/Design regions in every touched file (TWA0004).
- Write `web-domain/aggregates/overview.md` (currently empty): golden aggregate pattern statement; Profile is the exemplar.
- Verify `dev build` (0/0) and `dev test`.
- Tick kanban checklist; record recommendations in Notes.

#### Recommendations (decisions recorded)

- **ValueObject: retire (delete).** Zero subclasses; records give structural equality free; removes defect-bearing base (XOR hash, empty-Aggregate throw) from published surface while in 2.0.0-beta.
- **BaseEvent: delete.** Zero usages; only reason foundation-domain references TimeWarp.Mediator; deleting achieves full dependency-freedom. Reintroduce in foundation-application when domain events are built.
- **Shared rule fragments: no machinery now.** One aggregate, empty contract validator — nothing duplicated yet. Convention: aggregates expose `public const` limits (e.g. `Profile.MaxDisplayNameLength`) referenced by contract validators. Revisit meta-rules when a real slice duplicates a semantic rule.
- **Language/Region/Theme: keep validated strings.** Enum/Enumeration conversion belongs to task 105's sweep; coupling avoided.

#### Open questions

None.

- **Decision (2026-07-19): identity foundation-independence is dropped.** foundation-domain ships
  as a published `TimeWarp.Foundation.*` package, so timewarp-identity referencing it is a normal
  package dependency between two published libraries (ASP.NET Identity -> Microsoft.Extensions.*
  precedent) — not a reusable library depending on template scaffolding. The prior isolation was
  a don't-inherit-the-old-junk instinct; this task removes the old junk. Prerequisite: keep
  foundation-domain deliberately lean (domain primitives only) so the transitive cost to identity
  consumers stays negligible and its API stays stable.
- Sequencing: **106 -> 104-028 -> 104-003.** This task lands the primitives; 104-028 then has
  timewarp-identity adopt `Entity<TId>` (inheriting `Version` instead of defining its own) and
  adds the store-port conflict semantics; 104-003 handlers come after both.
- The identity library's `IPrincipalStore` conflict semantics (104-002 RFC D6) stay in 104-028 —
  the port contract is identity's own; this task provides the entity shape it inherits.
- The invariants guard runs at the EF persistence boundary, so it lives in
  foundation-application/foundation-infrastructure (or the host EF layer), not foundation-domain;
  the domain keeps only the declaration convention.
- Related: task 105 (Enumeration hardening) — same modernization sweep, separate task.

## Session

- Created: 2026-07-19
