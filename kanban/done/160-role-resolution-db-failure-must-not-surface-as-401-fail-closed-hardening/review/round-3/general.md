# Round 3 — general
**Date:** 2026-09-04
**Scope reviewed:** branch `task/160-role-resolution-db-failure-must-not-surface-as-401` vs `origin/master` (product `67339a62` + smoke bump `b4d82514`; files listed in review-framework.md) plus surrounding `GetEffectiveRoleIdsAsync` call sites

## Summary

Independent re-read of the full product + harness diff (including post-round-2 `b4d82514` SmokeDefault `web-jaribu-tests` 102 → 104) confirms the fail-closed contract. `EffectiveRolesResolver` wraps `IPrincipalRoleStore.GetRoleIdsAsync` failures as `RoleResolutionFailedException` while leaving `OperationCanceledException` (and subclass `TaskCanceledException`) and already-typed failures alone; `PrincipalRoleClaimsTransformation` does not catch; `RoleResolutionFailureMiddleware` maps the typed throw to status-only 503 (HasStarted rethrows), registered after Development `UseDeveloperExceptionPage` and before `UseAuthentication`/`UseAuthorization`. Roslynk callers of `GetEffectiveRoleIdsAsync` (`PermissionEvaluator`, `GetCurrentSession`, `ListPrincipals`, `SetPrincipalRoles`, claims transformation) all propagate rather than swallow-as-empty-roles or convert to Challenge 401. Host-free wrap/cancellation coverage (+2) matches the smoke bump; the in-proc DI-substituted suite proves authenticated → 503 and anonymous → 401 and is correctly outside `web-jaribu-tests`. Filename escape hatch for `middleware`/`exception` tokens and Design-region reconciliation hold. No issues found.

## Issues
