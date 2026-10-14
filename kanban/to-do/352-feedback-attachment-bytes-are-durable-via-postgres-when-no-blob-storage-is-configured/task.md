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

- [x] Postgres byte store + migration; mode selection Azure → Postgres → in-memory
- [x] Orphan 404 handling; Design regions
- [x] Tests (persistence across host restart, selection, delete)
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke` (flag-off apps still build), `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, from Steve's broken-image screenshot)
- 2026-10-14 implementer (ganda task work): Postgres byte store, migration, mode selection, orphan 404, tests, docs; gates green.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-14T12:35:38Z
- Review oracle: tw-implementation-review, reviewer general (claude, model claude-opus-5-5), effort 3, session not reported — 2026-10-14
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-14T12:38:47Z

## Results

**Store.** `EfFeedbackAttachmentBlobStore` (scoped, `PostgresDbContext`) implements the existing
`IFeedbackAttachmentBlobStore` port over a new table `feedback.feedback_attachment_bytes`
(`StorageKey varchar(80)` PK, `Content bytea`), entity `FeedbackAttachmentBytes` + its EF
configuration. Migration `20261014121541_AddFeedbackAttachmentBytes` (create table only).

**Mode selection** is Azure → Postgres → in-memory. `FeedbackAttachmentBlobModule` registers
Azure when `FeedbackAttachments:ConnectionString` is set (new `IsAzureConfigured`), else
in-memory; `PostgresDbModule` (runs after it) replaces in-memory with the EF store when a
Postgres connection string exists and Azure is not configured. `InMemoryBlobWarning` now
resolves the final blob store and only warns when it is still in-memory.

**Unit of work.** Not one transaction: each store call commits on its own. The upload handler
already writes bytes before the row (deleting them if the row insert throws) and remove deletes
the row before the bytes, so "row exists ⇒ bytes exist" holds; the worst failure leaves
unreferenced bytes, never a row without bytes. No FK from bytes to row for that reason.

**Streaming.** Reads are not streamed: EF materializes `bytea` as `byte[]`; real streaming needs a
raw `NpgsqlDataReader` with `SequentialAccess` and a reader-owning stream. With the 5 MiB cap
(`FeedbackAttachmentRules.MaxBytes`, enforced before any store write) the store loads the column
untracked into a read-only `MemoryStream`. Recorded in the store's Design region.

**Orphans.** Rows whose bytes are gone stay. Download returns 404 `Attachment no longer
available` (`FeedbackProblems.AttachmentGone`) after the access check — never 500. The optional
UI placeholder was not done; the `<img>` already carries `alt=FileName`.

**Docs.** Design regions updated (blob module, port, in-memory store, download handler, problems,
DbContext, PostgresDbModule). `tw-deploy` gets one bullet on where bytes live per mode;
`documentation/developer/guides/feedback-attachments.md` table updated. Template `(!postgres)`
excludes the three new Postgres files and the mode test.

**Tests.**
- `web-infrastructure-tests/feedback-postgres-persistence-tests.cs` (Testcontainers): upload via
  handler → fresh context download returns the same bytes → remove handler → fresh context shows
  row and bytes gone; put replaces / delete no-op and leaves nothing tracked; orphan row → 404.
- `web-server-integration-tests/features/feedback/feedback-attachment-blob-mode-tests.cs`:
  nothing → in-memory; Postgres only → EF; Azure + Postgres → Azure; Azure only → Azure.
- `features/feedback/feedback-attachment-tests.cs` runfile: in-memory orphan → 404 title.

**Gates (this worktree):** `dev build` 0/0; `dev test` all suites passed (web-infrastructure-tests
76/76); `dev template-smoke` SmokeDefault / SmokeNoPostgres / SmokeNoApi OK; `ganda repo audit`
passes; `dev check-version` 2.0.0-beta.20 ahead of NuGet beta.19.

Note: `dotnet ef migrations add` (via `dev db add-migration`) emitted the class as
`_20261014121541_AddFeedbackAttachmentBytes` and wrote the snapshot under
`projects/web-infrastructure/TimeWarp/...`; both were corrected by hand to the repo layout.

### Review disposition

- Rounds: 1. Roster: general. Effort: 3 (Budget.ByDiff, 1056 lines).
- Final counts: bug 0, suggestion 0, nit 0. Open 0, fixed 0, wontfix 0.
- Disposition: **clean**. No issues were raised.
- Gates re-run by the reviewer:
  - `./bin/dev build`: 0 warnings, 0 errors.
  - web-infrastructure-tests: 76/76.
  - `FeedbackAttachmentBlobMode_`: 4/4.
  - attachment runfile: 21/21.
- Artifacts: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

**Smoke:**
1. `cd tests/container-apps/web/web-infrastructure-tests && dotnet test` (Docker up).
2. `cd tests/container-apps/web/web-server-integration-tests && dotnet run -- --filter-class FeedbackAttachmentBlobMode_`
3. `dotnet run source/container-apps/web/features/feedback/feedback-attachment-tests.cs`
4. Optional live: `dev run`, paste an image into feedback, file it, restart `dev run`, open the item.

**Expect:**
1. All pass, including `Attachment_bytes_survive_a_fresh_context_and_go_with_remove`,
   `Blob_store_put_replaces_and_delete_of_a_missing_key_is_a_no_op`,
   `Download_of_a_row_without_bytes_is_a_404_no_longer_available` (no `[SKIP]` line).
2. 4/4 passed.
3. 21/21 passed, including `Download_Should_Return404WhenTheBytesAreGone`.
4. The image still renders after restart; no in-memory warning in web-server logs; items filed
   before this change show the alt text and their download returns 404 "Attachment no longer
   available".
