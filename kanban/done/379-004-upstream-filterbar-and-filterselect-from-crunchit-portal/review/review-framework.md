# Review framework

## Budget (by-diff)

- Lines changed: 184
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-22
**Host task:** kanban/to-do/379-004-upstream-filterbar-and-filterselect-from-crunchit-portal/
**Diff scope:** branch `task/379-004-upstream-filterbar-and-filterselect-from-crunchit` vs `master`
(merge base; commit 10c9358c6), product paths under `source/` and `tests/`
**Plan / brief:** task.md `## Goal (implementer restatement)` + `## Checklist`: FilterBar
(debounced search, TimeProvider) + FilterSelect composites, admin feedback list adoption,
style-guide card, render + debounce tests, Playwright filter proof
**Effort:** 3 (by-diff budget above)
**Reviewer roster:** general
**Session IDs:** review oracle, headless `ganda task work` (session id not exposed)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
