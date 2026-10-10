# Round 2 — merged findings
**Date:** 2026-10-10
**Sources:** general

## Counts

M1–M13.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 5 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 4 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: .template.config/template.json:104-105
- Description: The `(!postgres)` exclude list did not include the two new EF attachment files, so a postgres-off app would not compile.
- Source: general (round 1)
- Disposition notes: Verified. `ef-feedback-attachment-store-infrastructure.cs` and `feedback-attachment-entity-type-configuration-infrastructure.cs` are now in the exclude list (:104-105), next to `ef-feedback-store-infrastructure.cs` (:102). Template smoke was not re-run this round. The implementer's SmokeNoPostgres result is the evidence.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/feedback/feedback-attachment-http-application.cs:43-53
- Description: A non-ASCII name in `filename=` made Kestrel refuse the response header.
- Source: general (round 1)
- Disposition notes: Verified. `string.Create` maps every character outside `' '..'~'` to `_` (:43-50). Quotes, CR/LF, and backslash are stripped beforehand. `filename*=` carries `Uri.EscapeDataString(safe)` (:52-53). Covered by `Disposition_Should_UseAnAsciiFallbackForANonAsciiName` (feedback-attachment-tests.cs:143).

### M3 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/feedback/upload-feedback-attachment/upload-feedback-attachment-handler-application.cs:75-90
- Description: Pending uploads lost with a browser draft held the 8-file cap forever.
- Source: general (round 1)
- Disposition notes: Verified. Before counting, upload calls `RemoveExpiredUnlinkedAsync(owner, now - PendingLifetime)` (24h, feedback-attachment-rules-contracts.cs:31), then deletes those blobs (:76-82). The candidate query is scoped to the caller (ef store `OwnerPrincipalId == ownerPrincipalId`). Each delete is conditional on `Id` and `FeedbackItemId == null`, so a row linked by a racing submit is not deleted. The in-memory store does the same under `Gate`. Tests: `Upload_Should_RefuseMoreThanThePendingCap` and `Upload_Should_DeleteExpiredPendingFilesBeforeCountingTheCap`. The live Postgres test passed. See M12 and M13 for new failure-path findings.

### M4 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/feedback/pages/FeedbackListPage.razor:92-95
- Description: The pasted image's preview and markdown link were not rendered after a JS-invoked paste.
- Source: general (round 1)
- Disposition notes: Verified in code. `await InvokeAsync(StateHasChanged)` runs after `UploadOneAsync` (:95). The early-return paths only add notifications, and those render through their own state. Not proven in a browser, because Playwright is forbidden on this host.

### M5 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/feedback-paste.ts:98-120; FeedbackListPage.razor:72-105
- Description: A pasted file was sent as a `byte[]` over SignalR, with no size check before reading and no error handling.
- Source: general (round 1)
- Disposition notes: Verified.
  - JS refuses a file when `file.size > maxBytes` (:99) and then sends `DotNet.createJSStreamReference(file)` (:107). Blob input is supported.
  - `send` catches errors and reports them through `ReceivePasteRejected`. `reject` has its own try/catch, so nothing is left unhandled.
  - .NET disposes the `IJSStreamReference` with `await using` and checks `Length` before opening (:79). It reads with `OpenReadStreamAsync(maxAllowedSize: MaxBytes)` (:85). The inner stream is disposed before the reference.
  - `maxBytes` is passed from `FeedbackAttachmentRules.MaxBytes` (:52).
  - Not proven on a live InteractiveServer circuit.

