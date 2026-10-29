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
- Those scope strings are not a second copy inside foundation. Web builds the
  policy-to-scope map by inverting `AgentScopePermissionSeed.DefaultGrants`. Api
  builds it from the same policy-and-scope pairs it registers. Foundation stores
  no product policy names, scope strings, or the `agent-token` scheme name. A
  test walks every seed pair and reads that scope back from the OpenAPI operation,
  and the scheme name it matched is `AuthenticationSchemeNames.AgentToken`.
- Scalar is pointed at `JWTBearerAuth`, so the Authorize control has a scheme to
  apply.
- Bearer failures are documented on those operations: 401 and 403 bodies are
  `application/problem+json`. `WWW-Authenticate` is `Bearer`,
  `Bearer error="invalid_token"`, or `Bearer error="insufficient_scope"` when the
  agent-token handler emits them. A 403 description covers both an insufficient
  scope and a handler-level rejection such as "This link belongs to a different
  agent." An existing response description is kept. The header is not required,
  and the text says it appears only for the scope failure.

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
- [x] Web map is the inverse of AgentScopePermissionSeed; api map uses the same policy and scope constants as AddPolicy; foundation holds no product strings
- [x] Test walks every seed grant and keeps a domain 403 description with optional WWW-Authenticate

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- Implementation: grok task-work (2026-10-29). Per-operation `JWTBearerAuth` on `agent-token` operations, with the policy-to-scope map and 401/403 `problem+json` plus `WWW-Authenticate`. `ganda repo audit` exited 0. The advisory `kanban-landed` warning is eight other claims with no worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244). Left untouched. Version stays `2.0.0-beta.29` from task 435; `dev check-version` reports it one prerelease ahead of NuGet `2.0.0-beta.28`.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a184c1-775f-71b2-97cb-2a68acefad23, max-turns 500, Done — 2026-10-28T23:30:58Z
- Review round 1 (2026-10-29): review oracle claude-opus-5-5, effort 2, roster general. Re-ran `OpenApiBearerSecurity_` (12/12) and `OpenApiDocument_` (4/4). Open: M1 bug (policy-to-scope map has no drift check against `AgentScopePermissionSeed`) and M2 suggestion (403 description and required `WWW-Authenticate` contradict domain 403s). Fix pass needed. See `review/round-1/merged.md`. The goal restatement matches the task, so no misread.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session 6901262d-7915-46e1-abe0-28ca80be3467, max-turns 120 — 2026-10-28T23:34:58Z
- Fix pass (implement-fix-2, 2026-10-29): hosts pass `OpenApiAgentBearer`. Web inverts `AgentScopePermissionSeed.DefaultGrants`. Api `AddPolicy` stays two compile-time constants because TWA0024 ignores a loop, and `AgentBearerPolicies` builds the document map from those same constants and `AgentScopes`. 403 text is appended, including an `EndpointSummary` response when FastEndpoints has not copied it yet, and `WWW-Authenticate` is optional. `OpenApiBearerSecurity_` 13 passed (seed walk plus the human-ux sentence). `OpenApiDocument_` 4 passed. `api-server` build 0 warnings. `dev check-version` still reports `2.0.0-beta.29` one prerelease ahead of NuGet `2.0.0-beta.28`. `ganda repo audit` exited 0. The same advisory `kanban-landed` warning remains for the eight other claims; those worktrees are not this task.

## Results

`OpenApiBearerSecurity` is an operation transformer registered from `CommonServerModule.AddOpenApi` when the host passes an `OpenApiAgentBearer`. `EnableJWTBearerAuth` stays off. `AddAuth` registers `JWTBearerAuth` as an HTTP bearer scheme whose description lists the host's scopes. The transformer adds one security requirement only when the endpoint lists the host scheme and the verb is not anonymous. The document has no global `security` entry, so cookie-only and anonymous operations stay unsigned. A null bearer (the three-argument `AddOpenApi`) registers neither the scheme nor the transformer, and Scalar names no preferred scheme.

