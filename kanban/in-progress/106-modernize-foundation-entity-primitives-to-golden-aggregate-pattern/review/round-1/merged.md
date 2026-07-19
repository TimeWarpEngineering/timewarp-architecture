# Round 1 — merged findings
**Date:** 2026-07-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 5 | 0 | 0 |
| nit | 3 | 0 | 0 |

Full descriptions/suggestions: `general.md` (issue numbers match M numbers).

## Issues

### M1 — Severity: bug — Status: open
- File: source/foundation/foundation-domain/entities/base/entity.cs:30
- Description: `Version` concurrency token is inert — no mechanism anywhere increments it (`IsConcurrencyToken()` only compares originals; private setter blocks everyone else), so every row stays 0 and the D6 LWW debt is NOT closed; Design region describes a nonexistent mechanism.
- Suggestion: Increment in the PostgresDbContext hook via change-tracker (`entry.Property(...).CurrentValue = original + 1` for Modified roots); document that hosts pair `.IsConcurrencyToken()` with this increment; fix Design region; add a test proving Version moves.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/analyzers/timewarp-architecture-convention-analyzers/aggregate-invariants-analyzer.cs:98-111
- Description: Analyzer validator-shape check drifts from runtime guard ("same shape" Design claim false): abstract nested validators, ctor-parameter validators, non-FluentValidation `AbstractValidator` simple-name matches, and generic aggregates all pass build but throw at save.
- Suggestion: Require `!IsAbstract` + parameterless ctor; verify FluentValidation namespace or `IValidator` interface; guard wraps `Activator.CreateInstance` failures in the convention-pointing exception; add negative tests; fix "same shape" sentence.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/analyzers/timewarp-architecture-convention-analyzers/aggregate-invariants-analyzer.cs:95-96
- Description: TWA0012 only inspects the FIRST qualifying nested validator; a second public duplicate escapes — the exact harm the rule prevents. Guard has mirror ambiguity.
- Suggestion: Enumerate all qualifying validators; report TWA0012 on every non-private one; test with two nested validators.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: source/container-apps/web/web-domain/aggregates/profile/profile.cs:44-58
- Description: `MaxDisplayNameLength` enforced only by the validator, not `Create`/`Rename` — guard-clause/validator drift in the exemplar itself.
- Suggestion: Enforce the const in `Create` and `Rename` + rejection tests.
- Source: general
- Disposition notes:

### M5 — Severity: suggestion — Status: open
- File: source/foundation/foundation-application/services/domain-invariants-guard.cs:60-66
- Description: Discovery ignores base-type chain — EF dynamic proxies (subclass of entity) and base-declared validators spuriously throw `MissingInvariantsValidatorException`.
- Suggestion: Walk `BaseType` in discovery (contravariance makes base-declared validators work); document proxy compatibility.
- Source: general
- Disposition notes:

### M6 — Severity: suggestion — Status: open
- File: source/container-apps/web/web-infrastructure/persistence/postgres-db-context.cs:36-40
- Description: Child-entity-only changes leave the root `Unchanged` → root invariants never run for that save; undocumented boundary of the golden pattern.
- Suggestion: One Design-region sentence recording the limitation + intended future resolution.
- Source: general
- Disposition notes:

### M7 — Severity: nit — Status: open
- File: source/foundation/foundation-application/exceptions/missing-invariants-validator-exception.cs:37-40
- Description: Message inaccuracies: claims type "implements IAggregateRoot" (unchecked); points at timewarp-identity which has no nested Invariants and is absent from default generated apps.
- Suggestion: Point at the Profile exemplar / aggregates overview; reword first clause.
- Source: general
- Disposition notes:

### M8 — Severity: nit — Status: open
- File: source/foundation/foundation-domain/entities/base/entity.cs:7-8
- Description: Design region states "never a raw Guid" as fact; constraint permits it and repo tests instantiate `Entity<Guid>`.
- Suggestion: Reword as convention (candidate future analyzer).
- Source: general
- Disposition notes:

### M9 — Severity: nit — Status: open
- File: source/analyzers/timewarp-architecture-convention-analyzers/aggregate-invariants-analyzer.cs:53
- Description: TWA0012 message overstates internal-validator consequence (FluentValidation scanning defaults exclude internals).
- Suggestion: Soften to "can be picked up by assembly scanning".
- Source: general
- Disposition notes:

### M10 — Severity: suggestion — Status: open
- File: tests/analyzers/timewarp-architecture-analyzers-tests/aggregate-invariants-analyzer-tests.cs:66-164
- Description: Missing high-value cases: indirect IAggregateRoot (base class / extending interface), same-named non-validator nested type, internal validator for TWA0012, guard wrong-T validator case.
- Suggestion: Add the four cases.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single source — no collapsing needed. M numbers = general.md issue numbers.
