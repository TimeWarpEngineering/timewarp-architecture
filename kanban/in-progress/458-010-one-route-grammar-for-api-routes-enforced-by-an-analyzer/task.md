# One route grammar for API routes enforced by an analyzer

## Description

Route segments split three ways: PascalCase `/api/Feedback`, `/api/Roles`, `/api/Users/Current/Profile`,
`/api/Hello`; kebab `/api/agent-links`, `/api/admin/feedback`, `/api/identity/...`; and
`/Analytics/TrackEvent` and `/identity/sign-out` outside `/api`. User feedback is `/api/Feedback/...`
while admin feedback is `/api/admin/feedback/...`. `route-grammar.md` covers parameter tokens only, so
nothing enforces a style.

## Requirements

- Write one grammar in `skills/tw-web-api-contracts/references/route-grammar.md` (kebab segments
  under `/api/`).
- Enforce it with an analyzer on `[ApiRoute]` strings (new TWA id; record in
  `AnalyzerReleases.Unshipped.md`).
- Rename the non-conforming routes; regenerate the ingress golden (`dev regenerate-generated`) and
  update SPA callers. This breaks external callers of the demo routes, which the template accepts.
- Browser sign-out routes coordinate with task 458-007.

Owner: contracts (`[ApiRoute]`), skill `tw-web-api-contracts`, convention analyzers.

## Goal (implementer restatement)

Every `[ApiRoute]` template follows one grammar: it starts with `api/`, and every literal segment
is lowercase kebab (`[a-z0-9]+(-[a-z0-9]+)*`). Parameter tokens (`{Name}` / `{Name:constraint}`)
keep their existing grammar. A new convention analyzer, TWA0051, reports any `[ApiRoute]` string
that breaks the grammar, so the style cannot drift again.

Acceptance:

- `route-grammar.md` in `tw-web-api-contracts` states the segment grammar beside the parameter
  grammar and names TWA0051.
- TWA0051 is in `AnalyzerReleases.Unshipped.md` (next free id moves to TWA0052) and has analyzer
  tests (clean kebab route, PascalCase segment, missing `api/` prefix, leading slash, constant
  route, parameter tokens ignored).
- Every non-conforming route is renamed: `api/Feedback` -> `api/feedback`, `api/Hello` ->
  `api/hello`, `api/Roles` -> `api/admin/roles` (beside `api/admin/principals`),
  `api/Users/Current/...` -> `api/users/current/...`, `api/GetCurrentUser` -> `api/users/current`,
  `api/TodoItems` -> `api/todo-items`, `Analytics/TrackEvent` -> `api/analytics/events`,
  `api/weatherforecast` -> `api/weather-forecasts` (run-together words the analyzer cannot see).
- The ingress golden is regenerated; tests and comments that hard-code the old paths are updated.
  SPA callers go through contract `GetRoute`/`RouteTemplate`, so they follow the rename.
- `dev build` is 0 warnings (the analyzer runs on the real contracts, so a missed rename fails it).
- Browser sign-out (`/identity/sign-out*`) is not an `[ApiRoute]`; it stays with task 458-007.

## Checklist

- [x] Implement the requirements above
- [x] Tests or document checks named above pass

## Review

- Round 1: Done

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-13 (Grok G-010, Fable F-014) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.
- Post-deploy verification: re-fetch the live OpenAPI document after deploy and confirm the finding is gone.
- Grooming note: `SignOutBrowserSession` (`/identity/sign-out*`) does not use `[ApiRoute]` (owned by `458-007`); next free convention analyzer id is `TWA0051`; update hardcoded route assertions in `open-api-one-of-tests.cs`, `open-api-anonymous-intent-tests.cs`, `open-api-delete-request-body-tests.cs`, and `open-api-operation-tags-tests.cs` alongside the renames.

## Session

- Created: 2227132 (2026-11-24)
- 2026-11-26 implement: TWA0051 `ApiRouteGrammarAnalyzer`, route renames, golden regenerated, tests
  and skill examples updated (commit 1656c0642).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 0bdd8fa3-217a-4457-b276-5c6d62b9806d, max-turns 500, Done — 2026-11-25T17:07:03Z
- Review oracle round 1: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7), session 01a213ac-f39c-7d62-876f-ebedea64e08e, max-turns 120, artifacts `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md` — 2026-11-26
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a213ac-f39c-7d62-876f-ebedea64e08e, max-turns 120, artifacts general.md, merged.md, disposition.md — 2026-11-25T17:15:04Z

## Results

- Grammar: `skills/tw-web-api-contracts/references/route-grammar.md` now opens with a segment
  grammar (`api/` first, lowercase kebab literal segments, whole-segment tokens, admin-only under
  `api/admin/`). `SKILL.md` points at it (line count kept at 499 for the 500-line skill lint).
