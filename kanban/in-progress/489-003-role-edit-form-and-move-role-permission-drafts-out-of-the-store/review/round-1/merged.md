# Round 1 — merged findings
**Date:** 2026-11-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 4 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor:73
  (also PrincipalDetailPage.razor:31)
- Description: a successful save of one list recopies both page sets from the store, so an unsaved
  toggle in the other list is lost without a message. Example: toggle an agent scope, toggle a
  permission, click the permissions Save, and the scope toggle reverts. `PrincipalDetailPage` does
  the same for roles and agent scopes through `CopyDrafts`. The old store code behaved the same way.
- Suggestion: recopy only the list that was saved. The initial load still copies both.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/roles/role-state/role-state.update-role.cs:51
- Description: `Submitted` holds the form's live `UpdateRole.Command`. `HandleSuccess` reads its name
  and description after the request returns. The text inputs stay enabled during the save, so
  anything typed while the request is in flight goes into `RoleSnapshot` instead of the text the
  server saved.
- Suggestion: store the copy built in `GetRequest` (the command actually sent) in `Submitted`.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleEditForm.razor:29
- Description: the form rebinds only when the role id changes. Both role pages expose UpdateRole as an
  agent action. When the agent renames the open role, the form keeps the old text, and the user's
  next Save undoes the agent's rename.
- Suggestion: rebind when the snapshot's name or description differ from the values the form last
  copied from. A membership save keeps name and description, so it would not trigger a rebind.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/roles/role-state-contract-actions-tests.cs:94
  (also principal-state-tests.cs:115)
- Description: the acceptance rule that a failed save does not reload the page lists has no test.
  No test sends a failing `SetRolePermissions`, `SetRoleAgentScopes`, or `SetPrincipalRoles`, and
  `SetPrincipalAgentScopes` sending the page's scopes has no test.
- Suggestion: script a `SharedProblemDetails` answer. Assert that the success flag is false and that
  `RoleSnapshot` still holds the fetched lists. Add a `SetPrincipalAgentScopes` send test.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleEditForm.razor:43
- Description: a failed UpdateRole copies the snapshot back over the form, so the user's typed name
  and description are lost. The checkbox editors on the same page keep their sets after a failure.
- Suggestion: if failed saves should keep edits, gate the copy on a success flag in both
  `RoleEditForm` and `ProfilePage`.
- Source: general
- Disposition notes: wontfix (orchestrator, round 1). The form's Design comment states this
  behavior, and it matches the `ProfilePage` pattern that 489-002 landed in the same epic
  (`feature/edit-forms-whole-object`). Changing one form alone would split the epic's edit-form
  pattern. If the behavior should change, it changes at the epic level (parent 489) for both forms.

## Duplicates / conflicts

- None. A single reviewer ran. The RoleDetailPage and PrincipalDetailPage cases of the same pattern
  are folded into M1, and the role and principal test gaps are folded into M4.
