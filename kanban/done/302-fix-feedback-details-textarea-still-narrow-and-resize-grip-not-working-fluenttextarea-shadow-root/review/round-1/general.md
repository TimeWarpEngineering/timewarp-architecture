# Round 1 — general
**Date:** 2026-10-11
**Scope reviewed:** same as framework

## Summary

The fix uses FluentTextArea's own styling API: the `block` attribute plus `--inline-size: 100%` and
`--min-block-size: 9rem` in scoped `::deep` CSS, and drops the fixed host `Height`. That matches the
shadow styles described in the task and the recorded Playwright numbers. The new Playwright test
measures the shadow `part=root`/`part=control` boxes against Title, which closes the #459 gap. Risk
is in the test helpers, which ship in every generated app.

## Issues

### Issue 1 — Severity: bug
- File: tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-playwright-tests.cs:300
- Description: `ScreenshotPath` requires `timewarp-architecture.slnx` and a `kanban/<prefix>-*` folder.
  The template pack (`timewarp-architecture-template.csproj`) ships `tests/` but not `kanban/`, so in a
  generated app `ScreenshotPath` throws (`ShouldNotBeNull` or `DirectoryNotFoundException`) and the
  Playwright tests fail after their assertions pass. The pattern came from task 295; this change adds a
  second test (`VisibleDetailsBox_Should_MatchTitle_And_Resize`) that depends on it.
- Suggestion: Fall back to the test output folder when the repo root or the kanban task folder is absent.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-playwright-tests.cs:61
- Description: The new `OpenFeedbackAsync` helper duplicates the goto-and-wait block still inlined in
  `PasteAndUpload_Should_ShowAttachmentsOnTheItem`.
- Suggestion: Call `OpenFeedbackAsync` from the attachment test too.
- Status: open
