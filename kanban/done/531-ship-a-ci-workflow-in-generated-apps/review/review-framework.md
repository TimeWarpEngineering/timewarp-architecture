# Review framework — task 531

**Date:** 2026-11-26
**Host task:** kanban/in-progress/531-ship-a-ci-workflow-in-generated-apps/
**Diff scope:** branch `task/531-ship-a-ci-workflow-in-generated-apps` vs `master` (merge-base `ec8488cef941e9df48942dfe97ae5f9f88864677`). Product commit `da69eb292`. Kitchen commit `e1cab85c2` moves the task. 22 files, +1042 / −100.
**Plan / brief:** Ship a purpose-built CI workflow and the shared `setup-ci` action in generated apps. Thin trigger over `dev workflow`. No template-repo jobs. Green-key decision recorded in Results.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle Grok `01a21747-afa1-7391-a314-fa0c42db50b1`; implementer Claude `582162b7-1afd-4c49-9dfa-857b3355486d`

## Budget (by-diff)

- Lines changed: 1142
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
