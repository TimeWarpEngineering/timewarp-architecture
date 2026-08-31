# Round 2 — merged findings
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
- Description: Design said fingerprinted emit is remapped by the Blazor import map, but App.razor has no `<ImportMap />`.
- Suggestion: Reconcile Design to cite `<base href="/" />` + MapStaticAssets dual endpoints.
- Source: general
- Disposition notes: Round 2 verified Design now cites `./` resolution via `<base href="/" />`, MapStaticAssets unfingerprinted + fingerprinted twin, and no `<ImportMap />` in App.razor. App.razor matches.

## Duplicates / conflicts

- None. No new findings on the fix delta.
