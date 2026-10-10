# Round 1 — merged findings
**Date:** 2026-10-10
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 5 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: .template.config/template.json:102
- Description: The `(!postgres)` exclude list names `ef-feedback-store-infrastructure.cs` and `feedback-item-entity-type-configuration-infrastructure.cs`. It does not name the two new files. `ef-feedback-attachment-store-infrastructure.cs` uses `PostgresDbContext` and `TimeWarp.Architecture.Persistence` (lines 16, 20, 22), which are excluded when postgres is off. `feedback-attachment-entity-type-configuration-infrastructure.cs:22` uses `FeedbackItemEntityTypeConfiguration.SchemaName`, which is also excluded. So `dotnet new timewarp-architecture --postgres false` produces an app that does not compile.
- Suggestion: Add both files to the `(!postgres)` exclude array. Run `dev template-smoke` with postgres off.
- Source: general
- Disposition notes: Added `ef-feedback-attachment-store-infrastructure.cs` and `feedback-attachment-entity-type-configuration-infrastructure.cs` to the `(!postgres)` exclude list. No other new file depends on Postgres (migrations, snapshot, and the DbContext are already under `platform/postgres/**`). `dev template-smoke` passed: SmokeNoPostgres omits both files and builds with 0 warnings and 0 errors.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/feedback/feedback-attachment-http-application.cs:33
- Description: `ContentDisposition` puts `safe` unchanged into `filename="…"`. Only quotes, CR/LF and backslash are removed. The Design comment says "an ASCII filename", but nothing removes non-ASCII characters, and `FeedbackAttachmentNames.Normalize` (feedback-attachment-names-domain.cs:36-42) accepts them. Kestrel refuses non-ASCII response header values when `ResponseHeaderEncodingSelector` is unset, and it is unset here (no match in `source/`). Any file such as `résumé.pdf` or `截图.png` then fails to download with a 500, and its `<img>` on the item page is broken. A default macOS screenshot name also fails, because it contains U+202F before "AM"/"PM".
- Suggestion: Build an ASCII fallback for `filename=`, replacing non-ASCII characters with `_` or `?`. Keep the UTF-8 name only in `filename*=`. `ContentDispositionHeaderValue` with `FileNameStar` does this. Add a test with a non-ASCII name.
- Source: general
- Disposition notes: `FeedbackAttachmentHttp.ContentDisposition` now builds the `filename=` value as an ASCII fallback: every character outside printable ASCII becomes `_`. The UTF-8 name appears only percent-encoded in `filename*=`. New test `Disposition_Should_UseAnAsciiFallbackForANonAsciiName` covers `résumé.pdf` and a macOS screenshot name containing U+202F.

### M3 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/feedback/upload-feedback-attachment/upload-feedback-attachment-handler-application.cs:70
- Description: Upload refuses with 409 once a person has `MaxPerItem` (8) unlinked attachments. The client knows its pending files only through `FeedbackState.DraftAttachments` (web-spa feedback-state.cs:22). That list lives only in browser state, so a reload, closed tab or new device loses it. No endpoint lists pending uploads, and nothing cleans them up or expires them. After 8 uploads that were never filed, the user gets "Too many attachments" on every upload and has no way to clear them. The page check at FeedbackListPage.razor:73/105 still thinks there is room. The blobs also pile up for good.
- Suggestion: Pick one of these:
  - Add a "list my pending attachments" query and rebuild `DraftAttachments` when the page loads.
  - Have upload expire unlinked rows older than some limit, with their blobs.
  - Add a background sweep.
  
  Add a test for the cap, which no test covers today.
- Source: general
- Disposition notes: Added `FeedbackAttachmentRules.PendingLifetime` (24h) and `IFeedbackAttachmentStore.RemoveExpiredUnlinkedAsync`, implemented in both stores. EF deletes row by row with a conditional `ExecuteDelete` and returns only the rows it deleted; the in-memory store does this under its lock. Upload deletes the caller's expired pending rows and their blobs before checking the cap. New tests: `Upload_Should_RefuseMoreThanThePendingCap` and `Upload_Should_DeleteExpiredPendingFilesBeforeCountingTheCap`. The guide is updated. The count and the insert are still two separate calls, so concurrent uploads can go a few files over the cap; this is documented as a soft cap in the handler Design region and the guide.

