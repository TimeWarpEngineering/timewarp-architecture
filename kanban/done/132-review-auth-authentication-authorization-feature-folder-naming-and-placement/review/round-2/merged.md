# Round 2 — merged findings
**Date:** 2026-09-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: kanban/to-do/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/disposition.md; inventory.md §9
- Description: 118 map overstated current sharing (api identity-host treated as future; catalogs claimed already referenced by both families).
- Suggestion: Present-tense taxonomy + Q6; document `api/platform/identity-host/`.
- Source: general (round 1)
- Disposition notes: Fixed in round-1 fix loop; round-2 confirmed.

### M2 — Severity: bug — Status: fixed
- File: kanban/to-do/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/inventory.md:181; disposition.md:32,107,122-123
- Description: The M1 fix correctly records a duplicated `AgentTokenDefaults`, but overstates parity as “byte-identical.” Web `IdentityReadPolicy` / `DemoInvokePolicy` are `identity.read` / `demo.invoke` (historical aliases; Prefer `PermissionIds`; no `PrincipalIdClaimType` member). Api’s copy uses `agent-scope:identity:read` / `agent-scope:demo:invoke` and owns `PrincipalIdClaimType`. Shared claim-type *values* that tokens carry (`Scheme`, `ScopeClaimType`, principal-id claim string) do align; the whole class is not byte-identical.
- Suggestion: Soften inventory §9 + taxonomy/Q6/reject-defer: duplicated host defaults; token claim-type strings must stay aligned; policy-name constants already differ until 118 reuses Features catalogs / evaluator. Do not tell 118 the two classes are identical.
- Source: general (round 2)
- Disposition notes: Fixed on this id — inventory §9 + taxonomy/Q6/reject-defer now say duplicated host defaults; token claim-type strings must stay aligned; policy-name constants already differ. Dropped “byte-identical”.

## Duplicates / conflicts

- None. M2 is new on the M1 fix delta, not a re-open of M1.
