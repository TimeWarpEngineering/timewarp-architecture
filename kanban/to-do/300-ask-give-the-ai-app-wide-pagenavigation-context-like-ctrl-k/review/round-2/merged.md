# Round 2 — merged findings
**Date:** 2026-10-15
**Sources:** general (re-review of fix commit 273115a53 and the round-1 ledger)

## Counts (final, M1–M10)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 5 | 1 |

## Resolved prior

| ID | Severity | Status | Verified in round 2 |
|----|----------|--------|---------------------|
| M1 | suggestion | fixed | yes |
| M2 | suggestion | fixed | yes |
| M3 | suggestion | fixed | yes |
| M4 | suggestion | fixed | yes |
| M5 | nit | fixed | yes |
| M6 | nit | fixed | yes |
| M7 | nit | wontfix | rationale accepted |
| M8 | nit | fixed | yes (null-cascade and sign-in/out both recompute correctly) |

## Issues

### M9 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-tests.cs:132
- Description: The contiguity check exempts the top-level `""` group without saying so.
- Suggestion: Add a comment.
- Source: general
- Disposition notes: Comment added. The exemption is intentional, since top-level pages draw no category header.

### M10 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-tests.cs:270
- Description: The `PageMember` guard regex would also flag a lower-case local such as `currentPage.Title`.
- Suggestion: Anchor on an upper-case type name.
- Source: general
- Disposition notes: The regex is now `\b[A-Z]\w*Page\.(GetPageUrl|Title|NavIcon|Policy)\b`. Verified by the oracle: `dev build` 0/0 and `NavigationDestinations_` 6/6. Trivial test-only change, so no round 3.

## Duplicates / conflicts

- None.