### M4 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/feedback/pages/FeedbackListPage.razor:65
- Description: `ReceivePastedImage` is a `[JSInvokable]` call from JS. It is not a Blazor event callback, so the page gets no automatic `StateHasChanged` when it finishes. The only re-render is the one TimeWarp.State starts when `UploadFeedbackAttachment` changes state. That render happens inside the `await` at line 133, before `Previews[...]` (line 143) and `AppendMarkdown` (line 146) run. As a result, the pasted image's markdown link and thumbnail do not appear until some later, unrelated render. If the user keeps typing first, `@bind-Value` writes the textarea text back to `Draft.Body` and the link is lost. The Playwright assertion `body.ShouldContain("/api/Feedback/attachments/")` (feedback-attachment-playwright-tests.cs:112-113) probably fails for this reason. Playwright has not run on this host.
- Suggestion: Call `await InvokeAsync(StateHasChanged)` at the end of the paste path. Better, move the body and preview edits into state so the action's own render shows them.
- Source: general
- Disposition notes: After `ReceivePastedImage` adds the preview and the markdown link, it now calls `await InvokeAsync(StateHasChanged)`.

### M5 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/feedback-paste.ts:89
- Description: `send` reads the whole pasted file with `arrayBuffer()` and sends it to .NET as a `byte[]` through `invokeMethodAsync`. Size is checked only afterwards, in .NET (FeedbackListPage.razor:119). The default render mode is `InteractiveAuto` (blazor-settings.cs:17). On a first visit, before WASM is cached, the page runs on an InteractiveServer circuit. There the bytes travel over SignalR, which has a 32 KB default `MaximumReceiveMessageSize`, and nothing raises it (no `AddHubOptions` or `MaximumReceiveMessageSize` in `source/`). A normal screenshot paste then goes over the limit and drops the circuit. The `void send(...)` call also leaves the rejection unhandled, so the user sees nothing.
- Suggestion: Check `file.size` against `MaxBytes` in JS before reading. On the server circuit, stream the file instead of sending a `byte[]`, for example with `DotNetStreamReference` or `IJSStreamReference`. Alternatively, only enable paste when `RendererInfo.Name == "WebAssembly"`. Catch errors in `send` and report them.
- Source: general
- Disposition notes: Chose streaming over a WebAssembly-only gate. With a gate, paste would have been missing on the server circuit of a first InteractiveAuto visit, and the Playwright paste proof could break. JS now passes `DotNet.createJSStreamReference(file)`. .NET receives an `IJSStreamReference`, checks `Length`, and reads it with `OpenReadStreamAsync(maxAllowedSize: MaxBytes)`. `Register` takes `maxBytes` from `FeedbackAttachmentRules.MaxBytes`, and JS refuses a larger file before reading it. Errors from `send` are caught, logged to the console, and sent to the new `[JSInvokable] ReceivePasteRejected`, which shows the existing error notification. Recorded in the Design regions of `feedback-paste.ts` and the page. Not proven in a browser, because Playwright is forbidden on this host.

