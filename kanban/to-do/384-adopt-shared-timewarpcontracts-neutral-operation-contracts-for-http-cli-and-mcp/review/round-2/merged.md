# Round 2 — merged findings
**Date:** 2026-10-18
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/foundation/timewarp-contracts/timewarp-contracts.csproj
- Description: (round 1) the package's own `1.0.0-beta.1` version had no gate.
- Source: general (round 1)
- Disposition notes: Re-verified in round 2. The `<Version>` is removed, the package packs at
  repo `2.0.0-beta.20`, and `dev check-version` lists and passes it. No new check was added,
  per Steven's decision (2026-10-18). The smoke source map and publish-smoke wait list name
  the id.

### M2 — Severity: suggestion — Status: fixed
- File: source/foundation/foundation-contracts-generators/contracts-generator.cs;
  source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs
- Description: (round 1) the two `[Operation]` readers had different blank-value rules, and
  the tests used a stub attribute.
- Source: general (round 1)
- Disposition notes: Re-verified in round 2. Both readers use `IsNullOrWhiteSpace` for name
  (no operation) and description (null). The tests compile against the real attribute, and
  the FQN is pinned. Sourcegenerator tests pass 135/135.

No new findings on the fix delta.

## Duplicates / conflicts

- None (single reviewer).
