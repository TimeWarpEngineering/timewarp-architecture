# Round 2 — merged findings
**Date:** 2026-11-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Resolved prior

Round 1 raised no findings. Nothing to carry forward.

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/features/feedback/feedback-state.submit-feedback.cs:93
- Description: The path is chosen at submit from `RouteState.Routes`. `FeedbackPagePath.From` treats that sequence as a newest-first visit list. In TimeWarp.State.Plus 12.0.0-beta.19, `TwPageTitle` calls `RouteState.PushRouteInfo` on first render, and `TruncateToOrPush` pops every entry from the top through the previous equal absolute URL before pushing the current one. Both feedback pages render `TwPageTitle` through `TimeWarpPage`, so the second open of `/Feedback` in a circuit deletes the page the filer just left. A later filing then stores an older page (Counter, then Feedback, then Weather, then Feedback again, stores `/Counter` instead of `/Weather`). The first filing in a circuit still records the page just left. The page-path unit tests pass a list that still contains that page, so they do not catch the collapse. Checked with `ilspycmd -t TimeWarp.Features.Routing.RouteState` and `ilspycmd -t TimeWarp.Features.Routing.TwPageTitle` on the TimeWarp.State.Plus 12.0.0-beta.19 assembly (reason: confirm the stack still holds the page the form was opened from).
- Suggestion: Snapshot the last non-feedback app path when navigation happens, before `PushRouteInfo` truncates the stack, and submit that snapshot. Keep stripping the query and fragment, and send null when the server would refuse the path. Cover the collapsed stack: after a second push of the same `/Feedback` URL, the stored path is the page the filer just left.
- Source: general
- Disposition notes: fixed in the round 2 fix pass. The submit reads a new scoped `NavigationHistory` (addresses recorded on `LocationChanged`, never truncated) instead of `RouteState.Routes`; tests cover a second open of `/Feedback` (unit and through the WebMCP submit).

## Duplicates / conflicts

- Single reviewer. Nothing to collapse.
