# Document or exclude browser-navigation routes and liveness in OpenAPI

## Description

- `POST /identity/sign-out` is documented as only `204` with an empty form body; it returns `303`
  (`Location: /Login`) or `400`, and needs the antiforgery form field.
- `GET /identity/sign-out/antiforgery-token` is documented as `204`; live it returns `200
  application/json` `{formFieldName, requestToken}`.
- `GET /api/identity/entra/challenge` documents `204`, has no `mode` query parameter, and omits the
  `302` OIDC redirect.
- `GET /api/health`'s description names `GET /web-server/alive` as liveness; that route is served
  (200 `Healthy`) but is neither an operation nor in `x-timewarp-excluded-routes`, while
  `info.description` says a missing path is an exclusion.

## Requirements

- Decide per route: document the real contract from the endpoint declaration (303 + `Location`, 400,
  antiforgery field; 200 token body; `mode` enum + 302), or exclude the browser-navigation route and
  list it in `x-timewarp-excluded-routes` with a reason. Record the decision in a Design region.
- List liveness in `x-timewarp-excluded-routes` from the `IngressPaths` constants, and generate the
  health description from the same constants.
- Extend the discovery-coverage test so each served non-operation route is listed.

Owner: hand-written identity endpoints; transformer `source/foundation/foundation-server/open-api-discovery-coverage.cs`.

## Goal (implementer restatement)

The public web OpenAPI document must not lie about the hand-written browser routes, and its
"a missing path is an exclusion" promise must hold for every route web-server serves outside
FastEndpoints.

Decision: the three identity routes stay operations and document their real contract from the
endpoint declaration (they are FastEndpoints, Scalar groups them under Identity, and agents read
them); nothing is excluded for them.

Acceptance:

- `POST /identity/sign-out`: no 204. 303 with a `Location` header (`/Login`), 400 when the
  antiforgery token is missing or invalid, and a required `application/x-www-form-urlencoded` body
  whose one field is the configured antiforgery form field name.
- `GET /identity/sign-out/antiforgery-token`: no 204. 200 `application/json`
  `{formFieldName, requestToken}` from the `SignOutBrowserSession.AntiforgeryToken` type.
- `GET /api/identity/entra/challenge`: no 204. A required `mode` query parameter with enum
  `link` / `bootstrap` (from `EntraTicketProcessor` constants), an optional `returnUrl`, and 302
  with a `Location` header beside the existing 400/401/403/404.
- The decision is in a Design region.
- `x-timewarp-excluded-routes` lists liveness (`IngressPaths.WebServerAlivePath`), and the
  `/api/health` description names liveness from the same constant.
- The other web-server routes that are not operations (brand stylesheet and document, web
  manifest, the chat SignalR hub, the dictation WebSocket) are listed too, from the constants their
  `Map` calls use. `info.description` says Razor pages and static files are not listed.
- The discovery-coverage tests check: liveness is listed; and on the real web-server host, every
  served route that is not a Razor page, static asset, framework route, or Development-only route
  is either an operation or listed.

## Checklist

- [x] Implement the requirements above
- [x] Tests or document checks named above pass

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-08 and M-11 (Fable F-006, F-007, F-013; Grok G-009, G-010) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.
- Post-deploy verification: re-fetch the live OpenAPI document after deploy and confirm the finding is gone.
- Grooming note: if browser-navigation routes are excluded from OpenAPI, update `open-api-operation-tags-tests.cs` and `open-api-anonymous-intent-tests.cs` (which currently assert those routes exist in `/openapi/v1.json`); coordinate `/identity/sign-out*` with task 458-010.

## Session

- Created: 2227132 (2026-11-24)
- 2026-11-26 implement: identity operations document their wire contract; liveness and the
  web-server non-operation routes are in `x-timewarp-excluded-routes`; real-host route walk added.
  Boy-scout for `ganda repo audit`: `kanban-landed` failed on task 528's unlanded review kitchen
  (claim held by this identity, session dead), fixed with `ganda kanban reconcile --task-id 528`;
  `vscode-window-icon` fixed with `ganda repo audit --fix` (writes the ignored `.vscode/settings.json`).

## Results

- Decision (Design region in `features/identity/browser-navigation-open-api-server.cs`): the three
  identity routes stay operations and document the real contract; none is excluded.
  - `POST /identity/sign-out`: 204 removed; 303 with a `Location` header (example `/Login`), 400
    (empty body), and a required `application/x-www-form-urlencoded` body whose one required field
    is `AntiforgeryOptions.FormFieldName` read from the host.
  - `GET /identity/sign-out/antiforgery-token`: 204 removed; 200 `application/json` with the
    `SignOutBrowserSession.AntiforgeryToken` schema (`formFieldName`, `requestToken`).
  - `GET /api/identity/entra/challenge`: 204 removed; required `mode` query parameter with enum
    from `EntraTicketProcessor.ModeLink` / `ModeBootstrap`, optional `returnUrl`, and 302 with
    `Location`, beside the existing 400/401/403/404.
  - Statuses come from `Produces` / `ClearDefaultProduces(204)` in each `Configure`; parameters,
    the form field, and `Location` come from an endpoint-scoped `AddOpenApiOperationTransformer`.
