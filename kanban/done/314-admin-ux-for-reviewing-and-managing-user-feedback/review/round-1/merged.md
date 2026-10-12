# Round 1 — merged findings
**Date:** 2026-10-12
**Sources:** general-server, general-spa (roster axis: general, split by area; effort 3)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/feedback/download-feedback-attachment/download-feedback-attachment-contracts.cs:18 (and `-endpoint-server.cs:27`)
- Description: The download route kept `Policies(feedback.file.self)` while the handler moved to owner-or-`admin.feedback.read`. A role holding `admin.feedback.read` without the filing permission was 403'd before the handler ran, which contradicts decision 4.
- Suggestion: Gate the route on the human schemes only and let the handler decide.
- Source: general-server
- Disposition notes: The route policy was removed from both the contract attribute and the hand-written endpoint, and the Design regions were updated. New integration test `Ok_Download_Given_Administrator_With_Feedback_Read_And_No_File_Self`: the filer gets 200, a stranger Member gets 404, and an admin with only admin.access + admin.feedback.read gets 200 with the correct bytes. web-server-integration `Returns_` 140/140.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/feedback/ef-feedback-store-infrastructure.cs:106 / :69; tests/container-apps/web/web-infrastructure-tests/feedback-postgres-persistence-tests.cs
- Description: The new EF store paths (UpdateAsync append-only insert, stale Version → false, ListAllAsync filters and paging) had no live Postgres coverage.
- Suggestion: Extend the Postgres persistence tests.
- Source: general-server
- Disposition notes: Added `Update_appends_the_thread_and_rejects_a_stale_copy` and `ListAll_filters_by_status_kind_and_window_and_pages_newest_first` (Testcontainers). Writing them exposed **M9**.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/feedback/list-feedback/list-feedback-handler-application.cs:73
- Description: `(page - 1) * pageSize` could overflow because Page had no upper bound.
- Suggestion: Bound Page.
- Source: general-server
- Disposition notes: Added `ListFeedback.MaxPage` (the offset fits in an int). The validator now uses InclusiveBetween(1, MaxPage) and the handler clamps as well. Validator test cases added; triage runfile 28/28.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/feedback/in-memory-feedback-store-application.cs:65
- Description: The tie-break order for identical FiledAt differs between stores (.NET Guid order vs Postgres uuid order).
- Suggestion: Document it.
- Source: general-server
- Disposition notes: Documented in the in-memory store's Design region. Each store is stable on its own. There is no code change because no shared ordering exists without an extra column.

### M5 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/feedback-admin-state/feedback-admin-state.open-feedback-for-admin.cs:36
- Description: Opening item B left item A's body and manage controls visible under B's URL, indefinitely on a non-404 failure. The page drafts also carried over.
- Suggestion: Clear Current/LoadError on open of a different id, and reset the drafts on id change.
- Source: general-spa
- Disposition notes: Fixed in GetRequest and in FeedbackAdminItemPage.OnParametersSetAsync. New state test `Open_Of_Another_Item_Clears_The_Previous_One_When_It_Fails`.

### M6 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:31
- Description: A failed fetch left the previous rows under the new filter.
- Suggestion: Clear the rows or expose a list error.
- Source: general-spa
- Disposition notes: FetchFeedbackList.HandleError clears the rows and sets `ListLoadError`, which the page renders. Initialize clears it. New state test `Failed_Fetch_Clears_The_Rows_And_Sets_A_List_Error`.

### M7 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:116
- Description: The pager text read "Page 1 of 25 items", and the pager was hidden on an empty past-the-end page.
- Suggestion: Reword, and render the pager when TotalCount > 0 or Page > 1.
- Source: general-spa
- Disposition notes: The text now reads "Page N · X items", and the pager renders when `TotalCount > 0 || HasPreviousPage`.

### M8 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/feedback-admin-state-tests.cs:42
- Description: SPA coverage gaps around paging, filter reset, and the M5/M6 behaviors.
- Suggestion: Add state tests.
- Source: general-spa
- Disposition notes: Added `Filter_Change_Resets_Page_And_Paging_Flags_Follow_The_Total`, plus the M5/M6 tests (7/7). The nav and AuthorizeView gates are declarative markup on the existing `admin.feedback.read` / `.manage` policies, and the server-side denial is proven in the web-server authorization tests, so no bUnit harness was added for them.

### M9 — Severity: bug — Status: fixed (found during the M2 fix)
- File: source/container-apps/web/features/feedback/ef-feedback-store-infrastructure.cs:32
- Description: `AddAsync` left the filed item tracked. A later `UpdateAsync` in the same scoped DbContext then threw "another instance with the same key value is already being tracked". The in-memory store allows that sequence, so the two store implementations disagreed.
- Suggestion: Clear the tracker after Add, the same way Update already does.
- Source: orchestrator (surfaced by the new M2 Postgres test)
- Disposition notes: AddAsync now clears the change tracker in `finally`, and the Design region is updated. web-infrastructure-tests 70/70.

## Duplicates / conflicts

- None. The server and SPA scopes did not overlap.
