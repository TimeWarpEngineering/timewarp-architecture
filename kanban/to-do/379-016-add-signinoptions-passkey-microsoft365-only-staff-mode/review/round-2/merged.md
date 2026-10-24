# Round 2 — merged findings
**Date:** 2026-10-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/components/SignInChoices.razor:43
- Description: The Microsoft 365 offer was a `bool` that read `false` before the fetch finished, so work-account modes briefly said Microsoft 365 was unavailable.
- Source: general (round 1)
- Disposition notes: Re-verified in round 2. The offer is `bool?`; the unavailable line needs `false`; the button needs `true`; Settings needs `true`. Tests cover the unknown offer.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/components/Microsoft365AccountChoice.razor:1
- Description: No test pinned the choose-account page's mode behavior.
- Source: general (round 1)
- Disposition notes: Re-verified in round 2. `Microsoft365AccountChoice_Should_` pins all three modes (3 passed).

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/configuration/sign-in-mode.cs:8
- Description: The Design region carried provenance history.
- Source: general (round 1)
- Disposition notes: Re-verified in round 2. The sentence is gone.

## Duplicates / conflicts

- None. No new findings on the fix delta.
