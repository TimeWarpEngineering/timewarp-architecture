# Round 1 — merged findings
**Date:** 2026-11-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: documentation/developer/guides/feedback-attachments.md:22
- Description: The guide says that with the postgres flag on and no connection string, the feedback stores are not registered and the endpoints fail. `FeedbackModule.ConfigureServices` always registers `UnavailableFeedbackStorage` for the row ports and, unless Azure is configured, for the byte port (`feedback-module-infrastructure.cs:39-45`). `PostgresDbModule` returns before replacing those ports when `PostgresDbOptions:ConnectionString` is empty (`postgres-db-module-server.cs:95-100`). Reads then return empty results; writes throw `InvalidOperationException` with "Feedback storage is not available on this host." The mode-selection test `NothingConfigured_Should_UseUnavailableStorage` asserts the byte port resolves to `UnavailableFeedbackStorage`. The table just above (lines 13-16) also says an empty `FeedbackAttachments:ConnectionString` always selects `EfFeedbackAttachmentBlobStore`, which is true only when a Postgres connection is present. `FeedbackModule`'s design region and `## Session` describe the stand-in correctly.
- Suggestion: State the stand-in in the guide and in the table: Azure when its connection string is set, Postgres when a database connection is set and Azure is not, otherwise `UnavailableFeedbackStorage` (reads empty, writes refused). Drop the claim that the stores are unregistered.
- Source: general
- Disposition notes: Fixed in the fix pass (2026-11-25). The guide's table now has a Postgres connection column and a third row for `UnavailableFeedbackStorage` (reads empty, writes refused). The paragraph below it says `FeedbackModule` registers the stand-in for the row ports and, unless Azure is configured, the byte port; `PostgresDbModule` replaces them only when a Postgres connection is set; with none, reads are empty, removes do nothing, and filing, updating, or uploading throws `InvalidOperationException` with the stand-in's message. The claim that the stores are unregistered is gone.

## Duplicates / conflicts

- None. One reviewer.
