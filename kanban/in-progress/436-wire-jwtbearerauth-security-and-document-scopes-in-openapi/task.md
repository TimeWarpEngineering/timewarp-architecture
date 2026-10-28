# Wire JWTBearerAuth security and document scopes in OpenAPI

## Goal (implementer restatement)

The published OpenAPI document defines `JWTBearerAuth` and never uses it. Agents
cannot see which calls need a bearer, or which scope. FastEndpoints only copies
that scheme onto an operation when the endpoint's auth-scheme list contains the
name `JWTBearerAuth`. These hosts authenticate with `agent-token`, so every
operation stays unsigned.

Done when:

- Each operation that accepts `agent-token` has a per-operation `security` entry
  referencing `JWTBearerAuth`. Anonymous and cookie-only operations stay without
  that requirement. There is no document-wide security block that would force a
  bearer onto public ceremonies.
- Scope annotations match the policies: `identity.read` and
  `agent-scope:identity:read` and `agent-link.manage.self` require `identity:read`
  (agent self and human-link calls, including `api/identity/agent/me`);
  `credential.manage.self` requires `credential:manage` (the credentials
  operations); `demo.invoke` and `agent-scope:demo:invoke` require `demo:invoke`
  (metered capability). Feedback policies map to `feedback:read`, `feedback:note`,
  `feedback:severity`, and `feedback:status`.
- Scalar is pointed at `JWTBearerAuth`, so the Authorize control has a scheme to
  apply.
- Bearer failures are documented on those operations: 401 and 403 bodies are
  `application/problem+json`, and `WWW-Authenticate` is `Bearer`,
  `Bearer error="invalid_token"`, or `Bearer error="insufficient_scope"` as the
  agent-token handler emits them.

## Description

`components.securitySchemes.JWTBearerAuth` (Bearer/JWT) is defined but no
global `security` and zero per-operation `security` entries exist across all
60 paths. Scope requirements (`identity:read`, `credential:manage`,
`demo:invoke`) live only in `auth.md`/`llms.txt`, not the spec.

## Checklist

- [x] Add global or per-operation security referencing JWTBearerAuth
- [x] Annotate which ops need which scopes (agent/me=identity:read, credentials*=credential:manage, metered=demo:invoke)
- [x] Confirm Scalar shows Authorize
- [x] Document 401/403 + WWW-Authenticate problem+json behavior

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- Implementation: grok task-work (2026-10-29). Per-operation `JWTBearerAuth` on `agent-token` operations, with the policy-to-scope map and 401/403 `problem+json` plus `WWW-Authenticate`. `ganda repo audit` exited 0. The advisory `kanban-landed` warning is eight other claims with no worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244). Left untouched. Version stays `2.0.0-beta.29` from task 435; `dev check-version` reports it one prerelease ahead of NuGet `2.0.0-beta.28`.

## Results

`OpenApiBearerSecurity` is an operation transformer registered from `CommonServerModule.AddOpenApi`. `EnableJWTBearerAuth` stays off, and `AddAuth` registers `JWTBearerAuth` as an HTTP bearer scheme for the opaque agent token. The transformer adds one security requirement only when the endpoint lists `agent-token` and the verb is not anonymous. The document has no global `security` entry, so cookie-only and anonymous operations stay unsigned.

Scope strings come from the policy already on the endpoint. `identity.read`, `agent-scope:identity:read`, and `agent-link.manage.self` require `identity:read`. `credential.manage.self` requires `credential:manage`. `demo.invoke` and `agent-scope:demo:invoke` require `demo:invoke`. The feedback policies require `feedback:read`, `feedback:note`, `feedback:severity`, and `feedback:status`. An `agent-token` policy with no mapped scope still requires the bearer and an empty scope list. The operation description names that scope.

Those operations document 401 and 403 as `application/problem+json`. The `WWW-Authenticate` descriptions match the agent-token handler: `Bearer` when no credential was presented, `Bearer error="invalid_token"` when a bearer was rejected, and `Bearer error="insufficient_scope"` when the scope is missing. Scalar's preferred scheme is `JWTBearerAuth`, and `GET /scalar/v1` includes that name so Authorize applies it.

### How to validate

Smoke: from `tests/container-apps/web/web-server-integration-tests` (its `global.json` selects the Microsoft.Testing.Platform runner), `dotnet test web-server-integration-tests.csproj -- --filter-class OpenApiBearerSecurity_`, then the same command with `--filter-class OpenApiDocument_`. `./bin/dev check-version`. `ganda repo audit`.

Expect: `OpenApiBearerSecurity_` 12 passed. `GET /api/identity/agent/me`, `GET /api/agent/bearer-me` (`agent-scope:identity:read`), and `GET /api/agent-links` require `identity:read`. `GET /api/identity/credentials` requires `credential:manage`. `GET /api/demo/metered-capability` requires `demo:invoke`. `GET /api/feedback` requires `feedback:read`. `PUT /api/settings/site` requires `JWTBearerAuth` with an empty scope array. `GET /api/hello` and cookie-only `PUT /api/profile` omit `security`. The document root has no `security`. The scheme description names `identity:read`, `credential:manage`, `demo:invoke`, `application/problem+json`, `invalid_token`, and `insufficient_scope`. Bearer operations include 401 and 403 `application/problem+json` and a `WWW-Authenticate` header description. `GET /scalar/v1` returns 200 and the HTML contains `JWTBearerAuth`. `OpenApiDocument_` 4 passed (forwarded public origin unchanged). `check-version` reports source `2.0.0-beta.29` one prerelease ahead of NuGet `2.0.0-beta.28`. `ganda repo audit` exits 0 with the same advisory `kanban-landed` warning for the eight other claims.

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.
No per-operation security anywhere; global security absent.
