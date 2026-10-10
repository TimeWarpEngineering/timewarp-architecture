# Feedback: image paste and file upload on details field

## Description

Steven asked (voice call, 2026-10-10) whether users can paste images into, or upload files from,
the feedback screen's **Details** field. Today they can't do either. Add image paste and file upload
for feedback attachments, end to end (storage, data, API, UI, item page, tests).

This is the template's **first file feature**: the storage abstraction, upload/download endpoint
shape, limits, mock-mode behaviour and paste hook chosen here become the template standard, so
choose deliberately and document the decisions.

### Current state (verified 2026-10-10 on master `f8a3cbaba`)

- **Form:** `source/container-apps/web/projects/web-spa/features/feedback/pages/FeedbackListPage.razor`.
  Details is `<FluentTextArea @bind-Value="Draft.Body" Name="Body" AriaLabel="Details" data-qa="FeedbackBody" ... />`
  (line ~69), bound to `Body`, max 8,000 characters (`SubmitFeedback.MaxBodyLength` /
  `FeedbackItem.MaxBodyLength`). Pasting an image does nothing.
- **Submit is JSON only:** `source/container-apps/web/features/feedback/submit-feedback/submit-feedback-contracts.cs`,
  `Command { Kind, Title, Body, EmailCopy }`, `POST api/Feedback`, policy `PermissionIds.FeedbackFileSelf`,
  schemes `identity-session` + `mock-identity-session`, with a `GetMockResponseFactory`.
- **DB stores text only:** `feedback-item-entity-type-configuration-infrastructure.cs`; migration
  `20261009001245_AddFeedbackItems`.
- **Store pattern:** `i-feedback-store-application.cs`, `ef-feedback-store-infrastructure.cs`,
  `in-memory-feedback-store-application.cs`, `in-memory-feedback-stores-module-infrastructure.cs`
  (all under `source/container-apps/web/features/feedback/`).
- **No file/image handling anywhere in the web app:** no `InputFile` / `IBrowserFile` /
  `FluentInputFile`, no paste handling, no multipart, no blob storage.

## Requirements

1. **Attachment blob storage interface** (Azure Blob Storage or Cloudflare R2) with an in-memory
   implementation for tests and mock mode, following feedback's existing store pattern (interface in
   application, real + in-memory implementations, module registration). Decide and document the real
   backing store and its configuration.
2. **Feedback attachment record**: file name, content type, size, storage key, owning feedback item;
   plus an EF migration.
3. **Upload endpoint** taking the file as a stream with size and content-type limits (constants, not
   magic numbers), under the existing feedback permission, with a mock response factory so mock mode
   keeps working (tw-web-api-contracts, tw-mock-response-factory).
4. **Download endpoint** with a permission check: only the feedback item's owner or an admin can fetch
   an attachment. Serve with a safe `Content-Type` / `Content-Disposition`.
5. **File picker** (Blazor `InputFile` or `FluentInputFile`) under Details, with an attached-files list
   that lets the user remove an attachment before submit.
6. **Paste hook**: a small JS module on the Details box that catches pasted images and sends them
   through the same upload path, ideally inserting a thumbnail or link into the text.
7. **Feedback item page** (`FeedbackItemPage.razor`) lists/shows the attachments.
8. **Tests:** unit tests for the size/type limits and the owner/admin download permission; a real-browser
   (WASM) Playwright test for paste and for upload in `tests/container-apps/web/web-spa-playwright-tests`.
9. Follow repo `AGENTS.md` and TimeWarp skills: tw-feature-placement, tw-aggregate-pattern, tw-blazor,
   tw-blazor-css-strategy (no `<style>` or `style=` in components; CSS in `.razor.css`).

## Checklist

