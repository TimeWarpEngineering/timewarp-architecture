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

## Goal (implementer restatement)

Two production feedback items list an image attachment whose bytes were lost before task 352.
The pages show a broken image and a download link that 404s. After this task:

- The server says, per attachment, whether its file is still stored (`GetFeedback` and
  `GetFeedbackForAdmin`, and every admin mutation that returns the admin view). The SPA reads
  that flag; it does not probe download URLs.
- `FeedbackBody` (shared by the filer page and the admin page) renders an attachment whose file
  is gone as its file name with "file no longer stored", both in the attachment list and where
  the body markdown shows it as an image or a link.
- The two existing orphan rows get the missing-file state through one documented choice
  (mark vs. remove), recorded under `## Session`.
- The in-memory feedback stores, their module, and `InMemoryBlobWarning` are gone. Bytes live in
  Azure Blob Storage when configured, else Postgres.
- With the `postgres` template flag off, the whole feedback slice (server, SPA, tests) is
  compiled out and the generated app still builds with 0 warnings.
- Tests that ran on the in-memory stores run on Postgres instead. A Jaribu test proves an
  orphan row renders the missing state.

Acceptance: `dev build` 0 warnings; the changed test files pass; `dev template-smoke --matrix`
(postgres off) builds; screenshots of the missing-file state (admin and filer views, including
the body image) are in `evidence/`; `ganda repo audit` passes.

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

- [x] Add a file-present flag to the attachment detail contracts (`feedback-item-details-contracts.cs`,
      `feedback-admin-view-contracts.cs`) filled by the handler from `IFeedbackAttachmentBlobStore`
- [x] `FeedbackItemPage` and the admin item view render the missing-file state; body markdown
      image for a missing attachment renders that state too
- [x] Decide reconcile vs remove for existing orphan rows; implement as an idempotent startup or `dev` verb
      pass, not a hand SQL script (verify locally against seeded orphan rows in Postgres during the PR walk; live `arch.timewarp.work` reconcile runs post-deploy)
      — chose mark, derived on read: no stored flag and no pass to run (see `## Session`)
- [x] Delete the in-memory feedback stores, their module, and `InMemoryBlobWarning`; mode
      selection becomes Azure, then Postgres
- [x] Gate the feedback slice on the `postgres` flag; `dev template-smoke --matrix` green
- [x] Move feedback tests off the in-memory stores to the Postgres lane
- [x] Jaribu tests: orphan row renders missing state
- [x] Screenshots of the missing-file attachment state (admin + filer views, including body markdown image) captured locally from the running app in `evidence/` (live `arch.timewarp.work` verification runs post-deploy)
- [x] `dev build`, `ganda repo audit`

## Session
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session e556aad5-a349-4cf5-8ac5-947325901f05, max-turns 500, Done — 2026-11-24T20:59:49Z
- Reviewer: grok session 01a20f5b-b48c-7613-8231-9dc945372089 (2026-11-25), effort 3, roster general, round 1. Open finding M1 in `review/round-1/merged.md`. No disposition yet.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a20f5b-b48c-7613-8231-9dc945372089, max-turns 200, artifacts general.md, merged.md — 2026-11-24T21:10:07Z

### 2026-11-25 — implementer (task-work walk)

**Reconcile decision: mark the rows, derived on every read (no stored flag, no data change).**
`IFeedbackAttachmentBlobStore` gained `ExistsAsync`, and every view that lists attachments
(`GetFeedback`, `GetFeedbackForAdmin`, and the admin writes that return `FeedbackAdminView`)
sets `GetFeedback.Attachment.IsFilePresent` from it. The two production rows therefore show
"file no longer stored" as soon as this build deploys, with nothing to run.
Why not a stored "file missing" column plus a startup or `dev` pass: the column would have to
agree with the byte store, and AGENTS.md says to derive one from the other instead. A derived
flag also covers any later loss (an Azure blob deleted out of band), and there is no job to
schedule, rerun, or make idempotent. Why not remove the rows and strip the body image lines:
that rewrites what the filer wrote and loses the file names the requirement asks us to show.
Verified locally on seeded orphan rows in Postgres: the runfile tests delete the bytes of a
filed attachment and read the filer and admin views (`IsFilePresent` false), and the Playwright
test does the same through the browser (screenshots in `evidence/`). Live check after deploy:
`GET /api/admin/feedback/01a12f40-fd5d-77ca-beff-b33d0d65c964` and
`/01a126d4-e055-7344-9b90-59b8e0749615` list their attachment with `isFilePresent: false`, and
both item pages show the missing-file state.

