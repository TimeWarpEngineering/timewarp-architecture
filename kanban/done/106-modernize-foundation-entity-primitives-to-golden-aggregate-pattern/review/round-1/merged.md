# Round 1 — merged findings
**Date:** 2026-07-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 5 | 0 |
| nit | 0 | 3 | 0 |

Full descriptions/suggestions: `general.md` (issue numbers match M numbers).

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/foundation/foundation-domain/entities/base/entity.cs:30
- Description: `Version` concurrency token is inert — no mechanism anywhere increments it (`IsConcurrencyToken()` only compares originals; private setter blocks everyone else), so every row stays 0 and the D6 LWW debt is NOT closed; Design region describes a nonexistent mechanism.
- Suggestion: Increment in the PostgresDbContext hook via change-tracker (`entry.Property(...).CurrentValue = original + 1` for Modified roots); document that hosts pair `.IsConcurrencyToken()` with this increment; fix Design region; add a test proving Version moves.
- Source: general
- Disposition notes: Added `EntityVersion.Next(long)` (foundation-domain/entities/base/entity-version.cs) as the unit-tested pure increment seam (entity-version-tests.cs, 3 cases). `PostgresDbContext.SaveChanges(Async)` now calls it for every Modified `IAggregateRoot` entry via `entry.Property("Version").CurrentValue = EntityVersion.Next(originalValue)`, plus a new `OnModelCreating` that pins `PropertyAccessMode.Property` on the mapped "Version" property of any `IAggregateRoot` (defense-in-depth; verified via dotnet-inspect that EF Core 10's default `PreferFieldDuringConstruction` already routes non-construction writes through the property/private-setter). entity.cs Design region rewritten to state the real two-party mechanism (hook increments; host pairs `.IsConcurrencyToken()`) and to stop claiming a mechanism that didn't exist. No live EF round-trip test added (would require a new test package per the task's package constraint) — documented as an explicit gap in entity-version.cs's Design region.

### M2 — Severity: bug — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/aggregate-invariants-analyzer.cs:98-111
- Description: Analyzer validator-shape check drifts from runtime guard ("same shape" Design claim false): abstract nested validators, ctor-parameter validators, non-FluentValidation `AbstractValidator` simple-name matches, and generic aggregates all pass build but throw at save.
- Suggestion: Require `!IsAbstract` + parameterless ctor; verify FluentValidation namespace or `IValidator` interface; guard wraps `Activator.CreateInstance` failures in the convention-pointing exception; add negative tests; fix "same shape" sentence.
- Source: general
- Disposition notes: `ValidatesAggregate` now requires `!candidate.IsAbstract`, a parameterless constructor of any accessibility (`HasParameterlessConstructor`), and that the `AbstractValidator<T>` base's containing namespace is literally `"FluentValidation"`. `DomainInvariantsGuard.Instantiate` wraps `MissingMethodException`/`MemberAccessException` from `Activator.CreateInstance` into a new `MissingInvariantsValidatorException(Type, Exception)` overload with a cause message, while letting a genuine constructor bug (`TargetInvocationException`) propagate un-wrapped. Design region rewritten to state precisely where analyzer and guard now agree and the one remaining known gap (generic aggregate roots — reflection returns open nested-type definitions; documented, not fixed, no aggregate in this template is generic). Added analyzer tests `Given_Abstract_Nested_Validator_StillFlags_Missing` and `Given_WrongNamespace_AbstractValidator_StillFlags_Missing`, plus guard tests `Throws_MissingInvariantsValidatorException_when_nested_validator_targets_a_different_type` and `Wraps_constructor_failure_as_MissingInvariantsValidatorException`.

### M3 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/aggregate-invariants-analyzer.cs:95-96
- Description: TWA0012 only inspects the FIRST qualifying nested validator; a second public duplicate escapes — the exact harm the rule prevents. Guard has mirror ambiguity.
- Suggestion: Enumerate all qualifying validators; report TWA0012 on every non-private one; test with two nested validators.
- Source: general
- Disposition notes: `FindInvariantsValidators` (renamed from `FindInvariantsValidator`) now returns every qualifying nested type via `.Where(...).ToList()`, and `AnalyzeNamedType` reports TWA0012 on each non-private one. Guard: made the multi-candidate case deterministic rather than ambiguous — `SelectValidatorType` prefers a private candidate when more than one qualifies at a given type level (documented as a documented "your call" in the Design region: TWA0011/TWA0012 prevent this in reviewed code, so this is a best-effort pick, not ambiguity detection). Added analyzer test `Given_Two_Public_Nested_Validators_Flags_Both` (both flagged) and guard test `Prefers_the_private_candidate_when_multiple_nested_validators_qualify` (uses two validators with different rules so the outcome empirically proves which one ran).

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/web-domain/aggregates/profile/profile.cs:44-58
- Description: `MaxDisplayNameLength` enforced only by the validator, not `Create`/`Rename` — guard-clause/validator drift in the exemplar itself.
- Suggestion: Enforce the const in `Create` and `Rename` + rejection tests.
- Source: general
- Disposition notes: Added `EnsureDisplayNameLength` (`ArgumentOutOfRangeException.ThrowIfGreaterThan(displayName.Length, MaxDisplayNameLength, nameof(displayName))`), called from both `Create` and `Rename`. Design region reworded to explain the const is enforced in three places now (Create, Rename, Invariants) so the exemplar itself doesn't drift. Added `Accepts_displayName_at_max_length` / `Rejects_displayName_over_max_length` to `Create`, and `Rejects_displayName_over_max_length` to `Rename`, in profile-tests.cs.

### M5 — Severity: suggestion — Status: fixed
- File: source/foundation/foundation-application/services/domain-invariants-guard.cs:60-66
- Description: Discovery ignores base-type chain — EF dynamic proxies (subclass of entity) and base-declared validators spuriously throw `MissingInvariantsValidatorException`.
- Suggestion: Walk `BaseType` in discovery (contravariance makes base-declared validators work); document proxy compatibility.
- Source: general
- Disposition notes: `DiscoverAndInstantiateValidator` now walks `declaringType.BaseType` up the chain (object last), checking each level's own nested types against `IValidator<TAggregate>` — contravariance (`IValidator<in T>`, confirmed via dotnet-inspect) means a validator declared for a base type stays assignable to the derived runtime type's interface, so this resolves both the EF-proxy case and base-declared-validator case without any special-casing. Design region documents the proxy-compatibility rationale explicitly. Added guard tests `Finds_a_validator_declared_on_a_base_type` and `Runs_the_base_type_validator_against_the_subclass_instance` using a new `WidgetBaseWithValidator` / `WidgetSubclassWithoutOwnValidator` fixture pair.

### M6 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/web-infrastructure/persistence/postgres-db-context.cs:36-40
- Description: Child-entity-only changes leave the root `Unchanged` → root invariants never run for that save; undocumented boundary of the golden pattern.
- Suggestion: One Design-region sentence recording the limitation + intended future resolution.
- Source: general
- Disposition notes: Added a "Known boundary" paragraph to the Design region stating the limitation (only entries whose own Entity is IAggregateRoot are considered; child/owned-entity-only changes leave the root Unchanged) and the intended future resolution (resolve changed child entries to their owning root via navigation metadata once a real entity model with child/owned types exists).

### M7 — Severity: nit — Status: fixed
- File: source/foundation/foundation-application/exceptions/missing-invariants-validator-exception.cs:37-40
- Description: Message inaccuracies: claims type "implements IAggregateRoot" (unchecked); points at timewarp-identity which has no nested Invariants and is absent from default generated apps.
- Suggestion: Point at the Profile exemplar / aggregates overview; reword first clause.
- Source: general
- Disposition notes: Reworded `BuildMessage` to "was validated as an aggregate root but declares no nested Invariants validator" (drops the unchecked IAggregateRoot claim) and repointed both messages at `web-domain/aggregates/profile/profile.cs` + `aggregates/overview.md` instead of `source/libraries/timewarp-identity`. Purpose/Design regions updated to match (also records the reasoning: Principal/Credential have no nested Invariants validator, and the identity source path is excluded from generated apps under the default `foundationPackages=true`).

### M8 — Severity: nit — Status: fixed
- File: source/foundation/foundation-domain/entities/base/entity.cs:7-8
- Description: Design region states "never a raw Guid" as fact; constraint permits it and repo tests instantiate `Entity<Guid>`.
- Suggestion: Reword as convention (candidate future analyzer).
- Source: general
- Disposition notes: Reworded to "By convention TId is a [TypedId] value type (never a raw Guid)... the `where TId : struct, IEquatable<TId>` constraint does not enforce this... a build-time analyzer checking for a raw Guid TId is a candidate future enforcement (prefer-analyzers directive), not yet built" — states it as convention, not an enforced invariant, and explicitly acknowledges entity-tests.cs deliberately instantiates `Entity<Guid>`.

### M9 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/aggregate-invariants-analyzer.cs:53
- Description: TWA0012 message overstates internal-validator consequence (FluentValidation scanning defaults exclude internals).
- Suggestion: Soften to "can be picked up by assembly scanning".
- Source: general
- Disposition notes: `NonPrivateInvariants.messageFormat` reworded to "a non-private nested validator can be picked up by assembly scanning (such as AddValidatorsFromAssemblyContaining) and run a second time as a request validator" — no longer asserts it always will.

### M10 — Severity: suggestion — Status: fixed
- File: tests/analyzers/timewarp-architecture-analyzers-tests/aggregate-invariants-analyzer-tests.cs:66-164
- Description: Missing high-value cases: indirect IAggregateRoot (base class / extending interface), same-named non-validator nested type, internal validator for TWA0012, guard wrong-T validator case.
- Suggestion: Add the four cases.
- Source: general
- Disposition notes: Added all four: `Given_IAggregateRoot_Implemented_Via_BaseClass_Flags_Missing`, `Given_IAggregateRoot_Implemented_Via_Extended_Interface_Flags_Missing`, `Given_SameNamed_NonValidator_Nested_Type_StillFlags_Missing` (analyzer tests, via a new multi-file `Test(params (string, string)[])` overload), and `Throws_MissingInvariantsValidatorException_when_nested_validator_targets_a_different_type` (guard test, foundation-application-tests). Also added `Given_Internal_Nested_Validator_Flags_TWA0012` alongside them.

## Duplicates / conflicts

- Single source — no collapsing needed. M numbers = general.md issue numbers.
