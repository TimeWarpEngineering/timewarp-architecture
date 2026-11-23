# Round 3 — merged findings
**Date:** 2026-11-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor:100
  (also PrincipalDetailPage.razor:69)
- Description: a successful save of one list recopied both page sets.
- Source: general (round 1)
- Disposition notes: fixed in `3df4d6fab`. The round-3 merge does not touch these files.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/role-state/role-state.update-role.cs:51
- Description: `Submitted` held the form's live command.
- Source: general (round 1)
- Disposition notes: fixed in `3df4d6fab`. Unchanged since.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleEditForm.razor:32
- Description: the form rebound only on a role id change.
- Source: general (round 1)
- Disposition notes: fixed in `3df4d6fab`. Unchanged since.

### M4 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/roles/role-state-contract-actions-tests.cs:181
  (also principal-state-tests.cs:144)
- Description: failed saves and `SetPrincipalAgentScopes` had no test.
- Source: general (round 1)
- Disposition notes: fixed in `3df4d6fab`. Unchanged since.

### M5 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleEditForm.razor:69
- Description: a failed UpdateRole copies the snapshot back over the form.
- Source: general (round 1)
- Disposition notes: wontfix (orchestrator, round 1). It follows the 489-002 `ProfilePage`
  pattern. A change belongs to parent 489 for both forms.

### M6 — Severity: nit — Status: fixed
- File: kanban/in-progress/489-003-role-edit-form-and-move-role-permission-drafts-out-of-the-store/task.md (## Results)
- Description: `## Results` did not record the feature-branch merge `1807d3a06` or the catalog
  test fix `0fde1227a`, and no recorded gate covered `ActionCatalog_Should`.
- Source: general (round 3)
- Disposition notes: fixed by the review oracle in the kitchen (task.md is a kitchen file; no
  product change). `## Results` now records both commits and names CI `ci (integration)` on
  PR #659 as the gate for the roster test. CI was pending at review time. The walk's merge waits
  for green CI.

## Duplicates / conflicts

- None.