**No-database hosts.** Development validates DI at build, so a host built with the postgres flag
but no connection string (the direct-hosted Web.Server integration tests, an unconfigured
consumer) still needs the feedback ports registered. `UnavailableFeedbackStorage` fills them, the
same pattern as `UnavailableProtectedSecretStore`: reads are empty, writes throw. It keeps nothing,
so it is not a lossy store. `PostgresDbModule` replaces it. `FeedbackAttachmentBlobModule` became
`FeedbackModule` (it also registers `FeedbackOwnerNames`).

**postgres flag off.** `template.json` excludes the server slice (`features/feedback/**`), the SPA
pages and states, `FeedbackBody`, the paste module, `CaptureForFeedback`, and the feedback-only
tests. Shared files use `#if postgres` in C# only (`global-usings.cs`, `state-json-context.cs`,
sign-out, `program.cs`); no Razor conditionals. The Ask thumbs (which file feedback) moved their
handler into `AskAnswerBar.razor.cs` behind `#if postgres` and render only when `CanFileFeedback`.
The admin `@using` moved from the root `_Imports.razor` to `features/admin/feedback/_Imports.razor`.
Nav entry, routes, and WebMCP tools come from the page registry and catalog, so excluding the
pages removes them. Tests that keep running without postgres gate their feedback assertions.
Not changed: feedback permission ids and agent scopes (identity data, not the slice), and the
static `llms.txt` / `auth.md` / host-discovery `SKILL.md` text, which still names `/Feedback` as an
example route in a postgres-off app.

**Postgres lane for tests.** Co-located runfiles include `feedback-postgres-stores-tests.cs`:
one migrated template database per process, cloned per world (`CREATE DATABASE ... TEMPLATE`),
EF stores over a new context per call. In-proc HTTP and Playwright tests that file feedback use
`PostgresWebHost` (timewarp-testing), a web host over its own migrated database. Both turn
connection pooling off for those databases; with pooling, a Playwright run of several hosts hit
Postgres's `max_connections` (53300). Linking attachments now needs a real item (the FK the
in-memory store never checked), so three tests file one first. `Screenshot_Given_Wasm` waits for
the attachment buttons to be enabled: with Postgres the page's initial load keeps them disabled
long enough that the old click was dropped.

**Boy scout / audit:** recorded under `## Results`.

### 2026-11-25 — implementer fix pass (review round 1)

M1 fixed in `documentation/developer/guides/feedback-attachments.md`. The store table gained a
Postgres connection column and a row for `UnavailableFeedbackStorage`. The paragraph below it now
describes the stand-in (registered by `FeedbackModule`, replaced by `PostgresDbModule` only when a
Postgres connection is set; reads empty, removes no-ops, filing/updating/uploading throws) instead
of saying the stores are unregistered. Doc-only; no product or test code changed.

## Results

