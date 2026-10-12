# Round 1 — general-server
**Date:** 2026-10-12
**Scope reviewed:** `git diff master...HEAD` server side: source/ except projects/web-spa (feedback domain, store ports and EF/in-memory stores, handlers, contracts, migration 20261012115946_AddFeedbackTriage, permissions/seed), plus tests/ except web-spa-integration-tests.

## Summary
The domain invariants, filer/admin view separation, endpoint policies, email rules and migration are sound. Admin endpoints carry the right read/manage policy, filer reply is owner-only (404 otherwise), filer views drop internal notes and author ids, and email is best-effort after commit. One real authorization gap: attachment download for admins is still gated at the route by `feedback.file.self`, so `admin.feedback.read` alone does not deliver decision 4. The EF store paths (UpdateAsync, ListAllAsync) have no Postgres-backed test.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/features/feedback/download-feedback-attachment/download-feedback-attachment-contracts.cs:18
- Description: The route policy is still `PermissionIds.FeedbackFileSelf`; only the handler's in-body check was changed to `AdminFeedbackRead`. Decision 4 says `admin.feedback.read` covers attachment download, but a principal whose roles grant `admin.feedback.read` without `feedback.file.self` (any custom role built in the roles editor) is rejected 403 before the handler runs. The new authz test masks this: it sets the read-only Administrator role to `[AdminAccess, AdminFeedbackRead, FeedbackFileSelf]` and never calls the download route.
- Suggestion: Make the route policy accept either permission (for example a policy that is satisfied by `feedback.file.self` or `admin.feedback.read`), or drop the route policy to authenticated and keep the handler's owner-or-read check. Add a test: role with only `admin.access` + `admin.feedback.read` can download another principal's attachment; a role with neither gets 403/404.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/features/feedback/ef-feedback-store-infrastructure.cs:106 (UpdateAsync) and :69 (ListAllAsync); tests/container-apps/web/web-infrastructure-tests/feedback-postgres-persistence-tests.cs
- Description: The new EF-only logic is untested: attach-detached + insert only entries above the stored max sequence, Version conflict returning false, duplicate (item, sequence) key returning false, owned-entity mapping/migration round trip, and ListAllAsync status/kind/date/paging translation (including `List<FeedbackStatus>.Contains` through the string value converter). All handler and triage tests run on the in-memory store, which has no Version or key-conflict behavior, so the 409 "Changed" path is never exercised against a real store. The persistence test file was not touched in this diff.
- Suggestion: Extend the Postgres persistence tests: round-trip an item with a status change, reply and note; two stale copies updated in turn (second returns false); a status-filtered, paged ListAllAsync that matches the in-memory store's result.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/features/feedback/list-feedback/list-feedback-handler-application.cs:73
- Description: `(page - 1) * pageSize` is unchecked int arithmetic. The validator only bounds `Page >= 1`, so `Page=2147483647&PageSize=100` overflows to a negative `Skip`; Npgsql then fails with a negative OFFSET (500) for an admin-only endpoint.
- Suggestion: Add an upper bound on `Page` in the validator, or compute the offset as long and clamp.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/features/feedback/ef-feedback-store-infrastructure.cs:83 vs source/container-apps/web/features/feedback/in-memory-feedback-store-application.cs:65
- Description: Tie-break ordering differs between stores: EF orders `ThenBy(Id)` in Postgres uuid byte order, in-memory orders `ThenBy(Id.Value)` in .NET Guid order. Items with identical `FiledAt` can page in a different order per store, so "EF and in-memory agree" only holds without ties.
- Suggestion: Acceptable to leave; if exact agreement matters, tie-break on a value both order the same (for example `Id.Value.ToString()` ordinal in memory is not equal to uuid order either, so document it in the Design region).
- Status: open
