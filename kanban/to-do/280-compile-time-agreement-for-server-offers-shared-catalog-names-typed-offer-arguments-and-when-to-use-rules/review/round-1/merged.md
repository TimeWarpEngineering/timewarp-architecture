# Round 1 — merged findings
**Date:** 2026-10-05
**Sources:** general, tests

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 9 | 0 |
| nit | 0 | 4 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/action-offer-agreement-analyzer.cs:257
- Description: Constructor selection doesn't match TimeWarp.State's `ActionSetConstructorParser`, which takes the first `ConstructorDeclarationSyntax` in the attributed class declaration. The analyzer compares `SourceSpan.Start` across partial files and skips a leading static constructor.
- Suggestion: Mirror the generator: use the attributed declaration syntax and its first constructor declaration.
- Source: general (G1), tests (T3, multiple-ctor coverage)
- Disposition notes: fixed in 3f9df71a8 (see round-2 re-verification)

### M2 — Severity: suggestion — Status: fixed
- File: action-offer-agreement-analyzer.cs:247
- Description: TWA0030 accepts a bound optional parameter that comes after an unbound optional one. The client binder always refuses that.
- Suggestion: Report TWA0030 for this ordering.
- Source: general (G2)
- Disposition notes: fixed in 3f9df71a8 (see round-2 re-verification)

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/identity/offered-action-contracts.cs:65
- Description: `Create<TOffer>` uses the static type for the name lookup and serialization. An interface-typed argument throws, and a base-typed argument loses properties.
- Suggestion: Use `offer.GetType()`.
- Source: general (G3)
- Disposition notes: fixed in 3f9df71a8 (see round-2 re-verification)

### M4 — Severity: suggestion — Status: fixed
- File: tests/analyzers/timewarp-architecture-analyzers-tests/action-offer-agreement-analyzer-tests.cs
- Description: Analyzer branches with no test: JsonIgnore skip, inherited properties, `Guid?` vs `Guid`, multiple constructors, a JsonPropertyName with no matching parameter, gate value `false`.
- Suggestion: Add a small case for each.
- Source: general (G4), tests (T3)
- Disposition notes: fixed in 3f9df71a8 (see round-2 re-verification)

### M5 — Severity: nit — Status: fixed
- File: action-offer-agreement-analyzer.cs:227
- Description: A `string?` property passes against a non-nullable `string` parameter, and a null value is then refused at run time.
- Suggestion: Flag nullable property vs non-nullable reference parameter.
- Source: general (G5)
- Disposition notes: fixed in 3f9df71a8 (see round-2 re-verification)

### M6 — Severity: nit — Status: fixed
- File: offered-action-contracts.cs:48
- Description: "Offers built only from records" is a convention: the public ctor still allows hand-spelled names.
- Suggestion: State it in the Design region, or restrict the ctor.
- Source: general (G6)
- Disposition notes: fixed in 3f9df71a8 (see round-2 re-verification)

### M7 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/identity/credential-offers-application.cs:8
- Description: A Design-region line runs to 160 characters.
- Suggestion: Reflow it.
- Source: general (G7)
- Disposition notes: fixed in 3f9df71a8 (see round-2 re-verification)

### M8 — Severity: suggestion — Status: fixed
- File: action-offer-agreement-analyzer-tests.cs:293
- Description: No test covers the production-shaped transitive discovery (attribute assembly ≠ records assembly), so a regression there would silence the analyzer.
- Suggestion: Add a three-project test case.
- Source: tests (T1)
- Disposition notes: fixed in 3f9df71a8 (see round-2 re-verification)

### M9 — Severity: suggestion — Status: fixed
- File: action-offer-agreement-analyzer.cs:124
- Description: `if (catalogActions.IsEmpty) return;` lets bad offers through when there are zero catalog actions.
- Suggestion: Drop the early return (report TWA0029), or justify and pin it.
- Source: tests (T2)
- Disposition notes: fixed in 3f9df71a8 (see round-2 re-verification)

### M10 — Severity: suggestion — Status: fixed
- File: action-offer-agreement-analyzer.cs
- Description: When two actions share one explicit Name, the TWA0030 target is nondeterministic (ConcurrentBag) and the duplicate goes unreported.
- Suggestion: Make the behavior deterministic (report it, or order it) and pin it with a test.
- Source: tests (T3)
- Disposition notes: fixed in 3f9df71a8 (see round-2 re-verification)

### M11 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/credential-offers-tests.cs:74
- Description: The reflection check works out argument names with plain camelCase and ignores `[JsonPropertyName]` / `[JsonIgnore]`, so it will drift from the analyzer and the wire.
- Suggestion: Take the names from the real wire (`OfferedAction.Create(...).Arguments.Keys`) or honor the attributes.
- Source: tests (T4)
- Disposition notes: fixed in 3f9df71a8 (see round-2 re-verification)

### M12 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-contracts-tests/features/identity/identity-contracts-serialization-tests.cs:503
- Description: The Rename offer's wire key set is not pinned exactly.
- Suggestion: Assert `Arguments.Keys` is exactly `["credentialId"]`.
- Source: tests (T5)
- Disposition notes: fixed in 3f9df71a8 (see round-2 re-verification)

### M13 — Severity: nit — Status: fixed
- File: offered-action-contracts.cs:78
- Description: The throw in `OfferedAction.Create` for an unattributed type has no test.
- Suggestion: Add a host-free test.
- Source: tests (T6)
- Disposition notes: fixed in 3f9df71a8 (see round-2 re-verification)

## Duplicates / conflicts

- G4 + T3 (coverage gaps) collapsed into M4. T3's multiple-constructor coverage is folded into M1; its duplicate-name point is split out as M10.