- Liveness: `OpenApiDiscoveryCoverage` lists `GET /web-server/alive` from
  `IngressPaths.WebServerAlivePath`, and the `/api/health` description's liveness sentence is
  built from the same constant.
- Other non-operation routes: new public `OpenApiExcludedRoute` record and an
  `excludedWebRoutes` parameter on `CommonServerModule.AddOpenApi`. Web-server passes
  `WebServerOpenApiExcludedRoutes.All` (`/brand.css`, `/brand.json`, `/manifest.json`,
  `POST /chat-hub/negotiate`, `GET /chat-hub`, the dictation WebSocket), each built from the
  constant its `Map` call uses. `info.description` now says Razor pages, static files, and Blazor
  framework routes are not listed. `see` is written only when an entry has one.
- `llms.txt` and the host-discovery `SKILL.md` name liveness and the other excluded web routes;
  the skill's `sha256` digest in `.well-known/agent-skills/index.json` is updated.
- Tests: `open-api-discovery-coverage-tests.cs` gains a liveness check and `WebServer_Given_`,
  which boots the real web-server host, walks its `EndpointDataSource`, and fails on any served
  route that is neither an operation nor listed (skipping Razor pages, static assets, the
  `{**path:file}` fallback, `_blazor` / `_framework`, `/openapi` / `/scalar`, and the
  Development-only `/health` and DevTools config). A negative check (dropping `/brand.css` and
  `GET /chat-hub` from the list) made it fail naming both. New
  `open-api-browser-navigation-tests.cs` pins the three identity contracts.
- Not changed: the api-server document (no opt-in). `/alive` itself is mapped by `Program.Main`
  (`MapDefaultEndpoints`), which the test host does not run, so its listing is checked by the
  tiny-host liveness test, not the route walk.
- Follow-up: after deploy, re-fetch `https://arch.timewarp.work/openapi/v1.json` and confirm the
  three identity operations have no 204 and `x-timewarp-excluded-routes` lists `/web-server/alive`.

### Gates

None of these test files is a standalone runfile (no `#if !JARIBU_MULTI` guard); each ran through
`./bin/dev test --no-build <files…>` in one invocation after the build.

- `./bin/dev build` — pass (0 warnings, 0 errors).
- `tests/container-apps/web/web-server-integration-tests/open-api-discovery-coverage-tests.cs` — pass (`Document_Given_` 7/7, `WebServer_Given_` 1/1).
- `tests/container-apps/web/web-server-integration-tests/open-api-browser-navigation-tests.cs` (new) — pass 3/3.
- `tests/container-apps/web/web-server-integration-tests/open-api-anonymous-intent-tests.cs` (covers the edited endpoints) — pass 6/6.
- `tests/container-apps/web/web-server-integration-tests/open-api-operation-tags-tests.cs` (covers) — pass 2/2.
- `tests/container-apps/web/web-server-integration-tests/open-api-operation-security-tests.cs` (covers) — pass 6/6.
- `tests/container-apps/web/web-server-integration-tests/open-api-problem-details-tests.cs` (document check over the challenge 400/403) — pass 5/5.
- `tests/container-apps/web/web-server-integration-tests/features/identity/sign-out-browser-session-tests.cs` (covers) — pass 6/6.
- `tests/container-apps/web/web-server-integration-tests/features/identity/entra-challenge-link-tests.cs` (covers) — pass 7/7.
- `tests/container-apps/web/web-server-integration-tests/features/identity/entra-challenge-bootstrap-tests.cs` (covers) — pass 7/7.
- `tests/container-apps/web/web-server-integration-tests/features/identity/entra-challenge-rejection-tests.cs` (covers) — pass 4/4.
- `ganda repo audit` — first run failed (`kanban-landed` on task 528, `vscode-window-icon`); after
  the fixes in ## Session, `ganda repo audit --fix --checks vscode-window-icon` reran the audit:
  pass, all checks.

### How to validate

- Smoke: `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-discovery-coverage-tests.cs tests/container-apps/web/web-server-integration-tests/open-api-browser-navigation-tests.cs`;
  then delete the `BrandStylesheet.Path` entry in
  `source/container-apps/web/projects/web-server/web-server-open-api-excluded-routes.cs` and run the
  first file again. Or run the web host and `curl -s <origin>/openapi/v1.json | jq '.paths["/identity/sign-out"].post.responses, ."x-timewarp-excluded-routes"'`.
- Expect: both files pass (8 + 3). With the entry removed, `WebServer_Given_` fails with
  "Served but neither an operation nor in x-timewarp-excluded-routes: /brand.css". In the
  document, sign-out has 303 (with `Location`) and 400 and no 204, and the excluded list
  includes `/web-server/alive`.
