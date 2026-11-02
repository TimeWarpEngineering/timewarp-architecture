# Round 2 — general
**Date:** 2026-11-02
**Scope reviewed:** fix delta `778e45b44..5a60e94c5` (commit 5a60e94c5, "fix: keep generated apps
compiling when postgres or web is off"). Product paths: `.template.config/template.json`, the five
`OrderKnown` call sites, `agent-scope-order-application.cs`, `agent-scope-approval-tests.cs`,
`PrincipalDetailPage.razor`, `RoleDetailPage.razor`.

## Summary

The fix pass closes all five round-1 findings. It adds no behavior change to issuance. The four
EF agent-scope files are on the `(!postgres)` exclude list, and the test helper
`agent-scope-approval.cs` is on the `(!web)` list. One `AgentScopeOrder.OrderKnown` replaced the
three copies. The Member warning, the source-marking test, and the disabled save button are in.
I found no new defects in the delta.

## Re-verification of round-1 findings

- **M1 (bug) — fixed.** `template.json` `(!postgres)` now names
  `ef-role-agent-scope-store-infrastructure.cs`,
  `role-agent-scope-grant-entity-type-configuration-infrastructure.cs`,
  `ef-principal-agent-scope-store-infrastructure.cs`, and
  `principal-agent-scope-grant-entity-type-configuration-infrastructure.cs`. Each name matches a
  file under `platform/authorization/`. The fix also put
  `tests/common/timewarp-testing/applications/agent-scope-approval.cs` (which exists) on the
  `(!web)` list, beside `web-test-server-application.cs`. The plain record types
  (`*-grant-infrastructure.cs`) stay in, as the role-permission pair does. I did not re-run
  `template-smoke --matrix`. The implementer reported it as SUCCEEDED for all six variants.
- **M2 (suggestion) — fixed.** `AgentScopeOrder.OrderKnown` in
  `platform/authorization/agent-scope-order-application.cs` is the only copy. SetRoleAgentScopes,
  SetPrincipalAgentScopes, AgentScopeApproval, GetRole, and GetRoles call it. The private and
  internal copies are gone, and the GetRole/GetRoles reach into `SetRoleAgentScopes.Handler` is
  gone. Its behavior matches the old copies: known strings in `AgentScopes.All` order, ordinal
  match, unknown strings dropped. It adds `ThrowIfNull`, and every caller passes a non-null list.
  The file has Purpose and Design regions (TWA0004), and its name is kebab-case.
- **M3 (suggestion) — fixed.** `RoleDetailPage.razor` shows `MemberAgentScopeWarning` only when
  `RoleId == RoleIds.Member`. The SetRoleAgentScopes Design region records the rule and says the
  handler still stores the list, so Steve's alternative (refuse Member) is not decided by
  default. I checked the wording against `EffectiveRolesResolver`: a principal with no stored
  role resolves to `[Member]`. The sentence is accurate.
- **M4 (suggestion) — fixed.** `agent-scope-approval-tests.cs` builds `AgentScopeApproval` over
  the in-memory stores and a fixed resolver. It asserts that principal grants drop an unknown
  string and read back in catalog order. It checks the effective order and the default, role
  (with role id), and principal marks, plus `identity:read` and `feedback:read` with three
  sources each. The file is executable (100755) and has a Purpose region.
- **M5 (nit) — fixed.** Save agent scopes binds `Disabled="IsSavingAgentScopes"`, which reads
  `IsAnyActive(typeof(PrincipalState.SetPrincipalAgentScopesActionSet.Action))`, the same
  pattern RoleDetailPage uses.

## Goal check

The `## Goal (implementer restatement)` and its round-1 fix list match the original task and the
round-1 ledger. I found no misread.

## Gates re-run by this reviewer

- `dotnet run source/container-apps/web/platform/authorization/agent-scope-approval-tests.cs`:
  1/1 passed.
- `./bin/dev build`: 0 warnings, 0 errors.
- `web-server-integration-tests`, `dotnet run -c Release -- --filter-class AgentScopeApproval_`:
  4/4 passed.
- Not re-run: `template-smoke --matrix` (implementer reported it SUCCEEDED) and the full
  integration suite.

## Issues

None.
