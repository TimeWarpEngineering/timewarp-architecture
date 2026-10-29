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

## Results

Web-server opts in (`documentPublicWebRoutes`). Api-server does not.

- `GET /api/health` is an OpenAPI operation. The body is `text/plain` (`Healthy`, `Degraded`, or `Unhealthy`). 200 is Healthy or Degraded. 503 is Unhealthy. The description says a registered `PostgresDbContext` check opens a connection, and that liveness is `GET /web-server/alive`. The route is `AllowAnonymous`.
- `GET` and `POST /api` are OpenAPI operations. They document the rewrite to `/api/tip` (including `/api/`). The payment contract stays on `/api/tip`, and that operation's description gains the same alias sentence.
- `/.well-known/mcp/server-card.json`, `/.well-known/mcp.json`, `/.well-known/agent-skills/index.json`, `/.well-known/agent-card.json`, `/.well-known/agent.json`, and `/.well-known/webmcp.json` stay out of `paths`. `info.description` (the note Scalar renders) and `x-timewarp-excluded-routes` list them and point at `/llms.txt`. `llms.txt` and the discovery skill point back at that exclusion.

### How to validate

Smoke: from `tests/container-apps/web/web-server-integration-tests`, `dotnet test web-server-integration-tests.csproj -- --filter-class OpenApiDiscoveryCoverage_`. Then `dotnet run source/container-apps/web/features/tip/submit-tip/submit-tip-tests.cs -- --filter-class SubmitTipEndpoint_Given_`.

Expect: discovery coverage 6 passed. The tip class includes `Api Health_ Should_ Return Plaintext Status` passed (plaintext status name, 200 or 503). Api-server still builds; it does not opt in, so its document omits `/api/health` and `x-timewarp-excluded-routes`.

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json (60 paths, no
health/api-alias/well-known) + https://arch.timewarp.work/llms.txt,
2026-10-28.
