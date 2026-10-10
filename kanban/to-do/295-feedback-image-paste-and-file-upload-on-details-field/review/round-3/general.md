# Round 3 — general
**Date:** 2026-10-10
**Scope:** Fix delta `git diff 3d8bdac..HEAD` (commit 2c0de4527, M11–M13). I checked that M11, M12, and M13 are fixed and looked for new correctness or security defects in this delta only. M1–M10 were verified in round 2, and this delta does not regress them.

## Summary

M11, M12, and M13 are fixed. The code matches the implementer's notes.

- **M11:** Rollback now clears links by item id, not from a local list. The link phase and rollback use `CancellationToken.None`. The two rollback steps run separately and each logs its own failure.
- **M12:** Expiry can no longer be cancelled partway. Each blob delete is best-effort.
- **M13:** Submit lists every unavailable id. The client removes those files from the draft, the previews, and the body.

I found one new nit (Issue 1). When the rollback itself fails on the lost-race path, the failure is logged but the caller still gets a plain 400. The guide's "never keeps only some of its files" claim is then too strong.

The client JSON-array parse (`UnavailableIds`, JsonElement branch) has no automated test. Round 2 already accepted this gap. The parse matches how `SharedProblemDetails` deserializes: `[JsonExtensionData]` turns the value into a `JsonElement`, and there is no source-generated resolver that could drop the `List<Guid>` on write.

## Gate re-run

| Gate | Result |
|------|--------|
| `./bin/dev build` | Build succeeded, 0 warnings, 0 errors |
| web-jaribu-tests `--filter-class Features.Feedback` | 37/37 passed |
| web-infrastructure-tests `--filter-class Feedback_Model_Mapping_` | 3/3 passed |
| web-infrastructure-tests `--filter-class Feedback_Postgres_Persistence` | 2/2 passed (Testcontainers Postgres ran, not skipped) |
| foundation-contracts-tests `--filter-class HttpApiService_GetResponse` | 14/14 passed |
| `ganda repo audit` | "Repository passes all audit checks." |

## Verification of round-2 fixes

### M11 — fixed
- Rollback works by item id. EF `UnlinkAllAsync` is `ExecuteUpdate WHERE FeedbackItemId = @itemId` (ef-feedback-attachment-store-infrastructure.cs:96-104). The in-memory version loops under `Gate` (in-memory-feedback-attachment-store-application.cs:94-108). The interface is at i-feedback-attachment-store-application.cs:52-53.
- `CancellationToken.None` is used after the insert. `TryLinkAsync` uses it (submit-feedback-handler-application.cs:176), and so do both rollback calls (:198, :207). `LinkAllOrRollBackAsync` no longer takes the request token (:166-169).
- The rollback steps are independent. Unlink and item removal each sit in their own try/catch and log `LogRollbackFailed` (:196-212). `RollBackAsync` cannot throw now, so the `catch { …; throw; }` at :185-189 keeps the original exception.
- Blob cleanup: unlink runs before the delete, so the cascade (ON DELETE CASCADE) only fires when unlink itself failed. That case is logged and recorded in the Design region (:16-25). The handler has no blob store, so blobs are not cleaned up in that case. I accept that residual.
- Tests: `Submit_Should_UndoALinkThatCommittedThenThrewAndKeepTheError` and `Submit_Should_StillRemoveTheItemWhenUnlinkFails` (feedback-attachment-tests.cs). The live Postgres test now links two rows, checks that `UnlinkAllAsync(other)` is a no-op, and checks that `UnlinkAllAsync(item)` clears both (feedback-postgres-persistence-tests.cs:76-84).

### M12 — fixed
- Expiry is called with `CancellationToken.None` (upload-feedback-attachment-handler-application.cs:94-96). The EF per-row `ExecuteDeleteAsync` loop gets that token, so it cannot be cancelled partway (ef-feedback-attachment-store-infrastructure.cs:120-126).
- Each blob delete has its own try/catch that logs `LogExpiredBlobDeleteFailed` (Warning, with the storage key) and moves on to the next blob (upload handler :97-107; logger :34-40).
- Test: `Upload_Should_SucceedWhenAnExpiredBlobCannotBeDeleted`.
- A database error, not a cancellation, partway through the EF loop still loses the list of rows already deleted. That case is outside M12, which was about cancellation, and I did not raise it.

### M13 — fixed
- Server: the pre-check collects every unavailable id (submit handler :114-135). The lost-race path reports its id (:144-149). `AttachmentUnavailable(ids)` puts the list under `unavailableAttachmentIds` and the detail says the upload expired (feedback-problems-application.cs:66-81). The key constant is in submit-feedback-contracts.cs:32-33.
- Client state: `HandleError` removes those ids from `DraftAttachments` before the base notification (feedback-state.submit-feedback.cs:113-124). `UnavailableIds` reads either a JSON array or an in-process list (:127-151).
- Page: `SubmitAsync` snapshots the drafted ids. After a failed submit it removes the previews and markdown lines of the dropped ids (FeedbackListPage.razor:225, :241-250). After a successful submit it returns early (:238).
- Test: `Submit_Should_ListEveryUnavailableAttachment`. The client path has no test. Playwright is forbidden on this host.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/features/feedback/submit-feedback/submit-feedback-handler-application.cs:146-149, 194-212; documentation/developer/guides/feedback-attachments.md (submit paragraph)
- Description: On the lost-race path, `RollBackAsync` now swallows and logs its own failures, and the handler then returns the normal `AttachmentUnavailable([lostId])` 400. Before this delta, a failing `RemoveAsync` threw and the caller got a 500. Now, if the item removal fails, the item stays filed. If unlink also fails, the item keeps the files it already linked. The client is still told the filing was refused. It drops the lost id and the user submits again, which makes a second item. The guide's sentence "An item never keeps only some of its files" is not true in that case. The failure is logged at Error, so it can be seen, and it needs two store failures in a row.
- Suggestion: Pick one:
  - Keep the behaviour and soften the guide and Design text, for example "unless the rollback itself fails, which is logged".
  - Have `RollBackAsync` return whether it fully succeeded. If it did not, return a 500-class problem on the lost-race path instead of the "attach again" 400.
- Status: open
