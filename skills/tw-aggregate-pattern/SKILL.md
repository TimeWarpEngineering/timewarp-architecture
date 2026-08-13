---
name: tw-aggregate-pattern
description: "**TIMEWARP SKILL** — the golden aggregate-root pattern: typed id, `Entity<TId>` base, fail-closed `Create`, named mutations with no public setters, a private nested `Invariants` validator, and save-time enforcement via `DomainInvariantsGuard`/`AggregateDbContext`. Invoke before adding or reviewing an `IAggregateRoot`, or when TWA0011/TWA0012 fire. WHEN: add an aggregate, IAggregateRoot, aggregate root, TWA0011, TWA0012, Invariants validator, typed id."
when-to-use: aggregate root, IAggregateRoot, Entity<TId>, typed id, TypedId, Invariants validator, DomainInvariantsGuard, AggregateDbContext, TWA0011, TWA0012, fail-closed construction, named mutation, concurrency token, Version
---

# Aggregate pattern (TWA0011/0012)

An aggregate is a domain entity that is the consistency boundary for a set of invariants.
Every aggregate root in this repository follows the same golden pattern. This skill is the
pattern SSOT; `how-to-add-your-aggregate.md` is the human end-to-end walkthrough that defers to it.

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

## Exemplar

`web/features/profile/profile-domain.cs` + `profile-id-domain.cs` — read both before adding a
new aggregate. Its EF mapping (`profile-entity-type-configuration-infrastructure.cs` —
table/schema `profiles`, TypedId key conversion) is applied by `PostgresDbContext` via
`ApplyConfigurationsFromAssembly`. `Version`'s `.IsConcurrencyToken()`
is supplied for free by the `AggregateDbContext` `Version` convention — an aggregate's own
mapping does not declare it.

## Related skills and pointers

- `tw-feature-placement` — filename grammar and layer membership (`<name>[-<function>]-<layer>.cs`,
  the `domain` layer, registry)
- `tw-slice-isolation` — which slice an aggregate belongs to before placement
- `how-to-add-your-aggregate.md` — human end-to-end walkthrough (domain → EF mapping → host
  registration → application use → tests)
