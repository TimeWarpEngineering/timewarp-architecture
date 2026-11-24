# Feedback items expose last activity and the admin list filters on it

## Description

A feedback item has no "last changed" time, and the admin list cannot filter on one. A client that polls `GET api/admin/feedback` can find new filings (`Status=New`, or `FiledFrom`). It cannot find an older item that changed: a filer reply, a status change, or a note. The only way is to read every item's detail.

The feedback intake tool (a DevTools plugin in timewarp-ganda) polls many app instances with an agent token holding `feedback:read`. It needs two things this change gives it:

- "Snooze until new activity": hide an item until a time or until the filer replies, whichever comes first (the Linear triage behaviour).
- Seeing a filer's reply on an item that was already accepted into a kanban task.

Origin: timewarp-jenzi task 013 (research: cross-repo kanban board and feedback intake as DevTools plugins), `report/analysis.md` §Findings 5 and recommendation I10 (A1).

## Requirements

- `FeedbackItem` gets `LastActivityAt` (`DateTimeOffset`). It is set to `FiledAt` on filing and moves to the entry's `At` whenever a thread entry is added (status change, severity change, admin reply, filer reply, internal note). Store it in Postgres with an index, and in the in-memory store.
- The EF migration backfills existing rows with the newest thread entry's `At`, or `FiledAt` when there is none.
- The admin list item and the admin detail return `LastActivityAt`.
- `ListFeedback.Query` gets `ActivityFrom` (inclusive). It returns items whose `LastActivityAt` is at or after it, and it combines with the other filters as they combine today. `SortBy` gets `LastActivityAt`.
- `feedback:read` is enough to use the filter (no new scope).
- The filer's own views (`list-my-feedback`, `get-feedback`) are unchanged.

## Acceptance criteria

- [ ] A filer reply, an admin reply, a note, a status change and a severity change each move `LastActivityAt`. Tests on the domain.
- [ ] `ActivityFrom` returns an old item after a filer reply and does not return it before. A handler or integration test.
- [ ] An agent token with only `feedback:read` can list with `ActivityFrom`. Extend the per-scope agent HTTP test (`admin-feedback-authorization-agent-tests.cs`).
- [ ] Migration backfills existing rows. Checked on a database with feedback rows.
- [ ] OpenAPI shows the new field, filter and sort value.

## Checklist

- [ ] Domain field and the thread-entry hook
- [ ] EF configuration, migration and backfill; in-memory store
- [ ] Contracts: list item, detail, query filter, sort column
- [ ] Tests
- [ ] Gates

## Session

- Created: 518093 (2026-11-24), filed from timewarp-jenzi task 013.

## Notes

- Today's list filters: `Status`, `Severity`, `Kind`, `FiledFrom`, `FiledTo`, `IncludeClosed`, `Search`; sort `FiledAt`, `Title`, `Kind`, `Status` (`features/feedback/list-feedback/`).
- The internal concurrency `Version` is not exposed and is not a substitute: it is not a time and cannot be filtered on.
- No webhook or push is wanted (task 331 ruled them out). This keeps intake pull-only.
- Related: 331, 378, 521; timewarp-ganda 687 (board) and the intake plugin task there.
