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

After 352 the mode selection is Azure, then Postgres, then in-memory, so configuration can no
longer produce durable rows over volatile bytes. The orphans are pre-352 data only. The
in-memory feedback stores, and the `InMemoryBlobWarning` that guards a combination the code no
longer produces, remain for tests, mock mode, and apps generated without the `postgres` flag.
Decision (Steve, 2026-11-25): none of those needs a lossy store. Tests use the Postgres lane,
mock mode is identity only, and a generated app without a database gets no feedback feature
rather than one that discards filings on restart.

## Requirements

- An attachment row whose bytes are missing is visible as such. The admin view and the filer's
  item view show the file name with a "file no longer stored" state, not a broken `<img>` or a
  link that 404s. The body's markdown image for that attachment renders the same state.
- `GetFeedbackForAdmin` and `GetFeedback` report per attachment whether the file is present,
  so the SPA does not probe each download URL to find out.
- A one-time reconcile for the two production items: either mark the rows as file-missing or
  remove the rows and strip the body image lines. Pick one and record why in `## Session`.
  Deleting the feedback items themselves is out of scope.
- The in-memory feedback stores are deleted: `InMemoryFeedbackStore`,
  `InMemoryFeedbackAttachmentStore`, `InMemoryFeedbackAttachmentBlobStore`,
  `in-memory-feedback-stores-module-infrastructure.cs`, and `InMemoryBlobWarning`. Feedback
  rows live in Postgres; bytes in Postgres or Azure Blob Storage.
- The feedback slice compiles out when the `postgres` template flag is off, the same way the
  EF stores do: `#if postgres` / `<!--#if (postgres)-->` on the slice's registration, routes,
  nav entry, WebMCP tools, contracts, and tests (TWA0008 / TWA0010 regions stay intact). The
  default variant and `dev template-smoke --matrix` with postgres off both build with 0 warnings.
- Tests that used the in-memory stores move to the Postgres lane
  (`skills/tw-feature-placement/references/co-located-jaribu-runfiles.md`).

## Checklist

- [ ] Add a file-present flag to the attachment detail contracts (`feedback-item-details-contracts.cs`,
      `feedback-admin-view-contracts.cs`) filled by the handler from `IFeedbackAttachmentBlobStore`
- [ ] `FeedbackItemPage` and the admin item view render the missing-file state; body markdown
      image for a missing attachment renders that state too
- [ ] Decide reconcile vs remove for existing orphan rows; implement as an idempotent startup or `dev` verb
      pass, not a hand SQL script (verify locally against seeded orphan rows in Postgres during the PR walk; live `arch.timewarp.work` reconcile runs post-deploy)
- [ ] Delete the in-memory feedback stores, their module, and `InMemoryBlobWarning`; mode
      selection becomes Azure, then Postgres
- [ ] Gate the feedback slice on the `postgres` flag; `dev template-smoke --matrix` green
- [ ] Move feedback tests off the in-memory stores to the Postgres lane
- [ ] Jaribu tests: orphan row renders missing state
- [ ] Screenshots of the missing-file attachment state (admin + filer views, including body markdown image) captured locally from the running app in `evidence/` (live `arch.timewarp.work` verification runs post-deploy)
- [ ] `dev build`, `ganda repo audit`

## Notes

Verified 2026-11-24 through the agent API (`feedback:read`): `GET /api/admin/feedback/01a12f40-fd5d-77ca-beff-b33d0d65c964`
and `/01a126d4-e055-7344-9b90-59b8e0749615` list attachments `01a12f40-f348-76b7-a1c5-9e0702feeb0e`
(309049 B) and `01a126d4-7dc2-752f-aef1-8db79a34c205` (34330 B); both downloads return 404.
Problem text lives in `feedback-problems-application.cs:40`. Store selection: `postgres-db-module-server.cs:146`.
