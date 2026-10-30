# Round 2 — merged findings
**Date:** 2026-10-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: Directory.Packages.props:166
- Description: The pin comment block still stated the pre-456 versions (RC2, no Npgsql RC3,
  ServiceModel 10.0.652802) as current.
- Suggestion: Rewrite those present-tense sentences to the RC3 / 10.1.0 state.
- Source: general (round 1)
- Disposition notes: Fixed in f79283653. Lines 166–176 now describe the EF family, dotnet-ef, and
  Npgsql on RC3 moving together, and ServiceModel.Primitives on 10.1.0. All of these match the pins
  at lines 82 and 112–118 and `.config/dotnet-tools.json`. The duplicate task-456 paragraph was
  removed. The historical re-check lines are unchanged.

## Duplicates / conflicts

- None (single reviewer). No new findings this round.
