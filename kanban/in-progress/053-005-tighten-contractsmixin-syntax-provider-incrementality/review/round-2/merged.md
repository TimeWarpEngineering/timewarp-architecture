# Round 2 — merged findings
**Date:** 2026-09-11
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/foundation/foundation-contracts-generators/contracts-mixin-generator.cs:200
- Description: Re-verified first-wins + compile assertion. `Transform` breaks after the first successful `Part`. AllowMultiple fixture emits one hint, first route only, and generated trees compile with no errors. Focused suite 15/15 passed. No duplicate members.
- Suggestion: (none)
- Source: general
- Disposition notes: Confirmed on the post-fix working tree in round 2. No new findings.

## Duplicates / conflicts

- Carried M1 from round 1; no new IDs.
