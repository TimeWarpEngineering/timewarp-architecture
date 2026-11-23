# Round 2 — merged findings
**Date:** 2026-11-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor:100
  (also PrincipalDetailPage.razor:69)
- Description: a successful save of one list recopied both page sets.
- Source: general (round 1)
- Disposition notes: fixed in `3df4d6fab`. Each save recopies only its list. The load copies both.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/role-state/role-state.update-role.cs:51
- Description: `Submitted` held the form's live command.
- Source: general (round 1)
- Disposition notes: fixed in `3df4d6fab`. `Submitted` is the copy that was sent. Covered by
  `UpdateRole_Should_Store_The_Name_That_Was_Sent`.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleEditForm.razor:32
- Description: the form rebound only on a role id change, so an agent rename was not shown.
- Source: general (round 1)
- Disposition notes: fixed in `3df4d6fab`. The form rebinds when id, name, or description differ
  from the values it last copied.

### M4 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/roles/role-state-contract-actions-tests.cs:181
  (also principal-state-tests.cs:144)
- Description: failed saves and `SetPrincipalAgentScopes` had no test.
- Source: general (round 1)
- Disposition notes: fixed in `3df4d6fab`. Four tests added. The recorded filtered runs pass
  (8 and 9 tests).

### M5 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleEditForm.razor:69
- Description: a failed UpdateRole copies the snapshot back over the form.
- Source: general (round 1)
- Disposition notes: wontfix (orchestrator, round 1). It follows the 489-002 `ProfilePage`
  pattern. Changing it is an epic-level (489) decision for both forms.

## Duplicates / conflicts

- None. No new findings in round 2.
