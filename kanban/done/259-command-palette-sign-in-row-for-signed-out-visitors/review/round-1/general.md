# Round 1 — general
**Date:** 2026-09-30
**Scope reviewed:** same as framework (command-palette-roster.cs, command-palette-state.open.cs, command-palette-tests.cs, task.md)

## Summary

Adds a Page-kind Sign in row to the palette roster only while the principal is unauthenticated,
built from `LoginPage.Title` / `LoginPage.GetPageUrl()` (no hand-kept route), with the current
base-relative path as `?returnUrl` — byte-for-byte the same construction as `RedirectToLogin.razor`,
and validated downstream by `LoginPage.GetSafeReturnUrl`. TWA0009 edge is opted out with
`[CrossSliceReference]` like HomePage. Design/Purpose regions reconciled. Low risk.

Verified: `dotnet test -c Release -- --filter-class CommandPalette` in
web-spa-integration-tests → 28 passed, 0 failed (matches the Results claim). Tests cover
signed-out roster contents, signed-in absence, PageRegistry exclusion, ranking for all four
wordings, and Enter navigating to `/Login?returnUrl=%2FCounter`.

## Issues

None.
