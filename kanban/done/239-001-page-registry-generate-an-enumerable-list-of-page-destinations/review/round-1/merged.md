# Round 1 — merged findings
**Date:** 2026-09-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/interfaces/i-navigation-destination.cs:18
- Description: `INavigationDestination` is a public marker; a hand-written implementation without `Navigable = true` satisfies `TimeWarpNavLink` but is absent from `PageRegistry`, so "cannot drift" overclaims.
- Suggestion: add a test that every `INavigationDestination` type is in `PageRegistry.All`, or soften the wording.
- Source: general
- Disposition notes: Fixed on this task — new `All_Should_.Include_Every_Navigation_Destination` (web-spa-integration-tests, 4/4 pass) plus Design-region notes on the interface and the test file.

## Duplicates / conflicts

- None (single reviewer).