### M6 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/feedback/submit-feedback/submit-feedback-handler-application.cs:121-181; ef-feedback-attachment-store-infrastructure.cs:81-106
- Description: Link was check-then-act and not atomic, so a race could leave an item with only some of its attachments.
- Source: general (round 1)
- Disposition notes: Verified.
  - `TryLinkAsync` is a single `ExecuteUpdate WHERE Id AND OwnerPrincipalId AND FeedbackItemId IS NULL` and returns `affected == 1` (ef :88-93). The in-memory version runs under `Gate`.
  - `UnlinkAsync` is conditional on the current item.
  - Submit inserts the item, then links. On failure it unlinks, removes the item, and returns 400. On an exception it rolls back and rethrows.
  - `FindAsync` is `AsNoTracking`, so no stale tracked row exists.
  - Tests: `Submit_Should_RollBackWhenALinkLosesARace` and `Link_Should_SucceedOnceAndOnlyForTheOwner`. The live Postgres `Attachment_store_links_conditionally_and_expires_pending_rows` passed (not skipped).
  - The rollback has a remaining weak spot on failure paths, tracked as M11.

### M7 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/feedback/upload-feedback-attachment/upload-feedback-attachment-endpoint-server.cs:31-48
- Description: The handler's 413 problem could not be reached because Kestrel's limit equalled the cap.
- Source: general (round 1)
- Disposition notes: Verified.
  - `MaxRequestBodySize(MaxBytes + 1L)` (:31). The binder hands the body stream to the handler without reading it, so `FeedbackAttachmentContent.ReadAsync` sees byte MaxBytes+1 and returns `TooLarge()`.
  - A larger body, or a Content-Length over the limit, throws `BadHttpRequestException` inside `base.HandleAsync`. `BaseFastEndpoint.HandleAsync` has no catch, so the exception reaches the override's filter (status 413 and `!Response.HasStarted`, :41-42).
  - The override writes the same `application/problem+json` shape that `BaseFastEndpoint` uses.

### M8 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/feedback/feedback-attachment-blob-module-infrastructure.cs:36, 78-117
- Description: An in-memory blob store running next to durable rows failed silently.
- Source: general (round 1)
- Disposition notes: Verified. The hosted service is registered only on the in-memory branch (:36). It resolves `IFeedbackAttachmentStore` in a scope. This is always registered: in-memory by `InMemoryFeedbackStoresModule`, EF by `PostgresDbModule`. It warns when the row store is not the in-memory one, or outside Development (:107-110). It only warns.

### M9 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/feedback/feedback-attachment-tests.cs:143-245
- Description: Several cases had no tests.
- Source: general (round 1)
- Disposition notes: Verified. All four cases are tested:
  - `Upload_Should_RefuseMoreThanThePendingCap` (:180)
  - `Remove_Should_Return404ForAnotherPrincipalsPendingAttachment` (:235)
  - `Disposition_Should_UseAnAsciiFallbackForANonAsciiName` (:143)
  - `HeaderName_Should_DecodeThenStoreOnlyTheLastSegment` (:157) and `HeaderName_Should_RejectADecodedLineBreak` (:170), both through `FeedbackAttachmentHttp.DecodeFileNameHeader`

  `DecodeFileNameHeader` is the same `Uri.UnescapeDataString` as before. A malformed escape is left as-is and does not throw. `Normalize` still runs afterwards.

### M10 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/feedback/get-feedback/get-feedback-contracts.cs:106-107
- Description: The mock sample attachment was an image whose `img src` went to the real server.
- Source: general (round 1)
- Disposition notes: Verified. The sample is now `mock.txt` / `FeedbackAttachmentRules.Text`. The Design region records why.

### M11 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/feedback/submit-feedback/submit-feedback-handler-application.cs:141-181
- Description: Rollback unlinks only the ids in the local `linked` list, then deletes the item with `ExecuteDelete`. The attachment FK is `ON DELETE CASCADE` (feedback-attachment-entity-type-configuration-infrastructure.cs:40-43). Two failure paths go wrong:
  - A `TryLinkAsync` that commits but throws (it uses the request token) is not in the list. The item delete then cascade-deletes that attachment row, and its blob is orphaned.
  - An `UnlinkAsync` that throws skips `RemoveAsync`. The item is left filed with some of its files, and the original exception is lost.
