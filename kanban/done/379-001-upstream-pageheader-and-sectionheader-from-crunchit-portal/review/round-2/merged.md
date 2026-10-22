# Round 2 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/components/time-warp-page-render-tests.cs:237
- Description: An ASP0006 suppression around a dynamic-sequence `AddAttribute` loop.
- Source: general (round 1)
- Disposition notes: Fixed in `fee49aa3b`. `AddMultipleAttributes(1, PageParameters)` has no
  pragma. Build is 0/0 and the tests pass 4/4.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor:90
- Description: The title, `TwPageTitle`/`PageTitle`, and h1 block appeared twice.
- Source: general (round 1)
- Disposition notes: Fixed in `fee49aa3b`. A single `TitleBlock` RenderFragment is rendered from
  both branches.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/to-do/pages/TodoItemsPage.razor:10
- Description: The todo description was placeholder copy.
- Source: general (round 1)
- Disposition notes: Fixed in `fee49aa3b`. The text is now "Browse the sample todo list."

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:120
- Description: The body wrapped in `<ChildContent>` was not re-indented.
- Source: general (round 1)
- Disposition notes: Fixed in `fee49aa3b`. The commit changes whitespace only.

## Duplicates / conflicts

- None. Round 2 raised no new findings.