- Analyzer: `source/analyzers/timewarp-architecture-convention-analyzers/api-route-grammar-analyzer.cs`
  reports TWA0051 (Warning, so the build fails) on any `[ApiRoute]` value that breaks the grammar,
  hosted or `[ClientOnlyContract]`, const routes included. Recorded in `AnalyzerReleases.Unshipped.md`;
  next free id is TWA0052.
- Renames: `api/Feedback` -> `api/feedback`, `api/Hello` -> `api/hello`, `api/Roles` -> `api/admin/roles`,
  `api/Users/Current/{profile,xai-api-key}` -> `api/users/current/...`, `api/GetCurrentUser` ->
  `api/users/current`, `api/TodoItems` -> `api/todo-items`, `Analytics/TrackEvent` -> `api/analytics/events`,
  `api/weatherforecast` -> `api/weather-forecasts`. SPA callers use contract `GetRoute`/`RouteTemplate`,
  so no SPA edit was needed. Ingress golden now lists `api/analytics`, `api/feedback`, `api/hello`,
  `api/users` and drops the PascalCase prefixes.
- Not changed: `/identity/sign-out*` is not an `[ApiRoute]` (task 458-007). Source-generator test
  fixtures keep their synthetic `api/Roles` strings; TWA0051 does not run in those generator tests.
- Breaking: external callers of the old demo paths get 404 (accepted by the task).
- Follow-up: after deploy, re-fetch `https://arch.timewarp.work/openapi/v1.json` and confirm every
  path is under `/api/` and lowercase kebab.

### Gates

- `./bin/dev build` — pass (0 warnings, 0 errors; TWA0051 runs on the real contracts).
- `./bin/dev test --project timewarp-architecture-analyzers-tests --filter-class Should_Enforce_Route_Grammar`
  (new `tests/analyzers/timewarp-architecture-analyzers-tests/api-route-grammar-analyzer-tests.cs`) — 15/15 pass.
- `dotnet run --file source/container-apps/web/features/feedback/feedback-filing-tests.cs` — 18/18 pass.
- `dotnet run --file source/container-apps/web/features/feedback/feedback-attachment-submit-tests.cs` — 9/9 pass.
- `./bin/dev test --project web-server-integration-tests` (covers the edited roles, feedback, identity,
  profile, and `open-api-*` test files; not standalone runfiles) — 444/444 pass.
- `./bin/dev test --project web-contracts-tests` (role and SharedProblemDetails serialization files) — 48/48 pass.
- `./bin/dev test --project foundation-contracts-tests` (`http-api-service-tests.cs`) — 25/25 pass.
- `./bin/dev test --project web-spa-integration-tests --filter-class SafeMarkdown_Should_` — 17/17 pass.
- `./bin/dev test --project aspire-tests --filter-class GeneratedIngressRoutes_Given_` — 1/1 pass.
- `./bin/dev test --project yarp-integration-tests --filter-class Smoke_Given_` — 12/12 pass.
- Not run locally, CI runs them: `aspire-tests` `IngressSmoke_Given_` (boots the full AppHost) and the
  three edited `web-spa-playwright-tests` files (string-only path updates). None is a standalone runfile.
- `./bin/dev lint-skills` — pass (skills changed).
- `ganda repo audit` — pass.

### How to validate

- Smoke: change any `[ApiRoute]` to `"api/Widgets"` (or `"Analytics/TrackEvent"`) and run `./bin/dev build`;
  then run `./bin/dev test --project timewarp-architecture-analyzers-tests --filter-class Should_Enforce_Route_Grammar`.
- Expect: the build fails with TWA0051 naming the contract, the route, and the broken segment; the test
  class passes 15/15. With the renamed routes the build is clean, and `goldens/ingress/WebServerApiRoutePrefixes.g.cs`
  lists only lowercase prefixes including `api/analytics`.

### Review disposition

Disposition is **clean** after one round. Round 1 was effort 2, roster general, and raised no findings. Final counts: bug 0; suggestion 0; nit 0. No wontfix and no escalation. Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Learnings

- No misread. The `## Goal (implementer restatement)` matches the task: one kebab grammar under `api/` in `route-grammar.md`, a new analyzer id recorded in `AnalyzerReleases.Unshipped.md`, the non-conforming routes renamed, the ingress golden regenerated, and SPA callers following the contract route. The specific new paths (`api/admin/roles`, `api/analytics/events`, `api/users/current`, `api/weather-forecasts`) are the restatement of "rename the non-conforming routes"; the task did not pin those spellings. `/identity/sign-out` stays with task 458-007, as the task and the grooming note say.