### M6 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/feedback/submit-feedback/submit-feedback-handler-application.cs:93
- Description: Linking is check-then-act and not atomic:
  - Ownership and "unlinked" are checked at lines 94-107. Then `FeedbackStore.AddAsync` and one `LinkAsync` per attachment each save separately (lines 115-119).
  - If a remove or a second submit runs between the check and the link (a double-click, or two tabs), the item is already filed when `LinkAsync` throws (ef-feedback-attachment-store-infrastructure.cs:87-89, in-memory :76-81, or `Link`'s "already linked"). The user gets a 500 and an item with only some of its attachments.
  - In EF, two submits can both load the row with `FeedbackItemId == null`, and `Version` is not a concurrency token. The later save wins and silently moves the attachment to the second item.
  - Upload has the same race: `CountUnlinkedByOwnerAsync` followed by `AddAsync` lets concurrent uploads go past the cap.
- Suggestion: Wrap the item insert and the links in one transaction. Link with a conditional update, `ExecuteUpdate ... WHERE Id = @id AND OwnerPrincipalId = @owner AND FeedbackItemId IS NULL`, and check the affected row count. Alternatively, make Version a concurrency token for this entity.
- Source: general
- Disposition notes: Replaced `LinkAsync` with `TryLinkAsync(id, owner, itemId)`. EF runs it as a single `ExecuteUpdate ... WHERE Id AND OwnerPrincipalId AND FeedbackItemId IS NULL` and checks the affected row count; the in-memory store checks and writes under a lock. Also added `UnlinkAsync` (conditional on the current item) and `IFeedbackStore.RemoveAsync`. Submit inserts the item first, because the attachment row has a foreign key to it, then links each attachment. If a link fails or a store throws, it undoes the links already made, removes the item, and returns 400, so an item never keeps only some of its files. A single DB transaction would need a unit-of-work port spanning both stores; this is documented in the Design region. New tests: `Submit_Should_RollBackWhenALinkLosesARace` (a racing store decorator), `Link_Should_SucceedOnceAndOnlyForTheOwner`, and a live Postgres test `Attachment_store_links_conditionally_and_expires_pending_rows` (Testcontainers) that exercises the EF statements.

### M7 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/feedback/upload-feedback-attachment/upload-feedback-attachment-endpoint-server.cs:27
- Description: `MaxRequestBodySize(FeedbackAttachmentRules.MaxBytes)` matches the handler's cap exactly, so the handler's `TooLarge()` 413 problem (feedback-attachment-content-application.cs:45-48) can never be reached over HTTP. Kestrel throws `BadHttpRequestException` while the body is being read, or before it if Content-Length is too large. The response is then whatever the exception middleware produces, not the documented `application/problem+json` 413. The rejection is also logged as an unhandled exception. The cap is still enforced, so this is about response shape and logging, not security.
- Suggestion: Set the Kestrel limit a little above `MaxBytes` (for example `MaxBytes + 1`) so the handler returns its 413 problem. Or catch `BadHttpRequestException` in `ReadAsync` and turn it into `TooLarge()`.
- Source: general
- Disposition notes: The endpoint now sets `MaxRequestBodySize(MaxBytes + 1)`. It also overrides `HandleAsync` to catch a 413 `BadHttpRequestException` (Content-Length over the limit, or Kestrel stopping the body first) and write the same `application/problem+json` 413 through `FeedbackAttachmentHttp.TooLarge()`. The cap is still enforced by Kestrel and by the handler.

### M8 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/feedback/feedback-attachment-blob-module-infrastructure.cs:27
- Description: If `FeedbackAttachments:ConnectionString` is empty, the in-memory blob store is used on every host, including a deployed one where Postgres is set up. Rows then survive a restart but their bytes do not. Every download of an older attachment returns 404 (the handler at :71-77 turns the missing blob into NotFound). Nothing warns about this. Each instance also holds up to 5 MiB per file in memory with no limit on filed items. The guide documents the fallback for "unconfigured hosts", but leaving the setting out of a production deployment fails silently.
- Suggestion: Log a startup warning when the row store is EF but the blob store is in-memory. Or fail fast outside Development or mock mode.
- Source: general
- Disposition notes: When the in-memory blob store is chosen, `FeedbackAttachmentBlobModule` also registers a hosted service. At startup it logs a warning when the row store is not `InMemoryFeedbackAttachmentStore` (EF/Postgres) or the environment is not Development. It only warns, so tests and mock mode are unchanged. The guide is updated.

### M9 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/feedback/feedback-attachment-tests.cs:62
- Description: These cases have no tests:
  - The 8-per-person pending cap (`TooManyAttachments`).
  - Remove of another person's pending id returning 404.
  - A non-ASCII name in Content-Disposition (would have caught Issue 2).
  - The `X-File-Name` binder decoding a percent-encoded `../` or `%0d%0a` name.
  
  The size, type, magic-byte, owner/admin download and foreign-id submit tests are real and use the handlers directly.
- Suggestion: Add the four cases.
- Source: general
- Disposition notes: Added tests for the pending cap, remove of another person's pending id returning 404, a non-ASCII disposition, and X-File-Name decoding. The binder's decode moved to `FeedbackAttachmentHttp.DecodeFileNameHeader` so the application-only runfile can test it. `..%2F..%2Fetc%2Fpasswd.txt` is stored as `passwd.txt`; `a%0d%0aSet-Cookie...` returns 400.

### M10 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/feedback/get-feedback/get-feedback-contracts.cs:101
- Description: The mock `GetFeedback` response includes a sample attachment whose `DownloadPath` uses id `2222…`. The item page shows it with `<img src>` (FeedbackItemPage.razor). That request goes to the real server, not through `MockWebApiService`, so client mock mode shows a broken image. The download mock factory (download-feedback-attachment-contracts.cs:52-53) is never used by the browser.
- Suggestion: Make the mock attachment a non-image so it renders as a link, or use a data-URL path in mock mode. Or accept this and note it in the Design region.
- Source: general
- Disposition notes: The mock `GetFeedback` sample attachment is now `mock.txt` / `text/plain`, so the item page shows it as a link. Noted in the contract Design region.

## Duplicates / conflicts

- Single reviewer; none. Orchestrator verified M1 (template.json exclude list) and M2 (non-ASCII filename= in ContentDisposition) against the code.
