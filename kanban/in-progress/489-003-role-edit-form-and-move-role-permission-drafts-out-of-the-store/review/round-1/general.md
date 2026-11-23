# Round 1 — general
**Date:** 2026-11-23
**Scope reviewed:** same as the framework (`efb40c8e1` against `927fe88f4`, `source/` and `tests/`).

## Summary

`RoleState` now stores one `GetRole.Response` (`RoleSnapshot`). Only GetRole, UpdateRole, and the two
membership saves assign it. `RoleEditForm` copies `UpdateRole.Command.From` when the role id changes,
on Cancel, and after Save. New Role shows create and edit side by side. Permission, agent-scope, and
principal role and scope checkboxes are page-owned sets, and the save commands carry those sets.
The store-side draft dictionaries and toggle actions are gone; no references to them are left.
The change matches the task and the restatement, and the recorded gates (build, three targeted
test classes, the `From` runfile, audit) cover it. No bugs. The open findings are about keeping a
page's unsaved edits across a save, a race in the UpdateRole handler, a form that goes stale when an
agent updates the role, and no test for the failed-save rule in the acceptance list.

No commands were run beyond read-only `git diff`, `git log`, and `rg`.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor:73
  (also PrincipalDetailPage.razor:31)
- Description: after a successful save of one list, the page recopies both sets from the store.
  `OnSave` (permissions, line 96) and `OnSaveAgentScopes` (line 123) both call
  `CopyMembershipFromSnapshot`, which replaces `PermissionDraft` and `ScopeDraft`. A user who toggles
  an agent scope, then toggles a permission and clicks the permissions Save, loses the scope toggle
  without a message: `ScopeDraft` is reset to `RoleSnapshot.AgentScopes`, which the permission save
  did not change. `PrincipalDetailPage.OnSave` and `OnSaveAgentScopes` do the same through
  `CopyDrafts`. The old store code had the same effect (GetRoles and OpenPrincipal reseeded both
  dictionaries), so this is not new, but the sets now live on the page, and the page can keep the
  set it did not save.
- Suggestion: split the copy so each save recopies only the list it sent (for example
  `CopyPermissionsFromSnapshot` and `CopyScopesFromSnapshot`, and the same split for the principal
  page). The initial load still copies both.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/roles/role-state/role-state.update-role.cs:51
- Description: `GetRequest` stores `action.Command`, which is the form's live `UpdateRole.Command`,
  in `Submitted`. `HandleSuccess` builds the new snapshot from `submitted.Name` and
  `submitted.Description` after the HTTP call returns. `RoleEditForm` disables the Save and Cancel
  buttons while saving but not the text inputs. A user who types during the request makes the
  stored snapshot hold the typed text, not the text the server saved. The next Cancel or the
  post-save copy then shows a name the server never received.
- Suggestion: keep the copy that `GetRequest` builds and sends (the new `Command` with the stamped
  `UserId`) in `Submitted`, and build the snapshot from that copy.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleEditForm.razor:29
- Description: the form rebinds only when the role id changes. `RolePage` and `RoleDetailPage` both
  register `RoleState.UpdateRoleActionSet.Action` as a page agent action. When the agent renames the
  open role, `RoleSnapshot` changes but the form keeps the old name and description. If the user
  then clicks Save, the form sends the old text back and undoes the agent's rename.
- Suggestion: remember the name and description the form last copied from, and rebind in
  `OnParametersSet` when the snapshot's name or description differ from them. A membership save
  replaces the snapshot but keeps name and description, so it would not wipe typed edits.
  Alternatively, rebind when the snapshot instance changes and the form has no unsaved edit.
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/roles/role-state-contract-actions-tests.cs:94
  (also principal-state-tests.cs:115)
- Description: the acceptance list says a failed save does not reload the page lists from the store.
  The page relies on two state facts: the success flag stays false and `RoleSnapshot` is unchanged
  when the server returns problem details. No test sends a failing `SetRolePermissions`,
  `SetRoleAgentScopes`, or `SetPrincipalRoles`. The new tests cover only the success path. There is
  also no test for `SetPrincipalAgentScopes` sending the page's scopes.
- Suggestion: add a failing-save case to `RoleState_Contract_Actions_Should` (the scripted server
  returns `SharedProblemDetails` for that command). Assert that `LastSetRolePermissionsSucceeded` is
  false and that `RoleSnapshot.Permissions` still holds the fetched list. Add the same case for
  agent scopes, and a `SetPrincipalAgentScopes` send test beside `SetRoles_Should_Send_The_Command_Role_Ids`.
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleEditForm.razor:43
- Description: `HandleValidSubmit` copies from the snapshot after every save, including a failed one,
  so a server error throws away the name and description the user typed. The design comment states
  this, and it matches the `ProfilePage` pattern landed by 489-002 in the same epic. On the same
  page, the checkbox sets are kept after a failed save, so the two editors handle failure
  differently.
- Suggestion: if the epic wants failed saves to keep edits, add a `LastUpdateRoleSucceeded`-style
  gate here and in `ProfilePage` together, not in one form alone.
- Status: open
