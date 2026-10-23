# Review framework

## Budget (by-diff)

- Lines changed: 612
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 379-010

**Date:** 2026-10-23
**Host task:** kanban/to-do/379-010-upstream-integrationcard-and-syncbar-from-crunchit-portal/
**Diff scope:** branch `task/379-010-upstream-integrationcard-and-syncbar-from-crunchit` vs `master` (commit 1b86689a8), `source/` and `tests/`
**Plan / brief:** task.md Goal (implementer restatement), Decision, Adoption site, Checklist
**Effort:** 2 (by-diff budget), roster axis general only
**Reviewer roster:** general
**Session IDs:** review oracle claude-opus-5-5 (ganda task work, headless); session id not reported

### Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
