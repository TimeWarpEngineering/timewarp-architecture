# Role edit form and move role permission drafts out of the store

## Goal (implementer restatement)

The admin role screens should edit a copy of the role the server returned, and unsaved checkbox choices should live on the page that shows them.

`RoleState` gains a `GetRole` fetch and keeps that result as one `GetRole.Response` (`RoleSnapshot`). Only the fetch and the update handlers assign it. The New Role page shows the existing create form and a new edit form side by side. Both use `RoleDetailsValidator`. The edit form builds `UpdateRole.Command` with `UpdateRole.Command.From` when the loaded role id changes, on Cancel, and after Save. It does not copy on every render. `From` also copies `RoleId` from the get response, because that id is not an `IRoleDetails` field. The role detail page uses the same edit form for the role in the route.

Permission and agent-scope checkboxes no longer read or write `RoleState`. The detail page keeps those sets. Save sends `SetRolePermissions` or `SetRoleAgentScopes` built from the sets. A successful save updates the stored response; a failed save leaves the page sets as they are.

`PrincipalDetailPage` does the same for role and agent-scope checkboxes. `PrincipalState` no longer keeps `DraftRoleIds` or `DraftAgentScopes`. Opening a principal still stores the detail. The page copies the ids and scopes into its own sets, and Save sends the commands built from those sets.

Acceptance:

- `GetRole` stores the response object. `UpdateRole` changes that object's name and description and keeps its permission and agent-scope lists.
- Create and edit sit side by side on New Role, and both validate with `RoleDetailsValidator`. Cancel puts the edit form back to the stored role. Save sends `UpdateRole`.
- Role detail checkboxes start from the stored role. Toggling them does not change `RoleState`. Save sends the page's lists. A failed save does not reload those lists from the store.
- Principal detail checkboxes start from the opened principal. Save sends those ids and scopes. The store has no draft sets.

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

- [x] GetRole fetch + RoleState holds whole Response
- [x] Role edit form
- [x] Permission/scope drafts out of RoleState
- [x] Principal role/scope drafts out of PrincipalState
- [x] Tests

## Results

`RoleState` stores `GetRole.Response? RoleSnapshot`. `GetRole` assigns that response. `UpdateRole` rebuilds it from the submitted command and keeps the previous permission and agent-scope lists when the role id matches. `SetRolePermissions` and `SetRoleAgentScopes` are no longer draft requests: the generated handler sends the command the page built, and success replaces those lists on the open snapshot.

`RoleEditForm` builds `UpdateRole.Command` with `UpdateRole.Command.From` when the role id changes, on Cancel, and after Save. `CopyBeyondDetails` copies `RoleId` from `GetRole.Response`. New Role shows `RoleForm` and `RoleEditForm` side by side (`PageSplit`). The edit column loads the Administrator role. Role detail uses the same form when the caller can manage the role. Permission and agent-scope sets live on `RoleDetailPage`. A failed membership save does not copy the snapshot back over those sets.

`PrincipalState` no longer stores `DraftRoleIds` or `DraftAgentScopes`. `PrincipalDetailPage` copies role ids and agent scopes from the opened principal. Save sends `SetPrincipalRoles` or `SetPrincipalAgentScopes` built from those sets, then reopens the principal and copies the effective ids.

### How to validate

Smoke: sign in with `admin.roles.manage` and `admin.principals.manage`. Open `/Admin/Roles/New`. Change the Administrator name and click Cancel, then change it and click Save. Open that role's detail page, toggle a permission that is not a protected core, reload without Save, then toggle it and click Save. Open a principal, toggle a role, reload without Save, then toggle it and click Save.

Expect: New Role shows Create and Edit side by side. Both forms validate name and description. Cancel restores the stored Administrator name. Save keeps that name and leaves the permission and agent-scope lists as they were. On the detail page, a reload before Save restores the stored checkboxes. Save keeps the toggled grant. The principal page behaves the same: reload before Save restores the opened roles and scopes, and Save sends the page's lists. A read-only roles principal sees the description and disabled checkboxes, not the edit form.

The SPA was not exercised in a browser in this walk.

Gates:

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `dotnet run --file source/container-apps/web/features/admin/roles/update-role/update-role-validator-tests.cs` — passed, 6 tests, including `From_GetRole_Should_Copy_Name_Description_And_RoleId`.
- `tests/container-apps/web/web-spa-integration-tests/features/admin/roles/role-state-contract-actions-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). `./bin/dev test` does not take a file. CI runs the project. `dotnet run --project tests/container-apps/web/web-spa-integration-tests -c Release -- --filter-class RoleState_Contract_Actions_Should` — passed, 5 tests.
- `tests/container-apps/web/web-spa-integration-tests/features/admin/principal-state-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). CI runs the project. `dotnet run --project tests/container-apps/web/web-spa-integration-tests -c Release --no-build -- --filter-class PrincipalActions_Should_` — passed, 7 tests.
- `tests/container-apps/web/web-spa-integration-tests/serialization/state-json-context-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). CI runs the project. `dotnet run --project tests/container-apps/web/web-spa-integration-tests -c Release --no-build -- --filter-class Serialize_Should` — passed, 1 test (`Serialize_Every_State_Without_Reflection`).
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing). `analyzer-suppressions` passed: 843 suppressions, none above the baseline.

## Session

- Implementation: Grok 4.7, 2026-11-23. The role store keeps one `GetRole.Response`. New Role shows create and edit side by side. Permission, agent-scope, and principal checkbox drafts live on the page. Save sends the command the page built.

## Parent

- 489
