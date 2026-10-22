# Round 1 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/components/time-warp-page-render-tests.cs:240
- Description: A `#pragma warning disable ASP0006` around a dynamic-sequence `AddAttribute`
  loop silences the RenderTreeBuilder sequence-number analyzer. A suppression-free API
  (`AddMultipleAttributes`) exists, and the comment's claim that the sequence "cannot be a
  literal" is false.
- Suggestion: `child.AddMultipleAttributes(1, PageParameters);` Drop the pragma pair and the
  comment.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor:129
- Description: The title/`TwPageTitle`/`PageTitle`/h1 block is copied into both the no-slot
  and the slot branches. The two copies must agree.
- Suggestion: Write it once as a `RenderFragment` in `@code`. Render it from both branches.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/to-do/pages/TodoItemsPage.razor:10
- Description: The "Items on this list." description is a placeholder. The reference adopter
  should show a useful description.
- Suggestion: Describe what the user can do on the page.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:119
- Description: The body wrapped in `<ChildContent>` was not re-indented.
- Suggestion: Indent the wrapped body by one level.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