Web builds that map with `OpenApiAgentBearer.PolicyScopesFromGrants(AgentScopePermissionSeed.DefaultGrants)` and `AuthenticationSchemeNames.AgentToken`. One permission granted by two scopes throws. Api registers `AgentTokenDefaults.IdentityReadPolicy` and `DemoInvokePolicy` as compile-time `AddPolicy` names (TWA0024), and `AgentBearerPolicies.OpenApiBearer` maps those same names to `AgentScopes.IdentityRead` and `AgentScopes.DemoInvoke`. Foundation stores the protocol names (`JWTBearerAuth`, `application/problem+json`, `invalid_token`, `insufficient_scope`) and no product policy, scope, or scheme string.

The web document therefore follows the seed: `identity.read` and `agent-link.manage.self` require `identity:read`; `credential.manage.self` requires `credential:manage`; `demo.invoke` requires `demo:invoke`; the four feedback permissions require `feedback:read`, `feedback:note`, `feedback:severity`, and `feedback:status`. The api document's `agent-scope:identity:read` and `agent-scope:demo:invoke` policies require the same scopes, because those policy names are what api registers. An `agent-token` policy with no mapped scope still requires the bearer and an empty scope list.

401 and 403 stay `application/problem+json`. An existing description is kept; when FastEndpoints has not copied `EndpointSummary.Responses` yet, that summary text is used, then the bearer note is appended. The 403 note covers an insufficient scope and a handler-level rejection. `WWW-Authenticate` is documented and not required, and the text says the header appears only when the agent-token handler emits it. Scalar's preferred scheme is `JWTBearerAuth` when a bearer was registered, and `GET /scalar/v1` includes that name.

### How to validate

Smoke: from `tests/container-apps/web/web-server-integration-tests` (its `global.json` selects the Microsoft.Testing.Platform runner), `dotnet test web-server-integration-tests.csproj -- --filter-class OpenApiBearerSecurity_`, then the same command with `--filter-class OpenApiDocument_`. `dotnet build source/container-apps/api/projects/api-server/api-server.csproj`. `./bin/dev check-version`. `ganda repo audit`.

Expect: `OpenApiBearerSecurity_` 13 passed, including a walk of every `AgentScopePermissionSeed.DefaultGrants` pair (`identity.read` and `agent-link.manage.self` on `identity:read`, `credential.manage.self` on `credential:manage`, `demo.invoke` on `demo:invoke`, and the four feedback permissions on `feedback:read`, `feedback:note`, `feedback:severity`, and `feedback:status`). The registered scheme equals `AuthenticationSchemeNames.AgentToken` and web `AgentTokenDefaults.Scheme`. `GET /api/agent-links/human-ux` keeps "This link belongs to a different agent.", mentions handler-level rejection, and marks `WWW-Authenticate` optional. `PUT /api/settings/site` requires `JWTBearerAuth` with an empty scope array. `GET /api/hello` and cookie-only `PUT /api/profile` omit `security`. The document root has no `security`. The scheme description names `identity:read`, `credential:manage`, `demo:invoke`, `application/problem+json`, `invalid_token`, and `insufficient_scope`. Bearer operations include 401 and 403 `application/problem+json` and an optional `WWW-Authenticate` header. `GET /scalar/v1` returns 200 and the HTML contains `JWTBearerAuth`. `OpenApiDocument_` 4 passed (forwarded public origin unchanged, three-argument `AddOpenApi`). `api-server` builds, so TWA0024 still sees the two `AddPolicy` constants. `check-version` reports source `2.0.0-beta.29` one prerelease ahead of NuGet `2.0.0-beta.28`. `ganda repo audit` exits 0.

## Review

- Round 1: Blocked — open M1, M2

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.
No per-operation security anywhere; global security absent.
