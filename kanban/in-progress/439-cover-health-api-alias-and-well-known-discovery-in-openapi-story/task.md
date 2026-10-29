# Cover health, api-alias, and well-known discovery in OpenAPI story

## Description

`llms.txt` advertises `GET /api/health`, `GET|POST /api` (tip alias), and
`/.well-known/*` (mcp/server-card.json, agent-skills/index.json,
agent-card.json, webmcp.json). None appear in `/openapi/v1.json` (expected —
Web.Server-only doc — but agents infer not-in-spec = not-exist).

## Goal (implementer restatement)

Agents that read only `/openapi/v1.json` treat a missing path as a route that does not exist. `llms.txt` advertises three groups that the FastEndpoints document omits: `GET /api/health`, the `GET|POST /api` tip alias, and the `/.well-known/*` discovery documents. Each group needs an include-or-exclude decision. Included routes show up as operations. Excluded routes are named in the document itself, Scalar shows that note, and `llms.txt` points at the same note. `GET /api/health` must be a real route and machine-readable, not only a sentence in `llms.txt`.

Decisions:

- **Include** `GET /api/health`. It is a web-server health-check route. The document names the plaintext status body and that the route can open Postgres.
- **Include** `GET` and `POST /api` as alias operations. The server rewrites them to `/api/tip`. The contract stays on `/api/tip`.
- **Exclude** `/.well-known/mcp/server-card.json`, `/.well-known/mcp.json`, `/.well-known/agent-skills/index.json`, `/.well-known/agent-card.json`, `/.well-known/agent.json`, and `/.well-known/webmcp.json`. They are discovery documents, not FastEndpoints operations. The OpenAPI description (what Scalar renders) and `x-timewarp-excluded-routes` list them and link to `/llms.txt`.

Api-server does not opt in. Those routes are not on that host.

## Checklist

- [x] Decide include-vs-exclude for each path
- [x] Either add them to OpenAPI or add an explicit documented exclusion note + link from Scalar/llms.txt
- [x] Verify /api/health exists and is documented somewhere machine-readable

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- Implementation: task-work implementer (2026-10-29)
- `ganda repo audit` passed. Advisory only: `kanban-landed` warns that other live claims (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244) have no usable worktree. Left them alone.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a18564-fccd-7720-a0ec-d623d2aac057, max-turns 500, Done — 2026-10-29T02:24:54Z
- Review oracle round 1 (claude-opus-5-5, effort 2, roster: general), 2026-10-29. 2 open bugs: M1 public `/api/health` 404 through ingress, and M2 `/api` alias 500 AmbiguousMatch at ingress. Fix pass requested. Validation commands passed locally: 6 of 6 and 12 of 12. Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`. No disposition yet.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session cf6a9943-93b5-45bd-b47b-125163fa33da, max-turns 120 — 2026-10-29T02:29:50Z
- Review oracle round 2 (claude-opus-5-5, effort 1, roster: general), 2026-10-29. Delta since round 1 had no product change, so M1 and M2 stay open. Fix pass requested.
- Review oracle round 3 (claude-opus-5-5, effort 1, roster: general), 2026-10-29. Fix pass 2 also made no product change, so M1 and M2 stay open. implementer-grok has ended after one turn on two fix passes in a row. Check that profile before the next fix pass.
- Fix pass 3 (implement-fix-4, claude-opus-5-5), 2026-10-29. Fixed round-3 M1 and M2 in both ingress builders (standalone yarp in-memory routes and the AppHost `#if web` block). M1: added an exact `/api/health` route to web-server. M2: dropped the duplicate `/api/` route; the single `/api` route also matches `/api/`. Both paths are now `IngressPaths.WebServerHealthPath` and `IngressPaths.TipDiscoveryAliasPath`, which web-server's `MapHealthChecks` and `OpenApiDiscoveryCoverage` read too. New `StandaloneYarpIngress_` tests request `/api/health`, `/api`, and `/api/` through the gateway. With the old routes put back, both tests failed; with the fix, the suite passed 19 of 19. `dev build` had 0 warnings. The AppHost route is not covered by an in-proc test (aspire-tests boots the AppHost; not run here).
- Review oracle round 4 (reviewer-claude-fable, claude-fable-5-1, effort 2, roster: general, turn cap 120), 2026-10-29. Delta `2ebd4893d` reviewed. M1 and M2 verified fixed: `dev build` 0 warnings, yarp suite 19 of 19 (both new gateway tests also pass alone), discovery coverage 6 of 6, tip runfile 7 of 7. No new findings. Disposition `clean`. Artifacts: `review/round-4/general.md`, `review/round-4/merged.md`, `review/disposition.md`. Goal restatement matches the task; no misread.

