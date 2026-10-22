# Round 2 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/elements/EmptyState.razor:11
- Description: The Design region claimed that inserting the status region is announced.
- Source: general (round 1)
- Disposition notes: Fixed in 96ebe145f. The text now says insertion is announced inconsistently,
  and points to a persistent live region. `role="status"` stays.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor:161
- Description: The Full samples used `h2` under the Card's `h2`.
- Source: general (round 1)
- Disposition notes: Fixed in 96ebe145f. Both samples pass `HeadingLevel="3"`.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:214
- Description: The adopter titles ended in a period.
- Source: general (round 1)
- Disposition notes: Fixed in 96ebe145f, along with `FeedbackListPage.razor:436`.

## Duplicates / conflicts

- None. Round 2 raised no new findings.

## Evaluate

0 open. Disposition: clean.
