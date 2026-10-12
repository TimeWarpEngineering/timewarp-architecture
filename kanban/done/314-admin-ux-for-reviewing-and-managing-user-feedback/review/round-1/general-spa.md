# Round 1 — general-spa
**Date:** 2026-10-12
**Scope reviewed:** `git diff master...HEAD` for web-spa (features/admin/feedback state + pages, features/feedback filer state/pages, NavMenu, _Imports) and web-spa-integration-tests (feedback-admin-state-tests, action-catalog, command-palette, page-registry edits).

## Summary
The slice follows the sibling admin conventions (State actions only, OutcomeNotification toasts, isolated CSS, no `<style>`, manage controls inside `AuthorizeView` on `admin.feedback.manage`, nav entry gated on `admin.feedback.read`). The filer page only renders the server-filtered `Activity`, with no internal-note path. No authorization or leak defects found. Remaining issues are stale-display edge cases on the admin pages and test gaps.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/feedback-admin-state/feedback-admin-state.open-feedback-for-admin.cs:36
- Description: `OpenFeedbackForAdmin` does not clear `Current`/`LoadError` at request start. When the route changes from item A to item B (same component instance, `OnParametersSetAsync`), A's title, body, thread and manage controls (status, reply, note sections have no loading spinner for the open action) remain visible under B's URL until B loads. On a non-404 failure (403/500) A stays displayed indefinitely. The page-local `ReplyDraft`, `NoteDraft` and `PendingStatus` also survive the id change, so a half-typed reply for A can be sent on B. Also, a `LoadError` from a previous 404 persists until success.
- Suggestion: In `GetRequest` set `Current = null; LoadError = null;` (the page already guards on `Current is null`), and reset the drafts when `FeedbackItemId` changes in `OnParametersSetAsync`.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:31
- Description: `ApplyFilterAsync` / `GoToPageAsync` mutate filter/page state first and then fetch. If the fetch fails (problem details), `Items`/`TotalCount` still hold the previous query's rows while the selects/checkbox/`Page` already show the new filter, so the table and filter disagree with no inline indication (only a toast).
- Suggestion: Clear `ItemList` at fetch start, or only commit filter/page to state on fetch success (or expose a list `LoadError`).
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:116
- Description: Text reads "Page 1 of 25 items" (mixes page index with item count). Also the pager lives inside the non-empty branch, so if a page past the end returns zero rows (e.g. items closed in another session while on page 3) the "No feedback matches" message shows with no Previous button to recover.
- Suggestion: Show "Page N · X items", and render the pager when `TotalCount > 0` or `Page > 1`.
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/feedback-admin-state-tests.cs:42
- Description: Coverage gaps: no test for `SetPage` / `HasNextPage`/`HasPreviousPage` (filter change resetting Page to 1), none for the filer-side `ReplyAsFiler` / `OpenFeedback` setting `CurrentStatus`/`CurrentActivity`/`LastReplySucceeded`, and none for the nav gate (admin.feedback.read hides the entry) or the `AuthorizeView` manage gate. Command-palette test only checks the page is denied for a non-admin, not allowed for a read-only admin.
- Suggestion: Add state tests for paging and filer reply, and a palette/nav case for a principal holding `admin.feedback.read` only.
- Status: open
