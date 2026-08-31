# Round 1 — merged findings
**Date:** 2026-08-31
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/services/web-authn-js-module.cs:10
- Description: Design says fingerprinted emit is remapped by the Blazor import map, but `App.razor` has no `<ImportMap />`. Nested routes are still safe because `<base href="/" />` makes `./js/features/web-authn.js` resolve at `/js/features/web-authn.js`, and MapStaticAssets serves the unfingerprinted path (no-cache) alongside the fingerprinted twin. The passkey path works; the Design line overstates the mechanism.
- Suggestion: Reconcile Design to cite the `./` + `<base href="/" />` resolution (and dual SWA endpoints). Do not add `<ImportMap />` in this task.
- Source: general
- Disposition notes: Design region now cites `<base href="/" />` + MapStaticAssets dual endpoints and states App.razor has no `<ImportMap />`. No ImportMap added.

## Duplicates / conflicts

- None (single reviewer).
