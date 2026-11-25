# Feedback items record the app version and page at filing

## Description

A feedback item records owner, kind, title, body and filed-at. It does not record which version of the app the filer was running, or which page they were on. A bug report triaged days later cannot tell whether a later release already fixed it, and the "capture this page" screenshot is only an attachment with no page path.

The feedback intake tool (a DevTools plugin in timewarp-ganda) reads items from many app instances. The connection tells it which instance an item came from. It cannot tell it the version or the page.

Origin: timewarp-jenzi task 013 (research: cross-repo kanban board and feedback intake as DevTools plugins), `report/analysis.md` §Findings 5 and recommendations I7 and I10 (A2).

## Requirements

- On filing, the server records the app version: the web-server assembly's informational version (the same value the app reports elsewhere, not the stale hard-coded `.well-known` card version). The client does not send it.
- The client sends the page path it was on when the filer opened the feedback form (path only, no query string, so no tokens or PII in query values leak). The server validates it as a relative path with a length limit and stores it. It is optional: a filing without it still succeeds.
- Both are fixed after filing, like the other filing fields.
- The admin detail and admin list item return both. The admin feedback view shows them. The filer's own views do not need them.
- Existing rows keep null for both.

## Acceptance criteria

- [x] Filing stores the server version and the submitted page path. Handler test.
- [x] A page path with a query string or an absolute URL is stored as the path only, or refused, per the validator. Validator test.
- [x] The admin detail and list return both; an agent token with `feedback:read` sees them.
- [x] The admin view shows both.
- [x] Migration adds nullable columns.

## Goal (implementer restatement)

When someone files feedback, the item should also keep two facts that never change afterwards:
which build of the app took the filing (the web-server assembly's informational version, read by
the server, never sent by the client) and which page of the app the filer was on (a path only,
sent by the client, optional). Admins (the admin detail, the admin list, and an agent token with
`feedback:read`) see both; the admin detail page shows both; the filer's own views are unchanged.
Rows filed before this change keep null for both.

Acceptance, as I will prove it:

- Handler test: a submit with a page path stores the server-supplied version and that path; a
  submit without a path still files and stores null for the path.
- Validator test: a page path with a query string, a fragment, an absolute URL, a
  protocol-relative `//host` value, whitespace, or more than the length limit is refused (400);
  an omitted path is accepted.
- Admin detail and admin list items carry `AppVersion` and `PagePath`; the agent-token
  integration test with `feedback:read` reads both.
- The admin item page renders both (version and page rows), with an empty-value fallback.
- An EF migration adds two nullable columns; the model mapping test checks they are nullable
  with the domain length limits.

## Checklist

- [x] Domain fields and `FeedbackItem.File` arguments
- [x] Submit contract (page path), validator, handler (version from the server)
- [x] EF configuration and migration (Task 522 removes the in-memory feedback stores)
- [x] Admin contracts and view
- [x] Tests
- [x] Gates

## Depends on

- 520

## Session

- Created: 518093 (2026-11-24), filed from timewarp-jenzi task 013.
- 2026-11-25 implement: version and page path recorded at filing, admin contracts and page, migration, tests.
  Audit fix in-session: a new runfile copied the neighbour's `#:property NoWarn` and failed
  `analyzer-suppressions`; the shared world files it includes need those suppressions, so the
  task-521 tests moved into `feedback-owner-name-tests.cs` (already in the baseline) instead.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 756d9681-ca1f-44f6-9d98-a2cafeb99bd8, max-turns 500, Done — 2026-11-25T13:21:57Z

## Notes

- Domain: `features/feedback/feedback-item-domain.cs` (`File`). Submit: `features/feedback/submit-feedback/`.
- No "about" endpoint is asked for. The intake tool's instance registry already names each instance and its environment.
- Related: 520, 331, 378; timewarp-ganda 687 and the intake plugin task there.

## Results

What changed:

- Domain `FeedbackItem` gains `AppVersion` (max 128) and `PagePath` (max 512), set only by
  `File(..., appVersion, pagePath)` (optional arguments). `File` refuses a path that is not
  app-relative (`IsAppRelativePath`: one leading `/`, no `?`, `#`, backslash, whitespace or control
  characters); blank values store null. The save-time invariants check both.
