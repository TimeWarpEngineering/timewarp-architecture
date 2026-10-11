# Review framework

## Budget (by-diff)

- Lines changed: 2014
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 303

**Date:** 2026-10-11
**Host task:** kanban/to-do/303-agent-tools-mirror-ctrl-k-on-every-page-webmcp-and-ask-and-a-useful-page-context/
**Diff scope:** branch `task/303-agent-tools-mirror-ctrl-k-on-every-page-webmcp-and` vs `master` (commit e6adf7714)
**Plan / brief:** task.md Requirements + Steven's steering (Ctrl-K unchanged; global palette tools + `navigate` + page tools on every route; page-bound actions off-page return a navigate offer; text `page_context` on every page).
**Effort:** 3 (by-diff budget, 2014 lines)
**Reviewer roster:** general (deep pass)
**Session IDs:** review oracle Claude Code session (ganda task work, 2026-10-11)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
