# Order agent scopes in Role.AgentScopes so Roles handlers stop reordering

## Description

`Role.AgentScopes` returns grants in stored order. Three Roles handlers reorder them by hand with
`AgentScopeOrder.OrderKnown` (`platform/authorization/agent-scope-order-application.cs`):

- `get-role/get-role-handler-application.cs` (projection)
- `get-roles/get-roles-handler-application.cs` (projection)
- `set-role-agent-scopes/set-role-agent-scopes-handler-application.cs` (input before
  `role.SetAgentScopes`, and the response)

Every read of a role's agent scopes wants catalog order (`AgentScopes.All`). Task 476 recorded the
rule in `tw-aggregate-pattern`: ordering and normalization that every read wants belong to the
root, not the handler. Move the ordering into the Role aggregate so the Roles handlers project
`role.AgentScopes` as is.

## Requirements

- `Role.AgentScopes` (or `SetAgentScopes`, so the stored list is already ordered) yields grants in
  `RoleCatalog.AgentScopes` order. The domain uses its own catalog copy
  (`role-catalog-domain.cs`), not `AgentScopeOrder`, because web-domain does not reference the
  application layer.
- GetRole, GetRoles, and SetRoleAgentScopes stop calling `AgentScopeOrder.OrderKnown` on role
  scopes. Unknown scopes are still rejected or dropped as today (keep the current behavior and say
  which in the Design region).
- `AgentScopeOrder` stays for Principals and `AgentScopeApproval`, which do not go through the Role
  aggregate. Update its Design region so it no longer lists the Roles callers.
- A domain unit test proves the order: set scopes in reverse catalog order, read them back in
  catalog order. Existing Roles handler tests still pass.
- Read order must not depend on EF load order of the owned collection.

## Checklist

- [ ] Ordering moved into `Role`; domain test added
- [ ] Three Roles handlers no longer reorder; Design regions updated
- [ ] `dev build` 0 warnings, 0 errors; `dev test` green
- [ ] `ganda repo audit` exits 0

## Session

- Created: 1002142 (2026-11-05)

## Notes

Filed from task 476 (decision A, analyzer alone). Evidence: 476 `proposal.md` sections 2 and 8.
