# Review framework

## Budget (by-diff)

- Lines changed: 128
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 278

**Date:** 2026-10-05
**Host task:** kanban/to-do/278-pin-timewarpstate-1200-beta8-and-opt-the-counter-js-demo-into-addjavascriptdispatch/
**Diff scope:** branch `task/278-…` commit facb975d vs master (product files; kanban excluded)
**Plan / brief:** task.md Requirements 1–5 (State beta.8 pin, AddJavaScriptDispatch allow-list + alias, tests, other-dispatcher check)
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** ganda task work review oracle (headless Claude Opus 5.5)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