- [x] Decide and document the real attachment store (Blob vs R2) and limits (max size, allowed types)
- [x] Storage interface + real + in-memory implementations + module registration
- [x] Attachment record/entity configuration + EF migration
- [x] Upload endpoint (stream, limits, feedback permission, mock response factory)
- [x] Download endpoint (owner-or-admin check, safe headers)
- [x] File picker + attached-files list with remove, under Details
- [x] JS paste hook on Details using the same upload path (thumbnail/link in text if feasible)
- [x] FeedbackItemPage shows attachments
- [x] Unit tests: limits and permissions
- [x] Playwright WASM tests: paste and upload
- [ ] Full ganda walk (implement, review, audit, done-move, PR); CI green
- [ ] PR includes browser screenshots of paste, upload, and the item page showing attachments

## Results

Azure Blob Storage is the real byte store. An empty `FeedbackAttachments:ConnectionString` keeps the in-memory blob store, so tests and an unconfigured host do not need Azurite. Rows live on `feedback.feedback_attachments` (migration `20261010075816_AddFeedbackAttachments`). Upload is a raw body with `X-File-Name`. Download is owner-or-admin after the item is filed, and uploader-only while it is still a draft. The Details field has a file picker and a paste hook; both call the same upload. The item page lists the stored files. Decisions and limits are in `documentation/developer/guides/feedback-attachments.md`.

CI on PR #458 failed two tests. The action catalog roster omitted `Feedback.RemoveFeedbackAttachment` and `Feedback.UploadFeedbackAttachment`. The Playwright test opened `/Feedback` while signed out. That page is `[Authorize]` for `feedback.file.self`, and the SPA reads that from the identity-session cookie. `X-TimeWarp-Mock-Principal-Id` authenticates server API calls only, so the router rendered `RedirectToLogin`, which has no `.twe-appbar`. The console showed `LoginPage-1: created` and a healthy WASM boot. The shell was not broken. The test now creates an account with a virtual passkey, the same ceremony as the Ask sign-in proof, then opens `/Feedback`.

This host is TWE-001, so the Playwright browser run was not started. `web-spa-playwright-tests` builds with 0 warnings. CI writes `feedback-upload.png`, `feedback-paste.png`, and `feedback-item-attachments.png` beside this task when that test runs.

