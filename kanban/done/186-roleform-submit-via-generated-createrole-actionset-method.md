# RoleForm submit via generated CreateRole ActionSet method

## Description

RoleForm held `CreateRoleActionSet.Action` and `Mediator.Send(CreateAction)` so form
edits survived dispatch. That is not a special case. COPIC UX (EducationHistorySearchForm)
and `tw-web-api-contracts`: EditForm binds `IRoleDetails` (`CreateRole.Command` is the
model); Action ctor takes that Command; submit calls the generated
`RoleState.CreateRole(Command)`.

## Requirements

- Action ctor takes `CreateRole.Command`
- RoleForm binds `IRoleDetails` / Command, not the Action
- Submit is `await RoleState.CreateRole(Command)` — no `Mediator.Send`
- UserId stamped in handler `GetRequest`, not in the form (same as FetchRoles)

## Checklist

- [x] Action takes Command
- [x] RoleForm uses generated CreateRole
- [x] web-spa builds
- [x] Commit

## Results

RoleForm binds `CreateRole.Command` as `IRoleDetails` and submits with
`RoleState.CreateRole(Command)`. Action ctor takes the Command so the generator
emits that method. UserId is stamped in `GetRequest`.

### How to validate

```bash
rg -n "Mediator.Send|CreateRoleActionSet.Action" \
  source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleForm.razor
# Expect: no matches

rg -n "RoleState.CreateRole" \
  source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleForm.razor
# Expect: HandleValidSubmit calls RoleState.CreateRole(Command)

dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Debug
# Expect: 0/0
```

Browser (optional): `/Admin/Roles` → New Role → name/description → Save → land on
roles list with the new role.

## Session

- Implementation: grok 2026-08-13
