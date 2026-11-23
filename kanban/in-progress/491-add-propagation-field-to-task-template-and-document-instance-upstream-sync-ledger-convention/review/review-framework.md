# Review framework

## Budget (by-diff)

- Lines changed: 242
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-24
**Host task:** kanban/in-progress/491-add-propagation-field-to-task-template-and-document-instance-upstream-sync-ledger-convention/
**Diff scope:** branch `task/491-add-propagation-field-to-task-template-and` vs `master` (merge base), commits 4fff98a8a..3be9dd9d1
**Plan / brief:** task.md `## Goal (implementer restatement)`: `## Propagation` in `kanban/task-template.md`, the instance upstream-ledger convention in `kanban/readme.md`, the template link, and `kanban/readme.md` shipped in the board skeleton (pack csproj + `AssertKanbanSkeletonShipped`).
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle, Claude Code (claude-opus-5-5), headless print mode under `ganda task work`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