- Source: general (Issue 1)
- Disposition notes: Suggested fix:
  - In rollback, unlink by item id (`WHERE FeedbackItemId = @itemId`).
  - Use `CancellationToken.None` for the link phase once the item is inserted.
  - Make each rollback step independent, and log failures.
  - **Fixed:**
    - `IFeedbackAttachmentStore.UnlinkAsync(id, itemId)` is replaced by `UnlinkAllAsync(itemId)`. In EF this is `ExecuteUpdate ... WHERE FeedbackItemId = @itemId`; the in-memory store does it under its lock. Rollback no longer depends on an in-memory list, so a link that committed and then threw is undone too.
    - After `FeedbackStore.AddAsync`, linking and both rollback steps run with `CancellationToken.None`.
    - `RollBackAsync` runs unlink and item removal in separate try/catch blocks, each logging `LogRollbackFailed` (Error). The item is removed even if unlink throws, and the original exception is rethrown.
    - Blob cleanup after a cascade: because unlink runs before the delete, a cascade can only happen when the unlink step itself failed. That case is logged and recorded in the Design region, not cleaned up. The submit handler has no blob store dependency, and cleaning up there would need one.
    - Tests: `Submit_Should_UndoALinkThatCommittedThenThrewAndKeepTheError` and `Submit_Should_StillRemoveTheItemWhenUnlinkFails`. The live Postgres test now checks `UnlinkAllAsync` with two linked rows and with an unrelated item.

### M12 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/feedback/upload-feedback-attachment/upload-feedback-attachment-handler-application.cs:76-82
- Description: Expiry deletes rows, then blobs. If a blob delete throws, the upload fails with 500 and the remaining blobs are orphaned. A cancellation during the EF per-row delete loop (ef-feedback-attachment-store-infrastructure.cs:120-135) also loses the list of rows already deleted.
- Source: general (Issue 2)
- Disposition notes: Suggested fix: catch and log each blob delete and continue. Do not cancel the per-row delete loop partway.
  - **Fixed:** Upload now calls `RemoveExpiredUnlinkedAsync` with `CancellationToken.None`, so the EF per-row loop cannot be cancelled partway. Each expired blob delete is in its own try/catch that logs `LogExpiredBlobDeleteFailed` (Warning, with the storage key) and continues, so the upload succeeds. The handler now takes `ILogger<Handler>`. A blob that fails to delete is orphaned; its key is in the log. Recorded in the Design region and the guide. Test: `Upload_Should_SucceedWhenAnExpiredBlobCannotBeDeleted`.

### M13 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/feedback/upload-feedback-attachment/upload-feedback-attachment-handler-application.cs:75-82
- Description: A draft open longer than `PendingLifetime` loses its earlier uploads on the next upload. The client draft and the markdown links still reference them, and submit then fails as a whole with `AttachmentUnavailable`.
- Source: general (Issue 3)
- Disposition notes: Either record this trade-off in the Design region and the guide, or have the client drop unknown ids and tell the user.
  - **Fixed (client drops unknown ids):**
    - Submit's pre-check now collects every unavailable id instead of stopping at the first. A lost link race reports its id too. The 400 problem lists the ids under `SubmitFeedback.UnavailableAttachmentIdsExtension` (`unavailableAttachmentIds`). Its detail says pending uploads expire after 24 hours and asks the user to attach the file again.
    - `SubmitFeedbackActionSet.Handler.HandleError` removes those ids from `DraftAttachments` before publishing the usual problem notification. It reads either a JSON array or an in-process list.
    - `FeedbackListPage.SubmitAsync` then removes the dropped files' previews and markdown links, so the next submit files the rest.
    - Recorded in the Design regions (contract, upload handler, state action) and the guide.
    - Server test: `Submit_Should_ListEveryUnavailableAttachment`; the lost-race test also checks the listed id. The client path has no automated test: the JSON parse lives in web-spa, and the browser proof is Playwright, which is forbidden on this host.

## Duplicates / conflicts

- Single reviewer; none. M11–M13 are failure-path follow-ups to the M3 and M6 fixes. None of them reopens M3 or M6: the race and lockout scenarios from round 1 are fixed.
