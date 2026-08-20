# Round 1 — merged findings
**Date:** 2026-08-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/analyzers/timewarp-architecture-analyzers-tests/type-stem-identifier-analyzer-tests.cs:575
- Description: The documented do-not-skip set is almost unproven. `Given_ILogger_Stem_Is_Logger` only uses `logger` / `catalogLogger`, which pass whether the type is analyzed (stem `Logger`) or skipped entirely. DateTime, Guid, TimeSpan, CancellationToken, and enums-as-types have no cases. Enum members (Design skip) are untested — dropping `ContainingType?.TypeKind == TypeKind.Enum` would flag every named value and no test would fail. Only `Given_IHttpClientFactory_Factory_Flags` locks a do-not-skip entry with a true positive.
- Suggestion: Add true-positives that fail if the type is skipped (`ILogger<T> log`, `DateTime dt`, `Guid id`, `TimeSpan ts`, `CancellationToken ct`, `HttpStatusCode code`) and one clean enum-member case (`enum Color { Red }`).
- Source: general
- Disposition notes: Added `Given_Named_Role_Types_Are_Not_Skipped` (true positives for ILogger log, DateTime dt, Guid id, TimeSpan ts, CancellationToken ct, HttpStatusCode code) and `Given_Enum_Members_Are_Skipped`. Analyzer tests 27/27. Decider: orchestrator.

## Duplicates / conflicts

- None (single reviewer).