The next CI run (PR #458, run 38041035106, head `386050071`) still rejected `notes.txt`. `POST /api/Feedback/attachments` with `text/plain` matched the host's empty 415 endpoint before the upload endpoint ran, because a POST request DTO accepts only `application/json`. Clearing that default also deletes an `Accepts()` call in the same method, so the allow-list is added after that convention. `text/plain`, `text/plain; charset=utf-8`, and `image/png` now return 200. `image/svg+xml` stays the empty 415. When an upload fails and the API problem is missing or only the generic unhandled error, the form says the file could not be uploaded.

### How to validate

**Smoke:**

```powershell
cd tests/container-apps/web/web-server-integration-tests; dotnet test -c Release -- --filter-class Accepts_
cd ../web-spa-playwright-tests; dotnet build -c Release
```

**Expect:**

- `Accepts_`: 4 passed. `text/plain` (21-byte `notes.txt`), `text/plain` with charset, and `image/png` return 200. `image/svg+xml` returns an empty 415.
- `web-spa-playwright-tests` builds with 0 warnings and 0 errors. The browser run stays on CI. It signs in with a virtual passkey, opens `/Feedback`, fills the picker, pastes a 1×1 PNG into Details, submits, and opens the item page. Screenshots land next to this task file.

### Review disposition

- **Outcome:** clean, after 5 rounds. Each round had one general reviewer (effort 3 by diff size; Claude subagents for rounds 1–3, the review oracle for rounds 4–5). Reviewer session ids are in `review/review-framework.md`.
- **Final counts:**
  - bug: 5 fixed
  - suggestion: 5 fixed
  - nit: 5 fixed
  - 0 wontfix, 0 open
- **Fix commits:**
  - `3d8bdac` (M1–M10): postgres-off template excludes, an ASCII fallback for the Content-Disposition file name, 24-hour expiry of pending uploads, paste re-render, paste sent as a stream with its size checked first, conditional linking with rollback, a reachable 413 response, a startup warning for in-memory blobs, the missing tests, and a non-image mock attachment.
  - `2c0de45` (M11–M13): rollback by item id, best-effort expiry, and the client dropping expired attachments.
  - M14 was a guide-wording fix made with the disposition.
  - Round 4 reviewed the CI-fix delta (`5052084ad` and the catalog roster): no new findings. `ActionCatalog_Should` 10/10 and the Playwright project build (0 warnings) were re-run by the review oracle.
  - Round 5 reviewed the upload-415 delta (`741a56e20`): M15 (suggestion), the page matched the literal `"Unhandled Error"`; fixed by the review oracle with `HttpApiService.UnhandledErrorTitle`. `dev build` 0/0, `Accepts_` 4/4, `HttpApiService_GetResponse` 14/14 re-run.
- **Gates (re-run by a reviewer):**
  - `dev build`: 0 warnings, 0 errors.
  - Features.Feedback: 37/37.
  - Feedback_Model_Mapping_: 3/3.
  - Feedback Postgres persistence: 2/2.
  - HttpApiService_GetResponse: 14/14.
  - `ganda repo audit`: passes.
- **Not run:** Playwright, which is forbidden on TWE-001. The browser proof and screenshots are left for CI.
- **Paths:** `review/review-framework.md`, `review/round-5/merged.md` (last ledger), `review/disposition.md`.

## Session

- Created: 2344160 (2026-10-10, filed from Steven's voice call)
- 2026-10-10 implementer (ganda task work): attachments end to end; web-server and web-spa build clean; gates in Results; `ganda repo audit` 31 passed. Playwright was not executed on TWE-001.
- 2026-10-10 review oracle (Claude Opus 5.5, ganda task work): tw-implementation-review, 3 rounds; disposition clean.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-10T08:48:36Z
- 2026-10-10 review oracle (Claude Opus 5.5, ganda task work): round 4 on the CI-fix delta; no findings; disposition still clean.
- 2026-10-10 implementer (ganda task work, CI fix): catalog names added; Playwright signs in before `/Feedback`. `ActionCatalog_Should` 10/10. Playwright project builds. Browser run left for CI.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-10T09:19:19Z
- 2026-10-10 implementer (ganda task work, upload 415): the allow-list is applied after the JSON accepts default is cleared. `Accepts_` 4/4. Playwright project builds with 0 warnings. Browser run left for CI. A failed upload shows a message when the API problem is missing.
- 2026-10-10 review oracle (Claude Opus 5.5, ganda task work): round 5 on the upload-415 delta; M15 fixed; disposition clean (5 rounds, 15 fixed).
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-10T10:04:31Z

## Notes

- Acceptance: full ganda walk via `ganda task work 295 --yes` (implement, review, audit, done-move
  commit, PR), merge only via `ganda pr merge`; CI green; PR includes browser screenshots of paste and
  upload and of the item page showing attachments.
- Don't run the app on TWE-001; browser proof comes from the box/CI Playwright runs.
- 2026-10-10: CI `ci` failed after the PR opened (PR #458, run 38039240128, job 114176033572). `template-smoke` passed. Two test failures, both in scope:
  1. `ActionCatalog_Should.Enumerate_Expected_Names` (`tests/container-apps/web/web-spa-integration-tests/features/application/action-catalog-tests.cs:64`): the expected action-name list lacks the new `Feedback.*` actions this task added (`Feedback.RemoveFeedbackAttachment`, `Feedback.UploadFeedbackAttachment`, and any others). Update the expected list.
  2. `FeedbackAttachment_Given_Wasm.PasteAndUpload_Should_ShowAttachmentsOnTheItem` (new Playwright test, `tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-*`): `System.TimeoutException: Feedback form did not render`, after a 120000ms wait for `Locator(".twe-appbar")` to be visible. Investigate whether this is a real bug (the page/app shell not rendering with the attachment changes) or test setup (navigation, auth/sign-in, base URL, render mode, wait target); compare with the setup of the other Playwright tests that pass in the same run. Fix the root cause, don't just raise the timeout.
  Steven approved another walk pass (`ganda task work 295 --restart --no-merge --yes`) to fix both on this branch and PR #458. First walk log: `~/logs/task-work-timewarp-architecture-295-20261010-135714.log`.
- 2026-10-10 fix: the catalog expected list now includes the two attachment actions. The Playwright timeout was the signed-out login page (`LoginPage` has no `.twe-appbar`), not a shell that failed to render. The test signs in with a virtual passkey before opening `/Feedback`.
- 2026-10-10: second CI run after the CI-fix pass (PR #458, run 38041035106, job 114181177066, head `386050071`) still failed `ci`. `ActionCatalog_Should` now passes and the sign-in fix works: the feedback form renders. The remaining failure, `FeedbackAttachment_Given_Wasm.PasteAndUpload_Should_ShowAttachmentsOnTheItem`, timed out at `feedback-attachment-playwright-tests.cs:88` waiting for `[data-qa=FeedbackAttachment]` after the file picker set `notes.txt` (`text/plain`). The web-server log shows the cause, a real product bug: `POST /api/Feedback/attachments - text/plain 21` matched endpoint `415 HTTP Unsupported Media Type` and returned 415. The upload endpoint's content-type matching (e.g. an `Accepts<...>`/consumes restriction or a request-body binding that only takes certain media types) rejects the raw file body for allowed file types. Fix the endpoint so every allowed attachment type (text, images, etc.) uploads with its own Content-Type, add a test for a non-octet-stream upload, and make the UI surface an upload error instead of silently showing nothing. Steven approved another walk pass (`ganda task work 295 --restart --no-merge --yes`). Second walk log: `~/logs/task-work-timewarp-architecture-295-20261010-160656.log`.
- 2026-10-10 fix: the upload allow-list is added after the convention that clears the JSON default. `text/plain` and `image/png` return 200. `image/svg+xml` stays an empty 415. The form says the file could not be uploaded when the failure body has no useful problem.
- 2026-10-10: third CI run (PR #458, run 38043697206, head `c3135dc71`) still failed `ci` on one test. `template-smoke` passed and the action-catalog test passes. The 415 fix works: the file picker's `POST /api/Feedback/attachments` (text/plain) returned 200 and the first attachment rendered. `FeedbackAttachment_Given_Wasm.PasteAndUpload_Should_ShowAttachmentsOnTheItem` then timed out at `feedback-attachment-playwright-tests.cs:113` waiting for `[data-qa=FeedbackAttachment]` Nth(1), the pasted image. The web-server log shows no second upload request after the paste, so the paste never reached .NET. This is a real product bug, not test setup: the test dispatches paste on `[data-qa=FeedbackBody]`'s shadow-root textarea, which is the path users hit. Suspected cause, in `source/container-apps/web/projects/web-spa/source/features/feedback-paste.ts`: (1) `Register` finds the text area with `root.querySelector("fluent-text-area")`, which only searches descendants, so it misses when `root` (the element reference the page passes, `[data-qa=FeedbackBody]`) IS the `fluent-text-area`, and no listener is bound; (2) the fallback re-attaches only once, on `customElements.whenDefined`, which can fire before the shadow-root textarea exists, so the inner textarea may never be bound. Fix the binding (handle root being the text area itself, and bind once the inner textarea exists, e.g. after first render or via a MutationObserver), and confirm with the Playwright test in CI. Steven approved one more walk pass (`ganda task work 295 --restart --no-merge --yes`, 2026-10-10 18:25 ICT).
