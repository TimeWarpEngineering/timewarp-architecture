# Round 1 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalsPage.razor:107
- Description: When the Principal GUID is truncated at narrow widths, there is no tooltip to show the full value.
- Suggestion: Add a Tooltip/TooltipText that matches the feedback grid's Owner column.
- Source: general
- Disposition notes: Fixed on this task. Re-verified with `dev build` (0/0), `DataGridFrameGuard` (1/1), and Playwright `AdminGrids_Should_KeepThePrimaryColumn_WhenNarrow_Or_AskIsOpen` (1/1). This is a one-line attribute fix with its proof re-run, so no second round was opened.

## Duplicates / conflicts

- None (single reviewer).
