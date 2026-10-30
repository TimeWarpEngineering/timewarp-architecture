# Round 2 — general
**Date:** 2026-10-29
**Scope reviewed:** fix delta `10bd0ffa8..da2cb14ac` (round-1 `base.sha` → fix commit). Product files:
`source/foundation/foundation-server/open-api-agent-bearer.cs` (new),
`source/foundation/foundation-server/open-api-bearer-security-server.cs`,
`source/foundation/foundation-server/common-server-module.cs`,
`source/container-apps/web/platform/identity-host/open-api-agent-bearer-registration-server.cs` (new),
`source/container-apps/web/projects/web-server/program.cs`,
`source/container-apps/api/platform/identity-host/agent-bearer-policies-server.cs` (new),
`source/container-apps/api/projects/api-server/program.cs`,
`tests/container-apps/web/web-server-integration-tests/open-api-bearer-security-tests.cs`.

## Summary

The fix takes the product strings out of foundation. Hosts now pass an `OpenApiAgentBearer`: the
scheme name plus a policy-to-scope map. Web builds the map by inverting
`AgentScopePermissionSeed.DefaultGrants`. Api builds it from the same `AgentTokenDefaults` policy
constants and `AgentScopes` values that its `AddPolicy` / `RequireClaim` calls use. A permission
granted by two scopes throws. The 401 and 403 descriptions are now appended to any existing text,
falling back to `EndpointSummary.Responses`. They cover handler-level rejections, and
`WWW-Authenticate` is `Required = false`. A null bearer registers no scheme, transformer, or Scalar
preferred scheme. Low risk. Both round-1 findings are resolved and this round found no new defects.

## Re-verification of round-1 findings

- **M1 (bug) — fixed.** `OpenApiBearerSecurity` no longer holds `PolicyScopes`, the `AgentScopes`
  copy, or `agent-token`. `Registration_Should_MatchTheSeedAndTheAgentTokenScheme` asserts the
  scheme equals `AuthenticationSchemeNames.AgentToken` (and web `AgentTokenDefaults.Scheme`), and
  checks every seed pair. `SeedGrant_Should_DocumentTheGrantingScope` walks every
  `DefaultGrants` pair through the generated document. It fails when a new seed permission has no
  probe endpoint, so seed drift cannot pass silently. On api, the map and `AddPolicy` both
  reference the same constants. The Design region in `agent-bearer-policies-server.cs` records why
  `AddPolicy` stays literal (TWA0024 constant-name requirement).
- **M2 (suggestion) — fixed.** `DocumentBearerFailure` appends through `AppendNote`, keeps an
  `EndpointSummary` 403 text, and sets the header `Required = false` with "only when / absent on a
  handler-level" wording. `DomainForbidden_Should_KeepTheHandlerDetail` pins the get-human-ux
  sentence plus the optional header.

## Verification run

- `dotnet test web-server-integration-tests.csproj -- --filter-class OpenApiBearerSecurity_`: 13/13 passed.
- `--filter-class OpenApiDocument_`: 4/4 passed.
- `dotnet build source/container-apps/api/projects/api-server/api-server.csproj`: 0 warnings, 0 errors.

## Observations (not findings)

- No test reads the api-server map back from its registered policies. The two pairs share
  compile-time constants with `AddPolicy`, and each policy name spells its scope
  (`agent-scope:identity:read`). Given the TWA0024 constraint recorded in the Design region, the
  remaining drift risk is too small to raise.

## Issues

None.
