# Round 2 — general
**Date:** 2026-11-23
**Scope reviewed:** fix commit `3df4d6fab` (`source/` and `tests/`, 7 files, +218 / -21) against
`efb40c8e1`, plus the updated `## Results` and `### How to validate` record in `task.md`.

## Summary

The fix pass addresses the four open round-1 suggestions. Each save on `RoleDetailPage` and
`PrincipalDetailPage` now recopies only the list it saved. The load still copies both lists.
`UpdateRole` keeps the command it built and sent and reads that copy in `HandleSuccess`, so text
typed during the request no longer reaches `RoleSnapshot`. `RoleEditForm` rebinds when the loaded
role's id, name, or description differ from the values it last copied. A membership save keeps
name and description, so it does not rebind. New state tests cover failed `SetRolePermissions`,
`SetRoleAgentScopes`, and `SetPrincipalRoles` saves, the sent `UpdateRole` copy, and
`SetPrincipalAgentScopes`. The Purpose and Design regions match the new behavior. The recorded
gates cover the fix pass: build 0/0, both test classes by filter (8 and 9 tests), audit exit 0.

## Prior findings re-verified

- M1 fixed. `RoleDetailPage.OnSave` calls `CopyPermissionsFromSnapshot`, and `OnSaveAgentScopes`
  calls `CopyScopesFromSnapshot`. `PrincipalDetailPage.OnSave` calls `CopyRoleDraft`, and
  `OnSaveAgentScopes` calls `CopyScopeDraft`. `OnParametersSetAsync` returns early for an id that
  is already loaded, so the reopen after a principal save does not run `CopyDrafts`.
- M2 fixed. `GetRequest` assigns the new `Command` to `Submitted` and returns that same instance.
  `UpdateRole_Should_Store_The_Name_That_Was_Sent` changes the page command inside the scripted
  server's `BeforeResponse` hook and asserts that the snapshot keeps the sent text.
- M3 fixed. `OnParametersSet` compares id, name, and description with `BoundRoleId`, `BoundName`,
  and `BoundDescription`. `HasBoundCopy` covers the first bind. Save and Cancel go through `Bind`,
  so the bound values follow the snapshot. An agent rename now replaces any unsaved form text.
  The task's acceptance list states that behavior.
- M4 fixed. Three failed-save tests check the success flag and the stored lists, and
  `SetAgentScopes_Should_Send_The_Command_Scopes` checks the sent scopes and the success flag.
- M5 stays wontfix (round 1 rationale).

## Issues

None. The fix delta raised no new findings.

The restatement now includes the fix-pass behavior (recopy only the saved list, store the sent
command, rebind on an agent rename). Those details refine the task's Description and do not
change it, so there is no misread.
