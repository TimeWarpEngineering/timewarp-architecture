# Round 3 — general
**Date:** 2026-11-23
**Scope reviewed:** commits after round 2 on `task/489-003-role-edit-form-and-move-role-permission-drafts-out`:
`1807d3a06` (merge of `origin/feature/edit-forms-whole-object`) and `0fde1227a`
(`test(web-spa): action catalog expects Role.UpdateRole`). Prior findings M1 to M5 were re-read.

## Summary

The merge brings in 489-004 and 489-005 from the feature branch. Those changes were reviewed on
their own PRs (#660, #661). `git show --cc 1807d3a06` prints no combined hunks, so the merge
resolved no conflicts, and `git diff 659820c8a 1807d3a06` touches no file under
`features/admin/roles` or `features/admin/principals`. The 489-003 code reviewed in rounds 1 and 2
did not change.

`0fde1227a` adds `"Role.UpdateRole"` to `ActionCatalog_Should.ExpectedNames`. This task added
`[CatalogAction]` to `RoleState.UpdateRoleActionSet.Action`
(`role-state.update-role.cs:24`, agent visibility, `admin.roles.manage`), so the roster test needed
the new name. The entry sits in ordinal order between `Role.CreateRole` and
`Screenshot.CaptureScreenshot`, as `Enumerate_Expected_Names` requires. Only `CreateRole` and
`UpdateRole` carry `[CatalogAction]` under `features/admin`. The other catalog tests are not
affected. `catalog-agent-tests.cs` computes the `/Admin/Roles/New` tool list with
`ExpectedNamesAsync`, the developer-principal list lacks `admin.roles.manage`, and
`command-palette-tests.cs` excludes agent-visible actions by rule. The fix is correct and does not
weaken the test. It records a deliberate roster change, which is what the test's Design region
asks for.

Rounds 1 and 2 did not catch that the new `[CatalogAction]` breaks the roster test. CI on
PR #659 caught it, and that is why this round exists.

Prior findings: M1 to M4 stay fixed. The merge does not touch their files. M5 stays wontfix.

## Issues

### Issue 1 — Severity: nit
- File: kanban/in-progress/489-003-role-edit-form-and-move-role-permission-drafts-out-of-the-store/task.md (## Results)
- Description: `## Results` does not mention the feature-branch merge or `0fde1227a`, and the
  recorded gates (`--filter-class RoleState_Contract_Actions_Should` and
  `PrincipalActions_Should_`) do not include `ActionCatalog_Should`. The only validation for this
  commit is CI on PR #659. At review time CI on head `0fde1227a` was still pending
  (`gh pr checks 659`: `ci (integration)` pending).
- Suggestion: record the commit and name CI `ci (integration)` on PR #659 as its gate. The walk's
  merge waits for green CI.
- Status: open
