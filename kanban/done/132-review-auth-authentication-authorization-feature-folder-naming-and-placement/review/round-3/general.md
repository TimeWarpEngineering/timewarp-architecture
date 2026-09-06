# Round 3 — general
**Date:** 2026-09-04
**Scope reviewed:** post-M2 inventory.md + disposition.md; re-verified AgentTokenDefaults web vs api.

## Summary

M2 is fixed in the two product docs: inventory §9 and disposition (taxonomy api identity-host row, Q6, reject/defer) no longer call the duplicated `AgentTokenDefaults` “byte-identical.” Prose now states that token claim-type strings must stay aligned and that policy-name constants already differ, and it warns 118 not to treat the classes as identical. Spot-check of the sources still matches that description (web `identity.read` / `demo.invoke`, no `PrincipalIdClaimType`; api `agent-scope:*` plus `PrincipalIdClaimType`). No new defects on the M2 fix delta.

## Prior findings

### M1 — Severity: bug
- Status: fixed
- Notes: Unchanged since round 2. Taxonomy, Q6, inventory §9, and reject/defer still present `api/platform/identity-host/` as already live and correctly state that api does not reference `RoleIds` / `PermissionIds` / `AuthenticationSchemeNames` today.

### M2 — Severity: bug
- Status: fixed
- Notes: “byte-identical” removed from inventory.md and disposition.md. Replacement language covers aligned token claim-type strings (`Scheme`, `ScopeClaimType`, principal-id claim value) and divergent policy names (web historical `identity.read` / `demo.invoke` vs api `agent-scope:identity:read` / `agent-scope:demo:invoke`). Source copies still diverge exactly that way.

## Issues

None.
