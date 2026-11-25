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

- [ ] Implement the requirements above
- [ ] Tests or document checks named above pass

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
