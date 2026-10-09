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

- [ ] Decide and document the real attachment store (Blob vs R2) and limits (max size, allowed types)
- [ ] Storage interface + real + in-memory implementations + module registration
- [ ] Attachment record/entity configuration + EF migration
- [ ] Upload endpoint (stream, limits, feedback permission, mock response factory)
- [ ] Download endpoint (owner-or-admin check, safe headers)
- [ ] File picker + attached-files list with remove, under Details
- [ ] JS paste hook on Details using the same upload path (thumbnail/link in text if feasible)
- [ ] FeedbackItemPage shows attachments
- [ ] Unit tests: limits and permissions
- [ ] Playwright WASM tests: paste and upload
- [ ] Full ganda walk (implement, review, audit, done-move, PR); CI green
- [ ] PR includes browser screenshots of paste, upload, and the item page showing attachments

## Session

- Created: 2344160 (2026-10-10, filed from Steven's voice call)

## Notes

- Acceptance: full ganda walk via `ganda task work 295 --yes` (implement, review, audit, done-move
  commit, PR), merge only via `ganda pr merge`; CI green; PR includes browser screenshots of paste and
  upload and of the item page showing attachments.
- Don't run the app on TWE-001; browser proof comes from the box/CI Playwright runs.