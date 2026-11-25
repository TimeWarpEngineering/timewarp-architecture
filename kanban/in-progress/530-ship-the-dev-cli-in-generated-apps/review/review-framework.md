# Review framework — task 530

**Date:** 2026-11-26
**Host task:** kanban/in-progress/530-ship-the-dev-cli-in-generated-apps/
**Diff scope:** branch `task/530-ship-the-dev-cli-in-generated-apps` vs `master` (`master...HEAD`, 30 files, +1090/−364). Untracked kitchen files under `review/` are this round, not product scope.
**Plan / brief:** Ship `tools/dev-cli/` (and the root files it reads) in a generated app, drop template-only commands, and refuse or exclude commands whose feature flag is off. `dev template-smoke` builds that CLI and runs `dev build` for the default variant and `--matrix`.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** grok `01a2149c-8f0e-7c20-a0c6-5ae91cc3abd7`

## Budget (by-diff)

- Lines changed: 1454
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

## Record reviewed

Implementer `## Results` / `### How to validate` record `./bin/dev build`, targeted dev-cli tests, `./bin/dev template-smoke` and `--matrix` (six variants), `ganda repo audit`, and a manual packed-template pass over the flag-off refusals. That record covers the pack, the command list, and `dev build` through the generated CLI. It does not exercise `dev deploy migrate` on `--web false`. Gates were not re-run.
