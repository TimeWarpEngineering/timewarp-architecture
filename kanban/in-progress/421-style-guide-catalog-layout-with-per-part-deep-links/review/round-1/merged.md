# Round 1 — merged findings
**Date:** 2026-10-27
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Gate re-run (reviewer, this worktree at `964b41571`)

- `dev build -q`: Build completed successfully
- `dev test -q --no-build -g integration`: 8 of 8 projects passed
- `dev test -q --no-build -g playwright`: web-spa-playwright-tests passed (259.2 s)

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/style-guide/components/StyleGuideCatalog.razor:94
- Description: The catalog group headings are `<p>` elements with no heading or group semantics, and no group list is labelled. Assistive tech cannot see the grouping (WCAG 1.3.1).
- Suggestion: Use a real heading element styled the same, or label each nested `<ul>` with `aria-labelledby` pointing at the heading id.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/style-guide/style-guide-page-contract-tests.cs:247
- Description: The catalog `continue` meant for the filter's `ValueChanged` also skips the source-product-name check (line 255) on `StyleGuideCatalog.razor`.
- Suggestion: Narrow the exemption to the `Changed="` / `ValueChanged` assertions only.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/style-guide/samples/NotificationsSample.razor:5
- Description: The split dropped the rationale comments: NotificationState rather than `INotificationService`, the exception → ExceptionNotification → message bar chain, and the TWA0022 ban on direct mediator Send.
- Suggestion: Restore them as a short note in the sample, or in the page Design region next to the NotificationsSample line.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
