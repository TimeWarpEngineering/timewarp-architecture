# Round 2 — merged findings
**Date:** 2026-11-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 6 | 0 |

## Resolved prior (round 1, re-verified on 10ec4bcb6)

- **M1 — bug — fixed.** `host-graph-lease.cs` is deleted, and `git grep HostGraphLease -- tests source`
  finds nothing. Six classes (`Get_Principal_Returns_`, the list/set-roles class, `Agent_Returns_`, the
  human class, `Settings_Returns_`, and the pages class) each own a `static HostGraph?`. SetupOnce
  stores `<Fixture>.CreateHostAsync()`, and CleanUpOnce disposes it and sets it to null. That matches
  the master `Returns_` lifetime. The fixtures hold no static host state.
- **M2 — suggestion — fixed.** `ScreenshotPath<TTest>`, generic helpers, and `Session.StartAsync<TTest>()`.
  Each class has a private `ScreenshotPath` for its own type.
- **M3 — suggestion — fixed.** The regex captures an optional `partial`. A repeated name collapses
  only when every declaration is partial.
- **M4 — suggestion — fixed.** Each half's regions are trimmed to its own tests. The shared Entra
  design moved to `EntraChallengeFixture`. See M10 for a gap in the deep-link halves.
- **M5, M6, M7, M8, M9 — nit — fixed.** Stale class names are corrected and the "Layout" fragment is
  removed. The `httpClient` rename, the removal of the `Store` field, and the
  `Should_Enforce_Policy_Agreement_Edge_Cases` rename are all in place.

## Issues

### M10 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/features/identity/protected-page-deep-link-pages-tests.cs:6
- Description: The master original had a `#region Design`. Both halves now have their Design prose
  as loose `//` lines after Purpose and no Design region
  (`protected-page-deep-link-pages-tests.cs:6-19`, `protected-page-deep-link-settings-tests.cs:6-21`).
- Suggestion: Wrap each block in `#region Design` / `#endregion`.
- Source: general
- Disposition notes: Fixed in the round-3 fix pass. Both halves wrap their Design prose in `#region Design` / `#endregion` right after Purpose. The prose is unchanged.

## Duplicates / conflicts

- No new reviewer conflicts. A line-length check found many comment and code lines over 110
  characters in the delta, including `ask-focus-playwright-tests.cs:11`. The same is true in master
  files, so this is not a repo norm and was not raised.
