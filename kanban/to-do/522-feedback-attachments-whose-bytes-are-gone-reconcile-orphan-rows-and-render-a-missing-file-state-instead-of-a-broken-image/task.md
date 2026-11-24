# Feedback attachments whose bytes are gone: reconcile orphan rows and render a missing-file state instead of a broken image

## Description

On arch.timewarp.work two open feedback items, "Dark theme broken" (filed 2026-10-12) and
"Cramers complaint" (filed 2026-10-10), list one image attachment each with a file name and
size, and their bodies carry `![…](/api/Feedback/attachments/{id})`. Downloading either returns
404 `Attachment no longer available` ("The file for this attachment is no longer stored"). The
review page therefore shows a broken image and a download link that fails.

Cause: before task 352 (filed 2026-10-14) attachment bytes lived only in
`InMemoryFeedbackAttachmentBlobStore` while the attachment rows were already in Postgres. A
restart dropped the bytes and kept the rows. 352 made new bytes durable but did not touch rows
that had already lost theirs, and the UI has no state for an attachment whose file is gone.

A host that knowingly runs without durable files (the in-memory warning in
`feedback-attachment-blob-module-infrastructure.cs` allows it) hits the same shape after every
restart, so this is a live code path, not only a one-time cleanup.

## Requirements

- An attachment row whose bytes are missing is visible as such. The admin view and the filer's
  item view show the file name with a "file no longer stored" state, not a broken `<img>` or a
  link that 404s. The body's markdown image for that attachment renders the same state.
- `GetFeedbackForAdmin` and `GetFeedback` report per attachment whether the file is present,
  so the SPA does not probe each download URL to find out.
- A one-time reconcile for the two production items: either mark the rows as file-missing or
  remove the rows and strip the body image lines. Pick one and record why in `## Session`.
  Deleting the feedback items themselves is out of scope.
- No feedback attachment can reach this state silently in future: when the blob store is
  in-memory and the row store is durable, upload is refused with a problem detail that names
  the configuration, or the existing startup warning is promoted to a startup failure outside
  Development. Pick one and record why.

## Checklist

- [ ] Add a file-present flag to the attachment detail contracts (`feedback-item-details-contracts.cs`,
      `feedback-admin-view-contracts.cs`) filled by the handler from `IFeedbackAttachmentBlobStore`
- [ ] `FeedbackItemPage` and the admin item view render the missing-file state; body markdown
      image for a missing attachment renders that state too
- [ ] Decide reconcile vs remove for existing orphan rows; implement as a startup or `dev` verb
      pass, not a hand SQL script; run it against arch.timewarp.work
- [ ] Decide refuse-upload vs fail-startup for in-memory bytes over durable rows; implement
- [ ] Jaribu tests: orphan row renders missing state; upload or startup behaves as decided
- [ ] Screenshots of the two production items after the fix in `evidence/`
- [ ] `dev build`, `ganda repo audit`

## Notes

Verified 2026-11-24 through the agent API (`feedback:read`): `GET /api/admin/feedback/01a12f40-fd5d-77ca-beff-b33d0d65c964`
and `/01a126d4-e055-7344-9b90-59b8e0749615` list attachments `01a12f40-f348-76b7-a1c5-9e0702feeb0e`
(309049 B) and `01a126d4-7dc2-752f-aef1-8db79a34c205` (34330 B); both downloads return 404.
Problem text lives in `feedback-problems-application.cs:40`. Store selection: `postgres-db-module-server.cs:146`.
