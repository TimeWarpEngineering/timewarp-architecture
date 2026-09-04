# Review framework — task 160

**Date:** 2026-09-04
**Host task:** kanban/done/160-role-resolution-db-failure-must-not-surface-as-401-fail-closed-hardening/
**Diff scope:** branch `task/160-role-resolution-db-failure-must-not-surface-as-401` vs `origin/master` (product commit `67339a62`; later commits are kanban-only). Product files only; exclude kitchen.
**Plan / brief:** Fail-closed hardening so a role-store read failure for an authenticated principal returns HTTP 503, never 401 (Challenge) and never 403 (empty-roles). Wrap `IPrincipalRoleStore.GetRoleIdsAsync` failures as `RoleResolutionFailedException` in `EffectiveRolesResolver`; do not catch in `IClaimsTransformation`; map the typed exception in `RoleResolutionFailureMiddleware` registered after DeveloperExceptionPage (inner) and before `UseAuthentication`. Deterministic in-proc test with a DI-substituted throwing store.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:**
- Round 1: Grok review oracle 2026-09-04 (prior task-work review node; frozen under `round-1/`)
- Round 2: Grok review oracle 2026-09-04 (this task-work review node; independent re-review after implementer re-verify)

## Round 2 note

Round 1 is immutable. Product code did not change after round 1 (implementer re-verify was kanban-only). Round 2 independently re-verifies the same product diff and surrounding call sites; do not rubber-stamp round 1.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Product files in scope

- `source/container-apps/web/features/admin/principals/role-resolution-failed-exception-application.cs`
- `source/container-apps/web/features/admin/principals/role-resolution-failure-middleware-server.cs`
- `source/container-apps/web/features/admin/principals/effective-roles-resolver-application.cs`
- `source/container-apps/web/features/admin/principals/i-effective-roles-resolver-application.cs`
- `source/container-apps/web/features/admin/principals/principal-role-claims-transformation-server.cs`
- `source/container-apps/web/projects/web-server/program.cs`
- `source/container-apps/web/features/admin/principals/effective-roles-resolver-tests.cs`
- `tests/container-apps/web/web-server-integration-tests/features/admin/principals/role-resolution-failure-tests.cs`

## Surrounding call sites to re-verify

- `PrincipalRoleClaimsTransformation.TransformAsync` (auth path)
- `PermissionEvaluator` (`GetEffectiveRoleIdsAsync`)
- `GetCurrentSession` handler
- `ListPrincipals` / `SetPrincipalRoles` handlers
- Middleware order in `program.cs` relative to DeveloperExceptionPage, UseAuthentication, UseAuthorization
