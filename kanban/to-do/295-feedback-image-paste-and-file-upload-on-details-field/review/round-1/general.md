# Round 1 — general
**Date:** 2026-10-10
**Scope reviewed:** branch task/295-feedback-image-paste-and-file-upload-on-details-fi vs master (866d6e724)

## Summary
The server's security core holds up. The stream is read under a byte cap. Types are allow-listed and the magic bytes are checked. File names are cut to one path segment, with control and quote characters refused. Storage keys come from the server. Pending uploads stay with the uploader and filed ones go to the owner or an admin. Downloads send nosniff with a safe Content-Type, and submit refuses another person's attachment ids. The main defects are elsewhere: the template build breaks when `postgres` is off, and downloads crash for file names that are not ASCII. Uploads that are never filed count against the per-person cap forever, which can lock a user out. The paste path also has two client faults: it does not re-render, and it can overflow the SignalR message limit under InteractiveAuto.

## Issues

### Issue 1 — Severity: bug
- File: .template.config/template.json:102
- Description: The `(!postgres)` exclude list names `ef-feedback-store-infrastructure.cs` and `feedback-item-entity-type-configuration-infrastructure.cs`. It does not name the two new files. `ef-feedback-attachment-store-infrastructure.cs` uses `PostgresDbContext` and `TimeWarp.Architecture.Persistence` (lines 16, 20, 22), which are excluded when postgres is off. `feedback-attachment-entity-type-configuration-infrastructure.cs:22` uses `FeedbackItemEntityTypeConfiguration.SchemaName`, which is also excluded. So `dotnet new timewarp-architecture --postgres false` produces an app that does not compile.
- Suggestion: Add both files to the `(!postgres)` exclude array. Run `dev template-smoke` with postgres off.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/features/feedback/feedback-attachment-http-application.cs:33
- Description: `ContentDisposition` puts `safe` unchanged into `filename="…"`. Only quotes, CR/LF and backslash are removed. The Design comment says "an ASCII filename", but nothing removes non-ASCII characters, and `FeedbackAttachmentNames.Normalize` (feedback-attachment-names-domain.cs:36-42) accepts them. Kestrel refuses non-ASCII response header values when `ResponseHeaderEncodingSelector` is unset, and it is unset here (no match in `source/`). Any file such as `résumé.pdf` or `截图.png` then fails to download with a 500, and its `<img>` on the item page is broken. A default macOS screenshot name also fails, because it contains U+202F before "AM"/"PM".
- Suggestion: Build an ASCII fallback for `filename=`, replacing non-ASCII characters with `_` or `?`. Keep the UTF-8 name only in `filename*=`. `ContentDispositionHeaderValue` with `FileNameStar` does this. Add a test with a non-ASCII name.
- Status: open

### Issue 3 — Severity: bug
- File: source/container-apps/web/features/feedback/upload-feedback-attachment/upload-feedback-attachment-handler-application.cs:70
- Description: Upload refuses with 409 once a person has `MaxPerItem` (8) unlinked attachments. The client knows its pending files only through `FeedbackState.DraftAttachments` (web-spa feedback-state.cs:22). That list lives only in browser state, so a reload, closed tab or new device loses it. No endpoint lists pending uploads, and nothing cleans them up or expires them. After 8 uploads that were never filed, the user gets "Too many attachments" on every upload and has no way to clear them. The page check at FeedbackListPage.razor:73/105 still thinks there is room. The blobs also pile up for good.
- Suggestion: Pick one of these:
  - Add a "list my pending attachments" query and rebuild `DraftAttachments` when the page loads.
  - Have upload expire unlinked rows older than some limit, with their blobs.
  - Add a background sweep.
  
  Add a test for the cap, which no test covers today.
- Status: open

### Issue 4 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/feedback/pages/FeedbackListPage.razor:65
- Description: `ReceivePastedImage` is a `[JSInvokable]` call from JS. It is not a Blazor event callback, so the page gets no automatic `StateHasChanged` when it finishes. The only re-render is the one TimeWarp.State starts when `UploadFeedbackAttachment` changes state. That render happens inside the `await` at line 133, before `Previews[...]` (line 143) and `AppendMarkdown` (line 146) run. As a result, the pasted image's markdown link and thumbnail do not appear until some later, unrelated render. If the user keeps typing first, `@bind-Value` writes the textarea text back to `Draft.Body` and the link is lost. The Playwright assertion `body.ShouldContain("/api/Feedback/attachments/")` (feedback-attachment-playwright-tests.cs:112-113) probably fails for this reason. Playwright has not run on this host.
- Suggestion: Call `await InvokeAsync(StateHasChanged)` at the end of the paste path. Better, move the body and preview edits into state so the action's own render shows them.
- Status: open