The server reports `IsFilePresent` per attachment from the byte store; `FeedbackBody` (filer and
admin item pages) renders `MissingFile` (name + "file no longer stored") for a missing file in the
list and wherever the body shows it as an image or link. In-memory feedback stores, their module,
and `InMemoryBlobWarning` are deleted; bytes are Azure, then Postgres. The feedback slice compiles
out with `postgres` off. Feedback tests run on Postgres. Evidence: `evidence/filer-item-missing-files.png`,
`evidence/admin-item-missing-files.png` (from `FeedbackMissingFile_Given_Wasm`, a real in-proc
WASM host over Postgres with both files' bytes deleted).

### Gates

- `./bin/dev build` — passed, 0 warnings, 0 errors.
- `dotnet run --file source/container-apps/web/features/feedback/feedback-attachment-submit-tests.cs` — 9/9 passed.
- `dotnet run --file source/container-apps/web/features/feedback/feedback-attachment-tests.cs` — 14/14 passed.
- `dotnet run --file source/container-apps/web/features/feedback/feedback-filing-tests.cs` — 18/18 passed.
- `dotnet run --file source/container-apps/web/features/feedback/feedback-admin-tests.cs` — 20/20 passed.
- `dotnet run --file source/container-apps/web/features/feedback/feedback-owner-name-tests.cs` — 9/9 passed.
- `dotnet run --file source/container-apps/web/features/feedback/feedback-triage-tests.cs` — 28/28 passed (three classes: 11, 3, 14).
- `feedback-postgres-stores-tests.cs`, `feedback-attachment-world-tests.cs`, `feedback-triage-world-tests.cs` — support files (no runner); run through the runfiles above.
- `dotnet run --file source/container-apps/web/platform/agent-discovery/markdown-content-negotiation-tests.cs` — 6/6 passed.
- `./bin/dev test --project web-server-integration-tests --filter-class Feedback` — 19/19 passed (admin-feedback-authorization-agent/human, upload-feedback-attachment-endpoint, feedback-attachment-blob-mode; csproj tests, not standalone runfiles).
- `./bin/dev test --project web-server-integration-tests --filter-class OpenApi` — 56/56 passed (open-api-agent-reviewer-guide-tests, open-api-operation-tags-tests).
- `./bin/dev test --project web-spa-integration-tests --filter-class SafeMarkdownRender_` — 17/17 passed (two new orphan-attachment render tests).
- `./bin/dev test --project web-spa-integration-tests --filter-class NavigationDestinations_` — 8/8 passed.
- `./bin/dev test --project web-spa-integration-tests --filter-class PageRegistry_` — 7/7 passed.
- `./bin/dev test --project web-spa-integration-tests --filter-class SignOutState_` — 2/2 passed.
- Not run locally: `action-catalog-tests.cs` (`ActionCatalog_`) and the `CatalogAgent_` partial class files (`catalog-agent-feedback-page-context-tests.cs`, `-relay-`, `-screenshot-`, `-test-doubles`, `-test-fixture`, `-webmcp-tests.cs`). They boot the Aspire AppHost through `SpaSessionFixture`, which a worker must not run (shared user secrets); CI runs them. With postgres on, their edits are `#if postgres` gates only, so the compiled tests are unchanged.
- `./bin/dev test --project web-spa-playwright-tests --filter-class FeedbackAttachmentPlaywright_` — 9/9 passed (includes the new `FeedbackMissingFile_Given_Wasm`).
- `./bin/dev test --project web-spa-playwright-tests --filter-class Screenshot_Given_Wasm` — 1/1 passed.
- `./bin/dev test --project web-spa-playwright-tests --filter-class AskSignInPlaywright_` — 5/5 passed.
- `./bin/dev template-smoke --matrix` — SUCCEEDED: SmokeDefault, SmokeNoPostgres, SmokeNoApi, SmokeNoWeb, SmokeNoGrpc, SmokeNoYarp all built with 0 warnings; web-jaribu aggregator 494/494 (default) and 410/410 (postgres off).
- `ganda repo audit` — first run failed: `kanban-landed` (Error: task 315's claim kitchen differed from origin-home; another task's claim) and `vscode-window-icon` (Warning: ignored `.vscode/settings.json` missing). `ganda repo audit --fix` fixed both (boy scout); its verification pass: 44 passed, 0 failed, 2 skipped. Nothing in this branch's tree changed.

### Gates — fix pass (review round 1)

- `./bin/dev build` — passed (Build completed successfully).
- Test files: none. The fix edits only `documentation/developer/guides/feedback-attachments.md`; no production code or test file changed, so no test file covers it.
- `ganda repo audit` — passed ("Repository passes all audit checks.").

### How to validate

Smoke:

```bash
./bin/dev build
dotnet run --file source/container-apps/web/features/feedback/feedback-attachment-submit-tests.cs
./bin/dev test --project web-spa-integration-tests --filter-class SafeMarkdownRender_
./bin/dev test --project web-spa-playwright-tests --filter-class FeedbackMissingFile_
```

Expect: build 0 warnings. `Get_Should_ReportAFileWhoseBytesAreGoneAsMissing` passes (the stored
file has `IsFilePresent` true, the one whose bytes were deleted false). The render tests pass:
an orphan image in the body and in the list renders `data-qa="MissingFile"` with its name and
"file no longer stored", and no `<img>` or link to its download path. The Playwright test passes
and writes `filer-item-missing-files.png` and `admin-item-missing-files.png` under the test
output's `playwright-screenshots/FeedbackMissingFile_Given_Wasm/`, matching `evidence/`.
After deploy: `GET /api/admin/feedback/01a12f40-fd5d-77ca-beff-b33d0d65c964` shows
`isFilePresent: false` on its attachment, and its item page shows the missing-file state.

## Review

- Round 1: Blocked — open M1
- Round 1 fix pass: M1 fixed (guide states the `UnavailableFeedbackStorage` stand-in)

## Notes

Verified 2026-11-24 through the agent API (`feedback:read`): `GET /api/admin/feedback/01a12f40-fd5d-77ca-beff-b33d0d65c964`
and `/01a126d4-e055-7344-9b90-59b8e0749615` list attachments `01a12f40-f348-76b7-a1c5-9e0702feeb0e`
(309049 B) and `01a126d4-7dc2-752f-aef1-8db79a34c205` (34330 B); both downloads return 404.
Problem text lives in `feedback-problems-application.cs:40`. Store selection: `postgres-db-module-server.cs:146`.

## Learnings

### Misread — reconcile pass

The checklist required an idempotent startup or `dev` verb pass to mark the orphan rows or to remove them. `## Goal (implementer restatement)` narrowed that to "one documented choice (mark vs. remove)" and did not restate the pass. The implementation follows the restatement: `IsFilePresent` is derived from `ExistsAsync` on every read, with no stored flag and no job. The rows still show "file no longer stored" on deploy, the file names stay, and the items are not deleted, so the user-visible requirement holds. Not filed as a finding.
