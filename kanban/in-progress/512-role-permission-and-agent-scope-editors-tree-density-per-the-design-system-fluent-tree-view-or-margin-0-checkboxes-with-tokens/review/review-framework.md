# Review framework — task 512

**Date:** 2026-11-24
**Host task:** kanban/in-progress/512-role-permission-and-agent-scope-editors-tree-density-per-the-design-system-fluent-tree-view-or-margin-0-checkboxes-with-tokens/
**Diff scope:** branch `task/512-role-permission-and-agent-scope-editors-tree` vs merge-base `1afe5834f` (master), commits through `c30a94acc`. 32 files, 1104 insertions, 319 deletions (1423 lines changed). Product surface is `PermissionTree`, the role detail page, the design-system catalogue, `tw-blazor`, and the web-spa tests named in `## Results`.
**Plan / brief:** task 512 — replace the hand-built role permission editor and the agent-scope checkbox row with one catalogued editable tree, at the design-system density, with a skill rule and a coverage test.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** review oracle grok `01a20e56-cad8-7181-b479-974da52dd732` (ganda session 853266). Implementer session is on `task.md`.

## Budget (by-diff)

- Lines changed: 1423
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
