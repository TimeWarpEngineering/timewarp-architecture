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

This host is TWE-001, so the Playwright browser run was not started. The test project builds. CI writes `feedback-upload.png`, `feedback-paste.png`, and `feedback-item.png` beside this task when that test runs.

### How to validate

**Smoke:**

```powershell
cd tests/container-apps/web/web-jaribu-tests; dotnet test -- --filter-class Features.Feedback
cd ../web-infrastructure-tests; dotnet test -- --filter-class Feedback_Model_Mapping_
dotnet test -- --filter-class Feedback_Postgres_Persistence_
cd ../../../foundation/foundation-contracts-tests; dotnet test -- --filter-class HttpApiService_GetResponse
cd ../../container-apps/web/web-spa-playwright-tests; dotnet test
```

**Expect:**

- `Features.Feedback`: 25 passed. That covers size and type limits, owner download, admin denied before link and allowed after, another principal's pending id rejected, and the existing filing cases.
- `Feedback_Model_Mapping_`: 3 passed. The attachment table is nullable on the item id, Version is not a concurrency token, and the item foreign key cascades.
- `Feedback_Postgres_Persistence_`: 1 passed. `MigrateAsync` applies `AddFeedbackAttachments` on Testcontainers Postgres and the feedback item round-trips.
- `HttpApiService_GetResponse`: 14 passed. The file upload test posts the raw bytes as `text/plain` with no charset and `X-File-Name: my%20notes.txt`.
- Playwright `dotnet test` is for a CI runner, not TWE-001. It fills the picker, pastes a 1×1 PNG into Details, submits, and opens the item page. Screenshots land next to this task file.

### Review disposition

- **Outcome:** clean, after 3 rounds. Each round had one general reviewer (effort 3 by diff size; Claude subagents). Reviewer session ids are in `review/review-framework.md`.
- **Final counts:**
  - bug: 5 fixed
  - suggestion: 4 fixed
  - nit: 5 fixed
  - 0 wontfix, 0 open
- **Fix commits:**
  - `3d8bdac` (M1–M10): postgres-off template excludes, an ASCII fallback for the Content-Disposition file name, 24-hour expiry of pending uploads, paste re-render, paste sent as a stream with its size checked first, conditional linking with rollback, a reachable 413 response, a startup warning for in-memory blobs, the missing tests, and a non-image mock attachment.
  - `2c0de45` (M11–M13): rollback by item id, best-effort expiry, and the client dropping expired attachments.
  - M14 was a guide-wording fix made with the disposition.
- **Gates (re-run by a reviewer):**
  - `dev build`: 0 warnings, 0 errors.
  - Features.Feedback: 37/37.
  - Feedback_Model_Mapping_: 3/3.
  - Feedback Postgres persistence: 2/2.
  - HttpApiService_GetResponse: 14/14.
  - `ganda repo audit`: passes.
- **Not run:** Playwright, which is forbidden on TWE-001. The browser proof and screenshots are left for CI.
- **Paths:** `review/review-framework.md`, `review/round-3/merged.md` (last ledger), `review/disposition.md`.

## Session

- Created: 2344160 (2026-10-10, filed from Steven's voice call)
- 2026-10-10 implementer (ganda task work): attachments end to end; web-server and web-spa build clean; gates in Results; `ganda repo audit` 31 passed. Playwright was not executed on TWE-001.
- 2026-10-10 review oracle (Claude Opus 5.5, ganda task work): tw-implementation-review, 3 rounds; disposition clean.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-10T08:48:36Z

## Notes

- Acceptance: full ganda walk via `ganda task work 295 --yes` (implement, review, audit, done-move
  commit, PR), merge only via `ganda pr merge`; CI green; PR includes browser screenshots of paste and
  upload and of the item page showing attachments.
- Don't run the app on TWE-001; browser proof comes from the box/CI Playwright runs.
- 2026-10-10: CI `ci` failed after the PR opened (PR #458, run 38039240128, job 114176033572). `template-smoke` passed. Two test failures, both in scope:
  1. `ActionCatalog_Should.Enumerate_Expected_Names` (`tests/container-apps/web/web-spa-integration-tests/features/application/action-catalog-tests.cs:64`): the expected action-name list lacks the new `Feedback.*` actions this task added (`Feedback.RemoveFeedbackAttachment`, `Feedback.UploadFeedbackAttachment`, and any others). Update the expected list.
  2. `FeedbackAttachment_Given_Wasm.PasteAndUpload_Should_ShowAttachmentsOnTheItem` (new Playwright test, `tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-*`): `System.TimeoutException: Feedback form did not render`, after a 120000ms wait for `Locator(".twe-appbar")` to be visible. Investigate whether this is a real bug (the page/app shell not rendering with the attachment changes) or test setup (navigation, auth/sign-in, base URL, render mode, wait target); compare with the setup of the other Playwright tests that pass in the same run. Fix the root cause, don't just raise the timeout.
  Steven approved another walk pass (`ganda task work 295 --restart --no-merge --yes`) to fix both on this branch and PR #458. First walk log: `~/logs/task-work-timewarp-architecture-295-20261010-135714.log`.