## Results

Web-server opts in (`documentPublicWebRoutes`). Api-server does not.

- `GET /api/health` is an OpenAPI operation. The body is `text/plain` (`Healthy`, `Degraded`, or `Unhealthy`). 200 is Healthy or Degraded. 503 is Unhealthy. The description says a registered `PostgresDbContext` check opens a connection, and that liveness is `GET /web-server/alive`. The route is `AllowAnonymous`.
- `GET` and `POST /api` are OpenAPI operations. They document the rewrite to `/api/tip` (including `/api/`). The payment contract stays on `/api/tip`, and that operation's description gains the same alias sentence.
- Ingress (fix pass 3): both ingress builders route exact `/api/health` and `/api` to web-server. There is one alias route, because a route template ignores a trailing slash. The old separate `/api/` route tied with `/api` and returned 500 `AmbiguousMatchException`. Without the health route, `/api/health` fell to the Api.Server catch-all and returned 404.
- `/.well-known/mcp/server-card.json`, `/.well-known/mcp.json`, `/.well-known/agent-skills/index.json`, `/.well-known/agent-card.json`, `/.well-known/agent.json`, and `/.well-known/webmcp.json` stay out of `paths`. `info.description` (the note Scalar renders) and `x-timewarp-excluded-routes` list them and point at `/llms.txt`. `llms.txt` and the discovery skill point back at that exclusion.

### How to validate

Smoke: from `tests/container-apps/web/web-server-integration-tests`, `dotnet test web-server-integration-tests.csproj -- --filter-class OpenApiDiscoveryCoverage_`. Then `dotnet run source/container-apps/web/features/tip/submit-tip/submit-tip-tests.cs -- --filter-class SubmitTipEndpoint_Given_`. Then, from `tests/container-apps/yarp/yarp-integration-tests`, `dotnet test yarp-integration-tests.csproj`.

Expect: discovery coverage 6 passed. The tip class includes `Api Health_ Should_ Return Plaintext Status` passed (plaintext status name, 200 or 503). The yarp suite passes 19 of 19, including `ApiHealthThroughStandaloneYarp_Should_ReachWebServer` (text/plain status through the gateway) and `TipAliasThroughStandaloneYarp_Should_MatchWebServerTip` (`/api` and `/api/` through the gateway return the same status as web-server's `/api/tip`, not 500). After deploy, `curl -i https://arch.timewarp.work/api/health` returns a plaintext status, and `/api` returns the tip response instead of 500. Api-server still builds; it does not opt in, so its document omits `/api/health` and `x-timewarp-excluded-routes`.

### Review disposition

- Rounds: 4. Roster: general. Effort: 2 (rounds 1 and 4), 1 (rounds 2 and 3).
- Final counts (round 4): bug 0 open / 2 fixed / 0 wontfix; suggestion 0 / 0 / 0; nit 0 / 0 / 0.
- Disposition: `clean`. No wontfix, no escalation. M1 (`/api/health` 404 at ingress) and M2 (`/api` alias 500 at ingress) were fixed in `2ebd4893d` and re-verified in round 4.
- Paths: `review/review-framework.md`, `review/round-4/merged.md`, `review/disposition.md`.

## Review

- Round 1: Blocked — open M1, M2
- Round 2 (effort 1, general): Blocked — M1, M2 still open. Fix pass 1 made no product change (HEAD unchanged at `3f596ae40`). Artifacts: `review/round-2/general.md`, `review/round-2/merged.md`. No disposition yet.
- Round 2: Blocked — open M1, M2
- Round 3 (effort 1, general): Blocked. M1 and M2 are still open. Fix pass 2 (implement-fix-3) made no product change. HEAD is still `3f596ae40`, and the implementer ended after 1 model call. Artifacts: `review/round-3/general.md`, `review/round-3/merged.md`. No disposition yet.
- Round 3: Blocked — open M1, M2
- Round 4 (effort 2, general): Done. Delta `2ebd4893d` fixes M1 and M2 in both ingress builders. Verified: `dev build` 0 warnings, yarp 19 of 19, discovery coverage 6 of 6, tip runfile 7 of 7. 0 open. Disposition `clean` in `review/disposition.md`. Artifacts: `review/round-4/general.md`, `review/round-4/merged.md`.

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json (60 paths, no
health/api-alias/well-known) + https://arch.timewarp.work/llms.txt,
2026-10-28.

## Blocked

2026-10-29: placeholder — work starts now; this is not the final answer
