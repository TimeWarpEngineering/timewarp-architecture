# Review framework — task 483

**Date:** 2026-11-22
**Host task:** kanban/in-progress/483-option-4-contract-bound-action-sets-generated-from-contracts-464-option-c-on-state-3b/
**Diff scope:** branch `task/483-option-4-contract-bound-action-sets-generated-from` vs `master` (11 commits, 32 files, +2048/−186). Untracked kitchen only: this `review/` tree.
**Plan / brief:** `[ContractState]` source generator emits contract-bound action sets (Action, DefaultApiHandler, typed dispatch, required and optional hooks). Roles is the worked example. TWA0044–TWA0046 are the 464 Option A checks that still apply. Task 464 decision is recorded.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** review oracle grok `01a202fa-dc8d-74f1-b249-e3b3eb4e3fd4` (2026-11-22). Implementer sessions are on `task.md`.

## Budget (by-diff)

- Lines changed: 2234
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
