# Feedback items expose last activity and the admin list filters on it

## Description

A feedback item has no "last changed" time, and the admin list cannot filter on one. A client that polls `GET api/admin/feedback` can find new filings (`Status=New`, or `FiledFrom`). It cannot find an older item that changed: a filer reply, a status change, or a note. The only way is to read every item's detail.

The feedback intake tool (a DevTools plugin in timewarp-ganda) polls many app instances with an agent token holding `feedback:read`. It needs two things this change gives it:

- "Snooze until new activity": hide an item until a time or until the filer replies, whichever comes first (the Linear triage behaviour).
- Seeing a filer's reply on an item that was already accepted into a kanban task.

Origin: timewarp-jenzi task 013 (research: cross-repo kanban board and feedback intake as DevTools plugins), `report/analysis.md` §Findings 5 and recommendation I10 (A1).

## Requirements

- `FeedbackItem` gets `LastActivityAt` (`DateTimeOffset`). It is set to `FiledAt` on filing and moves to the entry's `At` whenever a thread entry is added (status change, severity change, admin reply, filer reply, internal note). Store it in Postgres with an index (Task 522 removes the in-memory feedback stores and gates the feedback slice on `postgres`).
- The EF migration backfills existing rows with the newest thread entry's `At`, or `FiledAt` when there is none.
- The admin list item and the admin detail return `LastActivityAt`.
- `ListFeedback.Query` gets `ActivityFrom` (inclusive). It returns items whose `LastActivityAt` is at or after it, and it combines with the other filters as they combine today. `SortBy` gets `LastActivityAt`.
- `feedback:read` is enough to use the filter (no new scope).
- The filer's own views (`list-my-feedback`, `get-feedback`) are unchanged.

## Acceptance criteria

- [x] A filer reply, an admin reply, a note, a status change and a severity change each move `LastActivityAt`. Tests on the domain.
- [x] `ActivityFrom` returns an old item after a filer reply and does not return it before. A handler or integration test.
- [x] An agent token with only `feedback:read` can list with `ActivityFrom`. Extend the per-scope agent HTTP test (`admin-feedback-authorization-agent-tests.cs`).
- [x] Migration backfills existing rows. Checked on a database with feedback rows.
- [x] OpenAPI shows the new field, filter and sort value.

## Goal (implementer restatement)

A poller holding only `feedback:read` must be able to ask "which feedback items changed since T?"
without reading every item's detail. So each `FeedbackItem` carries `LastActivityAt`: the filing
time at first, then the `At` of each new thread entry (status change, severity change, admin reply,
filer reply, internal note). It is a Postgres column with an index; the migration fills existing
rows from the newest thread entry (FiledAt when there is none). The admin list item and admin
detail return it. `ListFeedback.Query.ActivityFrom` (inclusive) keeps items whose `LastActivityAt`
is at or after it, ANDed with the existing filters, and `SortBy` gains `LastActivityAt`. No new
scope. The filer's views do not change.

Done when:

- Domain tests show each of the five mutations moves `LastActivityAt` (and filing sets it to FiledAt).
- A handler test shows `ActivityFrom` skips an old item before a filer reply and returns it after.
- The per-scope agent HTTP test lists with `ActivityFrom` on a `feedback:read`-only token.
- A migration test inserts rows at the previous migration and checks the backfill after migrating.
- An OpenAPI document test finds the `ActivityFrom` parameter, the `LastActivityAt` sort value, and
  the `LastActivityAt` field.

## Checklist

- [x] Domain field and the thread-entry hook
- [x] EF configuration, migration and backfill
- [x] Contracts: list item, detail, query filter, sort column
- [x] Tests
- [ ] Gates (audit blocked: source-file-line-cap on EF-generated migration files)

## Depends on

- 522

## Session

- Created: 518093 (2026-11-24), filed from timewarp-jenzi task 013.
- 2026-11-25 implementer (Claude Opus 5.5, ganda task work): domain, EF, migration, contracts, tests.
  `dev db add-migration` wrote the regenerated model snapshot to
  `projects/web-infrastructure/TimeWarp/Architecture/Persistence/Migrations/` instead of
  `platform/postgres/migrations/`; it was moved over the tracked snapshot (diff was only the new
  column, index, last migration id and the ProductVersion annotation). Worth a dev-cli follow-up if
  it repeats.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 2d2de821-6660-468a-b3f1-24d064a261c6, max-turns 500, Blocked — 2026-11-25T02:06:51Z

## Notes

- Today's list filters: `Status`, `Severity`, `Kind`, `FiledFrom`, `FiledTo`, `IncludeClosed`, `Search`; sort `FiledAt`, `Title`, `Kind`, `Status` (`features/feedback/list-feedback/`).
- The internal concurrency `Version` is not exposed and is not a substitute: it is not a time and cannot be filtered on.
- No webhook or push is wanted (task 331 ruled them out). This keeps intake pull-only.
- Related: 331, 378, 521; timewarp-ganda 687 (board) and the intake plugin task there.

## Results

What changed:

- `FeedbackItem.LastActivityAt`: `FiledAt` at filing; one private `Append` writer sets it to the
  entry's `At` for status change, severity change, admin reply, filer reply and internal note. A
  save-time invariant ties it to the last entry by `Sequence` (`FiledAt` when the thread is empty).
- EF: required column plus a single-column index `IX_feedback_items_LastActivityAt`;
  `EfFeedbackStore.UpdateAsync` marks it modified; `ListAllAsync` filters `ActivityFrom` (inclusive)
  and sorts by `LastActivityAt` (ties: `FiledAt` desc, then id).
