# Round 2 — merged findings
**Date:** 2026-09-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs
- Description: TWA0019 used `ReferencedAssemblySymbols` after discovery switched to marked refs, so a configured-but-unmarked assembly silent-emptied ingress prefixes.
- Suggestion: Align TWA0019 with marked names (mirroring TWE008).
- Source: general
- Disposition notes: Re-verified in round 2. TWA0019 uses `GetMarkedReferencedAssemblies`; unmarked + typo tests assert TWA0019 and empty `All`; docs aligned.

### M2 — Severity: nit — Status: fixed
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/mock-response-factory-registry-generator-tests.cs
- Description: Comment still described the old `contracts` substring name filter.
- Suggestion: Reword to `ApiEndpointsEmbedded`; assembly name incidental.
- Source: general
- Disposition notes: Re-verified in round 2. Comment describes the marker filter.

## Resolved prior

- M1 and M2 carried from round 1; both remain fixed. No new IDs.

## Duplicates / conflicts

- None. Single general reviewer; no new issues.
