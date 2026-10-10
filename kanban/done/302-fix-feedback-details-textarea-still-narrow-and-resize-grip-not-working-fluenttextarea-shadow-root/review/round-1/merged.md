# Round 1 — merged findings
**Date:** 2026-10-11
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-playwright-tests.cs:300
- Description: `ScreenshotPath` throws in generated apps. The template packs `tests/` but not `kanban/`, so
  both feedback Playwright tests fail after their assertions pass. The new 302 test adds a second dependency.
- Suggestion: Fall back to the test output folder when the repo root or kanban task folder is missing.
- Source: general
- Disposition notes: Fixed. `ScreenshotPath` uses `SingleOrDefault` over an existing `kanban/` folder and
  falls back to `AppContext.BaseDirectory`. Design region updated.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-playwright-tests.cs:61
- Description: The goto-and-wait block in `PasteAndUpload_Should_ShowAttachmentsOnTheItem` duplicates `OpenFeedbackAsync`.
- Suggestion: Reuse the helper.
- Source: general
- Disposition notes: Fixed. The attachment test calls `OpenFeedbackAsync`.

## Duplicates / conflicts

- None.
