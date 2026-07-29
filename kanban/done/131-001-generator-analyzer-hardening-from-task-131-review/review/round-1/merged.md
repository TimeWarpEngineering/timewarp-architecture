# Round 1 — merged findings
**Date:** 2026-07-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: endpoint-metadata.cs / fast-endpoint-source-generator.cs
- Description: Empty route silently skipped; missing ApiRoute folded into TWE007 with verb-only wording
- Suggestion: Fail closed with TWE007 for empty route; broaden message
- Source: general
- Disposition notes: Fixed — empty route sets VerbUnresolved; TWE007 message covers route+verb; tests added for missing ApiRoute and empty route

### M2 — Severity: nit — Status: fixed
- File: generators/fast-endpoint-source-generator.md
- Description: Stale “Invalid endpoint type configurations” after F-005
- Source: general
- Disposition notes: Fixed — points at TWE/SG + developer reference

### M3 — Severity: suggestion — Status: fixed
- File: fast-endpoint-source-generator-more-tests.cs / generator-test-harness.cs
- Description: Missing tests for missing ApiRoute / empty route; harness BaseFastEndpoint namespace comment wrong
- Source: general
- Disposition notes: Fixed — two TWE007 tests; harness comment Foundation.Features

## Duplicates / conflicts

- None
