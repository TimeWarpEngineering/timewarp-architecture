# Round 5 — general
**Date:** 2026-10-10
**Scope reviewed:** bf4ec7c8d..741a56e20 (upload-415 fix after the second PR #458 CI run)

## Summary

The upload endpoint cleared the FastEndpoints JSON accepts default but re-added the allow-list in
the same `Description` call, so the clear-convention deleted it and the host answered an empty
415 for `text/plain`. The fix adds the allow-list in `Finally`, after that convention. A new
`Accepts_` HTTP test posts text, text with charset, and PNG (200) and SVG (empty 415) through the
real host. The form now shows "The file could not be uploaded." when the failure has no useful
problem. The endpoint fix and the test are sound. One finding on the form fallback.

Verified: `dev build` 0 warnings / 0 errors; `Accepts_` 4/4; `HttpApiService_GetResponse` 14/14.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/feedback/pages/FeedbackListPage.razor:181
- Description: The page decides whether the failure was "explained" by comparing the notification title to the literal `"Unhandled Error"`. That string must agree with the title `HttpApiService.SynthesizeProblemFromStatus` writes (`http-api-service.cs:228`). A rename there silently breaks the fallback (the generic bar would count as explained). AGENTS.md: when two things must agree, derive one from the other.
- Suggestion: Expose the title as a public constant on `HttpApiService` and use it in the synthesizer, the page, and the existing `HttpApiService_GetResponse` assertion.
- Status: open
