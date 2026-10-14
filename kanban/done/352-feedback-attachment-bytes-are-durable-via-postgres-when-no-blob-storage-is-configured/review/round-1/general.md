# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** commit 965cf28b6 vs master (source, tests, template.json, tw-deploy, guide)

## Summary

The change adds `EfFeedbackAttachmentBlobStore` over a new `feedback.feedback_attachment_bytes` bytea
table and a migration. `PostgresDbModule` replaces the in-memory byte store when Postgres is connected
and Azure is not configured, and a row whose bytes are gone downloads as 404 "Attachment no longer
available". Checked:
- Program registration order: `FeedbackAttachmentBlobModule` runs before `PostgresDbModule`.
- Upload: bytes are written before the row, and the bytes are deleted if the row insert throws (`CancellationToken.None`).
- Remove: the row is deleted before the bytes.
- Expiry delete path.
- Scoped-store consumers: all three are handlers, so no singleton captures the scoped store.
- `InMemoryBlobWarning` resolves the final store.
- `(!postgres)` template excludes.
- Migration and snapshot.

Risk is low.

Observation, not raised as a finding: in the submit rollback, if `UnlinkAllAsync` fails but item removal
succeeds, the attachment rows cascade-delete with the item. Their bytes stay behind, unreferenced. This
already happened with Azure, it needs a double failure, and it never produces a row without bytes, so it
stays out of scope here.

Gates re-run by the reviewer:
- `./bin/dev build`: 0 warnings, 0 errors.
- web-infrastructure-tests: 76/76, Testcontainers, no skips.
- `FeedbackAttachmentBlobMode_`: 4/4.
- `feedback-attachment-tests.cs` runfile: 21/21.

## Issues

<!-- none -->
