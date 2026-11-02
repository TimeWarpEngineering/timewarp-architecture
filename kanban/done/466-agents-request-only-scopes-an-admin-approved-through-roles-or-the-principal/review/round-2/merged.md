# Round 2 — merged findings
**Date:** 2026-11-02
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: .template.config/template.json:98
- Description: The four EF agent-scope files were missing from the `(!postgres)` exclude list.
- Source: general (round 1)
- Disposition notes: Fixed in 5a60e94c5. All four are excluded. `agent-scope-approval.cs` is
  also excluded for `(!web)`. The implementer reported `template-smoke --matrix` SUCCEEDED.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/platform/authorization/agent-scope-order-application.cs:22
- Description: There were three copies of `OrderKnown`.
- Source: general (round 1)
- Disposition notes: Fixed in 5a60e94c5. `AgentScopeOrder.OrderKnown` is the only copy, and all
  five sites call it.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor:110
- Description: Member agent scopes reach every unassigned principal, and the editor gave no warning.
- Source: general (round 1)
- Disposition notes: Fixed in 5a60e94c5. The warning shows on Member only, and the
  SetRoleAgentScopes Design region records the rule. Refusing Member is still Steve's call.

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/platform/authorization/agent-scope-approval-tests.cs:36
- Description: No test covered effective-set source marking.
- Source: general (round 1)
- Disposition notes: Fixed in 5a60e94c5. The co-located Jaribu test passed 1/1 when the reviewer
  re-ran it.

### M5 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalDetailPage.razor:171
- Description: Save agent scopes was not disabled while saving.
- Source: general (round 1)
- Disposition notes: Fixed in 5a60e94c5. `Disabled` is bound to the action's active state.

## New findings

None in the fix delta.

## Duplicates / conflicts

- Single reviewer. Nothing was collapsed.
