# Round 1 — general
**Date:** 2026-07-19
**Scope reviewed:** commit 437f0e17 vs parent

## Summary
The commit is well-structured and most of the mechanics check out: the `SaveChanges(bool)`/`SaveChangesAsync(bool, ct)` overrides are the correct funnel points (the parameterless overloads delegate to them virtually), Added+Modified/skip-Deleted filtering is right, equality/operator null handling in `Entity<TId>` is correct, deleted types (`BaseEntity`, `BaseEvent`, `ValueObject`, `IInvariants`, the old `TimeWarp.Architecture.Entities`/`Abstractions` namespaces) leave zero dangling references anywhere in source/tests/docs/template config, the slnx conditional placement and `AnalyzerReleases.Unshipped.md` rows are correct, and the guard's null-caching keeps fail-closed semantics on repeat calls. Two bugs stand out: the `Version` concurrency token has no mechanism that ever changes it (the Design region's ".IsConcurrencyToken() … the store increments it on save" describes behavior that does not exist, so the 104-002 D6 debt is not actually closed), and the analyzer's validator-shape check drifts from the runtime guard's in ways that let a clean build throw at save time. The remaining findings are exemplar-quality and message-accuracy suggestions/nits.

## Issues

### Issue 1 — Severity: bug
- File: source/foundation/foundation-domain/entities/base/entity.cs:30 (Design claim at lines 13–15)
- Description: `public long Version { get; private set; }` can never change. The Design region says "Hosts map it with .IsConcurrencyToken(); application code never writes it — the store increments it on save." That mechanism does not exist: EF Core's `IsConcurrencyToken()` only adds the *original* value to the UPDATE/DELETE WHERE clause — it never increments anything. No code path in this commit (not the PostgresDbContext hook, not any interceptor, not a database-generated mapping) writes `Version`, and the private setter plus "application code never writes it" rule out everyone else. So every row's `Version` stays 0 forever, `WHERE "Version" = 0` always matches, and stale overwrites proceed silently — the exact last-write-wins debt (104-002 RFC D6) the type claims to close. (Npgsql's xmin pattern needs a `uint` mapped to the system column, not a `long`; SQL Server rowversion needs `byte[]`/`IsRowVersion` — neither matches the documented shape.) Per AGENTS.md, a Design region describing behavior the code doesn't have is a bug; here it also means the concurrency feature silently does nothing. The task checklist ticks "Tests: … Version semantics", but the tests (tests/foundation/foundation-domain-tests/entity-tests.cs:73-87) can only assert "defaults to 0, no public setter" — consistent with the token being inert. Note also 104-028 expects identity stores (including non-EF ones) to "inherit Version"; a private setter with no defined increment seam makes that impossible without reflection.
- Suggestion: Give the token a real mechanism and document that one. Simplest EF-side fix: in the same `PostgresDbContext` hook, for `Modified` `IAggregateRoot` entries do `entry.Property(nameof(Entity<T>.Version)).CurrentValue = original + 1` (change-tracker writes bypass the private setter via backing field), and state that hosts must pair `.IsConcurrencyToken()` with this increment (or use a provider-native token instead). Alternatively expose a `protected internal`/infrastructure-facing increment seam. Then fix the Design region to describe whichever mechanism actually ships, and add a test that proves Version moves.
- Status: open

### Issue 2 — Severity: bug
- File: source/analyzers/timewarp-architecture-convention-analyzers/aggregate-invariants-analyzer.cs:98-111
- Description: The analyzer's Design region claims a nested Invariants validator is "the same shape DomainInvariantsGuard discovers at runtime", but the shapes drift, and every drift is in the direction "build passes, save throws":
  1. **Abstract nested validator.** `ValidatesAggregate` never checks `candidate.IsAbstract`, so `private abstract class Invariants : AbstractValidator<Widget>` satisfies TWA0011. The runtime guard filters `!nested.IsAbstract` (domain-invariants-guard.cs:66) and cannot instantiate it anyway → `MissingInvariantsValidatorException` on the first save.
  2. **No parameterless constructor.** The analyzer doesn't check constructability; the guard's `Activator.CreateInstance(validatorType, nonPublic: true)` (domain-invariants-guard.cs:68) throws a raw `MissingMethodException` — not the friendly convention-pointing exception — for a validator whose only ctor takes parameters.
  3. **Simple-name `AbstractValidator` match.** Any 1-arity base class named `AbstractValidator` satisfies TWA0011 even if it is not FluentValidation's (implements no `IValidator<T>`); the guard then finds nothing → `MissingInvariantsValidatorException`. Name-based matching is the documented house style, but unlike TWA0002/0005 the runtime consequence here is an exception, not a soft mismatch.
  4. **Generic aggregate root.** For `class Order<T> : IAggregateRoot` with a nested validator, reflection's `GetNestedTypes()` on the constructed type returns the *open* nested type definition, so `IsAssignableFrom` fails and the guard throws even though a validator is declared (fail-closed, but wrongly). The analyzer very likely accepts this shape.
  All paths fail closed at runtime (exception, not silent persistence), so data integrity is preserved — but the analyzer is advertised as the build-time upgrade of exactly this check, and its Design region's "same shape" claim is currently false.
- Suggestion: In `ValidatesAggregate`, also require `!candidate.IsAbstract` and a parameterless (any-accessibility) constructor; either verify the `AbstractValidator` base's containing namespace is `FluentValidation` or check the candidate also implements an interface simple-named `IValidator` with matching T. In the guard, wrap `Activator.CreateInstance` failures in `MissingInvariantsValidatorException` (or a sibling) so misdeclared validators still get a convention-pointing message. Add analyzer negative tests for the abstract-validator and wrong-base cases. Soften or fix the "same shape" sentence in whichever direction remains.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/aggregate-invariants-analyzer.cs:95-96
- Description: `FindInvariantsValidator` uses `FirstOrDefault`, and TWA0012 is only evaluated against that first qualifying nested type. If an aggregate declares two qualifying validators — a `private` one first and a `public` one second (e.g. a leftover copy during refactoring) — TWA0012 stays silent, yet `AddValidatorsFromAssemblyContaining` will register the public one, which is precisely the harm TWA0012 exists to prevent. The runtime guard has the mirror ambiguity: `FirstOrDefault` over `GetNestedTypes` picks an unspecified one when several match.
- Suggestion: Enumerate *all* qualifying nested validators; report TWA0012 on every non-private one regardless of position. Optionally have the guard prefer/require a single match (or deterministically pick, e.g. the private one) and add a test with two nested validators.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/web-domain/aggregates/profile/profile.cs:44-58
- Description: `MaxDisplayNameLength` is enforced only by the nested Invariants validator (line 86), not by `Create` or `Rename` — `Profile.Create(new string('a', 101), …)` and `profile.Rename(longName)` both succeed, and the violation only surfaces at save time as `DomainInvariantViolationException`. The Design region sells the composition as "guard clauses make invalid states hard to construct; the guard makes them impossible to persist", and the plan named `MaxDisplayNameLength` the "single source of truth" — but in the exemplar itself the guard clauses and the validator have already drifted (the agreement-by-memory failure mode this repo explicitly organizes against). Since Profile is the template exemplar, every generated app will copy this asymmetry.
- Suggestion: Add `ArgumentOutOfRangeException.ThrowIfGreaterThan(displayName.Length, MaxDisplayNameLength)` (or equivalent) to `Create` and `Rename`, referencing the same const, and add the corresponding rejection tests to profile-tests.cs. Alternatively, if length is deliberately validator-only, say so explicitly in the Design region so copiers don't treat the omission as accidental.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/foundation/foundation-application/services/domain-invariants-guard.cs:60-66
- Description: Discovery runs against `aggregate.GetType()` and only that type's own `GetNestedTypes` — it never walks the base-type chain. Two consequences for template consumers: (a) any dynamic-proxy configuration (EF `UseLazyLoadingProxies`/change-tracking proxies, which subclass non-sealed entities) makes `GetType()` return the proxy type, whose `GetNestedTypes()` is empty → every save of a proxied aggregate throws `MissingInvariantsValidatorException` despite a correctly declared validator; (b) a validator declared on an abstract aggregate base class is never found for concrete leaves even though `IValidator<in T>` contravariance would make it assignable. Both fail closed (nothing invalid persists), but (a) breaks a legitimate EF configuration outright. Profile being `sealed` masks this today.
- Suggestion: Walk `type.BaseType` in `DiscoverValidator` until a nested validator assignable to `IValidator<runtimeType>` is found (contravariance makes a base-declared `AbstractValidator<Base>` work), or at minimum document the proxy incompatibility in the Design region.
- Status: open

### Issue 6 — Severity: suggestion
- File: source/container-apps/web/web-infrastructure/persistence/postgres-db-context.cs:36-40
- Description: The hook validates only entries whose `Entity is IAggregateRoot` in state Added/Modified. For a future aggregate with child entities (or owned types), mutating only a child leaves the root's entry `Unchanged`, so the root's Invariants — which are supposed to govern the whole consistency boundary — never run for that save. Harmless today (Profile is single-entity), but the pattern is being shipped as the golden template and neither the guard's nor the context's Design region records this boundary.
- Suggestion: Add one Design-region sentence stating the current limitation (root invariants run only when the root row itself is Added/Modified) and the intended future resolution (e.g. resolving changed entries to their owning roots via navigation metadata) so the gap is a recorded decision rather than a surprise.
- Status: open

### Issue 7 — Severity: nit
- File: source/foundation/foundation-application/exceptions/missing-invariants-validator-exception.cs:37-40
- Description: The message has three small inaccuracies: (a) it asserts the type "implements IAggregateRoot", but the guard never checks that — `EnsureValid` on any object without a nested validator produces this claim (the guard's own tests throw it for `WidgetWithoutValidator`, which implements nothing); (b) it points at "the golden aggregate pattern (source/libraries/timewarp-identity)" — Principal/Credential contain no nested Invariants validator at all (grep confirms; that adoption is 104-028), so the pointer demonstrates the wrong half of the pattern; (c) `source/libraries/timewarp-identity/**` is excluded from generated apps when `foundationPackages=true` (template.json line 80, the default), so in the very apps that consume this published package the path doesn't exist. The actual exemplar is `web-domain/aggregates/profile/profile.cs` + `aggregates/overview.md`.
- Suggestion: Point the message at the Profile exemplar / aggregates overview (or describe the shape without a repo path), and phrase the first clause as "was validated as an aggregate root but declares no nested Invariants validator".
- Status: open

### Issue 8 — Severity: nit
- File: source/foundation/foundation-domain/entities/base/entity.cs:7-8
- Description: The Design region states "TId is a [TypedId] value type (never a raw Guid)" as fact, but the constraint (`where TId : struct, IEquatable<TId>`) permits raw `Guid`, nothing enforces the claim, and the repo's own tests instantiate `Entity<Guid>` (entity-tests.cs Widget/Gadget). A Design region stating an unenforced convention as an invariant is the kind of drift TWA0004 regions are meant to avoid.
- Suggestion: Reword to "by convention TId is a [TypedId] value type" (or note it as a candidate future analyzer per the prefer-analyzers directive), or constrain/tighten if enforcement is intended.
- Status: open

### Issue 9 — Severity: nit
- File: source/analyzers/timewarp-architecture-convention-analyzers/aggregate-invariants-analyzer.cs:53
- Description: The TWA0012 message says a non-private nested validator "is picked up by AddValidatorsFromAssemblyContaining and runs a second time as a request validator". For an `internal` nested validator that's only true when the host passes `includeInternalTypes: true` (FluentValidation's default is false), so the message overstates the default-case consequence for one of the accessibilities it flags. The rule itself (private-only) is fine.
- Suggestion: Soften to "can be picked up by assembly scanning" or mention the internal caveat in the descriptor description.
- Status: open

### Issue 10 — Severity: suggestion
- File: tests/analyzers/timewarp-architecture-analyzers-tests/aggregate-invariants-analyzer-tests.cs:66-164
- Description: Coverage is good for the five basic shapes but misses cases the analyzer's semantics specifically depend on: TWA0011 firing on a class that implements `IAggregateRoot` *indirectly* (via a base class, or via another interface extending it — both flow through `AllInterfaces` and should be exercised); a nested type named `Invariants` that is *not* a validator (the "matching by base-chain shape, not by name" claim in the Design region has no test proving a same-named non-validator still triggers TWA0011); and an `internal` nested validator for TWA0012 (only `public` is tested). Guard tests similarly lack a "nested validator for the wrong T" case (should throw `MissingInvariantsValidatorException`).
- Suggestion: Add these four cheap cases; they pin exactly the semantic claims the Design regions make.
- Status: open
