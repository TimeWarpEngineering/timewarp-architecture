# Round 1 — general
**Date:** 2026-11-25
**Scope reviewed:** branch `task/522-feedback-attachments-whose-bytes-are-gone` vs merge-base `40d680e8`, against the task requirements and `## Goal (implementer restatement)`. Gates were not re-run; the implementer's `## Results` and `### How to validate` cover build, the changed feedback and SPA tests, template-smoke, and audit.

## Summary

The server sets `GetFeedback.Attachment.IsFilePresent` from `IFeedbackAttachmentBlobStore.ExistsAsync` on the filer read and on every admin view, and `FeedbackBody` renders `MissingFile` (file name and "file no longer stored") for a missing file in the list and in the body. In-memory feedback stores are gone; bytes are Azure, then Postgres, with `UnavailableFeedbackStorage` when the flag is on and no connection string is set. The recorded tests and the two evidence screenshots match that behavior. The developer guide describes the no-connection host as if the stores were not registered.

## Issues

### Issue 1 — Severity: bug
- File: documentation/developer/guides/feedback-attachments.md:22
- Description: The guide says that with the postgres flag on and no connection string, the feedback stores are not registered and the endpoints fail. `FeedbackModule.ConfigureServices` always registers `UnavailableFeedbackStorage` for the row ports and, unless Azure is configured, for the byte port (`feedback-module-infrastructure.cs:39-45`). `PostgresDbModule` returns before replacing those ports when `PostgresDbOptions:ConnectionString` is empty (`postgres-db-module-server.cs:95-100`). Reads then return empty results; writes throw `InvalidOperationException` with "Feedback storage is not available on this host." The mode-selection test `NothingConfigured_Should_UseUnavailableStorage` asserts the byte port resolves to `UnavailableFeedbackStorage`. The table just above (lines 13-16) also says an empty `FeedbackAttachments:ConnectionString` always selects `EfFeedbackAttachmentBlobStore`, which is true only when a Postgres connection is present. `FeedbackModule`'s design region and `## Session` describe the stand-in correctly.
- Suggestion: State the stand-in in the guide and in the table: Azure when its connection string is set, Postgres when a database connection is set and Azure is not, otherwise `UnavailableFeedbackStorage` (reads empty, writes refused). Drop the claim that the stores are unregistered.
- Status: open
