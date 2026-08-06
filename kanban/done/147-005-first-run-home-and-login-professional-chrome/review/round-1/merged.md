# Round 1 — merged findings
**Date:** 2026-08-06
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/pages/HomePage.razor:38
- Description: Anonymous Home CTA wrapped FluentButton inside NavLink (invalid HTML nesting; unreliable click path).
- Suggestion: FluentButton OnClick → NoSubRouteState.ChangeRoute(LoginPage.GetPageUrl()) like Profile.
- Source: general
- Disposition notes: Fixed — button + GoToLoginAsync; CrossSliceReference(typeof(LoginPage), …) for TWA0009 on the .cs partial. Build 0/0.

## Duplicates / conflicts

- None.
