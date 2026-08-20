# Round 2 — merged findings
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
- Description: Round-1: do-not-skip set was unproven. Round-2: `Given_Named_Role_Types_Are_Not_Skipped` locks ILogger/DateTime/Guid/TimeSpan/CancellationToken/HttpStatusCode with true positives; `Given_Enum_Members_Are_Skipped` stays clean.
- Suggestion: Add true-positives and an enum-member skip case.
- Source: general
- Disposition notes: Verified in round 2. No new findings.

## Duplicates / conflicts

- None. Prior M1 carried; no new IDs.
