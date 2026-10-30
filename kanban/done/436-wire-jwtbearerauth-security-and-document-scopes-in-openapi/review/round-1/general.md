# Round 1 — general
**Date:** 2026-10-29
**Scope reviewed:** commit `0f66e2636` (framework scope)

## Summary

The change adds an OpenAPI operation transformer. It puts a per-operation `JWTBearerAuth`
requirement on every `agent-token` operation that is not anonymous. The required scope comes
from the endpoint's policy. The transformer also documents bearer 401 and 403 responses, and
the change makes Scalar prefer that scheme. The behavior matches the goal and the original
checklist. There is no document-wide `security`. Cookie-only and anonymous operations are left
alone. Every real `agent-token` contract in `source/container-apps` uses a policy that the
transformer maps.

Verified:
- `dotnet test … -- --filter-class OpenApiBearerSecurity_`: 12 of 12 passed.
- `--filter-class OpenApiDocument_`: 4 of 4 passed.
- The 401 and 403 `WWW-Authenticate` values match both `agent-token` handlers (web and api).

There are two problems. The policy-to-scope map is a hand copy with no drift check. The 403
description ignores the domain 403s those operations return.

## Issues

### Issue 1 — Severity: bug
- File: source/foundation/foundation-server/open-api-bearer-security-server.cs:38
- Description: `PolicyScopes` is a hand-typed inverse of `AgentScopePermissionSeed.DefaultGrants`
  (`source/container-apps/web/platform/authorization/agent-scope-permission-seed-application.cs`)
  and of the api-server `AgentTokenDefaults.*Policy` constants. The `AgentScopes` nested class
  copies `TimeWarp.Identity.AgentScopes`, and `AgentTokenSchemeName` copies
  `AuthenticationSchemeNames.AgentToken`. The Design region says "the integration test pins them".
  The test pins six of the ten map entries. `agent-scope:demo:invoke`, `feedback.note`,
  `feedback.severity`, and `feedback.status` are not exercised. It pins them against literal
  expected strings, not against the seed. Two kinds of change pass every test but publish a wrong document:
  - adding a scope or permission pair to `AgentScopes` and the seed;
  - remapping a permission in the seed. The seed's own Design region expects this: a dedicated
    `human-link:manage` scope would replace `identity:read` for `agent-link.manage.self`.

  In both cases the operation silently documents the wrong scope, or only "Requires an agent
  bearer token" with no scope. That breaks the AGENTS.md hard rule "When two things must agree,
  generate one from the other or add a build check." It is also a template-wide platform
  package (foundation-server) that hard-codes demo and feedback slice policy names.
- Suggestion: pick one.
  - Preferred: derive the map from the source of truth. `CommonServerModule.AddOpenApi`, or a
    host registration, takes a policy-to-scope map. web-server builds it by inverting
    `AgentScopePermissionSeed.DefaultGrants`. api-server builds it from `AgentTokenDefaults`.
    foundation then carries no product strings.
  - Minimum: add a test in web-server-integration-tests, which already references web-server.
    It walks every `(scope, permissionId)` pair in `AgentScopePermissionSeed.DefaultGrants` and
    asserts that an `agent-token` probe with `Policies(permissionId)` documents exactly the
    seed's scope or scopes. Also assert that the transformer's scheme-name constant equals
    `AuthenticationSchemeNames.AgentToken`.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/foundation/foundation-server/open-api-bearer-security-server.cs:110
- Description: `DocumentBearerFailure` overwrites every existing 401 and 403 description on the
  operation. For 403 it says "The bearer authenticated but does not carry the required scope"
  and marks `WWW-Authenticate: Bearer error="insufficient_scope"` as `Required = true`. Handlers
  on these same `agent-token` operations also return domain 403s and 401s as `SharedProblemDetails`,
  and those carry no `WWW-Authenticate`:
  - `get-human-ux-handler-application.cs:55`: "This link belongs to a different agent.";
  - `request-agent-human-link-handler-application.cs:51`: "Only an agent principal can request a human link.";
  - `AgentLinkProblems.Unauthenticated()` returns 401 from the handler.

  An agent that reads the published contract will take that domain 403 for a scope failure. It
  may then mint a new token with more scope, and the call will still fail.
- Suggestion: make the 403 description cover both causes ("insufficient_scope from the bearer
  handler; a handler-level 403 problem+json carries no WWW-Authenticate"). Set the header to
  `Required = false`, or describe it as present only for the scope failure. Append to an
  existing response description instead of replacing it. Pin one operation with a domain 403 in
  the test.
- Status: open
