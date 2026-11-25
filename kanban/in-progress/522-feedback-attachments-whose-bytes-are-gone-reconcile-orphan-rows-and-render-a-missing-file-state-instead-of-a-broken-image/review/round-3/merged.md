# Round 3 — merged findings
**Date:** 2026-11-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: documentation/developer/guides/feedback-attachments.md:11
- Description: Round 1: the guide said that with the postgres flag on and no connection string, the feedback stores are not registered and the endpoints fail. The table also said an empty `FeedbackAttachments:ConnectionString` always selects `EfFeedbackAttachmentBlobStore`.
- Suggestion: State the stand-in: Azure when its connection string is set, Postgres when a database connection is set and Azure is not, otherwise `UnavailableFeedbackStorage` (reads empty, writes refused).
- Source: general
- Disposition notes: Re-verified in round 3 against the guide, `FeedbackModule`, `PostgresDbModule`, and `UnavailableFeedbackStorage`. The table has a Postgres connection column and a third row for `UnavailableFeedbackStorage`. The paragraph names both modules, the empty reads, the no-op removes, and the `InvalidOperationException` message. `Selection_Given_` covers Azure, Postgres, Azure-only, and nothing configured. The later paragraph on `IsFilePresent` matches `FeedbackContractMap.AttachmentsAsync` and `FeedbackBody`.

## Issues

<!-- No new issues. -->

## Duplicates / conflicts

- None. One reviewer. M1 carried from round 1.
