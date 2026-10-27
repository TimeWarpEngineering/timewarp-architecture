# Round 2 — general
**Date:** 2026-10-27
**Scope reviewed:** fix delta `8e9eed258..0a49d867d` (`ad83f400b` fix, `0a49d867d` kitchen note)

## Summary

The fix pass resolves all three round-1 findings and adds nothing out of scope. Catalog group names are now `h2.sg-catalog__heading` elements with a stable id (`sg-catalog-{group}`). Each nested part list sets `aria-labelledby` to that id. The heading CSS inherits `font-family` and `line-height`, and the global `h1…h6 { margin: 0 }` reset in `app.css` keeps the old eyebrow look. The contract test exempts `StyleGuideCatalog.razor` only from the `Changed="` and `ValueChanged` assertions, so the product-name scan reads it again. NotificationsSample and the page Design region restore the NotificationState, ExceptionNotification, and TWA0022 rationale. I checked those comments against `notification-state.exception-notification-handler.cs` and the TWA0022 entry in `AnalyzerReleases.Unshipped.md`, and they are accurate. The risk is low.

## Prior findings re-verified

- **M1 (suggestion), fixed:** `StyleGuideCatalog.razor:100-101` renders the h2 and the labelled ul. The contract test (`style-guide-page-contract-tests.cs:70-81`) checks every `StyleGuideParts.Group`. The Playwright catalog test checks headings by role after a deep link, on All, while filtered, and after the filter is cleared.
- **M2 (nit), fixed:** `style-guide-page-contract-tests.cs:260-272`: the catalog skips only the two callback assertions, and the `SourceProductName()` scan runs on every feature file.
- **M3 (nit), fixed:** `NotificationsSample.razor:6-14` and `StyleGuidePage.razor.cs` Design region.

## Gate re-run (this worktree at `0a49d867d`)

- `./bin/dev build -q`: client-name scan passed. The build completed successfully.
- `./bin/dev test -q --no-build -g integration`: 8 of 8 projects passed
- `./bin/dev test -q --no-build -g playwright`: web-spa-playwright-tests passed (262.0 s)

## Issues

None.
