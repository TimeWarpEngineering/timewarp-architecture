# Review framework — task 529

**Date:** 2026-11-26
**Host task:** kanban/in-progress/529-add-release-triggered-azure-deploy-for-generated-apps/
**Diff scope:** branch `task/529-add-release-triggered-azure-deploy-for-generated` vs `master` (three-dot). Product change is `e13bb8f7e` plus the kanban move/land commits. 17 files, 702 insertions, 188 deletions.
**Plan / brief:** task.md Description, Requirements, and `## Goal (implementer restatement)`. A generated app's workflow gains a release- or dispatch-gated `deploy` job that runs `dev deploy` under GitHub OIDC. `tw-deploy` matches that rule.
**Effort:** 3 (by-diff, 890 lines changed)
**Reviewer roster:** general
**Session IDs:** review oracle grok `01a2176e-f942-73f3-8fbe-c8fa2931659b` (2026-11-26). Implementer session is on `task.md`.

## Budget (by-diff)

- Lines changed: 890
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

## Restatement check

The implementer restatement matches the original task. The deploy input on `workflow_dispatch` keeps a manual CI run from deploying; the original rule is that a manual dispatch may deploy, and push and pull request never do. No misread recorded under `## Learnings`.
