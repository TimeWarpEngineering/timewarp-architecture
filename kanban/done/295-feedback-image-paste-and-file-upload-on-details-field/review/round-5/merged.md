# Round 5 — merged findings
**Date:** 2026-10-10
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 5 | 0 |
| suggestion | 0 | 5 | 0 |
| nit | 0 | 5 | 0 |

## Issues

### M15 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/feedback/pages/FeedbackListPage.razor:181
- Description: The upload-failure fallback matches the literal `"Unhandled Error"`, which must agree with the title synthesized in `HttpApiService`.
- Suggestion: One public constant used by the synthesizer, the page, and the test.
- Source: general
- Disposition notes: Fixed by the review oracle. `HttpApiService.UnhandledErrorTitle` is used in `SynthesizeProblemFromStatus`, `FeedbackListPage.razor`, and `http-api-service-tests.cs`. `dev build` 0/0; `HttpApiService_GetResponse` 14/14; `Accepts_` 4/4.

## Resolved prior

- M1–M14: fixed (see `round-3/merged.md`, `round-4/merged.md`). The delta does not reopen them.

## Duplicates / conflicts

- None.
