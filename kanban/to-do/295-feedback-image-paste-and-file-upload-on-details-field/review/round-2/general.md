# Round 2 — general
**Date:** 2026-10-10
**Scope:** Fix commit 3d8bdac (`git diff 866d6e724..HEAD`) against round-1 findings M1–M10, plus a scan of the fix for new correctness and security defects. Read-only review. Gates re-run in the foreground. `dev run`, AppHost, and Playwright were not run.

## Summary

All ten round-1 findings are fixed in the code. Each fix was checked at the cited lines (see merged.md). The new conditional-write store methods, the paste stream path, the Kestrel 413 handling, the startup warning, and `DecodeFileNameHeader` are correct for the paths they target. The live Postgres test exercises the `ExecuteUpdate`/`ExecuteDelete` statements and passed against a real container.

The fix adds three smaller problems, all on failure paths. None blocks the change:
- Submit rollback undoes only the links it recorded in memory, and the item's foreign key is `ON DELETE CASCADE`. A link that committed but threw, or an unlink that throws, can delete an attachment row with its item or leave a half-linked item (Issue 1).
- Expiry deletes rows before blobs, so if a blob delete fails, the remaining blobs are orphaned (Issue 2).
- A draft left open longer than `PendingLifetime` loses its earlier files on the next upload, and the whole submit then fails (Issue 3).

## Gate re-run (results)

| Gate | Result |
|------|--------|
| `./bin/dev build` (`dev` is not on PATH in this shell; ran the repo's binary) | Pass. 0 Warning(s), 0 Error(s) |
| `tests/container-apps/web/web-jaribu-tests`: `dotnet test -- --filter-class Features.Feedback` | Pass. 33/33, 0 failed, 0 skipped |
| `tests/container-apps/web/web-infrastructure-tests`: `dotnet test -- --filter-class Feedback_Model_Mapping_` | Pass. 3/3 |
| `tests/foundation/foundation-contracts-tests`: `dotnet test -- --filter-class HttpApiService_GetResponse` | Pass. 14/14 |
| Extra: `web-infrastructure-tests` `--filter-class Feedback_Postgres_Persistence` (includes `Attachment_store_links_conditionally_and_expires_pending_rows`) | Pass. 2/2, ran live (no `[SKIP]`) |
| `ganda repo audit` (worktree root) | Pass. "Repository passes all audit checks." |

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/features/feedback/submit-feedback/submit-feedback-handler-application.cs:141-181
- Description: `RollBackAsync` (:173-181) unlinks only the ids in the local `linked` list, then calls `FeedbackStore.RemoveAsync` (EF `ExecuteDelete`). The attachment FK to the item is `OnDelete(DeleteBehavior.Cascade)` (feedback-attachment-entity-type-configuration-infrastructure.cs:40-43; migration `20261010075816_AddFeedbackAttachments.cs:38`). Two failure paths go wrong:
  - `TryLinkAsync` takes the request `cancellationToken` (:154). If its `UPDATE` commits and the call still throws (a client disconnect or command timeout after the server ran the statement), that id is not in `linked`. The item delete then cascades and removes the attachment row. Its blob is never deleted, and the file is gone from the user's draft.
  - If `UnlinkAsync` throws during rollback, `RemoveAsync` never runs. The item stays filed with some of its files, which is the state M6 was meant to prevent. The rollback exception also replaces the original one in the `catch` path (:164-168).
- Suggestion: In rollback, clear every row linked to the item (`WHERE FeedbackItemId = @itemId`) instead of using the in-memory list. Pass `CancellationToken.None` to the link phase once the item is inserted, the same way rollback already does. Wrap each rollback step so one failure does not skip the item delete, and log it.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/features/feedback/upload-feedback-attachment/upload-feedback-attachment-handler-application.cs:76-82
- Description: Expired rows are deleted first (`RemoveExpiredUnlinkedAsync`), then their blobs one at a time. If `BlobStore.DeleteAsync` throws (for example a transient Azure error), the upload returns 500. The remaining blobs in the list have no row any more, so nothing will ever delete them. The EF implementation's per-row loop (ef-feedback-attachment-store-infrastructure.cs:120-135) also passes the request token. If the request is cancelled partway through, rows already deleted are never returned, so their blobs are orphaned too.
- Suggestion: Catch and log each blob delete and continue, so housekeeping never fails the user's upload. Pass `CancellationToken.None` to the per-row deletes, or return the rows deleted so far.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/features/feedback/upload-feedback-attachment/upload-feedback-attachment-handler-application.cs:75-82 (with submit-feedback-handler-application.cs:105-110)
- Description: Expiry counts from the upload time, not from the last activity on the draft. Suppose a user leaves the form open for more than 24h (`PendingLifetime`, feedback-attachment-rules-contracts.cs:31) and then adds one more file. That upload deletes the earlier files, but `FeedbackState.DraftAttachments` and the markdown links in the body still point at them. Submit then refuses the whole item with `AttachmentUnavailable`, and the message does not say which file is missing. This trade-off follows from the M3 fix. The guide records the lifetime but not this failure mode.
- Suggestion: Handle it either way:
  - Accept the behavior and record it in the handler's Design region and in the guide.
  - Have the client drop draft entries for ids the server no longer knows, using the remove 404 or a failed submit, and tell the user.
- Status: open
