# Round 3 — general
**Date:** 2026-11-02
**Scope reviewed:** The Member-decision delta `5a60e94c5..d39a9aa98` (product commit
`d0a9d35d9`, "fix: stop treating a roleless agent as Member"), 25 product files. The whole
branch against `## Goal (implementer restatement)` and the task Description. Rounds 1 and 2
already covered the base implementation `a012755c0`/`f9a137c39` and the fix `5a60e94c5`.

## Summary

The change puts Steve's "agents are not Members" decision in `EffectiveRolesResolver`, the
single source of effective roles, and leaves the approval code as it was. That was the
requested shape. When the stored role list is empty, the resolver reads the principal. A human
or a missing row resolves to Member. An agent or a service resolves to no role. Bootstrap still
unions Administrator and Member onto any kind. A principal-store failure becomes
`RoleResolutionFailedException`, and it can no longer fall back to Member. The admin Principal
page, ListPrincipals, session claims, the permission evaluator, the last-admin guard, and
issuance all call the same resolver, so they agree. `AdminLockoutGuards.SimulateEffectiveRoles`
takes the target's kind. `RolesGrantPermissionAsync` no longer substitutes Member a second time,
and both production callers pass a list that is already effective. Role claims are still
projected only for human-session schemes, so agent tokens get no roles. The Member page,
Principals page, `SetRoleAgentScopes`, and the Design regions all state the new rule.

The implementation matches the restatement and the task. The restatement adds the Member rule
from Steve's Session decision on top of the original Description. That decision is recorded in
`## Session`, so it is not a misread.

## Verification (re-run by this reviewer)

- `./bin/dev build`: Build succeeded, 0 warnings, 0 errors.
- `effective-roles-resolver-tests.cs` runfile: 17/17. It includes roleless human → Member,
  roleless agent and service → none, stored role kept, bootstrap agent → Member+Administrator,
  principal-store failure → typed exception, and assigned roles skip the principal read.
- `set-role-permissions-tests.cs` 15/15, `get-principal-tests.cs` 10/10,
  `list-principals-tests.cs` 13/13.
- `web-server-integration-tests --filter-class AgentScopeApproval_`: 5/5, including
  `MemberScopes_Should_NotApproveARolelessAgent`.
- Full `web-server-integration-tests`: 436 total, 435 passed, 1 skipped, 0 failed.
- The `Microsoft.Extensions.Options` central pin is removed. No `#:package` directive or
  `PackageReference` still names it (`git grep`), and the resolver runfile now compiles from the
  shared framework.
- `template-smoke` was not re-run. This delta does not add, remove, or move files, and it changes
  no `#if` region or template symbol.

## Issues

None.

## Observations (not findings)

- When a principal has no stored role, the resolver now reads `IPrincipalStore` again. That
  includes every claims transformation for a roleless human session and each row in
  ListPrincipals and the last-admin scan, where the caller already holds the `Principal`. Under
  EF this is one more point lookup per call, on top of the role-store read that was already
  there. It is correct, and keeping one resolver signature is the SSOT trade-off the Design
  region chose. If profiling ever shows a cost, a kind-aware overload is the natural follow-up.
- `SimulateEffectiveRoles` defaults `kind` to `Human`. The one production caller passes
  `target.Kind`, and only the tests use the default.
