# Round 3 — merged findings
**Date:** 2026-10-10
**Sources:** general (round 3); ledger carried from round-2/merged.md

## Counts

M1–M14.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 5 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 5 | 0 |

## Issues

### Resolved prior (verified in round 2, not regressed by 2c0de4527)
- M1 — bug — fixed. The `(!postgres)` exclude list now has the EF attachment files (.template.config/template.json:104-105).
- M2 — bug — fixed. The `filename=` fallback is ASCII-only (feedback-attachment-http-application.cs:43-53).
- M3 — bug — fixed. Expired pending uploads are deleted before the cap is counted. The delta touches only its failure handling (see M12).
- M4 — bug — fixed. `StateHasChanged` runs after a JS paste (FeedbackListPage.razor).
- M5 — bug — fixed. Pastes use a stream reference with size checks and error handling.
- M6 — suggestion — fixed. `TryLinkAsync` is a conditional write and failure is compensated. The delta strengthens the compensation (see M11).
- M7 — suggestion — fixed. The handler's 413 can be reached (`MaxRequestBodySize` is MaxBytes+1).
- M8 — suggestion — fixed. A warning is logged when the in-memory blob store runs next to durable rows.
- M9 — nit — fixed. The missing test cases were added.
- M10 — nit — fixed. The mock sample attachment is `mock.txt`.

### M11 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/feedback/submit-feedback/submit-feedback-handler-application.cs:166-212; ef-feedback-attachment-store-infrastructure.cs:96-104; in-memory-feedback-attachment-store-application.cs:94-108
- Description: Rollback cleared links only from a local list and used the request token. Its steps depended on each other.
- Source: general (round 2 Issue 1)
- Disposition notes: Verified in round 3.
  - `UnlinkAllAsync(itemId)` clears links by item id, so a link that committed and then threw is undone.
  - `CancellationToken.None` is used for linking (:176) and for both rollback steps (:198, :207).
  - The two steps are in independent try/catch blocks that log `LogRollbackFailed`, and the original exception is rethrown (:185-189).
  - Unlink runs before the delete, so the cascade only happens if unlink failed. That case is logged and recorded in the Design region.
  - Tests: two new Jaribu tests, and the live Postgres test now covers `UnlinkAllAsync`.
  - The swallowed-rollback residual on the lost-race path is tracked as M14.

### M12 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/feedback/upload-feedback-attachment/upload-feedback-attachment-handler-application.cs:94-107
- Description: One failed blob delete during expiry failed the upload and orphaned the remaining blobs. A cancellation could interrupt the per-row delete loop.
- Source: general (round 2 Issue 2)
- Disposition notes: Verified in round 3. Expiry runs with `CancellationToken.None`, and the EF loop gets that token. Each blob delete is best-effort and logs a Warning with the storage key. Test: `Upload_Should_SucceedWhenAnExpiredBlobCannotBeDeleted`.

### M13 — Severity: nit — Status: fixed
- File: submit-feedback-handler-application.cs:114-149; feedback-problems-application.cs:66-81; feedback-state.submit-feedback.cs:113-151; FeedbackListPage.razor:225-250
- Description: When a draft stayed open longer than `PendingLifetime`, its earlier uploads expired and submit failed as a whole.
- Source: general (round 2 Issue 3)
- Disposition notes: Verified in round 3.
  - The 400 lists every unavailable id under `unavailableAttachmentIds`.
  - The client removes those ids from `DraftAttachments`, the previews, and the markdown.
  - The JSON branch matches how `[JsonExtensionData]` deserializes.
  - Server test: `Submit_Should_ListEveryUnavailableAttachment`. The client path has no test because Playwright is forbidden. Round 2 accepted this.

### M14 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/feedback/submit-feedback/submit-feedback-handler-application.cs:146-149, 194-212; documentation/developer/guides/feedback-attachments.md
- Description: On the lost-race path, a rollback failure is logged and swallowed, and the caller still gets the normal "attach again" 400. Before this delta, a failing remove gave a 500.
  - If the item removal fails, the item stays filed. If unlink also fails, the item keeps some of its files.
  - The client drops the lost id, and the user's next submit makes a second item.
  - The guide's "An item never keeps only some of its files" is then not true.
  - It needs two store failures in a row, and it is logged at Error.
- Source: general (round 3 Issue 1)
- Disposition notes: Fixed by the orchestrator using the first option. The guide (`documentation/developer/guides/feedback-attachments.md`) and the submit handler's Design region now say a successful rollback leaves no partly linked item, and that a rollback step which throws is logged at Error and can leave the item filed. A second double-failure path was not worth the extra code for a nit.

## Duplicates / conflicts

- There was one reviewer, so there are no duplicates.
- M14 follows from the M11 fix. It does not reopen M11: the scenarios M11 named are fixed and tested.
