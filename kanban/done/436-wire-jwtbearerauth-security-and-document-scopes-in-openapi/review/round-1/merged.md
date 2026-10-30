# Round 1 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/foundation/foundation-server/open-api-bearer-security-server.cs:38
- Description: The policy-to-scope map, the scope strings, and the `agent-token` scheme name
  are hand copies of `AgentScopePermissionSeed.DefaultGrants`, `TimeWarp.Identity.AgentScopes`,
  the api-server `AgentTokenDefaults` policies, and `AuthenticationSchemeNames.AgentToken`.
  No build check or test ties them to those sources. The test pins six of the ten entries
  against literals. Adding a scope or remapping one in the seed publishes a wrong or empty
  scope with every test still green. That breaks the AGENTS.md hard rule "generate one from the
  other or add a build check."
- Suggestion: have the host pass in a map derived from the seed and `AgentTokenDefaults`
  (preferred: foundation then carries no product strings). At minimum, add a test that walks
  every `(scope, permission)` pair in `AgentScopePermissionSeed.DefaultGrants` and asserts the
  documented scope. The same test should assert the scheme name equals
  `AuthenticationSchemeNames.AgentToken`.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/foundation/foundation-server/open-api-bearer-security-server.cs:110
- Description: The 401 and 403 descriptions on the operation are always overwritten. The 403
  `WWW-Authenticate: insufficient_scope` header is marked required. Handlers on the same
  `agent-token` operations return domain 403s and 401s with no such header, for example
  get-human-ux "This link belongs to a different agent." and request-agent-human-link "Only an
  agent principal…". The contract tells agents that a domain 403 is a scope failure.
- Suggestion: the 403 description should cover both causes. Set the header to `Required = false`,
  or say it appears only on a scope failure. Append to an existing description instead of
  replacing it. Pin one domain-403 operation in the test.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
