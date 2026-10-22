# Round 1 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/elements/EmptyState.razor:11
- Description: The Design region says a `role="status"` region is announced when it is inserted, and
  that a list that becomes empty after a filter is announced. Screen readers reliably announce only a
  change of content inside a live region that already exists. When the region is inserted together
  with its text, NVDA and JAWS often stay silent. Both adopters insert the region fresh: the grid's
  `EmptyContent`, and the "Your filings" branch after `Section`'s spinner. The Design region claims a
  behaviour the component does not guarantee.
- Suggestion: Keep `role="status"`. Rewrite the Design text: insertion is announced inconsistently,
  and a page that needs a guaranteed announcement keeps its own persistent live region (for example
  an always-rendered result count). Remove the claim that a filter-to-empty transition is announced.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor:160
- Description: The Full samples use the default `h2` inside a Card whose title is already `h2`, so
  the style guide demonstrates a flat outline.
- Suggestion: Set `HeadingLevel="3"` on the two Full samples.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:214
- Description: The adopter titles end in a period (also `FeedbackListPage.razor:436`). The style-guide
  titles and the task example do not.
- Suggestion: Drop the trailing period from both adopter titles.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).

## Evaluate

There are 3 open findings and 0 bugs. All three are small edits on this task id, so a fix pass is
requested for M1, M2, and M3 rather than wontfix. M1 corrects the record in a Design region, which
is worth doing on this change. Round 2 re-verifies these IDs against the fix delta. No disposition
is written yet.
