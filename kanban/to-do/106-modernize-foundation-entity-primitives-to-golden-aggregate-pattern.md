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

- Sequencing: foundation primitives first, Profile rewrite as the exemplar; timewarp-identity may
  optionally adopt `Entity<TId>` later (its entities already follow the pattern by hand — the
  pattern is location-independent, so no rush).
- The identity library's `IPrincipalStore` LWW/concurrency-token gap (104-002 RFC D6) is related
  but tracked separately — the library owns its port semantics; this task covers the
  app/foundation side (`Version` on `Entity<TId>`).
- The invariants guard runs at the EF persistence boundary, so it lives in
  foundation-application/foundation-infrastructure (or the host EF layer), not foundation-domain;
  the domain keeps only the declaration convention.
- Related: task 105 (Enumeration hardening) — same modernization sweep, separate task.

## Session

- Created: 2026-07-19
