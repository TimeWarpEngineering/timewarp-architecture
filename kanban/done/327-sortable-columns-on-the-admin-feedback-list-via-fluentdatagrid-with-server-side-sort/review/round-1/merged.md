# Round 1 — merged findings
**Date:** 2026-10-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/feedback-admin-state/feedback-admin-state.fetch-feedback-list.cs:6
- Description: Design region said `[TrackAction]` drives the Section spinner; this change removed the Section's `LoadingActionType`, so it now drives the grid's `Loading`.
- Suggestion: reword the region.
- Source: general
- Disposition notes: Fixed — region now says "[TrackAction] drives the grid's Loading state."

### M2 — Severity: suggestion — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:69
- Description: Two rapid header clicks start two un-cancelled fetches; an out-of-order response could show rows that disagree with the header direction.
- Suggestion: ignore stale responses in `HandleSuccess`, or file a follow-up for all admin list fetches.
- Source: general
- Disposition notes: wontfix on this task (decided by review orchestrator). The same last-response-wins race already exists for the filter dropdowns and every admin list fetch; fixing it only for sort would be inconsistent. Belongs in a cross-list follow-up (stale-response guard for admin list fetches), not this sort task.

## Duplicates / conflicts

- None (single reviewer).
