# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** HEAD 35ecb7a04 vs HEAD~1

## Summary
No correctness bugs found. EF orders before Skip/Take (Count runs on the unordered query), the
ThenBy chain (primary, FiledAt desc when primary is not FiledAt, Id asc) matches between EF and
in-memory stores, and Kind/Status are `HasConversion<string>()` columns so name ordering is what
both stores do. Grid sort mapping was verified against decompiled FluentUI 5.0.0: a click on a
non-primary column sorts ascending (Auto), RefreshItems is skipped when the request is unchanged,
and the page's early-return means no dispatch loop. Every hand-computed expectation in the
in-memory, Postgres, SPA-state and Playwright tests checks out. Two small items below.

## Issues
### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/feedback-admin-state/feedback-admin-state.fetch-feedback-list.cs:6
- Description: The Design region still says "[TrackAction] drives the Section spinner". This commit removed `LoadingActionType` from the Feedback Section; the tracked action now drives the grid's `Loading` (`IsListLoading`). The region now disagrees with the page.
- Suggestion: Reword to "[TrackAction] drives the grid's Loading (FeedbackAdminListPage.IsListLoading)".
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:69
- Description: Two quick header clicks (Title asc, then Title desc) start two FetchFeedbackList calls. The grid cancels its own pending load, but the state fetches are not cancelled or sequenced. If the first response comes back last, `HandleSuccess` writes ascending rows while the header and state say descending. The filter dropdowns already have this race, but header double-clicks are a likelier trigger.
- Suggestion: Optional. Have HandleSuccess drop a response whose query (SortBy/SortDescending/Page/filters) no longer matches state, or file a follow-up for all admin list fetches.
- Status: open
