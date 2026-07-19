# Round 2 — merged findings
**Date:** 2026-07-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

Full descriptions/suggestions: `general.md` (issue numbers match M numbers).

Round 2 was a re-review of the round-1 fix commit. All ten round-1 findings (M1–M10) were
independently re-verified against the post-fix code and confirmed fixed, with no reopens — see
`general.md`'s "Prior findings verification" section for the per-finding confirmation. Round 2
surfaced three new findings (M11–M13) from scanning the fix delta itself; all three are addressed
below.

## Issues

### M11 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/web-infrastructure/persistence/postgres-db-context.cs:97-98
- Description: `IncrementModifiedVersions` calls `entry.Property(VersionPropertyName)` and casts `(long)versionProperty.OriginalValue!` unconditionally for every Modified `IAggregateRoot` entry. Nothing enforces that an `IAggregateRoot` also inherits `Entity<TId>` — the marker is a standalone interface (TWA0011 checks the validator, not the base class) — so a consumer aggregate implementing `IAggregateRoot` without a mapped `long Version` property (no `Entity<TId>` base, or `Version` ignored in mapping, or retyped) makes every save throw EF's generic `InvalidOperationException` ("The property 'Version' ... was not found") or an `InvalidCastException`, with no pointer at the convention. Notably `OnModelCreating` defends its own pin with a symmetric skip (`GetProperty(...) is null → continue`, line 77) — the increment path has no such guard, so the two halves of the same file disagree about whether Version-less roots are tolerated. Latent today (the context is entity-free and the exemplar inherits `Entity<TId>`), and it fails loud rather than silently, hence suggestion not bug.
- Suggestion: In `IncrementModifiedVersions`, skip entries where `entry.Metadata.FindProperty(VersionPropertyName)` is null (mirroring the OnModelCreating guard) — or, if a Version-less aggregate root should be a hard error, throw a convention-pointing message instead of EF's generic one. One Design-region sentence stating which choice was made.
- Source: general
- Disposition notes: Took the hard-error option (coordinator directive: fail closed and loud, mirroring the guard's fail-closed philosophy). `IncrementModifiedVersions` now checks `entry.Metadata.FindProperty(VersionPropertyName)` for null AND `ClrType != typeof(long)` before touching the property, and throws `InvalidOperationException` naming the offending type and pointing at `Entity<TId>` (entity.cs) plus the Profile exemplar / aggregates overview.md. Added a Design-region paragraph explaining the asymmetry with `OnModelCreating`'s skip-and-continue is intentional: the model-building skip is harmless (nothing to pin), but the save path is the actual enforcement point, so skipping there would let a misdeclared root persist without ever moving its concurrency token. No unit test covers the new throw — it requires a live change-tracker `EntityEntry`/`PropertyEntry`, which this entity-free context has no DbSet to produce without a new EF test package; documented as a second instance of the same "no live EF round-trip test" gap already recorded for the increment itself.

### M12 — Severity: suggestion — Status: fixed
- File: source/foundation/foundation-application/services/domain-invariants-guard.cs:13-16 vs source/analyzers/timewarp-architecture-convention-analyzers/aggregate-invariants-analyzer.cs:129-132
- Description: The M5 fix introduced a new analyzer/guard asymmetry the Design regions don't reconcile. The guard's Design region names "a validator declared on an abstract aggregate base rather than every concrete leaf" as one of the two real shapes the BaseType walk exists to support — and the passing `WidgetBaseWithValidator`/`WidgetSubclassWithoutOwnValidator` tests prove the guard supports it. But TWA0011 still requires every concrete leaf to declare its *own* nested validator with type argument equal to the *leaf* type (`ValidatesAggregate` matches `SymbolEqualityComparer.Equals(typeArg, aggregateType)` against directly nested members only), so the base-declared shape cannot build warning-free under the repo's warnings-as-errors policy. Net effect: of the two shapes the walk supports, only the EF-proxy case (proxy types are runtime-generated and never seen by the analyzer) is actually usable; the base-declared-validator case is runtime-supported but build-blocked, and the analyzer's own Design region ("concrete leaves are still checked") doesn't mention the contradiction with the guard's advertised capability. Safe direction (build-time friction, never silent corruption), hence suggestion.
- Suggestion: Either (a) teach `ValidatesAggregate` to also accept a nested validator inherited from a base class whose type argument is that base type (mirroring the guard's contravariance walk), with a test; or (b) declare in BOTH Design regions that base-declared validators exist for proxy support only and TWA0011 deliberately still demands a per-leaf declaration — so the disagreement becomes a recorded decision instead of an implicit contradiction.
- Source: general
- Disposition notes: Took option (b) per coordinator directive — the analyzer was NOT weakened. Rewrote both Design regions to record the decision explicitly: the guard's now distinguishes "Sanctioned" (EF dynamic proxy support — the actual reason the BaseType walk exists) from "Tolerated, not authored" (base-declared validators resolve via contravariance as a side effect of the same walk, but are not a second endorsed authoring shape); the analyzer's now states outright that TWA0011 requires every concrete leaf to declare its own nested validator even when a base class already declares one, names the guard's tolerance of the base-declared shape as proxy-support fallout rather than a sanctioned pattern, and calls the asymmetry intentional ("this analyzer's contract is 'every concrete leaf declares its own invariants,' full stop"). No code/test change — this is a documentation-only fix per the chosen option.

### M13 — Severity: nit — Status: fixed
- File: tests/analyzers/timewarp-architecture-analyzers-tests/aggregate-invariants-analyzer-tests.cs:169-215
- Description: Of the three new `ValidatesAggregate` conditions added for M2, two are pinned by tests (abstract nested validator, wrong-namespace `AbstractValidator`) but the third — `HasParameterlessConstructor` — has no analyzer test: an aggregate whose only nested validator has exclusively parameterized constructors should raise TWA0011, and no test exercises that branch (the equivalent runtime path *is* tested via `Wraps_constructor_failure_as_MissingInvariantsValidatorException`). The comment above `HasParameterlessConstructor` also makes a specific claim about synthesized default ctors appearing in `InstanceConstructors` that a test would pin cheaply.
- Suggestion: Add `Given_CtorParameterized_Nested_Validator_StillFlags_Missing` (validator with only `Invariants(string x)`) expecting TWA0011.
- Source: general
- Disposition notes: Added `Given_CtorParameterized_Nested_Validator_StillFlags_Missing` — a private nested `Invariants : AbstractValidator<Widget>` whose only constructor is `Invariants(string label)` — asserting TWA0011 fires. Passes.

## Duplicates / conflicts

- Single source — no collapsing needed. M numbers = round-2/general.md issue numbers (continuing from round 1's M1–M10).
