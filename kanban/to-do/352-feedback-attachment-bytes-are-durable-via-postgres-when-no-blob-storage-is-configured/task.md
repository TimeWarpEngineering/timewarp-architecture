# Feedback attachment bytes are durable via Postgres when no blob storage is configured

## Problem (2026-10-14, arch.timewarp.work)

Pasted images on older feedback items show as broken images. "Cramers complaint" (2026-10-10)
requests `/api/Feedback/attachments/01a126d4-…` and gets 404.

**Cause:**
- `features/feedback/feedback-attachment-blob-module-infrastructure.cs`: with
  `FeedbackAttachments:ConnectionString` empty (the committed default), the **bytes** go to
  `InMemoryFeedbackAttachmentBlobStore`.
- The feedback item and its attachment **rows** go to Postgres, which persists.
- Every restart (`dev run`, or a kind or Compose redeploy) wipes the bytes, but the rows survive, so
  every older download 404s.
- The module's Design region already admits this and only logs a startup warning
  (`InMemoryBlobWarning`).

## Requirements

- With the **`postgres` flag on** and no blob connection string, store attachment bytes in
  **Postgres**: a `bytea` column or table owned by the feedback slice, written in the same unit of
  work as the attachment row where practical. "Row exists" must imply "bytes exist".
  - Add an EF migration.
  - Respect the existing size limits (`feedback-attachment-rules-contracts.cs`).
  - Stream reads where EF and Npgsql allow, and record the approach.
- Azure Blob stays the store when `FeedbackAttachments:ConnectionString` is set (production
  choice; unchanged). In-memory stays only for tests, mock mode and apps generated without
  `postgres`. Keep the warning for that last case.
- Update the Design region: mode selection becomes Azure, then Postgres, then in-memory, and the
  "every older download would 404" caveat now applies only to in-memory.
- **Existing orphans:** rows whose bytes are gone stay as they are. The download returns a clear 404
  "attachment no longer available", never a 500. Optionally the UI shows a placeholder instead of a
  broken image.
- **Tests:**
  - Postgres persistence (Testcontainers): upload, then a fresh context/host, then download returns
    the same bytes;
  - mode-selection tests;
  - delete-attachment removes the bytes too.
- `tw-deploy`: one line on where attachment bytes live per mode.

## Checklist

- [ ] Postgres byte store + migration; mode selection Azure → Postgres → in-memory
- [ ] Orphan 404 handling; Design regions
- [ ] Tests (persistence across host restart, selection, delete)
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke` (flag-off apps still build), `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, from Steve's broken-image screenshot)
