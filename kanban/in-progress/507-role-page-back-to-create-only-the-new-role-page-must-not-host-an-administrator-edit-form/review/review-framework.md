# Review framework — task 507

**Date:** 2026-11-24
**Host task:** kanban/in-progress/507-role-page-back-to-create-only-the-new-role-page-must-not-host-an-administrator-edit-form/
**Diff scope:** branch `task/507-role-page-back-to-create-only-the-new-role-page` vs merge-base `159bae397` (master). Commits `43405bf09`, `aa3172daf`, `725d5c1da`. Product files: `RolePage.razor`, `RolePage.razor.cs`, `RoleEditForm.razor` (comment), `role-pages-playwright-tests.cs`, 489-003 `task.md`, task 507 kitchen and `evidence/`.
**Plan / brief:** New Role (`/Admin/Roles/New`) is create-only again. The Administrator edit form stays on `/Admin/Roles/{RoleId}`.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** grok session `01a20bb7-e67e-7780-be7e-d1d6934c4c56` (reviewer, 2026-11-24). Implementer session `27063109-2b43-4d3a-ac13-c8fbabf92fa3` (claude-opus-5-5).

## Budget (by-diff)

- Lines changed: 316
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
