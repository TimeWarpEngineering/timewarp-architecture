# Review framework

## Budget (by-diff)

- Lines changed: 1103
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 070-007

**Date:** 2026-10-07
**Host task:** kanban/to-do/070-007-azure-container-apps-publish-target-with-managed-postgres-and-its-own-safety-suite/
**Diff scope:** branch task/070-007-… vs master (commit e1528cc83)
**Plan / brief:** task.md Requirements — ACA publish target, Flexible Server, AcaPublish_Given_ suite, `dev publish aca` + CI, deploy/deprovision aca, tw-deploy section
**Effort:** 3 (thorough), roster axes: general
**Reviewer roster:** general (Claude subagent, Opus 5.5)
**Session IDs:** review oracle (ganda task work, headless); general reviewer agent acb1530a096bb256a

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