### Issue 5 — Severity: bug
- File: source/container-apps/web/projects/web-spa/source/features/feedback-paste.ts:89
- Description: `send` reads the whole pasted file with `arrayBuffer()` and sends it to .NET as a `byte[]` through `invokeMethodAsync`. Size is checked only afterwards, in .NET (FeedbackListPage.razor:119). The default render mode is `InteractiveAuto` (blazor-settings.cs:17). On a first visit, before WASM is cached, the page runs on an InteractiveServer circuit. There the bytes travel over SignalR, which has a 32 KB default `MaximumReceiveMessageSize`, and nothing raises it (no `AddHubOptions` or `MaximumReceiveMessageSize` in `source/`). A normal screenshot paste then goes over the limit and drops the circuit. The `void send(...)` call also leaves the rejection unhandled, so the user sees nothing.
- Suggestion: Check `file.size` against `MaxBytes` in JS before reading. On the server circuit, stream the file instead of sending a `byte[]`, for example with `DotNetStreamReference` or `IJSStreamReference`. Alternatively, only enable paste when `RendererInfo.Name == "WebAssembly"`. Catch errors in `send` and report them.
- Status: open

### Issue 6 — Severity: suggestion
- File: source/container-apps/web/features/feedback/submit-feedback/submit-feedback-handler-application.cs:93
- Description: Linking is check-then-act and not atomic:
  - Ownership and "unlinked" are checked at lines 94-107. Then `FeedbackStore.AddAsync` and one `LinkAsync` per attachment each save separately (lines 115-119).
  - If a remove or a second submit runs between the check and the link (a double-click, or two tabs), the item is already filed when `LinkAsync` throws (ef-feedback-attachment-store-infrastructure.cs:87-89, in-memory :76-81, or `Link`'s "already linked"). The user gets a 500 and an item with only some of its attachments.
  - In EF, two submits can both load the row with `FeedbackItemId == null`, and `Version` is not a concurrency token. The later save wins and silently moves the attachment to the second item.
  - Upload has the same race: `CountUnlinkedByOwnerAsync` followed by `AddAsync` lets concurrent uploads go past the cap.
- Suggestion: Wrap the item insert and the links in one transaction. Link with a conditional update, `ExecuteUpdate ... WHERE Id = @id AND OwnerPrincipalId = @owner AND FeedbackItemId IS NULL`, and check the affected row count. Alternatively, make Version a concurrency token for this entity.
- Status: open

### Issue 7 — Severity: suggestion
- File: source/container-apps/web/features/feedback/upload-feedback-attachment/upload-feedback-attachment-endpoint-server.cs:27
- Description: `MaxRequestBodySize(FeedbackAttachmentRules.MaxBytes)` matches the handler's cap exactly, so the handler's `TooLarge()` 413 problem (feedback-attachment-content-application.cs:45-48) can never be reached over HTTP. Kestrel throws `BadHttpRequestException` while the body is being read, or before it if Content-Length is too large. The response is then whatever the exception middleware produces, not the documented `application/problem+json` 413. The rejection is also logged as an unhandled exception. The cap is still enforced, so this is about response shape and logging, not security.
- Suggestion: Set the Kestrel limit a little above `MaxBytes` (for example `MaxBytes + 1`) so the handler returns its 413 problem. Or catch `BadHttpRequestException` in `ReadAsync` and turn it into `TooLarge()`.
- Status: open

### Issue 8 — Severity: suggestion
- File: source/container-apps/web/features/feedback/feedback-attachment-blob-module-infrastructure.cs:27
- Description: If `FeedbackAttachments:ConnectionString` is empty, the in-memory blob store is used on every host, including a deployed one where Postgres is set up. Rows then survive a restart but their bytes do not. Every download of an older attachment returns 404 (the handler at :71-77 turns the missing blob into NotFound). Nothing warns about this. Each instance also holds up to 5 MiB per file in memory with no limit on filed items. The guide documents the fallback for "unconfigured hosts", but leaving the setting out of a production deployment fails silently.
- Suggestion: Log a startup warning when the row store is EF but the blob store is in-memory. Or fail fast outside Development or mock mode.
- Status: open

### Issue 9 — Severity: nit
- File: source/container-apps/web/features/feedback/feedback-attachment-tests.cs:62
- Description: These cases have no tests:
  - The 8-per-person pending cap (`TooManyAttachments`).
  - Remove of another person's pending id returning 404.
  - A non-ASCII name in Content-Disposition (would have caught Issue 2).
  - The `X-File-Name` binder decoding a percent-encoded `../` or `%0d%0a` name.
  
  The size, type, magic-byte, owner/admin download and foreign-id submit tests are real and use the handlers directly.
- Suggestion: Add the four cases.
- Status: open

### Issue 10 — Severity: nit
- File: source/container-apps/web/features/feedback/get-feedback/get-feedback-contracts.cs:101
- Description: The mock `GetFeedback` response includes a sample attachment whose `DownloadPath` uses id `2222…`. The item page shows it with `<img src>` (FeedbackItemPage.razor). That request goes to the real server, not through `MockWebApiService`, so client mock mode shows a broken image. The download mock factory (download-feedback-attachment-contracts.cs:52-53) is never used by the browser.
- Suggestion: Make the mock attachment a non-image so it renders as a link, or use a data-URL path in mock mode. Or accept this and note it in the Design region.
- Status: open