- Migration `20261125015554_AddFeedbackLastActivity`: adds the column and index, then backfills each
  row from its highest-`Sequence` activity `At`, or `FiledAt` when there is none.
- Contracts: `ListFeedback.Query.ActivityFrom` (query string, operation remarks name it),
  `FeedbackListSortColumn.LastActivityAt = 4`, `ListFeedback.Item.LastActivityAt`,
  `FeedbackAdminView.LastActivityAt`. Filer contracts (`list-my-feedback`, `get-feedback`) untouched.
  Same `feedback.read` policy; no new scope.
- Tests: domain (`EachThreadMutation_Should_MoveLastActivityAt`), handler
  (`ActivityFrom_Should_ReturnAnOldItemOnlyAfterAFilerReply`: before/after a filer reply, inclusive
  bound, AND with Kind, sort both ways, admin detail), agent HTTP (feedback:read token lists with
  `ActivityFrom`), migration backfill (new `feedback-last-activity-migration-tests.cs`), model
  mapping (index), OpenAPI (`ListFeedback_Should_DocumentLastActivity`).

### How to validate

Smoke: with Docker running, `dotnet run --file source/container-apps/web/features/feedback/feedback-admin-tests.cs`
and `./bin/dev test --project web-infrastructure-tests --filter-class FeedbackLastActivityMigration_`.

Expect: all tests pass; `ActivityFrom_Should_ReturnAnOldItemOnlyAfterAFilerReply` and
`Existing_Rows_Should_Take_The_Last_Entry_Or_FiledAt` among them. Against a running app,
`GET api/admin/feedback?ActivityFrom=<iso>` with a `feedback:read` token returns only items whose
`lastActivityAt` is at or after the bound, and `/openapi/v1.json` lists `ActivityFrom` and the
`LastActivityAt` sort value.

### Gates

- `./bin/dev build`: passed, 0 warnings, 0 errors.
- `dotnet run --file source/container-apps/web/features/feedback/feedback-triage-tests.cs`: 15/15 passed.
- `dotnet run --file source/container-apps/web/features/feedback/feedback-admin-tests.cs`: 21/21 passed (Testcontainers Postgres).
- `tests/container-apps/web/web-infrastructure-tests/feedback-last-activity-migration-tests.cs` and `feedback-model-mapping-tests.cs` (not standalone runfiles; project tests): `./bin/dev test --project web-infrastructure-tests --filter-class Feedback`: 12/12 passed, 0 skipped.
- `tests/container-apps/web/web-server-integration-tests/features/feedback/admin-feedback-authorization-agent-tests.cs` (project test): `./bin/dev test --project web-server-integration-tests --filter-class AdminFeedbackAuthorization_.Agent_Returns_`: 3/3 passed.
- `tests/container-apps/web/web-server-integration-tests/open-api-agent-reviewer-guide-tests.cs` (project test): `./bin/dev test --project web-server-integration-tests --filter-class OpenApiAgentReviewerGuide_`: 4/4 passed.
- `tests/container-apps/web/web-spa-integration-tests/features/admin/feedback-admin-state-tests.cs` (project test, constructor call sites only): `./bin/dev test --project web-spa-integration-tests --filter-class FeedbackAdminState_`: 11/11 passed.
- `./bin/dev check-version`: source 2.0.0-beta.32 is ahead of NuGet 2.0.0-beta.31; no bump needed.
- `ganda repo audit` (run 1): failed `analyzer-suppressions` (EF's `#pragma warning disable 612, 618`
  in the new Designer) and `source-file-line-cap` (new Designer 561 lines; model snapshot 563 vs
  baseline 558). Fix: removed the 612/618 pragma pair from the new Designer (no obsolete API is used;
  `./bin/dev build` still 0 warnings, 0 errors).
- `ganda repo audit` (run 2, after the fix): `analyzer-suppressions` passes. `source-file-line-cap`
  still fails on `20261125015554_AddFeedbackLastActivity.Designer.cs` (559 > 500) and
  `PostgresDbContextModelSnapshot.cs` (563 > baseline 558). Both are EF Core scaffold output. The
  ratchet refuses a new entry or a raised count against origin/master, and the check has no
  per-path exclusion (ganda 1.0.0-beta.38 `SourceFileLineCapRules` only skips `*.g.cs` and similar),
  so any schema change in this repo now fails it. TWA0038 already caps C# here and skips these
  files through `generated_code = true` in `.editorconfig`. Unblock: ganda's line-cap check skips
  EF migration output (`*.Designer.cs`, `*ModelSnapshot.cs`, or paths `.editorconfig` marks
  `generated_code = true`), or the maintainer decides how this repo takes a migration under the
  ratchet.

## Unblock update (2026-11-25)

`timewarp-ganda` task 723 landed and is installed (`1.0.0-beta.38+878bfc9a14211d61e1e75980e5ecb9c725a18c3b`): `source-file-line-cap` and `analyzer-suppressions` now skip EF Core's generated `*.Designer.cs` and `*ModelSnapshot.cs` files.

Resumed walk actions:
1. Remove the three now-excluded EF entries (`20261102105501_AddAgentScopeGrants.Designer.cs`, `20261104193441_AddRoles.Designer.cs`, `PostgresDbContextModelSnapshot.cs`) from `.timewarp/line-cap-baseline.json` (shrinking the baseline cleanly).
2. Merge `origin/master` if needed, run `./bin/dev build` and `ganda repo audit`, check off the Gates checklist item, commit, and finish.
