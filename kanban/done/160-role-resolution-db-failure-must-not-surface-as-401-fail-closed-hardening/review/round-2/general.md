# Round 2 — general
**Date:** 2026-09-04
**Scope reviewed:** product commit `67339a62` vs origin/master (files listed in review-framework.md) plus surrounding `GetEffectiveRoleIdsAsync` call sites and middleware order

## Summary

Independent re-read of the product diff confirms the contract still holds. Store-read failures are typed at `EffectiveRolesResolver` for any `IPrincipalRoleStore` (filter excludes `OperationCanceledException` / already-typed `RoleResolutionFailedException`); `PrincipalRoleClaimsTransformation` does not catch; `RoleResolutionFailureMiddleware` maps the typed throw to 503 and sits after Development `UseDeveloperExceptionPage` and before `UseAuthentication`/`UseAuthorization`. Call sites (`PermissionEvaluator`, `GetCurrentSession`, `ListPrincipals`, `SetPrincipalRoles`) propagate rather than swallow-as-no-roles. Host-free wrap/cancellation coverage plus the in-proc DI-substituted throwing-store suite (authenticated → 503, anonymous → 401) match the required proof. Filename escape hatch for `middleware`/`exception` tokens is valid under the registered-functions grammar. No issues found.

## Issues
