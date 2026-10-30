# Round 2 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/foundation/foundation-server/open-api-bearer-security-server.cs
- Description: (round 1) Policy-to-scope map, scope strings, and `agent-token` were hand copies in
  foundation with no drift check.
- Source: general
- Disposition notes: Fixed in `da2cb14ac`. Hosts pass `OpenApiAgentBearer`. Web inverts
  `AgentScopePermissionSeed.DefaultGrants`, and api uses the `AddPolicy` constants. The tests walk every
  seed pair through the document and assert the scheme is `AuthenticationSchemeNames.AgentToken`.

### M2 — Severity: suggestion — Status: fixed
- File: source/foundation/foundation-server/open-api-bearer-security-server.cs
- Description: (round 1) 401/403 descriptions were overwritten. The 403 `WWW-Authenticate` was required,
  which contradicts handler-level 403s.
- Source: general
- Disposition notes: Fixed in `da2cb14ac`. Descriptions are appended (with an `EndpointSummary`
  fallback), the text covers handler-level rejection, and the header is `Required = false`. Pinned by
  `DomainForbidden_Should_KeepTheHandlerDetail`.

## New findings

None.

## Duplicates / conflicts

- None (single reviewer).
