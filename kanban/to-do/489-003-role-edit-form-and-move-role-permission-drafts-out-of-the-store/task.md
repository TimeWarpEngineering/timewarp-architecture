# Role edit form and move role permission drafts out of the store

## Description

- Add a `GetRole` fetch to the SPA (`GetRole.Response : IRoleDetails`) and an edit form next to
  the create-only `RoleForm`, using `UpdateRole.Command.From(IRoleDetails)`. Shows create vs
  edit side by side with the shared `RoleDetailsValidator`.
- Move `RoleState.DraftPermissionIds` / `DraftAgentScopes` out of the store into a working copy
  owned by the page (or a pending action); save dispatches the command built from it.
- Same treatment for `PrincipalState` role/scope checkbox drafts on `PrincipalDetailPage`.

## Depends on

- 489-002

## Checklist

- [ ] GetRole fetch + RoleState holds whole Response
- [ ] Role edit form
- [ ] Permission/scope drafts out of RoleState
- [ ] Principal role/scope drafts out of PrincipalState
- [ ] Tests

## Results

## Parent

- 489
