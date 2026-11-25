# Round 3 — merged findings
**Date:** 2026-11-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/feedback/feedback-state.submit-feedback.cs:98
- Description: Round 2: a later filing in the same circuit stored a stale page because `RouteState.Routes` drops the page just left when `/Feedback` is opened again. Round 3: `GetRequest` passes `NavigationHistory.Visits`. That list records the start address and every `LocationChanged` address, newest first, and never truncates to an earlier copy of the same URL. `Routes.razor` activates the scoped service before the first click. `FeedbackPagePath.From` still skips the feedback pages and still strips the query and fragment. `Use_The_Page_Left_When_Feedback_Is_Opened_Again` and the WebMCP submit both walk Counter, Feedback, `Weather?city=x`, Feedback and expect `/Weather`.
- Suggestion: done in `0beb777fe`.
- Source: general
- Disposition notes: fixed. Re-checked against the fix delta and the recorded build, filter tests, and audit. Gates were not re-run.

## Issues

None.

## Duplicates / conflicts

- Single reviewer. Nothing to collapse.
