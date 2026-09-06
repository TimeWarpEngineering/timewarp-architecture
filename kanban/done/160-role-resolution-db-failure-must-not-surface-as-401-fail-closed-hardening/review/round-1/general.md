# Round 1 — general
**Date:** 2026-09-04
**Scope reviewed:** branch task/160 vs origin/master (product files listed in review-framework.md) plus surrounding GetEffectiveRoleIdsAsync call sites

## Summary

The change correctly fail-closes role-store read failures to HTTP 503: `EffectiveRolesResolver` wraps store exceptions as `RoleResolutionFailedException` (leaving cancellation and already-typed failures alone), `PrincipalRoleClaimsTransformation` does not swallow, and `RoleResolutionFailureMiddleware` maps the typed throw to 503 when registered after `UseDeveloperExceptionPage` and before `UseAuthentication`. Surrounding call sites (`PermissionEvaluator`, `GetCurrentSession`, `ListPrincipals`, `SetPrincipalRoles`) all go through the resolver and let the exception propagate, so they share the same mapper rather than becoming Challenge 401 or empty-roles 403. Coverage matches the contract (host-free wrap/cancellation + in-proc authenticated→503 / anonymous→401). No issues found.

## Issues
