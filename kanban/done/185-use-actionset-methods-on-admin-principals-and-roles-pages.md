# Use ActionSet methods on admin principals and roles pages

## Description

Admin Principals and Roles list pages dispatched TimeWarp.State actions via
`Mediator.Send(new XxxState.YyyActionSet.Action(...))`. The ActionSet method
generator already emits `PrincipalState.SetRoleSelected`, `SetPrincipalRoles`,
`RoleState.SetPermissionSelected`, and `SetRolePermissions`. `BaseComponent.Send`
is obsolete for this reason: ActionSet methods wire cancellation tokens and are
the UI surface.

## Requirements

- PrincipalsPage checkbox/save call generated ActionSet methods
- RolesListPage checkbox/save call generated ActionSet methods
- Do not change RoleForm: it holds `CreateRoleActionSet.Action` as the EditForm
  model (Command is the bind target), so sending that instance is required

## Checklist

- [x] PrincipalsPage uses `SetRoleSelected` / `SetPrincipalRoles`
- [x] RolesListPage uses `SetPermissionSelected` / `SetRolePermissions`
- [x] Commit

## Notes

Task 184 fixed FluentCheckbox `Value`/`ValueChanged` bind. This is the follow-on
style fix: same pipeline, generated wrapper instead of raw `Send`.

RoleForm still `Mediator.Send(CreateAction)` — the form binds `CreateAction.Command`
as `IRoleDetails`; a fresh `RoleState.CreateRole()` would drop the edited command.

## Results

Principals and Roles list pages call the generated ActionSet methods. Same
`Sender.Send(Action)` path, with linked cancellation from the state.

### How to validate

```bash
rg -n "Mediator.Send" \
  source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalsPage.razor \
  source/container-apps/web/projects/web-spa/features/admin/roles/pages/RolesListPage.razor
# Expect: no matches

rg -n "SetRoleSelected|SetPrincipalRoles|SetPermissionSelected|SetRolePermissions" \
  source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalsPage.razor \
  source/container-apps/web/projects/web-spa/features/admin/roles/pages/RolesListPage.razor
# Expect: PrincipalsPage OnRoleSelected/OnSave and RolesListPage OnPermissionSelected/OnSave
```

Browser (optional, no behavior change vs 184): `/Admin/Principals` check a role, Save,
refresh — assignment still persists. Same for `/Admin/Roles` permissions.

## Session

- Implementation: grok 2026-08-13
