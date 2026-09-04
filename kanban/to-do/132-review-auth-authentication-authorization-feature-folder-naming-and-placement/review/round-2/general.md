# Round 2 — general
**Date:** 2026-09-04
**Scope reviewed:** post-fix inventory.md + disposition.md (M1 118 map / api identity-host); re-verified against this worktree.

## Summary

M1 is fixed: taxonomy, Q6, inventory §9, and reject/defer now describe `api/platform/identity-host/` as already live and correctly state that api does not reference `RoleIds` / `PermissionIds` / `AuthenticationSchemeNames` today (only a sample comment mentions `AuthenticationSchemeNames`; sample + host wiring use string / `AgentTokenDefaults` literals). Round-1 decisions that still hold were left alone and still match the tree (no `web/features/auth*` resurrection, `GetCurrentUser` client-only, SPA fold deferred to 132-001). One new defect in the fix prose: inventory/disposition call the duplicated `AgentTokenDefaults` “byte-identical,” but the web and api copies already diverge on policy-name strings (and where `PrincipalIdClaimType` lives).

## Prior findings

### M1 — Severity: bug
- Status: fixed
- Notes: `source/container-apps/api/platform/identity-host/` is documented in inventory §9 with the five live files; taxonomy rows and Q6 use present tense (“web today” / “Already present/live”); stale “web + api contracts,” “when it grows,” and “both families reference” phrasing is gone. Repo check: api has no `RoleIds`/`PermissionIds`/`AuthenticationSchemeNames` type references — only the agent-bearer sample comment names `AuthenticationSchemeNames`.

## Issues

### M2 — Severity: bug
- Status: open
- File: kanban/to-do/132-review-auth-authentication-authorization-feature-folder-naming-and-placement/inventory.md:181; disposition.md:32,107,122-123
- Description: The M1 fix correctly records a duplicated `AgentTokenDefaults`, but overstates parity as “byte-identical.” In this worktree, web `platform/identity-host/agent-token-defaults-server.cs` has `IdentityReadPolicy = "identity.read"` and `DemoInvokePolicy = "demo.invoke"` (historical aliases; Prefer `PermissionIds`; no `PrincipalIdClaimType` member — web uses `IdentitySessionDefaults.PrincipalIdClaimType`). Api’s copy has `IdentityReadPolicy = "agent-scope:identity:read"`, `DemoInvokePolicy = "agent-scope:demo:invoke"`, and its own `PrincipalIdClaimType = "timewarp:principal_id"`. Shared claim-type *values* that matter for cross-host tokens (`Scheme`, `ScopeClaimType`, principal-id claim string) do align; the whole class is not byte-identical. Web’s Design region already notes api still registers claim-based policies under its own names until evaluator adoption — repeating “byte-identical” in the 118 map can hide that remaining catalog/policy gap the same way M1 hid missing host wiring.
- Suggestion: Soften inventory §9 + taxonomy/Q6/reject-defer to: duplicated host defaults; claim-type strings that tokens carry must stay aligned; policy-name constants already differ (web → `PermissionIds`, api → `agent-scope:*`) until 118 reuses the Features catalogs / evaluator. Do not tell 118 the two `AgentTokenDefaults` classes are already identical.