- Port `IAppVersionAccessor` (`platform/versioning/i-app-version-accessor-application.cs`); server
  `AssemblyAppVersionAccessor` reads the web-server assembly's `AssemblyInformationalVersion` once,
  registered in `Web.Server` `Program.ConfigureServices`. The client never sends a version.
- `SubmitFeedback.Command.PagePath` (optional) plus `IsPagePath` and `MaxPagePathLength` (512).
  The validator refuses a query string, fragment, absolute or protocol-relative URL, whitespace,
  backslash, or over-length path. The handler stores the accessor's version (cut to 128 so an
  odd build string cannot refuse a filing) and the path.
- `FeedbackAdminView` and `ListFeedback.Item` carry `AppVersion` and `PagePath` (new optional
  constructor arguments, mock samples updated); `FeedbackContractMap.AdminViewAsync` and the list
  handler copy them. Filer contracts unchanged.
- EF: two nullable `character varying` columns; migration `20261125130740_AddFeedbackFilingContext`
  (snapshot moved from the folder `dotnet ef` wrote to into `platform/postgres/migrations/`).
- SPA: `FeedbackPagePath.From` picks the current page, or the newest visited route before the
  `/Feedback` pages (`RouteState.Routes`), keeps the path only, and sends null for a path the
  server would refuse. `SubmitFeedbackActionSet` sends it. The admin item page shows
  "App version" and "Page" facts ("Not recorded" when null).
- `template.json`: the new SPA test file joins the postgres-only exclusion list.

### How to validate

Smoke: with Docker running, `dotnet run --file source/container-apps/web/features/feedback/feedback-owner-name-tests.cs`
Expect: 17/17 passed, including `FeedbackFilingContext_Given_` (server version and page path
stored, null path accepted, bad paths refused, admin list/detail/status views return both).
Manual: `dev run`, open Counter, then Feedback, file a bug; as an administrator open
`/Admin/Feedback/{id}`: "App version" shows the build's informational version, "Page" shows `/Counter`.

Gates (run once each):

- `./bin/dev build`: succeeded, 0 warnings, 0 errors.
- `dotnet run --file source/container-apps/web/features/feedback/feedback-owner-name-tests.cs`: 17/17 passed (task-521 tests live here).
- `dotnet run --file source/container-apps/web/features/feedback/feedback-filing-tests.cs`: 18/18 passed (handler constructor stub only).
- `dotnet run --file source/container-apps/web/features/feedback/feedback-attachment-submit-tests.cs`: 9/9 passed (includes the edited `feedback-attachment-world-tests.cs`).
- `dotnet run --file source/container-apps/web/features/feedback/feedback-attachment-tests.cs`: 14/14 passed (same world file).
- `tests/container-apps/web/web-infrastructure-tests/feedback-model-mapping-tests.cs`: not a standalone runfile; ran `./bin/dev test --no-build --project web-infrastructure-tests --filter-class Feedback_Model_Mapping_`: 3/3 passed.
- `tests/container-apps/web/web-spa-integration-tests/features/feedback/feedback-page-path-tests.cs` (new): not a standalone runfile; ran `./bin/dev test --no-build --project web-spa-integration-tests --filter-class FeedbackPagePath_`: 6/6 passed.
- `tests/container-apps/web/web-server-integration-tests/features/feedback/admin-feedback-authorization-agent-tests.cs` (+ fixture): not a standalone runfile; ran `./bin/dev test --no-build --project web-server-integration-tests --filter-class AdminFeedbackAuthorization_.Agent_Returns_`: 3/3 passed (feedback:read token reads AppVersion = web-server informational version and PagePath `/Counter` on list and detail).
- `ganda repo audit`: first run failed `analyzer-suppressions` (copied NoWarn on a new runfile); fixed by folding the tests into `feedback-owner-name-tests.cs`; rerun passes with one non-blocking advisory (`vscode-window-icon`: local, gitignored `.vscode/settings.json` missing).
- Not run: `dev template-smoke` (CI runs the default variant on the PR; the template.json edit only adds a postgres-off exclusion, covered by the nightly matrix).

