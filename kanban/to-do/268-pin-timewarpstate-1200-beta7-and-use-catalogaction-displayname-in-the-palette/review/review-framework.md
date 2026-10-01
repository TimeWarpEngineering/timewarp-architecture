# Review framework

## Budget (by-diff)

- Lines changed: 174
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 268

**Date:** 2026-10-01
**Host task:** kanban/to-do/268-pin-timewarpstate-1200-beta7-and-use-catalogaction-displayname-in-the-palette/
**Diff scope:** branch `task/268-pin-timewarpstate-1200-beta7-and-use-catalogaction` vs `master` (commit b2295d87)
**Plan / brief:** task.md Requirements 1–5 (pins, DisplayName precedence, labels, tests, stale-asset note)
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** ganda task work review oracle (Claude Opus 5.5), 2026-10-01

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
